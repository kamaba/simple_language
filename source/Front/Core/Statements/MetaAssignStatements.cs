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
                            m_JudgmentValueMetaVariable = mcen.GetStoreMetaVariable();
                        }
                        else
                        {
                            Log.AddMetaCoreLog( LID.ShowExtendMessage, "Error 返回的判断语句: " + mcen.ToString() + "   并非是boolean类型!");
                        }
                    }
                    break;
                case MetaConstExpressNode mconen:
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, mconen.token, "Error -------------------------------------------1");
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
                            //Log.AddMetaCoreLog( LID.ShowExtendMessage, "Error 返回的判断语句: " + mcen.ToTokenString() + "   并非是boolean类型!");
                        }
                    }
                    break;
                case MetaNewObjectExpressNode _:
                case MetaArrayExpressNode _:
                case MetaAsIsExpressNode _:
                case MetaExpressTypeConvert _:
                case MetaAnonDataExpressNode _:
                case MetaUnaryOpExpressNode _:
                    {
                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error -------------------------------------------2");
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
            bool isRightDirectBraceLiteral = m_FileMetaOpAssignSyntax?.express is FileMetaBraceTerm;

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
            if ( (isRightDirectBraceLiteral  || m_FileMetaOpAssignSyntax?.express is not FileMetaCallTerm )
                && m_FileMetaOpAssignSyntax.express != null
                && metaCallLink.callNodeList.Count > 1)
            {
                TryParseRightExpress(null);
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
            //        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error set语句只能使用=号进行赋值操作!!");
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 赋值语句解析符号暂不支持: " + ett.ToString());
                    }
                    break;
            }
            if(m_OpSign != EOpSign.None )
            {
                m_LeftLastVisitVariable?.SetNotUseFast();
            }


            if (m_LeftMetaExpress != null)
            {
                m_MetaVariable = m_LeftMetaExpress.GetDefineMetaVariable();     //这里要使用定义时的变量，因为可能存在 a.b.c += 10; 这种情况，a.b.c 可能在之前被解析成一个临时变量了，这时候就需要回到定义时的变量上去进行类型检查和后续的赋值操作
                if (m_MetaVariable == null)
                {
                    Log.AddMetaCoreLog( LID.ShowExtendMessage, m_Token, "Error 变量没有发现" + m_LeftMetaExpress.ToString());
                    return;
                }
                if(m_MetaVariable.isConst )
                {
                    Log.AddMetaCoreLog( LID.MetaCoreAssertShowMessage, m_Token, "Error 当前左值声明为 const，不允许进行赋值或修改!!");
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
                        Log.AddMetaCoreLog( LID.ShowExtendMessage, m_Token, "in" + ownerMetaClass.name );
                    }
                }
                expressMdt = m_MetaVariable.GetFinalMetaType();
            }

            if (m_RightMetaExpress == null
                && m_FileMetaOpAssignSyntax?.express != null )
            {
                var rightPreferredMetaType = ResolveRightPreferredMetaTypeForDirectBraceLiteral(expressMdt);
                if (rightPreferredMetaType == null
                    || (!rightPreferredMetaType.isData
                        && !rightPreferredMetaType.isClass
                        && !rightPreferredMetaType.isMap
                        && rightPreferredMetaType.eMetaTypeType != EMetaTypeType.MetaGenClass))
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
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 解析新建变量语句时，表达式为空!!__2");
                    return;
                }
                if (m_LeftMethodCall == null)
                {
                    //TryCoerceRightArrayLiteralToLeftArrayTypeAfterLeftResolved();
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
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "MetaAssignStatements", m_FileMetaOpAssignSyntax.express.token);
                return false;
            }

            this.m_RightMetaExpress.Parse(new AllowUseSettings() { setterFunction = false, getterFunction = true, ifNotVariableThenAddVariable = rightMetaTypeHint!= null } );
            this.m_RightMetaExpress.CalcReturnType();
            var newexpress = ExpressManager.ConvertNewExpress(m_RightMetaExpress, rightMetaTypeHint, m_MetaVariable);            
            if (newexpress != m_RightMetaExpress )
            {
                m_RightMetaExpress = newexpress;
            }
            return true;
        }
        private MetaType ResolveRightPreferredMetaTypeForDirectBraceLiteral(MetaType setterParamMetaType)
        {
            if (m_LeftMethodCall != null)
            {
                return setterParamMetaType;
            }
            return m_MetaVariable?.GetFinalMetaType();
        }

        ///// <summary>
        ///// 赋值常先解析右值再解析左值（例如 setter 需先把右值放入参数再解析左值），故 <c>[1,2,100]</c> 首次推断无左值元素类型。
        ///// 左值最终类型可用后：若左为具元素模板的数组（非 object 元素）、右为未显式 Array-T-构造调用的数组字面量、且左右元素类型不一致，
        ///// <see cref="m_IsSettings"/> 或左值非变量访问时跳过。
        ///// </summary>
        //private void TryCoerceRightArrayLiteralToLeftArrayTypeAfterLeftResolved()
        //{
        //    if (m_IsSettings || m_LeftMethodCall != null || m_RightMetaExpress == null || m_MetaVariable == null)
        //    {
        //        return;
        //    }

        //    if (m_RightMetaExpress is not MetaNewObjectExpressNode mnoe 
        //        || mnoe.newType != MetaNewObjectExpressNode.ENewType.ArrayClass)
        //    {
        //        return;
        //    }

        //    if (mnoe.usesExplicitArrayElementTypeSyntax)
        //    {
        //        return;
        //    }

        //    var leftMt = m_MetaVariable.GetFinalMetaType();
        //    if (leftMt == null || !leftMt.IsArray())
        //    {
        //        return;
        //    }

        //    var leftElem = ClassManager.GetSingleTemplateArgMetaType(leftMt);
        //    if (leftElem?.metaClass == null || leftElem.metaClass == CoreMetaClassManager.objectMetaClass)
        //    {
        //        return;
        //    }

        //    var rightMt = m_RightMetaExpress.GetReturnMetaType();
        //    if (rightMt == null || !rightMt.IsArray())
        //    {
        //        return;
        //    }

        //    var rightElem = ClassManager.GetSingleTemplateArgMetaType(rightMt);
        //    if (rightElem != null )
        //    {
        //        return;
        //    }
        //    mnoe.CalcReturnType();
        //}

        void CheckLeftAndRightExpress()
        {
            if (m_RightMetaExpress == null || m_MetaVariable == null)
            {
                return;
            }


            if (TypeManager.CompareLeftRightMetaType(m_MetaVariable.GetFinalMetaType(), m_RightMetaExpress.GetReturnMetaType(), m_Token, out var convertMetaType ) )
            {
                if (convertMetaType != null)
                {
                    m_MetaVariable.SetRealMetaType(convertMetaType);
                }
            }
            else
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "left or right compare failed");
                return;
            }
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
            sb.AppendLine(";");
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
