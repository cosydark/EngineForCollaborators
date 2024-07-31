using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Editor.PrefabCreator;
using Editor.PrefabCreator.Module;
using UnityEditor;
using UnityEngine;

#if XRENDER
using XRender.Pipeline.Modules.MultiplePassRendering;
#endif


namespace Editor.CharacterLodGenerator
{
    public partial class CharacterLodGenerator
    {
        #region Process Function

        //根据PrefabList初始化FBX Mesh
        private void UpdateFBXMesh()
        {
            allMesh.Clear();
            foreach (var prefab in prefabList)
            {
                allMesh = allMesh.Union(GetFBXGameObjectsForPrefab(prefab)).ToList();
            }

            ParsingCharacterMesh(allMesh, out lodMesh, out vfxMesh, out otherMesh);
            if (lodMesh.Count < 1)
            {
                Debug.LogError("没有找到有效的LOD MESH 无法执行操作");
                EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent("没有找到有效的LOD MESH 无法执行操作"), 0.5d);

                return;
            }

            LOD0FBXSavePath = AssetDatabase.GetAssetPath(lodMesh[0]);
        }

        /// 为只有LOD0且没有LODGroup的物体实例列表快速添加LODGroup。
        private static void CheckAddLodGroup(List<GameObject> prefabInsList)
        {
            foreach (var prefabIns in prefabInsList)
            {
                if (prefabIns.GetComponent<LODGroup>() == null)
                {
                    CheckAddLodGroup(prefabIns);
                }
            }
        }

        private static void CheckAddLodGroup(GameObject prefabIns)
        {
            if (prefabIns == null) return;
            // 添加LODGroup组件
            LODGroup lodGroup = prefabIns.AddComponent<LODGroup>();

            // 获取所有的MeshRenderer和SkinnedMeshRenderer组件
            Renderer[] renderers = prefabIns.GetComponentsInChildren<Renderer>(true);

            // 创建一个列表来存储不包含特定字符串的渲染器
            List<Renderer> validRenderers = new List<Renderer>();

            // 遍历所有渲染器并检查它们的游戏对象名称
            foreach (Renderer renderer in renderers)
            {
                // 确保只选择MeshRenderer和SkinnedMeshRenderer
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                    continue;

                string nameLower = renderer.gameObject.name.ToLower();
                if (!nameLower.Contains("_vfx") && !nameLower.Contains("vfx_"))
                {
                    validRenderers.Add(renderer);
                }
            }

            // 如果有有效的渲染器，则创建LOD实例并设置渲染器
            if (validRenderers.Count > 0)
            {
                LOD lod = new LOD(0.01f, validRenderers.ToArray()); // 0.01f是屏幕相对大小的阈值，可以根据需要调整

                // 设置LODGroup的LODs数组，只包含一个LOD层级
                lodGroup.SetLODs(new LOD[] { lod });

                // 重新计算LODGroup的边界
                lodGroup.RecalculateBounds();
            }
        }


