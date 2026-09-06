//****************************************************************************
//  File:      MetaExpressTry.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/07/24 12:00:00
//  Description: try / try? / try! expression node
//****************************************************************************

using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;

namespace SimpleLanguage.Core
{
    public enum ETryMode
    {
        None,
        Try,            // try - exception caught by surrounding catch
        TryQuestion,    // try? - returns null on exception
        TryExclamation, // try! - crashes on exception
    }

    public sealed class MetaTryExpressNode : MetaExpressNodeBase
    {
        public override Token token => m_Token;
        public ETryMode tryMode => m_TryMode;
        public MetaExpressNodeBase innerExpress => m_InnerExpress;

        private ETryMode m_TryMode = ETryMode.None;
        private MetaExpressNodeBase m_InnerExpress = null;

        public MetaTryExpressNode(FileMetaSymbolTerm fme, MetaExpressNodeBase innerExpress)
        {
            m_Token = fme.token;
            m_InnerExpress = innerExpress;

            if (fme.symBolType == ETokenType.Try)
            {
                m_TryMode = ETryMode.Try;
                // Only plain 'try' requires a surrounding label{} block;
                // try? is self-contained, try! propagates (both OK outside label)
                if (!MetaMemberFunction.isInTryBlock)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, fme.token,
                        "Error: try 表达式只能在 label{} 或 checked label{} 块内使用");
                }
            }
            else if (fme.symBolType == ETokenType.TryQuestion)
            {
                m_TryMode = ETryMode.TryQuestion;
            }
            else if (fme.symBolType == ETokenType.TryExclamation)
            {
                m_TryMode = ETryMode.TryExclamation;
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
            sb.Append(m_Token.lexeme.ToString());
            sb.Append(m_InnerExpress?.ToFormatString() ?? "");
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
