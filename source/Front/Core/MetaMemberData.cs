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
        public EMemberDataType memberDataType => m_MemberDataType;
        public MetaExpressNode expressNode => m_Express;
        public Dictionary<string, MetaMemberData> metaMemberDataDict => m_MetaMemberDataDict;

        private EMemberDataType m_MemberDataType = EMemberDataType.None;
        private MetaExpressNode m_Express = null;
        private int m_Index = -1;
        private bool m_IsWithName = false;

        private Dictionary<string, MetaMemberData> m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();
        private FileMetaMemberData m_FileMetaMemeberData = null;
        //private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;

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
        //    m_Name = m_FileMetaOpAssignSyntax.variableRef.name;
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
            m_IsConst = mc.isConst || fmmd.isConst;
            m_Token = fmmd.nameToken;
            m_VariableFrom = EVariableFrom.DataMember;
            if (m_IsWithName)
            {
                m_Name = m_FileMetaMemeberData.name;
            }
            else
            {
                m_Name = m_Index.ToString();
            }
        }
        public MetaMemberData(MetaMemberData parentNode, FileMetaMemberData fmmd, int _index)
        {
            m_Index = _index;
            m_FileMetaMemeberData = fmmd;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaClass(parentNode.ownerMetaBase);
            m_IsConst = parentNode.isConst || fmmd.isConst;
            m_Token = fmmd.nameToken;            
            m_VariableFrom = EVariableFrom.DataMember;
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
        public MetaMemberData( MetaMemberData parentNode, string name, int _index, MetaExpressNode men )
        {
            m_Name = name;
            m_Index = _index;
            SetOwnerMetaClass(parentNode.ownerMetaBase);
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
            mmd.m_DefineMetaType = new MetaType(finalType);
            mmd.m_IsDefineMetaType = isDeclaredType;
            // Keep RealMetaType non-null even for inferred/object placeholders,
            // otherwise downstream code may read define/real shape and hit null.
            mmd.m_RealMetaType = new MetaType(finalType);

            if (finalType.isData)
            {
                mmd.m_MemberDataType = EMemberDataType.MemberData;
            }
            else if (finalType.IsArray())
            {
                mmd.m_MemberDataType = EMemberDataType.MemberArray;
            }
            else
            {
                mmd.m_MemberDataType = EMemberDataType.MemberClass;
            }

            return mmd;
        }

        /// <summary>
        /// 将前端按需构造的 <see cref="MetaMemberVariable"/> 并入所属 <see cref="MetaData"/> 的唯一成员表（<see cref="MetaData.metaMemberDataDict"/>），不再使用并行字典。
        /// </summary>
        public static MetaMemberData CreateFromInjectedMemberVariable(MetaData owner, MetaMemberVariable mmv, int fallbackIndex)
        {
            if (mmv == null || owner == null)
            {
                return null;
            }

            var mmd = new MetaMemberData();
            mmd.m_Name = mmv.name;
            mmd.m_Index = mmv.index >= 0 ? mmv.index : fallbackIndex;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;
            mmd.SetOwnerMetaClass(owner);
            mmd.m_IsStatic = mmv.isStatic;
            mmd.m_IsConst = mmv.isConst || owner.isConst;
            mmd.m_Permission = mmv.permission;

            var def = mmv.defineMetaType ?? new MetaType(CoreMetaClassManager.objectMetaClass);
            mmd.m_DefineMetaType = new MetaType(def);
            mmd.m_IsDefineMetaType = mmv.isDefineMetaType;

            if (mmv.realMetaType != null)
            {
                mmd.m_RealMetaType = new MetaType(mmv.realMetaType);
            }
            else
            {
                mmd.m_RealMetaType = new MetaType(mmd.m_DefineMetaType);
            }

            mmd.m_Express = mmv.express;

            if (def.isData)
            {
                mmd.m_MemberDataType = EMemberDataType.MemberData;
            }
            else if (def.IsArray())
            {
                mmd.m_MemberDataType = EMemberDataType.MemberArray;
            }
            else
            {
                mmd.m_MemberDataType = EMemberDataType.MemberClass;
            }

            if (mmv.token != null)
            {
                mmd.m_Token = mmv.token;
            }

            var plist = mmv.pingTokenList;
            if (plist != null)
            {
                for (int i = 0; i < plist.Count; i++)
                {
                    mmd.AddPingToken(plist[i]);
                }
            }

            return mmd;
        }
        public void SetIndex(int index) { m_Index = index; }
        public MetaMemberData GetMemberDataByName(string name)
        {
            if (m_MetaMemberDataDict.ContainsKey(name))
            {
                return m_MetaMemberDataDict[name];
            }
            return null;
        }
        public bool AddMetaMemberData(MetaMemberData mmd )
        {
            if (m_MetaMemberDataDict.ContainsKey(mmd.name))
            {
                return false;
            }
            m_MetaMemberDataDict.Add(mmd.name, mmd);

            return true;
        }
        public MetaMemberData CreateAnonymousMetaTypeClone(MetaData owner, MetaMemberData source, int index, bool keepDefaultExpress)
        {
            var childType = source.GetFinalMetaType();
            var clone = CreateDeclared(owner, source.name, index, childType,
                source.isDefineMetaType || childType.metaClass != CoreMetaClassManager.objectMetaClass);

            clone.m_IsWithName = source.m_IsWithName;
            clone.m_IsConst = source.m_IsConst;
            clone.m_IsStatic = source.m_IsStatic;
            clone.m_Token = source.m_Token;
            clone.m_MemberDataType = source.m_MemberDataType;
            // 新建匿名 data（未命中 m_AnonymousDataDict）时保留一份“默认元素表达式”用于首轮初始化；
            // 命中复用时，初始化值仍以当前 MetaMemberData 的表达式为准。
            clone.m_Express = keepDefaultExpress ? source.m_Express : null;

            if (source.m_DefineMetaType != null)
            {
                clone.m_DefineMetaType = new MetaType(source.m_DefineMetaType);
            }
            if (source.m_RealMetaType != null)
            {
                clone.m_RealMetaType = new MetaType(source.m_RealMetaType);
            }
            clone.m_IsDefineMetaType = source.m_IsDefineMetaType;

            foreach (var entry in source.m_MetaMemberDataDict)
            {
                var childClone = CreateAnonymousMetaTypeClone(owner, entry.Value, entry.Value.dataFieldOrderIndex, keepDefaultExpress);
                clone.AddMetaMemberData(childClone );
            }

            return clone;
        }
        public MetaData BuildAnonymousMetaDataType(out bool reusedFromAllDataDict)
        {
            reusedFromAllDataDict = false;
            if (m_MetaMemberDataDict.Count == 0)
            {
                return null;
            }

            var tempMetaData = new MetaData("DynamicData_" + m_Name + "_" + GetHashCode(), false, false, true);
            tempMetaData.SetMetaNode(m_OwnerMetaBase?.metaNode);
            tempMetaData.SetDeep(m_Deep + 1);
            if (m_Token != null)
            {
                tempMetaData.AddPingToken(m_Token);
            }

            int index = 0;
            foreach (var entry in m_MetaMemberDataDict)
            {
                var child = entry.Value;
                var clone = CreateAnonymousMetaTypeClone(tempMetaData, child, index, true);
                clone.SetOwnerBlockstatements(m_OwnerMetaBlockStatements);
                tempMetaData.AddMetaMemberData(clone );
                index++;
            }

            var matched = ClassManager.instance.FindMetaDataByNameAndFormat(tempMetaData);
            if (matched == null)
            {
                ClassManager.instance.AddAnonymousMetaData(tempMetaData);
                matched = tempMetaData;
            }
            else
            {
                reusedFromAllDataDict = true;
            }

            return matched;
        }
        private void CreateAnonymousDataNewExpress(MetaData anonymousMetaData)
        {
            if (anonymousMetaData == null)
            {
                return;
            }

            MetaNewObjectExpressNode newExpress = MetaNewObjectExpressNode.CreateAnonymousDataNewObjectExpress(
                this,
                anonymousMetaData,
                ownerMetaBase,
                m_OwnerMetaBlockStatements);
            if (newExpress == null)
            {
                return;
            }

            newExpress.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
            newExpress.CalcReturnType();
            m_Express = newExpress;
        }
        /// <summary>
        /// 后序遍历：先解析子字段（含嵌套匿名 data），再为本层 <see cref="MemberData"/> 构造匿名 <see cref="MetaData"/> 与 <see cref="MetaNewObjectExpressNode"/>。
        /// </summary>
        public void ResolveAnonymousDataHierarchyPostOrder()
        {
            var ordered = new List<MetaMemberData>(m_MetaMemberDataDict.Values);
            ordered.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));
            foreach (var child in ordered)
            {
                if (child.metaMemberDataDict.Count == 0) continue;

                child.ResolveAnonymousDataHierarchyPostOrder();
            }
            if (m_MemberDataType == EMemberDataType.MemberData && m_MetaMemberDataDict.Count > 0)
            {
                ResolveAnonymousDataMetaType();
            }
            else if (m_MemberDataType == EMemberDataType.MemberArray && m_MetaMemberDataDict.Count > 0)
            {
                ResolveArrayElementExpressFromMemberDict();
            }
        }

        private MetaType BuildArrayTypeForMemberDict(int elementCount)
        {
            if (m_DefineMetaType != null && m_DefineMetaType.IsArray())
            {
                var keep = new MetaType(m_DefineMetaType);
                keep.SetArrayLength(elementCount);
                return keep;
            }

            MetaType elementType = new MetaType(CoreMetaClassManager.objectMetaClass);
            var ordered = new List<MetaMemberData>(m_MetaMemberDataDict.Values);
            ordered.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));
            for (int i = 0; i < ordered.Count; i++)
            {
                var childType = ordered[i].GetFinalMetaType();
                if (childType != null)
                {
                    elementType = new MetaType(childType);
                    break;
                }
            }

            var arrayType = new MetaType(CoreMetaClassManager.arrayMetaClass, new List<MetaType>() { elementType });
            // Keep the last dimension flexible (-1) for member-dict reconstructed array literals;
            // concrete element count is carried by the initializer content instead of define type.
            arrayType.SetArrayLength(-1);
            return arrayType;
        }

        internal void ResolveArrayElementExpressFromMemberDict()
        {
            if (m_MemberDataType != EMemberDataType.MemberArray || m_MetaMemberDataDict.Count == 0)
            {
                return;
            }

            var ordered = new List<MetaMemberData>(m_MetaMemberDataDict.Values);
            ordered.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));

            var arrayType = BuildArrayTypeForMemberDict(ordered.Count);
            var parseSetting = new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress };
            MetaClass ownerMc = ownerMetaClass ?? CoreMetaClassManager.objectMetaClass;

            var arrayExpr = m_Express as MetaNewObjectExpressNode;
            if (arrayExpr == null || arrayExpr.newType != MetaNewObjectExpressNode.ENewType.ArrayClass)
            {
                arrayExpr = new MetaNewObjectExpressNode(arrayType, ownerMc, m_OwnerMetaBlockStatements, this);
                m_Express = arrayExpr;
            }

            if (arrayExpr.metaContent?.assignStatementsList == null)
            {
                return;
            }
            arrayExpr.metaContent.assignStatementsList.Clear();

            for (int i = 0; i < ordered.Count; i++)
            {
                var elementMember = ordered[i];
                MetaExpressNode elementExpr = elementMember.expressNode;
                if (elementExpr == null)
                {
                    if (elementMember.memberDataType == EMemberDataType.MemberData
                        && elementMember.metaMemberDataDict.Count > 0)
                    {
                        elementMember.ResolveAnonymousDataMetaType();
                    }
                    else
                    {
                        elementMember.ParseMetaExpress();
                    }
                    elementExpr = elementMember.expressNode;
                }

                if (elementExpr == null)
                {
                    continue;
                }

                var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(ownerMc), elementExpr);
                arrayExpr.metaContent.assignStatementsList.Add(mas);
            }

            arrayExpr.Parse(parseSetting);
            arrayExpr.CalcReturnType();

            var retArrayType = arrayExpr.GetReturnMetaDefineType();
            if (retArrayType != null)
            {
                m_DefineMetaType = new MetaType(retArrayType);
                m_RealMetaType = new MetaType(retArrayType);
                m_IsDefineMetaType = true;
                m_MemberDataType = EMemberDataType.MemberArray;
            }
        }

        internal void ResolveAnonymousDataMetaType()
        {
            if (m_MetaMemberDataDict.Count == 0)
            {
                return;
            }

            var anonymousMetaData = BuildAnonymousMetaDataType(out bool reusedFromAllDataDict);
            if (anonymousMetaData == null)
            {
                return;
            }

            m_DefineMetaType = new MetaType(anonymousMetaData);
            m_RealMetaType = new MetaType(anonymousMetaData);

            // 将 m_MetaMemberDataDict 中每个字段的 express（含嵌套匿名 MetaData 的 MetaNewObjectExpressNode）写入 new 对象的 MetaBraceAssignStatements
            if (m_Express == null)
            {
                CreateAnonymousDataNewExpress(anonymousMetaData);
            }
            if (m_Express is MetaNewObjectExpressNode existingMnoe)
            {
                // 命中 m_AnonymousDataDict：使用当前 MetaMemberData 表达式；未命中：使用匿名 data 默认元素表达式。
                existingMnoe.RebuildAnonymousAssignStatementsFromMemberDict(
                    this,
                    anonymousMetaData,
                    m_OwnerMetaBlockStatements,
                    preferSourceMemberExpress: reusedFromAllDataDict);
            }
            m_MetaMemberDataDict.Clear();
        }
        public override void SetDeep(int deep)
        {
            m_Deep = deep;
            foreach (var v in m_MetaMemberDataDict)
            {
                v.Value.SetDeep(m_Deep + 1);
            }
        }
        public override void CalcParseLevel()
        {
            if (isConst)
            {
                parseLevel = s_ConstLevel;
                s_ConstLevel = s_ConstLevel + 10000;
            }
            else if (isStatic)
            {
                if (parseLevel == -1)
                {
                    if (m_DefineMetaType != null)
                    {
                        parseLevel = s_IsHaveRetStaticLevel;
                        s_IsHaveRetStaticLevel = s_IsHaveRetStaticLevel + 100000;
                    }
                    else
                    {
                        parseLevel = s_NoHaveRetStaticLevel;
                        s_NoHaveRetStaticLevel = s_NoHaveRetStaticLevel + 100000;
                    }

                }
            }
            else
            {
                if (parseLevel == -1)
                {
                    if (m_DefineMetaType != null)
                    {
                        parseLevel = s_DefineMetaTypeLevel;
                        s_DefineMetaTypeLevel = s_DefineMetaTypeLevel + 1000000;
                    }
                    else
                    {
                        parseLevel = s_ExpressLevel;
                        s_ExpressLevel = s_ExpressLevel + 1000000;
                    }
                }
            }

            if (m_Express != null)
            {
                ExpressManager.CalcParseLevel(parseLevel, m_Express);
            }
        }
        public override void CreateMetaExpress()
        {
            if (m_RealMetaType != null) return; //认为已经解析过了
            if (m_FileMetaMemeberData != null)
            {
                switch (m_FileMetaMemeberData.DataType)
                {
                    case FileMetaMemberData.EMemberDataType.Data:
                        {
                            m_MemberDataType = EMemberDataType.MemberData;
                            int count = m_FileMetaMemeberData.fileMetaMemberData.Count;
                            for (int i = 0; i < count; i++)
                            {
                                MetaMemberData mmd = new MetaMemberData(this, m_FileMetaMemeberData.fileMetaMemberData[i], i );

                                mmd.CreateMetaExpress();

                                if (AddMetaMemberData(mmd ))
                                {
                                }
                                else
                                {
                                    Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, m_FileMetaMemeberData.fileMetaMemberData[i].token, "", m_FileMetaMemeberData.fileMetaMemberData[i].token, mmd.name);
                                }
                            }
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.Class:    // data Data{ $childData = Class1{}$ }
                        {
                            m_MemberDataType = EMemberDataType.MemberClass;
                            m_Express = new MetaCallLinkExpressNode(
                                m_FileMetaMemeberData.fileMetaCallTermValue.callLink,
                                ownerMetaData,
                                m_OwnerMetaBlockStatements,
                                this);
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.Array:      // data Data{ $childArray = [  ]$ }
                        {
                            m_MemberDataType = EMemberDataType.MemberArray;
                            int count = m_FileMetaMemeberData.fileMetaMemberData.Count;
                            for (int i = 0; i < count; i++)
                            {
                                MetaMemberData mmd = new MetaMemberData(this, m_FileMetaMemeberData.fileMetaMemberData[i], i);

                                mmd.CreateMetaExpress();
                                if (AddMetaMemberData(mmd))
                                {
                                }
                                else
                                {
                                    Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, m_FileMetaMemeberData.fileMetaMemberData[i].token, "", m_FileMetaMemeberData.fileMetaMemberData[i].token, mmd.name);
                                }
                            }
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.ConstValue:  // data const    a = "aaa"
                        {
                            m_MemberDataType = EMemberDataType.ConstValue;
                            if (m_FileMetaMemeberData.fileMetaConstValue != null)
                            {
                                m_Express = new MetaConstExpressNode(ownerMetaClass, null, m_FileMetaMemeberData.fileMetaConstValue);
                                m_Express.Parse(new AllowUseSettings());
                                m_Express.CalcReturnType();
                                var md = m_Express.GetReturnMetaDefineType();
                                this.m_DefineMetaType = md;
                                this.m_RealMetaType = md;
                            }
                        }
                        break;
                    default:
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, "");
                        }
                        break;
                }
            }
        }
        public override bool ParseMetaExpress()
        {
            if (m_RealMetaType != null) return true; //认为已经解析过了
            if (this.m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                m_Express = ExpressManager.ConvertNewExpress(m_Express, m_DefineMetaType, this );                
            }
            foreach( var v in m_MetaMemberDataDict )
            {
                v.Value.ParseMetaExpress();
            }
            return true;
        }
        public override void ParseRealMetaType()
        {
            foreach (var v in m_MetaMemberDataDict)
            {
                v.Value.ParseRealMetaType();
            }
            if ( m_Express != null && m_RealMetaType == null )
            {
                m_Express.CalcReturnType();
                m_DefineMetaType = m_Express.GetReturnMetaDefineType();
                if (m_DefineMetaType == null)
                {
                    string tokenText = m_FileMetaMemeberData?.fileMetaCallTermValue?.ToTokenString() ?? m_Name;
                    Log.AddMetaCoreLog(LID.MetaCoreDefineTypeIsNull, m_Token, tokenText);
                    return;
                }
                m_RealMetaType = m_DefineMetaType;
            }
        }
        public void StructNewObjectData()
        {
            var mne = m_Express as MetaNewObjectExpressNode;
            var cne = m_Express as MetaCallLinkExpressNode;
            if (mne != null)
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
                        if (mcen != null)
                        {
                            addMmd = new MetaMemberData(this, i.ToString(), i, mcen);
                            addMmd.ParseMetaExpress();
                        }
                        if (mnoe != null)
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
                        Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, cne.token, "534", cne.token, addMmd.name);
                        continue;
                    }
                    m_MetaMemberDataDict.Add(addMmd.name, addMmd);
                }
            }
            else if (cne != null)
            {
                MetaMemberData addMmd = new MetaMemberData(this, name, 0, cne);
                if (m_MetaMemberDataDict.ContainsKey(addMmd.name))
                {
                    Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, cne.token, "534", cne.token, addMmd.name);
                }
                m_MetaMemberDataDict.Add(addMmd.name, addMmd);
            }
            // 子成员（含嵌套匿名 data）须先完成 ParseMetaExpress，父级组装 MetaNewObjectExpressNode / MetaBraceAssignStatements 时才能拿到右值表达式。
            if (m_MemberDataType == EMemberDataType.MemberData && m_MetaMemberDataDict.Count > 0)
            {
                var orderedChildren = new List<MetaMemberData>(m_MetaMemberDataDict.Values);
                orderedChildren.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));
                foreach (var child in orderedChildren)
                {
                    child.ParseMetaExpress();
                }
            }
            // 匿名/结构化 data 字面量：BuildAnonymousMetaDataType 后，把 m_MetaMemberDataDict 各字段的 expressNode
            //（含子字段若为匿名 MetaData 则已是 MetaNewObjectExpressNode）写入本层 MetaNewObjectExpressNode 的 assign 列表。
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
        }
        public bool IsIncludeMetaData(MetaData md)
        {
            if (md == null) return false;

            MetaData belongMD = ownerMetaData
                ?? (ownerMetaClass != null ? ClassManager.instance.FindMetaDataByName(ownerMetaClass.allClassName) : null);
            if (belongMD != null)
            {
                if (belongMD == md)
                {
                    return true;
                }
            }

            return false;
        }
        public string ToFormatString( bool isDynamic )
        {
            StringBuilder sb = new StringBuilder();
            switch (this.m_MemberDataType)
            {
                case EMemberDataType.MemberData:
                    {
                        if (m_MetaMemberDataDict.Count == 0 && m_Express != null)
                        {
                            // Named data-typed member initialized by expression (e.g. MetaInfo(){...}):
                            // when no inline child dict exists, print the expression itself to avoid "{}" loss.
                            if (isDynamic)
                            {
                                if (m_IsWithName)
                                {
                                    sb.Append(m_Name);
                                    sb.Append(" = ");
                                }
                                sb.Append(m_Express.ToFormatString());
                            }
                            else
                            {
                                for (int i = 0; i < realDeep; i++)
                                    sb.Append(Global.tabChar);
                                if (m_IsWithName)
                                {
                                    sb.Append(m_Name);
                                    sb.Append(" = ");
                                }
                                sb.Append(m_Express.ToFormatString());
                            }
                            break;
                        }
                        if (isDynamic)
                        {
                            sb.Append(m_Name);
                            sb.Append(" = {");
                            foreach (var v in m_MetaMemberDataDict)
                            {
                                sb.Append(v.Value.ToFormatString(isDynamic));
                            }
                            sb.Append("}");
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            if( m_IsWithName )
                                sb.AppendLine(m_Name);
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.AppendLine("{");
                            foreach (var v in m_MetaMemberDataDict)
                            {
                                sb.AppendLine(v.Value.ToFormatString(isDynamic));
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
                            if (m_IsWithName)
                            {
                                sb.Append(m_Name);
                                sb.Append(" = ");
                            }
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
                            int i = 0;
                            foreach (var v in m_MetaMemberDataDict)
                            {
                                var itemText = v.Value.ToFormatString(isDynamic)?.Trim();
                                sb.Append(itemText);
                                if (i < m_MetaMemberDataDict.Count - 1)
                                    sb.Append(", ");
                                i++;
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
                                var itemText = v.Value.ToFormatString(isDynamic)?.Trim();
                                sb.Append(itemText);
                                if (i < m_MetaMemberDataDict.Count - 1)
                                    sb.Append(", ");
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
                            sb.Append(m_Name + " = ");
                            sb.Append(m_Express.ToFormatString());
                        }                 
                    }
                    break;
                default:
                    {
                        sb.Append("有没有支持的类型: " + m_MemberDataType.ToString());
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "error 暂不支持其它类型 1");
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
                            var itemText = v.Value.ToFormatString()?.Trim();
                            sb.Append(itemText);
                            if (i < m_MetaMemberDataDict.Count - 1)
                                sb.Append(", ");
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "error 暂不支持其它类型 1");
                    }
                    break;
            }
            return sb.ToString();
        }
    }
}
