using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaData : MetaClass
    {
        public bool isConst => m_IsConst;
        public bool isStatic => m_IsStatic;
        public bool isDynamic=>m_IsDynamic;

        public Dictionary<string, MetaMemberData> metaMemberDataDict => m_MetaMemberDataDict;

        protected bool m_IsConst = false;
        protected bool m_IsStatic = false;
        protected bool m_IsDynamic = false;

        protected Dictionary<string, MetaMemberData> m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();
        protected List<Token> m_PingTokensList = new List<Token>();


        public MetaData( FileMetaClass md )
        {
            m_Name = md.name;
            m_AllName = md.name;
            m_Type = EType.Data;
            m_IsConst =  md.isConst;
            m_IsStatic = md.isStatic;
            m_IsDynamic = false;
        }
        public MetaData(string _name, bool constToken, bool staticToken, bool dynamic ) : base(_name)
        {
            m_Name = _name;
            m_AllName = _name;
            m_Type = EType.Data;
            m_IsConst = constToken;
            m_IsStatic = staticToken;
            m_IsDynamic = dynamic;
        }
        public void AddPingToken(Token tok)
        {
            m_PingTokensList.Add(tok);
        }
        public MetaMemberData GetMemberDataByName(string name)
        {
            if (m_MetaMemberDataDict.ContainsKey(name))
            {
                return m_MetaMemberDataDict[name];
            }
            return null;
        }
        public void AddMetaMemberData(MetaMemberData mmd)
        {
            if (m_MetaMemberDataDict.ContainsKey(mmd.name))
            {
                return;
            }
            m_MetaMemberDataDict.Add(mmd.name, mmd);
        }
        public List<MetaMemberData> GetMetaMemberDataList()
        {
            List < MetaMemberData > list = new List<MetaMemberData> ();
            foreach ( var v in m_MetaMemberDataDict )
            {
                list.Add(v.Value);
            }
            return list;
        }
        //public void CreateMetaVariable()
        //{
        //    var m_MetaVariable = new MetaVariable(m_Name, MetaVariable.EVariableFrom.Member, null, null, new MetaType(this));

        //    MetaVariableManager.instance.AddMetaDataVariable(m_MetaVariable);
        //}
        public virtual void ParseFileMetaDataMemeberData(FileMetaClass fmc)
        {
            bool isHave = false;
            for (int i = 0; i < fmc.memberDataList.Count; i++)
            {
                var v = fmc.memberDataList[i];
                MetaNode mb = m_MetaNode.GetChildrenMetaNodeByName(v.name);
                if (mb != null)
                {
                    Log.AddInStructMeta(EError.None, "Error MetaData MetaDataMember已有定义类: " + m_AllName + "中 已有: " + v.token?.ToLexemeAllString() + "的元素!!");
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberData mmv = new MetaMemberData(this, v, i, isStatic);
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                mmv.ParseDefineMetaType();
                AddMetaMemberData(mmv);

                mmv.ParseChildMemberData();
            }

            if (fmc.memberVariableList.Count > 0 || fmc.memberFunctionList.Count > 0)
            {
                Log.AddInStructMeta(EError.None, "Error Data中不允许有Variable 和 Function!!");
            }
        }
        public override void ParseDefineComplete()
        {
            base.ParseDefineComplete();
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();

            if( m_IsDynamic )
            {
                if (isConst)
                {
                    stringBuilder.Append("const ");
                }
                //if (topLevelMetaNamespace != null)
                //{
                //    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
                //}
                //stringBuilder.Append(name + " = {");
                //foreach (var v in m_ChildrenNameNodeDict)
                //{
                //    MetaBase mb = v.Value;
                //    if (mb is MetaMemberData)
                //    {
                //        stringBuilder.Append((mb as MetaMemberData).ToFormatString());
                //        stringBuilder.Append(",");
                //    }
                //}
                stringBuilder.Append("}");
            }
            else
            {
                //for (int i = 0; i < realDeep; i++)
                //    stringBuilder.Append(Global.tabChar);
                //if (isConst)
                //{
                //    stringBuilder.Append("const ");
                //}
                //if (topLevelMetaNamespace != null)
                //{
                //    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
                //}
                //stringBuilder.Append(name + " ");

                //stringBuilder.Append(Environment.NewLine);
                //for (int i = 0; i < realDeep; i++)
                //    stringBuilder.Append(Global.tabChar);
                //stringBuilder.Append("{" + Environment.NewLine);

                //foreach (var v in m_ChildrenNameNodeDict)
                //{
                //    MetaBase mb = v.Value;
                //    if (mb is MetaMemberData)
                //    {
                //        stringBuilder.Append((mb as MetaMemberData).ToFormatString2(m_IsDynamic));
                //        stringBuilder.Append(Environment.NewLine);
                //    }
                //}

                //for (int i = 0; i < realDeep; i++)
                //    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("}" + Environment.NewLine);
            }

            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
