using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.CSharp;

namespace SimpleLanguage.Core
{
    public class MetaTypeCSharp
    {
        public static string GetClassNameByCSharpType(System.Type type)
        {
            if (type == null)
            {
                return null;
            }
            switch (type.Name)
            {
                case "Boolean":
                    return "Boolean";
                case "SByte":
                    return "SByte";
                case "Byte":
                    return "Byte";
                case "Single":
                    return "Float";
                case "Double":
                    return "Double";
                default:
                    return type.Name;
            }
#pragma warning disable CS0162 // 检测到无法访问的代码
            return "Object";
#pragma warning restore CS0162 // 检测到无法访问的代码
        }
        public static System.Type FindCSharpType( MetaClass mc )
        {
            System.Type type = null;

            if (mc == CoreMetaClassManager.byteMetaClass)
            {
                type = typeof(System.Byte);
            }
            else if (mc == CoreMetaClassManager.sbyteMetaClass)
            {
                type = typeof(System.SByte);
            }
            else if (mc == CoreMetaClassManager.int16MetaClass)
            {
                type = typeof(System.Int16);
            }
            else if (mc == CoreMetaClassManager.uint16MetaClass)
            {
                type = typeof(System.UInt16);
            }
            else if (mc == CoreMetaClassManager.int32MetaClass)
            {
                type = typeof(System.Int32);
            }
            else if(mc == CoreMetaClassManager.uint32MetaClass )
            {
                type = typeof(System.UInt32);
            }
            else if (mc == CoreMetaClassManager.int64MetaClass)
            {
                type = typeof(System.Int64);
            }
            else if (mc == CoreMetaClassManager.uint64MetaClass)
            {
                type = typeof(System.UInt64);
            }
            else if (mc == CoreMetaClassManager.float32MetaClass)
            {
                type = typeof(System.Single);
            }
            else if (mc == CoreMetaClassManager.float64MetaClass)
            {
                type = typeof(System.Double);
            }
            else if( mc == CoreMetaClassManager.stringMetaClass )
            {
                type = typeof(System.String);
            }
            else
            {
                type = CSharpManager.FindCSharpType(mc.allClassName );
            }
            return type;
        }
    }
}
