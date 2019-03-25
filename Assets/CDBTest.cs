using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTile
{
    public int LOD = 0;
    public float X = 0.0f;
    public float Y = 0.0f;

    public GameObject gameObject;
    public Mesh mesh;

    public Vector3[] vertices;
    public Vector3[] normals;
    public int[] triangles;
}


//[ExecuteInEditMode]
public class CDBTest : MonoBehaviour
{
    List<TestTile> tiles = new List<TestTile>();
    System.Random random = new System.Random();

    int Dimension(int lod) => 8;
    int MeshDimension(int lod) => Dimension(lod) + 1;
    float Size(int lod) => Mathf.Pow(2, 10 - lod);
    float Spacing(int lod) => Size(lod) / Dimension(lod);
    string Name(TestTile tile) => string.Format("TILE LOD:{0} X:{1:0.00} Y:{2:0.00} D:{3} SZ:{4:0.00}", tile.LOD, tile.X, tile.Y, Dimension(tile.LOD), Size(tile.LOD));

    float South(TestTile tile) => tile.Y;
    float West(TestTile tile) => tile.X;
    float North(TestTile tile) => tile.Y + Size(tile.LOD);
    float East(TestTile tile) => tile.X + Size(tile.LOD);



    ////////////////////////////////////////////////////////////
    void Start()
    {
        //BuildTieredSouth(100.0f, 100.0f, 1, 2);
        BuildQuads();
        //BuildTieredCollection();
        //BuildSpiral(100.0f, 100.0f);
        //BuildStairs(100.0f, 100.0f);
        UpdateMesh();
    }


    ////////////////////////////////////////////////////////////


    void BuildQuads()
    {
        float x = 0.0f;
        float y = 0.0f;

        int xFactor = 4;

        BuildQuadFromSW(1, x, y);
        BuildQuadFromSW(1, x + (Size(1) * 2), y);
        BuildQuadFromSW(1, x, y + (Size(1) * 2));
        BuildQuadFromSW(1, x + (Size(1) * 2), y + (Size(1) * 2));

        x = Size(1) * xFactor;

        BuildQuadFromSE(1, x, y);
        BuildQuadFromSE(1, x + (Size(1) * 2), y);
        BuildQuadFromSE(1, x, y + (Size(1) * 2));
        BuildQuadFromSE(1, x + (Size(1) * 2), y + (Size(1) * 2));

        y = Size(1) * xFactor;
        x = 0.0f;

        BuildQuadFromNW(1, x, y);
        BuildQuadFromNW(1, x + (Size(1) * 2), y);
        BuildQuadFromNW(1, x, y + (Size(1) * 2));
        BuildQuadFromNW(1, x + (Size(1) * 2), y + (Size(1) * 2));

        x = Size(1) * xFactor;

        BuildQuadFromNE(1, x, y);
        BuildQuadFromNE(1, x + (Size(1) * 2), y);
        BuildQuadFromNE(1, x, y + (Size(1) * 2));
        BuildQuadFromNE(1, x + (Size(1) * 2), y + (Size(1) * 2));
    }


    void BuildSpiral(float x, float y)
    {
        AddTile(1, x - Size(1), y);
        AddTile(1, x - Size(1), y - Size(1));
        AddTile(1, x, y - Size(1));

        AddTile(2, x + Size(2), y);
        AddTile(2, x + Size(2), y + Size(2));
        AddTile(2, x, y + Size(2));

        AddTile(3, x + Size(3), y + Size(3));
        AddTile(3, x, y + Size(3));
        AddTile(3, x, y);
    }

    void BuildStairs(float x, float y)
    {
        AddTile(1, x, y);
        y += Size(1);
        AddTile(2, x, y);
        x += Size(2);
        AddTile(3, x, y);
        x += Size(3);
        AddTile(4, x, y);
        x += Size(4);
        AddTile(5, x, y);
        x += Size(5);
        AddTile(6, x, y);
    }

