using System;
using System.Collections.Generic;
using System.Linq;
using HoudiniEngineUnity;
using UnityEditor;
using UnityEngine;

namespace Editor.LODGeneration
{
    public class SubmeshExtractor : EditorWindow
    {
        // [MenuItem("TA/Extract Submesh")]
        // private static void OpenWindow()
        // {
        //     var window = ScriptableObject.CreateInstance<SubmeshExtractor>() as SubmeshExtractor;
        //     window.minSize = new Vector2(200, 200);
        //     window.maxSize = new Vector2(200, 200);
        //     window.titleContent = new GUIContent("Extract Submesh");
        //     window.ShowUtility();
        // }

        // private void OnGUI()
        // {
        //     EditorGUILayout.Space(7);
        //     if (GUILayout.Button("Extract", GUILayout.Height(37)))
        //     {
        //         if (AssetDatabase.GetAssetPath(Selection.activeGameObject).EndsWith(".prefab"))
        //         {
        //             GameObject go = Selection.activeGameObject;
        //             RecordMaterial(go, out Dictionary<uint,Material> lookup,out List<string> fbxpath);
        //             foreach (var path in fbxpath)
        //             {
        //                 Extract(path);
        //             }
        //             
        //             if (fbxpath.Count > 0)
        //             {
        //                 Debug.Log($"处理了{fbxpath.Count}个fbx");
        //                 ApplyMaterial(go, ref lookup);
        //                 PostCheck(go);
        //             }
        //             else
        //             {
        //                 Debug.Log("这个prefab上没有需要处理的fbx");
        //             }
        //             
        //         }
        //         else
        //         {
        //             Debug.Log("请选择一个prefab");
        //         }
        //         
        //     }
        // }

        public void SubmeshExtract(List<GameObject> prefabs)
        {
            if (prefabs.Count < 1)
            {
                Debug.LogError("传递的Prefabs为空");
                return;
            }

            var lookupList = new List<Dictionary<uint, Material>>();
            List<string> fbxPath = new List<string>();
            for (int i = 0; i < prefabs.Count; i++)
            {
                var lookup = new Dictionary<uint, Material>();
                RecordMaterial(prefabs[i],out lookup,out fbxPath);
                lookupList.Add(lookup);
            }
            
            foreach (var path in fbxPath)
            {
                Extract(path);
            }
            
            if (fbxPath.Count > 0)
            {
                Debug.Log($"处理了{fbxPath.Count}个fbx");
                for (int i = 0; i < prefabs.Count; i++)
                {
                    var lookup = lookupList[i];
                    ApplyMaterial(prefabs[i], ref lookup);
                    PostCheck(prefabs[i]);
                }
            }
            else
            {
                Debug.Log("这个prefab上没有需要处理的fbx");
            }
        }
        

        private void PostCheck(GameObject go)
        {
            bool good = true;
            GameObject badgo = null;
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                {
                    good = false;
                    badgo = filter.gameObject;
                    break;
                }
            }

            if (!good)
            {
                EditorUtility.DisplayDialog("警告", "检测到处理完毕的prefab存在mesh引用丢失，这可能是因为原prefab中存在复制的mesh被拆分，需要手动清理下:/n" +
                                                  "请将这个gameobject的每个材质球对应的拆分后的mesh复制一份，并赋予正确的材质球", "了解");
                Selection.activeGameObject = badgo;
            }
        }
        
        
        private void RecordMaterial(GameObject go, out Dictionary<uint, Material> dictionary, out List<string> fbxPath)
        {
            dictionary = new Dictionary<uint, Material>();
            fbxPath = new List<string>();

            // 获取所有MeshRenderer和SkinnedMeshRenderer组件
            var renderers = go.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Mesh mesh = null;
                string assetPath = "";

                if (renderer is MeshRenderer && renderer.gameObject.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
                {
                    mesh = meshFilter.sharedMesh;
                    assetPath = AssetDatabase.GetAssetPath(mesh);
                }
                else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    mesh = skinnedMeshRenderer.sharedMesh;
                    assetPath = AssetDatabase.GetAssetPath(mesh);
                }

                if (mesh != null)
                {
                    fbxPath.Add(assetPath);

                    for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                    {
                        uint indexCount = mesh.GetIndexCount(subMeshIndex);
                        Material material = renderer.sharedMaterials[Math.Min(subMeshIndex,mesh.subMeshCount-1)];
                        if (!dictionary.ContainsKey(indexCount))
                        {
                            dictionary[indexCount] = material;
                        }
                    }
                }
            }

            fbxPath = fbxPath.Distinct().ToList();
            Debug.Log($"记录了{dictionary.Count}个材质球,fbx目录为{fbxPath}");
        }
        

        private void ApplyMaterial(GameObject go, ref Dictionary<uint, Material> dictionary)
        {
            Debug.Log($"尝试将存储的{dictionary.Count}个材质球应用给这些{go.name}上");

            // 获取所有Renderer组件（包括MeshRenderer和SkinnedMeshRenderer）
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Mesh mesh = null;

                // 检查渲染器类型并获取相应的Mesh
                if (renderer is MeshRenderer && renderer.gameObject.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
                {
                    mesh = meshFilter.sharedMesh;
                }
                else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    mesh = skinnedMeshRenderer.sharedMesh;
                }

                // 如果找到了Mesh，尝试应用材质
                if (mesh != null)
                {
                    Material[] materials = renderer.sharedMaterials;

                    for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                    {
                        uint indexCount = mesh.GetIndexCount(subMeshIndex);
                        if (dictionary.TryGetValue(indexCount, out Material matchedMaterial))
                        {
                            // Debug.Log($"{mesh.name} + ======== {matchedMaterial}");
                            materials[subMeshIndex] = matchedMaterial;
                        }
                        else
                        {
                            Debug.LogWarning($"No material found for submesh with index count: {indexCount}", renderer.gameObject);
                        }
                    }

                    renderer.sharedMaterials = materials;
                }
            }

            // 释放字典引用
            dictionary = null;
        }
        

        private void Extract(string gopath)
        {
            var session = HEU_SessionManager.GetOrCreateDefaultSession();
        
            HEU_Logger.Log(session.GetSessionData().SessionID.ToString());
            
            string hdaPath = "Packages/com.funplus.worldx.pcg-flow/Editor/GraphPreset/GraphAssociatedHDA/Submesh_Extractor.hda";
        
            string fullPath = HEU_AssetDatabase.GetAssetFullPath(hdaPath);
            GameObject HDA = HEU_HAPIUtility.InstantiateHDA(fullPath, Vector3.zero, session, false);

            // gameObjectName = gameObject.name;
            HEU_HoudiniAssetRoot houdiniAssetRoot = HDA.GetComponent<HEU_HoudiniAssetRoot>();
            HEU_HoudiniAsset HDAasset = houdiniAssetRoot.HoudiniAsset;
            
            string absPath = Application.dataPath.Replace("Assets", gopath);
            HDAasset.Parameters.SetStringParameterValue("file", absPath);
            HDAasset.RequestCook();
            AssetDatabase.ImportAsset(gopath);
            DestroyImmediate(HDA);
        }
    }
}

