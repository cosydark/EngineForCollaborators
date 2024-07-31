using System;
using FunPlus.WorldX.Stream.Defs;
using FunPlus.WorldX.WorldPartition;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ModulePreset = System.Collections.Generic.Dictionary<string, System.Object>;
using Object = UnityEngine.Object;

namespace Editor.PrefabCreator.Module
{
    public class SettingModule : Module
    {
        private bool isStatic = true;
        private bool freezeTransform = true;


        //用来记录Transform数据

        #region TransformData

        [Serializable]
        private class TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public TransformData(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
                scale = transform.localScale;
            }

            public void ApplyTo(Transform transform)
            {
                transform.position = position;
                transform.rotation = rotation;
                transform.localScale = scale;
            }
        }

        private TransformData prefabTransformTemp;

        #endregion


        //分块工具用参数
        private bool addWorldPartitionObject = true;
        private MultiEditingLayer multiEditingLayer = MultiEditingLayer.ARTBLOCK;

        //Tag 和Layer
        private String[] tags;  
        private String[] layers;
        int selectedTagIndex;
        int selectedLayerIndex;

        //Override------------------------------------------------------------------------------------------------------
        public SettingModule(string name, bool isExpanded = false, bool canDisabled = true) : base(name, isExpanded,
            canDisabled)
        {
        }   

        public override void Init(PrefabCreator pfc)
        {
            //恢复参数
            prefabTransformTemp = null;
            tags = InternalEditorUtility.tags;
            layers = InternalEditorUtility.layers;
            if (pfc.currentPrefab != null)
            {
                selectedTagIndex = Array.IndexOf(tags, Selection.activeGameObject.tag);
                selectedLayerIndex =
                    Array.IndexOf(layers, LayerMask.LayerToName(Selection.activeGameObject.layer));
            }

            UpdatePrefabs(pfc);
        }


