using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaMethodCallCSharp : MetaMethodCall
    {
        public MethodInfo methodInfo;
        public System.Object instance;
        public MetaMethodCallCSharp(MetaVariable mv, MetaFunction _fun, MetaInputParamCollection _metaInputParamCollection = null) 
            : base( null, null, _fun, null,  _metaInputParamCollection, null, null)
        {
            ParseCSharp();
        }

        public void ParseCSharp()
        {
            var mmf = m_VMCallMetaFunction as MetaMemberFunctionCSharp;
            if ( mmf != null )
            {
                if(mmf.isStatic == false)
                {
                    instance = new object();
                }
                methodInfo = mmf.methodInfo;
            }
        }
        public object Execute()
        {
            List<System.Type> typeList = new List<System.Type>();
            for( int i = 0; i < m_MetaInputParamList.Count; i++ )
            {
                var express = m_MetaInputParamList[i];

                MetaClass orgmc = express.GetReturnMetaClass();

                if (orgmc is MetaClassCSharp mcc)
                {
                    return mcc.csharpType;
                }

                if (orgmc == null)
                {
                    Debug.Write("Error 没有发现表达式类型MetaClass!");
                    return typeof(object);
                }

                System.Type type = MetaTypeCSharp.FindCSharpType(orgmc);
                typeList.Add(type);
            }
            Object[] paramsObjs = new Object[typeList.Count];

            return methodInfo.Invoke(instance, paramsObjs);
        }
    }
}
