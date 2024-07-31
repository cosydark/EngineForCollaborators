using System.Collections.Generic;
using Editor.LODGeneration;
using Editor.PrefabCreator;
using Editor.PrefabCreator.Module;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor.CharacterLodGenerator
{
    public partial class CharacterLodGenerator : OdinEditorWindow
    {
        #region parameter

        private List<GameObject> prefabList;

        private List<GameObject> prefabInsList;

        // private bool prefabsHaveLodGroup;
        private Dictionary<GameObject, List<Dictionary<GameObject, RendererMaterialManager>>> prefabsMaterialCache;
        public string LOD0FBXSavePath;


        public List<GameObject> allMesh;
        public List<GameObject> lodMesh;
        public List<GameObject> otherMesh;
        public GameObject vfxMesh;

        SkinnedMeshLODManager skinnedLODManager = new SkinnedMeshLODManager();
        public int lodCount; // total num of LOD
        public List<int> polyReductionRatio;
        public bool regenerateVFXMesh;
        private readonly List<float> defaultScreenRatio = new List<float>() { 0.8f, 0.6f, 0.4f, 0.2f, 0.1f, 0.05f };

        #endregion

        [MenuItem("美术/角色LOD生成工具")]
        private static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<CharacterLodGenerator>();
            window.titleContent = new GUIContent("Character Lod Generator");
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(400, 600);
        }

        protected override void OnEnable()
        {
            prefabInsList = new List<GameObject>();
            prefabList = new List<GameObject>();
            // prefabsHaveLodGroup = true;
            regenerateVFXMesh = true;
            allMesh = new List<GameObject>();
            lodMesh = new List<GameObject>();
            otherMesh = new List<GameObject>();
            vfxMesh = null;
            lodCount = 5;
            polyReductionRatio = new List<int>() { 100, 80, 50, 30, 20, 10 };
        }

        protected override void OnGUI()
        {
            SirenixEditorGUI.Title("Character Lod Generator", "", TextAlignment.Center, false);

            //Line 1
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 45;
            EditorGUI.BeginChangeCheck();
            GameObject newPrefabInput = (GameObject)EditorGUILayout.ObjectField("Prefab:",
                prefabList.Count > 0 ? prefabList[0] : null,
                typeof(GameObject), false, GUILayout.ExpandWidth(true));

            if (EditorGUI.EndChangeCheck())
            {
                if (newPrefabInput == null || !PrefabUtility.IsOutermostPrefabInstanceRoot(newPrefabInput))
                {
                    EditorWindow.GetWindow<SceneView>().ShowNotification(new GUIContent("请拖入Prefab对象"), 1d);
                }
                else
                {
                    OnEnable();
                    AddPrefab(newPrefabInput);
                    prefabInsList.Add((GameObject)PrefabUtility.InstantiatePrefab(newPrefabInput));
                    if (prefabInsList != null)
                    {
                        //移动位置和相机视角
                        prefabInsList[0].transform.position = PrefabCreatorUtils.CalcPreviewPosition();
                        Selection.activeGameObject = prefabInsList[0];
                        PrefabCreatorUtils.FocusSelect();
                    }
                }
            }


            EditorGUILayout.EndHorizontal();

            if (prefabList.Count > 1)
            {
                for (int i = 1; i < prefabList.Count; i++)
                {
                    var prefab = prefabList[i];
                    EditorGUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField("Prefab:", prefab, typeof(GameObject), true,
                        GUILayout.ExpandWidth(true));
                    GUI.enabled = true;

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        if (EditorUtility.DisplayDialog("确认删除", "你确定要删除这个Prefab吗？", "是", "否"))
                        {
                            // 从列表中移除Prefab
                            RemoveFromPrefabs(i);
                            i--;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            //Prefab变体
            if (prefabList.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("添加已有的Prefab的变体"))
                {
                    int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
                    string fbxFilter = prefabList[0].name;
                    EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, fbxFilter, controlID);
                }

                // 检测是否有对象选择器事件
                string commandName = Event.current.commandName;
                GameObject selectedPrefab = null;
                if (commandName == "ObjectSelectorClosed")
                {
                    // 当对象选择器关闭时，获取最终选中的Prefab
                    selectedPrefab = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                }


                // 显示当前选中的Prefab
                if (selectedPrefab != null)
                {
                    GameObject newGameObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                    if (prefabInsList.Count > 0)
                        newGameObject.transform.position = prefabInsList[0].transform.position +
                                                           PrefabCreatorUtils.GetOffset(selectedPrefab);
                    prefabInsList.Add(newGameObject);
                    AddPrefab(newGameObject);
                }

                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 35;

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
            // Line 3 LodMesh
            DisplayAndSelectGameObject("LOD", lodMesh);
            // Line 4 VFXMesh
            DisplayAndSelectGameObject("VFX", vfxMesh);
            // Line 5 OtherMesh
            DisplayAndSelectOtherGameObject();
            GUILayout.EndVertical();

            GUILayout.Label("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = 70;

            EditorGUILayout.LabelField("LOD 设置", EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = 120;
            regenerateVFXMesh = EditorGUILayout.Toggle("生成VFXMesh", regenerateVFXMesh);


            lodCount = EditorGUILayout.IntSlider("LOD 数量", lodCount, 4, 6);
            EditorGUILayout.Space();

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");

            for (int i = 1; i < lodCount; i++)
            {
                EditorGUILayout.LabelField("LOD " + i, EditorStyles.boldLabel);
                polyReductionRatio[i] = 100 - EditorGUILayout.IntSlider("减面比例", 100 - polyReductionRatio[i], 0, 100);
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            bool hasPrefabIns = prefabInsList != null;
            GUI.enabled = hasPrefabIns && prefabInsList.Count > 0 && lodMesh.Count > 0;

            if (lodMesh.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到LOD FBX，请检查", MessageType.Warning);
            }

            if (GUILayout.Button("生成LOD"))
            {
                GenerateLOD();
            }

            GUI.enabled = true;


            if (GUILayout.Button("保存Prefab"))
            {
                SavePrefabs();
            }
        }

        //保存Prefabs
        private void SavePrefabs()
        {
            foreach (var prefab in prefabInsList)
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(prefab);
                if (root != null)
                {
                    // 获取Prefab资产的路径
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                    // 获取Prefab资产
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    // 记录当前的Transform状态 保证保存的Prefab会冻结变换
                    Vector3 position = prefab.transform.position;
                    Quaternion rotation = prefab.transform.rotation;
                    Vector3 scale = prefab.transform.localScale;

                    // 将复制的prefab坐标归零
                    prefab.transform.position = Vector3.zero;
                    prefab.transform.rotation = Quaternion.identity;
                    prefab.transform.localScale = Vector3.one;


                    if (prefabAsset != null)
                    {
                        // 将更改应用到Prefab资产
                        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);
                        Debug.Log("Prefab 保存成功: " + prefabPath);
                    }
                    else
                    {
                        Debug.LogError("Could not find the prefab asset to save changes.");
                    }

                    prefab.transform.position = position;
                    prefab.transform.rotation = rotation;
                    prefab.transform.localScale = scale;
                }
            }
        }

        //添加Prefab对象
        private void AddPrefab(GameObject newPrefab)
        {
            prefabList.Add(newPrefab);

            UpdateFBXMesh();
        }

        //删除 某个Prefab变体时候的操作
        private void RemoveFromPrefabs(int index)
        {
            if (index > prefabList.Count || index > prefabInsList.Count || index < 0)
            {
                Debug.LogError("移除Prefab的Index错误");
            }

            DestroyImmediate(prefabInsList[index]);
            prefabList.RemoveAt(index);
            prefabInsList.RemoveAt(index);
            UpdateFBXMesh();
        }

        //开始LOD生成
        private void GenerateLOD()
        {
            CheckAddLodGroup(prefabInsList);
            CleanUpBeforeGenerateLOD();
            GetAllPrefabMaterialCache();


            //GenLod
            if (lodMesh.Count == 0)
            {
                Debug.LogError("No valid mesh!");
                return;
            }

            string result =
                skinnedLODManager.GenerateLODTransitionDistanceAndApply(lodMesh[0], lodCount, polyReductionRatio,
                    regenerateVFXMesh);
            Debug.Log(result);

            UpdateFBXMesh();

            UpdateLODGroupRenderer();
            RevertAllPrefabsMaterial();
            RenamePrefabsClonedChildren();
            UpdatePrefabsLodComponents();

            UpdateFBXMesh();
        }
    }
}