//****************************************************************************
//  File:      MetaEnum.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaEnum : MetaBase
    {
        public override string allName => string.IsNullOrEmpty(m_AllName) ? (m_MetaNode?.GetAllName() ?? m_Name) : m_AllName;
        public MetaClass extendClass => m_ExtendClass;
        public MetaData extendMetaData => m_ExtendMetaData;
        public bool isErrorEnum => m_ExtendClass == CoreMetaClassManager.errorMetaClass;
        public Dictionary<string, MetaMemberEnum> metaMemberEnumDict => m_MetaMemberEnumDict;
        public Dictionary<string, MetaMemberVariable> metaMemberVariableDict => m_MetaMemberVariableDict;
        /// <summary>源码绑定（用于 IR 导出路径等）。</summary>
        public FileMetaClass boundFileMetaClass => m_FileMetaClass;

        protected MetaMemberVariable m_ValuesMetaVariable = null;
        protected Dictionary<string, MetaMemberEnum> m_MetaMemberEnumDict = new Dictionary<string, MetaMemberEnum>();
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected MetaClass m_ExtendClass = null;
        protected MetaData m_ExtendMetaData = null;
        protected EClassDefineType m_ClassDefineType = EClassDefineType.InnerDefine;
        protected FileMetaClass m_FileMetaClass = null;
        public MetaEnum(string _name) : base()
        {
            m_Name = _name;
            m_Type = EType.Enum;
        }
        public void SetClassDefineType(EClassDefineType type)
        {
            m_ClassDefineType = type;
        }
        public void SetExtendClass(MetaClass mc)
        {
            m_ExtendClass = mc;
        }
        public void ParseFileCollectMemberVariableDefineMetaType()
        {
            foreach( var v in m_MetaMemberEnumDict )
            {
                v.Value.ParseDefineMetaType();
                v.Value.CreateMetaExpress();
            }
        }
        public MetaMemberVariable GetMetaMemberVariableByName(string name)
        {
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name];
            }
            return null;
        }
        public MetaVariable GetOrCreateValuesVariable()
        {
            CreateValues();
            return m_ValuesMetaVariable;
        }
        void CreateValues()
        {
            //鍒涘缓涓€涓狤num 閲岃竟鐨勯潤鎬佸厓绱犲垪琛紝鐢ㄦ潵閬嶅巻 姣斿enum { a = 1; b = 2} 鍒?enum { values = [a,b]
            if(m_ValuesMetaVariable == null )
            {
                List<MetaType> mtList = new List<MetaType>();
                var nmt = new MetaType(CoreMetaClassManager.memberMetaClass);
                mtList.Add(nmt);
                var mt = new MetaType(CoreMetaClassManager.arrayMetaClass, mtList);
                m_ValuesMetaVariable = new MetaMemberVariable( this, "values" );
                m_ValuesMetaVariable.SetVariableFrom(MetaVariable.EVariableFrom.EnumMember);
                m_ValuesMetaVariable.SetIsDefineMetaType(true);
                m_ValuesMetaVariable.SetMetaDefineType(mt);
                m_ValuesMetaVariable.SetRealMetaType(mt);
                m_ValuesMetaVariable.SetIndex(m_MetaMemberVariableDict.Count);

                MetaArrayExpressNode maen = new MetaArrayExpressNode( this, null, mt, m_ValuesMetaVariable );
                // values 鏁扮粍鍙簲鍖呭惈鐪熷疄鏋氫妇鎴愬憳锛屼笉搴旀妸 values 鑷繁涔熸斁杩涘幓锛?
                // 鍚﹀垯 for-in 鏋氫妇閬嶅巻浼氬嚭鐜伴澶栭」骞跺鑷村鍑虹殑 IR 閫昏緫寮傚父銆?
                var enumMembers = m_MetaMemberVariableDict.Values.Where(v => v.name != "values").ToList();

                foreach (var mme in enumMembers)
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByEnumMember(mme.GetFinalMetaType(), mme);
                    MetaCallLink mcl = new MetaCallLink(mvn);
                    MetaCallLinkExpressNode mclen = new MetaCallLinkExpressNode(mcl);
                    mclen.SetToken(mme.token);
                    maen.metaCallArray.Add(mclen);
                    maen.SetToken(mme.token);
                }
                maen.Parse( new AllowUseSettings());
                maen.CalcReturnType();

                var valuesNewExpress = new MetaNewObjectExpressNode(mt, maen, this, null );
                valuesNewExpress.SetToken(m_Token);

                valuesNewExpress.Parse(new AllowUseSettings());
                valuesNewExpress.CalcReturnType();
                m_ValuesMetaVariable.SetExpress(valuesNewExpress);
                m_MetaMemberVariableDict.Add(m_ValuesMetaVariable.name, m_ValuesMetaVariable);

            }
        }
        void UpsertRelationMemberValueAssign(MetaMemberEnum mme, MetaExpressNodeBase valueExpress)
        {
            if (mme == null || valueExpress == null)
            {
                return;
            }
            if (mme.relationMemberVariable?.express is not MetaNewObjectExpressNode mnoen)
            {
                return;
            }

            var valueMv = CoreMetaClassManager.memberMetaClass.GetMetaMemberVariableByName("value");
            if (valueMv == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Core.Member 缺少 value 字段，无法构造 Member 初始化");
                return;
            }

            var list = mnoen.assignStatementsList;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i]?.defineName == "value")
                {
                    list.RemoveAt(i);
                }
            }
            list.Add(new MetaBraceAssignStatements(valueMv, null, mme.ownerMetaBase, valueExpress));
        }
        /// <summary>
        /// Enum 的 extends 底层类型：仅允许内置整型族与 string（与成员语义分支一致），不允许用户 class。
        /// </summary>
        static bool IsAllowedEnumUnderlyingMetaClass(MetaClass mc)
        {
            if (mc == null)
            {
                return false;
            }
            return mc == CoreMetaClassManager.uint8MetaClass
                || mc == CoreMetaClassManager.int8MetaClass
                || mc == CoreMetaClassManager.int16MetaClass
                || mc == CoreMetaClassManager.uint16MetaClass
                || mc == CoreMetaClassManager.int32MetaClass
                || mc == CoreMetaClassManager.uint32MetaClass
                || mc == CoreMetaClassManager.int64MetaClass
                || mc == CoreMetaClassManager.uint64MetaClass
                || mc == CoreMetaClassManager.stringMetaClass;
        }

        public void ParseExtendsRelation()
        {
            if (m_ExtendClass != null)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, m_Token, "have parse extend in enum");
                return;
            }
            if (m_FileMetaClass == null)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, m_Token, "m_FileMetaClass is null in ParseExtendsRelation");
                return;
            }

            var fmcd = m_FileMetaClass.fileMetaExtendClass;
            if (fmcd == null)
            {
                m_ExtendClass = CoreMetaClassManager.objectMetaClass;
                return;
            }

            MetaNode mn = ClassManager.instance.GetMetaClassByNameAndFileMeta(
                null, fmcd.fileMeta, fmcd.stringList);

            if (mn == null)
            {
                mn = fmcd.fileMeta.GetMetaBaseByFileMetaClassRef(fmcd);
            }

            if (mn == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token,
                    "Error Enum extends 没有找到继承类: " + fmcd.allName);
                m_ExtendClass = CoreMetaClassManager.objectMetaClass;
                return;
            }

            if (mn.IsMetaClass())
            {
                var extMc = mn.GetMetaClassByTemplateCount(0);
                if (extMc == CoreMetaClassManager.dynamicMetaData)
                {
                    // 关键字 extends data → 底层为动态 data，成员可为多种已定义 data 的 new 表达式
                    m_ExtendClass = CoreMetaClassManager.dynamicMetaData;
                }
                else if (extMc == CoreMetaClassManager.errorMetaClass)
                {
                    // enum extends Error: error enum, members are structured objects
                    m_ExtendClass = extMc;
                }
                else if (IsAllowedEnumUnderlyingMetaClass(extMc))
                {
                    m_ExtendClass = extMc;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token,
                        "Error Enum extends 仅允许内置整数类型（byte/sbyte/short/ushort/int/uint/long/ulong 等）、string、关键字 data，或具体 data 类型名；不允许继承普通 class: "
                        + fmcd.allName);
                    m_ExtendClass = CoreMetaClassManager.int32MetaClass;
                }
            }
            else if (mn.isMetaData)
            {
                m_ExtendClass = null;
                m_ExtendMetaData = mn.metaData;
            }
            else if (mn.isMetaEnum)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                    "Error Enum 不允许继承另一个 Enum: " + fmcd.allName);
                m_ExtendClass = CoreMetaClassManager.int32MetaClass;
            }
            else
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                    "Error Enum extends 解析到未知节点类型: " + fmcd.allName);
                m_ExtendClass = CoreMetaClassManager.int32MetaClass;
            }
        }
        public void ParseFileMetaEnumMemeberEnum(FileMetaClass fmc)
        {
            m_FileMetaClass = fmc;
            m_Token = fmc.token;
            if (fmc.memberFunctionList.Count > 0)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, " Error member function not should function ");
            }
            if (fmc.templateDefineList.Count > 0)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error template define list ");
            }
            //for (int i = 0; i < fmc.templateParamList.Count; i++)
            //{
            //    string tTemplateName = fmc.templateParamList[i].name;
            //    if (m_MetaTemplateList.Find(a => a.name == tTemplateName) != null)
            //    {
            //        Debug.Write("Error 瀹氫箟妯″紡鍚嶇О閲嶅!!");
            //    }
            //    else
            //    {
            //        m_MetaTemplateList.Add(new MetaTemplate(this, fmc.templateParamList[i]));
            //    }
            //}

            bool isHave = false;
            foreach (var v in fmc.memberVariableList)
            {
                if (string.IsNullOrEmpty(v.name))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, v.token, "ParseFileMetaEnumMemeberEnum 没有找到定义变量名称!");
                    continue;
                }

                MetaBase mb = GetMetaMemberVariableByName(v.name);
                if (mb != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, v.token, "Error Enum MetaMemberData have member variable " + m_AllName + "涓?宸叉湁: " + v.token?.ToLexemeAllString() + "鐨勫厓绱?!");
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberEnum mme = new MetaMemberEnum( this, v );
                if (isHave)
                {
                    mme.SetName(mme.name + "__repeat__");
                }
                if (m_MetaMemberEnumDict.ContainsKey(mme.name))
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, v.token, "repeat name ");
                    return;
                }

                m_MetaMemberEnumDict.Add(mme.name, mme);

                MetaMemberVariable mmv = MetaMemberEnum.WrapAsEnumMemberObjectExpress(this, v, mme.index);
                if (mmv != null)
                {
                    m_MetaMemberVariableDict.Add(mme.name, mmv);
                    mme.SetRelationMemberVariable(mmv);
                    mmv.SetSourceMetaVariable(mme);
                }

                MetaVariableManager.instance.AddMetaEnumVariable(mme);
            }
        }
        public void ParseDefineComplete()
        {
            if (m_MetaMemberEnumDict.Count == 0)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Warning Enum : " + name );
                return;
            }
            AutoCreateExpress();
            CreateValues();

        }        
        void AutoCreateExpress()
        {

            if (m_ExtendClass == CoreMetaClassManager.uint8MetaClass
               || m_ExtendClass == CoreMetaClassManager.int8MetaClass
               || m_ExtendClass == CoreMetaClassManager.int16MetaClass
               || m_ExtendClass == CoreMetaClassManager.uint16MetaClass
               || m_ExtendClass == CoreMetaClassManager.int32MetaClass
               || m_ExtendClass == CoreMetaClassManager.uint32MetaClass
               || m_ExtendClass == CoreMetaClassManager.int64MetaClass
               || m_ExtendClass == CoreMetaClassManager.uint64MetaClass)
            {

                int i = 0;
                dynamic indexdynamic = 0;
                foreach (var v in m_MetaMemberEnumDict)
                {
                    MetaMemberEnum mme = v.Value;
                    if (mme == null) continue;

                    if (i == 0)
                    {
                        if (mme.realMetaType == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Warning Enum Member Enum realMetaType is null");
                            continue;
                        }
                    }
                    if (mme.express != null)
                    {
                        if (mme.enumValueConstExpressNode == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum const express ");
                            continue;
                        }

                        UpsertRelationMemberValueAssign(mme, mme.express);

                        dynamic explicitValue = 0;
                        if (m_ExtendClass == CoreMetaClassManager.uint8MetaClass)
                        {
                            try
                            {
                                explicitValue = Convert.ToByte(mme.enumValueConstExpressNode?.value);
                            }
                            catch (Exception ex)
                            {
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum ex" + ex.ToString());
                                continue;
                            }
                        }
                        else
                            if (m_ExtendClass == CoreMetaClassManager.int8MetaClass)
                            {
                                try
                                {
                                    explicitValue = (sbyte)Convert.ToByte(mme.enumValueConstExpressNode?.value);
                                }
                                catch (Exception ex)
                                {
                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
                                    continue;
                                }
                            }
                            else
                                if (m_ExtendClass == CoreMetaClassManager.int16MetaClass)
                                {
                                    try
                                    {
                                        explicitValue = (short)Convert.ToInt16(mme.enumValueConstExpressNode?.value);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
                                        continue;
                                    }
                                }
                                else
                                    if (m_ExtendClass == CoreMetaClassManager.uint16MetaClass)
                                    {
                                        try
                                        {
                                            explicitValue = (ushort)Convert.ToUInt16(mme.enumValueConstExpressNode?.value);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
                                            continue;
                                        }
                                    }
                                    else
                                        if (m_ExtendClass == CoreMetaClassManager.int32MetaClass)
                                        {
                                            try
                                            {
                                                explicitValue = (int)Convert.ToInt32(mme.enumValueConstExpressNode?.value);
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
                                                continue;
                                            }
                                        }
                                        else
                                            if (m_ExtendClass == CoreMetaClassManager.uint32MetaClass)
                                            {
                                                try
                                                {
                                                    explicitValue = (uint)Convert.ToUInt32(mme.enumValueConstExpressNode?.value);
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
                                                    continue;
                                                }
                                            }
                                            else
                                                if (m_ExtendClass == CoreMetaClassManager.int64MetaClass)
                                                {
                                                    explicitValue = (long)Convert.ToInt64(mme.enumValueConstExpressNode?.value);
                                                }
                                                else
                                                    if (m_ExtendClass == CoreMetaClassManager.uint64MetaClass)
                                                    {
                                                        explicitValue = (ulong)Convert.ToUInt64(mme.enumValueConstExpressNode?.value);
                                                    }

                        // Next implicit member follows explicit value.
                        indexdynamic = explicitValue + 1;
                    }
                    else
                    {
                        // auto increment when missing '='
                        var autoConst = new MetaConstExpressNode(m_ExtendClass.eType, indexdynamic++);
                        mme.SetExpress(autoConst);
                        mme.SetIsExplicitAssign(false);
                        mme.ParseMetaExpress();
                        mme.ParseRealMetaType();



                        UpsertRelationMemberValueAssign(mme, autoConst);
                    }
                    i++;
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.stringMetaClass
               || m_ExtendClass == CoreMetaClassManager.float32MetaClass
               || m_ExtendClass == CoreMetaClassManager.float64MetaClass)
            {
                foreach (var v in m_MetaMemberEnumDict)
                {
                    var mme = v.Value;
                    if (mme == null) continue;

                    if (mme.enumValueConstExpressNode == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╟onst鍊肩被鍙橀噺");
                        continue;
                    }
                    if (mme.enumValueConstExpressNode.eType != m_ExtendClass.eType)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╯tring鍊肩被鍙橀噺");
                        continue;
                    }
                }
            }
            else if (m_ExtendMetaData != null)
            {
                foreach (var v in m_MetaMemberEnumDict)
                {
                    var mme = v.Value;
                    if (mme == null) continue;

                    if (mme.express == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token,
                            "Error Enum extends data: member must have = assignment: " + v.Key);
                        continue;
                    }
                    if (mme.express is MetaNewObjectExpressNode mnoeData)
                    {
                        var retDt = mnoeData.GetReturnMetaType();
                        if (!retDt.isData)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token,
                                "Error Enum extends data: member value must be a data new expression");
                        }
                        else if (!m_ExtendMetaData.isDynamic
                            && !ReferenceEquals(retDt.metaData, m_ExtendMetaData))
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token,
                                "Error Enum extends data: 成员必须是 extends 所指定 data 类型的实例（"
                                + m_ExtendMetaData.allName + "），实际为: "
                                + (retDt.metaData?.allName ?? retDt.name ?? "?"));
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token,
                            "Error Enum extends data: member value must use data new expression");
                    }
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.dynamicMetaData)
            {
                foreach (var v in m_MetaMemberEnumDict)
                {
                    var mme = v.Value;
                    if (mme == null) continue;

                    if (mme.express == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍔ㄦ€佹垚鍛樼涓€浣嶅繀椤绘湁=鍙");
                        continue;
                    }
                    if (mme.express is MetaNewObjectExpressNode mnoe)
                    {
                        if (mnoe.GetReturnMetaType().isData)
                        {

                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata鍊肩被鍙橀噺, 涓嶅厑璁稿叾瀹冪被鍨");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata new 鍊肩被鍙橀噺");
                    }
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.errorMetaClass)
            {
                // enum extends Error: members can be simple values or structured objects
                foreach (var v in m_MetaMemberEnumDict)
                {
                    var mme = v.Value;
                    if (mme == null) continue;

                    if (mme.express == null)
                    {
                        // auto-assign index for simple members without explicit value
                        var autoConst = new MetaConstExpressNode(EType.Int32, (long)m_MetaMemberEnumDict.Count);
                        mme.SetExpress(autoConst);
                        mme.SetIsExplicitAssign(false);
                        mme.ParseMetaExpress();
                        mme.ParseRealMetaType();
                    }
                    // structured objects ({ id = 1, msg = "..." }) and simple values are both accepted
                }
            }
        }
        public void UpdateAllName()
        {
            m_AllName = m_MetaNode?.GetAllName() ?? m_Name;
            //foreach (var v in m_MetaMemberVariableDict)
            //{
            //    v.Value.UpdateAllName();
            //}
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append(permission.ToFormatString());
            stringBuilder.Append(" ");
            stringBuilder.Append("enum ");
            //if (topLevelMetaNamespace != null)
            //{
            //    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
            //}
            stringBuilder.Append(name);

            stringBuilder.Append(Environment.NewLine);
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v in m_MetaMemberVariableDict)
            {
                stringBuilder.AppendLine(v.Value.ToFormatString());
            }

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
