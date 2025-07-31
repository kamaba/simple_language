//****************************************************************************
//  File:      MetaMemberEnum.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: enum's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaMemberEnum : MetaVariable
    {
        public EFromType fromType => m_FromType;
        public int index => m_Index;
        public MetaExpressNode express => m_Express;
        public int parseLevel { get; set; } = -1;
        public bool isInnerDefine => m_IsInnerDefine;

        private EFromType m_FromType = EFromType.Code;
        private int m_Index = -1;
        private FileMetaMemberVariable m_FileMetaMemeberVariable;
        private MetaExpressNode m_Express = null;
        private bool m_IsInnerDefine = false;
        private List<MetaGenTemplate> m_MetaGenTemplateList = null;

        private bool m_IsSupportConstructionFunctionOnlyBraceType = false;  //是否支持构造函数使用 仅{}形式    Class1{ a = {} } 不支持
        private bool m_IsSupportConstructionFunctionConnectBraceType = true;  //是否支持构造函数名称后边加{}形式    Class1{ a = Class2(){} } 不支持
        private bool m_IsSupportConstructionFunctionOnlyParType = true; //是否支持构造函数使用 仅()形式    Class1{ a = () } 不支持
#pragma warning disable CS0414 // 字段“MetaMemberVariable.m_IsSupportInExpressUseStaticMetaMemeberFunction”已被赋值，但从未使用过它的值
        private bool m_IsSupportInExpressUseStaticMetaMemeberFunction = true;   //是否在成员支持静态函数的
#pragma warning restore CS0414 // 字段“MetaMemberVariable.m_IsSupportInExpressUseStaticMetaMemeberFunction”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“MetaMemberVariable.m_IsSupportInExpressUseStaticMetaVariable”已被赋值，但从未使用过它的值
        private bool m_IsSupportInExpressUseStaticMetaVariable = true;     //是否在成员中支持静态变量
#pragma warning restore CS0414 // 字段“MetaMemberVariable.m_IsSupportInExpressUseStaticMetaVariable”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“MetaMemberVariable.m_IsSupportInExpressUseCurrentClassNotStaticMemberMetaVariable”已被赋值，但从未使用过它的值
        private bool m_IsSupportInExpressUseCurrentClassNotStaticMemberMetaVariable = true;  //是否支持在表达式中使用本类或父类中的非静态变量
#pragma warning restore CS0414 // 字段“MetaMemberVariable.m_IsSupportInExpressUseCurrentClassNotStaticMemberMetaVariable”已被赋值，但从未使用过它的值

        public static int s_ConstLevel = 10000000;
        public static int s_IsHaveRetStaticLevel = 100000000;
        public static int s_NoHaveRetStaticLevel = 200000000;
        public static int s_DefineMetaTypeLevel = 1000000000;
        public static int s_ExpressLevel = 1500000000;
        public MetaConstExpressNode constExpressNode => m_Express as MetaConstExpressNode;

        //public MetaMemberEnum(MetaClass mc, string _name)
        //{
        //    m_Name = _name;
        //    m_FromType = EFromType.Manual;
        //    m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
        //    m_IsInnerDefine = true;
        //    m_VariableFrom = EVariableFrom.Static;

        //    SetOwnerMetaClass(mc);
        //} 
        //public MetaMemberEnum( MetaClass ownerMc, string _name, MetaTemplate mt )
        //{
        //    m_Name = _name;
        //    m_FromType = EFromType.Manual;
        //    m_DefineMetaType = new MetaType( mt );
        //    m_IsInnerDefine = true;
        //    m_VariableFrom = EVariableFrom.Static;

        //    SetOwnerMetaClass(ownerMc);
        //}
        //public MetaMemberEnum(MetaClass mc, string _name, MetaClass _defineTypeClass )
        //{
        //    m_Name = _name;
        //    m_IsInnerDefine = true;
        //    m_FromType = EFromType.Manual;
        //    m_DefineMetaType = new MetaType(_defineTypeClass);
        //    m_DefineMetaType.SetMetaClass(_defineTypeClass);
        //    m_VariableFrom = EVariableFrom.Static;

        //    SetOwnerMetaClass(mc);
        //}
        public MetaMemberEnum( MetaClass mc, FileMetaMemberVariable fmmv )
        {
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            AddPingToken( fmmv.nameToken );
            m_Index = mc.metaMemberVariableDict.Count;
            m_FromType = EFromType.Code;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsStatic = true;// enum 成员全部为static
            m_VariableFrom = EVariableFrom.Static;
            if (fmmv.staticToken != null )
            {
                Log.AddInStructMeta(EError.None, "Error ENum中，不允许有静态关键字，而是全部是静态关键字!!");
            }
            if (m_FileMetaMemeberVariable.permissionToken != null)
            {
                Log.AddInStructMeta(EError.None, "Error Enum中，不允许使用public/private等权限关键字!!");
                m_Permission = CompilerUtil.GetPerMissionByString(m_FileMetaMemeberVariable.permissionToken?.lexeme.ToString());
            }

            SetOwnerMetaClass(mc);
        }
        //public MetaMemberEnum(MetaGenTemplateClass mtc, MetaMemberEnum mmv, List<MetaGenTemplate> mgt) : base(mmv)
        //{
        //    m_MetaGenTemplateList = mgt;
        //    m_Name = mmv.m_Name;
        //    m_IsInnerDefine = mmv.m_IsInnerDefine;
        //    m_FromType = mmv.m_FromType;
        //    m_DefineMetaType = mmv.m_DefineMetaType;
        //    m_VariableFrom = EVariableFrom.Static;
        //    m_PintTokenList = mmv.m_PintTokenList;

        //    SetOwnerMetaClass(mtc);
        //}
        public override void ParseDefineMetaType()
        {
            if (m_FileMetaMemeberVariable != null)
            {
                if( m_FileMetaMemeberVariable.express != null )
                {
                    CreateExpressParam cep = new CreateExpressParam()
                    {
                        fme = m_FileMetaMemeberVariable.express,
                        metaType = m_DefineMetaType,
                        ownerMetaClass = m_OwnerMetaClass,
                        equalMetaVariable = this,
                        ownerMBS = m_OwnerMetaBlockStatements,
                        parsefrom = EParseFrom.MemberVariableExpress
                    };
                    m_Express = ExpressManager.CreateExpressNodeByCEP(cep);

                    if (m_Express == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error 没有解析到Express的内容 在MetaMemberData 里边 372");
                    }
                }
            }
        }
        public override bool ParseMetaExpress()
        {
            if(m_Express != null )
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
            }
            return true;
        }
        public void SetExpress( MetaConstExpressNode mcen )
        {
            m_Express = mcen;
        }        
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);

            sb.Append(permission.ToFormatString() + " ");
            if( isConst )
            {
                sb.Append("const ");
            }
            if( isStatic )
            {
                sb.Append("static ");
            }
            sb.Append(base.ToFormatString());
            if(m_Express != null )
            {
                sb.Append(" = ");
                sb.Append(m_Express.ToFormatString());
            }
            sb.Append(";");

            return sb.ToString();
        }
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_FileMetaMemeberVariable.nameToken.sourceBeginLine + " 与父类的Token位置: "
                    + m_FileMetaMemeberVariable.nameToken.sourceBeginLine.ToString());

            return sb.ToString();
        }
        MetaExpressNode CreateExpressNodeInClassMetaVariable()
        {
            var express = m_FileMetaMemeberVariable?.express;
            if (express == null) return null;

            var root = express.root;
            if (root == null)
                return null;
            if (root.left == null && root.right == null)
            {
                var fmpt = root as FileMetaParTerm;
                var fmct = root as FileMetaCallTerm;
                var fmbt = root as FileMetaBraceTerm;
                if (m_DefineMetaType != null )
                {
                    if (fmpt != null)            // for example: Class1 obj = (1,2,3,4);
                    {
                        if( m_IsSupportConstructionFunctionOnlyParType )
                        {
                            MetaInputParamCollection mpc = new MetaInputParamCollection(fmpt, ownerMetaClass, null);

                            MetaMemberFunction mmf = m_DefineMetaType.GetMetaMemberConstructFunction(mpc);

                            if (mmf == null) return null;

                            MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(fmpt, m_DefineMetaType, ownerMetaClass, mmf.metaBlockStatements );
                            if (mnoen != null)
                            {
                                return mnoen;
                            }
                        }
                        else
                        {
                            Log.AddInStructMeta(EError.None, "Error 现在配置中，不支持成员变量中使用类的()构造方式!!");
                        }
                    }
                    else if (fmbt != null)
                    {
                        MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(fmbt, m_DefineMetaType, ownerMetaClass, null, null);
                        return mnoen;
                    }
                    else if (fmct != null)
                    {
                        if( fmct.callLink.callNodeList.Count > 0 )
                        {
                            var finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                            if( finalNode.fileMetaBraceTerm != null && !m_IsSupportConstructionFunctionConnectBraceType )
                            {
                                Log.AddInStructMeta(EError.None, "Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                                return null;
                            }
                        }
                        AllowUseSettings auc = new AllowUseSettings();
                        auc.useNotConst = false;
                        auc.useNotStatic = true;

                        //MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByCall(fmct, m_DefineMetaType, ownerMetaClass, null, auc );
                        //if( mnoen != null )
                        //{
                        //    return mnoen;
                        //}
                    }
                }
                else
                {
                    if(fmpt != null )
                    {
                        Log.AddInStructMeta(EError.None, "Error 在类没有定义的变量中，不允许 使用()的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmbt != null)
                    {
                        Log.AddInStructMeta(EError.None, "Error 在类没有定义的变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmct != null)
                    {
                        if (fmct.callLink.callNodeList.Count > 0)
                        {
                            var finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                            if (finalNode.fileMetaBraceTerm != null && !m_IsSupportConstructionFunctionConnectBraceType)
                            {
                                Log.AddInStructMeta(EError.None, "Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                                return null;
                            }
                        }
                        AllowUseSettings auc = new AllowUseSettings();
                        auc.useNotConst = false;
                        auc.useNotStatic = true;
                        //MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByCall(fmct, m_DefineMetaType, ownerMetaClass, null, auc );
                        //if (mnoen != null)
                        //{
                        //    return mnoen;
                        //}
                    }
                }
                CreateExpressParam cep = new CreateExpressParam();
                cep.ownerMetaClass = ownerMetaClass;
                cep.metaType = m_DefineMetaType;
                cep.fme = root;
                return ExpressManager.CreateExpressNode(cep);
            }

            //MetaExpressNode mn = ExpressManager.CreateExpressNode(ownerMetaClass, null, m_DefineMetaType, root);

            //AllowUseSettings auc2 = new AllowUseSettings();
            //auc2.useNotConst = isConst;
            //auc2.useNotStatic = !isStatic;
            //mn.Parse(auc2);

            //return mn;
            return null;
        }
        //public override string ToFormatString()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    switch (m_FileMetaMemeberData.DataType)
        //    {
        //        case FileMetaMemberData.EMemberDataType.NameClass:
        //            {
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.AppendLine(m_Name);
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.AppendLine("{");
        //                foreach (var v in m_MetaMemberDataDict)
        //                {
        //                    sb.AppendLine(v.Value.ToFormatString());
        //                }
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append("}");

        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.Array:
        //            {
        //                int i = 0;
        //                for (i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append(m_Name + " = [");
        //                i = 0;
        //                foreach (var v in m_MetaMemberDataDict)
        //                {
        //                    sb.Append(v.Value.ToFormatString());
        //                    if (i < m_MetaMemberDataDict.Count - 1)
        //                        sb.Append(",");
        //                    i++;
        //                }
        //                sb.Append("]");
        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.NoNameClass:
        //            {
        //                sb.AppendLine();
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.AppendLine("{");
        //                foreach (var v in m_MetaMemberDataDict)
        //                {
        //                    sb.AppendLine(v.Value.ToFormatString());
        //                }
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append("}");
        //                //if( m_End )
        //                //{
        //                //    sb.AppendLine();
        //                //}
        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.KeyValue:
        //            {
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append(m_Name + " = " + m_Express.ToFormatString() + ";");
        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.Value:
        //            {
        //                sb.Append(m_Express.ToFormatString());
        //            }
        //            break;
        //    }
        //    return sb.ToString();
        //}
        //------------------------------------end-----------------------------------------------//
    }
}
