using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.PrefabCreator.Module;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ModulesPreset = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Object>>;
using Object = UnityEngine.Object;

namespace Editor.PrefabCreator
{
    public static class PrefabCreatorUtils
    {
        /// <summary>
        /// 聚焦选中的Scene视图中的Prefab。
        /// </summary>
        public static void FocusSelect()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected(); // 在Scene视图中聚焦选中的Prefab
            }
        }

        /// <summary>
        /// 计算预览位置。
        /// </summary>
        /// <returns>预览位置的Vector3坐标。</returns>
        public static Vector3 CalcPreviewPosition()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;

            var transform = sceneView.camera.transform;
            Ray ray = new Ray(transform.position, transform.forward);

            float distance = 80f;

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 80f))
            {
                distance = Vector3.Distance(sceneView.camera.transform.position, hit.point);
            }

            return sceneView.camera.transform.position + transform.forward * distance;
        }

        /// <summary>
        /// 获取对象的偏移量。
        /// </summary>
        /// <param name="gameObject">要计算偏移量的游戏对象。</param>
        /// <returns>偏移量的Vector3值。</returns>
        public static Vector3 GetOffset(GameObject gameObject)
        {
            // 获取Renderer组件，可能需要考虑所有子物体的Renderer
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                // 计算所有子物体的包围盒
                Bounds combinedBounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }

                // 假设我们想要沿着x轴偏移包围盒的宽度
                Vector3 offset = new Vector3(combinedBounds.size.x, 0, 0) * 1.2f;
                return offset;
            }

            return Vector3.forward * 5;
        }

        /// <summary>
        /// 检查FBX文件是否符合要求。
        /// </summary>
        /// <param name="fbx">待检查的FBX游戏对象。</param>
        /// <returns>如果FBX符合要求返回true，否则返回false。</returns>
        public static bool CheckFBX(GameObject fbx)
        {
            if (fbx == null)
            {
                return false;
            }

            // 找到该 FBX 对应Model目录的父目录，以此找到对应的Prefab, Material, Texture目录。
            string fbxPath = AssetDatabase.GetAssetPath(fbx);

            // 检查fbx 的路径是否正确，如果不在 Model 路径内，则不予以操作，因为没法正确根据路径找到Prefab 的目录
            if (!fbxPath.Contains("/Model"))
            {
                EditorUtility.DisplayDialog("Error", "FBX 存放的位置不正确！没法创建Prefab", "OK");
                return false;
            }

            // 如果不是 SM_ 开头的fbx，那也不能操作
            if (!fbxPath.Contains("/SM_"))
            {
                EditorUtility.DisplayDialog("Error", "操作对象并非 SM_开头的 FBX文件！操作失败", "OK");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查Prefab是否符合要求。
        /// </summary>
        /// <param name="prefab">待检查的Prefab游戏对象。</param>
        /// <param name="prefabPath">Prefab的路径。</param>
        /// <returns>如果Prefab符合要求返回true，否则返回false。</returns>
        public static bool CheckPrefab(GameObject prefab, string prefabPath)
        {
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "场景中不存在Prefab对象，请先创建Prefab后，再保存！", "OK");
                return false;
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog("Error", "Prefab对象没有有效的路径！  路径为:" + prefabPath, "OK");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查Prefab和FBX是否匹配。
        /// </summary>
        /// <param name="prefab">待检查的Prefab游戏对象。</param>
        /// <param name="fbx">待检查的FBX游戏对象。</param>
        /// <returns>如果Prefab和FBX匹配返回true，否则返回false。</returns>
        public static bool CheckPrefabFBX(GameObject prefab, GameObject fbx)
        {
            HashSet<string> MeshPaths = new HashSet<string>();
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "输入Prefab为空", "OK");
                return false;
            }

            // 获取Prefab中所有的MeshFilter或SkinnedMeshRenderer组件
            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
            var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();

            // 获取所有的Mesh
            var allMeshes = meshFilters.Select(mf => mf.sharedMesh)
                .Concat(skinnedMeshRenderers.Select(smr => smr.sharedMesh))
                .Distinct()
                .Where(mesh => mesh != null)
                .ToList();

            // 收集网格
            foreach (var mesh in allMeshes)
            {
                var meshName = mesh.name.ToLower();
                if (!(meshName.Contains("impost") || meshName.Contains("billboard")))
                {
                    MeshPaths.Add(AssetDatabase.GetAssetPath(mesh));
                }
            }

            switch (MeshPaths.Count)
            {
                case 0:
                    EditorUtility.DisplayDialog("Error", "无法获取Prefab的网格，请检查对象是否具有网格", "OK");
                    return false;
                case 1:
                    if (MeshPaths.Contains(AssetDatabase.GetAssetPath(fbx))) return true;
                    EditorUtility.DisplayDialog("Error", "Prefab的FBX与输入的FBX不同", "OK");
                    return false;
                default:
                    EditorUtility.DisplayDialog("Error", "不支持处理一个Prefab中拥有多个FBX的对象", "OK");
                    return false;
            }
        }

        /// <summary>
        /// 根据LOD级别获取对应的颜色。
        /// </summary>
        /// <param name="lodLevel">LOD级别。</param>
        /// <returns>对应LOD级别的颜色。</returns>
        public static Color GetLodLevelColor(int lodLevel)
        {
            // 将RGB值从0-255范围转换为0-1范围
            float Convert(int value) => value / 255f;

            switch (lodLevel)
            {
                case 0: return new Color(Convert(60), Convert(70), Convert(27), 1f); // LOD 0颜色
                case 1: return new Color(Convert(45), Convert(55), Convert(67), 1f); // LOD 1颜色
                case 2: return new Color(Convert(40), Convert(64), Convert(73), 1f); // LOD 2颜色
                case 3: return new Color(Convert(64), Convert(37), Convert(27), 1f); // LOD 3颜色
                case 4: return new Color(Convert(77), Convert(58), Convert(106), 1f); // LOD 4颜色
                case 5: return new Color(Convert(83), Convert(57), Convert(25), 1f); // LOD 5颜色
                case 6: return new Color(Convert(90), Convert(82), Convert(10), 1f); // LOD 6颜色
                case -1: return new Color(Convert(80), Convert(96), Convert(143), 1f); // Billboard Color
                case -2: return new Color(Convert(95), Convert(80), Convert(120), 1f); // Imposter Color
                case 99: return new Color(Convert(81), Convert(20), Convert(20), 1f); // CULL颜色
                default: return new Color(0.8f, 0.8f, 0.8f, 1f); // 默认颜色
            }
        }

        /// <summary>
        /// 选择目标游戏对象中的特定Mesh。
        /// </summary>
        /// <param name="targetGameObject">目标游戏对象。</param>
        /// <param name="meshName">要选择的Mesh的名称。</param>
        public static void selectTargetMesh(GameObject targetGameObject, string meshName)
        {
            Transform[] childTransforms = targetGameObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in childTransforms)
            {
                // 如果子对象的名称匹配
                if (t.name == meshName)
                {
                    Selection.activeGameObject = t.gameObject;
                    EditorGUIUtility.PingObject(t.gameObject);
                    break; // 找到匹配项后退出循环
                }
            }
        }

        /// <summary>
        /// 获取多个Mesh的三角形总数。
        /// </summary>
        /// <param name="meshes">包含多个Mesh的数组。</param>
        /// <returns>所有Mesh的三角形总数。</returns>
        public static int GetMeshesTrianglesCount(List<Mesh> meshes)
        {
            int totalTriangles = 0;
            foreach (Mesh mesh in meshes)
            {
                if (mesh != null)
                {
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        totalTriangles += mesh.GetTriangles(i).Length / 3;
                    }
                }
            }

            return totalTriangles;
        }

        /// <summary>
        /// 查找使用指定Mesh的渲染器。
        /// </summary>
        /// <param name="prefab">要查找的Prefab。</param>
        /// <param name="mesh">指定的Mesh。</param>
        /// <returns>找到的第一个使用指定Mesh的渲染器，如果没有找到则返回null。</returns>
        public static Renderer FindRendererForMesh(GameObject prefab, Mesh mesh)
        {
            // 首先检查所有的MeshFilter组件
            foreach (var meshFilter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh == mesh)
                {
                    return meshFilter.GetComponent<Renderer>();
                }
            }

            // 如果没有找到，再检查所有的SkinnedMeshRenderer组件
            foreach (var skinnedMeshRenderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinnedMeshRenderer.sharedMesh == mesh)
                {
                    return skinnedMeshRenderer;
                }
            }

            // 如果还是没有找到，返回null
            return null;
        }

        //根据LodGroup复制LOD0材质到后续LOD
        public static void CopyLod0MaterialByLodGroup(GameObject prefab)
        {
            LODGroup lodGroup = prefab.GetComponent<LODGroup>();

            // 确保LODGroup组件存在
            if (lodGroup == null)
            {
                EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent("对象不存在Lod"), 1d);
                return;
            }

            // 获取所有的LOD
            LOD[] lods = lodGroup.GetLODs();

            // 确保至少有两个LOD层级
            if (lods.Length < 2)
            {
                EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent("至少需要两个LOD层级"), 1d);
                return;
            }

            var lod0Renderers = lods[0].renderers;
            for (int lodIndex = 1; lodIndex < lods.Length; lodIndex++)
            {
                // 获取当前LOD的所有渲染器
                var lodRenderers = lods[lodIndex].renderers;
                for (int rendererIndex = 0; rendererIndex < lodRenderers.Length; rendererIndex++)
                {
                    // 获取对应的LOD0渲染器的材质，如果当前LOD层级的渲染器数量超过LOD0，则使用LOD0的最后一个渲染器的材质
                    int lod0Index = Mathf.Min(rendererIndex, lod0Renderers.Length - 1);

                    RendererMaterialManager meshMaterialManager =
                        new RendererMaterialManager(lod0Renderers[lod0Index].gameObject);
                    RendererMaterialManager.ApplyToRenderer(lodRenderers[rendererIndex].gameObject,
                        meshMaterialManager);
                }
            }

            EditorSceneManager.MarkSceneDirty(lodGroup.gameObject.scene);
            EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent("材质复制成功"), 1d);
        }

        public static GameObject FindClosestPrefab(List<GameObject> prefabs, int currentIndex)
        {
            if (prefabs.Count == 0) return null;
            if (currentIndex == 0) return prefabs.Count > 1 ? prefabs[1] : null;
            return prefabs[currentIndex - 1];
        }
    }

    //解析名称
    public static class ParsingObject
    {
        // 检查GameObject的父级是否名为"Collider"
        public static bool IsColliderParent(GameObject obj)
        {
            // 从当前对象开始向上遍历父级
            var current = obj.transform;
            while (current != null)
            {
                // 检查当前父级的名字是否为"Collider"（不区分大小写）
                if (string.Equals(current.name, "Collider", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // 向上移动到下一个父级
                current = current.parent;
            }

            // 如果所有父级都不是名为"Collider"的，则返回false
            return false;
        }

        public static bool IsBillboard(GameObject obj)
        {
            return obj.name.ToLower().Contains("billboard");
        }

        public static bool IsImposter(GameObject obj)
        {
            return obj.name.ToLower().Contains("impost");
        }

        public static int GetLodLevelFromName(string name)
        {
            // 从名称中提取LOD层级数字
            int lodIndex = name.IndexOf("_LOD", StringComparison.OrdinalIgnoreCase);
            if (lodIndex >= 0)
            {
                // 如果找到_LOD，提取其后的数字作为LOD层级
                lodIndex += 4; // 跳过_LOD字符
                string lodNumberStr = new string(name.Skip(lodIndex).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(lodNumberStr, out int lodLevel))
                {
                    return lodLevel;
                }
            }

            // 如果没有_LOD标识或者无法解析数字，则认为是LOD0
            return 0;
        }

#if XRENDER
        public static ImpostorManager.imposterShaderType GetTargetShaderType(List<GameObject> lod0)
        {
            // 定义检测关键词
            string[] foliageKeywords = new string[] { "foliage", "fern", "grass", "leaf", "vine", "tree" };
            string[] buildingKeyword = new string[] { "layeredrock" };

            foreach (var gameObject in lod0)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null && material.shader != null)
                        {
                            string shaderName = material.shader.name.ToLowerInvariant();
                            foreach (var keyword in foliageKeywords)
                            {
                                if (shaderName.Contains(keyword))
                                {
                                    return ImpostorManager.imposterShaderType.Foliage;
                                }
                            }
                            foreach (var keyword in buildingKeyword)
                            {
                                if (shaderName.Contains(keyword))
                                {
                                    return ImpostorManager.imposterShaderType.Building;
                                }
                            }
                        }
                    }
                }
            }

            // 如果没有匹配到特定关键词，则返回Default
            return ImpostorManager.imposterShaderType.Default;
        }