    void BuildTieredCollection()
    {
        float step = Size(1) * 3;
        float y = 10.0f;
        for (int i = 2; i <= 6; ++i)
        {
            y += step;
            float x = 10.0f;
            BuildTieredNorth(x, y, 1, i);
            x += step;
            BuildTieredSouth(x, y, 1, i);
            x += step;
            BuildTieredEast(x, y, 1, i);
            x += step;
            BuildTieredWest(x, y, 1, i);
        }
    }

    void BuildTieredNorth(float x, float y, int lod1, int lod2)
    {
        AddTile(lod1, x, y);
        int lod2count = (int)Mathf.Pow(2, lod2 - lod1);
        for (int i = 0; i < lod2count; ++i)
            AddTile(lod2, x + (i * Size(lod2)), y + Size(lod1));
    }

    void BuildTieredSouth(float x, float y, int lod1, int lod2)
    {
        AddTile(lod1, x, y);
        int lod2count = (int)Mathf.Pow(2, lod2 - lod1);
        for (int i = 0; i < lod2count; ++i)
            AddTile(lod2, x + (i * Size(lod2)), y - Size(lod2));
    }

    void BuildTieredEast(float x, float y, int lod1, int lod2)
    {
        AddTile(lod1, x, y);
        int lod2count = (int)Mathf.Pow(2, lod2 - lod1);
        for (int i = 0; i < lod2count; ++i)
            AddTile(lod2, x + Size(lod1), y + (i * Size(lod2)));
    }
    void BuildTieredWest(float x, float y, int lod1, int lod2)
    {
        AddTile(lod1, x, y);
        int lod2count = (int)Mathf.Pow(2, lod2 - lod1);
        for (int i = 0; i < lod2count; ++i)
            AddTile(lod2, x - Size(lod2), y + (i * Size(lod2)));
    }


    ////////////////////////////////////////////////////////////

    TestTile AddTile(int lod, float x, float y)
    {
        var tile = new TestTile()
        {
            LOD = lod,
            X = x,
            Y = y,
            vertices = new Vector3[MeshDimension(lod) * MeshDimension(lod)],
            triangles = new int[Dimension(lod) * Dimension(lod) * 6],
        };

        tile.gameObject = new GameObject();
        tile.gameObject.transform.SetParent(gameObject.transform);
        tile.gameObject.transform.Translate(20.0f, 0.0f, 120.0f);
        tile.gameObject.name = Name(tile);
        tile.gameObject.AddComponent<MeshRenderer>();
        tile.mesh = tile.gameObject.AddComponent<MeshFilter>().mesh;
        tile.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int dim = Dimension(tile.LOD);
        int meshDim = MeshDimension(tile.LOD);
        float spacing = Spacing(lod);
        for (int row = 0; row < meshDim; ++row)
        {
            for (int col = 0; col < meshDim; ++col)
            {
                float elev = lod * 4.0f;
                //elev += random.Next(2);
                tile.vertices[(row * meshDim) + col] = new Vector3(tile.X + (col * spacing), elev, tile.Y + (row * spacing));
            }
        }
        int triangleIndex = 0;
        for (int row = 0; row < dim; ++row)
        {
            for (int col = 0; col < dim; ++col, triangleIndex += 6)
            {
                int vertexIndex = (row * meshDim) + col;
                int upperLeftIndex = vertexIndex;
                int upperRightIndex = upperLeftIndex + 1;
                int lowerLeftIndex = upperLeftIndex + meshDim;
                int lowerRightIndex = lowerLeftIndex + 1;
                tile.triangles[triangleIndex + 0] = lowerLeftIndex;
                tile.triangles[triangleIndex + 1] = upperRightIndex;
                tile.triangles[triangleIndex + 2] = upperLeftIndex;
                tile.triangles[triangleIndex + 3] = upperRightIndex;
                tile.triangles[triangleIndex + 4] = lowerLeftIndex;
                tile.triangles[triangleIndex + 5] = lowerRightIndex;
            }
        }
        tile.mesh.vertices = tile.vertices;
        tile.mesh.triangles = tile.triangles;
        tiles.Add(tile);
        return tile;
    }


