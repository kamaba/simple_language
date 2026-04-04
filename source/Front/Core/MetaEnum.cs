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
        public MetaVariable GetOrCreateValuesVariable()
        {
            CreateValues();
            return m_ValuesMetaVariable;
        }

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
        public void CreateValues()
        {
            //创建一个Enum 里边的静态元素列表，用来遍历 比如enum { a = 1; b = 2} 则 enum { values = [a,b]
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
                // values 数组只应包含真实枚举成员，不应把 values 自己也放进去；
                // 否则 for-in 枚举遍历会出现额外项并导致导出的 IR 逻辑异常。
                var enumMembers = m_MetaMemberVariableDict.Values
                    .OfType<MetaMemberEnum>()
                    .OrderBy(v => v.index)
                    .ToList();

                foreach ( var v in enumMembers )
                {
                    MetaCallLink mcl = new MetaCallLink( this, nmt);
                    MetaVisitNode mvn = MetaVisitNode.CreateByVariable(v);
                    mcl.AddVisitNodeList(mvn);
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
                Log.AddInStructMeta(EError.None, "Error Enum中不允许有Function!!");
            }
            if (fmc.templateDefineList.Count > 0)
            {
                Log.AddInStructMeta(EError.None, "Error 在Enum定义中，不允许使用Template模板的形式!");
            }
            //for (int i = 0; i < fmc.templateParamList.Count; i++)
            //{
            //    string tTemplateName = fmc.templateParamList[i].name;
            //    if (m_MetaTemplateList.Find(a => a.name == tTemplateName) != null)
            //    {
            //        Debug.Write("Error 定义模式名称重复!!");
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
                    Log.AddInStructMeta(EError.None, "Error Enum MetaMemberData已有定义类: " + m_AllName + "中 已有: " + v.token?.ToLexemeAllString() + "的元素!!");
                    isHave = true;
                }
                else
                    isHave = false;
                // 如果类定义前带有 const，则传递父级 const 标志，使内部成员默认视为 const
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

            if (m_ExtendClass == CoreMetaClassManager.byteMetaClass
               || m_ExtendClass == CoreMetaClassManager.sbyteMetaClass
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
                //仅限data数据类型
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
                Log.AddInStructMeta(EError.None, "Warning 在enum : " + name + " 没有发现有任何成员");
                return;
            }

            if (m_ExtendClass == CoreMetaClassManager.byteMetaClass
               || m_ExtendClass == CoreMetaClassManager.sbyteMetaClass
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
                            Log.AddInStructMeta(EError.None, "Warning Enum Member Enum 成员第一位必须有=号");
                            continue;
                        }
                    }
                    if (mme.express != null)
                    {
                        // explicit assignment must be parsed first, so constExpressNode can be used
                        mme.ParseMetaExpress();
                        if (mme.enumValueConstExpressNode == null)
                        {
                            Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用const值类变量");
                            continue;
                        }

                        dynamic explicitValue = 0;
                        if (m_ExtendClass == CoreMetaClassManager.byteMetaClass)
                        {
                            try
                            {
                                explicitValue = Convert.ToByte(mme.enumValueConstExpressNode?.value);
                            }
                            catch (Exception ex)
                            {
                                Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内部int转byte出错");
                                continue;
                            }
                        }
                        else
                        if (m_ExtendClass == CoreMetaClassManager.sbyteMetaClass)
                        {
                            try
                            {
                                explicitValue = (sbyte)Convert.ToByte(mme.enumValueConstExpressNode?.value);
                            }
                            catch (Exception ex)
                            {
                                Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内部int转byte出错");
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
                                Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内部int转byte出错");
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
                                Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内部int转byte出错");
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
                                Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内部int转byte出错");
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
                                Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内部int转byte出错");
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
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum String成员必须有=号" + v.Key);
                        continue;
                    }
                    mme.ParseMetaExpress();
                    if (mme.enumValueConstExpressNode == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用const值类变量");
                        continue;
                    }
                    if (mme.enumValueConstExpressNode.eType != EType.String)
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用string值类变量");
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
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 动态成员第一位必须有=号");
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
                            Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用data值类变量, 不允许其它类型");
                        }
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用data new 值类变量");
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
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 成员必须有=号");
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
                            Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用data值类变量, 不允许其它类型");
                        }
                    }
                    else if (v.Value.constExpressNode != null)
                    {

                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用data new 值类变量");
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
