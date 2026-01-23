


using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaExpressTypeConvert : MetaExpressNode
    {
        public MetaExpressTypeConvert( List<FileMetaBaseTerm> fmcl, MetaClass mc, MetaBlockStatements mbs, MetaType defineMT, MetaVariable mv  )
        {
        }
        public override void Parse(AllowUseSettings auc)
        {
        }
        public override int CalcParseLevel(int level)
        {
            //if (m_MetaCallLink != null)
            //    level = m_MetaCallLink.CalcParseLevel(level);
            return level;
        }
        public override void CalcReturnType()
        {
            //if (m_MetaCallLink != null)
            //{
            //    m_MetaCallLink.CalcReturnType();
            //}

            m_MetaType = GetReturnMetaDefineType();
        }
        public MetaVariable GetMetaVariable()
        {
            //if (m_MetaCallLink != null)
            //{
            //    return m_MetaCallLink.ExecuteGetMetaVariable();
            //}
            return null;
        }
        public override MetaType GetReturnMetaDefineType()
        {
            if (m_MetaType != null)
            {
                return m_MetaType;
            }

            m_MetaType = new MetaType(CoreMetaClassManager.objectMetaClass);

            return m_MetaType;
        }
        public override string ToFormatString()
        {
            return "ExpressCallLink Error!!";
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            //if (m_MetaCallLink != null)
            //{
            //    sb.Append(m_MetaCallLink.ToTokenString());
            //}
            return sb.ToString();
        }
    }
}
