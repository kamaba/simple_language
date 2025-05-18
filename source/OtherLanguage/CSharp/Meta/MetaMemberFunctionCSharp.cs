using SimpleLanguage.Core.Statements;
using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaMemberFunction
    {
        //#ifdef CSharp
        public MethodInfo methodInfo;
        public PropertyInfo propertyInfo;
        public bool isCharp => methodCallType == EMethodCallType.CSharp;

        public MetaMemberFunction(MetaClass mc, MethodInfo mi) : base(mc)
        {
            methodInfo = mi;

            m_Name = mi.Name;

            m_MethodCallType = EMethodCallType.CSharp;

            HandleMethodInfo();
        }
        public MetaMemberFunction(MetaClass mc, string _name, MethodInfo mi) : base(mc)
        {
            methodInfo = mi;

            m_Name = _name;

            m_MethodCallType = EMethodCallType.CSharp;

            HandleMethodInfo();
        }
        public MetaMemberFunction(MetaClass mc, PropertyInfo pi) : base(mc)
        {
            propertyInfo = pi;

            m_MethodCallType = EMethodCallType.CSharp;

            methodInfo = pi.GetGetMethod();

            if (methodInfo != null)
            {
                HandleMethodInfo();

                isSet = pi.CanWrite;
                isGet = pi.CanRead;
            }
            else
            {
                var defineMetaClass = ClassManager.instance.GetMetaClassByCSharpType(pi.GetType());
                m_DefineMetaType = new MetaType(defineMetaClass);

                m_MetaBlockStatements = new MetaBlockStatements(this, null);
                m_MetaBlockStatements.isOnFunction = true;
            }

            m_Name = pi.Name;
        }
        void HandleMethodInfo()
        {
            isStatic = methodInfo.IsStatic;

            if (methodInfo.IsVirtual)
            {
                isOverrideFunction = methodInfo.IsFinal;
            }

            if (methodInfo.IsPublic)
            {
                permission = EPermission.Public;
            }
            else if (methodInfo.IsPrivate)
            {
                permission = EPermission.Private;
            }
            ParameterInfo[] pis = methodInfo.GetParameters();
            for (int i = 0; i < pis.Length; i++)
            {
                MetaDefineParamCSharp mdp = new MetaDefineParamCSharp(m_OwnerMetaClass, null, pis[i]);
                m_MetaMemberParamCollection.AddMetaDefineParam(mdp);
            }
            Init();

            if( methodInfo.DeclaringType != null )
            {
                var defineMetaClass = ClassManager.instance.GetMetaClassByCSharpType(methodInfo.ReturnType);
                m_DefineMetaType = new MetaType(defineMetaClass);

                m_ReturnMetaVariable.SetMetaDefineType(m_DefineMetaType);
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

                AddMetaStatements(mcallState);
            }
        }
        //#endif
    }
}
