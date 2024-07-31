using System;
using System.Collections.Generic;
using Editor.MathGeoLib;
using UnityEditor;
using UnityEngine;
using ModulePreset = System.Collections.Generic.Dictionary<string, System.Object>;
using Object = UnityEngine.Object;


namespace Editor.PrefabCreator.Module
{
    public class ColliderModule : Module
    {
        // 用于存储折叠状态的字典
        Dictionary<ColliderType, bool> foldoutStates = new Dictionary<ColliderType, bool>();
        private bool generateTempCollider = true;

        public ColliderModule(string name, bool isExpanded = false, bool canDisabled = false) : base(name, isExpanded,
            canDisabled)
        {
        }

        public enum ColliderType
        {
            Box,
            Sphere,
            Mesh,
            Capsule,
            Convex,
            Unknown // 用于无法识别的情况
        }

        static readonly string[] names_cb = { "_cb", "_c_b", "_box", "collider_b" };
        static readonly string[] names_cs = { "_cs", "_c_s", "_sphere", "collider_s" };
        static readonly string[] names_cc = { "_cc", "_c_c", "_convex", "collider_co" };
        static readonly string[] names_cp = { "_cp", "_c_p", "_capsule", "collider_ca" };
        static readonly string[] names_cm = { "_cm", "_c_m", "_mesh", "collider_m" };

        //Preset function ----------------------------------------------------------------------------------------------
        public override ModulePreset SaveToModulePreset()
        {
            return new ModulePreset
            {
                ["generateTempCollider"] = generateTempCollider
            };
        }

        public override void ApplyModulePreset(ModulePreset lodModulePreset, bool loadPrefabMode)
        {
            if (lodModulePreset.ContainsKey("generateTempCollider"))
            {
                generateTempCollider = (bool)lodModulePreset["generateTempCollider"];
            }

            // Debug.Log("Collider模块加载");
        }

        public static ColliderType GetColliderType(string name)
        {
            var meshName = name.ToLower(); // 转换为小写以进行不区分大小写的匹配

            foreach (var suffix in names_cb)
            {
                if (meshName.Contains(suffix))
                    return ColliderType.Box;
            }

            foreach (var suffix in names_cs)
            {
                if (meshName.Contains(suffix))
                    return ColliderType.Sphere;
            }

            foreach (var suffix in names_cc)
            {
                if (meshName.Contains(suffix))
                    return ColliderType.Convex;
            }

            foreach (var suffix in names_cp)
            {
                if (meshName.Contains(suffix))
                    return ColliderType.Capsule;
            }

            foreach (var suffix in names_cm)
            {
                if (meshName.Contains(suffix))
                    return ColliderType.Mesh;
            }

            return ColliderType.Unknown;
        }
        

        // 处理Collider Mesh列表
        public static Dictionary<string, ColliderType> GetColliderTypes(List<Mesh> colliderMeshes)
        {
            Dictionary<string, ColliderType> colliderTypes = new Dictionary<string, ColliderType>();

            foreach (Mesh mesh in colliderMeshes)
            {
                ColliderType type = GetColliderType(mesh.name); // 使用单个Mesh处理函数
                colliderTypes.Add(mesh.name, type);
            }

            return colliderTypes;
        }

