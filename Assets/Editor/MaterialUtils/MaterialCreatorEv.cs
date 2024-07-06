using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.MaterialUtils
{
    public static class MaterialCreatorEv
    {
            [MenuItem("Assets/XRender/EV/Creat Default Lit")]
            static void CreatDefaultLit() => CreateMaterialVariant("Assets/Res/Shader/MaterialLibrary/M_EV_DefaultLit.mat");
            [MenuItem("Assets/XRender/EV/Creat Layered Rock")]
            static void CreatLayeredRock() => CreateMaterialVariant("Assets/Res/Shader/MaterialLibrary/M_EV_LayeredRock.mat");
            [MenuItem("Assets/XRender/EV/Creat Layered Architecture")]
            static void CreatLayeredArchitecture() => CreateMaterialVariant("Assets/Res/Shader/MaterialLibrary/M_EV_LayeredArchitecture.mat");
            
            private static void CreateMaterialVariant(string parentMaterialPath)
            {
                Material selectedMaterial = AssetDatabase.LoadAssetAtPath<Material>(parentMaterialPath);
                var newMaterial = new Material(selectedMaterial);
                newMaterial.parent = selectedMaterial;

                string path = "";
                string folderPath = GetSelectedDirectory();

                if (folderPath is null or "" or "Assets")
                {
                    return;
                }
                string materialName = selectedMaterial.name.Replace("M_","MI_");
                path = folderPath + "/" + materialName + ".mat";
                AssetDatabase.CreateAsset(newMaterial, AssetDatabase.GenerateUniqueAssetPath(path));
                AssetDatabase.SaveAssets();
                Selection.activeObject = newMaterial;
            }
            
            private static string GetSelectedDirectory()
            {
                Object o = Selection.activeObject;

                if (o == null) return "Assets";
                string path = AssetDatabase.GetAssetPath(o.GetInstanceID());

                if (string.IsNullOrEmpty(path)) return "Assets";
                if (Directory.Exists(path))
                    return GetRelativePath(Path.GetFullPath(path));

                string res = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(res) && Directory.Exists(res))
                    return GetRelativePath(Path.GetFullPath(res));

                return "Assets";
            }

            private static string GetRelativePath(string path)
            {
                string full = Path.GetFullPath(path).Replace("\\", "/");
                string cur = Directory.GetCurrentDirectory().Replace("\\", "/");
                if (!cur.EndsWith("/"))
                    cur += "/";
                return full.Replace(cur, "");
            }
    }
}
