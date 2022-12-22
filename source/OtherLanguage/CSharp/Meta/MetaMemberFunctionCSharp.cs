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
        
        public bool isCSharp { get; set; }  = false;
        public MetaMemberFunction(MetaClass mc, MethodInfo mi) : base(mc)
        {
            isCSharp = true;

            methodInfo = mi;

            m_Name = mi.Name;

            m_MethodCallType = EMethodCallType.CSharp;

            HandleMethodInfo();
        }
        public MetaMemberFunction(MetaClass mc, string _name, MethodInfo mi) : base(mc)
        {
            isCSharp = true;

            methodInfo = mi;

            m_Name = _name;

            m_MethodCallType = EMethodCallType.CSharp;

            HandleMethodInfo();
        }
        public MetaMemberFunction(MetaClass mc, PropertyInfo pi) : base(mc)
        {
            isCSharp = true;

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
                MetaDefineParam mdp = new MetaDefineParam(m_OwnerMetaClass, null, pis[i]);
                m_MetaMemberParamCollection.AddMetaDefineParam(mdp);
            }

            var defineMetaClass = ClassManager.instance.GetMetaClassByCSharpType(methodInfo.ReturnType);
            m_DefineMetaType = new MetaType(defineMetaClass);

            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;
        }
        public void AddCSharpMetaStatements( string typeName, string methodName )
        {
            var ttype = System.Type.GetType(typeName);
            if( ttype != null )
            {
                MethodInfo mi = ttype.GetMethod(methodName);

                MetaCSharpCallStatements mcallState = new MetaCSharpCallStatements( m_MetaBlockStatements, methodName, ttype,  mi );

                AddMetaStatements(mcallState);
            }
        }
        //#endif
    }
}
