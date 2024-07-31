using System;
using System.Collections.Generic;
using System.Linq;
using Editor.ImpostorGeneration.AmplifyImpostors.Scripts;
using Editor.PrefabCreator.Module;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;


namespace Editor.ImpostorGeneration
{
#if XRENDER
    public class ImpostorManager: OdinEditorWindow
    {
        [BoxGroup("Impostor Generation", centerLabel: true)]
        [ShowInInspector] [HideLabel] [PreviewField(160, ObjectFieldAlignment.Center)]
        public static GameObject currentPrefab;
        
        [BoxGroup("Impostor Generation", centerLabel: true)] [ShowInInspector, PropertyRange(4, 10)]
        public int frames = 10;
        
        [HideInInspector]
        public int padding = 32;

        private List<int> TexSizeList = new List<int>()
        {
            32, 64, 128, 256, 512, 1024
        };
        
        [BoxGroup("Impostor Generation", centerLabel: true)] [ValueDropdown("TexSizeList")]
        public int texSize = 1024;
        
        [HideInInspector]
        public float tolerance = 0.75f;
        
        [HideInInspector]
        public float nomalScale = 0.01f;
        
        [HideInInspector]
        public int maxVertices = 8;
        
        private string assetPath = "";
        private static LODModel lodModel = null;
        private string shaderGuid = "";
        private imposterShaderType shaderType = imposterShaderType.Default;
        
        public enum imposterShaderType
        {
            Default,
            Building,
            Foliage
        }
        
        private readonly Dictionary<imposterShaderType, string> shaderGuidDic =
 new Dictionary<imposterShaderType, string>()
        {
            { imposterShaderType.Default, "a09ba80639693944f94e43be95a4946a" },
            { imposterShaderType.Building, "a09ba80639693944f94e43be95a4946a" },
            { imposterShaderType.Foliage, "5a9cf4a186619d04c94621ae965c3b27" }
        };
        
        private AmplifyImpostor impostorInstance;
        private AmplifyImpostorAsset impostorAsset;
        
        public void PreSetParameters(string prefabPath, GameObject prefab, LODModel lModel, imposterShaderType shaderType)
        {
            assetPath = prefabPath;
            lodModel = lModel;
            currentPrefab = prefab;
            //ToDo
            this.shaderType = shaderType;
            shaderGuid = shaderGuidDic[shaderType];
        }
        
        [Button("生成", ButtonSizes.Medium)]
        public void GenerateImpostor()
        {
            InitData();
            
            impostorInstance.DetectRenderPipeline();
            
            impostorInstance.RenderCombinedAlpha(impostorAsset);
            
            impostorInstance.GenerateAutomaticMesh(impostorAsset);
            EditorUtility.SetDirty(impostorInstance);
            
            EditorApplication.delayCall += DelayedBake;
        }
        
