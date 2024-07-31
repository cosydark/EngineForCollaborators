using System.Collections.Generic;
using System;

namespace FunPlus.WorldX.Stream.Defs
{
    [Serializable]
    public class BaseChunkInfo {
        public int baseChunkLv;
        public long baseChunkId;
        // public List<AssetInfo> gameObjectInfos;
        public byte[] entityInfos;
        // public AssetInfoResMap resMap;
        // public BaseChunkEntityPhysicDesc entityPhysicInfo;
        // public BaseChunkTriggerDesc triggerInfo;
        [NonSerialized]
        public bool isPreviewSrc = false;
    }

    [Serializable]
    public class EmptyBaseChunkIds{
        public int baseChunkLv;
        public List<long> emptyBaseChunkIds;

        public void Init(int _baseChunkLv, HashSet<long> rawIds)
        {
            baseChunkLv = _baseChunkLv;
            emptyBaseChunkIds = new List<long>();
            var enu = rawIds.GetEnumerator();
            while(enu.MoveNext()){
                emptyBaseChunkIds.Add(enu.Current);
            }
        }
    }

    // [Serializable]
    // public class StreamExportInfo
    // {
    //     public FileChunkInfo fci;
    //     public EditorFileChunkInfo efci;
    //     // public List<EditorChunkEntityPhsyicInfo> crossRegionPhyInfos;
    //     [NonSerialized]
    //     public string oriScenePath;
    //     
    //     public void Init(FileChunkId id, string sceneName){
    //         fci = new FileChunkInfo();
    //         efci = new EditorFileChunkInfo();
    //         // crossRegionPhyInfos = new List<EditorChunkEntityPhsyicInfo>();
    //         fci.Init();
    //         efci.Init();
    //         fci.fileChunkId = id.IntData;
    //         fci.sceneName = sceneName;
    //         efci.fileChunkId = id.IntData;
    //         efci.sceneName = sceneName;
    //     }
    // }


    //helper for parsing fileChunkId
    public struct FileChunkId {
        private int _rawId;
        public const int LV_BIT = 4;
        public const int LV_MASK = (1 << LV_BIT) - 1;
        public const int X_BIT = 14;
        public const int X_MASK = (1 << X_BIT) - 1;
        //use offset to support negative x and z
        public const int X_NEG_OFFSET = (1 << (X_BIT - 1));
        public const int Z_BIT = 32 - LV_BIT - X_BIT;
        public const int Z_MASK = (1 << Z_BIT) - 1;
        public const int Z_NEG_OFFSET = (1 << (Z_BIT - 1));
        public uint lv { get { return (uint)(_rawId >> (X_BIT + Z_BIT) & LV_MASK); } }
        public int x { get { return (_rawId >> Z_BIT & X_MASK) - X_NEG_OFFSET; } }
        public int z { get { return (_rawId & Z_MASK) - Z_NEG_OFFSET; } }
        public int IntData { get { return _rawId; } set { _rawId = value; } }
        public static FileChunkId CreateFromInt(int id) { return new FileChunkId { _rawId = id }; }
        public static FileChunkId CreateFromLvXZ(uint lv, int x, int z) {
            return new FileChunkId { _rawId = (int)lv << (X_BIT + Z_BIT) | (x + X_NEG_OFFSET) << Z_BIT | (z + Z_NEG_OFFSET) };
        }
        public bool isValid() { return lv <= StreamConst.MAX_FILE_CHUNK_LV; }
        public void move(int xoffset, int zoffset){
            _rawId = (_rawId >> (X_BIT + Z_BIT) & LV_MASK) << (X_BIT + Z_BIT) | ((_rawId >> Z_BIT & X_MASK) + xoffset) << Z_BIT | ((_rawId & Z_MASK) + zoffset);
        }
        public override string ToString()
        {
            return string.Format("[FileChunkId]Lv:{0},x:{1},z:{2}",lv,x,z);
        }
    }
    //helper for parsing baseChunkId
    public struct BaseChunkId : IEquatable<BaseChunkId> {
        private long _rawId;
        public const int X_BIT = 32;
        public const long X_MASK = ((long)1 << X_BIT) - 1;
        public const long X_NEG_OFFSET = ((long)1 << (X_BIT - 1));
        public const int Z_BIT = 64 - X_BIT;
        public const long Z_MASK = ((long)1 << X_BIT) - 1;
        public const long Z_NEG_OFFSET = ((long)1 << (Z_BIT - 1));

        public int x { get { return (int)((_rawId >> Z_BIT & X_MASK) - X_NEG_OFFSET); } }
        public int z { get { return (int)((_rawId & Z_MASK) - Z_NEG_OFFSET); } }
        public long IntData { get { return _rawId; } set { _rawId = value; } }
        public BaseChunkId(long raw) { _rawId = raw; }
        public static BaseChunkId CreateFromInt(long id) { return new BaseChunkId { _rawId = id }; }
        public static BaseChunkId CreateFromXZ(int x, int z) { return new BaseChunkId { _rawId = ((long)x + X_NEG_OFFSET) << Z_BIT | ((long)z + Z_NEG_OFFSET) }; }
        public void move(int xoffset, int zoffset){
            _rawId = ((_rawId >> Z_BIT & X_MASK) + xoffset) << Z_BIT | ((_rawId & Z_MASK) + zoffset);
        }

        public bool Equals(BaseChunkId other)
        {
            return this.IntData == other.IntData;
        }

        public override int GetHashCode()
        {
            return this.IntData.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[BaseChunkId]x:{0},z:{1}",x,z);
        }
    }

}