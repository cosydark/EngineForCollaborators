using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.PrefabCreator.Module;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using ModulePreset = System.Collections.Generic.Dictionary<string, System.Object>;
using ModulesPreset =
    System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Object>>;
using Object = UnityEngine.Object;

namespace Editor.PrefabCreator
{
    public partial class PrefabCreator : OdinEditorWindow
    {
        #region public paramter

        public GameObject fbx;
        public GameObject currentPrefab;

        public Dictionary<GameObject, PrefabInfo> prefabInfos = new Dictionary<GameObject, PrefabInfo>();

        internal readonly List<GameObject> prefabs = new List<GameObject>();

        public string PrefabSavePath;
        public string DefaultPrefabPath;

        //Preset
        public List<string> presetNames => _modulesPresetManager.GetPresetList();

        //标记是新建还是加载prefab，对于 加载预设和从prefab中还原设置的冲突进行处理。
        public bool loadFormPrefab;

        #endregion

        #region private paramter

        //Constant Path 
        private string modelParentPath = "Assets/Res/Environment/Level_IOT/Stone"; // 这是 Model 目录的 父目录 
        private string jsonFolderPath = "DCCExporter";

        //GUI  Parameter
        private Vector2 scrollPosition;
        private bool useJsonFiles;


        //RunTimeData
        private readonly Dictionary<string, IModule> moduleDictionary = new Dictionary<string, IModule>();
        private string[] jsonFiles;
        private int selectedIndex;
        private RootObject rootObject;
        private readonly ModulesPresetManager _modulesPresetManager = new ModulesPresetManager();
        private int selectedPresetIndex;
        private int loadAnotherPrefabControlID = 888888;
        private int loadDefaultPrefabControlId = 999999;

        //打开第一次操作
        private bool firstOperate = true;
        private ModulesPreset presetTemp = null;

        #endregion


        //模块管理 -----------------------------------------------------------------------------------------------------
        private void InitializeModules()
        {
            moduleDictionary.Clear();
            moduleDictionary.Add("LOD", new LODModel("LOD", true));
            moduleDictionary.Add("Collider", new ColliderModule("Collider"));
            // moduleDictionary.Add("Material", new ColliderModel("Material"));
            moduleDictionary.Add("Setting", new SettingModule("Settings", true));
            InitializeAllModules();
        }

        //调用每个模块的Init操作
        private void InitializeAllModules()
        {
            // Debug.Log("初始化模块-----");
            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                module.Init(this);
            }
        }