        void InitData()
        {
            impostorInstance = currentPrefab.GetComponent<AmplifyImpostor>();
            if (impostorInstance == null)
            {
                impostorInstance = currentPrefab.AddComponent<AmplifyImpostor>();
            }
            
            if (impostorInstance.RootTransform == null)
                impostorInstance.RootTransform = impostorInstance.transform;

            impostorInstance.ShaderGuid = shaderGuid;
            
            impostorAsset = impostorInstance.Data;
            if (impostorAsset == null)
            {
                impostorAsset = ScriptableObject.CreateInstance<AmplifyImpostorAsset>();
                impostorAsset.ImpostorType = ImpostorType.Octahedron;
                impostorAsset.SelectedSize = 2048;
                impostorAsset.LockedSizes = true;
                impostorAsset.TexSize.x = texSize;
                impostorAsset.TexSize.y = texSize;
                impostorAsset.DecoupleAxisFrames = false;
                impostorAsset.HorizontalFrames = frames;
                impostorAsset.VerticalFrames = frames;
                impostorAsset.PixelPadding = padding;
                impostorAsset.Tolerance = tolerance;
                impostorAsset.NormalScale = nomalScale;
                impostorAsset.MaxVertices = maxVertices;

                int index = assetPath.LastIndexOf('/');
                string parentPath = assetPath.Substring(0, index - 6);
                parentPath = parentPath.Replace("Environment", "Common/Impostor");
                
                // create each sub-folder from "Assets" folder
                string parentFolder = "Assets";
                string[] folders = parentPath.Split('/');
                foreach (string folder in folders)
                {
                    if (folder.Equals(parentFolder))
                        continue;
                        
                    // check if current folder exists
                    string currentFolderPath = $"{parentFolder}/{folder}";
                    if (!AssetDatabase.IsValidFolder(currentFolderPath))
                    {
                        // create it
                        AssetDatabase.CreateFolder(parentFolder, folder);
                    }
                    parentFolder = currentFolderPath;
                }
                
                string impostorAssetPath =
 parentPath + "/" + impostorInstance.transform.name.Replace("Prefab_", "") + "_Impostor.asset";
                AssetDatabase.CreateAsset(impostorAsset, impostorAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                impostorInstance.Data = impostorAsset;
            }
            impostorAsset.Preset =
 AssetDatabase.LoadAssetAtPath<AmplifyImpostorBakePreset>(AssetDatabase.GUIDToAssetPath(AmplifyImpostor.Preset));

            impostorInstance.m_lodReplacement = LODReplacement.InsertAfter;
            
            impostorInstance.LodGroup = impostorInstance.GetComponent<LODGroup>();
            if (impostorInstance.LodGroup != null)
            {
                LOD[] lods = impostorInstance.LodGroup.GetLODs();
                
                bool impostorExist = false;
                int lastLODRendererCount = lods[lods.Length - 1].renderers.Length;
                if (lastLODRendererCount == 1)
                {
                    string lastLOD = lods[lods.Length - 1].renderers[0].gameObject.name;
                    if (lastLOD.ToLower().EndsWith("_impostor"))
                        impostorExist = true;
                }

                int lastLODIndex = lods.Length - 1;
                if (impostorExist)
                {
                    Debug.Log("###Impostor exists!!!");
                    lastLODIndex--;
                }
                
                int vertexCount = 0;
                Renderer[] rend = lods[lastLODIndex].renderers;

                for (int i = 0; i < rend.Length; i++)
                {
                    if (rend[i] != null)
                    {
                        MeshFilter mf = rend[i].GetComponent<MeshFilter>();
                        if(mf != null && mf.sharedMesh != null)
                            vertexCount += mf.sharedMesh.vertexCount;
                    }
                }

                lastLODIndex = Mathf.Max(lastLODIndex, 1);

                // use the 2nd last mesh for bake
                for (int i = lastLODIndex - 1; i >= 0; i--)
                {
                    if(lods[i].renderers != null && lods[i].renderers.Length > 0)
                    {
                        impostorInstance.Renderers = lods[i].renderers;
                        break;
                    }
                }
                impostorInstance.m_insertIndex = lastLODIndex;
                Debug.Log("###Impostor Insert Index: " + impostorInstance.m_insertIndex);
                if (vertexCount < 8)
                    impostorInstance.m_lodReplacement = LODReplacement.InsertAfter;

                // delete old impostor and LOD[]
                if (impostorExist)
                {
                    Debug.Log("###Delete old Impostor: " + lods[lods.Length - 1].renderers[0].gameObject.name);
                    UnityEngine.Object.DestroyImmediate(lods[lods.Length - 1].renderers[0].gameObject);
                    
                    LOD[] newLOD = new LOD[lods.Length - 1];
                    for (int i = 0; i < newLOD.Length; i++)
                    {
                        newLOD[i] = lods[i];
                    }

                    newLOD[newLOD.Length - 1].screenRelativeTransitionHeight =
                        lods[lods.Length - 1].screenRelativeTransitionHeight;
                    impostorInstance.LodGroup.SetLODs(newLOD);
                }
            }
            else
            {
                impostorInstance.Renderers = impostorInstance.RootTransform.GetComponentsInChildren<Renderer>();
            }
            if (impostorInstance.Renderers == null)
                impostorInstance.Renderers = new Renderer[] { };
        }
        
        void DelayedBake()
        {
            try
            {
                impostorInstance.RenderAllDeferredGroups(impostorAsset);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogWarning("[AmplifyImpostors] Something went wrong with the baking process, please contact support@amplify.pt with this log message.\n" + e.Message + e.StackTrace);
            }
            bool createLodGroup = false;
            if (Preferences.GlobalCreateLodGroup)
            {
                LODGroup group = impostorInstance.RootTransform.GetComponentInParent<LODGroup>();
                if(group == null)
                    group = impostorInstance.RootTransform.GetComponentInChildren<LODGroup>();
                if(group == null && impostorInstance.LodGroup == null)
                    createLodGroup = true;
            }

            if (createLodGroup && impostorInstance.m_lastImpostor != null)
            {
                GameObject lodgo = new GameObject(impostorInstance.name + "_LODGroup");
                LODGroup lodGroup = lodgo.AddComponent<LODGroup>();
                lodGroup.transform.position = impostorInstance.transform.position;
                int hierIndex = impostorInstance.transform.GetSiblingIndex();

                impostorInstance.transform.SetParent(lodGroup.transform, true);
                impostorInstance.m_lastImpostor.transform.SetParent(lodGroup.transform, true);
                LOD[] lods = lodGroup.GetLODs();
                ArrayUtility.RemoveAt<LOD>(ref lods, 2);
                lods[0].fadeTransitionWidth = 0.5f;
                lods[0].screenRelativeTransitionHeight = 0.15f;
                lods[0].renderers = impostorInstance.RootTransform.GetComponentsInChildren<Renderer>();
                lods[1].fadeTransitionWidth = 0.5f;
                lods[1].screenRelativeTransitionHeight = 0.01f;
                lods[1].renderers = impostorInstance.m_lastImpostor.GetComponentsInChildren<Renderer>();
                lodGroup.fadeMode = LODFadeMode.CrossFade;
                lodGroup.animateCrossFading = true;
                lodGroup.SetLODs(lods);
                lodgo.transform.SetSiblingIndex(hierIndex);
            }

            EditorApplication.delayCall -= DelayedBake;

            UnityEngine.Object.DestroyImmediate(impostorInstance);

            ClearUnusedAsset();

            ResetAABB();
                
            SetImpostorHierarchy();
            
            // refresh prefab 
            lodModel.AfterImposterGenerated();
        }

        private void ResetAABB()
        {
            if (impostorInstance.LodGroup == null)
                return;

            LOD[] lods = impostorInstance.LodGroup.GetLODs();
            if (lods.Length > 1)
            {
                MeshFilter mf0 = lods[0].renderers[0].gameObject.GetComponent<MeshFilter>();
                Bounds bound = mf0.sharedMesh.bounds;

                MeshFilter mf = impostorInstance.m_lastImpostor.GetComponent<MeshFilter>();
                Mesh impostorMesh = mf.sharedMesh;
                string meshPath = AssetDatabase.GetAssetPath(impostorMesh);
                Mesh impostorMeshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (impostorMeshAsset != null)
                {
                    impostorMeshAsset.bounds = bound;
                    Debug.Log("###Impostor AABB is reset.");
                }
                AssetDatabase.SaveAssets();
            }
        }

        private void SetImpostorHierarchy()
        {
            if (impostorInstance.LodGroup == null)
                return;
            
            Transform lodTransform = impostorInstance.LodGroup.transform;
            Transform finalTransform = lodTransform;
            if (!lodTransform.name.ToLower().Equals("lod"))
            {
                int count = lodTransform.childCount;
                for (int i = 0; i < count; i++)
                {
                    if (lodTransform.GetChild(i).name.ToLower().Equals("lod"))
                    {
                        finalTransform = lodTransform.GetChild(i);
                        break;
                    }
                }
            }

            impostorInstance.m_lastImpostor.transform.name =
                "SM" + impostorInstance.LodGroup.transform.name.Replace("Prefab", "") + "_Impostor";
            if (impostorInstance.m_lastImpostor != null && finalTransform != null)
                impostorInstance.m_lastImpostor.transform.parent = finalTransform;
        }

        private void ClearUnusedAsset()
        {
            // find impostor material
            Material[] materials = currentPrefab.GetComponentsInChildren<Renderer>()
                .Select(renderer => renderer.sharedMaterial).ToArray();
            Material impostorMaterial = null;
            foreach (var mat in materials)
            {
                if (mat == null) 
                    continue;
                if (mat.name.ToLower().Contains("_impostor"))
                {
                    impostorMaterial = mat;
                    break;
                }
            }

            // delete unused textures
            if (impostorMaterial != null)
            {
                Texture AlbedoTex = impostorMaterial.GetTexture("_Albedo");
                if (AlbedoTex != null)
                {
                    string path = AssetDatabase.GetAssetPath(AlbedoTex);
                    string emissionPath = path.Replace("_CA", "_EmissionOcclusion");
                    AssetDatabase.DeleteAsset(emissionPath);
                    
                    string subsurfacePath = path.Replace("_CA", "_ORM");
                    if (shaderType != imposterShaderType.Foliage)
                    {
                        AssetDatabase.DeleteAsset(subsurfacePath);
                    }
                    else
                    {
                        /*float size = AlbedoTex.width;
                        //Debug.Log("Albedo Texture size is: " + size);
                        TextureImporter subTex = (TextureImporter)TextureImporter.GetAtPath(subsurfacePath);
                        subTex.maxTextureSize = (int)size / 2;
                        //Debug.Log("Texture size set to: " + subTex.maxTextureSize);
                        subTex.SaveAndReimport();*/
                    }
                }
            }
        }
    }
#endif
}