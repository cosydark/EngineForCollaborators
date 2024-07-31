using System.Collections.Generic;
using Sirenix.OdinInspector;


namespace FunPlus.WorldX.Stream.Defs {

    public class StreamConst
    {
        public const bool INSTANTIATE_ASYNC = true;
        //================基础配置 别乱动====================

        //最小一级BaseChunk对应的大小
        public const int MIN_BASE_CHUNK_SIZE = 64;
        public const int MAX_BASE_CHUNK_SIZE = 16384;
        public const int MAX_BASE_CHUNK_LV = 8;
        //最大文件等级
        public const int MAX_FILE_CHUNK_LV = 4;
        //每个文件中包含的 对应最大chunksize的baseChunk的个数 
        public const int SIZE_OF_MAX_SIZE_CHUNK_IN_FILE = 4;
        //高一级文件与低一级文件 规模比例 
        public const int FILE_CHUNK_LV_SIZE_RATIO = 4;
        //不同等级文件包含最大的chunk size  根据FILE_CHUNK_LV_SIZE_RATIO计算得来
        public static readonly int[] FILE_CHUNK_MAX_SIZE_MAP =
        {
            64, 256, 1024, 4096, 16384
        };

        public const int TERRAIN_CHUNK_LV = 2; //256size对应的等级

        //================基础配置 别乱动====================

        public const string SCENE_NAME_PATTERN = @"[\w]+/Level/([\w]+).unity";

        public const string STREAM_CHUNK_FILE_PATTEN = @"L([0-9]+)_x([+-]*[0-9]+)_z([+-]*[0-9]+).asset";
        public const string FILE_CHUNK_FMT = "L{0}_x{1}{2}_z{3}{4}.asset";
        public const string FILE_CHUNK_EDITOR_FMT = "L{0}_x{1}{2}_z{3}{4}_editor.asset";
        public const string XDOTS_FILE_CHUNK_FMT = "L{0}_x{1}{2}_z{3}{4}.unity";
        public const string FILE_EXIST_INDEX_FMT = "{0}_AFileExistIndexMap.asset";
        public const string FILE_GLOBAL_FMT = "{0}_GlobalStream.asset";
        public const string SCENE_CHUNKS_PATH_FMT = "Level/LevelLayout/{0}/ta_output/chunks/{1}/{2}";
        public const string XDOTS_SCENE_CHUNKS_PATH_FMT = "Level/LevelLayout/{0}/ta_output/chunks/{1}/{2}";
        public const string STREAM_ROOT_PATH_FMT = "Level/LevelLayout/{0}/Stream/";
        public const string XDOTS_ROOT_PATH_FMT = "Level/LevelLayout/{0}/XDots/";
        public const string RUNTIME_PATH_FMT = STREAM_ROOT_PATH_FMT + "Runtime/{1}";
        public const string GO_RUNTIME_PATH_FMT = STREAM_ROOT_PATH_FMT + "{1}";
        public const string XDOTS_MERGED_PATH_FMT = XDOTS_ROOT_PATH_FMT + "Merged/{1}";
        public const string XDOTS_STREAMINGSCENE_PATH_FMT = XDOTS_ROOT_PATH_FMT + "StreamingScene/{1}";
        public const string XDOTS_GLOBALSCENE_PATH_FMT = XDOTS_ROOT_PATH_FMT + "GlobalScene/{1}_Global.unity";
        public const string EDITOR_PATH_FMT = STREAM_ROOT_PATH_FMT + "Editor/{1}";
        public const string GO_EDITOR_PATH_FMT = STREAM_ROOT_PATH_FMT + "GoEditor/{1}";
        public const string UNITY_CHUNK_FILE_FMT = "chunk_x{0}_y{1}.unity";
        public const string UNITY_LAYER_FILE_FMT = "layer_x{0}_y{1}_layer{2}.unity";
        public const string UNITY_SECTION_FILE_FMT = "section_x{0}_y{1}_layer{2}_section{3}.unity";
        public const string UNITY_CHUNK_FILE_PATTEN = "chunk_x([+-]*[0-9]+)_y([+-]*[0-9]+).unity";
        public const string UNITY_MAIN_FILE_FMT = "Level/{0}/{1}.unity";

