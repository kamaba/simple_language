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
    public class MetaEnum : MetaClass
    {

        protected MetaMemberVariable m_ValuesMetaVariable = null;
        public MetaEnum(string _name) : base(_name)
        {
            m_Type = EType.Enum;
        }
        public MetaMemberVariable GetMemberVariableByName(string name)
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
                m_ValuesMetaVariable = new MetaMemberVariable(this, "values" );
                m_ValuesMetaVariable.SetVariableFrom(MetaVariable.EVariableFrom.EnumMember);
                m_ValuesMetaVariable.SetIsDefineMetaType(true);
                m_ValuesMetaVariable.SetMetaDefineType(mt);
                m_ValuesMetaVariable.SetRealMetaType(mt);
                m_ValuesMetaVariable.SetIndex(m_MetaMemberVariableDict.Count);

                MetaArrayExpressNode maen = new MetaArrayExpressNode( this, null, mt, m_ValuesMetaVariable );
                // values 鏁扮粍鍙簲鍖呭惈鐪熷疄鏋氫妇鎴愬憳锛屼笉搴旀妸 values 鑷繁涔熸斁杩涘幓锛?
                // 鍚﹀垯 for-in 鏋氫妇閬嶅巻浼氬嚭鐜伴澶栭」骞跺鑷村鍑虹殑 IR 閫昏緫寮傚父銆?
                var enumMembers = m_MetaMemberVariableDict.Values
                    .OfType<MetaMemberEnum>()
                    .OrderBy(v => v.index)
                    .ToList();

                foreach (var mme in enumMembers)
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByEnumMember(mme.defineMetaType, mme);
                    MetaCallLink mcl = new MetaCallLink(mvn);
                    MetaCallLinkExpressNode mclen = new MetaCallLinkExpressNode(mcl);
                    maen.metaCallArray.Add(mclen);
                }



                //newRMT.AddGenTemplateMetaType(m_RealMetaType);

                var valuesNewExpress = new MetaNewObjectExpressNode(maen, this, null, m_ValuesMetaVariable);
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
        }
        public void ParseFileMetaEnumMemeberEnum(FileMetaClass fmc)
        {
            if (fmc.memberFunctionList.Count > 0)
            {
                Log.AddMetaCoreLog(LID.Unknown, "Error Enum涓笉鍏佽鏈塅unction!!");
            }
            if (fmc.templateDefineList.Count > 0)
            {
                Log.AddMetaCoreLog(LID.Unknown, "Error 鍦‥num瀹氫箟涓紝涓嶅厑璁镐娇鐢═emplate妯℃澘鐨勫舰寮?");
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
                MetaBase mb = GetMetaMemberVariableByName(v.name);
                if (mb != null)
                {
                    Log.AddMetaCoreLog(LID.Unknown, "Error Enum MetaMemberData宸叉湁瀹氫箟绫? " + m_AllName + "涓?宸叉湁: " + v.token?.ToLexemeAllString() + "鐨勫厓绱?!");
                    isHave = true;
                }
                else
                    isHave = false;
                // 濡傛灉绫诲畾涔夊墠甯︽湁 const锛屽垯浼犻€掔埗绾?const 鏍囧織锛屼娇鍐呴儴鎴愬憳榛樿瑙嗕负 const
                bool parentIsConst = fmc.isConst;
                MetaMemberEnum mmv = new MetaMemberEnum(this, v, this.extendClass, parentIsConst);
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                AddMetaMemberEnum(mmv);
            }
        }
        public override void HandleExtendMemberVariable()
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
                foreach (var v in m_MetaMemberVariableDict )
                {
                    v.Value.SetMetaDefineType(mt);
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.dynamicMetaData)
            {
                //浠呴檺data鏁版嵁绫诲瀷
                var mt = new MetaType(m_ExtendClass);
                foreach (var v in m_MetaMemberVariableDict )
                {
                    v.Value.SetMetaDefineType(mt);
                }
            }
        }
        public override void ParseDefineComplete()
        {
            base.ParseDefineComplete();
        }
        public void ParseMemberMetaEnumExpress()
        {
            if (m_MetaMemberVariableDict.Count == 0)
            {
                Log.AddMetaCoreLog(LID.Unknown, "Warning 鍦╡num : " + name + " 娌℃湁鍙戠幇鏈変换浣曟垚鍛");
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
                foreach (var v in m_MetaMemberVariableDict )
                {
                    MetaMemberEnum mme = v.Value as MetaMemberEnum;
                    if (mme == null) continue;

                    mme.ParseDefineMetaType();

                    if (i++ == 0)
                    {
                        if (mme.express == null)
                        {
                            Log.AddMetaCoreLog(LID.Unknown, "Warning Enum Member Enum 鎴愬憳绗竴浣嶅繀椤绘湁=鍙");
                            continue;
                        }
                    }
                    if (mme.express != null)
                    {
                        // explicit assignment must be parsed first, so constExpressNode can be used
                        mme.ParseMetaExpress();
                        if (mme.enumValueConstExpressNode == null)
                        {
                            Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╟onst鍊肩被鍙橀噺");
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
                                Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                                Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呴儴int杞琤yte鍑洪敊");
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
                        mme.SetIsExplicitAssign( false );
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
                        Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum String鎴愬憳蹇呴』鏈?鍙" + v.Key);
                        continue;
                    }
                    mme.ParseMetaExpress();
                    if (mme.enumValueConstExpressNode == null)
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╟onst鍊肩被鍙橀噺");
                        continue;
                    }
                    if (mme.enumValueConstExpressNode.eType != EType.String)
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╯tring鍊肩被鍙橀噺");
                        continue;
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
                        Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍔ㄦ€佹垚鍛樼涓€浣嶅繀椤绘湁=鍙");
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
                            Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata鍊肩被鍙橀噺, 涓嶅厑璁稿叾瀹冪被鍨");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata new 鍊肩被鍙橀噺");
                    }
                }
            }
            else
            {
                foreach (var v in m_MetaMemberVariableDict )
                {
                    v.Value.ParseDefineMetaType();
                    if (v.Value.express == null)
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鎴愬憳蹇呴』鏈?鍙");
                        continue;
                    }
                    v.Value.ParseMetaExpress();

                    if (v.Value.express is MetaNewObjectExpressNode mnoe)
                    {
                        if (mnoe.GetReturnMetaDefineType().isData)
                        {

                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata鍊肩被鍙橀噺, 涓嶅厑璁稿叾瀹冪被鍨");
                        }
                    }
                    else if (v.Value.constExpressNode != null)
                    {

                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error Enum Member Enum 鍐呭厑璁镐娇鐢╠ata new 鍊肩被鍙橀噺");
                    }
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
