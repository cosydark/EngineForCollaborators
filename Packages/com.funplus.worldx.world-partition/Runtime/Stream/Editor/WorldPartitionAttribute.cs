using System;

namespace FunPlus.WorldX.Stream.Defs
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LayerInfoAttribute : Attribute
    {
        public string RootPath;
        public LayerType LayerType;
        public LayerShowType LayerShowType;
        
        public LayerInfoAttribute(string RootPath,LayerType layerType,LayerShowType layerShowType)
        {
            this.RootPath = RootPath;
            this.LayerType = layerType;
            LayerShowType = layerShowType;
        }
    }
    
    /// <summary>
    /// ����Ȩ�޹����Ĳ㼶ö��
    /// </summary>
    public enum LayerType
    {
        NULL,
        artist,
        designer,
    }

    /// <summary>
    /// ����ʵ��UI��ʾʱ���з���Ĳ㼶ö��
    /// </summary>
    public enum LayerShowType
    {
        Null,
        Art,
        Design,
        Personal,
    }
}
   

