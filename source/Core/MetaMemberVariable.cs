//****************************************************************************
//  File:      MetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: class's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static SimpleLanguage.Core.ExpressManager;
using static SimpleLanguage.Core.MetaBraceOrBracketStatementsContent;

namespace SimpleLanguage.Core
{
    public enum EFromType : byte
    {
        Code = 1,         //写的.s代码
        Manual = 2,         //手动，通过c#代码
        CodeAndManual = 3,  //
        CSharp= 4,              //通过c#的dll文件，或者是编译完的代码的识别
        ManualAndCSharp = 6,     //手动注入的c#代码进逻辑解析
        All = 7
    }
    public partial class MetaMemberVariable : MetaVariable, IComparable<MetaMemberVariable>
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
        private bool m_IsEnumValue = false;
        private bool m_IsInnerDefine = false;
        private List<MetaGenTemplate> m_MetaGenTemplateList = null;

        private bool m_IsSupportConstructionFunctionOnlyBraceType = false;  //是否支持构造函数使用 仅{}形式    Class1{ a = {} } 不支持
        private bool m_IsSupportConstructionFunctionConnectBraceType = true;  //是否支持构造函数名称后边加{}形式    Class1{ a = Class2(){} } 不支持
        private bool m_IsSupportConstructionFunctionOnlyParType = true; //是否支持构造函数使用 仅()形式    Class1{ a = () } 不支持
        private bool m_IsSupportInExpressUseStaticMetaMemeberFunction = true;   //是否在成员支持静态函数的
        private bool m_IsSupportInExpressUseStaticMetaVariable = true;     //是否在成员中支持静态变量
        private bool m_IsSupportInExpressUseCurrentClassNotStaticMemberMetaVariable = true;  //是否支持在表达式中使用本类或父类中的非静态变量

        public static int s_ConstLevel = 10000000;
        public static int s_IsHaveRetStaticLevel = 100000000;
        public static int s_NoHaveRetStaticLevel = 200000000;
        public static int s_DefineMetaTypeLevel = 1000000000;
        public static int s_ExpressLevel = 1500000000;
        public MetaConstExpressNode constExpressNode => m_Express as MetaConstExpressNode;

        private EMemberDataType m_MemberDataType = EMemberDataType.None;

        protected Dictionary<string, MetaMemberData> m_MetaMemberDataDict = new Dictionary<string, MetaMemberData>();


        public MetaMemberVariable( MetaMemberVariable mmv ) : base( mmv )
        {
            m_FromType = EFromType.Manual;
            m_DefineMetaType = mmv.m_DefineMetaType;
            m_IsInnerDefine = mmv.m_IsInnerDefine;
            m_Express = mmv.m_Express;
            m_VariableFrom = EVariableFrom.Member;
        }
        public MetaMemberVariable(MetaClass mc, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.Member;

            SetOwnerMetaClass(mc);
        } 
        public MetaMemberVariable( MetaClass ownerMc, string _name, MetaTemplate mt )
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType( mt );
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.Member;

