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

        protected List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        public MetaGenTempalteFunction(MetaMemberFunction mmc, List<MetaGenTemplate> list ) : base(mmc)
        {
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
        public MetaGenTemplate GetMetaGenTemplate( string name )
        {
            return m_MetaGenTemplateList.Find(a => a.name == name);
        }
        public override void Parse()
        {
        }
    }
}