        public override void DrawModuleGUI(PrefabCreator pfc)
        {
            float labelWidth = 80f;
            PrefabCreator.PrefabInfo prefabInfo = pfc.GetCurrentPrefabInfo();
            List<GameObject> colliderObjects = prefabInfo.colliderObjects;

            if (colliderObjects == null || colliderObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("没有可用的Collider", MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 300;
                bool newGenerateTempCollider = EditorGUILayout.Toggle("利用倒数第二级Lod生成临时的碰撞体", generateTempCollider);
                EditorGUIUtility.labelWidth = 0;

                EditorGUILayout.EndHorizontal();
                if (newGenerateTempCollider != generateTempCollider)
                {
                    generateTempCollider = newGenerateTempCollider;
                    GenerateTempCollider(pfc);
                }

                return;
            }

            // 对Collider进行分类
            Dictionary<ColliderType, List<GameObject>> collidersByType = new Dictionary<ColliderType, List<GameObject>>();
            foreach (var colliderObject in colliderObjects)
            {
                ColliderType type = GetColliderType(colliderObject.name);
                if (!collidersByType.ContainsKey(type))
                {
                    collidersByType[type] = new List<GameObject>();
                    // 如果字典中没有当前LOD层级的折叠状态，添加默认值（展开）
                    foldoutStates.TryAdd(type, true);
                }

                collidersByType[type].Add(colliderObject);
            }

            // 遍历每个Collider类型
            foreach (var kvp in collidersByType)
            {
                ColliderType type = kvp.Key;
                List<GameObject> collidersObject = kvp.Value;

                // 如果一个区域没有Collider那就不显示
                if (collidersObject.Count == 0)
                    continue;

                // 显示Collider类型和数量，并创建一个可折叠的部分
                foldoutStates[type] = EditorGUILayout.Foldout(foldoutStates[type],
                    type + " Colliders: " + collidersObject.Count, true);
                if (foldoutStates[type]) // 如果折叠部分是展开的
                {
                    EditorGUILayout.BeginVertical("box");

                    foreach (var colliderObject in collidersObject)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Collider 名称:", customLabelStyle,
                            GUILayout.Width(labelWidth));

                        // 判断pfc.prefab是否为null
                        if (pfc.currentPrefab != null)
                        {
                            if (GUILayout.Button(colliderObject.name, clickableTextStyle))
                            {
                                Selection.activeGameObject = colliderObject;
                                EditorGUIUtility.PingObject(colliderObject);
                            }
                        }
                        else
                        {
                            // pfc.prefab为null时，显示普通文本
                            EditorGUILayout.LabelField(colliderObject.name, customLabelStyle);
                        }

                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(5);
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }

        public override void Init(PrefabCreator pfc)
        {
            UpdatePrefabs(pfc);
        }


        private void CreateCollider(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Mesh mesh = null;

            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                mesh = meshFilter.sharedMesh;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }


            if (mesh == null)
            {
                Debug.LogWarning(target.name + " does not have a MeshFilter with a mesh.");
                return;
            }

            // 根据名称判断Collider类型
            ColliderType colliderType = GetColliderType(target.name);
            // 如果是Mesh或Convex，直接在目标对象上添加MeshCollider
            if (colliderType == ColliderType.Mesh || colliderType == ColliderType.Convex)
            {
                MeshCollider meshCollider = target.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh; // 使用PrefabInfo中获取的Mesh
                if (colliderType == ColliderType.Convex)
                {
                    meshCollider.convex = true;
                }

                // 移除MeshRenderer，因为我们只需要Collider
                MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    GameObject.DestroyImmediate(meshRenderer);
                }

                // 移除MeshFilter，如果不再需要
                MeshFilter targetMeshFilter = target.GetComponent<MeshFilter>();
                if (targetMeshFilter != null)
                {
                    GameObject.DestroyImmediate(targetMeshFilter);
                }
            }
            else
            {
                // 获取物体空间中的顶点
                Vector3[] vertices = mesh.vertices;

                // 转换顶点到世界空间
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = target.transform.TransformPoint(vertices[i]);
                }

                // 计算OBB
                OrientedBoundingBox obb = OrientedBoundingBox.BruteEnclosing(vertices);

                // 创建一个Cube来表示OBB
                GameObject obbCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obbCube.transform.position = obb.Center;
                obbCube.transform.localScale = obb.Extent * 2; // Extent是半尺寸，所以我们需要将它乘以2来得到全尺寸

                // 设置旋转
                Quaternion rotation = Quaternion.LookRotation(obb.Axis3, obb.Axis2);
                obbCube.transform.rotation = rotation;

                // 设置新创建的obbCube的父对象和名称
                obbCube.transform.SetParent(target.transform.parent);


                // 添加相应的Collider组件
                switch (colliderType)
                {
                    case ColliderType.Box:
                        // BoxCollider已经存在，因为我们使用了CreatePrimitive来创建obbCube
                        break;

                    case ColliderType.Sphere:
                        GameObject.DestroyImmediate(obbCube.GetComponent<BoxCollider>());
                        obbCube.AddComponent<SphereCollider>();
                        break;

                    case ColliderType.Capsule:
                        GameObject.DestroyImmediate(obbCube.GetComponent<BoxCollider>());
                        CapsuleCollider capsuleCollider = obbCube.AddComponent<CapsuleCollider>();
                        // 需要特殊处理下最长轴
                        Vector3 scale = obbCube.transform.localScale;
                        if (scale.x > scale.y && scale.x > scale.z)
                        {
                            // X轴是最长的
                            capsuleCollider.direction = 0;
                        }
                        else if (scale.y > scale.x && scale.y > scale.z)
                        {
                            // Y轴是最长的
                            capsuleCollider.direction = 1;
                        }
                        else
                        {
                            // Z轴是最长的
                            capsuleCollider.direction = 2;
                        }

                        break;

                    case ColliderType.Unknown:
                        Debug.LogWarning("Unknown collider type for " + target.name);
                        break;
                }

                // 删除Renderer
                MeshRenderer targetMeshRenderer = target.GetComponent<MeshRenderer>();
                if (targetMeshRenderer != null)
                {
                    GameObject.DestroyImmediate(targetMeshRenderer);
                }

                // 移除MeshFilter
                MeshFilter targetMeshFilter = target.GetComponent<MeshFilter>();
                if (targetMeshFilter != null)
                {
                    GameObject.DestroyImmediate(targetMeshFilter);
                }

                Transform ObbTransform = obbCube.GetComponent<Transform>();
                Transform tragetTransform = target.GetComponent<Transform>();
                tragetTransform.position = ObbTransform.position;
                tragetTransform.rotation = ObbTransform.rotation;
                tragetTransform.localScale = ObbTransform.localScale;

                // 获取并移除所有Collider组件
                Collider[] colliders = target.GetComponents<Collider>();
                foreach (Collider collider in colliders)
                {
                    GameObject.Destroy(collider);
                }

                colliders = obbCube.GetComponents<Collider>();
                
                foreach (Collider originalCollider in colliders)
                {
                    // 根据Collider类型创建新的Collider组件
                    Type componentType = originalCollider.GetType();
                    Collider newCollider = target.AddComponent(componentType) as Collider;

                    // 如果是BoxCollider
                    if (originalCollider is BoxCollider)
                    {
                        BoxCollider originalBoxCollider = originalCollider as BoxCollider;
                        BoxCollider newBoxCollider = newCollider as BoxCollider;
                        newBoxCollider.center = originalBoxCollider.center;
                        newBoxCollider.size = originalBoxCollider.size;
                    }
                    // 如果是SphereCollider
                    else if (originalCollider is SphereCollider)
                    {
                        SphereCollider originalSphereCollider = originalCollider as SphereCollider;
                        SphereCollider newSphereCollider = newCollider as SphereCollider;
                        newSphereCollider.center = originalSphereCollider.center;
                        newSphereCollider.radius = originalSphereCollider.radius;
                    }
                    // 如果是CapsuleCollider
                    else if (originalCollider is CapsuleCollider)
                    {
                        CapsuleCollider originalCapsuleCollider = originalCollider as CapsuleCollider;
                        CapsuleCollider newCapsuleCollider = newCollider as CapsuleCollider;
                        newCapsuleCollider.center = originalCapsuleCollider.center;
                        newCapsuleCollider.radius = originalCapsuleCollider.radius;
                        newCapsuleCollider.height = originalCapsuleCollider.height;
                        newCapsuleCollider.direction = originalCapsuleCollider.direction;
                    }
                    // 如果是MeshCollider
                    else if (originalCollider is MeshCollider)
                    {
                        MeshCollider originalMeshCollider = originalCollider as MeshCollider;
                        MeshCollider newMeshCollider = newCollider as MeshCollider;
                        newMeshCollider.sharedMesh = originalMeshCollider.sharedMesh;
                        newMeshCollider.convex = originalMeshCollider.convex;
                        newMeshCollider.inflateMesh = originalMeshCollider.inflateMesh;
                        newMeshCollider.skinWidth = originalMeshCollider.skinWidth;
                    }
                }

                GameObject.DestroyImmediate(obbCube);
            }
        }
        