        public const string UNITY_GLOBAL_RUNTIME_FILE_NAME = "{0}_GlobalStream.unity"; //stream后的global场景
        public const string UNITY_MAINSCENE_RUNTIME_FILE_NAME = "{0}_MainScene.unity";
        public const string UNITY_SUBSCENE_RUNTIME_FILE_NAME = "{0}_SubScene.unity";
        public const string UNITY_GLOBAL_FILE_NAME = "global.unity";  
        public const string UNITY_GLOBAL_FILE_FMT = "Level/LevelLayout/{0}/ta_output/chunks/{1}";
        public const string XDOTS_GLOBAL_FILE_FMT = "Level/LevelLayout/{0}/ta_output/chunks/{1}";
        public const string STREAM_PREVIEW_SCENE_PATH = @"Assets/Res/Level/StreamFuncScene/Preview.unity";
        public const string STREAM_LOADING_SCENE_PATH = @"Assets/Res/Level/StreamFuncScene/Loading.unity";

        public const string STREAM_DEFAULT_RES_HOLDER = @"Assets/Res/Scene/StreamSupport/streamDotsDefaultResHelper.prefab";

        public const string UNITY_EDITOR_RES_PREFIX = @"Assets/Res/";
        public const string UNITY_EDITOR_RES_TEMP_PREFIX = @"Assets/ResTemp/";

        public const string STREAM_TERRAIN_PREFAB_PATH = @"Assets/Res/Level/{0}/TerrainPrefab/Terrain_{1}_{2}.prefab";
        public const string VEGETATION_DATA_DIR = @"Level/{0}/VegetationData/";
        public const string VEGETATION_DATA_PATTEN = @"VegetationChunk(Ver1|)_(([+-]*[0-9]+)_([+-]*[0-9]+)|info).asset";
        public const string VEGETATION_MANIFEST_FMT = "{0}_VegetationManifest.asset";
        public const string VEGETATION_DATAS_FMT = "{0}_VegetationDatas.asset";
        public const string VEGETATION_DATA_FMT = @"VegetationChunk_{0}_{1}.asset";
        public const string VEGETATION_DATA_VER1_FMT = @"VegetationChunkVer1_{0}_{1}.asset";

        public const string PREVIEW_DATA_KEY = "PreviewData";
        public const string PREVIEW_DATA_FLAG = "PreviewFlag";
        public const string PREVIEW_ORI_SCENE = "PreviewOriScene";

        public const string EXPORT_RECORD_PATH = "stream_res_check_{0}{1}.txt";

        public const byte PKID_LAYER = 1;
        public const byte PKID_NAME = 2;
        public const byte PKID_CAST_SHADOW = 3;
        public const byte PKID_RECEIVE_SHADOW = 4;
        public const byte PKID_TAG = 5;
        public const byte PKID_ENVID = 6;
        public static readonly Dictionary<string, byte> PROP_KEY_2_ID = new Dictionary<string, byte>
        {
            { "m_Layer", PKID_LAYER},
            { "m_Name", PKID_NAME},
            { "m_CastShadows", PKID_CAST_SHADOW},
            { "m_ReceiveShadows", PKID_RECEIVE_SHADOW},
            { "m_TagString", PKID_TAG},
            { "envId", PKID_ENVID},
        };

        public const string STREAM_PROPS = @"m_Layer|m_Name|m_CastShadows|m_ReceiveShadows|m_TagString|envId";
        public const string EFFECT_PREFIX = "ef_";
        public const float EFFECT_BOUND_LIMIT = 20;

        public const byte PREVIEW_LAYER = 255;

        public const string STREAM_DEFAULT_MESH_PREFIX = @"Library/unity default resources";
        public const string STREAM_DEFAULT_MAT = @"Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/Material/DefaultHDMaterial.mat";

