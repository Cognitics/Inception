
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Features;
using UnityEngine;

namespace Cognitics.UnityCDB
{
    public class TileDataCache
    {
        public class Request
        {
            public CDB.Tile Tile;
            public CDB.Component Component;
            public DateTime LastRequested = DateTime.UtcNow;
            public static bool operator ==(Request a, Request b)
            {
                return (a.Tile == b.Tile) && (a.Component == b.Component);
            }
            public static bool operator !=(Request a, Request b) => !(a == b);

            public override int GetHashCode() => System.Tuple.Create(Tile, Component).GetHashCode();
            public override bool Equals(object obj) => (obj is Request) && (this == (Request)obj);
        }

        public abstract class Entry { }
        public class Entry<T> : Entry { public T data; }

        public int MaximumTasks = 4;
        public int tileLifeSpan = 30;
        public int MaximumEntries = 100;

        public string OnlineElevationServer;
        public string OnlineElevationLayer;
        public string OnlineImageryServer;
        public string OnlineImageryLayer;

        public List<Request> WaitingRequests = new List<Request>();
        public List<Request> LoadedRequestsMRU = new List<Request>();

        public ConcurrentDictionary<Request, Entry> Entries = new ConcurrentDictionary<Request, Entry>();
        public ConcurrentDictionary<Request, Task> RunningRequests = new ConcurrentDictionary<Request, Task>();

        public bool IsLoaded(CDB.Tile tile, CDB.Component component) => IsLoaded(new Request { Tile = tile, Component = component });
        private bool IsLoaded(Request request) => Entries.ContainsKey(request);
        private bool IsRunning(Request request) => RunningRequests.ContainsKey(request);

        private void WaitingRequestsAddOrUpdate(Request request)
        {
            Monitor.Enter(WaitingRequests);
            WaitingRequests.Remove(request);
            WaitingRequests.Add(request);
            Monitor.Exit(WaitingRequests);
        }

        private void LoadedRequestsAddOrUpdate(Request request)
        {
            Monitor.Enter(LoadedRequestsMRU);
            LoadedRequestsMRU.Remove(request);
            LoadedRequestsMRU.Add(request);
            Monitor.Exit(LoadedRequestsMRU);
        }

        private void ExpireEntries()
        {
            int tile = 0;

            Monitor.Enter(LoadedRequestsMRU);

            for (; tile < LoadedRequestsMRU.Count; ++tile)
            {
                int age = (int)(DateTime.UtcNow - LoadedRequestsMRU[tile].LastRequested).TotalSeconds;

                if (age < tileLifeSpan)
                    break;
            }

            for (int i = 0; i < tile; ++i)
            {
                Entry e;
                Entries.TryRemove(LoadedRequestsMRU[0], out e);
                LoadedRequestsMRU.Remove(LoadedRequestsMRU[0]);
            }

            if (LoadedRequestsMRU.Count > MaximumEntries)
            {
                Entry e;
                Entries.TryRemove(LoadedRequestsMRU[0], out e);
                LoadedRequestsMRU.Remove(LoadedRequestsMRU[0]);
            }

            Monitor.Exit(LoadedRequestsMRU);
        }

        public void RequestEntry(CDB.Tile tile, CDB.Component component)
        {
            var request = new Request { Tile = tile, Component = component };
            if (IsLoaded(request))
                return;
            if (IsRunning(request))
                return;
            WaitingRequestsAddOrUpdate(request);
        }


        public Entry<T> GetEntry<T>(CDB.Tile tile, CDB.Component component)
        {
            var request = new Request { Tile = tile, Component = component };
            if (Entries.TryGetValue(request, out Entry result))
            {
                request.LastRequested = DateTime.UtcNow;
                LoadedRequestsAddOrUpdate(request);
                return (Entry<T>)result;
            }
            if(IsRunning(request))
                return null;
            WaitingRequestsAddOrUpdate(request);
            return null;
        }

