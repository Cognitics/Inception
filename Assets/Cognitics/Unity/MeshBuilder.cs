using UnityEngine;

namespace Cognitics.Unity
{
    public class MeshBuilder
    {
        public static Mesh Mesh(float spacing, int dimension)
        {
            var vertices = new Vector3[dimension * dimension];
            {
                int vertexIndex = 0;
                for (int row = 0; row < dimension; ++row)
                {
                    for (int column = 0; column < dimension; ++column, ++vertexIndex)
                    {
                        float x = column * spacing;
                        float y = row * spacing;
                        vertices[vertexIndex].Set(x, 0.0f, y);
                    }
                }
            }

            var uv = new Vector2[dimension * dimension];
            {
                int vertexIndex = 0;
                for (int row = 0; row < dimension; ++row)
                    for (int column = 0; column < dimension; ++column, ++vertexIndex)
                        uv[vertexIndex] = new Vector2((float)column / (dimension - 1), (float)row / (dimension - 1));
            }

            var triangles = new int[(dimension - 1) * (dimension - 1) * 6];
            {
                int triangleIndex = 0;
                for (int row = 0; row < dimension - 1; ++row)
                {
                    for (int column = 0; column < dimension - 1; ++column)
                    {
                        int vertexIndex = (row * dimension) + column;
                        int lowerLeftIndex = vertexIndex;
                        int lowerRightIndex = lowerLeftIndex + 1;
                        int upperLeftIndex = lowerLeftIndex + dimension;
                        int upperRightIndex = upperLeftIndex + 1;
                        triangles[triangleIndex + 0] = lowerLeftIndex;
                        triangles[triangleIndex + 1] = upperLeftIndex;
                        triangles[triangleIndex + 2] = upperRightIndex;
                        triangles[triangleIndex + 3] = lowerLeftIndex;
                        triangles[triangleIndex + 4] = upperRightIndex;
                        triangles[triangleIndex + 5] = lowerRightIndex;
                        triangleIndex += 6;
                    }
                }
            }

            var mesh = new Mesh();
            if (vertices.Length > ushort.MaxValue)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            return mesh;
        }

    }

}
