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

        public void AddMetaMemberVariable(MetaMemberVariable mv )
        {
            metaMemeberVariableList.Add(mv);
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
