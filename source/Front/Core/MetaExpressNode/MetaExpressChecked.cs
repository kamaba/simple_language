//****************************************************************************
//  File:      MetaExpressChecked.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/07/27 12:00:00
//  Description: checked(expr) expression node — overflow-checked arithmetic
//****************************************************************************

using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// checked(expr) — evaluates expr with integer overflow checking enabled.
    /// On overflow, an OverflowException is thrown via the existing try/catch mechanism.
    /// Only affects integer arithmetic (+, -, *, /, %).
    /// </summary>
    public sealed class MetaCheckedExpressNode : MetaExpressNodeBase
    {
        public override Token token => m_Token;
        public MetaExpressNodeBase innerExpress => m_InnerExpress;

        private MetaExpressNodeBase m_InnerExpress = null;

        public MetaCheckedExpressNode(FileMetaSymbolTerm fme, MetaExpressNodeBase innerExpress)
        {
            m_Token = fme.token;
            m_InnerExpress = innerExpress;

            // checked(expr) expressions can only be used inside label{} or checked label{} blocks
            if (!MetaMemberFunction.isInTryBlock)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, fme.token,
                    "Error: checked 表达式只能在 label{} 或 checked label{} 块内使用");
            }
        }

        public override void Parse(AllowUseSettings auc)
        {
            if (m_InnerExpress != null)
            {
                m_InnerExpress.Parse(auc);
                m_ParsedState = m_InnerExpress.parseSuccessed ? EParseState.ParseSuccess : EParseState.ParsedFailed;
            }
            else
            {
                m_ParsedState = EParseState.ParsedFailed;
            }
        }

        public override void CalcReturnType()
        {
            if (m_InnerExpress != null)
            {
                m_InnerExpress.CalcReturnType();
                m_ExpressReturnMetaType = m_InnerExpress.expressReturnMetaType;
            }
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("checked");
            sb.Append("(");
            sb.Append(m_InnerExpress?.ToFormatString() ?? "");
            sb.Append(")");
            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_Token != null)
            {
                sb.Append(m_Token.ToLexemeAllString());
            }
            if (m_InnerExpress != null)
            {
                sb.Append(m_InnerExpress.ToString());
            }
            return sb.ToString();
        }
    }
}
