//****************************************************************************
//  File:      IRReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/12/04 12:00:00
//  Description:   meta new object express!
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace SimpleLanguage.Core
{
    // a = { i1 = 10 } 这个过程式的处理
    public class MetaBraceAssignStatements
    {
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
                    Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL96, "在Map里边，必须使用:个符号");
                    return;
                }
            }
            else
            {
                if (fmst.symBolType != ETokenType.Assign )
                {
                    Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL104, "在class或者是data里边，必须使用=个符号");
                    return;
                }
            }
            if (fmst.left is not FileMetaCallTerm fmct1)
            {
                Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL110, "在class或者是data里边，前值应该使用filemetaCallTerm");
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
            else
            {
                if (mt.isData)
                {
                    m_MetaMemberData = (mt.metaClass as MetaData).GetMemberDataByName(m_DefineName);
                    if (m_MetaMemberData == null)
                    {
                        System.Diagnostics.Debug.Write("Error 在类" + mt.metaClass?.allClassName + "函数: " + mbs?.ownerMetaFunction.name
                            + " 没有找到: 类" + mt.metaClass?.allClassName + " 变量:" + m_DefineName);
                    }
                    //m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberData, m_OwnerMetaBlockStatements, m_FileMetaOpAssignSyntax?.express);
                }
                else if (mt.isEnum)
                {
                    System.Diagnostics.Debug.Write("-----------------------------------Enum-------------------------");
                }
                else
                {
                    m_MetaMemberVariable = mt.metaClass.GetMetaMemberVariableByName(m_DefineName);
                    if (m_MetaMemberVariable == null)
                    {
                        System.Diagnostics.Debug.Write("Error 在类" + mt.metaClass?.allClassName + "函数: " + mbs?.ownerMetaFunction.name
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
                cep.metaType = new MetaType(m_MetaMemberVariable.defineMetaType);
                cep.ownerMBS = m_OwnerMetaBlockStatements;
                cep.ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass;
                m_MetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
            }
        }
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaType mc, MetaExpressNode men)
        {
            m_OwnerMetaBlockStatements = mbs;
            m_MetaExpress = men;
        }
        public MetaBraceAssignStatements(MetaBlockStatements mbs, MetaExpressNode men, MetaMemberVariable mmv )
        {
            m_OwnerMetaBlockStatements = mbs;
            m_MetaExpress = men;
            this.m_MetaMemberVariable = mmv;
        }
        public void Parse( AllowUseSettings aus )
        {
            if( m_MetaExpress != null )
            {
                m_MetaExpress.Parse(aus);
                m_MetaExpress = ExpressManager.ConvertNewExpress(m_MetaExpress, m_MetaType, this.m_MetaMemberVariable );                
            }
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
        public void CalcReturnType()
        {
            if (m_MetaExpress != null)
            {
                m_MetaExpress.CalcReturnType();
                if (m_MetaMemberVariable != null)
                {
                    MetaClass retMetaClass = m_MetaMemberVariable.defineMetaType.metaClass;
                    MetaClass ownerMetaClass = m_MetaMemberVariable.ownerMetaClass;
                    //bool m_IsNeedCastStatements = false;
                }
                else if( m_MetaMemberData != null )
                {
                    MetaClass retMetaClass = m_MetaMemberData.defineMetaType.metaClass;
                    MetaClass ownerMetaClass = m_MetaMemberData.ownerMetaClass;
                    //bool m_IsNeedCastStatements = false;
                }
                //ExpressManager.CalcDefineClassType(ref retMetaClass, m_MetaExpress, ownerMetaClass,
                //    m_OwnerMetaBlockStatements?.ownerMetaFunction, m_MetaVariable.name, ref m_IsNeedCastStatements);
                //差一个验证类型
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
                System.Diagnostics.Debug.Write("使用{}赋值，表达式不允许为空!!");
            }
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
    public class MetaNewObjectStatementsContent
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
        /// <summary>数组/大括号字面值左侧被赋值的变量（如 <c>var x = [..]</c> 中的 <c>x</c>），用于可空等上下文。</summary>
        public MetaVariable? equalMetaVariable => m_EqualMetaVariable;

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
        private Token m_Token = null;

        public MetaNewObjectStatementsContent( MetaClass mc, MetaBlockStatements mbs )
        {

        }

        public MetaNewObjectStatementsContent( MetaArrayExpressNode maen, MetaClass mc, MetaBlockStatements mbs, MetaVariable parentMt)
        {
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;
            m_EqualMetaVariable = parentMt;
            m_MetaArrayExpressNode = maen;

            for( int i = 0; i < m_MetaArrayExpressNode.metaCallArray.Count; i++ )
            {
                var men = m_MetaArrayExpressNode.metaCallArray[i];
                MetaBraceAssignStatements mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                //mas.CalcReturnType();
                m_AssignStatementsList.Add(mas);
            }
            m_ContentType = EStatementsContentType.ArrayValue;
        }
        public MetaNewObjectStatementsContent( FileMetaBaseTerm fmbt, MetaClass mc, MetaBlockStatements mbs, MetaVariable parentMt)
        {
            m_FileMetaBaseTerm = fmbt;
            m_Token = fmbt?.token;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = mc;
            m_EqualMetaVariable = parentMt;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
        }
        public void SetDefineMetaType( MetaType mt )
        {
            m_DefineMetaType = mt;
        }
        public void Parse(AllowUseSettings aws)
        {
            if( m_FileMetaBaseTerm is FileMetaBraceTerm fmbt2 )
            {

            }
            if (m_FileMetaBaseTerm?.fileMetaExpressList.Count > 0)
            {
                //Log.AddMetaCoreLog(LID.Unknown, "解析大括号里边的内容");
                for (int i = 0; i < m_FileMetaBaseTerm.fileMetaExpressList.Count; i++)
                {
                    var fas = m_FileMetaBaseTerm.fileMetaExpressList[i];
                    HandleBraceTermNode(fas, m_DefineMetaType, aws);
                }
            }
            else
            {
                for (int i = 0; i < this.m_AssignStatementsList.Count; i++)
                {
                    var asl = this.m_AssignStatementsList[i];
                    asl.Parse(aws);
                    asl.CalcReturnType();
                }
            }
        }
        //处理在{ Node1, Node2  } 在{}大括号中的Node1, Node2 这样的节点 Node1, 可以是 aaa = 1, "aa":1, 2:33, [1,2,3] [1] 3, this.value 这样的形式
        public void HandleBraceTermNode( FileMetaBaseTerm fmbt, MetaType mt, AllowUseSettings aws)
        {
            if (mt.isData)
            {
                //动态数据类的定义 在该行语句前直接使用 data a = { aaa = 10, bbb = 20} 这样的形式
                if (mt.isDynamicData)
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
                    if (m_EqualMetaVariable?.token != null)
                    {
                        m_NewTempMetaData.AddPingToken(m_EqualMetaVariable.token );
                    }
                    m_NewTempMetaData.AddPingToken(m_FileMetaBaseTerm.token);
                    m_DefineMetaType = new MetaType(m_NewTempMetaData);

                    if (fmbt is FileMetaSymbolTerm fmst)                   
                    {
                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements );
                        //mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbolterm in data ", m_EqualMetaVariable?.name, "");
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
                        //mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbolterm", m_EqualMetaVariable?.name, "");
                        return;
                    }
                    m_ContentType = EStatementsContentType.DataValueAssign;
                }
            }
            else if (mt.IsArray() )// 数组类型的处理
            {
                m_ContentType = EStatementsContentType.ArrayValue;
                var genList = m_DefineMetaType.GetGenTemplateMetaTypeList();
                if (genList.Count != 1 )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreArrayDiamondShould, "", m_Token);
                    return;
                }
                MetaType cmt = genList[0];
                if (fmbt is FileMetaBracketTerm fmst)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmst, cmt, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_EqualMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, mnoe, m_EqualMetaVariable as MetaMemberVariable);
                    m_AssignStatementsList.Add(mas);                    
                }
                else if (fmbt is FileMetaBraceTerm fmbrt)
                {
                    // 兼容多层数组字面量中使用大括号嵌套的写法：
                    // int[][][] a = { { {1,2}, {3,4} }, { {5,6}, {7,8} } };
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbrt, cmt, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_EqualMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, mnoe, m_EqualMetaVariable as MetaMemberVariable);
                    m_AssignStatementsList.Add(mas);
                }
                else if( fmbt is FileMetaCallTerm fmct )
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = fmct;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());                    
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    mas.Parse(new AllowUseSettings());
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaConstValueTerm fmcvt)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = fmcvt;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    // If array element type is explicitly declared (and not object),
                    // force const literal conversion to target element type instead of numeric promotion.
                    if (cmt != null
                        && cmt.metaClass != CoreMetaClassManager.objectMetaClass
                        && men is MetaConstExpressNode constExpressNode)
                    {
                        if (!MetaVariable.TryAdjustConstExpressByDefineMetaType(constExpressNode, cmt))
                        {
                            return;
                        }
                    }
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if( fmbt is FileMetaSymbolTerm fmst2 )
                {
                    if( fmst2.symBolType != ETokenType.Comma )
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL533, "间隔符号不对,应该使用,");
                    }
                }
                else if( fmbt is FileMetaTermExpress termexpress )
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = termexpress;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    men = ExpressManager.ConvertNewExpress(men, cep.metaType, m_EqualMetaVariable);
                    if (cmt != null
                        && cmt.metaClass != CoreMetaClassManager.objectMetaClass
                        && men is MetaConstExpressNode constExpressNode)
                    {
                        if (!MetaVariable.TryAdjustConstExpressByDefineMetaType(constExpressNode, cmt))
                        {
                            return;
                        }
                    }
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                    Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL553, "Error 在数组里边应该是FileMetaBracketTerm 类型!");
                }
            }
            // Array<Object>(n){ ... } 中嵌套 [1,2] 时，子字面量节点的 defineMetaType 为元素类型 object（非 Array），
            // 不可走「普通类 { 成员= }」分支，须与数组槽一致地接纳常量/调用/[]/表达式。
            else if (mt != null && mt.metaClass == CoreMetaClassManager.objectMetaClass && !mt.IsArray())
            {
                m_ContentType = EStatementsContentType.ArrayValue;
                MetaType cmt = mt;
                if (fmbt is FileMetaBracketTerm fmstOb)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmstOb, cmt, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_EqualMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, mnoe, m_EqualMetaVariable as MetaMemberVariable);
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaBraceTerm fmbrtOb)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbrtOb, cmt, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_EqualMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, mnoe, m_EqualMetaVariable as MetaMemberVariable);
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaCallTerm fmctOb)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = fmctOb;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    mas.Parse(new AllowUseSettings());
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaConstValueTerm fmcvtOb)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = fmcvtOb;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaSymbolTerm fmstOb2)
                {
                    if (fmstOb2.symBolType != ETokenType.Comma)
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL533, "间隔符号不对,应该使用,");
                    }
                }
                else if (fmbt is FileMetaTermExpress termexpressOb)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaClass = m_OwnerMetaClass;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = termexpressOb;
                    cep.equalMetaVariable = m_EqualMetaVariable;
                    MetaExpressNode men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    men = ExpressManager.ConvertNewExpress(men, cep.metaType, m_EqualMetaVariable);
                    var mas = new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, new MetaType(m_OwnerMetaClass), men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                    Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL553, "Error Array<Object> 元素槽不支持该语法节点!");
                }
            }
            else if (mt.isMap)   // 映射类型的处理 使用   a:10, b:20  20:"aa" 这样的形式
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
                    Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "isMap", m_EqualMetaVariable?.name, "");
                    return;
                }
            }
            else
            {
                //动态普通类的定义
                if (mt.isDynamicClass)
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
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbol term in dynamic class", m_EqualMetaVariable?.name, "");
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
                        mas.Parse(aws);
                        mas.CalcReturnType();
                        assignStatementsList.Add(mas);
                    }
                    else if( fmbt is FileMetaTermExpress fmte )
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbol term in common class", m_EqualMetaVariable?.name, "" );
                        return;
                    }
                    m_ContentType = EStatementsContentType.ClassValueAssign;
                }
            }            
        }
        public MetaType GetMaxLevelMetaType()
        {
            //这个函数要处理 [1,2,3] 相同情况的类型    [1, 2.3f, 3.0d] 相同数字的最大类型确定 
            // 纯数值元素：按 byte→sbyte→int16→uint16→int32→uint32→float32→int64→uint64→float64 升阶取最高阶具体类型（见 MetaTypeFactory）
            // [1,"123", 3.0f] 不相同时 结果是object
            var objmt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (m_AssignStatementsList == null || m_AssignStatementsList.Count == 0)
            {
                return objmt;
            }
            if (m_AssignStatementsList.Count == 1)
            {
                var only = m_AssignStatementsList[0].GetRetMetaType();
                if (only == null || only.isNull)
                {
                    return objmt;
                }
                return only;
            }

            var types = new List<MetaType>(m_AssignStatementsList.Count);
            for (int i = 0; i < m_AssignStatementsList.Count; i++)
            {
                var t = m_AssignStatementsList[i].GetRetMetaType();
                if (t == null || t.isNull)
                {
                    return objmt;
                }
                types.Add(t);
            }

            bool allNumeric = true;
            for (int i = 0; i < types.Count; i++)
            {
                if (!ClassManager.IsNumberClass(types[i].metaClass))
                {
                    allNumeric = false;
                    break;
                }
            }
            if (allNumeric)
            {
                int maxRank = int.MinValue;
                for (int i = 0; i < types.Count; i++)
                {
                    if (!NumberManager.TryGetLiteralPromotionRank(types[i].metaClass, out int rank))
                    {
                        return objmt;
                    }
                    if (rank > maxRank)
                    {
                        maxRank = rank;
                    }
                }
                var promotedMc = NumberManager.GetMetaClassForLiteralPromotionRank(maxRank);
                return promotedMc != null ? new MetaType(promotedMc) : objmt;
            }

            int frontOpLevel = 0;
            var mt = new MetaType(CoreMetaClassManager.objectMetaClass);
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool isAllSame = true;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
            for (int i = 0; i < m_AssignStatementsList.Count - 1; i++)
            {
                MetaBraceAssignStatements cmc = m_AssignStatementsList[i];
                MetaBraceAssignStatements nmc = m_AssignStatementsList[i + 1];

                var cmcmt = cmc.GetRetMetaType();
                var nmcmt = nmc.GetRetMetaType();
                if (cmcmt.isNull)
                {
                    return objmt;
                }
                if (nmcmt.isNull)
                {
                    return objmt;
                }
                if (!TypeManager.CompareMetaType(cmcmt, nmcmt))
                {
                    if (cmcmt.IsArray() && nmcmt.IsArray()
                        && TryGetCompatibleArrayMetaType(cmcmt, nmcmt, out var compatibleArrayMetaType))
                    {
                        mt = compatibleArrayMetaType;
                        frontOpLevel = cmc.opLevel > nmc.opLevel ? cmc.opLevel : nmc.opLevel;
                        isAllSame = true;
                        continue;
                    }
                    return objmt;
                }
                if (cmc.opLevel == nmc.opLevel && nmc.opLevel > frontOpLevel)
                {
                    if (cmc.opLevel == 10)
                    {
                        var cutmt = cmc.GetRetMetaType();
                        var nextmt = nmc.GetRetMetaType();
                        var cur = cutmt.metaClass;
                        var next = nextmt.metaClass;
                        var relation = ClassManager.ValidateClassRelationByMetaClass(cur, next);
                        if (relation == EClassRelation.Same
                            || relation == EClassRelation.Child)
                        {
                            mt = nextmt;
                            frontOpLevel = cmc.opLevel;
                        }
                        else if (relation == EClassRelation.Parent)
                        {
                            mt = cutmt;
                        }
                        else
                        {
                            isAllSame = false;
                            break;
                        }
                    }
                    else
                    {
                        mt = cmc.GetRetMetaType();
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
                            mt = cmc.GetRetMetaType();
                        }
                        else
                        {
                            frontOpLevel = nmc.opLevel;
                            mt = nmc.GetRetMetaType();
                        }
                    }
                }
            }
            return mt;
        }
        private static bool TryGetCompatibleArrayMetaType(MetaType leftArray, MetaType rightArray, out MetaType result)
        {
            result = null;
            if (leftArray == null || rightArray == null) return false;
            if (!leftArray.IsArray() || !rightArray.IsArray()) return false;

            var leftTemplate = leftArray.GetTemplateMetaClass();
            var rightTemplate = rightArray.GetTemplateMetaClass();
            if (leftTemplate != rightTemplate) return false;

            var leftArgs = leftArray.GetGenTemplateMetaTypeList();
            var rightArgs = rightArray.GetGenTemplateMetaTypeList();
            if (leftArgs == null || rightArgs == null || leftArgs.Count != rightArgs.Count || leftArgs.Count == 0)
            {
                return false;
            }

            var leftElement = leftArgs[0];
            var rightElement = rightArgs[0];

            if (TypeManager.CompareMetaType(leftElement, rightElement))
            {
                result = new MetaType(leftArray);
                return true;
            }

            if (leftElement.IsArray() && rightElement.IsArray())
            {
                if (!TryGetCompatibleArrayMetaType(leftElement, rightElement, out var nestedCompatible))
                {
                    return false;
                }

                MetaType build = new MetaType();
                build.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                build.AddDefineTemplateMetaType(nestedCompatible);
                result = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(build, true, out bool _);

                if (leftArray.arrayLength != -1)
                {
                    result.SetArrayLength(leftArray.arrayLength);
                }
                else if (rightArray.arrayLength != -1)
                {
                    result.SetArrayLength(rightArray.arrayLength);
                }
                return true;
            }

            // Array<T> 兼容仅允许 T 相同（或递归数组元素同构）。
            // 只要 T 不同（如 Array<Object> vs Array<Int32>），不尝试父子类/接口推导，
            // 调用方将回退为 Object。
            return false;
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
        public int arrayLength => m_MetaType.arrayLength;
        public MetaExpressNode arrayLengthExpress => m_ArrayLengthExpress;
        public List<MetaExpressNode> metaInputParamList => m_MetaInputParamList;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public MetaNewObjectStatementsContent metaContent => m_MetaContent;

        /// <summary>
        /// 为 true 表示语法上已写明数组元素类型（如 <c>Array&lt;Int16&gt;(n){ ... }</c> 等由调用链构造的数组），
        /// 与仅由 <c>[1,2,3]</c> 推断的元素类型不同；赋值时不对左值做跨数值基类型的字面量强转。
        /// </summary>
        public bool usesExplicitArrayElementTypeSyntax => m_UsesExplicitArrayElementTypeSyntax;

        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        private List<FileMetaBraceTerm> m_FileMetaBraceTermList = new List<FileMetaBraceTerm>();
        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;

        private MetaExpressNode m_MetaEnumValue = null;
        private MetaNewObjectStatementsContent m_MetaContent = null;
        private ENewType m_NewType = ENewType.CommomClass;
        private bool m_NeedInitMemberVariable = true;

        private MetaType m_DefineMetaType = null;
        private MetaType m_NewMetaType = null;
        private MetaType m_RealMetaType = null;
        private bool m_UsesExplicitArrayElementTypeSyntax = false;
        private MetaExpressNode m_ArrayLengthExpress = null;
        protected MetaVariable m_StoreMetaVariable = null; //模板或者是调用时的函数        
        protected MetaMemberFunction m_MetaMemberFunction = null;
        protected List<MetaExpressNode> m_MetaInputParamList = new List<MetaExpressNode>();

        // Class1(10){ c1 = 20, c2 = 30 }  int[2][]{ [1,2,3], [3,4,5] }
        public MetaNewObjectExpressNode( MetaType defineMt, MetaCallLinkExpressNode mcen )
        {
            m_DefineMetaType = defineMt != null ? new MetaType( defineMt ) : null;
            m_OwnerMetaClass = mcen.ownerMetaClass;
            m_OwnerMetaBlockStatements = mcen.ownerMetaBlockStatements;
            m_StoreMetaVariable = mcen.GetMetaVariable();

            m_MetaMemberFunction = mcen.metaCallLink.finalCallNode.methodCall?.function as MetaMemberFunction;
            m_NewMetaType = new MetaType( mcen.metaCallLink.finalCallNode.callMetaType );
            if ( mcen.metaCallLink.finalCallNode.callMetaType.IsArray() )
            {
                m_NewType = ENewType.ArrayClass;
                m_UsesExplicitArrayElementTypeSyntax = true;

                if (mcen.metaCallLink.callNodeList.Count > 0)
                {
                    var lastNode = mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1];

                    m_Token = lastNode.token;
                    if ( lastNode.metaInputParamCollection != null )
                    {
                        SetInputParams(lastNode.metaInputParamCollection);
                    }
                    else
                    {
                        if( lastNode.bracketExpressList?.Count > 0 )
                        {
                            MetaArrayExpressNode mean = lastNode.bracketExpressList[0] as MetaArrayExpressNode;
                            
                            if( mean.metaCallArray.Count == 1 )
                            {
                                m_MetaInputParamList.Add(mean.metaCallArray[0]);
                            }
                            else
                            {
                                Log.AddMetaCoreLog(LID.MetaCoreArrayDiamondShould, m_Token, "", 1 );
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, "");
                        }
                    }

                    var fma = lastNode.fileMetaBraceTerm;
                    m_MetaContent = new MetaNewObjectStatementsContent(fma, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    m_MetaContent.SetDefineMetaType(m_NewMetaType);
                }
            }
            else
            {
                m_NewType = ENewType.CommomClass;
                if (mcen.metaCallLink.callNodeList.Count > 0)
                {
                    var lastNode = mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1];
                    m_Token = lastNode.token;
                    SetInputParams(lastNode.metaInputParamCollection);

                    var fma = lastNode.fileMetaBraceTerm;
                    m_MetaContent = new MetaNewObjectStatementsContent(fma, m_OwnerMetaClass, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    m_MetaContent.SetDefineMetaType(m_NewMetaType);
                }
            }
        }
        // dynamic c = { c1 = 100, c2 = 200 }
        public MetaNewObjectExpressNode( MetaClass ownermc, List<MetaDynamicClass> list )
        {
            m_OwnerMetaClass = ownermc;
            m_OwnerMetaBlockStatements = null;
            m_MetaContent = null;;

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
            m_Token = m_FileMetaConstValueTerm.token;
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
        // 手动构建NewObject表达式
        public MetaNewObjectExpressNode(MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs)
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_DefineMetaType = new MetaType(mt);
            m_NewMetaType = new MetaType(mt);
            m_MetaType = new MetaType(mt);
            if( m_MetaType.IsArray() )
            {
                m_NewType = ENewType.ArrayClass;
            }
            else
            {
                m_NewType = ENewType.CommomClass;
            }
            m_MetaContent = new MetaNewObjectStatementsContent( ownerMC, mbs );
            m_MetaContent.SetDefineMetaType(m_NewMetaType);
            //m_MetaConstructFunctionCall = new MetaMethodCall(mt.metaClass, mt.defineTemplateMetaTypeList, m_OwnerMetaBlockStatements.ownerMetaFunction,
            //    null, null, null, null );
        }
        //public MetaNewObjectExpressNode(MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaVariable storeMv, MetaMemberFunction mmf)
        //{
        //    m_OwnerMetaClass = ownerMC;
        //    m_OwnerMetaBlockStatements = mbs;
        //    m_MetaType = new MetaType(mt);
        //    m_StoreMetaVariable = storeMv;
        //    m_MetaMemberFunction = mmf;
        //}
        // 解析后的[] 然后再进行newArray
        public MetaNewObjectExpressNode(MetaArrayExpressNode maen, MetaClass mc, MetaBlockStatements mbs, MetaVariable equalMV )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_NewType = ENewType.ArrayClass;
            m_Token = maen.token;
            m_MetaContent = new MetaNewObjectStatementsContent(maen, mc, mbs, equalMV );
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
            m_DefineMetaType = new MetaType(mt);
            m_NewMetaType = new MetaType(mt);
            if( m_NewMetaType.IsArray() )
            {
                m_NewType = ENewType.ArrayClass;
            }
            else
            {
                m_NewType = ENewType.CommomClass;
            }
            m_Token = fmbt.token;
            m_MetaContent = new MetaNewObjectStatementsContent(fmbt, ownerMC, mbs, equalMV);
            m_MetaContent.SetDefineMetaType(m_DefineMetaType);
        }
        // Array arr = [1,2,3]   [Class1(), Class2(), variable1.a.b(),100]
        public MetaNewObjectExpressNode( FileMetaBracketTerm fmbt, MetaType mt, MetaClass mc, MetaBlockStatements mbs, MetaVariable equalMV )
        {
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_Token = fmbt.token;
            m_MetaContent = new MetaNewObjectStatementsContent(fmbt, m_OwnerMetaClass, m_OwnerMetaBlockStatements, equalMV);

            m_NewMetaType = new MetaType(mt);
            m_NewType = ENewType.ArrayClass;
            m_MetaContent.SetDefineMetaType(m_NewMetaType);
        }
        public override void Parse(AllowUseSettings auc)
        {
            //该函数，进行，计算出， 要创建的类，使用的初始化函数，以及，初始化成员的解析            //
            if (m_NewType == ENewType.ArrayClass )
            {
                m_MetaContent.Parse(auc);

                if(m_MetaContent.assignStatementsList.Count > 0 )
                {
                    MetaType inputType = m_MetaContent.GetMaxLevelMetaType();

                    m_RealMetaType = new MetaType(inputType);
                    //List<MetaType> listMT = new List<MetaType>();
                    //for( int i = 0; i < m_MetaBraceOrBracketStatementsContent.assignStatementsList.Count; i++ )
                    //{
                    //    var mt = m_MetaBraceOrBracketStatementsContent.assignStatementsList[i].GetRetMetaType();
                    //    listMT.Add(mt);
                    //}
                    MetaType newRMT = new MetaType();
                    newRMT.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                    newRMT.AddDefineTemplateMetaType(m_RealMetaType);
                    //newRMT.AddGenTemplateMetaType(m_RealMetaType);
                    m_RealMetaType = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(newRMT, true, out bool isIGM);
                    m_RealMetaType.SetArrayLength(m_MetaContent.assignStatementsList.Count);

                    if(m_NewMetaType == null )
                    {
                        m_NewMetaType = new MetaType(m_RealMetaType);
                    }
                }
                else
                {
                    m_RealMetaType = null;
                }

            }
            else if( m_NewType == ENewType.CommomClass )
            {
                m_MetaContent.Parse(auc);
                if (m_MetaContent.contentType == MetaNewObjectStatementsContent.EStatementsContentType.DynamicClass)
                {
                    m_RealMetaType = m_MetaContent.defineMetaType;
                }
                else if (m_MetaContent.contentType == MetaNewObjectStatementsContent.EStatementsContentType.DynamicData)
                {
                    m_RealMetaType = m_MetaContent.defineMetaType;
                }
                else
                {
                    // Before creating instance type, check abstract class restriction
                    var metaClass = m_NewMetaType?.metaClass;
                    if (metaClass != null && metaClass.isAbstractClass)
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaExpressNewObjectL1026, m_Token, "Error: cannot instantiate abstract class: " + metaClass.name);
                        m_RealMetaType = null;
                    }
                    else
                    {
                        m_RealMetaType = new MetaType(m_NewMetaType);
                    }
                }
            }
        }
        //List<int> depthLength = new List<int>();
        //public void HnaldeArrayType( MetaCallNode lastNode, MetaType mt  )
        //{
        //    for (int i = 0; i < lastNode.bracketExpressList.Count; i++)
        //    {
        //        bool flag = true;
        //        if (lastNode.bracketExpressList[i] is MetaArrayExpressNode maen)
        //        {
        //            if( maen.metaCallArray.Count == 1 )
        //            {
        //                if( maen.metaCallArray[0] is MetaConstExpressNode mcenc )
        //                {
        //                    if (mcenc.eType == EType.Int32)
        //                    {
        //                        flag = false;
        //                        depthLength.Add((int)mcenc.value);
        //                    }
        //                }
        //            }
        //            else if( maen.metaCallArray.Count == 0 && i == lastNode.bracketExpressList.Count - 1 )
        //            {
        //                flag = false;
        //                depthLength.Add(-1);
        //            }
        //        }
        //        if (flag)
        //        {
        //            Log.AddMetaCoreLog(LID.Unknown, "在[]中，只允许数字形式存在");
        //        }
        //    }
        //    int use_n_numone = 0;
        //    for( int i = depthLength.Count - 1; i >= 0; i--)
        //    {
        //        if(depthLength[i] == -1 )
        //        {
        //            if( use_n_numone == 2 )
        //            {
        //                Log.AddMetaCoreLog(LID.Unknown, "在[]中，只允许从后边向前[3][-1][-1]这种形式，而不能使用[3][-1][2] 这种形式");
        //                continue;
        //            }
        //        }
        //        else
        //        {
        //            use_n_numone = 2;
        //        }
        //    }

        //    mt.SetArrayDismensionLength(depthLength);
        //}
        public void SetInputParams(MetaInputParamCollection _paramCollection)
        {
            int defineCount = 0;
            List<MetaDefineParam> mpList = new();
            if (m_MetaMemberFunction != null )
            {
                defineCount = m_MetaMemberFunction.metaMemberParamCollection.maxParamCount;
                if (m_MetaMemberFunction.metaMemberParamCollection != null)
                {
                    mpList = m_MetaMemberFunction.metaMemberParamCollection.metaDefineParamList;
                }
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
            if( newType == ENewType.ArrayClass )
            {
                if( m_MetaInputParamList.Count == 1 )
                {
                    if (m_NewMetaType != null)
                    {
                        if (m_MetaInputParamList[0] is MetaConstExpressNode mcen )
                        {
                            int len = Convert.ToInt32(mcen.value);
                            m_NewMetaType.SetArrayLength(len );
                        }
                    }
                    else
                    {
                        if (m_MetaInputParamList[0] is MetaConstExpressNode mcen)
                        {
                            //m_NewMetaType.SetArrayLength((int)mcen.value);
                        }
                        //Debug.Assert(false);
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreArrayNotFoundSetLength, m_Token, "", m_Token.lexeme.ToString() );
                }
            }
        }
        public void SetStoreMetaVariable( MetaVariable smv )
        {
            this.m_StoreMetaVariable = smv;
        }
        private static bool TryArrayElementAssignableForNewObject(MetaType targetArray, MetaType exprArray)
        {
            if (targetArray == null || exprArray == null) return false;
            if (!targetArray.IsArray() || !exprArray.IsArray()) return false;

            var targetTemplate = targetArray.GetTemplateMetaClass();
            var exprTemplate = exprArray.GetTemplateMetaClass();
            if (targetTemplate != exprTemplate) return false;

            var targetArgs = targetArray.GetGenTemplateMetaTypeList();
            var exprArgs = exprArray.GetGenTemplateMetaTypeList();
            if (targetArgs == null || exprArgs == null || targetArgs.Count != exprArgs.Count || targetArgs.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < targetArgs.Count; i++)
            {
                var tArg = targetArgs[i];
                var eArg = exprArgs[i];
                if (TypeManager.CompareMetaType(tArg, eArg))
                {
                    continue;
                }

                if (tArg.IsArray() && eArg.IsArray())
                {
                    if (TryArrayElementAssignableForNewObject(tArg, eArg))
                    {
                        continue;
                    }
                    return false;
                }

                // 数组元素类型在 new object 初始化场景必须严格一致，不允许父子类/接口等转换。
                return false;
            }

            return true;
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

            MetaInputParamCollection mipc = new MetaInputParamCollection(m_OwnerMetaClass, m_OwnerMetaBlockStatements);

            if (m_DefineMetaType != null && m_NewMetaType != null )
            {
                if (m_NewMetaType.IsArray() )
                {
                    if (m_StoreMetaVariable?.isDefineMetaType == true )
                    {
                        if (m_DefineMetaType.IsArray() == false)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "如果定义了，结构，必须与new对象的类型一样才可以");
                            return;
                        }
                        else
                        {
                            var list1 = m_DefineMetaType.ArrayDimensionLengthList();
                            var list2 = m_NewMetaType.ArrayDimensionLengthList();

                            if (list1.Count == list2.Count && list1.Count != 0 )
                            {
                                MetaType numericMergedArrayMeta = null;
                                for (int i = 0; i < list1.Count; i++)
                                {
                                    if( i == list1.Count - 1 )
                                    {
                                        if(list1[i] != -1 )
                                        {
                                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "最后一位数组定义，不能为实体值");
                                            return;
                                        }
                                        var cmt1 = m_DefineMetaType.GetMetaTypeByIndex(0);
                                        var cmt2 = m_NewMetaType.GetMetaTypeByIndex(0);

                                        if( !TypeManager.CompareMetaType( cmt1, cmt2 ) )
                                        {
                                            if (ClassManager.IsNumberClass(cmt1.metaClass) && ClassManager.IsNumberClass(cmt2.metaClass))
                                            {
                                                // 左值 Array<Int32>、右值模板 Array<Int16> 等：以左值元素类型为准，后续对字面量强转/升阶
                                                numericMergedArrayMeta = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(
                                                    m_DefineMetaType, m_NewMetaType, m_RealMetaType);
                                            }
                                            else if (cmt1.IsArray() && cmt2.IsArray() && TryArrayElementAssignableForNewObject(cmt1, cmt2))
                                            {
                                            }
                                            else
                                            {
                                                Log.AddMetaCoreLog(LID.MetaCoreArrayNotSupportInConvert, m_Token, "", cmt1.ToString(), cmt2.ToString() );
                                                return;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (list1[i] == -1)
                                        {
                                            if (list2[i] == -1)
                                            {
                                                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "不是最后一位 生成的数组，需要定义数组长度");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            if (list1[i] != list2[i])
                                            {
                                                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "最后一位数组定义，不能为实体值! 如果前边定义了长度，new的时候必须和前边的长度一样!");
                                                return;
                                            }
                                        }
                                    }
                                }
                                m_MetaType = numericMergedArrayMeta != null ? numericMergedArrayMeta : new MetaType(m_NewMetaType);
                            }
                            else
                            {
                                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "定义数组与new数组 的维度不同");
                                return;
                            }
                        }
                    }
                    else
                    {
                        m_MetaType = new MetaType(m_NewMetaType);
                    }
                }
                else
                {
                    if (m_DefineMetaType.IsArray() )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "如果定义了，结构，必须与new对象的类型一样才可以");
                        return;
                    }
                    else
                    {
                        if( m_NewMetaType.metaClass.IsContainMetaClass( m_DefineMetaType.metaClass ) )
                        {
                            m_MetaType = new MetaType(m_NewMetaType);
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "定义类型与new的类型不对应 ");
                            return;
                        }
                    }
                }
            }
            else if (m_NewMetaType != null && m_DefineMetaType == null)
            {
                m_MetaType = new MetaType(m_NewMetaType);
            }
            else if (m_NewMetaType != null && m_DefineMetaType == null )
            {
                m_MetaType = new MetaType(m_DefineMetaType);
            }
            else if (m_DefineMetaType == null && m_NewMetaType == null )
            {
            }
            else
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "没有找到没有各种定义类型的方法");
            }

            // 左值 Array<Int32> + 字面量/右模板 Array<Int16>：未走“变量已带定义类型”分支时，仍以左值元素类型合并 m_MetaType（如 ConvertNewExpress 传入的 SetAssignmentTarget）
            if (m_DefineMetaType != null && m_NewMetaType != null
                && m_DefineMetaType.IsArray() && m_NewMetaType.IsArray()
                && m_NewType == ENewType.ArrayClass)
            {
                var dEl = ClassManager.GetSingleTemplateArgMetaType(m_DefineMetaType);
                var nEl = ClassManager.GetSingleTemplateArgMetaType(m_NewMetaType);
                if (dEl != null && nEl != null
                    && ClassManager.IsNumberClass(dEl.metaClass) && ClassManager.IsNumberClass(nEl.metaClass)
                    && !TypeManager.CompareMetaType(dEl, nEl))
                {
                    var merged = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(
                        m_DefineMetaType, m_NewMetaType, m_RealMetaType);
                    if (merged != null)
                    {
                        m_MetaType = merged;
                    }
                }
            }

            if(m_RealMetaType != null )
            {
                if (m_RealMetaType.IsArray() )
                {
                    if (m_MetaType == null)
                    {
                        m_MetaType = new MetaType(m_RealMetaType);                        
                    }
                    else
                    {
                        if( m_MetaType.arrayLength == -1 )
                        {
                            m_MetaType.SetArrayLength(m_RealMetaType.arrayLength);
                        }
                        else
                        {
                            if( m_MetaType.arrayLength < m_RealMetaType.arrayLength )
                            {
                                //这也还是写具体的数组类型对比，和多维长度对比，暂留以后写
                                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "数组赋值内容给出的长度超出了定义长度!");
                                return;
                            }
                        }
                    }
                }
                else
                {
                    if (m_MetaType == null)
                    {
                        m_MetaType = new MetaType(m_RealMetaType);
                    }
                }
            }

            // 仅有左值 m_DefineMetaType（如 Array<Int32>）与字面量推断 m_RealMetaType（如 Array<Int16>）时，不能以推断类型覆盖左值元素类型
            if (m_DefineMetaType != null && m_NewMetaType == null && m_RealMetaType != null
                && m_DefineMetaType.IsArray() && m_RealMetaType.IsArray()
                && m_NewType == ENewType.ArrayClass)
            {
                var dEl = ClassManager.GetSingleTemplateArgMetaType(m_DefineMetaType);
                var rEl = ClassManager.GetSingleTemplateArgMetaType(m_RealMetaType);
                if (dEl != null && rEl != null
                    && ClassManager.IsNumberClass(dEl.metaClass) && ClassManager.IsNumberClass(rEl.metaClass)
                    && !TypeManager.CompareMetaType(dEl, rEl))
                {
                    var mergedDr = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(
                        m_DefineMetaType, null, m_RealMetaType);
                    if (mergedDr != null)
                    {
                        m_MetaType = mergedDr;
                    }
                }
            }

            if (m_MetaType == null)
            {
                return;
            }

            if( m_MetaType.IsArray() )
            {
                if(m_MetaInputParamList.Count == 0)
                {
                    int alen = m_MetaType.arrayLength;
                    if( alen == -1 && m_RealMetaType == null )
                    {
                        alen = 0;
                    }

                    mipc.AddMetaInputParam(new MetaInputParam(new MetaConstExpressNode(EType.Int32, alen )));
                    mipc.CaleReturnType();

                    m_MetaMemberFunction = CoreMetaClassManager.arrayMetaClass.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, mipc);
                    SetInputParams(mipc);
                }
                else if( m_MetaInputParamList.Count == 1 )
                {
                    if( m_MetaInputParamList[0] is MetaConstExpressNode mcen )
                    {
                        mcen.value = m_MetaType.arrayLength;
                    }                    
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "--" );
                }
                m_ArrayLengthExpress = m_MetaInputParamList[0];
                m_MetaMemberFunction = null;
            }
            else
            {
                //m_MetaMemberFunction = m_MetaType.metaClass.GetMetaMemberConstructFunction(mipc);
                //SetInputParams(mipc);
            }

            if (m_NewType == ENewType.ArrayClass
                && m_MetaContent != null
                && m_MetaType != null
                && m_MetaType.IsArray())
            {
                if (!NumberManager.TryUnifyNumericArrayLiteralMembersToDeclaredArrayType(m_MetaContent, m_MetaType, m_Token))
                {
                    if (TryRebuildMetaTypeFromLiteralNumericPromotion())
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                            "数组字面量无法全部强转为当前声明/推断的元素类型，已按数值升阶规则重新推断数组类型为 "
                            + m_MetaType.ToString() + "；请检查与左值类型是否仍可赋值。");
                        if (!NumberManager.TryUnifyNumericArrayLiteralMembersToDeclaredArrayType(m_MetaContent, m_MetaType, m_Token))
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            CheckDefineVariableMetaTypeAndContentMetaType();
        }
        public void SetRealMetaType( MetaType realType )
        {
            m_RealMetaType = realType;
        }

        /// <summary>
        /// 赋值场景下左值为数组类型（如 <c>Array&lt;Int32&gt;</c>）时写入，供 <see cref="CalcReturnType"/> 与右值模板做数值元素对齐。
        /// </summary>
        public void SetAssignmentTargetArrayMetaType(MetaType leftArrayMetaType)
        {
            if (leftArrayMetaType != null && leftArrayMetaType.IsArray())
            {
                m_DefineMetaType = new MetaType(leftArrayMetaType);
            }
        }

        /// <summary>
        /// 按字面量数值升阶规则（与 Parse 阶段一致）重建当前节点的 <see cref="m_MetaType"/>。
        /// 在无法强转为左值声明元素类型时作为兜底推断。
        /// </summary>
        private bool TryRebuildMetaTypeFromLiteralNumericPromotion()
        {
            if (m_MetaContent == null || m_MetaContent.assignStatementsList == null || m_MetaContent.assignStatementsList.Count == 0)
            {
                return false;
            }

            MetaType inputType = m_MetaContent.GetMaxLevelMetaType();
            if (inputType == null)
            {
                return false;
            }

            MetaType newRMT = new MetaType();
            newRMT.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
            newRMT.AddDefineTemplateMetaType(inputType);
            m_MetaType = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(newRMT, true, out _);
            m_MetaType.SetArrayLength(m_MetaContent.assignStatementsList.Count);
            return m_MetaType != null;
        }

        public void CheckDefineVariableMetaTypeAndContentMetaType()
        {
            //这里要解析 定义数组 的类型，与子元素的类型 是否，可以匹配

            if (m_MetaContent == null || m_MetaType == null)
            {
                return;
            }

            for( int i = 0; i < m_MetaContent.count; i++ )
            {
                var mbc = m_MetaContent.assignStatementsList[i];

                if (mbc == null) continue;

                MetaType mt2 = mbc.GetRetMetaType();
                if( m_MetaType.IsArray() )
                {
                    var cmt = m_MetaType.GetMetaTypeByIndex(0);
                    var equalMv = m_MetaContent.equalMetaVariable;
                    bool isNumLike =
                        cmt != null
                        && (ClassManager.IsNumberClass(cmt.metaClass) || ClassManager.IsAbstractNumberMetaType(cmt));
                    // 与左值 equalMetaVariable 的 Array<元素> 上可空一致：元素类型带 ? 或左值侧声明的模板实参为可空
                    bool allowNullableForNumericElement =
                        (cmt?.isNullable == true)
                        || (equalMv?.defineMetaType != null
                            && equalMv.defineMetaType.IsArray()
                            && equalMv.defineMetaType.GetMetaTypeByIndex(0)?.isNullable == true);
                    bool isOmittedExpression = mbc.expressNode == null;
                    bool isNullLiteral = mt2 != null && mt2.isNull;
                    if (isNumLike && (isOmittedExpression || isNullLiteral))
                    {
                        if (!allowNullableForNumericElement)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                                "数组元素为数值/Num 类型时，仅当元素类型可空（?）或左值声明为 Array<可空类型> 时才允许空位或 null 字面量。");
                        }
                        continue;
                    }
                    if (!isNumLike && (isOmittedExpression || isNullLiteral))
                    {
                        // 非数值/Num：空位与 null 字面量均不在这里做强类型对撞
                        continue;
                    }
                }
                if (mt2 == null)
                {
                    continue;
                }

                if( m_MetaType.IsArray() )
                {
                    var cmt = m_MetaType.GetMetaTypeByIndex(0);

                    bool isMatch = false;
                    if (cmt?.GetTemplateMetaClass() == CoreMetaClassManager.objectMetaClass)
                    {
                        // Array<Object> 允许任意元素类型。
                        isMatch = true;
                    }
                    else
                    {
                        isMatch = TypeManager.CompareMetaType(cmt, mt2);
                        if (!isMatch)
                        {
                            if (cmt != null && mt2 != null && cmt.IsArray() && mt2.IsArray())
                            {
                                isMatch = TryArrayElementAssignableForNewObject(cmt, mt2);
                            }
                        }
                    }

                    if (!isMatch)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "里边的元素与边的数据类型不对应，不对应，需要调整数据，或者是定义的结构 ");
                    }
                }
                else
                {
                    bool isMatch = TypeManager.CompareMetaType(m_MetaType, mt2);
                    if (!isMatch)
                    {
                        var cur = m_MetaType.GetTemplateMetaClass();
                        var cmp = mt2.GetTemplateMetaClass();
                        if (cur != null && cmp != null)
                        {
                            var relation = ClassManager.ValidateClassRelationByMetaClass(cur, cmp);
                            isMatch = relation == EClassRelation.Same
                                || relation == EClassRelation.Child
                                || relation == EClassRelation.Interface
                                || relation == EClassRelation.Num;
                        }
                    }

                    if (!isMatch)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "里边的元素与外边定义的类11，不对应，需要调整数据，或者是定义的结构 ");
                    }
                }

            }
        }
        public override MetaType GetReturnMetaDefineType()
        {
            if(this.m_MetaType != null )
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
                if (m_MetaContent != null)
                {
                    for( int i = 0; i < m_MetaContent.count ; i++ )
                    {
                        var bsc = m_MetaContent.assignStatementsList[i];
                        if( bsc == null )
                        {
                            continue;
                        }
                        sb.Append(bsc.metaMemberData?.ToFormatString2(m_MetaType.isDynamicData));

                        if( i < m_MetaContent.count - 1 )
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
                sb.Append(m_MetaContent?.ToFormatString());
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
