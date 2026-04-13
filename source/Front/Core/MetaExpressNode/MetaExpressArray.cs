


using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaArrayExpressNode : MetaExpressNode
    {
        public List<MetaExpressNode> metaCallArray => m_MetaCallArray;


        List<FileMetaBaseTerm>  m_FileMetaBaseTermList = new List<FileMetaBaseTerm>();
        List<MetaExpressNode>   m_MetaCallArray = new List<MetaExpressNode>();
        public MetaArrayExpressNode(MetaClass mc, MetaBlockStatements mbs, MetaType defineMT, MetaVariable mv)
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
        }
        public MetaArrayExpressNode( List<FileMetaBaseTerm> fmcl, MetaClass mc, MetaBlockStatements mbs, MetaType defineMT, MetaVariable mv  )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;

            MetaType cmt = null;
            if (defineMT != null )
            {
                if (!defineMT.IsArray() || defineMT.defineTemplateMetaTypeList.Count != 1)
                {
                    Debug.Assert(false);
                    Log.AddMetaCoreLog(LID.Unknown, "如果是表达式创建数组对象，必须最后一位知道是多少的长度!");
                    return;
                }
                this.m_MetaType = defineMT;
                cmt = m_MetaType.defineTemplateMetaTypeList[0];
            }

            m_FileMetaBaseTermList = fmcl;

            if (m_FileMetaBaseTermList != null)
            {
                for( int i = 0; i < m_FileMetaBaseTermList.Count; i++ )
                {
                    var cterm = m_FileMetaBaseTermList[i];
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.fme = cterm;
                    cep.equalMetaVariable = null;
                    cep.metaType = cmt;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass;

                    var en = ExpressManager.CreateExpressNodeByCEP(cep);
                    m_MetaCallArray.Add(en);
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
                m_MetaType.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);


                MetaType cmt = null;
                for ( int i = 0; i < m_MetaCallArray.Count; i++ )
                {
                    MetaType cmt2 = m_MetaCallArray[i].GetReturnMetaDefineType();
                    if( cmt2.metaClass == CoreMetaClassManager.objectMetaClass )
                    {
                        break;
                    }

                    if( cmt != null )
                    {
                        if( cmt.metaClass != cmt2.metaClass )
                        {
                            cmt =new MetaType( CoreMetaClassManager.objectMetaClass );
                            break;
                        }
                    }
                    else
                    {
                        cmt = cmt2;                       
                    }
                }
                m_MetaType.AddDefineTemplateMetaType(cmt);
                //m_MetaType.AddGenTemplateMetaType(cmt);

                var newmt = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(m_MetaType, true, out bool isgmc);
                newmt.SetArrayLength(m_MetaCallArray.Count);

                m_MetaType = newmt;// new MetaType(newmt.metaClass as MetaGenTemplateClass, m_MetaType.defineTemplateMetaTypeList, m_MetaType.defineTemplateMetaTypeList);
            }
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
