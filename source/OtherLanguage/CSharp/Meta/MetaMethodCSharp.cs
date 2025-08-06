using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaMethodCallCSharp : MetaMethodCall
    {
        public MethodInfo methodInfo;
        public System.Object instance;
        public MetaMethodCallCSharp(MetaVariable mv, MetaFunction _fun, MetaInputParamCollection _metaInputParamCollection = null) 
            : base( null, _fun, _metaInputParamCollection, null)
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
            var paramsTypes = MetaInputParamCollectionCSharp.GetCSharpParamTypes(m_MetaInputParamCollection);
            Object[] paramsObjs = new Object[paramsTypes.Length];

            return methodInfo.Invoke(instance, paramsObjs);
        }
    }
}
