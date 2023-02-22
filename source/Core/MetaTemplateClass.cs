//****************************************************************************
//  File:      TemplateMetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;
using SimpleLanguage.Core.SelfMeta;

namespace SimpleLanguage.Core
{
    public class MetaTemplateClass : MetaClass
    {
        protected List<MetaGenTemplate> m_MetaGenTemplateList = new List<MetaGenTemplate>();
        protected Dictionary<string, MetaGenMemberVariable> m_MetaGenMemberVariableDict = new Dictionary<string, MetaGenMemberVariable>();
        protected Dictionary<string, List<MetaGenMemberFunction>> m_MetaGenMemberFunctionListDict = new Dictionary<string, List<MetaGenMemberFunction>>();

        public MetaTemplateClass(MetaClass mc) : base(mc)
        {
        }
        public MetaGenMemberVariable GetMetaGenMemberVariable( string name )
        {
            if(m_MetaGenMemberVariableDict.ContainsKey(name) )
            {
                return m_MetaGenMemberVariableDict[name];
            }
            return null;
        }

        public override MetaMemberVariable GetMetaMemberVariableByName(string name)
        {
            if (m_MetaGenMemberVariableDict.ContainsKey(name))
            {
                return m_MetaGenMemberVariableDict[name];
            }
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name];
            }
            if (m_MetaExtendMemeberVariableDict.ContainsKey(name))
            {
                return m_MetaExtendMemeberVariableDict[name];
            }
            return null;
        }
        public void AddMetaGenTemplate( MetaGenTemplate mgt )
        {
            m_MetaGenTemplateList.Add(mgt);
        }
        public void UpdateGenMember()
        {
            for( int i = 0; i < m_MetaGenTemplateList.Count; i++ )
            {
                string tname = m_MetaGenTemplateList[i].name;

                foreach( var v in m_MetaMemberVariableDict.Values )
                {
                    if( v.metaDefineType.metaTemplate != null )
                    {
                        if( v.metaDefineType.metaTemplate.name == tname )
                        {
                            MetaGenMemberVariable mgmv = new MetaGenMemberVariable(v, m_MetaGenTemplateList[i]);

                            mgmv.UpdateGenMemberVariable();

                            m_MetaGenMemberVariableDict.Add(mgmv.name, mgmv);
                        }
                    }
                }
            }

            foreach (var v in m_MetaMemberFunctionListDict)
            {
                if (v.Value.Count > 0)
                {
                    var list = new List<MetaGenMemberFunction>();
                    m_MetaGenMemberFunctionListDict.Add(v.Key, list);

                    for (int j = 0; j < v.Value.Count; j++)
                    {
                        var curFun = v.Value[j];

                        MetaGenMemberFunction mgmf = new MetaGenMemberFunction(curFun, m_MetaGenTemplateList);
                        list.Add(mgmf);
                        mgmf.SetOwnerMetaClass(this);
                        mgmf.UpdateGenFunction();
                    }
                }
            }
        }
        public bool Adapter(MetaInputTemplateCollection mitc)
        {
            if( mitc.metaTemplateParamsList.Count == m_MetaGenTemplateList.Count )
            {
                for( int i = 0; i < mitc.metaTemplateParamsList.Count; i++ )
                {
                    var mtpl = mitc.metaTemplateParamsList[i];
                    var mgtl = m_MetaGenTemplateList[i];
                    if(mgtl.EqualWithMetaType(mtpl) )
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }
        //private void ParseInit()
        //{
        //    SetExtendClass(CoreMetaClassManager.objectMetaClass);
        //    HandleExtendClassVariable();
        //    MetaType mdt = new MetaType(this);
        //    m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, null, null);
        //}

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Name);
            sb.Append("<");
            for( int i = 0; i < m_MetaGenTemplateList.Count; i++ )
            {
                sb.Append(m_MetaGenTemplateList[i].ToFormatString());

                if (i < m_MetaGenTemplateList.Count - 1)
                    sb.Append(",");
            }
            sb.Append(">");

            return sb.ToString();
        }
    }
}
