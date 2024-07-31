using UnityEditor;
using UnityEngine;
using ModulePreset = System.Collections.Generic.Dictionary<string, System.Object>;

namespace Editor.PrefabCreator.Module
{
    public interface IModule
    {
        string ModuleName { get; }
        bool Enabled { get; set; }
        bool IsExpanded { get; set; }
        bool CanDisabled { get; set; }
        void DrawModuleGUI(PrefabCreator prefabCreator);
        void Init(PrefabCreator prefabCreator);
        void Dispose();
        void UpdatePrefabs(PrefabCreator prefabCreator);
        void BeforeExport(PrefabCreator prefabCreator);
        void AfterExported(PrefabCreator prefabCreator);
        void LoadFromPrefab(PrefabCreator prefabCreator);
        void OnDisable(PrefabCreator prefabCreator);
        ModulePreset SaveToModulePreset();
        void ApplyModulePreset(ModulePreset modulePreset, bool loadPrefabMode);
    }

    public abstract class Module : IModule
    {
        public string ModuleName { get; private set; }
        public bool Enabled { get; set; }
        public bool CanDisabled { get; set; }
        public bool IsExpanded { get; set; }

        protected Module(string name, bool isExpanded, bool canDisabled)
        {
            ModuleName = name;
            Enabled = true;
            CanDisabled = canDisabled;
            IsExpanded = isExpanded;
        }


        public abstract void DrawModuleGUI(PrefabCreator prefabCreator);

        public virtual void Init(PrefabCreator prefabCreator)
        {
        }

        public virtual void UpdatePrefabs(PrefabCreator prefabCreator)
        {
        }

        public virtual void BeforeExport(PrefabCreator prefabCreator)
        {
        }

        public virtual void AfterExported(PrefabCreator prefabCreator)
        {
        }

        public virtual void LoadFromPrefab(PrefabCreator prefabCreator)
        {
        }

        public virtual void OnDisable(PrefabCreator prefabCreator)
        {
        }

        //Preset
        public virtual ModulePreset SaveToModulePreset()
        {
            return new ModulePreset();
        }

        public virtual void ApplyModulePreset(ModulePreset modulePreset, bool loadPrefabMode)
        {
        }


        public virtual void Dispose()
        {
        }

        //自定义 显示样式 使用延迟创建方式，保证访问EditorStyles.label时 unity已经就绪
        private GUIStyle _customLabelStyle;

        protected GUIStyle customLabelStyle
        {
            get
            {
                if (_customLabelStyle == null)
                {
                    _customLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 10,
                        margin = new RectOffset(0, 0, 1, 1),
                    };
                }

                return _customLabelStyle;
            }
        }

        private GUIStyle _clickableTextStyle;

        protected GUIStyle clickableTextStyle
        {
            get
            {
                if (_clickableTextStyle == null)
                {
                    _clickableTextStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 10, // 减小字体大小
                        margin = new RectOffset(0, 0, 1, 1), // 减小边距
                        normal = { textColor = new Color(0.65f, 0.75f, 0.95f) }, // 设置可点击文本的颜色
                        alignment = TextAnchor.MiddleLeft, // 设置文本居中对齐
                        padding = new RectOffset(13, 0, 5, 0) // 可能需要调整填充来垂直居中文本
                    };
                }

                return _clickableTextStyle;
            }
        }
    }
}