using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;
using System;
using System.Collections;
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
        TemplateName,
        NamespaceName,
        ClassName,
        EnumName,
        EnumValue,
        DataName,
        DataValue,
        VariableName,
        MemberVariableName,
        MemberDataName,
        NewClass,
        FunctionName,
        ConstValue,
        This,
        Base,
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
        public MetaConstExpressNode constValue => m_ConstValue;
        public MetaInputTemplateCollection metaTemplateParamsCollection => m_MetaTemplateParamsCollection;
        public bool isFunction => m_IsFunction;
        public bool isArray => m_IsArray;
        public Token token => m_Token;

        private AllowUseConst m_AllowUseConst;
        private ECallNodeType m_CallNodeType;
        private ECallNodeSign m_CallNodeSign = ECallNodeSign.Null;
        private MetaBase m_CurrentMetaBase = null;
        private MetaConstExpressNode m_ConstValue;
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
        private List<MetaCallLink> m_MetaCallNodeList = new List<MetaCallLink>();
        private MetaExpressNode m_ExpressNode = null;    // a+b+(3+20+10.0f).ToString() 中的3+20+10.f就是表示式

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

            if (m_IsFunction)
            {
                m_MetaInputParamCollection = new MetaInputParamCollection(m_FileMetaCallNode.fileMetaParTerm, mc, mbs); 

                if (m_FileMetaCallNode.inputTemplateNodeList.Count > 0)
                {
                    m_MetaTemplateParamsCollection = new MetaInputTemplateCollection(m_FileMetaCallNode.inputTemplateNodeList, ownerClass);
                }
            }
            else
            {
                m_MetaInputParamCollection = new MetaInputParamCollection(mc, mbs);

                if( m_FileMetaCallNode.fileMetaParTerm != null )
                {
                    var firstNode = m_FileMetaCallNode.fileMetaParTerm.fileMetaExpressList[0];
                    if( firstNode == null )
                    {
                        Console.WriteLine("Error ~~~~不能使用输入()中的内容!!");
                    }
                    else
                    {
                        m_ExpressNode = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements( m_OwnerMetaFunctionBlock,
                            null, firstNode);
                    }
                }
            }
            m_IsArray = m_FileMetaCallNode.isArray;
            for (int i = 0; i < m_FileMetaCallNode.arrayNodeList.Count; i++)
            {
                MetaCallLink cmcl = new MetaCallLink(m_FileMetaCallNode.arrayNodeList[i], mc, mbs);
                m_MetaCallNodeList.Add(cmcl);
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
            if( m_ExpressNode != null )
            {
                m_ExpressNode.CalcReturnType();
                m_CurrentMetaBase = CoreMetaClassManager.GetMetaClassByEType(m_ExpressNode.eType);
                m_CallNodeType = ECallNodeType.Express;
                return true;
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

            for (int i = 0; i < this.m_MetaCallNodeList.Count; i++)
            {
                m_MetaCallNodeList[i].Parse(m_AllowUseConst);
            }
            m_MetaInputParamCollection.CaleReturnType();

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
                if (frontCNT == ECallNodeType.VariableName || frontCNT == ECallNodeType.MemberVariableName)
                {
                    MetaVariable mv = frontMetaBase as MetaVariable;
                    if (mv.isArray)   //Array.
                    {
                        // Array1.@0.x   Array1.1.x;
                        if (isAt)                  //Array.@
                        {
                            var gmit = mv.metaDefineType.GetMetaInputTemplateByIndex();
                            string inputMVName = "C_" + cnname;
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
                            Console.WriteLine("Error 在Array.后边如果使用变量或者是数字常量，必须使用Array.@方式!!");
                        }
                    }
                }
                //不是常量值 
                if (!isNotConstValue)
                {
                    m_CallNodeType = ECallNodeType.ConstValue;
                    m_ConstValue = new MetaConstExpressNode( m_Token.GetEType(), m_Token.lexeme );
                    m_CurrentMetaBase = CoreMetaClassManager.GetMetaClassByEType(m_Token.GetEType());

                    if (isFirstPosMustUseThisBaseOrStaticClassName)
                    {
                        Console.WriteLine("Error 第一位置只允许使用this/base");
                    }
                }
            }
            else if (etype == ETokenType.This)
            {
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
                var selfClass = CoreMetaClassManager.GetSelfMetaClass(cnname);
                if (selfClass != null)
                {
                    calcMetaBase = selfClass;
                    if (this.m_IsArray)
                    {
                        m_IsFunction = true;
                    }
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
                        calcMetaBase = GetFunctionOrVariableByOwnerClass(frontMetaBase as MetaClass, cnname, true);
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
                        if( isFunction )
                        {
                            m_CurrentMetaBase = calcMetaBase;
                            m_CallNodeType = ECallNodeType.EnumValue;
                        }
                        else
                        {
                            m_CurrentMetaBase = calcMetaBase;
                            m_CallNodeType = ECallNodeType.EnumValue;
                        }
                    }
                    else if (frontCNT == ECallNodeType.VariableName || frontCNT == ECallNodeType.MemberVariableName)
                    {
                        var mmv = frontMetaBase as MetaVariable;
                        MetaVariable getmv2 = null;
                        if (mmv.isArray)
                        {
                            // Array1.@i.x   Array1.@mmq.x;
                            getmv2 = m_OwnerMetaFunctionBlock.GetMetaVariableByName(cnname);
                            if (getmv2 != null)    //查找是否已定义过变量
                            {
                                if (isAt)
                                {
                                    string inputMVName = "I_" + cnname;
                                    m_MetaVariable = mmv.GetMetaVaraible(inputMVName);
                                    if (m_MetaVariable == null)
                                    {
                                        m_MetaVariable = new VisitMetaVariable(inputMVName, m_OwnerMetaClass, m_OwnerMetaFunctionBlock,
                                            mmv, getmv2);
                                        mmv.AddMetaVariable(m_MetaVariable);
                                    }
                                    calcMetaBase = m_MetaVariable;
                                }
                                else
                                {
                                    //Array1.i.x 不允许
                                    Console.WriteLine("Error 在Array.后边如果使用变量或者是数字常量，必须使用Array.@方式!!");
                                }
                            }
                        }
                        if (mmv.isTemplate)
                        {
                            calcMetaBase = mmv.metaDefineType.GetMetaInputTemplateByIndex();
                        }
                        var mc = mmv.metaDefineType.metaClass;
                        if( mc is MetaData )
                        {
                            MetaData md = mc as MetaData;
                            m_CurrentMetaBase = GetDataValueByMetaData(md, cnname);
                            m_CallNodeType = ECallNodeType.MemberDataName;
                        }
                        else if( mc is MetaEnum )
                        {
                            MetaEnum me = mc as MetaEnum;
                            //calcMetaBase = GetFunctionOrVariableByOwnerClass(md, cnname);
                            //m_CallNodeType = ECallNodeType.DataMemberName;
                        }
                        else
                        {
                            var gett2 = GetFunctionOrVariableByOwnerClass(mmv.metaDefineType.metaClass, cnname, false);
                            if (getmv2 != null && gett2 != null)
                            {
                                Console.WriteLine("定义变量名称与数组函数名称相同，建议更换变量名称!!");
                                return false;
                            }
                            if (calcMetaBase == null)
                            {
                                calcMetaBase = gett2;
                                if (calcMetaBase is MetaMemberFunction)
                                {
                                    m_IsFunction = true;
                                }

                                var gmmv2 = (gett2 as MetaMemberVariable);
                                if (gmmv2 != null && gmmv2.metaDefineType.metaClass == CoreMetaClassManager.templateMetaClass)
                                {
                                    string inputMVName = "T_" + cnname;
                                    m_MetaVariable = mmv.GetMetaVaraible(inputMVName);
                                    if (m_MetaVariable == null)
                                    {
                                        m_MetaVariable = new VisitMetaVariable(inputMVName, m_OwnerMetaClass, m_OwnerMetaFunctionBlock,
                                            mmv, gmmv2);
                                        mmv.AddMetaVariable(m_MetaVariable);
                                    }
                                    calcMetaBase = m_MetaVariable;
                                }
                                var gmmv3 = (mmv as IteratorMetaVariable);
                                if (gmmv3 != null)
                                {
                                    calcMetaBase = gmmv3.GetMetaVaraible(cnname);
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
                        var mmf = frontMetaBase as MetaMemberFunction;
                        calcMetaBase = GetFunctionOrVariableByOwnerClass(mmf.metaDefineType.metaClass, cnname, false);
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
                    if (this.m_IsArray)
                    {
                        for (int i = 0; i < m_MetaCallNodeList.Count; i++)
                        {
                            m_MetaInputParamCollection.AddMetaInputParam(new MetaInputParam(m_MetaCallNodeList[i].GetMetaExpressNode()));
                        }

                        if (m_MetaTemplateParamsCollection == null)
                        {
                            m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                        }
                        MetaType mitp = new MetaType(calcMetaBase as MetaClass);
                        m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(mitp);
                        calcMetaBase = CoreMetaClassManager.arrayMetaClass.GetMetaMemberConstructDefaultFunction();
                    }
                    else if ((calcMetaBase as MetaClass) == CoreMetaClassManager.arrayMetaClass)
                    {
                        calcMetaBase = CoreMetaClassManager.arrayMetaClass.GetMetaMemberConstructDefaultFunction();
                        m_IsArray = true;
                    }
                    else
                    {
                        var curmc = (calcMetaBase as MetaClass);
                        if (curmc == CoreMetaClassManager.arrayMetaClass)
                        {
                            if (m_MetaTemplateParamsCollection != null)
                            {
                                if (m_MetaTemplateParamsCollection.metaTemplateParamsList.Count != 1)
                                {
                                    Console.WriteLine("Error 在使用Array中，传出的<T>只允许有一个");
                                    return false;
                                }
                            }
                            else
                            {
                                m_MetaTemplateParamsCollection = new MetaInputTemplateCollection();
                                MetaClass getmc = m_MetaInputParamCollection?.GetMaxLevelMetaClassType();
                                if (getmc == null)
                                {
                                    getmc = CoreMetaClassManager.objectMetaClass;
                                }
                                MetaType mitp = new MetaType(getmc);
                                m_MetaTemplateParamsCollection.AddMetaTemplateParamsList(mitp);
                            }
                            m_CurrentMetaBase = curmc;
                            m_CallNodeType = ECallNodeType.NewClass;
                        }
                        else
                        {
                            calcMetaBase = curmc.GetMetaMemberConstructFunctionByTemplateAndParam(m_MetaTemplateParamsCollection, m_MetaInputParamCollection);
                            if( calcMetaBase == null )
                            {
                                calcMetaBase = curmc;
                                m_CallNodeType = ECallNodeType.NewClass;
                            }
                            else
                            {
                                m_CallNodeType = ECallNodeType.FunctionName;
                            }
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
                var mmv = calcMetaBase as MetaMemberVariable;
                if (mv != null)
                {
                    if (mmv != null)
                    {
                        m_CallNodeType = ECallNodeType.MemberVariableName;

                        if (isFirst)
                        {
                            Console.WriteLine("Error 类成员变量" + (calcMetaBase as MetaMemberVariable).allName + "不允许 非this.variable形式使用!! " + "位置: " + m_Token.ToLexemeAllString());
                            return false;
                        }
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
                    if (mv.isArray)             //arr[]
                    {
                        if (m_MetaCallNodeList.Count == 1 && this.isArray)  //arr[?]
                        {
                            MetaCallNode fmcn1 = m_MetaCallNodeList[0].finalMetaCallNode;
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
                                    m_MetaVariable = new VisitMetaVariable(tname, m_OwnerMetaClass, m_OwnerMetaFunctionBlock, mv, (fmcn1).GetMetaVariable());
                                    mv.AddMetaVariable(m_MetaVariable);
                                }
                            }
                            calcMetaBase = m_MetaVariable;

                        }
                    }
                }
                else if (calcMetaBase is MetaNamespace || calcMetaBase is MetaModule)
                {
                    m_CallNodeType = ECallNodeType.NamespaceName;
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
                        m_CallNodeType = ECallNodeType.ClassName;
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
            if (m_IsFunction)
            {
                if(inputname == mc.name )
                {
                    if( m_MetaInputParamCollection.metaParamList.Count > 0 )
                    {
                        MetaMemberFunction mmf1 = mc.GetMetaMemberConstructFunction(m_MetaInputParamCollection);
                        if( mmf1 != null )
                        {
                            return mmf1;
                        }
                    }
                    else
                    {
                        return mc;
                    }
                }
                if(m_MetaTemplateParamsCollection != null && m_MetaTemplateParamsCollection.metaTemplateParamsList.Count > 0 )
                {
                    //查找父类或子类中包含的节点
                    MetaMemberFunction mmf = mc.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(inputname, m_MetaTemplateParamsCollection, m_MetaInputParamCollection);
                    if (mmf != null)
                    {
                        return mmf;
                    }
                }
                //查找父类或子类中包含的节点
                MetaBase findMetaBase = mc?.GetMetaBaseInParentAndInChildrenMetaBaseByName(inputname);
                if (findMetaBase != null && findMetaBase is MetaClass)
                {
                    MetaClass tmc2 = findMetaBase as MetaClass;
                    if( tmc2.CompareInputTemplateList( m_MetaTemplateParamsCollection ) )
                    {
                        return findMetaBase;
                    }
                }
                //通过fileMeta查找是否有首定义字符
                findMetaBase = m_FileMetaCallNode.fileMeta.GetMetaBaseByName(inputname);
                if (findMetaBase != null)
                {
                    if (findMetaBase is MetaClass)
                    {
                        return findMetaBase;
                    }
                }
            }
            else
            {
                //查找core中的定义
                MetaBase findMetaBase = CoreMetaClassManager.GetSelfMetaClass(inputname);
                if (findMetaBase != null)
                {
                    return findMetaBase;
                }
                //查找selfModule中的定义
                findMetaBase = ModuleManager.instance.selfModule.GetChildrenMetaBaseByName(inputname);
                if (findMetaBase != null)
                {
                    return findMetaBase;
                }
                //查找父类或子类中包含的节点
                findMetaBase = mc?.GetMetaBaseInParentAndInChildrenMetaBaseByName(inputname);
                if (findMetaBase != null && !(findMetaBase is MetaMemberFunction))
                {
                    return findMetaBase;
                }
                //通过fileMeta查找是否有首定义字符
                findMetaBase = m_FileMetaCallNode.fileMeta.GetMetaBaseByName(inputname);
                if (findMetaBase != null && !(findMetaBase is MetaMemberFunction))
                {
                    return findMetaBase;
                }

                var ownerFun = m_OwnerMetaFunctionBlock?.ownerMetaFunction;

                if (ownerFun != null)
                {
                    //函数的参数是否是模版，如果是，则返回
                    var metaDefineParam = ownerFun.GetMetaDefineParamByName(inputname);
                    MetaVariable templateMetaVariable = null;
                    if (metaDefineParam != null)
                    {
                        if (metaDefineParam.IsTemplateMetaClass())
                        {
                            templateMetaVariable = metaDefineParam.metaVariable;
                            return templateMetaVariable;
                        }
                    }
                }
                //函数内成员
                MetaBase mfbMB = m_OwnerMetaFunctionBlock?.GetMetaVariableByName(inputname);

                return mfbMB;
            }
            return null;
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
                MetaFunction mf2 = mc.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect("Cast", m_MetaTemplateParamsCollection, m_MetaInputParamCollection);
                if (mf2 != null)
                {
                    return mf2;
                }
                else
                {
                    Console.WriteLine("没有找到Cast的函数，或者是重定义错误!!");
                    return null;
                }
            }
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
        public void SetInputParamCollection(MetaInputParamCollection mipc)
        {
            m_MetaInputParamCollection = mipc;
        }
        public MetaFunctionCall GetMetaFunctionCall()
        {
            if (m_CallNodeType == ECallNodeType.FunctionName)
            {
                MetaMemberFunction mmf = m_CurrentMetaBase as MetaMemberFunction;

                if (mmf != null)
                {
                    MetaFunctionCall mfc = new MetaFunctionCall(mmf.ownerMetaClass, mmf, m_MetaInputParamCollection );
                    return mfc;
                }
            }
            return null;
        }
        public MetaType GetMetaDefineType()
        {
            if (m_CallNodeType == ECallNodeType.Express)
            {
                //return m_ExpressNode.GetReturnMetaClass();
            }
            if (m_CurrentMetaBase == null) return null;

            if (m_CallNodeType == ECallNodeType.ClassName || m_CallNodeType == ECallNodeType.This
                || m_CallNodeType == ECallNodeType.ConstValue)
            {
                //return m_CurrentMetaBase as MetaClass;
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
            else if (m_CallNodeType == ECallNodeType.VariableName)
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
            m_MetaInputParamCollection.CaleReturnType();
            if (m_CallNodeType == ECallNodeType.VariableName)
            {
                MetaVariable mv = m_CurrentMetaBase as MetaVariable;
                if (mv != null && (!mv.isTemplate && mv.metaDefineType.metaClass == null))
                {
                    //Console.WriteLine("Error 未解析到:" + mv.allName + "位置在:" + mv.ToFormatString());
                    return;
                }
            }
            else if (m_CallNodeType == ECallNodeType.DataName )
            {
                MetaData md = m_CurrentMetaBase as MetaData;
                return;
            }
        }
        public MetaMemberVariable GetMetaMemeberVariable()
        {
            if ( m_CallNodeType == ECallNodeType.MemberVariableName)
            {
                MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
                return mmv;
            }
            return null;
        }
        public MetaVariable GetMetaVariable()
        {
            if (m_CallNodeType == ECallNodeType.VariableName || m_CallNodeType == ECallNodeType.MemberVariableName )
            {
                MetaVariable mv = m_CurrentMetaBase as MetaVariable;
                return mv;
            }
            else if( m_CallNodeType == ECallNodeType.DataName )
            {
                return (m_CurrentMetaBase as MetaData).metaVariable;
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

            if(m_CallNodeType == ECallNodeType.ClassName || m_CallNodeType == ECallNodeType.This
                || m_CallNodeType == ECallNodeType.ConstValue )
            {
                return m_CurrentMetaBase as MetaClass;
            }
            else if( m_CallNodeType == ECallNodeType.FunctionName )
            {
                var mmf = (m_CurrentMetaBase as MetaMemberFunction);
                if (mmf == null) return null;
                
                if( mmf.isCastFunction )
                {
                    if( !mmf.isOverrideFunction )
                    {
                        return m_MetaTemplateParamsCollection.metaTemplateParamsList[0].metaClass;
                    }
                }
                if( mmf.isConstructInitFunction )
                {
                    return mmf.ownerMetaClass;
                }
                return mmf.metaDefineType.metaClass;
            }
            else if( m_CallNodeType == ECallNodeType.VariableName )
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
            else if( m_CallNodeType == ECallNodeType.NewClass )
            {
                return m_CurrentMetaBase as MetaClass;
            }
            else if( m_CallNodeType == ECallNodeType.MemberVariableName )
            {
                MetaMemberVariable mmv = m_CurrentMetaBase as MetaMemberVariable;
                if( mmv != null )
                {
                    return mmv.metaDefineType.metaClass;
                }
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
                if (m_CallNodeType == ECallNodeType.ClassName)
                {
                    if (m_CallNodeSign == ECallNodeSign.Null)
                        sb.Append(m_CurrentMetaBase?.allNameIncludeModule);
                    else
                        sb.Append(m_CurrentMetaBase?.name);
                }
                else if( m_CallNodeType == ECallNodeType.DataName )
                {
                    sb.Append(m_CurrentMetaBase.allName);
                }
                else if(m_CallNodeType == ECallNodeType.NewClass )
                {
                    sb.Append(m_CurrentMetaBase?.name);
                }
                else if (m_CallNodeType == ECallNodeType.NamespaceName)
                {
                    if (m_CallNodeSign == ECallNodeSign.Null)
                        sb.Append(m_CurrentMetaBase?.allNameIncludeModule);
                    else
                        sb.Append(m_CurrentMetaBase?.name);
                }
                else if (m_CallNodeType == ECallNodeType.FunctionName)
                {
                    sb.Append(m_CurrentMetaBase?.name);
                    sb.Append(m_MetaTemplateParamsCollection?.ToFormatString());

                    if (m_MetaInputParamCollection != null)
                        sb.Append(m_MetaInputParamCollection.ToFormatString());
                }
                else if (m_CallNodeType == ECallNodeType.VariableName)
                {
                    if (m_CurrentMetaBase is VisitMetaVariable)
                    {
                        sb.Append(m_CurrentMetaBase?.ToFormatString());
                    }
                    else
                    {
                        sb.Append(m_CurrentMetaBase?.name);
                    }
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
                else if (m_CallNodeType == ECallNodeType.ConstValue)
                {
                    sb.Append(m_ConstValue.ToFormatString());
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
        public MetaFunctionCall metaFunctionCall => m_MetaFunctionCall;

        private FileMetaCallLink m_FileMetaCallLink;

        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private MetaCallNode m_FinalMetaCallNode = null;
        private MetaCallNode m_FirstNode = null;
        private List<MetaCallNode> m_CallNodeList = new List<MetaCallNode>();
        private MetaFunctionCall m_MetaFunctionCall = null;
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
            allowUseConst = _useConst;
            bool flag = m_FirstNode.ParseNode( _useConst );
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                if (flag)
                {
                    flag = m_CallNodeList[i].ParseNode(_useConst );
                }
            }
            m_MetaFunctionCall = finalMetaCallNode.GetMetaFunctionCall();

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