    void BuildQuadFromSW(int lod, float x, float y)
    {
        AddTile(lod, x, y);
        AddTile(lod, x + Size(lod), y);
        AddTile(lod, x, y + Size(lod));
        if (lod >= 4)
        {
            AddTile(lod, x + Size(lod), y + Size(lod));
            return;
        }
        BuildQuadFromSW(lod + 1, x + Size(lod), y + Size(lod));
    }

    void BuildQuadFromSE(int lod, float x, float y)
    {
        AddTile(lod, x, y);
        AddTile(lod, x + Size(lod), y);
        AddTile(lod, x + Size(lod), y + Size(lod));
        if (lod >= 4)
        {
            AddTile(lod, x, y + Size(lod));
            return;
        }
        BuildQuadFromSE(lod + 1, x, y + Size(lod));
    }

    void BuildQuadFromNW(int lod, float x, float y)
    {
        AddTile(lod, x, y);
        AddTile(lod, x, y + Size(lod));
        AddTile(lod, x + Size(lod), y + Size(lod));
        if (lod >= 4)
        {
            AddTile(lod, x + Size(lod), y);
            return;
        }
        BuildQuadFromNW(lod + 1, x + Size(lod), y);
    }

    void BuildQuadFromNE(int lod, float x, float y)
    {
        AddTile(lod, x, y + Size(lod));
        AddTile(lod, x + Size(lod), y);
        AddTile(lod, x + Size(lod), y + Size(lod));
        if (lod >= 4)
        {
            AddTile(lod, x, y);
            return;
        }
        BuildQuadFromNE(lod + 1, x, y);
    }

    ////////////////////////////////////////////////////////////


    void UpdateMesh()
    {
        //tiles.ForEach(t => t.mesh.RecalculateNormals());
        //tiles.ForEach(t => t.normals = t.mesh.normals);

        tiles.Sort(new LODComparer());
        var reverseTiles = new List<TestTile>(tiles);
        reverseTiles.Reverse();
        tiles.ForEach(tile => reverseTiles.ForEach(other => UpdateTileEdges(tile, other)));
        tiles.ForEach(t => t.mesh.vertices = t.vertices);
        //tiles.ForEach(t => t.mesh.normals = t.normals);
    }

    public class LODComparer : IComparer<TestTile>
    {
        public int Compare(TestTile a, TestTile b)
        {
            if (a.LOD < b.LOD)
                return -1;
            if (a.LOD > b.LOD)
                return 1;
            return 0;
        }
    }


    void UpdateTileEdges(TestTile tile, TestTile other)
    {
        if (tile.LOD < other.LOD)
            return;
        if (IsSouth(tile, other))
            UpdateTileEdges_South(tile, other);
        if (IsWest(tile, other))
            UpdateTileEdges_West(tile, other);
        if (IsNorth(tile, other))
            UpdateTileEdges_North(tile, other);
        if (IsEast(tile, other))
            UpdateTileEdges_East(tile, other);


        if (tile.LOD <= other.LOD)
            return;
        int step = VertexStep(tile, other);
        InterpolateSouthEdge(tile, step);
        InterpolateWestEdge(tile, step);
        InterpolateNorthEdge(tile, step);
        InterpolateEastEdge(tile, step);
    }

    int VertexStep(TestTile tile, TestTile other) => (int)Math.Pow(2, tile.LOD - other.LOD);