        /// 获取预制体中所有使用的FBX物体实例。
        private static List<GameObject> GetFBXGameObjectsForPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("No prefab provided.");
                return null;
            }

            // 存储找到的FBX GameObjects
            List<GameObject> fbxGameObjects = new List<GameObject>();

            // 获取预制体中所有的MeshFilter和SkinnedMeshRenderer组件
            MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            SkinnedMeshRenderer[] skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            // 遍历所有MeshFilter组件
            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    string path = AssetDatabase.GetAssetPath(mesh);
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    {
                        GameObject fbxGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (fbxGameObject != null && !fbxGameObjects.Contains(fbxGameObject))
                        {
                            fbxGameObjects.Add(fbxGameObject);
                        }
                    }
                }
            }

            // 遍历所有SkinnedMeshRenderer组件
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                if (mesh != null)
                {
                    string path = AssetDatabase.GetAssetPath(mesh);
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    {
                        GameObject fbxGameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (fbxGameObject != null && !fbxGameObjects.Contains(fbxGameObject))
                        {
                            fbxGameObjects.Add(fbxGameObject);
                        }
                    }
                }
            }

            return fbxGameObjects;
        }

        //删除所有PrefabIns的Lod的对象
        private void CleanUpBeforeGenerateLOD()
        {
            foreach (var prefab in prefabInsList)
            {
                RemoveLodMeshButLod0(prefab);
                if (regenerateVFXMesh) RemoveVFXMesh(prefab);
            }
        }

        /// 删除除LOD0之外的所有LOD网格对象。
        private static void RemoveLodMeshButLod0(GameObject source)
        {
            if (source == null)
            {
                Debug.LogError("Source GameObject is null.");
                return;
            }

            // 正则表达式用于匹配以"_LOD"开头，后跟非零数字的名称，不区分大小写
            string pattern = @"_LOD\d+";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

            int deleteCount = 0;
            // 从后往前遍历直接子对象
            for (int i = source.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = source.transform.GetChild(i);

                // 检查子对象名称是否匹配正则表达式
                if (child != null && regex.IsMatch(child.name))
                {
                    // 检查匹配的数字是否为0
                    if (int.TryParse(regex.Match(child.name).Value.Substring(4), out var lodNumber) && lodNumber != 0)
                    {
                        // 在编辑器模式下直接销毁对象
                        if (Application.isEditor)
                        {
                            Undo.RegisterCompleteObjectUndo(child.gameObject, "Remove LOD Mesh");
                            UnityEngine.Object.DestroyImmediate(child.gameObject);
                            deleteCount++;
                        }
                    }
                }
            }

            Debug.Log(source.name + "共计删除了" + deleteCount + "个旧LOD对象");
        }

        private static void RemoveVFXMesh(GameObject source)
        {
            if (source == null)
            {
                Debug.LogError("Source GameObject is null.");
                return;
            }

            // 正则表达式用于匹配包含"_vfx"、"vfx_"或"vfxmesh"的名称，不区分大小写
            string pattern = @"(_vfx|vfx_|vfxmesh)";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

            int deleteCount = 0;
            // 从后往前遍历直接子对象
            for (int i = source.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = source.transform.GetChild(i);

                // 检查子对象名称是否匹配正则表达式
                if (child != null && regex.IsMatch(child.name))
                {
                    // 在编辑器模式下直接销毁对象
                    if (Application.isEditor)
                    {
                        Undo.RegisterCompleteObjectUndo(child.gameObject, "Remove VFX Mesh");
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                        deleteCount++;
                    }
                }
            }

            Debug.Log(source.name + "共计删除了" + deleteCount + "个旧的VFX对象");
        }

        private void UpdateLODGroupRenderer()
        {
            string meshPath = LOD0FBXSavePath;
            foreach (var prefab in prefabInsList)
            {
                // find Bip data
                GameObject bip = null;
                for (int i = prefab.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = prefab.transform.GetChild(i);
                    if (child.name.ToLower().Contains("bip"))
                    {
                        bip = child.gameObject;
                    }
                }

                if (bip == null)
                {
                    if (EditorUtility.DisplayDialog("资源检测", "当前模型没有骨骼数据，请检查并修复。", "确认"))
                    {
                    }
                }

                // update LODGroup with new meshes

                LODGroup lodGroup = prefab.GetComponent<LODGroup>();
                LOD[] lods = new LOD[lodCount];
                lods[0] = lodGroup.GetLODs()[0];
                for (int i = 0; i < lodCount; i++)
                {
                    if (i != 0)
                    {
                        string assetPath = meshPath.Substring(0, meshPath.Length - 5) + i.ToString() + ".fbx";
                        AssetDatabase.ImportAsset(assetPath);
                        GameObject meshObj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                        GameObject lodInstance = Instantiate(meshObj);
                        Renderer[] renderers = lodInstance.GetComponentsInChildren<Renderer>();
                        lods[i] = new LOD();
                        lods[i].renderers = renderers;

                        UpdateBoneRoot(lodInstance, bip);
                        lodInstance.transform.parent = prefab.transform;
                        lodInstance.transform.localPosition = Vector3.zero;
                    }

                    lods[i].screenRelativeTransitionHeight = defaultScreenRatio[i];
                }

                lods[lodCount - 1].screenRelativeTransitionHeight = 0.01f;
                lodGroup.SetLODs(lods);


                string vfxPath = meshPath.Substring(0, meshPath.Length - 8) + "VFX.fbx";

                if (regenerateVFXMesh)
                {
                    AssetDatabase.ImportAsset(vfxPath);
                    GameObject vfxObj = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
                    if (vfxObj != null)
                    {
                        GameObject vfxInstance = Instantiate(vfxObj);
                        for (int i = vfxInstance.transform.childCount - 1; i >= 0; i--)
                        {
                            Transform child = vfxInstance.transform.GetChild(i);
                            Renderer[] renderers = child.gameObject.GetComponentsInChildren<Renderer>();
                            foreach (var renderer in renderers)
                            {
                                renderer.sharedMaterials = new Material[0];
                            }

                            if (child.name.ToLower().EndsWith("vfx"))
                                child.gameObject.name = "vfx_mesh";
                        }

                        UpdateBoneRoot(vfxInstance, bip);
                        vfxInstance.transform.parent = prefab.transform;
                        vfxInstance.transform.localPosition = Vector3.zero;
                    }
                }
            }
        }

        private void UpdateBoneRoot(GameObject instance, GameObject bip)
        {
            if (bip == null)
            {
                return;
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0)
                return;

            var dic = bip.GetComponentsInChildren<Transform>().ToDictionary(t => t.transform.name);
            // bind the bone in LOD0 Bip
            foreach (var mr in skinnedMeshRenderers)
            {
                var newBoneList = new List<Transform>();
                foreach (var b in mr.bones)
                {
                    newBoneList.Add(dic[b.name]);
                }

                mr.bones = newBoneList.ToArray();
                mr.rootBone = dic[mr.rootBone.name];
            }

            // delete current LOD Bip
            for (int i = instance.transform.childCount - 1; i >= 0; i--)
            {
                if (instance.transform.GetChild(i).name.ToLower().Contains("bip"))
                {
                    DestroyImmediate(instance.transform.GetChild(i).gameObject);
                }
            }
        }

        //删除子对象名称中的Clone
        private void RenamePrefabsClonedChildren()
        {
            foreach (var prefab in prefabInsList)
            {
                if (prefab == null)
                {
                    Debug.LogError("Parent GameObject is null.");
                    return;
                }

                // 遍历所有直接子对象
                foreach (Transform child in prefab.transform)
                {
                    // 检查子对象名称是否包含"(Clone)"
                    if (child.name.Contains("(Clone)"))
                    {
                        // 移除"(Clone)"并更新子对象的名称
                        child.name = child.name.Replace("(Clone)", "").Trim();
                    }
                }
            }
        }

        //更新后续LOD子对象的脚本、激活状态
        public void UpdatePrefabsLodComponents()
        {
            foreach (var prefab in prefabInsList)
            {
                Dictionary<GameObject, GameObject> lodSimilarities =
                    new Dictionary<GameObject, GameObject>();
                var lodGroup = prefab.GetComponent<LODGroup>();
                if (lodGroup == null) continue;

                LOD[] lods = lodGroup.GetLODs();
                if (lods.Length < 2) continue; // 至少需要两个LOD层级来比较

                // 获取LOD0的所有渲染器中的游戏对象
                List<GameObject> lod0Objects = lods[0].renderers.Select(renderer => renderer.gameObject).ToList();

                // 遍历除了LOD0之外的其他LOD
                for (int i = 1; i < lods.Length; i++)
                {
                    List<GameObject> otherLodObjects =
                        lods[i].renderers.Select(renderer => renderer.gameObject).ToList();

                    // 使用FindSimilarGameObjectPair函数找到相似的游戏对象对
                    Dictionary<GameObject, GameObject> similarPairs =
                        MatchGameObjectPairByName.FindSimilarGameObjectPair(otherLodObjects, lod0Objects);

                    // 将找到的相似对象添加到lodSimilarities字典中
                    foreach (var pair in similarPairs)
                    {
                        lodSimilarities[pair.Key] = pair.Value;
                    }
                }

                foreach (KeyValuePair<GameObject, GameObject> pair in lodSimilarities)
                {
                    GameObject lodObject = pair.Key;
                    GameObject lod0Object = pair.Value;

                    // 如果LOD0对象未激活，则将后续LOD对象也设置为非激活状态
                    if (!lod0Object.activeSelf)
                    {
                        lodObject.SetActive(false);
                    }

#if XRENDER
                    // 检查LOD0对象上的MultiplePassRenderer组件并添加新的组件到后续LOD对象
                    MultiplePassRenderer multiplePassRenderer = lod0Object.GetComponent<MultiplePassRenderer>();
                    if (multiplePassRenderer != null)
                    {
                        lodObject.AddComponent<MultiplePassRenderer>();
                    }
#endif
                }
            }
        }

        #endregion


        #region GUI Function

        //显示Mesh用的函数
        private static void DisplayAndSelectGameObject(string label, List<GameObject> gameObjects)
        {
            foreach (var mesh in gameObjects)
            {
                DisplayAndSelectGameObject(label, mesh);
            }
        }

        private static void DisplayAndSelectGameObject(string label, GameObject gameObject)
        {
            if (!gameObject) return;
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.ObjectField(label, gameObject, typeof(GameObject), true, GUILayout.ExpandWidth(true));
            GUI.enabled = true;
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                EditorGUIUtility.PingObject(
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GetAssetPath(gameObject)));
            }


            EditorGUILayout.EndHorizontal();
        }

        //显示、处理OtherMesh
        protected void DisplayAndSelectOtherGameObject()
        {
            foreach (var mesh in otherMesh)
            {
                if (!mesh) return;
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Other", mesh, typeof(GameObject), true, GUILayout.ExpandWidth(true));
                GUI.enabled = true;
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GetAssetPath(mesh)));
                }

                if (lodMesh.Count == 0)
                {
                    if (GUILayout.Button("重命名并设置为LOD0", GUILayout.Width(130)))
                    {
                        string path = AssetDatabase.GetAssetPath(mesh);
                        string directory = Path.GetDirectoryName(path);
                        string newFileName = Path.GetFileNameWithoutExtension(path) + "_LOD0" + Path.GetExtension(path);
                        string newPath = Path.Combine(directory, newFileName);

                        // 检查是否存在重名的资源
                        if (AssetDatabase.LoadAssetAtPath(newPath, typeof(GameObject)) != null)
                        {
                            EditorWindow.GetWindow<SceneView>()
                                .ShowNotification(new GUIContent("重命名失败；已存在名为 " + newFileName + " 的资源。"), 0.5d);
                        }
                        else
                        {
                            AssetDatabase.RenameAsset(path, newFileName);
                            AssetDatabase.SaveAssets();
                            UpdateFBXMesh();
                            Debug.Log("资源已重命名为: " + newFileName);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion


        #region Parsing Function

        //解析角色的模型
        private static void ParsingCharacterMesh(List<GameObject> allMesh, out List<GameObject> lodMesh,
            out GameObject vfxMesh, out List<GameObject> otherMesh)
        {
            lodMesh = new List<GameObject>();
            vfxMesh = null;
            otherMesh = new List<GameObject>();

            foreach (var mesh in allMesh)
            {
                string meshName = mesh.name.ToLower();
                if (meshName.Contains("_lod"))
                {
                    lodMesh.Add(mesh);
                }
                else if (meshName.Contains("_vfx"))
                {
                    vfxMesh = mesh;
                }
                else
                {
                    otherMesh.Add(mesh);
                }
            }

            lodMesh = lodMesh.OrderBy(mesh => ParsingObject.GetLodLevelFromName(mesh.name.ToLower())).ToList();
        }

        //根据Lod0名称获取指定Lod的名称
        public static string GetLodMeshName(string lod0Name, int lodLevel)
        {
            int lodIndex = lod0Name.IndexOf("_LOD", StringComparison.OrdinalIgnoreCase);
            string baseName = lodIndex >= 0 ? lod0Name.Substring(0, lodIndex) : lod0Name;
            string targetLodName = $"{baseName}_LOD{lodLevel}";
            return targetLodName;
        }

        #endregion


        #region Materail Function

        //通过LodGroup + 命名相似性还原单个对象的材质信息
        private void ApplyMaterialsToGameObjectByLodGroupAndNameSimilarity(GameObject gameObject,
            List<Dictionary<GameObject, RendererMaterialManager>> materialsCache)
        {
            LODGroup lodGroup = gameObject.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                Debug.LogError($"未找到 {gameObject.name} 上的 LodGroup 无法恢复材质");
                return;
            }

            if (materialsCache.Count < 1)
            {
                Debug.LogError("materialsCache 为空 无法恢复材质");
                return;
            }

            int count = 0;
            LOD[] lods = lodGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                LOD lod = lods[i];
                Renderer[] renderers = lod.renderers;

                //如果还原对象的Lod级别大于缓存的数据内容的话，则调用缓存中最后一个材质
                Dictionary<GameObject, RendererMaterialManager> lodMaterials = i < materialsCache.Count
                    ? materialsCache[i]
                    : materialsCache[materialsCache.Count - 1];

                //开始寻找名称相似的GameObject对
                List<GameObject> rendererGameObjectList = renderers.Select(r => r.gameObject).ToList();
                List<GameObject> materialGameObjectList = lodMaterials.Keys.ToList();
                var matchedPair =
                    MatchGameObjectPairByName.FindSimilarGameObjectPair(rendererGameObjectList, materialGameObjectList);
                //开始还原材质
                foreach (var rendererGameObject in matchedPair.Keys)
                {
                    GameObject matchedGameObject = matchedPair[rendererGameObject];
                    if (lodMaterials.ContainsKey(matchedGameObject))
                    {
                        RendererMaterialManager materialManager = lodMaterials[matchedGameObject];
                        RendererMaterialManager.ApplyToRenderer(rendererGameObject, materialManager);
                        count++;
                    }
                }
            }

            Debug.Log("还原了 " + count + " 个材质");
        }

        //还原所有对象的材质
        private void RevertAllPrefabsMaterial()
        {
            if (prefabInsList.Count > 0)
            {
                foreach (var prefab in prefabInsList)
                {
                    ApplyMaterialsToGameObjectByLodGroupAndNameSimilarity(prefab, prefabsMaterialCache[prefab]);
                }
            }
        }

        //获取所有Prefab对象的材质缓存
        private void GetAllPrefabMaterialCache()
        {
            prefabsMaterialCache =
                new Dictionary<GameObject, List<Dictionary<GameObject, RendererMaterialManager>>>();
            foreach (var prefab in prefabInsList)
            {
                prefabsMaterialCache[prefab] = GetPrefabMaterialCache(prefab);
            }
        }

        ///获取材质缓存
        private static List<Dictionary<GameObject, RendererMaterialManager>> GetPrefabMaterialCache(GameObject source,
            bool allLodLevel = false)
        {
            List<Dictionary<GameObject, RendererMaterialManager>> prefabMaterialCache =
                new List<Dictionary<GameObject, RendererMaterialManager>>();
            if (source.GetComponent<LODGroup>() == null)
            {
                Debug.LogError("对象没有LodGroup 组件，无法缓存材质");
                return prefabMaterialCache;
            }

            int count = 0;
            LOD[] lods = source.GetComponent<LODGroup>().GetLODs();
            //当allLodLevel = False 只获取LOD0 材质
            for (int i = 0; i < lods.Length && (allLodLevel || i == 0); i++)
            {
                Dictionary<GameObject, RendererMaterialManager> lodLayerMaterialCache =
                    new Dictionary<GameObject, RendererMaterialManager>();
                foreach (var renderer in lods[i].renderers)
                {
                    lodLayerMaterialCache[renderer.gameObject] = new RendererMaterialManager(renderer.gameObject);
                    count++;
                }

                prefabMaterialCache.Add((lodLayerMaterialCache));
            }

            Debug.Log("缓存了材质数量:" + count);
            return prefabMaterialCache;
        }

        #endregion
    }
}