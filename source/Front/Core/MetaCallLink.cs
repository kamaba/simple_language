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
        public bool hasNullConditional
        {
            get
            {
                for (int i = 0; i < m_VisitNodeList.Count; i++)
                {
                    if (m_VisitNodeList[i].isQuestionMarkDot)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public AllowUseSettings allowUseSettings { get; private set; } = null;
        public Token token => m_Token;

        private FileMetaCallLink m_FileMetaCallLink;
        private MetaBase m_OwnerMetaClass = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements = null;
        private List<MetaCallNode> m_CallNodeList = new List<MetaCallNode>();
        private MetaVariable m_StoreMetaVariable = null;
        private FileMetaBaseTerm m_RightBaseTerm = null;

        private MetaVisitNode m_FinalCallNode = null;
        private List<MetaVisitNode> m_VisitNodeList = new List<MetaVisitNode>();
        public MetaClass ownerMetaClass => m_OwnerMetaClass as MetaClass;
        public MetaData ownerMetaData => m_OwnerMetaClass as MetaData;
        public MetaEnum ownerMetaEnum => m_OwnerMetaClass as MetaEnum;
        public MetaBase ownerMetaBase => m_OwnerMetaClass;
        private Token m_Token = null;

        //public MetaCallLink( MetaBase ownerMetaClass, MetaBlockStatements ownerMetaBlockStatements,
        //    List<MetaCallNode> callNodeList, MetaVariable storeMetaVariable, Token token)
        //{
        //    m_OwnerMetaClass = ownerMetaClass;
        //    m_OwnerMetaBlockStatements = ownerMetaBlockStatements;
        //    m_CallNodeList = callNodeList;
        //    m_StoreMetaVariable = storeMetaVariable;
        //    m_Token = token;
        //}
        public MetaCallLink( FileMetaCallLink fmcl, MetaBase metaOwner, MetaBlockStatements mbs, FileMetaBaseTerm fileRightExpress )
        {
            m_FileMetaCallLink = RewriteLocalCallLinkIfNeed(fmcl, metaOwner);
            m_OwnerMetaClass = metaOwner;
            m_OwnerMetaBlockStatements = mbs;
            m_RightBaseTerm = fileRightExpress;
            CreateCallLinkNode(null, null );
        }

        public MetaCallLink(FileMetaCallLink fmcl, MetaBase metaOwner, MetaBlockStatements mbs, MetaType frontDefineMt, MetaVariable mv)
        {
            m_FileMetaCallLink = RewriteLocalCallLinkIfNeed(fmcl, metaOwner);
            m_OwnerMetaClass = metaOwner;
            m_OwnerMetaBlockStatements = mbs;
            CreateCallLinkNode(frontDefineMt, mv);
        }
        //public MetaCallLink(MetaBase omc, MetaType frontDefineMt, MetaVariable mv)
        //{
        //    m_OwnerMetaClass = omc;
        //    m_OwnerMetaBlockStatements = null;
        //    CreateCallLinkNode(frontDefineMt, mv);
        //}

        private static FileMetaCallLink RewriteLocalCallLinkIfNeed(FileMetaCallLink fmcl, MetaBase ownerMc)
        {
            // local.xxx is resolved directly by MetaCallNode's Local branch,
            // which looks up the per-file <FileName>_Local class and its static
            // `instance` member. No call-link rewrite is needed.
            return fmcl;
        }
        public MetaCallLink(MetaVisitNode mvn)
        {
            m_VisitNodeList.Add(mvn);
            m_FinalCallNode = mvn;
        }
        private void CreateCallLinkNode(MetaType frontDefineMt, MetaVariable mv)
        {

            int beginIndex = 1;
            if (m_FileMetaCallLink.callNodeList.Count > 0)
            {
                FileMetaCallNode fmcn = m_FileMetaCallLink.callNodeList[0];
                var firstNode = new MetaCallNode(null, fmcn, m_OwnerMetaClass, m_OwnerMetaBlockStatements, 
                    m_FileMetaCallLink.callNodeList.Count == 1 ? m_RightBaseTerm : null);
                m_CallNodeList.Add(firstNode);
                m_Token = firstNode.token;
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
                        var fmn1 = new MetaCallNode(null, cn1, m_OwnerMetaClass, m_OwnerMetaBlockStatements);
                    }
                    else
                    {
                        var fmn2 = new MetaCallNode(cn1, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements, 
                        m_FileMetaCallLink.callNodeList.Count == i ? m_RightBaseTerm : null);
                        fmn2.SetDefineMetaVariable(mv);
                        m_CallNodeList.Add(fmn2);
                    }
                }
                else
                {
                    var fmn1 = new MetaCallNode(null, cn1, m_OwnerMetaClass, m_OwnerMetaBlockStatements );
                    m_CallNodeList.Add(fmn1);
                    FileMetaCallNode cn2 = null;
                    if (i < m_FileMetaCallLink.callNodeList.Count)
                    {
                        cn2 = m_FileMetaCallLink.callNodeList[i++];
                    }
                    if (cn2 == null) continue;

                    var fmn2 = new MetaCallNode(null, cn2, m_OwnerMetaClass, m_OwnerMetaBlockStatements);
                    fmn2.SetFrontCallNode(fmn1);
                    m_CallNodeList.Add(fmn2);
                }
            }

            var m_FinalMetaCallNode = m_CallNodeList[m_CallNodeList.Count - 1];
            if (m_FinalMetaCallNode == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 鏉╃偞甯存稉鍙夌梾閺堝澹橀崚鏉挎値闁倻娈戦懞鍌滃仯  360!!!");
            }
            m_FinalMetaCallNode.SetDefineMetaVariable(mv);
        }
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
            allowUseSettings.getterFunction = false;
            bool flag = true;
            List<MetaCallNode> newList = new List<MetaCallNode>();

            MetaCallNode frontMCN = null;
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                if (flag)
                {
                    if (i == m_CallNodeList.Count - 1)
                    {
                        allowUseSettings.setterFunction = m_RightBaseTerm != null;
                        allowUseSettings.getterFunction = m_RightBaseTerm == null;
                    }
                    else
                    {
                        allowUseSettings.getterFunction = true;
                    }
                    m_CallNodeList[i].SetFrontCallNode(frontMCN);
                    flag = m_CallNodeList[i].ParseNode(allowUseSettings);

                    if (m_CallNodeList[i].callNodeType == ECallNodeType.NewClass
                        || m_CallNodeList[i].callNodeType == ECallNodeType.NewData)
                    {
                        if (i < m_CallNodeList.Count - 1)
                        {
                            flag = false;
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, "Parse Statement Error 閸︺劋濞囬悽鈭焑wClassName閻ㄥ嫭鏌熷蹇ョ礉閸氬氦绔熸稉宥呭帒鐠佸憡婀侀崗璺虹暊閻ㄥ嫯鐨熼悽?");
                        }
                    }
                    if( flag )
                    {
                        newList.AddRange(m_CallNodeList[i].metaCallNodeList);
                        frontMCN = newList[newList.Count - 1];
                    }
                }
            }

            if (flag)
            {
                m_VisitNodeList.Clear();
                AddVisitNodeListByNewList(newList);
            }
            if (m_VisitNodeList != null && m_VisitNodeList.Count > 0)
            {
                m_FinalCallNode = m_VisitNodeList[m_VisitNodeList.Count - 1];
                ValidateNullConditionalUsage();
            }
            else
            {
                Log.AddMetaCoreLog(LID.MetaCoreParseCallLinkFailed, m_Token, "Call link parse failed!", m_CallNodeList[0].token.ToLexemeAllString() );
                flag = false;
            }

            return flag;
        }
        public void AddVisitNodeListByNewList( List<MetaCallNode> newList)
        {
            StringBuilder sb = new StringBuilder();
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
        /// <summary>
        /// 鏍￠獙 ?. (null conditional) 鐨勪娇鐢ㄥ満鏅細
        /// 1. 涓嶈兘鐢ㄤ簬鏋勯€犲嚱鏁?_init_
        /// 2. 涓嶈兘鐢ㄤ簬 set 鍑芥暟
        /// 3. 涓嶈兘鐢ㄤ簬 void 杩斿洖鍊肩殑鍑芥暟
        /// </summary>
        private void ValidateNullConditionalUsage()
        {
            for (int i = 0; i < m_VisitNodeList.Count; i++)
            {
                var node = m_VisitNodeList[i];
                if (!node.isQuestionMarkDot)
                    continue;

                if (node.visitType != MetaVisitNode.EVisitType.MethodCall &&
                    node.visitType != MetaVisitNode.EVisitType.SystemCall)
                    continue;

                var func = node.methodCall?.function;
                if (!(func is MetaMemberFunction mmf))
                    continue;

                if (mmf.isConstructInitFunction)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, node.token,
                        "Error 绌烘潯浠惰繍绠楃 ?. 涓嶈兘鐢ㄤ簬鏋勯€犲嚱鏁?_init_!");
                }
                else if (mmf.isSet)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, node.token,
                        "Error 绌烘潯浠惰繍绠楃 ?. 涓嶈兘鐢ㄤ簬 set 鍑芥暟!");
                }
                else if (mmf.defineMetaType != null &&
                         mmf.defineMetaType.metaClass == CoreMetaClassManager.voidMetaClass)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, node.token,
                        "Error 绌烘潯浠惰繍绠楃 ?. 涓嶈兘鐢ㄤ簬 void 杩斿洖鍊肩殑鍑芥暟!");
                }
            }
        }
        public List<MetaCallNode> CreateMetaCallNodeList(MetaExpressNodeBase belc)
        {
            List<MetaCallNode> bracketCNList = new List<MetaCallNode>();
            switch (belc)
            {
                case MetaConstExpressNode mcen:
                    {
                        var newmcn = new MetaCallNode(mcen, m_OwnerMetaClass, m_OwnerMetaBlockStatements);
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "鐟欙絾鐎藉畵灞筋殰expressList 閻ㄥ嫭妞傞崐娆忓絺閻㈢喍绨￠梻顕€顣?");
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
                mvn.SetQuestionMarkDot(mcn.isQuestionMarkDot);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.Base)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByBase(mcn.metaVariable);
                mvn.SetToken(mcn.token);
                mvn.SetQuestionMarkDot(mcn.isQuestionMarkDot);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.FunctionInnerVariableName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.staticCallMetaType);
                mvn.SetToken(mcn.token);
                mvn.SetQuestionMarkDot(mcn.isQuestionMarkDot);
                m_VisitNodeList.Add(mvn);
            }
            else if (mcn.callNodeType == ECallNodeType.ClosureCall)
            {
                // 闂寘璋冪敤: funname( xx ) -> 鐢熸垚 ClosureCall 璁块棶鑺傜偣
                var mcc = new MetaClosureCall(mcn.metaVariable, MetaClosureVariable.ResolveClosureVariable(mcn.metaVariable), mcn.metaInputParamCollection);
                mcc.SetToken(mcn.token);
                MetaVisitNode mvnClosure = MetaVisitNode.CreateByClosureCall(mcc);
                mvnClosure.SetToken(mcn.token);
                mvnClosure.SetQuestionMarkDot(mcn.isQuestionMarkDot);
                m_VisitNodeList.Add(mvnClosure);
            }
            else if (mcn.callNodeType == ECallNodeType.MemberVariableName)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable, mcn.staticCallMetaType);
                mvn.SetToken(mcn.token);
                mvn.SetQuestionMarkDot(mcn.isQuestionMarkDot);
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
                    //    Debug.Assert(false, "濞屸剝婀侀崚娑樼紦const閸欐﹢鍣?");
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

                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.staticCallMetaType, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, mcn.metaType, null);
                    mmc.SetToken(frontNode.token);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }
                else if( frontNode?.callNodeType == ECallNodeType.Base )
                {
                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.staticCallMetaType, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, mcn.metaType, null);
                    mmc.SetToken(mcn.token);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }
                else
                {
                    var retmv = frontNode?.metaVariable;
                    //if (m_VisitNodeList.Count > 0 && retmv != null)
                    //{
                    //    m_VisitNodeList.RemoveAt(m_VisitNodeList.Count - 1);
                    //}
                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.staticCallMetaType,
                        mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, mcn.metaType, retmv);
                    mmc.SetToken(mcn.token);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }

                MetaVisitNode mvn2 = MetaVisitNode.CreateByMethodCall(mmc);
                mvn2.SetQuestionMarkDot(mcn.isQuestionMarkDot);
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
                    //    Debug.Assert(false, "濞屸剝婀侀崚娑樼紦const閸欐﹢鍣?");
                    //}

                    MetaVisitNode mvn1 = MetaVisitNode.CreateByNewConst(frontNode.ownerMetaClass, frontNode.ownerMetaFunctionBlock,
                        frontNode.metaType, frontNode.metaExpressValue as MetaConstExpressNode, frontNode.metaFunction as MetaMemberFunction,
                        frontNode.metaInputParamCollection );
                    m_VisitNodeList.Add(mvn1);

                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.staticCallMetaType, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, mcn.metaType, null);
                    mmc.SetToken(mcn.token);
                    mmc.SetDebugInputParTermText(debugParTermText);
                }
                else
                {
                    var retmv = frontNode?.metaVariable;
                    if (m_VisitNodeList.Count > 0 && retmv != null)
                    {
                        m_VisitNodeList.RemoveAt(m_VisitNodeList.Count - 1);
                    }
                    mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.staticCallMetaType, mcn.metaFunction, mcn.metaTemplateParamsList, paramCollection, mcn.metaType, retmv);
                    mmc.SetToken(mcn.token);
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
                    paramCollection, mcn.metaType );
                mmc.SetToken(mcn.token);
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
            // typealias锛堝 ObjectArray -> Array<Object>锛夎В鏋愪负 MetaType锛涢』鐢熸垚涓?ClassName/NewClass 涓€鑷寸殑 visit锛屽惁鍒欏祵濂?ObjectArray(n){} 鏃?New 璇箟銆佸悗缁?Meta 澶辫触
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
                            MetaMethodCall mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType, mcn.metaFunction, null, mcn.metaInputParamCollection, mcn.metaType, null);
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
                    // data 绫诲瀷鍚嶅弬涓庢垚鍛樿闂椂锛堝 AA.a 涓?a 涓哄疄渚嬪瓧娈碉級锛?
                    // 鍏堟妸 AA 鐨勯粯璁ら潤鎬佸疄渚嬪帇鏍堬紝鍐嶇敱鍚庣画 MemberDataName 鍙栧瓧娈点€?
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
                        MetaMethodCall mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType, mcn.metaFunction, null, mcn.metaInputParamCollection, mcn.metaType, null);

                        mmc.SetToken(mcn.token);
                        mvn.SetMethodCall(mmc);
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 娴ｈ法鏁ewClass閺傜懓绱￠敍灞芥倵鏉堥€涚瑝閸忎浇顔忕捄鐔峰従鐎瑰啫褰夐柌蹇曟祲閸忓啿鍞寸€?");
                }
            }
            else if (mcn.callNodeType == ECallNodeType.NewTemplate)
            {
                MetaVisitNode mvn = MetaVisitNode.CreateByNewTemplate(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType, mcn.metaFunction, mcn.defineMetaVariable );

                mvn.SetToken(mcn.token);
                MetaClass cmc = mcn.metaType.metaClass;
                MetaMethodCall mmc = new MetaMethodCall(mcn.ownerMetaClass, mcn.ownerMetaFunctionBlock, mcn.metaType, mcn.metaFunction, null, mcn.metaInputParamCollection, mcn.metaType, null);

                mmc.SetToken(mcn.token);
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
            else if (mcn.callNodeType == ECallNodeType.Local)
            {
                // local resolves to the static `instance` member on <FileName>_Local.
                // Create a variable visit node so IR emits LoadStaticField.
                MetaVisitNode mvn = MetaVisitNode.CreateByVariable(mcn.metaVariable);
                mvn.SetToken(mcn.token);
                m_VisitNodeList.Add(mvn);
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
            if (m_FinalCallNode != null && m_FinalCallNode.visitType ==  MetaVisitNode.EVisitType.ConstValue )
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