        public override void UpdatePrefabs(PrefabCreator pfc)
        {
            foreach (var prefab in pfc.prefabs)
            {
                PrefabCreator.PrefabInfo prefabInfo = pfc.prefabInfos[prefab];

                if (prefab == null || prefabInfo.colliderObjects == null)
                {
                    return;
                }

                // 如果没有Collider GameObjects，检查是否执行生成临时Collider
                if (prefabInfo.colliderObjects.Count < 1)
                {
                    GenerateTempCollider(pfc);
                    return;
                }

                Debug.Log("开始处理Collider 数量: " + prefabInfo.colliderObjects.Count);

                // 遍历PrefabInfo中的colliderObjects列表
                foreach (GameObject colliderObject in prefabInfo.colliderObjects)
                {
                    // 检查MeshRenderer组件 
                    MeshRenderer meshRenderer = colliderObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                    {
                        // 如果没有MeshRenderer，视为已经处理过
                        continue;
                    }

                    // 添加Collider组件
                    CreateCollider(colliderObject);
                }
            }
        }

        public void GenerateTempCollider(PrefabCreator pfc)
        {
            foreach (var prefab in pfc.prefabs)
            {
                // Debug.Log("LEN++++++++++" + pfc.prefabs.Count);
                // Debug.Log("生成临时碰撞体" + generateTempCollider + prefab);
                // 查找是否已经存在名为"temporaryCollider"的子对象
                Transform existingTempCollidersParent = prefab.transform.Find("TemporaryCollider");

                if (existingTempCollidersParent != null)
                {
                    //先统一删除
                    Object.DestroyImmediate(existingTempCollidersParent.gameObject);
                }

                if (generateTempCollider)
                {
                    // 如果generateTempCollider为true且已存在，则不执行任何操作以防止重复添加
                    if (existingTempCollidersParent != null) continue;

                    // 获取倒数第二级的LOD Mesh数组
                    List<List<Mesh>> lodList = pfc.prefabInfos[prefab].GetLodMeshList();
                    int lodLevel = Mathf.Max(0, lodList.Count - 2);
                    var meshes = lodList[lodLevel];

                    // 创建一个新的GameObject作为所有临时colliders的父对象
                    GameObject temporaryCollidersParent = new GameObject("TemporaryCollider");
                    temporaryCollidersParent.transform.SetParent(prefab.transform, false);

                    // 为每个Mesh创建一个新的GameObject并添加MeshCollider
                    foreach (Mesh mesh in meshes)
                    {
                        GameObject meshColliderObj = new GameObject("MeshCollider_" + mesh.name);
                        meshColliderObj.transform.SetParent(temporaryCollidersParent.transform, false);
                        MeshCollider meshCollider = meshColliderObj.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = mesh;
                    }
                }
            }
        }
    }
}