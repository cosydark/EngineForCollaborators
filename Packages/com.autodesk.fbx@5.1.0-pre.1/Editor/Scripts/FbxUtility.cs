using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Fbx;
using UnityEditor;
using UnityEngine;

namespace Editor.Scripts
{
    public static class FBXNode
    {
        public static NodeInfo[] AnalyzeFbx(string fbxPath)
        { 
            FbxManager manager = null;
            FbxImporter importer = null;
            FbxScene scene = null;

            try
            {
                manager = FbxManager.Create();
                FbxIOSettings ios = FbxIOSettings.Create(manager, "IO Setting");
                manager.SetIOSettings(ios);

                // Import
                importer = FbxImporter.Create(manager, "Importer");
                if (!importer.Initialize(fbxPath, -1, manager.GetIOSettings()))
                {
                    throw new Exception("Failed to initialize FbxImporter with path: " + fbxPath);
                }

                scene = FbxScene.Create(manager, "fbxScene");
                importer.Import(scene);

                // Analyze
                FbxNode rootNode = scene.GetRootNode();
                List<FbxNode> nodes = new List<FbxNode>();
                TraverseNodes(rootNode, ref nodes);

                // Sort
                List<NodeInfo> nodeInfos = nodes.Select(node => new NodeInfo { Node = node }).ToList();

                return nodeInfos.ToArray();
            }
            finally
            {
                // Destroy the created instances in reverse order of creation
                if (scene != null) scene.Destroy();
                if (importer != null) importer.Destroy();
                if (manager != null) manager.Destroy();
            }
        }

        private static void TraverseNodes(FbxNode node, ref List<FbxNode> nodes)
        {
            for (int i = 0; i < node.GetChildCount(); i++)
            {
                FbxNode childNode = node.GetChild(i);
                nodes.Add(childNode);
                TraverseNodes(childNode, ref nodes);
            }
        }

        public static NodeInfo GetMeshNodeInfoFromNodeInfos(Mesh mesh, NodeInfo[] nodeInfos)
        {
            NodeInfo meshNode = null;
            var matchingNodes = nodeInfos.Where(n => n.NodeName == mesh.name).ToArray();

            if (matchingNodes.Length <= 0)
            {
                Debug.LogError($"Error: 没有找到名称为'{mesh.name}'的节点。");
                return null;
            }

            meshNode = matchingNodes.OrderByDescending(n => n.MaterialElements.Count).First();
            if (matchingNodes.Length > 1)
            {
                Debug.LogWarning($"Warning: 有多个节点的名称为'{mesh.name}'. " +
                                 $"拥有最多 MaterialElements 的那个({meshNode.MaterialElements.Count}) 被选中。");
            }

            return meshNode;
        }

        public static NodeInfo GetMeshNodeInfo(Mesh mesh)
        {
            string fbxPath = AssetDatabase.GetAssetPath(mesh);
            NodeInfo[] nodeInfos = FBXNode.AnalyzeFbx(fbxPath);
            return FBXNode.GetMeshNodeInfoFromNodeInfos(mesh, nodeInfos);
        }
    }

    public class NodeInfo
    {
        public FbxNode Node
        {
            set
            {
                node = value;
                nodeName = node.GetName();
                materialElements = FetchMaterialElementsFromNode(node);
            }
            get => node;
        }

        public string NodeName => nodeName;
        public List<string> MaterialElements => materialElements;
        public string GetMaterialName(int index) => GetMaterialNameByIndex(index);
        public int GetMaterialIndex(string name) => GetMaterialIndexByName(name);

        #region PrivateLines

        private FbxNode node;
        private string nodeName;
        private List<string> materialElements;

        private List<string> FetchMaterialElementsFromNode(FbxNode node)
        {
            List<string> materialElementsName = new List<string>();
            for (int i = 0; i < node.GetMaterialCount(); i++)
            {
                materialElementsName.Add(node.GetMaterial(i).GetName());
            }

            return materialElementsName;
        }

        private string GetMaterialNameByIndex(int index)
        {
            return materialElements[index];
        }

        private int GetMaterialIndexByName(string materialElementsName)
        {
            int index = -1;
            for (int i = 0; i < materialElements.Count; i++)
            {
                if (materialElements[i] == materialElementsName)
                {
                    index = i;
                }
            }

            return index;
        }

        #endregion
    }

    public static class TexelDensityCalculator
    {
        public static float CalculateTexelDensity(Mesh mesh, Texture texture)
        {
            if (mesh == null)
            {
                Debug.LogError("Mesh or Texture is null.");
                return 0f;
            }

            float meshSurfaceArea = CalculateMeshSurfaceArea(mesh);
            Debug.Log("MeshSurface Area= " + meshSurfaceArea);
            float uvArea = CalculateUVArea(mesh);
            Debug.Log("UV Area= " + uvArea);
            float textureArea = texture.width * texture.height;

            // Texel Density = (Texture Area in Pixels) / (Mesh Surface Area in World Space * UV Area in UV Space)
            float texelDensity = textureArea * uvArea / (meshSurfaceArea);

            return Mathf.Sqrt(texelDensity);
        }

        private static float CalculateMeshSurfaceArea(Mesh mesh)
        {
            float surfaceArea = 0f;
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];

                surfaceArea += Vector3.Cross(v1 - v2, v1 - v3).magnitude * 0.5f;
            }

            return surfaceArea;
        }

        private static float CalculateUVArea(Mesh mesh)
        {
            float uvArea = 0f;
            var triangles = mesh.triangles;
            var uvs = mesh.uv;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector2 uv1 = uvs[triangles[i]];
                Vector2 uv2 = uvs[triangles[i + 1]];
                Vector2 uv3 = uvs[triangles[i + 2]];
                // 计算三角形的UV面积
                float triangleArea = Mathf.Abs(Vector2Cross(uv1 - uv2, uv1 - uv3) * 0.5f);
                uvArea += triangleArea;
            }

            return uvArea;
        }

        // 计算两个Vector2的外积
        private static float Vector2Cross(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }
    }
}