using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Editor.LODGeneration
{
    public class KDTreeNode
    {
        public Vector3 Point;
        public KDTreeNode Left;
        public KDTreeNode Right;

        public KDTreeNode(Vector3 point, KDTreeNode left = null, KDTreeNode right = null)
        {
            Point = point;
            Left = left;
            Right = right;
        }
    }

    public class KDTree
    {
        private KDTreeNode root;

        public void Build(List<Vector3> points, int depth = 0)
        {
            root = BuildKDTree(points, depth);
        }

        private KDTreeNode BuildKDTree(List<Vector3> points, int depth)
        {
            if (points.Count == 0)
                return null;

            int k = 3; // 3D space
            int axis = depth % k;

            List<Vector3> sortedPoints = points.OrderBy(p => p[axis]).ToList();
            int median = sortedPoints.Count / 2;

            return new KDTreeNode(
                sortedPoints[median],
                BuildKDTree(sortedPoints.GetRange(0, median), depth + 1),
                BuildKDTree(sortedPoints.GetRange(median + 1, sortedPoints.Count - median - 1), depth + 1)
            );
        }

        public (double, Vector3) SearchNearestNeighbor(Vector3 target)
        {
            double bestDistance = double.PositiveInfinity;
            Vector3 bestPoint = Vector3.zero;
            SearchNearestNeighbor(root, target, ref bestDistance, ref bestPoint, 0);
            return (bestDistance, bestPoint);
        }

        private void SearchNearestNeighbor(KDTreeNode root, Vector3 target, ref double bestDistance, ref Vector3 bestPoint, int depth)
        {
            if (root == null)
                return;

            int k = 3; // 3D space
            int axis = depth % k;

            double currentDistance = Vector3.Distance(root.Point, target);
            if (currentDistance < bestDistance)
            {
                bestDistance = currentDistance;
                bestPoint = root.Point;
            }

            KDTreeNode nextBranch = target[axis] < root.Point[axis] ? root.Left : root.Right;
            KDTreeNode oppositeBranch = target[axis] < root.Point[axis] ? root.Right : root.Left;

            SearchNearestNeighbor(nextBranch, target, ref bestDistance, ref bestPoint, depth + 1);

            if (Math.Pow(target[axis] - root.Point[axis], 2) < bestDistance)
            {
                SearchNearestNeighbor(oppositeBranch, target, ref bestDistance, ref bestPoint, depth + 1);
            }
        }
    }

}