using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaEnum : MetaClass
    {
        public bool isConst => m_IsConst;
        public MetaVariable metaVariable => m_MetaVariable;

        protected bool m_IsConst = false;
        protected MetaVariable m_MetaVariable = null;
        public MetaEnum(string _name, bool isConst ) : base(_name)
        {
            m_Type = EType.Enum;
            m_IsConst = isConst;
        }
        public void CreateMetaVariable()
        {
            m_MetaVariable = new MetaVariable(m_Name, null, null, new MetaType(this) );

            MetaVariableManager.instance.AddMetaEnumVariable(m_MetaVariable);
        }
        public override MetaBase GetChildrenMetaBaseByName(string name)
        {
            return base.GetChildrenMetaBaseByName(name);
        }
        public MetaMemberVariable GetMemberVariableByName( string name )
        {
            if( m_MetaMemberVariableDict.ContainsKey( name ) )
            {
                return m_MetaMemberVariableDict[name];
            }
            return null;
        }
        public void ParseFileMetaEnumMemeberData(FileMetaClass fmc)
        {
            for (int i = 0; i < fmc.templateParamList.Count; i++)
            {
                string tTemplateName = fmc.templateParamList[i].name;
                if (m_MetaTemplateList.Find(a => a.name == tTemplateName) != null)
                {
                    Console.WriteLine("Error 定义模式名称重复!!");
                }
                else
                {
                    m_MetaTemplateList.Add(new MetaTemplate(this, fmc.templateParamList[i]));
                }
            }

            bool isHave = false;
            foreach (var v in fmc.memberVariableList)
            {
                MetaBase mb = GetChildrenMetaBaseByName(v.name);
                if (mb != null)
                {
                    Console.WriteLine("Error 已有定义类: " + allName + "中 已有: " + v.token?.ToLexemeAllString() + "的元素!!");
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberVariable mmv = new MetaMemberVariable(this, v, true);
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberVariable(mmv);
            }

            if( fmc.memberFunctionList.Count > 0 )
            {
                Console.WriteLine("Error Enum中不允许有Function!!");
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
            stringBuilder.Append(permission.ToFormatString());
            stringBuilder.Append(" ");
            if( isConst )
            {
                stringBuilder.Append("const ");
            }
            stringBuilder.Append("enum ");
            if (topLevelMetaNamespace != null)
            {
                stringBuilder.Append(topLevelMetaNamespace.allName + ".");
            }
            stringBuilder.Append(name);
            if (m_MetaTemplateList.Count > 0)
            {
                stringBuilder.Append("<");
                for (int i = 0; i < m_MetaTemplateList.Count; i++)
                {
                    stringBuilder.Append(m_MetaTemplateList[i].ToFormatString());
                    if (i < m_MetaTemplateList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append(">");
            }

            stringBuilder.Append(Environment.NewLine);
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v in childrenNameNodeDict)
            {
                MetaBase mb = v.Value;
                if (mb is MetaEnum )
                {
                    stringBuilder.Append((mb as MetaEnum).ToFormatString());
                    stringBuilder.Append(Environment.NewLine);
                }
                else if (mb is MetaMemberVariable)
                {
                    MetaMemberVariable mmv = mb as MetaMemberVariable;
                    if (mmv.fromType == EFromType.Code)
                    {
                        stringBuilder.Append(mmv.ToFormatString());
                        stringBuilder.Append(Environment.NewLine);
                    }
                }
                else
                {
                    stringBuilder.Append("Errrrrroooorrr ---" + mb.ToFormatString());
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
