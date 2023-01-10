//****************************************************************************
//  File:      MetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  all variable 's define, if it's iterator style then use IteratorMetaVariable, other custom same style!
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaVariable : MetaBase
    {
        public bool isStatic { get; set; } = false;
        public virtual bool isConst { get; set; } = false;
        public bool isArgument { get; set; } = false;
        public bool isTemplate
        {
            get { return m_DefineMetaType.isUseInputTemplate; }
        }
        public bool isArray
        {
            get { return m_DefineMetaType.isArray; }
        }

        public MetaType metaDefineType => m_DefineMetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;

        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaType m_DefineMetaType = null;
        //用来存放扩展包含变量
        protected Dictionary<string, MetaVariable> m_MetaVariableDict = new Dictionary<string, MetaVariable>();
        protected MetaVariable() { }
        public MetaVariable(string _name, MetaBlockStatements mbs, MetaClass ownerClass, MetaType mdt )
        {
            m_Name = _name;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = ownerClass;
            m_DefineMetaType = mdt;
            if(m_DefineMetaType == null )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
        }       
        public virtual void SetOwnerMetaClass(MetaClass ownerclass)
        {
            m_OwnerMetaClass = ownerclass;
        }
        public virtual void SetOwnerBlockstatements(MetaBlockStatements mbs)
        {
            m_OwnerMetaBlockStatements = mbs;
        }
        public void SetDefineMetaClass(MetaClass defineClass)
        {
            m_DefineMetaType.SetMetaClass(defineClass);
        }
        public void SetMetaDefineType( MetaType mdt )
        {
            m_DefineMetaType = mdt;
        }       
        public virtual void Parse()
        {

        }
        public bool AddMetaVariable( MetaVariable mv )
        {
            if(m_MetaVariableDict.ContainsKey(mv.name) )
            {
                return false;
            }
            m_MetaVariableDict.Add(mv.name, mv);
            return true;
        }
        public virtual MetaVariable GetMetaVaraible( string name )
        {
            if( m_MetaVariableDict.ContainsKey( name ))
            {
                return m_MetaVariableDict[name];
            }
            return null;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_DefineMetaType.ToFormatString());
            sb.Append(" ");
            sb.Append(m_Name);
            return sb.ToString();
        }
    }

    public class VisitMetaVariable : MetaVariable
    {
        /*
         * 访问变量 一般使用 $x @x 必须先定义
         * int a = 20; Array arr = Array<int>( 1,2,3); 
         * int b = arr.@a; 这里的@a就是访问变量，使用arr为localMV, 使用m_VisitMetaVariable 是a 如果是常量，则保存
         * 常量的  arr.@0  m_VisitMV = null; m_AtName = "0";  返回值本身就是一个变量，相当于已经访问过了，在defineType
         * 中，返回模版类中的名称
         */
        public enum EVisitType
        {
            AT
        }

        MetaVariable m_LocalMetaVariable = null;
        EVisitType m_VisitType = EVisitType.AT;
        MetaVariable m_VisitMetaVariable = null;
        string m_AtName = "";

        public VisitMetaVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaVariable vmv)
        {
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_LocalMetaVariable = lmv;
            if (lmv.isArray)
            {
                if (vmv == null && string.IsNullOrEmpty(m_AtName))
                {
                    Console.WriteLine("Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_VisitMetaVariable = vmv;

                var gmit = m_LocalMetaVariable.metaDefineType.GetMetaInputTemplateByIndex();
                if (gmit == null)
                {
                    Console.WriteLine("Error 访问的Array中，没有找到模版 名称!!");
                    return;
                }
                m_DefineMetaType = new MetaType(gmit);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_LocalMetaVariable.name);
            if (m_LocalMetaVariable.isArray)
            {
                sb.Append("[");
                //sb.Append(m_DefineMetaType.ToFormatString());
                sb.Append(m_Name);
                sb.Append("]");
                //sb.Append(m_Express.ToFormatString());
            }
            else
            {

            }

            return sb.ToString();
        }
    }
    public class IteratorMetaVariable : MetaVariable
    {
        int m_Index = 0;
        MetaVariable m_LocalMetaVariable = null;
        MetaType m_OrgMetaDefineType = null;
        MetaVariable m_IndexMetaVariable = null;

        public IteratorMetaVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaType orgMC )
        {
            m_Name = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_LocalMetaVariable = lmv;
            m_OrgMetaDefineType = orgMC;
            m_IndexMetaVariable = new MetaVariable("index", mbs, mc, new MetaType(CoreMetaClassManager.int32MetaClass));
            if (lmv.isArray)
            {
                var gmit = m_LocalMetaVariable.metaDefineType.GetMetaInputTemplateByIndex();
                if (gmit == null)
                {
                    Console.WriteLine("Error 访问的Array中，没有找到模版 名称!!");
                    return;
                }
                m_DefineMetaType = new MetaType(gmit);
            }
        }

        public override MetaVariable GetMetaVaraible(string name)
        {
            if( name == "index" )
            {
                return m_IndexMetaVariable;
            }
            if (m_MetaVariableDict.ContainsKey(name))
            {
                return m_MetaVariableDict[name];
            }
            return m_OrgMetaDefineType.metaClass.GetMetaMemberVariableByName( name );
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_LocalMetaVariable.name);
            if (m_LocalMetaVariable.isArray)
            {
                sb.Append("[");
                //sb.Append(m_DefineMetaType.ToFormatString());
                sb.Append(m_Name);
                sb.Append("]");
                //sb.Append(m_Express.ToFormatString());
            }
            else
            {

            }

            return sb.ToString();
        }
    }
}
