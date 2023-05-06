using SimpleLanguage.Core.Statements;
using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaInputParam
    {
        public System.Type GetCSharpType()
        {
            MetaClass mc = m_Express.GetReturnMetaClass();

            System.Type type = mc.GetCSharpType();
            return type;
        }
    }
    public partial class MetaInputParamCollection
    {
        System.Type[] m_CShpartParamTypes;
        bool m_IsHaveParse = false;
        public System.Type[] GetCSharpParamTypes()
        {
            if(m_IsHaveParse )
            {
                return m_CShpartParamTypes;
            }
            m_CShpartParamTypes = new System.Type[count];

            for( int i = 0; i < count; i++ )
            {
                MetaInputParam mip = metaParamList[i] as MetaInputParam;
                m_CShpartParamTypes[i] = mip.GetCSharpType();
            }

            return m_CShpartParamTypes;
        }
    }
    public partial class MetaDefineParam
    {
        private ParameterInfo parameterInfo;
        public MetaDefineParam(MetaClass mc, MetaBlockStatements mbs, ParameterInfo pi)
        {
            m_OwnerMetaClass = mc;

            m_OwnerMetaBlockStatements = mbs;

            parameterInfo = pi;

            var defineMetaClassType = ClassManager.instance.GetMetaClassByCSharpType(pi.ParameterType);
            MetaType mdt = new MetaType(defineMetaClassType);
            m_MetaVariable = new MetaVariable( pi.Name, mbs, mc, mdt );
        }
    }
}
