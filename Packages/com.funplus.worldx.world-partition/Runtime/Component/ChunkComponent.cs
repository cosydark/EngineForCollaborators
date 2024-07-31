#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using FunPlus.WorldX.Stream.Defs;
using Sirenix.OdinInspector;
using WorldPartitionLayer = FunPlus.WorldX.Stream.Defs.MultiEditingLayer;

namespace FunPlus.WorldX.WorldPartition
{
    /// <summary>
    /// This class is used for collect custom data unsupported by Unity
    /// </summary>
    [ExecuteInEditMode]
    public class ChunkComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        [HideInInspector]
        public byte[] serializedBytes;

        public WorldPartitionLayer layer = WorldPartitionLayer.PROP;
        [ShowIf("IfShowGlobalLayers")]
        public MultiEditingGlobalLayer globalLayer = MultiEditingGlobalLayer.global;
        public int displayIndex1D = -1;

        public void ClearAll()
        {
        }
        
        private bool IfShowGlobalLayers()
        {
            if (layer == MultiEditingLayer.GLOBAL)
            {
                return true;
            }

            return false;
        }

        public static byte[] SerializeObject(object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static T DeSerializeObject<T>(byte[] bytes)
        {
            MemoryStream ms = new MemoryStream(bytes);
            BinaryFormatter bf = new BinaryFormatter();
            T unpacked = (T)bf.Deserialize(ms);
            return unpacked;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
        }

    }
}
#endif
