using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Cognitics.CoordinateSystems;

namespace Cognitics.UnityCDB
{
    public class VolumetricFeature : MonoBehaviour, IPointerClickHandler
    {
        public string textAttributes = "";
        public AirspaceData airspaceData = null;
        public Text textInfo = null;
        [HideInInspector] public int LOD = 0;

        [HideInInspector] public Database Database = null;
        [HideInInspector] public GameObject UserObject = null;
        [HideInInspector] public NetTopologySuite.Features.Feature Feature = null;

        private Dictionary<string, object> Attributes = new Dictionary<string, object>();
        private Vector3[] vertices = null;
        private int[] triangles = null;
        private int[] sideTriangles = null;
        private Vector3[] normals = null;

        // TODO: move into a central location
        public const float metersPerFoot = 0.3048f;
        public const float feetPerMeter = 1f / metersPerFoot;
        public const float flightLevelPerFoot = 0.01f;
        public const float feetPerFlightLevel = 1f / flightLevelPerFoot;
        private const int maxFlightLevel = 600;
        private const int contiguousUSA_Id = 4956;

        #region MonoBehaviour

        protected void Start()
        {
            if (Feature.Geometry == null)
            {
                Debug.LogErrorFormat("feature {0} has no geometry!!!", name);
                gameObject.SetActive(false);
                return;
            }
            if (!Feature.Geometry.IsSimple)
                Debug.LogWarningFormat("feature {0} geometry is not simple!!!", name);

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var attributeNames = new List<string>(Feature.Attributes.GetNames());
            var attributeValues = new List<object>(Feature.Attributes.GetValues());
            for (int i = 0; i < attributeNames.Count; i++)
                Attributes[attributeNames[i]] = attributeValues[i];

            // Copy the attributes into a field viewable from the inspector (for easy copy/paste debugging)
            string str = "";
            for (int i = 0; i < attributeNames.Count; i++)
                str += string.Format("{0}: {1}\n", attributeNames[i], attributeValues[i].ToString());
            //var multiPolygon = Feature.Geometry as NetTopologySuite.Geometries.MultiPolygon;
            //if (multiPolygon != null)
            //    str += "MP\n";
            textAttributes = str;

            name = GetName();

            int id = -1;
            GetId(out id);
            if (id == contiguousUSA_Id) // skip contiguous US airspace that seems to be in every airspace shapefile
            {
                gameObject.SetActive(false);
                return;
            }

            Rebuild();

            if (textInfo != null)
            {
                textInfo.text = name;
                textInfo.color = new Color(0.5f, 0.5f, 0.5f, 1f);

                if (vertices != null && vertices.Length > 0)
                {
                    var geographicCoordinates = new GeographicCoordinates(Feature.Geometry.Centroid.Y, Feature.Geometry.Centroid.X);
                    var cartesianCoordinates = geographicCoordinates.TransformedWith(Database.Projection);
                    Vector3 vec = new Vector3((float)cartesianCoordinates.X, vertices[0].y, (float)cartesianCoordinates.Y);
                    textInfo.transform.position = vec;
                    textInfo.transform.localScale = Vector3.one;
                }
            }

        }

        protected void Update()
        {
            if (textInfo != null && UserObject != null)
                textInfo.transform.rotation = UserObject.transform.rotation;
        }

        #endregion

        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData ped)
        {
            Debug.Log("feature pointer click: " + name);
        }

        #endregion

        #region Attributes

        private bool GetId(out int ival)
        {
            ival = -1;
            object val = null;
            if (Attributes.TryGetValue("OBJECTID", out val) && int.TryParse(val.ToString(), out ival))
                return true;

            return false;
        }

        private string GetName()
        {
            object val = null;
            if (Attributes.TryGetValue("NAME", out val) && val is string)
                return val as string;
            else
                return "[UNKNOWN]";
        }

        private bool GetElevation(bool upper, out float fval)
        {
            fval = 0f;
            object val = null;
            if (Attributes.TryGetValue(upper ? "UPPER_VAL" : "LOWER_VAL", out val))
            {
                if (val is string)
                    float.TryParse(val as string, out fval);
                else if (val is int)
                    fval = (float)val;
                else if (val is double)
                    fval = Convert.ToSingle(val);
                else
                    fval = (float)val;

                if (fval == -9998f)
                {
                    // Special case - assume max flight level for now I guess
                    fval = maxFlightLevel * feetPerFlightLevel * metersPerFoot;
                }
                else
                {
                    object uom = null;
                    if (Attributes.TryGetValue(upper ? "UPPER_UOM" : "LOWER_UOM", out uom) && uom is string)
                    {
                        if ((uom as string) == "FT")
                            fval *= metersPerFoot;
                        else if ((uom as string) == "FL")
                            fval *= metersPerFoot * feetPerFlightLevel;
                    }
                }

                return true;
            }

            return false;
        }

