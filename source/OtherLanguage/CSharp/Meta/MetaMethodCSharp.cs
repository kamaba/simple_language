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
            : base(mv, _fun, _metaInputParamCollection)
        {
            ParseCSharp();
        }

        public void ParseCSharp()
        {
            var mmf = m_MetaFunction as MetaMemberFunction;
            if ( mmf != null )
            {
                if( mmf.isCSharp )
                {
                    if( mmf.isStatic == false )
                    {
                        instance = new object();
                    }
                    methodInfo = mmf.methodInfo;
                }
            }
        }
        public object Execute()
        {
            var paramsTypes = m_MetaInputParamCollection.GetCSharpParamTypes();
            Object[] paramsObjs = new Object[paramsTypes.Length];

            return methodInfo.Invoke(instance, paramsObjs);
        }
    }
}
