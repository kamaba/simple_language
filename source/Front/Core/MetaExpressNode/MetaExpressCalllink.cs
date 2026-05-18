//****************************************************************************
//  File:      MetaExpressCalllink.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/8/5 12:00:00
//  Description:  
//****************************************************************************

using System.Text;
using SimpleLanguage.Compile;


namespace SimpleLanguage.Core
{
    public sealed class MetaCallLinkExpressNode : MetaExpressNodeBase
    {
        public MetaCallLink metaCallLink => m_MetaCallLink;

        private MetaCallLink m_MetaCallLink = null;
        public MetaCallLinkExpressNode( FileMetaCallLink fmcl, MetaBase owner, MetaBlockStatements mbs, MetaVariable mv )
        {
            m_OwnerMetaBase = owner;
            m_OwnerMetaBlockStatements = mbs;

            m_MetaCallLink = new MetaCallLink( fmcl, owner, mbs, mv?.defineMetaType, mv );
            m_Token = m_MetaCallLink.callNodeList[0].token;
        }
        public MetaCallLinkExpressNode( MetaCallLink mcl )
        {
            m_MetaCallLink = mcl;
        }
        public override void Parse( AllowUseSettings auc )
        {
            if(m_MetaCallLink!= null )
            {
                m_MetaCallLink.Parse( auc );

                if(m_MetaCallLink.finalCallNode != null
                    && m_MetaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.New )
                {
                    m_ConvertNewExpressNode = true;
                }
                else
                {
                    // Class/Data call-shape like DataHolder(){ ... } / MetaInfo(){ ... } may end as method-call
                    // (_init_) rather than visit-type New, but semantically still needs NewObject conversion
                    // so initializer assignStatements are preserved in Meta/IR.
                    var mmf = m_MetaCallLink.finalCallNode?.methodCall?.function as MetaMemberFunction;
                    if (mmf != null && mmf.isConstructInitFunction)
                    {
                        m_ConvertNewExpressNode = true;
                    }
                    else
                    {
                        // Fallback: if call syntax carries object-initializer braces, force NewObject conversion
                        // so brace assignments are materialized into MetaNewObjectExpressNode.assignStatementsList.
                        var nodes = m_MetaCallLink.callNodeList;
                        if (nodes != null && nodes.Count > 0)
                        {
                            var lastNode = nodes[nodes.Count - 1];
                            if (lastNode?.fileMetaBraceTerm != null)
                            {
                                m_ConvertNewExpressNode = true;
                            }
                        }
                    }
                }
            }
        }
        public override int CalcParseLevel(int level)
        {
            if (m_MetaCallLink != null)
                level = m_MetaCallLink.CalcParseLevel(level);
            return level;
        }
        public override void CalcReturnType()
        {
            if (m_MetaCallLink != null)
            {
                m_ExpressReturnMetaType = m_MetaCallLink.GetMetaType();
            }
        }
        public MetaVariable GetStoreMetaVariable()
        {
            if (m_MetaCallLink != null)
            {
                return m_MetaCallLink.storeMetaVariable;
            }
            return null;
        }
        public MetaVariable GetMetaVariable()
        {
            if( m_MetaCallLink != null )
            {
                return m_MetaCallLink.ExecuteGetMetaVariable();
            }
            return null;
        }
        public override MetaType GetReturnMetaType()
        {
            if (m_ExpressReturnMetaType != null)
            {
                return m_ExpressReturnMetaType;
            }
            if (m_MetaCallLink == null)
                return null;

            m_ExpressReturnMetaType = m_MetaCallLink.GetMetaType();
            return m_ExpressReturnMetaType;
        }
        public MetaExpressNodeBase ConvertConstExpressNode()
        {
            if (m_MetaCallLink == null)
                return null;
            return m_MetaCallLink.GetMetaExpressNode();
        }
        public override string ToFormatString()
        {
            if (m_MetaCallLink != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(m_MetaCallLink.ToFormatString());
                return sb.ToString();
            }
            return "ExpressCallLink Error!!";
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_MetaCallLink != null)
            {
                sb.Append(m_MetaCallLink.ToString());
            }
            return sb.ToString();
        }
    }
}
