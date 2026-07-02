//****************************************************************************
//  File:      MetaAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
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
        public MetaCallLinkExpressNode leftMetaExpress => m_LeftMetaExpress;

        private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        private MetaVariable m_MetaVariable = null;
        private EOpSign m_OpSign;
        private ELeftRightOpSign m_AutoAddExpressOpSign;
        private Token m_SignToken = null;

        private MetaCallLinkExpressNode m_LeftMetaExpress;
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

            MetaCallLink metaCallLink = null;
            FileMetaBaseTerm express = null;
            bool needTryGetRight = false;
            if (m_FileMetaOpAssignSyntax != null)
            {
                metaCallLink = new MetaCallLink(m_FileMetaOpAssignSyntax.variableRef,
                m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
                m_SignToken = m_FileMetaOpAssignSyntax?.assignToken;
                var callnodelist = m_FileMetaOpAssignSyntax?.variableRef?.callNodeList;
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
                express = m_FileMetaOpAssignSyntax.express;
                if( express != null && metaCallLink.callNodeList.Count > 1)
                {
                    needTryGetRight = true;
                    if ( m_FileMetaOpAssignSyntax.express is FileMetaCallTerm fmct )
                    {
                        if( fmct.callLink.callNodeList.Count > 0 && fmct.callLink.callNodeList[0].token.type == ETokenType.New )
                        {
                            needTryGetRight = false;
                        }
                    }

                }
            }
            else if( m_FileMetaDefineVariableSyntax != null)
            {
                System.Diagnostics.Debug.Assert(false);
                //metaCallLink = new MetaCallLink(m_FileMetaDefineVariableSyntax.,
                //m_OwnerMetaBlockStatements?.ownerMetaClass, m_OwnerMetaBlockStatements, null, null);
                m_SignToken = m_FileMetaDefineVariableSyntax?.assignToken;
                m_Token = m_FileMetaDefineVariableSyntax?.nameToken;

            }
            if(needTryGetRight )
            {
                TryParseRightExpress(express, null);
            }

            //if (metaCallLink == null)
            //{
            //    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error MetaAssignStatements ParseDefine!!!" + m_FileMetaOpAssignSyntax?.variableRef?.ToTokenString());
            //    return;
            //}

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

            if (m_LeftMetaExpress.metaCallLink.hasNullConditional)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 空条件运算符 ?. 不能用于赋值操作的左值（包括字段赋值和 setter 方法调用）!");
                return;
            }

            bool IsRightSetLeftValue = false;
            if (m_LeftMetaExpress.metaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                var fun = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall.function;
                if (fun is MetaMemberFunction mmf)
                {
                    if (mmf.isSet)
                    {
                        IsRightSetLeftValue = true;
                        m_LeftMethodCall = m_LeftMetaExpress.metaCallLink.finalCallNode.methodCall;
                        m_RightMetaExpress = null;

                        var firstParam = mmf.metaMemberParamCollection.metaDefineParamList[0];
                        if (firstParam?.metaVariable != null)
                        {
                            expressMdt = firstParam.metaVariable.GetFinalMetaType();
                        }
                    }
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error not support operator sign " + ett.ToString());
                    }
                    break;
            }
            if(m_OpSign != EOpSign.None )
            {
                if (m_LeftMetaExpress.metaCallLink.finalCallNode.visitType == MetaVisitNode.EVisitType.VisitVariable)
                {
                    m_LeftMetaExpress.metaCallLink.finalCallNode.visitVariable?.SetNotUseFast();
                }
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

            if (m_RightMetaExpress == null && express != null )
            {
                if (!TryParseRightExpress(express, expressMdt ))
                {
                    return;
                }
            }

            if( !IsRightSetLeftValue )
            {
                if (m_RightMetaExpress == null)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error right express is null!");
                    return;
                }
                if (m_LeftMethodCall == null)
                {
                    CheckLeftAndRightExpress();
                }
            }

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
            if (m_RightMetaExpress == null || m_MetaVariable == null)
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
