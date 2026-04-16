//****************************************************************************
//  File:      StructParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Project;
using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.Compile
{
    public partial class StructParse
    {
        public enum ESyntaxNodeStructType
        {
            None = 0,
            KeySyntax,
            CommonSyntax,
        }
        public class SyntaxNodeStruct
        {
            public ESyntaxNodeStructType eSyntaxNodeType = ESyntaxNodeStructType.None;

            public ETokenType tokenType = ETokenType.None;
            public ENodeType curNodeType = ENodeType.None;
            public int moveIndex = 0;
            public Node keyNode { get; private set; } = null;
            public List<Node> keyContent = new List<Node>();                //关键字后跟条件语句  if () switch() for()
            public Node blockNode = null;

            public List<Node> commonContent = new List<Node>();             //普通语句区间    Class.CalFun()

            public List<SyntaxNodeStruct> childrenKeySyntaxStructList = new List<SyntaxNodeStruct>();//关键字内嵌子语句, 像switch
            public List<SyntaxNodeStruct> followKeySyntaxStructList = new List<SyntaxNodeStruct>();//关键字跟随语句if/elif/elif/else  

            public SyntaxNodeStruct()
            {
            }
            public void SetMainKeyNode( Node _keyNode )
            {
                keyNode = _keyNode;
                tokenType = _keyNode.token.type;
                eSyntaxNodeType = ESyntaxNodeStructType.KeySyntax;
            }
            public void AddContent( Node node )
            {
                if (tokenType == ETokenType.If
                    || tokenType == ETokenType.ElseIf
                    || tokenType == ETokenType.Switch
                    || tokenType == ETokenType.For
                    || tokenType == ETokenType.While
                    || tokenType == ETokenType.DoWhile
                    || tokenType == ETokenType.Return
                    || tokenType == ETokenType.Transience
                    || tokenType == ETokenType.Case)
                {
                    keyContent.Add(node);
                }
                else if (tokenType == ETokenType.As
                    || tokenType == ETokenType.Is)
                {
                    commonContent.Add(node);
                }
                else if( tokenType == ETokenType.Else
                    || tokenType == ETokenType.Next
                    || tokenType == ETokenType.Break
                    || tokenType == ETokenType.Continue )
                {
                    Log.AddNodeLog( LID.ShowExtendMessage, "Error 不允许在Else后增加任何代码" + node.token?.ToLexemeAllString() );
                }
                else if( tokenType == ETokenType.Sharp )
                {

                }
                else
                {
                    commonContent.Add(node);
                    eSyntaxNodeType = ESyntaxNodeStructType.CommonSyntax;
                }
            }
            public void SetBraceNode( Node node )
            {
                blockNode = node;
                if (tokenType == ETokenType.If
                    || tokenType == ETokenType.ElseIf
                    || tokenType == ETokenType.Else
                    || tokenType == ETokenType.Switch
                    || tokenType == ETokenType.For
                    || tokenType == ETokenType.While || tokenType == ETokenType.DoWhile
                    || tokenType == ETokenType.Case
                    || tokenType == ETokenType.Default
                    || tokenType == ETokenType.Return
                    || tokenType == ETokenType.Transience
                    || tokenType == ETokenType.Label
                    || tokenType == ETokenType.Goto)
                {
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 结束{}错误，关键字不允许或者是其它错误" + node?.token?.ToLexemeAllString());
                }
            }
            public bool IsLineEndBreak()
            {
                if(tokenType == ETokenType.If
                    || tokenType == ETokenType.ElseIf
                    || tokenType == ETokenType.Else
                    || tokenType == ETokenType.Switch
                    || tokenType == ETokenType.For
                    || tokenType == ETokenType.While
                    || tokenType == ETokenType.DoWhile
                    || tokenType == ETokenType.Label )
                {
                    if( blockNode == null )
                    {
                        return false;
                    }
                    return !ProjectManager.isUseForceSemiColonInLineEnd;
                }
                return true;
            }
        }
        public class Condition
        {
            public bool isFirstKey = false;
            public bool isCheck = true;
            public List<ETokenType> eTokenTypeList = new List<ETokenType>();

            public bool IsMatchTokenType( ETokenType tokenType )
            {
                return eTokenTypeList.Contains( tokenType );
            }
            public Condition( ETokenType tokenType )
            {
                eTokenTypeList.Add( tokenType );
                if( tokenType == ETokenType.Else || tokenType == ETokenType.ElseIf )
                {
                    isFirstKey = true;
                }
                isCheck = true;
            }
            public void AddTokenTypeList(ETokenType tokenType )
            {
                eTokenTypeList.Add(tokenType);
            }
        }
        public SyntaxNodeStruct GetOneSyntax( Node pnode, Condition condition = null )
        {
            SyntaxNodeStruct keynodeStruct = new SyntaxNodeStruct();
            if (pnode.parseIndex >= pnode.childList.Count)
            {
                return null;
            }
            int index = 0;
            int tCurIndex = 0;
            Node curNode = null;
            Token curToken = null;
            ENodeType curNodeType = ENodeType.None;
            while (pnode.parseIndex < pnode.childList.Count)
            {
                tCurIndex = pnode.parseIndex + index++;
                curNode = null;
                if( tCurIndex < pnode.childList.Count )
                {
                    curNode = pnode.childList[tCurIndex];
                }
                if (curNode == null)
                {
                    break;
                }
                curNodeType = curNode.nodeType;
                curToken = curNode.token;
                if (curToken == null)
                {
                    break;
                }


                if (curNode.nodeType == ENodeType.Comment)
                {
                    break;
                }
                else if (curNodeType == ENodeType.LineEnd)
                {
                    if (condition != null
                        && condition.isFirstKey
                        && keynodeStruct.keyNode == null
                        && keynodeStruct.tokenType == ETokenType.None)
                    {
                        // when probing follow-key syntax (if/elif/else), skip leading newlines
                        continue;
                    }

                    if (keynodeStruct.IsLineEndBreak())
                    {
                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "warning 使用的是强制封号结束语句方式，注意这个节点会继承往下查找语句"
                                 + curToken?.ToLexemeAllString());
                        }

                        if(keynodeStruct.keyNode?.token?.type == ETokenType.Case
                            || keynodeStruct.keyNode?.token?.type == ETokenType.Default
                            || keynodeStruct.keyNode?.token?.type == ETokenType.ElseIf
                            || keynodeStruct.keyNode?.token?.type == ETokenType.If )
                        {
                            int tcurindex = 0;
                            int addIndex = 0;
                            while (true)
                            {
                                tcurindex = tCurIndex + 1;
                                addIndex++;
                                if (tcurindex < pnode.childList.Count)
                                {
                                    var tTurNode = pnode.childList[tcurindex];
                                    if (tTurNode != null)
                                    {
                                        if (tTurNode.nodeType == ENodeType.Brace)
                                        {
                                            keynodeStruct.SetBraceNode( tTurNode );
                                            break;
                                        }
                                    }
                                }
                            }
                            index += addIndex;
                        }
                        break;
                    }
                }
                else if (curNodeType == ENodeType.SemiColon)
                {
                    break;
                }
                else if (curNodeType == ENodeType.QuestionMark)
                {
                    keynodeStruct.AddContent(curNode);
                }
                else if (curNodeType == ENodeType.Colon)
                {
                    keynodeStruct.AddContent(curNode);
                }
                else if (curNodeType == ENodeType.Brace)
                {
                    Node nextNode = null;
                    bool isMustContactBrace = false;
                    ETokenType ttt = keynodeStruct.tokenType;
                    if (ttt == ETokenType.If
                                || ttt == ETokenType.ElseIf
                                || ttt == ETokenType.Else
                                || ttt == ETokenType.For
                                || ttt == ETokenType.While
                                || ttt == ETokenType.DoWhile
                                || ttt == ETokenType.Switch
                                || ttt == ETokenType.Case
                                || ttt == ETokenType.Label
                                || ttt == ETokenType.Default ) // ClassName(){}
                    {

                        isMustContactBrace = true;
                    }

                    if (isMustContactBrace)
                    {
                        keynodeStruct.SetBraceNode(curNode);
                    }
                    else
                    {
                        if (keynodeStruct.eSyntaxNodeType == ESyntaxNodeStructType.CommonSyntax)
                        {
                            keynodeStruct.AddContent(curNode);
                        }
                    }
                    break;
                }
                else if (curNodeType == ENodeType.Key)
                {
                    if (condition != null && condition.isCheck)
                    {
                        if (!condition.isFirstKey)
                        {
                            index = 0;
                            break;
                        }
                        if (!condition.IsMatchTokenType(curToken.type))
                        {
                            index = 0;
                            break;
                        }
                        condition.isCheck = false;
                    }
                    //else if (condition != null && !condition.isCheck)
                    //{
                    //    // When parsing a child syntax (e.g., switch body), once we already consumed the first key
                    //    // (case/default), the next key token should terminate this syntax node.
                    //    // Otherwise it would be treated as content and tokenType would be incorrect.
                    //    index--;
                    //    break;
                    //}

                    ETokenType ttt = curNode.token.type;
                    if (ttt == ETokenType.If
                        || ttt == ETokenType.ElseIf
                        || ttt == ETokenType.Else
                        || ttt == ETokenType.Switch
                        || ttt == ETokenType.For
                        || ttt == ETokenType.While || curNode.token?.type == ETokenType.DoWhile
                        || ttt == ETokenType.Case
                        || ttt == ETokenType.Default
                        || ttt == ETokenType.Return
                        || ttt == ETokenType.Transience
                        || ttt == ETokenType.Next
                        || ttt == ETokenType.Break
                        || ttt == ETokenType.Continue
                        || ttt == ETokenType.Label
                        || ttt == ETokenType.Goto
                        || ttt == ETokenType.Const)
                    {
                        keynodeStruct.SetMainKeyNode(curNode);
                    }
                    else if (ttt == ETokenType.Data)
                    {
                        keynodeStruct.AddContent(curNode);
                    }
                    else if (ttt == ETokenType.Var)
                    {
                        keynodeStruct.AddContent(curNode);
                    }
                    else if (ttt == ETokenType.In)
                    {
                        keynodeStruct.AddContent(curNode);
                    }
                    else if (ttt == ETokenType.Dynamic)
                    {
                        keynodeStruct.AddContent(curNode);
                    }
                    else if (ttt == ETokenType.As)
                    {
                        //keynodeStruct.SetMainKeyNode(curNode);
                        keynodeStruct.AddContent(curNode);
                    }
                    else if (ttt == ETokenType.Is)
                    {
                        //keynodeStruct.SetMainKeyNode(curNode);
                        keynodeStruct.AddContent(curNode);
                    }
                    else if (ttt == ETokenType.This
                       || ttt == ETokenType.Base
                       || ttt == ETokenType.New
                       || ttt == ETokenType.Global
                       || ttt == ETokenType.Local
                        )
                    {
                        keynodeStruct.AddContent(curNode);
                    }
                    //else if ( ttt == ETokenType.Class
                    //    || ttt == ETokenType.Interface
                    //    || ttt == ETokenType.Extends
                    //    || ttt == ETokenType.Public
                    //    || ttt == ETokenType.Private
                    //    || ttt == ETokenType.Projected
                    //    || ttt == ETokenType.Internal )
                    //{

                    //}
                    else
                    {
                        //Log.AddInHandleNode(curNode.token, 0, "Error 解析异常关键字");
                    }
                }
                else
                {
                    if (condition != null && condition.isCheck)
                    {
                        if (condition.isFirstKey)
                        {
                            index = 0;
                            break;
                        }
                    }
                    keynodeStruct.AddContent(curNode);
                }
            }
            keynodeStruct.moveIndex = index;
            return keynodeStruct;
        }

        private FileMetaSyntax CrateFileMetaSyntaxNoKey(List<Node> pNodeList)
        {
            List<Node> beforeNodeList = new List<Node>();
            Node assignNode = null;
            Node opAssignNode = null;
            ETokenType tet = ETokenType.None;
            List<Node> afterNodeList = new List<Node>();
            for (int j = 0; j < pNodeList.Count; j++)
            {
                var cnode = pNodeList[j];

                if (cnode.nodeType == ENodeType.Assign)
                {
                    if (assignNode == null && opAssignNode == null)
                    {
                        assignNode = cnode;
                        continue;
                    }
                }
                else
                {
                    tet = cnode.token.type;
                    if (tet == ETokenType.PlusAssign
                        || tet == ETokenType.MinusAssign
                        || tet == ETokenType.MultiplyAssign
                        || tet == ETokenType.DivideAssign
                        || tet == ETokenType.ModuloAssign
                        || tet == ETokenType.InclusiveOrAssign
                        || tet == ETokenType.CombineAssign
                        || tet == ETokenType.XORAssign
                        || tet == ETokenType.ShiAssign
                        || tet == ETokenType.ShrAssign
                        || tet == ETokenType.DoublePlus
                        || tet == ETokenType.DoubleMinus)
                    {
                        if (assignNode == null && opAssignNode == null)
                        {
                            opAssignNode = cnode;
                            continue;
                        }
                    }
                }
                if (assignNode != null || opAssignNode != null)
                    afterNodeList.Add(cnode);
                else
                {
                    beforeNodeList.Add(cnode);
                }
            }
            if (beforeNodeList.Count == 0)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error 发生错误，有符号的情况，必须有前置变量");
                return null;
            }

            Token staticToken = null;
            Token dynamicToken = null;
            Token varToken = null;
            Token dataToken = null;
            Token nameToken = null;
            FileMetaClassDefine classRef = null;
            FileMetaCallLink varRef = null;

            //Node parseNode = new Node(null);
            //parseNode.SetChildList(beforeNodeList);
            //parseNode.parseIndex = 0;
            //var handleBeforeList = HandleBeforeNode(parseNode);
            var handleBeforeList = HandleNodeSingleLine(beforeNodeList);

            List <Node> defineNodeList = new List<Node>();
            for (int i = 0; i < handleBeforeList.Count; i++)
            {
                var cnode = handleBeforeList[i];
                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    defineNodeList.Add(cnode);
                }
                else
                {
                    Token token = cnode.token;
                    if (token.type == ETokenType.Global
                        || token.type == ETokenType.Local
                        ||  token.type == ETokenType.This
                        || token.type == ETokenType.Base )
                    {
                        defineNodeList.Add(cnode);
                    }
                    else if (token?.type == ETokenType.Static)
                    {
                        if (staticToken != null)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 多个Static!!");
                        }
                        staticToken = token;
                    }
                    else if (token?.type == ETokenType.Type
                        || token?.type == ETokenType.String)
                    {
                        defineNodeList.Add(cnode);
                    }
                    else if (token?.type == ETokenType.Dynamic)
                    {
                        if (varToken != null || dynamicToken != null || dataToken != null)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 多个Dynamic!!");
                        }
                        dynamicToken = token;
                        defineNodeList.Add(cnode);
                    }
                    else if (token?.type == ETokenType.Var)
                    {
                        if (varToken != null || dynamicToken != null || dataToken != null )
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 多个Var!!");
                        }
                        varToken = token;
                        defineNodeList.Add(cnode);
                    }
                    else if (token?.type == ETokenType.Data )
                    {
                        if (varToken != null || dynamicToken != null || dataToken != null)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 多个Data!!");
                        }
                        dataToken = token;
                        defineNodeList.Add(cnode);
                    }
                    else
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析发现没有该节点!!" + token?.ToLexemeAllString());
                        //new Exception("Error 解析发现没有该节点");
                    }
                }
            }
            if (defineNodeList.Count == 0 || defineNodeList.Count > 3)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error 定义类型少于1");
                return null;
            }
            else if (defineNodeList.Count == 1  )
            {
                nameToken = defineNodeList[0].token;
                varRef = new FileMetaCallLink(m_FileMeta, defineNodeList[0]);
            }
            else if (defineNodeList.Count == 2)
            {
                if(varToken != null || dynamicToken != null || dataToken != null )
                {
                    nameToken = defineNodeList[1].token;
                    varRef = new FileMetaCallLink(m_FileMeta, defineNodeList[1]);
                }
                else
                {
                    classRef = new FileMetaClassDefine(m_FileMeta, defineNodeList[0]);
                    var node2 = defineNodeList[1];
                    if (node2.linkTokenList.Count != 1)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 定义名称只允许一个字符串!!");
                        return null;
                    }
                    nameToken = node2.token;
                }
            }

            FileMetaBaseTerm fme = null;
            if (assignNode != null && afterNodeList.Count > 0 )
            {
                if(afterNodeList[0].nodeType == ENodeType.Key
                    && afterNodeList[0].token?.type != ETokenType.This
                    && afterNodeList[0].token?.type != ETokenType.Base
                    && afterNodeList[0].token?.type != ETokenType.Local
                    && afterNodeList[0].token?.type != ETokenType.Global
                    && afterNodeList[0].token?.type != ETokenType.New )
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 暂不支持 a = if/switch{}语法");
                    //var fme22 = HandleCreateFileMetaSyntaxByPNode(afterNodeList);
                    //if ((afterNodeList[0].token.type == ETokenType.If
                    //    || afterNodeList[0].token.type == ETokenType.Switch)
                    //    )
                    //{
                    //    if (fme22 is FileMetaKeyIfSyntax)
                    //    {
                    //        fme = new FileMetaIfSyntaxTerm(m_FileMeta, fme22 as FileMetaKeyIfSyntax);

                    //    }
                    //    else if (fme22 is FileMetaKeySwitchSyntax)
                    //    {
                    //        fme = new FileMetaSwitchSyntaxTerm(m_FileMeta, fme22 as FileMetaKeySwitchSyntax);
                    //    }
                    //    else
                    //    {
                    //        Debug.Write("Error 生成if/switch语句失败!!");
                    //    }
                    //}
                    //else
                    //{
                    //    Debug.Write("Error 不允许嵌套除if/switch以外的语句!!");
                    //}
                }
            }
            if (fme == null && afterNodeList.Count > 0 )
            {
                fme = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, afterNodeList, FileMetaTermExpress.EExpressType.Common);
            }

            if (assignNode != null)
            {
                if (nameToken == null)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 当为定义变量时，名称不能为空!!");
                    return null;
                }
                if (classRef != null)
                {
                    FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef,
                        nameToken, assignNode.token, staticToken, fme);
                    return fmdvs;
                }
                if (varRef != null)
                {
                    FileMetaOpAssignSyntax fms = new FileMetaOpAssignSyntax(varRef, assignNode.token, dynamicToken, dataToken, varToken,  fme, true);
                    return fms;
                }
            }
            else if (opAssignNode != null)
            {
                if (varRef == null)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 当为定义变量时，名称不能为空!!");
                    return null;
                }
                FileMetaOpAssignSyntax fms = new FileMetaOpAssignSyntax(varRef, opAssignNode.token, dynamicToken, varToken, dataToken, fme);
                return fms;
            }
            else
            {
                if (nameToken == null)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 当为定义变量时，名称不能为空!!");
                    return null;
                }
                if (classRef != null)
                {
                    FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef, nameToken, staticToken, null, null);
                    return fmdvs;
                }
                else
                {
                    FileMetaCallSyntax fmcs = new FileMetaCallSyntax(varRef);

                    return fmcs;
                }
            }
            return null;
        }
        public FileMetaSyntax HandleCreateFileMetaSyntaxByPNode( Node pnode )
        {
            FileMetaSyntax fms = null;
            SyntaxNodeStruct akss = GetOneSyntax(pnode);
            pnode.parseIndex += akss.moveIndex;
            if( akss.eSyntaxNodeType == ESyntaxNodeStructType.None )
            {
            }
            else if( akss.eSyntaxNodeType == ESyntaxNodeStructType.CommonSyntax )
            {
                if( akss.commonContent.Count > 0 )
                {
                    fms = CrateFileMetaSyntaxNoKey(akss.commonContent);                    
                    AddParseSyntaxNodeInfo(fms);
                }
            }
            else if( akss.eSyntaxNodeType == ESyntaxNodeStructType.KeySyntax )
            {
                if (akss.tokenType == ETokenType.If)
                {
                    while (true)
                    {
                        Condition condition = new Condition(ETokenType.ElseIf);
                        condition.AddTokenTypeList(ETokenType.Else);
                        SyntaxNodeStruct cakss = GetOneSyntax(pnode, condition);
                        if (cakss == null  )
                        {
                            //if (cakss.moveIndex == 0)
                            //    break;
                            //pnode.parseIndex += cakss.moveIndex;
                            //continue;
                            break;
                        }

                        if (cakss.tokenType == ETokenType.ElseIf)
                        {
                            pnode.parseIndex += cakss.moveIndex;
                            akss.followKeySyntaxStructList.Add(cakss);
                            continue;
                        }
                        else if (cakss.tokenType == ETokenType.Else)
                        {
                            pnode.parseIndex += cakss.moveIndex;
                            akss.followKeySyntaxStructList.Add(cakss);
                        }
                        break;
                    }
                    FileMetaKeyIfSyntax fmkis = FileMetaKeyIfSyntax.ParseIfSyntax(m_FileMeta, akss);
                    fms = fmkis;
                    AddParseSyntaxNodeInfo(fmkis);

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis.ifExpressSyntax.executeBlockSyntax);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();


                    for (int i = 0; i < akss.followKeySyntaxStructList.Count; i++)
                    {
                        FileMetaBlockSyntax fmbs = null;
                        if (i < fmkis.elseIfExpressSyntax.Count)
                        {
                            fmbs = fmkis.elseIfExpressSyntax[i].executeBlockSyntax;
                        }
                        else
                        {
                            fmbs = fmkis.elseExpressSyntax?.executeBlockSyntax;
                        }

                        ParseCurrentNodeInfo pcnic2 = new ParseCurrentNodeInfo(fmbs);
                        m_CurrentNodeInfoStack.Push(pcnic2);
                        ParseSyntax(akss.followKeySyntaxStructList[i].blockNode);
                        m_CurrentNodeInfoStack.Pop();
                    }
                }
                else if (akss.tokenType == ETokenType.Switch)
                {
                    var children = ParseSwitchChildren(akss.blockNode);

                    FileMetaKeySwitchSyntax fmkis = ParseSwitchSyntax(m_FileMeta, akss, children);
                    fms = fmkis;
                    AddParseSyntaxNodeInfo(fmkis);

                    //ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis);
                    //m_CurrentNodeInfoStack.Push(pcnic);
                    //ParseSyntax(akss.blockNode);
                    ParseSwitchChildrenBlocks(fmkis, children);
                }
                else if (akss.tokenType == ETokenType.For)
                {
                    FileMetaKeyForSyntax fmkis = ParseForSyntax(m_FileMeta, akss);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis.executeBlockSyntax);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.While
                    || akss.tokenType == ETokenType.DoWhile)
                {
                    FileMetaConditionExpressSyntax fmkis = ParseConditionSyntax(m_FileMeta, akss);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis.executeBlockSyntax);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.Return
                    || akss.tokenType == ETokenType.Transience)
                {
                    FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, akss.keyContent, FileMetaTermExpress.EExpressType.Common);

                    FileMetaKeyReturnSyntax fmkis = new FileMetaKeyReturnSyntax(m_FileMeta, akss.keyNode.token, conditionExpress);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    if( akss.keyNode.blockNode != null )
                    {
                        ParseSyntax(akss.keyNode.blockNode);
                    }
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.Label
                    || akss.tokenType == ETokenType.Goto)
                {
                    Token labelToken = null;
                    if (akss.keyContent.Count != 1)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析Goto Label语法，只支持 goto id;的语法!!");
                    }
                    else
                    {
                        labelToken = akss.keyContent[0].token;
                        if (labelToken.type != ETokenType.Identifier)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析GotoLabel中 后边必须使用普通字符");
                        }
                    }

                    FileMetaKeyGotoLabelSyntax fmkis = new FileMetaKeyGotoLabelSyntax(m_FileMeta, akss.keyNode.token, labelToken);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fms);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.keyNode.blockNode);
                    m_CurrentNodeInfoStack.Pop();
                }
                else if(akss.tokenType == ETokenType.Break 
                    || akss.tokenType == ETokenType.Continue 
                    || akss.tokenType == ETokenType.Next )
                {
                    FileMetaKeyOnlySyntax fmkis = new FileMetaKeyOnlySyntax(m_FileMeta, akss.keyNode.token, null);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                }
            }
            return fms;
        }

        public FileMetaKeyForSyntax ParseForSyntax(FileMeta fm, SyntaxNodeStruct sns)
        {
            var cnode = sns.keyNode;

            FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(fm, sns.blockNode.token, sns.blockNode.endToken);
            var fms = new FileMetaKeyForSyntax(fm, cnode.token, executeBlock);

            var parlist = sns.keyContent;
            if (parlist.Count == 0)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error For语句中，条件区域没有相关的值!!");
            }
            List<Node> defineVariableSyntaxNodeList = new List<Node>();
            List<Node> conditionExpressNodeList = new List<Node>();
            List<Node> stepExecuteSyntaxNodeList = new List<Node>();

            int syntax = 0;         // 0 defineVariable include( a in array) 1 conditionExpress use one comma a, b 2 stepExecuteSyntax a,b,c
            Token inToken = null;
            for (int i = 0; i < parlist.Count; i++)
            {
                if (parlist[i].token?.type == ETokenType.Comma)
                {
                    if (syntax == 0)
                    {
                        syntax = 1;
                    }
                    else if (syntax == 1)
                    {
                        syntax = 2;
                    }
                    continue;
                }
                else if (parlist[i].token?.type == ETokenType.In)
                {
                    inToken = parlist[i].token;
                    continue;
                }

                if (syntax == 0)
                {
                    if (inToken != null)
                    {
                        conditionExpressNodeList.Add(parlist[i]);
                    }
                    else
                    {
                        defineVariableSyntaxNodeList.Add(parlist[i]);
                    }
                }
                else if (syntax == 1)
                {
                    conditionExpressNodeList.Add(parlist[i]);
                }
                else if (syntax == 2)
                {
                    stepExecuteSyntaxNodeList.Add(parlist[i]);
                }
            }

            FileMetaSyntax defineVariableSyntax = null;
            if (defineVariableSyntaxNodeList.Count > 0)
            {
                defineVariableSyntax = CrateFileMetaSyntaxNoKey(defineVariableSyntaxNodeList);
                defineVariableSyntax.isAppendSemiColon = false;
                fms.SetFileMetaClassDefine(defineVariableSyntax);
            }
            if (defineVariableSyntax == null)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析for 第一部分错误，解析语句出错，不是定义类型语句!!");
            }
            if (inToken != null)
            {
                var cfe = FileMetatUtil.CreateFileMetaExpress(fm, conditionExpressNodeList, FileMetaTermExpress.EExpressType.Common);
                if (cfe == null)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析for 第二部分错误!!");
                }
                else
                {
                    fms.SetInKeyAndArrayVariable(inToken, cfe);
                }
            }
            else
            {
                if (conditionExpressNodeList.Count > 0)
                {
                    var cfe = FileMetatUtil.CreateFileMetaExpress(fm, conditionExpressNodeList, FileMetaTermExpress.EExpressType.Common);
                    if (cfe == null)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析for 第二部分错误!!");
                    }
                    else
                    {
                        fms.SetConditionExpress(cfe);
                    }
                }
                if (stepExecuteSyntaxNodeList.Count > 0)
                {
                    FileMetaSyntax fms2 = CrateFileMetaSyntaxNoKey(stepExecuteSyntaxNodeList);
                    if (fms2 is FileMetaOpAssignSyntax)
                    {
                        fms.SetStepFileMetaOpAssignSyntax(fms2 as FileMetaOpAssignSyntax);
                    }
                    else
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析for 第三部分错误!!");
                    }
                }
            }
            return fms;
        }

        public FileMetaKeySwitchSyntax ParseSwitchSyntax(FileMeta fm, SyntaxNodeStruct sns)
        {
            return ParseSwitchSyntax(fm, sns, sns.childrenKeySyntaxStructList);
        }

        private List<SyntaxNodeStruct> ParseSwitchChildren(Node switchBodyNode)
        {
            var list = new List<SyntaxNodeStruct>();
            if (switchBodyNode == null) return list;

            // The body is a Brace node; parse its children sequentially.
            switchBodyNode.parseIndex = 0;

            while (true)
            {
                var condition = new Condition(ETokenType.Case);
                condition.isCheck = false;
                var one = GetOneSyntax(switchBodyNode, condition);
                if (one == null) break;
                if (one.moveIndex <= 0) break;

                switchBodyNode.parseIndex += one.moveIndex;

                // Only accept case/default. Anything else inside switch body is ignored here.
                if (one.tokenType == ETokenType.Case || one.tokenType == ETokenType.Default)
                    list.Add(one);
            }

            return list;
        }

        private void ParseSwitchChildrenBlocks(FileMetaKeySwitchSyntax fmkis, List<SyntaxNodeStruct> children)
        {
            if (fmkis == null || children == null) return;

            int caseIndex = 0;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child?.blockNode == null) continue;

                FileMetaBlockSyntax fmbs = null;
                if (child.tokenType == ETokenType.Case)
                {
                    if (caseIndex < 0 || caseIndex >= fmkis.fileMetaKeyCaseSyntaxList.Count) continue;
                    fmbs = fmkis.fileMetaKeyCaseSyntaxList[caseIndex].executeBlockSyntax;
                    caseIndex++;
                }
                else if (child.tokenType == ETokenType.Default)
                {
                    fmbs = fmkis.defaultExecuteBlockSyntax;
                }
                else
                {
                    continue;
                }

                if (fmbs == null) continue;

                var pcnic2 = new ParseCurrentNodeInfo(fmbs);
                m_CurrentNodeInfoStack.Push(pcnic2);
                ParseSyntax(child.blockNode);
                m_CurrentNodeInfoStack.Pop();
            }
        }

        public FileMetaKeySwitchSyntax ParseSwitchSyntax(FileMeta fm, SyntaxNodeStruct sns, List<SyntaxNodeStruct> children)
        {
            var cnode = sns.keyNode;
            FileMetaCallLink fmcl = null;
            if (cnode.parNode != null && cnode.parNode.childList?.Count > 0)
            {
                fmcl = new FileMetaCallLink(fm, cnode.parNode.childList[0]);
            }
            if( fmcl == null )
            {
                fmcl = new FileMetaCallLink(fm, sns.keyContent[0] );
            }
            if( fmcl == null )
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error 创建 FileMetaCallLink 失败");
            }
            var fms = new FileMetaKeySwitchSyntax(fm, cnode.token, sns.blockNode.token, sns.blockNode.endToken, fmcl);

            children ??= sns.childrenKeySyntaxStructList;
            for (int i = 0; i < children.Count; i++)
            {
                var caseMS = children[i];
                if (caseMS.tokenType == ETokenType.Case)
                {
                    var fcase = new FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax(fm, caseMS.keyNode.token);
                    ParseCaseHeader(fm, fcase, caseMS);
                    fcase.SetExecuteBlockSyntax(new FileMetaBlockSyntax(fm, caseMS.blockNode.token, caseMS.blockNode.endToken));
                    fms.AddFileMetaKeyCaseSyntaxList(fcase);
                }
                else if (caseMS.tokenType == ETokenType.Default)
                {
                    var fdefault = new FileMetaBlockSyntax(fm, caseMS.blockNode.token, caseMS.blockNode.endToken);
                    fms.SetDefaultExecuteBlockSyntax(fdefault);
                }
                else
                {
                    Log.AddNodeLog( LID.ShowExtendMessage, "Error switch中不能出现除case/default子外的语句!!");
                }
            }

            return fms;
        }

        private static void ParseCaseHeader(FileMeta fm, FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax fcase, SyntaxNodeStruct caseMS)
        {
            if (fm == null || fcase == null || caseMS == null) return;

            // case header tokens are stored in keyContent (see SyntaxNodeStruct.AddContent)
            var parlist = caseMS.keyContent;
            if (parlist == null || parlist.Count == 0)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error Case语句不允许没有检查值!!");
                return;
            }

            var childList = new List<Node>();
            bool isComma = false;
            for (int i = 0; i < parlist.Count; i++)
            {
                if (parlist[i].token?.type == ETokenType.Comma)
                {
                    isComma = true;
                    continue;
                }
                childList.Add(parlist[i]);
            }

            if (isComma)
            {
                bool isSame = true;
                for (int i = 0; i < childList.Count - 1; i++)
                {
                    var curNode = childList[i];
                    var nextNode = childList[i + 1];
                    var type = curNode.token.type;
                    if (type != ETokenType.Number && type != ETokenType.String)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 逗号分割只允许number,string");
                        isSame = false;
                        break;
                    }
                    if (type != nextNode.token.type)
                    {
                        isSame = false;
                        break;
                    }
                }
                if (!isSame)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 使用逗号切割开后，类型不相同!!");
                }

                for (int i = 0; i < childList.Count; i++)
                {
                    fcase.AddConstValueTokenList(new FileMetaConstValueTerm(fm, childList[i].token));
                }
            }
            else
            {
                if (parlist.Count == 2)
                {
                    if (parlist[0].token?.type == ETokenType.Identifier
                        || parlist[1].token?.type == ETokenType.Identifier)
                    {
                        fcase.SetDefineClassNode(parlist[0]);
                        fcase.SetVariableToken(parlist[1].token);
                    }
                }
                else if (parlist.Count == 1)
                {
                    var ttype = parlist[0].token?.type;
                    if (ttype == ETokenType.Type
                        || ttype == ETokenType.Identifier)
                    {
                        fcase.SetDefineClassNode(parlist[0]);
                    }
                    else if (ttype == ETokenType.Number
                        || ttype == ETokenType.String)
                    {
                        fcase.AddConstValueTokenList(new FileMetaConstValueTerm(fm, parlist[0].token));
                    }
                }
            }
        }

        public FileMetaConditionExpressSyntax ParseConditionSyntax(FileMeta fm, SyntaxNodeStruct sns)
        {
            var cnode = sns.keyNode;
            FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, sns.keyContent, FileMetaTermExpress.EExpressType.Common);
            FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(fm, sns.blockNode.token, sns.blockNode.endToken);
            var fms = new FileMetaConditionExpressSyntax(fm, cnode.token, conditionExpress, executeBlock);

            return fms;
        }
    }
}