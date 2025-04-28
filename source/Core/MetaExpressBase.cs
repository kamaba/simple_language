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
                return MetaTypeFactory.GetOpLevel( eType );
            }
        }
        public EType eType { get; protected set; } = EType.None;

        protected MetaClass m_OwnerMetaClass = null;
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaType m_MetaDefineType = null;
        public virtual Token GetToken() { return null; }
        public virtual int CalcParseLevel(int level) { return level; }
        public virtual void CalcReturnType() {  }
        public virtual void Parse(AllowUseSettings auc) { }
        public MetaClass GetReturnMetaClass()
        {
            if( m_MetaDefineType == null )
            {
                GetReturnMetaDefineType();
            }
            return m_MetaDefineType?.metaClass;
        }
        public void SetEType( EType etype )
        {
            eType = etype;
        }
        public virtual MetaType GetReturnMetaDefineType()
        {
            if(m_MetaDefineType != null && m_MetaDefineType.isDefineMetaClass )
            {
                return m_MetaDefineType;
            }
            if (eType == EType.None)
            {
                return null;
            }
            MetaClass mc = CoreMetaClassManager.GetMetaClassByEType(eType);

            if( mc == null)
            {
                Console.WriteLine("Error 获取返回值错误  Etype: " + eType.ToString() );
            }

            m_MetaDefineType = new MetaType(mc);
            return m_MetaDefineType;
        }
        public virtual string ToFormatString()
        {
            return "";
        }
        public virtual string ToIRString()
        {
            return "";
        }
        public virtual string ToTokenString()
        {
            return "";
        }

    }
}
