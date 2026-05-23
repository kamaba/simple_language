


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
    public  abstract class MetaExpressNodeBase
    {
        public virtual int opLevel
        {
            get
            {
                return MetaTypeFactory.GetOpLevelByMetaType(m_ExpressReturnMetaType);
            }
        }
        public bool convertNewExpressNode => m_ConvertNewExpressNode;
        public bool convertCallExpressNode => m_ConvertCallExpressNode;
        public virtual Token token => m_Token;
        public MetaType expressReturnMetaType => m_ExpressReturnMetaType;
        /// <summary>所属上下文：普通类。</summary>
        public MetaClass ownerMetaClass => m_OwnerMetaBase as MetaClass;
        /// <summary>所属上下文：<see cref="MetaData"/>（与 <see cref="MetaVariable"/> 对称）。</summary>
        public MetaData ownerMetaData => m_OwnerMetaBase as MetaData;
        /// <summary>所属上下文：<see cref="MetaEnum"/>。</summary>
        public MetaEnum ownerMetaEnum => m_OwnerMetaBase as MetaEnum;
        /// <summary>原始宿主节点（Class / Data / Enum）。</summary>
        public MetaBase ownerMetaBase => m_OwnerMetaBase;
        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;


        /// <summary>宿主：<see cref="MetaClass"/> / <see cref="MetaData"/> / <see cref="MetaEnum"/>，与 <see cref="MetaVariable"/> 一致。</summary>
        protected MetaBase m_OwnerMetaBase = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaType m_ExpressReturnMetaType = null;
        protected bool m_ConvertNewExpressNode = false;
        protected bool m_ConvertCallExpressNode = false;
        protected Token m_Token = null;
        protected bool m_Parse = false;

        public void SetToken(Token token) { m_Token = token; }
        public virtual int CalcParseLevel(int level) { return level; }
        public virtual void CalcReturnType() { }
        public virtual void Parse(AllowUseSettings auc) { }
        public MetaClass GetReturnMetaClass()
        {
            if (m_ExpressReturnMetaType == null)
            {
                GetReturnMetaType();
            }
            return m_ExpressReturnMetaType?.metaClass;
        }
        public virtual MetaType GetReturnMetaType()
        {
            return m_ExpressReturnMetaType;
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
}
