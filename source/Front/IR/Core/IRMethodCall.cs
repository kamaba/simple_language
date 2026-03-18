//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRMethodCall
    {
        public List<IRMetaType> irTemplateMetaType => m_IrTemplateMetaType;
        public IRMetaType metaType => m_MetaType;
        public IRMethod irMethod => m_IRMethod;
        public int paramCount => m_ParamCount;
        public string methodName => m_IRMethod != null ? m_IRMethod.onlyFunctionName : "";


        private List<IRMetaType> m_IrTemplateMetaType = null;
        private IRMetaType m_MetaType = null;
        private IRMethod m_IRMethod = null;
        private int m_ParamCount = 0;
        public IRMethodCall(IRMetaType mt, List<IRMetaType> mtList, IRMethod irmethod, int paramCount)
        {
            m_MetaType = mt;
            m_IrTemplateMetaType = mtList;
            m_IRMethod = irmethod;
            m_ParamCount = paramCount;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RuntimeCall{");

            sb.Append("StaticRuntimeType=");
            sb.Append(FormatIRMetaType(m_MetaType));

            sb.Append(", StaticRuntimeTypeList=");
            sb.Append("[");
            if (m_MetaType?.irMetaTypeList != null)
            {
                for (int i = 0; i < m_MetaType.irMetaTypeList.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatIRMetaType(m_MetaType.irMetaTypeList[i]));
                }
            }
            sb.Append("]");

            sb.Append(", Method=");
            sb.Append(m_IRMethod?.id ?? "<null>");

            sb.Append(", MethodRuntimeTypeList=");
            sb.Append("[");
            if (m_IrTemplateMetaType != null)
            {
                for (int i = 0; i < m_IrTemplateMetaType.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatIRMetaType(m_IrTemplateMetaType[i]));
                }
            }
            sb.Append("]");

            sb.Append(", ParamCount=");
            sb.Append(m_ParamCount);
            sb.Append("}");
            return sb.ToString();
        }

        private static string FormatIRMetaType(IRMetaType mt)
        {
            if (mt == null)
            {
                return "<null>";
            }

            var sb = new StringBuilder();
            sb.Append(mt.irMetaClass?.irName ?? "<null>");

            if (mt.irMetaTypeList != null && mt.irMetaTypeList.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < mt.irMetaTypeList.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatIRMetaType(mt.irMetaTypeList[i]));
                }
                sb.Append(">");
            }

            if (mt.templateIndex >= 0)
            {
                sb.Append("[T:");
                sb.Append(mt.templateIndex);
                sb.Append("]");
            }

            return sb.ToString();
        }
    }
}
