using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Editor.PrefabCreator.Module;
using HoudiniEngineUnity;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Editor.LODGeneration
{
    [Serializable, Toggle("preprocess")]
    public class RawDataPreprocess
    {
        public bool preprocess;
        [PropertyRange(70, 100)]
        public int polyReduceRatio;
    }
    
    public class StaticMeshLODManager: OdinEditorWindow
    {
        [BoxGroup("LOD Generation", centerLabel: true)]
        [ShowInInspector] [HideLabel] [PreviewField(160, ObjectFieldAlignment.Center)]
        public static GameObject fbx;
        
        [BoxGroup("LOD Generation", centerLabel: true)]
        [InfoBox("各类数据的权重，范围[0~10]，不同模板数值不同")] [OnValueChanged("UpdateParameter")]
        public static float maskWeight = 0;
        
        [BoxGroup("LOD Generation", centerLabel: true)] [OnValueChanged("UpdateParameter")]
        public static float normalWeight = 0;
        
        [BoxGroup("LOD Generation", centerLabel: true)] [OnValueChanged("UpdateParameter")]
        public static float uvWeight = 0;
        
        [BoxGroup("LOD Generation", centerLabel: true)] [OnValueChanged("UpdateParameter")]
        public static float edgeWeight = 0;
        
        [BoxGroup("LOD Generation", centerLabel: true)]
        [InfoBox("LOD总级数，4级表示LOD0~LOD3")] [ShowInInspector, PropertyRange(2, 6)] [OnValueChanged("UpdateParameter")]
        public static int LODCount = 4;
        
        [BoxGroup("LOD Generation", centerLabel: true)]
        [InfoBox("控制各级的屏占比（整体变化），数值越大即越早切换低模")] [ShowInInspector, PropertyRange(0.1, 1.5)]
        public static float offset = 1;
        
        [BoxGroup("LOD Generation", centerLabel: true)]
        [InfoBox("指定从第n级开始更新lod，数值2表示以LOD2作为基准更新LOD3及后续")] [ShowInInspector, PropertyRange(0, 4)]
        public static int lodStartIndex = 0;

        [ToggleGroup("LOD0Preprocess", "是否对LOD0实施减面")] [ShowInInspector]
        public static bool LOD0Preprocess = false;
        //public static RawDataPreprocess rawDataPreprocess = new RawDataPreprocess(){preprocess = false, polyReduceRatio = 100};
        
        [ToggleGroup("LOD0Preprocess")] [InfoBox("LOD0减面比例")] [ShowInInspector, PropertyRange(70, 100)]
        public static int LOD0PolyReduce = 100;
        
        private static int resolution = 2048;
        private static float fov = 90;
        private static float nearPlaneDistance = 0.15f;
        private static int cullPixelThreshold = 15;
        private static float transitionTime = 3;// complete LOD transition in 1s
        private static float moveSpeed = 1;// meter / second
        private static float transitionThresholdMin = 0.1f;
        private static float transitionThresholdMax = 0.3f;
        private static float transitionThresholdCulling = 0.05f;
        private float[] polyreductionRatio = new float[5] { 0.5f, 0.25f, 0.12f, 0.6f, 0.3f };
        
        private static string assetPath = "";
        private static LODModel lodModel = null;
        private static GameObject HDARoot;
        
        private void UpdateParameter()
        {
            lodModel.lodPara.edgeWeight = edgeWeight;
            lodModel.lodPara.maskWeight = maskWeight;
            lodModel.lodPara.normalWeight = normalWeight;
            lodModel.lodPara.uvWeight = uvWeight;
            lodModel.lodPara.LODCount = LODCount;
            lodModel.lodPara.factor = offset;
        }
        
        [Button("生成LOD并保存到本地", ButtonSizes.Medium)]
        public void GenerateLODTransitionDistanceAndApply()
        {
            InitData();

            Selection.activeGameObject = null;

            bool result;
            result = GenerateLODGroupAsFBX();
            if (!result)
            {
                Logger += "生成失败！\n";
                return;
            }

            result = ImportFBXAndCalculateScreenRatio();
            if (!result)
            {
                Logger += "生成失败！\n";
            }
        }

        [Button("关闭窗口", ButtonSizes.Medium)]
        public void CloseWindow()
        {
            GetWindow<StaticMeshLODManager>().Close();
        }
        
        public void PreSetParameters(string fbxPath,LODModel lModel)
        {
            assetPath = fbxPath;
            lodModel = lModel;
            offset = 1;
            lodStartIndex = 0;
            Logger = "";
            LoadParameter();
            this.Repaint();
        }
        
        public static void LoadParameter()
        {
            edgeWeight = lodModel.lodPara.edgeWeight;
            maskWeight = lodModel.lodPara.maskWeight;
            normalWeight = lodModel.lodPara.normalWeight;
            uvWeight = lodModel.lodPara.uvWeight;
            LODCount = lodModel.lodPara.LODCount;
            offset = lodModel.lodPara.factor;
            fbx = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        private bool GenerateLODGroupAsFBX()
        {
            // precheck parameter
            if (lodStartIndex >= LODCount - 1)
            {
                Logger += "Error!!! 请设置正确的参数： lodStartIndex !!!\n";
                return false;
            }
            
            // 输入fbx绝对路径
            string absPath = Application.dataPath.Replace("Assets", assetPath);
            if (!absPath.EndsWith(".fbx") && !absPath.EndsWith(".FBX"))
            {
                Logger += "Error!!! 对象不是FBX格式文件。\n";
                return false;
            }
            
            int[] submeshSetArray = new int[16];
            for (int i = 0; i < submeshSetArray.Length; i++)
            {
                submeshSetArray[i] = LODCount;
            }
            
            var session = HEU_SessionManager.GetOrCreateDefaultSession();
        
            HEU_Logger.Log(session.GetSessionData().SessionID.ToString());
            
            string hdaPath = "Packages/com.funplus.worldx.pcg-flow/Content/WX_HoudiniPackage/otls/Toolkit/Common_LOD_Generator.hda";
        
            string fullPath = HEU_AssetDatabase.GetAssetFullPath(hdaPath);
            GameObject HDA = HEU_HAPIUtility.InstantiateHDA(fullPath, Vector3.zero, session, false);

            // gameObjectName = gameObject.name;
            HEU_HoudiniAssetRoot houdiniAssetRoot = HDA.GetComponent<HEU_HoudiniAssetRoot>();
            HEU_HoudiniAsset HDAasset = houdiniAssetRoot.HoudiniAsset;

            HDAasset.Parameters.SetStringParameterValue("file_in1", absPath);
            HDAasset.Parameters.SetFloatParameterValue("uv_weight", uvWeight);
            HDAasset.Parameters.SetFloatParameterValue("mask_weight", maskWeight);
            HDAasset.Parameters.SetFloatParameterValue("normal_weight", normalWeight);
            HDAasset.Parameters.SetFloatParameterValue("edge_weight", edgeWeight);
            HDAasset.Parameters.SetIntParameterValue("LODCount", LODCount);
            HDAasset.Parameters.SetIntParameterValue("startIndex", lodStartIndex);
            HDAasset.Parameters.SetIntParameterValue("LOD0_ratio", LOD0Preprocess ? LOD0PolyReduce : 100);
            if (HDAasset.RequestCook())
            {
                Logger += "HDA cook successfully.\n";
            }
            
            return true;
        }
        
        private bool ImportFBXAndCalculateScreenRatio()
        {
            HEU_SessionManager.GetOrCreateDefaultSession();
            
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();

            // import FBX
            AssetDatabase.ImportAsset(assetPath);
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            else
            {
                Logger += "Error!!! FBX导入失败。\n";
                return false;
            }
            
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            
            // achieve all meshfilters
            List<Mesh>[] lodMeshList = new List<Mesh>[LODCount];//in order(0-1-2-3)
            for (int i = 0; i < lodMeshList.Length; i++)
            {
                lodMeshList[i] = new List<Mesh>();
            }
            MeshFilter[] meshFilters = fbxObj.GetComponentsInChildren<MeshFilter>();
            
            // group lod mesh by name
            try
            {
                foreach (var mf in meshFilters)
                {
                    string name = mf.gameObject.name;
                    if (!name.Contains("LOD"))
                        continue;

                    int level;
                    if (name.ToLower().EndsWith("_modified"))
                    {
                        level = int.Parse(name[name.Length - 10].ToString());
                    }
                    else
                    {
                        string[] names = name.Split("LOD");
                        level = int.Parse(names[names.Length-1]);
                    }
                    if (level < 0 || level > 9)
                        continue;//skip others
                    
                    lodMeshList[level].Add(mf.sharedMesh);
                    Debug.Log("Mesh: " + mf.sharedMesh.name + ", level: " + level + ", tri: " + mf.sharedMesh.triangles.Length/3);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            // check polyreduction 
            float triangleCount = lodMeshList[0].Sum(mesh => mesh.triangles.Length / 3);
            for (int i = 1; i < LODCount; i++)
            {
                float count = lodMeshList[i].Sum(mesh => mesh.triangles.Length / 3);
                float ratio = count / triangleCount;
                if (Mathf.Abs(ratio - polyreductionRatio[i - 1]) > 0.1f)
                    Logger += string.Format("Warning!!! LOD {0} 减面比例不在预期内！请检查！\n", i);
            }
            
            // in order(0-1-2-3), including cull ratio
            float[] ratioValueArray = new float[LODCount];

            // compute cull ratio
            List<Mesh> lastMesh = lodMeshList[lodMeshList.Length - 1];
            int rendererCount = lodMeshList[0].Count;
            Bounds lastMeshBound = new Bounds();
            for (int i = 0; i < rendererCount; i++)
            {
                Mesh mesh = lastMesh[i];
                lastMeshBound.Encapsulate(mesh.bounds);
            }
            float lastMeshRadius = Mathf.Sqrt(Mathf.Pow(lastMeshBound.size.x / 2, 2) + Mathf.Pow(lastMeshBound.size.y / 2, 2) + Mathf.Pow(lastMeshBound.size.z / 2, 2));
            float cullDistance = (lastMeshRadius * resolution) / (2 * cullPixelThreshold * Mathf.Tan(fov / 2.0f));//Zmin (assume object in the center of frustum)
            cullDistance = lastMeshRadius + cullDistance;
            float cullRatio = CalculateAppropriateScreenPercentage(cullDistance, lastMeshRadius);
            // Logger += string.Format("当前cullratio：{0}。\n", cullRatio);

            // assume uniform screen ratio
            float baseNum;
            if (offset == 1.0f)
                baseNum = (1 - cullRatio) / (LODCount);
            else
            {
                float temp = 0;
                for (int i = 0; i < LODCount; i++)
                {
                    temp += Mathf.Pow(offset, i);
                }

                baseNum = (1 - cullRatio) / temp;
            }
            
            for (int i = 0; i < LODCount; i++)
            {
                float interval = 0;
                for (int j = i; j >= 0; j--)
                {
                    interval += (baseNum * Mathf.Pow(offset, j));
                }
                ratioValueArray[i] = 1 - interval;
                // Logger += string.Format("LOD {0} 切换下一级占比：{1}。\n", i, ratioValueArray[i]);
            }
            List<float> screenPercentageList = ratioValueArray.ToList();
            
            // check validity of LODGroup
            for (int i = 1; i < screenPercentageList.Count; i++)
            {
                if (screenPercentageList[i] > screenPercentageList[i - 1])
                {
                    Logger += "Error!!! 屏占比不合法。\n";
                    return false;
                }
            }

            List<float> transitionWidthList = new List<float>();
            for (int i = 0; i < screenPercentageList.Count; i++)
            {
                float transitionWidth = transitionTime * moveSpeed / CalculateDistanceFromScreenPercentage(screenPercentageList[i], lastMeshRadius);
                Logger += string.Format("LOD {0}, Transition width: {1}\n", i, transitionWidth);

                transitionWidth = Mathf.Min(transitionWidth, transitionThresholdMax);
                if (transitionWidth < transitionThresholdMin)
                {
                    if (i >= 3)
                        transitionWidth = transitionThresholdCulling;
                    else 
                        transitionWidth = transitionThresholdMin;
                }
                
                transitionWidthList.Add(transitionWidth);
            }
            
            DateTime now = DateTime.Now;
            importer.userData = "ModifyData: " + now.Year + "," + now.Month + "," + now.Day;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();

            if (importer != null)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
            
            Logger += "成功生成LOD并计算得到屏占比。\n—————————————————\n";
            Debug.Log("成功生成LOD!");
            
            List<int> submeshSetList = new List<int>();
            lodModel.AfterLODGenerated(screenPercentageList, transitionWidthList, submeshSetList);
            
            DestroyImmediate(HDARoot);
            
            sw1.Stop();
            //Debug.Log("sum time is : " + sw1.ElapsedMilliseconds / 1000);
            return true;
        }

        [Title("日志", bold: true)]
        [HideLabel]
        [MultiLineProperty(15)]
        [ShowInInspector]
        public static string Logger = "";
        
        private static float CalculateDistanceFromScreenPercentage(float ratio, float radius)
        {
            // 计算屏幕高度的一半对应的像素大小
            float screenSizeY = 2 * nearPlaneDistance * Mathf.Tan(fov / 2.0f);

            // 使用屏幕比例来计算物体在屏幕上的像素大小
            float pixelSize = ratio * screenSizeY;

            // 计算物体的tanAngle
            float tanAngle = pixelSize / (2 * nearPlaneDistance);

            // 由于tanAngle = sinAngle / cosAngle，且sinAngle = opposite / hypotenuse = radius / distance
            // 我们可以解出distance = radius / sinAngle
            // 首先计算sinAngle的平方
            float sinAngleSquared = Mathf.Pow(tanAngle, 2) / (1 + Mathf.Pow(tanAngle, 2));

            // 然后计算sinAngle
            float sinAngle = Mathf.Sqrt(sinAngleSquared);

            // 最后计算distance
            float distance = radius / sinAngle;

            return distance;
        }
        
        private static float CalculateAppropriateScreenPercentage(float distance, float radius)
        {
            //compute screen percentage
            float sinAngle = radius / distance;
            float cosAngle = Mathf.Sqrt(1 - Mathf.Pow(sinAngle, 2));
            float tanAngle = sinAngle / cosAngle;
            float pixelSize = 2 * nearPlaneDistance * tanAngle;
            float screenSizeY = 2 * nearPlaneDistance * Mathf.Tan(fov / 2.0f);
            float ratio = pixelSize / screenSizeY;

            return ratio;
        }
        
        public static float CalculateGeometricError(Mesh rawMesh, Mesh mesh, KDTree kdTree, KDTree lodkdTree)
        {
            //compute average error value of all vertex
            Vector3[] vertices = mesh.vertices;
            Vector3[] originalVertices = rawMesh.vertices;
            
            //from low-detail to high-detail
            float sum1 = 0;
            foreach (var vert in vertices)
            {
                (double diff, Vector3 nearestPoint) = kdTree.SearchNearestNeighbor(vert);
                
                sum1 += (float)diff;
            }
            float geoError1 = sum1 / vertices.Length;
            
            //from high-detail to low-detail
            float sum2 = 0;
            foreach (var vert in originalVertices)
            {
                (double diff, Vector3 nearestPoint) = lodkdTree.SearchNearestNeighbor(vert);

                sum2 += (float)diff;
            }
            float geoError2 = sum2 / originalVertices.Length;
            
            return Mathf.Max(geoError1, geoError2)/*(geoError1 + geoError2) / 2*/;
        }
        
        private static bool InitData()
        {
            Logger = "";
            MeshFilter[] mfs = fbx.GetComponentsInChildren<MeshFilter>();
            if (mfs != null && mfs.Length >= 1)
            {
                LODGroup lodGroup = fbx.GetComponentInChildren<LODGroup>();
                if (lodGroup != null)
                {
                    if (lodGroup.GetLODs().Length == 0 || lodGroup.GetLODs()[0].renderers.Length == 0)
                    {
                        Logger += "Error!!! FBX的lodgroup格式不正确。\n";
                        return false;
                    }
                }
            }
            else
                return false;
            
            return true;
        }
    }
}