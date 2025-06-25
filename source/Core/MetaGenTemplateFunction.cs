//****************************************************************************
//  File:      MetaGenTempalteFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************

using SimpleLanguage.Core.Statements;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public class MetaGenTempalteFunction : MetaMemberFunction
    {
        public List<MetaGenTemplate> metaGenTemplateList => m_MetaGenTemplateList;

        protected MetaMemberFunction m_OriginalMetaMemberFunction = null;
        protected List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        public MetaGenTempalteFunction(MetaMemberFunction mmc, List<MetaGenTemplate> list ) : base(mmc.ownerMetaClass)
        {
            m_OriginalMetaMemberFunction = mmc;
            UpdateGenMemberFunctionByTemplateClass(mmc);
            m_MetaGenTemplateList = list;
        }
        public MetaGenTempalteFunction(MetaClass mc, string _name) : base(mc)
        {
            m_Name = _name;
            isCanRewrite = true;
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

                if (c1.metaType.metaClass != c2)
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
                //MetaMemberParamCollection.metaDefineParamList[i].
            }
            m_FileMetaMemberFunction = mmf.fileMetaMemberFunction;
            m_Name = mmf.name;

            m_IsStatic = mmf.isStatic;
            isGet = mmf.isGet;
            isSet = mmf.isSet;
            isFinal = mmf.isFinal;
            m_MetaBlockStatements = mmf.metaBlockStatements;
            m_ConstructInitFunction = mmf.isConstructInitFunction;
            m_ReturnMetaVariable = mmf.returnMetaVariable;
            m_DefineMetaType = mmf.metaDefineType;

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
        public override void Parse()
        {
        }
    }
}
