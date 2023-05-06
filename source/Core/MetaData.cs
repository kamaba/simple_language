using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaData : MetaClass
    {
        public bool isConst => m_IsConst;
        public MetaVariable metaVariable => m_MetaVariable;
        public Dictionary<string, MetaMemberData> metaMemberDataDict => m_MetaMemberDataDict;

        protected bool m_IsConst = false;

        protected string m_VariableName = "";
        protected MetaVariable m_MetaVariable = null;
        protected Dictionary<string, MetaMemberData > m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();

        public MetaData(string _name, bool isConst ) : base( _name )
        {
            m_VariableName = _name;
            m_Type = EType.Data;
            m_IsConst = isConst;
        }

        public override MetaBase GetChildrenMetaBaseByName(string name)
        {
            return base.GetChildrenMetaBaseByName(name);
        }
        public MetaMemberData GetMemberDataByName( string name )
        {
            if(m_MetaMemberDataDict.ContainsKey( name ) )
            {
                return m_MetaMemberDataDict[name];
            }
            return null;
        }
        public void AddMetaMemberData( MetaMemberData mmd )
        {
            if (m_MetaMemberDataDict.ContainsKey(mmd.name))
            {
                return;
            }
            m_MetaMemberDataDict.Add(mmd.name, mmd);
            AddMetaBase(mmd.name, mmd);
        }
        public void CreateMetaVariable()
        {
            m_MetaVariable = new MetaVariable(m_Name, null, null, new MetaType(this));

            MetaVariableManager.instance.AddMetaDataVariable(m_MetaVariable);
        }
        public virtual void ParseFileMetaDataMemeberData(FileMetaClass fmc)
        {
            bool isHave = false;
            for ( int i = 0; i < fmc.memberDataList.Count; i++ )
            {
                var v = fmc.memberDataList[i];
                MetaBase mb = GetChildrenMetaBaseByName(v.name );
                if (mb != null)
                {
                    Console.WriteLine("Error 已有定义类: " + allName + "中 已有: " + v.token?.ToLexemeAllString() + "的元素!!");
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberData mmv = new MetaMemberData( this,v );
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberData(mmv);

                mmv.ParseChildMemberData();
            }

            if( fmc.memberVariableList.Count > 0 ||  fmc.memberFunctionList.Count > 0 )
            {
                Console.WriteLine("Error Data中不允许有Variable 和 Function!!");
            }
        }
        public override void ParseDefineComplete()
        {
            base.ParseDefineComplete();

            CreateMetaVariable();
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            if( isConst )
            {
                stringBuilder.Append("const ");
            }
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
