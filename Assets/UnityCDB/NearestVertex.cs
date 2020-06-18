using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitics.UnityCDB
{
    public class NearestVertex
    {
        public int closestVertexIndex = -1;
        private Vector3[] vertices;
        private Vector3 tileOrigin;
        private Vector3 position;
        private int MeshDimension;
        public float maxElevation;
        private float xSpacing;
        private float zSpacing;
        private int Nearest2D = -1;
        private float DistanceToNearestCandidate;

        private Vector3 LeftLowerBound;
        private Vector3 RightUpperBound;

        public int GetNearestVertexIndex(Vector3 position, Vector3[] vertices)
        {
            this.vertices = vertices;
            this.position = position;

            tileOrigin = vertices[0];
            LeftLowerBound = tileOrigin;
            RightUpperBound = vertices[vertices.Length - 1];

            MeshDimension = (int)Mathf.Sqrt(vertices.Length);
            xSpacing = Math.Abs(vertices[1].x - vertices[0].x);
            zSpacing = Math.Abs(vertices[MeshDimension].z - vertices[0].z);

            /** Increase this for greater accuracy from the OptimizeByElevation function.
             *  At 1, the results are perfect. Current value is (0.87)^2 ~= 0.757, which means that
             *  the tile is searched only if the distance to the 2D nearest vertex is greater than 
             *  75% of the distance to the potential 3D nearest vertex.
             **/

            float comparisonPercentage = 0.87f;

            Locate2D();

            OptimizeByElevation(position, vertices, comparisonPercentage);

            return closestVertexIndex;
        }

        public int GetNearestVertexIndex(Vector3 position, Vector3[] vertices, Vector3 leftLowerBound, Vector3 rightUpperBound)
        {
            this.vertices = vertices;
            this.position = position;

            MeshDimension = (int)Mathf.Sqrt(vertices.Length);
            if (vertices.Length <= MeshDimension || vertices.Length <= 1)
            {
                closestVertexIndex = -1;
                Debug.LogError("GetNearestVertexIndex: " + closestVertexIndex);
                return closestVertexIndex;
            }

            tileOrigin = vertices[0];
            LeftLowerBound = leftLowerBound;
            RightUpperBound = rightUpperBound;

            xSpacing = Math.Abs(vertices[1].x - vertices[0].x);
            zSpacing = Math.Abs(vertices[MeshDimension].z - vertices[0].z);

            /** Increase this for greater accuracy from the OptimizeByElevation function.
             *  At 1, the results are perfect. Current value is (0.87)^2 ~= 0.757, which means that
             *  the tile is searched only if the distance to the 2D nearest vertex is greater than 
             *  75% of the distance to the potential 3D nearest vertex.
             **/
            float comparisonPercentage = 0.87f;

            Locate2D();

            OptimizeByElevation(position, vertices, comparisonPercentage);

            return closestVertexIndex;
        }


        private void OptimizeByElevation(Vector3 position, Vector3[] vertices, float comparisonPercentage)
        {
            if (vertices.Length == 0 || Nearest2D > vertices.Length - 1)
                return;

            // If the user is higher than the tile's maxElevation
            if (position.y > maxElevation)
            {
                Vector3 v = new Vector3(vertices[Nearest2D].x, maxElevation, vertices[Nearest2D].z);

                if ((position - v).sqrMagnitude * comparisonPercentage < (position - vertices[Nearest2D]).sqrMagnitude)
                    closestVertexIndex = Nearest2D;

                else if (LeftLowerBound != vertices[0] || RightUpperBound != vertices[vertices.Length - 1])
                    Locate3DWithBounds();
                else
                    Locate3D();
            }

            // If the user is lower than the tile's maxElevation and higher than the nearest vertex2D elevation
            else if (position.y < maxElevation && position.y > vertices[Nearest2D].y)
            {
                Vector3 v = new Vector3(vertices[Nearest2D].x, position.y, vertices[Nearest2D].z);

                if ((position - v).sqrMagnitude * comparisonPercentage < (position - vertices[Nearest2D]).sqrMagnitude)
                    closestVertexIndex = Nearest2D;

                else if (LeftLowerBound != vertices[0] || RightUpperBound != vertices[vertices.Length - 1])
                    Locate3DWithBounds();
                else
                    Locate3D();
            }

            else
            {
                Vector3 v = new Vector3(vertices[Nearest2D].x, maxElevation, vertices[Nearest2D].z);

                if ((position - v).sqrMagnitude < (position - vertices[Nearest2D]).sqrMagnitude * comparisonPercentage)
                    closestVertexIndex = Nearest2D;

                else if (LeftLowerBound != vertices[0] || RightUpperBound != vertices[vertices.Length - 1])
                    Locate3DWithBounds();
                else
                    Locate3D();
            }
        }


        /***************************** 3D LOCATION *******************************/

        private void Locate3D()
        {
            int storedMinDistance = Nearest2D;
            int maxSearchRadius = CalculateSearchRadius(storedMinDistance);

            for (int searchRadius = 1; searchRadius < maxSearchRadius; ++searchRadius)
            {
                int storedMinCopy = storedMinDistance;

                // 2D x-y center of search area, snapped to grid
                int x2D = Nearest2D % MeshDimension;
                int y2D = Nearest2D / MeshDimension;

                int xstart = Mathf.Max(x2D - searchRadius, 0);
                int xstop = Mathf.Min(x2D + searchRadius, MeshDimension - 1);

                int ystart = Mathf.Max(y2D - searchRadius, 0);
                int ystop = Mathf.Min(y2D + searchRadius, MeshDimension - 1);

                for (int y = ystart, x = xstart; x <= xstop; ++x) // Top
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                for (int y = ystop, x = xstart; x <= xstop; ++x) // Bottom
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                for (int y = ystart, x = xstart; y <= ystop; ++y) // Left side
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                for (int y = ystart, x = xstop; y <= ystop; ++y) // Right side
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                // If a closer vertex is found, recalculate the search radius
                if (storedMinCopy != storedMinDistance)
                    maxSearchRadius = CalculateSearchRadius(storedMinDistance);
            }

            closestVertexIndex = storedMinDistance;
        }

        private void Locate3DWithBounds()
        {
            int storedMinDistance = Nearest2D;
            int maxSearchRadius = CalculateSearchRadius(storedMinDistance);

            for (int searchRadius = 1; searchRadius < maxSearchRadius; ++searchRadius)
            {
                int storedMinCopy = storedMinDistance;

                // 2D x-y center of search area, snapped to grid
                int x2D = Nearest2D % MeshDimension;
                int y2D = Nearest2D / MeshDimension;

                int xLowerBound = (int)(Math.Floor((LeftLowerBound.x - tileOrigin.x) / xSpacing));
                int zLowerBound = (int)(Math.Floor((LeftLowerBound.z - tileOrigin.z) / zSpacing));

                int xUpperBound = (int)(Math.Ceiling((RightUpperBound.x - tileOrigin.x) / xSpacing));
                int zUpperBound = (int)(Math.Ceiling((RightUpperBound.z - tileOrigin.z) / zSpacing));

                int xstart = Mathf.Max(Mathf.Max(x2D - searchRadius, xLowerBound), 0);
                int xstop = Mathf.Min(Mathf.Min(x2D + searchRadius, xUpperBound), MeshDimension - 1);

                int ystart = Mathf.Max(Mathf.Max(y2D - searchRadius, zLowerBound), 0);
                int ystop = Mathf.Min(Mathf.Min(y2D + searchRadius, zUpperBound), MeshDimension - 1);

                for (int y = ystart, x = xstart; x <= xstop; ++x) // Top
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                for (int y = ystop, x = xstart; x <= xstop; ++x) // Bottom
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                for (int y = ystart, x = xstart; y <= ystop; ++y) // Left side
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                for (int y = ystart, x = xstop; y <= ystop; ++y) // Right side
                    storedMinDistance = CompareIndexes(storedMinDistance, y, x);

                // If a closer vertex is found, recalculate the search radius
                if (storedMinCopy != storedMinDistance)
                    maxSearchRadius = CalculateSearchRadius(storedMinDistance);
            }

            closestVertexIndex = storedMinDistance;
        }

        private int CalculateSearchRadius(int storedMinDistance)
        {
            int maxSearchRadius;
            DistanceToNearestCandidate = Vector3.Distance(position, vertices[storedMinDistance]);

            maxSearchRadius = Mathf.Min((int)(DistanceToNearestCandidate / xSpacing), (int)(DistanceToNearestCandidate / zSpacing));

            // Check to make sure the radius can be divided by 2
            if (maxSearchRadius % 2 != 0)
                ++maxSearchRadius;

            return maxSearchRadius;
        }

        private int CompareIndexes(int currentMin, int y, int x)
        {
            int currentIndex = Index2DTo1D(x, y);

            float storedClosest = (vertices[currentMin] - position).sqrMagnitude;
            float candidateClosest = (vertices[Index2DTo1D(x, y)] - position).sqrMagnitude;

            if (storedClosest > candidateClosest)
                currentMin = currentIndex;

            return currentMin;
        }

        private int Index2DTo1D(int x, int y)
        {
            return y * MeshDimension + x;
        }



        /***************************** 2D LOCATION *******************************/

        public void Locate2D()
        {
            Vector3 nearestCoordinate = position;

            nearestCoordinate.x = Mathf.Clamp(nearestCoordinate.x, LeftLowerBound.x, RightUpperBound.x);
            nearestCoordinate.z = Mathf.Clamp(nearestCoordinate.z, LeftLowerBound.z, RightUpperBound.z);

            nearestCoordinate.x = Mathf.Clamp(nearestCoordinate.x, tileOrigin.x, vertices[vertices.Length - 1].x);
            nearestCoordinate.z = Mathf.Clamp(nearestCoordinate.z, tileOrigin.z, vertices[vertices.Length - 1].z);

            int closestX = (int)((nearestCoordinate.x - tileOrigin.x) / xSpacing);
            int closestZ = (int)((nearestCoordinate.z - tileOrigin.z) / zSpacing);

            Nearest2D = MeshDimension * closestZ + closestX;
        }

        public float Distance2D(Vector3 a, Vector3 b)
        {
            a.y = 0;
            b.y = 0;
            return Vector3.Distance(a, b);
        }

        private float Difference(float a, float b)
        {
            return Math.Abs(a - b);
        }


        /** Debug Helpers **/
        private void DrawLineYellow(int vertexIndex)
        {
            Vector3 vertex = vertices[vertexIndex];
            Debug.DrawLine(vertex, position, Color.yellow);
        }
        private void DrawLineRed(int vertexIndex)
        {
            Vector3 vertex = vertices[vertexIndex];
            Debug.DrawLine(vertex, position, Color.red);
        }
        private void AnimateSearchRadius(List<int> checkedIndexes)
        {
            for (int i = 0; i < checkedIndexes.Count; ++i)
                Debug.DrawLine(vertices[i], position);
        }
    }
}