        public List<Request> Run()
        {
            ExpireEntries();
            var completedRequests = new List<Request>();
            foreach (var runningRequest in RunningRequests)
            {
                if (runningRequest.Value.IsCompleted)
                    completedRequests.Add(runningRequest.Key);
            }
            completedRequests.ForEach(r =>
            {
                Task removed;
                RunningRequests.TryRemove(r, out removed);
                LoadedRequestsAddOrUpdate(r);
            });
            if (WaitingRequests.Count <= 0)
                return completedRequests;
            if (RunningRequests.Count >= MaximumTasks)
                return completedRequests;
            Request request = WaitingRequests[WaitingRequests.Count - 1];
            WaitingRequests.RemoveAt(WaitingRequests.Count - 1);
            RunningRequests[request] = Task.Run(() => TaskRun(request));
            return completedRequests;
        }

        ////////////////////////////////////////////////////////////

        private void TaskRun(Request request)
        {
            //Console.WriteLine("[TileDataCache] LOAD " + request.Tile.Name + " " + request.Component.Name);
            if (request.Component is CDB.PrimaryTerrainElevation)
                TaskRun_PrimaryTerrainElevation(request);
            if (request.Component is CDB.YearlyVstiRepresentation)
                TaskRun_YearlyVstiRepresentation(request);
            if (request.Component is CDB.VectorComponentFeatures)
                TaskRun_VectorComponentFeatures(request);
            if (request.Component is CDB.VectorComponentClassAttributes)
                TaskRun_VectorComponentClassAttributes(request);
            if (request.Component is CDB.VectorComponentExtendedAttributes)
                TaskRun_VectorComponentExtendedAttributes(request);
        }

