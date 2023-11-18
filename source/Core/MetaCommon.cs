using Simple.CC.C2;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static Simple.CC.C2.MC2;
using static SimpleLanguage.Core.MetaNewObjectExpressNode;

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
        TemplateName,
        NamespaceName,
        ExternalNamespaceName,
        ClassName,
        ExternalClassName,
        EnumName,
        EnumDefaultValue,
        EnumNewValue,
        DataName,
        DataValue,
        VariableName,
        VisitVariable,
        IteratorVariable,
        MemberVariableName,
        MemberDataName,
        NewClass,
        FunctionName,
        ConstValue,
        This,
        Base,
        Global,
        Express
    }
    public class MetaCallNode
    {
        public MetaClass ownerClass
        {
            get
            {
                if (m_OwnerMetaClass != null)
                    return m_OwnerMetaClass;
                return m_OwnerMetaFunctionBlock?.ownerMetaClass;
            }
        }

        public string name;

        // 当前是否是第一个元素
        public bool isFirst { get; set; } = false;
        // 第一位是否只能使用this. base.的方式
        public bool isFirstPosMustUseThisBaseOrStaticClassName { get; set; } = false;
        public ECallNodeType callNodeType => m_CallNodeType;
        public MetaConstExpressNode constValue => m_ExpressNode as MetaConstExpressNode;
        public MetaExpressNode metaExpressValue => m_ExpressNode;
        public MetaInputTemplateCollection metaTemplateParamsCollection => m_MetaTemplateParamsCollection;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent => m_MetaBraceStatementsContent;

        private AllowUseConst m_AllowUseConst;
        private ECallNodeType m_CallNodeType;
        private ECallNodeSign m_CallNodeSign = ECallNodeSign.Null;
        public bool m_IsArray = false;
        public bool m_IsFunction = false;

        private MetaCallNode m_FrontCallNode = null;
        private FileMetaCallNode m_FileMetaCallSign = null;
        private FileMetaCallNode m_FileMetaCallNode = null;
        private Token m_Token = null;

        private MetaBlockStatements m_OwnerMetaFunctionBlock { get; set; } = null;
        private MetaClass m_OwnerMetaClass { get; set; } = null;
        private MetaCallLink m_ParentMetaCallLink { get; set; } = null;
        public MetaMemberData m_MetaMemberData { get; set; } = null;
        public MetaModule m_MetaMoule { get; set; } = null;
        public MetaNamespace m_MetaNamespace { get; set; } = null;
        public MetaEnum m_MetaEnum { get; set; } = null;
        public MetaClass m_MetaClass { get; set; } = null;
        public MetaVariable m_MetaVariable { get; set; } = null;
        public MetaFunction m_MetaFunction { get; set; } = null;
        public MetaInputParamCollection m_MetaInputParamCollection { get; set; } = null;
        public MetaInputTemplateCollection m_MetaTemplateParamsCollection { get; set; } = null;
        public MetaBraceOrBracketStatementsContent m_MetaBraceStatementsContent = null;
        public List<MetaCallLink> m_MetaArrayCallNodeList = new List<MetaCallLink>();
        public MetaExpressNode m_ExpressNode { get; set; } = null;    // a+b+([expressNode[3+20+10.0f]).ToString() 中的3+20+10.f就是表示式 , Enum.Value(expressNode) , fun(expressNode)

        protected MetaCallNode()
        { }
        public MetaCallNode(FileMetaCallNode fmcn1, FileMetaCallNode fmcn2, MetaClass mc, MetaBlockStatements mbs, MetaCallLink mcl)
        {
            m_FileMetaCallSign = fmcn1;
            m_FileMetaCallNode = fmcn2;
            m_Token = m_FileMetaCallNode.token;
            m_OwnerMetaClass = mc;
            m_OwnerMetaFunctionBlock = mbs;
            m_ParentMetaCallLink = mcl;
            m_IsFunction = m_FileMetaCallNode.isCallFunction;

            m_IsArray = m_FileMetaCallNode.isArray;
            for (int i = 0; i < m_FileMetaCallNode.arrayNodeList.Count; i++)
            {
                MetaCallLink cmcl = new MetaCallLink(m_FileMetaCallNode.arrayNodeList[i], mc, mbs);
                m_MetaArrayCallNodeList.Add(cmcl);
            }

            if (m_FileMetaCallNode.inputTemplateNodeList.Count > 0)
            {
                m_MetaTemplateParamsCollection = new MetaInputTemplateCollection(m_FileMetaCallNode.inputTemplateNodeList, mc);
            }
            if (m_FileMetaCallNode.fileMetaBraceTerm != null)
            {
                m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
            }
        }
        public void SetFrontCallNode(MetaCallNode mcn)
        {
            m_FrontCallNode = mcn;
        }
        public bool ParseNode(AllowUseConst _auc)
        {
            m_AllowUseConst = _auc;
            if (m_FileMetaCallSign != null)
            {
                if (m_FileMetaCallSign.token.type == ETokenType.Period)
                {
                    m_CallNodeSign = ECallNodeSign.Period;
                }
                else if (m_FileMetaCallSign.token.type == ETokenType.And)
                {
                    m_CallNodeSign = ECallNodeSign.Pointer;
                    Console.WriteLine("Error MetaStatements Parse  不允许使用其它连接符!!");
                    return false;
                }
                else
                {
                    Console.WriteLine("Error MetaStatements Parse  不允许使用其它连接符!!");
                    return false;
                }
            }
            if (m_FileMetaCallNode == null)
            {
                Console.WriteLine("Error 定义原数据为空!! " + m_Token.ToLexemeAllString());
            }
            if (m_FileMetaCallNode.fileMetaParTerm != null && !m_IsFunction )
            {
                var firstNode = m_FileMetaCallNode.fileMetaParTerm.fileMetaExpressList[0];
                if (firstNode == null)
                {
                    Console.WriteLine("Error 不能使用输入()中的内容 0号位的没有内容!!");
                }
                else
                {
                    m_ExpressNode = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(m_OwnerMetaFunctionBlock,
                        null, firstNode);
                    m_ExpressNode.CalcReturnType();
                    m_MetaClass = CoreMetaClassManager.GetMetaClassByEType(m_ExpressNode.eType);
                    m_CallNodeType = ECallNodeType.Express;
                    return true;
                }
            }
            if (m_FrontCallNode == null && isFirst == false)
            {
                Console.WriteLine("Error 非第一位未发现前边的Node!!!");
                return false;
            }
            return CreateCallNode();
        }
        bool CreateCallNode()
        {
            name = m_FileMetaCallNode.name;
            string fatherName = m_FrontCallNode?.name;
            bool isAt = m_FileMetaCallNode.atToken != null;

            ETokenType etype = m_Token.type;

            MetaBase frontMetaBase = null;
            ECallNodeType frontCNT = ECallNodeType.Null;

            for (int i = 0; i < this.m_MetaArrayCallNodeList.Count; i++)
            {
                m_MetaArrayCallNodeList[i].Parse(m_AllowUseConst);
            }

            if( m_IsFunction )
            {
                m_MetaInputParamCollection = new MetaInputParamCollection(m_FileMetaCallNode.fileMetaParTerm, ownerClass, m_OwnerMetaFunctionBlock);

                m_MetaInputParamCollection?.CaleReturnType();
            }

            if (m_FrontCallNode != null)
            {
                frontCNT = m_FrontCallNode.m_CallNodeType;
            }
            if (!isFirst && frontCNT == ECallNodeType.Null )
            {
                Console.WriteLine("Error 前边节点没有发现MetaBase!!");
                return false;
            }

            if (etype == ETokenType.Number || etype == ETokenType.String || etype == ETokenType.Boolean)
            {
                bool isNotConstValue = false;
                if (frontCNT == ECallNodeType.VariableName
                    || frontCNT == ECallNodeType.MemberVariableName
                    || frontCNT == ECallNodeType.VisitVariable )
                {
                    MetaVariable mv = frontMetaBase as MetaVariable;
                    if (mv.isArray)   //Array.
                    {
                        // Array1.$0.x   Array1.1.x;
                        if (isAt)                  //Array.$
                        {
                            string inputMVName = "Visit_" + name;
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
                            Console.WriteLine("Error 在Array.后边如果使用变量或者是数字常量，必须使用Array.$方式!!");
                        }
                    }
                }
                //不是常量值 
                if (!isNotConstValue)
                {
                    m_CallNodeType = ECallNodeType.ConstValue;
                    m_ExpressNode = new MetaConstExpressNode( m_Token.GetEType(), m_Token.lexeme );
                    m_MetaClass = CoreMetaClassManager.GetMetaClassByEType(m_Token.GetEType());
                }
            }
            else if( etype == ETokenType.Global )
            {
                if (isFirst)
                {
                    if (m_IsFunction)
                    {
                        Console.WriteLine("Error 不允许global的函数形式!!");
                    }
                    else
                    {
                        m_CallNodeType = ECallNodeType.Global;
                    }
                }
                else
                {
                    Console.WriteLine("Error 只有第一位置可以使用This关键字" + m_Token.ToLexemeAllString());
                }
            }
            else if (etype == ETokenType.This)
            {   
                //this.普通的函数，变量，get/set方法
                if (isFirst)
                {
                    if (m_IsFunction)
                    {
                        Console.WriteLine("Error 不允许this的函数形式!!");
                    }
                    else
                    {
                        m_MetaClass = ownerClass;
                        m_MetaVariable = (m_OwnerMetaFunctionBlock.ownerMetaFunction as MetaMemberFunction).thisMetaVariable;
                        m_CallNodeType = ECallNodeType.This;
                    }
                }
                else
                {
                    Console.WriteLine("Error 只有第一位置可以使用This关键字" + m_Token.ToLexemeAllString());
                }
                if (!m_AllowUseConst.useNotStatic)
                {
                    Console.WriteLine("Error 表达式中,限制使用非静态字段，不允许使用this字段，而直接使用类名称!!" + m_Token.ToLexemeAllString());
                }
                if (!m_AllowUseConst.useThis)
                {
                    Console.WriteLine("Error 不允许使用this语法" + m_Token.ToLexemeAllString());
                }
            }
            else if (etype == ETokenType.Base)
            {
                MetaClass parentClass = ownerClass.parentNode as MetaClass;
                if ( parentClass == null )
                {
                    Console.WriteLine("Error 使用base没有找到父节点!!");
                    return false;
                }

                if (isFirst)
                {
                    if (m_IsFunction)
                    {
                        Console.WriteLine("Error 不允许base的函数形式!!");
                    }
                    else
                    {
                        m_MetaClass = parentClass;
                        m_CallNodeType = ECallNodeType.Base;
                    }
                }
                else
                {
                    Console.WriteLine("Error 只有第一位置可以使用base关键字" + m_Token.ToLexemeAllString());
                }
                if (!m_AllowUseConst.useNotStatic)
                {
                    Console.WriteLine("Error 静态类中，不允许使用this字段，而直接使用类名称!!" + m_Token.ToLexemeAllString());
                }
                if (!m_AllowUseConst.useThis)
                {
                    Console.WriteLine("Error 不允许使用this语法" + m_Token.ToLexemeAllString());
                }
            }
            else if (etype == ETokenType.Type)
            {
                var selfClass = CoreMetaClassManager.GetCoreMetaClass(name);
                if (selfClass != null)
                {
                    m_MetaClass = selfClass;
                }
            }
            else if (etype == ETokenType.Identifier)
            {
                MetaBase tempMetaBase = null;
                if (isFirst)
                {
                    // Class1. ns. Int32[]
                    m_MetaClass = GetFirstNode(name, ownerClass) as MetaClass;
                }
                else
                {
                    if (frontCNT == ECallNodeType.NamespaceName)
                    {
                        tempMetaBase = frontMetaBase.GetChildrenMetaBaseByName(name) as MetaNamespace;
                    }
                    else if( frontCNT == ECallNodeType.ExternalNamespaceName )
                    {
                        var mn = frontMetaBase as MetaNamespace;
                        var mm = frontMetaBase as MetaModule;
                        if (mn != null)
                        {
                            tempMetaBase = mn.GetCSharpMetaClassOrNamespaceAndCreateByName(name);
                        }
                        else if (mm != null)
                        {
                            tempMetaBase = mm.GetCSharpMetaClassOrNamespaceAndCreateByName(name);
                        }
                    }
                    else if (frontCNT == ECallNodeType.ClassName)
                    {
                        // ClassName 一般使用在 Class1.静态变量，或者是静态方法的调用
                        var fMC = frontMetaBase as MetaClass;
                        tempMetaBase = fMC.GetChildrenMetaClassByName(name);  //查找子类名称
                        if (tempMetaBase == null)
                        {
                            MetaMemberVariable mmv = fMC.GetMetaMemberVariableByName(name);  //查找静态变量
                            if(mmv != null )
                            {
                                if( !mmv.isStatic )
                                {
                                    Console.WriteLine("Error 调用非静态成员，不能使用Class.Variable的方式!");
                                    return false;
                                }
                                tempMetaBase = mmv;
                                m_MetaVariable = mmv;
                                m_CallNodeType = ECallNodeType.MemberVariableName;
                            }
                            if(tempMetaBase == null )
                            {
                                //查找静态函数
                                MetaMemberFunction mmf = fMC.GetMetaMemberFunctionByNameAndInputParamCollect(name, m_MetaInputParamCollection);
                                if (mmf != null)
                                {
                                    if( !mmf.isStatic )
                                    {
                                        Console.WriteLine("Error 调用非静态成员，不能使用Class.Variable的方式!");
                                        return false;
                                    }
                                    m_MetaFunction = mmf;
                                    //calcMetaBase = new MetaFunctionCall(fMC, mmf, m_MetaInputParamCollection);
                                }
                            }
                        }
                    }
                    else if( frontCNT == ECallNodeType.ExternalClassName )
                    {
                        MetaClass tmetaClass= frontMetaBase as MetaClass;
                        tempMetaBase = GetFunctionOrVariableByOwnerClass(tmetaClass, name, true);
                        MetaMemberFunction mmf = tempMetaBase as MetaMemberFunction;
                        if (mmf != null)
                        {
                            m_MetaFunction = mmf;
                            m_CallNodeType = ECallNodeType.FunctionName;
                        }
                    }
                    else if( frontCNT == ECallNodeType.Global )
                    {
                        m_MetaMemberData = GetDataValueByMetaData(ProjectManager.globalData, name);
                        m_CallNodeType = ECallNodeType.MemberDataName;
                    }
                    else if (frontCNT == ECallNodeType.DataName)
                    {
                        m_MetaMemberData = GetDataValueByMetaData(frontMetaBase as MetaData, name);
                        m_CallNodeType = ECallNodeType.MemberDataName;
                    }
                    else if (frontCNT == ECallNodeType.MemberDataName)
                    {
                        m_MetaMemberData = GetDataValueByMetaMemberData(frontMetaBase as MetaMemberData, name );
                        m_CallNodeType = ECallNodeType.MemberDataName;
                    }
                    else if( frontCNT == ECallNodeType.EnumName )
                    {
                        var mb = GetEnumValue(frontMetaBase as MetaEnum, name);
                        if( !m_IsFunction )
                        {
                            m_MetaEnum = mb as MetaEnum;
                            m_CallNodeType = ECallNodeType.EnumDefaultValue;
                        }
                    }
                    else if (frontCNT == ECallNodeType.VariableName
                        || frontCNT == ECallNodeType.MemberVariableName
                        || frontCNT == ECallNodeType.VisitVariable )
                    {
                        MetaBase tempMetaBase2 = null;
                        var mv = frontMetaBase as MetaVariable;
                        MetaVariable getmv2 = null;
                        if ( mv.isArray )
                        {
                            if( isAt )
                            {
                                // Array1.$i.x   Array1.$mmq.x;
                                getmv2 = m_OwnerMetaFunctionBlock.GetMetaVariableByName(name);
                                if (getmv2 != null)    //查找是否已定义过变量
                                {
                                    string inputMVName = "Visit_" + name;
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
                            //if (mv.metaDefineType.isGenTemplateClass)
                            //{
                            //    calcMetaBase = mv.metaDefineType.GetMetaInputTemplateByIndex();
                            //}
                        }

                        if(tempMetaBase2 == null )
                        {
                            var mc = mv.metaDefineType.metaClass;
                            if (mc is MetaData)
                            {
                                MetaData md = mc as MetaData;
                                m_MetaMemberData = GetDataValueByMetaData(md, name);
                                m_CallNodeType = ECallNodeType.MemberDataName;
                            }
                            else if (mc is MetaEnum)
                            {
                                MetaEnum me = mc as MetaEnum;
                                //m_MetaEnum = GetFunctionOrVariableByOwnerClass(me, name);
                                //m_CallNodeType = ECallNodeType.DataMemberName;
                            }
                            else
                            {
                                var gett2 = GetFunctionOrVariableByOwnerClass(mv.metaDefineType.metaClass, name, false);
                               
                                if (tempMetaBase2 == null)
                                {
                                    MetaMemberFunction mmf = gett2 as MetaMemberFunction;
                                    if (mmf != null )
                                    {
                                        if( m_MetaInputParamCollection == null )
                                        {
                                            m_MetaInputParamCollection = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaFunctionBlock);
                                        }
                                        m_MetaFunction = mmf;
                                        m_CallNodeType = ECallNodeType.FunctionName;
                                    }

                                    if(tempMetaBase2 == null )
                                    {
                                        MetaMemberVariable mmv = (gett2 as MetaMemberVariable);
                                        if( mmv != null)
                                        {
                                            tempMetaBase2 = new MetaVisitVariable(mv, mmv);
                                        }
                                    }
                                    if (tempMetaBase2 == null)
                                    {
                                        var gmmv3 = (mv as MetaIteratorVariable);
                                        if (gmmv3 != null)
                                        {
                                            tempMetaBase2 = gmmv3.GetMetaVaraible(name);
                                            if (tempMetaBase2 != null)
                                            {
                                                m_MetaVariable = tempMetaBase2 as MetaVariable;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (frontCNT == ECallNodeType.This)
                    {
                        var aa = GetFunctionOrVariableByOwnerClass( m_FrontCallNode.m_MetaClass, name, false);
                        if( aa != null )
                        {
                            if( m_IsFunction )
                            {
                                m_CallNodeType = ECallNodeType.FunctionName;
                                m_MetaFunction = aa as MetaFunction;
                            }
                            else
                            {
                                m_CallNodeType = ECallNodeType.MemberVariableName;
                                m_MetaVariable = aa as MetaVariable;
                            }
                        }
                    }
                    else if (frontCNT == ECallNodeType.Base)
                    {
                        var mmv = frontMetaBase as MetaClass; 
                        var aa = GetFunctionOrVariableByOwnerClass(mmv, name, false);
                        if (aa != null)
                        {
                            if (m_IsFunction)
                            {
                                m_MetaFunction = aa as MetaFunction;
                            }
                            else
                            {
                                m_MetaVariable = aa as MetaVariable;
                            }
                        }
                    }
                    else if (frontCNT == ECallNodeType.FunctionName)
                    {
                        MetaType retMT = m_MetaFunction.returnMetaVariable.metaDefineType;
                        if( retMT != null && retMT.metaClass != null )
                        {
                            var aa = GetFunctionOrVariableByOwnerClass(retMT.metaClass, name, false);
                            if (aa != null)
                            {
                                if (m_IsFunction)
                                {
                                    m_MetaFunction = aa as MetaFunction;
                                }
                                else
                                {
                                    m_MetaVariable = aa as MetaVariable;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error 函数没有返回类型"); 
                        }
                    }
                    else if (frontCNT == ECallNodeType.ConstValue)
                    {
                        var mmv = frontMetaBase as MetaClass;
                        tempMetaBase = GetFunctionOrVariableByOwnerClass(mmv, name, false);
                    }
                    else if( frontCNT == ECallNodeType.Express )
                    {
                        var mmv = frontMetaBase as MetaClass;
                        tempMetaBase = GetFunctionOrVariableByOwnerClass(mmv, name, false);
                    }
                    else if (frontCNT == ECallNodeType.TemplateName)
                    {
                        var mt = frontMetaBase as MetaTemplate;
                        if (mt != null)
                        {
                            if (mt.constraintMetaClassList.Count > 0)
                            {
                                for (int i = 0; i < mt.constraintMetaClassList.Count; i++)
                                {
                                    var constraintClass = mt.constraintMetaClassList[i];
                                    tempMetaBase = GetFunctionOrVariableByOwnerClass(constraintClass, name, false);
                                    if (tempMetaBase != null)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                tempMetaBase = GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.objectMetaClass, name, false);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error 暂不支持上节点的类型: " + frontCNT.ToString());
                    }
                }
            }
           

            //下边的代码未重构后，未经过验证，需要验证
            if (m_IsFunction)
            {                
                if ( m_MetaNamespace != null || m_MetaMoule != null )
                {
                    Console.WriteLine("Error 函数调用与命名空间冲突!!");
                    return false;
                }
                else if ( m_MetaClass != null )
                {
                    MetaClass curmc = m_MetaClass;
                    if (this.m_IsArray)
                    {
                        curmc = CoreMetaClassManager.arrayMetaClass;
                    }
                    if (curmc.isTemplateClass)
                    {
                        MetaInputTemplateCollection tmitc = m_MetaTemplateParamsCollection;
                        if (curmc == CoreMetaClassManager.rangeMetaClass)
                        {
                            MetaClass mc = m_MetaInputParamCollection.GetMaxLevelMetaClassType();
                            if (m_MetaTemplateParamsCollection == null)
                            {
                                m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                                m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(new MetaType(mc));
                                tmitc = m_MetaTemplateParamsCollection;
                            }
                        }
                        else if (curmc == CoreMetaClassManager.arrayMetaClass)
                        {
                            if (m_MetaInputParamCollection == null)
                            {
                                m_MetaInputParamCollection = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaFunctionBlock);
                            }

                            if (tmitc == null)
                            {
                                tmitc = new MetaInputTemplateCollection();
                                m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                                m_MetaBraceStatementsContent.Parse();

                                MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
                                if (m_MetaBraceStatementsContent != null)
                                {
                                    MetaClass tmc = m_MetaBraceStatementsContent.GetMaxLevelMetaClassType();
                                    if (tmc != CoreMetaClassManager.objectMetaClass)
                                    {
                                        mitp = new MetaType(tmc);
                                    }
                                }
                                tmitc.AddMetaTemplateParamsList(mitp);
                            }
                        }
                        if (tmitc != null && tmitc.metaTemplateParamsList.Count > 0)
                        {
                            MetaGenTemplateClass mtc = curmc.GetGenTemplateMetaClass(tmitc);
                            if (mtc == null)
                            {
                                mtc = curmc.GenerateTemplateClass(tmitc);
                            }
                            if (mtc == null)
                            {
                                Console.WriteLine("Error 在MetaCommon没有找到相关的生成类");
                                return false;
                            }
                            MetaMemberFunction mmf = mtc.GetMetaMemberFunctionByNameAndInputParamCollect("__Init__", m_MetaInputParamCollection);
                            if (mmf == null)
                            {
                                Console.WriteLine("Error 没有找到相关的__Init__类!!");
                                return false;
                            }
                            m_MetaClass = mtc;
                            m_MetaFunction = mmf;
                            m_CallNodeType = ECallNodeType.NewClass;
                        }
                        else
                        {
                            Console.WriteLine("Error 没有找到相关的模版类!!");
                            return false;
                        }
                    }
                    else
                    {
                        //ArrClass()
                        MetaMemberFunction mmf = curmc.GetMetaMemberFunctionByNameAndInputParamCollect("__Init__", m_MetaInputParamCollection);
                        if (mmf == null)
                        {
                            Console.WriteLine("Error 没有找到" + curmc.allName + "的__Init__方法!)");
                            return false;
                        }
                        m_MetaClass = curmc;
                        m_MetaFunction = mmf;
                        m_CallNodeType = ECallNodeType.NewClass;

                        if (m_FileMetaCallNode.fileMetaBraceTerm != null)  //可以使用  ArrClass(){ x = ??} 的方式
                        {
                            m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                            m_MetaBraceStatementsContent.SetMetaType(new MetaType(curmc));
                            m_MetaBraceStatementsContent.Parse();
                        }
                    }

                    if (!m_AllowUseConst.callFunction && m_IsFunction)
                    {
                        Console.WriteLine("Error 当前位置不允许有函数调用方式使用!!!" + m_Token?.ToLexemeAllString());
                    }
                }
                else if ( m_MetaFunction != null )
                {
                    m_CallNodeType = ECallNodeType.FunctionName;
                    MetaMemberFunction mmf = m_MetaFunction as MetaMemberFunction;
                    if (mmf != null)
                    {
                        if (mmf.isConstructInitFunction && !m_AllowUseConst.callConstructFunction)
                        {
                            Console.WriteLine("Error 不允许使用构造函数" + m_Token.ToLexemeAllString());
                            return false;
                        }
                    }
                }
                else if( m_MetaFunction != null )
                {
                    m_CallNodeType = ECallNodeType.FunctionName;
                }
                else if(  m_MetaVariable is MetaMemberVariable )  // Enum e = Enum.MetaVaraible( 2 )
                {
                    MetaMemberVariable mmv = m_MetaVariable as MetaMemberVariable;
                    m_CallNodeType = ECallNodeType.EnumNewValue;

                    var splitParamList = m_FileMetaCallNode.fileMetaParTerm.SplitParamList();
                    if (splitParamList.Count == 1)
                    {
                        m_ExpressNode = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements( m_OwnerMetaFunctionBlock, mmv.metaDefineType, splitParamList[0].root, false, false);
                    }
                    else
                    {
                        Console.WriteLine("Error 在Enum.value中，必须权有一个值!!");
                        return false;
                    }
                    return true;
                }
                else
                {
                    Console.WriteLine("Error 使用函数调用与当前节点不吻合!!");
                    return false;
                }
            }
            else
            {
                if (m_MetaVariable != null)
                {
                    var tmv = m_MetaVariable;
                    if (m_AllowUseConst.useNotStatic == false && m_MetaVariable.isStatic == false)
                    {
                        if (frontCNT == ECallNodeType.VariableName
                            || frontCNT == ECallNodeType.MemberVariableName
                            || frontCNT == ECallNodeType.This
                            || frontCNT == ECallNodeType.Base)
                        {
                            Console.WriteLine("Error 1 静态调用，不能调用非静态字段!!");
                            return false;
                        }
                    }
                    if (m_MetaVariable.isArray)             //Array<int> arr = {1,2,3}; 这个arr[0]就是mv
                    {
                        if (m_MetaArrayCallNodeList.Count == 1 && this.m_IsArray)  //arr[?]
                        {
                            MetaCallNode fmcn1 = null;// m_MetaArrayCallNodeList[0].finalMetaCallNode;
                            if (fmcn1?.callNodeType == ECallNodeType.ConstValue)       //arr[0]
                            {
                                string tname = fmcn1.constValue.value.ToString();
                                m_MetaVariable = m_MetaVariable.GetMetaVaraible(tname);
                                if (m_MetaVariable == null)
                                {
                                    m_MetaVariable = new MetaVisitVariable(tname, m_OwnerMetaClass, m_OwnerMetaFunctionBlock, m_MetaVariable, null);
                                    tmv.AddMetaVariable(m_MetaVariable);
                                }
                            }
                            else if (fmcn1?.callNodeType == ECallNodeType.VariableName)    //arr[var]
                            {
                                //var gmv = (fmcn1).GetMetaVariable();
                                //string tname = "VarName_" + gmv.name + "_VarHashCode_" + gmv.GetHashCode().ToString();
                                //m_MetaVariable = tmv.GetMetaVaraible(tname);
                                //if (m_MetaVariable == null)
                                //{
                                //    m_MetaVariable = new MetaVisitVariable(tname, m_OwnerMetaClass, m_OwnerMetaFunctionBlock, tmv, gmv );
                                //    tmv.AddMetaVariable(m_MetaVariable);
                                //}
                            }
                            m_CallNodeType = ECallNodeType.VisitVariable;
                        }
                    }
                }
                else if ( m_MetaNamespace != null || m_MetaMoule != null )
                {
                    if(m_MetaNamespace.refFromType == RefFromType.CSharp )
                    {
                        m_CallNodeType = ECallNodeType.ExternalNamespaceName;
                    }
                    else
                    {
                        m_CallNodeType = ECallNodeType.NamespaceName;
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
                                Console.WriteLine("Error 数组不能超过三维!!");
                            }
                            m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                            m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(new MetaType(m_MetaClass));

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
                                    Console.WriteLine("Error 数组的第二维长度应该大于0");
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
                                    Console.WriteLine("Error 数组的第三维长度应该大于0");
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
                            MetaClass templateMC = tmc.GetGenTemplateMetaClassIfNotThenGenTemplateClass(m_MetaTemplateParamsCollection);
                            if (templateMC != null)
                            {
                                MetaMemberFunction mmf = templateMC.GetMetaMemberFunctionByNameAndInputParamCollect("__Init__", m_MetaInputParamCollection);
                                if (mmf != null)
                                {
                                    m_MetaFunction = mmf;
                                    m_MetaClass = templateMC;
                                    m_CallNodeType = ECallNodeType.NewClass;

                                    if (m_FileMetaCallNode.fileMetaBraceTerm != null)
                                    {
                                        m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass);
                                        m_MetaBraceStatementsContent.Parse();
                                    }

                                }
                            }
                            else
                            {
                                Console.WriteLine("Error 没有找到模版!!");
                            }
                        }
                    }
                }
                //else if (calcMetaBase is MetaTemplate)
                //{
                //    m_CallNodeType = ECallNodeType.TemplateName;
                //}
                else
                {
                    Console.WriteLine("Error !! 非函数类型!!" + m_FileMetaCallNode.token.ToLexemeAllString());
                }
            }
            return true;
        }
        public MetaBase GetFirstNode(string inputname, MetaClass mc)
        {
            MetaBase retMC = null;

            // 查找定义关键字的class => range   array
            if (m_Token.extend != null)
            {                
                EType etype = EType.None;
                if (Enum.TryParse<EType>(m_Token.extend.ToString(), out etype))
                {
                    retMC = CoreMetaClassManager.GetMetaClassByEType(etype);
                }
            }
            // 查找 coreModule的模块
            if( retMC== null )
            {
                retMC = ModuleManager.instance.GetMetaModuleByName(inputname);
            }
            //查找core中的定义
            if( retMC == null )
            {
                retMC = CoreMetaClassManager.GetCoreMetaClass(inputname);
            }
            //查找selfModule中的定义
            if (retMC == null )
            {
                retMC = ModuleManager.instance.selfModule.GetChildrenMetaBaseByName(inputname);
            }
            //查找父类或子类中包含的节点
            if( retMC == null )
            {
                retMC = mc.GetChildrenMetaClass(inputname);
            }
            //通过fileMeta查找是否有首定义字符
            if( retMC == null )
            {
                retMC = m_FileMetaCallNode.fileMeta.GetMetaBaseByName(inputname);
            }

            //函数内成员
            if ( retMC == null )
            {
                retMC = m_OwnerMetaFunctionBlock?.GetMetaVariableByName(inputname);
                //var ownerFun = m_OwnerMetaFunctionBlock?.ownerMetaFunction;
                //if (ownerFun != null)
                //{
                //    ////函数的参数是否是模版，如果是，则返回
                //    //var metaDefineParam = ownerFun.GetMetaDefineParamByName(inputname);
                //    //if (metaDefineParam != null)
                //    //{
                //    //    retMC = metaDefineParam.metaVariable;
                //    //}
                //}
            }
            if (m_IsFunction)
            {
                MetaMemberFunction mmf = mc.GetMetaMemberFunctionByNameAndInputParamCollect(inputname, m_MetaInputParamCollection);
                if( mmf != null )
                {
                    m_MetaFunction = mmf;
                }
            }
            return retMC;
        }
        public MetaBase HandleCastFunction(MetaClass mc)
        {
            if (m_MetaTemplateParamsCollection == null)
            {
                Console.WriteLine("Error 没有Cast<>的使用，请正确使用Cast!!");
                return null;
            }
            else
            {
                if (m_MetaTemplateParamsCollection.metaTemplateParamsList.Count != 1)
                {
                    Console.WriteLine("Error 没有Cast<ClassName>()的使用，请正确使用Cast!!");
                    return null;
                }
                MetaClass castClass = m_MetaTemplateParamsCollection.metaTemplateParamsList[0].metaClass;
                if (castClass == null)
                {
                    Console.WriteLine("Error 没有Cast<ClassName>()的使用，没有找到Cast<ClassName> 中的ClassName");
                    return null;
                }
                //MetaFunction mf2 = mc.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect("Cast", m_MetaTemplateParamsCollection, m_MetaInputParamCollection);
                //if (mf2 != null)
                //{
                //    return mf2;
                //}
                //else
                //{
                //    Console.WriteLine("没有找到Cast的函数，或者是重定义错误!!");
                //    return null;
                //}
            }
            return null;
        }
        public MetaBase GetEnumValue( MetaEnum me, string inputname )
        {
            return me.GetMemberVariableByName(inputname);
        }
        public MetaMemberData GetDataValueByMetaData(MetaData md, string inputName)
        {
            return md.GetMemberDataByName(inputName);
        }
        public MetaMemberData GetDataValueByMetaMemberData(MetaMemberData md, string inputName)
        {
            return md.GetMemberDataByName(inputName);
        }
        public MetaBase GetFunctionOrVariableByOwnerClass(MetaClass mc, string inputname, bool isStatic)
        {
            if (m_IsFunction)
            {
                if (inputname == "Cast")
                {
                    return HandleCastFunction(mc);
                }
                return mc.GetCSharpMemberFunctionAndCreateByNameAndInputParamCollect(inputname, isStatic, m_MetaInputParamCollection);                
            }
            else
            {
                var gmb = mc.GetCSharpMemberVariableAndCreateByName(inputname);
                if (gmb == null)
                {
                    return mc.GetMetaDefineGetSetMemberFunctionByName(inputname, m_AllowUseConst.getterFunction,
                        m_AllowUseConst.setterFunction);
                }
                else
                {
                    if( gmb is MetaMemberFunction )
                    {
                        m_IsFunction = true;
                    }
                }
                return gmb;
            }
        }

        public MetaBase GetCSharpFunctionOrVariableByOwnerClass(MetaClass mc, string inputname, bool isStatic)
        {
            if (m_IsFunction)
            {
                return mc.GetCSharpMemberFunctionAndCreateByNameAndInputParamCollect(inputname, isStatic, m_MetaInputParamCollection);
            }
            else
            {
                var gmb = mc.GetCSharpMemberVariableAndCreateByName(inputname);
                if (gmb == null)
                {
                    return mc.GetMetaDefineGetSetMemberFunctionByName(inputname, m_AllowUseConst.getterFunction,
                        m_AllowUseConst.setterFunction);
                }
                else
                {
                    if (gmb is MetaMemberFunction)
                    {
                        m_IsFunction = true;
                    }
                }
                return gmb;
            }
        }
        //public MetaType GetMetaDefineType()
        //{
        //    //if (m_CallNodeType == ECallNodeType.Express
        //    //    || m_CallNodeType == ECallNodeType.EnumNewValue
        //    //    || m_CallNodeType == ECallNodeType.ConstValue )
        //    //{
        //    //    return m_ExpressNode.GetReturnMetaDefineType();
        //    //}
        //    //if (m_CurrentMetaBase == null) return null;

        //    //if( m_CallNodeType == ECallNodeType.NewClass )
        //    //{
        //    //    return new MetaType(m_MetaClass);
        //    //}
        //    //else if (m_CallNodeType == ECallNodeType.ClassName || m_CallNodeType == ECallNodeType.This )
        //    //{
        //    //    return new MetaType(m_CurrentMetaBase as MetaClass);
        //    //}
        //    //else if (m_CallNodeType == ECallNodeType.FunctionName)
        //    //{
        //    //    var mmf = (m_CurrentMetaBase as MetaMemberFunction);
        //    //    if (mmf == null) return null;

        //    //    if (mmf.isCastFunction)
        //    //    {
        //    //        //if (!mmf.isOverrideFunction)
        //    //        //{
        //    //        //    return m_MetaTemplateParamsCollection.metaTemplateParamsList[0].inputMetaClass;
        //    //        //}
        //    //    }
        //    //    //if (mmf.isConstructInitFunction)
        //    //    //{
        //    //    //    return mmf.ownerMetaClass;
        //    //    //}
        //    //    return mmf.metaDefineType;
        //    //}
        //    //else if (m_CallNodeType == ECallNodeType.VariableName
        //    //    || m_CallNodeType == ECallNodeType.VisitVariable )
        //    //{
        //    //    MetaVariable mv = m_CurrentMetaBase as MetaVariable;
        //    //    if (mv != null)
        //    //    {
        //    //        return mv.metaDefineType;
        //    //    }
        //    //}
        //    //else if( m_CallNodeType == ECallNodeType.DataName )
        //    //{
        //    //    MetaData mv = m_CurrentMetaBase as MetaData;
        //    //    if (mv != null)
        //    //    {
        //    //        return new MetaType( mv );
        //    //    }
        //    //}
        //    //else if (m_CallNodeType == ECallNodeType.MemberVariableName)
        //    //{
        //    //    MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
        //    //    if (mmv != null)
        //    //    {
        //    //        return mmv.metaDefineType;
        //    //    }
        //    //}
        //    //else if( m_CallNodeType == ECallNodeType.EnumDefaultValue
        //    //    || m_CallNodeType == ECallNodeType.EnumNewValue )
        //    //{
        //    //    MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
        //    //    if( mmv != null )
        //    //    {
        //    //        return mmv.metaDefineType;
        //    //    }
        //    //}
        //    return null;
        //}
        //public MetaClass GetMetaClass()
        //{
        //    if( m_CallNodeType == ECallNodeType.Express )
        //    {
        //        return m_ExpressNode.GetReturnMetaClass();
        //    }
        //    //if (m_CurrentMetaBase == null) return null;

        //    //if(m_CallNodeType == ECallNodeType.ClassName
        //    //    || m_CallNodeType == ECallNodeType.ExternalClassName
        //    //    || m_CallNodeType == ECallNodeType.This
        //    //    || m_CallNodeType == ECallNodeType.ConstValue )
        //    //{
        //    //    return m_CurrentMetaBase as MetaClass;
        //    //}
        //    //else if( m_CallNodeType == ECallNodeType.FunctionName )
        //    //{
        //    //    var mmf = (m_MetaFunction);
        //    //    if (mmf == null) return null;

        //    //    //return mmf.GetMetaClass();
        //    //}
        //    //else if( m_CallNodeType == ECallNodeType.NewClass )
        //    //{
        //    //    return m_MetaClass;
        //    //}
        //    //else if (m_CallNodeType == ECallNodeType.VariableName)
        //    //{
        //    //    MetaVariable mv = m_CurrentMetaBase as MetaVariable;
        //    //    if (mv != null)
        //    //    {
        //    //        if( m_CurrentMetaBase is MetaIteratorVariable)
        //    //        {
        //    //            return (m_CurrentMetaBase as MetaIteratorVariable).GetIteratorMetaClass();
        //    //        }
        //    //        else
        //    //        {
        //    //            return mv.metaDefineType.metaClass;
        //    //        }
        //    //    }
        //    //}
        //    //else if (m_CallNodeType == ECallNodeType.VisitVariable )
        //    //{
        //    //    MetaVariable mv = m_CurrentMetaBase as MetaVariable;
        //    //    if (mv != null)
        //    //    {
        //    //        return mv.metaDefineType.metaClass;
        //    //    }
        //    //}
        //    //else if( m_CallNodeType == ECallNodeType.DataName )
        //    //{
        //    //    MetaData mv = m_CurrentMetaBase as MetaData;
        //    //    if (mv != null)
        //    //    {
        //    //        return mv;
        //    //    }
        //    //}
        //    //else if( m_CallNodeType == ECallNodeType.MemberVariableName 
        //    //    || m_CallNodeType == ECallNodeType.EnumDefaultValue )
        //    //{
        //    //    MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
        //    //    if( mmv != null )
        //    //    {
        //    //        return mmv.metaDefineType.metaClass;
        //    //    }
        //    //}
        //    //else if (m_CallNodeType == ECallNodeType.EnumName)
        //    //{
        //    //    return m_CurrentMetaBase as MetaEnum;
        //    //}
        //    return null;
        //}
        public override string ToString()
        {
            return m_Token?.lexeme.ToString();
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
           
            if (m_CallNodeSign == ECallNodeSign.Period)
            {
                sb.Append(".");
            }
            if( m_CallNodeType == ECallNodeType.Express )
            {
                sb.Append(m_ExpressNode.ToFormatString());
            }
            else
            {
                //if (m_CallNodeType == ECallNodeType.ClassName
                //    || m_CallNodeType == ECallNodeType.ExternalClassName )
                //{
                //    if (m_CallNodeSign == ECallNodeSign.Null)
                //        sb.Append(m_CurrentMetaBase?.allNameIncludeModule);
                //    else
                //        sb.Append(m_CurrentMetaBase?.name);
                //}
                //else if(m_CallNodeType == ECallNodeType.EnumName )
                //{
                //    sb.Append(m_CurrentMetaBase.allName);
                //}
                //else if (m_CallNodeType == ECallNodeType.EnumDefaultValue)
                //{
                //    sb.Append(m_CurrentMetaBase.name);
                //}
                //else if (m_CallNodeType == ECallNodeType.EnumNewValue)
                //{
                //    sb.Append(m_CurrentMetaBase.name);
                //    sb.Append("(");
                //    sb.Append(m_ExpressNode.ToFormatString());
                //    sb.Append(")");                    
                //}
                //else if( m_CallNodeType == ECallNodeType.DataName )
                //{
                //    sb.Append(m_CurrentMetaBase.allName);
                //}
                //else if(m_CallNodeType == ECallNodeType.NewClass )
                //{
                //    sb.Append(m_MetaClass.ToFormatString());
                //}
                //else if (m_CallNodeType == ECallNodeType.NamespaceName 
                //    || m_CallNodeType == ECallNodeType.ExternalNamespaceName )
                //{
                //    if (m_CallNodeSign == ECallNodeSign.Null)
                //        sb.Append(m_CurrentMetaBase?.allNameIncludeModule);
                //    else
                //        sb.Append(m_CurrentMetaBase?.name);
                //}
                //else if (m_CallNodeType == ECallNodeType.FunctionName)
                //{
                //    //MetaFunctionCall mfc = m_CurrentMetaBase as MetaFunctionCall;
                //    //sb.Append(mfc?.ToCommonString());
                //}
                //else if (m_CallNodeType == ECallNodeType.VariableName)
                //{
                //    sb.Append(m_CurrentMetaBase?.name);
                //}
                //else if( m_CallNodeType == ECallNodeType.VisitVariable )
                //{
                //    sb.Append(m_CurrentMetaBase?.ToFormatString());
                //}
                //else if (m_CallNodeType == ECallNodeType.MemberVariableName)
                //{
                //    sb.Append(m_CurrentMetaBase?.name);
                //}
                //else if (m_CallNodeType == ECallNodeType.MemberDataName )
                //{
                //    sb.Append(m_CurrentMetaBase?.name);
                //}
                //else if (m_CallNodeType == ECallNodeType.This)
                //{
                //    sb.Append("this");
                //}
                //else if (m_CallNodeType == ECallNodeType.Base)
                //{
                //    sb.Append("base");
                //}
                //else if (m_CallNodeType == ECallNodeType.Global)
                //{
                //    sb.Append("global");
                //}
                //else if (m_CallNodeType == ECallNodeType.ConstValue)
                //{
                //    sb.Append(m_ExpressNode.ToFormatString());
                //}

                //if (m_CurrentMetaBase == null)
                //{
                //    //sb.Append("Error 解析Token错误" + token?.ToLexemeAllString());
                //    sb.Append(m_Token?.lexeme.ToString() + "Error(CurrentMetaBase is Null!)");
                //}
            }      
            return sb.ToString();
        }
    }
    public class AllowUseConst
    {
        public bool useNotStatic = false;
        public bool useNotConst = true;
        public bool useThis = true;
        public bool callFunction = true;
        public bool callConstructFunction = true;
        public bool setterFunction = false;
        public bool getterFunction = true ;
        public bool useBrace = false;

        public AllowUseConst()
        {

        }

        public AllowUseConst(AllowUseConst clone )
        {
            useNotStatic = clone.useNotStatic;
            useNotConst = clone.useNotConst;
            useThis = clone.useThis;
            callFunction = clone.callFunction;
            callConstructFunction = clone.callConstructFunction;
            setterFunction = clone.setterFunction;
            getterFunction = clone.getterFunction;
            useBrace = clone.useBrace;
        }
    }
    public partial class MetaCallLink
    {
        public MetaVisitNode finalCallNode => m_FinalCallNode;
        public List<MetaVisitNode> callNodeList
        {
            get
            {
                List<MetaVisitNode> list = new List<MetaVisitNode>();

                list.Add(m_FirstCallNode);
                list.AddRange(m_VisitNodeList);

                return list;
            }
        }
        public AllowUseConst allowUseConst { get; set; }

        private FileMetaCallLink m_FileMetaCallLink;
        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private List<MetaCallNode> m_CallNodeList = new List<MetaCallNode>();

        private MetaVisitNode m_FinalCallNode = null;
        private MetaVisitNode m_FirstCallNode = null;
        private List<MetaVisitNode> m_VisitNodeList = new List<MetaVisitNode>();
        public MetaCallLink(FileMetaCallLink fmcl, MetaClass metaClass, MetaBlockStatements mbs )
        {
            m_FileMetaCallLink = fmcl;
            m_OwnerMetaClass = metaClass;
            m_OwnerMetaBlockStatements = mbs;
            CreateCallLinkNode();
        }
        private void CreateCallLinkNode()
        {
            MetaCallNode frontMetaNode = null;
            if (m_FileMetaCallLink.callNodeList.Count > 0)
            {
                FileMetaCallNode fmcn = m_FileMetaCallLink.callNodeList[0];
                var firstNode = new MetaCallNode(null, fmcn, m_OwnerMetaClass, m_OwnerMetaBlockStatements, this );
                firstNode.isFirst = true;
                frontMetaNode = firstNode;
                m_CallNodeList.Add(firstNode);
            }
            for (int i = 1; i < m_FileMetaCallLink.callNodeList.Count; i = i + 2)
            {
                var cn1 = m_FileMetaCallLink.callNodeList[i];
                var cn2 = m_FileMetaCallLink.callNodeList[i + 1];
                var fmn = new MetaCallNode(cn1, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements, this);
                fmn.isFirst = false;
                fmn.SetFrontCallNode(frontMetaNode);
                m_CallNodeList.Add(fmn);
                frontMetaNode = fmn;
            }
            var m_FinalMetaCallNode = frontMetaNode;
            if( m_FinalMetaCallNode == null )
            {
                Console.WriteLine("Error 连接串没有找到合适的节点  360!!!");
            }
        }
        public Token GetToken() { return null; }
        public bool Parse( AllowUseConst _useConst )
        {
            allowUseConst = new AllowUseConst(_useConst);
            allowUseConst.setterFunction = false;
            allowUseConst.getterFunction = true;
            bool flag = true;// m_FirstNode.ParseNode(allowUseConst);
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                if (flag)
                {
                    if( i == m_CallNodeList.Count - 1 )
                    {
                        allowUseConst.setterFunction = _useConst.setterFunction;
                        allowUseConst.getterFunction = _useConst.getterFunction;
                        flag = m_CallNodeList[i].ParseNode(allowUseConst);
                    }
                    else
                    {
                        flag = m_CallNodeList[i].ParseNode(allowUseConst);
                    }
                }
            }
            if( flag )
            {
                int i = 0;
                while(true)
                {
                    if( i >= m_CallNodeList.Count )
                    {
                        break;
                    }
                    MetaCallNode mcn = m_CallNodeList[i++];
                    if( mcn == null )
                    {
                        break;
                    }
                    if( i < m_CallNodeList.Count )
                    {
                        MetaCallNode nmcn = m_CallNodeList[i++];
                        if( nmcn == null )
                        {
                            Debug.WriteLine("Error 生成链接点报错");
                            break;
                        }
                        if( nmcn.callNodeType == ECallNodeType.VariableName
                            || nmcn.callNodeType == ECallNodeType.MemberVariableName )
                        {
                            MetaVisitVariable mvv = new MetaVisitVariable(mcn.m_MetaVariable, nmcn.m_MetaVariable);
                            MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mvv);
                            m_VisitNodeList.Add(mvn);
                        }
                        else if (nmcn.callNodeType == ECallNodeType.VisitVariable)
                        {
                            MetaVisitNode mvn = MetaVisitNode.CreateByVariable(nmcn.m_MetaVariable as MetaVisitVariable );
                            m_VisitNodeList.Add(mvn);
                        }
                        else if ( nmcn.callNodeType == ECallNodeType.IteratorVariable )
                        {
                        }
                        else if (nmcn.callNodeType == ECallNodeType.DataName)
                        {

                        }
                        else if (nmcn.callNodeType == ECallNodeType.EnumName)
                        {

                        }
                        else if (nmcn.callNodeType == ECallNodeType.MemberDataName )
                        {

                        }
                        else if (nmcn.callNodeType == ECallNodeType.FunctionName)
                        {
                            MetaMethodCall mmc = new MetaMethodCall(nmcn.m_MetaClass, nmcn.m_MetaFunction, nmcn.m_MetaInputParamCollection);
                            MetaVisitNode mvn = MetaVisitNode.CreateByMethodCall(mmc);
                            m_VisitNodeList.Add(mvn);
                        }
                        else if (nmcn.callNodeType == ECallNodeType.NewClass )
                        {
                            MetaMethodCall mmc = new MetaMethodCall( nmcn.m_MetaClass, nmcn.m_MetaFunction, nmcn.m_MetaInputParamCollection );
                            MetaVisitNode mvn = MetaVisitNode.CreateByMethodCall(mmc);
                            m_VisitNodeList.Add(mvn);
                        }
                    }
                    else
                    {
                        if(mcn.callNodeType == ECallNodeType.NewClass)
                        {
                            MetaMethodCall mmc = new MetaMethodCall(mcn.m_MetaClass, mcn.m_MetaFunction, mcn.m_MetaInputParamCollection);
                            MetaVisitNode mvn = MetaVisitNode.CreateByMethodCall(mmc);
                            mvn.visitType = MetaVisitNode.EVisitType.NewMethodCall;
                            m_VisitNodeList.Add(mvn);
                        }
                    }
                }
            }
            if( m_VisitNodeList != null && m_VisitNodeList.Count > 0 )
            {
                m_FinalCallNode = m_VisitNodeList[m_VisitNodeList.Count - 1];
            }
            else
            {
                Console.WriteLine("Error 解析执行链出错");
                flag = false;
            }

            return flag;
        }
        public int CalcParseLevel(int level)
        {
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                level = m_VisitNodeList[i].CalcParseLevel(level);
            }
            return level;
        }
        public void CalcReturnType()
        {
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                m_VisitNodeList[i].CalcReturnType();
            }
        }
        public MetaVariable ExecuteGetMetaVariable()
        {
            //return m_FinalMetaCallNode?.GetMetaVariable();
            return null;
        }
        public MetaClass ExecuteGetMetaClass()
        {
            //return m_FinalMetaCallNode?.GetMetaClass();
            return null;
        }
        public MetaExpressNode GetMetaExpressNode()
        {
            //if( m_FinalMetaCallNode.callNodeType == ECallNodeType.ConstValue )
            //{
            //    return new MetaConstExpressNode(EType.Int32, m_FinalMetaCallNode.constValue);
            //}
            return null;
        }
        public MetaType GetMetaDeineType()
        {
            //if(m_FinalMetaCallNode == null )
            //{
            //    return null;
            //}
            //return m_FinalMetaCallNode.GetMetaDefineType();
            return null;
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                sb.Append(m_CallNodeList[i].ToFormatString());
            }
            return sb.ToString();
        }
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_FileMetaCallLink.ToTokenString());
            return sb.ToString();

        }
    }
}
