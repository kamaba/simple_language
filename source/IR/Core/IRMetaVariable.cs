//****************************************************************************
//  File:      IRMetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.Core;

namespace SimpleLanguage.IR
{
    public enum IRMetaVariableFrom
    {
        None,
        Argument,
        LocalStatement,
        Member,
        Static,
        Return,
        Member2,
    }
    public class IRMetaVariable
    {
        public MetaExpressNode express => m_ExpressNode;
        public IRMetaClass irMetaClass => m_IRMetaClass;
        public IRMetaVariableFrom irMetaVariableFrom => m_IRMetaVariableFrom;

        MetaVariable m_MetaVariable = null;
        public int id { get; set; } = 0;
        public string name { get; set; }
        public int index { get; set; } = 0;
        public bool isTemplate { get; set; } = false;

        private MetaExpressNode m_ExpressNode = null;
        private IRMetaClass m_IRMetaClass = null;
        private IRMetaVariableFrom m_IRMetaVariableFrom = IRMetaVariableFrom.None;

        public IRMetaVariable( MetaVariable mv )
        {
            m_MetaVariable = mv;
            id = mv.GetHashCode();
            name = mv.ownerMetaBlockStatements?.ownerMetaFunction.name + "_local[" + mv.name + "]";
            m_IRMetaVariableFrom = mv.variableFrom == MetaVariable.EVariableFrom.Argument 
                ?  IRMetaVariableFrom.Argument : IRMetaVariableFrom.LocalStatement;
            //m_IRMetaClass = IRManager.instance.GetIRMetaClassById(mv.meta)
            if( mv.metaDefineType.templateMetaClass != null )
            {
                m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(mv.metaDefineType.templateMetaClass.allClassName);
            }
            else
            {
                m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(mv.metaDefineType.metaClass.allClassName);
            }
            isTemplate = mv.isTemplate;
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberEnum mme)
        {
            m_MetaVariable = mme;
            id = mme.GetHashCode();
            name = mme.ownerMetaClass.allClassName + "." + mme.name;
            m_ExpressNode = mme.express;
            m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            m_IRMetaClass = irmc;
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberData mmd)
        {
            m_MetaVariable = mmd;
            id = mmd.GetHashCode();
            name = mmd.ownerMetaClass.allClassName + "." + mmd.name;
            m_ExpressNode = mmd.expressNode;
            m_IRMetaVariableFrom = mmd.isStatic ? IRMetaVariableFrom.Static : IRMetaVariableFrom.Member;
            m_IRMetaClass = irmc;
        }
        public IRMetaVariable( IRMetaClass irmc, MetaMemberVariable mmv )
        {
            m_MetaVariable = mmv;
            id = mmv.GetHashCode();
            name = mmv.ownerMetaClass.allClassName + "." + mmv.name;
            m_ExpressNode = mmv.express;
            m_IRMetaVariableFrom = IRMetaVariableFrom.Member;
            m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(mmv.metaDefineType.metaClass.allClassName);
        }

        public override string ToString()
        {
            return name;
        }
    }
}
