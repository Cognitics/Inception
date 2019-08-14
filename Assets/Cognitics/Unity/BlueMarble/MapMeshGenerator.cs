
using UnityEngine;

namespace Cognitics.Unity.BlueMarble
{
    public class MapMeshGenerator : MonoBehaviour
    {
        void Start()
        {
            gameObject.AddComponent<MeshFilter>().mesh = GenerateMesh();
        }

        public static Mesh GenerateMesh()
        {
            var vertices = new Vector3[4];
            vertices[0].Set(-180.0f, -90.0f, 0.0f);
            vertices[1].Set(180.0f, -90.0f, 0.0f);
            vertices[2].Set(180.0f, 90.0f, 0.0f);
            vertices[3].Set(-180.0f, 90.0f, 0.0f);

            var uvs = new Vector2[4];
            uvs[0].Set(0.0f, 0.0f);
            uvs[1].Set(1.0f, 0.0f);
            uvs[2].Set(1.0f, 1.0f);
            uvs[3].Set(0.0f, 1.0f);

            var triangles = new int[6];
            triangles[0] = 0;
            triangles[1] = 2;
            triangles[2] = 1;
            triangles[3] = 0;
            triangles[4] = 3;
            triangles[5] = 2;

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            return mesh;
        }

    }
}
