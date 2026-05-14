//****************************************************************************
//  File:      MetaEnum.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleLanguage.Core
{
    public class MetaEnum : MetaBase
    {
        public string allClassName => string.IsNullOrEmpty(m_AllName) ? (m_MetaNode?.GetAllName() ?? m_Name) : m_AllName;
        public MetaClass extendClass => m_ExtendClass;
        public MetaData extendMetaData => m_ExtendMetaData;
        public Dictionary<string, MetaMemberVariable> metaMemberVariableDict => m_MetaMemberVariableDict;
        /// <summary>源码绑定（用于 IR 导出路径等）。</summary>
        public FileMetaClass boundFileMetaClass => m_FileMetaClass;

        protected MetaMemberVariable m_ValuesMetaVariable = null;
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected MetaClass m_ExtendClass = null;
        protected MetaData m_ExtendMetaData = null;
        protected EClassDefineType m_ClassDefineType = EClassDefineType.InnerDefine;
        protected FileMetaClass m_FileMetaClass = null;
        public MetaEnum(string _name) : base()
        {
            m_Name = _name;
            m_AllName = _name;
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
        public void CreateValues()
        {
            //鍒涘缓涓€涓狤num 閲岃竟鐨勯潤鎬佸厓绱犲垪琛紝鐢ㄦ潵閬嶅巻 姣斿enum { a = 1; b = 2} 鍒?enum { values = [a,b]
            if(m_ValuesMetaVariable == null )
            {
                List<MetaType> mtList = new List<MetaType>();
                var nmt = new MetaType(CoreMetaClassManager.memberMetaClass);
                mtList.Add(nmt);
                var mt = new MetaType(CoreMetaClassManager.arrayMetaClass, mtList);
                m_ValuesMetaVariable = new MetaMemberVariable(CoreMetaClassManager.enumMetaData, "values" );
                m_ValuesMetaVariable.SetVariableFrom(MetaVariable.EVariableFrom.EnumMember);
                m_ValuesMetaVariable.SetIsDefineMetaType(true);
                m_ValuesMetaVariable.SetMetaDefineType(mt);
                m_ValuesMetaVariable.SetRealMetaType(mt);
                m_ValuesMetaVariable.SetIndex(m_MetaMemberVariableDict.Count);

                MetaArrayExpressNode maen = new MetaArrayExpressNode(CoreMetaClassManager.enumMetaData, null, mt, m_ValuesMetaVariable );
                // values 鏁扮粍鍙簲鍖呭惈鐪熷疄鏋氫妇鎴愬憳锛屼笉搴旀妸 values 鑷繁涔熸斁杩涘幓锛?
                // 鍚﹀垯 for-in 鏋氫妇閬嶅巻浼氬嚭鐜伴澶栭」骞跺鑷村鍑虹殑 IR 閫昏緫寮傚父銆?
                var enumMembers = m_MetaMemberVariableDict.Values
                    .OfType<MetaMemberEnum>()
                    .OrderBy(v => v.index)
                    .ToList();

                foreach (var mme in enumMembers)
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByEnumMember(new MetaType(this), mme);
                    MetaCallLink mcl = new MetaCallLink(mvn);
                    MetaCallLinkExpressNode mclen = new MetaCallLinkExpressNode(mcl);
                    maen.metaCallArray.Add(mclen);
                }



                //newRMT.AddGenTemplateMetaType(m_RealMetaType);

                var valuesNewExpress = new MetaNewObjectExpressNode(mt, CoreMetaClassManager.enumMetaData, null, m_ValuesMetaVariable);
                foreach (var itemExpress in maen.metaCallArray)
                {
                    valuesNewExpress.metaContent.assignStatementsList.Add(
                        new MetaBraceAssignStatements(null, new MetaType(CoreMetaClassManager.enumMetaData), itemExpress));
                }
                MetaType inputType = valuesNewExpress.metaContent.GetMaxLevelMetaType();

                MetaType newRMT = new MetaType();
                newRMT.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                newRMT.AddDefineTemplateMetaType(inputType);
                newRMT = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(newRMT, true, out bool isIGM);
                newRMT.SetArrayLength(valuesNewExpress.metaContent.assignStatementsList.Count);

                //valuesNewExpress.Parse(new AllowUseSettings());
                valuesNewExpress.SetRealMetaType(newRMT);
                valuesNewExpress.CalcReturnType();
                m_ValuesMetaVariable.SetExpress(valuesNewExpress);
                m_MetaMemberVariableDict.Add(m_ValuesMetaVariable.name, m_ValuesMetaVariable);

            }
        }
        public MetaMemberEnum GetMemberEnumByName(string name)
        {
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name] as MetaMemberEnum;
            }
            return null;
        }
        public void AddMetaMemberEnum(MetaMemberEnum mmd)
        {
            if (m_MetaMemberVariableDict.ContainsKey(mmd.name))
            {
                return;
            }
            m_MetaMemberVariableDict.Add(mmd.name, mmd);

            MetaVariableManager.instance.AddMetaEnumVariable(mmd);
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
            if (m_ClassDefineType == EClassDefineType.InnerDefine)
            {
                return;
            }
            if (m_ExtendClass != null)
            {
                return;
            }
            if (m_FileMetaClass == null)
            {
                return;
            }

            var fmcd = m_FileMetaClass.fileMetaExtendClass;
            if (fmcd == null)
            {
                m_ExtendClass = CoreMetaClassManager.int32MetaClass;
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
                Log.AddMetaCoreLog(LID.ShowExtendMessage,
                    "Error Enum extends 没有找到继承类: " + fmcd.allName);
                m_ExtendClass = CoreMetaClassManager.int32MetaClass;
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
                else if (IsAllowedEnumUnderlyingMetaClass(extMc))
                {
                    m_ExtendClass = extMc;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage,
                        "Error Enum extends 仅允许内置整数类型（byte/sbyte/short/ushort/int/uint/long/ulong 等）、string、关键字 data，或具体 data 类型名；不允许继承普通 class: "
                        + fmcd.allName);
                    m_ExtendClass = CoreMetaClassManager.int32MetaClass;
                }
            }
            else if (mn.isMetaData)
            {
                m_ExtendClass = CoreMetaClassManager.dynamicMetaData;
                m_ExtendMetaData = mn.metaData;
            }
            else if (mn.isMetaEnum)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage,
                    "Error Enum 不允许继承另一个 Enum: " + fmcd.allName);
                m_ExtendClass = CoreMetaClassManager.int32MetaClass;
            }
            else
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage,
                    "Error Enum extends 解析到未知节点类型: " + fmcd.allName);
                m_ExtendClass = CoreMetaClassManager.int32MetaClass;
            }
        }
        public void ParseFileMetaEnumMemeberEnum(FileMetaClass fmc)
        {
            m_FileMetaClass = fmc;
            if (fmc.memberFunctionList.Count > 0)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, " Error member function not should function ");
            }
            if (fmc.templateDefineList.Count > 0)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error template define list ");
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "没有找到定义变量名称!");
                    continue;
                }

                MetaBase mb = GetMetaMemberVariableByName(v.name);
                if (mb != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum MetaMemberData have member variable " + m_AllName + "涓?宸叉湁: " + v.token?.ToLexemeAllString() + "鐨勫厓绱?!");
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberEnum mmv = new MetaMemberEnum( this, v, this.extendClass, true );
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberEnum(mmv);
            }
        }
        public void HandleExtendMemberVariable()
        {
            if (m_ExtendClass == null)
            {
                return;
            }

            if (m_ExtendClass == CoreMetaClassManager.uint8MetaClass
               || m_ExtendClass == CoreMetaClassManager.int8MetaClass
               || m_ExtendClass == CoreMetaClassManager.int16MetaClass
               || m_ExtendClass == CoreMetaClassManager.uint16MetaClass
               || m_ExtendClass == CoreMetaClassManager.int32MetaClass
               || m_ExtendClass == CoreMetaClassManager.uint32MetaClass
               || m_ExtendClass == CoreMetaClassManager.int64MetaClass
               || m_ExtendClass == CoreMetaClassManager.uint64MetaClass
               || m_ExtendClass == CoreMetaClassManager.stringMetaClass)
            {
                var mt = new MetaType(m_ExtendClass);
                foreach (var v in m_MetaMemberVariableDict)
                {
                    v.Value.SetMetaDefineType(mt);
                }
            }
            else if (m_ExtendMetaData != null)
            {
                var mt = new MetaType(m_ExtendMetaData);
                foreach (var v in m_MetaMemberVariableDict)
                {
                    v.Value.SetMetaDefineType(mt);
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.dynamicMetaData)
            {
                var mt = new MetaType(m_ExtendClass);
                foreach (var v in m_MetaMemberVariableDict)
                {
                    v.Value.SetMetaDefineType(mt);
                }
            }
            else
            {
                var mt = new MetaType(m_ExtendClass);
                foreach (var v in m_MetaMemberVariableDict)
                {
                    v.Value.SetMetaDefineType(mt);
                }
            }
        }
        public void ParseDefineComplete()
        {
            if (m_MetaMemberVariableDict.Count == 0)
            {
                Log.AddMetaCoreLog(LID.AutoMetaEnumL191, "Warning 鍦╡num : " + name + " 娌℃湁鍙戠幇鏈変换浣曟垚鍛");
                return;
            }

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
                foreach (var v in m_MetaMemberVariableDict)
                {
                    MetaMemberEnum mme = v.Value as MetaMemberEnum;
                    if (mme == null) continue;

                    mme.ParseDefineMetaType();

                    if (i++ == 0)
                    {
                        if (mme.express == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Warning Enum Member Enum 鎴愬憳绗竴浣嶅繀椤绘湁=鍙");
                            continue;
                        }
                    }
                    if (mme.express != null)
                    {
                        // explicit assignment must be parsed first, so constExpressNode can be used
                        mme.ParseMetaExpress();
                        if (mme.enumValueConstExpressNode == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╟onst鍊肩被鍙橀噺");
                            continue;
                        }

                        dynamic explicitValue = 0;
                        if (m_ExtendClass == CoreMetaClassManager.uint8MetaClass)
                        {
                            try
                            {
                                explicitValue = Convert.ToByte(mme.enumValueConstExpressNode?.value);
                            }
                            catch (Exception ex)
                            {
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                    }

                    // Wrap enum member to Core.Member instance: { name, value, index }.
                    mme.WrapAsMemberObjectExpress();
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.stringMetaClass)
            {
                foreach (var v in m_MetaMemberVariableDict)
                {
                    if (v.Value is not MetaMemberEnum mme) continue;

                    mme.ParseDefineMetaType();
                    if (mme.express == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum String鎴愬憳蹇呴』鏈?鍙" + v.Key);
                        continue;
                    }
                    mme.ParseMetaExpress();
                    if (mme.enumValueConstExpressNode == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╟onst鍊肩被鍙橀噺");
                        continue;
                    }
                    if (mme.enumValueConstExpressNode.eType != EType.String)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╯tring鍊肩被鍙橀噺");
                        continue;
                    }

                    mme.WrapAsMemberObjectExpress();
                }
            }
            else if (m_ExtendMetaData != null)
            {
                foreach (var v in m_MetaMemberVariableDict)
                {
                    if (v.Value is not MetaMemberEnum mme) continue;

                    mme.ParseDefineMetaType();
                    if (mme.express == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage,
                            "Error Enum extends data: member must have = assignment: " + v.Key);
                        continue;
                    }
                    mme.ParseMetaExpress();
                    if (mme.express is MetaNewObjectExpressNode mnoeData)
                    {
                        var retDt = mnoeData.GetReturnMetaDefineType();
                        if (!retDt.isData)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage,
                                "Error Enum extends data: member value must be a data new expression");
                        }
                        else if (!m_ExtendMetaData.isDynamic
                            && !ReferenceEquals(retDt.metaData, m_ExtendMetaData))
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage,
                                "Error Enum extends data: 成员必须是 extends 所指定 data 类型的实例（"
                                + m_ExtendMetaData.allClassName + "），实际为: "
                                + (retDt.metaData?.allClassName ?? retDt.name ?? "?"));
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage,
                            "Error Enum extends data: member value must use data new expression");
                    }
                    mme.WrapAsMemberObjectExpress();
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.dynamicMetaData)
            {
                foreach (var v in m_MetaMemberVariableDict)
                {
                    if (v.Value is not MetaMemberEnum mme) continue;

                    mme.ParseDefineMetaType();
                    if (mme.express == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍔ㄦ€佹垚鍛樼涓€浣嶅繀椤绘湁=鍙");
                        continue;
                    }
                    mme.ParseMetaExpress();
                    if (mme.express is MetaNewObjectExpressNode mnoe)
                    {
                        if (mnoe.GetReturnMetaDefineType().isData)
                        {

                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata鍊肩被鍙橀噺, 涓嶅厑璁稿叾瀹冪被鍨");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata new 鍊肩被鍙橀噺");
                    }
                }
            }
            else
            {
                foreach (var v in m_MetaMemberVariableDict)
                {
                    if (v.Value is not MetaMemberEnum mmeClass) continue;

                    mmeClass.ParseDefineMetaType();
                    if (mmeClass.express == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage,
                            "Error Enum extends class: member must have = assignment");
                        continue;
                    }
                    mmeClass.ParseMetaExpress();

                    if (mmeClass.express is MetaNewObjectExpressNode mnoeClass)
                    {
                        var retType = mnoeClass.GetReturnMetaDefineType();
                        if (retType?.metaClass != null && retType.metaClass != m_ExtendClass)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage,
                                "Error Enum extends class: member type " + retType.metaClass.allClassName
                                + " does not match extends class " + m_ExtendClass.allClassName);
                        }
                    }
                    else if (mmeClass.constExpressNode != null)
                    {
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage,
                            "Error Enum extends class: member value must use new expression or const value");
                    }
                    mmeClass.WrapAsMemberObjectExpress();
                }
            }
        }
        //public override string ToFormatString()
        //{
        //    StringBuilder stringBuilder = new StringBuilder();
        //    stringBuilder.Clear();
        //    for (int i = 0; i < realDeep; i++)
        //        stringBuilder.Append(Global.tabChar);
        //    stringBuilder.Append(permission.ToFormatString());
        //    stringBuilder.Append(" ");
        //    stringBuilder.Append("enum ");
        //    if (topLevelMetaNamespace != null)
        //    {
        //        stringBuilder.Append(topLevelMetaNamespace.allName + ".");
        //    }
        //    stringBuilder.Append(name);

        //    stringBuilder.Append(Environment.NewLine);
        //    for (int i = 0; i < realDeep; i++)
        //        stringBuilder.Append(Global.tabChar);
        //    stringBuilder.Append("{" + Environment.NewLine);

        //    foreach (var v in m_MetaMemberEnumDict )
        //    {
        //        MetaMemberEnum mmv = v.Value;                
        //        if (mmv.fromType == EFromType.Code)
        //        {
        //            stringBuilder.Append(mmv.ToFormatString());
        //            stringBuilder.Append(Environment.NewLine);
        //        }
        //        else
        //        {
        //            stringBuilder.Append("Errrrrroooorrr ---" + mmv.ToFormatString());
        //            stringBuilder.Append(Environment.NewLine);
        //        }
        //    }

        //    for (int i = 0; i < realDeep; i++)
        //        stringBuilder.Append(Global.tabChar);
        //    stringBuilder.Append("}" + Environment.NewLine);

        //    return stringBuilder.ToString();
        //}
    }
}