        public const string ECS_BOX_COLLIDER_BUCKET_PATH = @"Scene/StreamSupport/BoxColliderBucket.prefab";
        public const string ECS_CAPSULE_COLLIDER_BUCKET_PATH = @"Scene/StreamSupport/CapsuleColliderBucket.prefab";
        public const string ECS_SPHERE_COLLIDER_BUCKET_PATH = @"Scene/StreamSupport/SphereColliderBucket.prefab";
        public const string ECS_MESH_COLLIDER_BUCKET_PATH = @"Scene/StreamSupport/MeshColliderBucket.prefab";

        public const string EDITOR_CROSS_REGION_PATH = @"CrossRegion/";
        public const string CROSS_REGION_FMT = @"Src{0}Dst{1}.asset";
        public const string CROSS_REGINO_SRC_DEL_PREFIX = @"Src{0}";
        public const string CROSS_REGION_SINGLE_FILE_FMT = @"L{0}_x{1}{2}_z{3}{4}";
        public const string CROSS_REGION_HIGH_LV_SEARCH_PARTTEN_FMT = @"SrcL([0-9]+)_x([+-]*[0-9]+)_z([+-]*[0-9]+)Dst{0}.asset";

        public const int ENTITY_PHYSIC_SMALL_OBJECT_SIZE = 10;

        public const string EXPORTER_SVN_RECORD_PATH = "svn_export_records.txt";
        public const string SVN_REVERSION_PATTERN = @"r([0-9]+) [\s\S]*";
        public const string SVN_FILE_DIFF_PATTERN = @"\s+[AM]\s+\S+" + UNITY_CHUNK_FILE_PATTEN + @"[\s\S]*";
        public const string SVN_CHECK_ROOT_FMT = @"Level/{0}/{1}/ta_output/chunks";
    }

    public enum StreamChunkWorkMode {
        Normal, Preview
    }
    
    public enum SceneMeshType
    {
        Unknown = -1,
        SceneObject,
        Terrain,
        Water,
        Skybox,
        Foliage,
        Cloud,
    }

