//****************************************************************************
//  File:      MetaStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile;

namespace SimpleLanguage.Core.Statements
{
    public class MetaStatements : MetaBase
    {
        public MetaStatements nextMetaStatements => m_NextMetaStatements;
        public MetaBlockStatements parentBlockStatements => m_OwnerMetaBlockStatements;
        public MetaVariable trMetaVariable => m_TrMetaVariable;

        public virtual MetaClass ownerMetaClass
        {
            get
            {
                return ownerMetaFunction?.ownerMetaClass;
            }
        }
        public virtual MetaFunction ownerMetaFunction
        {
            get
            {
                return m_OwnerMetaBlockStatements?.ownerMetaFunction;
            }
        }

        protected MetaVariable m_TrMetaVariable = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaStatements m_NextMetaStatements = null;
        protected bool m_IsNeedCastState = false;
        protected MetaStatements()
        {  }
        protected MetaStatements(MetaBlockStatements mf )
        {
            m_OwnerMetaBlockStatements = mf;
        }
        public virtual void SetTRMetaVariable( MetaVariable mv )
        {
            m_TrMetaVariable = mv;
            if(m_NextMetaStatements != null )
            {
                m_NextMetaStatements.SetTRMetaVariable(mv);
            }
        }
        public override void SetDeep( int dp )
        {
            m_Deep = dp;
            if (m_NextMetaStatements != null)
            {
                m_NextMetaStatements.SetDeep(deep);
            }
        }
        public virtual void SetNextStatements( MetaStatements ms )
        {
            m_NextMetaStatements = ms;
        }
        public virtual void UpdateOwnerMetaClass( MetaClass ownerclass )
        {
            if(m_TrMetaVariable != null )
            {
                m_TrMetaVariable.SetOwnerMetaClass(ownerclass);
            }
            if( m_NextMetaStatements != null )
            {
                m_NextMetaStatements.UpdateOwnerMetaClass(ownerclass);
            }
        }
    }
}
