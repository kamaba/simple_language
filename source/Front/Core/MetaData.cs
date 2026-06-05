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
        public List<MetaMemberFunction> staticMetaMemberFunctionList => m_StaticMetaMemberFunctionList;
        public List<MetaMemberFunction> nonStaticVirtualMetaMemberFunctionList => m_NonStaticVirtualMetaMemberFunctionList;
        public List<MetaMemberFunction> fileCollectMetaMemberFunctionList => m_FileCollectMetaMemberFunctionList;



        private bool m_IsConst = false;
        private bool m_IsStatic = false;
        private bool m_IsDynamic = false;
        private EClassDefineType m_ClassDefineType = EClassDefineType.InnerDefine;
        private FileMetaClass m_FileMetaClass = null;
        private Dictionary<string, MetaMemberData> m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();
        private MetaClass m_ExtendClass = CoreMetaClassManager.dataMetaClass;
        private List<MetaMemberFunction> m_FileCollectMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        private List<MetaMemberFunction> m_NonStaticVirtualMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        private List<MetaMemberFunction> m_StaticMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected Dictionary<string, MetaMemberFunctionTemplateNode> m_MetaMemberFunctionTemplateNodeDict = new Dictionary<string, MetaMemberFunctionTemplateNode>();

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
                MetaMemberData mmv = new MetaMemberData(this, v, this, i,  false );
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberData( mmv );
                MetaVariableManager.instance.AddMetaDataVariable(mmv);
            }
        }
        public void HandleExtendContent()
        {
            HandleExtendMemberVariable();
            HandleExtendMemberFunction();
        }
        public void HandleExtendMemberVariable()
        {
            foreach (var v in m_ExtendClass.metaExtendMemeberVariableDict )
            {
                var c = v.Value;
                if (this.m_MetaMemberDataDict.ContainsKey(c.name))
                {
                    var ld = Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 继承的类123:{m_AllName} 在继承的父类{m_ExtendClass?.allName} 中已包含:{c.name} ");
                    //ld.valDict.Add(EMetaType.MetaClass, this);
                    //ld.valDict.Add(EMetaType.MetaExtendsClass, m_ExtendClass);
                    //ld.valDict.Add(EMetaType.MetaMemberVariable, c);
                    continue;
                }
                //this.m_MetaMemberDataDict.Add(c.name, c);
            }            
        }
        public void HandleExtendMemberFunction()
        {
            bool canAdd = false;
            foreach (var v in this.m_ExtendClass.nonStaticVirtualMetaMemberFunctionList)
            {
                canAdd = true;
                var efun = v;
                //if (efun.isConstructInitFunction) { continue; }
                m_NonStaticVirtualMetaMemberFunctionList.Add(efun);
            }

            foreach (var v2 in this.m_FileCollectMetaMemberFunctionList)
            {
                if (v2.isStatic)
                {
                    var find = m_StaticMetaMemberFunctionList.Find(a => a == v2);
                    if (find != null) continue;

                    m_StaticMetaMemberFunctionList.Add(v2);
                }
                else
                {
                    var find = m_NonStaticVirtualMetaMemberFunctionList.Find(a => a == v2);
                    if (find != null) continue;

                    if (v2.isOverrideFunction && v2.overrideMetaMemberFunction != null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, find.token, "有override标记，但没有父类 ");
                    }
                    m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                }
            }

            foreach (var v2 in m_NonStaticVirtualMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }
            foreach (var v2 in m_StaticMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }
        }
        public bool AddMetaMemberFunction(MetaMemberFunction mmf)
        {
            MetaMemberFunctionTemplateNode find = null;
            if (this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(mmf.name))
            {
                find = m_MetaMemberFunctionTemplateNodeDict[mmf.name];
            }
            else
            {
                find = new MetaMemberFunctionTemplateNode();
                m_MetaMemberFunctionTemplateNodeDict.Add(mmf.name, find);
            }
            if (find.AddMetaMemberFunction(mmf))
            {
                //m_CurrentClassMetaMemberFunctionList.Add(mmf);
                //m_AllMetaMemberFunctionList.Add(mmf);
                return true;
            }
            return false;
        }
        public void ParseDefineComplete()
        {
            // 嵌套 data/array 字面量已在 MetaMemberData 表达式管线（MetaAnonDataExpressNode / MetaArrayExpressNode → MetaNewObjectExpressNode）中解析。
        }
        public void UpdateAllName()
        {
            m_AllName = m_MetaNode?.GetAllName() ?? m_Name;
             foreach (var v in m_MetaMemberDataDict)
            {
                v.Value.UpdateAllName();
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
