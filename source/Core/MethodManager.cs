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
        public List<MetaMemberFunction> metaClassTemplateGenFunctionList => m_MetaClassTemplateGenFunctionList;
        public List<MetaMemberFunction> metaFunctionTemplateFunctionList => m_MetaFunctionTemplateFunctionList;
        public List<MetaMemberFunction> metaDynamicFunctionList => m_MetaDynamicFunctionList;

        private Dictionary<string, MetaFunction> m_MetaAllFunctionDict = new Dictionary<string, MetaFunction>();

        private List<MetaMemberFunction> m_MetaOriginalFunctionList = new List<MetaMemberFunction>();
        private List<MetaMemberFunction> m_MetaClassTemplateGenFunctionList = new List<MetaMemberFunction>();
        private List<MetaMemberFunction> m_MetaFunctionTemplateFunctionList = new List<MetaMemberFunction>();
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
        public void AddClassTemplateMemeberFunction(MetaMemberFunction mmf)
        {
            if (m_MetaClassTemplateGenFunctionList.IndexOf(mmf) == -1)
            {
                m_MetaClassTemplateGenFunctionList.Add(mmf);
                AddMetaAllFunction(mmf);
            }
        }
        public void AddFunctionTemplateMemeberFunction(MetaMemberFunction mmf)
        {
            if (m_MetaFunctionTemplateFunctionList.IndexOf(mmf) == -1)
            {
                m_MetaFunctionTemplateFunctionList.Add(mmf);
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
        public void CreateMetaExpress( int type )
        {
            List<MetaMemberFunction> list = type switch
            {
                1 => m_MetaClassTemplateGenFunctionList,
                2 => m_MetaFunctionTemplateFunctionList,
                3 => m_MetaDynamicFunctionList,
                _ => m_MetaOriginalFunctionList,
            };
            foreach (var v in list)
            {
                v.CreateMetaExpress();
            }
        }
        public void ParseMetaExpress(int type)
        {
            List<MetaMemberFunction> list = type switch
            {
                1 => m_MetaClassTemplateGenFunctionList,
                2 => m_MetaFunctionTemplateFunctionList,
                3 => m_MetaDynamicFunctionList,
                _ => m_MetaOriginalFunctionList,
            };
            foreach (var v in list)
            {
                v.ParseMetaExpress();
            }
        }
        public void ParseStatements( int type )
        {
            List<MetaMemberFunction> list = type switch
            {
                1 => m_MetaClassTemplateGenFunctionList,
                2 => m_MetaFunctionTemplateFunctionList,
                3 => m_MetaDynamicFunctionList,
                _ => m_MetaOriginalFunctionList,
            };
            foreach (var v in list)
            {
                v.ParseMetaExpress();
            }
        }
    }
}
