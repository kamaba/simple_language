using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaClass
    {
        //#if SupportCSharp
        public System.Type csharpType => m_CSharpType;
        System.Type m_CSharpType;
        //#endif

        public MetaClass(string _name, System.Type type) :
            this(_name)
        {
            m_CSharpType = type;
        }
        public System.Type GetCSharpType()
        {
            if( m_CSharpType == null )
            {
                if( this == CoreMetaClassManager.objectMetaClass )
                {
                    m_CSharpType = typeof(System.Object);
                }
                else
                {
                    switch (eType)
                    {
                        case EType.Null:
                            {
                                m_CSharpType = typeof(System.Nullable);
                            }
                            break;
                        case EType.Int32:
                            {
                                m_CSharpType = typeof(System.Int32);
                            }
                            break;
                        case EType.String:
                            {
                                m_CSharpType = typeof(System.String);
                            }
                            break;
                        default:
                            {
                                m_CSharpType = CSharpManager.FindCSharpType(allName);
                            }
                            break;
                    }
                }
            }
            return m_CSharpType;
        }
        public void ParseCSharp()
        {
            if (m_CSharpType == null) return;

            var preperties = m_CSharpType.GetProperties();

            for( int i = 0; i < preperties.Length; i++ )
            {
                MetaMemberVariable mv = new MetaMemberVariable(this, preperties[i]);

                AddMetaMemberVariable(mv);
            }
            var fields = m_CSharpType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];

                MetaMemberVariable mv = new MetaMemberVariable(this, f );

                AddMetaMemberVariable(mv);
            }

            var methods = m_CSharpType.GetMethods();
            for( int i = 0; i < methods.Length; i++ )
            {
                MetaMemberFunction mmf = new MetaMemberFunction(this, methods[i]);

                AddMetaMemberFunction(mmf);
            }

        }
        public MetaMemberFunction GetCSharpMemberFunctionAndCreateByNameAndInputParamCollect(string name, bool isStatic, MetaInputParamCollection mipc)
        {
            MetaMemberFunction mmf = GetMetaMemberFunctionByNameAndInputParamCollect(name, mipc);
            if( mmf == null && refFromType == RefFromType.CSharp )
            {
                BindingFlags bf = System.Reflection.BindingFlags.Public;
                if(isStatic)
                {
                    bf |= BindingFlags.Static;
                }
                Binder binder = null;

                System.Type[] types = mipc.GetCSharpParamTypes();

                MethodInfo mi = m_CSharpType.GetMethod(name, types );
                if (mi == null) return null;
                MetaMemberFunction cmmf = new MetaMemberFunction(this, mi);
                AddMetaMemberFunction(cmmf, false );
                return cmmf;
            }
            return mmf;
        }
        public MetaVariable GetCSharpMemberVariableAndCreateByName( string name )
        {
            MetaMemberVariable mmv = GetMetaMemberVariableByName(name);
            if(mmv == null && refFromType == RefFromType.CSharp )
            {
                FieldInfo fi = m_CSharpType.GetField(name);
                if (fi != null )
                {
                    MetaMemberVariable cmmv = new MetaMemberVariable(this, fi);
                    AddMetaMemberVariable(cmmv);
                    return cmmv;
                }
                PropertyInfo pi = m_CSharpType.GetProperty(name);
                if( pi != null )
                {
                    MetaMemberFunction mmf = new MetaMemberFunction(this, pi);
                    AddMetaMemberFunction(mmf, false);
                    return mmf;
                }
            }
            return mmv;
        }
        public MetaBase GetCSharpMetaBaseOrCreateMetaBaseByName(string name, bool isStatic, MetaInputParamCollection mipc)
        {
            if( mipc != null )
            {
                return GetCSharpMemberFunctionAndCreateByNameAndInputParamCollect(name, isStatic, mipc);
            }
            else
            {
                return GetCSharpMemberVariableAndCreateByName(name);
            }
        }
    }
}
