using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaCSharpCallStatements : MetaStatements
    {
        MethodInfo m_MethodInfo = null;
        MetaFunctionCall metaFunctionCall = null;
        public MetaCSharpCallStatements(MetaBlockStatements mbs, string _name, System.Type type, MethodInfo methodInfo) : base(mbs)
        {
            m_MethodInfo = methodInfo;

            MetaFunction mf = new MetaMemberFunction( mbs.ownerMetaClass, _name, m_MethodInfo);

            var mipc = new MetaInputParamCollection(mbs.ownerMetaClass, mbs);
            metaFunctionCall = new MetaFunctionCall(mbs.ownerMetaClass, mf, mipc );
        }
        public override void ParseIRStatements()
        {
            IRCallFunction irCallFun = new IRCallFunction(irMethod, metaFunctionCall );            
            //m_IRDataList.AddRange(irCallFun.IRDataList);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append(Environment.NewLine);
            if (nextMetaStatements != null)
            {
                sb.Append(nextMetaStatements.ToFormatString());
            }

            return sb.ToString();
        }
    }
}
