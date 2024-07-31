using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.ImpostorGeneration;
using Editor.LODGeneration;
using Editor.Scripts;
using UnityEditor;
using UnityEngine;
using ModulePreset = System.Collections.Generic.Dictionary<string, System.Object>;
using PrefabMaterialCache =
    System.Collections.Generic.List<
        System.Collections.Generic.List<Editor.PrefabCreator.Module.RendererMaterialManager>>;

#if TA_TOOLS
using Editor.MaterialTools;
#endif

namespace Editor.PrefabCreator.Module
{
    #region struct

    [Serializable]
    public struct LODGenParameter
    {
        public float maskWeight;
        public float normalWeight;
        public float uvWeight;
        public float edgeWeight;
        public int LODCount;
        public float factor;

        public static LODGenParameter CreateDefault()
        {
            return new LODGenParameter
            {
                maskWeight = 0,
                normalWeight = 0,
                uvWeight = 0,
                edgeWeight = 0,
                LODCount = 4,
                factor = 1
            };
        }
    }

    #endregion

    //本体
    public class LODModel : Module
    {
        #region RuntimeData

        private Dictionary<GameObject, PrefabMaterialCache> prefabsMaterialsCacheDictionary;
        private PrefabCreator pfcTemp;
        private NodeInfo[] fbxNodeInfos;


        //设置材质用的临时参数
        private Renderer setMaterialToRenderer;
        private int setMaterialToIndex;

        #endregion

        #region LodGen Data

        private readonly Dictionary<int, bool> lodFoldouts = new Dictionary<int, bool>();

        private StaticMeshLODManager staticMeshLODWindow;

        public LODGenParameter lodPara = LODGenParameter.CreateDefault();

#if XRENDER
        private ImpostorManager impostorWindow;
#endif

        #endregion

        //Override -------------------------------------------------------------------------------------------------
        public LODModel(string name, bool isExpanded, bool canDisabled = false) : base(name, isExpanded, canDisabled)
        {
        }

        public override void Init(PrefabCreator pfc)
        {
            if (staticMeshLODWindow != null)
            {
                staticMeshLODWindow.Close();
            }

            //缓存Fbx相关的数据
            if (pfc.fbx != null)
            {
                fbxNodeInfos = FBXNode.AnalyzeFbx(AssetDatabase.GetAssetPath(pfc.fbx));
            }

#if TA_TOOLS
            //监听创建材质的事件
            CreateMaterialVariantMenu.OnMaterialCreated += HandleMaterialCreated;
#endif
        }

        public override void Dispose()
        {
            if (staticMeshLODWindow != null)
                staticMeshLODWindow.Close();
#if TA_TOOLS
            CreateMaterialVariantMenu.OnMaterialCreated -= HandleMaterialCreated;
#endif
        }

        // GUI 
        public override void DrawModuleGUI(PrefabCreator pfc)
        {
            List<List<GameObject>> lodsObjectList = pfc.GetCurrentPrefabInfo().lodObjects;
            List<List<Mesh>> lodsMeshList = pfc.GetCurrentPrefabInfo().GetLodMeshList();

            if (lodsObjectList == null)
            {
                EditorGUILayout.HelpBox("Prefab或者Lod的Mesh不存在", MessageType.Info);
                return;
            }

            // 定义标签宽度
            const float labelWidth = 70f;

            GameObject prefab = pfc.currentPrefab;
            LODGroup lodGroup = null;
            LOD[] lods = null;
            if (prefab != null)
            {
                lodGroup = prefab.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    lods = lodGroup.GetLODs();
                }
            }

            bool hasLodGroup = lodGroup != null;
            bool hasPrefab = prefab != null;

            int lod0Triangles = lodsMeshList.FirstOrDefault()?.Sum(mesh => mesh?.triangles.Length / 3 ?? 0) ?? 0;


