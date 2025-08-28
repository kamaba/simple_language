//****************************************************************************
//  File:      MetaEnum.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public class MetaEnum : MetaClass
    {
        public MetaVariable metaVariable => m_MetaVariable;

        protected MetaVariable m_MetaVariable = null;
        public Dictionary<string, MetaMemberEnum> metaMemberEnumDict => m_MetaMemberEnumDict;

        protected Dictionary<string, MetaMemberEnum> m_MetaMemberEnumDict = new Dictionary<string, MetaMemberEnum>();
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
            if( m_MetaVariable == null )
            {
                MetaInputTemplateCollection mitc = new MetaInputTemplateCollection();
                mitc.AddMetaTemplateParamsList(new MetaType(this.extendClass));
                var mt = new MetaType(CoreMetaClassManager.arrayMetaClass, null, mitc);
                m_MetaVariable = new MetaVariable( "values", MetaVariable.EVariableFrom.Static, null, this, mt);
                m_MetaVariable.SetIsStatic( true );
                foreach ( var v in m_MetaMemberEnumDict )
                {
                    m_MetaVariable.AddMetaVariable(v.Value);
                }
            }
        }
        public MetaMemberEnum GetMemberEnumByName(string name)
        {
            if (m_MetaMemberEnumDict.ContainsKey(name))
            {
                return m_MetaMemberEnumDict[name];
            }
            return null;
        }
        public void AddMetaMemberEnum(MetaMemberEnum mmd)
        {
            if (m_MetaMemberEnumDict.ContainsKey(mmd.name))
            {
                return;
            }
            m_MetaMemberEnumDict.Add(mmd.name, mmd);
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
                MetaMemberEnum mmv = new MetaMemberEnum(this, v);
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
                foreach (var v in m_MetaMemberEnumDict)
                {
                    v.Value.SetMetaDefineType(mt);
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.dynamicMetaData)
            {
                //仅限data数据类型
                var mt = new MetaType(m_ExtendClass);
                foreach (var v in m_MetaMemberEnumDict)
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
            if (m_MetaMemberEnumDict.Count == 0)
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
                foreach (var v in m_MetaMemberEnumDict)
                {
                    v.Value.ParseDefineMetaType();

                    if (i++ == 0)
                    {
                        if (v.Value.express == null)
                        {
                            Log.AddInStructMeta( EError.None, "Warning Enum Member Enum 成员第一位必须有=号");
                            continue;
                        }
                    }
                    if (v.Value.express != null)
                    {
                        if (v.Value.constExpressNode == null)
                        {
                            Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用const值类变量");
                            continue;
                        }
                        else if (m_ExtendClass == CoreMetaClassManager.byteMetaClass)
                        {
                            try
                            {
                                indexdynamic = Convert.ToByte(v.Value.constExpressNode.value);
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
                                indexdynamic = (sbyte)Convert.ToByte(v.Value.constExpressNode.value);
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
                                indexdynamic = (short)Convert.ToInt16(v.Value.constExpressNode.value);
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
                                indexdynamic = (ushort)Convert.ToInt16(v.Value.constExpressNode.value);
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
                                indexdynamic = (int)Convert.ToInt32(v.Value.constExpressNode.value);
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
                                indexdynamic = (uint)Convert.ToInt32(v.Value.constExpressNode.value);
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
                            indexdynamic = (long)v.Value.constExpressNode.value;
                        }
                        else
                        if (m_ExtendClass == CoreMetaClassManager.uint64MetaClass)
                        {
                            indexdynamic = (ulong)v.Value.constExpressNode.value;
                        }

                    }
                    else
                    {
                        v.Value.SetExpress(new MetaConstExpressNode(m_ExtendClass.eType, indexdynamic++));
                    }
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.stringMetaClass)
            {
                foreach (var v in m_MetaMemberEnumDict)
                {
                    v.Value.ParseDefineMetaType();
                    if (v.Value.express == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum String成员必须有=号" + v.Key);
                        continue;
                    }
                    if (v.Value.constExpressNode == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用const值类变量");
                        continue;
                    }
                    if (v.Value.constExpressNode.eType != EType.String)
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用string值类变量");
                        continue;
                    }
                }
            }
            else if (m_ExtendClass == CoreMetaClassManager.dynamicMetaData)
            {
                foreach (var v in m_MetaMemberEnumDict)
                {
                    v.Value.ParseDefineMetaType();
                    if (v.Value.express == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 动态成员第一位必须有=号");
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
                    else
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 内允许使用data new 值类变量");
                    }
                }
            }
            else
            {
                foreach (var v in m_MetaMemberEnumDict)
                {
                    v.Value.ParseDefineMetaType();
                    if (v.Value.express == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error Enum Member Enum 成员第一位必须有=号");
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