    public enum MultiEditingLayer
    {
        [LabelText("全局层")]
        [LayerInfo("ArtAssets/Global_Root",LayerType.artist,LayerShowType.Null)]
        GLOBAL      = 00,   // 不存chunk
        [LabelText("VEGETATION")]
        [LayerInfo("ArtAssets/Vegetation_Root",LayerType.artist,LayerShowType.Art)]
        //笔刷植被先和植被Vegetation同一命名，因为笔刷植被不会有实体，仅添加untiy作为操作选取的对象，统一流程
        VEGETATION         = 01,   // 植被笔刷
        [LabelText("LIGHT")]
        [LayerInfo("ArtAssets/Light_Root",LayerType.artist,LayerShowType.Art)]
        LIGHT       = 02,   // 灯光
        [LabelText("DECAL")]
        [LayerInfo("ArtAssets/Decal_Root",LayerType.artist,LayerShowType.Art)]
        DECAL  = 03,   // 自动生成
        [LabelText("TERRAIN")]
        [LayerInfo("ArtAssets/Terrain_Root",LayerType.artist,LayerShowType.Art)]
        TERRAIN     = 04,   // 地表
        [LabelText("ROCK")]
        [LayerInfo("ArtAssets/Rock_Root",LayerType.artist,LayerShowType.Art)]
        ROCK        = 05,   // 山石
        [LabelText("BUILD")]
        [LayerInfo("ArtAssets/Build_Root",LayerType.artist,LayerShowType.Art)]
        BUILD       = 06,   // 建筑
        [LabelText("PLANT")]
        [LayerInfo("ArtAssets/Vegetation_Root",LayerType.artist,LayerShowType.Art)]
        PLANT       = 07,   // 植物
        [LabelText("PROP")]
        [LayerInfo("ArtAssets/Props_Root",LayerType.artist,LayerShowType.Art)]
        PROP        = 08,   // 通用物件
        [LabelText("EFFECT")]
        [LayerInfo("ArtAssets/Effect_Root",LayerType.artist,LayerShowType.Art)]
        EFFECT      = 09,   // 特效
        [LabelText("ARTBLOCK")]
        [LayerInfo("ArtAssets/ArtBlock_Root",LayerType.artist,LayerShowType.Art)]
        ARTBLOCK    = 10,   // 美术白盒层
        [LabelText("场景大世界关卡")]
        [LayerInfo("DesignAssets/BigWorld_Level_Root",LayerType.designer,LayerShowType.Design)]
        BIGWORLDLEVEL = 11,   // 场景大世界关卡
        [LabelText("箱庭关卡")]
        [LayerInfo("DesignAssets/Box_Level_Root",LayerType.designer,LayerShowType.Design)]
        BOXLEVEL    = 12,   // 箱庭关卡
        [LabelText("关卡物体")]
        [LayerInfo("DesignAssets/Entity_Level_Root",LayerType.designer,LayerShowType.Design)]
        LEVELENTITY = 13,   // 关卡物体
        [LabelText("PCG")]
        [LayerInfo("ArtAssets/PCG_Root",LayerType.artist,LayerShowType.Art)]
        PCG         = 14,//PCG工具层
        [LabelText("POI地图标识")]
        [LayerInfo("DesignAssets/POI_Root",LayerType.designer,LayerShowType.Design)]
        POI         = 15,//POI
        [LabelText("策划_左厚望")]
        [LayerInfo("DesignAssets/LD_ZUOHOUWANG",LayerType.designer,LayerShowType.Personal)]
        LD_ZUOHOUWANG     = 16,
        [LabelText("策划_贺桥")]
        [LayerInfo("DesignAssets/LD_HEQIAO",LayerType.designer,LayerShowType.Personal)]
        LD_HEQIAO     = 17,
        [LabelText("策划_马迅伟")]
        [LayerInfo("DesignAssets/LD_MAXUNWEI",LayerType.designer,LayerShowType.Personal)]
        LD_MAXUNWEI        = 18,
        [LabelText("策划_易锦鸿")]
        [LayerInfo("DesignAssets/LD_YIJINHONG",LayerType.designer,LayerShowType.Personal)]
        LD_YIJINHONG       = 19,
        [LabelText("策划_刘佳昊")]
        [LayerInfo("DesignAssets/LD_LIUJIAHAO",LayerType.designer,LayerShowType.Personal)]
        LD_LIUJIAHAO         = 20,
        [LabelText("策划_钟天")]
        [LayerInfo("DesignAssets/LD_ZHONGTIAN",LayerType.designer,LayerShowType.Personal)]
        LD_ZHONGTIAN         = 21,
        [LabelText("策划_周晓晨")]
        [LayerInfo("DesignAssets/LD_ZHOUXIAOCHEN",LayerType.designer,LayerShowType.Personal)]
        LD_ZHOUXIAOCHEN         = 22,
        [LabelText("策划_韩晓渝")]
        [LayerInfo("DesignAssets/LD_HANXIAOYU",LayerType.designer,LayerShowType.Personal)]
        LD_HANXIAOYU         = 23,
        [LabelText("策划_龚雯婷")]
        [LayerInfo("DesignAssets/LD_GONGWENTING",LayerType.designer,LayerShowType.Personal)]
        LD_GONGWENTING         = 24,
        [LabelText("主线任务")]
        [LayerInfo("DesignAssets/Main_Quest_Root",LayerType.designer,LayerShowType.Design)]
        MAINQUESTLEVEL    = 25,
        [LabelText("支线任务")]
        [LayerInfo("DesignAssets/Side_Quest_Root",LayerType.designer,LayerShowType.Design)]
        SIDEQUESTLEVEL    = 26,
        [LabelText("日常任务")]
        [LayerInfo("DesignAssets/Daily_Quest_Root",LayerType.designer,LayerShowType.Design)]
        DAILYQUESTLEVEL    = 27,
        [LabelText("策划_宋云龙")]
        [LayerInfo("DesignAssets/LD_SONGYUNLONG",LayerType.designer,LayerShowType.Personal)]
        LD_SONGYUNLONG = 28,
        [LabelText("策划_许惠坤")]
        [LayerInfo("DesignAssets/LD_XUHUIKUN",LayerType.NULL,LayerShowType.Null)]
        LD_XUHUIKUN = 29,
        [LabelText("音效")]
        [LayerInfo("DesignAssets/Audio_Root",LayerType.designer,LayerShowType.Design)]
        AUDIO         = 30,//音效层
        [LabelText("策划_杨子航")]
        [LayerInfo("DesignAssets/LD_YANGZIHANG",LayerType.designer,LayerShowType.Personal)]
        LD_YANGZIHANG = 31,
        [LabelText("策划_曹一宁")]
        [LayerInfo("DesignAssets/LD_CAOYINING",LayerType.designer,LayerShowType.Personal)]
        LD_CAOYINING = 32,
        [LabelText("策划_曾楠")]
        [LayerInfo("DesignAssets/LD_ZENGNAN",LayerType.designer,LayerShowType.Personal)]
        LD_ZENGNAN = 33,
        [LabelText("策划_戴婧")]
        [LayerInfo("DesignAssets/LD_DAIJING",LayerType.designer,LayerShowType.Personal)]
        LD_DAIJING = 34,
        [LabelText("策划_徐一乘")]
        [LayerInfo("DesignAssets/LD_XUYICHENG",LayerType.designer,LayerShowType.Personal)]
        LD_XUYICHENG = 35,
        [LabelText("策划_李旭")]
        [LayerInfo("DesignAssets/LD_LIXU",LayerType.designer,LayerShowType.Personal)]
        LD_LIXU = 36,
        [LabelText("策划_付炳源")]
        [LayerInfo("DesignAssets/LD_FUBINGYUAN",LayerType.designer,LayerShowType.Personal)]
        LD_FUBINGYUAN = 37,
        [LabelText("策划_汪岩嵩")]
        [LayerInfo("DesignAssets/LD_WANGYANSONG",LayerType.designer,LayerShowType.Personal)]
        LD_WANGYANSONG = 38,
        [LabelText("策划_伊兴良")]
        [LayerInfo("DesignAssets/LD_YIXINGLIANG",LayerType.designer,LayerShowType.Personal)]
        LD_YIXINGLIANG = 39,
        [LabelText("策划_金晓丽")]
        [LayerInfo("DesignAssets/LD_JINXIAOLI",LayerType.designer,LayerShowType.Personal)]
        LD_JINXIAOLI = 40,
        [LabelText("策划_赵相如")]
        [LayerInfo("DesignAssets/LD_ZHAOXIANGRU",LayerType.designer,LayerShowType.Personal)]
        LD_ZHAOXIANGRU = 41,
        COUNT       = 42,//总数
        
    }


