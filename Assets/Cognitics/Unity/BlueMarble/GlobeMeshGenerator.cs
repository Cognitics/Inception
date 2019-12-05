
using UnityEngine;

namespace Cognitics.Unity.BlueMarble
{
    public class GlobeMeshGenerator : MonoBehaviour
    {
        CoordinateSystems.WGS84Transform WGS84Transform = new CoordinateSystems.WGS84Transform();
        
        void Start()
        {
            gameObject.AddComponent<MeshFilter>().mesh = GenerateMesh();
        }

        public Mesh GenerateMesh(float scale = 1e-5f)
        {
            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = GenerateVertices(scale);
            mesh.uv = GenerateUVs();
            mesh.triangles = GenerateTriangles();
            return mesh;
        }

        //int VertexCount = 1 + ((89 + 1 + 89) * 361) + 1;
        int VertexCount = 179 * 361;

        Vector3[] GenerateVertices(float scale)
        {
            var vertices = new Vector3[VertexCount];
            //vertices[0] = Vector3ForGeodetic(-90.0, 0.0, scale);
            int vertexIndex = 0;
            for (int ilat = -89; ilat <= 89; ++ilat)
                for (int ilon = -180; ilon <= 180; ++ilon, ++vertexIndex)
                    vertices[vertexIndex] = Vector3ForGeodetic(ilat, ilon, scale);
            //vertices[vertexIndex] = Vector3ForGeodetic(90.0, 0.0, scale);
            return vertices;
        }

        Vector3 Vector3ForGeodetic(double latitude, double longitude, float scale)
        {
            WGS84Transform.GeodeticToECEF(latitude, longitude, 0.0, out double x, out double y, out double z);
            x *= scale;
            y *= scale;
            z *= scale;
            return new Vector3((float)x, (float)z, (float)y);
        }

        Vector2[] GenerateUVs()
        {
            var uvs = new Vector2[VertexCount];
            //uvs[0] = Vector2.zero;
            int vertexIndex = 0;
            for (int ilat = -89; ilat <= 89; ++ilat)
                for (int ilon = -180; ilon <= 180; ++ilon, ++vertexIndex)
                    uvs[vertexIndex].Set((ilon + 180) / 360.0f, (ilat + 90) / 180.0f);
            //uvs[vertexIndex] = Vector2.one;
            return uvs;
        }

        int[] GenerateTriangles()
        {
            var triangles = new int[178 * 360 * 6];
            int triangleIndex = 0;
            /*
            for (int ilon = 0; ilon < 360; ++ilon, triangleIndex += 3)
            {
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = ilon + 1;
                triangles[triangleIndex + 2] = ilon + 2;
            }
            */
            for (int ilat = -89; ilat < 89; ++ilat)
            {
                for (int ilon = -180; ilon < 180; ++ilon, triangleIndex += 6)
                {
                    int vertexIndex = ((ilat + 89) * 361) + (ilon + 180);

                    int lowerLeftIndex = vertexIndex;
                    int lowerRightIndex = lowerLeftIndex + 1;
                    int upperLeftIndex = lowerLeftIndex + 361;
                    int upperRightIndex = lowerRightIndex + 361;

                    triangles[triangleIndex + 0] = lowerLeftIndex;
                    triangles[triangleIndex + 1] = upperLeftIndex;
                    triangles[triangleIndex + 2] = upperRightIndex;

                    triangles[triangleIndex + 3] = lowerLeftIndex;
                    triangles[triangleIndex + 4] = upperRightIndex;
                    triangles[triangleIndex + 5] = lowerRightIndex;
                }
            }
            /*
            for (int ilon = 0; ilon < 360; ++ilon, triangleIndex += 3)
            {
                triangles[triangleIndex + 0] = (VertexCount - 1 - 361) + ilon + 1;
                triangles[triangleIndex + 1] = (VertexCount - 1 - 361) + ilon + 0;
                triangles[triangleIndex + 2] = VertexCount - 1;
            }
            */
            return triangles;
        }



    }
}
