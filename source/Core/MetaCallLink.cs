//****************************************************************************
//  File:      MetaCallLink.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/7/29 12:00:00
//  Description:  this's a common node handles
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.source;
using SimpleLanguage.source.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaCallLink
    {
        public List<MetaCallNode> callNodeList => m_CallNodeList;
        public MetaVisitNode finalCallNode => m_FinalCallNode;
        public List<MetaVisitNode> visitNodeList => m_VisitNodeList;
        public AllowUseSettings allowUseSettings { get; private set; }

        private FileMetaCallLink m_FileMetaCallLink;
        private MetaClass m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private List<MetaCallNode> m_CallNodeList = new List<MetaCallNode>();

        private MetaVisitNode m_FinalCallNode = null;
        private List<MetaVisitNode> m_VisitNodeList = new List<MetaVisitNode>();
        public MetaCallLink(FileMetaCallLink fmcl, MetaClass metaClass, MetaBlockStatements mbs, MetaType frontDefineMt, MetaVariable mv )
        {
            m_FileMetaCallLink = fmcl;
            m_OwnerMetaClass = metaClass;
            m_OwnerMetaBlockStatements = mbs;
            CreateCallLinkNode(frontDefineMt, mv );
        }
        public MetaCallLink(MetaVisitNode mvn )
        {
            m_VisitNodeList.Add( mvn );
        }
        private void CreateCallLinkNode(MetaType frontDefineMt, MetaVariable mv )
        {
            MetaCallNode frontMetaNode = null;

            int beginIndex = 1;
            if (m_FileMetaCallLink.callNodeList.Count > 0)
            {
                FileMetaCallNode fmcn = m_FileMetaCallLink.callNodeList[0];
                var firstNode = new MetaCallNode(null, fmcn, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                frontMetaNode = firstNode;
                m_CallNodeList.Add(firstNode);
                firstNode.SetStoreMetaVariable(mv);
                //AddMetaArrayNode(fmcn, frontDefineMt, mv, frontMetaNode);
            }


            for (int i = beginIndex; i < m_FileMetaCallLink.callNodeList.Count; )
            {
                var cn1 = m_FileMetaCallLink.callNodeList[i++];

                if( cn1.token.type == ETokenType.Period)
                {
                    FileMetaCallNode cn2 = null;
                    if (i < m_FileMetaCallLink.callNodeList.Count)
                    {
                        cn2 = m_FileMetaCallLink.callNodeList[i++];
                    }
                    if( cn2 == null )
                    {
                        var fmn1 = new MetaCallNode(null, cn1, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                        fmn1.SetFrontCallNode(frontMetaNode);
                        fmn1.SetStoreMetaVariable(mv);
                        frontMetaNode = fmn1;
                        //AddMetaArrayNode(cn1, frontDefineMt, mv, frontMetaNode);
                    }
                    else
                    {
                        var fmn2 = new MetaCallNode(cn1, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                        fmn2.SetFrontCallNode(frontMetaNode);
                        fmn2.SetStoreMetaVariable(mv);
                        m_CallNodeList.Add(fmn2);                        
                        frontMetaNode = fmn2;
                        //AddMetaArrayNode(cn2, frontDefineMt, mv, frontMetaNode);
                    }
                }
                else
                {
                    var fmn1 = new MetaCallNode(null, cn1, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                    fmn1.SetFrontCallNode(frontMetaNode);
                    fmn1.SetStoreMetaVariable(mv);
                    frontMetaNode = fmn1;
                    m_CallNodeList.Add(fmn1);
                    //AddMetaArrayNode(cn1, frontDefineMt, mv, frontMetaNode);

                    FileMetaCallNode cn2 = null;
                    if (i  < m_FileMetaCallLink.callNodeList.Count)
                    {
                        cn2 = m_FileMetaCallLink.callNodeList[i++];
                    }
                    if (cn2 == null) continue;

                    var fmn2 = new MetaCallNode(null, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                    fmn2.SetFrontCallNode(fmn1);
                    fmn2.SetStoreMetaVariable(mv);
                    m_CallNodeList.Add(fmn2);
                    frontMetaNode = fmn2;
                    //AddMetaArrayNode(cn2, frontDefineMt, mv, frontMetaNode  );
                }
            }

            var m_FinalMetaCallNode = frontMetaNode;
            if( m_FinalMetaCallNode == null )
            {
                Log.AddInStructMeta(EError.None, "Error 连接串没有找到合适的节点  360!!!");
            }
            m_FinalMetaCallNode.SetDefineMetaVariable(mv);
        }
        /*
        void AddMetaArrayNode(FileMetaCallNode cn2, MetaType frontDefineMt, MetaVariable mv, MetaCallNode frontMetaNode )
        {
            if( cn2.isArray )
            {
                if( cn2.fileMetaBracketTermList.Count > 3 )
                {
                    Log.AddInStructMeta(EError.None, "Error 数组不能超过三维!!");
                }

                for (int j = 0; j < cn2.fileMetaBracketTermList.Count; j++)
                {
                    var arraycontent = cn2.fileMetaBracketTermList[j];
                    var firstNode = new MetaCallNode(null, fmcn, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                    frontMetaNode = firstNode;
                    m_CallNodeList.Add(firstNode);

                    MetaCallLink cmcl = new MetaCallLink(cn2.fileMetaBracketTermList[j], m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt, mv, frontMetaNode);
                    for (int i = 0; i < cmcl.m_CallNodeList.Count; i++)
                    {
                        cmcl.m_CallNodeList[i].SetVisitFlag(true);
                    }
                    m_CallNodeList.AddRange(cmcl.m_CallNodeList);
                }
            }
            //if (cn2.arrayNodeList.Count > 0)
            //{
            //    if (cn2.arrayNodeList.Count > 3)
            //    {
            //        Log.AddInStructMeta(EError.None, "Error 数组不能超过三维!!");
            //    }
            //    for (int j = 0; j < cn2.arrayNodeList.Count; j++)
            //    {
            //        MetaCallLink cmcl = new MetaCallLink(cn2.arrayNodeList[j], m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt, mv, frontMetaNode);
            //        for( int i = 0; i < cmcl.m_CallNodeList.Count; i++ )
            //        {
            //            cmcl.m_CallNodeList[i].SetVisitFlag(true);
            //        }
            //        m_CallNodeList.AddRange(cmcl.m_CallNodeList);
            //    }
            //}
        }
        */
        public Token GetToken() { return null; }
        public bool Parse( AllowUseSettings _useConst )
        {
            allowUseSettings = new AllowUseSettings(_useConst);
            allowUseSettings.setterFunction = false;
            allowUseSettings.getterFunction = true;
            bool flag = true;
            List<MetaCallNode> newList = new List<MetaCallNode>();
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                if (flag)
                {
                    if( i == m_CallNodeList.Count - 1 )
                    {
                        allowUseSettings.setterFunction = _useConst.setterFunction;
                        allowUseSettings.getterFunction = _useConst.getterFunction;
                    }
                    flag = m_CallNodeList[i].ParseNode(allowUseSettings);

                    if (m_CallNodeList[i].callNodeType == ECallNodeType.NewClass 
                        || m_CallNodeList[i].callNodeType == ECallNodeType.NewData )
                    {
                        if( i < m_CallNodeList.Count - 1 )
                        {
                            flag = false;
                            Log.AddInStructMeta(EError.None, "Parse Statement Error 在使用NewClassName的方式，后边不允许有其它的调用!");
                        }
                    }
                    if( flag )
                    {
                        newList.Add(m_CallNodeList[i]);
                        var cnt = m_CallNodeList[i];
                        if ( (cnt.callNodeType == ECallNodeType.MemberVariableName
                            || cnt.callNodeType == ECallNodeType.FunctionInnerVariableName
                            || cnt.callNodeType == ECallNodeType.ClassName ) 
                            && cnt.bracketExpressList.Count > 0 )
                        {
                            if (cnt.metaVariable != null)
                            {
                                var frontcn = cnt;
                                if( cnt.metaVariable.isArray )
                                {
                                    //arryobject.@i arrayobject.@1
                                    if( cnt.bracketExpressList.Count <= cnt.metaVariable.realMetaType.ArrayDimension() )
                                    {
                                        for (int j = 0; j < cnt.bracketExpressList.Count; j++)
                                        {
                                            MetaCallNode mcn = new MetaCallNode(cnt.bracketExpressList[j], cnt.ownerMetaFunctionBlock.ownerMetaClass, cnt.ownerMetaFunctionBlock, cnt.metaType );
                                            mcn.SetFrontCallNode(frontcn);
                                            mcn.ParseNode(allowUseSettings);
                                            newList.Add(mcn);
                                            frontcn = mcn;
                                        }
                                        if (m_CallNodeList.Count > i + 1  )
                                        {
                                            m_CallNodeList[i + 1].SetFrontCallNode( frontcn );
                                        }
                                    }
                                    else
                                    {
                                        Log.AddInStructMeta(EError.None, "Parse 使用[][][] 访问超过了数组的维度!");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "");
                    }
                }
            }

            if ( flag )
            {
                m_VisitNodeList.Clear();
                int i = 0;
                MetaCallNode frontNode = null;
                while (true)
                {
                    if( i >= newList.Count )
                    {
                        break;
                    }
                    MetaCallNode mcn = newList[i++];
                    if( mcn == null )
                    {
                        break;
                    }
                    AddVisitNodeList(i, mcn, frontNode);

                    frontNode = mcn;
                }
            }
            if( m_VisitNodeList != null && m_VisitNodeList.Count > 0 )
            {
                m_FinalCallNode = m_VisitNodeList[m_VisitNodeList.Count - 1];
            }
            else
            {
                Log.AddInStructMeta( EError.None, "Error 解析执行链出错");
                flag = false;
            }

            return flag;
        }
        public List<MetaCallNode> CreateMetaCallNodeList(MetaExpressNode belc)
        {
            List<MetaCallNode> bracketCNList = new List<MetaCallNode>();
            switch (belc)
            {
                case MetaConstExpressNode mcen:
                    {
                        var newmcn = new MetaCallNode(mcen, m_OwnerMetaClass, m_OwnerMetaBlockStatements, mcen.metaType );
                        bracketCNList.Add(newmcn);
                    }
                    break;
                case MetaCallLinkExpressNode mclen:
                    {
                        bracketCNList.AddRange(mclen.metaCallLink.callNodeList);
                    }
                    break;
                case MetaArrayExpressNode maen:
                    {
                        for (int k = 0; k < maen.metaCallArray.Count; k++)
                        {
                            MetaExpressNode cen = maen.metaCallArray[k];
                            var bcnList = CreateMetaCallNodeList(cen);
                            bracketCNList.AddRange(bcnList);
                        }
                    }
                    break;
                default:
                    {
                        Log.AddInStructMeta(EError.None, "解析嵌套expressList 的时候发生了问题!");
                    }
                    break;
            }
            return bracketCNList;
        }
        public void AddVisitNodeList( int index, MetaCallNode mcn, MetaCallNode frontNode)
        {
            if (mcn.callNodeType == ECallNodeType.This)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByThis(mcn.metaVariable);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.Base)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByBase(mcn.metaVariable);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.FunctionInnerVariableName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.metaType);
                m_VisitNodeList.Add(mvn);
            }
            else
            if (mcn.callNodeType == ECallNodeType.MemberVariableName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.callMetaType);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.MemberFunctionName)
            {
                MetaVariable newmv = null;
                MetaMethodCall mmc = null;
                if (frontNode?.callNodeType == ECallNodeType.ConstValue)
                {
                    MetaVisitNode fvn = m_VisitNodeList[m_VisitNodeList.Count - 1];
                    m_VisitNodeList.Remove(fvn);

                    //MetaMemberVariable mmv = frontNode.m_MetaClass.GetMetaMemberVariableByName("value");

                    //MetaBraceAssignStatements mas = new MetaBraceAssignStatements(frontNode.ownerMetaFunctionBlock,fvn.constValueExpress, mmv);
                    //MetaBraceOrBracketStatementsContent mbobs = new MetaBraceOrBracketStatementsContent(frontNode.ownerMetaFunctionBlock, frontNode.m_MetaClass);

                    //mbobs.assignStatementsList.Add(mas);

                    string name = "auto_constvalue_" + fvn.constValueExpress.eType.ToString() + "_" + fvn.constValueExpress.value.ToString();
                    newmv = frontNode.ownerMetaFunctionBlock.GetMetaVariable(name);
                    if (newmv == null)
                    {
                        var mccm = CoreMetaClassManager.GetMetaClassByEType(fvn.constValueExpress.eType);
                        newmv = new MetaVariable(name, MetaVariable.EVariableFrom.LocalStatement,
                        frontNode.ownerMetaFunctionBlock, frontNode.metaType.metaClass, new MetaType(mccm));

                        frontNode.ownerMetaFunctionBlock.AddMetaVariable(newmv);
                    }

                    MetaVisitNode mvn1 = MetaVisitNode.CreateByNewClass(frontNode.metaType, newmv);
                    m_VisitNodeList.Add(mvn1);

                    mmc = new MetaMethodCall(mcn.callMetaType.metaClass, mcn.callMetaType.defineTemplateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, mcn.metaInputParamCollection, newmv, mcn.storeMetaVariable);
                }
                else
                {
                    var retmv = frontNode?.metaVariable;
                    if (m_VisitNodeList.Count > 0 && retmv != null)
                    {
                        m_VisitNodeList.RemoveAt(m_VisitNodeList.Count - 1);
                    }
                    mmc = new MetaMethodCall(mcn.callMetaType.metaClass, mcn.callMetaType.defineTemplateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, mcn.metaInputParamCollection, retmv, mcn.storeMetaVariable);
                }

                MetaVisitNode mvn2 = MetaVisitNode.CreateByMethodCall(mmc);
                m_VisitNodeList.Add(mvn2);
            }
            else if (mcn.callNodeType == ECallNodeType.ConstValue)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByConstExpress(mcn.metaExpressValue as MetaConstExpressNode, mcn.metaVariable);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.ClassName)
            {
                if( mcn.bracketExpressList.Count > 0 )
                {
                    MetaClass cmc = mcn.metaType.metaClass;
                    MetaVisitNode mvn = MetaVisitNode.CreateByNewArrayClass(mcn.metaType, mcn.bracketExpressList, mcn.storeMetaVariable);
                    m_VisitNodeList.Add(mvn);
                }
                else
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByVisitMetaClass(mcn.metaType);
                    m_VisitNodeList.Add(mvn);
                }
            }
            else if (mcn.callNodeType == ECallNodeType.TypeName)
            {
                if (mcn.bracketExpressList.Count > 0)
                {
                    MetaClass cmc = mcn.metaType.metaClass;
                    MetaVisitNode mvn = MetaVisitNode.CreateByNewArrayClass(mcn.metaType, mcn.bracketExpressList, mcn.storeMetaVariable);
                    m_VisitNodeList.Add(mvn);
                }
                else
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByVisitMetaClass(mcn.metaType);
                    m_VisitNodeList.Add(mvn);
                }
            }
            else if (mcn.callNodeType == ECallNodeType.NewClass)
            {
                if (index == m_CallNodeList.Count)
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByNewClass(mcn.metaType, mcn.metaVariable);
                    m_VisitNodeList.Add(mvn);

                    if (mcn.metaFunction != null)
                    {
                        MetaMethodCall mmc = new MetaMethodCall(mcn.metaType.metaClass, null, mcn.metaFunction, null, mcn.metaInputParamCollection, null, mcn.storeMetaVariable);
                        mvn.SetMethodCall(mmc);
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Error 使用NewClass方式，后边不允许跟其它变量相关内容!");
                }
            }
            else if (mcn.callNodeType == ECallNodeType.NewTemplate)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByNewTemplate(mcn.metaType, mcn.metaFunction, mcn.storeMetaVariable);
                MetaClass cmc = mcn.metaType.metaClass;
                MetaMethodCall mmc = new MetaMethodCall(mcn.metaType.metaClass, null, mcn.metaFunction, null, mcn.metaInputParamCollection, null, mcn.storeMetaVariable);
                mvn.SetMethodCall(mmc);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.NewData)
            {
                MetaVisitNode mvn = MetaVisitNode.CraeteByNewData(mcn.metaType);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.EnumName)
            {
                //Debug.Write("Meta Common Parse IteratorVariable----------------------------------------------------");
            }
            else if (mcn.callNodeType == ECallNodeType.EnumValueArray)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByEnumDefaultValue(mcn.metaType, mcn.metaVariable);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.VisitVariable)
            {
                //if( mcn.extraAddLoadVariable )
                //{
                //    MetaVisitNode mvn1 = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                //    m_VisitNodeList.Add(mvn1);
                //}
                if( mcn.metaVariable is MetaVisitVariable mvv )
                {
                    MetaVisitNode mvn1 = MetaVisitNode.CreateByVisitVariable(mvv);
                    m_VisitNodeList.Add(mvn1);
                }
                //for( int i = 0; i < mcn.metaArrayCallNodeList.Count; i++ )
                //{
                //    m_VisitNodeList.AddRange(mcn.metaArrayCallNodeList[i].m_VisitNodeList);
                //}
            }
            else if (mcn.callNodeType == ECallNodeType.IteratorVariable)
            {
                Log.AddInStructMeta(EError.None, "Meta Common Parse IteratorVariable----------------------------------------------------");
            }
            else if (mcn.callNodeType == ECallNodeType.DataName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.EnumDefaultValue)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByEnumDefaultValue(mcn.metaType, mcn.metaVariable);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.MemberDataName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                m_VisitNodeList.Add(mvn);
                //Debug.Write("Meta Common Parse MemberDataName----------------------------------------------------");
            }
            else if( mcn.callNodeType == ECallNodeType.TemplateName )
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByTemplate(mcn.metaTemplate);
                m_VisitNodeList.Add(mvn);
            }
            else if( mcn.callNodeType == ECallNodeType.Express )
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByEpxress(mcn.metaExpressValue);
                m_VisitNodeList.Add(mvn);
            }
        }

        public MetaMemberFunction GetInitMemberFunction( MetaClass curmc )
        {
            MetaMemberFunction mmf = curmc.GetMetaMemberConstructDefaultFunction();

            return mmf;
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
            return m_FinalCallNode?.GetRetMetaVariable();
        }
        public MetaClass ExecuteGetMetaClass()
        {
            return m_FinalCallNode?.GetMetaClass();
        }
        public MetaExpressNode GetMetaExpressNode()
        {
            //if( m_FinalMetaCallNode.callNodeType == ECallNodeType.ConstValue )
            //{
            //    return new MetaConstExpressNode(EType.Int32, m_FinalMetaCallNode.constValue);
            //}
            return null;
        }
        public MetaType GetMetaDefineType()
        {
            MetaType mt = null;
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                mt = m_VisitNodeList[i].GetMetaType();
            }
            return mt;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.m_VisitNodeList.Count; i++)
            {
                sb.Append(m_VisitNodeList[i].ToString());
                if (i < this.m_VisitNodeList.Count - 1)
                    sb.Append("  ->  ");
            }
            return sb.ToString();
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.m_VisitNodeList.Count; i++)
            {
                sb.Append(m_VisitNodeList[i].ToFormatString() );
                if( i < this.m_VisitNodeList.Count - 1 )
                    sb.Append("  ->  ");
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