    public enum MultiEditingGlobalLayer
    {
        [LabelText("默认")]
        [LayerInfo("Global/Default",LayerType.artist,LayerShowType.Null)]
        global = 00,
        [LabelText("ART_Light")]
        [LayerInfo("Global/Global_Light",LayerType.artist,LayerShowType.Null)]
        ART_Light = 01,
        [LabelText("ART_Post")]
        [LayerInfo("Global/Global_Post",LayerType.artist,LayerShowType.Null)]
        ART_Post = 02,
        [LabelText("ART_Background")]
        [LayerInfo("Global/Global_BackGroud",LayerType.artist,LayerShowType.Null)]
        ART_Background = 03,
        [LabelText("ART_Cloud")]
        [LayerInfo("Global/Global_Clouds",LayerType.artist,LayerShowType.Null)]
        ART_Cloud = 04,
        [LabelText("ART_Effect")]
        [LayerInfo("Global/Global_Effect",LayerType.artist,LayerShowType.Null)]
        ART_Effect = 05,
        [LabelText("ART_Tool")]
        [LayerInfo("Global/Global_Tool",LayerType.artist,LayerShowType.Null)]
        ART_Tool = 06,
        [LabelText("ART_PCG")]
        [LayerInfo("Global/Global_PCG",LayerType.artist,LayerShowType.Null)]
        ART_PCG = 07,
        COUNT = 08
    }
    
    /// <summary>
    /// 用于编辑器刷新hierarchy内所有obj可编辑状态时，指定刷新的内容
    /// </summary>
    public enum RefreshMovableState
    {
        RefreshSelect,
        RefreshAll,
        RefreshLockedChunks,
    }

}


