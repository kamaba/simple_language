//****************************************************************************
//  File:      MethodManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.CoreFileMeta;

namespace SimpleLanguage.Core
{
    class MethodManager
    {
        public static MethodManager s_Instance = null;
        public static MethodManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new MethodManager();
                }
                return s_Instance;
            }
        }
        public List<MetaMemberFunction> metaOriginalFunctionList => m_MetaOriginalFunctionList;
        public List<MetaMemberFunction> metaDynamicFunctionList => m_MetaDynamicFunctionList;

        private Dictionary<string, MetaFunction> m_MetaAllFunctionDict = new Dictionary<string, MetaFunction>();

        private List<MetaMemberFunction> m_MetaOriginalFunctionList = new List<MetaMemberFunction>();
        private List<MetaMemberFunction> m_MetaDynamicFunctionList = new List<MetaMemberFunction>();


        public static MetaVariable GetMetaVariableInMetaClass( MetaClass mc, FileMetaCallLink fmcl )
        {
            MetaClass mb = mc;
            MetaVariable mv = null;
            for ( int i = 0; i < fmcl.callNodeList.Count; i++ )
            {
                var cnl = fmcl.callNodeList[i];

                mv = mb.GetMetaMemberVariableByName(cnl.name);
                if( mv == null )
                {
                    return null;
                }

                mb = mv.metaDefineType.metaClass;

                if (mb == null)
                    return null;

            }
            return mv;
        }
        public void AddMetaAllFunction( MetaFunction mf )
        {
            if(m_MetaAllFunctionDict.ContainsKey(mf.functionAllName ) )
            {
                return;
            }
            m_MetaAllFunctionDict.Add(mf.functionAllName, mf);
        }
        public void AddOriginalMemeberFunction(MetaMemberFunction mmf)
        {
            if (m_MetaOriginalFunctionList.IndexOf(mmf) == -1)
            {
                m_MetaOriginalFunctionList.Add(mmf);
                AddMetaAllFunction(mmf);
            }
        }
        public void AddDynamicMemeberFunction(MetaMemberFunction mmf)
        {
            if (m_MetaDynamicFunctionList.IndexOf(mmf) == -1)
            {
                m_MetaDynamicFunctionList.Add(mmf);
                AddMetaAllFunction(mmf);
            }
        }
        public void ParseStatements()
        {
            var list = new List<MetaMemberFunction>(m_MetaOriginalFunctionList);

            list.Sort( (a, b) => a.parseLevel - b.parseLevel );

            foreach (var v in list)
            {
                v.CreateMetaExpress();
                v.ParseMetaExpress();
                v.ParseStatements();
            }
        }
    }
}
