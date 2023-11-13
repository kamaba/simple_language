using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public class MetaCallExpressNode : MetaExpressNode
    {
        public MetaCallLink metaCallLink => m_MetaCallLink;

        private MetaCallLink m_MetaCallLink = null;
        public MetaCallExpressNode( FileMetaCallLink fmcl, MetaClass mc, MetaBlockStatements mbs )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
           
            if(fmcl != null )
            {
                m_MetaCallLink = new MetaCallLink( fmcl, mc, mbs );
            }
        }
        public MetaCallExpressNode( MetaCallLink mcl )
        {
            m_MetaCallLink = mcl;
        }
        public override void Parse( AllowUseConst auc )
        {
            if(m_MetaCallLink!= null )
            {
                m_MetaCallLink.Parse( auc );
            }
        }
        public override Token GetToken() { return m_MetaCallLink.GetToken(); }
        public override int CalcParseLevel(int level)
        {
            if (m_MetaCallLink != null)
                level = m_MetaCallLink.CalcParseLevel(level);
            return level;
        }
        public override void CalcReturnType()
        {
            if(m_MetaCallLink != null )
            {
                m_MetaCallLink.CalcReturnType();
                var egmc = m_MetaCallLink.ExecuteGetMetaClass();
                if( egmc != null )
                {
                    eType = egmc.eType;
                }
            }
        }
        public MetaVariable GetMetaVariable()
        {
            if( m_MetaCallLink != null )
            {
                return m_MetaCallLink.ExecuteGetMetaVariable();
            }
            return null;
        }
        public override MetaType GetReturnMetaDefineType()
        {
            if (m_MetaDefineType != null)
            {
                return m_MetaDefineType;
            }
            if (m_MetaCallLink == null)
                return null;

            m_MetaDefineType = m_MetaCallLink.GetMetaDeineType();
            return m_MetaDefineType;
        }
        public MetaExpressNode ConvertConstExpressNode()
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
                sb.Append( m_MetaCallLink.ToFormatString() );
                return sb.ToString();
            }
            return "ExpressCallLink Error!!";
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_MetaCallLink != null)
            {
                sb.Append(m_MetaCallLink.ToTokenString());
            }
            return sb.ToString();
        }
    }
}
