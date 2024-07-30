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
        private string LastTextureName = "initial";
        private Vector2 resolution = new Vector2(0, 0);
        private ComputeBuffer computeShader;
        private Channels channel;
        private RenderTexture renderTexture;
        
        [MenuItem("TA/Texture Remapping Tool")]
        public static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<TextureRemappingTool>() as TextureRemappingTool;
            window.minSize = new Vector2(720, 880 + 30);
            window.maxSize = new Vector2(720, 880 + 30);
            window.titleContent = new GUIContent("Texture Remapping Tool");
            window.ShowUtility();
        }

        private void OnEnable()
        {
            renderTexture = RenderTexture.GetTemporary(GetGrayValueRenderTextureDescriptor(new Vector2(1, 1)));
        }

        private void OnDestroy()
        {
            CoreUtils.Destroy(renderTexture);
        }

        private void Setup()
        {
            resolution.x = texture2D.width; resolution.y = texture2D.height;
            renderTexture.Release();
            renderTexture = RenderTexture.GetTemporary(GetGrayValueRenderTextureDescriptor(resolution));
            renderTexture.filterMode = FilterMode.Point;
            switch (channel)
            {
                case Channels.R:
                    TextureUtility.SplitR(texture2D, renderTexture);
                    break;
                case Channels.G:
                    TextureUtility.SplitG(texture2D, renderTexture);
                    break;
                case Channels.B:
                    TextureUtility.SplitB(texture2D, renderTexture);
                    break;
                case Channels.A:
                    TextureUtility.SplitA(texture2D, renderTexture);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                if (GUILayout.Button("Measure Range", GUILayout.Height(40), GUILayout.Width(140)))
                {
                    if (texture2D is null) {return;}
                    Setup();
                    float average = 0;
                    TextureUtility.MeasureMinMaxPixel(renderTexture, GetChannelIndexFromChannel(channel), ref average);
                    
                }
            EditorGUILayout.Space(3);
                if (GUILayout.Button("Save", GUILayout.Height(40), GUILayout.Width(140)))
                {
                }
            EditorGUILayout.Space(3);
                channel = (Channels)EditorGUILayout.EnumPopup("", channel, GUILayout.Width(90));
            EditorGUILayout.Space(3);
                EditorGUILayout.LabelField($"Texture Info: <{resolution.x}, {resolution.y}>", AddColor(Color.white));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            // Row 2
            EditorGUILayout.Space(3);
                // EditorGUILayout.LabelField($"Value Range: <{valueRange.x}, {valueRange.y}>", AddColor(GetColorFromChannel(channel)));
                if (texture2D != null)
                {
                    GUI.DrawTexture(new Rect(10, 150 + 50, 700, 700), renderTexture);
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
        private enum Channels {R ,G, B, A}

        private Color GetColorFromChannel(Channels channel)
        {
            return channel switch
            {
                Channels.R => Color.red,
                Channels.G => Color.green,
                Channels.B => Color.blue,
                Channels.A => Color.white,
                _ => Color.magenta
            };
        }
        private int GetChannelIndexFromChannel(Channels channel)
        {
            return channel switch
            {
                Channels.R => 0,
                Channels.G => 1,
                Channels.B => 2,
                Channels.A => 3,
                _ => 0
            };
        }
        private static GUIStyle AddColor(Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = color;
            return style;
        }
    }
}

