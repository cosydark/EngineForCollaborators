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
        public static void Initialize(Texture2D texture2D, RenderTexture renderTexture) => DispatchToRenderTexture(texture2D, renderTexture, 0);
        public static void GetGrayValue(RenderTexture source, Vector4 averageValue) => ModifyGrayValueRange(source, averageValue);
        public static Vector4 GetAverage(RenderTexture source) => MeasureAveragePixelValue(source);
        
        // Measure Average Pixel Value
        private static readonly string ComputeShaderPath = "Assets/Editor/TextureUtility/CS_TextureUtility.compute";
        private static readonly int MeasureSourceTexture = Shader.PropertyToID("_Measure_SourceTexture");
        private static readonly int MeasurePixelColor = Shader.PropertyToID("_Measure_PixelColor");
        private static readonly int MeasureResolution = Shader.PropertyToID("_Measure_Resolution");
        private static Vector4 MeasureAveragePixelValue(RenderTexture source)
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
            List<Vector4> value = new List<Vector4>();
            Vector4 total = Vector4.zero;
            for (int i = 0; i < pixelColors.Length; i++)
            {
                Vector4 v = pixelColors[i].Color;
                value.Add(v);
                total += v;
            }
            colorBuffer.Release();
            total = total / pixelColors.Length;
            return total;
        }
        
        private struct PixelColor
        {
            public Vector4 Color;
        }
        // Dispatch To RenderTexture
        private static readonly int SourceTexture = Shader.PropertyToID("_Initialize_SourceTexture");
        private static readonly int TextureIO = Shader.PropertyToID("_Initialize_TextureIO");
        private static void DispatchToRenderTexture(Texture2D source, RenderTexture renderTexture, int channelIndex)
        {
            ComputeShader cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputeShaderPath);
            cs.SetTexture(InitializeKernel, SourceTexture, source);
            cs.SetTexture(InitializeKernel, TextureIO, renderTexture);
            cs.Dispatch(InitializeKernel, renderTexture.width / 16, renderTexture.height / 16, 1);
        }
        // Get Gray Scale Result
        private static readonly int ModifyGrayValueRange_TextureIO = Shader.PropertyToID("_ModifyGrayValueRange_TextureIO");
        private static readonly int ModifyGrayValueRange_AverageValue = Shader.PropertyToID("_ModifyGrayValueRange_AverageValue");
        private static void ModifyGrayValueRange(RenderTexture source, Vector4 averageValue)
        {
            ComputeShader cs = AssetDatabase.LoadAssetAtPath<ComputeShader>(ComputeShaderPath);
            cs.SetTexture(ModifyGrayValueRangeKernel, ModifyGrayValueRange_TextureIO, source);
            cs.SetVector(ModifyGrayValueRange_AverageValue, averageValue);
            cs.Dispatch(ModifyGrayValueRangeKernel, source.width / 16, source.height / 16, 1);
        }
        private static readonly int InitializeKernel = 0;
        private static readonly int MeasureMinMaxPixelValueKernel = 1;
        private static readonly int ModifyGrayValueRangeKernel = 2;
        // Save To Disk
        public static void SaveRenderTextureToFile(RenderTexture renderTexture, string filePath)
        {
            var oldRt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false, true);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(filePath, tex.EncodeToTGA());
            RenderTexture.active = oldRt;
        }

    }
}

