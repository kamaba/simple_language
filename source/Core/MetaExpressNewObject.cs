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
    public class MetaNewObjectExpressNode : MetaExpressNode
    {
        public class NewObjectAssignStatements
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
            public NewObjectAssignStatements(MetaBlockStatements mbs, MetaType mt, FileMetaCallLink fmcl )
            {
                m_OwnerMetaBlockStatements = mbs;
                if (fmcl != null)
                {
                    m_MetaExpress = new MetaCallExpressNode(fmcl, mt.metaClass, mbs);
                    AllowUseConst auc = new AllowUseConst();
                    auc.useNotConst = false;
                    auc.useNotStatic = false;
                    m_MetaExpress.Parse(auc);
                }
            }
            public NewObjectAssignStatements(MetaBlockStatements mbs, MetaType mc, MetaExpressNode men)
            {
                m_OwnerMetaBlockStatements = mbs;
                m_MetaExpress = men;
            }
            public NewObjectAssignStatements(MetaBlockStatements mbs, MetaType mt, FileMetaOpAssignSyntax fmos )
            {
                m_FileMetaOpAssignSyntax = fmos;
                m_OwnerMetaBlockStatements = mbs;
                if (fmos != null)
                {
                    m_AssignToken = fmos.assignToken;
                    if (fmos.variableRef.isOnlyName)
                    {
                        m_DefineName = fmos.variableRef.name;
                        if( mt.isDynamicClass )
                        {
                            m_MetaMemberVariable = new MetaMemberVariable(null, m_DefineName);
                        }
                        else
                        {
                            m_MetaMemberVariable = mt.metaClass.GetMetaMemberByAllName(m_DefineName) as MetaMemberVariable;
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
            public NewObjectAssignStatements(MetaBlockStatements mbs, MetaType mt, FileMetaDefineVariableSyntax fmdvs)
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
                    m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberVariable, m_OwnerMetaBlockStatements, fileExpress );

                }
            }

            public MetaClass GetRetMetaClass()
            {
                if(m_MetaMemberVariable != null )
                {
                    return m_MetaMemberVariable.ownerMetaClass;
                }
                if( m_MetaExpress != null )
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
                    
                    if(m_MetaMemberVariable != null )
                    {
                        MetaClass retMetaClass = m_MetaMemberVariable.metaDefineType.metaClass;
                        MetaClass ownerMetaClass = m_MetaMemberVariable.ownerMetaClass;
                        bool m_IsNeedCastStatements = false;

                        m_MetaMemberVariable.SetMetaDefineType( m_MetaExpress.GetReturnMetaDefineType() );
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
                            MetaCallExpressNode clen = new MetaCallExpressNode(callTerm.callLink, mc, mbs );
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
                            mt.SetDynamicClass(true);
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

        public List<NewObjectAssignStatements> assignStatementsList = new List<NewObjectAssignStatements>();
        public MetaFunctionCall constructFunctionCall => m_MetaConstructFunctionCall;
        public bool isAnonymousClass => m_MetaDefineType != null ? m_MetaDefineType.isDynamicClass : false;
        public MetaClass GetMaxLevelMetaClassType()
        {
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            bool isAllSame = true;
            for (int i = 0; i < assignStatementsList.Count - 1; i++)
            {
                NewObjectAssignStatements cmc = assignStatementsList[i];
                NewObjectAssignStatements nmc = assignStatementsList[i+1];
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

        private MetaFunctionCall m_MetaConstructFunctionCall = null;
        private MetaExpressNode m_MetaEnumValue = null;

        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaBraceTerm m_FileMetaBraceTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        private FileMetaBracketTerm m_FileMetaBracketTerm = null;
        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;
        
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
                m_MetaConstructFunctionCall = new MetaFunctionCall(ownerMC, tfunction, mdpc);
                m_MetaConstructFunctionCall.Parse();
            }

            Init();
        }
        public MetaNewObjectExpressNode(FileMetaCallTerm fmct, MetaCallLink mcl, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs )
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaDefineType = new MetaType(mt);
            eType = EType.Array;
        }

        public MetaNewObjectExpressNode( MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaFunctionCall mmf )
        {
            m_MetaConstructFunctionCall = mmf;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaDefineType = new MetaType(mt);
            eType = EType.Array;
        }
        // Class1<Int32> a = Class1<Int32>( 10 ){ a = 20; } 
        // Enum1 e1 = Enum1.Val1( 20 );
        public MetaNewObjectExpressNode(FileMetaCallTerm fmct, MetaCallLink mcl, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaFunctionCall mmf )
        {
            m_FileMetaCallTerm = fmct;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaConstructFunctionCall = mmf;
            m_MetaDefineType = new MetaType( mt );
            var fmcn = mcl.finalMetaCallNode;
            if ( !m_MetaDefineType.isDefineMetaClass )
            {
                if( fmcn.callNodeType == ECallNodeType.NewClass )
                {
                    m_MetaDefineType.SetMetaClass(fmcn.GetMetaClass());
                }
            }
            if (fmcn.callNodeType == ECallNodeType.EnumDefaultValue)
            {
                m_MetaDefineType.SetEnumValue(fmcn.GetMetaMemeberVariable());
            }
            else if (fmcn.callNodeType == ECallNodeType.EnumNewValue)
            {
                m_MetaDefineType.SetEnumValue(fmcn.GetMetaMemeberVariable());
                m_MetaEnumValue = fmcn.metaExpressValue;
            }
            m_MetaDefineType.SetMetaInputTemplateCollection(fmcn.metaTemplateParamsCollection);


            var list = fmct.callLink?.callNodeList;
            if (list != null && list.Count > 0 )
            {
                var listfinalNode = list[list.Count - 1];
                FileMetaBraceTerm fmbt = listfinalNode.fileMetaBraceTerm;
                if(fmbt != null )
                {
                    m_FileMetaBraceTerm = fmbt;
                    
                    if( fmbt.isArray )
                    {
                        for (int i = 0; i < m_FileMetaBraceTerm.fileMetaCallLinkList.Count; i++)
                        {
                            var fas = m_FileMetaBraceTerm.fileMetaCallLinkList[i];
                            NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, mt, fas );
                            assignStatementsList.Add(mas);
                        }
                        var metaInputTemplateCollection = new MetaInputTemplateCollection();
                        if (assignStatementsList.Count > 0)
                        {
                            MetaClass mc = GetMaxLevelMetaClassType();
                            metaInputTemplateCollection.AddMetaTemplateParamsList(new MetaType(mc));
                        }
                        else
                        {
                            metaInputTemplateCollection.AddMetaTemplateParamsList(new MetaType(CoreMetaClassManager.objectMetaClass));
                        }
                        m_MetaDefineType.SetMetaInputTemplateCollection(metaInputTemplateCollection);
                    }
                    else
                    {
                        ParseBraceTermDefineVariable();
                    }
                }
                FileMetaParTerm fmpt = listfinalNode.fileMetaParTerm;
                if (fmpt != null)
                {
                    m_FileMetaParTerm = fmpt;
                    //MetaInputParamCollection mipc = new MetaInputParamCollection(m_FileMetaParTerm, ownerMC, mbs);
                    //MetaInputParam mip = new MetaInputParam(new MetaConstExpressNode(EType.Int16, assignStatementsList.Count));
                    //mipc.AddMetaInputParam(mip);
                }

            }
            Init();
        }
        // Class1 c = { a = 20, b = 20 };  => Class1 c = Class1(); c.a = 20; c.b = 20;
        public MetaNewObjectExpressNode( FileMetaBraceTerm fmbt, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs )
        {
            m_FileMetaBraceTerm = fmbt;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaDefineType = new MetaType(mt);

            MetaInputTemplateCollection metaInputTemplateCollection = null;
            if (mt.isArray )              //   {1,2,3,4}
            {
                for (int i = 0; i < m_FileMetaBraceTerm.fileMetaCallLinkList.Count; i++)
                {
                    var fas = m_FileMetaBraceTerm.fileMetaCallLinkList[i];
                    NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, mt, fas);
                    assignStatementsList.Add(mas);
                }
                metaInputTemplateCollection = new MetaInputTemplateCollection();
                if (assignStatementsList.Count > 0)
                {
                    MetaClass mc = GetMaxLevelMetaClassType();
                    metaInputTemplateCollection.AddMetaTemplateParamsList(new MetaType(mc));
                }
                else
                {
                    metaInputTemplateCollection.AddMetaTemplateParamsList(new MetaType(CoreMetaClassManager.objectMetaClass));
                }
                m_MetaDefineType.SetMetaInputTemplateCollection(metaInputTemplateCollection);
            }
            else                             //Class1 c = { a = 20, b = 20 };
            {
                ParseBraceTermDefineVariable();
            }
            Init();
        }
        // Class1 c = ( 1, 2 );  => Class1 c = Class1( 1, 2 );
        public MetaNewObjectExpressNode( FileMetaParTerm fmpt, MetaType mt, MetaClass mc, MetaBlockStatements mbs, MetaFunctionCall mmf)
        {
            m_FileMetaParTerm = fmpt;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaConstructFunctionCall = mmf;
            m_MetaDefineType = new MetaType(mt);

            Init();
        }
        // Array<int> arr = [1,2,3]
        public MetaNewObjectExpressNode(FileMetaBracketTerm fmbt, MetaType mt, MetaClass mc, MetaBlockStatements mbs, MetaFunctionCall mmf )
        {
            m_FileMetaBracketTerm = fmbt;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaConstructFunctionCall = mmf;
            m_MetaDefineType = new MetaType(mt);

            List<FileInputParamNode> list = new List<FileInputParamNode>();
            var splitList = fmbt.SplitParamList();
            for (int i = 0; i < splitList.Count; i++)
            {
                var fas = splitList[i];

                MetaExpressNode men = ExpressManager.instance.CreateExpressNode( mc, mbs, new MetaType( CoreMetaClassManager.int32MetaClass ), fas );
                NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, new MetaType(mc), men );

                assignStatementsList.Add(mas);
            }
            MetaClass inputType = GetMaxLevelMetaClassType();

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            MetaType mitp = new MetaType(inputType);
            metaInputTemplateCollection.AddMetaTemplateParamsList(mitp); 
            m_MetaDefineType.SetMetaInputTemplateCollection(metaInputTemplateCollection);

            /*
            MetaInputParamCollection mipc = new MetaInputParamCollection(mc, mbs);
            MetaInputParam mip = new MetaInputParam(new MetaConstExpressNode(EType.Int16, assignStatementsList.Count));
            mipc.AddMetaInputParam(mip);
            m_MetaConstructFunctionCall.SetMetaInputParamCollection(mipc);
            */
            eType = EType.Array;
        }
        private void Init()
        {
            if(m_MetaConstructFunctionCall != null )
            {
                eType = m_MetaConstructFunctionCall.function.metaDefineType.metaClass.eType;
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
        void ParseBraceTermDefineVariable()
        {
            if (m_FileMetaBraceTerm != null && m_FileMetaBraceTerm.fileMetaAssignSyntaxList != null)
            {
                if (m_MetaDefineType.metaClass == CoreMetaClassManager.objectMetaClass)
                {
                    m_MetaDefineType.SetDynamicClass(true);

                    //构建匿名类中的项
                    for (int i = 0; i < m_FileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
                    {
                        var fas = m_FileMetaBraceTerm.fileMetaAssignSyntaxList[i];
                        var foas = fas as FileMetaOpAssignSyntax;
                        var fdvs = fas as FileMetaDefineVariableSyntax;
                        NewObjectAssignStatements mas = null;
                        if (foas != null)
                        {
                            foas.express.BuildAST();
                            mas = new NewObjectAssignStatements(m_OwnerMetaBlockStatements, m_MetaDefineType, foas);
                        }
                        else if (fdvs != null)
                        {
                            fdvs.express.BuildAST();
                            mas = new NewObjectAssignStatements(m_OwnerMetaBlockStatements, m_MetaDefineType, fdvs);
                        }
                        mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }

                    MetaClass anonClass = new MetaClass("DynamicClass__" + GetHashCode());
                    for (int i = 0; i < assignStatementsList.Count; i++)
                    {
                        var mmv = assignStatementsList[i].metaMemberVariable;
                        anonClass.AddMetaMemberVariable(assignStatementsList[i].metaMemberVariable, false );
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
                        if( list.Count == assignStatementsList.Count )
                        {

                        }
                    }
                    m_MetaDefineType = new MetaType(retClass);
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
                            NewObjectAssignStatements mas = new NewObjectAssignStatements(m_OwnerMetaBlockStatements, m_MetaDefineType, foas);
                            assignStatementsList.Add(mas);
                        }
                        else 
                        {
                            Console.WriteLine("Error 该处应该是使用赋值，不能有其它表达式!!" + fas.token?.ToLexemeAllString() );
                            continue;
                        }
                    }
                }
            }
        }
        public override void Parse(AllowUseConst auc)
        {
            for( int i = 0; i < assignStatementsList.Count; i++ )
            {
                //assignStatementsList[i].Parse(isStatic, isConst, isCallFunction, isCallConstuctionFunction);
            }
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
            for (int i = 0; i < assignStatementsList.Count; i++)
            {
                assignStatementsList[i].CalcReturnType();
            }
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

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_MetaDefineType.ToFormatString() );

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
                sb.Append(m_MetaConstructFunctionCall?.ToFormatString());

                if (assignStatementsList.Count > 0)
                {
                    sb.Append("{");
                    for (int i = 0; i < assignStatementsList.Count; i++)
                    {
                        var mas = assignStatementsList[i];

                        sb.Append(mas.ToFormatString());
                        if (i < assignStatementsList.Count - 1)
                            sb.Append(", ");
                    }
                    sb.Append("}");
                }
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

                MetaFunctionCall mfc = new MetaFunctionCall(mmf.ownerMetaClass, mmf, mpc );

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
                if ( mcl.finalMetaCallNode.callNodeType == ECallNodeType.NewClass )
                {
                    isNewClass = true;
                }
                else if(mcl.finalMetaCallNode.callNodeType == ECallNodeType.EnumNewValue )
                {
                    isNewEnum = true;
                }
                var metaFunCall = mcl.metaFunctionCall;
                if (metaFunCall != null)
                {
                    if( metaFunCall.isConstruction )
                    {
                        isNewClass = true;
                    }
                }
                if(isNewClass)
                {
                    MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mcl, mt, omc, mbs, metaFunCall );

                    return mnoen;
                }
                else if( isNewEnum )
                {
                    MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mcl, mt, omc, mbs, null );

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
