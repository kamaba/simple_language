//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeCall
    {
        public List<RuntimeDefType> templateRuntimeDefTypeList => m_TemplateRuntimeDefTypeList;
        public RuntimeDefType runtimeDefType => m_RuntimeDefType;
        public RuntimeMethod method => m_Method;
        public int paramCount => m_ParamCount;
        public string methodName => m_Method != null ? m_Method.onlyFunctionName : "";


        private List<RuntimeDefType> m_TemplateRuntimeDefTypeList = null;
        private RuntimeDefType m_RuntimeDefType = null;
        private RuntimeMethod m_Method = null;
        private int m_ParamCount = 0;
        public RuntimeCall(RuntimeDefType mt, List<RuntimeDefType> mtList, RuntimeMethod irmethod, int paramCount)
        {
            m_RuntimeDefType = mt;
            m_TemplateRuntimeDefTypeList = mtList;
            m_Method = irmethod;
            m_ParamCount = paramCount;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if(m_RuntimeDefType != null )
            {
                sb.Append( "[" + m_RuntimeDefType.runtimeClass?.name);

                if(m_RuntimeDefType.runtimeDefTypeList.Count > 0)
                {
                    sb.Append("<");
                    for( int i = 0; i < m_RuntimeDefType.runtimeDefTypeList.Count; i++ )
                    {
                        sb.Append(m_RuntimeDefType.runtimeDefTypeList[i].ToString());
                        if( i < m_RuntimeDefType.runtimeDefTypeList.Count - 1 )
                        {
                            sb.Append(",");
                        }
                    }
                    sb.Append(">");
                }
                sb.Append("] ");
            }
            if (m_Method != null)
            {
                sb.Append(m_Method.id.ToString());
            }

            if (m_TemplateRuntimeDefTypeList?.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < m_TemplateRuntimeDefTypeList.Count; i++)
                {
                    sb.Append(m_TemplateRuntimeDefTypeList[i].ToString());
                    if (i < m_TemplateRuntimeDefTypeList.Count - 1)
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
