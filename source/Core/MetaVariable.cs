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
using System.Diagnostics;
using System.Text;

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
        public void SetIsStatic( bool iss )
        {
            this.isStatic = iss;
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


    public class MetaVisitVariable : MetaVariable
    {
        /*
         * 访问变量 一般使用 $x $x 必须先定义
         * int a = 20; Array arr = Array<int>( 1,2,3); 
         * int b = arr.$a; 这里的$a就是访问变量，使用arr为localMV, 使用m_VisitMetaVariable 是a 如果是常量，则保存
         * 常量的  arr.$0  m_VisitMV = null; m_AtName = "0";  返回值本身就是一个变量，相当于已经访问过了，在defineType
         * 中，返回模版类中的名称
         */
        public enum EVisitType
        {
            Link,
            AT
        }
        public MetaVariable sourceMetaVariable => m_SourceMetaVariable;
        public MetaVariable targetMetaVariable => m_TargetMetaVariable;

        MetaVariable m_SourceMetaVariable = null;
        EVisitType m_VisitType = EVisitType.AT;
        MetaVariable m_TargetMetaVariable = null;
        string m_AtName = "";

        public MetaVisitVariable(MetaVariable source, MetaVariable target)
        {
            m_VisitType = EVisitType.Link;
            m_SourceMetaVariable = source;
            m_TargetMetaVariable = target;
            m_DefineMetaType = target.metaDefineType;
        }
        public int GetIRMemberIndex()
        {
            var mmv = m_SourceMetaVariable as MetaMemberVariable;
            if (mmv != null)
            {
                //return mmv.ownerMetaClass.GetLocalMemberVariableIndex(mmv);
            }
            return -1;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaVariable vmv)
        {
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_SourceMetaVariable = lmv;
            if (lmv.isArray)
            {
                if (vmv == null && string.IsNullOrEmpty(m_AtName))
                {
                    Debug.Write("Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_TargetMetaVariable = vmv;

                var gmit = m_SourceMetaVariable.metaDefineType.GetMetaInputTemplateByIndex();
                if (gmit == null)
                {
                    Debug.Write("Error 访问的Array中，没有找到模版 名称!!");
                    return;
                }
                m_DefineMetaType = new MetaType(gmit);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VisitType == EVisitType.Link)
            {
                if (m_SourceMetaVariable != null)
                {
                    sb.Append("[" + m_SourceMetaVariable.metaDefineType.allName + "]");
                    sb.Append(m_SourceMetaVariable.name);
                    sb.Append(".");
                }
                sb.Append("[" + m_TargetMetaVariable.metaDefineType.allName + "]");
                sb.Append(m_TargetMetaVariable.name);
            }
            else
            {
                sb.Append(m_SourceMetaVariable.name);
                if (m_SourceMetaVariable.isArray)
                {
                    sb.Append("[");
                    //sb.Append(m_DefineMetaType.ToFormatString());
                    sb.Append(m_Name);
                    sb.Append("]");
                    //sb.Append(m_Express.ToFormatString());
                }
                else
                {
                    sb.Append(m_TargetMetaVariable.name);
                }
            }

            return sb.ToString();
        }
    }
    public class MetaIteratorVariable : MetaVariable
    {
#pragma warning disable CS0414 // 字段“MetaIteratorVariable.m_Index”已被赋值，但从未使用过它的值
        int m_Index = 0;
#pragma warning restore CS0414 // 字段“MetaIteratorVariable.m_Index”已被赋值，但从未使用过它的值
        MetaVariable m_LocalMetaVariable = null;
        MetaType m_OrgMetaDefineType = null;
        MetaVariable m_IndexMetaVariable = null;
        MetaVariable m_ValueMetaVariable = null;

        public MetaIteratorVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaType orgMC)
        {
            m_Name = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_LocalMetaVariable = lmv;
            m_OrgMetaDefineType = orgMC;
            m_IndexMetaVariable = new MetaVariable("index", EVariableFrom.ArrayInner, mbs, mc, new MetaType(CoreMetaClassManager.int32MetaClass));
            m_ValueMetaVariable = new MetaVariable("value", EVariableFrom.ArrayInner, mbs, mc, new MetaType(orgMC.metaClass));
            m_IndexMetaVariable.AddPingToken(lmv.pingToken);
            m_ValueMetaVariable.AddPingToken(lmv.pingToken);
            if (lmv.isArray)
            {
                var gmit = m_LocalMetaVariable.metaDefineType.GetMetaInputTemplateByIndex();
                if (gmit == null)
                {
                    Debug.Write("Error 访问的Array中，没有找到模版 名称!!");
                    return;
                }
                m_DefineMetaType = new MetaType(gmit);
            }
            else
            {
                m_DefineMetaType = lmv.metaDefineType;
            }
        }
        public MetaClass GetIteratorMetaClass()
        {
            return m_OrgMetaDefineType.metaClass;
        }


        public override MetaVariable GetMetaVaraible(string name)
        {
            if (name == "index")
            {
                return m_IndexMetaVariable;
            }
            else if (name == "value")
            {
                return m_ValueMetaVariable;
            }
            if (m_MetaVariableDict.ContainsKey(name))
            {
                return m_MetaVariableDict[name];
            }
            return m_OrgMetaDefineType.metaClass.GetMetaMemberVariableByName(name);
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
