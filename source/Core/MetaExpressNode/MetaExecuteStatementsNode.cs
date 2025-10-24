using System.Text;
using SimpleLanguage.Compile;


namespace SimpleLanguage.Core
{
    public sealed class MetaExecuteStatementsNode : MetaExpressNode
    {
        private MetaIfStatements m_MetaIfStatements = null;
        private MetaSwitchStatements m_MetaSwitchStatements = null;
        public MetaExecuteStatementsNode( MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, MetaIfStatements ifstate)
        {
            m_MetaDefineType = mdt;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaIfStatements = ifstate;
        }
        public MetaExecuteStatementsNode(MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, MetaSwitchStatements switchstate)
        {
            m_MetaDefineType = mdt;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaSwitchStatements = switchstate;
        }
        public void UpdateTrMetaVariable(MetaVariable trmv)
        {
            if (m_MetaIfStatements != null)
            {
                m_MetaIfStatements.SetTRMetaVariable(trmv);
            }
            else if (m_MetaSwitchStatements != null)
            {
                m_MetaSwitchStatements.SetTRMetaVariable(trmv);
            }
        }
        public void SetDeep(int dp)
        {
            if (m_MetaIfStatements != null)
            {
                m_MetaIfStatements.SetDeep(dp);
            }
            else if (m_MetaSwitchStatements != null)
            {
                m_MetaSwitchStatements.SetDeep(dp);
            }
        }
        public override MetaType GetReturnMetaDefineType()
        {
            if(m_MetaDefineType != null)
            {
                return m_MetaDefineType;
            }
            if (m_MetaIfStatements != null || m_MetaSwitchStatements != null)
            {
                m_MetaDefineType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }

            return m_MetaDefineType;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if( m_MetaIfStatements != null )
            {
                sb.Append(m_MetaIfStatements.ToFormatString());
            }
            else if( m_MetaSwitchStatements != null )
            {
                sb.Append(m_MetaSwitchStatements.ToFormatString());
            }
            else
            {
                sb.Append(base.ToFormatString());
            }
            return sb.ToString();
        }
        public static MetaExecuteStatementsNode CreateMetaExecuteStatementsNodeByIfExpress( MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, FileMetaKeyIfSyntax ifStatements)
        {
            if (ifStatements == null) return null;

            MetaIfStatements newIfStatements = new MetaIfStatements(mbs, ifStatements );
            MetaExecuteStatementsNode mesn = new MetaExecuteStatementsNode(mdt, ownerMC, mbs, newIfStatements);

            return mesn;
        }
        public static MetaExecuteStatementsNode CreateMetaExecuteStatementsNodeBySwitchExpress(MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, FileMetaKeySwitchSyntax switchStatements)
        {
            if (switchStatements == null) return null;

            MetaSwitchStatements newSwtichStatements = new MetaSwitchStatements(mbs, switchStatements);
            MetaExecuteStatementsNode mesn = new MetaExecuteStatementsNode(mdt, ownerMC, mbs, newSwtichStatements);

            return mesn;
        }
    }
}
