//****************************************************************************
//  File:      MetaCallNode.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/5/17 12:00:00
//  Description:  this's a calllink's node handle
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum ECallNodeSign
    {
        Null,
        Period,
        NullConditional,
        Pointer,
    }
    public enum ECallNodeType
    {
        None,
        MetaNode,
        MetaType,
        ClassName,
        TemplateName,
        EnumName,
        EnumMember,
        DataName,
        //DataValue,
        FunctionInnerVariableName,
        VisitVariable,
        IteratorVariable,
        MemberVariableName,
        //MemberDataName,
        NewClass,
        NewTemplate,
        NewData,
        MemberFunctionName,
        FunctionCall,
        SystemFunctionCall,
        ConstValue,
        This,
        Base,
        Local,
        Global,
        Express,
        GetType,
        ClosureCall,
    }
    public enum EParseFrom
    {
        None,
        MemberVariableExpress,
        InputParamExpress,
        StatementLeftExpress,
        StatementRightExpress,
    }
    public sealed class AllowUseSettings
    {
        public bool useNotStatic = false;
        public bool useNotConst = true;
        public bool callFunction = true;
        public bool callConstructFunction = true;
        public bool setterFunction = false;
        public bool getterFunction = true;
        public bool ifNotVariableThenAddVariable = true;
        public bool isTryRightExpress = false;
        public EParseFrom parseFrom { get; set; }

        public AllowUseSettings()
        {

        }

        public AllowUseSettings(AllowUseSettings clone)
        {
            useNotStatic = clone.useNotStatic;
            useNotConst = clone.useNotConst;
            callFunction = clone.callFunction;
            callConstructFunction = clone.callConstructFunction;
            setterFunction = clone.setterFunction;
            getterFunction = clone.getterFunction;
            isTryRightExpress = clone.isTryRightExpress;
            ifNotVariableThenAddVariable = clone.ifNotVariableThenAddVariable;
        }
    }
    public sealed class MetaCallNode
    {
        public string name => m_Name;
        public Token token => m_Token;
        public ECallNodeType callNodeType => m_CallNodeType;
        public ECallNodeSign callNodeSign => m_CallNodeSign;
        public List<MetaCallNode> metaCallNodeList => m_MetaCallNodeList;
        public bool isQuestionMarkDot => m_CallNodeSign == ECallNodeSign.NullConditional;
        public MetaExpressNodeBase metaExpressValue => m_ExpressNode;
        public List<MetaExpressNodeBase> bracketExpressList => m_BracketExpressList;
        public List<MetaType> metaTemplateParamsList => m_MetaTemplateParamsList;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;
        public MetaClass ownerMetaClass => m_OwnerMetaBase as MetaClass;
        public MetaData ownerMetaData => m_OwnerMetaBase as MetaData;
        public MetaEnum ownerMetaEnum => m_OwnerMetaBase as MetaEnum;
        public MetaBase ownerMetaBase => m_OwnerMetaBase;
        public MetaBlockStatements ownerMetaFunctionBlock => m_OwnerMetaFunctionBlock;
        public MetaVariable defineMetaVariable => m_DefineMetaVariable; 
        public FileMetaBraceTerm fileMetaBraceTerm => m_FileMetaCallNode != null ? m_FileMetaCallNode.fileMetaBraceTerm : null;
        public FileMetaParTerm fileMetaParTerm => m_FileMetaCallNode != null ? m_FileMetaCallNode.fileMetaParTerm : null;
        public MetaType staticCallMetaType => m_StaticCallMetaType;
        public MetaEnum metaEnum => m_MetaEnum;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaTemplate metaTemplate => m_MetaTemplate;
        public MetaFunction metaFunction => m_MetaFunction;
        public MetaType metaType => m_MetaType;

        private AllowUseSettings m_AllowUseSettings;
        private ECallNodeType m_CallNodeType = ECallNodeType.None;
        private ECallNodeSign m_CallNodeSign = ECallNodeSign.Null;
        public bool m_IsArray = false;
        public bool m_IsFunction = false;
        private Token m_Token = null;

        private MetaCallNode m_FrontCallNode = null;
        private FileMetaCallNode m_FileMetaCallSign = null;
        private FileMetaCallNode m_FileMetaCallNode = null;
        private MetaExpressNodeBase m_InputExpressNode = null;
        private MetaType m_StaticCallMetaType = null;
        private MetaBlockStatements m_OwnerMetaFunctionBlock = null;
        private MetaBase m_OwnerMetaBase = null;
        private MetaInputParamCollection m_MetaInputParamCollection = null;
        private List<MetaType> m_MetaTemplateParamsList = new List<MetaType>();
        private MetaExpressNodeBase m_ExpressNode = null;    // a+b+([expressNode[3+20+10.0f]).ToString() 娑擃厾娈?+20+10.f鐏忚鲸妲哥悰銊с仛瀵?, fun(expressNode)
        private MetaVariable m_StoreMetaVariable = null;        // store metaVariable 鍍?a.val = new(){} val灏辨槸store 
        private MetaVariable m_DefineMetaVariable = null;       // define variable 瀹氫箟鍙橀噺锛屾槸姣斿 鍍弒et鏂规硶锛屽瑙ｆ瀽鏈夌害鏉熶綔鐢?姣斿 a.set( value ); value鐨勫嚱鏁板畾涔夊氨鏄畾涔夊彉閲?鏄浼犺繘鏉ョ殑锛岃€屼笉鐢ㄨ嚜宸卞啀鍒涘缓涓€涓彉閲?
        private List<MetaExpressNodeBase> m_BracketExpressList = new List<MetaExpressNodeBase>();   // a[1][1][1][]   鐟欙絾鐎介惃鍕Ц鏉╂瑤閲滈柌宀冪珶閻?,閹存牞鈧懏妲搁崷鈺梋闁插矁绔熼惃鍕綁闁?

        private MetaNode m_MetaNode = null;
        private MetaType m_MetaType = null;
        private MetaClass m_MetaClass = null;
        private MetaData m_MetaData = null;
        private MetaEnum m_MetaEnum = null;
        private MetaTemplate m_MetaTemplate = null;
        private MetaVariable m_MetaVariable = null;
        private MetaFunction m_MetaFunction = null;
        private string m_Name;
        private FileMetaBaseTerm m_FileRightExpress = null;
        private MetaExpressNodeBase m_RightExpress = null;
        private List<MetaCallNode> m_MetaCallNodeList = new List<MetaCallNode>();

        private bool m_VisitFlag = false;

        public MetaCallNode()
        { }
        public MetaCallNode(MetaExpressNodeBase mcen, MetaBase owmc, MetaBlockStatements mbs)
        {
            m_InputExpressNode = mcen;
            m_OwnerMetaBase = owmc;
            m_OwnerMetaFunctionBlock = mbs;
        }
        public MetaCallNode(FileMetaCallNode fmcn1, FileMetaCallNode fmcn2, MetaBase owmc, MetaBlockStatements mbs, 
            FileMetaBaseTerm rightExpress = null )
        {
            m_FileMetaCallSign = fmcn1;
            m_FileMetaCallNode = fmcn2;
            m_Token = m_FileMetaCallNode?.token;
            m_OwnerMetaBase = owmc;
            m_OwnerMetaFunctionBlock = mbs;
            m_FileRightExpress = rightExpress;

            if (m_FileMetaCallSign != null)
            {
                if (m_FileMetaCallSign.token.type == ETokenType.Period)
                {
                    m_CallNodeSign = ECallNodeSign.Period;
                }
                else if (m_FileMetaCallSign.token?.type == ETokenType.QuestionMarkDot)
                {
                    m_CallNodeSign = ECallNodeSign.NullConditional;
                }
                else if (m_FileMetaCallSign.questionMarkDotToken != null)
                {
                    m_CallNodeSign = ECallNodeSign.NullConditional;
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_FileMetaCallSign.token, "Error MetaStatements Parse  token == questionMarkDotToken !");
                    return;
                }
                else if (m_FileMetaCallSign.token.type == ETokenType.And)
                {
                    m_CallNodeSign = ECallNodeSign.Pointer;
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_FileMetaCallSign.token, "Error MetaStatements Parse  token == And !");
                    return;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_FileMetaCallSign.token, "Error MetaStatements Parse  token 顑?!");
                    return;
                }
            }

            // Sometimes the parser keeps the argument parTerm but doesn't set isCallFunction.
            // If this node is an identifier and has parTerm, treat it as a function-call node
            // so we can build MetaInputParamCollection for argument resolution.
            m_IsFunction = m_FileMetaCallNode.isCallFunction
                           || (m_FileMetaCallNode.fileMetaParTerm != null && m_Token?.type == ETokenType.Identifier);

            m_IsArray = m_FileMetaCallNode.isArray;
            /*
            if (m_FileMetaCallNode.fileMetaBraceTerm != null)
            {
                m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
            }
            */
        }
        public void SetAllowUseSettings( AllowUseSettings alus )
        {
            m_AllowUseSettings = alus;
        }
        public void SetToken( Token token )
        {
            this.m_Token = token;
        }
        public void SetRightExpress(MetaExpressNodeBase menb )
        {
            this.m_RightExpress = menb;
        }
        public void SetFrontCallNode(MetaCallNode mcn)
        {
            m_FrontCallNode = mcn;
        }
        public void SetStoreMetaVariable(MetaVariable mv)
        {
            this.m_StoreMetaVariable = mv;
        }
        public void SetDefineMetaVariable( MetaVariable mv )
        {
            this.m_DefineMetaVariable = mv;
        }
        public bool ParseNode(AllowUseSettings _auc)
        {
            bool flag = false;
            m_AllowUseSettings = _auc;

            TryGetRightExpress(null, null);

            if (m_InputExpressNode != null)
            {
                flag = FindArrayNode();
            }
            else
            {
                if (m_FileMetaCallNode == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 111111! " + m_Token.ToLexemeAllString());
                }
                if (m_FileMetaCallNode != null && m_FileMetaCallNode.fileMetaParTerm != null && !m_IsFunction)
                {
                    var firstNode = m_FileMetaCallNode.fileMetaParTerm.fileMetaExpressList[0];
                    if (firstNode == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 123123123!");
                    }
                    else
                    {
                        CreateExpressParam cep = new CreateExpressParam()
                        {
                            ownerMetaBase = m_OwnerMetaBase,
                            ownerMBS = m_OwnerMetaFunctionBlock,
                            metaType = null,
                            fme = firstNode,
                        };
                        m_ExpressNode = ExpressManager.CreateExpressNode(cep);
                        m_ExpressNode.Parse(_auc);
                        m_ExpressNode.CalcReturnType();
                        m_MetaType = m_ExpressNode.GetReturnMetaType();
                        m_CallNodeType = ECallNodeType.Express;
                        m_MetaCallNodeList.Add(this);
                        return true;
                    }
                }
                else
                {
                    flag = CreateCallNode();
                }
                if (!flag) return false;
                m_MetaCallNodeList.Add(this);


                var frontcn = this;
                for (int i = 0; i < m_FileMetaCallNode.fileMetaBracketTermList.Count; i++)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.fme = m_FileMetaCallNode.fileMetaBracketTermList[i];
                    cep.equalMetaVariable = null;
                    cep.metaType = null;
                    cep.ownerMBS = m_OwnerMetaFunctionBlock;
                    cep.ownerMetaBase = m_OwnerMetaFunctionBlock.ownerMetaBase;

                    var en = ExpressManager.CreateExpressNodeByCEP(cep);
                    en.Parse(_auc);
                    if (!en.parseSuccessed )
                    {
                        return false;
                    }
                    m_BracketExpressList.Add(en);

                    if (frontcn.callNodeType == ECallNodeType.MemberVariableName
                      || frontcn.callNodeType == ECallNodeType.FunctionInnerVariableName
                      || frontcn.callNodeType == ECallNodeType.VisitVariable
                            )
                    {
                        MetaCallNode mcn = new MetaCallNode(en, m_OwnerMetaFunctionBlock.ownerMetaClass, m_OwnerMetaFunctionBlock);
                        mcn.SetFrontCallNode(frontcn);
                        mcn.SetRightExpress(m_RightExpress);
                        if (!mcn.ParseNode(_auc))
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "bracket express parse failed!");
                            return false;
                        }
                        m_MetaCallNodeList.Add(mcn);
                        frontcn = mcn;
                    }


                    TryGetRightExpress(frontcn?.metaType, frontcn?.metaVariable );
                }
            }
            return flag;
        }
        void TryGetRightExpress( MetaType mt, MetaVariable mv )
        {
            if (m_RightExpress != null) return;

            if (m_FileRightExpress != null )
            {
                if(m_FileRightExpress is FileMetaCallTerm fmct && mv == null )
                {
                    if(fmct.callLink?.callNodeList?.Count == 1 )
                    {
                        var token = fmct.callLink.callNodeList[0].token;
                        if (token?.type == ETokenType.New )
                        {
                            return;
                        }
                    }
                }

                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = m_FileRightExpress;
                cep.equalMetaVariable = mv;
                cep.metaType = mt;
                cep.ownerMBS = m_OwnerMetaFunctionBlock;
                cep.ownerMetaBase = m_OwnerMetaFunctionBlock.ownerMetaBase;

                m_RightExpress = ExpressManager.CreateExpressNodeByCEP(cep);
                m_RightExpress.Parse(new AllowUseSettings() { isTryRightExpress = mt == null });
                if (!m_RightExpress.parseSuccessed)
                {
                    m_RightExpress = null;
                    return;
                }
                // Compute the return type right after parsing (same as
                // MetaAssignStatements.TryParseRightExpress does). Lazy nodes
                // such as MetaUnaryOpExpressNode (e.g. `-9`) keep
                // m_ExpressReturnMetaType null until CalcReturnType() runs;
                // _setItem_ parameter matching reads GetReturnMetaType(), and
                // a null there makes the match fail so a subscript write with
                // a negative literal silently mis-binds.
                m_RightExpress.CalcReturnType();
            }
        }
        bool FindArrayNode()
        {
            if (m_InputExpressNode is MetaConstExpressNode mcen)
            {
                m_ExpressNode = mcen;
                m_VisitFlag = true;
                m_Name = mcen.value.ToString();
                HandleVisit();
            }
            else if (m_InputExpressNode is MetaCallLinkExpressNode mclen)
            {
                m_ExpressNode = mclen;
                m_VisitFlag = true;
                m_Name = mclen.metaCallLink.finalCallNode.variable.name;
                HandleVisit();
            }
            else if (m_InputExpressNode is MetaArrayExpressNode maen2)
            {
                if (maen2.metaCallArray.Count == 1)
                {
                    var maen3 = maen2.metaCallArray[0];
                    if (maen3 is MetaConstExpressNode mcen2)
                    {
                        m_ExpressNode = mcen2;
                        m_VisitFlag = true;
                        m_Name = mcen2.value.ToString();
                        HandleVisit();
                    }
                    else if (maen3 is MetaCallLinkExpressNode mclen2)
                    {
                        m_ExpressNode = mclen2;
                        m_VisitFlag = true;
                        m_Name = mclen2.metaCallLink.finalCallNode.variable.name;
                        HandleVisit();
                    }
                    else if (maen3 is MetaOpExpressNode moen)
                    {
                        m_ExpressNode = moen;
                        m_VisitFlag = true;
                        m_Name = "express" + moen.GetHashCode().ToString();
                        HandleVisit();
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
                else
                {
                    HandleVisit();
                    //if (fn.metaClass is MetaGenTemplateClass mgtc )
                    //{
                    //}
                    //else
                    //{
                    //    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error is fained11 ");
                    //}
                }
            }
            else
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error is fained222 " );
            }
            return true;
        }
        bool CreateCallNode()
        {
            m_Token = m_FileMetaCallNode?.token;
            int tokenLine = m_FileMetaCallNode?.token != null ? m_FileMetaCallNode.token.sourceBeginLine : -1;
            m_Name = m_FileMetaCallNode.name;

            string fatherName = m_FrontCallNode?.m_Name;
            bool isAt = m_FileMetaCallNode?.atToken != null || m_VisitFlag;
            bool isFirst = m_FrontCallNode == null;
            int templateCount = this.m_FileMetaCallNode.inputTemplateNodeList.Count;


            ETokenType etype = m_Token.type;
            ECallNodeType frontCNT = ECallNodeType.None;
            if (!isFirst)
            {
                frontCNT = m_FrontCallNode.callNodeType;
            }

            if (m_IsFunction)
            {
                m_MetaInputParamCollection = new MetaInputParamCollection(m_FileMetaCallNode.fileMetaParTerm, ownerMetaBase, m_OwnerMetaFunctionBlock);
                m_MetaInputParamCollection.Parse( m_AllowUseSettings );
                m_MetaInputParamCollection.CaleReturnType();
            }

            if (etype == ETokenType.Number
                || etype == ETokenType.String
                || etype == ETokenType.Boolean)
            {
                bool isNotConstValue = false;
                if (frontCNT == ECallNodeType.FunctionInnerVariableName
                    || frontCNT == ECallNodeType.MemberVariableName
                    || frontCNT == ECallNodeType.VisitVariable)
                {
                    m_ExpressNode = new MetaConstExpressNode(m_Token.GetEType(), m_Token.lexeme);
                    // Array1.$0.x   Array1.1.x;
                    if (isAt)                  //Array.$
                    {
                        isNotConstValue = true;
                        HandleVisit();
                    }
                }
                //else if (frontCNT == ECallNodeType.MemberDataName)
                //{
                //    MetaMemberData mmd = m_FrontCallNode.m_MetaVariable as MetaMemberData;
                //    if (mmd != null)
                //    {
                //        if (mmd.memberDataType == EMemberDataType.MemberArray)
                //        {
                //            if (isAt)                  //Array.$
                //            {
                //                string inputMVName = m_Name;
                //                var arrayFieldData = mmd.GetFinalMetaType()?.metaData;
                //                m_MetaVariable = arrayFieldData?.GetMemberDataByName(inputMVName);           //Array.@var
                //                if (m_MetaVariable == null)
                //                {
                //                    MetaCallLink clink = new MetaCallLink(MetaVisitNode.CreateByVariable(m_MetaVariable));
                //                    m_MetaVariable = new MetaVisitVariable(inputMVName, ownerMetaClass, m_OwnerMetaFunctionBlock,
                //                        mmd, clink);
                //                    mmd.AddMetaVariable(m_MetaVariable);
                //                }
                //                isNotConstValue = true;
                //            }
                //            else
                //            {
                //                //Array1.0.x 娑撳秴鍘戠拋?
                //                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 閸λ媟ray.閸氬氦绔熸俊鍌涚亯娴ｈ法鏁ら崣姗€鍣洪幋鏍偓鍛Ц閺佹澘鐡х敮鎼佸櫤閿涘苯绻€妞よ濞囬悽藡rray.$閺傜懓绱?!");
                //            }
                //        }
                //    }
                //}
                if (!isNotConstValue)
                {
                    FileMetaConstValueTerm fmcvt = new FileMetaConstValueTerm(m_FileMetaCallNode.fileMeta, m_Token);
                    m_ExpressNode = new MetaConstExpressNode(m_OwnerMetaBase, m_OwnerMetaFunctionBlock, fmcvt);
                    m_ExpressNode.Parse(m_AllowUseSettings);
                    m_CallNodeType = ECallNodeType.ConstValue;
                    m_MetaType = m_ExpressNode.GetReturnMetaType();
                    m_MetaClass = m_MetaType.metaClass;
                }
                //else
                //{
                //    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error not const value");
                //}
            }
            else if ( etype == ETokenType.Global)
            {
                if (!isFirst)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global can only be used at first position." + m_Token.ToLexemeAllString());
                    return false;
                }
                
                if (m_IsFunction)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "global now allow function");
                    return false;
                }
                else
                {
                    // New behavior: global.xxx reads from Project{} static members in .sp.
                    var projectMc = ClassManager.instance.TryGetProjectMetaClass();
                    if (projectMc != null)
                    {
                        m_MetaClass = projectMc;
                        m_MetaType = new MetaType(projectMc);
                        m_CallNodeType = ECallNodeType.Global;
                        return true;
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "global now allow function");
                        return false;
                    }
                }                
            }
            else if (etype == ETokenType.New)
            {
                if (!isFirst)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error new can only be used at first position." + m_Token.ToLexemeAllString());
                    return false;       
                }
                if (!m_IsFunction)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error new cannot be used as non-function form." + m_Token.ToLexemeAllString());
                    return false;
                }
                else
                {
                    if ( m_DefineMetaVariable == null)
                    {
                        if (m_AllowUseSettings.isTryRightExpress == false)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error missing front define meta type." + m_Token.ToLexemeAllString());
                        }
                        return false;
                    }
                    m_MetaType = m_DefineMetaVariable.GetFinalMetaType();
                    if (m_MetaType.eMetaTypeType == EMetaTypeType.Template)
                    {
                        m_MetaTemplate = m_MetaType.metaTemplate;
                        m_CallNodeType = ECallNodeType.NewTemplate;
                        MetaMemberFunction mmf = m_MetaType.metaClass.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, m_MetaInputParamCollection);
                        if (mmf == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 111" + m_MetaType.metaClass.allName + "init!)", m_Token);
                            return false;
                        }
                        m_MetaType = new MetaType(m_MetaTemplate, "");
                        this.m_MetaFunction = mmf;
                    }
                    else if (m_MetaType.eMetaTypeType == EMetaTypeType.MetaClass)
                    {
                        m_MetaClass = m_MetaType.metaClass;
                        m_CallNodeType = ECallNodeType.NewClass;
                    }
                    else if(m_MetaType.eMetaTypeType == EMetaTypeType.MetaData )
                    {
                        m_MetaData = m_MetaType.metaData;
                        m_CallNodeType = ECallNodeType.NewData;
                    }
                    else
                    {
                        m_CallNodeType = ECallNodeType.NewTemplate;
                        m_MetaClass = m_MetaType.metaClass;
                    }
                }
            }
            else if (etype == ETokenType.This)
            {
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.MemberVariableExpress)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error this is not allowed in member variable expression." + m_Token.ToLexemeAllString());
                }
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error this is not allowed in input parameter expression." + m_Token.ToLexemeAllString());
                }
                //this.
                if (!isFirst)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error this can only be used at first position." + m_Token.ToLexemeAllString());
                    return false;
                }
                if (m_IsFunction)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error this()?!" + m_Token.ToLexemeAllString());
                    return false;
                }
                
                m_MetaClass = ownerMetaClass;
                MetaMemberFunction mmf = m_OwnerMetaFunctionBlock.ownerMetaFunction as MetaMemberFunction;
                m_CallNodeType = ECallNodeType.This;
                // 闂寘鍑芥暟: this 浠庡涓诲疄渚嬫柟娉曟崟鑾风殑 this 鑾峰彇
                if (mmf != null && mmf.isClosureFunction)
                {
                    m_MetaVariable = mmf.capturedThis;
                    if (m_MetaVariable == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error 闂寘鍦ㄩ潤鎬佹柟娉曚腑瀹氫箟, 涓嶈兘浣跨敤 this!");
                        return false;
                    }
                }
                else
                {
                    m_MetaVariable = mmf?.thisMetaVariable;
                    if (m_MetaVariable == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage,  m_Token, "Error static function cannot use this.");
                        return false;
                    }
                    if (mmf?.isStatic == true && m_MetaVariable.isStatic == false)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error static function cannot use this.");
                        return false;
                    }
                }
                m_MetaType = new MetaType(m_MetaVariable.GetFinalMetaType());
            }
            else if (etype == ETokenType.Base)
            {
                if (!isFirst)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error base can only be used at first position." + m_Token.ToLexemeAllString());
                    return false;
                }
                if (m_IsFunction)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error base cannot be used as function form.");
                    return false;
                }
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.MemberVariableExpress)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error base is not allowed in member variable expression." + m_Token.ToLexemeAllString());
                }
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error base is not allowed in input parameter expression." + m_Token.ToLexemeAllString());
                }

                MetaClass owningMc = ownerMetaClass;
                if (owningMc == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error base requires class context.");
                    return false;
                }

                MetaType parentMetaType = owningMc.extendClassMetaType;
                if (parentMetaType == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error base parent class not found.");
                    return false;
                }
                m_MetaType = parentMetaType;
                m_MetaClass = m_MetaType.metaClass;
                m_MetaVariable = (m_OwnerMetaFunctionBlock.ownerMetaFunction as MetaMemberFunction).thisMetaVariable;
                m_CallNodeType = ECallNodeType.Base;                
            }
            else if (etype == ETokenType.Local)
            {
                if (!isFirst)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error local can only be used at first position." + m_Token.ToLexemeAllString());
                    return false;
                }
                if (m_IsFunction)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error local cannot be used as function call form." + m_Token.ToLexemeAllString());
                    return false;
                }

                // local.xxx => <FileName>_Local.instance.xxx
                // The LocalManager creates a <FileName>_Local class with a static
                // member variable `instance` (typed as the class itself).
                var fm = m_FileMetaCallNode?.fileMeta;
                if (fm == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error local resolve failed: fileMeta is null");
                    return false;
                }
                if (fm.GetFileMetaLocalSyntax() == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error current file does not define local{}, cannot use local.xxx");
                    return false;
                }

                var localMc = LocalManager.instance.GetFileLocalClass(fm);
                if (localMc == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error local class not found for file: " + fm.path);
                    return false;
                }
                var instanceMv = localMc.GetMetaMemberVariableByName("instance");
                if (instanceMv == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error local instance member not found on class: " + localMc.name);
                    return false;
                }
                m_MetaClass = localMc;
                m_MetaType = new MetaType(localMc);
                m_MetaVariable = instanceMv;
                m_CallNodeType = ECallNodeType.Local;
                m_StaticCallMetaType = m_MetaType;
                return true;
            }
            else if ( etype == ETokenType.Identifier || etype == ETokenType.Type)
            {
                if (isFirst)
                {
                    // Class1. ns. Int32[]
                    if (GetFirstNode(m_Name, ownerMetaBase, this.m_FileMetaCallNode.inputTemplateNodeList.Count) == false)
                    {
                        return false;
                    }
                    // If the syntax is a call on a class name like `ClassName(...)` treat as instantiation attempt.
                    // Disallow instantiation of abstract classes early during parsing.
                    //if (m_FileMetaCallNode != null && m_FileMetaCallNode.fileMetaParTerm != null)
                    //{
                    //    if (m_MetaClass != null && m_MetaClass.isAbstractClass)
                    //    {
                    //        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 娑撳秷鍏樼€圭偘绶ラ崠鏍ㄥ▕鐠烇紕琚? " + m_MetaClass.name + " " + m_Token.ToLexemeAllString());
                    //        Debug.Assert(false);
                    //        return false;
                    //    }
                    //}
                }
                else
                {
                    if (frontCNT == ECallNodeType.MetaNode)
                    {
                        MetaNode mn = null;
                        if (m_FrontCallNode.m_MetaNode.isMetaNamespace)
                        {
                            if (m_FrontCallNode.m_MetaNode.metaNamespace.refFromType == RefFromType.CSharp)
                            {
                                mn = SimpleLanguage.CSharp.CSharpManager.FindAndCreateMetaNode(m_FrontCallNode.m_MetaNode, m_Name);
                                if (mn.IsMetaClass())
                                {
                                    m_MetaClass = mn.GetMetaClassByTemplateCount(0);
                                    m_CallNodeType = ECallNodeType.ClassName;
                                    m_MetaType = new MetaType(m_MetaClass);
                                }
                                else if (mn.isMetaNamespace)
                                {
                                    m_MetaNode = mn;
                                    m_CallNodeType = ECallNodeType.MetaNode;
                                }
                            }
                        }

                        if (mn == null)
                        {
                            mn = m_FrontCallNode.m_MetaNode.GetChildrenMetaNodeByName(m_Name);
                            if (mn != null)
                            {
                                m_MetaNode = mn;
                                m_CallNodeType = ECallNodeType.MetaNode;
                                if( mn.isMetaNamespace )
                                { }
                                else if (mn.isMetaData)
                                {
                                    m_MetaData = mn.metaData;
                                    m_MetaType = new MetaType(m_MetaData);
                                    m_CallNodeType = ECallNodeType.DataName;
                                }
                                else if (mn.isMetaEnum)
                                {
                                    m_MetaEnum = mn.metaEnum;
                                    m_MetaType = new MetaType(m_MetaEnum);
                                    m_CallNodeType = ECallNodeType.EnumName;
                                }
                                else if (mn.IsMetaClass())
                                {
                                    m_MetaClass = mn.GetMetaClassByTemplateCount(this.m_FileMetaCallNode.inputTemplateNodeList.Count);
                                    // Keep in sync with GetFirstNode's class branch: a mid-link element
                                    // that resolves to a class (e.g. `Map` in `Std.Map<int,int>(8)`) must
                                    // also be flagged ClassName when it carries template args. Otherwise
                                    // callNodeType stays MetaNode, the generic instantiation branch below
                                    // (ClassName + metaTemplateParamsList) never runs, and New falls back
                                    // to the raw template class id; the VM then resolves that id to the
                                    // FIRST registered instantiation (e.g. Map<Int32,String>) and builds
                                    // an object with wrong generic arguments.
                                    if (m_MetaClass != null
                                        && this.m_FileMetaCallNode.inputTemplateNodeList.Count > 0)
                                    {
                                        m_CallNodeType = ECallNodeType.ClassName;
                                    }
                                    m_MetaType = new MetaType(m_MetaClass);
                                }
                                else
                                {
                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error not found type");
                                }
                            }
                            else if(m_FrontCallNode.m_MetaClass != null )
                            {
                                HandleMetaClass( m_CallNodeType, templateCount);
                            }
                            else if( m_FrontCallNode.m_MetaNode.isMetaModule )
                            {
                                // ModuleName.name：模块根下的 enum/data/class/namespace 未命中时，
                                // 继续遍历该模块 Project 类定义的静态成员（变量/函数）。
                                // 例如引用 Std 模块后，Std.Pi 取 Std 工程 Project 定义的静态成员 Pi。
                                HandleModuleProjectMember();
                            }
                        }
                    }
                    //else if (frontCNT == ECallNodeType.TypeName)
                    //{
                    //    HandleGetTypeByMetaType(m_FrontCallNode.metaType);
                    //}
                    else if (frontCNT == ECallNodeType.ClassName
                        || frontCNT == ECallNodeType.MetaType)
                    {
                        //if (m_MetaTemplateParamsList.Count > 0)
                        //{
                        //    var ngmc = m_MetaClass.AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList(m_MetaTemplateParamsList);

                        //    if (ngmc is MetaGenTemplateClass mgtc)
                        //    {
                        //        mgtc.ParseGenTemplateClass(mgtc);
                        //        mgtc.ParseGenMemberVarible();
                        //        m_MetaClass = mgtc;
                        //        List<MetaType> listmt2 = new List<MetaType>();
                        //        for (int i = 0; i < mgtc.metaTemplateClass.metaTemplateList.Count; i++)
                        //        {
                        //            listmt2.Add(new MetaType(mgtc.metaTemplateClass.metaTemplateList[i]));
                        //        }
                        //        m_MetaType = new MetaType(mgtc, listmt2);
                        //    }
                        //    else
                        //    {
                        //        m_MetaType = new MetaType(m_MetaClass, m_MetaTemplateParamsList);
                        //    }

                        //}
                        HandleMetaClass(m_FrontCallNode.callNodeType, templateCount);
                    }
                    else if (frontCNT == ECallNodeType.Global)
                    {
                        HandleMetaClass(m_FrontCallNode.callNodeType, templateCount);
                    }
                    else if (frontCNT == ECallNodeType.DataName)
                    {
                        var retmmd = m_FrontCallNode.m_MetaData.GetMemberDataByName(m_Name);
                        m_StaticCallMetaType = new MetaType(m_FrontCallNode.m_MetaData);
                        if (retmmd == null)
                        {
                            if (GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.dataMetaClass, m_Name))
                            {

                            }
                            else
                            {
                                Log.AddMetaCoreLog(LID.MetaCoreNotFoundMetaMemberVariable, m_Token, $"data name not found",
                                    m_FrontCallNode.m_MetaData.allName, m_Name);
                                return false;
                            }
                        }
                        else
                        {
                            m_MetaType = new MetaType(m_FrontCallNode.m_MetaData);
                            m_MetaVariable = retmmd;
                            if (retmmd.memberDataType == EMemberDataType.MemberClass)
                            {
                                m_CallNodeType = ECallNodeType.MemberVariableName;
                            }
                            else if (retmmd.memberDataType == EMemberDataType.ConstValue)
                            {
                                if (m_MetaVariable.isConst)
                                {
                                    //杩欏潡锛屽彲浠ュ啓鎴愬父閲忔ā寮?
                                    //m_CallNodeType = ECallNodeType.ConstValue;
                                    //EType etyp = CoreMetaClassManager.GetETypeByMetaClass(m_MetaVariable.GetFinalMetaType().metaClass);
                                    //this.m_ExpressNode = new MetaConstExpressNode(etyp, m_MetaVariable.)
                                    m_CallNodeType = ECallNodeType.MemberVariableName;
                                }
                                else
                                {
                                    m_CallNodeType = ECallNodeType.MemberVariableName;
                                }
                            }
                            else if (retmmd.memberDataType == EMemberDataType.MemberArray)
                            {
                                m_MetaClass = retmmd.GetFinalMetaType()?.metaClass;
                            }
                            else if (retmmd.memberDataType == EMemberDataType.MemberData)
                            {
                                m_CallNodeType = ECallNodeType.MemberVariableName;
                                m_MetaVariable = retmmd;
                            }
                            else
                            {
                                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "not found memberDataType");
                            }
                        }
                    }
                    else if (frontCNT == ECallNodeType.EnumName)
                    {
                        MetaMemberVariable mmv = null;
                        if ( m_Name == "values")
                        {
                            mmv = m_FrontCallNode.m_MetaEnum.GetOrCreateValuesVariable() as MetaMemberVariable;
                        }
                        else
                        {
                            mmv = m_FrontCallNode.m_MetaEnum.GetMetaMemberVariableByName(m_Name);
                        }
                        if (mmv == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, m_FrontCallNode.m_MetaEnum.name + "not found enum.member?" + m_Name);
                            return false;
                        }
                        
                        if (m_IsFunction)// Enum e = Enum.MetaVaraible( 2 )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, m_FrontCallNode.m_MetaEnum.name + "(" + m_Name + ")" + "not allow!");
                            return false;
                        }
                        else
                        {
                            m_MetaVariable = mmv;
                            m_CallNodeType = ECallNodeType.EnumMember; 
                            if (m_FrontCallNode?.metaEnum != null)
                            {
                                m_MetaType = new MetaType(mmv);
                            }
                            m_StaticCallMetaType = m_MetaType;
                        }                        
                    }
                    else if (frontCNT == ECallNodeType.FunctionInnerVariableName
                        || frontCNT == ECallNodeType.MemberVariableName
                        || frontCNT == ECallNodeType.VisitVariable
                        || frontCNT == ECallNodeType.EnumMember )
                    {
                        HandleMetaVariable(m_FrontCallNode.m_MetaVariable, isAt );
                    }
                    else if (frontCNT == ECallNodeType.ClosureCall)
                    {
                        // 闂寘璋冪敤缁撴灉鐨勯摼寮忚闂? 鎸夐棴鍖呰繑鍥炵被鍨嬭В鏋愭垚鍛?(涓庢櫘閫氬嚱鏁拌皟鐢ㄨ繑鍥炲€奸摼寮忚闂竴鑷?
                        MetaType closureRetMT = m_FrontCallNode.m_MetaType;
                        MetaClass closureRetMC = closureRetMT?.metaClass;
                        if (closureRetMC != null)
                        {
                            if (GetFunctionOrVariableByOwnerClass(closureRetMC, m_Name) == false)
                            {
                                return false;
                            }
                            var cv = MetaClosureVariable.ResolveClosureVariable(m_FrontCallNode.m_MetaVariable);
                            m_StoreMetaVariable = cv?.closureDefineStatements?.closureFunction?.returnMetaVariable;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 闂寘璋冪敤娌℃湁杩斿洖绫诲瀷!");
                            return false;
                        }
                    }
                    else if (frontCNT == ECallNodeType.Local)
                    {
                        HandleMetaVariable(m_FrontCallNode.m_MetaVariable, isAt);
                    }
                    else if (frontCNT == ECallNodeType.This)
                    {
                        if (GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaClass, m_Name) == false)
                        {
                            return false;
                        }
                    }
                    else if ( frontCNT == ECallNodeType.Base)
                    {
                        if (GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaClass, m_Name) == false)
                        {
                            return false;
                        }
                    }
                    else if (frontCNT == ECallNodeType.ConstValue)
                    {
                        if (m_FrontCallNode.m_ExpressNode != null)
                        {
                            //MetaConstExpressNode mcen = m_FrontCallNode.m_ExpressNode as MetaConstExpressNode;
                            //string mvname = "auto_constvalue_" + mcen.eType.ToString()
                            //    + "_" + mcen.GetHashCode();
                            //var fmetaVariable = m_OwnerMetaFunctionBlock.GetMetaVariable(mvname);
                            //if (fmetaVariable == null)
                            //{
                            //    m_FrontCallNode.m_MetaVariable = new MetaVariable(mvname,
                            //        MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaFunctionBlock, ownerMetaBase,
                            //        new MetaType(m_FrontCallNode.m_MetaClass));
                            //    m_OwnerMetaFunctionBlock.AddMetaVariable(m_FrontCallNode.m_MetaVariable);

                            //}

                            m_FrontCallNode.m_MetaInputParamCollection = new MetaInputParamCollection(ownerMetaBase, ownerMetaFunctionBlock);

                            MetaInputParam mip = new MetaInputParam(m_FrontCallNode.metaExpressValue);
                            m_FrontCallNode.m_MetaInputParamCollection.AddMetaInputParam(mip);

                            MetaMemberFunction mmf = m_FrontCallNode.m_MetaClass.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, m_FrontCallNode.m_MetaInputParamCollection);
                            if (mmf == null)
                            {
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 濞屸剝婀侀幍鎯у煂 閸忓厖绨猾璁宠厬" + m_FrontCallNode.m_MetaClass.allName + "閻ㄥ垳init_閺傝纭?)");
                                return false;
                            }
                            m_FrontCallNode.m_MetaFunction = mmf;
                        }
                        if (GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaClass, m_Name) == false)
                        {
                            return false;
                        }
                    }
                    else if (frontCNT == ECallNodeType.Express)
                    {
                        if (m_FrontCallNode.m_MetaType == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error m_FrontCallNode m_MetaType is null");
                            return false;
                        }
                        if (GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaType.GetTemplateMetaClass(), m_Name) == false)
                        {
                            return false;
                        }
                    }
                    else if (frontCNT == ECallNodeType.MemberFunctionName)
                    {
                        MetaFunction mf = m_FrontCallNode.m_MetaFunction;
                        MetaType retMT = mf.returnMetaVariable.GetFinalMetaType();
                        MetaClass mc = retMT.metaClass;
                        if (mc != null )
                        {
                            if (GetFunctionOrVariableByOwnerClass(mc, m_Name) == false)
                            {
                                return false;
                            }
                            m_StoreMetaVariable = m_FrontCallNode.m_MetaFunction.returnMetaVariable;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error visit variable type is null");
                        }
                    }
                    else if (frontCNT == ECallNodeType.TemplateName)
                    {
                        var mt = m_FrontCallNode.m_MetaTemplate;
                        if (mt != null)
                        {
                            if (mt.extendsMetaClass != null)
                            {
                                GetFunctionOrVariableByOwnerClass(mt.extendsMetaClass, m_Name);
                            }
                            else
                            {
                                if (m_Name == "instance")
                                {
                                    m_MetaVariable = new MetaVariable("instance", MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaFunctionBlock,
                                        null, null);
                                    m_CallNodeType = ECallNodeType.MemberVariableName;
                                }
                                else
                                {
                                    GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.objectMetaClass, m_Name);

                                }
                            }
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 閺嗗倷绗夐弨顖涘瘮娑撳﹨濡悙鍦畱缁鐎? " + frontCNT.ToString());
                    }
                }
            }





            if (m_CallNodeType == ECallNodeType.TemplateName)
            {
                //if (m_OwnerMetaClass is MetaGenTemplateClass mgtc)
                //{
                //    var find2 = mgtc.GetMetaGenTemplate(name);
                //    if (find2 != null)
                //    {
                //        m_MetaClass = find2.metaType.metaClass;
                //        m_CallNodeType = ECallNodeType.ClassName;
                //    }
                //    else
                //    {
                //        if(m_OwnerMetaFunctionBlock?.ownerMetaFunction is MetaGenTempalteFunction mgtf )
                //        {
                //            var find3 = mgtf.GetMetaGenTemplate(name);
                //            if( find3 != null )
                //            {
                //                m_MetaClass = find3.metaType.metaClass;
                //                m_CallNodeType = ECallNodeType.ClassName;
                //            }
                //        }
                //    }
                //}
            }

            if (this.m_FileMetaCallNode.inputTemplateNodeList.Count > 0)
            {
                MetaMemberFunction tmf = null;
                if (m_OwnerMetaFunctionBlock != null)
                {
                    tmf = m_OwnerMetaFunctionBlock.ownerMetaFunction as MetaMemberFunction;
                }
                if (m_MetaClass != null || tmf != null)
                {
                    CreateMetaTemplateParams(m_MetaClass, tmf);
                }
            }

            if (m_CallNodeType == ECallNodeType.ClassName)
            {
                if (m_MetaTemplateParamsList.Count > 0)
                {
                    var ngmc = m_MetaClass.AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList(m_MetaTemplateParamsList);
                    if (ngmc is MetaGenTemplateClass mgtc)
                    {
                        mgtc.ParseGenTemplateClass(mgtc);
                        mgtc.ParseGenMemberVarible();
                        m_MetaClass = mgtc;
                        List<MetaType> listmt2 = new List<MetaType>();
                        for (int i = 0; i < mgtc.metaTemplateClass.metaTemplateList.Count; i++)
                        {
                            listmt2.Add(new MetaType(mgtc.metaTemplateClass.metaTemplateList[i]));
                        }
                        m_MetaType = new MetaType(mgtc, listmt2 );
                    }
                    else
                    {
                        m_MetaType = new MetaType(m_MetaClass, m_MetaTemplateParamsList);
                    }
                }
            }
            else if (this.m_CallNodeType == ECallNodeType.MemberFunctionName)
            {
                if (m_MetaFunction is MetaMemberFunction mmf)
                {
                    if (mmf.isTemplateFunction)
                    {
                        MetaClass mcagm = m_MetaClass;
                        if (m_FrontCallNode != null)
                        {
                            if (m_FrontCallNode.m_MetaClass != null)
                            {
                                mcagm = m_FrontCallNode.m_MetaClass;
                            }
                            else if (m_FrontCallNode.m_MetaVariable != null)
                            {
                                mcagm = m_FrontCallNode.m_MetaVariable.realMetaType.metaClass;
                            }
                        }
                        MetaGenTemplateFunction mgtfind = mmf.AddGenTemplateMemberFunctionByMetaTypeList(mcagm, m_MetaTemplateParamsList);
                        if (mgtfind != null)
                        {
                            m_MetaFunction = mgtfind;
                            m_MetaType = m_MetaFunction.GetFinalMetaType();
                        }
                        ReCalcReturnMetaType();
                    }
                }
            }

            //娑撳绔熼惃鍕敩閻焦婀柌宥嗙€崥搴礉閺堫亞绮℃潻鍥崣鐠囦緤绱濋棁鈧憰渚€鐛欑拠?
            if (m_IsFunction)
            {
                if (m_CallNodeType == ECallNodeType.MemberFunctionName
                    || m_CallNodeType == ECallNodeType.SystemFunctionCall)
                {
                    return true;
                }
                else if (MetaClosureVariable.ResolveClosureVariable(m_MetaVariable) != null)
                {
                    // 闂寘鍙橀噺琚皟鐢? funname( xx ) -> 鐢熸垚 ClosureCall 璁块棶鑺傜偣
                    m_CallNodeType = ECallNodeType.ClosureCall;
                    var cv = MetaClosureVariable.ResolveClosureVariable(m_MetaVariable);
                    var funcRet = cv?.closureDefineStatements?.closureFunction?.returnMetaVariable?.defineMetaType;
                    m_MetaType = funcRet ?? new MetaType(CoreMetaClassManager.objectMetaClass);
                    return true;
                }
                else if ( IsFunctionTypeVariable( m_MetaVariable ) )
                {
                    // Function 绫诲瀷鍙橀噺琚皟鐢? 闂存帴闂寘璋冪敤 (typealias 瀹氫箟鐨勫嚱鏁扮鍚嶇被鍨嬪彉閲忕瓑)
                    m_CallNodeType = ECallNodeType.ClosureCall;
                    // 鑻ュ彉閲忕被鍨嬩负 FunctionSignatureMetaClass锛屽垯浠庣鍚嶅彇杩斿洖绫诲瀷
                    var fmt = m_MetaVariable?.GetFinalMetaType();
                    if ( fmt?.metaClass is FunctionSignatureMetaClass fsmc )
                    {
                        m_MetaType = fsmc.returnMetaType ?? new MetaType(CoreMetaClassManager.objectMetaClass);
                    }
                    else
                    {
                        m_MetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                    }
                    return true;
                }
                else if (m_MetaTemplate != null)
                {
                    m_CallNodeType = ECallNodeType.NewTemplate;
                }
                else if (m_MetaClass != null)
                {
                    MetaClass curmc = m_MetaClass;
                    if (this.m_IsArray)
                    {
                        curmc = CoreMetaClassManager.arrayMetaClass;
                    }
                    if( m_MetaType.isClass )
                    {
                        if( m_MetaClass is MetaGenTemplateClass mgtc )
                        {
                            curmc = mgtc.metaTemplateClass;
                        }
                        MetaMemberFunction mmf = curmc.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, m_MetaInputParamCollection);
                        bool allowDefaultConstructWithoutInit = (m_MetaInputParamCollection == null || m_MetaInputParamCollection.count == 0);
                        if (mmf == null && !allowDefaultConstructWithoutInit)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Class._init_" + curmc.allName + "not found");
                            return false;
                        }
                        m_MetaFunction = mmf;
                        if ((m_CallNodeType != ECallNodeType.NewTemplate)
                            && (m_CallNodeType != ECallNodeType.NewClass))
                        {
                            m_CallNodeType = ECallNodeType.NewClass;
                        }
                    }
                    this.m_MetaClass = curmc;

                    if (!m_AllowUseSettings.callFunction && m_IsFunction)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 瑜版挸澧犳担宥囩枂娑撳秴鍘戠拋鍛婃箒閸戣姤鏆熺拫鍐暏閺傜懓绱℃担璺ㄦ暏!!!" + m_Token?.ToLexemeAllString());
                    }
                }
                else if (m_MetaData != null)
                {
                    if (m_MetaData.isStatic)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error data static 涓嶅厑璁歌繘琛屽疄渚嬪寲(new/鏋勯€犺皟鐢?: " + m_MetaData.allName);
                        return false;
                    }
                    m_CallNodeType = ECallNodeType.NewData;
                    /*
                    if (m_FileMetaCallNode.fileMetaBraceTerm != null)  //閸欘垯浜掓担璺ㄦ暏  ArrClass(){ x = ??} 閻ㄥ嫭鏌熷?
                    {
                        if (m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 閸︹問nputParam 闁插矁绔熼敍灞剧€鍝勫毐閺佸府绱濋崣顏勫帒鐠?娴ｈ法鏁lassName() 閻ㄥ嫭鏌熷? " +
                                "娑撳秴鍘戠拋闀愬▏閻?ClassName(){}閻ㄥ嫭鏌熷" + m_FileMetaCallNode.fileMetaBraceTerm.ToTokenString());
                            return false;
                        }
                        m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                        m_MetaBraceStatementsContent.SetMetaType(new MetaType(m_MetaData));
                        m_MetaBraceStatementsContent.Parse();
                    }
                    */
                }
                else if (m_MetaEnum != null)
                {

                }
                else if( m_MetaType != null )
                {

                }
                else if (m_MetaFunction != null)
                {

                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error not support type");
                    return false;
                }
            }
            else
            {
                if (m_MetaVariable != null)
                {
                    var tmv = m_MetaVariable;
                    if (m_AllowUseSettings.useNotStatic == false && m_MetaVariable.isStatic)
                    {
                        if (frontCNT == ECallNodeType.FunctionInnerVariableName
                            || frontCNT == ECallNodeType.MemberVariableName
                            || frontCNT == ECallNodeType.This
                            || frontCNT == ECallNodeType.Base)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"Error {m_MetaVariable.ownerMetaBase.allName} is {m_MetaVariable.name} shuld not static variable");
                            return false;
                        }
                    }
                }
                else if (m_MetaClass != null )
                {
                }
                else if (m_MetaData != null)
                {
                }
                else if (m_MetaFunction != null)
                {

                }
                else if (m_MetaEnum != null)
                {

                }
                else if (m_MetaNode != null)
                {

                }
                else if( m_MetaType != null )
                {

                }
                else if (m_MetaTemplate != null)
                {
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreParseCallNodeNotFoundContent, m_Token,  $"Name:{name} not found!", m_Token );
                }
            }
            return true;
        }

        /// <summary>
        /// 鍒ゆ柇鍙橀噺鏄惁涓?Function 绫诲瀷 (鐢ㄤ簬闂存帴闂寘璋冪敤妫€娴?銆?
        /// </summary>
        private bool IsFunctionTypeVariable( MetaVariable mv )
        {
            if ( mv == null )
                return false;
            var mt = mv.GetFinalMetaType();
            if ( mt == null || mt.metaClass == null )
                return false;
            // 鍏煎 FunctionMetaClass 鍙婂叾瀛愮被 (濡?FunctionSignatureMetaClass)
            return mt.metaClass is FunctionMetaClass;
        }

        public void ReCalcReturnMetaType()
        {
            if (!m_MetaType.isTemplate) return;
            List<MetaTemplate> inputTemplateList = new List<MetaTemplate>();
            if (staticCallMetaType != null)
            {
                foreach (var v in staticCallMetaType.GetGenTemplateMetaTypeList())
                {
                    inputTemplateList.Add(v.metaTemplate);
                }
            }
            foreach (var v in m_MetaTemplateParamsList)
            {
                inputTemplateList.Add(v.metaTemplate);
            }

            if (m_MetaType.metaTemplate.index < inputTemplateList.Count)
            {
                m_MetaType = new MetaType(inputTemplateList[m_MetaType.metaTemplate.index]);
            }
        }
        bool HandleMetaClass( ECallNodeType frontCNT, int templateCount)
        {
            //-------------------------------------------------
            // special-case: a.type() where 'a' is a variable => produce Type for runtime variable
            if (m_Name == "type")
            {
                if (m_FrontCallNode.metaType.isTemplate)
                {
                    m_MetaType = new MetaType(m_FrontCallNode.metaType);
                    m_CallNodeType = ECallNodeType.GetType;
                }
                else
                {
                    m_MetaType = new MetaType(m_FrontCallNode.metaType);
                    m_CallNodeType = ECallNodeType.GetType;
                }
                return true;
            }
            // ClassName 娑撯偓閼割兛濞囬悽銊ユ躬 Class1.闂堟瑦鈧礁褰夐柌蹇ョ礉閹存牞鈧懏妲搁棃娆愨偓浣规煙濞夋洜娈戠拫鍐暏
            MetaNode tmb = null;
            MetaNode curMetaNode = null;
            if (frontCNT == ECallNodeType.MetaType)
            {
                curMetaNode = m_FrontCallNode.m_MetaType.metaClass != null ?
                    m_FrontCallNode.m_MetaType.metaClass.metaNode :
                    m_FrontCallNode.m_MetaType.metaClass.metaNode;
            }
            //else if( frontCNT == ECallNodeType.GenClassName )
            //{
            //    curMetaNode = m_FrontCallNode.m_GenMetaClass.metaNode;
            //}
            else
            {
                curMetaNode = m_FrontCallNode.m_MetaClass.metaNode;
            }
            if (tmb == null)
            {
                MetaClass ownerForMemberLookup = m_FrontCallNode.m_MetaClass;
                if (frontCNT == ECallNodeType.MetaType && m_FrontCallNode.m_MetaType != null)
                    ownerForMemberLookup = m_FrontCallNode.m_MetaType.metaClass;
                if (ownerForMemberLookup == null
                    || GetFunctionOrVariableByOwnerClass(ownerForMemberLookup, m_Name) == false)
                {
                    return false;
                }
                //閺屻儲澹橀棃娆愨偓浣稿毐閺?
                if (m_MetaFunction is MetaMemberFunction mmf)
                {
                    if (!mmf.isStatic)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Class.MemeberFunction not should non static");
                        return false;
                    }
                    if (mmf.isConstructInitFunction && !m_AllowUseSettings.callConstructFunction)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error constructor call is not allowed here." + m_Token.ToLexemeAllString());
                        return false;
                    }
                    if (m_FrontCallNode != null)
                    {
                        if (frontCNT == ECallNodeType.MetaType && m_FrontCallNode.m_MetaType != null)
                            this.m_StaticCallMetaType = new MetaType(m_FrontCallNode.m_MetaType);
                        else
                            this.m_StaticCallMetaType = new MetaType(m_FrontCallNode.m_MetaClass, this.m_FrontCallNode.m_MetaTemplateParamsList);
                    }
                    else
                    {
                        this.m_StaticCallMetaType = new MetaType(mmf.ownerMetaClass);
                    }
                }
                if (m_MetaVariable is MetaMemberVariable mmv)
                {
                    if (!mmv.isStatic && !mmv.isConst)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"Error {mmv.ownerMetaBase.allName}'s member variable {mmv.name} is static! ");
                        return false;
                    }
                    if (m_FrontCallNode != null)
                        this.m_StaticCallMetaType = new MetaType(m_FrontCallNode.metaType);
                    else
                    {
                        this.m_StaticCallMetaType = new MetaType(mmv.ownerMetaClass);
                    }
                }
            }
            else
            {
                if (tmb.IsMetaClass() == false)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 閸︺劌缍嬮崜宥囪: {m_FrontCallNode?.m_MetaClass.name} " +
                        $"闁插本鐓￠幍鎯у煂娴滃棗鐡欐い鐧哥礉娴ｅ棔绗夐弰顖滆{m_Name} ");
                    return false;
                }
                m_MetaClass = tmb.GetMetaClassByTemplateCount(templateCount);
                m_CallNodeType = ECallNodeType.ClassName;
            }
            //-----------------------------------------------------------------------------------
            //--------------------------------------
            /*
            if (m_FrontCallNode.m_MetaClass != null)
            {
                if (GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaClass, m_Name) == false)
                {
                    return false;
                }
                if (m_MetaVariable != null && m_MetaVariable.permission == EPermission.Private)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 娑撳秴鍘戠拋姝岊問闂?private 閹存劕鎲?);
                    return false;
                }
                if (m_MetaFunction != null && m_MetaFunction.permission == EPermission.Private)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 娑撳秴鍘戠拋姝岊問闂?private 閸戣姤鏆?);
                    return false;
                }
            }

            var gmv = m_FrontCallNode.m_MetaVariable;
            if (gmv != null)
            {
                gmv.ParseRealMetaType();
                var gmc = gmv.realMetaType?.metaClass;
                if (gmc != null)
                {
                    if (GetFunctionOrVariableByOwnerClass(gmc, m_Name) == false)
                    {
                        return false;
                    }

                    if (m_MetaVariable != null && m_MetaVariable.permission == EPermission.Private)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 娑撳秴鍘戠拋姝岊問闂?private 閹存劕鎲?);
                        return false;
                    }
                    if (m_MetaFunction != null && m_MetaFunction.permission == EPermission.Private)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 娑撳秴鍘戠拋姝岊問闂?private 閸戣姤鏆?);
                        return false;
                    }
                }
            }

            m_MetaVariable = m_FrontCallNode.m_MetaData.GetMemberDataByName(m_Name);
            m_CallNodeType = ECallNodeType.MemberVariableName;

            // special-case: a.type() where 'a' is a variable => produce Type for runtime variable
            if (m_Name == "type")
            {
                if (m_FrontCallNode.metaType.isTemplate)
                {
                    m_MetaType = new MetaType(m_FrontCallNode.metaType);
                    m_CallNodeType = ECallNodeType.GetType;
                }
                else
                {
                    m_MetaType = new MetaType(m_FrontCallNode.metaType);
                    m_CallNodeType = ECallNodeType.GetType;
                }
                var mt = new MetaType(CoreMetaClassManager.typeMetaClass);
                m_MetaVariable = new MetaVariable("type_return", MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaFunctionBlock,
                    m_OwnerMetaBase, mt);
                return true;
            }
            // ClassName 娑撯偓閼割兛濞囬悽銊ユ躬 Class1.闂堟瑦鈧礁褰夐柌蹇ョ礉閹存牞鈧懏妲搁棃娆愨偓浣规煙濞夋洜娈戠拫鍐暏
            MetaNode tmb = null;
            MetaNode curMetaNode = null;
            if (frontCNT == ECallNodeType.MetaType)
            {
                curMetaNode = m_FrontCallNode.m_MetaType.metaClass != null ?
                    m_FrontCallNode.m_MetaType.metaClass.metaNode :
                    m_FrontCallNode.m_MetaType.metaClass.metaNode;
            }
            //else if( frontCNT == ECallNodeType.GenClassName )
            //{
            //    curMetaNode = m_FrontCallNode.m_GenMetaClass.metaNode;
            //}
            else
            {
                curMetaNode = m_FrontCallNode.m_MetaClass.metaNode;
            }
            if (tmb == null)
            {
                MetaClass ownerForMemberLookup = m_FrontCallNode.m_MetaClass;
                if (frontCNT == ECallNodeType.MetaType && m_FrontCallNode.m_MetaType != null)
                    ownerForMemberLookup = m_FrontCallNode.m_MetaType.metaClass;
                if (ownerForMemberLookup == null
                    || GetFunctionOrVariableByOwnerClass(ownerForMemberLookup, m_Name) == false)
                {
                    return false;
                }
                //閺屻儲澹橀棃娆愨偓浣稿毐閺?
                if (m_MetaFunction is MetaMemberFunction mmf)
                {
                    if (!mmf.isStatic)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 鐠嬪啰鏁ら棃鐐烘饯閹焦鍨氶崨妯哄毐閺佸府绱濇稉宥堝厴娴ｈ法鏁lass.Variable閻ㄥ嫭鏌熷?");
                        return false;
                    }
                    if (mmf.isConstructInitFunction && !m_AllowUseSettings.callConstructFunction)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error constructor call is not allowed here." + m_Token.ToLexemeAllString());
                        return false;
                    }
                    if (m_FrontCallNode != null)
                    {
                        if (frontCNT == ECallNodeType.MetaType && m_FrontCallNode.m_MetaType != null)
                            this.m_CallMetaType = new MetaType(m_FrontCallNode.m_MetaType);
                        else
                            this.m_CallMetaType = new MetaType(m_FrontCallNode.m_MetaClass, this.m_FrontCallNode.m_MetaTemplateParamsList);
                    }
                    else
                    {
                        this.m_CallMetaType = new MetaType(mmf.ownerMetaClass);
                    }
                }
                if (m_MetaVariable is MetaMemberVariable mmv)
                {
                    if (!mmv.isStatic && !mmv.isConst)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 鐠嬪啰鏁ら棃鐐烘饯閹焦鍨氶崨妯哄綁闁插骏绱濇稉宥堝厴娴ｈ法鏁lass.Variable閻ㄥ嫭鏌熷?");
                        return false;
                    }
                    if (m_FrontCallNode != null)
                        this.m_CallMetaType = new MetaType(m_FrontCallNode.metaType);
                    else
                    {
                        this.m_CallMetaType = new MetaType(mmv.ownerMetaClass);
                    }
                }
            }
            else
            {
                if (tmb.IsMetaClass() == false)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"Error 閸︺劌缍嬮崜宥囪: {m_FrontCallNode?.m_MetaClass.name} " +
                        $"闁插本鐓￠幍鎯у煂娴滃棗鐡欐い鐧哥礉娴ｅ棔绗夐弰顖滆{m_Name} ");
                    return false;
                }
                m_MetaClass = tmb.GetMetaClassByTemplateCount(templateCount);
                m_CallNodeType = ECallNodeType.ClassName;
            }
            */
            return true;
        }
        bool HandleMetaVariable(MetaVariable mv, bool isAt )
        {
            MetaBase tempMetaBase2 = null;
            if (mv == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error HandleGetTypeByMetaVariable mv is null");
                return false;
            }

            // ensure variable meta types are calculated
            mv.CreateMetaExpress();
            mv.ParseMetaExpress();
            mv.ParseRealMetaType();

            //if( frontCNT == ECallNodeType.VisitVariable && (mv is MetaVisitVariable mvv) )
            //{
            //    mv = mvv;
            //}
            MetaVariable getmv2 = null;
            if (mv.isArray)
            {
                if (isAt)
                { 

                    HandleVisit();
                }
                //if (mv.realMetaType.isGenTemplateClass)
                //{
                //    calcMetaBase = mv.realMetaType.GetMetaInputTemplateByIndex();
                //}
            }

            if (tempMetaBase2 == null)
            {
                var mtt = mv.GetFinalMetaType();
                if (mtt.isData)
                {
                    //if (TryBuildDataToStringSystemCall(m_Name))
                    //{
                    //    return true;
                    //}
                    var md = mtt.metaData;
                    var retmmd = md.GetMemberDataByName(m_Name);
                    m_MetaVariable = retmmd;
                    if (retmmd == null)
                    {
                        if (GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.objectMetaClass, m_Name))
                        {

                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"Error 濞屸剝婀侀幍鎯у煂{m_Name} 閻ㄥ嚜etaData閺佺増宓?");
                            return false;
                        }
                    }
                    else
                    {
                        if (retmmd.memberDataType == EMemberDataType.MemberClass)
                        {
                            m_MetaClass = m_MetaVariable.realMetaType.metaClass;
                            m_MetaVariable = retmmd;
                            m_CallNodeType = ECallNodeType.MemberVariableName;
                            m_MetaType = new MetaType(m_MetaVariable.GetFinalMetaType());
                        }
                        //else if (retmmd.memberDataType == EMemberDataType.ConstValue)
                        //{
                        //    m_CallNodeType = ECallNodeType.ConstValue;
                        //    m_ExpressNode = retmmd.expressNode as MetaConstExpressNode;
                        //}
                        //else if (retmmd.memberDataType == EMemberDataType.MemberArray)
                        //{
                        //    m_CallNodeType = ECallNodeType.MemberDataName;
                        //}
                        else
                        {
                            m_MetaType = new MetaType(m_MetaVariable.GetFinalMetaType());
                            m_MetaVariable = retmmd;
                            m_CallNodeType = ECallNodeType.MemberVariableName;
                        }
                    }
                }
                else if (mtt.isEnum)
                {
                    if (mv.realMetaType.isEnumMember)
                    {
                        if (GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.memberMetaClass, m_Name) == false)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (mtt.isEnumMember)
                {
                    if (GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.memberMetaClass, m_Name) == false)
                    {
                        return false;
                    }
                }
                else
                {
                    MetaClass mc = mtt.metaClass == null ? mv.GetTemplateMetaClass() : mtt.metaClass;
                    if (isAt)
                    {
                        HandleVisit();
                    }
                    else
                    {
                        if (GetFunctionOrVariableByOwnerClass(mc, m_Name) == false)
                        {
                            return false;
                        }
                    }
                }
            }

            // result of `.type()` is Core.Type
            //m_MetaType = new MetaType(CoreMetaClassManager.typeMetaClass);

            //// call meta information: provide the target meta type as the call meta-type
            //// so downstream code knows which runtime type to wrap
            //m_CallMetaType = new MetaType(mv.GetFinalMetaType());

            //// create a placeholder MetaFunction to mark this as a function-like access
            //m_MetaFunction = new MetaFunction(m_MetaType.metaClass ?? CoreMetaClassManager.typeMetaClass);

            //m_CallNodeType = ECallNodeType.FunctionCall;

            return true;
        }
        void HandleVisit()
        {
            if (m_FrontCallNode?.m_MetaVariable != null)
            {
                var variable = m_FrontCallNode.m_MetaVariable;
                if (m_FileMetaCallNode?.atToken != null || m_VisitFlag)
                {
                    // Variable-key subscript write on a non-array container
                    // (e.g. `map[k] = v`): resolve directly to a _setItem_
                    // method call so the right-hand value becomes the value
                    // argument. Otherwise the subscript resolves to a
                    // _getItem_ read (see MetaVisitVariable) and the
                    // assignment silently stores nothing.
                    if (m_VisitFlag
                        && m_AllowUseSettings?.setterFunction == true
                        && m_RightExpress != null
                        && m_ExpressNode is MetaCallLinkExpressNode keyExpr)
                    {
                        var fmtSet = variable.GetFinalMetaType();
                        if (fmtSet != null && !fmtSet.IsArray())
                        {
                            MetaClass visitMcSet = fmtSet.metaClass != null ? fmtSet.metaClass : fmtSet.GetTemplateMetaClass();
                            if (visitMcSet != null)
                            {
                                var setParams = new MetaInputParamCollection(ownerMetaBase, m_OwnerMetaFunctionBlock);
                                setParams.AddMetaInputParam(new MetaInputParam(keyExpr));
                                // 值参数需转换 New 对象表达式（同 setter 路径）
                                var setItemValue = ExpressManager.ConvertNewExpress(m_RightExpress, null);
                                if (setItemValue == null) { return; }
                                setParams.AddMetaInputParam(new MetaInputParam(setItemValue));
                                var setMethod = visitMcSet.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_setItem_", 0, setParams);
                                if (setMethod != null)
                                {
                                    m_MetaFunction = setMethod;
                                    m_MetaClass = visitMcSet;
                                    m_MetaInputParamCollection = setParams;
                                    m_CallNodeType = ECallNodeType.MemberFunctionName;
                                    m_MetaType = setMethod.returnMetaVariable?.GetFinalMetaType();
                                    return;
                                }
                            }
                        }
                    }

                    // Array1.$i.x   Array1.$mmq.x;
                    var getmv2 = m_OwnerMetaFunctionBlock.GetMetaVariableByName(m_Name);
                    if (getmv2 != null)    //閺屻儲澹橀弰顖氭儊瀹告彃鐣炬稊澶庣箖閸箖閸欐﹢鍣?
                    {
                        // 索引变量必须绑定当前作用域解析到的那个变量。
                        // 不同作用域存在同名变量时（例如嵌套 for 各自声明的循环变量 i），
                        // 仅用 "Visit_" + m_Name 做缓存键会让后面的 arr[i] 错误复用
                        // 前面作用域缓存的绑定，导致加载错误的槽位。
                        // 因此键中追加索引变量对象的哈希以区分作用域。
                        string inputMVName = "Visit_" + m_Name + "_" + getmv2.GetHashCode();
                        m_MetaVariable = variable.GetMetaVariable(inputMVName);
                        if (m_MetaVariable == null)
                        {
                            MetaVisitNode mvn = MetaVisitNode.CreateByVariable(getmv2);
                            MetaCallLink mcl = new MetaCallLink(mvn);
                            m_MetaVariable = new MetaVisitVariable(inputMVName, ownerMetaClass, m_OwnerMetaFunctionBlock, variable, mcl);
                            variable.AddMetaVariable(m_MetaVariable);
                        }
                        m_CallNodeType = ECallNodeType.VisitVariable;
                    }
                    else if (m_ExpressNode is MetaConstExpressNode mcen)
                    {
                        // Numeric const subscripts are parsed into an index for the
                        // array bounds check below. Non-numeric const keys (e.g. a
                        // string key of Map) must skip that check and fall through
                        // to the _getItem_/_setItem_ lookup; converting them with
                        // Convert.ToInt32 would throw a FormatException.
                        int index = -1;
                        int.TryParse(mcen.value?.ToString(), out index);
                        var fmt = variable.GetFinalMetaType();
                        if (fmt.IsArray() )
                        {
                            var deflen = fmt.arrayLength;
                            if (deflen != -1)
                            {
                                if (deflen > 0 && deflen < index)
                                {
                                    Log.AddMetaCoreLog(LID.ShowExtendMessage, mcen.token, "Array index out of range.");
                                    return;
                                }
                            }

                            m_MetaVariable = new MetaVisitVariable("Visit_" + mcen.value.ToString(), ownerMetaClass, m_OwnerMetaFunctionBlock, variable, mcen);

                            m_CallNodeType = ECallNodeType.VisitVariable;
                        }
                        else
                        {
                            // 闈炴暟缁勭被鍨嬶細妫€鏌ユ槸鍚︽敮鎸?_getItem_/_setItem_ 涓嬫爣璁块棶
                            MetaClass visitMc = fmt.metaClass;
                            if (visitMc == null)
                                visitMc = fmt.GetTemplateMetaClass();
                            if (visitMc != null)
                            {
                                var inputParam = new MetaInputParamCollection(ownerMetaBase, m_OwnerMetaFunctionBlock);
                                inputParam.AddMetaInputParam(new MetaInputParam(mcen));

                                // 璧嬪€煎満鏅紙setterFunction=true锛夋煡鎵?_setItem_锛屽惁鍒欐煡鎵?_getItem_
                                MetaMemberFunction visitMethod = null;
                                if (m_AllowUseSettings?.setterFunction == true
                                    && m_RightExpress != null )
                                {
                                    // 值参数需转换 New 对象表达式（同 setter 路径）
                                    var setItemValue = ExpressManager.ConvertNewExpress(m_RightExpress, null);
                                    if (setItemValue == null) { return; }
                                    inputParam.AddMetaInputParam(new MetaInputParam(setItemValue));
                                    visitMethod = visitMc.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_setItem_", 0, inputParam );
                                }
                                else
                                {
                                    visitMethod = visitMc.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_getItem_", 0, inputParam );
                                }

                                if (visitMethod != null)
                                {
                                    m_MetaFunction = visitMethod;
                                    m_MetaClass = visitMc;
                                    m_MetaInputParamCollection = inputParam;
                                    m_CallNodeType = ECallNodeType.MemberFunctionName;
                                    m_MetaType = visitMethod.returnMetaVariable?.GetFinalMetaType();
                                    return;
                                }
                            }

                            Log.AddMetaCoreLog(LID.MetaCoreVisitTypeShouldIsArray, mcen.token, variable.realMetaType.ToString(), variable.name);
                            return;
                        }
                    }
                    else if (m_ExpressNode is MetaOpExpressNode moen)
                    {
                        // 同上：下标为表达式时，缓存键需要包含表达式对象身份，
                        // 避免不同下标表达式（或不同作用域的同名下标）互相复用绑定。
                        string inputMVName = "Visit_" + m_Name + "_" + moen.GetHashCode();
                        m_MetaVariable = variable.GetMetaVariable(inputMVName);
                        if (m_MetaVariable == null)
                        {
                            m_MetaVariable = new MetaVisitVariable(inputMVName, ownerMetaClass, m_OwnerMetaFunctionBlock, variable, moen);
                            variable.AddMetaVariable(m_MetaVariable);
                        }
                        m_CallNodeType = ECallNodeType.VisitVariable;
                    }
                    else
                    {
                        Debug.Assert(false);
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Cannot find suitable visit variable/access node.");
                    }

                    m_MetaVariable.ParseDefineMetaType();
                    m_MetaVariable.ParseRealMetaType();
                    m_MetaType = m_MetaVariable.GetFinalMetaType();

                }
            }
        }

        //void HandleGetTypeByMetaType(MetaType mc)
        //{
        //    m_MetaFunction = new MetaFunction(mc.metaClass);
        //    m_MetaType = new MetaType(CoreMetaClassManager.typeMetaClass);
        //    m_CallMetaType = new MetaType(mc);
        //    m_CallNodeType = ECallNodeType.FunctionCall;
        //}

        //private bool TryBuildDataToStringSystemCall(string inputname)
        //{
        //    if (!m_IsFunction || !string.Equals(inputname, "toString", StringComparison.Ordinal))
        //    {
        //        return false;
        //    }

        //    if (m_MetaInputParamCollection == null || m_MetaInputParamCollection.count != 0)
        //    {
        //        return false;
        //    }

        //    var frontNode = m_FrontCallNode;
        //    if (frontNode == null)
        //    {
        //        return false;
        //    }

        //    var targetType = frontNode.metaVariable?.GetFinalMetaType() ?? frontNode.metaType;
        //    if (targetType == null || !targetType.isData)
        //    {
        //        return false;
        //    }

        //    var inputParams = new MetaInputParamCollection(ownerMetaBase, m_OwnerMetaFunctionBlock);
        //    MetaVisitNode callerNode;
        //    if (frontNode.metaVariable != null)
        //    {
        //        var callerMt = frontNode.metaType ?? frontNode.metaVariable.GetFinalMetaType();
        //        callerNode = MetaVisitNode.CreateByVariable(frontNode.metaVariable);
        //    }
        //    else
        //    {
        //        callerNode = MetaVisitNode.CreateByVisitMetaData(new MetaType(targetType));
        //    }
        //    callerNode.SetToken(m_Token);
        //    var callerCallLink = new MetaCallLink(callerNode);
        //    var callerExpress = new MetaCallLinkExpressNode(callerCallLink);
        //    callerExpress.SetToken(m_Token);
        //    callerExpress.CalcReturnType();
        //    inputParams.AddMetaInputParam(new MetaInputParam(callerExpress));
        //    m_MetaInputParamCollection = inputParams;

        //    var ownerClass = ownerMetaClass ?? CoreMetaClassManager.objectMetaClass;
        //    m_MetaFunction = new MetaMemberFunction.MetaBuiltinFunction(ownerClass, ESystemMethodCall.SystemBuildDataString.ToString());
        //    m_MetaFunction.SetIndex((int)ESystemMethodCall.SystemBuildDataString);
        //    m_MetaType = new MetaType(CoreMetaClassManager.stringMetaClass);
        //    m_CallNodeType = ECallNodeType.SystemFunctionCall;
        //    return true;
        //}
        //private MetaMemberData GetOrCreateDataDefaultStaticInstanceVariable(MetaData dataType)
        //{
        //    if (dataType == null || dataType.isStatic)
        //    {
        //        return null;
        //    }

        //    var globalData = ProjectManager.globalData;
        //    if (globalData == null)
        //    {
        //        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error data 榛樿闈欐€佸疄渚嬪垱寤哄け璐ワ細globalData 涓虹┖銆?);
        //        return null;
        //    }

        //    string varName = "__data_default_instance_" + dataType.allName + "_" + dataType.GetHashCode();
        //    var exist = globalData.GetMemberDataByName(varName) as MetaMemberData;
        //    if (exist != null)
        //    {
        //        return exist;
        //    }

        //    var mmv = new MetaMemberData(globalData, varName, globalData.metaMemberDataDict.Count );
        //    mmv.SetIsStatic(true);
        //    mmv.SetIsDefineMetaType(true);
        //    mmv.SetMetaDefineType(new MetaType(dataType));
        //    mmv.SetRealMetaType(new MetaType(dataType));
        //    var newobj = new MetaNewObjectExpressNode(new MetaType(dataType), globalData, null, null);
        //    newobj.Parse(new AllowUseSettings());
        //    newobj.CalcReturnType();
        //    mmv.SetExpress(newobj);

        //    globalData.AddMetaMemberData(mmv);
        //    return mmv;
        //}

        // lowercase container keywords => Core container class name
        // map() => Map<Object,Object>   list()/stack()/hashset()/queue()/array() => <Object>
        // range() => Range<int>          tuple() => Tuple (no template)
        // local variable lookup takes priority, so map/list/etc. still work as variable names
        private static readonly Dictionary<string, string> s_LowercaseContainerClassNameDict
            = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "map", "Map" },
            { "list", "List" },
            { "stack", "Stack" },
            { "hashset", "HashSet" },
            { "queue", "Queue" },
            { "tuple", "Tuple" },
            { "array", "Array" },
            { "range", "Range" },
        };

        // default template args for a lowercase container keyword when no explicit template args
        // returns null when there is no default (tuple is the plain no-template class;
        // uppercase class names never auto-infer here)
        private static List<MetaType> GetLowercaseContainerDefaultTemplateArgs(string inputname)
        {
            switch (inputname)
            {
                case "map":
                    return new List<MetaType>()
                    {
                        new MetaType(CoreMetaClassManager.objectMetaClass),
                        new MetaType(CoreMetaClassManager.objectMetaClass),
                    };
                case "range":
                    return new List<MetaType>() { new MetaType(CoreMetaClassManager.int32MetaClass) };
                case "list":
                case "stack":
                case "hashset":
                case "queue":
                case "array":
                    return new List<MetaType>() { new MetaType(CoreMetaClassManager.objectMetaClass) };
                default:
                    return null;
            }
        }

        public bool GetFirstNode(string inputname, MetaBase mb , int count)
        {
            MetaVariable mv = m_OwnerMetaFunctionBlock?.GetMetaVariableByName(inputname);
            if (mv != null)
            {
                m_MetaVariable = mv;
                m_CallNodeType = ECallNodeType.FunctionInnerVariableName;
                m_MetaType = mv.GetFinalMetaType();
                return true;
            }

            MetaClass mc = mb as MetaClass;
            MetaData md = mb as MetaData;
            MetaEnum me = mb as MetaEnum;

            if (m_IsFunction)
            {
                // Treat runtime/native bridge calls as system functions.
                // Accept either exact enum name or literal string.

                if( SystemMethodCallDeclarationRegistry.TryGetDeclaration( inputname, out SystemMethodCallDeclaration decl ) )
                {
                    m_MetaFunction = new MetaMemberFunction.MetaBuiltinFunction(mc, decl);
                    var retMt = m_MetaFunction.GetFinalMetaType();
                    m_MetaType = retMt != null ? new MetaType(retMt) : null;
                    m_CallNodeType = ECallNodeType.SystemFunctionCall;
                    return true;

                //if (mb != null && Enum.TryParse<ESystemMethodCall>(inputname, true, out var inputindex))
                //{
                //    m_MetaFunction = new MetaMemberFunction.MetaBuiltinFunction(mc, inputname);
                //    m_MetaFunction.SetIndex((int)inputindex);
                //    var retMt = m_MetaFunction.GetFinalMetaType();
                //    m_CallMetaType = retMt != null ? new MetaType(retMt) : new MetaType(mc);
                //    m_MetaType = retMt != null ? new MetaType(retMt) : null;
                //    m_CallNodeType = ECallNodeType.SystemFunctionCall;
                //    return true;
                //}
            }
            }

            MetaNode retMC = null;
            // 閺屻儲澹樼€规矮绠熼崗鎶芥暛鐎涙娈慶lass => range   array
            if (m_Token.extend != null)
            {
                MetaNode findMB = CoreMetaClassManager.GetCoreMetaClass(m_Token.extend.ToString());
                if (findMB?.IsMetaClass() == true)
                {
                    retMC = findMB;
                }
            }
            // lowercase container keywords: map()/list()/stack()/hashset()/queue()/tuple()/array()/range()
            // => Core container class; falls back to normal identifier lookup when Core class not found
            // m_IsFunction guard: only the call form name(...) resolves to the Core container,
            // a plain identifier reference / duplicate-name probe (built from a bare name token,
            // e.g. `Map<K,V> map = new()`) keeps the normal lookup path
            if (retMC == null && m_IsFunction && s_LowercaseContainerClassNameDict.TryGetValue(inputname, out string containerCoreName))
            {
                MetaNode findMB2 = CoreMetaClassManager.GetCoreMetaClass(containerCoreName);
                if (findMB2?.IsMetaClass() == true)
                {
                    retMC = findMB2;
                }
            }
            //閺屻儲澹樼猾缁樐侀崹?
            if (retMC == null && mc != null)
            {
                var t = mc.GetMetaTemplateByName(inputname);
                if (t != null)
                {
                    m_MetaTemplate = t;
                    m_CallNodeType = ECallNodeType.TemplateName;
                    return true;
                }
            }
            //閺屻儲澹橀悥鍓佽閹存牕鐡欑猾璁宠厬閸栧懎鎯堥惃鍕Ν閻?
            if (retMC == null && mc != null)
            {
                retMC = mc.metaNode.GetChildrenMetaNodeByName(inputname);
            }
            //闁俺绻僨ileMeta閺屻儲澹橀弰顖氭儊閺堝顩荤€规矮绠熺€涙顑?
            if (retMC == null)
            {
                retMC = ClassManager.instance.GetMetaClassByNameAndFileMeta(m_OwnerMetaBase, m_FileMetaCallNode.fileMeta, new List<string>(1) { inputname });
            }
            if (retMC != null)
            {
                m_MetaNode = retMC;
                m_CallNodeType = ECallNodeType.MetaNode;
                if (retMC.isMetaModule || retMC.isMetaNamespace)
                {
                    m_MetaNode = retMC;
                }
                else if (retMC.isMetaData)
                {
                    m_MetaData = retMC.metaData;
                    m_CallNodeType = ECallNodeType.DataName;
                    m_MetaType = new MetaType(m_MetaData);
                }
                else if (retMC.isMetaEnum)
                {
                    m_MetaEnum = retMC.metaEnum;
                    m_CallNodeType = ECallNodeType.EnumName;
                    m_MetaType = new MetaType(m_MetaEnum);
                }
                else if (retMC.IsMetaClass())
                {
                    // language keyword: lowercase container name without explicit template args
                    // => use default template args
                    //   range => Range<int>   map => Map<Object,Object>
                    //   list/stack/hashset/queue/array => <Object>   tuple() => plain Tuple (normal path)
                    // keep case-sensitive behavior so `Range` does not auto-infer here
                    var defaultTemplateArgs = count == 0 ? GetLowercaseContainerDefaultTemplateArgs(inputname) : null;
                    if (defaultTemplateArgs != null)
                    {
                        m_MetaClass = retMC.GetMetaClassByTemplateCount(defaultTemplateArgs.Count);
                        if (m_MetaClass == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreFindMetaClassByTemplateNum, m_Token, "", retMC.allName, defaultTemplateArgs.Count.ToString());
                            return false;
                        }
                        m_MetaType = new MetaType(m_MetaClass, defaultTemplateArgs);
                    }
                    else
                    {
                        m_MetaClass = retMC.GetMetaClassByTemplateCount(count);
                        if (m_MetaClass == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreFindMetaClassByTemplateNum, m_Token, "", retMC.allName, count.ToString());
                            return false;
                        }
                        else
                        {
                            m_MetaType = new MetaType(m_MetaClass);
                            m_CallNodeType = ECallNodeType.ClassName;
                        }
                    }

                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 濞屸剝婀侀崣鎴ｎ嚉RetMC閻ㄥ嫮琚崚鐜€etaCommon");
                }
            }
            else
            {
                // 鍐呯疆/宸ョ▼/鏂囦欢 typealias锛堝惈 TypeManager.m_GlobalTypeAliasDict锛屽 ObjectArray -> Array<Object>锛?
                var fmAlias = m_FileMetaCallNode?.fileMeta;
                if (fmAlias != null
                    && TypeManager.instance.TryResolveTypeAlias(inputname, fmAlias, out var aliasMt)
                    && aliasMt != null)
                {
                    m_MetaType = new MetaType(aliasMt);
                    m_MetaClass = m_MetaType.metaClass;
                    m_CallNodeType = ECallNodeType.MetaType;
                    return true;
                }

                if (mc != null)
                {
                    var mmv = mc.GetMetaMemberVariableByName(inputname);
                    if (mmv != null)
                    {
                        if (mmv.isStatic)
                        {
                            m_MetaVariable = mmv;
                            m_MetaClass = mc;
                            m_MetaType = mmv.realMetaType;
                            List<MetaType> mtList = new List<MetaType>();
                            for (int i = 0; i < mmv.ownerMetaClass.metaTemplateList.Count; i++)
                            {
                                mtList.Add(new MetaType(mmv.ownerMetaClass.metaTemplateList[i], mmv.ownerMetaClass.metaTemplateList[i].name));
                            }
                            m_StaticCallMetaType = new MetaType(mmv.ownerMetaClass, mtList);
                            m_CallNodeType = ECallNodeType.MemberVariableName;
                            return true;
                        }
                        else if (LocalManager.IsFileLocalClass(mc))
                        {
                            // local{} init 上下文: 裸名字 a 等价于隐式 this.a,
                            // 解析为 _Local 类的非静态成员变量(实例成员访问)
                            m_MetaVariable = mmv;
                            m_MetaClass = mc;
                            m_MetaType = mmv.GetFinalMetaType();
                            m_CallNodeType = ECallNodeType.MemberVariableName;
                            return true;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"find meta member variable by name{inputname}");
                            return false;
                        }
                    }
                    var mmf = mc.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount(inputname, m_MetaTemplateParamsList.Count, m_MetaInputParamCollection, true);
                    if (mmf != null)
                    {
                        if (mmf.isStatic)
                        {
                            m_MetaFunction = mmf;
                            m_MetaClass = mc; List<MetaType> mtList = new List<MetaType>();
                            for (int i = 0; i < mmf.ownerMetaClass.metaTemplateList.Count; i++)
                            {
                                mtList.Add(new MetaType(mmf.ownerMetaClass.metaTemplateList[i], mmv.ownerMetaClass.metaTemplateList[i].name));
                            }
                            m_StaticCallMetaType = new MetaType(mmf.ownerMetaClass, mtList);
                            m_CallNodeType = ECallNodeType.MemberFunctionName;
                            m_MetaType = mmf.GetFinalMetaType();
                            return true;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "not found functon by [" + inputname + "]" );
                            return false;
                        }
                    }
                }
                else if (md != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error data 涓嶆敮鎸佸湪鏈綋鍐呰皟鐢?" + me.allName);
                    return false;
                    //var mmd = md.GetMemberDataByName(inputname);
                    //if (mmd != null)
                    //{
                    //    m_MetaData = md;
                    //    m_MetaVariable = mmd;
                    //    m_MetaType = mmd.realMetaType;
                    //    m_CallNodeType = ECallNodeType.MemberVariableName;
                    //    return true;
                    //}
                }
                else if (me != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error enum 涓嶆敮鎸佸湪鏈綋鍐呰皟鐢?" + me.allName);
                    return false;
                    //if (m_IsFunction)
                    //{
                    //    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error data 涓嶆敮鎸佸嚱鏁拌皟鐢? " + me.allName);
                    //    return false;

                    //}
                    //else
                    //{
                    //    var mmv = me.GetMemberEnumByName(inputname);
                    //    if (mmv != null)
                    //    {
                    //        m_MetaEnum = me;
                    //        m_MetaVariable = mmv;
                    //        m_MetaType = mmv.realMetaType;
                    //        m_CallNodeType = ECallNodeType.MemberVariableName;
                    //        return true;
                    //    }
                    //    else
                    //    {
                    //        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"Error data '{me.allName}' does not have member variable '{inputname}'");
                    //        return false;
                    //    }
                    //}
                }

            }

            //閸戣姤鏆熼崘鍛灇閸?
            if (retMC == null)
            {
                var ownerFun = m_OwnerMetaFunctionBlock?.ownerMetaFunction;
                if (ownerFun != null)
                {
                    //閸戣姤鏆熼惃鍕棘閺佺増妲搁崥锔芥Ц濡紕澧楅敍灞筋洤閺嬫粍妲搁敍灞藉灟鏉╂柨娲?
                    var metaTemplate = ownerFun.GetMetaDefineTemplateByName(inputname);
                    if (metaTemplate != null)
                    {
                        m_MetaTemplate = metaTemplate;
                        m_CallNodeType = ECallNodeType.TemplateName;
                        return true;
                    }
                }
            }
            return true;
        }
        public bool CreateMetaTemplateParams(MetaClass mc, MetaMemberFunction mmf)
        {
            for (int i = 0; i < this.m_FileMetaCallNode.inputTemplateNodeList.Count; i++)
            {
                var itnlc = this.m_FileMetaCallNode.inputTemplateNodeList[i];
                var ct = TypeManager.instance.RegisterTemplateDefineMetaTemplateFunction(ownerMetaClass, mmf, itnlc, true);
                if (ct != null)
                {
                    m_MetaTemplateParamsList.Add(ct);
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "濞屸剝婀侀崣鎴犲箛鐎圭偘缍嬮惃鍕侀弶璺ㄨ!!" + m_MetaClass?.name);
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 取模块根下的 Project 类（.sp 工程类，ref module 加载后位于模块根下）。
        /// 用于 ModuleName.name 限定访问时遍历模块 Project 静态成员。
        /// </summary>
        private MetaClass GetModuleProjectMetaClass( MetaModule metaModule )
        {
            if (metaModule?.metaNode == null)
            {
                return null;
            }
            var projectNode = metaModule.metaNode.GetChildrenMetaNodeByName("Project");
            if (projectNode == null || !projectNode.IsMetaClass())
            {
                return null;
            }
            return projectNode.GetMetaClassByTemplateCount(0);
        }

        /// <summary>
        /// ModuleName.name（模块前缀限定名）：模块根下 enum/data/class/namespace 未命中时，
        /// 继续遍历该模块 Project 类定义的静态成员（变量/函数）。
        /// 例如引用 Std 模块后，Std.Pi / Std.Fn() 取 Std 工程 Project 定义的静态成员。
        /// </summary>
        private bool HandleModuleProjectMember()
        {
            if (m_FrontCallNode?.m_MetaNode?.isMetaModule != true)
            {
                return false;
            }
            var projectMc = GetModuleProjectMetaClass(m_FrontCallNode.m_MetaNode.metaModule);
            if (projectMc == null)
            {
                return false;
            }
            return GetFunctionOrVariableByOwnerClass(projectMc, m_Name, projectMc);
        }

        public bool GetFunctionOrVariableByOwnerClass(MetaClass mc, string inputname, MetaClass staticCallMetaClass = null)
        {
            MetaMemberVariable mmv = null;
            MetaMemberFunction mmf = null;
            if (m_IsFunction)
            {   
                //this.CreateMetaTemplateParams(null, m_OwnerMetaFunctionBlock.ownerMetaFunction as MetaMemberFunction);
                //List<MetaClass> mcList = new List<MetaClass>();
                //for( int i = 0;i < this.m_MetaTemplateParamsList.Count; i++ )
                //{
                //    var itn = this.m_MetaTemplateParamsList[i];
                //    if(itn.metaClass != null )
                //    {
                //        mcList.Add(itn.metaClass);
                //    }
                //}
                //if(mcList.Count != this.m_MetaTemplateParamsList.Count)
                //{
                //    mcList = null;
                //}
                mmf = mc.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount(inputname, this.m_FileMetaCallNode.inputTemplateNodeList.Count, m_MetaInputParamCollection, true);
            }
            else
            {
                mmv = mc.GetMetaMemberVariableByName(inputname);
                if (mmv == null &&  (m_AllowUseSettings.setterFunction ||m_AllowUseSettings.getterFunction ) )
                {
                    if( m_AllowUseSettings.setterFunction )
                    {
                        if (m_MetaInputParamCollection == null )
                        {
                            m_MetaInputParamCollection = new MetaInputParamCollection(ownerMetaBase, m_OwnerMetaFunctionBlock);
                        }
                        if( m_MetaInputParamCollection.metaInputParamList.Count > 0 )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "set 鐨勬柟娉? 涓嶅簲璇ユ湁鍙傛暟锛岃€屾槸閫氳繃澶栭儴浼犲叆");
                            m_MetaInputParamCollection.Clear();
                        }
                        if(m_RightExpress != null )
                        {
                            // setter 参数不走 MetaInputParam.Parse 的转换链路，
                            // New 对象表达式（如 x.prop = ArrClass(){...}）必须
                            // 在此 ConvertNewExpress，否则 IR 生成时报
                            // IRMethodNotSupportNew 且参数指令缺失。
                            var setterArg = ExpressManager.ConvertNewExpress(m_RightExpress, null);
                            if (setterArg != null)
                            {
                                MetaInputParam mip = new MetaInputParam(setterArg);
                                m_MetaInputParamCollection.AddMetaInputParam(mip);
                            }
                        }
                    }
                    if( !m_AllowUseSettings.setterFunction && m_AllowUseSettings.getterFunction)
                    {
                        if (m_MetaInputParamCollection?.metaInputParamList.Count > 0)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "get 鐨勬柟娉? 涓嶅簲璇ユ湁鍙傛暟");
                            m_MetaInputParamCollection.Clear();
                        }
                    }
                    mmf = mc.GetMetaDefineGetSetMemberFunctionByName(inputname, m_MetaInputParamCollection,
                        m_AllowUseSettings.getterFunction,
                        m_AllowUseSettings.setterFunction);
                }
            }

            if (mmv == null && mmf == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreParseCallNodeNotFoundInClass, m_Token, "", m_Token, mc.allName);
                return false;
            }

            if (mmv != null)
            {
                m_MetaVariable = mmv;
                m_MetaType = mmv.GetFinalMetaType();
                if( m_MetaType == null )
                {
                    mmv.ParseMetaExpress();
                    mmv.ParseRealMetaType();
                    m_MetaType = mmv.GetFinalMetaType();
                    if( m_MetaType == null )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error not found metatype class type" );
                    }
                }
                else
                {
                    // Type is already known (e.g. primitive types like int/float),
                    // but the member's expression might not have been parsed yet.
                    // Ensure ParseMetaExpress is called so the referenced member
                    // gets a smaller parseOrder and executes first at runtime.
                    mmv.ParseMetaExpress();
                }
                m_CallNodeType = ECallNodeType.MemberVariableName;
                if( mmv.isStatic || m_CallNodeType == ECallNodeType.Base )
                {
                    // 模块前缀限定访问（如 Std.Pi）时前节点是模块类型而非成员所属类，
                    // 静态调用的 IR 定位依赖 staticCallMetaType.metaClass，需显式指定 Project 类。
                    m_StaticCallMetaType = staticCallMetaClass != null
                        ? new MetaType(staticCallMetaClass)
                        : new MetaType(m_FrontCallNode.metaType);
                }
            }
            else if (mmf != null)
            {
                m_MetaFunction = mmf;
                m_MetaType = mmf.returnMetaVariable.GetFinalMetaType();
                m_CallNodeType = ECallNodeType.MemberFunctionName;
                if (mmf.isStatic || m_FrontCallNode.m_CallNodeType == ECallNodeType.Base)
                {
                    // 同上：模块前缀限定访问（如 Std.Fn()）时静态调用需显式指向 Project 类。
                    m_StaticCallMetaType = staticCallMetaClass != null
                        ? new MetaType(staticCallMetaClass)
                        : new MetaType(m_FrontCallNode.metaType);
                }
            }
            return true;
        }

        /// <summary>
        /// 璺ㄦā鍧楋紙ref module锛夊鍏ョ殑娉涘瀷绫伙細娉涘瀷瀹炰緥涓嶄細鍏嬮殕鎴愬憳鍑芥暟锛?
        /// 鎴愬憳鍑芥暟杩斿洖绫诲瀷涓彲鑳芥畫鐣欏０鏄庣被鐨勬湭缁戝畾妯℃澘鍙傛暟锛堝 List<T> 鐨?getRange 杩斿洖 List<T>锛夈€?
        /// 褰撴帴鏀惰€呮槸娉涘瀷瀹炰緥锛堝 List<int>锛夋椂锛岀敤鎺ユ敹鑰呯殑妯℃澘瀹炲弬鏇挎崲杩斿洖绫诲瀷涓殑鏈粦瀹氭ā鏉匡紝
        /// 浣?`Std.List<int> sub = list.getRange(2,2)` 杩欑被璧嬪€肩殑绫诲瀷姣旇緝閫氳繃銆?
        /// </summary>
        private MetaType TryBindReceiverTemplateArgs(MetaType retMt, MetaMemberFunction mmf)
        {
            if (retMt == null || mmf?.ownerMetaClass == null || m_FrontCallNode == null)
                return retMt;

            var recvMt = m_FrontCallNode.metaType;
            var recvMgtc = recvMt?.metaClass as MetaGenTemplateClass;
            if (recvMgtc == null)
                return retMt;

            // 鍙鐞嗚繑鍥炵被鍨嬩腑娈嬬暀浜嗘帴鏀惰€呭０鏄庣被鐨勬湭缁戝畾妯℃澘鐨勬儏鍐?
            if (!MetaTypeContainsOwnerTemplate(retMt, recvMgtc.metaTemplateClass))
                return retMt;

            // 鎷疯礉鍚庢浛鎹紝閬垮厤姹℃煋鍏变韩鐨勫嚱鏁拌繑鍥炵被鍨嬪畾涔?
            var boundMt = new MetaType(retMt);
            if (TypeManager.instance.UpdateMetaTypeByGenClassAndFunction(boundMt, recvMgtc, null))
                return boundMt;
            return retMt;
        }

        private static bool MetaTypeContainsOwnerTemplate(MetaType mt, MetaClass ownerClass)
        {
            if (mt == null) return false;
            if (mt.isTemplate && mt.metaTemplate?.ownerClass == ownerClass)
                return true;
            var childList = mt.GetGenTemplateMetaTypeList();
            if (childList == null) return false;
            for (int i = 0; i < childList.Count; i++)
            {
                if (MetaTypeContainsOwnerTemplate(childList[i], ownerClass))
                    return true;
            }
            return false;
        }

        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_CallNodeSign == ECallNodeSign.Period)
            {
                sb.Append(".");
            }
            if (m_CallNodeType == ECallNodeType.Express)
            {
                sb.Append(m_ExpressNode.ToFormatString());
            }
            else
            {
                if (m_CallNodeType == ECallNodeType.ClassName
                     //|| m_CallNodeType == ECallNodeType.GenClassName
                     )
                {
                    if (m_CallNodeSign == ECallNodeSign.Null)
                        sb.Append(m_MetaClass?.allName);
                    else
                        sb.Append(m_MetaClass?.name);
                }
                else if (m_CallNodeType == ECallNodeType.EnumName)
                {
                    sb.Append(m_MetaEnum?.allName ?? m_MetaEnum?.name);
                }
                else if (m_CallNodeType == ECallNodeType.EnumMember )
                {
                    sb.Append(m_MetaVariable?.name);
                }
                else if (m_CallNodeType == ECallNodeType.DataName)
                {
                    sb.Append(m_MetaData?.allName);
                }
                //else if (m_CallNodeType == ECallNodeType.MemberDataName)
                //{
                //    sb.Append(m_MetaVariable?.name);
                //}
                else if (m_CallNodeType == ECallNodeType.NewClass)
                {
                    sb.Append(m_MetaClass.ToFormatString());
                }
                else if(m_CallNodeType == ECallNodeType.NewData )
                {
                    sb.Append( m_MetaData.ToFormatString() );
                }
                else if (m_CallNodeType == ECallNodeType.NewTemplate)
                {
                    sb.Append(m_MetaClass.ToFormatString());
                }
                else if (m_CallNodeType == ECallNodeType.MetaNode)
                {
                    sb.Append(m_MetaNode?.name);
                }
                else if (m_CallNodeType == ECallNodeType.MemberFunctionName)
                {
                    //sb.Append(m_MetaFunction.isStatic ? "[static]" : "[nonstatic]" + m_MetaFunction?.ToFormatString());
                    sb.Append(m_MetaFunction?.ToString());
                }
                else if (m_CallNodeType == ECallNodeType.FunctionInnerVariableName)
                {
                    sb.Append(m_MetaVariable?.name);
                }
                else if (m_CallNodeType == ECallNodeType.VisitVariable)
                {
                    sb.Append(m_MetaVariable?.ToString());
                }
                else if (m_CallNodeType == ECallNodeType.MemberVariableName)
                {
                    sb.Append(m_MetaVariable?.name);
                }
                else if (m_CallNodeType == ECallNodeType.This)
                {
                    sb.Append("this");
                }
                else if (m_CallNodeType == ECallNodeType.Base)
                {
                    sb.Append("base");
                }
                else if (m_CallNodeType == ECallNodeType.Global)
                {
                    sb.Append("global");
                }
                else if (m_CallNodeType == ECallNodeType.MetaType)
                {
                    sb.Append(m_MetaType.ToString());
                }
                else if (m_CallNodeType == ECallNodeType.GetType )
                {
                    sb.Append("type");
                }
                else if (m_CallNodeType == ECallNodeType.ConstValue)
                {
                    sb.Append(m_ExpressNode.ToString());
                }
                else
                {
                    //sb.Append("Error 鐟欙絾鐎絋oken闁挎瑨顕? + token?.ToLexemeAllString());
                    sb.Append(m_Token?.lexeme.ToString() + "CallNodeType:" + m_CallNodeType.ToString() + 
                        "Error(CurrentMetaBase is Null!)");
                }
            }
            return sb.ToString();
        }
        public override string ToString()
        {
            return ToFormatString();
        }
    }
}











