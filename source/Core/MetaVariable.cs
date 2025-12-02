//****************************************************************************
//  File:      MetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  all variable 's define, if it's iterator style then use IteratorMetaVariable, other custom same style!
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Parse;
using System.Collections.Generic;
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
        public bool isDefineMetaType => m_IsDefineMetaType;
        public virtual bool isStatic => m_IsStatic;
        public virtual bool isConst => m_IsConst;
        public virtual bool isParsed => m_IsParsed;
        public virtual bool isIterate => m_IsIterate;
        public bool isArgument => m_VariableFrom == EVariableFrom.Argument;
        public bool isGlobal => m_VariableFrom == EVariableFrom.Global;
        public bool isArray
        {
            get { return m_IsDefineMetaType ? (m_DefineMetaType != null ? m_DefineMetaType.isArray : false) : (m_RealMetaType != null ? m_RealMetaType.isArray : false); }
        }

        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;
        public EVariableFrom variableFrom => m_VariableFrom;
        public  MetaType metaDefineType => m_DefineMetaType;
        public MetaType realMetaType => m_RealMetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public MetaVariable sourceMetaVariable => m_SourceMetaVariable;
        public Token pingToken => m_PintTokenList.Count > 0 ? m_PintTokenList[0] : null;

        #region 属性

        protected MetaClass m_OwnerMetaClass = null;
        protected MetaType m_DefineMetaType = null;
        protected MetaType m_RealMetaType = null;
        protected EVariableFrom m_VariableFrom;
        protected List<Token> m_PintTokenList = new List<Token>();
        protected bool m_IsParsed = false;
        protected bool m_IsStatic = false;
        protected bool m_IsConst = false;
        protected bool m_IsIterate = false;
        protected bool m_IsDefineMetaType = false;      //该字段是表明，该类型使用了定义类型， 如果是var 或者是没定义的，则可以使用真实的类型
        //用来存放扩展包含变量
        protected Dictionary<string, MetaVariable> m_MetaVariableDict = new Dictionary<string, MetaVariable>();
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaVariable m_SourceMetaVariable = null;
        #endregion

        protected MetaVariable() { }
        public MetaVariable(MetaVariable mv) : base(mv)
        {
            m_OwnerMetaClass = mv.m_OwnerMetaClass;
            m_DefineMetaType = new MetaType( mv.m_DefineMetaType );
            if(mv.m_RealMetaType != null )
                m_RealMetaType = new MetaType(mv.m_RealMetaType);
            m_VariableFrom = mv.m_VariableFrom;
            m_PintTokenList = mv.m_PintTokenList;
            m_IsStatic = mv.m_IsStatic;
            m_IsConst = mv.m_IsConst;
            m_IsParsed = mv.m_IsParsed;
            m_SourceMetaVariable = mv;
            m_IsDefineMetaType = mv.m_IsDefineMetaType;

            foreach ( var v in mv.m_MetaVariableDict)
            {
                m_MetaVariableDict.Add(v.Key, new MetaVariable( v.Value ) );
            }
            m_OwnerMetaBlockStatements = mv.m_OwnerMetaBlockStatements;
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
            m_RealMetaType = new MetaType(m_DefineMetaType);
        } 
        public virtual void SetOwnerMetaClass(MetaClass ownerclass)
        {
            m_OwnerMetaClass = ownerclass;
        }
        public void SetIsStatic( bool iss )
        {
            this.m_IsStatic = iss;
        }
        public void SetIsDefineMetaType( bool flag )
        {
            this.m_IsDefineMetaType = flag;
        }
        public void SetRealMetaType( MetaType realMt )
        {
            this.m_RealMetaType = realMt;
            this.m_IsIterate = realMt.isIterate;
        }
        public MetaClass GetOwnerClassTemplateClass()
        {
            if( isArray )
            {
                return CoreMetaClassManager.arrayMetaClass;
            }

            if( m_OwnerMetaClass is MetaGenTemplateClass mgtc )
            {
                return mgtc.metaTemplateClass;
            }
            return m_OwnerMetaClass;
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
        public void SetIsIterate( bool iterate )
        {
            this.m_IsIterate = iterate;
        }
        public virtual void SetOwnerBlockstatements(MetaBlockStatements mbs)
        {
            m_OwnerMetaBlockStatements = mbs;
        }
        public virtual void ParseDefineMetaType()
        {

        }
        public virtual void ParseRealMetaType()
        {

        }
        public virtual bool Parse()
        {
            return true;
        }
        public virtual void CreateMetaExpress()
        {

        }
        public virtual bool ParseMetaExpress()
        {
            return true;
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
        public virtual MetaVariable GetMetaVariable( string name )
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
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[" + m_DefineMetaType.ToString() + "]");
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
        public bool fastVisit => m_FastVisit;
        public MetaVariable sourceMetaVariable => m_SourceMetaVariable;
        public MetaCallLink targetMetaVisitCallLink => m_TargetMetaVisitCallLink;
        public MetaConstExpressNode fastVisitConstExpressNode => m_FastVisitConstExpressNode;

        MetaVariable m_SourceMetaVariable = null;
        EVisitType m_VisitType = EVisitType.AT;
        MetaCallLink m_TargetMetaVisitCallLink = null;
        string m_AtName = "";
        private bool m_FastVisit = false;
        private MetaConstExpressNode m_FastVisitConstExpressNode = null;
        private int? m_Index = null;

        public MetaVisitVariable(MetaVariable source, MetaVariable target)
        {
            m_VisitType = EVisitType.Link;
            m_SourceMetaVariable = source;
           // m_TargetMetaVariable = target;
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
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaConstExpressNode mvv)
        {
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_IsDefineMetaType = lmv.isDefineMetaType;                 
            m_FastVisitConstExpressNode = mvv;
            m_FastVisit = true;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaCallLink mvv )
        {
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_FastVisit = false;
            if (lmv.isArray)
            {
                if (mvv == null && string.IsNullOrEmpty(m_AtName))
                {
                    Log.AddInStructMeta(EError.None, "Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_TargetMetaVisitCallLink = mvv;

                if( mvv.visitNodeList.Count == 1 )
                {
                    if( mvv.visitNodeList[0].constValueExpress != null )
                    {
                        if(mvv.visitNodeList[0].constValueExpress.eType == EType.Int32 )
                        {
                            m_Index = (int)mvv.visitNodeList[0].constValueExpress.value;
                        }
                    }
                }
            }
        }
        public override void ParseDefineMetaType()
        {
            MetaType getMt = null;
            if (m_SourceMetaVariable.isDefineMetaType)
            {
                if (m_SourceMetaVariable.metaDefineType.isArray)
                {
                    List<int> arraydim = m_SourceMetaVariable.metaDefineType.arrayDimensionLengthList;
                    if ( arraydim.Count > 1 )
                    {
                        getMt = new MetaType(m_SourceMetaVariable.metaDefineType.metaClass);
                        getMt.SetArrayDimensionByFrontMetaType(m_SourceMetaVariable.metaDefineType);
                    }    
                    else
                    {
                        getMt = new MetaType(m_SourceMetaVariable.metaDefineType.metaClass);
                    }
                }
                else
                {

                }
            }
            
            if(getMt == null )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            else
            {
                m_DefineMetaType = new MetaType(getMt);
            }
        }
        public override void  ParseRealMetaType()
        {
            if (m_SourceMetaVariable.realMetaType.isArray)
            {
                List<int> arraydim = m_SourceMetaVariable.realMetaType.arrayDimensionLengthList;
                if (arraydim.Count > 1 )
                {
                    m_RealMetaType = new MetaType(m_SourceMetaVariable.realMetaType.metaClass);
                    m_RealMetaType.SetArrayDimensionByFrontMetaType(m_SourceMetaVariable.realMetaType);
                }
                else
                {
                    if (m_Index != null && m_Index > 0 && m_Index < m_SourceMetaVariable.realMetaType.arrayMetaTypeList.Count)
                    {
                        m_RealMetaType = m_SourceMetaVariable.realMetaType.arrayMetaTypeList[(int)m_Index];
                    }
                    else
                    {
                        m_RealMetaType = new MetaType(m_SourceMetaVariable.realMetaType.metaClass);
                    }
                }
            }
            else
            {
                m_RealMetaType = new MetaType(m_SourceMetaVariable.realMetaType.metaClass);
            }
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VisitType == EVisitType.Link)
            {
                if (m_SourceMetaVariable != null)
                {
                    sb.Append("[" + m_SourceMetaVariable.metaDefineType.name + "]");
                    sb.Append(m_SourceMetaVariable.name);
                    sb.Append(".");
                }
                //sb.Append("[" + m_TargetMetaVisitNode..name + "]");
                //sb.Append(m_TargetMetaVariable.name);
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
                    //sb.Append(m_TargetMetaVariable.name);
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
        MetaVariable m_ContentMetaVariable = null;
        MetaType m_OrgMetaDefineType = null;
        MetaVariable m_IndexMetaVariable = null;
        MetaVariable m_ValueMetaVariable = null;
        FileMetaClassDefine m_FileMetaClassDefine = null;
        private Token m_VariableNameToken = null;

        public MetaIteratorVariable(FileMetaClassDefine _fmcl, Token variableNameToken, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaType orgMC)
        {
            m_VariableFrom = EVariableFrom.LocalStatement;
            m_FileMetaClassDefine = _fmcl;
            m_VariableNameToken = variableNameToken;
            m_Name = variableNameToken.lexeme.ToString();
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_ContentMetaVariable = lmv;
            m_OrgMetaDefineType = orgMC;
            m_IndexMetaVariable = new MetaVariable("index", EVariableFrom.ArrayInner, mbs, mc, new MetaType(CoreMetaClassManager.int32MetaClass));
            m_ValueMetaVariable = new MetaVariable("value", EVariableFrom.ArrayInner, mbs, mc, new MetaType(orgMC.metaClass));
            m_IndexMetaVariable.AddPingToken(lmv.pingToken);
            m_ValueMetaVariable.AddPingToken(lmv.pingToken);
        }
        public override bool Parse()
        {
            if(m_FileMetaClassDefine != null )
            {
                m_DefineMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, m_FileMetaClassDefine );
                m_IsDefineMetaType = true;
            }
            else
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            if (m_ContentMetaVariable.isArray)
            {
                var gmit = m_ContentMetaVariable.realMetaType;
                if (gmit == null)
                {
                    Log.AddInStructMeta(EError.None, "Error 访问的Array中，没有找到模版 名称!!");
                    return false;
                }
                m_RealMetaType = new MetaType(gmit);
            }
            else
            {
                m_RealMetaType = new MetaType(m_ContentMetaVariable.realMetaType.metaClass);
            }
            return true;
        }
        public MetaClass GetIteratorMetaClass()
        {
            return m_OrgMetaDefineType.metaClass;
        }


        public override MetaVariable GetMetaVariable(string name)
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

            sb.Append(m_ContentMetaVariable.name);
            if (m_ContentMetaVariable.isArray)
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
        public override string ToString()
        {
            return m_Name;
        }
    }
}
