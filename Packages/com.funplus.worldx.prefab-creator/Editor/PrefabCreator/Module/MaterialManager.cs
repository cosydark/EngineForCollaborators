using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Editor.Scripts;
using UnityEngine;
using PrefabMaterialCache = System.Collections.Generic.List<System.Collections.Generic.List<Editor.PrefabCreator.Module.RendererMaterialManager>>;

namespace Editor.PrefabCreator.Module
{
    public class RendererMaterialManager
    {
        private readonly List<Material> _materials;
        private readonly Dictionary<string, Material> _fbxMaterialNameDictionary;
        public int materialCount => _materials.Count;

        public RendererMaterialManager(GameObject gameObject)
        {
            _materials = new List<Material>();
            _fbxMaterialNameDictionary = new Dictionary<string, Material>();

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null) return;
            Mesh mesh = GetMeshFromRenderer(renderer);
            if (mesh == null) return;


            NodeInfo currentNode = FBXNode.GetMeshNodeInfo(mesh);
            if (currentNode == null)
            {
                Debug.LogError("当前Mesh没有对应的NodeInfo");
                return;
            }

            AddMaterialsFromNodeInfo(renderer, currentNode);
        }

        public static void ApplyToRenderer(GameObject targetMeshGameObject, RendererMaterialManager materialManager)
        {
            //跳过billboard和imposter
            if (ParsingObject.IsImposter(targetMeshGameObject) || ParsingObject.IsBillboard(targetMeshGameObject))
            {
                return;
            }

            Renderer targetRenderer = targetMeshGameObject.GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogError($"{targetMeshGameObject} 缺少Renderer");
                return;
            }

            Mesh mesh = GetMeshFromRenderer(targetRenderer);
            if (mesh == null)
            {
                Debug.LogError($"{targetMeshGameObject} 缺少Mesh");
                return;
            }

            NodeInfo currentNode = FBXNode.GetMeshNodeInfo(mesh);
            if (currentNode == null || currentNode.MaterialElements.Count != mesh.subMeshCount)
            {
                Debug.LogError("材质应用错误");
                return;
            }

            Material[] materialsToApply = new Material[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                string currentName = currentNode.GetMaterialName(i);

                //使用Fbx原名称去寻找材质球
                if (materialManager._fbxMaterialNameDictionary.TryGetValue(currentName, out var material1))
                {
                    // Debug.Log("Found!" + currentName);
                    materialsToApply[i] = material1;
                } //使用默认的材质球
                else
                {
                    // Debug.Log("Not Found!" + currentName + "    FBXDict =" + materialManager._fbxMaterialNameDictionary.Keys);

                    materialsToApply[i] = materialManager._materials[^1];
                }
            }

