//****************************************************************************
//  File:      MetaAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using static SimpleLanguage.Core.Statements.MetaIfStatements;

namespace SimpleLanguage.Core.Statements
{
    public class MetaAssignManager
    {
        public MetaVariable judgmentValueMetaVariable => m_JudgmentValueMetaVariable;
        public MetaExpressNode expressNode => m_ExpressNode;
        public bool isNeedSetMetaVariable => m_IsNeedSetMetaVariable;

        private MetaExpressNode m_ExpressNode = null;
        private MetaVariable m_JudgmentValueMetaVariable = null;
        private bool m_IsNeedSetMetaVariable = false;
        private MetaBlockStatements m_MetaBlockStatements = null;
        private MetaType m_MetaDefineType = null;

        public MetaAssignManager(MetaExpressNode expressNode, MetaBlockStatements mbs, MetaType defaultMdt )
        {
            m_ExpressNode = expressNode;
            m_MetaBlockStatements = mbs;
            m_MetaDefineType = defaultMdt;

            CreateMetaVariable();
        }
        public void CreateMetaVariable()
        {
            switch (m_ExpressNode)
            {
                case MetaCallLinkExpressNode mcen:
                    {
                        var retMc = mcen.GetReturnMetaClass();
                        if (retMc == CoreMetaClassManager.booleanMetaClass)
                        {
                            m_JudgmentValueMetaVariable = mcen.GetMetaVariable();
                        }
                        else
                        {
                            Console.WriteLine("Error 返回的判断语句: " + mcen.ToTokenString() + "   并非是boolean类型!");
                        }
                    }
                    break;
                case MetaConstExpressNode mconen:
                    {
                        Console.WriteLine("Error -------------------------------------------");
                    }
                    break;
                case MetaOpExpressNode moen:
                    {
                        Console.WriteLine("Error -------------------------------------------");
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Error -------------------------------------------");
                    }
                    break;
            }

            if (m_JudgmentValueMetaVariable == null)
            {
                m_IsNeedSetMetaVariable = true;
                m_JudgmentValueMetaVariable = new MetaVariable("autocreate_" + GetHashCode(), MetaVariable.EVariableFrom.LocalStatement, m_MetaBlockStatements, m_MetaBlockStatements.ownerMetaClass, m_MetaDefineType);
                m_JudgmentValueMetaVariable.AddPingToken(m_ExpressNode.GetToken());
            }
        }
    }
    public partial class MetaAssignStatements : MetaStatements
    {
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaExpressNode expressNode => m_ExpressNode;

        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;

        private MetaVariable m_MetaVariable = null;
        private EOpSign m_OpSign;
        private ELeftRightOpSign m_AutoAddExpressOpSign;
        private Token m_SignToken = null;
        private bool m_IsSetStatements = false;
        private bool m_IsAssign = false;

        private MetaExpressNode m_ExpressNode;
        private MetaCallLinkExpressNode m_LeftMetaExpress;
        private MetaExpressNode m_FinalMetaExpress;
        private bool m_IsNeedCastStatements = false;