        private void TaskRun_PrimaryTerrainElevation(Request request)
        {
            try
            {
                var component = (CDB.PrimaryTerrainElevation)request.Component;
                if ((OnlineElevationServer != null) && !component.Exists(request.Tile))
                {
                    string uri = string.Format("{0}?SERVICE=WCS&VERSION=1.0.0&CRS=EPSG:4326&REQUEST=GetCoverage&FORMAT=GeoTIFF&COVERAGE={1}{2}{3}",
                        OnlineElevationServer,
                        WebUtility.UrlEncode(OnlineElevationLayer),
                        string.Format("&WIDTH={0}&HEIGHT={1}", request.Tile.RasterDimension, request.Tile.RasterDimension),
                        string.Format("&BBOX={0},{1},{2},{3}",
                            (double)request.Tile.Bounds.MinimumCoordinates.Longitude,
                            (double)request.Tile.Bounds.MinimumCoordinates.Latitude,
                            (double)request.Tile.Bounds.MaximumCoordinates.Longitude,
                            (double)request.Tile.Bounds.MaximumCoordinates.Latitude));
                    var web = (HttpWebRequest)WebRequest.Create(uri);
                    var response = (HttpWebResponse)web.GetResponse();
                    if (response.ContentType == "image/tiff")
                    {
                        Debug.Log(uri);
                        string filename = component.Filename(request.Tile);
                        string filepath = Path.GetDirectoryName(filename);
                        if (!Directory.Exists(filepath))
                            Directory.CreateDirectory(filepath);
                        using (var s = response.GetResponseStream())
                        {
                            using (var m = new MemoryStream())
                            {
                                s.CopyTo(m);
                                if (m.Length < 1024 * 1024 * sizeof(float) * 2)
                                    File.WriteAllBytes(filename, m.ToArray());
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("FAILED: " + uri);
                        using (var reader = new StreamReader(response.GetResponseStream()))
                            Debug.Log(reader.ReadToEnd());
                    }
                }
                float[] result = null;
                if (component.Exists(request.Tile))
                {
                    float[] source = component.Read(request.Tile);
                    result = new float[request.Tile.MeshDimension * request.Tile.MeshDimension];
                    for (int row = 0; row < request.Tile.RasterDimension; ++row)
                    {
                        for (int column = 0; column < request.Tile.RasterDimension; ++column)
                            result[(row * request.Tile.MeshDimension) + column] = source[((request.Tile.RasterDimension - row - 1) * request.Tile.RasterDimension) + column];
                        result[(row * request.Tile.MeshDimension) + request.Tile.RasterDimension] = result[(row * request.Tile.MeshDimension) + request.Tile.RasterDimension - 1];
                    }
                    for (int column = 0; column < request.Tile.MeshDimension; ++column)
                        result[(request.Tile.RasterDimension * request.Tile.MeshDimension) + column] = result[((request.Tile.RasterDimension - 1) * request.Tile.MeshDimension) + column];
                }
                Entries[request] = new Entry<float[]>() { data = result };
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void TaskRun_YearlyVstiRepresentation(Request request)
        {
            try
            {
                var component = (CDB.YearlyVstiRepresentation)request.Component;
                if ((OnlineImageryServer != null) && !component.Exists(request.Tile) && !component.AlternateExists(request.Tile))
                {
                    string uri = string.Format("{0}?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&SRS=EPSG:4326&Format=image/tiff&LAYERS={1}{2}{3}",
                        OnlineImageryServer,
                        WebUtility.UrlEncode(OnlineImageryLayer),
                        string.Format("&WIDTH={0}&HEIGHT={1}", request.Tile.RasterDimension, request.Tile.RasterDimension),
                        string.Format("&BBOX={0},{1},{2},{3}",
                            (double)request.Tile.Bounds.MinimumCoordinates.Longitude,
                            (double)request.Tile.Bounds.MinimumCoordinates.Latitude,
                            (double)request.Tile.Bounds.MaximumCoordinates.Longitude,
                            (double)request.Tile.Bounds.MaximumCoordinates.Latitude));
                    var web = (HttpWebRequest)WebRequest.Create(uri);
                    var response = (HttpWebResponse)web.GetResponse();
                    if (response.ContentType == "image/tiff")
                    {
                        Debug.Log(uri);
                        string filename = component.AlternateFilename(request.Tile);
                        string filepath = Path.GetDirectoryName(filename);
                        if (!Directory.Exists(filepath))
                            Directory.CreateDirectory(filepath);
                        using (var s = response.GetResponseStream())
                        {
                            using (var m = new MemoryStream())
                            {
                                s.CopyTo(m);
                                if (m.Length < 1024 * 1024 * 3 * 2)
                                    File.WriteAllBytes(filename, m.ToArray());
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("FAILED: " + uri);
                        using (var reader = new StreamReader(response.GetResponseStream()))
                            Debug.Log(reader.ReadToEnd());
                    }
                }
                byte[] result = null;
                if (component.Exists(request.Tile))
                {
                    byte[] source = component.Read(request.Tile);
                    result = new byte[source.Length];
                    for (int row = 0; row < request.Tile.RasterDimension; ++row)
                    {
                        for (int column = 0; column < request.Tile.RasterDimension; ++column)
                        {
                            int dataIndex = ((row * request.Tile.RasterDimension) + column) * 3;
                            int sourceIndex = (((request.Tile.RasterDimension - row - 1) * request.Tile.RasterDimension) + column) * 3;
                            result[dataIndex + 0] = source[sourceIndex + 0];
                            result[dataIndex + 1] = source[sourceIndex + 1];
                            result[dataIndex + 2] = source[sourceIndex + 2];
                        }
                    }
                }
                if (component.AlternateExists(request.Tile))
                {
                    try
                    {
                        result = component.AlternateRead(request.Tile);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                Entries[request] = new Entry<byte[]>() { data = result };
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
                    
        }

        private void TaskRun_VectorComponentFeatures(Request request)
        {
            var component = (CDB.VectorComponentFeatures)request.Component;
            List<Feature> result = null;
            if (component.Exists(request.Tile))
                result = component.Read(request.Tile);
            Entries[request] = new Entry<List<Feature>>() { data = result };
        }

        private void TaskRun_VectorComponentClassAttributes(Request request)
        {
            var component = (CDB.VectorComponentClassAttributes)request.Component;
            Dictionary<string, AttributesTable> result = null;
            if (component.Exists(request.Tile))
                result = component.Read(request.Tile);
            Entries[request] = new Entry<Dictionary<string, AttributesTable>>() { data = result };
        }

        private void TaskRun_VectorComponentExtendedAttributes(Request request)
        {
            var component = (CDB.VectorComponentExtendedAttributes)request.Component;
            Dictionary<int, AttributesTable> result = null;
            if (component.Exists(request.Tile))
                result = component.Read(request.Tile);
            Entries[request] = new Entry<Dictionary<int, AttributesTable>>() { data = result };
        }
    }
}
