using System.Reflection;

namespace SimpleLanguage.Core
{
    public sealed class MetaMemberFunctionCSharp : MetaMemberFunction
    {
        //#ifdef CSharp
        public MethodInfo methodInfo;
        public PropertyInfo propertyInfo;
        public bool isCharp => methodCallType == EMethodCallType.CSharp;

        public MetaMemberFunctionCSharp(MetaClass mc, MethodInfo mi) : base(mc)
        {
            methodInfo = mi;

            m_Name = mi.Name;

            m_MethodCallType = EMethodCallType.CSharp;

            HandleMethodInfo();
        }
        public MetaMemberFunctionCSharp(MetaClass mc, string _name, MethodInfo mi) : base(mc)
        {
            methodInfo = mi;

            m_Name = _name;

            m_MethodCallType = EMethodCallType.CSharp;

            HandleMethodInfo();
        }
        public MetaMemberFunctionCSharp(MetaClass mc, PropertyInfo pi) : base(mc)
        {
            propertyInfo = pi;

            m_MethodCallType = EMethodCallType.CSharp;

            methodInfo = pi.GetGetMethod();

            if (methodInfo != null)
            {
                HandleMethodInfo();

                SetIsSet( pi.CanWrite );
                SetIsGet( pi.CanRead );
            }
            else
            {
                var mt = MetaTypeCSharp.GetMetaTypeByCSharpType(pi.GetType());
                m_ReturnMetaVariable?.SetMetaDefineType( mt );

                m_MetaBlockStatements = new MetaBlockStatements(this, null);
                m_MetaBlockStatements.isOnFunction = true;
            }

            m_Name = pi.Name;
        }
        void HandleMethodInfo()
        {
            m_IsStatic = methodInfo.IsStatic;

            if (methodInfo.IsVirtual)
            {
                SetIsOverrideFunction(methodInfo.IsFinal);
            }

            if (methodInfo.IsPublic)
            {
                m_Permission = EPermission.Public;
            }
            else if (methodInfo.IsPrivate)
            {
                m_Permission = EPermission.Private;
            }
            ParameterInfo[] pis = methodInfo.GetParameters();
            for (int i = 0; i < pis.Length; i++)
            {
                MetaDefineParamCSharp mdp = new MetaDefineParamCSharp(this, pis[i]);
                m_MetaMemberParamCollection.AddMetaDefineParam(mdp);
            }
            base.Init();

            if( methodInfo.DeclaringType != null )
            {
                var mt = MetaTypeCSharp.GetMetaTypeByCSharpType(methodInfo.ReturnType);
                m_ReturnMetaVariable.SetMetaDefineType(mt);
                m_ReturnMetaVariable.SetIsDefineMetaType(true);
            }


            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;
        }
        public void AddCSharpMetaStatements( string typeName, string methodName )
        {
            var ttype = System.Type.GetType(typeName);
            if( ttype != null )
            {
                MethodInfo mi = ttype.GetMethod(methodName);

                MetaOtherPlatformStatements mcallState = new MetaOtherPlatformStatements( m_MetaBlockStatements );

                AddFrontMetaStatements(mcallState);
            }
        }
        //#endif
    }
}
