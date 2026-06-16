//****************************************************************************
//  File:      MetaCallLink.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/7/29 12:00:00
//  Description:  this's a common node handles
//****************************************************************************
using SimpleLanguage.Compile;

using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaCallLink
    {
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public List<MetaCallNode> callNodeList => m_CallNodeList;
        public MetaVisitNode finalCallNode => m_FinalCallNode;
        public List<MetaVisitNode> visitNodeList => m_VisitNodeList;
        public AllowUseSettings allowUseSettings { get; private set; } = null;

        private FileMetaCallLink m_FileMetaCallLink;
        private MetaBase m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private List<MetaCallNode> m_CallNodeList = new List<MetaCallNode>();
        private MetaVariable m_StoreMetaVariable = null;

        private MetaVisitNode m_FinalCallNode = null;
        private List<MetaVisitNode> m_VisitNodeList = new List<MetaVisitNode>();
        public MetaClass ownerMetaClass => m_OwnerMetaClass as MetaClass;
        public MetaData ownerMetaData => m_OwnerMetaClass as MetaData;
        public MetaEnum ownerMetaEnum => m_OwnerMetaClass as MetaEnum;
        public MetaBase ownerMetaBase => m_OwnerMetaClass;

        public MetaCallLink(FileMetaCallLink fmcl, MetaBase metaOwner, MetaBlockStatements mbs, MetaType frontDefineMt, MetaVariable mv)
        {
            m_FileMetaCallLink = RewriteLocalCallLinkIfNeed(fmcl, metaOwner);
            m_OwnerMetaClass = metaOwner;
            m_OwnerMetaBlockStatements = mbs;
            m_StoreMetaVariable = mv;
            CreateCallLinkNode(frontDefineMt, mv);
        }
        public MetaCallLink(MetaBase omc, MetaType frontDefineMt, MetaVariable mv)
        {
            m_OwnerMetaClass = omc;
            m_OwnerMetaBlockStatements = null;
            CreateCallLinkNode(frontDefineMt, mv);
        }

        private static FileMetaCallLink RewriteLocalCallLinkIfNeed(FileMetaCallLink fmcl, MetaBase ownerMc)
        {
            if (fmcl == null || ownerMc == null) return fmcl;
            if (fmcl.callNodeList == null || fmcl.callNodeList.Count == 0) return fmcl;

            // local.xxx => local_<fileHash>.xxx (instance stored on globalData)
            var first = fmcl.callNodeList[0];
            if (first == null) return fmcl;
            if (first.name != "local") return fmcl;

            var fileMeta = first.fileMeta;
            if (fileMeta == null) return fmcl;

            // Only allow local usage in the file that defines local{}.
            if (fileMeta.GetFileMetaLocalSyntax() == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 褰撳墠鏂囦欢鏈畾涔?local{}锛屼笉鍏佽浣跨敤 local.xxx" + (first.token != null ? (" " + first.token.ToLexemeAllString()) : ""));
                return fmcl;
            }

            var localVarName = "local_" + fileMeta.path.GetHashCode();

            // Create a new call link based on a synthetic Node chain: localVarName + original suffix
            var baseToken = new Token(fileMeta.path, ETokenType.Identifier, localVarName, first.token?.sourceBeginLine ?? 0, first.token?.sourceBeginChar ?? 0);
            var baseNode = new Node(baseToken) { nodeType = ENodeType.IdentifierLink };

            // Copy original chain tokens except the leading 'local'
            for (int i = 1; i < fmcl.callNodeList.Count; i++)
            {
                var cn = fmcl.callNodeList[i];
                if (cn == null) continue;
                var t = cn.token;
                if (t == null) continue;
                var n = new Node(t);
                n.nodeType = t.type == ETokenType.Period ? ENodeType.Period : ENodeType.IdentifierLink;
                baseNode.AddLinkNode(n);
            }

            return new FileMetaCallLink(fileMeta, baseNode, true);
        }
        public MetaCallLink(MetaVisitNode mvn)
        {
            m_VisitNodeList.Add(mvn);
            m_FinalCallNode = mvn;
        }
        private void CreateCallLinkNode(MetaType frontDefineMt, MetaVariable mv)
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


            for (int i = beginIndex; i < m_FileMetaCallLink.callNodeList.Count;)
            {
                var cn1 = m_FileMetaCallLink.callNodeList[i++];

                if (cn1.token.type == ETokenType.Period)
                {
                    FileMetaCallNode cn2 = null;
                    if (i < m_FileMetaCallLink.callNodeList.Count)
                    {
                        cn2 = m_FileMetaCallLink.callNodeList[i++];
                    }
                    if (cn2 == null)
                    {
                        var fmn1 = new MetaCallNode(null, cn1, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                        fmn1.SetFrontCallNode(frontMetaNode);
                        frontMetaNode = fmn1;
                        //AddMetaArrayNode(cn1, frontDefineMt, mv, frontMetaNode);
                    }
                    else
                    {
                        var fmn2 = new MetaCallNode(cn1, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                        fmn2.SetFrontCallNode(frontMetaNode);
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
                    if (i < m_FileMetaCallLink.callNodeList.Count)
                    {
                        cn2 = m_FileMetaCallLink.callNodeList[i++];
                    }
                    if (cn2 == null) continue;

                    var fmn2 = new MetaCallNode(null, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements, frontDefineMt);
                    fmn2.SetFrontCallNode(fmn1);
                    m_CallNodeList.Add(fmn2);
                    frontMetaNode = fmn2;
                    //AddMetaArrayNode(cn2, frontDefineMt, mv, frontMetaNode  );
                }
            }

            var m_FinalMetaCallNode = frontMetaNode;
            if (m_FinalMetaCallNode == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 杩炴帴涓叉病鏈夋壘鍒板悎閫傜殑鑺傜偣  360!!!");
            }
            m_FinalMetaCallNode.SetStoreMetaVariable(mv);
        }
        /*
        void AddMetaArrayNode(FileMetaCallNode cn2, MetaType frontDefineMt, MetaVariable mv, MetaCallNode frontMetaNode )
        {
            if( cn2.isArray )
            {
                if( cn2.fileMetaBracketTermList.Count > 3 )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 鏁扮粍涓嶈兘瓒呰繃涓夌淮!!");
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
            //        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 鏁扮粍涓嶈兘瓒呰繃涓夌淮!!");
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
        public bool Parse(AllowUseSettings _useConst)
        {
            if ((m_CallNodeList == null || m_CallNodeList.Count == 0)
                && m_VisitNodeList != null
                && m_VisitNodeList.Count > 0)
            {
                m_FinalCallNode = m_VisitNodeList[m_VisitNodeList.Count - 1];
                return true;
            }

            allowUseSettings = new AllowUseSettings(_useConst);
            allowUseSettings.setterFunction = false;
            allowUseSettings.getterFunction = true;
            bool flag = true;
            List<MetaCallNode> newList = new List<MetaCallNode>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                if (flag)
                {
                    if (i == m_CallNodeList.Count - 1)
                    {
                        allowUseSettings.setterFunction = _useConst.setterFunction;
                        allowUseSettings.getterFunction = _useConst.getterFunction;
                        allowUseSettings.expressNodeList = _useConst.expressNodeList;
                    }
                    else
                    {
                        allowUseSettings.getterFunction = true;
                    }
                    flag = m_CallNodeList[i].ParseNode(allowUseSettings);

                    if (m_CallNodeList[i].callNodeType == ECallNodeType.NewClass
                        || m_CallNodeList[i].callNodeType == ECallNodeType.NewData)
                    {
                        if (i < m_CallNodeList.Count - 1)
                        {
                            flag = false;
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Parse Statement Error 鍦ㄤ娇鐢∟ewClassName鐨勬柟寮忥紝鍚庤竟涓嶅厑璁告湁鍏跺畠鐨勮皟鐢?");
                        }
                    }
                    if (flag)
                    {
                        newList.Add(m_CallNodeList[i]);
                        var cnt = m_CallNodeList[i];
                        sb.Append(m_CallNodeList[i].name);
                        sb.Append(".");
                        if ((cnt.callNodeType == ECallNodeType.MemberVariableName
                            || cnt.callNodeType == ECallNodeType.FunctionInnerVariableName
                            || cnt.callNodeType == ECallNodeType.ClassName)
                            && cnt.bracketExpressList.Count > 0)
                        {
                            if (cnt.metaVariable != null)
                            {
                                var frontcn = cnt;
                                if (cnt.metaVariable.isArray)
                                {
                                    //arryobject.@i arrayobject.@1
                                    MetaType mtt = cnt.metaVariable.GetFinalMetaType();
                                    if (cnt.bracketExpressList.Count <= mtt.ArrayDimension() )
                                    {
                                        for (int j = 0; j < cnt.bracketExpressList.Count; j++)
                                        {
                                            MetaCallNode mcn = new MetaCallNode(cnt.bracketExpressList[j], cnt.ownerMetaFunctionBlock.ownerMetaClass, cnt.ownerMetaFunctionBlock, cnt.metaType);
                                            mcn.SetFrontCallNode(frontcn);
                                            mcn.ParseNode(allowUseSettings);
                                            newList.Add(mcn);
                                            frontcn = mcn;
                                        }
                                        if (m_CallNodeList.Count > i + 1)
                                        {
                                            m_CallNodeList[i + 1].SetFrontCallNode(frontcn);
                                        }
                                    }
                                    else
                                    {
                                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Parse 浣跨敤[][][] 璁块棶瓒呰繃浜嗘暟缁勭殑缁村害!");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreParseCallNodeLinkFailed, m_CallNodeList[i].token, "callLink", sb.ToString(), m_CallNodeList[i].token.ToLexemeAllString() );
                    }
                }
            }

            sb.Clear();
            if (flag)
            {
                m_VisitNodeList.Clear();
                int i = 0;
                MetaCallNode frontNode = null;
                while (true)
                {
                    if (i >= newList.Count)
                    {
                        break;
                    }
                    MetaCallNode mcn = newList[i++];
                    if (mcn == null)
                    {
                        break;
                    }
                    AddVisitNodeList(i, mcn, frontNode);

                    sb.Append($"[Pos:{i} Name:{mcn.name} Status:{"OK"}");

                    frontNode = mcn;
                }
            }
            if (m_VisitNodeList != null && m_VisitNodeList.Count > 0)
            {
                m_FinalCallNode = m_VisitNodeList[m_VisitNodeList.Count - 1];
            }
            else
            {
                Log.AddMetaCoreLog(LID.MetaCoreParseCallLinkFailed, sb.ToString() );
                flag = false;
            }

            return flag;
        }
        public List<MetaCallNode> CreateMetaCallNodeList(MetaExpressNodeBase belc)
        {
            List<MetaCallNode> bracketCNList = new List<MetaCallNode>();
            switch (belc)
            {
                case MetaConstExpressNode mcen:
                    {
                        var newmcn = new MetaCallNode(mcen, m_OwnerMetaClass, m_OwnerMetaBlockStatements, mcen.expressReturnMetaType);
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
                            MetaExpressNodeBase cen = maen.metaCallArray[k];
                            var bcnList = CreateMetaCallNodeList(cen);
                            bracketCNList.AddRange(bcnList);
                        }
                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "瑙ｆ瀽宓屽expressList 鐨勬椂鍊欏彂鐢熶簡闂!");
                    }
                    break;
            }
            return bracketCNList;
        }
        public void AddVisitNodeList(MetaVisitNode mvn)
        {
            m_VisitNodeList.Add(mvn);
        }
        public void AddVisitNodeList(int index, MetaCallNode mcn, MetaCallNode frontNode)
        {
            if (mcn.callNodeType == ECallNodeType.This)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByThis(mcn.metaVariable);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.Base)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByBase(mcn.metaVariable);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.FunctionInnerVariableName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.metaType);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.MemberVariableName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.callMetaType);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.MemberFunctionName)
            {
                MetaVariable newmv = null;
                MetaMethodCall mmc = null;
                // Some call shapes attach "parTerm" (the call arg list) to the receiver node,
                // not to the function-name node. If the current node has no input param collection,
                // fall back to the previous node's collection to keep debug/IR symmetric.
                var paramCollection = mcn.metaInputParamCollection;
                if (paramCollection == null)
                    paramCollection = frontNode?.metaInputParamCollection;

                var debugParTermText = mcn.fileMetaParTerm?.ToFormatString();
                if (string.IsNullOrEmpty(debugParTermText))
                    debugParTermText = frontNode?.fileMetaParTerm?.ToFormatString();
                if (frontNode?.callNodeType == ECallNodeType.ConstValue)
                {
                    MetaVisitNode fvn = m_VisitNodeList[m_VisitNodeList.Count - 1];
                    m_VisitNodeList.Remove(fvn);

                    //MetaMemberVariable mmv = frontNode.m_MetaClass.GetMetaMemberVariableByName("value");

                    //MetaBraceAssignStatements mas = new MetaBraceAssignStatements(frontNode.ownerMetaFunctionBlock,fvn.constValueExpress, mmv);
                    //MetaBraceOrBracketStatementsContent mbobs = new MetaBraceOrBracketStatementsContent(frontNode.ownerMetaFunctionBlock, frontNode.m_MetaClass);

                    //mbobs.assignStatementsList.Add(mas);

                    //string name = "auto_constvalue_" + fvn.constValueExpress.eType.ToString() + "_" + fvn.constValueExpress.GetHashCode();
                    //newmv = frontNode.ownerMetaFunctionBlock.GetMetaVariable(name);
                    //if (newmv == null)
                    //{
                    //    Debug.Assert(false, "娌℃湁鍒涘缓const鍙橀噺!");
                    //    //var mccm = CoreMetaClassManager.GetMetaClassByEType(fvn.constValueExpress.eType);
                    //    //newmv = new MetaVariable(name, MetaVariable.EVariableFrom.LocalStatement,
                    //    //frontNode.ownerMetaFunctionBlock, frontNode.metaType.metaClass, new MetaType(mccm));

                    //    //frontNode.ownerMetaFunctionBlock.AddMetaVariable(newmv);
                    //}

                    MetaVisitNode mvn1 = MetaVisitNode.CreateByNewConst(frontNode.ownerMetaClass, frontNode.ownerMetaFunctionBlock,
                        frontNode.metaType, frontNode.metaExpressValue as MetaConstExpressNode, frontNode.metaFunction as MetaMemberFunction,
                        frontNode.metaInputParamCollection );
                    mvn1.SetToken(frontNode.token);
                    m_VisitNodeList.Add(mvn1);

                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.callMetaType.metaClass, mcn.callMetaType.defineTemplateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, null, null);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }
                else
                {
                    var retmv = frontNode?.metaVariable;
                    //if (m_VisitNodeList.Count > 0 && retmv != null)
                    //{
                    //    m_VisitNodeList.RemoveAt(m_VisitNodeList.Count - 1);
                    //}
                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.callMetaType.metaClass, mcn.callMetaType.defineTemplateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, retmv, mcn.storeMetaVariable);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }

                MetaVisitNode mvn2 = MetaVisitNode.CreateByMethodCall(mmc);
                m_VisitNodeList.Add(mvn2);
            }
            else if (mcn.callNodeType == ECallNodeType.FunctionCall)
            {
                // function-like call that isn't a named member function (e.g. type() getter)
                MetaVariable newmv = null;
                MetaMethodCall mmc = null;
                var paramCollection = mcn.metaInputParamCollection;
                if (paramCollection == null)
                    paramCollection = frontNode?.metaInputParamCollection;

                var debugParTermText = mcn.fileMetaParTerm?.ToFormatString();
                if (string.IsNullOrEmpty(debugParTermText))
                {
                    debugParTermText = frontNode?.fileMetaParTerm?.ToFormatString();
                }
                if (frontNode?.callNodeType == ECallNodeType.ConstValue)
                {
                    MetaVisitNode fvn = m_VisitNodeList[m_VisitNodeList.Count - 1];
                    m_VisitNodeList.Remove(fvn);

                    //string name = "auto_constvalue_" + fvn.constValueExpress.eType.ToString() + "_" + fvn.constValueExpress.GetHashCode();
                    //newmv = frontNode.ownerMetaFunctionBlock.GetMetaVariable(name);
                    //if (newmv == null)
                    //{
                    //    Debug.Assert(false, "娌℃湁鍒涘缓const鍙橀噺!");
                    //}

                    MetaVisitNode mvn1 = MetaVisitNode.CreateByNewConst(frontNode.ownerMetaClass, frontNode.ownerMetaFunctionBlock,
                        frontNode.metaType, frontNode.metaExpressValue as MetaConstExpressNode, frontNode.metaFunction as MetaMemberFunction,
                        frontNode.metaInputParamCollection );
                    m_VisitNodeList.Add(mvn1);

                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.callMetaType?.metaClass, mcn.callMetaType?.defineTemplateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, null, null);
                    mvn1.SetToken(mcn.token);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }
                else
                {
                    var retmv = frontNode?.metaVariable;
                    if (m_VisitNodeList.Count > 0 && retmv != null)
                    {
                        m_VisitNodeList.RemoveAt(m_VisitNodeList.Count - 1);
                    }
                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.callMetaType?.metaClass, mcn.callMetaType?.defineTemplateMetaTypeList, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, retmv, mcn.storeMetaVariable);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }

                MetaVisitNode mvn2 = MetaVisitNode.CreateByMethodCall(mmc);
                m_VisitNodeList.Add(mvn2);
            }
            else if (mcn.callNodeType == ECallNodeType.SystemFunctionCall)
            {
                MetaMethodCall mmc;
                var retmv = frontNode?.metaVariable;
                if (m_VisitNodeList.Count > 0 && retmv != null)
                {
                    m_VisitNodeList.RemoveAt(m_VisitNodeList.Count - 1);
                }
                var paramCollection = mcn.metaInputParamCollection;
                if (paramCollection == null)
                    paramCollection = frontNode?.metaInputParamCollection;

                var debugParTermText = mcn.fileMetaParTerm?.ToFormatString();
                if (string.IsNullOrEmpty(debugParTermText))
                    debugParTermText = frontNode?.fileMetaParTerm?.ToFormatString();
                mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock,
                    mcn.metaFunction,
                    mcn.metaTemplateParamsList,
                    paramCollection);
                mmc.SetDebugInputParTermText(debugParTermText);

                MetaVisitNode mvn2 = MetaVisitNode.CreateBySystemCall(mmc);
                mvn2.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn2);
            }
            else if (mcn.callNodeType == ECallNodeType.ConstValue)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByConstExpress(mcn.metaExpressValue as MetaConstExpressNode);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.ClassName)
            {
                if (mcn.bracketExpressList.Count > 0)
                {
                    MetaClass cmc = mcn.metaType.metaClass;
                    MetaVisitNode mvn = MetaVisitNode.CreateByNewArrayClass(mcn.metaType, mcn.bracketExpressList);
                    mvn.SetToken(mcn.token);
                    m_VisitNodeList.Add(mvn);
                }
                else
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByVisitMetaClass(mcn.metaType); mvn.SetToken(mcn.token);
                    m_VisitNodeList.Add(mvn);
                }
            }
            // typealias（如 ObjectArray -> Array<Object>）解析为 MetaType；须生成与 ClassName/NewClass 一致的 visit，否则嵌套 ObjectArray(n){} 无 New 语义、后续 Meta 失败
            else if (mcn.callNodeType == ECallNodeType.MetaType)
            {
                if (mcn.metaType != null && mcn.metaType.IsArray())
                {
                    if (mcn.bracketExpressList.Count > 0)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByNewArrayClass(mcn.metaType, mcn.bracketExpressList);
                        mvn.SetToken(mcn.token);
                        m_VisitNodeList.Add(mvn);
                    }
                    else if (index == m_CallNodeList.Count)
                    {
                        MetaVisitNode mvn = MetaVisitNode.CreateByNewClass(mcn.metaType); 
                        mvn.SetToken(mcn.token);
                        m_VisitNodeList.Add(mvn);
                        if (mcn.metaFunction != null)
                        {
                            MetaMethodCall mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType.metaClass, null, mcn.metaFunction, null, mcn.metaInputParamCollection, null, mcn.storeMetaVariable);
                            mvn.SetMethodCall(mmc);
                        }
                    }
                }
                else if (mcn.metaType != null)
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByVisitMetaClass(mcn.metaType);
                    mvn.SetToken(mcn.token);
                    m_VisitNodeList.Add(mvn);
                }
            }
            else if (mcn.callNodeType == ECallNodeType.DataName)
            {
                if (mcn.metaVariable != null)
                {
                    // data 类型名参与成员访问时（如 AA.a 且 a 为实例字段），
                    // 先把 AA 的默认静态实例压栈，再由后续 MemberDataName 取字段。
                    MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                    mvn.SetToken(mcn.token);
                    m_VisitNodeList.Add(mvn);
                }
                else
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByVisitMetaData(mcn.metaType);
                    mvn.SetToken(mcn.token);
                    m_VisitNodeList.Add(mvn);
                }
            }
            else if (mcn.callNodeType == ECallNodeType.EnumName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVisitMetaEnum(mcn.metaType);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            //else if (mcn.callNodeType == ECallNodeType.TypeName)
            //{
            //    if (mcn.bracketExpressList.Count > 0)
            //    {
            //        MetaClass cmc = mcn.metaType.metaClass;
            //        MetaVisitNode mvn = MetaVisitNode.CreateByNewArrayClass(mcn.metaType, mcn.bracketExpressList, mcn.storeMetaVariable);
            //        mvn.SetToken(mcn.token);
            //        m_VisitNodeList.Add(mvn);
            //    }
            //    else
            //    {
            //        MetaVisitNode mvn = MetaVisitNode.CreateByVisitMetaClass(mcn.metaType);
            //        mvn.SetToken(mcn.token);
            //        m_VisitNodeList.Add(mvn);
            //    }
            //}
            else if (mcn.callNodeType == ECallNodeType.NewClass)
            {
                if (index == m_CallNodeList.Count)
                {
                    MetaVisitNode mvn = MetaVisitNode.CreateByNewClass(mcn.metaType);
                    mvn.SetToken(mcn.token);
                    m_VisitNodeList.Add(mvn);

                    if (mcn.metaFunction != null)
                    {
                        MetaMethodCall mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType.metaClass, null, mcn.metaFunction, null, mcn.metaInputParamCollection, null, mcn.storeMetaVariable);
                        mvn.SetMethodCall(mmc);
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 浣跨敤NewClass鏂瑰紡锛屽悗杈逛笉鍏佽璺熷叾瀹冨彉閲忕浉鍏冲唴瀹?");
                }
            }
            else if (mcn.callNodeType == ECallNodeType.NewTemplate)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByNewTemplate(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType, mcn.metaFunction, mcn.storeMetaVariable);

                mvn.SetToken(mcn.token);
                MetaClass cmc = mcn.metaType.metaClass;
                MetaMethodCall mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType.metaClass, null, mcn.metaFunction, null, mcn.metaInputParamCollection, null, mcn.storeMetaVariable);
                mvn.SetMethodCall(mmc);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.NewData)
            {
                MetaVisitNode mvn = MetaVisitNode.CraeteByNewData(mcn.metaType);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.EnumName)
            {
                //Debug.Write("Meta Common Parse IteratorVariable----------------------------------------------------");
                MetaVisitNode mvn = MetaVisitNode.CraeteByEnum(mcn.metaType);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.VisitVariable)
            {
                //if( mcn.extraAddLoadVariable )
                //{
                //    MetaVisitNode mvn1 = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                //    m_VisitNodeList.Add(mvn1);
                //}
                if (mcn.metaVariable is MetaVisitVariable mvv)
                {
                    MetaVisitNode mvn1 = MetaVisitNode.CreateByVisitVariable(mvv);
                    mvn1.SetToken(mcn.token);
                    m_VisitNodeList.Add(mvn1);
                }
                //for( int i = 0; i < mcn.metaArrayCallNodeList.Count; i++ )
                //{
                //    m_VisitNodeList.AddRange(mcn.metaArrayCallNodeList[i].m_VisitNodeList);
                //}
            }
            else if (mcn.callNodeType == ECallNodeType.IteratorVariable)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Meta Common Parse IteratorVariable----------------------------------------------------");
            }
            else if (mcn.callNodeType == ECallNodeType.DataName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.EnumMember)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByEnumMember(mcn.metaType, mcn.metaVariable);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            //else if (mcn.callNodeType == ECallNodeType.MemberDataName)
            //{
            //    MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.callMetaType );
            //    mvn.SetToken(mcn.token);
            //    m_VisitNodeList.Add(mvn);
            //    //Debug.Write("Meta Common Parse MemberDataName----------------------------------------------------");
            //}
            else if (mcn.callNodeType == ECallNodeType.TemplateName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByTemplate(mcn.metaTemplate);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.Express)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByEpxress(mcn.metaExpressValue);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.Global)
            {
                //MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                //m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.GetType)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByGetType(mcn.ownerMetaBase, mcn.metaType, mcn.metaVariable);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
            }
        }
        public int CalcParseLevel(int level)
        {
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                level = m_VisitNodeList[i].CalcParseLevel(level);
            }
            return level;
        }
        public MetaVariable GetDefineMetaVariable()
        {
            return m_FinalCallNode?.GetDefineMetaVariable();
        }
        public MetaVariable GetStoreMetaVariable()
        {
            return m_FinalCallNode?.GetStoreMetaVariable();
        }
        public MetaVariable GetReturnMetaVariable()
        {
            return m_FinalCallNode?.GetReturnMetaVariable();
        }
        public MetaExpressNodeBase GetMetaExpressNode()
        {
            if (m_FinalCallNode.visitType ==  MetaVisitNode.EVisitType.ConstValue )
            {
                return new MetaConstExpressNode(EType.Int32, m_FinalCallNode.constValueExpress );
            }
            return null;
        }
        public MetaType GetMetaType()
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
                sb.Append(m_VisitNodeList[i].ToFormatString());
                if (i < this.m_VisitNodeList.Count - 1)
                    sb.Append("  ->  ");
            }
            return sb.ToString();
        }
    }
}
