using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaDynamicData : MetaClass
    {
        public MetaDynamicData(string _name ) : base( _name )
        {
            m_Type = EType.Class;
        }
        public override void ParseDefineComplete()
        {
            base.ParseDefineComplete();
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            
            if (topLevelMetaNamespace != null)
            {
                stringBuilder.Append(topLevelMetaNamespace.allName + ".");
            }
            stringBuilder.Append(name + " ");

            stringBuilder.Append(Environment.NewLine);
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v in childrenNameNodeDict)
            {
                MetaBase mb = v.Value;
                if (mb is MetaMemberData )
                {
                    stringBuilder.Append((mb as MetaMemberData).ToFormatString());
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