            // 遍历每个LOD Mesh层级
            for (int lodLevel = 0; lodLevel < lodsObjectList.Count; lodLevel++)
            {
                List<GameObject> lodObjects = lodsObjectList[lodLevel];
                List<Mesh> lodMeshes = lodsMeshList[lodLevel];
                int trianglesCount = lodMeshes.Where(mesh => mesh != null).Sum(mesh => mesh.triangles.Length / 3);
                int materialsCount = lodMeshes.Where(mesh => mesh != null).Sum(mesh => mesh.subMeshCount);

                //添加折叠情况的默认值
                lodFoldouts[lodLevel] = lodFoldouts.GetValueOrDefault(lodLevel, false);

                // 绘制LOD层级的框框
                Color backgroundColor = PrefabCreatorUtils.GetLodLevelColor(lodLevel);
                Rect rect = EditorGUILayout.BeginVertical("box");
                EditorGUI.DrawRect(rect, backgroundColor);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"LOD {lodLevel}", EditorStyles.boldLabel, GUILayout.Width(70));

                // 显示当前LOD级别的总面数是LOD0的百分比
                float percentageOfLod0 = lod0Triangles > 0 ? (float)trianglesCount / lod0Triangles * 100f : 0f;
                EditorGUILayout.LabelField($"{percentageOfLod0:0.##}%", EditorStyles.boldLabel, GUILayout.Width(100));

                if (hasLodGroup)
                {
                    GUILayout.FlexibleSpace();
                    float blendInHeight = lodLevel > 0 ? lods[lodLevel - 1].screenRelativeTransitionHeight : 1f;
                    EditorGUILayout.LabelField($"{(blendInHeight * 100):0.##}%", EditorStyles.boldLabel,
                        GUILayout.Width(60f));
                }

                EditorGUILayout.EndHorizontal();


                trianglesCount = PrefabCreatorUtils.GetMeshesTrianglesCount(lodMeshes);
                lodFoldouts[lodLevel] = EditorGUILayout.Foldout(lodFoldouts[lodLevel],
                    $"      {lodMeshes.Count} Meshes            {trianglesCount} Tris           {materialsCount} Mats",
                    true);

                // 如果当前LOD层级是展开的，绘制其内容
                if (lodFoldouts[lodLevel])
                {
                    // 遍历当前LOD层级的所有GameObject对象
                    for (int i = 0; i < lodObjects.Count; i++)
                    {
                        var lodObject = lodObjects[i];
                        var mesh = lodMeshes[i];

                        if (mesh == null)
                        {
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();

                        // 使用自定义的GUIStyle和固定宽度来展示Mesh名称
                        EditorGUILayout.LabelField("Mesh 名称:", customLabelStyle, GUILayout.Width(labelWidth));

                        // 判断pfc.prefab是否为null
                        if (hasPrefab)
                        {
                            if (GUILayout.Button(lodObject.name, clickableTextStyle))
                            {
                                PrefabCreatorUtils.selectTargetMesh(pfc.currentPrefab, mesh.name);
                            }
                        }
                        else
                        {
                            // pfc.prefab为null时，显示普通文本
                            EditorGUILayout.LabelField(lodObject.name, customLabelStyle);
                        }


                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("三角面数量:", customLabelStyle, GUILayout.Width(labelWidth));
                        EditorGUILayout.LabelField((mesh.triangles.Length / 3) + " tris", customLabelStyle,
                            GUILayout.Width(70f));


                        // 显示subMeshCount
                        EditorGUILayout.LabelField("材质数:", customLabelStyle, GUILayout.Width(labelWidth));
                        EditorGUILayout.LabelField(mesh.subMeshCount.ToString(), customLabelStyle,
                            GUILayout.Width(40f));

                        EditorGUILayout.EndHorizontal();

                        // 显示材质
                        EditorGUILayout.BeginVertical();
                        NodeInfo meshNodeInfo = FBXNode.GetMeshNodeInfoFromNodeInfos(mesh, fbxNodeInfos);
                        Renderer renderer = lodObject.GetComponent<Renderer>();
                        Material[] materials = renderer.sharedMaterials;

                        if (meshNodeInfo.MaterialElements.Count != materials.Length)
                        {
                            EditorGUILayout.HelpBox("对象的材质数量与模型的材质数量不匹配", MessageType.Warning);
                        }
                        else
                        {
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);

                            EditorGUI.BeginChangeCheck(); // 开始检测更改

                            for (int j = 0; j < materials.Length; j++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Element " + j, GUILayout.Width(85));

                                materials[j] =
                                    (Material)EditorGUILayout.ObjectField(materials[j], typeof(Material), false);

#if TA_TOOLS
                                if (materials[j] == null ||
                                    AssetDatabase.GetAssetPath(materials[j]).StartsWith("Packages/"))
                                {
                                    if (GUILayout.Button(new GUIContent("快速创建材质", "利用Fbx中的材质名称和保存路径，快速创建材质球")))
                                    {
                                        QuickCreateMaterial(pfc, meshNodeInfo, j, renderer);
                                    }
                                }

#else
                                GUI.enabled = false;
                                if (materials[j] == null ||AssetDatabase.GetAssetPath(materials[j]).StartsWith("Packages/"))
                                {
                                    if (GUILayout.Button(new GUIContent("快速创建材质", "利用Fbx中的材质名称和保存路径，快速创建材质球")))
                                    {}
                                }
                                GUI.enabled = true;

#endif

                                EditorGUILayout.EndHorizontal();
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(renderer, "Change Materials");
                                renderer.sharedMaterials = materials;
                                EditorUtility.SetDirty(renderer);
                            }

                            EditorGUILayout.EndVertical();
                        }

                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                    }
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(5); // 在LOD层级之间添加更多间隔
            }

