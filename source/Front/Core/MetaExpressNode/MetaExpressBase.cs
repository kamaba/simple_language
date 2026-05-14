


namespace SimpleLanguage.Core
{
    public enum ELeftRightOpSign
    {
        None,
        Add,
        Minus,
        Multiply,
        Divide,
        Modulo,
        InclusiveOr,
        Combine,
        XOR,
        Shi,
        Shr,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        And,
        Or,
    }
    public enum ESingleOpSign
    {
        None,
        Neg,
        Not,
        Xor,
    }
    public class MetaExpressNode
    {
        public virtual int opLevel
        {
            get
            {
                return MetaTypeFactory.GetOpLevelByMetaType(m_MetaType);
            }
        }
        public bool convertNewExpressNode => m_ConvertNewExpressNode;
        public bool convertCallExpressNode => m_ConvertCallExpressNode;
        public virtual Token token => m_Token;
        public MetaType metaType => m_MetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;


        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaType m_MetaType = null;
        protected bool m_ConvertNewExpressNode = false;
        protected bool m_ConvertCallExpressNode = false;
        protected Token m_Token = null;
        protected bool m_Parse = false;


        public virtual int CalcParseLevel(int level) { return level; }
        public virtual void CalcReturnType() { }
        public virtual void Parse(AllowUseSettings auc) { }
        public MetaClass GetReturnMetaClass()
        {
            if (m_MetaType == null)
            {
                GetReturnMetaDefineType();
            }
            return m_MetaType?.metaClass;
        }
        public virtual void SetMetaType( MetaType mt )
        {
            m_MetaType = mt;
        }
        public virtual MetaType GetReturnMetaDefineType()
        {
            return m_MetaType;
        }
        public virtual string ToFormatString()
        {
            return "";
        }
        public virtual string ToTokenString()
        {
            return "";
        }

    }
}
