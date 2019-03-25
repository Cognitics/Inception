
using System;
using System.Collections.Generic;

namespace Cognitics.UnityCDB
{
    public static class EdgeCrackElimination
    {
        public static void EliminateCracks(List<Tile> tiles)
        {
            tiles.ForEach(tile => tile.Database.SetMeshPerimeter(tile.vertices, tile.MeshDimension, tile.perimeter));
            tiles.Sort(new TileComparer());
            var reverseTiles = new List<Tile>(tiles);
            reverseTiles.Reverse();
            tiles.ForEach(tile => reverseTiles.ForEach(other => UpdateTileEdges(tile, other)));
            tiles.ForEach(tile => { tile.GetComponent<UnityEngine.MeshFilter>().mesh.vertices = tile.vertices; });
        }

        ////////////////////////////////////////////////////////////////////////////////

        static float South(Tile tile) => (float)tile.GeographicBounds.MinimumCoordinates.Latitude;
        static float West(Tile tile) => (float)tile.GeographicBounds.MinimumCoordinates.Longitude;
        static float North(Tile tile) => (float)tile.GeographicBounds.MaximumCoordinates.Latitude;
        static float East(Tile tile) => (float)tile.GeographicBounds.MaximumCoordinates.Longitude;
        static int LOD(Tile tile) => tile.CDBTile.LOD;
        static int Dimension(Tile tile) => tile.RasterDimension;
        static int MeshDimension(Tile tile) => tile.MeshDimension;
        static float Size(Tile tile) => North(tile) - South(tile);
        static float Spacing(Tile tile) => Size(tile) / Dimension(tile);

        ////////////////////////////////////////////////////////////////////////////////


        public class TileComparer : IComparer<Tile>
        {
            public int Compare(Tile a, Tile b)
            {
                if (LOD(a) < LOD(b))
                    return -1;
                if (LOD(a) > LOD(b))
                    return 1;
                if (South(a) < South(b))
                    return -1;
                if (South(a) > South(b))
                    return 1;
                if (West(a) < West(b))
                    return -1;
                if (West(a) > West(b))
                    return 1;
                return 0;
            }
        }



        ////////////////////////////////////////////////////////////////////////////////

        static void UpdateTileEdges(Tile tile, Tile other)
        {
            if (!tile.IsLoaded || !other.IsLoaded)
                return;
            if (LOD(tile) < LOD(other))
                return;
            if (IsNorthEast(tile, other))
                UpdateTileEdges_NorthEast(tile, other);
            if (IsNorth(tile, other))
                UpdateTileEdges_North(tile, other);
            if (IsEast(tile, other))
                UpdateTileEdges_East(tile, other);
            if (IsSouth(tile, other))
                UpdateTileEdges_South(tile, other);
            if (IsWest(tile, other))
                UpdateTileEdges_West(tile, other);
        }

        static void UpdateTileEdges_NorthEast(Tile tile, Tile other)
        {
            int vertexIndex = (MeshDimension(tile) * MeshDimension(tile)) - 1;
            tile.vertices[vertexIndex].y = other.vertices[0].y;
            //tile.normals[vertexIndex] = other.normals[0];
        }

        static void UpdateTileEdges_North(Tile tile, Tile other)
        {
            float offsetDistanceX = West(tile) - West(other);
            float offsetDistanceY = North(tile) - South(other);
            int offsetIndexX = (int)Math.Round(offsetDistanceX / Spacing(other));
            int offsetIndexY = (int)Math.Round(offsetDistanceY / Spacing(other));
            int offsetIndex = (offsetIndexY * MeshDimension(other)) + offsetIndexX;

            int tileVertexOffset = MeshDimension(tile) * (MeshDimension(tile) - 1);
            int tileVertexStep = 1;
            int otherVertexOffset = offsetIndex;
            int otherVertexStep = 1;
            int tileStep = (int)Math.Pow(2, LOD(tile) - LOD(other));

            int otherCount = Dimension(tile) / tileStep;
            for (int i = 0; i <= otherCount; ++i)
            {
                int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
                int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
                tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
                //tile.normals[tileVertexIndex] = other.normals[otherVertexIndex];
            }
            if (LOD(tile) > LOD(other))
                InterpolateNorthEdge(tile, tileStep);
            
        }

        static void UpdateTileEdges_East(Tile tile, Tile other)
        {
            float offsetDistanceX = East(tile) - West(other);
            float offsetDistanceY = South(tile) - South(other);
            int offsetIndexX = (int)Math.Round(offsetDistanceX / Spacing(other));
            int offsetIndexY = (int)Math.Round(offsetDistanceY / Spacing(other));
            int offsetIndex = (offsetIndexY * MeshDimension(other)) + offsetIndexX;

            int tileVertexOffset = MeshDimension(tile) - 1;
            int tileVertexStep = MeshDimension(tile);
            int otherVertexOffset = offsetIndex;
            int otherVertexStep = MeshDimension(other);
            int tileStep = (int)Math.Pow(2, LOD(tile) - LOD(other));

            int otherCount = Dimension(tile) / tileStep;
            for (int i = 0; i <= otherCount; ++i)
            {
                int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
                int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
                tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
                //tile.normals[tileVertexIndex] = other.normals[otherVertexIndex];
            }
            if (LOD(tile) > LOD(other))
                InterpolateEastEdge(tile, tileStep);
        }

