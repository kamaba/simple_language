//****************************************************************************
//  File:      MetaGenTempalteFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Core.Statements;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaGenTempalteFunction : MetaMemberFunction
    {
        public MetaMemberFunction sourceTemplateFunctionMetaMemberFunction => m_SourceTemplateFunctionMetaMemberFunction;
        public List<MetaGenTemplate> metaGenTemplateList => m_MetaGenTemplateList;

        protected MetaMemberFunction m_SourceTemplateFunctionMetaMemberFunction = null;
        protected List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        public MetaGenTempalteFunction(MetaMemberFunction mmc, List<MetaGenTemplate> list ) : base(mmc.ownerMetaClass)
        {
            m_SourceTemplateFunctionMetaMemberFunction = mmc;
            UpdateGenMemberFunctionByTemplateClass(mmc);
            m_MetaGenTemplateList = list;
        }
        public MetaGenTempalteFunction( MetaGenTempalteFunction mgtf ) : base(mgtf)
        {
            m_SourceMetaMemberFunction = mgtf.m_SourceMetaMemberFunction;
            m_SourceTemplateFunctionMetaMemberFunction = mgtf.m_SourceTemplateFunctionMetaMemberFunction;
            m_MetaGenTemplateList = mgtf.m_MetaGenTemplateList;
        }
        public MetaGenTempalteFunction(MetaClass mc, string _name) : base(mc)
        {
            m_Name = _name;
            m_IsCanRewrite = true;
            m_MetaMemberParamCollection.Clear();

            m_MetaBlockStatements = new MetaBlockStatements(this, null);
            m_MetaBlockStatements.isOnFunction = true;

            Init();
        }
        public bool MatchInputTemplateInsance(List<MetaClass> instMcList)
        {
            if (m_MetaGenTemplateList.Count != instMcList.Count)
            {
                return false;
            }

            for (int i = 0; i < m_MetaGenTemplateList.Count; i++)
            {
                var c1 = m_MetaGenTemplateList[i];
                var c2 = instMcList[i];

                if (c1.metaType.metaClass != c2 )
                {
                    return false;
                }
            }
            return true;

        }
        public void UpdateGenMemberFunctionByTemplateClass(MetaMemberFunction mmf)
        {
            m_MetaMemberParamCollection = mmf.metaMemberParamCollection;
            for( int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++ )
            {
            }
            m_FileMetaMemberFunction = mmf.fileMetaMemberFunction;
            m_Name = mmf.name;

            m_IsStatic = mmf.isStatic;
            m_IsGet = mmf.isGet;
            m_IsSet = mmf.isSet;
            m_IsFinal = mmf.isFinal;
            m_MetaBlockStatements = mmf.metaBlockStatements;
            m_ConstructInitFunction = mmf.isConstructInitFunction;
            m_SourceMetaMemberFunction = mmf.sourceMetaMemberFunction;
            m_ReturnMetaVariable = new MetaVariable(mmf.returnMetaVariable);

            //    m_OriginalMetaMemberFunction = mmf;
            //    m_Name = mmf.m_Name;
            //    m_FileMetaMemberFunction = mmf.m_FileMetaMemberFunction;
            //    isStatic = mmf.isStatic;
            //    isGet = mmf.isGet;
            //    isSet = mmf.isSet;
            //    isFinal = mmf.isFinal;
            //    m_IsMustNeedReturnStatements = mmf.m_IsMustNeedReturnStatements;
            //    m_MethodCallType = mmf.m_MethodCallType;
            //    isTemplateInParam = mmf.isTemplateInParam;
            //    m_IsTemplateFunction = mmf.m_IsTemplateFunction;
            //    m_DefineMetaType = new MetaType(mmf.m_DefineMetaType);
            //    m_MetaBlockStatements = new MetaBlockStatements(this);
            //    m_MetaBlockStatements.isOnFunction = true;
            //    m_MetaMemberParamCollection = new MetaDefineParamCollection();
        }
        public MetaGenTemplate GetMetaGenTemplate( string name )
        {
            return m_MetaGenTemplateList.Find(a => a.name == name);
        }
        void ParseMetaMemberFunctionDefineMetaType()
        {
            if ( m_ReturnMetaVariable?.metaDefineType != null)
            {
                if (!(m_ReturnMetaVariable.metaDefineType.eType == EMetaTypeType.MetaClass
                    && m_ReturnMetaVariable.metaDefineType.metaClass.isTemplateClass == false))
                {
                    TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(m_ReturnMetaVariable.metaDefineType, m_OwnerMetaClass as MetaGenTemplateClass, this );
                }
            }
            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                var mdp = m_MetaMemberParamCollection.metaDefineParamList[i];
                if (!(mdp.metaVariable.metaDefineType.eType == EMetaTypeType.MetaClass
                    && mdp.metaVariable.metaDefineType.metaClass.isTemplateClass == false))
                {
                    TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(mdp.metaVariable.metaDefineType, m_OwnerMetaClass as MetaGenTemplateClass, this );
                }
            }
        }
        public void UpdateRegsterGenMetaFunction()
        {
            //这个过程是 绑定 原来注册过来的T的已有的类

            List<MetaGenTemplate> mgtList = m_MetaGenTemplateList;
            var curfun = this.m_SourceMetaMemberFunction;
            while (true)
            {
                if (curfun.sourceMetaMemberFunction == null)
                    break;
                curfun = curfun.sourceMetaMemberFunction;
            }

            for (int i = 0; i < curfun.bindStructTemplateFunctionMtList.Count; i++)
            {
                curfun.bindStructTemplateFunctionMtList[i].UpdateMetaGenTemplate(mgtList);
            }
        }
        public void UpdateRegsterGenMetaFunctionAndClass(List<MetaGenTemplate> classGtList)
        {
            //这个过程是 绑定 原来注册过来的T的已有的类

            List<MetaGenTemplate> mgtList = m_MetaGenTemplateList;
            var curfun = this.m_SourceMetaMemberFunction;
            while (true)
            {
                if (curfun.sourceMetaMemberFunction == null)
                    break;
                curfun = curfun.sourceMetaMemberFunction;
            }
            if(curfun.bindStructTemplateFunctionAndClassMtList.Count == 0 )
            {
                return;
            }

            mgtList.AddRange(classGtList);

            for (int i = 0; i < curfun.bindStructTemplateFunctionAndClassMtList.Count; i++)
            {
                curfun.bindStructTemplateFunctionAndClassMtList[i].UpdateMetaGenTemplate(mgtList);
            }
        }
        public override bool Parse()
        {
            UpdateRegsterGenMetaFunction();

            if( this.m_OwnerMetaClass.isTemplateClass )
            {
                for( int i = 0; i < this.m_OwnerMetaClass.metaGenTemplateClassList.Count; i++ )
                {
                    var mgtc = this.m_OwnerMetaClass.metaGenTemplateClassList[i];
                    mgtc.UpdateRegisterTemplateFunction();
                }
            }
            ParseMetaMemberFunctionDefineMetaType();
            UpdateFunctionName();

            return true;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(returnMetaVariable?.metaDefineType?.ToFormatString());
            sb.Append(" ");
            sb.Append(name);
            sb.Append("<");
            for( int i = 0; i < m_MetaGenTemplateList.Count; i++ )
            {
                var mgt = m_MetaGenTemplateList[i];
                sb.Append(mgt.ToString());
            }
            sb.Append(">");           
            sb.Append("(");

            for (int i = 0; i < m_MetaMemberParamCollection.metaDefineParamList.Count; i++)
            {
                MetaDefineParam mpl = m_MetaMemberParamCollection.metaDefineParamList[i];
                sb.Append(mpl.ToString());
                if (i < m_MetaMemberParamCollection.metaDefineParamList.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");

            return sb.ToString();
        }
    }
}
