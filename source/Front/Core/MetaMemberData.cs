//****************************************************************************
//  File:      MetaMemberData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/6 12:00:00
//  Description: class's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
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
        /// <summary>Source declaration order within the owning <see cref="MetaData"/> (used by IR field indices).</summary>
        public int dataFieldOrderIndex => m_Index;
        public override bool isConst { get { return m_IsConst; } }
        public EMemberDataType memberDataType => m_MemberDataType;
        public MetaExpressNode expressNode => m_Express;
        public Dictionary<string, MetaMemberData> metaMemberDataDict => m_MetaMemberDataDict;

        private MetaExpressNode m_Express = null;
        private EMemberDataType m_MemberDataType = EMemberDataType.None;
        private int m_Index = -1;
        private bool m_End = false;
        private bool m_IsWithName = false;

        private Dictionary<string, MetaMemberData> m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();
        private FileMetaMemberData m_FileMetaMemeberData = null;
        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;

        private MetaMemberData()
        {
            m_VariableFrom = EVariableFrom.DataMember;
        }
        //public MetaMemberData(MetaData mc, FileMetaOpAssignSyntax fmoa)
        //{
        //    m_FileMetaOpAssignSyntax = fmoa;
        //    m_DefineMetaType = new MetaType(mc);
        //    SetOwnerMetaClass(mc);
        //    m_IsConst = mc.isConst;
        //    m_VariableFrom = EVariableFrom.DataMember;
        //    m_Token = fmoa.token;
        //    ParseName();
        //}
        public MetaMemberData(MetaData mc, FileMetaMemberData fmmd, int index, bool isStatic )
        {
            m_FileMetaMemeberData = fmmd;
            m_Name = fmmd.name;
            m_Index = index;
            m_IsStatic = isStatic;
            m_IsWithName = m_FileMetaMemeberData.isWithName;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(mc);
            m_IsConst = mc.isConst;
            m_Token = fmmd.nameToken;
            m_VariableFrom = EVariableFrom.DataMember;
        }
        public MetaMemberData(MetaMemberData parentNode, FileMetaMemberData fmmd, int _index, bool isEnd = false)
        {
            m_Index = _index;
            m_End = isEnd;
            m_FileMetaMemeberData = fmmd;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(parentNode.ownerMetaClass);
            m_IsConst = parentNode.isConst;
            m_Token = fmmd.nameToken;
            m_VariableFrom = EVariableFrom.DataMember;

            ParseName();
        }
        public MetaMemberData( MetaMemberData parentNode, string name, int _index, MetaExpressNode men )
        {
            m_Name = name;
            m_Index = _index;
            SetOwnerMetaClass(parentNode.ownerMetaClass);
            m_IsConst = parentNode.isConst;
            m_Express = men;
            m_VariableFrom = EVariableFrom.DataMember;
            m_Token = men.token;
        }
        public static MetaMemberData CreateConst(MetaData owner, string name, int index, MetaConstExpressNode constExpress)
        {
            var mmd = new MetaMemberData();
            mmd.m_Name = name;
            mmd.m_Index = index;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;
            mmd.m_DefineMetaType = new MetaType(constExpress?.GetReturnMetaClass() ?? CoreMetaClassManager.objectMetaClass);
            mmd.m_RealMetaType = new MetaType(mmd.m_DefineMetaType);
            mmd.m_IsDefineMetaType = true;
            mmd.SetOwnerMetaClass(owner);
            mmd.m_IsConst = owner?.isConst ?? false;
            mmd.m_IsStatic = false;
            mmd.m_Express = constExpress;
            mmd.m_MemberDataType = EMemberDataType.ConstValue;
            mmd.AddPingToken(constExpress.token);
            return mmd;
        }

        public static MetaMemberData CreateObject(MetaData owner, string name, int index)
        {
            var mmd = new MetaMemberData();
            mmd.m_Name = name;
            mmd.m_Index = index;
            mmd.m_IsWithName = true;
            mmd.m_DefineMetaType = new MetaType(owner);
            mmd.m_RealMetaType = new MetaType(mmd.m_DefineMetaType);
            mmd.m_IsDefineMetaType = true;
            mmd.SetOwnerMetaClass(owner);
            mmd.m_IsConst = owner?.isConst ?? false;
            mmd.m_MemberDataType = EMemberDataType.MemberData;
            mmd.m_VariableFrom = EVariableFrom.DataMember;
            return mmd;
        }

        public static MetaMemberData CreateArray(MetaData owner, string name, int index, MetaType elementType = null, int length = -1)
        {
            var mmd = new MetaMemberData();
            mmd.m_Name = name;
            mmd.m_Index = index;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;

            var et = elementType ?? new MetaType(CoreMetaClassManager.objectMetaClass);
            var arrType = new MetaType(CoreMetaClassManager.arrayMetaClass, new List<MetaType>() { et });
            arrType.SetArrayLength(length);

            mmd.m_DefineMetaType = arrType;
            mmd.m_RealMetaType = new MetaType(arrType);
            mmd.m_IsDefineMetaType = true;
            mmd.SetOwnerMetaClass(owner);
            mmd.m_IsConst = owner?.isConst ?? false;
            mmd.m_MemberDataType = EMemberDataType.MemberArray;
            return mmd;
        }
        public static MetaMemberData CreateDeclared(MetaData owner, string name, int index, MetaType defineMetaType, bool isDeclaredType)
        {
            var mmd = new MetaMemberData();
            mmd.m_Name = string.IsNullOrEmpty(name) ? index.ToString() : name;
            mmd.m_Index = index;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;
            mmd.SetOwnerMetaClass(owner);
            mmd.m_IsConst = owner?.isConst ?? false;

            var finalType = defineMetaType ?? new MetaType(CoreMetaClassManager.objectMetaClass);
            mmd.m_DefineMetaType = finalType;
            mmd.m_IsDefineMetaType = isDeclaredType;
            if (isDeclaredType)
            {
                mmd.m_RealMetaType = new MetaType(finalType);
            }

            if (finalType.isData)
            {
                mmd.m_MemberDataType = EMemberDataType.MemberData;
            }
            else if (finalType.IsArray())
            {
                mmd.m_MemberDataType = EMemberDataType.MemberArray;
            }

            return mmd;
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
        private MetaType GetStructuralMetaType()
        {
            if (m_IsDefineMetaType && m_DefineMetaType != null)
            {
                return m_DefineMetaType;
            }
            if (m_RealMetaType != null)
            {
                return m_RealMetaType;
            }
            if (m_DefineMetaType != null)
            {
                return m_DefineMetaType;
            }
            return new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        private void ResolveAnonymousDataMetaType()
        {
            if (m_MetaMemberDataDict.Count == 0)
            {
                return;
            }

            var tempMetaData = new MetaData("DynamicData_" + m_Name + "_" + GetHashCode(), false, false, true);
            int index = 0;
            foreach (var entry in m_MetaMemberDataDict)
            {
                var child = entry.Value;
                var childType = child.GetStructuralMetaType();
                var clone = CreateDeclared(tempMetaData, child.name, index, childType, child.isDefineMetaType || childType.metaClass != CoreMetaClassManager.objectMetaClass);
                tempMetaData.AddMetaMemberData(clone);
                index++;
            }

            var matched = ClassManager.instance.FindMetaData(tempMetaData);
            if (matched == null)
            {
                ClassManager.instance.AddMetaData(tempMetaData);
                matched = tempMetaData;
            }

            m_DefineMetaType = new MetaType(matched);
            m_RealMetaType = new MetaType(matched);
            m_IsDefineMetaType = true;
            m_MemberDataType = EMemberDataType.MemberData;
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
                            m_Express = new MetaCallLinkExpressNode(
                                m_FileMetaMemeberData.fileMetaCallTermValue.callLink,
                                m_OwnerMetaClass,
                                m_OwnerMetaBlockStatements,
                                this);
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
                                m_Express = new MetaConstExpressNode( ownerMetaClass, null, m_FileMetaMemeberData.fileMetaConstValue);
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
                        Log.AddMetaCoreLog(LID.AutoMetaMemberDataL272, "Error 没有解析到Express的内容 在MetaMemberData 里边 372");
                    }
                }
            }
        }
        public override bool ParseMetaExpress()
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                m_Express = ExpressManager.ConvertNewExpress(m_Express, m_DefineMetaType, this );
                m_Express.CalcReturnType();
                m_DefineMetaType = m_Express.GetReturnMetaDefineType();
                if (m_DefineMetaType == null)
                {
                    string tokenText = m_FileMetaMemeberData?.fileMetaCallTermValue?.ToTokenString() ?? m_Name;
                    Log.AddMetaCoreLog(LID.MetaCoreDefineTypeIsNull, m_Token, tokenText);
                    return false;
                }
                if (m_DefineMetaType.isData)
                {
                    m_MemberDataType = EMemberDataType.MemberData;
                }
                else if (m_DefineMetaType.IsArray() )
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
                }
            }
            if (m_MemberDataType == EMemberDataType.MemberData && m_MetaMemberDataDict.Count > 0)
            {
                ResolveAnonymousDataMetaType();
            }
            if (m_DefineMetaType != null)
            {
                m_IsDefineMetaType = true;
                if (m_RealMetaType == null)
                    m_RealMetaType = new MetaType(m_DefineMetaType);
            }
            return true;
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
                    //mmd.ParseMetaExpress();

                    if (AddMetaMemberData(mmd))
                    {
                        mmd.ParseChildMemberData();
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, m_FileMetaMemeberData.fileMetaMemberData[i].token, "" , m_FileMetaMemeberData.fileMetaMemberData[i].token, mmd.name );
                    }
                }
            }
            else if(m_Express != null )
            {
                var mne = m_Express as MetaNewObjectExpressNode;
                var cne = m_Express as MetaCallLinkExpressNode;
                if ( mne != null )
                {
                    for (int i = 0; i < mne.metaContent?.assignStatementsList?.Count; i++)
                    {

                        if (m_MemberDataType == EMemberDataType.MemberData && m_MetaMemberDataDict.Count > 0)
                        {
                            ResolveAnonymousDataMetaType();
                        }
                        var asl = mne.metaContent.assignStatementsList[i];

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
                            Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, cne.token, "534" , cne.token, addMmd.name );
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
                        Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, cne.token, "534", cne.token, addMmd.name);
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
                        Log.AddMetaCoreLog(LID.AutoMetaMemberDataL552, "error 暂不支持其它类型 1");
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
                        MetaType mt = GetFinalMetaType();                        
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
                        Log.AddMetaCoreLog(LID.AutoMetaMemberDataL605, "error 暂不支持其它类型 1");
                    }
                    break;
            }
            return sb.ToString();
        }
    }
}
