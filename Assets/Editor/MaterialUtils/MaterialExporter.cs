using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.MaterialUtils
{
    public class MaterialExporter : EditorWindow
    {
        // [MenuItem("场景工具/材质导出")]
        // [MenuItem("TA/Material Exporter")]
        // private static void OpenWindow()
        // {
        //     var window = ScriptableObject.CreateInstance<MaterialExporter>() as MaterialExporter;
        //     window.minSize = new Vector2(200, 250);
        //     window.maxSize = new Vector2(200, 300);
        //     window.titleContent = new GUIContent("ARK Tools");
        //     window.ShowUtility();
        // }
        [MenuItem("Assets/XRender/Generate XRP Material")]
        private static void GenerateXrpMaterial()
        {
            Material selectedMaterial = Selection.activeObject as Material;

            if (selectedMaterial == null)
            {
                Debug.LogWarning("No material selected.");
                return;
            }
            string materialPathInProject = AssetDatabase.GetAssetPath(selectedMaterial);
            string materialMaterialName = AssetDatabase.LoadAssetAtPath<Material>(materialPathInProject).name;
            string materialShaderName = AssetDatabase.LoadAssetAtPath<Material>(materialPathInProject).shader.name;
            string[] material = ReadFileInProject(materialPathInProject);
            string[] materialNoKeywords = CleanKeywords(material);
            string[] properties = FetchProperties(material);
            string[] k = Array.Empty<string>();
            string[] p = Array.Empty<string>();
            PropertiesToKeywords(properties, materialShaderName, ref k, ref p);
            string[] materialWithNewP = AddStringArrayToStringArray(materialNoKeywords, p, "    m_Floats:");
            string[] materialWithNewKP = AddStringArrayToStringArray(materialWithNewP, k, "  m_ValidKeywords:");
            string[] materialNewShader = ResetParentAndShader(materialWithNewKP);
            SaveToFile(materialNewShader, materialMaterialName,
                materialPathInProject.Replace($"{materialMaterialName}.mat", ""));
        }
        // private void OnGUI()
        // {
        //     EditorGUILayout.Space(7);
        //     if (GUILayout.Button("Gen Material", GUILayout.Height(37)))
        //     {
        //         // string materialPathInProject = "Assets/Res/Environment/Level_FB_MaterialSample/Props/Material/MI_EV_LayeredRock.mat";
        //         // string materialMaterialName = AssetDatabase.LoadAssetAtPath<Material>(materialPathInProject).name;
        //         // string materialShaderName = AssetDatabase.LoadAssetAtPath<Material>(materialPathInProject).shader.name;
        //         // string[] material = ReadFileInProject(materialPathInProject);
        //         // string[] materialNoKeywords = CleanKeywords(material);
        //         // string[] properties = FetchProperties(material);
        //         // string[] k = Array.Empty<string>();
        //         // string[] p = Array.Empty<string>();
        //         // PropertiesToKeywords(properties, materialShaderName, ref k, ref p);
        //         // string[] materialWithNewP = AddStringArrayToStringArray(materialNoKeywords, p, "    m_Floats:");
        //         // string[] materialWithNewKP = AddStringArrayToStringArray(materialWithNewP, k, "  m_ValidKeywords:");
        //         // string[] materialNewShader = ResetParentAndShader(materialWithNewKP);
        //         // SaveToFile(materialNewShader, materialMaterialName);
        //     }
        // }
        
        private static string[] ResetParentAndShader(string[] strings)
        {
            List<string> r = new List<string>();
            for (int i = 0; i < strings.Length; i++)
            {
                if (strings[i].Contains("m_Shader"))
                {
                    r.Add("  m_Shader: {fileID: 4800000, guid: 5ca1b500b4895814cb6bd669f0400d88, type: 3}");
                    continue;
                }
                if (strings[i].Contains("m_Parent"))
                {
                    r.Add("  m_Parent: {fileID: 2100000, guid: c6b6747b6ae355b4fab18207f4c9035b, type: 2}");
                    continue;
                }
                r.Add(strings[i]);
            }
            return r.ToArray();
        }
        private static string[] FetchProperties(string[] strings)
        {
            string startMarker = "m_SavedProperties:";
            string endMarker = " m_BuildTextureStacks:";
            bool isInsideMarkers = false;

            List<string> result = new List<string>();
            foreach (string str in strings)
            {
                if (str.Contains(startMarker))
                {
                    isInsideMarkers = true;
                    continue;
                }
                if (isInsideMarkers)
                {
                    if (str.Contains(endMarker))
                    {
                        isInsideMarkers = false;
                        continue;
                    }
                    result.Add(str);
                }
            }
            return result.ToArray();
        }
        private static string[] CleanKeywords(string[] strings)
        {
            string[] cleanValidKeywords = CleanByMarkers(strings, "m_ValidKeywords", "m_InvalidKeywords");
            string[] cleanInvalidKeywords = CleanByMarkers(cleanValidKeywords, "m_InvalidKeywords", "m_LightmapFlags");
            return cleanInvalidKeywords;
        }
        private static void PropertiesToKeywords(string[] strings, string materialShaderName, ref string[] keywords, ref string[] properties)
        {
            List<KeywordsTable> keywordsTables = new List<KeywordsTable>();
            string[] keywordsTablesFile = ReadFileInProject("Assets/Editor/MaterialUtils/KeywordsTable.txt");
            for (int i = 1; i < keywordsTablesFile.Length; i++)
            {
                KeywordsTable table = new KeywordsTable();
                table.KeywordsTableString = keywordsTablesFile[i];
                if (!materialShaderName.Contains(table.ShaderName)) { continue; }
                keywordsTables.Add(table);
            }
            //
            List<string> k = new List<string>();
            List<string> p = new List<string>();
            for (int i = 0; i < strings.Length; i++)
            {
                for (int j = 0; j < keywordsTables.Count; j++)
                {
                    if (!strings[i].Contains(keywordsTables[j].Properties)) { continue; }
                    if (int.Parse(strings[i].Trim().Split(':')[1]) != keywordsTables[j].PropertiesValue) { continue; }
                    k.Add($"  - {keywordsTables[j].Keyword}");
                    p.Add($"    - {keywordsTables[j].NewProperties}: {keywordsTables[j].NewPropertiesValue}");
                }
            }
            keywords = k.ToArray();
            properties = p.ToArray();
        }
        private static string[] AddStringArrayToStringArray(string[] sourceString, string[] additionalStrings, string cut)
        {
            int index = Array.IndexOf(sourceString, cut) + 1;
            // Get Head
            string[] head = new string[index];
            Array.Copy(sourceString, head, index);
            // Get Tail
            List<string> t = new List<string>();
            for (int i = index; i < sourceString.Length; i++)
            {
                t.Add(sourceString[i]);
            }
            string[] tail = t.ToArray();
            // Merge
            return head.Concat(additionalStrings).ToArray().Concat(tail).ToArray();
        }
        #region Urtils
        private static string[] ReadFileInProject(string path)
        {
            return System.IO.File.ReadAllLines(path);
        }
        private static void PrintStringArrayAsDebug(string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                Debug.Log(strings[i]);
            }
        }
        private static string[] CleanByMarkers(string[] strings, string startMarker, string endMarker)
        {
            List<string> extractedStrings = new List<string>();
            bool isInsideMarkers = false;

            foreach (string str in strings)
            {
                if (str.Contains(startMarker))
                {
                    isInsideMarkers = true;
                    extractedStrings.Add(str);
                }
                if (str.Contains(endMarker))
                {
                    isInsideMarkers = false;
                }
                
                if (isInsideMarkers)
                {
                    continue;
                }
                extractedStrings.Add(str);
            }
            return extractedStrings.ToArray();
        }
        private static void SaveToFile(string[] strings, string name, string path)
        {
            string targetDirectory = path;
            string fileName = $"{name}.txt";
            string filePath = Path.Combine(targetDirectory, fileName);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (string line in strings)
                {
                    writer.WriteLine(line);
                }
            }
        }
        private struct KeywordsTable
        {
            public string Properties;
            public int PropertiesValue;
            public string Keyword;
            public string NewProperties;
            public int NewPropertiesValue;
            public string ShaderName;
            public string KeywordsTableString
            {
                set
                {
                    string str = value.Trim();
                    string[] splitString = str.Split('@');
                    Properties = splitString[0].Trim();
                    PropertiesValue = int.Parse(splitString[1].Trim());
                    Keyword = splitString[2].Trim();
                    NewProperties = splitString[3].Trim();
                    NewPropertiesValue = int.Parse(splitString[4].Trim());
                    ShaderName = splitString[5].Trim();
                }
            }
        }
        #endregion
    }
}
