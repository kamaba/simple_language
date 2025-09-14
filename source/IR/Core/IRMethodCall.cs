//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using System.Collections.Generic;

namespace SimpleLanguage.IR
{
    public class IRMethodCall
    {
        public List<IRMetaType> irTemplateMetaType => m_IrTemplateMetaType;
        public IRMetaType metaType => m_MetaType;
        public IRMethod irMethod => m_IRMethod;


        private List<IRMetaType> m_IrTemplateMetaType = null;
        private IRMetaType m_MetaType = null;
        private IRMethod m_IRMethod = null;
        public IRMethodCall(IRMetaType mt, List<IRMetaType> mtList, IRMethod irmethod )
        {
            m_MetaType = mt;
            m_IrTemplateMetaType = mtList;
            m_IRMethod = irmethod;
        }
        public override string ToString()
        {
            return "";
        }
    }
}
