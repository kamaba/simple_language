


namespace SimpleLanguage.Core
{
    public enum ELeftRightOpSign
    {
        Add,
        IAdd,
        Minus,
        IMinus,
        Multiply,
        IMultiply,
        Divide,
        IDivide,
        Modulo,
        IModulo,
        InclusiveOr,
        Combine,
        XOR,
        Shi,
        IShi,
        Shr,
        IShr,
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        And,
        IAnd,
        Or,
        IOr,
        Cast,
        IsType,
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
        public bool isNewExpressNode => m_IsNewExpressNode;
        public MetaType metaType => m_MetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;

        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaType m_MetaType = null;
        protected bool m_IsNewExpressNode = false;


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
