//****************************************************************************
//  File:      IRMetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.IR
{
    public enum IRMetaVariableFrom
    {
        None,
        Argument,
        LocalStatement,
        Member,
        Static,
        Global,
        Return,
    }
    public class IRMetaVariable
    {
        public MetaExpressNode express => m_ExpressNode;
        public IRMetaType irMetaType => m_IRMetaType;
        public IRMetaVariableFrom irMetaVariableFrom => m_IRMetaVariableFrom;
        public int id => m_Id;
        public string name => m_Name;
        public int index => m_Index;
        public List<IRData> irDataList => m_IRDataList;


        private MetaExpressNode m_ExpressNode = null;
        private List<IRData> m_IRDataList = new List<IRData>();
        private IRMetaType m_IRMetaType = null;
        private IRMetaVariableFrom m_IRMetaVariableFrom = IRMetaVariableFrom.None;
        private bool m_IsTemplate = false;
        private int m_Id = -1;
        private int m_Index = -1;
        private string m_Name = "";
        private MetaVariable m_MetaVariable = null;

        public IRMetaVariable( MetaVariable mv, int index = -1 )
        {
            m_MetaVariable = mv;
            m_Id = mv.GetHashCode();
            m_Index = index;
            m_Name = mv.ownerMetaBlockStatements?.ownerMetaFunction.name + (mv.isStatic?"_static":"_local") + "[" + mv.name + "]";
            if( mv.variableFrom == MetaVariable.EVariableFrom.Member )
            {
                if( mv.isStatic )
                {
                    m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
                }
                else
                {
                    m_IRMetaVariableFrom = IRMetaVariableFrom.Member;
                }
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.Argument )
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.Argument;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.LocalStatement )
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.LocalStatement;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.Global )
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.Global;
            }
            else
            {
                Log.AddGenIR(EError.None, "IRMetaVariable 没有找到对应的from ");
            }
            //if( mv.metaDefineType.eType == EMetaTypeType.Template )
            //{
            //    m_IsTemplate = true;
            //    m_TemplateName = IRManager.GetIRNameByMetaType(mv.metaDefineType);
            //    m_IRMetaClass = new IRMetaClass(IRManager.instance, mv.metaDefineType.metaTemplate.name);
            //    if( m_IRMetaClass == null )
            //    {

            //    }
            //}
            //else 
            m_IRMetaType = new IRMetaType(mv.metaDefineType);
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberEnum mme)
        {
            m_MetaVariable = mme;
            m_Id = mme.GetHashCode();
            m_Name = mme.ownerMetaClass.allClassName + "." + mme.name;
            m_ExpressNode = mme.express;
            m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            //m_IRMetaClass = irmc;
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberData mmd)
        {
            m_MetaVariable = mmd;
            m_Id = mmd.GetHashCode();
            m_Name = mmd.ownerMetaClass.allClassName + "." + mmd.name;
            m_ExpressNode = mmd.expressNode;
            m_IRMetaVariableFrom = mmd.isStatic ? IRMetaVariableFrom.Static : IRMetaVariableFrom.Member;
            //m_IRMetaClass = irmc;
        }
        public IRMetaVariable( IRMetaClass irmc, MetaMemberVariable mmv, int index = -1 )
        {
            m_MetaVariable = mmv;
            m_Id = mmv.GetHashCode();
            m_Index = index;
            m_Name = mmv.ownerMetaClass.allClassName + "." + mmv.name;
            m_ExpressNode = mmv.express;
            if (mmv.isStatic || mmv.isConst )
                m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            else
                m_IRMetaVariableFrom = IRMetaVariableFrom.Member;

            m_IRMetaType = new IRMetaType(mmv.metaDefineType);
        }
        public void SetExpress( MetaExpressNode men )
        {
            this.m_ExpressNode = men;
        }
        public void SetIRDataList( List<IRData> list )
        {
            this.m_IRDataList = list;
        }
        public override string ToString()
        {
            return name;
        }
    }
}
