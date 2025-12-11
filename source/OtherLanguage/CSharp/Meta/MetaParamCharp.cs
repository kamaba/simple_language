
using SimpleLanguage.CSharp;
using SimpleLanguage.VM;
using System;
using System.Diagnostics;
using System.Reflection;
using static SimpleLanguage.Core.MetaVariable;

namespace SimpleLanguage.Core
{
    public sealed class MetaInputParamCSharp
    {
        public static System.Type GetCSharpType(MetaInputParam mip )
        {
            if(mip.express is MetaConstExpressNode mcen )
            {
                System.Type st = null;

                switch (mcen.eType)
                {
                    case EType.Boolean:
                        {
                            st = typeof(bool);
                        }
                        break;
                    case EType.Byte:
                        {
                            st = typeof(Byte);
                        }
                        break;
                    case EType.SByte:
                        {
                            st = typeof(SByte);
                        }
                        break;
                    case EType.Int16:
                        {
                            st = typeof(Int16);
                        }
                        break;
                    case EType.UInt16:
                        {
                            st = typeof(UInt16);
                        }
                        break;
                    case EType.Int32:
                        {
                            st = typeof(Int32);
                        }
                        break;
                    case EType.UInt32:
                        {
                            st = typeof(UInt32);
                        }
                        break;
                    case EType.Int64:
                        {
                            st = typeof(Int64);
                        }
                        break;
                    case EType.UInt64:
                        {
                            st = typeof(UInt64);
                        }
                        break;
                    case EType.Float32:
                        {
                            st = typeof(Single);
                        }
                        break;
                    case EType.Float64:
                        {
                            st = typeof(Double);
                        }
                        break;
                    case EType.String:
                        {
                            st = typeof(String);
                        }
                        break;
                    default:
                        {
                            st = mcen.value.GetType();
                        }
                        break;
                }

                return st;
            }

            MetaClass orgmc = mip.express.GetReturnMetaClass();

            if( orgmc is MetaClassCSharp mcc )
            {
                return mcc.csharpType;
            }

            if(orgmc == null )
            {
                Debug.Write("Error 没有发现表达式类型MetaClass!");
                return typeof(object);
            }

            System.Type type = FindCSharpType(orgmc);

            return type;
        }

        public static System.Type FindCSharpType(MetaClass mc)
        {
            System.Type type = null;// typeof( mc );

            if (mc == CoreMetaClassManager.booleanMetaClass)
            {
                //type = typeof(BoolObject);
                type = typeof(System.Boolean);
            }
            else if (mc == CoreMetaClassManager.byteMetaClass)
            {
                //type = typeof(Int8Object);
                type = typeof(System.Byte);
            }
            else if (mc == CoreMetaClassManager.sbyteMetaClass)
            {
                //type = typeof(SInt8Object);
                type = typeof(System.SByte);
            }
            else if (mc == CoreMetaClassManager.int16MetaClass)
            {
                //type = typeof(Int16Object);
                type = typeof(System.Int16);
            }
            else if (mc == CoreMetaClassManager.uint16MetaClass)
            {
                //type = typeof(UInt16Object);
                type = typeof(System.UInt16);
            }
            else if (mc == CoreMetaClassManager.int32MetaClass)
            {
                //type = typeof(Int32Object);
                type = typeof(System.Int32);
            }
            else if (mc == CoreMetaClassManager.uint32MetaClass)
            {
                //type = typeof(UInt32Object);
                type = typeof(System.UInt32);
            }
            else if (mc == CoreMetaClassManager.int64MetaClass)
            {
                //type = typeof(Int64Object);
                type = typeof(System.Int64);
            }
            else if (mc == CoreMetaClassManager.uint64MetaClass)
            {
                //type = typeof(UInt64Object);
                type = typeof(System.UInt64);
            }
            else if (mc == CoreMetaClassManager.float32MetaClass)
            {
                //type = typeof(Float32Object);
                type = typeof(System.Single);
            }
            else if (mc == CoreMetaClassManager.float64MetaClass)
            {
                //type = typeof(Float64Object);
                type = typeof(System.Double);
            }
            else if (mc == CoreMetaClassManager.stringMetaClass)
            {
                //type = typeof(StringObject);
                type = typeof(System.String);
            }
            else if (mc == CoreMetaClassManager.objectMetaClass)
            {
                //type = typeof(SObject);
                type = typeof(System.Object);
            }
            else if (mc == CoreMetaClassManager.arrayMetaClass)
            {
                type = typeof(ArrayObject);
            }
            else
            {
                type = CSharpManager.FindCSharpType(mc.allClassName);
            }

            return type;
        }
    }
    public sealed class MetaInputParamCollectionCSharp
    {
        System.Type[] m_CShpartParamTypes;
        bool m_IsHaveParse = false;

        public static System.Type[]  GetCSharpParamTypes( MetaInputParamCollection mipc )
        {
            //if(m_IsHaveParse )
            //{
            //    return m_CShpartParamTypes;
            //}
            var m_CShpartParamTypes = new System.Type[mipc.count];

            for (int i = 0; i < mipc.count; i++)
            {
                MetaInputParam mip = mipc.metaInputParamList[i];
                m_CShpartParamTypes[i] = MetaInputParamCSharp.GetCSharpType(mip);
            }

            return m_CShpartParamTypes;
        }
    }
    public class MetaDefineParamCSharp : MetaDefineParam
    {
        private ParameterInfo parameterInfo;
        public MetaDefineParamCSharp(MetaFunction mf, ParameterInfo pi)
            :base(pi.Name, mf)
        {
            m_OwnerMetaFunction = mf;

            parameterInfo = pi;

            var defineMetaClassType = MetaTypeCSharp.GetMetaClassByCSharpType(pi.ParameterType);
            MetaType mdt = new MetaType(defineMetaClassType);
            m_MetaVariable = new MetaVariable( pi.Name, EVariableFrom.Argument, null, mf.ownerMetaClass, mdt );
            m_MetaVariable.SetIsDefineMetaType(true);
        }
    }
}
