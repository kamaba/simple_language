using Simple.CC.C2;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
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

        // 当前是否是第一个元素
        public bool isFirst { get; set; } = false;
        // 第一位是否只能使用this. base.的方式
        public bool isFirstPosMustUseThisBaseOrStaticClassName { get; set; } = false;

        public ECallNodeType callNodeType => m_CallNodeType;
        public MetaConstExpressNode constValue => m_ExpressNode as MetaConstExpressNode;
        public MetaExpressNode metaExpressValue => m_ExpressNode;
        public MetaInputTemplateCollection metaTemplateParamsCollection => m_MetaTemplateParamsCollection;
        public MetaBraceOrBracketStatementsContent metaBraceStatementsContent => m_MetaBraceStatementsContent;
        public Token token => m_Token;

        private AllowUseConst m_AllowUseConst;
        private ECallNodeType m_CallNodeType;
        private ECallNodeSign m_CallNodeSign = ECallNodeSign.Null;
        private MetaBase m_CurrentMetaBase = null;
        private bool m_IsArray = false;
        private bool m_IsFunction = false;

        private MetaCallNode m_FrontCallNode = null;
        private FileMetaCallNode m_FileMetaCallSign = null;
        private FileMetaCallNode m_FileMetaCallNode = null;
        private Token m_Token = null;

        private MetaBlockStatements m_OwnerMetaFunctionBlock = null;
        private MetaClass m_OwnerMetaClass = null;
        private MetaCallLink m_ParentMetaCallLink = null;
        private MetaVariable m_MetaVariable = null;
        private MetaInputParamCollection m_MetaInputParamCollection = null;
        private MetaInputTemplateCollection m_MetaTemplateParamsCollection = null;
        private MetaBraceOrBracketStatementsContent m_MetaBraceStatementsContent = null;
        private List<MetaCallLink> m_MetaArrayCallNodeList = new List<MetaCallLink>();
        private MetaExpressNode m_ExpressNode = null;    // a+b+([expressNode[3+20+10.0f]).ToString() 中的3+20+10.f就是表示式 , Enum.Value(expressNode) , fun(expressNode)

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
                    m_CurrentMetaBase = CoreMetaClassManager.GetMetaClassByEType(m_ExpressNode.eType);
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
            string cnname = m_FileMetaCallNode.name;
            string fatherName = m_FrontCallNode?.m_CurrentMetaBase?.allName;
            bool isAt = m_FileMetaCallNode.atToken != null;

            ETokenType etype = m_Token.type;

            MetaBase calcMetaBase = null;
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
                frontMetaBase = m_FrontCallNode.m_CurrentMetaBase;
            }
            if (!isFirst && (frontMetaBase == null || frontCNT == ECallNodeType.Null))
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
                            string inputMVName = "Visit_" + cnname;
                            m_MetaVariable = mv.GetMetaVaraible(inputMVName);           //Array.@var
                            if (m_MetaVariable == null)
                            {
                                m_MetaVariable = new VisitMetaVariable(inputMVName, m_OwnerMetaClass, m_OwnerMetaFunctionBlock,
                                    mv, null);
                                mv.AddMetaVariable(m_MetaVariable);
                            }
                            calcMetaBase = m_MetaVariable;
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
                    m_CurrentMetaBase = CoreMetaClassManager.GetMetaClassByEType(m_Token.GetEType());
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
                        m_CurrentMetaBase = ProjectManager.globalData;
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
                        m_CurrentMetaBase = ownerClass;
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
                        m_CurrentMetaBase = parentClass;
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
                var selfClass = CoreMetaClassManager.GetCoreMetaClass(cnname);
                if (selfClass != null)
                {
                    calcMetaBase = selfClass;
                }
            }
            else if (etype == ETokenType.Identifier)
            {
                if (isFirst)
                {
                    // Class1. ns. Int32[]
                    calcMetaBase = GetFirstNode(cnname, ownerClass);
                }
                else
                {
                    if (frontCNT == ECallNodeType.NamespaceName)
                    {
                        calcMetaBase = frontMetaBase.GetChildrenMetaBaseByName(cnname);
                    }
                    else if( frontCNT == ECallNodeType.ExternalNamespaceName )
                    {
                        var mn = frontMetaBase as MetaNamespace;
                        var mm = frontMetaBase as MetaModule;
                        if (mn != null)
                        {
                            calcMetaBase = mn.GetCSharpMetaClassOrNamespaceAndCreateByName(cnname);
                        }
                        else if (mm != null)
                        {
                            calcMetaBase = mm.GetCSharpMetaClassOrNamespaceAndCreateByName(cnname);
                        }
                    }
                    else if (frontCNT == ECallNodeType.ClassName)
                    {
                        // ClassName 一般使用在 Class1.静态变量，或者是静态方法的调用
                        var fMC = frontMetaBase as MetaClass;
                        calcMetaBase = fMC.GetChildrenMetaClassByName(cnname);  //查找子类名称
                        if (calcMetaBase == null)
                        {
                            MetaMemberVariable mmv = fMC.GetMetaMemberVariableByName(cnname);  //查找静态变量
                            if(mmv != null )
                            {
                                if( !mmv.isStatic )
                                {
                                    Console.WriteLine("Error 调用非静态成员，不能使用Class.Variable的方式!");
                                    return false;
                                }
                                calcMetaBase = mmv;
                            }
                            if(calcMetaBase == null )
                            {
                                //查找静态函数
                                MetaMemberFunction mmf = fMC.GetMetaMemberFunctionByNameAndInputParamCollect(cnname, m_MetaInputParamCollection);
                                if (mmf != null)
                                {
                                    if( !mmf.isStatic )
                                    {
                                        Console.WriteLine("Error 调用非静态成员，不能使用Class.Variable的方式!");
                                        return false;
                                    }
                                    calcMetaBase = new MetaFunctionCall(fMC, mmf, m_MetaInputParamCollection);
                                }
                            }
                        }
                    }
                    else if( frontCNT == ECallNodeType.ExternalClassName )
                    {
                        MetaClass tmetaClass= frontMetaBase as MetaClass;
                        calcMetaBase = GetFunctionOrVariableByOwnerClass(tmetaClass, cnname, true);
                        MetaMemberFunction mmf = calcMetaBase as MetaMemberFunction;
                        if (mmf != null)
                        {
                            calcMetaBase = new MetaFunctionCall( tmetaClass, mmf, m_MetaInputParamCollection);
                        }
                    }
                    else if( frontCNT == ECallNodeType.Global )
                    {
                        m_CurrentMetaBase = GetDataValueByMetaData(frontMetaBase as MetaData, cnname);
                        m_CallNodeType = ECallNodeType.MemberDataName;
                    }
                    else if (frontCNT == ECallNodeType.DataName)
                    {
                        m_CurrentMetaBase = GetDataValueByMetaData(frontMetaBase as MetaData, cnname);
                        m_CallNodeType = ECallNodeType.MemberDataName;
                    }
                    else if (frontCNT == ECallNodeType.MemberDataName)
                    {
                        m_CurrentMetaBase = GetDataValueByMetaMemberData(frontMetaBase as MetaMemberData, cnname);
                        m_CallNodeType = ECallNodeType.MemberDataName;
                    }
                    else if( frontCNT == ECallNodeType.EnumName )
                    {
                        calcMetaBase = GetEnumValue(frontMetaBase as MetaEnum, cnname);
                        if( !m_IsFunction )
                        {
                            m_CurrentMetaBase = calcMetaBase;
                            m_CallNodeType = ECallNodeType.EnumDefaultValue;
                        }
                    }
                    else if (frontCNT == ECallNodeType.VariableName
                        || frontCNT == ECallNodeType.MemberVariableName
                        || frontCNT == ECallNodeType.VisitVariable )
                    {
                        var mv = frontMetaBase as MetaVariable;
                        MetaVariable getmv2 = null;
                        if ( mv.isArray )
                        {
                            if(isAt )
                            {
                                // Array1.$i.x   Array1.$mmq.x;
                                getmv2 = m_OwnerMetaFunctionBlock.GetMetaVariableByName(cnname);
                                if (getmv2 != null)    //查找是否已定义过变量
                                {
                                    string inputMVName = "Visit_" + cnname;
                                    m_MetaVariable = mv.GetMetaVaraible(inputMVName);
                                    if (m_MetaVariable == null)
                                    {
                                        m_MetaVariable = new VisitMetaVariable(inputMVName, m_OwnerMetaClass, m_OwnerMetaFunctionBlock,
                                            mv, getmv2);
                                        mv.AddMetaVariable(m_MetaVariable);
                                    }
                                    calcMetaBase = m_MetaVariable;
                                    m_CallNodeType = ECallNodeType.VisitVariable;
                                }
                            }
                            //if (mv.metaDefineType.isGenTemplateClass)
                            //{
                            //    calcMetaBase = mv.metaDefineType.GetMetaInputTemplateByIndex();
                            //}
                        }

                        if(calcMetaBase == null )
                        {
                            var mc = mv.metaDefineType.metaClass;
                            if (mc is MetaData)
                            {
                                MetaData md = mc as MetaData;
                                m_CurrentMetaBase = GetDataValueByMetaData(md, cnname);
                                m_CallNodeType = ECallNodeType.MemberDataName;
                            }
                            else if (mc is MetaEnum)
                            {
                                MetaEnum me = mc as MetaEnum;
                                //calcMetaBase = GetFunctionOrVariableByOwnerClass(md, cnname);
                                //m_CallNodeType = ECallNodeType.DataMemberName;
                            }
                            else
                            {
                                var gett2 = GetFunctionOrVariableByOwnerClass(mv.metaDefineType.metaClass, cnname, false);
                               
                                if (calcMetaBase == null)
                                {
                                    MetaMemberFunction mmf = gett2 as MetaMemberFunction;
                                    if (mmf != null )
                                    {
                                        if( m_MetaInputParamCollection == null )
                                        {
                                            m_MetaInputParamCollection = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaFunctionBlock);
                                        }
                                        calcMetaBase = m_CurrentMetaBase = new MetaFunctionCall(mv, mmf, m_MetaInputParamCollection);
                                        m_CallNodeType = ECallNodeType.FunctionName;
                                    }

                                    if( calcMetaBase == null )
                                    {
                                        MetaMemberVariable mmv = (gett2 as MetaMemberVariable);
                                        if( mmv != null)
                                        {
                                            calcMetaBase = new VisitMetaVariable(mv, mmv);
                                        }
                                    }
                                    if (calcMetaBase == null)
                                    {
                                        var gmmv3 = (mv as IteratorMetaVariable);
                                        if (gmmv3 != null)
                                        {
                                            calcMetaBase = gmmv3.GetMetaVaraible(cnname);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (frontCNT == ECallNodeType.This)
                    {
                        var mmv = frontMetaBase as MetaClass;
                        calcMetaBase = GetFunctionOrVariableByOwnerClass(mmv, cnname, false);
                    }
                    else if (frontCNT == ECallNodeType.Base)
                    {
                        var mmv = frontMetaBase as MetaClass;
                        calcMetaBase = GetFunctionOrVariableByOwnerClass(mmv, cnname, false);
                    }
                    else if (frontCNT == ECallNodeType.FunctionName)
                    {
                        var mmf = frontMetaBase as MetaFunctionCall;
                        MetaType retMT = mmf.GetRetMetaType();
                        if( retMT != null && retMT.metaClass != null )
                        {
                            calcMetaBase = GetFunctionOrVariableByOwnerClass(retMT.metaClass, cnname, false);
                        }
                        else
                        {
                            Console.WriteLine("Error 函数没有返回类型"); 
                        }
                    }
                    else if (frontCNT == ECallNodeType.ConstValue)
                    {
                        var mmv = frontMetaBase as MetaClass;
                        calcMetaBase = GetFunctionOrVariableByOwnerClass(mmv, cnname, false);
                    }
                    else if( frontCNT == ECallNodeType.Express )
                    {
                        var mmv = frontMetaBase as MetaClass;
                        calcMetaBase = GetFunctionOrVariableByOwnerClass(mmv, cnname, false);
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
                                    calcMetaBase = GetFunctionOrVariableByOwnerClass(constraintClass, cnname, false);
                                    if (calcMetaBase != null)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                calcMetaBase = GetFunctionOrVariableByOwnerClass(CoreMetaClassManager.objectMetaClass, cnname, false);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error 暂不支持上节点的类型: " + frontCNT.ToString());
                    }
                }
            }
            if (m_CurrentMetaBase != null)
            {
                return true;
            }
            if (calcMetaBase == null)
            {
                Console.WriteLine("Error 没有获得MetaBase节点" + m_Token.ToLexemeAllString());
                return false;
            }

            if (m_IsFunction)
            {                
                if (calcMetaBase is MetaNamespace || calcMetaBase is MetaModule)
                {
                    Console.WriteLine("Error 函数调用与命名空间冲突!!");
                    return false;
                }
                else if (calcMetaBase is MetaClass)
                {
                    MetaClass curmc = calcMetaBase as MetaClass;
                    if (this.m_IsArray)
                    {
                        curmc = CoreMetaClassManager.arrayMetaClass;
                    }
                    if (curmc.isTemplateClass)
                    {
                        MetaInputTemplateCollection tmitc = m_MetaTemplateParamsCollection;
                        if (curmc == CoreMetaClassManager.rangeMetaClass  )
                        {
                            MetaClass mc = m_MetaInputParamCollection.GetMaxLevelMetaClassType();
                            if (m_MetaTemplateParamsCollection == null)
                            {
                                m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                                m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(new MetaType(mc));
                                tmitc = m_MetaTemplateParamsCollection;
                            }
                        }
                        else if( curmc == CoreMetaClassManager.arrayMetaClass )
                        {
                            if(m_MetaInputParamCollection == null )
                            {
                                m_MetaInputParamCollection = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaFunctionBlock);
                            }

                            if (tmitc == null)
                            {
                                tmitc = new MetaInputTemplateCollection();

                                MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
                                if(m_FileMetaCallNode.fileMetaBraceTerm != null)
                                {
                                    m_MetaBraceStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaCallNode.fileMetaBraceTerm, m_OwnerMetaFunctionBlock, m_OwnerMetaClass );
                                    m_MetaBraceStatementsContent.Parse();
                                    MetaClass tmc = m_MetaBraceStatementsContent.GetMaxLevelMetaClassType();
                                    if( tmc != CoreMetaClassManager.objectMetaClass )
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
                            calcMetaBase = new MetaFunctionCall(mtc, mmf, m_MetaInputParamCollection);
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
                        MetaMemberFunction mmf = curmc.GetMetaMemberFunctionByNameAndInputParamCollect( "__Init__", m_MetaInputParamCollection);
                        if( mmf == null)
                        {
                            Console.WriteLine("Error 没有找到" + curmc.allName + "的__Init__方法!)");
                            return false;
                        }
                        calcMetaBase = new MetaFunctionCall(curmc, mmf, m_MetaInputParamCollection);
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
                else if (calcMetaBase is MetaFunction)
                {
                    m_CallNodeType = ECallNodeType.FunctionName;
                    MetaFunction mf = calcMetaBase as MetaFunction;
                    MetaMemberFunction mmf = calcMetaBase as MetaMemberFunction;
                    if (mmf != null)
                    {
                        if (mmf.isConstructInitFunction && !m_AllowUseConst.callConstructFunction)
                        {
                            Console.WriteLine("Error 不允许使用构造函数" + m_Token.ToLexemeAllString());
                            return false;
                        }
                    }
                    //calcMetaBase = new MetaFunctionCall( m_ow)
                }
                else if( calcMetaBase is MetaFunctionCall )
                {
                    m_CallNodeType = ECallNodeType.FunctionName;
                }
                else if( calcMetaBase is MetaMemberVariable )  // Enum e = Enum.MetaVaraible( 2 )
                {
                    MetaMemberVariable mmv = calcMetaBase as MetaMemberVariable;
                    m_CurrentMetaBase = calcMetaBase;

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
                var mv = calcMetaBase as MetaVariable;
                if (mv != null)
                {
                    var mmv = calcMetaBase as MetaMemberVariable;
                    var vmv = calcMetaBase as VisitMetaVariable;
                    if (mmv != null)
                    {
                        m_CallNodeType = ECallNodeType.MemberVariableName;
                    }
                    else if(vmv != null )
                    {
                        m_CallNodeType = ECallNodeType.VisitVariable;
                    }
                    else
                    {
                        m_CallNodeType = ECallNodeType.VariableName;
                    }
                    if (m_AllowUseConst.useNotStatic == false && mv.isStatic == false)
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
                    if (mv.isArray)             //Array<int> arr = {1,2,3}; 这个arr[0]就是mv
                    {
                        if (m_MetaArrayCallNodeList.Count == 1 && this.m_IsArray)  //arr[?]
                        {
                            MetaCallNode fmcn1 = m_MetaArrayCallNodeList[0].finalMetaCallNode;
                            if (fmcn1?.callNodeType == ECallNodeType.ConstValue)       //arr[0]
                            {
                                string tname = fmcn1.constValue.value.ToString();
                                m_MetaVariable = mv.GetMetaVaraible(tname);
                                if (m_MetaVariable == null)
                                {
                                    m_MetaVariable = new VisitMetaVariable(tname, m_OwnerMetaClass, m_OwnerMetaFunctionBlock, mv, null);
                                    mv.AddMetaVariable(m_MetaVariable);
                                }
                            }
                            else if (fmcn1?.callNodeType == ECallNodeType.VariableName)    //arr[var]
                            {
                                var gmv = (fmcn1).GetMetaVariable();
                                string tname = "VarName_" + gmv.name + "_VarHashCode_" + gmv.GetHashCode().ToString();
                                m_MetaVariable = mv.GetMetaVaraible(tname);
                                if (m_MetaVariable == null)
                                {
                                    m_MetaVariable = new VisitMetaVariable(tname, m_OwnerMetaClass, m_OwnerMetaFunctionBlock, mv, gmv );
                                    mv.AddMetaVariable(m_MetaVariable);
                                }
                            }
                            calcMetaBase = m_MetaVariable;
                            m_CallNodeType = ECallNodeType.VisitVariable;
                        }
                    }
                }
                else if (calcMetaBase is MetaNamespace || calcMetaBase is MetaModule)
                {
                    if( calcMetaBase.refFromType == RefFromType.CSharp )
                    {
                        m_CallNodeType = ECallNodeType.ExternalNamespaceName;
                    }
                    else
                    {
                        m_CallNodeType = ECallNodeType.NamespaceName;
                    }
                }
                else if (calcMetaBase is MetaClass)
                {
                    if( calcMetaBase is MetaEnum )
                    {
                        m_CallNodeType = ECallNodeType.EnumName;
                    }
                    else if( calcMetaBase is MetaData )
                    {
                        m_CallNodeType = ECallNodeType.DataName;
                    }
                    else
                    {
                        if( calcMetaBase.refFromType == RefFromType.CSharp )
                        {
                            m_CallNodeType = ECallNodeType.ExternalClassName;
                        }
                        else
                        {
                            m_CallNodeType = ECallNodeType.ClassName;
                        }
                    }
                    if( this.m_IsArray )
                    {
                        if (m_MetaArrayCallNodeList.Count > 0)   //int[??]
                        {
                            if (m_MetaArrayCallNodeList.Count > 3)
                            {
                                Console.WriteLine("Error 数组不能超过三维!!");
                            }
                            m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                            m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(new MetaType(calcMetaBase as MetaClass));

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
                                    calcMetaBase = new MetaFunctionCall(templateMC, mmf, m_MetaInputParamCollection);
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
                else if (calcMetaBase is MetaTemplate)
                {
                    m_CallNodeType = ECallNodeType.TemplateName;
                }
                else
                {
                    Console.WriteLine("Error !! 非函数类型!!" + m_FileMetaCallNode.token.ToLexemeAllString());
                }
            }
            m_CurrentMetaBase = calcMetaBase;
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
                    retMC = new MetaFunctionCall(mc, mmf, m_MetaInputParamCollection);
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
        public void SetInputParamCollection(MetaInputParamCollection mipc)
        {
            m_MetaInputParamCollection = mipc;
        }
        public MetaFunctionCall GetMetaFunctionCall()
        {
            return m_CurrentMetaBase as MetaFunctionCall;
            if (m_CallNodeType == ECallNodeType.FunctionName)
            {
                MetaMemberFunction mmf = m_CurrentMetaBase as MetaMemberFunction;

                if (mmf != null)
                {
                    MetaFunctionCall mfc = new MetaFunctionCall(mmf.ownerMetaClass, mmf, m_MetaInputParamCollection );
                    return mfc;
                }
            }
            else if( m_CallNodeType == ECallNodeType.NewClass )
            {
            }
            return null;
        }
        public MetaType GetMetaDefineType()
        {
            if (m_CallNodeType == ECallNodeType.Express
                || m_CallNodeType == ECallNodeType.EnumNewValue
                || m_CallNodeType == ECallNodeType.ConstValue )
            {
                return m_ExpressNode.GetReturnMetaDefineType();
            }
            if (m_CurrentMetaBase == null) return null;

            if( m_CallNodeType == ECallNodeType.NewClass )
            {
                return new MetaType((m_CurrentMetaBase as MetaFunctionCall).callerMetaClass);
            }
            else if (m_CallNodeType == ECallNodeType.ClassName || m_CallNodeType == ECallNodeType.This )
            {
                return new MetaType(m_CurrentMetaBase as MetaClass);
            }
            else if (m_CallNodeType == ECallNodeType.FunctionName)
            {
                var mmf = (m_CurrentMetaBase as MetaMemberFunction);
                if (mmf == null) return null;

                if (mmf.isCastFunction)
                {
                    //if (!mmf.isOverrideFunction)
                    //{
                    //    return m_MetaTemplateParamsCollection.metaTemplateParamsList[0].inputMetaClass;
                    //}
                }
                //if (mmf.isConstructInitFunction)
                //{
                //    return mmf.ownerMetaClass;
                //}
                return mmf.metaDefineType;
            }
            else if (m_CallNodeType == ECallNodeType.VariableName
                || m_CallNodeType == ECallNodeType.VisitVariable )
            {
                MetaVariable mv = m_CurrentMetaBase as MetaVariable;
                if (mv != null)
                {
                    return mv.metaDefineType;
                }
            }
            else if( m_CallNodeType == ECallNodeType.DataName )
            {
                MetaData mv = m_CurrentMetaBase as MetaData;
                if (mv != null)
                {
                    return new MetaType( mv );
                }
            }
            else if (m_CallNodeType == ECallNodeType.MemberVariableName)
            {
                MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
                if (mmv != null)
                {
                    return mmv.metaDefineType;
                }
            }
            else if( m_CallNodeType == ECallNodeType.EnumDefaultValue
                || m_CallNodeType == ECallNodeType.EnumNewValue )
            {
                MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
                if( mmv != null )
                {
                    return mmv.metaDefineType;
                }
            }
            return null;
        }
        public int CalcParseLevel(int level)
        {
            if (m_CurrentMetaBase == null) return level;
            var mv = m_CurrentMetaBase as MetaMemberVariable;
            if (mv != null)
            {
                return mv.CalcParseLevelBeCall(level);
            }
            return level;
        }
        public void CalcReturnType()
        {
            m_MetaInputParamCollection?.CaleReturnType();
            if (m_CallNodeType == ECallNodeType.VariableName)
            {
                MetaVariable mv = m_CurrentMetaBase as MetaVariable;
                //if (mv != null && (!mv.isTemplateClass && mv.metaDefineType.metaClass == null))
                //{
                //    //Console.WriteLine("Error 未解析到:" + mv.allName + "位置在:" + mv.ToFormatString());
                //    return;
                //}
            }
            else if (m_CallNodeType == ECallNodeType.DataName )
            {
                MetaData md = m_CurrentMetaBase as MetaData;
                return;
            }
        }
        public MetaMemberVariable GetMetaMemeberVariable()
        {
            if ( m_CallNodeType == ECallNodeType.MemberVariableName
                || m_CallNodeType == ECallNodeType.EnumDefaultValue
                || m_CallNodeType == ECallNodeType.EnumNewValue )
            {
                MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
                return mmv;
            }
            return null;
        }
        public MetaVariable GetMetaVariable()
        {
            if (m_CallNodeType == ECallNodeType.VariableName 
                || m_CallNodeType == ECallNodeType.MemberVariableName
                || m_CallNodeType == ECallNodeType.VisitVariable )
                
            {
                MetaVariable mv = m_CurrentMetaBase as MetaVariable;
                return mv;
            }
            else if (m_CallNodeType == ECallNodeType.DataName)
            {
                return (m_CurrentMetaBase as MetaData).metaVariable;
            }
            else if (m_CallNodeType == ECallNodeType.EnumName)
            {
                return (m_CurrentMetaBase as MetaEnum).metaVariable;
            }
            else if( m_CallNodeType == ECallNodeType.MemberDataName )
            {
                return (m_CurrentMetaBase as MetaVariable);
            }
            return null;
        }
        public MetaClass GetMetaClass()
        {
            if( m_CallNodeType == ECallNodeType.Express )
            {
                return m_ExpressNode.GetReturnMetaClass();
            }
            if (m_CurrentMetaBase == null) return null;

            if(m_CallNodeType == ECallNodeType.ClassName
                || m_CallNodeType == ECallNodeType.ExternalClassName
                || m_CallNodeType == ECallNodeType.This
                || m_CallNodeType == ECallNodeType.ConstValue )
            {
                return m_CurrentMetaBase as MetaClass;
            }
            else if( m_CallNodeType == ECallNodeType.FunctionName || m_CallNodeType == ECallNodeType.NewClass )
            {
                var mmf = (m_CurrentMetaBase as MetaFunctionCall);
                if (mmf == null) return null;

                return mmf.GetMetaClass();
            }
            else if (m_CallNodeType == ECallNodeType.VariableName)
            {
                MetaVariable mv = m_CurrentMetaBase as MetaVariable;
                if (mv != null)
                {
                    if( m_CurrentMetaBase is IteratorMetaVariable )
                    {
                        return (m_CurrentMetaBase as IteratorMetaVariable).GetIteratorMetaClass();
                    }
                    else
                    {
                        return mv.metaDefineType.metaClass;
                    }
                }
            }
            else if (m_CallNodeType == ECallNodeType.VisitVariable )
            {
                MetaVariable mv = m_CurrentMetaBase as MetaVariable;
                if (mv != null)
                {
                    return mv.metaDefineType.metaClass;
                }
            }
            else if( m_CallNodeType == ECallNodeType.DataName )
            {
                MetaData mv = m_CurrentMetaBase as MetaData;
                if (mv != null)
                {
                    return mv;
                }
            }
            else if( m_CallNodeType == ECallNodeType.MemberVariableName 
                || m_CallNodeType == ECallNodeType.EnumDefaultValue )
            {
                MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
                if( mmv != null )
                {
                    return mmv.metaDefineType.metaClass;
                }
            }
            else if (m_CallNodeType == ECallNodeType.EnumName)
            {
                return m_CurrentMetaBase as MetaEnum;
            }
            return null;
        }
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
                if (m_CallNodeType == ECallNodeType.ClassName
                    || m_CallNodeType == ECallNodeType.ExternalClassName )
                {
                    if (m_CallNodeSign == ECallNodeSign.Null)
                        sb.Append(m_CurrentMetaBase?.allNameIncludeModule);
                    else
                        sb.Append(m_CurrentMetaBase?.name);
                }
                else if(m_CallNodeType == ECallNodeType.EnumName )
                {
                    sb.Append(m_CurrentMetaBase.allName);
                }
                else if (m_CallNodeType == ECallNodeType.EnumDefaultValue)
                {
                    sb.Append(m_CurrentMetaBase.name);
                }
                else if (m_CallNodeType == ECallNodeType.EnumNewValue)
                {
                    sb.Append(m_CurrentMetaBase.name);
                    sb.Append("(");
                    sb.Append(m_ExpressNode.ToFormatString());
                    sb.Append(")");                    
                }
                else if( m_CallNodeType == ECallNodeType.DataName )
                {
                    sb.Append(m_CurrentMetaBase.allName);
                }
                else if(m_CallNodeType == ECallNodeType.NewClass )
                {
                    MetaFunctionCall mfc = m_CurrentMetaBase as MetaFunctionCall;
                    sb.Append(mfc.ToFormatString());
                }
                else if (m_CallNodeType == ECallNodeType.NamespaceName 
                    || m_CallNodeType == ECallNodeType.ExternalNamespaceName )
                {
                    if (m_CallNodeSign == ECallNodeSign.Null)
                        sb.Append(m_CurrentMetaBase?.allNameIncludeModule);
                    else
                        sb.Append(m_CurrentMetaBase?.name);
                }
                else if (m_CallNodeType == ECallNodeType.FunctionName)
                {
                    MetaFunctionCall mfc = m_CurrentMetaBase as MetaFunctionCall;
                    sb.Append(mfc?.ToCommonString());
                }
                else if (m_CallNodeType == ECallNodeType.VariableName)
                {
                    sb.Append(m_CurrentMetaBase?.name);
                }
                else if( m_CallNodeType == ECallNodeType.VisitVariable )
                {
                    sb.Append(m_CurrentMetaBase?.ToFormatString());
                }
                else if (m_CallNodeType == ECallNodeType.MemberVariableName)
                {
                    sb.Append(m_CurrentMetaBase?.name);
                }
                else if (m_CallNodeType == ECallNodeType.MemberDataName )
                {
                    sb.Append(m_CurrentMetaBase?.name);
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
                else if (m_CallNodeType == ECallNodeType.ConstValue)
                {
                    sb.Append(m_ExpressNode.ToFormatString());
                }

                if (m_CurrentMetaBase == null)
                {
                    //sb.Append("Error 解析Token错误" + token?.ToLexemeAllString());
                    sb.Append(m_Token?.lexeme.ToString() + "Error(CurrentMetaBase is Null!)");
                }
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
        public MetaCallNode finalMetaCallNode => m_FinalMetaCallNode;
        public List<MetaCallNode> callNodeList
        {
            get
            {
                List<MetaCallNode> list = new List<MetaCallNode>();

                list.Add(m_FirstNode);
                list.AddRange(m_CallNodeList);

                return list;
            }
        }
        public AllowUseConst allowUseConst { get; set; }
        public MetaFunctionCall metaFunctionCall => finalMetaCallNode?.GetMetaFunctionCall();

        private FileMetaCallLink m_FileMetaCallLink;

        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private MetaCallNode m_FinalMetaCallNode = null;
        private MetaCallNode m_FirstNode = null;
        private List<MetaCallNode> m_CallNodeList = new List<MetaCallNode>();
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
                m_FirstNode = new MetaCallNode(null, fmcn, m_OwnerMetaClass, m_OwnerMetaBlockStatements, this );
                m_FirstNode.isFirst = true;
                frontMetaNode = m_FirstNode;
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
            m_FinalMetaCallNode = frontMetaNode;

            if( m_FinalMetaCallNode == null )
            {
                Console.WriteLine("Error 连接串没有找到合适的节点  360!!!");
            }
        }
        public bool Parse( AllowUseConst _useConst )
        {
            allowUseConst = new AllowUseConst(_useConst);
            allowUseConst.setterFunction = false;
            allowUseConst.getterFunction = true;
            bool flag = m_FirstNode.ParseNode(allowUseConst);
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

            return flag;
        }
        public int CalcParseLevel(int level)
        {
            if (m_FirstNode != null)
                level = m_FirstNode.CalcParseLevel(level);

            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                level = m_CallNodeList[i].CalcParseLevel(level);
            }
            return level;
        }
        public void CalcReturnType()
        {
            m_FirstNode.CalcReturnType();
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                m_CallNodeList[i].CalcReturnType();
            }
        }
        public MetaVariable ExecuteGetMetaVariable()
        {
            return m_FinalMetaCallNode?.GetMetaVariable();
        }
        public MetaClass ExecuteGetMetaClass()
        {
            return m_FinalMetaCallNode?.GetMetaClass();
        }
        public MetaExpressNode GetMetaExpressNode()
        {
            if( m_FinalMetaCallNode.callNodeType == ECallNodeType.ConstValue )
            {
                return new MetaConstExpressNode(EType.Int32, m_FinalMetaCallNode.constValue);
            }
            return null;
        }
        public MetaType GetMetaDeineType()
        {
            if(m_FinalMetaCallNode == null )
            {
                return null;
            }
            return m_FinalMetaCallNode.GetMetaDefineType();
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_FirstNode.ToFormatString());
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
