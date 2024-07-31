#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    namespace FunPlus.WorldX.WorldPartition
    {
    [ExecuteInEditMode()]
    public class TerrainGridChecker : MonoBehaviour
    {
        public Camera targetCamera;
        public Material material;
        private Mesh mesh;

        private void OnEnable()
        {
                mesh = GetFullScreenTriangle();
                material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.funplus.worldx.world-partition/Editor/Shaders/TerrainGridChecker/TerrainGridChecker.mat");
                targetCamera = SceneView.lastActiveSceneView.camera;
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable() 
        {
                DestroyImmediate(mesh);
                SceneView.duringSceneGui -= OnSceneGUI;
        }
        
        
        void OnSceneGUI(SceneView sceneView)
        {
            if (mesh && material && targetCamera)
            {
                material.SetPass(0);
                Graphics.SetRenderTarget(targetCamera.targetTexture);
                Graphics.DrawMeshNow(mesh,  SceneView.lastActiveSceneView.camera.transform.localToWorldMatrix);
            }
            else
            {
                Debug.Log("缺少材质|Mesh|相机重新设置默认值",this);
                OnEnable();
            }
        }


        Mesh GetFullScreenTriangle()
        {
            Mesh mesh = new Mesh {
                name = "Post-Processing Full-Screen Triangle",
                vertices = new Vector3[] {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                },
                triangles = new int[] { 0, 1, 2 },
            };
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
    }

#endif