//****************************************************************************
//  File:      IRMetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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

        private MetaExpressNode m_ExpressNode = null;
        private IRMetaClass m_IRMetaClass = null;
        private IRMetaVariableFrom m_IRMetaVariableFrom = IRMetaVariableFrom.None;
        public IRMetaVariable( MetaVariable mv )
        {
            m_MetaVariable = mv;
            id = mv.GetHashCode();
            name = mv.ownerMetaBlockStatements?.ownerMetaFunction.allName + "_local[" + mv.allName + "]";
            m_IRMetaVariableFrom = mv.variableFrom == MetaVariable.EVariableFrom.Argument 
                ?  IRMetaVariableFrom.Argument : IRMetaVariableFrom.LocalStatement;
            //m_IRMetaClass = IRManager.instance.GetIRMetaClassById(mv.meta)
        }
        public IRMetaVariable(MetaMemberEnum mme)
        {
            m_MetaVariable = mme;
            id = mme.GetHashCode();
            name = mme.ownerMetaClass.allName + "." + mme.name;
            m_ExpressNode = mme.express;
            m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
        }
        public IRMetaVariable(MetaMemberData mmd)
        {
            m_MetaVariable = mmd;
            id = mmd.GetHashCode();
            name = mmd.ownerMetaClass.allName + "." + mmd.name;
            m_ExpressNode = mmd.expressNode;
            m_IRMetaVariableFrom = mmd.isStatic ? IRMetaVariableFrom.Static : IRMetaVariableFrom.Member;
        }
        public IRMetaVariable( MetaMemberVariable mmv )
        {
            m_MetaVariable = mmv;
            id = mmv.GetHashCode();
            name = mmv.ownerMetaClass.allName + "." + mmv.name;
            m_ExpressNode = mmv.express;
            m_IRMetaVariableFrom = IRMetaVariableFrom.Member;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
