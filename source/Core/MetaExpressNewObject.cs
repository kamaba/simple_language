﻿using System;
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
            private Token assignToken;
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

            FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax;
            public NewObjectAssignStatements(MetaBlockStatements mbs, MetaClass mc, FileMetaCallLink fmcl)
            {
                m_OwnerMetaBlockStatements = mbs;
                if (fmcl != null)
                {
                    m_MetaExpress = new MetaCallExpressNode(fmcl, mc, mbs);
                    AllowUseConst auc = new AllowUseConst();
                    auc.useNotConst = false;
                    auc.useNotStatic = false;
                    m_MetaExpress.Parse(auc);
                }
            }
            public NewObjectAssignStatements(MetaBlockStatements mbs, MetaClass mc, MetaExpressNode men)
            {
                m_OwnerMetaBlockStatements = mbs;
                m_MetaExpress = men;
            }
            public NewObjectAssignStatements(MetaBlockStatements mbs, MetaClass ownerMetaClass, FileMetaOpAssignSyntax fmos )
            {
                m_FileMetaOpAssignSyntax = fmos;
                m_OwnerMetaBlockStatements = mbs;
                if (fmos != null)
                {
                    assignToken = fmos.assignToken;
                    if (fmos.variableRef.isOnlyName)
                    {
                        m_DefineName = fmos.variableRef.name;
                        m_MetaMemberVariable = ownerMetaClass.GetMetaMemberByAllName(m_DefineName) as MetaMemberVariable;
                        if (m_MetaMemberVariable == null)
                        {
                            Console.WriteLine("Error 在类" + ownerMetaClass?.allName + "函数: " + mbs?.ownerMetaFunction.name
                                + " 没有找到: 类" + ownerMetaClass?.allName + " 变量:" + m_DefineName);
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
                    //case FileMetaTermExpress fmte:
                    //    {

                    //    }
                    //    break;
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
                sb.Append(assignToken?.lexeme.ToString());
                sb.Append(m_MetaExpress?.ToFormatString());

                return sb.ToString();
            }
        }

        public List<NewObjectAssignStatements> assignStatementsList = new List<NewObjectAssignStatements>();
        public MetaFunctionCall constructFunctionCall => m_MetaConstructFunctionCall;
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

            m_MetaDefineType = new MetaType(CoreMetaClassManager.arrayMetaClass, metaInputTemplateCollection);

            MetaInputParamCollection mdpc = new MetaInputParamCollection( ownerMC, mbs );
            String[] arr = m_FileMetaConstValueTerm.name.Split("..");
            if( arr.Length == 2 )
            {
                int arr0 = int.Parse(arr[0]);
                MetaConstExpressNode mcen1 = new MetaConstExpressNode(EType.Int32, arr0 );
                NewObjectAssignStatements mas1 = new NewObjectAssignStatements(mbs, CoreMetaClassManager.objectMetaClass, mcen1);
                assignStatementsList.Add(mas1);

                MetaConstExpressNode mcen_ = new MetaConstExpressNode(EType.String, "-" );
                NewObjectAssignStatements mas_ = new NewObjectAssignStatements(mbs, CoreMetaClassManager.objectMetaClass, mcen_);
                assignStatementsList.Add(mas_);


                int arr1 = int.Parse(arr[1]);
                MetaConstExpressNode mcen2 = new MetaConstExpressNode(EType.Int32, arr[1]);
                NewObjectAssignStatements mas2 = new NewObjectAssignStatements(mbs, CoreMetaClassManager.objectMetaClass, mcen2 );
                assignStatementsList.Add(mas2);

                if( arr0 > arr1 )
                {
                    Console.WriteLine("error 数组定义，必须前边的数字更小!!");
                }
                else
                {
                    int len = arr1 - arr0 + 1;
                    MetaInputParam mip = new MetaInputParam(new MetaConstExpressNode( EType.Int16, len ) );
                    mdpc.AddMetaInputParam(mip);
                }
            }
            var tfunction = m_MetaDefineType.GetMetaMemberConstructFunction();

            if(tfunction != null )
            {
                m_MetaConstructFunctionCall = new MetaFunctionCall(ownerMC, tfunction, mdpc);
                m_MetaConstructFunctionCall.Parse();
            }

            Init();
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
                            NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, mt.metaClass, fas);
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
                        for (int i = 0; i < m_FileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
                        {
                            var fas = m_FileMetaBraceTerm.fileMetaAssignSyntaxList[i];
                            fas.express.BuildAST();

                            NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, m_MetaDefineType.metaClass, fas);

                            assignStatementsList.Add(mas);
                        }
                    }
                }
                FileMetaParTerm fmpt = listfinalNode.fileMetaParTerm;
                if (fmpt != null)
                {
                    m_FileMetaParTerm = fmpt;
                    MetaInputParamCollection mipc = new MetaInputParamCollection(m_FileMetaParTerm, ownerMC, mbs );
                    MetaInputParam mip = new MetaInputParam(new MetaConstExpressNode(EType.Int16, assignStatementsList.Count));
                    mipc.AddMetaInputParam(mip);
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
                    NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, mt.metaClass, fas);
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
                for (int i = 0; i < m_FileMetaBraceTerm.fileMetaAssignSyntaxList.Count; i++)
                {
                    var fas = m_FileMetaBraceTerm.fileMetaAssignSyntaxList[i];
                    fas.express.BuildAST();

                    NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, mt.metaClass, fas);

                    assignStatementsList.Add(mas);
                }
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
                NewObjectAssignStatements mas = new NewObjectAssignStatements(mbs, mc, men );

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
                eType = EType.Class;
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

            if( m_MetaDefineType != null )
            {
                sb.Append(m_MetaDefineType.ToFormatString() );
            }

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
                if( mcl.finalMetaCallNode.callNodeType == ECallNodeType.NewClass )
                {
                    isNewClass = true;
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