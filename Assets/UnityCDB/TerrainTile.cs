
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using UnityEngine;
using Cognitics.CoordinateSystems;
using Cognitics.GeoPackage;
using BitMiracle.LibTiff.Classic;

namespace Cognitics.UnityCDB
{
    public class TerrainTileData
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uv;
        public int[] triangles;
        public Color[] pixels;
        public float[] perimeter;
    }

    public class TerrainTile
    {
        public GeoPackage.Database Database = null;
        public RasterLayer Elevation = null;
        public RasterLayer Imagery = null;
        public ScaledFlatEarthProjection Projection;

        public ConcurrentDictionary<QuadTreeNode, TerrainTileData> DataByTile = new ConcurrentDictionary<QuadTreeNode, TerrainTileData>();

        private List<TileMatrix> ElevationTileMatrixSet = null;

        public TerrainTile(GeoPackage.Database database)
        {
            Database = database;
            foreach (var layer in Database.Layers("2d-gridded-coverage"))
            {
                if (layer.SpatialReferenceSystemID != 4326)
                    continue;
                Elevation = (RasterLayer)layer;
            }
            foreach (var layer in Database.Layers("tiles"))
            {
                if (layer.SpatialReferenceSystemID != 4326)
                    continue;
                Imagery = (RasterLayer)layer;
            }
            if (Elevation == null)
                return;
            ElevationTileMatrixSet = Elevation.TileMatrices().ToList();
        }

        public void QuadTreeDataUpdate(QuadTreeNode tile)       // QuadTreeDelegate
        {
            // TODO: any update operations (regardless of state)
        }

        public void QuadTreeDataLoad(QuadTreeNode tile)       // QuadTreeDelegate
        {
            Task.Run(() => TaskLoad(tile));
        }

        public void QuadTreeDataLoaded(QuadTreeNode tile)       // QuadTreeDelegate
        {
            // TODO
            /*
            var meshRenderer = tile.gameObject.AddComponent<MeshRenderer>();
            var meshFilter = tile.gameObject.AddComponent<MeshFilter>();
            var mesh = meshFilter.mesh;
            var data = DataByTile[tile];
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = data.vertices;
            mesh.triangles = data.triangles;
            mesh.uv = data.uv;
            */

            // TODO: imagery

        }

        public void QuadTreeDataUnload(QuadTreeNode tile)       // QuadTreeDelegate
        {
            // TODO
            //tile.gameObject.GetComponent<MeshFilter>().mesh = null;
        }

        private void TaskLoad(QuadTreeNode tile)
        {
            DataByTile[tile] = new TerrainTileData();
            GenerateMesh(tile);
            ApplyElevation(tile);

            tile.IsLoaded = true;
            tile.IsLoading = false;
        }

        private void GenerateMesh(QuadTreeNode tile)
        {
            var data = DataByTile[tile];

            TileMatrix tileMatrix = ElevationTileMatrixSet[tile.Depth];
            int MeshDimension = (int)tileMatrix.TileWidth;

            CartesianBounds cartesianBounds = tile.GeographicBounds.TransformedWith(Projection);
            double spacingX = (cartesianBounds.MaximumCoordinates.X - cartesianBounds.MinimumCoordinates.X) / tileMatrix.TileWidth;
            double spacingY = (cartesianBounds.MaximumCoordinates.Y - cartesianBounds.MinimumCoordinates.Y) / tileMatrix.TileHeight;
            double originX = cartesianBounds.MinimumCoordinates.X;
            double originY = cartesianBounds.MinimumCoordinates.Y;

            // vertices
            {
                data.vertices = new Vector3[MeshDimension * MeshDimension];
                int vertexIndex = 0;
                for (int row = 0; row < MeshDimension; ++row)
                    for (int column = 0; column < MeshDimension; ++column, ++vertexIndex)
                        data.vertices[vertexIndex] = new Vector3((float)(originX + (column * spacingX)), 0.0f, (float)(originY + (row * spacingY)));
            }

            // triangles
            {
                data.triangles = new int[(MeshDimension - 1) * (MeshDimension - 1) * 6];
                int triangleIndex = 0;
                for (int row = 0; row < (MeshDimension - 1); ++row)
                {
                    for (int column = 0; column < (MeshDimension - 1); ++column, triangleIndex += 6)
                    {
                        int vertexIndex = (row * MeshDimension) + column;

                        int lowerLeftIndex = vertexIndex;
                        int lowerRightIndex = lowerLeftIndex + 1;
                        int upperLeftIndex = lowerLeftIndex + MeshDimension;
                        int upperRightIndex = upperLeftIndex + 1;

                        data.triangles[triangleIndex + 0] = lowerLeftIndex;
                        data.triangles[triangleIndex + 1] = upperLeftIndex;
                        data.triangles[triangleIndex + 2] = upperRightIndex;

                        data.triangles[triangleIndex + 3] = lowerLeftIndex;
                        data.triangles[triangleIndex + 4] = upperRightIndex;
                        data.triangles[triangleIndex + 5] = lowerRightIndex;
                    }
                }
            }

            // uvs
            {
                data.uv = new Vector2[data.vertices.Length];
                int vertexIndex = 0;
                for (int row = 0; row < MeshDimension; ++row)
                    for (int column = 0; column < MeshDimension; ++column, ++vertexIndex)
                        data.uv[vertexIndex] = new Vector2((float)column / (MeshDimension - 1), (float)row / (MeshDimension - 1));
            }

        }

        private void ApplyElevation(QuadTreeNode tile)
        {
            var data = DataByTile[tile];

            TileMatrix tileMatrix = ElevationTileMatrixSet[tile.Depth];
            long zoomLevel = tile.Depth;
            long matrixRow = (long)Mathf.Floor((float)(tile.GeographicBounds.MinimumCoordinates.Latitude - Elevation.MinY) / tileMatrix.TilesHigh);
            long matrixColumn = (long)Mathf.Floor((float)(tile.GeographicBounds.MinimumCoordinates.Longitude - Elevation.MinX) / tileMatrix.TilesWide);
            var elevation = Elevation.Tile(zoomLevel, matrixRow, matrixColumn);

            //File.WriteAllBytes("D:/elevtest.dat", elevation.Bytes);

            int meshDimension = (int)tileMatrix.TileWidth;
            var array = TiffToFloatArray(elevation.Bytes, meshDimension);
            int vertexIndex = 0;
            for (int row = 0; row < meshDimension; ++row)
                for (int column = 0; column < meshDimension; ++column, ++vertexIndex)
                    data.vertices[vertexIndex].y = array[vertexIndex];
        }



        private float[] TiffToFloatArray(byte[] data, int dimension)
        {
            var tiff = Tiff.ClientOpen("TiffToFloatArray", "r", new MemoryStream(data), new TiffStream());

            FieldValue[] value = tiff.GetField(TiffTag.IMAGEWIDTH);
            int width = value[0].ToInt();
            value = tiff.GetField(TiffTag.IMAGELENGTH);
            int height = value[0].ToInt();
            FieldValue[] bitDepth = tiff.GetField(TiffTag.BITSPERSAMPLE);
            FieldValue[] dataTypeTag = tiff.GetField(TiffTag.SAMPLEFORMAT);
            int bpp = bitDepth[0].ToInt();
            int dataType = dataTypeTag[0].ToInt();
            int stride = tiff.ScanlineSize();
            byte[] buffer = new byte[stride];

            var result = new float[dimension * dimension];

            for (int row = 0; row < dimension; ++row)
            {
                if (!tiff.ReadScanline(buffer, row))
                    break;

                // Case of float
                if (bpp == 32 && dataType == 3)
                    for (int col = 0; col < dimension; ++col)
                        result[(row * dimension) + col] = BitConverter.ToSingle(buffer, col * 4);

                // case of Int32
                else if (bpp == 32 && dataType == 2)
                    for (int col = 0; col < dimension; ++col)
                        result[(row * dimension) + col] = BitConverter.ToInt32(buffer, col * 4);

                // Case of Int16
                else if (bpp == 16 && dataType == 2)
                    for (int col = 0; col < dimension; ++col)
                        result[(row * dimension) + col] = BitConverter.ToInt16(buffer, col * 2);

                // Case of Int8
                else if (bpp == 8 && dataType == 2)
                    for (int col = 0; col < dimension; ++col)
                        result[(row * dimension) + col] = buffer[col];

                // Case of Unknown Datatype
                else
                {
                    Console.WriteLine(
                        ": Unknown Tiff file format " +
                        "(bits per pixel:" + bpp.ToString() +
                        ",  dataType code: " + dataType.ToString() +
                        "). Expected bpp values: 8, 16, or 32. Expected dataType values: 1 (two's complement signed int), or 3 (IEEE float)."
                        );
                }
            }

            return result;
       
        }
    }



}
