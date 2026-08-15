//****************************************************************************
//  File:      MetaAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
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
                        if( mconen.eType != EType.Boolean )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, mconen.token, "Error -------------------------------------------1");
                        }
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
        //public EOpSign opSign => m_OpSign;
        public ELeftRightOpSign autoAddExpressOpSign => m_AutoAddExpressOpSign;
        public MetaExpressNodeBase rightMetaExpress => m_RightMetaExpress;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaCallLink leftMetaExpress => m_LeftMetaExpress;

        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        private MetaVariable m_MetaVariable = null;
        private EOpSign m_OpSign;
        private ELeftRightOpSign m_AutoAddExpressOpSign;
        private Token m_SignToken = null;

        private MetaCallLink m_LeftMetaExpress = null;
        private MetaMethodCall m_LeftMethodCall = null;

        private MetaExpressNodeBase m_RightMetaExpress;

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
            MetaType expressMdt = new MetaType(CoreMetaClassManager.objectMetaClass);

            FileMetaBaseTerm rightExpress = null;
            FileMetaCallLink fmcl = null;
            if (m_FileMetaOpAssignSyntax != null)
            {
                fmcl = m_FileMetaOpAssignSyntax?.variableRef;
                m_SignToken = m_FileMetaOpAssignSyntax?.assignToken;
                var leftCallNodeList = m_FileMetaOpAssignSyntax?.variableRef?.callNodeList;
                if (leftCallNodeList != null && leftCallNodeList.Count > 0)
                {
                    m_Token = leftCallNodeList[leftCallNodeList.Count - 1].token;
                }
                else
                {
                    m_Token = m_SignToken;
                }
                if (m_FileMetaOpAssignSyntax?.staticToken != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 不允许在语句中，出现static字段! " + m_FileMetaOpAssignSyntax?.variableRef?.ToTokenString());
                }
                rightExpress = m_FileMetaOpAssignSyntax.express;
            }
            else if (m_FileMetaDefineVariableSyntax != null)
            {
                System.Diagnostics.Debug.Assert(false);
                //metaCallLink = new MetaCallLink(m_FileMetaDefineVariableSyntax.,
                //m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
                m_SignToken = m_FileMetaDefineVariableSyntax?.assignToken;
                m_Token = m_FileMetaDefineVariableSyntax?.nameToken;

            }
            ETokenType ett = m_SignToken.type;
            if (ett == ETokenType.DoublePlus || ett == ETokenType.DoubleMinus)
            {
            }
            else
            {
                if( rightExpress == null  )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "right express it null!");
                    return;
                }
            }

            bool isAssignSign = false;
            switch (ett)
            {
                case ETokenType.Assign:
                    {
                        m_OpSign = EOpSign.None;
                        m_AutoAddExpressOpSign = ELeftRightOpSign.None;
                        isAssignSign = true;
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
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error not support operator sign " + ett.ToString());
                    }
                    break;
            }

            m_LeftMetaExpress = new MetaCallLink(fmcl, m_OwnerMetaBlockStatements.ownerMetaBase, m_OwnerMetaBlockStatements, isAssignSign ?  rightExpress : null );
            m_LeftMetaExpress.Parse(new AllowUseSettings());
            m_MetaVariable = m_LeftMetaExpress.GetStoreMetaVariable();

            if ( isAssignSign == false && m_RightMetaExpress == null)
            {
                if (TryParseRightExpress(rightExpress, null ) == false)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "TryParseRightExpress express parse failed!");
                    return;
                }

            }


            //if (leftCallNodeList.Count == 0)
            //{
            //    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "leftCallNodeList.Count == 0");
            //    return;
            //}
            //List<MetaCallNode> mcnList = new List<MetaCallNode>();
            //FileMetaCallNode fmcn1 = leftCallNodeList[0];
            //FileMetaBaseTerm fmte = null;
            //if( leftCallNodeList.Count == 1 && isAssignSign )
            //{
            //    fmte = rightExpress;
            //}
            //var firstNode = new MetaCallNode(null, fmcn1, m_OwnerMetaBlockStatements.ownerMetaBase, m_OwnerMetaBlockStatements, null, fmte);

            //var _auc = new AllowUseSettings();
            //if (firstNode.ParseNode(_auc) == false)
            //{
            //    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "firstNode.ParseNode is failed" );
            //    return;
            //}
            //mcnList.AddRange(firstNode.metaCallNodeList);


            //bool leftHasNullConditional = false;
            //MetaType leftMt = firstNode.metaType;
            //MetaType frontDefineMt = leftMt;
            //var frontcn = firstNode.metaCallNodeList[firstNode.metaCallNodeList.Count-1];
            //if (leftCallNodeList.Count == 1)
            //{
            //    //var mtt = firstNode.metaVariable.GetFinalMetaType();
            //    //if ((firstNode.callNodeType == ECallNodeType.MemberVariableName
            //    //            || firstNode.callNodeType == ECallNodeType.FunctionInnerVariableName
            //    //            || firstNode.callNodeType == ECallNodeType.ClassName)
            //    //            && firstNode.bracketExpressList.Count > 0)
            //    //{
            //    //    MetaMemberFunction mmf = mtt.metaClass?.GetOperatorMetaMemberFunctionByName("_setItem_");
            //    //    if( mmf == null )
            //    //    {
            //    //        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "GetOperatorMetaMemberFunctionByName _setItem_ is null");
            //    //        return;
            //    //    }
            //    //    else
            //    //    { 
            //    //        if(!isAssignSign)
            //    //        {
            //    //            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "GetOperatorMetaMemberFunctionByName _setItem_ need a=b is null");
            //    //            return;
            //    //        }
            //    //    }

            //        //if (mtt.IsArray() && frontcn.bracketExpressList.Count <= mtt.ArrayDimension())
            //        //{
            //        //    for (int j = 0; j < frontcn.bracketExpressList.Count; j++)
            //        //    {
            //        //        MetaCallNode mcn = new MetaCallNode(frontcn.bracketExpressList[j], firstNode.ownerMetaFunctionBlock.ownerMetaClass,
            //        //            firstNode.ownerMetaFunctionBlock, firstNode.metaType);
            //        //        mcn.SetFrontCallNode(frontcn);
            //        //        if( mcn.ParseNode(_auc) )
            //        //        {
            //        //            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "bracket express parse failed!");
            //        //            return;
            //        //        }
            //        //        mcnList.Add(mcn);
            //        //        frontcn = mcn;
            //        //    }
            //        //}
            //        //else
            //        //{
            //        //    if (frontcn.bracketExpressList.Count != 1)
            //        //    {
            //        //        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "firstNode.ParseNode bracket express List need equal 1");
            //        //        return;
            //        //    }
            //        //    MetaCallNode mcn = new MetaCallNode(frontcn.bracketExpressList[0], frontcn.ownerMetaFunctionBlock.ownerMetaClass,
            //        //        frontcn.ownerMetaFunctionBlock, frontcn.metaType);
            //        //    mcn.SetFrontCallNode(frontcn);
            //        //    if (m_RightMetaExpress == null)
            //        //    {
            //        //        if (TryParseRightExpress(rightExpress, leftMt) == false)
            //        //        {
            //        //            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "TryParseRightExpress express parse failed!");
            //        //            return;
            //        //        }
            //        //    }
            //        //    _auc.expressNodeList.Add(m_RightMetaExpress);
            //        //    _auc.getterFunction = false;
            //        //    if ( mcn.ParseNode(_auc) == false )
            //        //    {
            //        //        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "bracket express parse failed!");
            //        //        return;
            //        //    }
            //        //    mcnList.Add(mcn);
            //        //    leftMt = mcn.metaType;
            //        //    frontcn = mcn;                            
            //        //}
            //    }
            //    m_LeftMetaExpress = new MetaCallLink(m_OwnerMetaBlockStatements.ownerMetaBase,
            //        m_OwnerMetaBlockStatements, mcnList, mcnList[mcnList.Count - 1].metaVariable, m_Token);
            //    m_LeftMetaExpress.AddVisitNodeListByNewList(mcnList);
            //    if (m_RightMetaExpress == null )
            //    {
            //        if (TryParseRightExpress(rightExpress, leftMt) == false)
            //        {
            //            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "TryParseRightExpress express parse failed!");
            //            return;
            //        }
            //    }                
            //}
            //else
            //{
            //    for (int i = 1; i < leftCallNodeList.Count;)
            //    {
            //        var cn1 = leftCallNodeList[i++];

            //        if (cn1.token.type == ETokenType.Period)
            //        {
            //            FileMetaCallNode cn2 = null;
            //            if (i < leftCallNodeList.Count)
            //            {
            //                cn2 = leftCallNodeList[i++];
            //            }
            //            if (cn2 == null)
            //            {
            //                var fmn1 = new MetaCallNode(null, cn1, m_OwnerMetaBlockStatements.ownerMetaBase, m_OwnerMetaBlockStatements,
            //                    frontDefineMt);
            //                fmn1.SetFrontCallNode(frontcn);
            //                frontcn = fmn1;
            //            }
            //            else
            //            {
            //                if (leftCallNodeList.Count == i && isAssignSign)
            //                {
            //                    fmte = rightExpress;
            //                }
            //                var fmn2 = new MetaCallNode(cn1, cn2, m_OwnerMetaBlockStatements.ownerMetaBase, m_OwnerMetaBlockStatements,
            //                    frontDefineMt, fmte);
            //                fmn2.SetFrontCallNode(frontcn);
            //                if( i == leftCallNodeList.Count )
            //                {
            //                    if (m_RightMetaExpress == null)
            //                    {
            //                        if (TryParseRightExpress(rightExpress, null ) == false)
            //                        {
            //                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "TryParseRightExpress express parse failed!");
            //                            return;
            //                        }
            //                    }
            //                    _auc.expressNodeList.Add(m_RightMetaExpress);
            //                }

            //                if(fmn2.ParseNode(_auc) == false) 
            //                {
            //                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "bracket express parse failed!");
            //                    return;
            //                }
            //                leftMt = fmn2.metaType;
            //                mcnList.AddRange(fmn2.metaCallNodeList);
            //                frontcn = fmn2;
            //            }


            //        }
            //        else
            //        {
            //            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "MetaAssignStatement build call node List!");
            //            return;
            //        }

            //    }
            //    m_LeftMetaExpress = new MetaCallLink(m_OwnerMetaBlockStatements.ownerMetaBase,
            //        m_OwnerMetaBlockStatements, mcnList, mcnList[mcnList.Count - 1].metaVariable, m_Token);
            //    m_LeftMetaExpress.AddVisitNodeListByNewList(mcnList);
            //}

            //m_LeftMetaExpress = new MetaCallLinkExpressNode(metaCallLink);
            //AllowUseSettings auc = new AllowUseSettings();
            //auc.useNotStatic = false;
            //auc.useNotConst = m_FileMetaOpAssignSyntax?.constToken == null ? false : true;            
            //auc.getterFunction = false;
            //if (m_RightMetaExpress != null )
            //{
            //    auc.setterFunction = true;
            //    auc.expressNodeList.Add(m_RightMetaExpress);
            //}
            //m_LeftMetaExpress.Parse(auc);
            //m_LeftMetaExpress.CalcReturnType();

            //if (m_LeftMetaExpress.metaCallLink.hasNullConditional)
            //{
            //    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 空条件运算符 ?. 不能用于赋值操作的左值（包括字段赋值和 setter 方法调用）!");
            //    return;
            //}

            //bool IsRightSetLeftValue = false;
            //if (m_LeftMetaExpress.metaCallLink.finalCallNode?.visitType == MetaVisitNode.EVisitType.MethodCall)
            //{
            //    var fun = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall.function;
            //    if (fun is MetaMemberFunction mmf)
            //    {
            //        if (mmf.isSet)
            //        {
            //            IsRightSetLeftValue = true;
            //            m_LeftMethodCall = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall;
            //            m_RightMetaExpress = null;

            //            var firstParam = mmf.metaMemberParamCollection.metaDefineParamList[0];
            //            if (firstParam?.metaVariable != null)
            //            {
            //                expressMdt = firstParam.metaVariable.GetFinalMetaType();
            //            }
            //        }
            //        else if (mmf.isGet && mmf.name == "_getItem_")
            //        {
            //            // _getItem_ 调用用于赋值场景：转换为 _setItem_ 调用
            //            var ownerMc = mmf.ownerMetaClass;
            //            if (ownerMc != null)
            //            {
            //                // 先解析右值（needTryGetRight=false 时还未解析）
            //                if (m_RightMetaExpress == null && m_FileMetaOpAssignSyntax?.express != null)
            //                {
            //                    TryParseRightExpress(m_FileMetaOpAssignSyntax.express, null);
            //                }

            //                if (m_RightMetaExpress != null)
            //                {
            //                    // 构建 _setItem_ 参数：index（来自 _getItem_）+ value（来自右值）
            //                    var setItemParam = new MetaInputParamCollection(ownerMc, m_OwnerMetaBlockStatements);
            //                    var leftMc = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall;
            //                    foreach (var ep in leftMc.metaInputParamList)
            //                    {
            //                        setItemParam.AddMetaInputParam(new MetaInputParam(ep));
            //                    }
            //                    setItemParam.AddMetaInputParam(new MetaInputParam(m_RightMetaExpress));

            //                    var setItemMethod = ownerMc.GetMetaDefineGetSetMemberFunctionByName("_setItem_", setItemParam, false, true);
            //                    if (setItemMethod != null)
            //                    {
            //                        IsRightSetLeftValue = true;
            //                        var newMethodCall = new MetaMethodCall(ownerMc, m_OwnerMetaBlockStatements, null,
            //                            setItemMethod, null, setItemParam, null, null, null);
            //                        m_LeftMethodCall = newMethodCall;
            //                        m_RightMetaExpress = null;

            //                        // 获取 value 参数类型用于类型检查
            //                        if (setItemMethod.metaMemberParamCollection.metaDefineParamList.Count > 1)
            //                        {
            //                            var valueParam = setItemMethod.metaMemberParamCollection.metaDefineParamList[1];
            //                            if (valueParam?.metaVariable != null)
            //                            {
            //                                expressMdt = valueParam.metaVariable.GetFinalMetaType();
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}



            //if (m_LeftMetaExpress != null)
            //{
            //    m_MetaVariable = m_LeftMetaExpress.GetDefineMetaVariable();     //这里要使用定义时的变量，因为可能存在 a.b.c += 10; 这种情况，a.b.c 可能在之前被解析成一个临时变量了，这时候就需要回到定义时的变量上去进行类型检查和后续的赋值操作

            //    if(m_MetaVariable != null )
            //    {
            //        if (m_MetaVariable.isConst)
            //        {
            //            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 当前左值声明为 const，不允许进行赋值或修改!!");
            //            return;
            //        }

            //        m_Name = m_MetaVariable.name;
            //        if (m_MetaVariable.isGlobal)
            //        {
            //            if (ownerMetaClass.name == "Project")
            //            {

            //            }
            //            else
            //            {
            //                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "in" + ownerMetaClass.name);
            //            }
            //        }
            //        expressMdt = m_MetaVariable.GetFinalMetaType();
            //    }
            //    else
            //    {
            //        expressMdt = null;
            //    }
            //}

            //if (m_RightMetaExpress == null && express != null )
            //{
            //    if (!TryParseRightExpress(express, expressMdt ))
            //    {
            //        return;
            //    }
            //}

            //if( !IsRightSetLeftValue )
            //{
            //    if (m_RightMetaExpress == null)
            //    {
            //        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error right express is null!");
            //        return;
            //    }
            //    if (m_LeftMethodCall == null)
            //    {
            //        CheckLeftAndRightExpress();
            //    }
            //}

            return;
        }

        private bool TryParseRightExpress(FileMetaBaseTerm express, MetaType rightMetaTypeHint)
        {
            CreateExpressParam cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                metaType = rightMetaTypeHint,
                ownerMetaBase = ownerMetaClass,
                fme = express,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
                equalMetaVariable = m_MetaVariable,
            };
            m_RightMetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
            if (m_RightMetaExpress == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "MetaAssignStatements", m_FileMetaOpAssignSyntax.express.token);
                return false;
            }
            var alus = new AllowUseSettings()
            {
                setterFunction = false,
                getterFunction = true,
                ifNotVariableThenAddVariable = rightMetaTypeHint != null,
                isTryRightExpress = rightMetaTypeHint == null
            };
            this.m_RightMetaExpress.Parse(alus);

            if( this.m_RightMetaExpress.parseSuccessed )
            {
                this.m_RightMetaExpress?.CalcReturnType();
                var newexpress = ExpressManager.ConvertNewExpress(m_RightMetaExpress, rightMetaTypeHint);
                if (newexpress != m_RightMetaExpress && m_RightMetaExpress != null)
                {
                    m_RightMetaExpress = newexpress;
                }

                // Const-fold literal RHS to match the target variable's type,
                // e.g. b8 = 250 where 250 is Int32 but b8 is Byte -> fold to UInt8.
                if (m_RightMetaExpress is MetaConstExpressNode mcen && rightMetaTypeHint != null)
                {
                    ExpressManager.TryAdjustConstExpressByDefineMetaType(rightMetaTypeHint, mcen);
                }
            }
            else
            {
                m_RightMetaExpress = null;
                return false;
            }
            return true;
        }
        void CheckLeftAndRightExpress()
        {
            if (m_RightMetaExpress == null )
            {
                return;
            }

            var leftMt = m_MetaVariable.GetFinalMetaType();
            if (leftMt.metaClass == CoreMetaClassManager.memberMetaClass )
            {
                leftMt = m_MetaVariable.sourceMetaVariable.realMetaType;
            }

            if (TypeManager.CompareLeftRightMetaType(leftMt, m_RightMetaExpress.GetReturnMetaType(), m_Token, out var convertMetaType ) )
            {
                if (convertMetaType != null)
                {
                    m_MetaVariable.SetRealMetaType(convertMetaType);
                }
            }
            else
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "left or right compare failed");
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
                sb.Append(m_LeftMetaExpress.ToFormatString());
            }
            else
            {
                if (m_MetaVariable != null)
                {
                    sb.Append(m_LeftMetaExpress.ToFormatString());
                }
                else
                {
                    sb.Append("NotFind[ " + m_LeftMetaExpress?.ToFormatString());
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
