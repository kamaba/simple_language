
using SimpleLanguage.Core.Statements;

namespace SimpleLanguage.Core
{
    public enum ELeftRightOpSign
    {
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
                return MetaTypeFactory.GetOpLevelByMetaType(m_MetaDefineType);
            }
        }

        public MetaType metaDefineType => m_MetaDefineType;

        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaType m_MetaDefineType = null;
        public virtual int CalcParseLevel(int level) { return level; }
        public virtual void CalcReturnType() { }
        public virtual void Parse(AllowUseSettings auc) { }
        public MetaClass GetReturnMetaClass()
        {
            if (m_MetaDefineType == null)
            {
                GetReturnMetaDefineType();
            }
            return m_MetaDefineType?.metaClass;
        }
        public virtual void SetMetaType( MetaType mt )
        {
            m_MetaDefineType = mt;
        }
        public virtual MetaType GetReturnMetaDefineType()
        {
            return m_MetaDefineType;
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
