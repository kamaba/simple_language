using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Parse;



namespace SimpleLanguage.Core
{
    public class MetaBraceAssignStatements
    {
        public int opLevel => m_MetaExpress.opLevel;
        public MetaMemberVariable metaMemberVariable => m_MetaMemberVariable;
        public MetaMemberData metaMemberData => m_MetaMemberData;
        public MetaExpressNode expressNode => m_MetaExpress;

        private MetaMemberVariable m_MetaMemberVariable;
        private MetaMemberData m_MetaMemberData;
        private MetaExpressNode m_MetaExpress;
        private MetaBlockStatements m_OwnerMetaBlockStatements;
        private MetaType m_MetaType = null;
        private string m_DefineName;

        private Token m_AssignToken = null;
        //private FileMetaDefineVariableSyntax m_FileMetaDefineVariableSyntax = null;
        //private FileMetaOpAssignSyntax m_FileMetaOpAssignSyntax = null;
        private FileMetaSymbolTerm m_FileMetaSymbolTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        public MetaBraceAssignStatements(FileMetaCallTerm fmct, MetaType mt, MetaBlockStatements mbs )
        {
            m_MetaType = mt;
            m_FileMetaCallTerm = fmct;
            
            m_MetaExpress = new MetaCallLinkExpressNode(fmct.callLink, mt.metaClass, mbs, null);
            AllowUseSettings auc = new AllowUseSettings();
            auc.useNotConst = false;
            auc.useNotStatic = false;
            m_MetaExpress.Parse(auc);
            m_MetaExpress.CalcReturnType();            
        }
        public MetaBraceAssignStatements(FileMetaSymbolTerm fmst, MetaType mt, MetaBlockStatements mbs )
        {
            m_MetaType = mt;
            m_FileMetaSymbolTerm = fmst;
            m_OwnerMetaBlockStatements = mbs;

            if( m_MetaType.isMap )
            {
                if( fmst.symBolType != ETokenType.Colon )
                {
                    Log.AddInStructMeta(EError.None, "在Map里边，必须使用:个符号");
                    return;
                }
            }
            else
            {
                if (fmst.symBolType != ETokenType.Assign )
                {
                    Log.AddInStructMeta(EError.None, "在class或者是data里边，必须使用=个符号");
                    return;
                }
            }
            if (fmst.left is not FileMetaCallTerm fmct1)
            {
                Log.AddInStructMeta(EError.None, "在class或者是data里边，前值应该使用filemetaCallTerm");
                return;
            }

            if( fmct1.callLink.callNodeList.Count > 0 )
            {
                m_DefineName = fmct1.callLink.callNodeList[fmct1.callLink.callNodeList.Count - 1].name;
            }
            if ( mt.isDynamicData || mt.isDynamicClass )
            {
                if (mt.isDynamicClass)
                {
                    m_MetaMemberVariable = new MetaMemberVariable(null, m_DefineName);
                }
                else
                {
                    m_MetaMemberData = new MetaMemberData(mt.metaClass as MetaData, null );
                    m_MetaMemberData.SetOwnerBlockstatements(m_OwnerMetaBlockStatements);
                    m_MetaMemberData.ParseDefineMetaType();
                    m_MetaExpress = m_MetaMemberData.expressNode;
                    m_MetaMemberData.ParseMetaExpress();
                    m_MetaMemberData.ParseChildMemberData();
                }
            }
            else if( mt.isArray )
            {
            }
            else
            {
                if (mt.isData)
                {
                    m_MetaMemberData = (mt.metaClass as MetaData).GetMemberDataByName(m_DefineName);
                    if (m_MetaMemberData == null)
                    {
                        Debug.Write("Error 在类" + mt.metaClass?.allClassName + "函数: " + mbs?.ownerMetaFunction.name
                            + " 没有找到: 类" + mt.metaClass?.allClassName + " 变量:" + m_DefineName);
                    }
                    //m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberData, m_OwnerMetaBlockStatements, m_FileMetaOpAssignSyntax?.express);
                }
                else if (mt.isEnum)
                {
                    Debug.Write("-----------------------------------Enum-------------------------");
                }
                else
                {
                    m_MetaMemberVariable = mt.metaClass.GetMetaMemberVariableByName(m_DefineName);
                    if (m_MetaMemberVariable == null)
                    {
                        Debug.Write("Error 在类" + mt.metaClass?.allClassName + "函数: " + mbs?.ownerMetaFunction.name
                            + " 没有找到: 类" + mt.metaClass?.allClassName + " 变量:" + m_DefineName);
                    }
                    //m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberVariable, m_OwnerMetaBlockStatements, m_FileMetaOpAssignSyntax?.express);
                }
            }

            if(fmst.right != null )
            {
                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = fmst.right;
                cep.equalMetaVariable = m_MetaMemberVariable;
                cep.metaType = new MetaType(m_MetaMemberVariable.metaDefineType);
                cep.ownerMBS = m_OwnerMetaBlockStatements;
                cep.ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass;

                m_MetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
                m_MetaExpress.Parse(new AllowUseSettings());
            }
        }
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaType mc, MetaExpressNode men)
        {
            m_OwnerMetaBlockStatements = mbs;
            m_MetaExpress = men;

            if( m_MetaExpress is MetaArrayExpressNode maen )
            {
                m_MetaExpress = new MetaNewObjectExpressNode(maen, mc.metaClass, mbs, null);
            }
        }
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaExpressNode men, MetaMemberVariable mmv )
        {
            m_OwnerMetaBlockStatements = mbs;
            m_MetaExpress = men;
            this.m_MetaMemberVariable = mmv;
        }
        /*
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaType mt, FileMetaOpAssignSyntax fmos)
        {
            m_FileMetaOpAssignSyntax = fmos;
            m_OwnerMetaBlockStatements = mbs;
            if (fmos != null)
            {
                m_AssignToken = fmos.assignToken;
                if (fmos.variableRef.isOnlyName)
                {
                    

                }
                else
                {
                    Debug.Write("Error 在类" + mbs.ownerMetaClass?.allClassName + "函数: " + mbs.ownerMetaFunction.name
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
                var getMC = ClassManager.instance.GetMetaClassAndRegisterExptendTemplateClassInstance(mbs.ownerMetaClass, fmcd);
                var mdt = new MetaType(getMC);
                m_MetaMemberVariable = new MetaMemberVariable(null, m_DefineName, mdt.metaClass);

                var fileExpress = m_FileMetaDefineVariableSyntax.express;
                m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberVariable, m_OwnerMetaBlockStatements, fileExpress);

            }
        }
        */
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
        public MetaType GetRetMetaType()
        {
            if (m_MetaExpress != null)
            {
                return m_MetaExpress.GetReturnMetaDefineType();
            }
            return null;
        }
        public void SetDefineName( string definaname )
        {
            this.m_DefineName = definaname;
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
                    //bool m_IsNeedCastStatements = false;
                }
                else if( m_MetaMemberData != null )
                {
                    MetaClass retMetaClass = m_MetaMemberData.metaDefineType.metaClass;
                    MetaClass ownerMetaClass = m_MetaMemberData.ownerMetaClass;
                    //bool m_IsNeedCastStatements = false;
                }
                //ExpressManager.CalcDefineClassType(ref retMetaClass, m_MetaExpress, ownerMetaClass,
                //    m_OwnerMetaBlockStatements?.ownerMetaFunction, m_MetaVariable.name, ref m_IsNeedCastStatements);
                //差一个验证类型
            }
            else
            {
                Debug.Write("使用{}赋值，表达式不允许为空!!");
            }
            return true;
        }
        // 创建NewObject 即 Class c = Class(){ var1 = 1; } 的方式使用 1即生成的表达式
        /*
        public MetaExpressNode CreateExpressNodeInNewObjectStatements(MetaVariable mv, MetaBlockStatements mbs, FileMetaBaseTerm fme)
        {
            if (fme == null)
            {
                Debug.Write("Error !!!!!!!!!!");
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
                        MetaCallLinkExpressNode clen = new MetaCallLinkExpressNode(callTerm.callLink, mc, mbs, null);
                        AllowUseSettings auc = new AllowUseSettings();
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
                        MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbt, mt, mc, mbs, mv);
                        return mnoe;
                    }
                case FileMetaBracketTerm fmbt:
                    {
                        if( mv is MetaMemberData )
                        {
                            MetaType mt = new MetaType(CoreMetaClassManager.arrayMetaClass);
                            MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbt, mt, mt.metaClass, mbs, mv );
                            return mnoe;
                        }
                        else
                        {
                            Debug.Write("Error 只有在data varname = {} 支持 { cha1 = [] } 的格式,其它的表达式中不支持");
                        }
                        break;
                    }
                default:
                    {
                        Debug.Write("Error 暂不支持该类型的在NewObject中的解析!!");
                    }
                    break;
            }
            return null;
        }
        */
        
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
            DataValueAssign,
            DynamicClass,
            DynamicData,
        }
        public EStatementsContentType contentType => m_ContentType;
        public int count => m_AssignStatementsList.Count;
        public List<MetaBraceAssignStatements> assignStatementsList => m_AssignStatementsList;
        public MetaType defineMetaType => m_DefineMetaType;


        private List<MetaBraceAssignStatements> m_AssignStatementsList = new List<MetaBraceAssignStatements>();
        private MetaArrayExpressNode m_MetaArrayExpressNode = null;
        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private MetaType m_DefineMetaType = null;
        private MetaVariable m_EqualMetaVariable = null;
        private MetaData m_NewMetaData = null;
        private MetaData m_NewTempMetaData = null;
        private EStatementsContentType m_ContentType = EStatementsContentType.None;

        private FileMetaBaseTerm m_FileMetaBaseTerm = null;

        public MetaBraceOrBracketStatementsContent( MetaArrayExpressNode maen, MetaClass mc, MetaBlockStatements mbs, MetaVariable parentMt)
        {
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;
            m_EqualMetaVariable = parentMt;
            m_MetaArrayExpressNode = maen;

            for( int i = 0; i < m_MetaArrayExpressNode.metaCallArray.Count; i++ )
            {
                var men = m_MetaArrayExpressNode.metaCallArray[i];
                MetaBraceAssignStatements mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                mas.CalcReturnType();
                m_AssignStatementsList.Add(mas);
            }
            m_ContentType = EStatementsContentType.ArrayValue;
        }
        public MetaBraceOrBracketStatementsContent( FileMetaBaseTerm fmbt, MetaClass mc, MetaBlockStatements mbs, MetaVariable parentMt)
        {
            m_FileMetaBaseTerm = fmbt;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;
            m_EqualMetaVariable = parentMt;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        public void SetMetaType( MetaType mt )
        {
            m_DefineMetaType = mt;
        }
        public void Parse()
        {
            /*
            if(m_FileMetaBaseTerm is FileMetaBracketTerm fmbt  )
            {
                List<FileInputParamNode> list = new List<FileInputParamNode>();
                var splitList = fmbt.SplitParamList();
                for (int i = 0; i < splitList.Count; i++)
                {
                    var fas = splitList[i];

                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(CoreMetaClassManager.int32MetaClass);
                    cep.fme = fas;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    MetaBraceAssignStatements mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);                    
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                m_ContentType = EStatementsContentType.ArrayValue;
            }
            else 
            */
            if( m_FileMetaBaseTerm is FileMetaBraceTerm fmbt2 )
            {

            }
            if (m_FileMetaBaseTerm?.fileMetaExpressList.Count > 0)
            {
                Log.AddInStructMeta(EError.None, "解析大括号里边的内容");
                for (int i = 0; i < m_FileMetaBaseTerm.fileMetaExpressList.Count; i++)
                {
                    var fas = m_FileMetaBaseTerm.fileMetaExpressList[i];
                    HandleBraceTermNode(fas);
                }
            }
        }
        //处理在{ Node1, Node2  } 在{}大括号中的Node1, Node2 这样的节点 Node1, 可以是 aaa = 1, "aa":1, 2:33, [1,2,3] [1] 3, this.value 这样的形式
        public void HandleBraceTermNode( FileMetaBaseTerm fmbt )
        {
            if (m_DefineMetaType.isData)
            {
                //动态数据类的定义 在该行语句前直接使用 data a = { aaa = 10, bbb = 20} 这样的形式
                if (m_DefineMetaType.isDynamicData)
                {
                    string anname = "DynamicData_";
                    if (m_EqualMetaVariable != null)
                    {
                        anname = anname + m_EqualMetaVariable.name + "_";
                    }
                    if (m_FileMetaBaseTerm != null)
                    {
                        anname = anname + m_FileMetaBaseTerm.token?.path + "_" + m_FileMetaBaseTerm.token?.sourceBeginLine.ToString() + "_" + GetHashCode().ToString();
                    }

                    m_NewTempMetaData = new MetaData(anname, false, false, true);
                    if (m_EqualMetaVariable?.pingToken != null)
                    {
                        m_NewTempMetaData.AddPingToken(m_EqualMetaVariable.pingToken);
                    }
                    m_NewTempMetaData.AddPingToken(m_FileMetaBaseTerm.token);
                    m_DefineMetaType = new MetaType(m_NewTempMetaData);

                    if (fmbt is FileMetaSymbolTerm fmst)                   
                    {
                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements );
                        mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "构造动态数据实例的时候，需要 使用 命名=内容 的格式");
                        return;
                    }

                    for (int i = 0; i < assignStatementsList.Count; i++)
                    {
                        var mmv = assignStatementsList[i].metaMemberData;
                        m_NewTempMetaData.AddMetaMemberData(mmv);
                    }
                    MetaData retClass = ClassManager.instance.FindMetaData(m_NewTempMetaData);
                    if (retClass == null)
                    {
                        ClassManager.instance.AddMetaData(m_NewTempMetaData);
                        retClass = m_NewTempMetaData;
                    }
                    m_NewMetaData = retClass;
                    for (int i = 0; i < assignStatementsList.Count; i++)
                    {
                        var mmv = assignStatementsList[i].metaMemberData;
                        //mmv.metaDefineType.SetRawMetaClass(m_NewMetaData);
                        //mmv.metaDefineType.SetMetaClass(m_NewMetaData);
                    }
                    m_DefineMetaType.SetMetaClass(m_NewMetaData);
                    m_DefineMetaType.SetTemplateMetaClass(m_NewMetaData);
                    m_ContentType = EStatementsContentType.DynamicData;
                    m_EqualMetaVariable?.SetMetaDefineType(m_DefineMetaType);
                }
                else
                {
                    //固定数据类赋值 在该行语句前直接使用 data a{ aaa = 10; bbb = 20 }  a = { aaa = 10, bbb = 20} 这样的形式 前边data 已经定义过了
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements);
                        mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "构造动态数据实例的时候，需要 使用 命名=内容 的格式");
                        return;
                    }
                    m_ContentType = EStatementsContentType.DataValueAssign;
                }
            }
            else if (m_DefineMetaType.isArray)// 数组类型的处理
            {
                m_ContentType = EStatementsContentType.ArrayValue;
                m_DefineMetaType.SetArrayDimension(1);

                if (fmbt is FileMetaBracketTerm fmst)
                {
                    for (int i = 0; i < fmst.fileMetaExpressList.Count; i++)
                    {
                        var fmstc = fmst.fileMetaExpressList[i];

                        MetaBraceAssignStatements mas = null;
                        if ( fmstc is FileMetaBraceTerm fmbt2 )
                        {
                            MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbt2, m_DefineMetaType, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_EqualMetaVariable);
                            mnoe.Parse(new AllowUseSettings());
                            mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, mnoe, m_EqualMetaVariable as MetaMemberVariable);
                        }
                        else if( fmstc is FileMetaBracketTerm fmbt3 )
                        {
                            MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbt3, m_DefineMetaType, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_EqualMetaVariable);
                            mnoe.Parse(new AllowUseSettings());
                            mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, mnoe, m_EqualMetaVariable as MetaMemberVariable);
                        }
                        else if (fmbt is FileMetaConstValueTerm fmcvt)
                        {
                            CreateExpressParam cep = new CreateExpressParam();
                            cep.ownerMetaClass = m_OwnerMetaClass;
                            cep.ownerMBS = m_OwnerMetaBlockStatements;
                            cep.metaType = new MetaType(CoreMetaClassManager.int32MetaClass);
                            cep.fme = fmcvt;
                            cep.equalMetaVariable = m_EqualMetaVariable;
                            MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                            men.Parse(new AllowUseSettings());
                            mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                            mas.CalcReturnType();
                            m_AssignStatementsList.Add(mas);
                        }
                        else if( fmstc is FileMetaCallTerm valuefmct)
                        {
                            mas = new MetaBraceAssignStatements(valuefmct, m_DefineMetaType, m_OwnerMetaBlockStatements );
                        }
                        else if (fmstc != null && fmstc.token.type == ETokenType.Comma)
                        {
                            continue;
                            //if (isComma)
                            //{
                            //    Log.AddInStructFileMeta(EError.None, "Error 多重逗号，导致解析无法解析!!");
                            //    break;
                            //}
                            //if (fmbtList.Count == 0)
                            //{
                            //    Log.AddInStructFileMeta(EError.None, "Error 首符号不能为逗号");
                            //    break;
                            //}
                            //isComma = true;
                            //fmbtListList.Add(fmbtList);
                            //fmbtList = new List<FileMetaBaseTerm>();
                        }
                        else
                        {
                            Log.AddInStructMeta(EError.None, "Error 不允许的表达式类形在 a = {} 这种的形式里边");
                        }
                        mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                }
                else if (fmbt is FileMetaConstValueTerm fmcvt)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType( m_DefineMetaType.metaClass );
                    cep.fme = fmcvt;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    men.CalcReturnType();
                    MetaType mt2 = men.GetReturnMetaDefineType();
                    if ( !mt2.metaClass.IsContainMetaClass( m_DefineMetaType.metaClass ) )
                    {
                        Log.AddInStructMeta(EError.None, "里边的元素与外边定义的类，不对应，需要调整数据，或者是定义的结构 "); 
                    }
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if( fmbt is FileMetaSymbolTerm fmst2 )
                {
                    if( fmst2.symBolType != ETokenType.Comma )
                    {
                        Log.AddInStructMeta(EError.None, "间隔符号不对,应该使用,");
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error 在数组里边应该是FileMetaBracketTerm 类型!");
                }
            }
            else if (m_DefineMetaType.isMap)   // 映射类型的处理 使用   a:10, b:20  20:"aa" 这样的形式
            {
                if (fmbt is FileMetaSymbolTerm fmst)
                {
                    MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements);
                    mas.CalcReturnType();
                    assignStatementsList.Add(mas);
                    m_ContentType = EStatementsContentType.ClassValueAssign;
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "构造动态数据实例的时候，需要 使用 命名=内容 的格式");
                    return;
                }
            }
            else
            {
                //动态普通类的定义
                if (m_DefineMetaType.isDynamicClass)
                {
                    MetaDynamicClass anonClass = new MetaDynamicClass("DynamicClass__" + GetHashCode());
                    //构建匿名类中的项
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        var mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements);                        
                        mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "构造动态数据实例的时候，需要 使用 命名=内容 的格式");
                        return;
                    }
                    /*
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
                    */

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
                else// 普通类赋值处理
                {
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        var mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements);                        
                        mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "构造动态数据实例的时候，需要 使用 命名=内容 的格式");
                        return;
                    }
                    m_ContentType = EStatementsContentType.ClassValueAssign;
                }
            }            
        }
        public MetaClass GetMaxLevelMetaClassType()
        {
            //这个函数要处理 [1,2,3] 相同情况的类型    [1, 2.3f, 3.0d] 相同数字的最大类型确定 
            // [1UL, 2.3f] 这种情况，刚都按object处理  [1,"123", 3.0f] 不相同时 结果是object
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool isAllSame = true;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值

            int frontOpLevel = 0;
            for (int i = 0; i < m_AssignStatementsList.Count - 1; i++)
            {
                MetaBraceAssignStatements cmc = m_AssignStatementsList[i];
                MetaBraceAssignStatements nmc = m_AssignStatementsList[i + 1];

                var cmcmt = cmc.GetRetMetaType();
                var nmcmt = nmc.GetRetMetaType();
                if( cmcmt.isArray && nmcmt.isArray && frontOpLevel < nmc.opLevel )
                {
                    mc = CoreMetaClassManager.arrayMetaClass;
                    frontOpLevel = nmc.opLevel;
                }
                else
                {
                    if (cmc.opLevel == nmc.opLevel && nmc.opLevel > frontOpLevel)
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
                                frontOpLevel = cmc.opLevel;
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
                            frontOpLevel = cmc.opLevel;
                            isAllSame = true;
                        }

                    }
                    else
                    {
                        if (nmc.opLevel > frontOpLevel)
                        {
                            if (cmc.opLevel > nmc.opLevel)
                            {
                                frontOpLevel = cmc.opLevel;
                                mc = cmc.GetRetMetaClass();
                            }
                            else
                            {
                                frontOpLevel = nmc.opLevel;
                                mc = nmc.GetRetMetaClass();
                            }
                        }
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
    public sealed class MetaNewObjectExpressNode : MetaExpressNode
    {
        public enum ENewType
        {
            DefaultType, //int32,uint32/string/..
            CommomClass,  //define class
            ArrayClass,     // array class
            ListClass,
            MapClass,
        }

        public bool needInitMemberVariable => m_NeedInitMemberVariable;
        public ENewType newType => m_NewType;
        public int arrayLength => m_ArrayLength;
        public List<MetaExpressNode> metaInputParamList => m_MetaInputParamList;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public MetaBraceOrBracketStatementsContent metaBraceOrBracketStatementsContent => m_MetaBraceOrBracketStatementsContent;

        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        private List<FileMetaBraceTerm> m_FileMetaBraceTermList = new List<FileMetaBraceTerm>();
        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;

        private MetaExpressNode m_MetaEnumValue = null;
        private MetaBraceOrBracketStatementsContent m_MetaBraceOrBracketStatementsContent = null;
        private ENewType m_NewType = ENewType.CommomClass;
        private int m_ArrayLength = 0;
        private int m_ArrayDimension = 0;
        private bool m_NeedInitMemberVariable = true;


        protected MetaVariable m_StoreMetaVariable = null; //模板或者是调用时的函数        
        protected MetaMemberFunction m_MetaMemberFunction = null;
        protected List<MetaExpressNode> m_MetaInputParamList = new List<MetaExpressNode>();

        // Class1(10){ c1 = 20, c2 = 30 }  int[2][]{ [1,2,3], [3,4,5] }
        public MetaNewObjectExpressNode( MetaCallLinkExpressNode mcen )
        {
            m_OwnerMetaClass = mcen.ownerMetaClass;
            m_OwnerMetaBlockStatements = mcen.ownerMetaBlockStatements;
            m_StoreMetaVariable = mcen.GetMetaVariable();
            m_MetaMemberFunction = mcen.metaCallLink.finalCallNode.methodCall.function as MetaMemberFunction;
            m_MetaType = mcen.metaCallLink.finalCallNode.callMetaType;
            
            if( mcen.metaCallLink.finalCallNode.callMetaType.isArray )
            {
                m_NewType = ENewType.ArrayClass;
                var lastNode = mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1];
                m_ArrayDimension = mcen.metaCallLink.finalCallNode.callMetaType.arrayDimension;
                var fma = lastNode.fileMetaBraceTerm;
                m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fma, m_OwnerMetaClass,
                    m_OwnerMetaBlockStatements,  m_StoreMetaVariable );

                HnaldeArrayType(lastNode);
                m_MetaBraceOrBracketStatementsContent.SetMetaType(m_MetaType);

            }
            else
            {
                m_NewType = ENewType.CommomClass;
                if( mcen.metaCallLink.callNodeList.Count > 0 )
                {
                    var fma = mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1].fileMetaBraceTerm;
                    m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fma, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    m_MetaBraceOrBracketStatementsContent.SetMetaType(m_MetaType);
                }
            }
            //if(mcen.metaCallLink.callNodeList.Count > 0 )
            //{
            //    m_FileMetaBraceTerm = mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1].fileMetaBraceTerm;
            //}

            //if (m_FileMetaBraceTerm != null)  //可以使用  ArrClass(){ x = ??} 的方式
            //{
            //    //if (EParseFrom.InputParamExpress)
            //    //{
            //    //    Log.AddInStructMeta(EError.None, "Error 在InputParam 里边，构建函数，只允许 使用ClassName() 的方式, " +
            //    //        "不允许使用 ClassName(){}的方式" + m_FileMetaCallNode.fileMetaBraceTerm.ToTokenString());
            //    //    return false;
            //    //}
            //    //m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(m_FileMetaBraceTerm, m_OwnerMetaBlockStatements, m_OwnerMetaClass, m_StoreMetaVariable );
            //}

        }
        /* 下边的要合并到上边的处理方法里边
        // Class1<Int32> a = Class1<Int32>( 10 ){ a = 20; } 
        // Enum1 e1 = Enum1.Val1( 20 );
        // c = Class1(){ a = 20, b = 20}  => 定义类
        public MetaNewObjectExpressNode(FileMetaCallTerm fmct, MetaCallLink mcl, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaMethodCall mmf)
        {
            m_FileMetaCallTerm = fmct;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            //m_MetaConstructFunctionCall = mmf;
            m_MetaType = new MetaType(mt);
            var fmcn = mcl.finalCallNode;

            bool needByFileMetaParTermSetTemplate = false;
            MetaInputTemplateCollection mitc = new MetaInputTemplateCollection();
            MetaClass createMC = null;
            if (false)//!m_MetaType.isDefineMetaClass) 此处需要检查是否使用定义的类型
            {
                if (fmcn.visitType == MetaVisitNode.EVisitType.New)
                {
                    m_MetaType.SetMetaClass(fmcn.methodCall.function.ownerMetaClass);
                }
            }
            //if (fmcn.callNodeType == ECallNodeType.EnumDefaultValue)
            //{
            //    m_MetaType.SetEnumValue(fmcn.GetMetaMemeberVariable());
            //}
            //else if (fmcn.callNodeType == ECallNodeType.EnumNewValue)
            //{
            //    m_MetaType.SetEnumValue(fmcn.GetMetaMemeberVariable());
            //    m_MetaEnumValue = fmcn.metaExpressValue;
            //}
            else if (fmcn.visitType == MetaVisitNode.EVisitType.MethodCall)
            {
                createMC = mmf.function.ownerMetaClass;
                m_MetaType.SetMetaClass(createMC);
                if (createMC.metaTemplateList.Count > 0)
                {
                    //if (fmcn.metaTemplateParamsCollection == null)
                    //{
                    //    needByFileMetaParTermSetTemplate = true;
                    //}
                }
            }

            ////if (fmcn != null && fmcn.methodCall?.instance != null)
            ////{
            ////    m_MetaBraceOrBracketStatementsContent = fmcn.metaBraceStatementsContent;
            ////}
            var list = fmct.callLink?.callNodeList;
            if (list != null && list.Count > 0)
            {
                var listfinalNode = list[list.Count - 1];
                FileMetaBraceTerm fmbt = listfinalNode.fileMetaBraceTerm;
                if (fmbt != null)
                {
                    //Debug.Write("Error 待测试!!!");
                    m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fmbt, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    m_MetaBraceOrBracketStatementsContent.SetMetaType(m_MetaType);
                    m_MetaBraceOrBracketStatementsContent.Parse();
                    var metaInputTemplateCollection = new MetaInputTemplateCollection();
                    MetaClass mc = m_MetaBraceOrBracketStatementsContent.GetMaxLevelMetaClassType();
                    metaInputTemplateCollection.AddMetaTemplateParamsList(new MetaType(mc));

                    m_MetaType.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);

                    //if (fmcn.metaTemplateParamsCollection == null)
                    //{
                    //    m_MetaType.SetMetaInputTemplateCollection(metaInputTemplateCollection);
                    //}
                    //else
                    //{
                    //    m_MetaType.SetMetaInputTemplateCollection(fmcn.metaTemplateParamsCollection);
                    //}
                    m_MetaType.UpdateMetaClassByRawMetaClassAndInputTemplateCollection();
                }
                FileMetaParTerm fmpt = listfinalNode.fileMetaParTerm;
                if (fmpt != null)
                {
                    m_FileMetaParTerm = fmpt;
                    if (needByFileMetaParTermSetTemplate)
                    {
                        Debug.Write("Error 待测试!!!");
                        List<MetaClass> mtList = new List<MetaClass>();
                        MetaInputParamCollection mipc = new MetaInputParamCollection(m_FileMetaParTerm, ownerMC, mbs);
                        for (int i = 0; i < mipc.count; i++)
                        {
                            var mp = mipc.metaInputParamList[i];
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
                                m_MetaType.AddDefineTemplateMetaType(new MetaType(mtList[0]));
                                m_MetaType.AddGenTemplateMetaType(new MetaType(mtList[0]));
                            }
                        }
                    }
                    //MetaInputParam mip = new MetaInputParam(new MetaConstExpressNode(EType.Int16, assignStatementsList.Count));
                    //mipc.AddMetaInputParam(mip);
                }

            }
            Init();
        }
        */

        // dynamic c = { c1 = 100, c2 = 200 }
        public MetaNewObjectExpressNode( MetaClass ownermc, List<MetaDynamicClass> list )
        {
            m_OwnerMetaClass = ownermc;
            m_OwnerMetaBlockStatements = null;
            m_MetaBraceOrBracketStatementsContent = null;;

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            //MetaType mitp = new MetaType(MetaDynamicClass);
            //metaInputTemplateCollection.AddMetaTemplateParamsList(mitp);
            m_MetaType = new MetaType(CoreMetaClassManager.arrayMetaClass, null, metaInputTemplateCollection);

            //MetaInputParamCollection mipc = new MetaInputParamCollection(mc, mbs);
            //mipc.AddMetaInputParam(new MetaInputParam(new MetaConstExpressNode(EType.Int32, m_MetaBraceOrBracketStatementsContent.count)));
            //MetaMemberFunction mmf = m_MetaType.metaClass.GetMetaMemberConstructFunction(mipc);

            //m_MetaConstructFunctionCall = new MetaMethodCall(m_MetaType.metaClass, mmf, mipc);
        }
        // 1..x
        public MetaNewObjectExpressNode(FileMetaConstValueTerm arrayLinkToken, MetaClass ownerMC, MetaBlockStatements mbs )
        {
            m_FileMetaConstValueTerm = arrayLinkToken;
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
            metaInputTemplateCollection.AddMetaTemplateParamsList(mitp);

            m_MetaType = new MetaType(CoreMetaClassManager.rangeMetaClass, null, metaInputTemplateCollection);

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
            var tfunction = m_MetaType.GetMetaMemberConstructFunction(mdpc);

            if(tfunction != null )
            {
                //m_MetaConstructFunctionCall = new MetaMethodCall(null, null, tfunction, null, mdpc, null, null);
            }
        }
        public MetaNewObjectExpressNode( MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs )
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaType = new MetaType(mt);
            //m_MetaConstructFunctionCall = new MetaMethodCall(mt.metaClass, mt.defineTemplateMetaTypeList, m_OwnerMetaBlockStatements.ownerMetaFunction,
            //    null, null, null, null );
        }
        // 解析后的[] 然后再进行newArray
        public MetaNewObjectExpressNode(MetaArrayExpressNode maen, MetaClass mc, MetaBlockStatements mbs, MetaVariable equalMV )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;

            m_MetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_NewType = ENewType.ArrayClass;

            m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(maen, mc, mbs, equalMV );
            m_ArrayLength = m_MetaBraceOrBracketStatementsContent.assignStatementsList.Count;
        }

        // Class1 c = { a = 20, b = 20 };  => Class1 c = Class1(); c.a = 20; c.b = 20;
        // dynamic c = {a = 20, b = 20} => 动态类 
        // data c = {a = 20, b = 20} | c = {a = 20, b = 20} => 动态数据  
        // Map<int,string> map1 = new(10){ 1:"20", 2:"30", 3:"50" }
        // List<int> list1 = new(){ 1,2,3,4,5 }
        public MetaNewObjectExpressNode(FileMetaBraceTerm fmbt, MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaVariable equalMV)
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaType = new MetaType(mt);
            m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fmbt, ownerMC, mbs, equalMV);
            m_MetaBraceOrBracketStatementsContent.SetMetaType(m_MetaType);
        }
        // Array arr = [1,2,3]   [Class1(), Class2(), variable1.a.b(),100]
        public MetaNewObjectExpressNode( FileMetaBracketTerm fmbt, MetaType mt, MetaClass mc, MetaBlockStatements mbs, MetaVariable equalMV )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_MetaBraceOrBracketStatementsContent = new MetaBraceOrBracketStatementsContent(fmbt, m_OwnerMetaClass, m_OwnerMetaBlockStatements, equalMV);

            m_MetaType = new MetaType(mt);
            m_MetaType.SetArrayDimension(1);
            m_NewType = ENewType.ArrayClass;
            m_MetaBraceOrBracketStatementsContent.SetMetaType(m_MetaType);
        }
        public override void Parse(AllowUseSettings auc)
        {
            //该函数，进行，计算出， 要创建的类，使用的初始化函数，以及，初始化成员的解析
            
            if(m_NewType == ENewType.ArrayClass )
            {
                m_MetaBraceOrBracketStatementsContent.Parse();
                MetaClass inputType = m_MetaBraceOrBracketStatementsContent.GetMaxLevelMetaClassType();

                m_MetaType = new MetaType(inputType);

                int disension = 1;
                List<MetaType> listMT = new List<MetaType>();
                bool isPureArray = true;
                for( int i = 0; i < m_MetaBraceOrBracketStatementsContent.assignStatementsList.Count; i++ )
                {
                    var mt = m_MetaBraceOrBracketStatementsContent.assignStatementsList[i].GetRetMetaType();
                    listMT.Add(mt);
                    if( !mt.isArray )
                    {
                        isPureArray = false;
                    }
                }
                if( isPureArray )
                {
                    disension++;
                }
                m_MetaType.SetArrayMetaType(listMT);
                m_MetaType.SetArrayDimension(disension);

                MetaInputParamCollection mipc = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaBlockStatements);
                mipc.AddMetaInputParam(new MetaInputParam(new MetaConstExpressNode(EType.Int32, m_MetaBraceOrBracketStatementsContent.count)));
                mipc.CaleReturnType();
                m_MetaMemberFunction = m_MetaType.metaClass.GetMetaMemberConstructFunction(mipc);
                SetInputParams(mipc);
                m_ArrayLength = m_MetaBraceOrBracketStatementsContent.count ;
            }
            else if( m_NewType == ENewType.CommomClass )
            {
                m_MetaBraceOrBracketStatementsContent.Parse();
                if (m_MetaBraceOrBracketStatementsContent.contentType == MetaBraceOrBracketStatementsContent.EStatementsContentType.DynamicClass)
                {
                    m_MetaType = m_MetaBraceOrBracketStatementsContent.defineMetaType;
                }
                if (m_MetaBraceOrBracketStatementsContent.contentType == MetaBraceOrBracketStatementsContent.EStatementsContentType.DynamicData)
                {
                    m_MetaType = m_MetaBraceOrBracketStatementsContent.defineMetaType;
                }
            }
        }
        List<int> depthLength = new List<int>();
        public void HnaldeArrayType( MetaCallNode lastNode  )
        {
            for (int i = 0; i < lastNode.bracketExpressList.Count; i++)
            {
                bool flag = true;
                if (lastNode.bracketExpressList[i] is MetaArrayExpressNode maen)
                {
                    if( maen.metaCallArray.Count == 1 )
                    {
                        if( maen.metaCallArray[0] is MetaConstExpressNode mcenc )
                        {
                            if (mcenc.eType == EType.Int32)
                            {
                                flag = false;
                                depthLength.Add((int)mcenc.value);
                            }
                        }
                    }
                    else if( maen.metaCallArray.Count == 0 && i == lastNode.bracketExpressList.Count - 1 )
                    {
                        depthLength.Add(-1);
                    }
                }
                if (flag)
                {
                    Log.AddInStructMeta(EError.None, "在[]中，只允许数字形式存在");
                }
            }

            HandleArrayCreateType(m_MetaType);
        }
        void HandleArrayCreateType( MetaType mt )
        {
            int length = 0;
            if (depthLength.Count > 0)
            {
                length = depthLength[0];
                depthLength.RemoveAt(0);
            }
            else
            {
                return;
            }
            for (int f = 0; f < (int)length; f++)
            {
                MetaType mtnew = new MetaType(m_MetaType.metaClass);
                mtnew.SetArrayDimension(depthLength.Count-1);
                mt.AddArrayMetaType(mtnew);

                if(depthLength.Count == 0 )
                {
                    continue;
                }
                else
                {
                    HandleArrayCreateType(mtnew);
                }
            }
            depthLength.Add(length);
        }
        void SetInputParams(MetaInputParamCollection _paramCollection)
        {
            if(m_MetaMemberFunction == null )
            {
                return;
            }
            int defineCount = m_MetaMemberFunction.metaMemberParamCollection.maxParamCount;
            List<MetaDefineParam> mpList = new();
            if (m_MetaMemberFunction.metaMemberParamCollection != null)
            {
                mpList = m_MetaMemberFunction.metaMemberParamCollection.metaDefineParamList;
            }

            int inputCount = _paramCollection != null ? _paramCollection.metaInputParamList.Count : 0;
            for (int i = 0; i < defineCount; i++)
            {
                if (i < inputCount)
                {
                    MetaInputParam mip = _paramCollection.metaInputParamList[i];
                    m_MetaInputParamList.Add(mip.express);
                }
                else
                {
                    MetaDefineParam mdp = mpList[i];
                    if (mdp != null)
                    {
                        m_MetaInputParamList.Add(mdp.expressNode);
                    }
                }
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
            //for (int i = 0; i < assignStatementsList.Count; i++)
            //{
            //    assignStatementsList[i].CalcReturnType();
            //}
        }
        public override MetaType GetReturnMetaDefineType()
        {
            if(m_MetaType != null )
            {
                return m_MetaType;
            }
            //if (m_MetaConstructFunctionCall != null)
            //{
            //    m_MetaType = m_MetaConstructFunctionCall.GeMetaDefineType();
            //}
            return m_MetaType;
        }
        public override string ToTokenString()
        {
            return base.ToTokenString();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();


            if( m_MetaType.isEnum )
            {
                sb.Append(m_MetaType.name );
                sb.Append(".");
                sb.Append(m_MetaType.enumValue.name);
                if(m_MetaEnumValue != null)
                {
                    sb.Append("(");
                    sb.Append(m_MetaEnumValue.ToFormatString());
                    sb.Append(")");
                }
            }
            else if( m_MetaType.isData )
            {
                sb.Append(m_MetaType.name);
                sb.Append("{");
                if (m_MetaBraceOrBracketStatementsContent != null)
                {
                    for( int i = 0; i < m_MetaBraceOrBracketStatementsContent.count ; i++ )
                    {
                        var bsc = m_MetaBraceOrBracketStatementsContent.assignStatementsList[i];
                        if( bsc == null )
                        {
                            continue;
                        }
                        sb.Append(bsc.metaMemberData?.ToFormatString2(m_MetaType.isDynamicData));

                        if( i < m_MetaBraceOrBracketStatementsContent.count - 1 )
                        {
                            sb.Append(",");
                        }
                    }
                }
                sb.Append("}");
            }
            else
            {
                if ( m_MetaType != null )
                {
                    sb.Append(m_MetaType.name + "()");
                    sb.Append(".");
                }
                //if(m_MetaConstructFunctionCall.m_CallerMetaVariable != null )
                //{
                //    sb.Append(m_MetaConstructFunctionCall.m_CallerMetaVariable.name);
                //    sb.Append(".");
                //    sb.Append(m_MetaConstructFunctionCall.function.name);
                //    sb.Append("(");
                //    if( m_MetaConstructFunctionCall.metaInputParamCollection != null )
                //    {
                //        int count = m_MetaConstructFunctionCall.metaInputParamCollection.metaInputParamList.Count;
                //        for ( int i = 0; i < count; i++ )
                //        {
                //            var mp = m_MetaConstructFunctionCall.metaInputParamCollection.metaInputParamList[i];
                //            sb.Append(mp.ToFormatString());
                //            if( i < count - 1 )
                //            {
                //                sb.Append(",");
                //            }
                //        }
                //    }
                //    sb.Append(")");
                //}
                sb.Append(m_MetaBraceOrBracketStatementsContent?.ToFormatString());
            }

            return sb.ToString();
        }
        /// <summary>
        ///  Class2 c = new( 1, 2 );
        /// </summary>
        /// <param name="root"></param>
        /// <param name="mc"></param>
        /// <param name="mbs"></param>
        /// <param name="selfMc"></param>
        /// <returns></returns>
        /*
        public static MetaNewObjectExpressNode CreateNewObjectExpressNodeByPar(FileMetaParTerm root, MetaType mt, MetaClass omc, MetaBlockStatements mbs)
        {
            var fmct = (root as FileMetaParTerm);
            if (fmct == null) return null;
            if (mt == null) return null;

            MetaInputParamCollection mpc = new MetaInputParamCollection(root, omc, mbs);

            if( mpc.metaInputParamList.Count > 0 )
            {
                MetaMemberFunction mmf = mt.GetMetaMemberConstructFunction(mpc);

                if (mmf == null) return null;

              
                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mt, omc, mbs );

                return mnoen;

            }
            else
            {
                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode( root, mt, omc, mbs );

                return mnoen;
            }
        }
        */
    }
}
