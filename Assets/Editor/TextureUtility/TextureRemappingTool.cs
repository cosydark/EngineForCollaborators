using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TextureUtilities
{
        public class TextureRemappingTool : EditorWindow
    {
        // Unreal Private
        private Texture2D texture2D;
        // Real Private
        private Vector2 resolution = Vector2.zero;
        private Vector4 averageValue = Vector4.one;
        private ComputeBuffer computeShader;
        private RenderTexture renderTexture;
        
        [MenuItem("TA/Texture Remapping Tool")]
        public static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<TextureRemappingTool>() as TextureRemappingTool;
            window.minSize = new Vector2(720, 880);
            window.maxSize = new Vector2(720, 880);
            window.titleContent = new GUIContent("Texture Remapping Tool");
            window.ShowUtility();
        }

        private void OnEnable()
        {
            renderTexture = RenderTexture.GetTemporary(GetGrayValueRenderTextureDescriptor(new Vector2(1, 1)));
        }

        private void OnDestroy()
        {
            renderTexture.Release();
        }

        private void Setup()
        {
            resolution.x = texture2D.width; resolution.y = texture2D.height;
            renderTexture.Release();
            renderTexture = RenderTexture.GetTemporary(GetGrayValueRenderTextureDescriptor(resolution));
            renderTexture.filterMode = FilterMode.Point;
            TextureUtility.Initialize(texture2D, renderTexture);
        }
    
        private void OnGUI()
        {
            // Row 0
            EditorGUILayout.Space(3);
                texture2D = (Texture2D)EditorGUILayout.ObjectField("Texture", texture2D, typeof(Texture2D), false);
            EditorGUILayout.Space(3);
            // Row 1
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(3);
                if (GUILayout.Button("Compute", GUILayout.Height(40), GUILayout.Width(140)))
                {
                    if (texture2D is null) {return;}
                    Setup();
                    averageValue = TextureUtility.GetAverage(renderTexture);
                    TextureUtility.GetGrayValue(renderTexture, averageValue);
                }
            EditorGUILayout.Space(3);
                if (GUILayout.Button("Save", GUILayout.Height(40), GUILayout.Width(140)))
                {
                    TextureUtility.SaveRenderTextureToFile(renderTexture,
                        GetTexturePath(texture2D).Replace(texture2D.name, $"{texture2D.name}_New"));
                    AssetDatabase.Refresh();// Necessary?
                }
            EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"Texture Info: <{resolution.x}, {resolution.y}>", AddColor(Color.white));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            // Row 2
            EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"Average Value: <{averageValue.x}, {averageValue.y}, {averageValue.z}, {averageValue.w}>");
                if (texture2D != null)
                {
                    GUI.DrawTexture(new Rect(10, 150, 700, 700), renderTexture);
                }
                else
                {
                    EditorGUILayout.LabelField("Texture not found", EditorStyles.boldLabel);
                }
        }

        private RenderTextureDescriptor GetGrayValueRenderTextureDescriptor(Vector2 resolution)
        {
            return new RenderTextureDescriptor
            {
                width = (int)resolution.x,
                height = (int)resolution.y,
                volumeDepth = 1,
                dimension = TextureDimension.Tex2D,
                colorFormat = RenderTextureFormat.ARGBHalf,
                enableRandomWrite = true,
                msaaSamples = 1,
            };
        }
        private static GUIStyle AddColor(Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            return style;
        }
        static string GetTexturePath(Texture2D texture)
        {
            string relativePath = AssetDatabase.GetAssetPath(texture);
            string absolutePath = System.IO.Path.GetFullPath(relativePath);
            return absolutePath;
        }
    }
}

