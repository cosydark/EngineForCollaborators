using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.LODGeneration
{
    public class LODFadeManager
    {
        private static int resolution = 2048;
        private static float fov = 90;
        private static float nearPlaneDistance = 0.15f;
        private static int cullPixelThreshold = 15;
        private static float transitionTime = 3;// complete LOD transition in 1s
        private static float moveSpeed = 1;// meter / second
        private static float transitionThreshold = 0.05f;
        
        [MenuItem("GameObject/SetLODCrossFade")]
        public static void ApplyLODCrossFade()
        {
            GameObject go = Selection.activeGameObject;
            LODGroup lodGroup = go.GetComponentInChildren<LODGroup>();
            if (lodGroup == null)
            {
                Debug.Log("Gameobject has no LODGroup!");
                return;
            }

            if (lodGroup.fadeMode != LODFadeMode.CrossFade)
                lodGroup.fadeMode = LODFadeMode.CrossFade;

            float objectSize = lodGroup.size;
            LOD[] lods = lodGroup.GetLODs();
            for (int i = 0; i < lods.Length; i++)
            {
                float transitionWidth = transitionTime * moveSpeed / CalculateDistanceFromScreenPercentage(lods[i].screenRelativeTransitionHeight, objectSize);
                Debug.LogFormat("LOD {0} transitionWidth is: {1}", i, transitionWidth);
                if (transitionWidth < transitionThreshold)
                {
                    break;
                }

                lods[i].fadeTransitionWidth = transitionWidth;
            }
            lodGroup.SetLODs(lods);
            AssetDatabase.SaveAssets();
            Debug.Log("LODCrossFade set successfully!");
        }
        
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

        private static float transitionThresholdMin = 0.1f;
        private static float transitionThresholdMax = 0.3f;
        private static float transitionThresholdCulling = 0.05f;
        
        [MenuItem("Tools/EnableLODCrossFade")]
        public static void OpenCrossFadeMode()
        {
            // 获取指定路径下所有的 Prefab 路径
            string[] prefabPaths = Directory.GetFiles("Assets/Res/Environment", "*.prefab", SearchOption.AllDirectories);

            int dealCount = 0;
            foreach (string prefabPath in prefabPaths)
            {
                string lowPath = prefabPath.ToLower();
                // 只处理建筑，石头，物件
                if (!lowPath.Contains("build") && !lowPath.Contains("rock") && !lowPath.Contains("stone") && !lowPath.Contains("props"))
                    continue;
                
                // 加载 Prefab 资源
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    // 获取 Prefab 中的 LODGroup 组件
                    LODGroup lodGroup = prefab.GetComponent<LODGroup>();
                    if (lodGroup != null)
                    {
                        // 设置 FadeMode
                        lodGroup.fadeMode = LODFadeMode.CrossFade;

                        LOD[] lods = lodGroup.GetLODs();
                        
                        Renderer[] lastRenderers = lods[lods.Length-1].renderers;
                        int rendererCount = lastRenderers.Length;
                        Bounds lastMeshBound = new Bounds();
                        for (int i = 0; i < rendererCount; i++)
                        {
                            if (lastRenderers[i] == null)
                            {
                                Debug.LogError("Renderer is null: " + lodGroup.gameObject.name);
                                continue;
                            }
                            lastMeshBound.Encapsulate(lastRenderers[i].bounds);
                        }
                        float lastMeshRadius = Mathf.Sqrt(Mathf.Pow(lastMeshBound.size.x / 2, 2) + Mathf.Pow(lastMeshBound.size.y / 2, 2) + Mathf.Pow(lastMeshBound.size.z / 2, 2));
                        
                        for (int i = 0; i < lods.Length; i++)
                        {
                            //lods[i].fadeTransitionWidth = 0.1f;
                            float transitionWidth = transitionTime * moveSpeed / CalculateDistanceFromScreenPercentage(lods[i].screenRelativeTransitionHeight, lastMeshRadius);
                            transitionWidth = Mathf.Min(transitionWidth, transitionThresholdMax);
                            if (transitionWidth < transitionThresholdMin)
                            {
                                // 如果是LOD3级之后，过小的过渡范围置为0.05f
                                if (i >= 3)
                                    transitionWidth = transitionThresholdCulling;
                                // 其他的LDO，置为0.1f
                                else 
                                    transitionWidth = transitionThresholdMin;
                            }

                            lods[i].fadeTransitionWidth = transitionWidth;
                        }
                        lodGroup.SetLODs(lods);
                        
                        // 标记 Prefab 为 'dirty' 以保存更改
                        EditorUtility.SetDirty(prefab);
                        dealCount++;
                    }
                }
            }

            // 保存所有更改到 Prefab
            AssetDatabase.SaveAssets();
            Debug.Log(dealCount + " LODGroup Fade Mode set to CrossFade for all prefabs in : Assets/Res/Environment");
        }
    }
}