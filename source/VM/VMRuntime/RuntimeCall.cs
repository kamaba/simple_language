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
        public List<RuntimeDefType> runtimeMethodTemplateRuntimeDefTypeList => m_RuntimeMethodTemplateDefTypeList;
        public RuntimeDefType runtimeTypeDefType => m_RuntimeTypeDefType;
        public RuntimeMethod method => m_Method;
        public int paramCount => m_ParamCount;
        public string methodName => m_Method != null ? m_Method.onlyFunctionName : "";


        private List<RuntimeDefType> m_RuntimeMethodTemplateDefTypeList = null;
        private RuntimeDefType m_RuntimeTypeDefType = null;
        private RuntimeMethod m_Method = null;
        private int m_ParamCount = 0;
        public RuntimeCall(RuntimeDefType mt, List<RuntimeDefType> mtList, RuntimeMethod irmethod, int paramCount)
        {
            m_RuntimeTypeDefType = mt;
            m_RuntimeMethodTemplateDefTypeList = mtList;
            m_Method = irmethod;
            m_ParamCount = paramCount;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if(m_RuntimeTypeDefType != null )
            {
                sb.Append( "[" + m_RuntimeTypeDefType.runtimeClass?.name);

                if(m_RuntimeTypeDefType.runtimeDefTypeList.Count > 0)
                {
                    sb.Append("<");
                    for( int i = 0; i < m_RuntimeTypeDefType.runtimeDefTypeList.Count; i++ )
                    {
                        sb.Append(m_RuntimeTypeDefType.runtimeDefTypeList[i].ToString());
                        if( i < m_RuntimeTypeDefType.runtimeDefTypeList.Count - 1 )
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

            if (m_RuntimeMethodTemplateDefTypeList?.Count > 0)
            {
                sb.Append("<");
                for (int i = 0; i < m_RuntimeMethodTemplateDefTypeList.Count; i++)
                {
                    sb.Append(m_RuntimeMethodTemplateDefTypeList[i].ToString());
                    if (i < m_RuntimeMethodTemplateDefTypeList.Count - 1)
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
