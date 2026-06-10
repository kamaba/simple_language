//****************************************************************************
//  File:      MetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  all variable 's define, if it's iterator style then use IteratorMetaVariable, other custom same style!
//****************************************************************************

using SimpleLanguage.Compile;

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaVariable : MetaBase, IComparable<MetaVariable>
    {
        public enum EVariableFrom
        {
            None,
            Static,
            Global,
            Argument,
            LocalStatement,
            ArrayValue,
            ClassMember,
            EnumMember,
            DataMember,
        }

        public static int s_ConstLevel = 10000000;
        public static int s_IsHaveRetStaticLevel = 100000000;
        public static int s_NoHaveRetStaticLevel = 200000000;
        public static int s_DefineMetaTypeLevel = 1000000000;
        public static int s_ExpressLevel = 1500000000;
        public bool isDefineMetaType => m_IsDefineMetaType;
        public virtual bool isStatic => m_IsStatic;
        public virtual bool isConst => m_IsConst;
        public virtual bool isParsed => m_IsParsed;
        public bool isArgument => m_VariableFrom == EVariableFrom.Argument;
        public bool isGlobal => m_VariableFrom == EVariableFrom.Global;
        public bool isArray
        {
            get { return m_IsDefineMetaType ? (m_DefineMetaType != null ? m_DefineMetaType.IsArray() : false) : (m_RealMetaType != null ? m_RealMetaType.IsArray() : false); }
        }
        public int parseLevel { get; set; } = -1;

        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;
        public EVariableFrom variableFrom => m_VariableFrom;
        public MetaType defineMetaType => m_DefineMetaType;
        public MetaType realMetaType => m_RealMetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaBase as MetaClass;
        public MetaData ownerMetaData => m_OwnerMetaBase as MetaData;
        public MetaEnum ownerMetaEnum => m_OwnerMetaBase as MetaEnum;
        public MetaBase ownerMetaBase => m_OwnerMetaBase;
        public MetaVariable sourceMetaVariable => m_SourceMetaVariable;

        #region 属性

        // MetaData / MetaEnum 不再继承自 MetaClass，故 owner 升级为 MetaBase，
        // 同时仍通过 ownerMetaClass / ownerMetaData / ownerMetaEnum 提供分类视图。
        protected MetaBase m_OwnerMetaBase = null;
        protected MetaType m_DefineMetaType = null;
        protected MetaType m_RealMetaType = null;
        protected EVariableFrom m_VariableFrom;
        protected bool m_IsParsed = false;
        protected bool m_IsStatic = false;
        protected bool m_IsConst = false;
        protected bool m_IsDefineMetaType = false;      //该字段是表明，该类型使用了定义类型， 如果是var 或者是没定义的，则可以使用真实的类型
        //用来存放扩展包含变量
        protected Dictionary<string, MetaVariable> m_MetaVariableDict = new Dictionary<string, MetaVariable>();
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaVariable m_SourceMetaVariable = null;
        #endregion

        protected MetaVariable() { }
        public MetaVariable(MetaVariable mv) : base(mv)
        {
            m_OwnerMetaBase = mv.m_OwnerMetaBase;
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
        public MetaVariable(string _name, EVariableFrom from, MetaBlockStatements mbs, MetaBase ownerBase, MetaType mdt )
        {
            m_Name = _name;
            m_VariableFrom = from;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = ownerBase;
            m_DefineMetaType = mdt;
            if (m_DefineMetaType == null )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                m_IsDefineMetaType = false;
            }
            else
            {
                m_IsDefineMetaType = true;
            }
            if (mdt != null)
            {
                m_RealMetaType = new MetaType(mdt);
            }
        } 
        /// <summary>宿主可为 <see cref="MetaClass"/> / <see cref="MetaData"/> / <see cref="MetaEnum"/>，内部按需使用分类属性。</summary>
        public virtual void SetOwnerMetaBase(MetaBase ownerBase)
        {
            m_OwnerMetaBase = ownerBase;
        }
        public void SetSourceMetaVariable( MetaVariable mv  )
        {
            m_SourceMetaVariable = mv;
        }
        public void SetIsStatic( bool iss )
        {
            this.m_IsStatic = iss;
        }
        public void SetIsConst(bool isc)
        {
            this.m_IsConst = isc;
        }
        public void SetIsDefineMetaType( bool flag )
        {
            this.m_IsDefineMetaType = flag;
        }
        public MetaType GetFinalMetaType()
        {
            if (this.m_IsDefineMetaType)
            {
                return m_DefineMetaType;
            }
            else
            {
                if (m_RealMetaType != null)
                {
                    return m_RealMetaType;
                }
            }
            return null;
        }
        public MetaClass GetFinalTemplateMetaClass()
        {
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            if (m_IsDefineMetaType)
            {                
                if (m_DefineMetaType.metaClass is MetaGenTemplateClass mgtc)
                {
                    mc = mgtc.metaTemplateClass;
                }
                else
                {
                    mc = m_DefineMetaType.metaClass;
                }
            }
            else 
            {
                if(m_RealMetaType != null )
                {
                    if (m_RealMetaType.metaClass is MetaGenTemplateClass mgtc)
                    {
                        mc = mgtc.metaTemplateClass;
                    }
                    else
                    {
                        mc = m_RealMetaType.metaClass;
                    }
                }
            }
            return mc;
        }
        public void SetRealMetaType( MetaType realMt )
        {
            this.m_RealMetaType = realMt;
        }
        public bool GetIsCanCanIterate()
        {

            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            if (m_IsDefineMetaType)
            {
                if (m_DefineMetaType.isEnum)
                {
                    return true;
                }
                else if (m_DefineMetaType.isData)
                {
                    return false;
                }
                else
                {
                    if (m_DefineMetaType.metaClass is MetaGenTemplateClass mgtc)
                    {
                        mc = mgtc.metaTemplateClass;
                    }
                    else
                    {
                        mc = m_DefineMetaType.metaClass;
                    }
                }
            }
            else
            {
                if (m_RealMetaType.isEnum)
                {
                    return true;
                }
                else if (m_RealMetaType.isData)
                {
                    return false;
                }
                else
                {
                    if (m_RealMetaType != null)
                    {
                        if (m_RealMetaType.metaClass is MetaGenTemplateClass mgtc)
                        {
                            mc = mgtc.metaTemplateClass;
                        }
                        else
                        {
                            mc = m_RealMetaType.metaClass;
                        }
                    }
                }
            }
            MetaClass findMc = ClassManager.instance.GetClassByName("Core.IIterable");
            if (mc.GetInterfaceByMetaClass(findMc))
            {
                return true;
            }
            MetaClass findMc2 = ClassManager.instance.GetClassByName("Core.IIterable<T>", 1 );
            if (mc.GetInterfaceByMetaClass(findMc2))
            {
                return true;
            }   
            return false;
        }

        public MetaClass GetOwnerClassTemplateClass()
        {
            if(m_OwnerMetaBase is MetaGenTemplateClass mgtc )
            {
                return mgtc.metaTemplateClass;
            }
            return m_OwnerMetaBase as MetaClass;
        }
        public virtual MetaClass GetTemplateMetaClass()
        {
            if( isArray )
            {
                return CoreMetaClassManager.arrayMetaClass;
            }

            if( m_IsDefineMetaType )
            {
                if ( m_DefineMetaType.metaClass is MetaGenTemplateClass mgtc)
                {
                    return mgtc.metaTemplateClass;
                }
                return m_DefineMetaType.metaClass;
            }
            else
            {
                if( m_RealMetaType.metaClass is MetaGenTemplateClass mgtc )
                {
                    return mgtc.metaTemplateClass;
                }
                return m_RealMetaType.metaClass;
            }
        }
        public void SetMetaDefineType( MetaType mdt )
        {
            m_DefineMetaType = mdt;
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
        public virtual void CalcParseLevel()
        {

        }
        public virtual void CreateMetaExpress()
        {

        }
        public virtual bool ParseMetaExpress()
        {
            return true;
        }
        public int CompareTo(MetaVariable mmv)
        {
            if (ReferenceEquals(mmv, null))
                return 1;

            if (this.parseLevel > mmv.parseLevel)
                return 1;
            if (this.parseLevel < mmv.parseLevel)
                return -1;
            return 0;
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

            if( isDefineMetaType )
                sb.Append("[" + m_DefineMetaType.ToString() + "]");
            else
                sb.Append("[" + m_RealMetaType.ToString() + "]");
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
        public MetaExpressNodeBase visitExpressNode => m_VisitExpressNode;
        public MetaConstExpressNode fastVisitConstExpressNode => m_VisitExpressNode as MetaConstExpressNode;

        private MetaVariable m_SourceMetaVariable = null;
        private EVisitType m_VisitType = EVisitType.AT;
        //private MetaCallLink m_TargetMetaVisitCallLink = null;
        string m_AtName = "";
        private bool m_FastVisit = false;
        private MetaExpressNodeBase m_VisitExpressNode = null;
        private int? m_Index = null;

        public MetaVisitVariable(MetaVariable source, MetaVariable target)
        {
            m_VisitType = EVisitType.Link;
            m_SourceMetaVariable = source;
           // m_TargetMetaVariable = target;
            m_DefineMetaType = target.defineMetaType;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaConstExpressNode mvv)
        {
            m_VariableFrom = EVariableFrom.ArrayValue;
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_IsDefineMetaType = lmv.isDefineMetaType;   
            m_VisitExpressNode = mvv;
            m_FastVisit = true;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaCallLink mvv)
        {
            m_VariableFrom = EVariableFrom.ArrayValue;
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_FastVisit = false;
            if (lmv.isArray)
            {
                if (mvv == null && string.IsNullOrEmpty(m_AtName))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_VisitExpressNode = new MetaCallLinkExpressNode(mvv);

                if (mvv.visitNodeList.Count == 1)
                {
                    if (mvv.visitNodeList[0].constValueExpress != null)
                    {
                        if (mvv.visitNodeList[0].constValueExpress.eType == EType.Int32)
                        {
                            m_Index = (int)mvv.visitNodeList[0].constValueExpress.value;
                            m_FastVisit = true;
                        }
                    }
                }
            }
            m_IsDefineMetaType = lmv.isDefineMetaType;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaOpExpressNode moe )
        {
            m_VariableFrom = EVariableFrom.ArrayValue;
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_FastVisit = false;
            if (lmv.isArray)
            {
                if (moe == null && string.IsNullOrEmpty(m_AtName))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_VisitExpressNode = moe;
            }
            m_IsDefineMetaType = lmv.isDefineMetaType;
        }
        public override void ParseDefineMetaType()
        {
            MetaType getMt = null;
            if ( this.m_SourceMetaVariable.isDefineMetaType)
            {
                if (m_SourceMetaVariable.defineMetaType.IsArray() )
                {
                    var mtlist = m_SourceMetaVariable.defineMetaType.defineTemplateMetaTypeList;
                    if (mtlist.Count > 0 )
                    {
                        getMt = new MetaType( mtlist[0] );
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, "ParseDefineMetaType not array ");
                }
            }
            
            if(getMt == null )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            else
            {
                m_DefineMetaType = getMt;
            }
        }
        public override void  ParseRealMetaType()
        {
            if(this.m_SourceMetaVariable.isDefineMetaType )
            {
                return;
            }
            if (m_SourceMetaVariable.realMetaType.IsArray() )
            {
                var mtlist = m_SourceMetaVariable.realMetaType.defineTemplateMetaTypeList;
                if( mtlist.Count == 1 )
                {
                    m_RealMetaType = new MetaType(mtlist[0]);
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "ParseDefineRealMetaType not array ");
                }
            }
            else
            {
                m_RealMetaType = new MetaType(m_SourceMetaVariable.realMetaType);
            }
        }
        public void SetNotUseFast()
        {
            m_FastVisit = false;
        }        
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VisitType == EVisitType.Link)
            {
                if (m_SourceMetaVariable != null)
                {
                    sb.Append("[" + m_SourceMetaVariable.defineMetaType.name + "]");
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
        int m_Index = 0;
        MetaVariable m_ContentMetaVariable = null;
        //MetaType m_OrgMetaDefineType = null;
        //MetaVariable m_IndexMetaVariable = null;
        //MetaVariable m_ValueMetaVariable = null;
        FileMetaClassDefine m_FileMetaClassDefine = null;
        private Token m_VariableNameToken = null;

        public MetaIteratorVariable(FileMetaClassDefine _fmcl, Token variableNameToken, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv )
        {
            m_FileMetaClassDefine = _fmcl;
            m_VariableFrom = EVariableFrom.LocalStatement;
            m_VariableNameToken = variableNameToken;
            m_Name = variableNameToken.lexeme.ToString();
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_ContentMetaVariable = lmv;
            //m_OrgMetaDefineType = orgMC;
            //m_IndexMetaVariable = new MetaVariable("index", EVariableFrom.ArrayInner, mbs, mc, new MetaType(CoreMetaClassManager.int32MetaClass));
            //m_ValueMetaVariable = new MetaVariable("value", EVariableFrom.ArrayInner, mbs, mc, new MetaType(orgMC.metaClass));
            //m_IndexMetaVariable.AddPingToken(lmv.pingToken);
            //m_ValueMetaVariable.AddPingToken(lmv.pingToken);
        }
        public override void ParseRealMetaType()
        {
            if(m_FileMetaClassDefine != null )
            {
                m_DefineMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, m_FileMetaClassDefine );
                m_IsDefineMetaType = true;
            }
            else
            {
                if( m_DefineMetaType == null )
                {
                    if(m_ContentMetaVariable.isDefineMetaType )
                    {
                        m_DefineMetaType = m_ContentMetaVariable.defineMetaType.GetMetaTypeByIndex(0);
                    }
                    else
                    {
                        m_DefineMetaType = m_ContentMetaVariable.realMetaType.GetMetaTypeByIndex(0);
                    }
                }
            }
            m_RealMetaType = new MetaType(m_DefineMetaType);
            return;
        }
        public override MetaVariable GetMetaVariable(string name)
        {
            //if (name == "index")
            //{
            //    return m_IndexMetaVariable;
            //}
            //else if (name == "value")
            //{
            //    return m_ValueMetaVariable;
            //}
            if (m_MetaVariableDict.ContainsKey(name))
            {
                return m_MetaVariableDict[name];
            }
            //return m_OrgMetaDefineType.metaClass.GetMetaMemberVariableByName(name);
            return null;
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