#endif
    }

    //Preset管理
    public class ModulesPresetManager
    {
        public Dictionary<string, ModulesPreset> presets;

        public static string SavePath => "Packages/com.funplus.worldx.prefab-creator/Editor/PrefabCreator/ModulesPreset.json";

        public ModulesPresetManager()
        {
            presets = new Dictionary<string, ModulesPreset>();
            LoadFromFile();
        }

        private void SaveToFile()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
                File.WriteAllText(SavePath, json);
                Debug.Log("Preset保存成功");
            }
            catch (Exception ex)
            {
                Debug.LogError("Error saving ModulesPresetManager: " + ex.Message);
            }
        }

        private void LoadFromFile()
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                Debug.Log("LOAD JSON ____"+json);
                JsonConvert.PopulateObject(json, this, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("预设文件读取失败: " + ex.Message);
            }
        }

        public void UpdatePreset(string presetName, ModulesPreset preset)
        {
            presets[presetName] = preset;
            SaveToFile();
        }

        public void RemovePreset(string presetName)
        {
            presets.Remove(presetName);
            SaveToFile();
        }

        public List<string> GetPresetList()
        {
            if (presets == null)
            {
                Debug.LogWarning("presets初始化失败！！！");
                return new List<string>();
            }

            return presets.Keys.ToList();
        }

        public ModulesPreset GetPreset(string presetName)
        {
            return presets[presetName];
        }

        public ModulesPreset GetPreset(int index)
        {
            if (index >= GetPresetList().Count)
            {
                Debug.LogWarning($"index={index}  超出范围！！！");
                return null;
            }

            return presets[GetPresetList()[index]];
        }
    }

    //弹出的输入Preset命名的窗口
    public class PresetNamePopup : EditorWindow
    {
        private string presetName = "";
        private PrefabCreator pfcTemp;

        public static void OpenPresetNamePopup(PrefabCreator pfcTemp)
        {
            PresetNamePopup window = CreateInstance<PresetNamePopup>();
            window.pfcTemp = pfcTemp;
            window.position = new Rect(Screen.width / 2.0f, Screen.height / 2.0f, 250, 145);
            window.titleContent = new GUIContent("Preset Name");
            window.presetName = pfcTemp.GetCurrentPresetName();
            window.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("输入预设名称", EditorStyles.wordWrappedLabel);
            GUILayout.Space(50);

            presetName = EditorGUILayout.TextField("名称:", presetName);
            GUILayout.Space(5);

            if (presetName.Length == 0)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("保存预设"))
            {
                if (pfcTemp.presetNames.Contains(presetName))
                {
                    bool overwrite = EditorUtility.DisplayDialog(
                        "覆盖预设",
                        $"预设 '{presetName}' 已经存在。你想要覆盖它吗？",
                        "覆盖",
                        "取消"
                    );

                    if (overwrite)
                    {
                        if (pfcTemp.SavePreset(presetName))
                        {
                            GetWindow<SceneView>().ShowNotification(new GUIContent("预设保存成功"), 0.5d);
                            Close();
                        }
                        else
                        {
                            GetWindow<SceneView>().ShowNotification(new GUIContent("预设保存失败"), 0.5d);
                        }
                    }
                }
                else
                {
                    if (pfcTemp.SavePreset(presetName))
                    {
                        GetWindow<SceneView>().ShowNotification(new GUIContent("预设保存成功"), 0.5d);
                        Close();
                    }
                }
            }

            GUI.enabled = true;

            if (GUILayout.Button("取消"))
            {
                Close();
            }
        }
    }

    //材质拷贝工具
    public class PrefabMaterialCopyTools : EditorWindow
    {
        private readonly List<List<RendererMaterialManager>> materialCache = new List<List<RendererMaterialManager>>();

        [MenuItem("美术/预制体工具/打开材质工具窗口")]
        public static void OpenWindow()
        {
            PrefabMaterialCopyTools window = GetWindow<PrefabMaterialCopyTools>();
            window.titleContent = new GUIContent("Prefab材质工具");
            window.Show();
        }

        private void OnGUI()
        {
            // 设置整体布局的边距和间距
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(10);

            // 设置按钮样式
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.fontStyle = FontStyle.Bold;

            // 拷贝材质按钮
            if (GUILayout.Button("拷贝材质", buttonStyle, GUILayout.Height(40)))
            {
                CopyMaterial();
            }

            GUILayout.Space(10);

            // 粘贴材质并保存预制体按钮
            if (GUILayout.Button("粘贴材质并保存预制体", buttonStyle, GUILayout.Height(40)))
            {
                PasteMaterial();
                SavePrefab();
            }

            GUILayout.Space(10);

            // 复制LOD0的材质到后续层级按钮
            if (GUILayout.Button("复制LOD0的材质到后续层级", buttonStyle, GUILayout.Height(40)))
            {
                GameObject[] selectedObjects = Selection.gameObjects;
                if (selectedObjects.Length < 1)
                {
                    GetWindow<SceneView>().ShowNotification(new GUIContent("请于场景中选中Prefab对象"), 0.5d);
                }
                else
                {
                    GameObject prefab = selectedObjects[0];
                    PrefabCreatorUtils.CopyLod0MaterialByLodGroup(prefab);
                    PrefabUtility.ApplyPrefabInstance(prefab, InteractionMode.UserAction);
                }
            }

            GUILayout.Space(10);
            GUILayout.EndVertical();
        }

        private void CopyMaterial()
        {
            materialCache.Clear();
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length < 1)
            {
                GetWindow<SceneView>().ShowNotification(new GUIContent("请于场景中选中Prefab对象"), 0.5d);
            }

            GameObject prefab = selectedObjects[0];
            // 检查Prefab是否为null
            if (prefab == null)
            {
                Debug.LogError("Prefab为空，没法执行保存材质的操作");
                return;
            }


            var tempPrefabInfo = new PrefabCreator.PrefabInfo(prefab);
            var lodObjectsList = tempPrefabInfo.lodObjects;


            int meshCount = 0;
            // 遍历lodList中的每个LOD层级
            foreach (List<GameObject> lodObjects in lodObjectsList)
            {
                List<RendererMaterialManager> lodMaterials = new List<RendererMaterialManager>();
                // 遍历当前LOD层级的所有GameObject对象
                foreach (GameObject go in lodObjects)
                {
                    // 找到对应的Renderer组件
                    Renderer renderer = go.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        RendererMaterialManager meshMaterialManager =
                            new RendererMaterialManager(renderer.gameObject);
                        // Debug.Log("缓存————————" + renderer.gameObject);
                        lodMaterials.Add(meshMaterialManager);
                        meshCount++;
                    }
                }

                materialCache.Add(lodMaterials);
            }

            GetWindow<SceneView>().ShowNotification(new GUIContent("拷贝了" + meshCount + "个材质信息"), 0.5d);
        }

        private void PasteMaterial()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects.Length < 1)
            {
                GetWindow<SceneView>().ShowNotification(new GUIContent("请于场景中选中Prefab对象"), 0.5d);
                return;
            }

            GameObject prefab = selectedObjects[0];
            // 检查Prefab是否为null
            if (prefab == null)
            {
                Debug.LogError("Prefab为空，没法执行粘贴材质的操作");
                return;
            }

            var tempPrefabInfo = new PrefabCreator.PrefabInfo(prefab);
            var lodObjectsList = tempPrefabInfo.lodObjects;

            if (materialCache.Count == 0)
            {
                GetWindow<SceneView>().ShowNotification(new GUIContent("请先拷贝材质再粘贴"), 0.5d);
                return;
            }

            if (materialCache[0].Count == 0)
            {
                GetWindow<SceneView>().ShowNotification(new GUIContent("没有材质信息，请尝试重新拷贝"), 0.5d);
                return;
            }

            for (int i = 0; i < lodObjectsList.Count; i++)
            {
                List<GameObject> lodObjects = lodObjectsList[i];
                List<RendererMaterialManager> lodMaterials = i < materialCache.Count
                    ? materialCache[i]
                    : materialCache[materialCache.Count - 1];

                for (int j = 0; j < lodObjects.Count; j++)
                {
                    GameObject go = lodObjects[j];
                    Renderer renderer = go.GetComponent<Renderer>();
                    RendererMaterialManager meshMaterialManager;
                    if (j < lodMaterials.Count)
                    {
                        meshMaterialManager = lodMaterials[j];
                    }
                    else
                    {
                        // 如果没有足够的材质信息，则复用最后一个材质
                        meshMaterialManager = lodMaterials[lodMaterials.Count - 1];
                        Debug.LogWarning($"LOD {i} 的 Renderer {j} 使用了复用的材质信息。");
                    }

                    // 应用材质
                    RendererMaterialManager.ApplyToRenderer(renderer.gameObject, meshMaterialManager);
                }
            }

            PrefabUtility.ApplyPrefabInstance(prefab, InteractionMode.UserAction);
            GetWindow<SceneView>().ShowNotification(new GUIContent("还原、保存完成"), 0.5d);
        }

        private void SavePrefab()
        {
            if (PrefabUtility.GetPrefabInstanceStatus(Selection.activeGameObject) != PrefabInstanceStatus.Connected)
            {
                GetWindow<SceneView>().ShowNotification(new GUIContent("对象不是个prefab对象，无法保存"), 0.5d);
                return;
            }

            GameObject selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject != null)
            {
                // 获取Prefab实例的根对象
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(selectedGameObject);
                if (root != null)
                {
                    // 获取Prefab资产
                    Object prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(root);


                    // 记录当前的Transform状态 保证保存的Prefab会冻结变换
                    Vector3 position = selectedGameObject.transform.position;
                    Quaternion rotation = selectedGameObject.transform.rotation;
                    Vector3 scale = selectedGameObject.transform.localScale;

                    // 将复制的prefab坐标归零
                    selectedGameObject.transform.position = Vector3.zero;
                    selectedGameObject.transform.rotation = Quaternion.identity;
                    selectedGameObject.transform.localScale = Vector3.one;

                    // 应用所有修改到Prefab
                    PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction);

                    // 输出结果
                    Debug.Log(prefabAsset.name + " prefab has been updated with the changes.");

                    selectedGameObject.transform.position = position;
                    selectedGameObject.transform.rotation = rotation;
                    selectedGameObject.transform.localScale = scale;
                }
                else
                {
                    Debug.LogError("The selected GameObject is not a Prefab instance.");
                }
            }
            else
            {
                GetWindow<SceneView>().ShowNotification(new GUIContent("请于场景中选中Prefab对象"), 0.5d);
            }
        }
    }
}