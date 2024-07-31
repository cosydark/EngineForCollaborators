using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Editor.PrefabCreator
{
    public partial class PrefabCreator
    {
        public class PrefabInfo
        {
            private readonly GameObject prefab;
            private List<GameObject> meshObjects = new List<GameObject>();
            public List<GameObject> colliderObjects = new List<GameObject>();
            public List<List<GameObject>> lodObjects = new List<List<GameObject>>();
            public List<GameObject> billboardObjects = new List<GameObject>();
            public List<GameObject> imposterObjects = new List<GameObject>();
            private Dictionary<GameObject, Mesh> gameObjectToMeshDict = new Dictionary<GameObject, Mesh>();
            public string savePath = "";
            public float volume;
            public int faces = 0;

            public PrefabInfo()
            {
            }

            public PrefabInfo(GameObject prefab)
            {
                this.prefab = prefab;
                ProcessPrefabObjects();
                GetVolume();
            }

            public PrefabInfo(GameObject prefab, String prefabSavePath)
            {
                this.prefab = prefab;
                savePath = prefabSavePath + prefab.name + ".prefab";
                ProcessPrefabObjects();
                GetVolume();

                Debug.Log("Len collider:" + colliderObjects.Count + " LodLevel:" + lodObjects.Count + " Len Imposter:" +
                          imposterObjects.Count + " Len Billboard:" + billboardObjects.Count);
            }

            private Mesh GetMeshFromGameObject(GameObject gameObject)
            {
                return gameObjectToMeshDict.GetValueOrDefault(gameObject);
            }

            // 获取colliderMeshList的方法
            public List<Mesh> GetColliderMeshList()
            {
                List<Mesh> colliderMeshList =
                    colliderObjects.Select(GetMeshFromGameObject).Where(mesh => mesh != null).ToList();

                return colliderMeshList;
            }

            // 获取lodMeshList的方法
            public List<List<Mesh>> GetLodMeshList()
            {
                List<List<Mesh>> lodMeshList = new List<List<Mesh>>();
                foreach (List<GameObject> lodGroup in lodObjects)
                {
                    List<Mesh> lodMeshes = lodGroup.Select(GetMeshFromGameObject).Where(mesh => mesh != null).ToList();

                    lodMeshList.Add(lodMeshes);
                }

                return lodMeshList;
            }


            private void ProcessPrefabObjects()
            {
                if (prefab == null)
                {
                    return;
                }

                // 获取所有的 MeshFilter 组件
                MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    ProcessMeshRendererComponent(meshFilter);
                }

                // 获取所有的 SkinnedMeshRenderer 组件
                SkinnedMeshRenderer[] skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    ProcessMeshRendererComponent(skinnedMeshRenderer);
                }

                //寻找被删掉Mesh Renderer的 Collider对象
                Transform[] allChildren = prefab.GetComponentsInChildren<Transform>();
                foreach (Transform child in allChildren)
                {
                    // 检查子对象的名称是否包含 "Collider"（不区分大小写）
                    if (child.name.ToLowerInvariant().Contains("collider"))
                    {
                        // 检查子对象是否没有子对象
                        if (child.childCount == 0)
                        {
                            // 检查子对象是否没有 MeshRenderer 和 SkinnedMeshRenderer 组件
                            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                            SkinnedMeshRenderer skinnedRenderer = child.GetComponent<SkinnedMeshRenderer>();
                            if (meshRenderer == null && skinnedRenderer == null)
                            {
                                //将他视为已经被处理过的Collider对象 收纳进来
                                colliderObjects.Add(child.gameObject);
                            }
                        }
                    }
                }
            }

            private void ProcessMeshRendererComponent(Component meshRendererComponent)
            {
                var mesh =
                    meshRendererComponent.GetType().GetProperty("sharedMesh")?.GetValue(meshRendererComponent) as Mesh;
                if (mesh == null) return;

                var gameObject = meshRendererComponent.gameObject;
                meshObjects.Add(gameObject);
                gameObjectToMeshDict.Add(gameObject, mesh);

                if (ParsingObject.IsColliderParent(gameObject))
                {
                    colliderObjects.Add(gameObject);
                }
                else if (ParsingObject.IsBillboard(gameObject))
                {
                    billboardObjects.Add(gameObject);
                }
                else if (ParsingObject.IsImposter(gameObject))
                {
                    imposterObjects.Add(gameObject);
                }
                else
                {
                    int lodLevel = ParsingObject.GetLodLevelFromName(gameObject.name);
                    while (lodObjects.Count <= lodLevel)
                    {
                        lodObjects.Add(new List<GameObject>());
                    }

                    lodObjects[lodLevel].Add(gameObject);
                }
            }


            private void GetVolume()
            {
                if (GetLodMeshList().Count > 0)
                {
                    List<Mesh> meshesList = GetLodMeshList()[0];

                    // 初始化一个空的包围盒
                    Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.zero);
                    bool hasBounds = false;

                    // 遍历所有的Mesh对象
                    foreach (Mesh mesh in meshesList)
                    {
                        if (mesh == null) continue;

                        // 如果是第一个有效的Mesh，设置combinedBounds为该Mesh的包围盒
                        if (!hasBounds)
                        {
                            combinedBounds = mesh.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            // 合并当前Mesh的包围盒到总包围盒中
                            combinedBounds.Encapsulate(mesh.bounds);
                        }
                    }

                    if (hasBounds)
                    {
                        // 计算总包围盒的尺寸
                        float width = combinedBounds.size.x;
                        float height = combinedBounds.size.y;
                        float depth = combinedBounds.size.z;
                        volume = width * height * depth;
                    }
                    else
                    {
                        Debug.LogWarning($"没有有效的Mesh对象在{prefab}的LOD列表中，无法计算体积");
                    }
                }
                else
                {
                    Debug.LogWarning($"LOD列表为空，无法获取{prefab}的体积");
                }
            }
        }
    }
}