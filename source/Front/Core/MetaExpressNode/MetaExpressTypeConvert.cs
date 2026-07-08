


using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaExpressTypeConvert : MetaExpressNodeBase
    {
        public MetaExpressTypeConvert( List<FileMetaBaseTerm> fmcl, MetaClass mc, MetaBlockStatements mbs, MetaType defineMT, MetaVariable mv  )
        {
        }
        public override void Parse(AllowUseSettings auc)
        {
            m_ParsedState = EParseState.ParseSuccess;
        }
        //public override int CalcParseLevel(int level)
        //{
        //    //if (m_MetaCallLink != null)
        //    //    level = m_MetaCallLink.CalcParseLevel(level);
        //    return level;
        //}
        public override void CalcReturnType()
        {
            //if (m_MetaCallLink != null)
            //{
            //    m_MetaCallLink.CalcReturnType();
            //}

            m_ExpressReturnMetaType = GetReturnMetaType();
        }
        public MetaVariable GetMetaVariable()
        {
            //if (m_MetaCallLink != null)
            //{
            //    return m_MetaCallLink.ExecuteGetMetaVariable();
            //}
            return null;
        }
        public override MetaType GetReturnMetaType()
        {
            if (m_ExpressReturnMetaType != null)
            {
                return m_ExpressReturnMetaType;
            }

            m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);

            return m_ExpressReturnMetaType;
        }
        public override string ToFormatString()
        {
            return "ExpressCallLink Error!!";
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            //if (m_MetaCallLink != null)
            //{
            //    sb.Append(m_MetaCallLink.ToString());
            //}
            return sb.ToString();
        }
    }
}
