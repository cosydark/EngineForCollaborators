using System;
using System.Collections;
using System.Collections.Generic;
using FunPlus.WorldX.Stream.Defs;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using WorldPartitionLayer = FunPlus.WorldX.Stream.Defs.MultiEditingLayer;

namespace FunPlus.WorldX.WorldPartition
{
    [DisallowMultipleComponent]
    public class WorldPartitionObject : MonoBehaviour
    {
        public WorldPartitionLayer layer = WorldPartitionLayer.PROP;
        [ShowIf("IfShowGlobalLayers")]
        public MultiEditingGlobalLayer globalLayer = MultiEditingGlobalLayer.global;
        public bool isMissingPrefab = false;
        
#if UNITY_EDITOR
        [ShowIf("IfShowSaveToVegetationSystem")]
        [LabelText("由植被系统渲染")]
        public bool saveIntoVegetationSystem = true;
#endif
        
        /// <summary>
        /// when enabled, the GameObject wont be stored neither in chunk nor global layer, it will be stored in the main scene
        /// </summary>
        public bool storeInScene = false;
        
        //新增用于挂载后它及其以下的所有物件不再参与分块保存
        public bool Ignore;

        [PropertyTooltip("跳过编辑器时的检测，但导出和运行时保留")]
        public bool SkipEditorCheck;

#if UNITY_EDITOR
        //暂时不再限制prefab内挂载分层脚本
        // private void OnValidate()
        // {
        //     DestoryComponent(this);
        // }

        public static void DestoryComponent(Component component)
        {
            if (PrefabUtility.GetPrefabAssetType(component) == PrefabAssetType.Regular
                && PrefabUtility.GetPrefabInstanceStatus(component) == PrefabInstanceStatus.NotAPrefab)
            {
                EditorApplication.CallbackFunction func = null;
                func = delegate
                {
                    EditorApplication.update -= func;
                    if (component == null)
                    {
                        return;
                    }
                    GameObject obj = component?.gameObject;
                    EditorUtility.DisplayDialog("警告！", "prefab中不允许挂载WorldPartitionObject脚本!!已删除！！请在实例上挂载！！", "好的");
                    Component.DestroyImmediate(component, true);
                    Selection.activeObject = obj;
                };
                EditorApplication.update += func;
            }
        }
        private bool IfShowGlobalLayers()
        {
            if (layer == MultiEditingLayer.GLOBAL)
            {
                return true;
            }

            return false;
        }
        
        private bool IfShowSaveToVegetationSystem()
        {
            // 目前只支持两种植被类型，如果要更多种 请在这添加
            if (layer == MultiEditingLayer.VEGETATION || layer == MultiEditingLayer.PLANT)
            {
                return true;
            }

            return false;
        }

        public bool IsSaveToVegetationSystem()
        {
            return IfShowSaveToVegetationSystem() && saveIntoVegetationSystem;
        }
        
#endif
    }
}