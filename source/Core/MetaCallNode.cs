//****************************************************************************
//  File:      MetaCallNode.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/5/17 12:00:00
//  Description:  this's a calllink's node handle
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum ECallNodeSign
    {
        Null,
        Period,
        Pointer,
    }
    public enum ECallNodeType
    {
        Null,
        MetaNode,
        MetaType,
        ClassName,
        //GenClassName,
        TypeName,
        TemplateName,
        EnumName,
        EnumDefaultValue,
        EnumValueArray,
        DataName,
        DataValue,
        FunctionInnerVariableName,
        VisitVariable,
        IteratorVariable,
        MemberVariableName,
        MemberDataName,
        NewClass,
        NewTemplate,
        NewData,
        MemberFunctionName,
        ConstValue,
        This,
        Base,
        Global,
        Express
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
        }
    }
    //public class Level1<T1,T2>
    //{
    //    static T1 t1 = default(T1);
    //    static T1 Open( T1 t )
    //    {
    //        Level1<T1, short>.t1 = t1;

    //        return t1
    //    }
    //}

    public sealed class MetaCallNode
    {
        public string mame => m_Name;
        public MetaCallNode frontCallNode => m_FrontCallNode;
        public ECallNodeType callNodeType => m_CallNodeType;
        public MetaExpressNode metaExpressValue => m_ExpressNode;
        public List<MetaType> metaTemplateParamsList => m_MetaTemplateParamsList;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent => m_MetaBraceStatementsContent;
        public MetaInputParamCollection metaInputParamCollection => m_MetaInputParamCollection;
        public MetaBlockStatements ownerMetaFunctionBlock => m_OwnerMetaFunctionBlock;
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public MetaType callMetaType => m_CallMetaType;
        //public MetaClass metaClass => m_MetaClass;
        //public MetaGenTemplateClass genMetaClass => m_GenMetaClass;
        //public MetaData metaData => m_MetaData;
        //public MetaEnum metaEnum => m_MetaEnum;
        public MetaVariable metaVariable => m_MetaVariable;
        public MetaFunction metaFunction => m_MetaFunction;
        public MetaType metaType => m_MetaType;

        private AllowUseSettings m_AllowUseSettings;
        private ECallNodeType m_CallNodeType;
        private ECallNodeSign m_CallNodeSign = ECallNodeSign.Null;
        public bool m_IsArray = false;
        public bool m_IsFunction = false;

        private MetaCallNode m_FrontCallNode = null;
        private FileMetaCallNode m_FileMetaCallSign = null;
        private FileMetaCallNode m_FileMetaCallNode = null;
        private Token m_Token = null;

        private MetaType m_CallMetaType = null;


        private MetaBlockStatements m_OwnerMetaFunctionBlock = null;
        private MetaClass m_OwnerMetaClass = null;
        private MetaInputParamCollection m_MetaInputParamCollection = null;
        private List<MetaType> m_MetaTemplateParamsList = new List<MetaType>();
        private MetaBraceOrBracketStatementsContent m_MetaBraceStatementsContent  = null;
        private MetaType m_FrontDefineMetaType = null;
        private MetaExpressNode m_ExpressNode = null;    // a+b+([expressNode[3+20+10.0f]).ToString() 中的3+20+10.f就是表示式 , fun(expressNode)
        private List<MetaCallLink> m_MetaArrayCallNodeList = new List<MetaCallLink>();
        private MetaVariable m_DefineMetaVariable = null;
        private MetaVariable m_StoreMetaVariable = null;


        private MetaNode m_MetaNode = null;
        private MetaType m_MetaType = null;
        private MetaClass m_MetaClass = null;
        private MetaData m_MetaData = null;
        private MetaEnum m_MetaEnum = null;
        private MetaTemplate m_MetaTemplate = null;
        private MetaVariable m_MetaVariable = null;
        private MetaFunction m_MetaFunction = null;
        //private MetaGenTemplateClass m_GenMetaClass = null;
        //private MetaGenTempalteFunction m_MetaGenTemplateFunction = null;
        private string m_Name;
        public MetaCallNode()
        { }
        public MetaCallNode(FileMetaCallNode fmcn1, FileMetaCallNode fmcn2, MetaClass mc, MetaBlockStatements mbs, MetaType fdmt )
        {
            m_FileMetaCallSign = fmcn1;
            m_FileMetaCallNode = fmcn2;
            m_Token = m_FileMetaCallNode.token;
            m_OwnerMetaClass = mc;
            m_OwnerMetaFunctionBlock = mbs;
            m_FrontDefineMetaType = fdmt;
            m_IsFunction = m_FileMetaCallNode.isCallFunction;

            m_IsArray = m_FileMetaCallNode.isArray;
            for (int i = 0; i < m_FileMetaCallNode.arrayNodeList.Count; i++)
            {
                MetaCallLink cmcl = new MetaCallLink(m_FileMetaCallNode.arrayNodeList[i], mc, mbs, fdmt, m_DefineMetaVariable );
                m_MetaArrayCallNodeList.Add(cmcl);
            }
            //if (m_FileMetaCallNode.fileMetaBraceTerm != null)
            //{
            //    m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent( m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
            //}
        }
        public void SetFrontCallNode(MetaCallNode mcn)
        {
            m_FrontCallNode = mcn;
        }
        public void SetDefineMetaVariable( MetaVariable mv )
        {
            this.m_DefineMetaVariable = mv;
        }
        public void SetStoreMetaVariable( MetaVariable mv )
        {
            this.m_StoreMetaVariable = mv;
        }
        public bool ParseNode(AllowUseSettings _auc)
        {
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
                    Log.AddInStructMeta(EError.None, "Error MetaStatements Parse  不允许使用其它连接符!!");
                    return false;
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error MetaStatements Parse  不允许使用其它连接符!!");
                    return false;
                }
            }
            if (m_FileMetaCallNode == null)
            {
                Log.AddInStructMeta(EError.None, "Error 定义原数据为空!! " + m_Token.ToLexemeAllString());
            }
            if (m_FileMetaCallNode.fileMetaParTerm != null && !m_IsFunction )
            {
                var firstNode = m_FileMetaCallNode.fileMetaParTerm.fileMetaExpressList[0];
                if (firstNode == null)
                {
                    Log.AddInStructMeta(EError.None, "Error 不能使用输入()中的内容 0号位的没有内容!!");
                }
                else
                {
                    CreateExpressParam cep = new CreateExpressParam()
                    {
                        ownerMBS = m_OwnerMetaFunctionBlock,
                        metaType = null,
                        fme = firstNode,
                    };
                    m_ExpressNode = ExpressManager.CreateExpressNode(cep);
                    m_ExpressNode.CalcReturnType();
                    m_MetaClass = null;// CoreMetaClassManager.GetMetaClassByEType(m_ExpressNode.eType);
                    m_CallNodeType = ECallNodeType.Express;
                    return true;
                }
            }
            return CreateCallNode();
        }
        bool CreateCallNode()
        {
            int tokenLine = m_FileMetaCallNode.token != null ? m_FileMetaCallNode.token.sourceBeginLine : -1;
            m_Name = m_FileMetaCallNode.name;

            string fatherName = m_FrontCallNode?.m_Name;
            bool isAt = m_FileMetaCallNode.atToken != null;
            // 当前是否是第一个元素
            bool isFirst = m_FrontCallNode == null;
            int templateCount = this.m_FileMetaCallNode.inputTemplateNodeList.Count;


            ETokenType etype = m_Token.type;
            ECallNodeType frontCNT = ECallNodeType.Null;

            for (int i = 0; i < this.m_MetaArrayCallNodeList.Count; i++)
            {
                m_MetaArrayCallNodeList[i].Parse(m_AllowUseSettings);
            }
            if (m_FrontCallNode != null)
            {
                frontCNT = m_FrontCallNode.callNodeType;
            }

            if ( m_IsFunction )
            {                
                m_MetaInputParamCollection = new MetaInputParamCollection(m_FileMetaCallNode.fileMetaParTerm, m_OwnerMetaClass, m_OwnerMetaFunctionBlock);

                m_MetaInputParamCollection.CaleReturnType();
            }

            if (!isFirst && frontCNT == ECallNodeType.Null )
            {
                Log.AddInStructMeta( EError.None, "Error 前边节点没有发现MetaBase!!");
                return false;
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
                    MetaVariable mv = m_FrontCallNode.m_MetaVariable;
                    if (mv.isArray)   //Array.
                    {
                        // Array1.$0.x   Array1.1.x;
                        if (isAt)                  //Array.$
                        {
                            string inputMVName = m_Name;
                            m_MetaVariable = mv.GetMetaVaraible(inputMVName);           //Array.@var
                            if (m_MetaVariable == null)
                            {
                                m_MetaVariable = new MetaVisitVariable(inputMVName, m_OwnerMetaClass, m_OwnerMetaFunctionBlock,
                                    mv, null);
                                mv.AddMetaVariable(m_MetaVariable);
                            }
                            isNotConstValue = true;
                        }
                        else
                        {
                            //Array1.0.x 不允许
                            Log.AddInStructMeta(EError.None, "Error 在Array.后边如果使用变量或者是数字常量，必须使用Array.$方式!!");
                        }
                    }
                }
                else if (frontCNT == ECallNodeType.MemberDataName)
                {
                    MetaMemberData mmd = m_FrontCallNode.m_MetaVariable as MetaMemberData;
                    if (mmd != null)
                    {
                        if (mmd.memberDataType == EMemberDataType.MemberArray)
                        {
                            if (isAt)                  //Array.$
                            {
                                string inputMVName = m_Name;
                                m_MetaVariable = mmd.GetMemberDataByName(inputMVName);           //Array.@var
                                if (m_MetaVariable == null)
                                {
                                    m_MetaVariable = new MetaVisitVariable(inputMVName, m_OwnerMetaClass, m_OwnerMetaFunctionBlock,
                                        mmd, null);
                                    mmd.AddMetaVariable(m_MetaVariable);
                                }
                                isNotConstValue = true;
                            }
                            else
                            {
                                //Array1.0.x 不允许
                                Log.AddInStructMeta(EError.None, "Error 在Array.后边如果使用变量或者是数字常量，必须使用Array.$方式!!");
                            }
                        }
                    }
                }
                //不是常量值 
                if (!isNotConstValue)
                {
                    m_CallNodeType = ECallNodeType.ConstValue;
                    m_ExpressNode = new MetaConstExpressNode(m_Token.GetEType(), m_Token.lexeme);
                    m_MetaClass = CoreMetaClassManager.GetMetaClassByEType(m_Token.GetEType());
                    m_MetaVariable = m_OwnerMetaFunctionBlock.GetMetaVariable(m_Token.GetHashCode().ToString());
                    if (m_MetaVariable == null)
                    {
                        m_MetaVariable = new MetaVariable(m_Token.GetHashCode().ToString(),
                            MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaFunctionBlock, m_OwnerMetaClass,
                            new MetaType(m_MetaClass));
                        m_OwnerMetaFunctionBlock.AddMetaVariable(m_MetaVariable);
                    }
                }
            }
            else if (etype == ETokenType.Global)
            {
                if (isFirst)
                {
                    if (m_IsFunction)
                    {
                        Log.AddInStructMeta(EError.None, "Error 不允许global的函数形式!!");
                    }
                    else
                    {
                        m_MetaData = ProjectManager.globalData;
                        m_CallNodeType = ECallNodeType.Global;
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error  不允许 使用global 只有第一位置可以使用This关键字" + m_Token.ToLexemeAllString());
                }
            }
            else if (etype == ETokenType.New)
            {
                if (isFirst)
                {
                    if (!m_IsFunction)
                    {
                        Log.AddInStructMeta(EError.None, "Error 不允许new是非函数形式出现!!" + m_Token.ToLexemeAllString());
                    }
                    else
                    {
                        if (m_FrontDefineMetaType == null)
                        {
                            Log.AddInStructMeta(EError.None, "Error 没有前置定义类型!!" + m_Token.ToLexemeAllString());
                            return false;
                        }
                        m_MetaType = m_FrontDefineMetaType;
                        m_StoreMetaVariable = m_DefineMetaVariable;
                        if (m_FrontDefineMetaType.eType == EMetaTypeType.Template )
                        {
                            m_MetaTemplate = m_FrontDefineMetaType.metaTemplate;
                            m_MetaType = new MetaType(m_MetaTemplate);
                            m_CallNodeType = ECallNodeType.NewTemplate;
                            m_CallMetaType = new MetaType(m_MetaTemplate);
                            MetaMemberFunction mmf = m_FrontDefineMetaType.metaClass.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, m_MetaInputParamCollection);
                            if (mmf == null)
                            {
                                Log.AddInStructMeta(EError.None, "Error 没有找到 关于类中" + m_FrontDefineMetaType.metaClass.allClassName + "的_init_方法!)", m_Token);
                                return false;
                            }
                            this.m_MetaFunction = mmf;
                        }
                        else if( m_FrontDefineMetaType.eType == EMetaTypeType.MetaClass )
                        {
                            m_MetaClass = m_FrontDefineMetaType.metaClass;
                            m_CallNodeType = ECallNodeType.NewClass;
                        }
                        else
                        {
                            m_CallNodeType = ECallNodeType.NewTemplate;
                            m_MetaClass = m_FrontDefineMetaType.metaClass;
                        }
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error 只有第一位置可以使用new关键字" + m_Token.ToLexemeAllString());
                }
            }
            else if (etype == ETokenType.This)
            {
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.MemberVariableExpress)
                {
                    Log.AddInStructMeta(EError.None, "Error 不允许在成员变量中使用this关键字" + m_Token.ToLexemeAllString());
                }
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress)
                {
                    Log.AddInStructMeta(EError.None, "Error 不允许在输入变量中中使用this关键字" + m_Token.ToLexemeAllString());
                }
                //this.普通的函数，变量，get/set方法
                if (isFirst)
                {
                    if (m_IsFunction)
                    {
                        Log.AddInStructMeta(EError.None, "Error 不允许this的函数形式!!" + m_Token.ToLexemeAllString());
                    }
                    else
                    {
                        m_MetaClass = m_OwnerMetaClass;
                        m_MetaVariable = (m_OwnerMetaFunctionBlock.ownerMetaFunction as MetaMemberFunction).thisMetaVariable;
                        m_CallNodeType = ECallNodeType.This;
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error 只有第一位置可以使用This关键字" + m_Token.ToLexemeAllString());
                }
            }
            else if (etype == ETokenType.Base)
            {
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.MemberVariableExpress)
                {
                    Log.AddInStructMeta(EError.None, "Error 不允许在成员变量中使用base关键字" + m_Token.ToLexemeAllString());
                }
                if (this.m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress)
                {
                    Log.AddInStructMeta(EError.None, "Error 不允许在输入变量中中使用base关键字" + m_Token.ToLexemeAllString());
                }

                MetaClass parentClass = m_OwnerMetaClass.metaNode.parentNode.GetMetaClassByTemplateCount(0);
                if (parentClass == null)
                {
                    Log.AddInStructMeta(EError.None, "Error 使用base没有找到父节点!!");
                    return false;
                }

                if (isFirst)
                {
                    if (m_IsFunction)
                    {
                        Log.AddInStructMeta(EError.None, "Error 不允许base的函数形式!!");
                    }
                    else
                    {
                        m_MetaClass = parentClass;
                        m_CallNodeType = ECallNodeType.Base;
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error 只有第一位置可以使用base关键字" + m_Token.ToLexemeAllString());
                }
            }
            else if (etype == ETokenType.Type)
            {
                var selfClass = CoreMetaClassManager.GetCoreMetaClass(m_Name);
                if (selfClass != null)
                {
                    m_MetaClass = selfClass.GetMetaClassByTemplateCount(0);
                    m_CallNodeType = ECallNodeType.TypeName;
                }
            }
            else if (etype == ETokenType.Identifier)
            {
                if (isFirst)
                {
                    // Class1. ns. Int32[]
                    if( GetFirstNode( m_Name, m_OwnerMetaClass, this.m_FileMetaCallNode.inputTemplateNodeList.Count) == false )
                    {
                        return false;
                    }
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
                                mn = SimpleLanguage.CSharp.CSharpManager.FindAndCreateMetaNode(m_FrontCallNode.m_MetaNode, m_Name );
                                if (mn.IsMetaClass())
                                {
                                    m_MetaClass = mn.GetMetaClassByTemplateCount(0);
                                    m_CallNodeType = ECallNodeType.ClassName;
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
                                if (mn.isMetaNamespace || mn.isMetaModule)
                                {
                                    m_MetaNode = mn;
                                    m_CallNodeType = ECallNodeType.MetaNode;
                                }
                                else if (mn.isMetaData)
                                {
                                    m_MetaData = mn.metaData;
                                    m_CallNodeType = ECallNodeType.DataName;
                                }
                                else if (mn.isMetaEnum)
                                {
                                    m_MetaEnum = mn.metaEnum;
                                    m_MetaVariable = m_MetaEnum.metaVariable;
                                    m_CallNodeType = ECallNodeType.EnumName;
                                }
                                else if (mn.IsMetaClass())
                                {
                                    m_MetaClass = mn.GetMetaClassByTemplateCount(this.m_FileMetaCallNode.inputTemplateNodeList.Count);
                                    m_CallNodeType = ECallNodeType.ClassName;
                                }
                                else
                                {
                                    Log.AddInStructMeta(EError.None, "Error 没有发该RetMC的类别MetaCommon");
                                }
                            }
                        }
                    }
                    else if (frontCNT == ECallNodeType.ClassName
                        || frontCNT == ECallNodeType.MetaType )
                    {
                        // ClassName 一般使用在 Class1.静态变量，或者是静态方法的调用
                        MetaNode tmb = null;
                        MetaNode curMetaNode = null;
                        if( frontCNT == ECallNodeType.MetaType )
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
                            if (GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaClass, m_Name) == false)
                            {
                                return false;
                            }
                            //查找静态函数
                            if (m_MetaFunction is MetaMemberFunction mmf)
                            {
                                if (!mmf.isStatic)
                                {
                                    Log.AddInStructMeta(EError.None, "Error 调用非静态成员，不能使用Class.Variable的方式!");
                                    return false;
                                }
                                if (mmf.isConstructInitFunction && !m_AllowUseSettings.callConstructFunction)
                                {
                                    Log.AddInStructMeta(EError.None, "Error 不允许使用构造函数" + m_Token.ToLexemeAllString());
                                    return false;
                                }
                                if (m_FrontCallNode != null)
                                    this.m_CallMetaType = new MetaType(m_FrontCallNode.m_MetaClass, this.m_FrontCallNode.m_MetaTemplateParamsList );
                                else
                                {
                                    this.m_CallMetaType = new MetaType(mmf.ownerMetaClass);
                                }
                            }                           
                            if ( m_MetaVariable is MetaMemberVariable mmv )
                            {
                                if (!mmv.isStatic)
                                {
                                    Log.AddInStructMeta(EError.None, "Error 调用非静态成员，不能使用Class.Variable的方式!");
                                    return false;
                                }
                                if( m_FrontCallNode != null )
                                    this.m_CallMetaType = new MetaType(m_FrontCallNode.m_MetaClass, this.m_FrontCallNode.m_MetaTemplateParamsList );
                                else
                                {
                                    this.m_CallMetaType = new MetaType(mmv.ownerMetaClass);
                                }
                           }
                        }
                        else
                        {
                            if(tmb.IsMetaClass() == false )
                            {
                                Log.AddInStructMeta(EError.None, $"Error 在当前类: {m_FrontCallNode?.m_MetaClass.name} " +
                                    $"里查找到了子项，但不是类{m_Name } ");
                                return false;
                            }
                            m_MetaClass = tmb.GetMetaClassByTemplateCount(templateCount);
                            m_CallNodeType = ECallNodeType.ClassName;
                        }
                    }
                    else if( frontCNT == ECallNodeType.Global || frontCNT == ECallNodeType.DataName) 
                    {
                        m_MetaVariable = GetDataValueByMetaData(m_FrontCallNode.m_MetaData, m_Name );
                        m_CallNodeType = ECallNodeType.MemberDataName;
                    }
                    else if (frontCNT == ECallNodeType.MemberDataName)
                    {
                        var md = m_FrontCallNode.m_MetaVariable as MetaMemberData;
                        MetaMemberData findMd = null;
                        if ( md != null )
                        {
                            findMd = md.GetMemberDataByName(m_Name);
                        }
                        if (findMd == null)
                        {
                            Log.AddInStructMeta(EError.None, $"Error 没有找到{m_Name} 的MetaData数据!");
                            return false;
                        }
                        if (findMd.memberDataType == EMemberDataType.MemberClass)
                        {
                            m_MetaClass = m_MetaVariable.realMetaType.metaClass;
                            m_CallNodeType = ECallNodeType.MemberVariableName;
                        }
                        else if (findMd.memberDataType == EMemberDataType.ConstValue)
                        {
                            m_CallNodeType = ECallNodeType.ConstValue;
                        }
                        else if (findMd.memberDataType == EMemberDataType.MemberArray)
                        {
                            m_MetaVariable = findMd;
                            m_CallNodeType = ECallNodeType.MemberDataName;
                        }
                        else
                        {
                            m_CallNodeType = ECallNodeType.MemberDataName;
                        }
                    }
                    else if( frontCNT == ECallNodeType.EnumName )
                    {
                        if(m_Name == "values")
                        {
                            m_MetaVariable = m_FrontCallNode.m_MetaEnum.metaVariable;
                            if( m_MetaVariable == null )
                            {
                                m_FrontCallNode.m_MetaEnum.CreateValues();
                                m_MetaVariable = m_FrontCallNode.m_MetaEnum.metaVariable;
                                if (m_MetaVariable == null)
                                {
                                    return false;
                                }
                            }
                            m_CallNodeType = ECallNodeType.EnumValueArray;
                        }
                        else
                        {
                            MetaMemberEnum mme = m_FrontCallNode.m_MetaEnum.GetMemberEnumByName(m_Name);
                            if (mme != null)
                            {
                                if (m_IsFunction)// Enum e = Enum.MetaVaraible( 2 )
                                {
                                    Log.AddInStructMeta(EError.None, "不能使用Enum.metaVariable(2) 这样的格式!");
                                    return false;
                                }
                                else
                                {
                                    m_MetaVariable = mme;
                                    m_CallNodeType = ECallNodeType.EnumDefaultValue;
                                }
                            }
                            else
                            {
                                Log.AddInStructMeta(EError.None, "Error 不能使用Enum.xxxx未发现后续!");
                                return false;
                            }
                        }
                    }
                    else if (frontCNT == ECallNodeType.FunctionInnerVariableName
                        || frontCNT == ECallNodeType.MemberVariableName 
                        || frontCNT == ECallNodeType.VisitVariable)
                    {
                        MetaBase tempMetaBase2 = null;
                        var mv = m_FrontCallNode.m_MetaVariable;
                        if( frontCNT == ECallNodeType.VisitVariable )
                        {
                            mv = (mv as MetaVisitVariable).targetMetaVariable;
                        }
                        MetaVariable getmv2 = null;
                        if ( mv.isArray )
                        {
                            if( isAt )
                            {
                                // Array1.$i.x   Array1.$mmq.x;
                                getmv2 = m_OwnerMetaFunctionBlock.GetMetaVariableByName(m_Name);
                                if (getmv2 != null)    //查找是否已定义过变量
                                {
                                    string inputMVName = "Visit_" + m_Name;
                                    m_MetaVariable = mv.GetMetaVaraible(inputMVName);
                                    if (m_MetaVariable == null)
                                    {
                                        m_MetaVariable = new MetaVisitVariable(inputMVName, m_OwnerMetaClass, m_OwnerMetaFunctionBlock,
                                            mv, getmv2);
                                        mv.AddMetaVariable(m_MetaVariable);
                                    }
                                    tempMetaBase2 = m_MetaVariable;
                                    m_CallNodeType = ECallNodeType.VisitVariable;
                                }
                            }
                            //if (mv.realMetaType.isGenTemplateClass)
                            //{
                            //    calcMetaBase = mv.realMetaType.GetMetaInputTemplateByIndex();
                            //}
                        }

                        if (tempMetaBase2 == null )
                        {
                            MetaClass mc = mv.realMetaType.metaClass;
                            if (mc is MetaData)
                            {
                                MetaData md = mc as MetaData;
                                var retmmd = GetDataValueByMetaData(md, m_Name);
                                m_MetaVariable = retmmd;
                                if (retmmd == null)
                                {
                                    Log.AddInStructMeta(EError.None, $"Error 没有找到{m_Name} 的MetaData数据!");
                                    return false;
                                }
                                if (retmmd.memberDataType == EMemberDataType.MemberClass)
                                {
                                    m_MetaClass = m_MetaVariable.realMetaType.metaClass;
                                    m_MetaVariable = retmmd;
                                    m_CallNodeType = ECallNodeType.MemberVariableName;
                                }
                                else if (retmmd.memberDataType == EMemberDataType.ConstValue)
                                {
                                    m_CallNodeType = ECallNodeType.ConstValue;
                                }
                                else if (retmmd.memberDataType == EMemberDataType.MemberArray)
                                {
                                    m_CallNodeType = ECallNodeType.MemberDataName;
                                }
                                else
                                {
                                    m_CallNodeType = ECallNodeType.MemberDataName;
                                }
                            }
                            else
                            if (mc is MetaEnum)
                            {
                                MetaEnum me = mc as MetaEnum;
                                m_MetaVariable = me.GetMemberVariableByName(m_Name);
                                m_CallNodeType = ECallNodeType.MemberVariableName;
                                m_FrontCallNode.SetStoreMetaVariable(m_MetaVariable);
                            }
                            else
                            {
                                MetaClass tmc = mv.realMetaType.eType == EMetaTypeType.TemplateClassWithTemplate ? mv.realMetaType.metaClass : mv.realMetaType.metaClass;
                                if (GetFunctionOrVariableByOwnerClass(tmc, m_Name) == false)
                                {
                                    return false;
                                }
                            }                            
                        }
                    }
                    else if (frontCNT == ECallNodeType.This
                        || frontCNT == ECallNodeType.Base
                        || frontCNT == ECallNodeType.ConstValue )
                    {
                        if(GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaClass, m_Name ) == false )
                        {
                            return false;
                        }
                    }
                    else if (frontCNT == ECallNodeType.Express)
                    {
                        if (GetFunctionOrVariableByOwnerClass(m_FrontCallNode.m_MetaClass, m_Name) == false)
                        {
                            return false;
                        }
                    }
                    else if (frontCNT == ECallNodeType.MemberFunctionName )
                    {
                        MetaFunction mf = m_FrontCallNode.m_MetaFunction;
                        MetaType retMT = mf.returnMetaVariable.realMetaType;
                        if( retMT != null && retMT.metaClass != null )
                        {
                            if( GetFunctionOrVariableByOwnerClass(retMT.metaClass, m_Name) == false )
                            {
                                return false;
                            }
                        }
                        else
                        {
                            Log.AddInStructMeta(EError.None, "Error 函数没有返回类型"); 
                        }
                    }
                    else if (frontCNT == ECallNodeType.TemplateName)
                    {
                        var mt = m_FrontCallNode.m_MetaTemplate;
                        if (mt != null)
                        {
                            if (mt.extendsMetaClass != null)
                            {
                                GetFunctionOrVariableByOwnerClass( mt.extendsMetaClass, m_Name );
                            }
                            else
                            {
                                if(m_Name == "instance" )
                                {
                                    m_MetaVariable = new MetaVariable("instance", MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaFunctionBlock,
                                        null, null);
                                    m_CallNodeType = ECallNodeType.MemberVariableName;
                                }
                                else
                                {
                                    GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.objectMetaClass, m_Name );

                                }
                            }
                        }
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "Error 暂不支持上节点的类型: " + frontCNT.ToString());
                    }
                }
            }

            //如果检查到在函数体里边的T,需要对T进行实例化，看是类的T还是模板函数的T
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

            if(this.m_FileMetaCallNode.inputTemplateNodeList.Count > 0 )
            {
                MetaMemberFunction tmf = m_OwnerMetaFunctionBlock.ownerMetaFunction as MetaMemberFunction;
                if (m_MetaClass != null || tmf != null)
                {
                    CreateMetaTemplateParams(m_MetaClass, tmf);
                }
            }
          
            if ( m_CallNodeType == ECallNodeType.ClassName )
            {
                if (m_MetaTemplateParamsList.Count > 0)
                {
                    var ngmc = m_MetaClass.AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList(m_MetaTemplateParamsList);

                    if( ngmc is MetaGenTemplateClass mgtc )
                    {
                        mgtc.Parse();
                        m_MetaClass = mgtc;
                    }

                    m_MetaType = new MetaType(m_MetaClass, m_MetaTemplateParamsList);
                }
            }             
            else if( this.m_CallNodeType == ECallNodeType.MemberFunctionName )
            {
                if( m_MetaFunction is MetaMemberFunction mmf )
                {
                    if ( mmf.isTemplateFunction )
                    {
                        MetaClass mcagm = m_MetaClass;
                        if( m_FrontCallNode !=  null )
                        {
                            if(m_FrontCallNode.m_MetaClass != null)
                            {
                                mcagm = m_FrontCallNode.m_MetaClass;
                            }
                            else if ( m_FrontCallNode.m_MetaVariable != null )
                            {
                                mcagm = m_FrontCallNode.m_MetaVariable.realMetaType.metaClass;
                            }
                        }
                        MetaGenTempalteFunction mgtfind = mmf.AddGenTemplateMemberFunctionByMetaTypeList(mcagm, m_MetaTemplateParamsList);
                        if (mgtfind != null)
                        {
                            m_MetaFunction = mgtfind;
                        }
                    }
                }
            }

            //下边的代码未重构后，未经过验证，需要验证
            if (m_IsFunction)
            {                
                if ( m_CallNodeType == ECallNodeType.MetaNode )
                {
                    Log.AddInStructMeta(EError.None, "Error 函数调用与命名空间冲突!!");
                    return false;
                }
                else if( m_CallNodeType == ECallNodeType.MemberFunctionName )
                {
                    if( this.m_DefineMetaVariable != null 
                        && m_MetaFunction.returnMetaVariable.realMetaType.metaClass != CoreMetaClassManager.voidMetaClass )
                    {
                        this.m_StoreMetaVariable = this.m_DefineMetaVariable;
                    }
                    return true;
                }
                else if( m_MetaTemplate != null )
                {
                    m_CallNodeType = ECallNodeType.NewTemplate;
                }
                else if ( m_MetaClass != null )
                {
                    MetaClass curmc = m_MetaClass;
                    if (this.m_IsArray)
                    {
                        curmc = CoreMetaClassManager.arrayMetaClass;
                    }
                    //if (curmc is MetaGenTemplateClass mgtc)
                    //{
                    //    //MetaInputTemplateCollection tmitc = m_MetaTemplateParamsCollection;
                    //    if (curmc == CoreMetaClassManager.rangeMetaClass)
                    //    {
                    //        MetaClass mc = m_MetaInputParamCollection.GetMaxLevelMetaClassType();
                    //        //if (m_MetaTemplateParamsCollection == null)
                    //        //{
                    //        //    m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                    //        //    m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(new MetaType(mc));
                    //        //    tmitc = m_MetaTemplateParamsCollection;
                    //        //}
                    //    }
                    //    else if (curmc == CoreMetaClassManager.arrayMetaClass )
                    //    {
                    //        if (m_MetaInputParamCollection == null)
                    //        {
                    //            m_MetaInputParamCollection = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaFunctionBlock);
                    //        }

                    //        //if (tmitc == null)
                    //        //{
                    //        //    tmitc = new MetaInputTemplateCollection();
                    //        //    m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                    //        //    m_MetaBraceStatementsContent.Parse();

                    //        //    MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
                    //        //    if (m_MetaBraceStatementsContent != null)
                    //        //    {
                    //        //        MetaClass tmc = m_MetaBraceStatementsContent.GetMaxLevelMetaClassType();
                    //        //        if (tmc != CoreMetaClassManager.objectMetaClass)
                    //        //        {
                    //        //            mitp = new MetaType(tmc);
                    //        //        }
                    //        //    }
                    //        //    tmitc.AddMetaTemplateParamsList(mitp);
                    //        //}
                    //    }
                    //    MetaMemberFunction mmf = curmc.GetMetaMemberFunctionByNameAndInputTemplateInputParam("_init_", null, m_MetaInputParamCollection);
                    //    if (mmf == null)
                    //    {
                    //        Log.AddInStructMeta(EError.None, $"Error 没有找到相关的_init_类!! 类[{curmc.allClassName}] 函数:[_init_({m_MetaInputParamCollection.count} )]", m_Token);
                    //        return false;
                    //    }
                    //    m_MetaClass = curmc;
                    //    m_MetaFunction = mmf;
                    //    m_CallNodeType = ECallNodeType.NewClass;                        
                    //}
                    //else
                    {
                        //ArrClass()
                        MetaMemberFunction mmf = curmc.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, m_MetaInputParamCollection);
                        if (mmf == null)
                        {
                            Log.AddInStructMeta(EError.None, "Error 没有找到 关于类中" + curmc.allClassName + "的_init_方法!)", m_Token);
                            return false;
                        }
                        MetaType retMt = m_MetaType;
                        if( retMt != null )
                        {
                            retMt.SetTemplateMetaClass(m_MetaClass);
                        }
                        if (m_DefineMetaVariable == null)
                        {
                            m_MetaVariable = new MetaVariable("new_" + curmc.allClassName + "_" + curmc.GetHashCode(), MetaVariable.EVariableFrom.LocalStatement, m_OwnerMetaFunctionBlock, 
                                m_OwnerMetaClass, retMt);
                            m_OwnerMetaFunctionBlock.AddMetaVariable(m_MetaVariable);
                        }
                        else
                        {
                            m_MetaVariable = m_DefineMetaVariable;
                        }
                        this.m_MetaClass = curmc;
                        m_MetaFunction = mmf;
                        if( (m_CallNodeType != ECallNodeType.NewTemplate)
                            &&(m_CallNodeType != ECallNodeType.NewClass) )
                        {
                            m_CallNodeType = ECallNodeType.NewClass;
                        }
                        

                        if (m_FileMetaCallNode.fileMetaBraceTerm != null)  //可以使用  ArrClass(){ x = ??} 的方式
                        {
                            if( m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress  )
                            {
                                Log.AddInStructMeta(EError.None, "Error 在InputParam 里边，构建函数，只允许 使用ClassName() 的方式, " +
                                    "不允许使用 ClassName(){}的方式" + m_FileMetaCallNode.fileMetaBraceTerm.ToTokenString() );
                                return false;
                            }
                            m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                            m_MetaBraceStatementsContent.SetMetaType(new MetaType(curmc));
                            m_MetaBraceStatementsContent.Parse();
                        }
                    }

                    if (!m_AllowUseSettings.callFunction && m_IsFunction)
                    {
                        Log.AddInStructMeta(EError.None, "Error 当前位置不允许有函数调用方式使用!!!" + m_Token?.ToLexemeAllString());
                    }
                }
                else if( m_MetaData != null )
                {
                    m_CallNodeType = ECallNodeType.NewData;
                    if (m_FileMetaCallNode.fileMetaBraceTerm != null)  //可以使用  ArrClass(){ x = ??} 的方式
                    {
                        if (m_AllowUseSettings.parseFrom == EParseFrom.InputParamExpress)
                        {
                            Log.AddInStructMeta(EError.None, "Error 在InputParam 里边，构建函数，只允许 使用ClassName() 的方式, " +
                                "不允许使用 ClassName(){}的方式" + m_FileMetaCallNode.fileMetaBraceTerm.ToTokenString());
                            return false;
                        }
                        m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                        m_MetaBraceStatementsContent.SetMetaType(new MetaType(m_MetaData));
                        m_MetaBraceStatementsContent.Parse();
                    }
                }
                else if( m_MetaEnum != null )
                {

                }
                else if( m_MetaFunction != null )
                {

                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error 使用函数调用与当前节点不吻合!!");
                    return false;
                }
            }
            else
            {
                if (m_MetaVariable != null)
                {
                    var tmv = m_MetaVariable;
                    if (m_AllowUseSettings.useNotStatic == false && m_MetaVariable.isStatic )
                    {
                        if (frontCNT == ECallNodeType.FunctionInnerVariableName
                            || frontCNT == ECallNodeType.MemberVariableName
                            || frontCNT == ECallNodeType.This
                            || frontCNT == ECallNodeType.Base)
                        {
                            Log.AddInStructMeta(EError.None, "Error 1 静态调用，不能调用非静态字段!!");
                            return false;
                        }
                    }
                    if (m_MetaVariable.isArray)             //Array<int> arr = {1,2,3}; 这个arr[0]就是mv
                    {
                        if (m_MetaArrayCallNodeList.Count == 1 && this.m_IsArray )  //arr[?]
                        {
                            MetaCallNode fmcn1 = null;// m_MetaArrayCallNodeList[0].finalMetaCallNode;
                            if (fmcn1?.callNodeType == ECallNodeType.ConstValue)       //arr[0]
                            {
                                string tname = (fmcn1.metaExpressValue as MetaConstExpressNode).value.ToString();
                                m_MetaVariable = m_MetaVariable.GetMetaVaraible(tname);
                                if (m_MetaVariable == null)
                                {
                                    m_MetaVariable = new MetaVisitVariable(tname, m_OwnerMetaClass, m_OwnerMetaFunctionBlock, m_MetaVariable, null);
                                    tmv.AddMetaVariable(m_MetaVariable);
                                }
                            }
                            else if (fmcn1?.callNodeType == ECallNodeType.FunctionInnerVariableName)    //arr[var]
                            {
                                var gmv = (fmcn1).m_MetaVariable;
                                string tname = "VarName_" + gmv.name + "_VarHashCode_" + gmv.GetHashCode().ToString();
                                m_MetaVariable = tmv.GetMetaVaraible(tname);
                                if (m_MetaVariable == null)
                                {
                                    m_MetaVariable = new MetaVisitVariable(tname, m_OwnerMetaClass, m_OwnerMetaFunctionBlock, tmv, gmv);
                                    tmv.AddMetaVariable(m_MetaVariable);
                                }
                            }
                            m_CallNodeType = ECallNodeType.VisitVariable;
                        }
                    }
                }
                else if ( m_MetaClass is MetaClass)
                {
                    if( this.m_IsArray )
                    {
                        if (m_MetaArrayCallNodeList.Count > 0)   //int[??]
                        {
                            if (m_MetaArrayCallNodeList.Count > 3)
                            {
                                Log.AddInStructMeta(EError.None, "Error 数组不能超过三维!!");
                            }
                            //m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                            //m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(new MetaType(m_MetaClass));

                            m_MetaInputParamCollection = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaFunctionBlock);

                            Token token = m_FileMetaCallNode.arrayNodeList[0].callNodeList[0].token;
                            int arr1 = int.Parse(token.lexeme.ToString());
                            int arr2 = 0;
                            int arr3 = 0;
                            int count = arr1;

                            MetaConstExpressNode mcen1 = null;
                            MetaConstExpressNode mcen2 = null;
                            MetaConstExpressNode mcen3 = null;
                            if (m_FileMetaCallNode.arrayNodeList.Count > 1 )
                            {
                                Token token2 = m_FileMetaCallNode.arrayNodeList[1].callNodeList[0].token;
                                mcen2 = new MetaConstExpressNode(token.GetEType(), token2.lexeme);
                                arr2 = int.Parse(token2.lexeme.ToString());
                                if (arr2 == 0)
                                {
                                    Log.AddInStructMeta(EError.None, "Error 数组的第二维长度应该大于0");
                                }
                                count = count * arr2;
                            }
                            if (m_FileMetaCallNode.arrayNodeList.Count > 2 )
                            {
                                Token token3 = m_FileMetaCallNode.arrayNodeList[1].callNodeList[0].token;
                                mcen3 = new MetaConstExpressNode(token.GetEType(), token.lexeme);
                                arr3 = int.Parse(token.lexeme.ToString());
                                if (arr3 == 0)
                                {
                                    Log.AddInStructMeta(EError.None, "Error 数组的第三维长度应该大于0");
                                }
                                count = count * arr3;
                            }

                            mcen1 = new MetaConstExpressNode(EType.Int32, count);
                            m_MetaInputParamCollection.AddMetaInputParam(new MetaInputParam(mcen1));

                            if (mcen2 != null)
                            {
                                m_MetaInputParamCollection.AddMetaInputParam(new MetaInputParam(mcen2));
                            }
                            if (mcen3 != null)
                            {
                                m_MetaInputParamCollection.AddMetaInputParam(new MetaInputParam(mcen3));
                            }
                            MetaClass tmc = CoreMetaClassManager.arrayMetaClass;
                            //MetaClass templateMC = tmc.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_MetaTemplateParamsCollection);
                            //if (templateMC != null)
                            //{
                            //    MetaMemberFunction mmf = templateMC.GetMetaMemberFunctionByNameAndInputParamCollect("_init_", m_MetaInputParamCollection);
                            //    if (mmf != null)
                            //    {
                            //        m_MetaFunction = mmf;
                            //        m_MetaClass = templateMC;
                            //        m_CallNodeType = ECallNodeType.NewClass;

                            //        if (m_FileMetaCallNode.fileMetaBraceTerm != null)
                            //        {
                            //            m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                            //            m_MetaBraceStatementsContent.Parse();
                            //        }

                            //    }
                            //}
                            //else
                            //{
                            //    Debug.WriteLine("Error 没有找到模版!!");
                            //}
                        }
                    }
                }
                else if( m_MetaData != null )
                {

                }
                else if( m_MetaFunction != null )
                {

                }
                else if(m_MetaEnum != null )
                {

                }
                else if( m_MetaNode != null )
                {

                }
                else if ( m_MetaTemplate != null )
                {
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error !! 非函数类型!!" + m_FileMetaCallNode.token.ToLexemeAllString());
                }
            }
            return true;
        }
        public bool GetFirstNode(string inputname, MetaClass mc, int count )
        {

            if( m_AllowUseSettings.parseFrom == EParseFrom.MemberVariableExpress )
            {
            }
            MetaNode retMC = null;
            // 查找定义关键字的class => range   array
            if (m_Token.extend != null)
            {                
                EType etype = EType.None;
                if (Enum.TryParse<EType>(m_Token.extend.ToString(), out etype))
                {
                    var retMC2 = CoreMetaClassManager.GetMetaClassByEType(etype);
                    if ( retMC2 != null )
                    {
                        retMC = retMC2.metaNode;
                    }
                }
            }
            //查找类模型
            if( retMC == null)
            {
                var t = mc.GetMetaTemplateByName(inputname);
                if ( t != null )
                {
                    m_MetaTemplate = t;
                    m_CallNodeType = ECallNodeType.TemplateName;
                    return true;
                }
            }
            //通过fileMeta查找是否有首定义字符
            if (retMC == null)
            {
                retMC = ClassManager.instance.GetMetaClassByNameAndFileMeta(m_OwnerMetaClass, m_FileMetaCallNode.fileMeta, new List<string>(1) { inputname });
            }
            //查找父类或子类中包含的节点
            if ( retMC == null )
            {
                retMC = mc.metaNode.GetChildrenMetaNodeByName(inputname);
            }
            if (retMC != null)
            {
                if( retMC.isMetaModule || retMC.isMetaNamespace )
                {
                    m_MetaNode = retMC;
                    m_CallNodeType = ECallNodeType.MetaNode;
                }
                else if (retMC.isMetaData)
                {
                    m_MetaData = retMC.metaData;
                    m_CallNodeType = ECallNodeType.DataName;
                }
                else if (retMC.isMetaEnum)
                {
                    m_MetaEnum = retMC.metaEnum;
                    m_MetaVariable = m_MetaEnum.metaVariable;
                    m_CallNodeType = ECallNodeType.EnumName;
                }
                else if (retMC.IsMetaClass() )
                {
                    m_MetaClass = retMC.GetMetaClassByTemplateCount(count);
                    m_CallNodeType = ECallNodeType.ClassName;
                    if( m_MetaClass == null )
                    {
                        Log.AddInStructMeta(EError.None, $"找到{retMC.allName} 里边模板数据为{count} 没有找到相关的类!");
                        return false;
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error 没有发该RetMC的类别MetaCommon");
                }
            }
            else
            {
                var mmv = mc.GetMetaMemberVariableByName(inputname);
                if( mmv != null )
                {
                    if( mmv.isStatic )
                    {
                        m_MetaVariable = mmv;
                        m_MetaClass = mc;
                        m_MetaType = mmv.realMetaType;
                        List<MetaType> mtList = new List<MetaType>();
                        for( int i = 0; i < mmv.ownerMetaClass.metaTemplateList.Count; i++ )
                        {
                            mtList.Add(new MetaType(mmv.ownerMetaClass.metaTemplateList[i]));
                        }
                        m_CallMetaType = new MetaType(mmv.ownerMetaClass, mtList);
                        m_CallNodeType = ECallNodeType.MemberVariableName;
                        return true;
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "第一位的成员变量名称必须是个静态变量才可以哇!");
                        return false;
                    }
                }
                var mmf = mc.GetFirstMetaMemberFunctionByName(inputname);
                if( mmf != null )
                {
                    if ( mmf.isStatic )
                    {
                        m_MetaFunction = mmf;
                        m_MetaClass = mc; List<MetaType> mtList = new List<MetaType>();
                        for (int i = 0; i < mmv.ownerMetaClass.metaTemplateList.Count; i++)
                        {
                            mtList.Add(new MetaType(mmv.ownerMetaClass.metaTemplateList[i]));
                        }
                        m_CallMetaType = new MetaType(mmv.ownerMetaClass, mtList);
                        m_CallNodeType = ECallNodeType.MemberFunctionName;
                        return true;
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "第一位的成员函数名称必须是个静态函数才可以哇!");
                        return false;
                    }
                }
            }

            //函数内成员
            if ( retMC == null )
            {
                MetaVariable mv = m_OwnerMetaFunctionBlock?.GetMetaVariableByName(inputname);
                if(mv != null )
                {
                    m_MetaVariable = mv;
                    m_CallNodeType = ECallNodeType.FunctionInnerVariableName;
                }
                var ownerFun = m_OwnerMetaFunctionBlock?.ownerMetaFunction;
                if (ownerFun != null)
                {
                    //函数的参数是否是模版，如果是，则返回
                    var metaDefineParam = ownerFun.GetMetaDefineParamByName(inputname);
                    if (metaDefineParam != null)
                    {
                        if( metaDefineParam.metaVariable != null )
                        {
                            m_MetaVariable = mv;
                            m_CallNodeType = ECallNodeType.FunctionInnerVariableName;
                        }
                    }
                }
            }
            return true;
        }
        public MetaBase HandleCastFunction(MetaClass mc)
        {
            //if (m_MetaTemplateParamsCollection == null)
            //{
            //    Debug.WriteLine("Error 没有Cast<>的使用，请正确使用Cast!!");
            //    return null;
            //}
            //else
            //{
            //    if (m_MetaTemplateParamsCollection.metaTemplateParamsList.Count != 1)
            //    {
            //        Debug.WriteLine("Error 没有Cast<ClassName>()的使用，请正确使用Cast!!");
            //        return null;
            //    }
            //    MetaClass castClass = m_MetaTemplateParamsCollection.metaTemplateParamsList[0].metaClass;
            //    if (castClass == null)
            //    {
            //        Debug.WriteLine("Error 没有Cast<ClassName>()的使用，没有找到Cast<ClassName> 中的ClassName");
            //        return null;
            //    }
            //    //MetaFunction mf2 = mc.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect("Cast", m_MetaTemplateParamsCollection, m_MetaInputParamCollection);
            //    //if (mf2 != null)
            //    //{
            //    //    return mf2;
            //    //}
            //    //else
            //    //{
            //    //    Debug.Write("没有找到Cast的函数，或者是重定义错误!!");
            //    //    return null;
            //    //}
            //}
            return null;
        }
        public MetaMemberData GetDataValueByMetaData(MetaData md, string inputName)
        {
            return md.GetMemberDataByName(inputName);
        }
        public MetaMemberData GetDataValueByMetaMemberData(MetaMemberData md, string inputName)
        {
            return md.GetMemberDataByName(inputName);
        }
        public bool CreateMetaTemplateParams( MetaClass mc, MetaMemberFunction mmf )
        {
            for (int i = 0; i < this.m_FileMetaCallNode.inputTemplateNodeList.Count; i++)
            {
                var itnlc = this.m_FileMetaCallNode.inputTemplateNodeList[i];
                var ct = TypeManager.instance.RegisterTemplateDefineMetaTemplateFunction(m_OwnerMetaClass, mc, mmf, itnlc, true);
                if (ct != null)
                {
                    m_MetaTemplateParamsList.Add(ct);
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "没有发现实体的模板类!!" + m_MetaClass?.name);
                    return false;
                }
            }
            return true;
        }
        public bool GetFunctionOrVariableByOwnerClass( MetaClass mc, string inputname )
        {
            MetaMemberVariable mmv = null;
            MetaMemberFunction mmf = null;
            if (m_IsFunction)
            {
                if (inputname == "cast")
                {
                    HandleCastFunction(mc);
                }

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
                if (mmv == null)
                {
                    mmf = mc.GetMetaDefineGetSetMemberFunctionByName(inputname, m_MetaInputParamCollection,
                        m_AllowUseSettings.getterFunction,
                        m_AllowUseSettings.setterFunction);
                }
            }

            if( mmv == null && mmf == null )
            {
                Log.AddInStructMeta(EError.None, "Error 设置notStatic时，没有找到相应的变量!" + m_Token?.ToLexemeAllString());
                return false;
            }

            if( mmv != null )
            {
                m_MetaVariable = mmv;
                m_MetaType = mmv.realMetaType;
                m_CallMetaType = new MetaType(mmv.ownerMetaClass);
                m_CallNodeType = ECallNodeType.MemberVariableName;
                //var gmmv3 = (mv as MetaIteratorVariable);
                //if (gmmv3 != null)
                //{
                //    tempMetaBase2 = gmmv3.GetMetaVaraible(m_Name);
                //    if (tempMetaBase2 != null)
                //    {
                //        m_MetaVariable = tempMetaBase2 as MetaVariable;
                //    }
                //}
            }
            else if( mmf != null )
            {
                m_MetaFunction = mmf;
                m_MetaType = mmf.returnMetaVariable.realMetaType;
                m_CallMetaType = new MetaType(mmf.ownerMetaClass);
                m_CallNodeType = ECallNodeType.MemberFunctionName;
                //this.m_GenMetaClass = m_FrontCallNode.m_GenMetaClass;
            }
            return true;
        }
        //public MetaBase GetCSharpFunctionOrVariableByOwnerClass(MetaClass mc, string inputname, bool isStatic)
        //{
        //    if (m_IsFunction)
        //    {
        //        return mc.GetCSharpMemberFunctionAndCreateByNameAndInputParamCollect(inputname, isStatic, m_MetaInputParamCollection);
        //    }
        //    else
        //    {
        //        var gmb = mc.GetCSharpMemberVariableAndCreateByName(inputname);
        //        if (gmb == null)
        //        {
        //            return mc.GetMetaDefineGetSetMemberFunctionByName(inputname, m_AllowUseSettings.getterFunction,
        //                m_AllowUseSettings.setterFunction);
        //        }
        //        else
        //        {
        //            if (gmb is MetaMemberFunction)
        //            {
        //                m_IsFunction = true;
        //            }
        //        }
        //        return gmb;
        //    }
        //}
        public override string ToString()
        {
            return ToFormatString();
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
                        sb.Append(m_MetaClass?.allClassName);
                    else
                        sb.Append(m_MetaClass?.name);
                }
                else if (m_CallNodeType == ECallNodeType.EnumName)
                {
                    sb.Append(m_MetaEnum.allClassName );
                }
                else if (m_CallNodeType == ECallNodeType.EnumDefaultValue)
                {
                    sb.Append(m_MetaVariable?.name);
                }
                else if (m_CallNodeType == ECallNodeType.DataName)
                {
                    sb.Append(m_MetaData.allClassName );
                }
                else if (m_CallNodeType == ECallNodeType.MemberDataName)
                {
                    sb.Append(m_MetaVariable?.name);
                }
                else if (m_CallNodeType == ECallNodeType.NewClass)
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
                else if( m_CallNodeType == ECallNodeType.MetaType )
                {
                    sb.Append(m_MetaType.ToString());
                }
                else if (m_CallNodeType == ECallNodeType.ConstValue)
                {
                    sb.Append(m_ExpressNode.ToString());
                }
                else
                {
                    //sb.Append("Error 解析Token错误" + token?.ToLexemeAllString());
                    sb.Append(m_Token?.lexeme.ToString() + "Error(CurrentMetaBase is Null!)");
                }
            }
            return sb.ToString();
        }
    }
}
