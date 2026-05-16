//****************************************************************************
//  File:      MetaAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaAssignManager
    {
        public MetaVariable judgmentValueMetaVariable => m_JudgmentValueMetaVariable;
        public MetaExpressNodeBase expressNode => m_ExpressNode;
        public bool isNeedSetMetaVariable => m_IsNeedSetMetaVariable;

        private MetaExpressNodeBase m_ExpressNode = null;
        private MetaVariable m_JudgmentValueMetaVariable = null;
        private bool m_IsNeedSetMetaVariable = false;
        private MetaBlockStatements m_MetaBlockStatements = null;
        private MetaType m_MetaDefineType = null;

        public MetaAssignManager(MetaExpressNodeBase expressNode, MetaBlockStatements mbs, MetaType defaultMdt )
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
                            Log.AddMetaCoreLog( LID.AutoMetaAssignStatementsL52, "Error 返回的判断语句: " + mcen.ToString() + "   并非是boolean类型!");
                        }
                    }
                    break;
                case MetaConstExpressNode mconen:
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaAssignStatementsL58, "Error -------------------------------------------");
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
                            //Log.AddMetaCoreLog( LID.Unknown, "Error 返回的判断语句: " + mcen.ToTokenString() + "   并非是boolean类型!");
                        }
                    }
                    break;
                case MetaNewObjectExpressNode _:
                case MetaArrayExpressNode _:
                case MetaAsIsExpressNode _:
                case MetaExpressTypeConvert _:
                    {
                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaAssignStatementsL75, "Error -------------------------------------------");
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
        public MetaExpressNodeBase rightMetaExpress => m_RightMetaExpress;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaCallLinkExpressNode leftMetaExpress => m_LeftMetaExpress;

        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        private MetaVariable m_MetaVariable = null;
        private EOpSign m_OpSign;
        private ELeftRightOpSign m_AutoAddExpressOpSign;
        private Token m_SignToken = null;

        private MetaCallLinkExpressNode m_LeftMetaExpress;
        private MetaMethodCall m_LeftMethodCall = null;
        private MetaVisitVariable m_LeftLastVisitVariable = null;

        private MetaExpressNodeBase m_RightMetaExpress;
        private bool m_IsSettings = false;

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
            System.Diagnostics.Debug.Assert(false);
            m_FileMetaDefineVariableSyntax = fmos;
            this.m_MetaVariable = mbs.ownerMetaClass.GetMetaMemberVariableByName(m_FileMetaDefineVariableSyntax.name);

            Parse();
        }
        private void Parse()
        {
            MetaCallLink metaCallLink = null;
            bool isRightDirectBraceLiteral = IsRightDirectBraceLiteral(m_FileMetaOpAssignSyntax?.express);

            if (m_FileMetaOpAssignSyntax != null)
            {
                metaCallLink = new MetaCallLink(m_FileMetaOpAssignSyntax.variableRef,
                m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
                m_SignToken = m_FileMetaOpAssignSyntax?.assignToken;
                var callnodelist = m_FileMetaOpAssignSyntax?.variableRef ?. callNodeList;
                if( callnodelist != null && callnodelist.Count > 0 )
                {
                    m_Token = callnodelist[callnodelist.Count - 1].token;
                }
                else
                {
                    m_Token = m_SignToken;
                }
                if (m_FileMetaOpAssignSyntax?.staticToken != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 不允许在语句中，出现static字段! " + m_FileMetaOpAssignSyntax?.variableRef?.ToTokenString());
                }
            }
            else if( m_FileMetaDefineVariableSyntax != null)
            {
                //metaCallLink = new MetaCallLink(m_FileMetaDefineVariableSyntax.,
                //m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
                m_SignToken = m_FileMetaDefineVariableSyntax?.assignToken;
                m_Token = m_FileMetaDefineVariableSyntax?.nameToken;

            }

            MetaType expressMdt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (!isRightDirectBraceLiteral && m_FileMetaOpAssignSyntax.express != null)
            {
                if (!TryParseRightExpress(null))
                {
                    return;
                }
            }
            if (metaCallLink == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error MetaAssignStatements ParseDefine!!!" + m_FileMetaOpAssignSyntax?.variableRef?.ToTokenString());
                return;
            }

            m_LeftMetaExpress = new MetaCallLinkExpressNode(metaCallLink);
            AllowUseSettings auc = new AllowUseSettings();
            auc.useNotStatic = false;
            auc.useNotConst = m_FileMetaOpAssignSyntax?.constToken == null ? false : true;            
            auc.getterFunction = true;
            if (m_RightMetaExpress != null )
            {
                auc.setterFunction = true;
                auc.expressNodeList.Add(m_RightMetaExpress);
            }
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
                        m_IsSettings = true;
                        m_LeftMethodCall = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall;
                        m_RightMetaExpress = null;

                        var firstParam = mmf.metaMemberParamCollection.metaDefineParamList[0];
                        if (firstParam?.metaVariable != null)
                        {
                            expressMdt = firstParam.metaVariable.isDefineMetaType
                                ? firstParam.metaVariable.defineMetaType
                                : (firstParam.metaVariable.realMetaType ?? firstParam.metaVariable.defineMetaType);
                        }
                    }
                }
            }
            else if( m_LeftMetaExpress.metaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.VisitVariable )
            {
                m_LeftLastVisitVariable = m_LeftMetaExpress.metaCallLink.finalCallNode.visitVariable;
            }

            // setStatements    Class1{ set void A(int value) { } }  a.A = 10;  => a.A(10);
            //if (m_LeftMethodCall != null)
            //{
            //    if (m_SignToken?.type == ETokenType.Assign)
            //    {
            //        m_RightMetaExpress = null;
            //    }
            //    else
            //    {
            //        //这里只能使用等号进行赋值操作  a.A += 10;  是不允许的
            //        Log.AddMetaCoreLog(LID.AutoMetaAssignStatementsL189, m_Token, "Error set语句只能使用=号进行赋值操作!!");
            //        return;
            //    }
            //}

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
                case ETokenType.ModuloAssign:
                    {
                        m_OpSign = EOpSign.Modulo;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Modulo;
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
                case ETokenType.ShiAssign:
                    {
                        m_OpSign = EOpSign.Shi;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Shi;
                    }
                    break;
                case ETokenType.ShrAssign:
                    {
                        m_OpSign = EOpSign.Shr;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Shr;
                    }
                    break;
                case ETokenType.DoublePlus:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Plus;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Add;
                        m_RightMetaExpress = new MetaConstExpressNode(EType.Int8, 1);
                    }
                    break;
                case ETokenType.DoubleMinus:
                    {
                        //m_IsAssign = true;
                        m_OpSign = EOpSign.Minus;
                        m_RightMetaExpress = new MetaConstExpressNode(EType.Int8, 1);
                        m_AutoAddExpressOpSign = ELeftRightOpSign.Minus;
                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaAssignStatementsL288, "Error 赋值语句解析符号暂不支持: " + ett.ToString());
                    }
                    break;
            }
            if(m_OpSign != EOpSign.None )
            {
                m_LeftLastVisitVariable?.SetNotUseFast();
            }


            if (m_LeftMethodCall == null)
            {
                m_MetaVariable = m_LeftMetaExpress.GetMetaVariable();
                if (m_MetaVariable == null)
                {
                    Log.AddMetaCoreLog( LID.AutoMetaAssignStatementsL304, m_Token, "Error 变量没有发现" + m_LeftMetaExpress.ToString());
                    return;
                }
                if(m_MetaVariable.isConst )
                {
                    Log.AddMetaCoreLog( LID.AutoMetaAssignStatementsL309, m_Token, "Error 当前左值声明为 const，不允许进行赋值或修改!!");
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
                        Log.AddMetaCoreLog( LID.MetaCoreGlobalSettingNeedInProject, m_Token, "in" + ownerMetaClass.name );
                    }
                }
                expressMdt = m_MetaVariable.GetFinalMetaType();
            }

            if (!m_IsSettings && isRightDirectBraceLiteral && m_RightMetaExpress == null && m_FileMetaOpAssignSyntax?.express != null)
            {
                var rightPreferredMetaType = ResolveRightPreferredMetaTypeForDirectBraceLiteral(expressMdt);
                if (rightPreferredMetaType == null 
                    || (rightPreferredMetaType?.eMetaTypeType != EMetaTypeType.MetaClass 
                    && rightPreferredMetaType?.eMetaTypeType != EMetaTypeType.MetaGenClass ) )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                        "右值为 {} 初始化时，左值类型必须是 class/data/array/map，当前类型不支持该写法。");
                    return;
                }

                if (!TryParseRightExpress(rightPreferredMetaType))
                {
                    return;
                }
            }

            if( !m_IsSettings )
            {
                if (m_RightMetaExpress == null)
                {
                    Log.AddMetaCoreLog(LID.AutoMetaAssignStatementsL365, m_Token, "Error 解析新建变量语句时，表达式为空!!__2");
                    return;
                }
                if (m_LeftMethodCall == null)
                {
                    TryCoerceRightArrayLiteralToLeftArrayTypeAfterLeftResolved();
                    CheckLeftAndRightExpress();
                }
            }

            return;
        }

        private bool TryParseRightExpress(MetaType rightMetaTypeHint)
        {
            if (m_FileMetaOpAssignSyntax?.express == null)
            {
                return true;
            }

            CreateExpressParam cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                metaType = rightMetaTypeHint,
                ownerMetaBase = ownerMetaClass,
                fme = m_FileMetaOpAssignSyntax.express,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
                equalMetaVariable = m_MetaVariable,
                allowNewVariable = true,
            };
            m_RightMetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
            if (m_RightMetaExpress == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreShouldHaveRightExpress, m_Token, "MetaAssignStatements", m_FileMetaOpAssignSyntax.express.token);
                return false;
            }

            m_RightMetaExpress.Parse(new AllowUseSettings() { setterFunction = false, getterFunction = true } );
            m_RightMetaExpress = ExpressManager.ConvertNewExpress(m_RightMetaExpress, rightMetaTypeHint, m_MetaVariable);
            if (m_RightMetaExpress == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreShouldHaveRightExpress, m_Token, "MetaAssignStatements.ConvertNewExpress", m_FileMetaOpAssignSyntax.express.token);
                return false;
            }
            m_RightMetaExpress.CalcReturnType();
            return true;
        }

        private static bool IsRightDirectBraceLiteral(FileMetaBaseTerm rightExpress)
        {
            if (rightExpress == null)
            {
                return false;
            }

            if (rightExpress is FileMetaBraceTerm)
            {
                return true;
            }

            return rightExpress.root is FileMetaBraceTerm;
        }

        private MetaType ResolveRightPreferredMetaTypeForDirectBraceLiteral(MetaType setterParamMetaType)
        {
            if (m_LeftMethodCall != null)
            {
                return setterParamMetaType;
            }
            return m_MetaVariable?.GetFinalMetaType();
        }

        /// <summary>
        /// 赋值常先解析右值再解析左值（例如 setter 需先把右值放入参数再解析左值），故 <c>[1,2,100]</c> 首次推断无左值元素类型。
        /// 左值最终类型可用后：若左为具元素模板的数组（非 object 元素）、右为未显式 Array-T-构造调用的数组字面量、且左右元素类型不一致，
        /// 则 <see cref="MetaNewObjectExpressNode.SetAssignmentTargetArrayMetaType"/> + <see cref="MetaNewObjectExpressNode.CalcReturnType"/> 纠正（详见 MetaExpressNewObject / NumberManager）。
        /// <see cref="m_IsSettings"/> 或左值非变量访问时跳过。
        /// </summary>
        private void TryCoerceRightArrayLiteralToLeftArrayTypeAfterLeftResolved()
        {
            if (m_IsSettings || m_LeftMethodCall != null || m_RightMetaExpress == null || m_MetaVariable == null)
            {
                return;
            }

            if (m_RightMetaExpress is not MetaNewObjectExpressNode mnoe || mnoe.newType != MetaNewObjectExpressNode.ENewType.ArrayClass)
            {
                return;
            }

            if (mnoe.usesExplicitArrayElementTypeSyntax)
            {
                return;
            }

            var leftMt = m_MetaVariable.GetFinalMetaType();
            if (leftMt == null || !leftMt.IsArray())
            {
                return;
            }

            var leftElem = ClassManager.GetSingleTemplateArgMetaType(leftMt);
            if (leftElem?.metaClass == null || leftElem.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                return;
            }

            var rightMt = m_RightMetaExpress.GetReturnMetaType();
            if (rightMt == null || !rightMt.IsArray())
            {
                return;
            }

            var rightElem = ClassManager.GetSingleTemplateArgMetaType(rightMt);
            if (rightElem != null && TypeManager.CompareMetaType(leftElem, rightElem))
            {
                return;
            }

            mnoe.SetAssignmentTargetArrayMetaType(leftMt);
            mnoe.CalcReturnType();
        }

        void CheckLeftAndRightExpress()
        {
            var token = m_RightMetaExpress.token;

            MetaType expressRetMetaDefineType = m_RightMetaExpress.GetReturnMetaType();
            //Class1{  set name( string _n) { _name = _n } }
            // c1 = Class1()
            // c1.name = "aa"  =>   c1.name("aa")
            // 相当于 给 set 函数传参数

            MetaType mdt = m_MetaVariable.GetFinalMetaType();
            if (!TryForceConvertRightExpressByLeftMetaType(mdt, ref expressRetMetaDefineType))
            {
                return;
            }

            //if( mdt.metaTemplate != null )
            //{
            //    if( expressRetMetaDefineType?.metaTemplate != mdt.metaTemplate )
            //    {
            //        Log.AddMetaCoreLog( LID.Unknown, "Error 模版与类定义的模版不相同!!");
            //    }
            //}
            //else
            //{
            if (expressRetMetaDefineType == null || expressRetMetaDefineType.metaClass == CoreMetaClassManager.nullMetaClass)
            {

            }
            else
            {
                var relation = TypeManager.ResolveAssignRelation(
                    mdt,
                    m_RightMetaExpress,
                    true,
                    false,
                    out expressRetMetaDefineType,
                    out MetaClass curClass,
                    out MetaClass compareClass,
                    out _,
                    m_MetaVariable);

                if (relation == EClassRelation.CompareClassError)
                {
                    Log.AddMetaCoreLog(LID.AutoMetaAssignStatementsL452, m_Token, "Error 赋值表达式返回定义类型为空");
                    return;
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
                if (relation == EClassRelation.No)
                {
                    var targetTemplateList = mdt?.GetGenTemplateMetaTypeList();
                    var exprTemplateList = expressRetMetaDefineType?.GetGenTemplateMetaTypeList();
                    bool hasTemplateInEither = (targetTemplateList != null && targetTemplateList.Count > 0)
                        || (exprTemplateList != null && exprTemplateList.Count > 0);
                    if (hasTemplateInEither)
                    {
                        sb.Append("模板类型不匹配（接口模板位置仅在可协变标记下允许协变），请检查模板参数或接口变型规则。");
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, sb.ToString());
                        return;
                    }

                    sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, sb.ToString());
                    m_IsNeedCastState = true;
                }
                else if (relation == EClassRelation.Similar)
                {
                    // 数字相似/升阶赋值（如具体数值赋给 Num 或变窄变宽）为正常语义，不记 ShowExtendMessage，避免误判为告警
                    m_IsNeedCastState = true;
                }
                else if (relation == EClassRelation.Same)
                {
                }
                else if( relation == EClassRelation.Num )
                {
                    // 左值为 Num（如迭代器 _iteratorValue）右值为具体 Int8 等：编译期已强转，属预期行为，不输出扩展告警
                    m_IsNeedCastState = true;
                }
                else if (relation == EClassRelation.Parent)
                {
                    sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, sb.ToString());
                    m_IsNeedCastState = true;
                }
                else if (relation == EClassRelation.Child)
                {
                    if (compareClass != null)
                    {
                        m_MetaVariable.SetRealMetaType(expressRetMetaDefineType);
                    }
                }
                else
                {
                    sb.Append("表达式错误，或者是定义类型错误");
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, sb.ToString());
                }
            }
            //}
        }

        private bool TryForceConvertRightExpressByLeftMetaType(MetaType leftMetaType, ref MetaType rightMetaType)
        {
            if (leftMetaType == null || m_RightMetaExpress == null)
            {
                return true;
            }

            if (m_RightMetaExpress is MetaConstExpressNode rightConst)
            {
                if (!MetaVariable.TryAdjustConstExpressByDefineMetaType(rightConst, leftMetaType))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                        "赋值强制转换失败：右值常量无法转换到左值类型 " + leftMetaType.ToString());
                    return false;
                }
                m_RightMetaExpress.CalcReturnType();
                rightMetaType = m_RightMetaExpress.GetReturnMetaType();
                return true;
            }

            if (leftMetaType.IsArray() && rightMetaType != null && rightMetaType.IsArray())
            {
                var leftElemType = ClassManager.GetSingleTemplateArgMetaType(leftMetaType);
                var rightElemType = ClassManager.GetSingleTemplateArgMetaType(rightMetaType);
                if (leftElemType == null || rightElemType == null)
                {
                    return true;
                }

                if (leftElemType.IsNum() && rightElemType.IsNum())
                {
                    if (m_RightMetaExpress is MetaNewObjectExpressNode mnoe)
                    {
                        // [1,2,1000] 等仅推断元素类型时，允许按左值元素类型强转常量；
                        // Array<Int16>(n){ ... } 等已在语法中指定元素类型时，元素类型与左值不一致则报错，不自动改字面量类型。
                        if (mnoe.usesExplicitArrayElementTypeSyntax)
                        {
                            if (!TypeManager.CompareMetaType(leftElemType, rightElemType))
                            {
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                                    "右值数组已在创建表达式中指定元素类型（例如 Array<Int16>(...)），与左值元素类型 "
                                    + leftElemType.ToString() + " 不一致，不能自动强转。请使用与左值一致的元素类型，或使用未标注类型的字面量 [...]。");
                                return false;
                            }
                        }
                        else
                        {
                            if (!TryForceConvertArrayLiteralElements(mnoe, leftElemType))
                            {
                                return false;
                            }
                            m_RightMetaExpress.CalcReturnType();
                            rightMetaType = m_RightMetaExpress.GetReturnMetaType();
                            m_IsNeedCastState = true;
                        }
                    }
                }
            }

            return true;
        }

        private bool TryForceConvertArrayLiteralElements(MetaNewObjectExpressNode arrayNode, MetaType targetElemType)
        {
            if (arrayNode == null || targetElemType == null)
            {
                return true;
            }

            var list = arrayNode.assignStatementsList;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var expr = item?.expressNode;
                if (expr == null) continue;

                if (expr is MetaConstExpressNode c)
                {
                    if (!NumberManager.TryForceAdjustConstExpressByMetaType(c, targetElemType, m_Token))
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                            "数组元素强制转换失败（可能溢出或类型不匹配）: 目标类型 " + targetElemType.ToString());
                        return false;
                    }
                    c.CalcReturnType();
                    continue;
                }

                if (expr is MetaNewObjectExpressNode childArrayNode && targetElemType.IsArray())
                {
                    var nextElemType = ClassManager.GetSingleTemplateArgMetaType(targetElemType);
                    if (nextElemType != null && !TryForceConvertArrayLiteralElements(childArrayNode, nextElemType))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        public override void UpdateOwnerMetaClass(MetaBase ownerBase)
        {
            //this.m_MetaVariable?.SetOwnerMetaClass(ownerBase);
            base.UpdateOwnerMetaClass(ownerBase);
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
            
            
            if(this.m_LeftMethodCall != null)
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
