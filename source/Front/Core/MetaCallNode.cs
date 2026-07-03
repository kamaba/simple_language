//****************************************************************************
//  File:      MetaCallNode.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/5/17 12:00:00
//  Description:  this's a calllink's node handle
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
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
        //GenClassName,
        //TypeName,
        TemplateName,
        EnumName,
        EnumMember,
        DataName,
        DataValue,
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
        public List<MetaExpressNodeBase> expressNodeList = new List<MetaExpressNodeBase>();
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
            expressNodeList = clone.expressNodeList;
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
        public bool isQuestionMarkDot => m_IsQuestionMarkDot;
        public MetaExpressNodeBase metaExpressValue => m_ExpressNode;
        public List<MetaExpressNodeBase> bracketExpressList => m_BracketExpressList;
        public List<MetaType> metaTemplateParamsList => m_MetaTemplateParamsList;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;
        public MetaClass ownerMetaClass => m_OwnerMetaBase as MetaClass;
        public MetaData ownerMetaData => m_OwnerMetaBase as MetaData;
        public MetaEnum ownerMetaEnum => m_OwnerMetaBase as MetaEnum;
        public MetaBase ownerMetaBase => m_OwnerMetaBase;
        public MetaBlockStatements ownerMetaFunctionBlock => m_OwnerMetaFunctionBlock;
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public FileMetaBraceTerm fileMetaBraceTerm => m_FileMetaCallNode != null ? m_FileMetaCallNode.fileMetaBraceTerm : null;
        public FileMetaParTerm fileMetaParTerm => m_FileMetaCallNode != null ? m_FileMetaCallNode.fileMetaParTerm : null;
        public MetaType staticCallMetaType => m_StaticCallMetaType;
        //public MetaGenTemplateClass genMetaClass => m_GenMetaClass;
        //public MetaData metaData => m_MetaData;
        public MetaEnum metaEnum => m_MetaEnum;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaTemplate metaTemplate => m_MetaTemplate;
        public MetaFunction metaFunction => m_MetaFunction;
        public MetaType metaType => m_MetaType;

        private AllowUseSettings m_AllowUseSettings;
        private ECallNodeType m_CallNodeType = ECallNodeType.None;
        private ECallNodeSign m_CallNodeSign = ECallNodeSign.Null;
        private bool m_IsQuestionMarkDot = false;
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
        private MetaType m_FrontDefineMetaType = null;
        private MetaExpressNodeBase m_ExpressNode = null;    // a+b+([expressNode[3+20+10.0f]).ToString() 涓殑3+20+10.f灏辨槸琛ㄧず寮?, fun(expressNode)
        private MetaVariable m_StoreMetaVariable = null;        // store metaVariable 像 a.val = new(){} val就是store 
        private MetaVariable m_DefineMetaVariable = null;       // define variable 定义变量，是比如 像set方法，对解析有约束作用 比如 a.set( value ); value的函数定义就是定义变量 是要传进来的，而不用自己再创建一个变量
        private List<MetaExpressNodeBase> m_BracketExpressList = new List<MetaExpressNodeBase>();   // a[1][1][1][]   瑙ｆ瀽鐨勬槸杩欎釜閲岃竟鐨?,鎴栬€呮槸鍦╗]閲岃竟鐨勫彉閲?

        private MetaNode m_MetaNode = null;
        private MetaType m_MetaType = null;
        private MetaClass m_MetaClass = null;
        private MetaData m_MetaData = null;
        private MetaEnum m_MetaEnum = null;
        private MetaTemplate m_MetaTemplate = null;
        private MetaVariable m_MetaVariable = null;
        private MetaFunction m_MetaFunction = null;
        private string m_Name;
        //private bool m_NextNotAllowParse = false;
        private bool m_VisitFlag = false;

        public MetaCallNode()
        { }
        public MetaCallNode(MetaExpressNodeBase mcen, MetaBase mc, MetaBlockStatements mbs, MetaType fdmt)
        {
            m_InputExpressNode = mcen;
            m_OwnerMetaBase = mc;
            m_OwnerMetaFunctionBlock = mbs;
            m_FrontDefineMetaType = fdmt;
        }
        public MetaCallNode( MetaBase mb, MetaBlockStatements mbs )
        {

        }
        public MetaCallNode(FileMetaCallNode fmcn1, FileMetaCallNode fmcn2, MetaBase mc, MetaBlockStatements mbs, MetaType fdmt)
        {
            m_FileMetaCallSign = fmcn1;
            m_FileMetaCallNode = fmcn2;
            m_Token = m_FileMetaCallNode?.token;
            m_OwnerMetaBase = mc;
            m_OwnerMetaFunctionBlock = mbs;
            m_FrontDefineMetaType = fdmt;

            if (fmcn1 != null && fmcn1.token?.type == ETokenType.QuestionMarkDot)
            {
                m_IsQuestionMarkDot = true;
            }
            if (fmcn2 != null && fmcn2.questionMarkDotToken != null)
            {
                m_IsQuestionMarkDot = true;
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
            if (m_FileMetaCallSign != null)
            {
                if (m_FileMetaCallSign.token.type == ETokenType.Period)
                {
                    m_CallNodeSign = ECallNodeSign.Period;
                }
                else if (m_FileMetaCallSign.token.type == ETokenType.And)
                {
                    m_CallNodeSign = ECallNodeSign.Pointer;
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaStatements Parse  涓嶅厑璁镐娇鐢ㄥ叾瀹冭繛鎺ョ!!");
                    return false;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error MetaStatements Parse  涓嶅厑璁镐娇鐢ㄥ叾瀹冭繛鎺ョ!!");
                    return false;
                }
            }

            if (m_InputExpressNode != null)
            {
                flag = FindArrayNode();
            }
            else
            {
                if (m_FileMetaCallNode == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 瀹氫箟鍘熸暟鎹负绌?! " + m_Token.ToLexemeAllString());
                }
                if (m_FileMetaCallNode != null && m_FileMetaCallNode.fileMetaParTerm != null && !m_IsFunction)
                {
                    var firstNode = m_FileMetaCallNode.fileMetaParTerm.fileMetaExpressList[0];
                    if (firstNode == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 涓嶈兘浣跨敤杈撳叆()涓殑鍐呭 0鍙蜂綅鐨勬病鏈夊唴瀹?!");
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
                        return true;
                    }
                }
                else
                {
                    flag = CreateCallNode();
                }
                if (this.m_FileMetaCallNode.fileMetaBracketTermList.Count > 0)
                {
                    MetaType mt = null;
                    if (m_MetaVariable != null)
                    {
                        var fmt = m_MetaVariable.GetFinalMetaType();
                        if (fmt.IsArray())
                        {
                            mt = new MetaType(CoreMetaClassManager.arrayMetaClass);
                            mt.AddDefineTemplateMetaType(new MetaType(CoreMetaClassManager.int32MetaClass));
                            //mt = new MetaType( CoreMetaClassManager.int32MetaClass );
                        }
                    }
                    for (int i = 0; i < m_FileMetaCallNode.fileMetaBracketTermList.Count; i++)
                    {
                        CreateExpressParam cep = new CreateExpressParam();
                        cep.fme = m_FileMetaCallNode.fileMetaBracketTermList[i];
                        cep.equalMetaVariable = null;
                        cep.metaType = mt;
                        cep.ownerMBS = m_OwnerMetaFunctionBlock;
                        cep.ownerMetaBase = m_OwnerMetaFunctionBlock.ownerMetaBase;

                        var en = ExpressManager.CreateExpressNodeByCEP(cep);
                        en.Parse(_auc);
                        m_BracketExpressList.Add(en);
                    }
                }
            }
            return flag;
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
                    Debug.Assert(false);
                }
            }
            else
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 涓嶆敮鎸佽〃杈惧紡绫诲瀷!!");
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
                //                //Array1.0.x 涓嶅厑璁?
                //                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 鍦ˋrray.鍚庤竟濡傛灉浣跨敤鍙橀噺鎴栬€呮槸鏁板瓧甯搁噺锛屽繀椤讳娇鐢ˋrray.$鏂瑰紡!!");
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
                    if (m_FrontDefineMetaType == null)
                    {
                        if( m_AllowUseSettings.isTryRightExpress == false )
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error missing front define meta type." + m_Token.ToLexemeAllString());
                        }
                        return false;
                    }
                    m_MetaType = m_FrontDefineMetaType;
                    if (m_FrontDefineMetaType.eMetaTypeType == EMetaTypeType.Template)
                    {
                        m_MetaTemplate = m_FrontDefineMetaType.metaTemplate;
                        m_MetaType = new MetaType(m_MetaTemplate, "");
                        m_CallNodeType = ECallNodeType.NewTemplate;
                        MetaMemberFunction mmf = m_FrontDefineMetaType.metaClass.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, m_MetaInputParamCollection);
                        if (mmf == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 111" + m_FrontDefineMetaType.metaClass.allName + "init!)", m_Token);
                            return false;
                        }
                        this.m_MetaFunction = mmf;
                    }
                    else if (m_FrontDefineMetaType.eMetaTypeType == EMetaTypeType.MetaClass)
                    {
                        m_MetaClass = m_FrontDefineMetaType.metaClass;
                        m_CallNodeType = ECallNodeType.NewClass;
                    }
                    else if( m_FrontDefineMetaType.eMetaTypeType == EMetaTypeType.MetaData )
                    {
                        m_MetaData = m_FrontDefineMetaType.metaData;
                        m_CallNodeType = ECallNodeType.NewData;
                    }
                    else
                    {
                        m_CallNodeType = ECallNodeType.NewTemplate;
                        m_MetaClass = m_FrontDefineMetaType.metaClass;
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
                m_MetaVariable = mmf?.thisMetaVariable;
                m_CallNodeType = ECallNodeType.This;
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
                if (isFirst)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "local.notfound");
                    return false;
                }

                var fm = m_FileMetaCallNode?.fileMeta?.GetFileMetaLocalSyntax();
                if (fm == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error local 瑙ｆ瀽澶辫触: fileMeta 涓虹┖");
                    return false;
                }
                var global = ClassManager.instance.TryGetProjectMetaClass();
                if (global == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "global now allow function");
                    return false;
                }

                var varName = "local_" + m_FileMetaCallNode?.fileMeta.path  + "_"+ m_FileMetaCallNode?.fileMeta.path.GetHashCode();
                var mv = global.GetMetaMemberVariableByName(varName);
                if (mv == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error local 瑙ｆ瀽澶辫触: 娌℃湁鎵惧埌 local instance 鍙橀噺: " + varName);
                    return false;
                }
                m_MetaVariable = mv;
                m_CallNodeType = ECallNodeType.Local;
                m_MetaType = mv.realMetaType;
                m_StaticCallMetaType = new MetaType(m_MetaType);
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
                    //        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 涓嶈兘瀹炰緥鍖栨娊璞＄被: " + m_MetaClass.name + " " + m_Token.ToLexemeAllString());
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
                                    m_MetaType = new MetaType(CoreMetaClassManager.enumMetaData);
                                    m_CallNodeType = ECallNodeType.EnumName;
                                }
                                else if (mn.IsMetaClass())
                                {
                                    m_MetaClass = mn.GetMetaClassByTemplateCount(this.metaTemplateParamsList.Count);
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
                                    //这块，可以写成常量模式
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
                                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 娌℃湁鎵惧埌 鍏充簬绫讳腑" + m_FrontCallNode.m_MetaClass.allName + "鐨刜init_鏂规硶!)");
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "娌℃湁鎺ㄧ畻鍑虹浉褰撶殑绫诲瀷");
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 鍑芥暟娌℃湁杩斿洖绫诲瀷");
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 鏆備笉鏀寔涓婅妭鐐圭殑绫诲瀷: " + frontCNT.ToString());
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

            //涓嬭竟鐨勪唬鐮佹湭閲嶆瀯鍚庯紝鏈粡杩囬獙璇侊紝闇€瑕侀獙璇?
            if (m_IsFunction)
            {
                if (m_CallNodeType == ECallNodeType.MemberFunctionName)
                {
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 褰撳墠浣嶇疆涓嶅厑璁告湁鍑芥暟璋冪敤鏂瑰紡浣跨敤!!!" + m_Token?.ToLexemeAllString());
                    }
                }
                else if (m_MetaData != null)
                {
                    if (m_MetaData.isStatic)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error data static 不允许进行实例化(new/构造调用): " + m_MetaData.allName);
                        return false;
                    }
                    m_CallNodeType = ECallNodeType.NewData;
                    /*
                    if (m_FileMetaCallNode.fileMetaBraceTerm != null)  //鍙互浣跨敤  ArrClass(){ x = ??} 鐨勬柟寮?
                    {
                        if (m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 鍦↖nputParam 閲岃竟锛屾瀯寤哄嚱鏁帮紝鍙厑璁?浣跨敤ClassName() 鐨勬柟寮? " +
                                "涓嶅厑璁镐娇鐢?ClassName(){}鐨勬柟寮" + m_FileMetaCallNode.fileMetaBraceTerm.ToTokenString());
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
            // ClassName 涓€鑸娇鐢ㄥ湪 Class1.闈欐€佸彉閲忥紝鎴栬€呮槸闈欐€佹柟娉曠殑璋冪敤
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
                //鏌ユ壘闈欐€佸嚱鏁?
                if (m_MetaFunction is MetaMemberFunction mmf)
                {
                    if (!mmf.isStatic)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 璋冪敤闈為潤鎬佹垚鍛樺嚱鏁帮紝涓嶈兘浣跨敤Class.Variable鐨勬柟寮?");
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 鍦ㄥ綋鍓嶇被: {m_FrontCallNode?.m_MetaClass.name} " +
                        $"閲屾煡鎵惧埌浜嗗瓙椤癸紝浣嗕笉鏄被{m_Name} ");
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 涓嶅厑璁歌闂?private 鎴愬憳");
                    return false;
                }
                if (m_MetaFunction != null && m_MetaFunction.permission == EPermission.Private)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 涓嶅厑璁歌闂?private 鍑芥暟");
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 涓嶅厑璁歌闂?private 鎴愬憳");
                        return false;
                    }
                    if (m_MetaFunction != null && m_MetaFunction.permission == EPermission.Private)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error global." + m_Name + " 涓嶅厑璁歌闂?private 鍑芥暟");
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
            // ClassName 涓€鑸娇鐢ㄥ湪 Class1.闈欐€佸彉閲忥紝鎴栬€呮槸闈欐€佹柟娉曠殑璋冪敤
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
                //鏌ユ壘闈欐€佸嚱鏁?
                if (m_MetaFunction is MetaMemberFunction mmf)
                {
                    if (!mmf.isStatic)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 璋冪敤闈為潤鎬佹垚鍛樺嚱鏁帮紝涓嶈兘浣跨敤Class.Variable鐨勬柟寮?");
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 璋冪敤闈為潤鎬佹垚鍛樺彉閲忥紝涓嶈兘浣跨敤Class.Variable鐨勬柟寮?");
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"Error 鍦ㄥ綋鍓嶇被: {m_FrontCallNode?.m_MetaClass.name} " +
                        $"閲屾煡鎵惧埌浜嗗瓙椤癸紝浣嗕笉鏄被{m_Name} ");
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, $"Error 娌℃湁鎵惧埌{m_Name} 鐨凪etaData鏁版嵁!");
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
                /*
                string tname = "";
                if (m_FrontCallNode?.metaExpressValue is MetaConstExpressNode mce)       //arr[0]
                {
                    tname = mce.value.ToString();
                }
                else
                {
                    if(m_FrontCallNode != null )
                    {
                        var gmv = m_FrontCallNode?.metaVariable;
                        tname = "VarName_" + gmv.name;
                    }
                }
                */
                var variable = m_FrontCallNode.m_MetaVariable;
                if (m_FileMetaCallNode?.atToken != null || m_VisitFlag)
                {
                    // Array1.$i.x   Array1.$mmq.x;
                    var getmv2 = m_OwnerMetaFunctionBlock.GetMetaVariableByName(m_Name);
                    if (getmv2 != null)    //鏌ユ壘鏄惁宸插畾涔夎繃鍙橀噺
                    {
                        string inputMVName = "Visit_" + m_Name;
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
                        var index = Convert.ToInt32(mcen.value);
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
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreVisitTypeShouldIsArray, mcen.token, variable.realMetaType.ToString(), variable.name);
                            return;
                        }

                        m_MetaVariable = new MetaVisitVariable("Visit_" + mcen.value.ToString(), ownerMetaClass, m_OwnerMetaFunctionBlock, variable, mcen);

                        m_CallNodeType = ECallNodeType.VisitVariable;
                    }
                    else if (m_ExpressNode is MetaOpExpressNode moen)
                    {
                        string inputMVName = "Visit_" + m_Name;
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
        //        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error data 默认静态实例创建失败：globalData 为空。");
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

                if( SystemMethodCallDeclarationRegistry.TryResolveName( inputname, out ESystemMethodCall call ) )
                {
                    m_MetaFunction = new MetaMemberFunction.MetaBuiltinFunction(mc, call.ToString());
                    m_MetaFunction.SetIndex( (int)call );
                    var retMt = m_MetaFunction.GetFinalMetaType();
                    //m_StaticCallMetaType = new MetaType(mc);
                    m_MetaType = retMt != null ? new MetaType(retMt) : null;
                    m_CallNodeType = ECallNodeType.SystemFunctionCall;
                    return true;
                }

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

            MetaNode retMC = null;
            // 鏌ユ壘瀹氫箟鍏抽敭瀛楃殑class => range   array
            if (m_Token.extend != null)
            {
                MetaNode findMB = CoreMetaClassManager.GetCoreMetaClass(m_Token.extend.ToString());
                if (findMB?.IsMetaClass() == true)
                {
                    retMC = findMB;
                }
            }
            //鏌ユ壘绫绘ā鍨?
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
            //鏌ユ壘鐖剁被鎴栧瓙绫讳腑鍖呭惈鐨勮妭鐐?
            if (retMC == null && mc != null)
            {
                retMC = mc.metaNode.GetChildrenMetaNodeByName(inputname);
            }
            //閫氳繃fileMeta鏌ユ壘鏄惁鏈夐瀹氫箟瀛楃
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
                    // language keyword: lowercase `range` => default Range<int>
                    // keep case-sensitive behavior so `Range` does not auto-infer here
                    if (count == 0
                        && string.Equals(inputname, "range", StringComparison.Ordinal))
                    {
                        m_MetaClass = retMC.GetMetaClassByTemplateCount(1);
                        m_MetaType = new MetaType(m_MetaClass, new List<MetaType>()
                        {
                            new MetaType(CoreMetaClassManager.int32MetaClass)
                        });
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 娌℃湁鍙戣RetMC鐨勭被鍒玀etaCommon");
                }
            }
            else
            {
                // 内置/工程/文件 typealias（含 TypeManager.m_GlobalTypeAliasDict，如 ObjectArray -> Array<Object>）
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
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "绗竴浣嶇殑鎴愬憳鍙橀噺鍚嶇О蹇呴』鏄釜闈欐€佸彉閲忔墠鍙互鍝?");
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
                            return true;
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "绗竴浣嶇殑鎴愬憳鍑芥暟鍚嶇О蹇呴』鏄釜闈欐€佸嚱鏁版墠鍙互鍝?");
                            return false;
                        }
                    }
                }
                else if (md != null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error data 不支持在本体内调用 " + me.allName);
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error enum 不支持在本体内调用 " + me.allName);
                    return false;
                    //if (m_IsFunction)
                    //{
                    //    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error data 不支持函数调用: " + me.allName);
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

            //鍑芥暟鍐呮垚鍛?
            if (retMC == null)
            {
                var ownerFun = m_OwnerMetaFunctionBlock?.ownerMetaFunction;
                if (ownerFun != null)
                {
                    //鍑芥暟鐨勫弬鏁版槸鍚︽槸妯＄増锛屽鏋滄槸锛屽垯杩斿洖
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
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "娌℃湁鍙戠幇瀹炰綋鐨勬ā鏉跨被!!" + m_MetaClass?.name);
                    return false;
                }
            }
            return true;
        }
        public bool GetFunctionOrVariableByOwnerClass(MetaClass mc, string inputname)
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
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "set 的方法  不应该有参数，而是通过外部传入");
                            m_MetaInputParamCollection.Clear();
                        }
                        MetaInputParam mip = new MetaInputParam(m_AllowUseSettings.expressNodeList[0]);
                        m_MetaInputParamCollection.AddMetaInputParam(mip);
                    }
                    if( !m_AllowUseSettings.setterFunction && m_AllowUseSettings.getterFunction)
                    {
                        if (m_MetaInputParamCollection?.metaInputParamList.Count > 0)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "get 的方法  不应该有参数");
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
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "没有找到metatype的类型" );
                    }
                }
                m_CallNodeType = ECallNodeType.MemberVariableName;
                if( mmv.isStatic || m_CallNodeType == ECallNodeType.Base )
                {
                    m_StaticCallMetaType = new MetaType(m_FrontCallNode.metaType);
                }
            }
            else if (mmf != null)
            {
                m_MetaFunction = mmf;
                m_MetaType = mmf.returnMetaVariable.GetFinalMetaType();
                m_CallNodeType = ECallNodeType.MemberFunctionName;
                if (mmf.isStatic || m_FrontCallNode.m_CallNodeType == ECallNodeType.Base)
                {
                    m_StaticCallMetaType = new MetaType(m_FrontCallNode.metaType);
                }
            }
            return true;
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
                    //sb.Append("Error 瑙ｆ瀽Token閿欒" + token?.ToLexemeAllString());
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
//                        Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error 娌℃湁鎵惧埌{m_Name} 鐨凪etaData鏁版嵁!");
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
//                            //这块，可以写成常量模式
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