


using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaArrayExpressNode : MetaExpressNodeBase
    {
        public List<MetaExpressNodeBase> metaCallArray => m_MetaCallArray;


        private List<FileMetaBaseTerm>  m_FileMetaBaseTermList = null;
        private List<MetaExpressNodeBase>   m_MetaCallArray = new List<MetaExpressNodeBase>();
        public MetaArrayExpressNode(MetaBase mc, MetaBlockStatements mbs, MetaType defineMT, MetaVariable mv)
        {
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
        }
        public MetaArrayExpressNode(FileMetaBracketTerm fmbt, MetaBase mc, MetaBlockStatements mbs, MetaType defineMT, MetaVariable mv  )
        {
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_FileMetaBaseTermList = fmbt.fileMetaExpressList;
            m_Token = fmbt.token;
            if ( m_FileMetaBaseTermList == null )
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "m_FileMetaBaseTermList need not null");
                return;
            }

            MetaType cmt = null;
            if (defineMT != null )
            {
                if( defineMT.metaClass != CoreMetaClassManager.objectMetaClass )
                {
                    if (!defineMT.IsArray() )
                    {
                        Token token = m_Token;
                        if (mv?.token != null)
                        {
                            token = mv?.token;
                        }
                        Log.AddMetaCoreLog(LID.MetaCoreArrayMustIsArray, token, "MetaArrayExpressNode", mv?.name);
                        return;
                    }
                    this.m_ExpressReturnMetaType = defineMT;
                    if( m_ExpressReturnMetaType.defineTemplateMetaTypeList.Count > 0 )
                        cmt = m_ExpressReturnMetaType.defineTemplateMetaTypeList[0];
                    else
                    {
                        if( defineMT.metaClass is MetaGenTemplateClass mgtc )
                        {
                            if( mgtc.genMetaClassTemplateList.Count > 0 )
                            {
                                cmt = new MetaType( mgtc.genMetaClassTemplateList[0] );
                            }
                        }
                    }

                    if (cmt == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, "MetaArrayExpressNode defineMT need have template meta type");
                    }
                }
            }
            for (int i = 0; i < m_FileMetaBaseTermList.Count; i++)
            {
                var cterm = m_FileMetaBaseTermList[i];
                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = cterm;
                cep.equalMetaVariable = null;
                cep.metaType = cmt;
                cep.ownerMBS = m_OwnerMetaBlockStatements;
                cep.ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass;

                var en = ExpressManager.CreateExpressNodeByCEP(cep);
                m_MetaCallArray.Add(en);
            }
        }
        public override void Parse(AllowUseSettings auc)
        {
            for (int i = 0; i < m_MetaCallArray.Count; i++)
            {
                var mcac = m_MetaCallArray[i];
                mcac.Parse(auc);
            }
        }
        public override int CalcParseLevel(int level)
        {
            return level;
        }
        public override void CalcReturnType()
        {
            m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);

            if (m_MetaCallArray.Count > 0)
            {
                m_ExpressReturnMetaType.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);


                MetaType cmt = null;
                for (int i = 0; i < m_MetaCallArray.Count; i++)
                {
                    MetaType cmt2 = m_MetaCallArray[i].GetReturnMetaType();
                    if (cmt2.metaClass == CoreMetaClassManager.objectMetaClass)
                    {
                        break;
                    }

                    if (cmt != null)
                    {
                        if (cmt.metaClass != cmt2.metaClass)
                        {
                            cmt = new MetaType(CoreMetaClassManager.objectMetaClass);
                            break;
                        }
                    }
                    else
                    {
                        cmt = cmt2;
                    }
                }
                m_ExpressReturnMetaType.AddDefineTemplateMetaType(cmt);

                var newmt = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(m_ExpressReturnMetaType, true, out bool isgmc);
                newmt.SetArrayLength(m_MetaCallArray.Count);

                m_ExpressReturnMetaType = newmt;// new MetaType(newmt.metaClass as MetaGenTemplateClass, m_MetaType.defineTemplateMetaTypeList, m_MetaType.defineTemplateMetaTypeList);
            }
        }
        public override string ToFormatString()
        {
            if (m_MetaCallArray != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for( int i = 0; i < m_MetaCallArray.Count; i++ )
                {
                    sb.Append(m_MetaCallArray[i].ToFormatString());
                    if( m_MetaCallArray.Count - 1 != i )
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
                return sb.ToString();
            }
            return "MetaExpressArray.ToFormatString()";
        }
        public override string ToString()
        {
            return ToFormatString();
        }
    }
}