            targetRenderer.sharedMaterials = materialsToApply;
        }

        public static void ApplyToPrefab(GameObject gameObject, PrefabMaterialCache prefabMaterialsCache)
        {
            LODGroup lodGroup = gameObject.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                Debug.LogError($"未找到 {gameObject.name} 上的 LodGroup 无法恢复材质");
                return;
            }

            LOD[] lods = lodGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                LOD lod = lods[i];
                Renderer[] sortedRenderers = lod.renderers.OrderBy(r => r.gameObject.name).ToArray();

                List<RendererMaterialManager> lodMaterials = i < prefabMaterialsCache.Count
                    ? prefabMaterialsCache[i]
                    : prefabMaterialsCache[^1];

                for (int j = 0; j < sortedRenderers.Length; j++)
                {
                    Renderer renderer = sortedRenderers[j];
                    RendererMaterialManager rendererMaterialManager;
                    if (j < lodMaterials.Count)
                    {
                        rendererMaterialManager = lodMaterials[j];
                    }
                    else
                    {
                        // 如果没有足够的材质对象信息，则复用最后一个材质对象
                        rendererMaterialManager = lodMaterials[^1];
                        Debug.LogWarning($"LOD {i} 的 Renderer {j} 在 {gameObject.name} 上使用了复用的材质对象。");
                    }

                    // 应用材质
                    ApplyToRenderer(renderer.gameObject, rendererMaterialManager);
                }
            }
        }

        public static void ApplyToPrefabs(List<GameObject> prefabs,
            IReadOnlyDictionary<GameObject, PrefabMaterialCache> prefabsMaterialCacheDictionary)
        {
            Debug.Log("模型处理完毕 执行后处理");
            foreach (var prefab in prefabs)
            {
                PrefabMaterialCache prefabMaterialsCache = prefabsMaterialCacheDictionary[prefab];
                ApplyToPrefab(prefab, prefabMaterialsCache);
            }

            Debug.Log("材质还原完毕");
        }

        #region Private Function

        public static Mesh GetMeshFromRenderer(Renderer renderer)
        {
            switch (renderer)
            {
                case MeshRenderer when renderer.GetComponent<MeshFilter>():
                    return renderer.GetComponent<MeshFilter>().sharedMesh;
                case SkinnedMeshRenderer skinnedMeshRenderer:
                    return skinnedMeshRenderer.sharedMesh;
                default:
                    return null;
            }
        }

        private void AddMaterialsFromNodeInfo(Renderer renderer, NodeInfo currentNode)
        {
            var materials = renderer.sharedMaterials.ToList();
            if (materials.Count == 0) return;
            _materials.AddRange(materials);
            for (int i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                _fbxMaterialNameDictionary[currentNode.GetMaterialName(i)] = material;
                // Debug.Log("Cache --->" + currentNode.GetMaterialName(i));
            }
        }

        #endregion
    }

    public class MatchGameObjectPairByName
    {
        public static Dictionary<GameObject, GameObject> FindSimilarGameObjectPair(List<GameObject> A,
            List<GameObject> B)
        {
            if (A == null || B == null)
                throw new ArgumentNullException("Input lists A and B cannot be null.");

            Dictionary<GameObject, GameObject> SimilarGameObjectPair = new Dictionary<GameObject, GameObject>();

            var dictA = ConvertToNameGameObjectDict(A);
            var dictB = ConvertToNameGameObjectDict(B);

            var keysA = new HashSet<string>(dictA.Keys);
            var keysB = new HashSet<string>(dictB.Keys);

            foreach (var keyA in keysA)
            {
                string matchedKeyB = FindMostSimilarString(keyA, keysB);
                if (!string.IsNullOrEmpty(matchedKeyB))
                {
                    GameObject matchedGameObjectB = dictB[matchedKeyB];
                    SimilarGameObjectPair[dictA[keyA]] = matchedGameObjectB;
                }
            }

            return SimilarGameObjectPair;
        }

        private static string FindMostSimilarString(string target, HashSet<string> stringSet)
        {
            if (string.IsNullOrEmpty(target) || stringSet == null)
                return null;

            string mostSimilar = null;
            int lowestDistance = int.MaxValue;

            foreach (var strB in stringSet)
            {
                int distance = ComputeLevenshteinDistance(target, strB);
                if (distance < lowestDistance)
                {
                    lowestDistance = distance;
                    mostSimilar = strB;
                }
            }

            return mostSimilar;
        }

        private static Dictionary<string, GameObject> ConvertToNameGameObjectDict(List<GameObject> list)
        {
            if (list == null)
                throw new ArgumentNullException("Input list cannot be null.");

            Dictionary<string, GameObject> dictionary = new Dictionary<string, GameObject>();
            foreach (var gameObject in list)
            {
                string name = gameObject.name;
                // Remove LOD structure
                name = Regex.Replace(name, "_LOD[0-9]*", "", RegexOptions.IgnoreCase);
                dictionary[name] = gameObject;
            }

            return dictionary;
        }

        private static int ComputeLevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
                return string.IsNullOrEmpty(b) ? 0 : b.Length;

            if (string.IsNullOrEmpty(b))
                return a.Length;

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];

            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
            for (int j = 1; j <= lengthB; j++)
            {
                int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }

            return distances[lengthA, lengthB];
        }
    }
}