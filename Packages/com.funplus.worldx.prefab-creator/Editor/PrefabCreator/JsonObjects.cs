using System;
using System.Collections.Generic;

namespace Editor.PrefabCreator
{
    [Serializable]
    public class LodInfo
    {
        public string name;
        public List<string> materials;
    }

    [Serializable]
    public class TexObj
    {
        public string baseColor;
        public string normal;
    }

    [Serializable]
    public class MaterialInfo
    {
        public string name;
        public TexObj texObj;
        public int parentMaterialIndex = -1;
    }
    
    [Serializable]
    public class ColliderInfo
    {
        public string name;
        public string type;
        public TransformData  transform;
        
    }

    [Serializable]
    public class RootObject
    {
        public List<LodInfo> lod;
        public List<MaterialInfo> materials;
        public List<ColliderInfo> colliders;
        public string fbxPath;
    }

    [Serializable]
    public class TransformData
    {
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        
        // capsule
        public float radius;
        public float height;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;
    }


    public enum ColliderTypes
    {
        box,
        sphere,
        capsule,
        convex,
        mesh
    }
    
}