        private bool GetHeight(out float fval)
        {
            fval = 0f;
            object val = null;
            if (Attributes.TryGetValue("height_abo", out val))
            {
                if (val is string)
                    float.TryParse(val as string, out fval);
                else if (val is int)
                    fval = (float)val;
                else if (val is double)
                    fval = Convert.ToSingle(val);
                else
                    fval = (float)val;

                return true;
            }

            return false;
        }

        private string GetLocalType()
        {
            object val = null;
            if (Attributes.TryGetValue("LOCAL_TYPE", out val) && val is string)
                return val as string;
            else
                return "[UNKNOWN]";
        }

        private Color GetColor(string _class)
        {
            if (airspaceData != null)
            {
                if (_class == "CLASS_B")
                    return airspaceData.ClassColors[0];
                else if (_class == "CLASS_C")
                    return airspaceData.ClassColors[1];
                else if (_class == "CLASS_D")
                    return airspaceData.ClassColors[2];
                else if (_class == "CLASS_E2")
                    return airspaceData.ClassColors[3];
                else if (_class == "CLASS_E3")
                    return airspaceData.ClassColors[4];
                else if (_class == "CLASS_E4")
                    return airspaceData.ClassColors[5];
                else if (_class == "CLASS_E5")
                    return airspaceData.ClassColors[6];
                else if (_class == "CLASS_E6")
                    return airspaceData.ClassColors[7];
                else if (_class == "CONT_AREA_EXT")
                    return airspaceData.ClassColors[8];
                else if (_class == "MODE C")
                    return airspaceData.ClassColors[9];
                else
                    return airspaceData.ClassColors[10];
            }

            return Color.black;
        }

        #endregion

        #region Geometry

        public bool GenerateFeature(GeoAPI.Geometries.IGeometry geometry, float top, float bottom, GameObject node)
        {
            var mesh = node.GetOrAddComponent<MeshFilter>().mesh;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.Clear(true);

            if (!ProcessGeometry(geometry, top, bottom, node))
            {
                node.SetActive(false);
                return false;
            }

            var meshRenderer = node.GetOrAddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            var material = meshRenderer.material;
            if (material != null)
            {
                meshRenderer.material = material;
                meshRenderer.material.color = GetColor(GetLocalType());
            }

            Destroy(node.GetComponent<MeshCollider>());
            node.AddComponent<MeshCollider>();

            return true;
        }

        private bool ProcessGeometry(GeoAPI.Geometries.IGeometry geometry, float top, float bottom, GameObject node)
        {
            int index = 0;
            vertices = new Vector3[geometry.Coordinates.Length];
            if (vertices.Length < 3)
                return false;

            Vector3 referenceVec = Vector3.zero;
            foreach (var coordinate in geometry.Coordinates)
            {
                var geographicCoordinates = new GeographicCoordinates(coordinate.Y, coordinate.X);
                var cartesianCoordinates = geographicCoordinates.TransformedWith(Database.Projection);
                Vector3 vec = new Vector3((float)cartesianCoordinates.X, top, (float)cartesianCoordinates.Y);

                if (index == 0)
                {
                    referenceVec = vec;
                }
                else
                {
                    float distSq = Vector3.SqrMagnitude(vec - referenceVec);
                    if (distSq > 250f * LOD * LOD)
                        referenceVec = vec;
                    else
                        continue;
                }

                vertices[index] = vec;

                index++;
            }

            Array.Resize(ref vertices, index);

            // We have a poly which needs triangulation
            var triangulatorInput = new List<Vector2>();
            var scratch = new List<Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (i != vertices.Length - 1) // skip the last vertex so that triangulator plays nicely
                    triangulatorInput.Add(new Vector2(vertices[i].x, vertices[i].z));
            }
            for (int i = 0; i < vertices.Length - 1; i++)
                scratch.Add(new Vector3(vertices[i].x, top, vertices[i].z));
            for (int i = 0; i < vertices.Length - 1; i++)
                scratch.Add(new Vector3(vertices[i].x, bottom, vertices[i].z));

            // Store off the side vertices
            sideTriangles = new int[3 * scratch.Count];
            int j = 0;
            for (int i = 0; i < sideTriangles.Length; i += 6, j++)
            {
                sideTriangles[i + 0] = j;
                sideTriangles[i + 1] = j + scratch.Count / 2;
                if (j + 1 == scratch.Count / 2)
                    j = -1;
                sideTriangles[i + 2] = j + 1;

                sideTriangles[i + 3] = sideTriangles[i + 2];
                sideTriangles[i + 4] = sideTriangles[i + 1];
                sideTriangles[i + 5] = j + scratch.Count / 2 + 1;
            }

