using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor.LODGeneration
{
    public class LODRegenerate : EditorWindow
    {
        [MenuItem("TA/LOD Regenerate")]
        private static void OpenWindow()
        {
            var window = ScriptableObject.CreateInstance<LODRegenerate>() as LODRegenerate;
            window.minSize = new Vector2(200, 200);
            window.maxSize = new Vector2(200, 200);
            window.titleContent = new GUIContent("LOD Regenerate");
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(7);
            if (GUILayout.Button("Set Up", GUILayout.Height(37)))
            {
                SetUp(Selection.activeGameObject);
            }
        }

        public static void SetUpLOD(GameObject g) => SetUp(g);
        
        #region PrivateFunctions

        private static void SetUp(GameObject g)
        {
            RemoveLodGroup(g);
            List<GameObject> lods = FetchLodRoots(g);
            for (int i = 0; i < lods.Count; i++)
            {
                SetUpLod(lods[i]);
            }
        }
        
        private static void RemoveLodGroup(GameObject g)
        {
            LODGroup[] lodGroups = g.GetComponentsInChildren<LODGroup>();
            for (int i = 0; i < lodGroups.Length; i++)
            {
                Object.DestroyImmediate(lodGroups[i]);
            }
        }

        private static List<GameObject> FetchLodRoots(GameObject g)
        {
            Transform[] transforms = g.GetComponentsInChildren<Transform>();
            List<GameObject> lods = new List<GameObject>();
            for (int i = 0; i < transforms.Length; i++)
            {
                if(transforms[i].name != "LOD") { continue; }
                lods.Add(transforms[i].gameObject);
            }
            return lods;
        }
        
        private static void SetUpLod(GameObject g)
        {
            LODGroup lodGroup = g.AddComponent<LODGroup>();
            List<LOD> lods = new List<LOD>();
            for (int i = 0; i < g.transform.childCount; i++)
            {
                Transform child = g.transform.GetChild(i);
                Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
                lods.Add(new LOD(ComputeLODScreenRelative(i), renderers));
            }
            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();
        }

        private static float ComputeLODScreenRelative(int index)
        {
            return index switch
            {
                0 => 0.5f,
                1 => 0.25f,
                2 => 0.1f,
                3 => 0.02f,
                _ => -1
            };
        }
        #endregion
    }
}