        static void UpdateTileEdges_South(Tile tile, Tile other)
        {
            float offsetDistanceX = West(tile) - West(other);
            float offsetDistanceY = South(tile) - South(other);
            int offsetIndexX = (int)Math.Round(offsetDistanceX / Spacing(other));
            int offsetIndexY = (int)Math.Round(offsetDistanceY / Spacing(other));
            int offsetIndex = (offsetIndexY * MeshDimension(other)) + offsetIndexX;

            int tileVertexOffset = 0;
            int tileVertexStep = 1;
            int otherVertexOffset = offsetIndex;
            int otherVertexStep = 1;
            int tileStep = (int)Math.Pow(2, LOD(tile) - LOD(other));

            int otherCount = Dimension(tile) / tileStep;
            for (int i = 0; i <= otherCount; ++i)
            {
                int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
                int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
                tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
                //tile.normals[tileVertexIndex] = other.normals[otherVertexIndex];
            }
            if (LOD(tile) > LOD(other))
                InterpolateSouthEdge(tile, tileStep);
        }

        static void UpdateTileEdges_West(Tile tile, Tile other)
        {
            float offsetDistanceX = West(tile) - West(other);
            float offsetDistanceY = South(tile) - South(other);
            int offsetIndexX = (int)Math.Round(offsetDistanceX / Spacing(other));
            int offsetIndexY = (int)Math.Round(offsetDistanceY / Spacing(other));
            int offsetIndex = (offsetIndexY * MeshDimension(other)) + offsetIndexX;

            int tileVertexOffset = 0;
            int tileVertexStep = MeshDimension(tile);
            int otherVertexOffset = offsetIndex;
            int otherVertexStep = MeshDimension(other);
            int tileStep = (int)Math.Pow(2, LOD(tile) - LOD(other));

            int otherCount = Dimension(tile) / tileStep;
            for (int i = 0; i <= otherCount; ++i)
            {
                int tileVertexIndex = tileVertexOffset + (tileVertexStep * i * tileStep);
                int otherVertexIndex = otherVertexOffset + (otherVertexStep * i);
                tile.vertices[tileVertexIndex].y = other.vertices[otherVertexIndex].y;
                //tile.normals[tileVertexIndex] = other.normals[otherVertexIndex];
            }
            
            if (LOD(tile) > LOD(other))
                InterpolateWestEdge(tile, tileStep);
        }

        static void InterpolateSouthEdge(Tile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, 0, 1);
        static void InterpolateNorthEdge(Tile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, MeshDimension(tile) * (MeshDimension(tile) - 1), 1);
        static void InterpolateWestEdge(Tile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, 0, MeshDimension(tile));
        static void InterpolateEastEdge(Tile tile, int segmentWidth) => InterpolateEdge(tile, segmentWidth, MeshDimension(tile) - 1, MeshDimension(tile));

        static void InterpolateEdge(Tile tile, int segmentWidth, int vertexArrayOffset, int vertexArrayStep)
        {
            for (int i = 0; i + segmentWidth < MeshDimension(tile); i += segmentWidth)
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

        static bool IsSouth(Tile tile, Tile other)
        {
            if (South(tile) < South(other))
                return false;
            if (South(tile) > North(other))
                return false;
            if (East(tile) <= West(other))
                return false;
            if (West(tile) >= East(other))
                return false;
            if ((LOD(tile) == LOD(other)) && (South(tile) == South(other)))
                return false;
            return true;
        }
        static bool IsWest(Tile tile, Tile other)
        {
            if (West(tile) < West(other))
                return false;
            if (West(tile) > East(other))
                return false;
            if (North(tile) <= South(other))
                return false;
            if (South(tile) >= North(other))
                return false;
            if ((LOD(tile) == LOD(other)) && (West(tile) == West(other)))
                return false;
            return true;
        }
        static bool IsNorth(Tile tile, Tile other)
        {
            if (North(tile) > North(other))
                return false;
            if (North(tile) < South(other))
                return false;
            if (East(tile) <= West(other))
                return false;
            if (West(tile) >= East(other))
                return false;
            if ((LOD(tile) == LOD(other)) && (North(tile) == North(other)))
                return false;
            return true;
        }
        static bool IsEast(Tile tile, Tile other)
        {
            if (East(tile) > East(other))
                return false;
            if (East(tile) < West(other))
                return false;
            if (North(tile) <= South(other))
                return false;
            if (South(tile) >= North(other))
                return false;
            if ((LOD(tile) == LOD(other)) && (East(tile) == East(other)))
                return false;
            return true;
        }

        static bool IsNorthEast(Tile tile, Tile other)
        {
            return (North(tile) == South(other)) && (East(tile) == West(other));
        }

    }
}
