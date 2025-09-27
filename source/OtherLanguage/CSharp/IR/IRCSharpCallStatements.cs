
using SimpleLanguage.Core;
using SimpleLanguage.Core.Statements;
using System.Reflection;

namespace SimpleLanguage.IR
{
    public class IRCSharpCallStatements : IRStatements
    {
        MethodInfo m_MethodInfo = null;
        MetaMethodCall metaFunctionCall = null;
        public IRCSharpCallStatements(MetaBlockStatements mbs, string _name, System.Type type, MethodInfo methodInfo) 
        {
            m_MethodInfo = methodInfo;

            MetaFunction mf = new MetaMemberFunctionCSharp( mbs.ownerMetaClass, _name, m_MethodInfo);

            var mipc = new MetaInputParamCollection(mbs.ownerMetaClass, mbs);
            metaFunctionCall = new MetaMethodCall( null, null, mf, null, mipc, null, null );
        }
        //public override void ParseIRStatements()
        //{
        //    IRCallFunction irCallFun = new IRCallFunction(irMethod, metaFunctionCall );            
        //    m_IRStatements.Add( irCallFun );
        //}
        //public override string ToFormatString()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    for (int i = 0; i < realDeep; i++)
        //        sb.Append(Global.tabChar);
        //    sb.Append(Environment.NewLine);
        //    if (nextMetaStatements != null)
        //    {
        //        sb.Append(nextMetaStatements.ToFormatString());
        //    }

        //    return sb.ToString();
        //}
    }
}
