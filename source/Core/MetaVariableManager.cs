using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaVariableManager
    {
        public static MetaVariableManager s_Instance = null;
        public static MetaVariableManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new MetaVariableManager();
                }
                return s_Instance;
            }
        }
        public List<MetaMemberVariable> metaMemeberVariableList = new List<MetaMemberVariable>();

        public Dictionary<string, MetaVariable> metaDataVariableList = new Dictionary<string, MetaVariable>();
        public Dictionary<string, MetaVariable> metaEnumVariableList = new Dictionary<string, MetaVariable>();

        public void AddMetaMemberVariable(MetaMemberVariable mv)
        {
            metaMemeberVariableList.Add(mv);
        }
        public void AddMetaDataVariable(MetaVariable mv)
        {
            metaDataVariableList.Add(mv.name, mv);
        }
        public void AddMetaEnumVariable( MetaVariable mv )
        {
            metaEnumVariableList.Add(mv.name, mv);
        }
        public MetaVariable GetMetaVariable( string cname )
        {
            if(metaDataVariableList.ContainsKey( cname ) )
            {
                return metaDataVariableList[cname];
            }
            return null;
        }
        public void ParseExpress()
        {
            foreach (var v in metaMemeberVariableList)
            {
                v.CreateExpress();
            }
            foreach (var v in metaMemeberVariableList)
            {
                v.CalcParseLevel();
            }
            metaMemeberVariableList.Sort((x, y) => x.CompareTo(y));

            foreach (var v in metaMemeberVariableList)
            {
                v.CalcReturnType();
            }
        }
    }
}