    void blah(TestTile tile, TestTile other)
    {
        int otherIndexSouth = (int)Math.Round((South(tile) - South(other)) / Spacing(other.LOD));
        int otherIndexNorth = (int)Math.Round((North(tile) - South(other)) / Spacing(other.LOD));
        int otherIndexWest = (int)Math.Round((West(tile) - West(other)) / Spacing(other.LOD));
        int otherIndexEast = (int)Math.Round((East(tile) - West(other)) / Spacing(other.LOD));
        int otherVertexOffset = (MeshDimension(other.LOD) * otherIndexSouth) + otherIndexWest;
        int otherVertexOffsetNorthWest = (MeshDimension(other.LOD) * otherIndexNorth) + otherIndexWest;
        int otherVertexOffsetSouthEast = (MeshDimension(other.LOD) * otherIndexSouth) + otherIndexEast;
        int tileVertexOffset = 0;
        int tileVertexOffsetNorthWest = MeshDimension(tile.LOD) * (MeshDimension(tile.LOD) - 1);
        int tileVertexOffsetSouthEast = MeshDimension(tile.LOD) - 1;

        int tileToOtherRatio = (int)Math.Pow(2, tile.LOD - other.LOD);
        int otherCount = Dimension(tile.LOD) / tileToOtherRatio;
        for (int i = 0; i <= otherCount; ++i)
        {
            tile.vertices[tileVertexOffset + (i * tileToOtherRatio)].y = other.vertices[otherVertexOffset + i].y; // south
            tile.vertices[tileVertexOffset + (MeshDimension(tile.LOD) * i * tileToOtherRatio)].y = other.vertices[otherVertexOffset + (MeshDimension(other.LOD) * i)].y;    // west
            tile.vertices[tileVertexOffsetNorthWest + (i * tileToOtherRatio)].y = other.vertices[otherVertexOffsetNorthWest + i].y;    // north
            tile.vertices[tileVertexOffsetSouthEast + (MeshDimension(tile.LOD) * i * tileToOtherRatio)].y = other.vertices[otherVertexOffsetSouthEast + (MeshDimension(other.LOD) * i)].y;   // east
        }

        if (tile.LOD <= other.LOD)
            return;
        InterpolateSouthEdge(tile, tileToOtherRatio);
        InterpolateWestEdge(tile, tileToOtherRatio);
        InterpolateNorthEdge(tile, tileToOtherRatio);
        InterpolateEastEdge(tile, tileToOtherRatio);
    }



    void UpdateTileEdges_South(TestTile tile, TestTile other)
    {
        float offsetDistanceX = West(tile) - West(other);
        float offsetDistanceY = South(tile) - South(other);
        int offsetIndexX = Mathf.RoundToInt(offsetDistanceX / Spacing(other.LOD));
        int offsetIndexY = Mathf.RoundToInt(offsetDistanceY / Spacing(other.LOD));
        int offsetIndex = (offsetIndexY * MeshDimension(other.LOD)) + offsetIndexX;

        int tileVertexOffset = 0;
        int tileVertexStep = 1;
        int otherVertexOffset = offsetIndex;
        int otherVertexStep = 1;

        int tileStep = (int)Mathf.Pow(2, tile.LOD - other.LOD);

        int otherCount = Dimension(tile.LOD) / tileStep;
        for (int i = 0; i <= otherCount; ++i)
        {
            int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
            int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
            tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
        }
    }

    void UpdateTileEdges_West(TestTile tile, TestTile other)
    {
        float offsetDistanceX = West(tile) - West(other);
        float offsetDistanceY = South(tile) - South(other);
        int offsetIndexX = Mathf.RoundToInt(offsetDistanceX / Spacing(other.LOD));
        int offsetIndexY = Mathf.RoundToInt(offsetDistanceY / Spacing(other.LOD));
        int offsetIndex = (offsetIndexY * MeshDimension(other.LOD)) + offsetIndexX;

        int tileVertexOffset = 0;
        int tileVertexStep = MeshDimension(tile.LOD);
        int otherVertexOffset = offsetIndex;
        int otherVertexStep = MeshDimension(other.LOD);

        int tileStep = (int)Mathf.Pow(2, tile.LOD - other.LOD);

        int otherCount = Dimension(tile.LOD) / tileStep;
        for (int i = 0; i <= otherCount; ++i)
        {
            int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
            int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
            tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
        }
    }

    void UpdateTileEdges_North(TestTile tile, TestTile other)
    {
        float offsetDistanceX = West(tile) - West(other);
        float offsetDistanceY = North(tile) - South(other);
        int offsetIndexX = Mathf.RoundToInt(offsetDistanceX / Spacing(other.LOD));
        int offsetIndexY = Mathf.RoundToInt(offsetDistanceY / Spacing(other.LOD));
        int offsetIndex = (offsetIndexY * MeshDimension(other.LOD)) + offsetIndexX;

        int tileVertexOffset = MeshDimension(tile.LOD) * (MeshDimension(tile.LOD) - 1);
        int tileVertexStep = 1;
        int otherVertexOffset = offsetIndex;
        int otherVertexStep = 1;
        int tileStep = (int)Mathf.Pow(2, tile.LOD - other.LOD);

        int otherCount = Dimension(tile.LOD) / tileStep;
        for (int i = 0; i <= otherCount; ++i)
        {
            int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
            int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
            tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
        }
    }

