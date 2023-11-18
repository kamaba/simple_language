using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public class MetaBraceAssignStatements
    {
        public MetaMemberVariable metaMemberVariable => m_MetaMemberVariable;
        public MetaExpressNode expressNode => m_MetaExpress;

        private MetaMemberVariable m_MetaMemberVariable;
        private Token m_AssignToken;
        private MetaExpressNode m_MetaExpress;
        private MetaBlockStatements m_OwnerMetaBlockStatements;
        private string m_DefineName = "";

        public int opLevel
        {
            get
            {
                return m_MetaExpress.opLevel;
            }
        }
        FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax;
        FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax;
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaType mt, FileMetaCallLink fmcl)
        {
            m_OwnerMetaBlockStatements = mbs;
            if (fmcl != null)
            {
                m_MetaExpress = new MetaCallExpressNode(fmcl, mt.metaClass, mbs);
                AllowUseConst auc = new AllowUseConst();
                auc.useNotConst = false;
                auc.useNotStatic = false;
                m_MetaExpress.Parse(auc);
                m_MetaExpress.CalcReturnType();
            }
        }
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaType mc, MetaExpressNode men)
        {
            m_OwnerMetaBlockStatements = mbs;
            m_MetaExpress = men;
        }
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaType mt, FileMetaOpAssignSyntax fmos)
        {
            m_FileMetaOpAssignSyntax = fmos;
            m_OwnerMetaBlockStatements = mbs;
            if (fmos != null)
            {
                m_AssignToken = fmos.assignToken;
                if (fmos.variableRef.isOnlyName)
                {
                    m_DefineName = fmos.variableRef.name;
                    if (mt.isDynamicClass)
                    {
                        m_MetaMemberVariable = new MetaMemberVariable(null, m_DefineName);
                    }
                    else
                    {
                        m_MetaMemberVariable = mt.metaClass.GetMetaMemberVariableByName(m_DefineName);
                        if (m_MetaMemberVariable == null)
                        {
                            Console.WriteLine("Error 在类" + mt.metaClass?.allName + "函数: " + mbs?.ownerMetaFunction.name
                                + " 没有找到: 类" + mt.metaClass?.allName + " 变量:" + m_DefineName);
                        }
                    }
                    m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberVariable, m_OwnerMetaBlockStatements, m_FileMetaOpAssignSyntax?.express);

                }
                else
                {
                    Console.WriteLine("Error 在类" + mbs.ownerMetaClass?.allName + "函数: " + mbs.ownerMetaFunction.name
                        + " 语句: " + fmos.variableRef.ToTokenString());
                }
            }
        }
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaType mt, FileMetaDefineVariableSyntax fmdvs)
        {
            m_FileMetaDefineVariableSyntax = fmdvs;
            m_OwnerMetaBlockStatements = mbs;
            if (fmdvs != null)
            {
                m_AssignToken = fmdvs.assignToken;

                m_DefineName = m_FileMetaDefineVariableSyntax.name;

                var fmcd = m_FileMetaDefineVariableSyntax.fileMetaClassDefine;
                var mdt = new MetaType(fmcd, mt.metaClass);
                m_MetaMemberVariable = new MetaMemberVariable(null, m_DefineName, mdt.metaClass);

                var fileExpress = m_FileMetaDefineVariableSyntax.express;
                m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberVariable, m_OwnerMetaBlockStatements, fileExpress);

            }
        }

        public MetaClass GetRetMetaClass()
        {
            if (m_MetaMemberVariable != null)
            {
                return m_MetaMemberVariable.ownerMetaClass;
            }
            if (m_MetaExpress != null)
            {
                return m_MetaExpress.GetReturnMetaClass();
            }
            return null;
        }
        public bool CalcReturnType()
        {
            if (m_MetaExpress != null)
            {
                m_MetaExpress.CalcReturnType();

                if (m_MetaMemberVariable != null)
                {
                    MetaClass retMetaClass = m_MetaMemberVariable.metaDefineType.metaClass;
                    MetaClass ownerMetaClass = m_MetaMemberVariable.ownerMetaClass;
                    bool m_IsNeedCastStatements = false;

                    m_MetaMemberVariable.SetMetaDefineType(m_MetaExpress.GetReturnMetaDefineType());
                }
                //ExpressManager.CalcDefineClassType(ref retMetaClass, m_MetaExpress, ownerMetaClass,
                //    m_OwnerMetaBlockStatements?.ownerMetaFunction, m_MetaVariable.name, ref m_IsNeedCastStatements);
                //差一个验证类型
            }
            else
            {
                Console.WriteLine("使用{}赋值，表达式不允许为空!!");
            }
            return true;
        }
        // 创建NewObject 即 Class c = Class(){ var1 = 1; } 的方式使用 1即生成的表达式
        public MetaExpressNode CreateExpressNodeInNewObjectStatements(MetaVariable mv, MetaBlockStatements mbs, FileMetaBaseTerm fme)
        {
            if (fme == null)
            {
                Console.WriteLine("Error !!!!!!!!!!");
                return null;
            }

            FileMetaBaseTerm curFMBT = fme;
            if (fme.left == null && fme.right == null)
            {
                if (fme is FileMetaTermExpress)
                {
                    curFMBT = (fme as FileMetaTermExpress).root;
                }
            }

            MetaClass mc = mbs?.ownerMetaClass;
            MetaClass selfMC = mv?.metaDefineType?.metaClass;
            switch (curFMBT)
            {
                case FileMetaConstValueTerm constValueTerm:
                    {
                        MetaExpressNode men = new MetaConstExpressNode(constValueTerm);

                        return men;
                    }
                case FileMetaCallTerm callTerm:
                    {
                        MetaCallExpressNode clen = new MetaCallExpressNode(callTerm.callLink, mc, mbs);
                        AllowUseConst auc = new AllowUseConst();
                        auc.useNotConst = false;
                        auc.useNotStatic = false;
                        auc.callConstructFunction = true;
                        auc.callFunction = true;
                        clen.Parse(auc);

                        return clen;
                    }
                case FileMetaBraceTerm fmbt:
                    {
                        MetaType mt = new MetaType(CoreMetaClassManager.objectMetaClass);
                        MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbt, mt, mc, mbs);

                        return mnoe;
                    }
                default:
                    {
                        Console.WriteLine("Error 暂不支持该类型的在NewObject中的解析!!");
                    }
                    break;
            }
            return null;
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_MetaMemberVariable != null)
            {
                sb.Append(m_MetaMemberVariable.name);
            }
            sb.Append(m_AssignToken?.lexeme.ToString());
            sb.Append(m_MetaExpress?.ToFormatString());

            return sb.ToString();
        }
    }
    public class MetaBraceOrBracketStatementsContent
    {
        public enum EStatementsContentType
        {
            None,
            ArrayValue,
            ClassValueAssign,
            DynamicClass,
        }
        public EStatementsContentType contentType => m_ContentType;
        public int count => m_AssignStatementsList.Count;
        public List<MetaBraceAssignStatements> assignStatementsList => m_AssignStatementsList;
        public MetaType defineMetaType => m_DefineMetaType;


        private List<MetaBraceAssignStatements> m_AssignStatementsList = new List<MetaBraceAssignStatements>();
        private FileMetaBraceTerm m_FileMetaBraceTerm = null;
        private FileMetaBracketTerm m_FileMetaBracketTerm = null;
        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private MetaType m_DefineMetaType = null;
        private EStatementsContentType m_ContentType = EStatementsContentType.None;
        public MetaBraceOrBracketStatementsContent(FileMetaBraceTerm fileMetaBraceTerm, MetaBlockStatements mbs, MetaClass mc )
        {
            m_FileMetaBraceTerm = fileMetaBraceTerm;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;
        }
        public MetaBraceOrBracketStatementsContent(FileMetaBracketTerm fileMetaBracketTerm, MetaBlockStatements mbs, MetaClass mc)
        {
            m_FileMetaBracketTerm = fileMetaBracketTerm;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;
        }
        public void SetMetaType( MetaType mt )
        {
            m_DefineMetaType = mt;
        }
        public void Parse()
        {
            if( m_FileMetaBracketTerm != null )
            {
                List<FileInputParamNode> list = new List<FileInputParamNode>();
                var splitList = m_FileMetaBracketTerm.SplitParamList();
                for (int i = 0; i < splitList.Count; i++)
                {
                    var fas = splitList[i];

                    MetaExpressNode men = ExpressManager.instance.CreateExpressNode(m_OwnerMetaClass, m_OwnerMetaBlockStatements, new MetaType(CoreMetaClassManager.int32MetaClass), fas);
                    MetaBraceAssignStatements mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    m_AssignStatementsList.Add(mas);
                }
                m_ContentType = EStatementsContentType.ArrayValue;
            }

            if (m_FileMetaBraceTerm != null )
            {
                if( m_FileMetaBraceTerm.fileMetaCallLinkList != null )
                {
                    for (int i = 0; i < m_FileMetaBraceTerm.fileMetaCallLinkList.Count; i++)
                    {
                        var fas = m_FileMetaBraceTerm.fileMetaCallLinkList[i];
                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), fas);
                        m_AssignStatementsList.Add(mas);
                    }
                    m_ContentType = EStatementsContentType.ClassValueAssign;
                }
                if( m_FileMetaBraceTerm.fileMetaAssignSyntaxList != null && m_FileMetaBraceTerm.fileMetaAssignSyntaxList.Count > 0 )
                {
                    if (m_DefineMetaType.metaClass == CoreMetaClassManager.objectMetaClass)
                    {
                        //m_MetaDefineType.SetDynamicClass(true);  ??

                        //构建匿名类中的项
                        for (int i = 0; i < m_FileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
                        {
                            var fas = m_FileMetaBraceTerm.fileMetaAssignSyntaxList[i];
                            var foas = fas as FileMetaOpAssignSyntax;
                            var fdvs = fas as FileMetaDefineVariableSyntax;
                            MetaBraceAssignStatements mas = null;
                            if (foas != null)
                            {
                                foas.express.BuildAST();
                                mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, m_DefineMetaType, foas);
                            }
                            else if (fdvs != null)
                            {
                                fdvs.express.BuildAST();
                                mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, m_DefineMetaType, fdvs);
                            }
                            mas.CalcReturnType();
                            assignStatementsList.Add(mas);
                        }

                        MetaDynamicClass anonClass = new MetaDynamicClass("DynamicClass__" + GetHashCode());
                        for (int i = 0; i < assignStatementsList.Count; i++)
                        {
                            var mmv = assignStatementsList[i].metaMemberVariable;
                            anonClass.AddMetaMemberVariable(assignStatementsList[i].metaMemberVariable, false);
                        }
                        MetaClass retClass = ClassManager.instance.FindDynamicClass(anonClass);
                        if (retClass == null)
                        {
                            for (int i = 0; i < assignStatementsList.Count; i++)
                            {
                                var mmv = assignStatementsList[i].metaMemberVariable;
                                mmv.SetOwnerMetaClass(retClass);
                            }
                            ClassManager.instance.AddDynamicClass(anonClass);
                            retClass = anonClass;
                        }
                        else
                        {
                            var list = anonClass.allMetaMemberVariableList;
                            if (list.Count == assignStatementsList.Count)
                            {

                            }
                        }
                        m_DefineMetaType = new MetaType(retClass);
                        m_ContentType = EStatementsContentType.DynamicClass;
                    }
                    else
                    {
                        for (int i = 0; i < m_FileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
                        {
                            var fas = m_FileMetaBraceTerm.fileMetaAssignSyntaxList[i];
                            var foas = fas as FileMetaOpAssignSyntax;
                            if (foas != null)
                            {
                                foas.express.BuildAST();
                                MetaBraceAssignStatements mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, m_DefineMetaType, foas);
                                assignStatementsList.Add(mas);
                            }
                            else
                            {
                                Console.WriteLine("Error 该处应该是使用赋值，不能有其它表达式!!" + fas.token?.ToLexemeAllString());
                                continue;
                            }
                        }
                        m_ContentType = EStatementsContentType.ClassValueAssign;
                    }
                }
            }
        }
        public MetaClass GetMaxLevelMetaClassType()
        {
            //这个函数要处理 [1,2,3] 相同情况的类型    [1, 2.3f, 3.0d] 相同数字的最大类型确定 
            // [1UL, 2.3f] 这种情况，刚都按object处理  [1,"123", 3.0f] 不相同时 结果是object
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            bool isAllSame = true;
            for (int i = 0; i < m_AssignStatementsList.Count - 1; i++)
            {
                MetaBraceAssignStatements cmc = m_AssignStatementsList[i];
                MetaBraceAssignStatements nmc = m_AssignStatementsList[i + 1];
                if (cmc.opLevel == nmc.opLevel)
                {
                    if (cmc.opLevel == 10)
                    {
                        var cur = cmc.GetRetMetaClass();
                        var next = nmc.GetRetMetaClass();
                        var relation = ClassManager.ValidateClassRelationByMetaClass(cur, next);
                        if (relation == ClassManager.EClassRelation.Same
                            || relation == ClassManager.EClassRelation.Child)
                        {
                            mc = next;
                        }
                        else if (relation == ClassManager.EClassRelation.Parent)
                        {
                            mc = cur;
                        }
                        else
                        {
                            isAllSame = false;
                            break;
                        }
                    }
                    else
                    {
                        mc = cmc.GetRetMetaClass();
                        isAllSame = true;
                    }

                }
                else
                {
                    if (cmc.opLevel > nmc.opLevel)
                    {
                        mc = cmc.GetRetMetaClass();
                    }
                    else
                    {
                        mc = nmc.GetRetMetaClass();
                    }
                }
            }
            return mc;
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_AssignStatementsList.Count > 0)
            {
                sb.Append("{");
                for (int i = 0; i < m_AssignStatementsList.Count; i++)
                {
                    var mas = m_AssignStatementsList[i];

                    sb.Append(mas.ToFormatString());
                    if (i < m_AssignStatementsList.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
            }
            return sb.ToString();
        }
    }
    public class MetaNewObjectExpressNode : MetaExpressNode
    {
        public MetaMethodCall constructFunctionCall => m_MetaConstructFunctionCall;
        public MetaBraceOrBracketStatementsContent metaBraceOrBracketStatementsContent => m_MetaBraceOrBracketStatementsContent;

        private MetaMethodCall m_MetaConstructFunctionCall = null;
        private MetaExpressNode m_MetaEnumValue = null;
        private MetaBraceOrBracketStatementsContent m_MetaBraceOrBracketStatementsContent = null;

        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;

        // 1..x
        public MetaNewObjectExpressNode(FileMetaConstValueTerm arrayLinkToken, MetaClass ownerMC, MetaBlockStatements mbs )
        {
            m_FileMetaConstValueTerm = arrayLinkToken;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
            metaInputTemplateCollection.AddMetaTemplateParamsList(mitp);

            m_MetaDefineType = new MetaType(CoreMetaClassManager.rangeMetaClass, metaInputTemplateCollection);

            MetaInputParamCollection mdpc = new MetaInputParamCollection( ownerMC, mbs );
            String[] arr = m_FileMetaConstValueTerm.name.Split("..");
            if (arr.Length == 2)
            {
                int arr0 = 0;
                if (int.TryParse(arr[0], out arr0))
                {
                    MetaConstExpressNode mcen1 = new MetaConstExpressNode(EType.Int32, arr0);
                    MetaInputParam mip = new MetaInputParam(mcen1);
                    mdpc.AddMetaInputParam(mip);
                }
                else
                {
                    //处理前边定义过的变量
                }

                int arr1 = 0;
                if (int.TryParse(arr[1], out arr1))
                {
                    MetaConstExpressNode mcen2 = new MetaConstExpressNode(EType.Int32, arr[1]);
                    MetaInputParam mip2 = new MetaInputParam(mcen2);
                    mdpc.AddMetaInputParam(mip2);
                }
                else
                {
                    //处理前边定义过的变量
                }

                MetaInputParam mip3 = new MetaInputParam(new MetaConstExpressNode(EType.Int32, 1 ));
                mdpc.AddMetaInputParam(mip3);
            }
            var tfunction = m_MetaDefineType.GetMetaMemberConstructFunction(mdpc);

            if(tfunction != null )
            {
                m_MetaConstructFunctionCall = new MetaMethodCall(ownerMC, tfunction, mdpc);
                m_MetaConstructFunctionCall.Parse();
            }

            Init();
        }
        //public MetaNewObjectExpressNode(FileMetaCallTerm fmct, MetaCallLink mcl, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs )
        //{
        //    m_OwnerMetaClass = ownerMC;
        //    m_OwnerMetaBlockStatements = mbs;
        //    m_MetaDefineType = new MetaType(mt);
        //    eType = EType.Array;
        //}
        public MetaNewObjectExpressNode( MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaMethodCall mmf )
        {
            m_MetaConstructFunctionCall = mmf;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaDefineType = new MetaType(mt);
            Init();
        }
        // Class1<Int32> a = Class1<Int32>( 10 ){ a = 20; } 
        // Enum1 e1 = Enum1.Val1( 20 );
        public MetaNewObjectExpressNode(FileMetaCallTerm fmct, MetaCallLink mcl, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaMethodCall mmf )
        {
            m_FileMetaCallTerm = fmct;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaConstructFunctionCall = mmf;
            m_MetaDefineType = new MetaType( mt );
            var fmcn =  mcl.finalCallNode;

            bool needByFileMetaParTermSetTemplate = false;
            MetaInputTemplateCollection mitc = new MetaInputTemplateCollection();
            MetaClass createMC = null;
            if (!m_MetaDefineType.isDefineMetaClass)
            {
                if (fmcn.visitType == MetaVisitNode.EVisitType.NewMethodCall )
                {
                    m_MetaDefineType.SetMetaClass(fmcn.methodCall.function.ownerMetaClass);
                }
            }
            //if (fmcn.callNodeType == ECallNodeType.EnumDefaultValue)
            //{
            //    m_MetaDefineType.SetEnumValue(fmcn.GetMetaMemeberVariable());
            //}
            //else if (fmcn.callNodeType == ECallNodeType.EnumNewValue)
            //{
            //    m_MetaDefineType.SetEnumValue(fmcn.GetMetaMemeberVariable());
            //    m_MetaEnumValue = fmcn.metaExpressValue;
            //}
            else if (fmcn.visitType ==  MetaVisitNode.EVisitType.MethodCall )
            {
                createMC = mmf.function.ownerMetaClass;
                m_MetaDefineType.SetMetaClass(createMC);
                if (createMC.metaTemplateList.Count > 0)
                {
                    //if (fmcn.metaTemplateParamsCollection == null)
                    //{
                    //    needByFileMetaParTermSetTemplate = true;
                    //}
                }
            }

            //if (fmcn != null && fmcn.metaBraceStatementsContent != null)
            //{
            //    m_MetaBraceOrBracketStatementsContent = fmcn.metaBraceStatementsContent;
            //}
            var list = fmct.callLink?.callNodeList;
            if (list != null && list.Count > 0)
            {
                Console.WriteLine("Error 待测试!!!");
                var listfinalNode = list[list.Count - 1];
                FileMetaBraceTerm fmbt = listfinalNode.fileMetaBraceTerm;
                if (fmbt != null)
                {
                    m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fmbt, m_OwnerMetaBlockStatements, m_OwnerMetaClass);
                    m_MetaBraceOrBracketStatementsContent.Parse();
                    if (fmbt.isArray)
                    {
                        var metaInputTemplateCollection = new MetaInputTemplateCollection();
                        MetaClass mc = m_MetaBraceOrBracketStatementsContent.GetMaxLevelMetaClassType();
                        metaInputTemplateCollection.AddMetaTemplateParamsList(new MetaType(mc));

                        m_MetaDefineType.SetRawMetaClass(CoreMetaClassManager.arrayMetaClass);

                        //if (fmcn.metaTemplateParamsCollection == null)
                        //{
                        //    m_MetaDefineType.SetMetaInputTemplateCollection(metaInputTemplateCollection);
                        //}
                        //else
                        //{
                        //    m_MetaDefineType.SetMetaInputTemplateCollection(fmcn.metaTemplateParamsCollection);
                        //}
                        m_MetaDefineType.UpdateMetaClassByRawMetaClassAndInputTemplateCollection();
                    }
                }
                FileMetaParTerm fmpt = listfinalNode.fileMetaParTerm;
                if (fmpt != null)
                {
                    Console.WriteLine("Error 待测试!!!");
                    m_FileMetaParTerm = fmpt;
                    if (needByFileMetaParTermSetTemplate)
                    {
                        List<MetaClass> mtList = new List<MetaClass>();
                        MetaInputParamCollection mipc = new MetaInputParamCollection(m_FileMetaParTerm, ownerMC, mbs);
                        for (int i = 0; i < mipc.count; i++)
                        {
                            var mp = mipc.metaParamList[i] as MetaInputParam;
                            mp.CaleReturnType();
                            mtList.Add(mp.GetRetMetaClass());
                        }
                        if (createMC == CoreMetaClassManager.rangeMetaClass)
                        {
                            bool isSame = true;
                            for (int i = 0; i < mtList.Count - 1; i++)
                            {
                                var curMc = mtList[i];
                                var nextMc = mtList[i + 1];
                                if (curMc != nextMc)
                                {
                                    isSame = false;
                                    break;
                                }
                            }
                            if (isSame)
                            {
                                var mitc1 = new MetaInputTemplateCollection();
                                mitc1.AddMetaTemplateParamsList(new MetaType(mtList[0]));
                                m_MetaDefineType.SetMetaInputTemplateCollection(mitc1);
                            }
                        }
                    }
                    //MetaInputParam mip = new MetaInputParam(new MetaConstExpressNode(EType.Int16, assignStatementsList.Count));
                    //mipc.AddMetaInputParam(mip);
                }

            }
            Init();
        }
        // Class1 c = { a = 20, b = 20 };  => Class1 c = Class1(); c.a = 20; c.b = 20;
        public MetaNewObjectExpressNode( FileMetaBraceTerm fmbt, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs )
        {
            m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fmbt, m_OwnerMetaBlockStatements, m_OwnerMetaClass );
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaDefineType = new MetaType(mt);
            m_MetaBraceOrBracketStatementsContent.SetMetaType(m_MetaDefineType);
            m_MetaBraceOrBracketStatementsContent.Parse();

            if( m_MetaBraceOrBracketStatementsContent.contentType == MetaBraceOrBracketStatementsContent.EStatementsContentType.DynamicClass )
            {
                m_MetaDefineType = m_MetaBraceOrBracketStatementsContent.defineMetaType;
            }
            else if( m_MetaBraceOrBracketStatementsContent.contentType == MetaBraceOrBracketStatementsContent.EStatementsContentType.ArrayValue )
            {
                var metaInputTemplateCollection = new MetaInputTemplateCollection();
                MetaClass mc = m_MetaBraceOrBracketStatementsContent.GetMaxLevelMetaClassType();
                metaInputTemplateCollection.AddMetaTemplateParamsList(new MetaType(mc));
                m_MetaDefineType = new MetaType(CoreMetaClassManager.arrayMetaClass, metaInputTemplateCollection);
            }
            Init();
        }
        // Class1 c = ( 1, 2 );  => Class1 c = Class1( 1, 2 );
        public MetaNewObjectExpressNode( FileMetaParTerm fmpt, MetaType mt, MetaClass mc, MetaBlockStatements mbs, MetaMethodCall mmf)
        {
            m_FileMetaParTerm = fmpt;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaConstructFunctionCall = mmf;
            m_MetaDefineType = new MetaType(mt);

            Init();
        }
        // Array<int> arr = [1,2,3]
        public MetaNewObjectExpressNode( FileMetaBracketTerm fmbt, MetaClass mc, MetaBlockStatements mbs )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fmbt, m_OwnerMetaBlockStatements, m_OwnerMetaClass);
            m_MetaBraceOrBracketStatementsContent.Parse();
            MetaClass inputType = m_MetaBraceOrBracketStatementsContent.GetMaxLevelMetaClassType();

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            MetaType mitp = new MetaType(inputType);
            metaInputTemplateCollection.AddMetaTemplateParamsList(mitp);
            m_MetaDefineType = new MetaType(CoreMetaClassManager.arrayMetaClass, metaInputTemplateCollection);

            MetaInputParamCollection mipc = new MetaInputParamCollection( mc, mbs );
            mipc.AddMetaInputParam(new MetaInputParam(new MetaConstExpressNode(EType.Int32, m_MetaBraceOrBracketStatementsContent.count) ) );
            MetaMemberFunction mmf = m_MetaDefineType.metaClass.GetMetaMemberConstructFunction(mipc);

            m_MetaConstructFunctionCall = new MetaMethodCall(m_MetaDefineType.metaClass, mmf, mipc );

            eType = EType.Array;
        }
        private void Init()
        {
            if(m_MetaConstructFunctionCall != null )
            {
                eType = m_MetaDefineType.metaClass.eType;
            }
            else
            {
                if(m_MetaDefineType.isEnum )
                {
                    eType = EType.Enum;
                }
                else if( m_MetaDefineType.isData )
                {
                    eType = EType.Data;
                }
                else
                {
                    eType = EType.Class;
                }
            }
        }
        public override Token GetToken()
        {
            if(m_FileMetaConstValueTerm  != null)
            {
                return m_FileMetaConstValueTerm.token;
            }
            return base.GetToken();
        }
        public override void Parse(AllowUseConst auc)
        {
            //for( int i = 0; i < assignStatementsList.Count; i++ )
            //{
            //    //assignStatementsList[i].Parse(isStatic, isConst, isCallFunction, isCallConstuctionFunction);
            //}
        }
        public override int CalcParseLevel(int level)
        {
            //for (int i = 0; i < assignStatementsList.Count; i++)
            //{
            //    var mas = assignStatementsList[i];
            //    level = mas.CalcParseLevel(level);
            //}
            return level;
        }
        public override void CalcReturnType()
        {
            base.CalcReturnType();
            //for (int i = 0; i < assignStatementsList.Count; i++)
            //{
            //    assignStatementsList[i].CalcReturnType();
            //}
        }
        public override MetaType GetReturnMetaDefineType()
        {
            if(m_MetaDefineType != null )
            {
                return m_MetaDefineType;
            }
            if (m_MetaConstructFunctionCall != null)
            {
                m_MetaDefineType = m_MetaConstructFunctionCall.GeMetaDefineType();
            }
            return m_MetaDefineType;
        }
        public override string ToTokenString()
        {
            return base.ToTokenString();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();


            if( m_MetaDefineType.isEnum )
            {
                sb.Append(m_MetaDefineType.allName);
                sb.Append(".");
                sb.Append(m_MetaDefineType.enumValue.name);
                if(m_MetaEnumValue != null)
                {
                    sb.Append("(");
                    sb.Append(m_MetaEnumValue.ToFormatString());
                    sb.Append(")");
                }
            }
            else
            {
                if ( m_MetaDefineType != null )
                {
                    //sb.Append(m_MetaDefineType.allName + "()");
                }
                sb.Append(m_MetaConstructFunctionCall?.ToFormatString());

                sb.Append(m_MetaBraceOrBracketStatementsContent?.ToFormatString());
            }

            return sb.ToString();
        }
        /// <summary>
        ///  Class2 c = ( 1, 2 );
        /// </summary>
        /// <param name="root"></param>
        /// <param name="mc"></param>
        /// <param name="mbs"></param>
        /// <param name="selfMc"></param>
        /// <returns></returns>
        public static MetaNewObjectExpressNode CreateNewObjectExpressNodeByPar(FileMetaParTerm root, MetaType mt, MetaClass omc, MetaBlockStatements mbs)
        {
            var fmct = (root as FileMetaParTerm);
            if (fmct == null) return null;
            if (mt == null) return null;

            MetaInputParamCollection mpc = new MetaInputParamCollection(root, omc, mbs);

            if( mpc.metaParamList.Count > 0 )
            {
                MetaMemberFunction mmf = mt.GetMetaMemberConstructFunction(mpc);

                if (mmf == null) return null;

                MetaMethodCall mfc = new MetaMethodCall(mmf.ownerMetaClass, mmf, mpc );

                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mt, omc, mbs, mfc);

                return mnoen;

            }
            else
            {
                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mt, omc, mbs, null );

                return mnoen;
            }
        }
        public static MetaNewObjectExpressNode CreateNewObjectExpressNodeByCall(FileMetaCallTerm root, MetaType mt, MetaClass omc, MetaBlockStatements mbs, AllowUseConst auc )
        {
            var fmct = (root as FileMetaCallTerm);
            if (fmct == null) return null;

            if (fmct != null && fmct.callLink != null && fmct.callLink.callNodeList.Count > 0 )
            {
                FileMetaCallNode finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                MetaCallLink mcl = new MetaCallLink(fmct.callLink, omc, mbs );
                if (!mcl.Parse(auc)) return null;
                mcl.CalcReturnType();
                bool isNewClass = false;
                bool isNewEnum = false;
                if ( mcl.finalCallNode?.visitType ==  MetaVisitNode.EVisitType.NewMethodCall )
                {
                    isNewClass = true;
                }
                //else if (mcl.finalMetaCallNode.callNodeType == ECallNodeType.EnumNewValue)
                //{
                //    isNewEnum = true;
                //}
                if (mcl.finalCallNode.methodCall != null)
                {
                    if ( (mcl.finalCallNode.methodCall.function as MetaMemberFunction).isConstructInitFunction )
                    {
                        isNewClass = true;
                    }
                }
                if (isNewClass)
                {
                    MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mcl, mt, omc, mbs, mcl.finalCallNode.methodCall );

                    return mnoen;
                }
                else if (isNewEnum)
                {
                    MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mcl, mt, omc, mbs, null);

                    return mnoen;
                }
            }

            return null;
        }       
    }
    public class MetaExecuteStatementsNode : MetaExpressNode
    {
        private MetaIfStatements m_MetaIfStatements = null;
        private MetaSwitchStatements m_MetaSwitchStatements = null;
        public MetaExecuteStatementsNode( MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, MetaIfStatements ifstate)
        {
            m_MetaDefineType = mdt;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaIfStatements = ifstate;
        }
        public MetaExecuteStatementsNode(MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, MetaSwitchStatements switchstate)
        {
            m_MetaDefineType = mdt;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaSwitchStatements = switchstate;
        }
        public void UpdateTrMetaVariable(MetaVariable trmv)
        {
            if (m_MetaIfStatements != null)
            {
                m_MetaIfStatements.SetTRMetaVariable(trmv);
            }
            else if (m_MetaSwitchStatements != null)
            {
                m_MetaSwitchStatements.SetTRMetaVariable(trmv);
            }
        }
        public void SetDeep(int dp)
        {
            if (m_MetaIfStatements != null)
            {
                m_MetaIfStatements.SetDeep(dp);
            }
            else if (m_MetaSwitchStatements != null)
            {
                m_MetaSwitchStatements.SetDeep(dp);
            }
        }
        public override MetaType GetReturnMetaDefineType()
        {
            if(m_MetaDefineType != null)
            {
                return m_MetaDefineType;
            }
            if (m_MetaIfStatements != null || m_MetaSwitchStatements != null)
            {
                m_MetaDefineType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }

            return m_MetaDefineType;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if( m_MetaIfStatements != null )
            {
                sb.Append(m_MetaIfStatements.ToFormatString());
            }
            else if( m_MetaSwitchStatements != null )
            {
                sb.Append(m_MetaSwitchStatements.ToFormatString());
            }
            else
            {
                sb.Append(base.ToFormatString());
            }
            return sb.ToString();
        }
        public static MetaExecuteStatementsNode CreateMetaExecuteStatementsNodeByIfExpress( MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, FileMetaKeyIfSyntax ifStatements)
        {
            if (ifStatements == null) return null;

            MetaIfStatements newIfStatements = new MetaIfStatements(mbs, ifStatements );
            MetaExecuteStatementsNode mesn = new MetaExecuteStatementsNode(mdt, ownerMC, mbs, newIfStatements);

            return mesn;
        }
        public static MetaExecuteStatementsNode CreateMetaExecuteStatementsNodeBySwitchExpress(MetaType mdt, MetaClass ownerMC, MetaBlockStatements mbs, FileMetaKeySwitchSyntax switchStatements)
        {
            if (switchStatements == null) return null;

            MetaSwitchStatements newSwtichStatements = new MetaSwitchStatements(mbs, switchStatements);
            MetaExecuteStatementsNode mesn = new MetaExecuteStatementsNode(mdt, ownerMC, mbs, newSwtichStatements);

            return mesn;
        }
    }
}