        public override void DrawModuleGUI(PrefabCreator pfc)
        {
            GUIStyle toggleStyle = new GUIStyle(EditorStyles.toggle);
            toggleStyle.fontSize = 10;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical();
            // 检查pfc.prefab是否为null
            if (pfc.currentPrefab == null)
            {
                // 禁用UI元素
                GUI.enabled = false;
                // 显示帮助信息
                EditorGUILayout.HelpBox("请先创建或者加载Prefab", MessageType.Info);
            }

            //第一行 Tag Layer设置
            EditorGUILayout.BeginHorizontal();
            selectedTagIndex = EditorGUILayout.Popup("Tag", selectedTagIndex, tags);
            selectedLayerIndex = EditorGUILayout.Popup("Layer", selectedLayerIndex, layers);
            EditorGUILayout.EndHorizontal();

            // 第二行
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 150;
            freezeTransform = EditorGUILayout.Toggle("Transform 冻结变换", freezeTransform, toggleStyle);
            GUI.enabled = false;
            EditorGUIUtility.labelWidth = 110;
            bool newIsStatic = EditorGUILayout.Toggle("勾选 Static(禁用)", isStatic, toggleStyle);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (pfc.currentPrefab == null)
            {
                // 禁用UI元素
                GUI.enabled = false;
            }

            // 第三行
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 180;
            EditorGUI.BeginChangeCheck();
            var addPartition = EditorGUILayout.Toggle("挂载多人编辑工具用的脚本", addWorldPartitionObject);
            MultiEditingLayer newLayer = multiEditingLayer;
            if (addPartition)
            {
                EditorGUIUtility.labelWidth = 85;
                newLayer = (MultiEditingLayer)EditorGUILayout.EnumPopup("对象类型层", multiEditingLayer);
            }

            if (EditorGUI.EndChangeCheck() &&
                (addPartition != addWorldPartitionObject || newLayer != multiEditingLayer))
            {
                addWorldPartitionObject = addPartition;
                multiEditingLayer = newLayer;
            }

            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();


            // 恢复UI元素的启用状态
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck() && pfc.prefabs.Count != 0)
            {
                isStatic = newIsStatic;
                UpdatePrefabs(pfc);
            }
        }

        public override void BeforeExport(PrefabCreator pfc)
        {
            var prefabInstance = pfc.currentPrefab;
            if (freezeTransform)
            {
                // 记录当前的Transform状态
                prefabTransformTemp = new TransformData(prefabInstance.transform);

                // 将复制的prefab坐标归零
                prefabInstance.transform.position = Vector3.zero;
                prefabInstance.transform.rotation = Quaternion.identity;
                prefabInstance.transform.localScale = Vector3.one;
            }
        }

        public override void AfterExported(PrefabCreator pfc)
        {
            if (freezeTransform && prefabTransformTemp != null)
            {
                // 恢复Transform状态
                prefabTransformTemp.ApplyTo(pfc.currentPrefab.transform);

                // 清除临时记录
                prefabTransformTemp = null;
            }
        }

        public override void LoadFromPrefab(PrefabCreator pfc)
        {
            var prefab = pfc.currentPrefab;
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(pfc.DefaultPrefabPath);

            // 检查Prefab是否为null
            if (prefab == null || prefabAsset == null)
            {
                Debug.LogError("RestoreSettingsFromPrefab: 提供的Prefab是null，或者PrefabPath路径不正确");
                return;
            }

            // 检查Prefab是否具有WorldPartitionObject组件
            var worldPartitionScript = prefab.GetComponent<WorldPartitionObject>();
            addWorldPartitionObject = worldPartitionScript != null;
            if (addWorldPartitionObject)
            {
                // 如果有WorldPartitionObject组件，还原multiEditingLayer的值
                multiEditingLayer = worldPartitionScript.layer;
            }

            // 检查Prefab的isStatic属性
            isStatic = prefab.isStatic;
            // 检查Prefab的Transform属性是否归零
            bool isTransformZero = prefabAsset.transform.position == Vector3.zero &&
                                   prefabAsset.transform.rotation == Quaternion.identity &&
                                   prefabAsset.transform.localScale == Vector3.one;
            freezeTransform = isTransformZero;

            //获取Tag 和Layer
            selectedTagIndex = Array.IndexOf(tags, Selection.activeGameObject.tag);
            selectedLayerIndex = Array.IndexOf(layers, LayerMask.LayerToName(Selection.activeGameObject.layer));

            // 输出还原的设置，以便验证
            Debug.Log($"从预制体恢复设置: 添加世界分区对象 = {addWorldPartitionObject}，" +
                      $"多人编辑层 = {multiEditingLayer}，是否静态 = {isStatic}，" +
                      $"冻结变换 = {freezeTransform}");
        }

        public override void UpdatePrefabs(PrefabCreator pfc)
        {
            var prefabs = pfc.prefabs;
            foreach (var prefab in prefabs)
            {
                //执行操作
                UpdateTagAndLayer(prefab, selectedTagIndex, selectedLayerIndex);
                UpdateStaticRecursively(prefab, isStatic);
                UpdateWorldPartitionScript(prefab, addWorldPartitionObject);
            }
        }

        public override void OnDisable(PrefabCreator pfc)
        {
            var prefabs = pfc.prefabs;
            foreach (var prefab in prefabs)
            {
                //执行操作
                UpdateStaticRecursively(prefab, false);
                UpdateWorldPartitionScript(prefab, false);
                UpdateTagAndLayer(prefab, 0, 0);
            }
        }


        //Preset function ----------------------------------------------------------------------------------------------
        public override ModulePreset SaveToModulePreset()
        {
            return new ModulePreset
            {
                ["freezeTransform"] = freezeTransform,
                ["addWorldPartitionObject"] = addWorldPartitionObject,
                ["multiEditingLayer"] = (long)multiEditingLayer,
            };
        }

        public override void ApplyModulePreset(ModulePreset lodModulePreset, bool loadFormPrefab)
        {
            if (!loadFormPrefab)
            {
                if (lodModulePreset.TryGetValue("freezeTransform", out var value1))
                {
                    freezeTransform = (bool)value1;
                }

                if (lodModulePreset.TryGetValue("addWorldPartitionObject", out var value2))
                {
                    addWorldPartitionObject = (bool)value2;
                }

                if (lodModulePreset.TryGetValue("multiEditingLayer", out var value3))
                {
                    multiEditingLayer = (MultiEditingLayer)Enum.ToObject(typeof(MultiEditingLayer), (int)(long)value3);
                }
            }

            // Debug.Log("Setting模块加载");
        }

        //Update function

        private void UpdateWorldPartitionScript(GameObject obj, bool worldPartitionObjectComponent)
        {
            var worldPartitionScript = obj.GetComponent<WorldPartitionObject>();
            if (worldPartitionObjectComponent)
            {
                if (worldPartitionScript == null)
                {
                    worldPartitionScript = obj.AddComponent<WorldPartitionObject>();
                }

                worldPartitionScript.layer = multiEditingLayer;
            }
            else
            {
                if (worldPartitionScript != null)
                {
                    Object.DestroyImmediate(worldPartitionScript); // 在编辑模式下使用DestroyImmediate
                }
            }
        }

        private void UpdateStaticRecursively(GameObject obj, bool objectStatic)
        {
            obj.isStatic = objectStatic;
            foreach (Transform child in obj.transform)
            {
                UpdateStaticRecursively(child.gameObject, objectStatic);
            }
        }

        private void UpdateTagAndLayer(GameObject obj, int tagIndex, int layerIndex)
        {
            if (tags.Length == 0 || layers.Length == 0)
            {
                tags = InternalEditorUtility.tags;
                layers = InternalEditorUtility.layers;
            }

            obj.tag = tags[tagIndex];
            obj.layer = LayerMask.NameToLayer(layers[layerIndex]);
        }
    }
}