    void UpdateTileEdges_East(TestTile tile, TestTile other)
    {
        float offsetDistanceX = East(tile) - West(other);
        float offsetDistanceY = South(tile) - South(other);
        int offsetIndexX = Mathf.RoundToInt(offsetDistanceX / Spacing(other.LOD));
        int offsetIndexY = Mathf.RoundToInt(offsetDistanceY / Spacing(other.LOD));
        int offsetIndex = (offsetIndexY * MeshDimension(other.LOD)) + offsetIndexX;

        int tileVertexOffset = MeshDimension(tile.LOD) - 1;
        int tileVertexStep = MeshDimension(tile.LOD);
        int otherVertexOffset = offsetIndex;
        int otherVertexStep = MeshDimension(other.LOD);
        int tileStep = (int)Mathf.Pow(2, tile.LOD - other.LOD);

        int otherCount = Dimension(tile.LOD) / tileStep;
        for (int i = 0; i <= otherCount; ++i)
        {
            int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
            int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
            tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
        }
    }

    void InterpolateSouthEdge(TestTile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, 0, 1);
    void InterpolateNorthEdge(TestTile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, MeshDimension(tile.LOD) * (MeshDimension(tile.LOD) - 1), 1);
    void InterpolateWestEdge(TestTile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, 0, MeshDimension(tile.LOD));
    void InterpolateEastEdge(TestTile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, MeshDimension(tile.LOD) - 1, MeshDimension(tile.LOD));

    void InterpolateEdge(TestTile tile, int segmentWidth, int vertexArrayOffset, int vertexArrayStep)
    {
        for (int i = 0; i + segmentWidth < MeshDimension(tile.LOD); i += segmentWidth)
        {
            int startIndex = vertexArrayOffset + (i * vertexArrayStep);
            int endIndex = startIndex + (segmentWidth * vertexArrayStep);
            float a = tile.vertices[startIndex].y;
            float b = tile.vertices[endIndex].y;
            float slope = (b - a) / segmentWidth;
            for (int j = 1; j < segmentWidth; ++j)
                tile.vertices[startIndex + (j * vertexArrayStep)].y = a + (slope * j);
        }
    }


    ////////////////////////////////////////////////////////////


    bool IsSouth(TestTile tile, TestTile other)
    {
        if (South(tile) < South(other))
            return false;
        if (South(tile) > North(other))
            return false;
        if (East(tile) <= West(other))
            return false;
        if (West(tile) >= East(other))
            return false;
        if ((tile.LOD == other.LOD) && (South(tile) == South(other)))
            return false;
        return true;
    }
    bool IsWest(TestTile tile, TestTile other)
    {
        if (West(tile) < West(other))
            return false;
        if (West(tile) > East(other))
            return false;
        if (North(tile) <= South(other))
            return false;
        if (South(tile) >= North(other))
            return false;
        if ((tile.LOD == other.LOD) && (West(tile) == West(other)))
            return false;
        return true;
    }
    bool IsNorth(TestTile tile, TestTile other)
    {
        if (North(tile) > North(other))
            return false;
        if (North(tile) < South(other))
            return false;
        if (East(tile) <= West(other))
            return false;
        if (West(tile) >= East(other))
            return false;
        if ((tile.LOD == other.LOD) && (North(tile) == North(other)))
            return false;
        return true;
    }
    bool IsEast(TestTile tile, TestTile other)
    {
        if (East(tile) > East(other))
            return false;
        if (East(tile) < West(other))
            return false;
        if (North(tile) <= South(other))
            return false;
        if (South(tile) >= North(other))
            return false;
        if ((tile.LOD == other.LOD) && (East(tile) == East(other)))
            return false;
        return true;
    }



}
