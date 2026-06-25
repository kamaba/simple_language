


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
        private List<MetaExpressNodeBase> m_MetaCallArray = new List<MetaExpressNodeBase>();
        public MetaArrayExpressNode(MetaBase mc, MetaBlockStatements mbs, MetaType defineMT, MetaVariable mv)
        {
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
        }
        public static MetaArrayExpressNode CreateFromFileMetaMemberData(
            FileMetaMemberData fmmd,
            MetaBase owner,
            MetaBlockStatements mbs,
            MetaType elementHint)
        {
            if (fmmd == null || fmmd.DataType != FileMetaMemberData.EMemberDataType.Array)
            {
                return null;
            }

            var node = new MetaArrayExpressNode(owner, mbs, elementHint, null);
            node.m_Token = fmmd.nameToken ?? fmmd.token;

            MetaType cmt = null;
            if (elementHint != null && elementHint.IsArray())
            {
                var templates = elementHint.defineTemplateMetaTypeList;
                if (templates != null && templates.Count > 0)
                {
                    cmt = templates[0];
                }
            }

            for (int i = 0; i < fmmd.fileMetaMemberData.Count; i++)
            {
                var child = fmmd.fileMetaMemberData[i];
                var en = MetaMemberData.CreateExpressFromFileMetaMemberData(child, owner, mbs, cmt);
                if (en != null)
                {
                    node.m_MetaCallArray.Add(en);
                }
            }
            return node;
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
        //public override int CalcParseLevel(int level)
        //{
        //    return level;
        //}
        public override void CalcReturnType()
        {
            if (m_ExpressReturnMetaType != null) return;

            if (m_MetaCallArray.Count == 0)
            {
                var build0 = new MetaType();
                build0.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                build0.AddDefineTemplateMetaType(new MetaType(CoreMetaClassManager.objectMetaClass));
                var array0 = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(build0, true, out bool _);
                array0.SetArrayLength(0);
                m_ExpressReturnMetaType = array0;                
                return;
            }

            List<MetaType> mtList = new List<MetaType>();
            for (int i = 0; i < m_MetaCallArray.Count; i++)
            {
                var express = m_MetaCallArray[i];
                express.CalcReturnType();
                mtList.Add(express.GetReturnMetaType());
            }

            MetaType inferredElementMetaType = TypeManager.GetMaxCompatibleMetaTypeFromList(mtList);

            var build = new MetaType();
            build.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
            build.AddDefineTemplateMetaType(inferredElementMetaType);
            var resultArrayMetaType = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(build, true, out bool _isGenericMetaClass);
            resultArrayMetaType.SetArrayLength(m_MetaCallArray.Count);

            m_ExpressReturnMetaType = resultArrayMetaType;
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
            return "ExpressArrayNode:" + ToFormatString();
        }
    }
}
