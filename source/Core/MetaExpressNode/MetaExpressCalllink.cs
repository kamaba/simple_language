using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.Statements;

namespace SimpleLanguage.Core
{
    public class MetaCallLinkExpressNode : MetaExpressNode
    {
        public MetaCallLink metaCallLink => m_MetaCallLink;

        private MetaCallLink m_MetaCallLink = null;

        private MetaVariable m_EqualMetaVariable = null;
        public MetaCallLinkExpressNode( FileMetaCallLink fmcl, MetaClass mc, MetaBlockStatements mbs, MetaVariable mv )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_EqualMetaVariable = mv;
            if (fmcl != null )
            {
                Dictionary<string, MetaType> mmtd = new Dictionary<string, MetaType>();
                if (mc.isGenTemplate)
                {
                    (mc as MetaGenTemplateClass).GetMetaTemplateMT(mmtd);
                }

                m_MetaCallLink = new MetaCallLink( fmcl, mc, mbs, mmtd);
            }
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
            if(m_MetaCallLink != null )
            {
                m_MetaCallLink.CalcReturnType();
            }

            m_MetaDefineType = GetReturnMetaDefineType();
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
