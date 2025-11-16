
using System.Diagnostics;
using System.Reflection;
using static SimpleLanguage.Core.MetaVariable;

namespace SimpleLanguage.Core
{
    public sealed class MetaInputParamCSharp
    {
        public static System.Type GetCSharpType(MetaInputParam mip )
        {
            MetaClass orgmc = mip.express.GetReturnMetaClass();

            if( orgmc is MetaClassCSharp mcc )
            {
                return mcc.csharpType;
            }

            if(orgmc == null )
            {
                Debug.Write("Error 没有发现表达式类型MetaClass!");
                return typeof(object);
            }

            System.Type type = MetaTypeCSharp.FindCSharpType(orgmc);

            return type;
        }
    }
    public sealed class MetaInputParamCollectionCSharp
    {
        System.Type[] m_CShpartParamTypes;
        bool m_IsHaveParse = false;

        public static System.Type[]  GetCSharpParamTypes( MetaInputParamCollection mipc )
        {
            //if(m_IsHaveParse )
            //{
            //    return m_CShpartParamTypes;
            //}
            var m_CShpartParamTypes = new System.Type[mipc.count];

            for (int i = 0; i < mipc.count; i++)
            {
                MetaInputParam mip = mipc.metaInputParamList[i];
                m_CShpartParamTypes[i] = MetaInputParamCSharp.GetCSharpType(mip);
            }

            return m_CShpartParamTypes;
        }
    }
    public class MetaDefineParamCSharp : MetaDefineParam
    {
        private ParameterInfo parameterInfo;
        public MetaDefineParamCSharp(MetaFunction mf, ParameterInfo pi)
            :base(pi.Name, mf)
        {
            m_OwnerMetaFunction = mf;

            parameterInfo = pi;

            var defineMetaClassType = ClassManager.instance.GetMetaClassByCSharpType(pi.ParameterType);
            MetaType mdt = new MetaType(defineMetaClassType);
            m_MetaVariable = new MetaVariable( pi.Name, EVariableFrom.Argument, null, mf.ownerMetaClass, mdt );
        }
    }
}
