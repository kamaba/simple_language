


using SimpleLanguage.Compile;
using SimpleLanguage.Core.IR;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaArrayExpressNode : MetaExpressNode
    {
        public List<MetaExpressNode> metaCallArray => m_MetaCallArray;


        List<FileMetaBaseTerm>  m_FileMetaBaseTermList = new List<FileMetaBaseTerm>();
        List<MetaExpressNode>   m_MetaCallArray = new List<MetaExpressNode>();
        public MetaArrayExpressNode( List<FileMetaBaseTerm> fmcl, MetaClass mc, MetaBlockStatements mbs, MetaVariable mv)
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;

            m_FileMetaBaseTermList = fmcl;

            if (m_FileMetaBaseTermList != null)
            {
                for (int i = 0; i < m_FileMetaBaseTermList.Count; i++)
                {
                    var fmc = m_FileMetaBaseTermList[i];

                    if( fmc is FileMetaSymbolTerm fmst )
                    {
                    }
                    else
                    {
                        CreateExpressParam cep = new CreateExpressParam();
                        cep.fme = fmc;
                        cep.equalMetaVariable = mv;
                        cep.metaType = mv?.metaDefineType;
                        cep.ownerMBS = m_OwnerMetaBlockStatements;
                        cep.ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass;

                        var en = ExpressManager.CreateExpressNodeByCEP(cep);
                        m_MetaCallArray.Add(en);
                    }
                }
            }
        }
        public override void Parse(AllowUseSettings auc)
        {
            for (int i = 0; i < m_MetaCallArray.Count; i++)
            {
                var mcac = m_MetaCallArray[i];
                mcac.Parse(auc);

                //if (mcac.finalCallNode.visitType == MetaVisitNode.EVisitType.New)
                //{
                //    m_IsNewExpressNode = true;
                //}
            }
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

            if( m_MetaCallArray.Count > 0 )
            {
                //m_MetaType.SetArrayDimension(1);
                m_MetaType.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                MetaType cmt = new MetaType(CoreMetaClassManager.objectMetaClass);
                m_MetaType.AddDefineTemplateMetaType(cmt);
                m_MetaType.AddGenTemplateMetaType(cmt);

                m_MetaType = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(m_MetaType, true, out bool isgmc);
                m_MetaType.SetArrayLength(m_MetaCallArray.Count);
            }
            //for (int i = 0; i < m_MetaCallArray.Count; i++)
            //{
            //    var mcac = m_MetaCallArray[i];
            //}
            //if (m_MetaCallLink == null)
            //    return null;

            //m_MetaDefineType = m_MetaCallLink.GetMetaDefineType();
            return m_MetaType;
        }
        public override string ToFormatString()
        {
            //if (m_MetaCallLink != null)
            //{
            //    StringBuilder sb = new StringBuilder();
            //    sb.Append(m_MetaCallLink.ToFormatString());
            //    return sb.ToString();
            //}
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
