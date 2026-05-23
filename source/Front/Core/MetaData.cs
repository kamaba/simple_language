using SimpleLanguage.Compile;

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaData : MetaBase
    {
        /// <summary>源码绑定（用于 IR 导出路径等），可能为 null（如纯运行时匿名 data）。</summary>
        public FileMetaClass boundFileMetaClass => m_FileMetaClass;
        public string allName => string.IsNullOrEmpty(m_AllName) ? (m_MetaNode?.GetAllName() ?? m_Name) : m_AllName;
        public bool isConst => m_IsConst;
        public bool isStatic => m_IsStatic;
        public bool isDynamic=>m_IsDynamic;
        public Dictionary<string, MetaMemberData> metaMemberDataDict => m_MetaMemberDataDict;



        private bool m_IsConst = false;
        private bool m_IsStatic = false;
        private bool m_IsDynamic = false;
        private EClassDefineType m_ClassDefineType = EClassDefineType.InnerDefine;
        private FileMetaClass m_FileMetaClass = null;
        private Dictionary<string, MetaMemberData> m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();

        public MetaData( FileMetaClass md )
        {
            m_Name = md.name;
            m_Type = EType.Data;
            m_IsConst =  md.isConst;
            m_IsStatic = md.isStatic;
            m_IsDynamic = false;
            m_Token = md.token;
            AddPingToken(md?.token);
        }
        public MetaData(string _name, bool constToken, bool staticToken, bool dynamic ) : base()
        {
            m_Name = _name;
            m_Type = EType.Data;
            m_IsConst = constToken;
            m_IsStatic = staticToken;
            m_IsDynamic = dynamic;
        }
        public void SetAllName( string an )
        {
            this.m_AllName = an;
        }
        public void SetClassDefineType(EClassDefineType type)
        {
            m_ClassDefineType = type;
        }
        public MetaVariable GetMetaMemberVariableByName(string name)
        {
            if (m_MetaMemberDataDict.TryGetValue(name, out var mmd))
            {
                return mmd;
            }
            return null;
        }
        /// <summary>兼容注入路径：将 <see cref="MetaMemberVariable"/> 转为 <see cref="MetaMemberData"/> 写入 <see cref="m_MetaMemberDataDict"/>。</summary>
        public MetaMemberData AddMetaMemberVariable(MetaMemberVariable mmv )
        {
            if (mmv == null)
            {
                return null;
            }
            if (m_MetaMemberDataDict.ContainsKey(mmv.name))
            {
                return null;
            }
            var mmd = MetaMemberData.CreateFromInjectedMemberVariable(this, mmv, m_MetaMemberDataDict.Count);
            AddMetaMemberData(mmd);
            return mmd;
        }
        public override void SetDeep(int deep)
        {
            this.m_Deep = deep;
            foreach (var v in m_MetaMemberDataDict)
            {
                v.Value.SetDeep(deep + 1);
            }
        }
        public MetaMemberData GetMemberDataByName(string name)
        {
            if (m_MetaMemberDataDict.ContainsKey(name))
            {
                return m_MetaMemberDataDict[name];
            }
            return null;
        }
        public void AddMetaMemberData(MetaMemberData mmd )
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
        public void ParseFileMetaDataMemeberData(FileMetaClass fmc)
        {
            m_FileMetaClass = fmc;
            if (fmc.memberVariableList.Count > 0 || fmc.memberFunctionList.Count > 0)
            {
                Log.AddMetaCoreLog(LID.MetaCoreDataNotAllowHasFunction, "Error Data中不允许有Variable 和 Function!!");
            }

            bool isHave = false;
            for (int i = 0; i < fmc.memberDataList.Count; i++)
            {
                var v = fmc.memberDataList[i];
                MetaNode mb = m_MetaNode.GetChildrenMetaNodeByName(v.name);
                if (mb != null)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, v.token, "", v.token, mb.name );
                    isHave = true;
                }
                else
                    isHave = false;
                if( v.isWithName == false )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, v.token, "这里需要个名字的定义!");
                    continue;
                }
                MetaMemberData mmv = new MetaMemberData(this, v, i, false );
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberData( mmv );
                MetaVariableManager.instance.AddMetaDataVariable(mmv);
            }
        }
        public void ParseDefineComplete()
        {
            // 嵌套 data/array 字面量已在 MetaMemberData 表达式管线（MetaAnonDataExpressNode / MetaArrayExpressNode → MetaNewObjectExpressNode）中解析。
        }

        /// <summary>
        /// 按字段最终类型生成匿名 <see cref="MetaData"/> 形状，并与全局匿名表去重。
        /// </summary>
        public static MetaData ResolveCanonicalAnonymousType(
            IEnumerable<MetaMemberData> sourceFields,
            MetaBase owner,
            string nameHint = null)
        {
            if (sourceFields == null)
            {
                return null;
            }

            var ordered = new List<MetaMemberData>(sourceFields);
            if (ordered.Count == 0)
            {
                return null;
            }
            ordered.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));

            string hint = string.IsNullOrEmpty(nameHint) ? "DynamicData" : nameHint;
            var tempMetaData = new MetaData(hint + "_" + ordered[0].GetHashCode(), false, false, true);

            int index = 0;
            foreach (var field in ordered)
            {
                var childType = field.GetFinalMetaType() ?? field.defineMetaType
                    ?? new MetaType(CoreMetaClassManager.objectMetaClass);
                //var clone = MetaMemberData.CreateDeclared(
                //    tempMetaData,
                //    field.name,
                //    index,
                //    childType,
                //    field.isDefineMetaType || childType.metaClass != CoreMetaClassManager.objectMetaClass);
                //clone.SetExpress(field.expressNode);
                
                tempMetaData.AddMetaMemberData(field);
                index++;
            }

            var matched = ClassManager.instance.FindMetaDataByNameAndType(tempMetaData);
            if (matched == null)
            {
                ClassManager.instance.AddAnonymousMetaData(tempMetaData);
                return tempMetaData;
            }
            return matched;
        }
        public void UpdateAllName()
        {
            m_AllName = m_MetaNode?.GetAllName() ?? m_Name;
             foreach (var v in m_MetaMemberDataDict)
            {
                //v.Value.UpdateAllName();
            }
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
                stringBuilder.Append(allName + " = {");
                int index = 0;
                foreach (var v in m_MetaMemberDataDict)
                {
                    stringBuilder.Append(v.Value.ToFormatString(true));
                    if (index < m_MetaMemberDataDict.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                    index++;
                }
                stringBuilder.Append("}");
            }
            else
            {
                for (int i = 0; i < realDeep; i++)
                    stringBuilder.Append(Global.tabChar);
                if (isConst)
                {
                    stringBuilder.Append("const ");
                }
                stringBuilder.Append("data ");
                stringBuilder.Append(allName);
                stringBuilder.Append(Environment.NewLine);

                for (int i = 0; i < realDeep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("{" + Environment.NewLine);

                foreach (var v in m_MetaMemberDataDict)
                {
                    stringBuilder.Append(v.Value.ToFormatString(false));
                    stringBuilder.Append(Environment.NewLine);
                }

                for (int i = 0; i < realDeep; i++)
                    stringBuilder.Append(Global.tabChar);
                stringBuilder.Append("}" + Environment.NewLine);
            }

            return stringBuilder.ToString();
        }
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();

            if (m_IsDynamic)
            {
                if (isConst)
                {
                    stringBuilder.Append("const ");
                }
                stringBuilder.Append(allName + " = {");
                int index = 0;
                foreach (var v in m_MetaMemberDataDict)
                {
                    stringBuilder.Append(v.Value.ToString());
                    if (index < m_MetaMemberDataDict.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                    index++;
                }
                stringBuilder.Append("}");
            }
            else
            {
                if (isConst)
                {
                    stringBuilder.Append("const ");
                }
                stringBuilder.Append("data ");
                stringBuilder.Append(allName);
                stringBuilder.Append("{");

                int i = 0;
                foreach (var v in m_MetaMemberDataDict)
                {
                    stringBuilder.Append(v.Value.ToString());
                    if( i++ < m_MetaMemberDataDict.Count - 1 )
                    {
                        stringBuilder.Append(",");
                    }
                }

                stringBuilder.Append("}" );
            }

            return stringBuilder.ToString();
        }
    }
}
