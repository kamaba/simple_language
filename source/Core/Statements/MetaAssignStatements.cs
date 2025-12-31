//****************************************************************************
//  File:      MetaAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;

using SimpleLanguage.Parse;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
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
                            Log.AddInStructMeta( EError.None, "Error 返回的判断语句: " + mcen.ToTokenString() + "   并非是boolean类型!");
                        }
                    }
                    break;
                case MetaConstExpressNode mconen:
                    {
                        Log.AddInStructMeta(EError.None, "Error -------------------------------------------");
                    }
                    break;
                case MetaOpExpressNode moen:
                    {
                        if (moen.isEqualType)
                        {
                            //m_JudgmentValueMetaVariable = CreateOptimizeAfterExpress;
                        }
                        else
                        {
                            //Log.AddInStructMeta( EError.None, "Error 返回的判断语句: " + mcen.ToTokenString() + "   并非是boolean类型!");
                        }
                    }
                    break;
                default:
                    {
                        Log.AddInStructMeta(EError.None, "Error -------------------------------------------");
                    }
                    break;
            }

            if (m_JudgmentValueMetaVariable == null)
            {
                m_IsNeedSetMetaVariable = true;
                m_JudgmentValueMetaVariable = new MetaVariable("autocreate_" + GetHashCode(), MetaVariable.EVariableFrom.LocalStatement, m_MetaBlockStatements, m_MetaBlockStatements.ownerMetaClass, m_MetaDefineType);
                //m_JudgmentValueMetaVariable.AddPingToken(m_ExpressNode.GetToken());
            }
        }
    }
    public partial class MetaAssignStatements : MetaStatements
    {
        public MetaMethodCall leftMethodCall => m_LeftMethodCall;
        public EOpSign opSign => m_OpSign;
        public ELeftRightOpSign autoAddExpressOpSign => m_AutoAddExpressOpSign;
        public MetaExpressNode rightMetaExpress => m_RightMetaExpress;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaCallLinkExpressNode leftMetaExpress => m_LeftMetaExpress;
        public bool isNewStatements => false;

        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;

        private MetaVariable m_MetaVariable = null;
        private EOpSign m_OpSign;
        private ELeftRightOpSign m_AutoAddExpressOpSign;
        private Token m_SignToken = null;
        //private bool m_IsAssign = false;

        private MetaCallLinkExpressNode m_LeftMetaExpress;
        private MetaMethodCall m_LeftMethodCall = null;
        private MetaVisitVariable m_LeftLastVisitVariable = null;

        private MetaExpressNode m_RightMetaExpress;
        private bool m_IsNeedCastStatements = false;

        public MetaAssignStatements( MetaBlockStatements mbs ):base( mbs )
        {

        }
        public MetaAssignStatements(MetaBlockStatements mbs, FileMetaOpAssignSyntax fmos) : base(mbs)
        {
            m_FileMetaOpAssignSyntax = fmos;

            Parse();
        }
        public MetaAssignStatements(MetaBlockStatements mbs, FileMetaDefineVariableSyntax fmos) : base(mbs)
        {
            m_FileMetaDefineVariableSyntax = fmos;

            this.m_MetaVariable = mbs.ownerMetaClass.GetMetaMemberVariableByName(m_FileMetaDefineVariableSyntax.name);

            Parse();
        }
        private void Parse()
        {
            MetaCallLink metaCallLink = null;

            if (m_FileMetaOpAssignSyntax != null)
            {
                metaCallLink = new MetaCallLink(m_FileMetaOpAssignSyntax.variableRef,
                m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
                m_SignToken = m_FileMetaOpAssignSyntax?.assignToken;
            }
            else if( m_FileMetaDefineVariableSyntax != null)
            {
                //metaCallLink = new MetaCallLink(m_FileMetaDefineVariableSyntax.fileMetaClassDefine,
                //m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
                m_SignToken = m_FileMetaDefineVariableSyntax?.assignToken;

            }

            if (metaCallLink == null)
            {
                Log.AddInStructMeta(EError.None, "Error MetaAssignStatements ParseDefine!!!" + m_FileMetaOpAssignSyntax?.variableRef?.ToTokenString());
                return;
            }
            if(m_FileMetaOpAssignSyntax?.staticToken != null )
            {
                Log.AddInStructMeta(EError.None, "Error 不允许在语句中，出现static字段! " + m_FileMetaOpAssignSyntax?.variableRef?.ToTokenString());
            }

            m_LeftMetaExpress = new MetaCallLinkExpressNode(metaCallLink);
            AllowUseSettings auc = new AllowUseSettings();
            auc.useNotStatic = false;
            auc.useNotConst = m_FileMetaOpAssignSyntax?.constToken == null ? false : true;
            auc.setterFunction = true;
            auc.getterFunction = false;
            m_LeftMetaExpress.Parse(auc);
            m_LeftMetaExpress.CalcReturnType();

            if (m_LeftMetaExpress.metaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                var fun = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall.function;
                if (fun is MetaMemberFunction)
                {
                    MetaMemberFunction mmf = fun as MetaMemberFunction;
                    if (mmf.isSet)
                    {
                        m_LeftMethodCall = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall;
                    }
                }
            }
            else if( m_LeftMetaExpress.metaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.VisitVariable )
            {
                m_LeftLastVisitVariable = m_LeftMetaExpress.metaCallLink.finalCallNode.visitVariable;
            }

            // setStatements    Class1{ set void A(int value) { } }  a.A = 10;  => a.A(10);
            if (m_LeftMethodCall != null)
            {
                if (m_SignToken?.type == ETokenType.Assign)
                {
                }
                else
                {
                    //这里只能使用等号进行赋值操作  a.A += 10;  是不允许的
                    Log.AddInStructMeta(EError.None, "Error set语句只能使用=号进行赋值操作!!");
                    return;
                }
            }

            ETokenType ett = m_SignToken.type;
            switch( ett )
            {
                case ETokenType.Assign:
                    {
                        m_OpSign = EOpSign.None;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.None;
                    }
                    break;
                case ETokenType.PlusAssign:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Plus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Add;
                    }
                    break;
                case ETokenType.MinusAssign:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Minus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Minus;
                    }
                    break;
                case ETokenType.DivideAssign:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Divide;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Divide;
                    }
                    break;
                case ETokenType.MultiplyAssign:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Multiply;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Multiply;
                    }
                    break;
                case ETokenType.InclusiveOrAssign:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.InclusiveOr;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.InclusiveOr;
                    }
                    break;
                case ETokenType.CombineAssign:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Combine;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Combine;
                    }
                    break;
                case ETokenType.XORAssign:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.XOR;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.XOR;
                    }
                    break;
                case ETokenType.DoublePlus:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Plus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Add;
                        m_RightMetaExpress = new MetaConstExpressNode(EType.Int32, 1);
                    }
                    break;
                case ETokenType.DoubleMinus:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Minus;
                        m_RightMetaExpress = new MetaConstExpressNode(EType.Int32, 1);
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Minus;
                    }
                    break;
                default:
                    {
                        Log.AddInStructMeta(EError.None, "Error 赋值语句解析符号暂不支持: " + ett.ToString());
                    }
                    break;
            }
            if(m_OpSign != EOpSign.None )
            {
                m_LeftLastVisitVariable?.SetNotUseFast();
            }


            MetaType expressMdt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (m_LeftMethodCall == null)
            {
                m_MetaVariable = m_LeftMetaExpress.GetMetaVariable();
                if (m_MetaVariable == null)
                {
                    Log.AddInStructMeta( EError.None, "Error 变量没有发现" + m_LeftMetaExpress.ToTokenString());
                    return;
                }
                if(m_MetaVariable.isConst )
                {
                    Log.AddInStructMeta( EError.None, "Error 类型为Const类型，不允许使用赋值!!");
                    return;
                }

                m_Name = m_MetaVariable.name;
                if( m_MetaVariable.isGlobal )
                {
                    if( ownerMetaClass.name == "Project" )
                    {

                    }
                    else
                    {
                        Log.AddInStructMeta( EError.None, "Error 只能在Project工程下的函数中，给全局变量赋值!!");
                        return;
                    }
                }
                if( m_MetaVariable.isDefineMetaType )
                {
                    expressMdt = m_MetaVariable.defineMetaType;
                }
                else
                {
                    expressMdt = m_MetaVariable.realMetaType;
                }
            }

            if (m_FileMetaOpAssignSyntax.express != null)
            {
                CreateExpressParam cep = new CreateExpressParam()
                {
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = expressMdt,
                    ownerMetaClass = ownerMetaClass,
                    fme = m_FileMetaOpAssignSyntax.express,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress,
                    equalMetaVariable = m_MetaVariable,
                    allowNewVariable = true,
                };
                m_RightMetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
                m_RightMetaExpress.Parse(new AllowUseSettings());
                if (m_RightMetaExpress == null)
                {
                    Debug.Assert(false, "");
                    Log.AddInStructMeta( EError.None, "Error 解析新建变量语句时，表达式解析为空!!");
                    return;
                }

                var mcen = m_RightMetaExpress as MetaCallLinkExpressNode;
                if (mcen?.isNewExpressNode == true)
                {
                    m_RightMetaExpress = new MetaNewObjectExpressNode(expressMdt, mcen);
                    m_RightMetaExpress.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                    m_RightMetaExpress.CalcReturnType();
                }
                else if (m_RightMetaExpress is MetaArrayExpressNode maen)
                {
                    m_RightMetaExpress = new MetaNewObjectExpressNode(maen, ownerMetaClass, m_OwnerMetaBlockStatements, m_MetaVariable );
                    m_RightMetaExpress.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                }
            }
            else
            {
                if(m_RightMetaExpress == null)
                {
                    Log.AddInStructMeta( EError.None, "Error 解析新建变量语句时，表达式为空!!__2");
                    return;
                }
            }
            m_RightMetaExpress.CalcReturnType();

            MetaType expressRetMetaDefineType = m_RightMetaExpress.GetReturnMetaDefineType();
            if (expressRetMetaDefineType == null)
            {
                Log.AddInStructMeta( EError.None, "Error 解析新建变量语句时，表达式返回类型为空!!__3");
                return;
            }
            else
            {
                if( m_MetaVariable is MetaMemberVariable mmv )
                {

                }
                else
                {
                    if( !expressRetMetaDefineType.isNull )
                        m_MetaVariable.SetRealMetaType(expressRetMetaDefineType);
                }
            }

            if(m_LeftMethodCall == null )
            {
                CheckLeftAndRightExpress();
            }
            else
            {
                m_LeftMethodCall.AddMetaInputParamList(m_RightMetaExpress);
                if(!m_LeftMethodCall.ValidateInputParamAndDefineParam() )
                {
                    Log.AddInStructMeta(EError.None, "Error 输入参数与定义参数不正确");
                    return;
                }
            }
            return;
        }
        void CheckLeftAndRightExpress()
        {
            MetaType expressRetMetaDefineType = m_RightMetaExpress.GetReturnMetaDefineType();
            //Class1{  set name( string _n) { _name = _n } }
            // c1 = Class1()
            // c1.name = "aa"  =>   c1.name("aa")
            // 相当于 给 set 函数传参数

            MetaType mdt = m_MetaVariable.realMetaType;

            //if( mdt.metaTemplate != null )
            //{
            //    if( expressRetMetaDefineType?.metaTemplate != mdt.metaTemplate )
            //    {
            //        Log.AddInStructMeta( EError.None, "Error 模版与类定义的模版不相同!!");
            //    }
            //}
            //else
            //{
            if (expressRetMetaDefineType.metaClass == CoreMetaClassManager.nullMetaClass)
            {

            }
            else
            {
                ClassManager.EClassRelation relation = ClassManager.EClassRelation.No;
                MetaClass curClass = mdt.metaClass;

                MetaClass compareClass = null;
                MetaConstExpressNode constExpressNode = m_RightMetaExpress as MetaConstExpressNode;
                if (constExpressNode != null && constExpressNode.eType == EType.Null)
                {
                    relation = ClassManager.EClassRelation.Same;
                }
                else
                {
                    compareClass = expressRetMetaDefineType.metaClass;
                    if (mdt.isTemplate)
                    {
                        if (curClass == compareClass)
                        {
                            relation = ClassManager.EClassRelation.Same;
                        }
                    }
                    else
                    {
                        relation = ClassManager.ValidateClassRelationByMetaClass(curClass, compareClass);
                    }
                }
                StringBuilder sb = new StringBuilder();
                sb.Append("Warning 在类: " + m_OwnerMetaBlockStatements?.ownerMetaClass.allClassName + " 函数: " + m_OwnerMetaBlockStatements.ownerMetaFunction?.name + "中  ");
                if (curClass != null)
                {
                    sb.Append(" 定义类 : " + curClass.allClassName);
                }
                sb.Append(" 名称为: " + m_Name?.ToString());
                sb.Append("与后边赋值语句中 ");
                if (compareClass != null)
                    sb.Append("表达式类为: " + compareClass.allClassName);
                if (relation == ClassManager.EClassRelation.No)
                {
                    sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                    Log.AddInStructMeta(EError.None, sb.ToString());
                    m_IsNeedCastState = true;
                }
                else if (relation == ClassManager.EClassRelation.Similar)
                {
                    sb.Append("数字类型相似，可能会有强转会有精度的丢失!");
                    Log.AddInStructMeta(EError.None, sb.ToString());
                    m_IsNeedCastState = true;
                }
                else if (relation == ClassManager.EClassRelation.Same)
                {
                }
                else if (relation == ClassManager.EClassRelation.Parent)
                {
                    sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                    Log.AddInStructMeta(EError.None, sb.ToString());
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
                    Log.AddInStructMeta(EError.None, sb.ToString());
                }
            }
            //}
        }
        public override void UpdateOwnerMetaClass(MetaClass ownerclass)
        {
            //this.m_MetaVariable?.SetOwnerMetaClass(ownerclass);
            base.UpdateOwnerMetaClass(ownerclass);
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
            
            
            if(m_LeftMethodCall != null)
            {
                sb.Append(m_LeftMetaExpress.metaCallLink.ToFormatString());
            }
            else
            {
                if (m_MetaVariable != null)
                {
                    sb.Append(m_LeftMetaExpress.metaCallLink.ToFormatString());
                }
                else
                {
                    sb.Append("NotFind[ " + m_LeftMetaExpress.metaCallLink?.ToFormatString());
                }
                sb.Append(" = ");

                if (m_RightMetaExpress != null)
                {
                    sb.Append(m_RightMetaExpress.ToFormatString());
                }
                if(m_IsNeedCastState)
                {
                    sb.Append(".Cast<");
                    sb.Append(m_MetaVariable.defineMetaType.ToFormatString());
                    sb.Append(">()");
                }
            }
            return sb.ToString();
        }

    }
}
