using SimpleLanguage.Core.Statements;
using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static SimpleLanguage.Core.MetaVariable;

namespace SimpleLanguage.Core
{
    public partial class MetaInputParam
    {
        public System.Type GetCSharpType()
        {
            MetaClass orgmc = m_Express.GetReturnMetaClass();

            System.Type type = MetaTypeCSharp.FindCSharpType(orgmc);

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
    public class MetaDefineParamCSharp : MetaDefineParam
    {
        private ParameterInfo parameterInfo;
        public MetaDefineParamCSharp(MetaClass mc, MetaBlockStatements mbs, ParameterInfo pi)
            :base( mc,  mbs )
        {
            m_OwnerMetaClass = mc;

            m_OwnerMetaBlockStatements = mbs;

            parameterInfo = pi;

            var defineMetaClassType = ClassManager.instance.GetMetaClassByCSharpType(pi.ParameterType);
            MetaType mdt = new MetaType(defineMetaClassType);
            m_MetaVariable = new MetaVariable( pi.Name, EVariableFrom.Argument, mbs, mc, mdt );
        }
    }
}
