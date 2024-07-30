using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace TextureUtilities
{
    public class TextureUtility
    {
        public static void SplitR(Texture2D texture2D, RenderTexture renderTexture) => RGBASplit(texture2D, renderTexture, 0);
        public static void SplitG(Texture2D texture2D, RenderTexture renderTexture) => RGBASplit(texture2D, renderTexture, 1);
        public static void SplitB(Texture2D texture2D, RenderTexture renderTexture) => RGBASplit(texture2D, renderTexture, 2);
        public static void SplitA(Texture2D texture2D, RenderTexture renderTexture) => RGBASplit(texture2D, renderTexture, 3);
        public static Vector2 MeasureMinMaxPixel(Texture source, int channelIndex, ref float average) => MeasureMinMaxPixelValue(source, channelIndex, ref average);
        private static readonly string ComputeShaderPath = "Assets/Editor/TextureUtility/CS_TextureUtility.compute";
        
        private static readonly int MeasureSourceTexture = Shader.PropertyToID("_Measure_SourceTexture");
        private static readonly int MeasurePixelColor = Shader.PropertyToID("_Measure_PixelColor");
        private static readonly int MeasureResolution = Shader.PropertyToID("_Measure_Resolution");
        private static Vector2 MeasureMinMaxPixelValue(Texture source, int channelIndex, ref float average)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Measure Texture";
            PixelColor[] pixelColors = new PixelColor[source.width * source.width];
            GraphicsBuffer colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pixelColors.Length, Marshal.SizeOf(typeof(PixelColor)));
            colorBuffer.SetData(pixelColors);
            ComputeShader cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputeShaderPath);
            int x = (int)Mathf.Ceil(source.width * source.width / 1024.0f);
            cmd.SetComputeTextureParam(cs, MeasureMinMaxPixelValueKernel, MeasureSourceTexture, source);
            cmd.SetComputeBufferParam(cs, MeasureMinMaxPixelValueKernel, MeasurePixelColor, colorBuffer);
            cmd.SetComputeIntParam(cs, MeasureResolution, source.width);
            cmd.DispatchCompute(cs, MeasureMinMaxPixelValueKernel, x, 1, 1);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            // Fetch Data
            colorBuffer.GetData(pixelColors);
            List<float> value = new List<float>();
            float total = 0;
            for (int i = 0; i < pixelColors.Length; i++)
            {
                float v = GetPixelValueByIndex(pixelColors[i].Color, channelIndex);
                value.Add(v);
                total += v;
            }
            colorBuffer.Release();
            average = total / pixelColors.Length;
            Vector2 minMax = new Vector2(value.Min(), value.Max());
            return minMax;
        }
        
        private static readonly int SourceTexture = Shader.PropertyToID("_RGBASplit_SourceTexture");
        private static readonly int TextureIO = Shader.PropertyToID("_RGBASplit_TextureIO");
        private static readonly int ChannelIndex = Shader.PropertyToID("_RGBASplit_ChannelIndex");
        private static void RGBASplit(Texture2D source, RenderTexture renderTexture, int channelIndex)
        {
            ComputeShader cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputeShaderPath);
            cs.SetTexture(RGBASplitKernel, SourceTexture, source);
            cs.SetTexture(RGBASplitKernel, TextureIO, renderTexture);
            cs.SetInt(ChannelIndex, channelIndex);
            cs.Dispatch(RGBASplitKernel, renderTexture.width / 16, renderTexture.height / 16, 1);
        }
        private static float GetPixelValueByIndex(Color color, int channelIndex)
        {
            return channelIndex switch
            {
                0 => color.r,
                1 => color.g,
                2 => color.b,
                3 => color.a,
                _ => 0f
            };
        }
        private static float GetPixelValueByIndex(Vector4 color, int channelIndex)
        {
            return channelIndex switch
            {
                0 => color.x,
                1 => color.y,
                2 => color.z,
                3 => color.w,
                _ => 0f
            };
        }
        private struct PixelColor
        {
            public Vector4 Color;
        }
        private static readonly int RGBASplitKernel = 0;
        private static readonly int MeasureMinMaxPixelValueKernel = 1;
        private static readonly int InitialKernel = 2;
    }
}

