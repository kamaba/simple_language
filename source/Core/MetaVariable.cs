//****************************************************************************
//  File:      MetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  all variable 's define, if it's iterator style then use IteratorMetaVariable, other custom same style!
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Text;
using static SimpleLanguage.Core.MetaVariable;

namespace SimpleLanguage.Core
{
    public class MetaVariable : MetaBase
    {
        public enum EVariableFrom
        {
            None,
            Static,
            Global,
            Argument,
            LocalStatement,
            Member,
            ArrayInner,
        }

        public bool isStatic { get; protected set; } = false;
        public virtual bool isConst { get; set; } = false;
        public bool isArgument => m_VariableFrom == EVariableFrom.Argument;
        public bool isGlobal => m_VariableFrom == EVariableFrom.Global;
        public bool isArray
        {
            get { return m_DefineMetaType != null ? m_DefineMetaType.isArray : false ; }
        }        
        
        public EVariableFrom variableFrom => m_VariableFrom;
        public MetaType metaDefineType => m_DefineMetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public Token pingToken => m_PintTokenList.Count > 0 ? m_PintTokenList[0] : null;

        protected MetaClass m_OwnerMetaClass = null;
        protected MetaType m_DefineMetaType = null;
        protected EVariableFrom m_VariableFrom;
        protected List<Token> m_PintTokenList = new List<Token>();
        //用来存放扩展包含变量
        protected Dictionary<string, MetaVariable> m_MetaVariableDict = new Dictionary<string, MetaVariable>();

        //protected MetaNewStatements m_FromMetaNewStatementsCreate = null;
        //protected MetaDefineParam m_FromMetaDefineParamCreate = null;
        //protected MetaExpressNode m_FromExpressNodeCreate = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;
        protected MetaVariable() { }
        public MetaVariable( MetaVariable mv )
        {
            m_Name = mv.m_Name;
            m_DefineMetaType = mv.m_DefineMetaType;
            m_OwnerMetaClass = mv.m_OwnerMetaClass;
            m_OwnerMetaBlockStatements = mv.m_OwnerMetaBlockStatements;
            m_MetaVariableDict = mv.m_MetaVariableDict;
            m_PintTokenList = mv.m_PintTokenList;

            isStatic = mv.isStatic;
            isConst = mv.isConst;
            m_VariableFrom = mv.m_VariableFrom;
        }
        public MetaVariable(string _name, EVariableFrom from, MetaBlockStatements mbs, MetaClass ownerClass, MetaType mdt )
        {
            m_Name = _name;
            m_VariableFrom = from;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = ownerClass;
            m_DefineMetaType = mdt;
            if (m_DefineMetaType == null )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
        } 
        public virtual void SetOwnerMetaClass(MetaClass ownerclass)
        {
            m_OwnerMetaClass = ownerclass;
        }
        public void AddPingToken( string path, int beginline, int beginpos, int endline, int endpos )
        {
            var pingToken = new Token(path, ETokenType.None, "", beginline, beginpos);
            pingToken.SetSrouceEnd( endline, endpos );

            var find1 = m_PintTokenList.Find( a=> a.sourceBeginLine == beginline && a.sourceBeginChar == beginpos );
            if( find1 == null )
            {
                m_PintTokenList.Add(pingToken);
            }
        }
        public void AddPingToken( Token token )
        {
            var find1 = m_PintTokenList.Find(
                a => a.sourceBeginLine == token.sourceBeginLine
                && a.sourceBeginChar == token.sourceBeginChar
                && a.sourceEndLine == token.sourceEndLine
                && a.sourceEndChar == token.sourceEndChar
                && a.path == token.path );
            if (find1 == null)
            {
                m_PintTokenList.Add(token);
            }
        }
        public void SetDefineMetaClass(MetaClass defineClass)
        {
            m_DefineMetaType.SetMetaClass(defineClass);
        }
        public void SetMetaDefineType( MetaType mdt )
        {
            m_DefineMetaType = mdt;
        }
        // 这里注释掉是因为，使用token进行定位，而不再使用解析完成后的语句

        public virtual void SetOwnerBlockstatements(MetaBlockStatements mbs)
        {
            m_OwnerMetaBlockStatements = mbs;
        }
        //public void SetFromMetaNewStatementsCreate(MetaNewStatements ns)
        //{
        //    //m_FromMetaNewStatementsCreate = ns;
        //}
        //public void SetFromMetaDefineParamCreate(MetaDefineParam mdp)
        //{
        //    //m_FromMetaDefineParamCreate = mdp;
        //}
        //public void SetFromExpressNodeCreate( MetaExpressNode men)
        //{
        //    //m_FromExpressNodeCreate = men;
        //}
        public virtual void ParseName()
        {

        }
        public virtual void ParseDefineMetaType()
        {

        }
        public virtual bool ParseMetaExpress()
        {
            return true;
        }
        public void GenTemplateMetaVaraible( MetaGenTemplateClass mgt, MetaBlockStatements mbs )
        {
            //m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mgt;
            if(m_DefineMetaType.isTemplate )
            {
                var tmc = mgt.GetMetaGenTemplate(m_DefineMetaType.metaTemplate.name);
                if( tmc != null )
                {
                    m_DefineMetaType.ClearMetaTemplate();
                    m_DefineMetaType.SetMetaClass(tmc.metaType.metaClass);
                }
            }
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
        public virtual string ToStatementString()
        {
            return "";
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[" + m_DefineMetaType.ToFormatString() + "]");
            sb.Append(m_Name);
            return sb.ToString();
        }
    }
}