            SetOwnerMetaClass(ownerMc);
        }
        //public MetaMemberVariable(MetaMemberVariable parentNode, FileMetaMemberVariable fmmv, int _index)
        //{
        //    m_Name = m_FileMetaMemeberVariable.name;
        //    m_FromType = EFromType.Manual;
        //    m_Index = _index;
        //    m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
        //    SetOwnerMetaClass(parentNode.ownerMetaClass);
        //    isConst = parentNode.isConst;

        //    m_FileMetaMemeberVariable = fmmv;
        //    m_IsEnumValue = false;
        //    m_Name = fmmv.name;
        //    m_Index = _index;
        //    fmmv.SetMetaMemberVariable(this);
        //    m_FromType = EFromType.Code;
        //    m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
        //    isStatic = m_FileMetaMemeberVariable?.staticToken != null;
        //    if (isStatic)
        //    {
        //        Console.WriteLine("Error ENum中，不允许有静态关键字，而是全部是静态关键字!!");
        //    }
        //    if (m_FileMetaMemeberVariable.permissionToken != null)
        //    {
        //        permission = CompilerUtil.GetPerMissionByString(m_FileMetaMemeberVariable.permissionToken?.lexeme.ToString());
        //    }

        //    //SetOwnerMetaClass(mc);

        //    Parse();
        //}
        //public MetaMemberData GetMemberDataByName(string name)
        //{
        //    if (m_MetaMemberDataDict.ContainsKey(name))
        //    {
        //        return m_MetaMemberDataDict[name];
        //    }
        //    return null;
        //}
        //public bool AddMetaMemberData(MetaMemberData mmd)
        //{
        //    if (m_MetaMemberDataDict.ContainsKey(mmd.name))
        //    {
        //        return false;
        //    }
        //    m_MetaMemberDataDict.Add(mmd.name, mmd);
        //    return true;
        //}
        //public override void Parse()
        //{
        //    if (m_FileMetaMemeberData != null)
        //    {
        //        switch (m_FileMetaMemeberData.DataType)
        //        {
        //            case FileMetaMemberData.EMemberDataType.NameClass:    // data Data{ childData{} }
        //                {
        //                    m_Name = m_FileMetaMemeberData.name;
        //                    m_MemberDataType = EMemberDataType.MemberData;
        //                }
        //                break;
        //            case FileMetaMemberData.EMemberDataType.Array:      // data Data{ childArray[  ] }
        //                {
        //                    m_Name = m_FileMetaMemeberData.name;
        //                    m_MemberDataType = EMemberDataType.MemberData;
        //                }
        //                break;
        //            case FileMetaMemberData.EMemberDataType.NoNameClass:   // data Data{ childArray{ {}, {} } }
        //                {
        //                    m_Name = m_Index.ToString();
        //                    m_MemberDataType = EMemberDataType.MemberData;
        //                }
        //                break;
        //            case FileMetaMemberData.EMemberDataType.KeyValue:  // data Data{ childArray{ a = 1; b = 2 } }
        //                {
        //                    m_Name = m_FileMetaMemeberData.name;
        //                    m_MemberDataType = EMemberDataType.ConstValue;
        //                    m_Express = new MetaConstExpressNode(m_FileMetaMemeberData.fileMetaConstValue);
        //                }
        //                break;
        //            case FileMetaMemberData.EMemberDataType.Value:
        //                {
        //                    m_Name = m_Index.ToString();
        //                    m_MemberDataType = EMemberDataType.ConstValue;
        //                    m_Express = new MetaConstExpressNode(m_FileMetaMemeberData.fileMetaConstValue);
        //                }
        //                break;
        //            case FileMetaMemberData.EMemberDataType.Data:
        //                {
        //                    m_Name = m_FileMetaMemeberData.name;
        //                    m_MemberDataType = EMemberDataType.MemberData;
        //                    m_Express = new MetaCallLinkExpressNode(m_FileMetaMemeberData.fileMetaCallTermValue.callLink, null, null);
        //                    m_Express.Parse(new AllowUseSettings());
        //                    m_DefineMetaType = m_Express.GetReturnMetaDefineType();
        //                    if (m_DefineMetaType == null)
        //                    {
        //                        Console.WriteLine("Error 在生成Data时，没有找到." + m_FileMetaMemeberData.fileMetaCallTermValue.ToTokenString());
        //                        return;
        //                    }
        //                    var mc = m_Express.GetReturnMetaClass();
        //                    MetaData refMD = mc as MetaData;
        //                    if (refMD != null)
        //                    {
        //                        CopyByMetaData(refMD);
        //                    }
        //                    else
        //                    {
        //                        Console.WriteLine("Error 在生成Data时，发现不是Data数据!!");
        //                    }
        //                }
        //                break;
        //        }
        //    }
        //}
        //public MetaMemberData Copy()
        //{
        //    var newMMD = new MetaMemberData(m_OwnerMetaClass as MetaData);
        //    newMMD.m_Name = m_Name;
        //    newMMD.m_MemberDataType = m_MemberDataType;
        //    if (m_MemberDataType == EMemberDataType.MemberData)
        //    {
        //        foreach (var v in m_MetaMemberDataDict)
        //        {
        //            newMMD.AddMetaMemberData(v.Value.Copy());
        //        }
        //    }
        //    else if (m_MemberDataType == EMemberDataType.ConstValue)
        //    {
        //        newMMD.m_Express = m_Express;
        //    }
        //    return newMMD;
        //}
        //public void CopyByMetaData(MetaData md)
        //{
        //    MetaData curMD = m_OwnerMetaClass as MetaData;
        //    foreach (var v in md.metaMemberDataDict)
        //    {
        //        if (v.Value.IsIncludeMetaData(curMD))
        //        {
        //            Console.WriteLine("Error 当前有循环引用数量现象，请查正!!" + md.allName);
        //            continue;
        //        }
        //        var newMMD = v.Value.Copy();
        //        this.AddMetaMemberData(newMMD);
        //    }
        //}
        //public bool IsIncludeMetaData(MetaData md)
        //{
        //    if (md == null) return false;

        //    MetaData belongMD = m_OwnerMetaClass as MetaData;
        //    if (belongMD != null)
        //    {
        //        if (belongMD == md)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}
        public MetaMemberVariable(MetaClass mc, string _name, MetaClass _defineTypeClass )
        {
            m_Name = _name;
            m_IsInnerDefine = true;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(_defineTypeClass);
            m_DefineMetaType.SetMetaClass(_defineTypeClass);
            m_VariableFrom = EVariableFrom.Member;

            SetOwnerMetaClass(mc);
        }
        public MetaMemberVariable( MetaClass mc, FileMetaMemberVariable fmmv, bool isEnum = false )
        {
            m_FileMetaMemeberVariable = fmmv;
            m_IsEnumValue = isEnum;
            m_Name = fmmv.name;
            AddPingToken( fmmv.nameToken );
            m_Index = mc.metaMemberVariableDict.Count;
            fmmv.SetMetaMemberVariable(this);
            m_FromType = EFromType.Code;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            isStatic = m_FileMetaMemeberVariable?.staticToken != null;
            m_VariableFrom = EVariableFrom.Member;
            if ( isStatic && isEnum )
            {
                Console.WriteLine("Error ENum中，不允许有静态关键字，而是全部是静态关键字!!");
            }
            if (m_FileMetaMemeberVariable.permissionToken != null)
            {
                if( isEnum )
                {
                    Console.WriteLine("Error Enum中，不允许使用public/private等权限关键字!!");
                }
                permission = CompilerUtil.GetPerMissionByString(m_FileMetaMemeberVariable.permissionToken?.lexeme.ToString());
            }

            SetOwnerMetaClass(mc);
        }
        public MetaMemberVariable(MetaGenTemplateClass mtc, MetaMemberVariable mmv, List<MetaGenTemplate> mgt) : base(mmv)
        {
            m_MetaGenTemplateList = mgt;
            m_Name = mmv.m_Name;
            m_IsInnerDefine = mmv.m_IsInnerDefine;
            m_FromType = mmv.m_FromType;
            m_DefineMetaType = mmv.m_DefineMetaType;
            m_VariableFrom = EVariableFrom.Member;
            m_PintTokenList = mmv.m_PintTokenList;

            SetOwnerMetaClass(mtc);
        }
        public override void ParseName()
        {
            if (m_FileMetaMemeberVariable != null)
            {
                m_Name = m_FileMetaMemeberVariable.name;
            }
        }
        public override void ParseDefineMetaType()
        {
            if (m_FileMetaMemeberVariable != null)
            {
                if (m_FileMetaMemeberVariable.classDefineRef != null)
                {
                    m_DefineMetaType = new MetaType(m_FileMetaMemeberVariable.classDefineRef, ownerMetaClass);
                }
                else
                {

                }
            }
        }
        public void UpdateGenMemberVariable()
        {
            if (m_MetaGenTemplateList != null)
            {
                for (int i = 0; i < m_MetaGenTemplateList.Count; i++)
                {
                    MetaGenTemplate mgt = m_MetaGenTemplateList[i];
                    if (mgt.name == m_DefineMetaType.metaTemplate.name)
                    {
                        m_DefineMetaType = mgt.metaType;
                    }
                }
            }
        }
        public virtual int CalcParseLevelBeCall(int level)
        {
            parseLevel = level - 1;

            return parseLevel;
        }
        public virtual void CalcParseLevel()
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
                ExpressManager.instance.CalcParseLevel(parseLevel, m_Express);
            }
        }
        public void CreateExpress()
        {
            if(m_FileMetaMemeberVariable?.DataType == FileMetaMemberVariable.EMemberDataType.Array )
            {
                m_Express = CreateExpressNodeInClassMetaVariable();
                //ParseChildMemberData();
            }
            else
            {
                m_Express = CreateExpressNodeInClassMetaVariable();
            }
        }        
        public void CalcReturnType()
        {
            if (m_Express != null)
            {
                m_Express.CalcReturnType();
                var enode = SimulateExpressRun(m_Express);
                if (enode != null)
                {
                    m_Express = enode;
                }
                CalcDefineClassType();
            }
            else
            {
                m_Express = m_DefineMetaType.GetDefaultExpressNode();
            }
            if (m_Express == null && m_DefineMetaType == null)
            {
                Console.WriteLine("Error 表达式为空 或者 表达示必须有返回值");
            }
        }
        public int CompareTo(MetaMemberVariable mmv)
        {
            if (this.parseLevel > mmv.parseLevel)
                return 1;
            else
                return -1;
        }
        void CalcDefineClassType()
        {
            //var metaFunction = m_OwnerMetaBlockStatements?.ownerMetaFunction;
            string defineName = m_Name;
            if (!m_DefineMetaType.isDefineMetaClass)
            {
                if (m_Express != null)
                {
                    MetaConstExpressNode constExpressNode = m_Express as MetaConstExpressNode;
                    bool isCheckReturnType = true;
                    if (constExpressNode != null)
                    {
                        if (constExpressNode.eType == EType.Null)
                        {
                            isCheckReturnType = false;
                        }
                    }
                    if (isCheckReturnType)
                    {
                        var dmct = m_Express.GetReturnMetaDefineType();
                        if (dmct != null)
                        {
                            if( dmct.metaClass == ownerMetaClass )
                            {
                                Console.WriteLine("Error 自己类内部不允许包含 自己的实体，必须赋值为null");
                                return;
                            }
                            m_DefineMetaType = dmct;
                        }
                    }
                }
            }
            else
            {
                if (m_Express != null)
                {                    
                    ClassManager.EClassRelation relation = ClassManager.EClassRelation.No;
                    MetaConstExpressNode constExpressNode = m_Express as MetaConstExpressNode;
                    MetaClass curClass = m_DefineMetaType.metaClass;

                    MetaClass compareClass = null;
                    MetaType expressRetMetaDefineType = null;
                    if (constExpressNode != null && constExpressNode.eType == EType.Null)
                    {
                        relation = ClassManager.EClassRelation.Same;
                    }
                    else
                    {
                        expressRetMetaDefineType = m_Express.GetReturnMetaDefineType();
                        if (expressRetMetaDefineType == null)
                        {
                            Console.WriteLine("Error 表达式中返回定义类型为空 " + m_Express.ToTokenString());
                            return;
                        }

                        compareClass = expressRetMetaDefineType.metaClass;
                        relation = ClassManager.ValidateClassRelationByMetaClass(curClass, compareClass);
                    }

                    StringBuilder sb = new StringBuilder();
                    //sb.Append("Warning 在类: " + metaFunction?.ownerMetaClass.allName + " 函数: " + metaFunction?.name + "中  ");
                    if (curClass != null)
                    {
                        sb.Append(" 定义类 : " + curClass.allName);
                    }
                    if (defineName != null)
                    {
                        sb.Append(" 名称为: " + defineName?.ToString());
                    }
                    sb.Append("与后边赋值语句中 ");
                    if (compareClass != null)
                        sb.Append("表达式类为: " + compareClass.allName);
                    if (relation == ClassManager.EClassRelation.No)
                    {
                        sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                        Console.WriteLine(sb.ToString());
                    }
                    else if (relation == ClassManager.EClassRelation.Same)
                    {
                        if( !(constExpressNode != null && constExpressNode.eType == EType.Null ) )
                        {
                            if (expressRetMetaDefineType.metaClass == ownerMetaClass)
                            {
                                Console.WriteLine("Error 自己类内部不允许包含 自己的实体，必须赋值为null");
                                return;
                            }
                            SetMetaDefineType(expressRetMetaDefineType);
                        }
                    }
                    else if (relation == ClassManager.EClassRelation.Parent)
                    {
                        sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                        Console.WriteLine(sb.ToString());
                    }
                    else if (relation == ClassManager.EClassRelation.Child)
                    {
                        if (compareClass != null)
                        {
                            if (expressRetMetaDefineType.metaClass == ownerMetaClass)
                            {
                                Console.WriteLine("Error 自己类内部不允许包含 自己的实体，必须赋值为null");
                                return;
                            }
                            SetMetaDefineType(expressRetMetaDefineType);
                        }
                    }
                    else
                    {
                        sb.Append("表达式错误，或者是定义类型错误");
                        Console.WriteLine(sb.ToString());
                    }
                }
            }
        }
        public MetaExpressNode SimulateExpressRun(MetaExpressNode node)
        {
            MetaExpressNode newnode = node;
            if ( node is MetaCallLinkExpressNode )
            {
                MetaCallLinkExpressNode mcen = node as MetaCallLinkExpressNode;
                if( mcen != null )
                {
                    newnode = mcen.ConvertConstExpressNode();
                }
            }
            else if( node is MetaOpExpressNode )
            {
                MetaOpExpressNode moen = node as MetaOpExpressNode;
                var left = SimulateExpressRun(moen.left);
                var right = SimulateExpressRun(moen.right);
                if (left != null)
                {
                    moen.SetLeft( left );
                }
                if (right != null)
                {
                    moen.SetRight( right );
                }
                newnode = node;
            }
            else if( node is MetaUnaryOpExpressNode )
            {
                MetaUnaryOpExpressNode muoen = node as MetaUnaryOpExpressNode;
                var v = SimulateExpressRun(muoen.value);
                if (v != null)
                {
                    muoen.SetValue( v );
                }
                newnode = node;
            }
            return newnode;
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

                            MetaMethodCall mfc = new MetaMethodCall(ownerMetaClass, mmf, mpc );

                            MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(fmpt, m_DefineMetaType, ownerMetaClass, null, mfc);
                            if (mnoen != null)
                            {
                                return mnoen;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error 现在配置中，不支持成员变量中使用类的()构造方式!!");
                        }
                    }
                    else if (fmbt != null)
                    {
                        if( m_IsEnumValue )
                        {
                            MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(fmbt, m_DefineMetaType, ownerMetaClass, null, null );
                            return mnoen;
                        }
                        else
                        {
                            if (m_IsSupportConstructionFunctionOnlyBraceType)
                            {
                            }
                            else
                            {
                                Console.WriteLine("Error 在类变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                            }
                            return null;
                        }
                    }
                    else if (fmct != null)
                    {
                        if( fmct.callLink.callNodeList.Count > 0 )
                        {
                            var finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                            if( finalNode.fileMetaBraceTerm != null && !m_IsSupportConstructionFunctionConnectBraceType )
                            {
                                Console.WriteLine("Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                                return null;
                            }
                        }
                        AllowUseSettings auc = new AllowUseSettings();
                        auc.useNotConst = false;
                        auc.useNotStatic = true;

                        MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByCall(fmct, m_DefineMetaType, ownerMetaClass, null, auc );
                        if( mnoen != null )
                        {
                            return mnoen;
                        }
                    }
                }
                else
                {
                    if(fmpt != null )
                    {
                        Console.WriteLine("Error 在类没有定义的变量中，不允许 使用()的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmbt != null)
                    {
                        Console.WriteLine("Error 在类没有定义的变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmct != null)
                    {
                        if (fmct.callLink.callNodeList.Count > 0)
                        {
                            var finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                            if (finalNode.fileMetaBraceTerm != null && !m_IsSupportConstructionFunctionConnectBraceType)
                            {
                                Console.WriteLine("Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                                return null;
                            }
                        }
                        AllowUseSettings auc = new AllowUseSettings();
                        auc.useNotConst = false;
                        auc.useNotStatic = true;
                        MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByCall(fmct, m_DefineMetaType, ownerMetaClass, null, auc );
                        if (mnoen != null)
                        {
                            return mnoen;
                        }
                    }
                }
                CreateExpressParam cep = new CreateExpressParam();
                cep.metaClass = ownerMetaClass;
                cep.metaType = m_DefineMetaType;
                cep.fme = root;
                return ExpressManager.instance.CreateExpressNode(cep);
            }

            MetaExpressNode mn = ExpressManager.instance.VisitFileMetaExpress(ownerMetaClass, null, m_DefineMetaType, root);

            AllowUseSettings auc2 = new AllowUseSettings();
            auc2.useNotConst = isConst;
            auc2.useNotStatic = !isStatic;
            mn.Parse(auc2);

            return mn;
        }



        //-----------------------------data------------------------------------------------//       
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
                    //if (m_MetaMemberDataDict.ContainsKey(name))
                    //{
                    //    return m_MetaMemberDataDict[name].GetString(name);
                    //}
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
        public void ParseChildMemberData()
        {
            if( m_FileMetaMemeberVariable == null )
            {
                return;
            }

            int count = m_FileMetaMemeberVariable.fileMetaMemberVariable.Count;

            if(m_FileMetaMemeberVariable.DataType == FileMetaMemberVariable.EMemberDataType.Array )
            {
                List<MetaDynamicClass> list = new List<MetaDynamicClass>();

                for (int i = 0; i < count; i++)
                {
                    var fmmv = m_FileMetaMemeberVariable.fileMetaMemberVariable[i];

                    MetaDynamicClass mdc = new MetaDynamicClass(fmmv.GetHashCode().ToString());
                    list.Add(mdc);

                    for ( int j = 0; j < fmmv.fileMetaMemberVariable.Count; j++ )
                    {
                        MetaMemberVariable mmv = new MetaMemberVariable(mdc, fmmv.fileMetaMemberVariable[j] as FileMetaMemberVariable, false);

                        mmv.ParseChildMemberData();
                        mmv.CalcDefineClassType();

                    }
                    
                }
                m_Express = new MetaNewObjectExpressNode(this.m_OwnerMetaClass, list);
            }
            else if( m_FileMetaMemeberVariable.DataType == FileMetaMemberVariable.EMemberDataType.ConstVariable )
            {
                CreateExpress();
            }
            else if( m_FileMetaMemeberVariable.DataType == FileMetaMemberVariable.EMemberDataType.KeyValue )
            {

            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var fmmv = m_FileMetaMemeberVariable.fileMetaMemberVariable[i];

                    MetaDynamicClass mdc = new MetaDynamicClass(i.GetHashCode().ToString());

                    MetaMemberVariable mmd = new MetaMemberVariable(mdc, fmmv, false);
                    if (AddMetaVariable(mmd))
                    {
                        this.AddMetaBase(mmd.name, mmd);
                    }
                    else
                    {
                        Console.WriteLine("Error 命名有重名!!");
                    }
                }
            }
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
