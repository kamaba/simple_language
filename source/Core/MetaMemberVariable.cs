﻿//****************************************************************************
//  File:      MetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: class's memeber variable meta data
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Text;

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
        public MetaExpressNode express => m_Express;
        public int parseLevel { get; set; } = -1;

        private EFromType m_FromType = EFromType.Code;
        private FileMetaMemberVariable m_FileMetaMemeberVariable;
        private MetaExpressNode m_Express = null;

        private bool m_IsSupportConstructionFunctionOnlyBraceType = false;  //是否支持构造函数使用 仅{}形式    Class1{ a = {} } 不支持
        private bool m_IsSupportConstructionFunctionConnectBraceType = false;  //是否支持构造函数名称后边加{}形式    Class1{ a = Class2(){} } 不支持
        private bool m_IsSupportConstructionFunctionOnlyParType = true; //是否支持构造函数使用 仅()形式    Class1{ a = () } 不支持
        private bool m_IsSupportInExpressUseStaticMetaMemeberFunction = true;   //是否在成员支持静态函数的
        private bool m_IsSupportInExpressUseStaticMetaVariable = true;     //是否在成员中支持静态变量
        private bool m_IsSupportInExpressUseCurrentClassNotStaticMemberMetaVariable = true;  //是否支持在表达式中使用本类或父类中的非静态变量

        public static int s_ConstLevel = 10000000;
        public static int s_IsHaveRetStaticLevel = 100000000;
        public static int s_NoHaveRetStaticLevel = 200000000;
        public static int s_DefineMetaTypeLevel = 1000000000;
        public static int s_ExpressLevel = 1500000000;

        public MetaMemberVariable(MetaClass mc, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);

            SetOwnerMetaClass(mc);
        }
        public MetaMemberVariable(MetaClass mc, string _name, MetaClass _defineTypeClass )
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(_defineTypeClass);
            m_DefineMetaType.SetMetaClass(_defineTypeClass);

            SetOwnerMetaClass(mc);
        }
        public MetaMemberVariable( MetaClass mc, FileMetaMemberVariable fmmv )
        {
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            fmmv.SetMetaMemberVariable(this);
            m_FromType = EFromType.Code;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            isStatic = m_FileMetaMemeberVariable?.staticToken != null;
            if (m_FileMetaMemeberVariable.permissionToken != null)
            {
                permission = CompilerUtil.GetPerMissionByString(m_FileMetaMemeberVariable.permissionToken?.lexeme.ToString());
            }

            SetOwnerMetaClass(mc);
        }
        public override void Parse()
        {
            if (m_FileMetaMemeberVariable != null)
            {
                if (m_FileMetaMemeberVariable.classDefineRef != null)
                {
                    m_DefineMetaType = new MetaType(m_FileMetaMemeberVariable.classDefineRef, ownerMetaClass );
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
            m_Express = CreateExpressNodeInClassMetaVariable();
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
            var metaFunction = m_OwnerMetaBlockStatements?.ownerMetaFunction;
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
                    var expressRetMetaDefineType = m_Express.GetReturnMetaDefineType();
                    if( expressRetMetaDefineType == null )
                    {
                        Console.WriteLine("Error 表达式中返回定义类型为空 " + m_Express.ToTokenString());
                        return;
                    }
                    ClassManager.EClassRelation relation = ClassManager.EClassRelation.No;
                    MetaConstExpressNode constExpressNode = m_Express as MetaConstExpressNode;
                    MetaClass curClass = m_DefineMetaType.metaClass;

                    MetaClass compareClass = null;
                    if (constExpressNode != null && constExpressNode.eType == EType.Null)
                    {
                        relation = ClassManager.EClassRelation.Same;
                    }
                    else
                    {
                        compareClass = expressRetMetaDefineType.metaClass;
                        relation = ClassManager.ValidateClassRelationByMetaClass(curClass, compareClass);
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append("Warning 在类: " + metaFunction?.ownerMetaClass.allName + " 函数: " + metaFunction?.name + "中  ");
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
                        bool isSame = true;
                        if ( m_DefineMetaType.isUseInputTemplate )
                        {
                            if (m_DefineMetaType.inputTemplateCollection.metaTemplateParamsList.Count == expressRetMetaDefineType.inputTemplateCollection.metaTemplateParamsList.Count)
                            {
                                for (int i = 0; i < m_DefineMetaType.inputTemplateCollection.metaTemplateParamsList.Count; i++)
                                {
                                    var itp = m_DefineMetaType.inputTemplateCollection.metaTemplateParamsList[i];
                                    var etp = expressRetMetaDefineType.inputTemplateCollection.metaTemplateParamsList[i];
                                    if (itp != etp )
                                    {
                                        isSame = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isSame)
                        {
                            if( expressRetMetaDefineType.metaClass == ownerMetaClass )
                            {
                                Console.WriteLine("Error 自己类内部不允许包含 自己的实体，必须赋值为null");
                                return;
                            }
                            SetMetaDefineType(expressRetMetaDefineType);
                        }
                        else
                        {
                            relation = ClassManager.EClassRelation.No;
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
            MetaExpressNode newnode = null;
            if ( node is MetaCallExpressNode )
            {
                MetaCallExpressNode mcen = node as MetaCallExpressNode;
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

                            MetaFunctionCall mfc = new MetaFunctionCall(ownerMetaClass, mmf, mpc );

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
                        if (m_IsSupportConstructionFunctionOnlyBraceType)
                        {
                        }
                        else
                        {
                            Console.WriteLine("Error 在类变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        }
                        return null;
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
                        AllowUseConst auc = new AllowUseConst();
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
                        AllowUseConst auc = new AllowUseConst();
                        auc.useNotConst = false;
                        auc.useNotStatic = true;
                        MetaNewObjectExpressNode mnoen = MetaNewObjectExpressNode.CreateNewObjectExpressNodeByCall(fmct, m_DefineMetaType, ownerMetaClass, null, auc );
                        if (mnoen != null)
                        {
                            return mnoen;
                        }
                    }
                }
                return ExpressManager.instance.CreateExpressNode(ownerMetaClass, null, m_DefineMetaType, root);
            }

            MetaExpressNode mn = ExpressManager.instance.VisitFileMetaExpress(ownerMetaClass, null, m_DefineMetaType, root);

            AllowUseConst auc2 = new AllowUseConst();
            auc2.useNotConst = isConst;
            auc2.useNotStatic = !isStatic;
            mn.Parse(auc2);

            return mn;
        }
        
    }
}