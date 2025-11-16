//****************************************************************************
//  File:      MetaClassCSharp.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************

using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaClassCSharp : MetaClass
    {
        public System.Type csharpType => m_CSharpType;

        private System.Type m_CSharpType = null;

        public MetaClassCSharp(string _name, System.Type type) :
            base( _name, EClassDefineType.InnerDefine )
        {
            m_CSharpType = type;
            m_RefFromType = RefFromType.CSharp;
        }
        public void ParseCSharp()
        {
            if (m_CSharpType == null) return;

            var preperties = m_CSharpType.GetProperties();

            for( int i = 0; i < preperties.Length; i++ )
            {
                MetaMemberVariableCSharp mv = new MetaMemberVariableCSharp(this, preperties[i]);

                AddMetaMemberVariable(mv);
            }
            var fields = m_CSharpType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];

                MetaMemberVariableCSharp mv = new MetaMemberVariableCSharp(this, f );

                AddMetaMemberVariable(mv);
            }

            var methods = m_CSharpType.GetMethods();
            for( int i = 0; i < methods.Length; i++ )
            {
                MetaMemberFunctionCSharp mmf = new MetaMemberFunctionCSharp(this, methods[i]);

                AddMetaMemberFunction(mmf);
            }
        }

        public override MetaMemberVariable GetMetaMemberVariableByName(string name)
        {
            MetaMemberVariable mmv = base.GetMetaMemberVariableByName(name);
            
            if( mmv != null )
            {
                return mmv;
            }
            if (m_RefFromType == RefFromType.CSharp)
            {
                mmv = GetCSharpMemberVariableAndCreateByName(name);
            }
            return mmv;
        }
        public MetaMemberVariable GetCSharpMemberVariableAndCreateByName(string name)
        {
            FieldInfo fi = m_CSharpType.GetField(name);
            if (fi != null)
            {
                MetaMemberVariableCSharp cmmv = new MetaMemberVariableCSharp(this, fi);
                AddMetaMemberVariable(cmmv);
                return cmmv;
            }
            //PropertyInfo pi = m_CSharpType.GetProperty(name);
            //if (pi != null)
            //{
            //    MetaMemberFunction mmf = new MetaMemberFunction(this, pi);
            //    AddMetaMemberFunction(mmf, false);
            //    return mmf;
            //}
            return null;
        }
        public override MetaMemberFunction GetMetaMemberFunctionByNameAndInputTemplateInputParamCount(string name, int templateParamCount, MetaInputParamCollection mmpc, bool isIncludeExtendClass = true)
        {
            MetaMemberFunction findmmf = base.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount(name, templateParamCount, mmpc, isIncludeExtendClass);

            if( findmmf != null )
            {
                return findmmf;
            }

            if (m_RefFromType == RefFromType.CSharp)
            {
                findmmf = GetCSharpMemberFunctionAndCreateByNameAndInputParamCollect(name, false, mmpc );
            }
            return findmmf;
        }

        public MetaMemberFunctionCSharp GetCSharpMemberFunctionAndCreateByNameAndInputParamCollect(string name, bool isStatic, MetaInputParamCollection mipc)
        {
            BindingFlags bf = System.Reflection.BindingFlags.Public;
            if (isStatic)
            {
                bf |= BindingFlags.Static;
            }
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            Binder binder = null;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值

            System.Type[] types = MetaInputParamCollectionCSharp.GetCSharpParamTypes(mipc);

            MethodInfo mi = m_CSharpType.GetMethod(name, types);
            if (mi == null) return null;
            MetaMemberFunctionCSharp cmmf = new MetaMemberFunctionCSharp(this, mi);
            AddMetaMemberFunction(cmmf);
            return cmmf;
        }
    }
}