            // 绘制Billboard
            DrawFoldLayout(-1, "Billboard", lods, pfc.GetCurrentPrefabInfo().billboardObjects, hasLodGroup, pfc);
            // 绘制Imposter
            DrawFoldLayout(-2, "Imposter", lods, pfc.GetCurrentPrefabInfo().imposterObjects, hasLodGroup, pfc);

            // 遍历每个LOD层级结束后绘制Cull
            if (hasLodGroup)
            {
                // 检查是否有Culled状态
                float cullHeight = lods[lods.Length - 1].screenRelativeTransitionHeight;
                if (cullHeight > 0f) // 如果Cull的screenRelativeTransitionHeight大于0，则表示有Culled状态
                {
                    Color backgroundColor = PrefabCreatorUtils.GetLodLevelColor(99);
                    Rect rect = EditorGUILayout.BeginVertical("box");
                    EditorGUI.DrawRect(rect, backgroundColor);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Culled", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    // 显示Culled的屏占比为百分比形式
                    EditorGUILayout.LabelField($"{(cullHeight * 100):0.##}%", EditorStyles.boldLabel,
                        GUILayout.Width(60f));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5); // 在Culled状态后添加间隔
                }
            }


            // 在LOD列表显示完毕后添加按钮
            //Line1
            if (!hasPrefab)
            {
                EditorGUILayout.HelpBox("请先创建或者加载 Prefab", MessageType.Info);
                return;
            }

            GUILayout.BeginVertical();
            GUI.enabled = (hasPrefab);

            // Generator Box
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Generator", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("打开LOD自动生成流程", GUILayout.Width(175)))
            {
                OpenLodGenWindow(ref pfc);
            }

#if XRENDER
            if (GUILayout.Button("Impostor快速生成", GUILayout.Width(175)))
            {
                OpenImpostorGenWindow(ref pfc);
            }
#else
            GUI.enabled = false;
            if (GUILayout.Button("Impostor快速生成", GUILayout.Width(175)))
            {
            }