//else if (frontCNT == ECallNodeType.MemberDataName)
//{
//    if (TryBuildDataToStringSystemCall(m_Name))
//    {
//        return true;
//    }

//    var md = m_FrontCallNode.m_MetaVariable as MetaMemberData;
//    MetaMemberData findMd = null;
//    if (md != null)
//    {
//        if( m_IsFunction )
//        {
//            MetaClass mc = md.GetFinalMetaType().metaClass;
//            if( mc != null )
//            {
//                m_MetaClass = mc;
//                m_CallNodeType = ECallNodeType.MemberVariableName;
//                m_MetaType = new MetaType(mc);
//                if( !GetFunctionOrVariableByOwnerClass(m_MetaClass, m_Name) )
//                {
//                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "find function failed");
//                    return false;
//                }
//                else
//                {
//                }

//            }
//            else
//            {
//                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "");
//                return false;
//            }
//        }   
//        else
//        {
//            var dataType = md.GetFinalMetaType();
//            if (dataType != null)
//            {
//                if( dataType.isData )
//                {
//                    findMd = dataType.metaData.GetMemberDataByName(m_Name);
//                    if (findMd == null)
//                    {
//                        Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 濞屸剝婀侀幍鎯у煂{m_Name} 閻ㄥ嚜etaData閺佺増宓?");
//                        return false;
//                    }
//                    if (findMd.memberDataType == EMemberDataType.MemberClass)
//                    {
//                        m_MetaClass = findMd.GetFinalMetaType()?.metaClass;
//                        m_CallNodeType = ECallNodeType.MemberVariableName;
//                    }
//                    else if (findMd.memberDataType == EMemberDataType.ConstValue)
//                    {
//                        if (findMd.isConst)
//                        {
//                            //杩欏潡锛屽彲浠ュ啓鎴愬父閲忔ā寮?
//                            //m_CallNodeType = ECallNodeType.ConstValue;
//                            //EType etyp = CoreMetaClassManager.GetETypeByMetaClass(m_MetaVariable.GetFinalMetaType().metaClass);
//                            //this.m_ExpressNode = new MetaConstExpressNode(etyp, m_MetaVariable.)
//                            m_CallNodeType = ECallNodeType.MemberVariableName;
//                            m_ExpressNode = findMd.expressNode;
//                        }
//                        else
//                        {
//                            m_CallNodeType = ECallNodeType.MemberVariableName;
//                            m_ExpressNode = findMd.expressNode;
//                        }
//                        m_MetaVariable = findMd;
//                    }
//                    else if (findMd.memberDataType == EMemberDataType.MemberArray)
//                    {
//                        m_MetaClass = findMd.GetFinalMetaType()?.metaClass;
//                    }
//                    else if (findMd.memberDataType == EMemberDataType.MemberData)
//                    {
//                        m_CallNodeType = ECallNodeType.MemberVariableName;
//                        m_MetaVariable = findMd;
//                    }
//                    else
//                    {
//                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "not found memberDataType");
//                    }
//                }
//                else
//                {
//                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "dmemberDataType not is data ");
//                    return false;
//                }
//            }
//            else
//            {
//                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "dmemberDataType not is data 2");
//                return false;
//            }
//        }
//    }
//    //else if (findMd.memberDataType == EMemberDataType.ConstValue)
//    //{
//    //    m_CallNodeType = ECallNodeType.ConstValue;
//    //}
//    //else if (findMd.memberDataType == EMemberDataType.MemberArray)
//    //{
//    //    m_MetaVariable = findMd;
//    //    m_CallNodeType = ECallNodeType.MemberDataName;
//    //}
//}