        public MetaAssignStatements( MetaBlockStatements mbs, FileMetaOpAssignSyntax fmos) : base(mbs)
        {
            m_FileMetaOpAssignSyntax = fmos;

            Parse();
        }
        private void Parse()
        {
            var metaCallLink = new MetaCallLink(m_FileMetaOpAssignSyntax.variableRef, m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements );

            if (metaCallLink == null)
            {
                Console.WriteLine("Error MetaAssignStatements ParseDefine!!!" + m_FileMetaOpAssignSyntax.variableRef?.ToTokenString());
                return;
            }

            m_SignToken = m_FileMetaOpAssignSyntax.assignToken;
            m_LeftMetaExpress = new MetaCallLinkExpressNode(metaCallLink);

            ETokenType ett = m_SignToken.type;

            bool isCanNew = false;
            switch( ett )
            {
                case ETokenType.Assign:
                    {
                        isCanNew = true;
                    }
                    break;
                case ETokenType.PlusAssign:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.Plus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Add;
                    }
                    break;
                case ETokenType.MinusAssign:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.Minus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Minus;
                    }
                    break;
                case ETokenType.DivideAssign:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.Divide;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Divide;
                    }
                    break;
                case ETokenType.MultiplyAssign:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.Multiply;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Multiply;
                    }
                    break;
                case ETokenType.InclusiveOrAssign:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.InclusiveOr;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.InclusiveOr;
                    }
                    break;
                case ETokenType.CombineAssign:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.Combine;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Combine;
                    }
                    break;
                case ETokenType.XORAssign:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.XOR;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.XOR;
                    }
                    break;
                case ETokenType.DoublePlus:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.Plus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Add;
                    }
                    break;
                case ETokenType.DoubleMinus:
                    {
                        m_IsAssign = true;
                        m_OpSign = EOpSign.Minus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Minus;
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Error 赋值语句解析符号暂不支持: " + ett.ToString());
                    }
                    break;
            }
            AllowUseSettings auc = new AllowUseSettings();
            auc.useNotStatic = m_FileMetaOpAssignSyntax?.staticToken != null ? false : true;
            auc.useNotConst = m_FileMetaOpAssignSyntax?.constToken == null ? false : true;
            auc.setterFunction = true;
            auc.getterFunction = false;
            m_LeftMetaExpress.Parse(auc);
            m_LeftMetaExpress.CalcReturnType();

            if(m_LeftMetaExpress.metaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.MethodCall )
            {
                var fun = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall.function;
                if ( fun is MetaMemberFunction)
                {
                    MetaMemberFunction mmf = fun as MetaMemberFunction;
                    if (mmf.isSet)
                    {
                        m_IsSetStatements = true;
                    }
                }
            }

            MetaType expressMdt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (!m_IsSetStatements)
            {
                m_MetaVariable = m_LeftMetaExpress.GetMetaVariable();
                if (m_MetaVariable == null)
                {
                    Console.WriteLine("Error 变量没有发现" + m_LeftMetaExpress.ToTokenString());
                    return;
                }
                if(m_MetaVariable.isConst )
                {
                    Console.WriteLine("Error 类型为Const类型，不允许使用赋值!!");
                }

                m_Name = m_MetaVariable.name;
                if( m_MetaVariable.isGlobal )
                {
                    if( ownerMetaClass.name == "Project" )
                    {

                    }
                    else
                    {
                        Console.WriteLine("Error 只能在Project工程下的函数中，给全局变量赋值!!");
                        return;
                    }
                }
                expressMdt = m_MetaVariable.metaDefineType;
            }

            if (m_FileMetaOpAssignSyntax.express != null)
            {
                m_ExpressNode = ExpressManager.CreateExpressNodeInMetaFunctionNewStatementsWithIfOrSwitch(m_FileMetaOpAssignSyntax.express, m_OwnerMetaBlockStatements, expressMdt);
                
                if (m_ExpressNode == null)
                {
                    Console.WriteLine("Error 解析新建变量语句时，表达式解析为空!!");
                    return;
                }
            }
            else
            {
                if ( m_SignToken != null && m_ExpressNode == null)
                {
                    if (m_SignToken?.type == ETokenType.DoublePlus
                        || m_SignToken?.type == ETokenType.DoubleMinus)
                    {
                        m_ExpressNode = new MetaConstExpressNode(EType.Int32, 1);
                    }
                }
            }

            if (m_IsAssign)
            {
                m_FinalMetaExpress = new MetaOpExpressNode(m_LeftMetaExpress, m_ExpressNode, m_AutoAddExpressOpSign);
            }
            else
            {
                m_FinalMetaExpress = m_ExpressNode;
            }

            if (m_FinalMetaExpress == null)
            {
                Console.WriteLine("Error 类: " + ownerMetaClass?.allName + "没有找到变量:[" + m_FileMetaOpAssignSyntax.express.ToFormatString() + "]的定义!!! 69 ");
                return;
            }
            m_FinalMetaExpress.CalcReturnType();

            MetaType expressRetMetaDefineType = m_FinalMetaExpress.GetReturnMetaDefineType();
            if (expressRetMetaDefineType == null)
            {
                Console.WriteLine("Error 解析新建变量语句时，表达式返回类型为空!!__3");
                return;
            }

            if(m_IsSetStatements == false )
            {
                //Class1{  set name( string _n) { _name = _n } }
                // c1 = Class1()
                // c1.name = "aa"  =>   c1.name("aa")
                // 相当于 给 set 函数传参数

                MetaType mdt = m_MetaVariable.metaDefineType;

                if( mdt.metaTemplate != null )
                {
                    if( expressRetMetaDefineType?.metaTemplate != mdt.metaTemplate )
                    {
                        Console.WriteLine("Error 模版与类定义的模版不相同!!");
                    }
                }
                else
                {
                    ClassManager.EClassRelation relation = ClassManager.EClassRelation.No;
                    MetaClass curClass = mdt.metaClass;

                    MetaClass compareClass = null;
                    MetaConstExpressNode constExpressNode = m_ExpressNode as MetaConstExpressNode;
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
                    sb.Append("Warning 在类: " + m_OwnerMetaBlockStatements?.ownerMetaClass.allName + " 函数: " + m_OwnerMetaBlockStatements.ownerMetaFunction?.name + "中  ");
                    if (curClass != null)
                    {
                        sb.Append(" 定义类 : " + curClass.allName);
                    }
                    sb.Append(" 名称为: " + m_Name?.ToString());
                    sb.Append("与后边赋值语句中 ");
                    if (compareClass != null)
                        sb.Append("表达式类为: " + compareClass.allName);
                    if (relation == ClassManager.EClassRelation.No)
                    {
                        sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                        Console.WriteLine(sb.ToString());
                        m_IsNeedCastState = true;
                    }
                    else if( relation == ClassManager.EClassRelation.Similar )
                    {
                        sb.Append("数字类型相似，可能会有强转会有精度的丢失!");
                        Console.WriteLine(sb.ToString());
                        m_IsNeedCastState = true;
                    }
                    else if (relation == ClassManager.EClassRelation.Same)
                    {
                        m_MetaVariable.SetMetaDefineType(expressRetMetaDefineType);
                    }
                    else if (relation == ClassManager.EClassRelation.Parent)
                    {
                        sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                        Console.WriteLine(sb.ToString());
                        m_IsNeedCastState = true;
                    }
                    else if (relation == ClassManager.EClassRelation.Child)
                    {
                        if (compareClass != null)
                        {
                            m_MetaVariable.SetMetaDefineType(expressRetMetaDefineType);
                        }
                    }
                    else
                    {
                        sb.Append("表达式错误，或者是定义类型错误");
                        Console.WriteLine(sb.ToString());
                    }
                }
            }
            else
            {
                MetaInputParam mip = new MetaInputParam(m_ExpressNode);
                //mfc.metaInputParamCollection.AddMetaInputParam(mip);
                //mfc.CheckMetaFunctionMatchInputParamCollection();
            }
            return;
        }

        public override MetaStatements GenTemplateClassStatement(MetaGenTemplateClass mgt, MetaBlockStatements parentMs)
        {
            if (m_NextMetaStatements != null)
            {
                //m_NextMetaStatements.GenTemplateClassStatement(mgt);
            }

            return null;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(GetFormatString());
            sb.Append(";");
            sb.Append(Environment.NewLine);
            if (nextMetaStatements != null)
            {
                sb.Append(nextMetaStatements.ToFormatString());
            }
            return sb.ToString();
        }
        public override string GetFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            
            
            if(m_IsSetStatements)
            {
                sb.Append(m_MetaCallLink.ToFormatString());
            }
            else
            {
                if (m_MetaVariable != null)
                {
                    sb.Append(m_MetaCallLink.ToFormatString());
                }
                else
                {
                    sb.Append("NotFind[ " + m_MetaCallLink?.ToFormatString());
                }
                sb.Append(" = ");

                if (m_FinalMetaExpress != null)
                {
                    sb.Append(m_FinalMetaExpress.ToFormatString());
                }
                if(m_IsNeedCastState)
                {
                    sb.Append(".Cast<");
                    sb.Append(m_MetaVariable.metaDefineType.ToFormatString());
                    sb.Append(">()");
                }
            }
            return sb.ToString();
        }

    }
}
