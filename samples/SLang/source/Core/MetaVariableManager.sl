using System.Collections.Generic;

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
        public List<MetaMemberData> metaMemberDataVariableList = new List<MetaMemberData>();
        public List<MetaMemberEnum> metaMemberEnumVariableList = new List<MetaMemberEnum>();

        public void AddMetaMemberVariable(MetaMemberVariable mv)
        {
            metaMemeberVariableList.Add(mv);
        }
        //public void AddGlobalMetaVariable( MetaVariable mv )
        //{

        //}
        //public MetaVariable GetGlobalMetaVariable( string name )
        //{
        //    return null;
        //}
        public void AddMetaDataVariable(MetaMemberData mv)
        {
            metaMemberDataVariableList.Add(mv);
        }
        public void AddMetaEnumVariable(MetaMemberEnum mv )
        {
            metaMemberEnumVariableList.Add(mv);
        }
        public void ParseMetaClassMemberExpress()
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
                v.ParseMetaExpress();
                v.CalcReturnType();
                v.ParseChildMemberData();
            }
        }
        public void ParseMetaDataMemberExpress()
        {
            foreach (var v in metaMemberDataVariableList )
            {
                v.ParseMetaExpress();
            }
        }
        public void ParseMetaEnumMemberExpress()
        {
            foreach (var v in metaMemberEnumVariableList )
            {
                v.ParseMetaExpress();
            }
        }
    }
}