            GUI.enabled = true;
#endif

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);


            // Tools Box
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Tools", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("复制LOD0材质到后续LOD", GUILayout.Width(175)))
            {
                if (pfc.currentPrefab == null)
                {
                    EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent("请先点击创建或者加载Prefab后，再进行操作"), 1d);
                }
                else
                {
                    PrefabCreatorUtils.CopyLod0MaterialByLodGroup(pfc.currentPrefab);
                }
            }

            if (GUILayout.Button("拆分拥有多个材质的Mesh对象", GUILayout.Width(175)))
            {
                var Extractor = ScriptableObject.CreateInstance<SubmeshExtractor>();
                Extractor.SubmeshExtract(pfc.prefabs);
                pfc.GetPrefabsInfo();
                pfc.UpdateAllModules();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // 恢复GUI状态
            GUI.enabled = true;
            // 结束整体水平布局
            GUILayout.EndVertical();
        }


        //Preset
        public override ModulePreset SaveToModulePreset()
        {
            return new ModulePreset
            {
                ["lodPara"] = lodPara
            };
        }

        public override void ApplyModulePreset(ModulePreset lodModulePreset, bool loadFormPrefab)
        {
            if (lodModulePreset.ContainsKey("lodPara"))
            {
                lodPara = (LODGenParameter)lodModulePreset["lodPara"];
                if (staticMeshLODWindow != null)
                {
                    staticMeshLODWindow.PreSetParameters(AssetDatabase.GetAssetPath(pfcTemp.fbx), this);
                }
                //Debug.Log("Lod模块加载成功");
            }
        }


        //生成LOD-------------------------------------------------------------------------------------------------------
        //打开LOD生成流程
        private void OpenLodGenWindow(ref PrefabCreator pfc)
        {
            pfcTemp = pfc;
            SaveLodMaterialsToCache(pfc);
            staticMeshLODWindow = EditorWindow.GetWindow<StaticMeshLODManager>();
            staticMeshLODWindow.PreSetParameters(AssetDatabase.GetAssetPath(pfc.fbx), this);
            staticMeshLODWindow.Show();
        }

        //保存材质
        private void SaveLodMaterialsToCache(PrefabCreator pfc, bool saveAllLods = false)
        {
            prefabsMaterialsCacheDictionary = new Dictionary<GameObject, PrefabMaterialCache>();
            List<GameObject> prefabs = pfc.prefabs;
            int meshCount = 0;

            // 遍历所有Prefab
            foreach (var prefab in prefabs)
            {
                // Debug.Log("缓存——————", prefab);
                var prefabMaterialsCache = new PrefabMaterialCache();
                // 检查Prefab是否为null
                if (prefab == null)
                {
                    Debug.LogError("Prefab为空，没法执行保存材质的操作");
                    return;
                }

                List<List<Mesh>> lodList = pfc.prefabInfos[prefab].GetLodMeshList();
                if (!saveAllLods && lodList.Count > 0)
                {
                    lodList = new List<List<Mesh>> { lodList[0] };
                }

                // 遍历LOD列表
                foreach (List<Mesh> lodMeshes in lodList)
                {
                    List<RendererMaterialManager> lodMaterials = new List<RendererMaterialManager>();

                    // 遍历当前LOD层级的所有Mesh对象
                    foreach (Mesh mesh in lodMeshes)
                    {
                        // 找到对应的Renderer组件
                        Renderer renderer = PrefabCreatorUtils.FindRendererForMesh(prefab, mesh);
                        if (renderer != null)
                        {
                            RendererMaterialManager meshMaterialManager =
                                new RendererMaterialManager(renderer.gameObject);
                            // Debug.Log("缓存————————" + renderer.gameObject);
                            lodMaterials.Add(meshMaterialManager);
                            meshCount += meshMaterialManager.materialCount;
                        }
                    }

                    prefabMaterialsCache.Add(lodMaterials);
                }

                prefabsMaterialsCacheDictionary[prefab] = prefabMaterialsCache;
            }

            Debug.Log("缓存了 " + meshCount + " 个材质信息");
        }

        //LOD生成完毕后调用
        public void AfterLODGenerated(List<float> ratioList, List<float> transitionWidthList, List<int> subMeshSetting)
        {
            pfcTemp.GetPrefabsInfo();
            //还原LOD级别
            SetupLodGroup(pfcTemp);
            //设置过渡距离
            SetPrefabLodTransition(pfcTemp, transitionWidthList, ratioList);
            //还原Billboard Imposter
            RevertBillboardImposter(pfcTemp);
            //还原材质
            RendererMaterialManager.ApplyToPrefabs(pfcTemp.prefabs, prefabsMaterialsCacheDictionary);
            //更新模型信息
            //缓存Fbx相关的数据
            if (pfcTemp.fbx != null)
            {
                fbxNodeInfos = FBXNode.AnalyzeFbx(AssetDatabase.GetAssetPath(pfcTemp.fbx));
            }

            pfcTemp.UpdateAllModules();
        }

        //设置lods
        private static void SetupLodGroup(PrefabCreator pfc)
        {
            //设置LOD
            foreach (var prefab in pfc.prefabs)
            {
                LODGroup lodGroup = prefab.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    lodGroup = prefab.AddComponent<LODGroup>();
                }

                // 获取PrefabInfo
                PrefabCreator.PrefabInfo prefabInfo = pfc.prefabInfos[prefab];
                List<List<GameObject>> lodsList = prefabInfo.lodObjects;

                LOD[] newLods = new LOD[lodsList.Count];

                for (int i = 0; i < lodsList.Count; i++)
                {
                    List<Renderer> renderers = new List<Renderer>();
                    foreach (var lodObject in lodsList[i])
                    {
                        var renderer = lodObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderers.Add(renderer);
                        }
                    }

                    float screenRelativeTransitionHeight = 0.5f - (i * 0.1f); //临时值
                    newLods[i] = new LOD(screenRelativeTransitionHeight, renderers.ToArray());
                    Debug.Log(
                        $"LOD {i} set for prefab: {prefab.name} with transition height: {screenRelativeTransitionHeight} and {renderers.Count} renderers.");
                }

                lodGroup.SetLODs(newLods);
                lodGroup.RecalculateBounds();
                Debug.Log($"LODGroup for prefab: {prefab.name} has been set with {newLods.Length} LODs.");
            }
        }

        //设置LOD过渡
        private static void SetPrefabLodTransition(PrefabCreator pfc,
            List<float> transitionWidthList,
            IReadOnlyList<float> RatioList)
        {
            foreach (var prefab in pfc.prefabs)
            {
                LODGroup lodGroup = prefab.GetComponent<LODGroup>();
                if (lodGroup == null)
                {
                    Debug.LogError("未找到LodGroup无法设置过渡");
                    return;
                }

                LOD[] lods = lodGroup.GetLODs();

                if (prefab != null)
                {
                    if (lodGroup != null)
                    {
                        // 确保ratioList的数量与LODs的数量匹配（最后一个是Culled距离）
                        if (RatioList.Count == lods.Length)
                        {
                            for (int i = 0; i < lods.Length; i++)
                            {
                                lods[i].screenRelativeTransitionHeight = RatioList[i];
                                if (i <= transitionWidthList.Count - 1)
                                {
                                    lods[i].fadeTransitionWidth = transitionWidthList[i];
                                }
                                else
                                {
                                    lods[i].fadeTransitionWidth = 0;
                                }
                            }

                            // 设置Culled距离
                            lodGroup.SetLODs(lods);
                            lodGroup.fadeMode = LODFadeMode.CrossFade;
                            lodGroup.RecalculateBounds();
                            Debug.Log("LOD过渡距离设置成功");
                        }
                        else
                        {
                            Debug.LogError(
                                $"LOD过渡层级的数量与提供的屏幕比率列表不匹配。lod数量为{lods.Length},给定比率数量为{RatioList.Count}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"{prefab.name} 上没有找到 LODGroup 组件。");
                    }
                }
                else
                {
                    Debug.LogError("Prefab 为 null，无法设置 LOD 过渡层级。");
                }
            }
        }

        //还原Billboard Imposter
        private void RevertBillboardImposter(PrefabCreator pfc)
        {
            foreach (var prefab in pfc.prefabs)
            {
                PrefabCreator.PrefabInfo prefabInfo = pfc.prefabInfos[prefab];
                List<GameObject> imposterObjects = prefabInfo.imposterObjects;
                List<GameObject> billboardObjects = prefabInfo.billboardObjects;
                if (imposterObjects.Count > 0)
                {
                    InsertToLod(imposterObjects, prefab);
                }

                if (billboardObjects.Count > 0)
                {
                    InsertToLod(billboardObjects, prefab);
                }
            }
        }

        private void InsertToLod(List<GameObject> targetObjects, GameObject prefab)
        {
            // 获取或添加LODGroup组件
            LODGroup lodGroup = prefab.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                lodGroup = prefab.AddComponent<LODGroup>();
            }

            LOD[] lods = lodGroup.GetLODs();
            int m_insertIndex = lods.Length - 1;
            Array.Resize(ref lods, lods.Length + 1);
            for (int i = lods.Length - 1; i > m_insertIndex; i--)
            {
                lods[i].screenRelativeTransitionHeight = lods[i - 1].screenRelativeTransitionHeight;
                lods[i].fadeTransitionWidth = lods[i - 1].fadeTransitionWidth;
                lods[i].renderers = lods[i - 1].renderers;
            }

            float firstTransition = 1;
            if (m_insertIndex > 0)
                firstTransition = lods[m_insertIndex - 1].screenRelativeTransitionHeight;


            // 如果有Imposter对象，添加为新的LOD级别

            foreach (var targetObject in targetObjects)
            {
                lods[m_insertIndex + 1].renderers = targetObject.GetComponents<Renderer>();
            }


            lods[m_insertIndex].screenRelativeTransitionHeight =
                (lods[m_insertIndex + 1].screenRelativeTransitionHeight + firstTransition) * 0.5f;

            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
        }

#if XRENDER
        //生成Imposter--------------------------------------------------------------------------------------------------
        //打开Imposter生成流程
        private void OpenImpostorGenWindow(ref PrefabCreator pfc)
        {
            //SaveAllLodMaterials(pfc);
            pfcTemp = pfc;
            string assetPath = AssetDatabase.GetAssetPath(pfc.fbx);

            if (pfc.GetCurrentPrefabInfo().lodObjects.Count < 1)
            {
                Debug.LogError("请先添加LOD");
                return;
            }


            ImpostorManager.imposterShaderType shaderType =
                ParsingObject.GetTargetShaderType(pfc.GetCurrentPrefabInfo().lodObjects[0]);

            impostorWindow = EditorWindow.GetWindow<ImpostorManager>();
            impostorWindow.PreSetParameters(assetPath, pfc.currentPrefab, this, shaderType);
            impostorWindow.Show();
            //ImpostorManager impostorManager = new ImpostorManager();
            //impostorManager.PreSetParameters(assetPath, pfc.currentPrefab, this, shaderType);
            //impostorManager.GenerateImpostor();


            Debug.Log("开始生成imposter，ShaderType = " + shaderType.ToString());

            //window.Show();
        }

        public void AfterImposterGenerated()
        {
            // refresh window

            pfcTemp.GetPrefabsInfo();
            pfcTemp.UpdateAllModules();
        }
#endif
        //Function--------------------------------------------------------------------------------------------------

#if TA_TOOLS
        private void QuickCreateMaterial(PrefabCreator pfc, NodeInfo meshNodeInfo,
            int index, Renderer renderer)
        {
            // 确定材质保存位置
            string directoryPath =
                Path.GetDirectoryName(pfc.GetCurrentPrefabInfo().savePath);
            string parentDirectoryPath = Path.GetDirectoryName(directoryPath);
            string materialFolderPath = Path.Combine(parentDirectoryPath, "Material");
            bool saveMaterial = true;
            // 检查Material文件夹是否存在
            if (!Directory.Exists(materialFolderPath))
            {
                bool createDirectory = EditorUtility.DisplayDialog(
                    "目录不存在",
                    $"材质目录不存在: {materialFolderPath}\n是否要创建该目录？",
                    "创建",
                    "取消"
                );

                if (createDirectory)
                {
                    Directory.CreateDirectory(materialFolderPath);
                }
                else
                {
                    saveMaterial = false;
                }
            }

            if (saveMaterial)
            {
                string materialSavePath =
                    Path.Combine(materialFolderPath, meshNodeInfo.GetMaterialName(index) + ".mat");

                setMaterialToRenderer = renderer;
                setMaterialToIndex = index;
                // 传递参数打开窗口
                CreateMaterialVariantMenu window =
                    (CreateMaterialVariantMenu)EditorWindow.GetWindow(
                        typeof(CreateMaterialVariantMenu));
                window.materialSavePath = materialSavePath;
                window.Show();
            }
        }

#endif

        //材质创建窗口创建材质后的调用
        private void HandleMaterialCreated(Material newMaterial)
        {
            if (setMaterialToRenderer != null && setMaterialToIndex != -1)
            {
                Material[] materials = setMaterialToRenderer.sharedMaterials;
                if (materials.Length > setMaterialToIndex)
                {
                    materials[setMaterialToIndex] = newMaterial;
                    Undo.RecordObject(setMaterialToRenderer, "Assign New Material");
                    setMaterialToRenderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(setMaterialToRenderer);

                    // 重置索引
                    setMaterialToIndex = -1;
                    setMaterialToRenderer = null;
                }
                else
                {
                    Debug.LogError("Material index out of range！！");
                }
            }
        }

        private void DrawFoldLayout(int lodLevelIndex, string type, LOD[] lods, List<GameObject> objects,
            bool hasLodGroup, PrefabCreator pfc)
        {
            if (objects.Count == 0) return;

            lodFoldouts[lodLevelIndex] = lodFoldouts.GetValueOrDefault(lodLevelIndex, false);
            // 绘制LOD层级的框框
            Color backgroundColor = PrefabCreatorUtils.GetLodLevelColor(lodLevelIndex);
            Rect rect = EditorGUILayout.BeginVertical("box");
            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(type, EditorStyles.boldLabel, GUILayout.Width(70));


            if (hasLodGroup)
            {
                GUILayout.FlexibleSpace();
                float blendInHeight = lods[^2].screenRelativeTransitionHeight;
                EditorGUILayout.LabelField($"{(blendInHeight * 100):0.##}%", EditorStyles.boldLabel,
                    GUILayout.Width(60f));
            }

            EditorGUILayout.EndHorizontal();

            lodFoldouts[lodLevelIndex] = EditorGUILayout.Foldout(lodFoldouts[lodLevelIndex], type, true);

            // 如果当前LOD层级是展开的，绘制其内容
            if (lodFoldouts[lodLevelIndex])
            {
                // 遍历当前LOD层级的所有GameObject对象
                foreach (var t in objects)
                {
                    var lodObject = t;
                    var mesh = t;

                    if (mesh == null)
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("GameObject 名称:", customLabelStyle, GUILayout.Width(130));

                    // 判断pfc.prefab是否为null
                    if (pfc.currentPrefab != null)
                    {
                        if (GUILayout.Button(lodObject.name, clickableTextStyle))
                        {
                            PrefabCreatorUtils.selectTargetMesh(pfc.currentPrefab, mesh.name);
                        }
                    }
                    else
                    {
                        // pfc.prefab为null时，显示普通文本
                        EditorGUILayout.LabelField(lodObject.name, customLabelStyle);
                    }


                    EditorGUILayout.EndHorizontal();


                    // 显示材质
                    EditorGUILayout.BeginVertical();
                    Renderer renderer = lodObject.GetComponent<Renderer>();
                    Material[] materials = renderer.sharedMaterials;
                    {
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);

                        EditorGUI.BeginChangeCheck(); // 开始检测更改

                        for (int j = 0; j < materials.Length; j++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Element " + j, GUILayout.Width(85));
                            materials[j] =
                                (Material)EditorGUILayout.ObjectField(materials[j], typeof(Material), false);

                            EditorGUILayout.EndHorizontal();
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(renderer, "Change Materials");
                            renderer.sharedMaterials = materials;
                            EditorUtility.SetDirty(renderer);
                        }

                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }
}