            var triangulator = new Triangulator();
            var result = new List<Vector2>();
            triangulator.Process(triangulatorInput, ref result);
            if (result.Count == 0 || result.Count % 3 != 0)
            {
                Debug.LogError(string.Format("{0}: triangulation failed!", name));
                return false;
            }

            // Add vertices for top and bottom caps
            vertices = new Vector3[2 * result.Count];

            int v = 0;
            for (int i = 0; i < result.Count; i++, v++)
                vertices[v].Set(result[i].x, top, result[i].y);
            for (int i = 0; i < result.Count; i++, v++)
                vertices[v].Set(result[i].x, bottom, result[i].y);

            // Correct the side triangles list to match the new array
            for (int i = 0; i < sideTriangles.Length; i++)
            {
                int sideTriIndex = sideTriangles[i];
                if (sideTriIndex > scratch.Count)
                    sideTriIndex = sideTriIndex - scratch.Count;
                Vector3 sideVec = scratch[sideTriIndex];
                for (int verticesIndex = 0; verticesIndex < vertices.Length; verticesIndex++)
                {
                    Vector3 verticesVec = vertices[verticesIndex];
                    if (Mathf.Approximately(sideVec.x, verticesVec.x) && Mathf.Approximately(sideVec.y, verticesVec.y) && Mathf.Approximately(sideVec.z, verticesVec.z))
                    {
                        sideTriangles[i] = verticesIndex;
                        break;
                    }
                }
            }

            var mesh = node.GetComponent<MeshFilter>().mesh;
            mesh.vertices = vertices;

            int topCapTriangleCount = mesh.vertices.Length / 2 / 3;
            int bottomCapTriangleCount = topCapTriangleCount;
            int sideTriangleCount = sideTriangles.Length / 3;
            int totalTriangleCount = topCapTriangleCount + bottomCapTriangleCount + sideTriangleCount;
            triangles = new int[3 * totalTriangleCount];

            // Top cap
            int start = 0;
            int end = 3 * topCapTriangleCount;
            for (int i = start; i < end; i++)
                triangles[i] = end - 1 - i; // start at the back, for winding purposes

            // Bottom cap
            start = end;
            end = start + 3 * bottomCapTriangleCount;
            for (int i = start; i < end; i++)
                triangles[i] = i;

            // Sides
            start = end;
            end = start + 3 * sideTriangleCount;
            j = 0;
            for (int i = start; i < end; i++, j++)
                triangles[i] = sideTriangles[j];

            mesh.triangles = triangles;

            normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; ++i)
                normals[i] = Vector3.up;

            mesh.normals = normals;

            return true;
        }

        #endregion

        #region Misc

        public bool Rebuild()
        {
            float top = 0f;
            float bottom = 0f;
            if (!GetHeight(out top))
            {
                GetElevation(true, out top);
                GetElevation(false, out bottom);
            }

            GeoAPI.Geometries.IGeometry[] geometries = null;
            var multiPolygon = Feature.Geometry as NetTopologySuite.Geometries.MultiPolygon;
            if (multiPolygon != null)
            {
                geometries = multiPolygon.Geometries;
            }
            else
            {
                geometries = new GeoAPI.Geometries.IGeometry[1];
                geometries[0] = Feature.Geometry;
            }

            int index = 0;
            bool success = true;
            foreach (var geometry in geometries)
            {
                GameObject node = null;
                if (geometries.Length > 1)
                {
                    node = new GameObject(string.Format("{0} ({1})", name, index.ToString()));
                    node.transform.SetParent(gameObject.transform, false);
                }
                else
                {
                    node = gameObject;
                }

                if (GenerateFeature(geometry, top, bottom, node))
                {
                    //Debug.LogFormat("Feature {0}/{1} of {2} with {3} vertices at LOD {4} build succeeded.", index+1, geometries.Length, name, vertices.Length, LOD);
                }
                else
                {
                    //Debug.LogErrorFormat("Feature {0}/{1} of {2} failed to build!!! Try changing the LOD.", index+1, geometries.Length, name);
                    success = false;
                }

                index++;
            }

            return success;
        }

        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VolumetricFeature)), CanEditMultipleObjects]
    public class VolumetricFeatureInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var volumetricFeature = (VolumetricFeature)target;

            int LOD = EditorGUILayout.IntField("LOD", volumetricFeature.LOD);
            volumetricFeature.LOD = Mathf.Clamp(LOD, 0, 5);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = EditorApplication.isPlaying;
            {
                if (GUILayout.Button("Rebuild"))
                    volumetricFeature.Rebuild();

                if (GUILayout.Button("Rebuild All Selected"))
                {
                    foreach (var gameObj in Selection.gameObjects)
                    {
                        var feature = gameObj.GetComponent<VolumetricFeature>();
                        feature.Rebuild();
                    }
                }
            }
            GUI.enabled = wasEnabled;
        }
    }
#endif
}
