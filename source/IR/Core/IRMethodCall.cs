//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
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


        private List<IRMetaType> m_IrTemplateMetaType = null;
        private IRMetaType m_MetaType = null;
        private IRMethod m_IRMethod = null;
        private int m_ParamCount = 0;
        public IRMethodCall(IRMetaType mt, List<IRMetaType> mtList, IRMethod irmethod, int paramCount )
        {
            m_MetaType = mt;
            m_IrTemplateMetaType = mtList;
            m_IRMethod = irmethod;
            m_ParamCount = paramCount;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if(m_MetaType != null )
            {
                sb.Append( "[" + m_MetaType.irMetaClass?.irName);

                if(m_MetaType.irMetaTypeList.Count > 0)
                {
                    sb.Append("<");
                    for( int i = 0; i <  m_MetaType.irMetaTypeList.Count; i++ )
                    {
                        sb.Append(m_MetaType.irMetaTypeList[i].ToString());
                        if( i < m_MetaType.irMetaTypeList.Count - 1 )
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append(">");
                }
                sb.Append("] ");
            }
            if (m_IRMethod != null)
            {
                sb.Append(m_IRMethod.id.ToString());
            }

            if (m_IrTemplateMetaType?.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < m_IrTemplateMetaType.Count; i++)
                {
                    sb.Append(m_IrTemplateMetaType[i].ToString());
                    if (i < m_IrTemplateMetaType.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(">");
            }

            return sb.ToString();
        }
    }
}
