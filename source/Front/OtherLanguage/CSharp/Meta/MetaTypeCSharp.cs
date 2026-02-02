
using SimpleLanguage.CSharp;
using System.Diagnostics;

namespace SimpleLanguage.Core
{
    public class MetaTypeCSharp
    {
        public static MetaType GetMetaTypeByCSharpType(System.Type type)
        {
            if( type.Name == "Nullable`1" )
            {
                if( type.GenericTypeArguments.Length == 1 )
                {
                    MetaClass mc = GetMetaClassByCSharpType(type.GenericTypeArguments[0]);

                    var mt = new MetaType(mc);
                    mt.SetNullable(true);
                    return mt;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else
            {
                MetaClass mc = GetMetaClassByCSharpType(type);
                var mt = new MetaType(mc);
                return mt;
            }
            Debug.Assert(false);
            return null;
        }
        static MetaClass GetMetaClassByCSharpType(System.Type type)
        {
            string typeName = type.Name;
            switch (typeName)
            {
                case "Boolean":
                case "BoolObject":
                    return CoreMetaClassManager.byteMetaClass;
                case "Byte":
                case "ByteObject":
                    return CoreMetaClassManager.byteMetaClass;
                case "SByte":
                case "SByteObject":
                    return CoreMetaClassManager.sbyteMetaClass;
                case "Int16":
                case "Int16Object":
                    return CoreMetaClassManager.int16MetaClass;
                case "UInt16":
                case "UInt16Object":
                    return CoreMetaClassManager.uint16MetaClass;
                case "Int32":
                case "Int32Object":
                    return CoreMetaClassManager.int32MetaClass;
                case "UInt32":
                case "UInt32Object":
                    return CoreMetaClassManager.uint32MetaClass;
                case "Int64":
                case "Int64Object":
                    return CoreMetaClassManager.int64MetaClass;
                case "UInt64":
                case "UInt64Object":
                    return CoreMetaClassManager.uint64MetaClass;
                case "Single":
                case "Float32Object":
                    return CoreMetaClassManager.float32MetaClass;
                case "Double":
                case "Float64Object":
                    return CoreMetaClassManager.float64MetaClass;
                case "String":
                case "StringObject":
                    return CoreMetaClassManager.stringMetaClass;
                case "Object":
                case "SObject":
                    return CoreMetaClassManager.objectMetaClass;
                case "Void":
                case "VoidObject":
                    return CoreMetaClassManager.voidMetaClass;
                case "ArrayObject":
                    {
                        return CoreMetaClassManager.arrayMetaClass;
                    }
            }
            Debug.Assert(false);
            return null;
        }
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
            System.Type type = null;// typeof( mc );
            
            if( mc == CoreMetaClassManager.booleanMetaClass )
            {
                //type = typeof(BoolObject);
                //type = typeof(System.Boolean);
            }
            else if (mc == CoreMetaClassManager.byteMetaClass)
            {
                //type = typeof(Int8Object);
                //type = typeof(System.Byte);
            }
            else if (mc == CoreMetaClassManager.sbyteMetaClass)
            {
                //type = typeof(SInt8Object);
                //type = typeof(System.SByte);
            }
            else if (mc == CoreMetaClassManager.int16MetaClass)
            {
                //type = typeof(Int16Object);
                //type = typeof(System.Int16);
            }
            else if (mc == CoreMetaClassManager.uint16MetaClass)
            {
                //type = typeof(UInt16Object);
                //type = typeof(System.UInt16);
            }
            else if (mc == CoreMetaClassManager.int32MetaClass)
            {
                //type = typeof(Int32Object);
                //type = typeof(System.Int32);
            }
            else if(mc == CoreMetaClassManager.uint32MetaClass )
            {
                //type = typeof(UInt32Object);
                //type = typeof(System.UInt32);
            }
            else if (mc == CoreMetaClassManager.int64MetaClass)
            {
                //type = typeof(Int64Object);
                //type = typeof(System.Int64);
            }
            else if (mc == CoreMetaClassManager.uint64MetaClass)
            {
                //type = typeof(UInt64Object);
                //type = typeof(System.UInt64);
            }
            else if (mc == CoreMetaClassManager.float32MetaClass)
            {
                //type = typeof(Float32Object);
                //type = typeof(System.Single);
            }
            else if (mc == CoreMetaClassManager.float64MetaClass)
            {
                //type = typeof(Float64Object);
                //type = typeof(System.Double);
            }
            else if( mc == CoreMetaClassManager.stringMetaClass )
            {
                //type = typeof(StringObject);
                //type = typeof(System.String);
            }
            else if( mc == CoreMetaClassManager.objectMetaClass )
            {
                //type = typeof(SObject);
                //type = typeof(System.Object);
            }
            else if( mc == CoreMetaClassManager.arrayMetaClass )
            {
                //type = typeof(ArrayObject);
            }
            else
            {
                type = CSharpManager.FindCSharpType(mc.allClassName );
            }
            
            return type;
        }
    }
}
