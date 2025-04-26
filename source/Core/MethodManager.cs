using SimpleLanguage.IR;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.VM.Runtime;
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

        public Dictionary<int, MetaMemberFunction> metaMemberFunctionDict = new Dictionary<int, MetaMemberFunction>();
        public Dictionary<int, MetaMemberFunction> dynamicMetaMemberFunctionDict = new Dictionary<int, MetaMemberFunction>();


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
        public void AddMemeberFunction( MetaMemberFunction mmf )
        {
            if(metaMemberFunctionDict.ContainsKey( mmf.GetHashCode() ) )
            {
                return;
            }
            metaMemberFunctionDict.Add(mmf.GetHashCode(), mmf);
        }
        public void AddDynamicMemberFunction( MetaMemberFunction mmf )
        {
            if (dynamicMetaMemberFunctionDict.ContainsKey(mmf.GetHashCode()))
            {
                return;
            }
            dynamicMetaMemberFunctionDict.Add(mmf.GetHashCode(), mmf);
            
        }
        public void ParseMetaExpress()
        {
            var tempDict = new Dictionary<int, MetaMemberFunction>(metaMemberFunctionDict);
            foreach (var v in tempDict)
            {
                v.Value.ParseMetaExpress();
            }
        }
        public void ParseStatements()
        {
            foreach (var v in metaMemberFunctionDict)
            {
                v.Value.ParseStatements();
            }
        }
    }
}
