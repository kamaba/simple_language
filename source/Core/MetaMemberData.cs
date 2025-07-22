//****************************************************************************
//  File:      MetaMemberData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/6 12:00:00
//  Description: class's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum EMemberDataType
    {
        None,
        ConstValue,
        MemberData,
        MemberArray,
        MemberClass,
    }
    public sealed class MetaMemberData : MetaVariable
    {
        public override bool isConst { get { return m_IsConst; } }
        public EMemberDataType memberDataType => m_MemberDataType;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaExpressNode expressNode => m_Express;
        public Dictionary<string, MetaMemberData> metaMemberDataDict => m_MetaMemberDataDict;

        private MetaExpressNode m_Express = null;
        private MetaVariable m_MetaVariable = null;
        private EMemberDataType m_MemberDataType = EMemberDataType.None;
        private int m_Index = -1;
        private bool m_End = false;
        private bool m_IsWithName = false;

        private Dictionary<string, MetaMemberData> m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();

        private FileMetaMemberData m_FileMetaMemeberData = null;
        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;

        public MetaMemberData(MetaData mc, FileMetaOpAssignSyntax fmoa)
        {
            m_FileMetaOpAssignSyntax = fmoa;
            m_DefineMetaType = new MetaType(mc);
            SetOwnerMetaClass(mc);
            m_IsConst = mc.isConst;
            ParseName();
        }
        public MetaMemberData(MetaData mc, FileMetaMemberData fmmd, int index, bool isStatic )
        {
            m_FileMetaMemeberData = fmmd;
            fmmd.SetMetaMemberData(this);
            m_Name = fmmd.name;
            m_Index = index;
            m_IsStatic = isStatic;
            m_IsWithName = m_FileMetaMemeberData.isWithName;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(mc);
            m_IsConst = mc.isConst;
        }
        public MetaMemberData(MetaMemberData parentNode, FileMetaMemberData fmmd, int _index, bool isEnd = false)
        {
            m_Index = _index;
            m_End = isEnd;
            m_FileMetaMemeberData = fmmd;
            fmmd.SetMetaMemberData(this);
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(parentNode.ownerMetaClass);
            m_IsConst = parentNode.isConst;

            ParseName();
        }
        public MetaMemberData( MetaMemberData parentNode, string name, int _index, MetaExpressNode men )
        {
            m_Name = name;
            m_Index = _index;
            SetOwnerMetaClass(parentNode.ownerMetaClass);
            m_IsConst = parentNode.isConst;
            m_Express = men;
        }
        public void SetIndex(int index) { m_Index = index; }
        public string GetString(string name, bool isInChildren = true)
        {
            var constExpress = (m_Express as MetaConstExpressNode);
            if (constExpress != null)
            {
                return constExpress.value.ToString();
            }
            else
            {
                if (isInChildren)
                {
                    if (m_MetaMemberDataDict.ContainsKey(name))
                    {
                        return m_MetaMemberDataDict[name].GetString(name);
                    }
                }
            }
            return null;
        }
        public int GetInt(string name, int defaultValue = 0)
        {
            var constExpress = (m_Express as MetaConstExpressNode);
            if (constExpress != null)
            {
                if (constExpress.eType == EType.Int16
                    || constExpress.eType == EType.UInt16
                    || constExpress.eType == EType.Int32
                    || constExpress.eType == EType.UInt32
                    || constExpress.eType == EType.Int64
                    || constExpress.eType == EType.UInt64)
                {
                    return int.Parse(constExpress.value.ToString());
                }
            }
            return defaultValue;
        }
        public MetaMemberData GetMemberDataByName(string name)
        {
            if (m_MetaMemberDataDict.ContainsKey(name))
            {
                return m_MetaMemberDataDict[name];
            }
            return null;
        }
        public bool AddMetaMemberData(MetaMemberData mmd)
        {
            if (m_MetaMemberDataDict.ContainsKey(mmd.name))
            {
                return false;
            }
            m_MetaMemberDataDict.Add(mmd.name, mmd);

            MetaVariableManager.instance.AddMetaDataVariable(mmd);

            return true;
        }
        private void ParseName()
        {
            if (m_FileMetaMemeberData != null)
            {
                m_IsWithName = m_FileMetaMemeberData.isWithName;
                if (m_IsWithName)
                {
                    m_Name = m_FileMetaMemeberData.name;
                }
                else
                {
                    m_Name = m_Index.ToString();
                }
            }
            else if (m_FileMetaOpAssignSyntax != null)
            {
                m_Name = m_FileMetaOpAssignSyntax.variableRef.name;
            }
        }
        public override void ParseDefineMetaType()
        {
            if (m_FileMetaMemeberData != null)
            {
                switch (m_FileMetaMemeberData.DataType)
                {
                    case FileMetaMemberData.EMemberDataType.Data:
                        {
                            m_MemberDataType = EMemberDataType.MemberData;
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.Class:    // data Data{ $childData = Class1{}$ }
                        {
                            m_MemberDataType = EMemberDataType.MemberClass;
                            m_Express = new MetaCallLinkExpressNode(m_FileMetaMemeberData.fileMetaCallTermValue.callLink, null, null, null);
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.Array:      // data Data{ $childArray = [  ]$ }
                        {
                            m_MemberDataType = EMemberDataType.MemberArray;
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.ConstValue:  // data Data{ $childArray = { a = 1; b = 2 }$ }
                        {
                            m_MemberDataType = EMemberDataType.ConstValue;
                            if(m_FileMetaMemeberData.fileMetaConstValue != null )
                                m_Express = new MetaConstExpressNode(m_FileMetaMemeberData.fileMetaConstValue);
                        }
                        break;
                }
            }
            else if (m_FileMetaOpAssignSyntax != null)
            {
                if(m_FileMetaOpAssignSyntax.variableRef != null )
                {
                    if( m_FileMetaOpAssignSyntax.variableRef.isOnlyName )
                    {
                        m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                    }

                    FileMetaBaseTerm curFMBT = m_FileMetaOpAssignSyntax?.express;
                    var fme = m_FileMetaOpAssignSyntax?.express;

                    CreateExpressParam cep = new CreateExpressParam()
                    {
                        fme = curFMBT,
                        metaType = m_DefineMetaType,
                        equalMetaVariable = this,
                        ownerMBS = m_OwnerMetaBlockStatements,
                        parsefrom = EParseFrom.StatementRightExpress
                    };
                    m_Express = ExpressManager.CreateExpressNode(cep);
                    if (m_Express == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error 没有解析到Express的内容 在MetaMemberData 里边 372");
                    }
                }
            }
        }
        public override bool ParseMetaExpress()
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                m_Express.CalcReturnType();
                m_DefineMetaType = m_Express.GetReturnMetaDefineType();
                if (m_DefineMetaType == null)
                {
                    Log.AddInStructMeta(EError.None, "Error 在生成Data时，没有找到." + m_FileMetaMemeberData.fileMetaCallTermValue.ToTokenString());
                    return false;
                }
                if (m_DefineMetaType.isData)
                {
                    m_MemberDataType = EMemberDataType.MemberData;
                }
                else if (m_DefineMetaType.isArray)
                {
                    m_MemberDataType = EMemberDataType.MemberArray;
                }
                else if (m_Express is MetaConstExpressNode)
                {
                    m_MemberDataType = EMemberDataType.ConstValue;
                }
                else
                {
                    m_MemberDataType = EMemberDataType.MemberClass;

                    switch (m_Express)
                    {
                        case MetaCallLinkExpressNode mcen:
                            {
                                m_MetaVariable = mcen.GetMetaVariable();
                                break;
                            }
                    }
                }
            }
            return true;
        }
        public MetaMemberData Copy()
        {
            //var newMMD = new MetaMemberData(m_OwnerMetaClass as MetaData,  );
            //newMMD.m_Name = m_Name;
            //newMMD.m_MemberDataType = m_MemberDataType;
            //if (m_MemberDataType == EMemberDataType.MemberData)
            //{
            //    foreach (var v in m_MetaMemberDataDict)
            //    {
            //        newMMD.AddMetaMemberData(v.Value.Copy());
            //    }
            //}
            //else if (m_MemberDataType == EMemberDataType.ConstValue)
            //{
            //    newMMD.m_Express = m_Express;
            //}
            //return newMMD;
            return null;
        }
        public void CopyByMetaData(MetaData md)
        {
            MetaData curMD = m_OwnerMetaClass as MetaData;
            foreach (var v in md.metaMemberDataDict)
            {
                if (v.Value.IsIncludeMetaData(curMD))
                {
                    Log.AddInStructMeta(EError.None, "Error 当前有循环引用数量现象，请查正!!" + md.allClassName );
                    continue;
                }
                var newMMD = v.Value.Copy();
                this.AddMetaMemberData(newMMD);
            }
        }
        public bool IsIncludeMetaData(MetaData md)
        {
            if (md == null) return false;

            MetaData belongMD = m_OwnerMetaClass as MetaData;
            if (belongMD != null)
            {
                if (belongMD == md)
                {
                    return true;
                }
            }

            return false;
        }
        public void ParseChildMemberData()
        {
            if (m_FileMetaMemeberData != null)
            {
                int count = m_FileMetaMemeberData.fileMetaMemberData.Count;
                for (int i = 0; i < count; i++)
                {
                    MetaMemberData mmd = new MetaMemberData(this, m_FileMetaMemeberData.fileMetaMemberData[i], i, i == count - 1);

                    mmd.ParseName();
                    mmd.ParseDefineMetaType();
                    mmd.ParseMetaExpress();

                    if (AddMetaMemberData(mmd))
                    {
                        //this.AddMetaBase(mmd.name, mmd);

                        mmd.ParseChildMemberData();
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "Error ParseChildMemberData 命名有重名!!" + mmd.name );
                    }
                }
            }
            else if(m_Express != null )
            {
                var mne = m_Express as MetaNewObjectExpressNode;
                var cne = m_Express as MetaCallLinkExpressNode;
                if ( mne != null )
                {
                    for (int i = 0; i < mne.metaBraceOrBracketStatementsContent?.assignStatementsList?.Count; i++)
                    {
                        var asl = mne.metaBraceOrBracketStatementsContent.assignStatementsList[i];

                        if (asl == null) continue;

                        MetaMemberData addMmd = null;
                        if (m_MemberDataType == EMemberDataType.MemberArray)
                        {
                            var mcen = asl.expressNode as MetaConstExpressNode;
                            var mnoe = asl.expressNode as MetaNewObjectExpressNode;
                            if ( mcen != null )
                            {
                                addMmd = new MetaMemberData(this, i.ToString(), i, mcen);
                                addMmd.ParseMetaExpress();
                            }
                            if( mnoe != null )
                            {
                                addMmd = new MetaMemberData(this, i.ToString(), i, mnoe);
                                addMmd.ParseMetaExpress();
                            }
                        }
                        else if (m_MemberDataType == EMemberDataType.MemberData)
                        {
                            addMmd = asl.metaMemberData;
                        }

                        if (addMmd == null) continue;

                        if (m_MetaMemberDataDict.ContainsKey(addMmd.name))
                        {
                            Log.AddInStructMeta(EError.None, "Error 重复的MetaMemberData的名称 在484");
                            continue;
                        }
                        m_MetaMemberDataDict.Add(addMmd.name, addMmd);
                    }
                }
                else if(cne != null )
                {
                    MetaMemberData addMmd = new MetaMemberData( this, name, 0, cne );
                    if (m_MetaMemberDataDict.ContainsKey(addMmd.name))
                    {
                        Log.AddInStructMeta(EError.None, "Error 重复的MetaMemberData的名称 在410");
                    }
                    m_MetaMemberDataDict.Add(addMmd.name, addMmd);
                }
            }
        }
        public string ToFormatString2( bool isDynamic )
        {
            StringBuilder sb = new StringBuilder();
            switch (this.m_MemberDataType)
            {
                case EMemberDataType.MemberData:
                    {
                        if (isDynamic)
                        {
                            sb.Append(m_Name);
                            sb.Append("{");
                            foreach (var v in m_MetaMemberDataDict)
                            {
                                sb.Append(v.Value.ToFormatString2(isDynamic));
                            }
                            sb.Append("}");
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.AppendLine(m_Name);
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.AppendLine("{");
                            foreach (var v in m_MetaMemberDataDict)
                            {
                                sb.AppendLine(v.Value.ToFormatString());
                            }
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.Append("}");
                        }

                    }
                    break;
                case EMemberDataType.MemberClass:
                    {
                        if (isDynamic)
                        {
                            sb.Append(m_Name);
                            sb.Append(" = ");
                            sb.Append(m_Express.ToFormatString());
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.Append(m_Name + " = " );
                            sb.Append(m_Express.ToFormatString());
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                        }
                    }
                    break;
                case EMemberDataType.MemberArray:
                    {
                        if (isDynamic)
                        {
                            sb.Append(m_Name);
                            sb.Append("[");
                            foreach (var v in m_MetaMemberDataDict)
                            {
                                sb.Append(v.Value.ToFormatString2(isDynamic));
                            }
                            sb.Append("]");
                        }
                        else
                        {

                            int i = 0;
                            for (i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.Append(m_Name + " = [");
                            i = 0;
                            foreach (var v in m_MetaMemberDataDict)
                            {
                                sb.Append(v.Value.ToFormatString());
                                if (i < m_MetaMemberDataDict.Count - 1)
                                    sb.Append(",");
                                i++;
                            }
                            sb.Append("]");
                        }
                    }
                    break;
                case EMemberDataType.ConstValue:
                    {
                        if (isDynamic)
                        {
                            sb.Append(m_Express.ToFormatString());
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.Append(m_Express.ToFormatString());
                        }                 
                    }
                    break;
                default:
                    {
                        Log.AddInStructMeta(EError.None, "error 暂不支持其它类型 1");
                    }
                    break;
            }
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if ( m_IsWithName )
            {
                sb.Append(m_Name);
                sb.Append(" = ");
            }
            switch (m_MemberDataType)
            {
                case EMemberDataType.MemberData:
                    {
                        sb.AppendLine("{");
                        foreach (var v in m_MetaMemberDataDict)
                        {
                            sb.AppendLine(v.Value.ToFormatString());
                        }
                        sb.AppendLine("}");
                    }
                    break;
                case EMemberDataType.MemberClass:
                    {
                        sb.Append(m_Express.ToFormatString());
                    }
                    break;
                case EMemberDataType.MemberArray:
                    {
                        int i = 0;
                        sb.Append("[");
                        i = 0;
                        foreach (var v in m_MetaMemberDataDict)
                        {
                            sb.Append(v.Value.ToFormatString());
                            if (i < m_MetaMemberDataDict.Count - 1)
                                sb.Append(",");
                            i++;
                        }
                        sb.Append("]");
                    }
                    break;
                case EMemberDataType.ConstValue:
                    {
                        sb.Append(m_Express.ToFormatString());
                    }
                    break;
                default:
                    {
                        Log.AddInStructMeta(EError.None, "error 暂不支持其它类型 1");
                    }
                    break;
            }
            return sb.ToString();
        }
    }
}
