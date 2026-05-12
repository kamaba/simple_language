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
            var snapshot = metaMemberDataVariableList.ToArray();
            foreach (var v in snapshot)
            {
                v.ParseMetaExpress();
            }

            // 嵌套 const / 匿名 {} / 数组元素未进入 metaMemberDataVariableList，需在 ParseExpress 之后按树后序补全匿名 MetaData 与 NewObject。
            foreach (var md in ClassManager.instance.EnumerateDefineMetaData())
            {
                var roots = new List<MetaMemberData>();
                foreach (var kv in md.metaMemberDataDict)
                {
                    roots.Add(kv.Value);
                }
                roots.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));
                for (int i = 0; i < roots.Count; i++)
                {
                    MetaMemberData.ResolveAnonymousDataHierarchyPostOrder(roots[i]);
                }
            }
        }
        public void ParseMetaEnumMemberExpress()
        {
            var snapshot = metaMemberEnumVariableList.ToArray();
            foreach (var v in snapshot)
            {
                v.ParseMetaExpress();
            }
        }
    }
}