        //更新每个模块的设置到大纲中Prefab对象
        public void UpdateAllModules()
        {
            // Debug.Log("同步模块设置-----");
            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                // 检查模块是否启用
                if (module.Enabled)
                {
                    module.UpdatePrefabs(this);
                }
            }
        }

        //调用可禁用模块的禁用事件
        private void DoDisableModulesWhichCanDisable()
        {
            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                if (module.CanDisabled)
                {
                    module.OnDisable(this);
                }
            }
        }

        //在导出保存 前对对象执行操作
        private void DoExportAllModules()
        {
            if (currentPrefab == null)
            {
                Debug.LogWarning("无法执行DoExportAllModules，Prefab为空");
                return;
            }

            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                // 检查模块是否启用
                if (module.Enabled)
                {
                    module.BeforeExport(this);
                }
            }
        }

        //在导出保存 后对对象执行操作
        private void RestoreFromPrefabAllModels()
        {
            if (currentPrefab == null)
            {
                Debug.LogWarning("无法执行RestoreFromPrefabAllModels，Prefab为空");
                return;
            }
            else
            {
                Debug.Log("开始从Prefab中读取预设");
            }

            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                // 检查模块是否启用
                if (module.Enabled)
                {
                    module.LoadFromPrefab(this);
                }
            }
        }

        //在导出保存 后对对象执行操作
        private void DoAfterExportedAllModules()
        {
            if (currentPrefab == null)
            {
                Debug.LogWarning("无法执行DoAfterExportedAllModules，Prefab为空");
                return;
            }

            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                // 检查模块是否启用
                if (module.Enabled)
                {
                    module.AfterExported(this);
                }
            }
        }

        //关闭窗口时调用每个模块的销毁操作
        private void DoDisposeAllModules()
        {
            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                module.Dispose();
            }
        }

        // Preset ----------------------------------------------------------------------------------------------------

        #region Preset

        public bool SavePreset(string moduleName)
        {
            var currentPreset = SaveAllModulesToPreset();
            _modulesPresetManager.UpdatePreset(moduleName, currentPreset);
            Repaint();
            return true;
        }
        
        //获取所有模组的预设
        private ModulesPreset SaveAllModulesToPreset()
        {
            var preset = new ModulesPreset();

            foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
            {
                IModule module = moduleEntry.Value;
                ModulePreset modulePreset = moduleEntry.Value.SaveToModulePreset();
                preset[module.ModuleName] = modulePreset;
            }

            return preset;
        }

        private void ApplyCurrentPreset()
        {
            // Debug.Log("尝试加载预设");
            ModulesPreset preset = _modulesPresetManager.GetPreset(selectedPresetIndex);
            ApplyPreset(preset);
        }

        private void ApplyPreset(ModulesPreset preset)
        {
            if (preset != null)
            {
                foreach (KeyValuePair<string, IModule> moduleEntry in moduleDictionary)
                {
                    IModule module = moduleEntry.Value;
                    if (preset.TryGetValue(module.ModuleName, out var modulePreset))
                    {
                        module.ApplyModulePreset(modulePreset, loadFormPrefab);
                    }
                }

                GetWindow<SceneView>().ShowNotification(new GUIContent("预设加载成功"), 0.5d);
                UpdateAllModules();
            }
        }

        private void DeleteCurrentPreset()
        {
            ModulesPreset preset = _modulesPresetManager.GetPreset(selectedPresetIndex);
            if (preset != null)
            {
                _modulesPresetManager.RemovePreset(_modulesPresetManager.GetPresetList()[selectedPresetIndex]);
            }
        }

        #endregion


        //Override-----------------------------------------------------------------------------------------------------

        protected override void OnEnable()
        {
            // 定义 DCCExport 文件夹和子文件夹的路径
            string dccExportPath = "Assets/DCCExporter";
            string[] subFolders = { "Model", "Texture", "Json", "Prefab" };

            // 检查 DCCExport 文件夹是否存在，如果不存在则创建
            if (!AssetDatabase.IsValidFolder(dccExportPath))
            {
                AssetDatabase.CreateFolder("Assets", "DCCExporter");
            }

            // 遍历子文件夹数组，检查每个子文件夹是否存在，如果不存在则创建
            foreach (string subFolder in subFolders)
            {
                string subFolderPath = Path.Combine(dccExportPath, subFolder);
                if (!AssetDatabase.IsValidFolder(subFolderPath))
                {
                    AssetDatabase.CreateFolder(dccExportPath, subFolder);
                }
            }

            // 刷新 AssetDatabase，确保 Unity 编辑器更新文件夹状态
            AssetDatabase.Refresh();

            jsonFolderPath = Application.dataPath + "/" + "DCCExporter" + "/" + "Json";
            currentPrefab = null;
            prefabs.Clear();
            OnChangeInput(null);

            selectedPresetIndex = 0;
            InitializeModules();
            ApplyCurrentPreset();
        }

        protected override void OnDestroy()
        {
            DoDisposeAllModules();
        }

        protected override void OnGUI()
        {
            // init --------------------------------------------------------------------------------------------------
            if (moduleDictionary.Count == 0)
            {
                InitializeModules();
                ApplyCurrentPreset();
            }

            bool hasPrefab = currentPrefab != null && prefabs.Count > 0;

            // 操作对象 --------------------------------------------------------------------------------------------

            #region TargetRegion

            //Line1
            SirenixEditorGUI.Title("Prefab Creator", "", TextAlignment.Center, false);
            // EditorGUILayout.BeginHorizontal();
            // useJsonFiles = EditorGUILayout.ToggleLeft("使用Json文件", useJsonFiles, GUILayout.Width(100));
            // EditorGUI.BeginChangeCheck();
            // EditorGUI.BeginDisabledGroup(!useJsonFiles);
            // selectedIndex = EditorGUILayout.Popup(selectedIndex, jsonFiles); // 设置Popup
            // if (GUILayout.Button("更新列表", GUILayout.Width(65)))
            // {
            //     OnChangeInput();
            //     GUI.FocusControl(null);
            // }
            //
            // EditorGUI.EndDisabledGroup();
            // if (EditorGUI.EndChangeCheck())
            // {
            //     useJsonFiles = true;
            //     currentPrefab = null;
            //     OnChangeInput();
            // }
            //
            // EditorGUILayout.EndHorizontal();

            //Line2
            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 45;
            GameObject newFbxInput = (GameObject)EditorGUILayout.ObjectField("Mesh:", fbx, typeof(GameObject), false,
                GUILayout.ExpandWidth(true));


            if (EditorGUI.EndChangeCheck())
            {
                // 检查选择的GameObject是否是一个模型
                if (newFbxInput != null && !AssetDatabase.GetAssetPath(newFbxInput)
                        .EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    GetWindow<SceneView>().ShowNotification(new GUIContent("请拖入Fbx模型"), 1d);
                }
                else
                {
                    // useJsonFiles = false;
                    OnChangeInput(newFbxInput);
                }
            }


            //Line3
            if (!hasPrefab)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Prefab:", null, typeof(GameObject), true, GUILayout.ExpandWidth(true));
                GUI.enabled = true;
                if (GUILayout.Button("尝试加载默认Prefab", GUILayout.Width(150)))
                {
                    LoadPrefab();
                }

                EditorGUILayout.EndHorizontal();
            }

            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = false;
                EditorGUILayout.ObjectField("Prefab:", prefab, typeof(GameObject), true, GUILayout.ExpandWidth(true));
                GUI.enabled = true;

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(
                        AssetDatabase.LoadAssetAtPath<Object>(prefabInfos[prefab].savePath));
                    ChangeCurrentPrefab(prefab);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("确认删除", "你确定要删除这个Prefab吗？", "是", "否"))
                    {
                        if (prefab == currentPrefab)
                        {
                            // 如果被删除的是CurrentPrefab，找到一个相近的Prefab
                            currentPrefab = PrefabCreatorUtils.FindClosestPrefab(prefabs, i);
                        }

                        // 从列表中移除Prefab
                        RemoveFromPrefabs(prefabs[i]);
                        i--;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (hasPrefab)
            {
                //添加Prefab 变体的功能
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("添加已有的Prefab的变体"))
                {
                    loadAnotherPrefabControlID = GUIUtility.GetControlID(FocusType.Passive);
                    string fbxFilter = fbx.name.Replace("SM_", "");
                    EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, fbxFilter, loadAnotherPrefabControlID);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUIUtility.labelWidth = 0;

            CheckChooseEvent();

            #endregion

            //预设区----------------------------------------------------------------------------------------------------

            #region Preset

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            string[] presetNames = _modulesPresetManager.GetPresetList().ToArray();
            EditorGUIUtility.labelWidth = 50;
            EditorGUI.BeginChangeCheck();
            selectedPresetIndex =
                EditorGUILayout.Popup("预设：", selectedPresetIndex, presetNames, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                ApplyCurrentPreset();
            }

            // 应用选定的预设
            if (GUILayout.Button("加载", GUILayout.Width(45)))
            {
                ApplyCurrentPreset();
            }

            if (GUILayout.Button("保存", GUILayout.Width(45)))
            {
                PresetNamePopup.OpenPresetNamePopup(this);
            }

            if (selectedPresetIndex < 3)
            {
                GUI.enabled = false;
            }

            if (GUILayout.Button("删除", GUILayout.Width(45)))
            {
                bool confirmDelete = EditorUtility.DisplayDialog(
                    "确认删除",
                    "你确定要删除吗？",
                    "删除",
                    "取消"
                );
                if (confirmDelete)
                {
                    DeleteCurrentPreset();
                    selectedPresetIndex = Mathf.Max(0, selectedPresetIndex - 1);
                }
            }

            GUI.enabled = true;

            if (GUILayout.Button("路径", GUILayout.Width(45)))
            {
                string path = ModulesPresetManager.SavePath;
                Object obj = AssetDatabase.LoadAssetAtPath(ModulesPresetManager.SavePath, typeof(Object));
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                }
                else
                {
                    Debug.LogError("保存的对象不存在，无法找到路径: " + path);
                }
            }

            GUILayout.EndHorizontal();

            #endregion

            //模块区   -------------------------------------------------------------------------------------------

            #region ModuleRegion

            GUILayout.Label("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var kvp in moduleDictionary)
            {
                var moduleName = kvp.Key;
                var module = kvp.Value;
                GUILayout.BeginHorizontal(GUI.skin.box);

                // 检查模块是否允许被禁用
                if (module.CanDisabled)
                {
                    bool prevEnabled = module.Enabled;
                    bool newEnable = EditorGUILayout.ToggleLeft("", module.Enabled, GUILayout.Width(18));
                    if (newEnable != module.Enabled)
                    {
                        module.Enabled = newEnable;
                        //执行关闭模块时的操作
                        OnChangeModelEnable(newEnable);
                    }

                    if (prevEnabled != module.Enabled && !module.Enabled && module.IsExpanded)
                    {
                        module.IsExpanded = false;
                    }

                    GUI.enabled = module.Enabled;
                }


                Color defaultColor = GUI.backgroundColor;
                GUI.backgroundColor = module.Enabled
                    ? (module.IsExpanded ? new Color(0.7f, 1f, 0.7f) : new Color(0.6f, 0.7f, 0.6f))
                    : Color.gray;
                if (GUILayout.Button(module.ModuleName, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
                {
                    if (module.Enabled)
                    {
                        // 切换模块的展开状态
                        module.IsExpanded = !module.IsExpanded;
                    }
                }

                GUI.backgroundColor = defaultColor;
                GUILayout.EndHorizontal();
                GUI.enabled = true;

                // 根据模块的IsExpanded属性来决定是否绘制设置
                if (module.IsExpanded)
                {
                    EditorGUI.indentLevel++;
                    GUILayout.BeginVertical(GUI.skin.box);
                    module.DrawModuleGUI(this);
                    GUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
            }

            GUILayout.Label("", GUI.skin.horizontalSlider);
            GUILayout.Space(15);

            #endregion

            // 功能区  ---------------------------------------------------------------------------------------------

            #region FunctionRegion

            GUILayout.BeginHorizontal();
            GUI.enabled = prefabs.Count == 0;
            if (GUILayout.Button("创建Prefab"))
            {
                CreatePrefab();
            }

            GUI.enabled = prefabs.Count != 0;
            if (GUILayout.Button("保存Prefab"))
            {
                SavePrefabs();
            }

            GUI.enabled = true;

            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            #endregion
        }


        //Function -----------------------------------------------------------------------------------------------------

        #region Public Function

        public PrefabInfo GetCurrentPrefabInfo()
        {
            if (currentPrefab != null && prefabInfos.TryGetValue(currentPrefab, out var info))
            {
                return info;
            }

            return new PrefabInfo();
        }

        public void GetPrefabsInfo()
        {
            if (prefabs == null) return;
            prefabInfos.Clear();
            UpdatePrefabPath(fbx);
            foreach (var prefab in prefabs)
            {
                //获取填充prefab 的mesh信息
                var tempPrefabInfo = new PrefabInfo(prefab, PrefabSavePath);

                //获取Face

                #region faces

                int faces = 0;
                var lodMeshList = tempPrefabInfo.GetLodMeshList();
                if (lodMeshList.Count > 0)
                {
                    foreach (var mesh in lodMeshList[0])
                    {
                        faces += mesh.triangles.Length / 3;
                    }
                }


                tempPrefabInfo.faces = faces;
                prefabInfos[prefab] = tempPrefabInfo;

                #endregion
            }
        }

        public string GetCurrentPresetName()
        {
            var presetList = _modulesPresetManager.GetPresetList();
            if (selectedIndex < presetList.Count)
            {
                return presetList[selectedPresetIndex];
            }

            return "";
        }

        #endregion

        #region Private Function

        [MenuItem("美术/预制体创建工具")]
        private static void OpenWindow()
        {
            var window = GetWindow<PrefabCreator>();
            window.titleContent = new GUIContent("Prefab Creator");
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(400, 600);
        }

        private void CreatePrefab()
        {
            loadFormPrefab = false;
            currentPrefab = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            if (currentPrefab != null)
            {
                //移动位置和相机视角
                currentPrefab.transform.position = PrefabCreatorUtils.CalcPreviewPosition();
                Selection.activeGameObject = currentPrefab;
                PrefabCreatorUtils.FocusSelect();

                //修改命名
                if (currentPrefab.name.StartsWith("SM_"))
                {
                    currentPrefab.name = currentPrefab.name.Replace("SM_", "Prefab_");
                }

                AddPrefabs(currentPrefab);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "创建Prefab对象失败", "OK");
            }
        }

        private void LoadPrefab()
        {
            loadFormPrefab = true;
            GameObject prefabFile = (GameObject)AssetDatabase.LoadAssetAtPath<Object>(DefaultPrefabPath);
            if (prefabFile == null)
            {
                bool option = EditorUtility.DisplayDialog(
                    "加载Prefab",
                    "未找到对应的Prefab，请确定" + DefaultPrefabPath + "路径下存在Prefab以供加载",
                    "手动选择",
                    "取消"
                );

                if (option)
                {
                    loadDefaultPrefabControlId = GUIUtility.GetControlID(FocusType.Passive);
                    string fbxFilter = fbx.name.Replace("SM_", "");
                    EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, fbxFilter, loadDefaultPrefabControlId);
                }
            }
            else
            {
                currentPrefab = (GameObject)PrefabUtility.InstantiatePrefab(prefabFile);
                //移动位置和相机视角
                currentPrefab.transform.position = PrefabCreatorUtils.CalcPreviewPosition();
                Selection.activeGameObject = currentPrefab;
                PrefabCreatorUtils.FocusSelect();

                //执行从prefab中读取设置
                RestoreFromPrefabAllModels();
                //然后添加prefab
                AddPrefabs(currentPrefab);
                UpdateAllModules();
            }
        }
        
        private void OnChangeModelEnable(bool isEnable)
        {
            // 首先，确保原Prefab存在
            if (currentPrefab == null)
            {
                return;
            }

            if (isEnable)
            {
                UpdateAllModules();
            }
            else
            {
                DoDisableModulesWhichCanDisable();
            }
        }

        private void OnChangeInput(GameObject newFBX)
        {
            currentPrefab = null;
            prefabs.Clear();
            prefabInfos.Clear();


            if (!firstOperate && fbx != newFBX)
            {
                //替换模型时,保留之前的预设状态,供下次使用
                presetTemp = SaveAllModulesToPreset();
            }

            fbx = newFBX;

            bool loadSuccess = RefreshJsonFiles();
            loadSuccess = loadSuccess && LoadFBX();

            GetPrefabsInfo();
            InitializeAllModules();
            
            if (presetTemp != null)
            {
                Debug.Log("应用之前的预设");
                ApplyPreset(presetTemp);
            }
        }

        private bool LoadFBX()
        {
            if (useJsonFiles)
            {
                //使用Json模式________________________________________
                bool loadSuccess = false;
                string jsonFilePath = Path.Combine(jsonFolderPath, jsonFiles[selectedIndex] + ".json");
                string jsonContent = File.ReadAllText(jsonFilePath);
                //从JSON解析 RootObject信息
                rootObject = JsonUtility.FromJson<RootObject>(jsonContent);

                if (rootObject != null)
                {
                    string relativePath = "Assets/" + Path.GetRelativePath(Application.dataPath, rootObject.fbxPath);

                    if (AssetDatabase.LoadAssetAtPath<GameObject>(relativePath) is { } loadedFBX)
                    {
                        fbx = loadedFBX;
                        loadSuccess = true;
                        // 更新 Prefab 的目标路径及父路径
                        UpdatePrefabPath(fbx);
                    }
                    else
                    {
                        Debug.LogWarning("FBX 未找到，路径为: " + relativePath);
                    }

                    // // Set material index
                    // foreach (var materialInfo in rootObject.materials)
                    // {
                    //     materialInfo.parentMaterialIndex = GuessIndexFromName(materialInfo.name, materialInfo.parentMaterialIndex);
                    // }
                }

                return loadSuccess;
            }

            //使用非Json模式_________________________________________
            if (fbx != null)
            {
                UpdatePrefabPath(fbx);
                return true;
            }

            return false;
        }

        private void CheckChooseEvent()
        {
            Event e = Event.current;
            string commandName = e.commandName;

            if (e.type == EventType.ExecuteCommand && commandName == "ObjectSelectorClosed")
            {
                var selectedPrefab = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                int id = EditorGUIUtility.GetObjectPickerControlID();

                if (currentPrefab != null && prefabs.Count > 0)
                {
                    if (selectedPrefab == null || id != loadAnotherPrefabControlID) return;
                    Debug.Log("Load Another Prefab");
                    GameObject newGameObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                    if (currentPrefab != null)
                        newGameObject.transform.position = currentPrefab.transform.position +
                                                           PrefabCreatorUtils.GetOffset(selectedPrefab);
                    currentPrefab = newGameObject;
                    AddPrefabs(newGameObject);
                    e.Use();
                }
                else
                {
                    if (selectedPrefab == null || id != loadDefaultPrefabControlId) return;
                    Debug.Log("Load Default Prefab");

                    DefaultPrefabPath = AssetDatabase.GetAssetPath(selectedPrefab);
                    currentPrefab = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                    currentPrefab.transform.position = PrefabCreatorUtils.CalcPreviewPosition();
                    Selection.activeGameObject = currentPrefab;
                    PrefabCreatorUtils.FocusSelect();

                    //执行模块的操作
                    RestoreFromPrefabAllModels();
                    AddPrefabs(currentPrefab);
                    e.Use();
                }
            }
        }

        //根据FBX 更新默认的prefabPath，和Prefab路径
        private void UpdatePrefabPath(Object newFbx)
        {
            if (newFbx != null)
            {
                string fbxPath = AssetDatabase.GetAssetPath(newFbx);
                string fbxName = newFbx.name;
                string parentPath = Path.GetDirectoryName(fbxPath);
                modelParentPath = Path.GetDirectoryName(parentPath);
                if (modelParentPath != null)
                {
                    modelParentPath = modelParentPath.Replace("\\", "/");
                    string prefabName = fbxName.Replace("SM_", "Prefab_");
                    PrefabSavePath = modelParentPath + "/Prefab/";
                    DefaultPrefabPath = modelParentPath + "/Prefab/" + prefabName + ".prefab";
                }
            }
        }

        private bool RefreshJsonFiles()
        {
            jsonFiles = Directory.GetFiles(jsonFolderPath, "*.json").Select(Path.GetFileNameWithoutExtension).ToArray();
            return !(jsonFiles == null || jsonFiles.Length <= 0);
        }

        private void SavePrefabs()
        {
            bool dialog = false;
            int option = 1;
            GameObject tempCurrentPrefab = currentPrefab;
            GameObject prefabVariant = null;
            foreach (var prefab in prefabs)
            {
                currentPrefab = prefab;
                UpdatePrefabPath(fbx); // 确保fbx是在这个上下文中定义的变量
                string prefabSavePath = prefabInfos[prefab].savePath;
                if (!PrefabCreatorUtils.CheckFBX(fbx) || !PrefabCreatorUtils.CheckPrefab(prefab, prefabSavePath))
                    continue;

                // 检查原始目标路径是否已经有对象
                GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabSavePath);
                if (existingPrefab != null)
                {
                    if (!dialog)
                    {
                        dialog = true;
                        // 提示用户选择如何操作
                        option = EditorUtility.DisplayDialogComplex(
                            "Prefabs处理",
                            "请选择如何处理已存在的Prefabs：",
                            "创建变体",
                            "覆盖",
                            "取消"
                        );

                        if (option == 2) // 如果用户选择取消，则不执行任何操作
                            return;
                    }


                    switch (option)
                    {
                        case 0: // 创建变体
                            prefabSavePath = AssetDatabase.GenerateUniqueAssetPath(prefabSavePath);
                            string prefabName = Path.GetFileNameWithoutExtension(prefabSavePath);
                            prefab.name = prefabName;
                            break;
                        case 1: // 覆盖
                            // 如果选择覆盖，则不需要更改路径或Prefab名称
                            break;
                    }
                }

                Debug.Log("创建Prefab   路径为：" + prefabSavePath);

                //执行 所有模块的导出前预处理
                DoExportAllModules();
                // 创建Prefab Variant
                prefabVariant =
                    PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, prefabSavePath, InteractionMode.UserAction);
                //执行 所有模块的导后的处理 恢复Prefab的预先状态
                DoAfterExportedAllModules();
            }

            if (prefabVariant != null)
            {
                AssetDatabase.Refresh();
                Selection.SetActiveObjectWithContext(prefabVariant, null); // 将新的Prefab Variant设置为选中状态

                // 显示成功弹窗
                EditorUtility.DisplayDialog("成功", "Prefab创建成功！", "确定");
            }
            else
            {
                // 显示错误弹窗
                EditorUtility.DisplayDialog("错误", "Prefab创建失败！", "确定");
            }
        }

        //多Prefab的操作 -------------------------
        //切换不同的Prefab预览对象
        private void ChangeCurrentPrefab(GameObject prefab)
        {
            currentPrefab = prefab;
            UpdateAllModules();
        }

        private void AddPrefabs(GameObject prefab)
        {
            if (!PrefabCreatorUtils.CheckPrefabFBX(prefab, fbx))
            {
                return;
            }

            prefabs.Add(prefab);
            GetPrefabsInfo();

            switch (firstOperate)
            {
                case true:
                    //如果打开工具后的第一次操作,那么则根据面数去自动选择一个Preset
                    firstOperate = false;
                    int faces = prefabInfos[prefab].faces;
                    selectedPresetIndex = faces < 2500 ? 0 : faces < 10000 ? 1 : 2;
                    ApplyCurrentPreset();
                    break;
                case false:

                    break;
            }

            UpdateAllModules();
        }

        private void RemoveFromPrefabs(GameObject prefab)
        {
            prefabs.Remove(prefab);
            GetPrefabsInfo();
        }

        #endregion
    }
}