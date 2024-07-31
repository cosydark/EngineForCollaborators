using System.Collections.Generic;
using HoudiniEngineUnity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.LODGeneration
{
    public class SkinnedMeshLODManager
    {
        // private bool protection = false;
        // private int matMergeFromLevel = 2;

        public static string Logger = "";
        private static GameObject HDARoot;
        private static int maxLODCount = 6;
        
        public string GenerateLODTransitionDistanceAndApply(GameObject fbx, int LODCount, List<int> polyReductionRatio, bool generateVFX)
        {
            Selection.activeGameObject = null;

            bool result = GenerateLODGroupAsFBX(fbx, LODCount, polyReductionRatio, generateVFX);
            if (!result)
            {
                Logger += "生成失败！\n";
                return Logger;
            }
            
            return Logger;
        }
        
        private bool GenerateLODGroupAsFBX(GameObject fbx, int LODCount, List<int> polyReductionRatio, bool generateVFX)
        {
            //TODO xiaoliu
            //输入fbx绝对路径
            string assetPath = AssetDatabase.GetAssetPath(fbx);
            Debug.Log("assetPath: " + assetPath);
            string absPath = Application.dataPath.Replace("Assets", assetPath);
            if (!absPath.EndsWith(".fbx") && !absPath.EndsWith(".FBX"))
            {
                Logger += "Error!!! 对象不是FBX格式文件。\n";
                return false;
            }
            
            var session = HEU_SessionManager.GetOrCreateDefaultSession();
        
            HEU_Logger.Log(session.GetSessionData().SessionID.ToString());
            
            string hdaPath = "Packages/com.funplus.worldx.pcg-flow/Content/WX_HoudiniPackage/otls/Toolkit/Common_LOD_Generator.hda";
        
            string fullPath = HEU_AssetDatabase.GetAssetFullPath(hdaPath);
            GameObject HDA = HEU_HAPIUtility.InstantiateHDA(fullPath, Vector3.zero, session, false);

            // gameObjectName = gameObject.name;
            HEU_HoudiniAssetRoot houdiniAssetRoot = HDA.GetComponent<HEU_HoudiniAssetRoot>();
            HEU_HoudiniAsset HDAasset = houdiniAssetRoot.HoudiniAsset;

            // ToDo
            HDAasset.Parameters.SetStringParameterValue("file_in1", absPath);
            // for character
            HDAasset.Parameters.SetBoolParameterValue("isCH", true);
            //ToDo
            HDAasset.Parameters.SetBoolParameterValue("i_want_a_vfx_mesh", generateVFX);
            HDAasset.Parameters.SetIntParameterValue("CH_LODCount", LODCount);
            float[] polyRatioArray = new float[maxLODCount-1];
            for (int i = 1; i < LODCount; i++)
            {
                polyRatioArray[i-1] = polyReductionRatio[i];
            }
            HDAasset.Parameters.SetFloatParameterValues("ch_percentage", polyRatioArray);

            if (HDAasset.RequestCook())
            {
                Logger += "HDA cook successfully.\n";
            }

            Object.DestroyImmediate(HDARoot);
            // lodModel.callback
            return true;
        }
    }
}