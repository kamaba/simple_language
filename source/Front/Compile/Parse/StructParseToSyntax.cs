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
                    || tokenType == ETokenType.Case
                    || tokenType == ETokenType.Try
                    || tokenType == ETokenType.Catch
                    || tokenType == ETokenType.Finally
                    || tokenType == ETokenType.Throw
                    || tokenType == ETokenType.Defer
                    || tokenType == ETokenType.ErrDefer
                    || tokenType == ETokenType.Yield
                    || tokenType == ETokenType.Function)
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
                    // For Label/Goto, keep KeySyntax type so the Label case is reached
                    if (tokenType != ETokenType.Label && tokenType != ETokenType.Goto)
                    {
                        eSyntaxNodeType = ESyntaxNodeStructType.CommonSyntax;
                    }
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
                    || tokenType == ETokenType.Goto
                    || tokenType == ETokenType.Try
                    || tokenType == ETokenType.Catch
                    || tokenType == ETokenType.Finally
                    || tokenType == ETokenType.Defer
                    || tokenType == ETokenType.ErrDefer
                    || tokenType == ETokenType.Checked
                    || tokenType == ETokenType.Unchecked
                    || tokenType == ETokenType.Function)
                {
                }
                else
                {
                    Log.AddNodeLog(LID.NodeKeyNotMatchBrack, node?.token, tokenType.ToString() );
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
                    || tokenType == ETokenType.Label
                    || tokenType == ETokenType.Try
                    || tokenType == ETokenType.Catch
                    || tokenType == ETokenType.Finally
                    || tokenType == ETokenType.Defer
                    || tokenType == ETokenType.ErrDefer
                    || tokenType == ETokenType.Function )
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
                if( tokenType == ETokenType.Else || tokenType == ETokenType.ElseIf
                    || tokenType == ETokenType.Catch || tokenType == ETokenType.Finally )
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
        private static bool NeedAttachTrailingBrace(ETokenType tokenType)
        {
            return tokenType == ETokenType.If
                || tokenType == ETokenType.ElseIf
                || tokenType == ETokenType.Else
                || tokenType == ETokenType.Switch
                || tokenType == ETokenType.For
                || tokenType == ETokenType.While
                || tokenType == ETokenType.DoWhile
                || tokenType == ETokenType.Case
                || tokenType == ETokenType.Default
                || tokenType == ETokenType.Label
                || tokenType == ETokenType.Try
                || tokenType == ETokenType.Catch
                || tokenType == ETokenType.Finally
                || tokenType == ETokenType.Defer
                || tokenType == ETokenType.ErrDefer
                || tokenType == ETokenType.Checked
                || tokenType == ETokenType.Unchecked
                || tokenType == ETokenType.Function;
        }
        private static bool IsSkippableNodeBetweenKeyAndBrace(Node node)
        {
            if (node == null)
            {
                return false;
            }
            return node.nodeType == ENodeType.Comment
                || node.nodeType == ENodeType.LineEnd;
        }
        /// <summary>
        /// 匿名闭包跨行前瞻: 当前语句已含 '=' 与 '(' 参数列表节点, 且换行后紧跟 '{' 块,
        /// 则语句不结束, 继续读取闭包体 (var name = ( params ) \n { ... })
        /// </summary>
        private static bool IsAnonymousClosurePending(Node pnode, int curIndex, SyntaxNodeStruct keynodeStruct)
        {
            if (pnode == null || keynodeStruct == null)
            {
                return false;
            }
            bool hasAssign = false;
            bool hasPar = false;
            var content = keynodeStruct.commonContent;
            for (int i = 0; i < content.Count; i++)
            {
                var n = content[i];
                if (n == null) continue;
                if (n.nodeType == ENodeType.Assign) hasAssign = true;
                else if (n.nodeType == ENodeType.Par) hasPar = true;
            }
            if (!hasAssign || !hasPar)
            {
                return false;
            }
            for (int peek = curIndex + 1; peek < pnode.childList.Count; peek++)
            {
                var pn = pnode.childList[peek];
                if (pn == null) break;
                if (pn.nodeType == ENodeType.Comment || pn.nodeType == ENodeType.LineEnd)
                    continue;
                return pn.nodeType == ENodeType.Brace;
            }
            return false;
        }
        private static int AttachTrailingBraceNode(Node pnode, int startIndex, SyntaxNodeStruct keynodeStruct)
        {
            if (pnode == null || keynodeStruct == null)
            {
                return 0;
            }
            if (keynodeStruct.blockNode != null || !NeedAttachTrailingBrace(keynodeStruct.tokenType))
            {
                return 0;
            }

            int moveCount = 0;
            for (int i = startIndex + 1; i < pnode.childList.Count; i++)
            {
                var nextNode = pnode.childList[i];
                if (nextNode == null)
                {
                    break;
                }

                if (IsSkippableNodeBetweenKeyAndBrace(nextNode))
                {
                    moveCount++;
                    continue;
                }

                if (nextNode.nodeType == ENodeType.Brace)
                {
                    keynodeStruct.SetBraceNode(nextNode);
                    moveCount++;
                }
                break;
            }
            return moveCount;
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
                    index += AttachTrailingBraceNode(pnode, tCurIndex, keynodeStruct);
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
                        // 匿名闭包: var name = ( params ) 换行后紧跟 { 闭包体, 语句继续
                        if (IsAnonymousClosurePending(pnode, tCurIndex, keynodeStruct))
                        {
                            continue;
                        }

                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "warning 使用的是强制封号结束语句方式，注意这个节点会继承往下查找语句"
                                 + curToken?.ToLexemeAllString());
                        }

                        index += AttachTrailingBraceNode(pnode, tCurIndex, keynodeStruct);
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
                else if (curNodeType == ENodeType.DoubleQuestion)
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
                                || ttt == ETokenType.Default
                                || ttt == ETokenType.Try
                                || ttt == ETokenType.Catch
                                || ttt == ETokenType.Finally
                                || ttt == ETokenType.Defer
                                || ttt == ETokenType.ErrDefer
                                || ttt == ETokenType.Function ) // ClassName(){}
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
                        || ttt == ETokenType.Try
                        || ttt == ETokenType.Catch
                        || ttt == ETokenType.Finally
                        || ttt == ETokenType.Throw
                        || ttt == ETokenType.Defer
                        || ttt == ETokenType.ErrDefer
                        || ttt == ETokenType.Checked
                        || ttt == ETokenType.Unchecked
                        || ttt == ETokenType.Yield
                        || ttt == ETokenType.Const
                        || ttt == ETokenType.Function)
                    {
                        // 匿名闭包: var name = function( params ) { body }
                        // 当 function 出现在语句中间 (commonContent 已有内容如 var/name/=) 时,
                        // 不抢占主关键字, 而是作为普通内容节点继续收集, 后续由 CrateFileMetaSyntaxNoKey 拦截
                        if (ttt == ETokenType.Function && keynodeStruct.commonContent.Count > 0)
                        {
                            keynodeStruct.AddContent(curNode);
                            continue;
                        }
                        // function 类型声明: function f = expr
                        // function 后是 无参数列表的标识符 且再后是 = 时, 作为普通声明语句收集
                        if (ttt == ETokenType.Function && keynodeStruct.commonContent.Count == 0
                            && IsFunctionDeclareAhead(pnode, tCurIndex))
                        {
                            keynodeStruct.AddContent(curNode);
                            continue;
                        }
                        // checked label Name {} catch{} - checked modifier on label
                        if (curNode.token?.type == ETokenType.Checked)
                        {
                            bool hasNextLabel = false;
                            for (int peek = tCurIndex + 1; peek < pnode.childList.Count; peek++)
                            {
                                var pn = pnode.childList[peek];
                                if (pn == null) break;
                                if (pn.nodeType == ENodeType.LineEnd || pn.nodeType == ENodeType.Comment)
                                    continue;
                                if (pn.token?.type == ETokenType.Label)
                                    hasNextLabel = true;
                                break;
                            }
                            if (hasNextLabel)
                            {
                                m_PendingCheckedLabel = true;
                                continue; // skip 'checked', let 'label' handler process it
                            }
                        }
                        // try/checked without a following {} block is an expression prefix, not a block keyword
                        if (curNode.token?.type == ETokenType.Try
                            || curNode.token?.type == ETokenType.Checked)
                        {
                            bool hasBrace = false;
                            for (int peek = tCurIndex + 1; peek < pnode.childList.Count; peek++)
                            {
                                var pn = pnode.childList[peek];
                                if (pn == null) break;
                                if (pn.nodeType == ENodeType.LineEnd || pn.nodeType == ENodeType.Comment)
                                    continue;
                                if (pn.nodeType == ENodeType.Brace)
                                    hasBrace = true;
                                break;
                            }
                            if (!hasBrace)
                            {
                                keynodeStruct.commonContent.Add(curNode);
                                keynodeStruct.eSyntaxNodeType = ESyntaxNodeStructType.CommonSyntax;
                                continue;
                            }
                        }
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
                    else if (ttt == ETokenType.Spawn
                        || ttt == ETokenType.Await)
                    {
                        // spawn/await 一元前缀表达式关键字: 作为普通内容收集,
                        // 由 CrateFileMetaSyntaxNoKey / TransformCoroutineKeywordNodes 展开为 Coroutine.xxx() 调用
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
            // Check if first node is 'try' or 'checked' keyword (expression prefix like "try riskyFunc()" / "checked(a + b)")
            if (pNodeList.Count > 0
                && (pNodeList[0].token?.type == ETokenType.Try
                    || pNodeList[0].token?.type == ETokenType.Checked))
            {
                var tryExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, pNodeList, FileMetaTermExpress.EExpressType.Common);
                if (tryExpress != null)
                {
                    return new FileMetaCallSyntax(tryExpress);
                }
                return null;
            }

            // spawn/await 关键字展开: 把 spawn f(a,b) / await expr 替换为 Coroutine.spawnClosureN(...) / Coroutine.awaitFunction(...) 调用节点
            TransformCoroutineKeywordNodes(pNodeList);

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
                Log.AddNodeLog(LID.NodeAssetFrontListNodeIsZero, opAssignNode?.token, "" );
                return null;
            }

            Token staticToken = null;
            Token constToken = null;
            Token dynamicToken = null;
            Token varToken = null;
            Token dataToken = null;
            Token functionToken = null;
            Token nameToken = null;
            FileMetaClassDefine classRef = null;
            FileMetaCallLink varRef = null;

            //var handleBeforeList = FileMetatUtil.HandleClassDefineNodes(beforeNodeList);
            //var handleBeforeList = beforeNodeList;

            List <Node> defineNodeList = new List<Node>();
            for (int i = 0; i < beforeNodeList.Count; i++)
            {
                var cnode = beforeNodeList[i];
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
                    else if (token?.type == ETokenType.Const)
                    {
                        if (constToken != null)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 多个Const!!");
                        }
                        constToken = token;
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
                    else if (token?.type == ETokenType.Function)
                    {
                        // function 类型声明: function f = expr
                        // 定义时不检查函数签名类型 (类似 var 的宽松语义), 变量类型固定为 Function 基类
                        if (varToken != null || dynamicToken != null || dataToken != null || functionToken != null)
                        {
                            // Log.AddNodeLog(LID.ShowExtendMessage, "Error 多个Function!!");
                        }
                        functionToken = token;
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
                    var tlist = node2.GetLinkTokenList();
                    if (tlist.Count != 1)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, defineNodeList[0].token, "Error 定义名称只允许一个字符串!!");
                        return null;
                    }
                    nameToken = node2.token;
                }
            }

            FileMetaBaseTerm fme = null;
            if (assignNode != null && afterNodeList.Count == 0)
            {
                Log.AddNodeLog(LID.MetaCoreAssertShowMessage, assignNode.token,
                    "Error '=' 后缺少赋值表达式；不支持 '=\\n{}' 或 '= 后注释再换行 { }' 这类写法。请将右值与 '=' 放在同一行。");
                return null;
            }
            if (assignNode != null && afterNodeList.Count > 0 )
            {
                bool hasSameLineExpression = false;
                for (int i = 0; i < afterNodeList.Count; i++)
                {
                    var n = afterNodeList[i];
                    if (n == null) continue;
                    if (n.nodeType == ENodeType.Comment)
                    {
                        continue;
                    }

                    if (n.nodeType == ENodeType.LineEnd || n.nodeType == ENodeType.SemiColon)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, assignNode.token,
                            "Error '=' 后不允许直接换行或结束，右值必须与 '=' 同行出现（例如: [ { new() ClassName() 等）");
                        return null;
                    }

                    hasSameLineExpression = true;
                    break;
                }

                if (!hasSameLineExpression)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, assignNode.token,
                        "Error '=' 后未找到同一行右值表达式");
                    return null;
                }

                // 匿名闭包拦截 (新语法): var name = function( 参数列表 ) { 闭包体 }
                // function 声明也支持: function name = function( 参数列表 ) { 闭包体 }
                // Func<...> 类型声明也支持: Func<void,int,int> name = function( 参数列表 ) { 闭包体 }
                // (声明类型仅作文档, 运行时统一按 Function 处理)
                bool isFuncTypeDeclare = classRef != null && classRef.stringList != null
                    && classRef.stringList.Count == 1 && classRef.stringList[0] == "Func";
                if ((varToken != null || functionToken != null || isFuncTypeDeclare)
                    && afterNodeList.Count >= 3
                    && afterNodeList[0].nodeType == ENodeType.Key
                    && afterNodeList[0].token?.type == ETokenType.Function
                    && afterNodeList[1].nodeType == ENodeType.Par
                    && afterNodeList[2].nodeType == ENodeType.Brace)
                {
                    // 闭包只能出现在方法体内
                    var curInfo = currentNodeInfo;
                    if (curInfo == null ||
                        (curInfo.parseType != EParseNodeType.Statements && curInfo.parseType != EParseNodeType.Function))
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, nameToken, "Error 闭包只能定义在方法体内!");
                        return null;
                    }
                    List<FileMetaParamterDefine> paramList = ParseClosureParamList(afterNodeList[1]);
                    Node braceNode = afterNodeList[2];
                    FileMetaBlockSyntax closureBlock = new FileMetaBlockSyntax(m_FileMeta, braceNode.token, braceNode.endToken);
                    FileMetaDefineClosureSyntax fmdcs = new FileMetaDefineClosureSyntax(m_FileMeta,
                        afterNodeList[0].token, nameToken, true, paramList, closureBlock);
                    ParseCurrentNodeInfo pcnicClosure = new ParseCurrentNodeInfo(closureBlock);
                    m_CurrentNodeInfoStack.Push(pcnicClosure);
                    ParseSyntax(braceNode);
                    m_CurrentNodeInfoStack.Pop();
                    return fmdcs;
                }
                // 旧语法报错: var name = ( 参数列表 ) { 闭包体 } (需使用 function 关键字)
                if (varToken != null
                    && afterNodeList.Count >= 2
                    && afterNodeList[0].nodeType == ENodeType.Par
                    && afterNodeList[1].nodeType == ENodeType.Brace)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, nameToken,
                        "Error 匿名闭包需使用 function 关键字: var name = function( 参数 ) { 闭包体 }");
                    return null;
                }

                if(afterNodeList[0].nodeType == ENodeType.Key
                    && afterNodeList[0].token?.type != ETokenType.This
                    && afterNodeList[0].token?.type != ETokenType.Base
                    && afterNodeList[0].token?.type != ETokenType.Local
                    && afterNodeList[0].token?.type != ETokenType.Global
                    && afterNodeList[0].token?.type != ETokenType.New
                    && afterNodeList[0].token?.type != ETokenType.Try
                    && afterNodeList[0].token?.type != ETokenType.Checked )
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
                    Log.AddNodeLog(LID.NodeNotFoundNameToken, assignNode.token, "" );
                    return null;
                }
                if (classRef != null)
                {
                    FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef,
                        nameToken, assignNode.token, staticToken, constToken, fme);
                    return fmdvs;
                }
                if (varRef != null)
                {
                    FileMetaOpAssignSyntax fms = new FileMetaOpAssignSyntax(varRef, assignNode.token, dynamicToken, dataToken, varToken, functionToken, fme, true);
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
                FileMetaOpAssignSyntax fms = new FileMetaOpAssignSyntax(varRef, opAssignNode.token, dynamicToken, dataToken, varToken, functionToken, fme);
                return fms;
            }
            else
            {
                if (nameToken == null)
                {
                    Log.AddNodeLog(LID.NodeNotFoundNameToken, assignNode.token, "");
                    return null;
                }
                if (classRef != null)
                {
                    FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef, nameToken, null, staticToken, constToken, null);
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
        //======================================================================================
        // 协程关键字 (spawn/await) 展开 & Coroutine 调用节点合成
        //======================================================================================
        private int m_SpawnClosureCounter = 0;

        /// <summary>
        /// 判断是否为 function 类型声明: function f = expr
        /// function 后第一个非跳过节点为 无参数列表的 IdentifierLink, 且再后一个非跳过节点为 =
        /// </summary>
        private bool IsFunctionDeclareAhead( Node pnode, int curIndex )
        {
            int state = 0;
            for (int i = curIndex + 1; i < pnode.childList.Count; i++)
            {
                var n = pnode.childList[i];
                if (n == null) break;
                if (n.nodeType == ENodeType.LineEnd || n.nodeType == ENodeType.Comment
                    || n.nodeType == ENodeType.SemiColon)
                {
                    continue;
                }
                if (state == 0)
                {
                    // function 后必须是 标识符 且不带参数列表 (带参数列表的是闭包定义)
                    if (n.nodeType != ENodeType.IdentifierLink || n.parNode != null)
                        return false;
                    state = 1;
                }
                else if (state == 1)
                {
                    // 标识符后必须是 =
                    return n.nodeType == ENodeType.Assign;
                }
            }
            return false;
        }

        /// <summary>
        /// 程序化合成 Coroutine.methodName( args... ) 的 IdentifierLink 调用节点,
        /// 结构与正常解析 "Coroutine.methodName( args... )" 完全一致。
        /// 注意: argNodes 原样作为 Par 的 childList, 多实参时由调用者负责插入 Comma 分隔节点。
        /// </summary>
        private Node CreateCoroutineCallNode( Token keyToken, string methodName, List<Node> argNodes )
        {
            // Coroutine 根节点
            Token corToken = new Token(keyToken);
            corToken.SetLexeme("Coroutine", ETokenType.Identifier);
            Node corNode = new Node(corToken);
            corNode.nodeType = ENodeType.IdentifierLink;

            // '.' 链接节点
            Token periodToken = new Token(keyToken);
            periodToken.SetLexeme(".", ETokenType.Period);
            Node periodNode = new Node(periodToken);
            periodNode.nodeType = ENodeType.Period;

            // 方法名节点
            Token methodToken = new Token(keyToken);
            methodToken.SetLexeme(methodName, ETokenType.Identifier);
            Node methodNode = new Node(methodToken);
            methodNode.nodeType = ENodeType.IdentifierLink;

            corNode.SetLinkNode(new List<Node> { periodNode, methodNode });

            // 实参 Par 节点
            Token parToken = new Token(keyToken);
            parToken.SetLexeme("(", ETokenType.LeftPar);
            Node parNode = new Node(parToken);
            parNode.nodeType = ENodeType.Par;
            if (argNodes != null)
            {
                for (int i = 0; i < argNodes.Count; i++)
                {
                    if (argNodes[i] == null) continue;
                    parNode.AddChild(argNodes[i], false);
                }
            }
            Token rightParToken = new Token(keyToken);
            rightParToken.SetLexeme(")", ETokenType.RightPar);
            parNode.endToken = rightParToken;
            methodNode.SetParNode(parNode);
            return corNode;
        }

        /// <summary>
        /// spawn/await 关键字展开 (原地修改节点列表):
        ///     spawn f(a,b)              ->  Coroutine.spawnClosure2( f, a, b )
        ///     spawn function(){...}     ->  先提升为具名闭包语句, 再 Coroutine.spawnClosure0( tmpName )
        ///     await expr                ->  Coroutine.awaitFunction( expr )
        /// </summary>
        private void TransformCoroutineKeywordNodes( List<Node> pNodeList )
        {
            for (int i = 0; i < pNodeList.Count; i++)
            {
                var cnode = pNodeList[i];
                if (cnode == null) continue;
                if (cnode.nodeType != ENodeType.Key) continue;
                var ttype = cnode.token?.type;
                if (ttype != ETokenType.Spawn && ttype != ETokenType.Await) continue;

                // 找到关键字后第一个非跳过节点
                int opIndex = -1;
                for (int k = i + 1; k < pNodeList.Count; k++)
                {
                    var n = pNodeList[k];
                    if (n == null) break;
                    if (n.nodeType == ENodeType.LineEnd || n.nodeType == ENodeType.Comment
                        || n.nodeType == ENodeType.SemiColon)
                        continue;
                    opIndex = k;
                    break;
                }
                if (opIndex < 0)
                {
                    // Log.AddNodeLog(LID.ShowExtendMessage, cnode.token,
                    //     "Error " + cnode.token.lexeme + " 后缺少表达式!");
                    return;
                }
                var opNode = pNodeList[opIndex];

                if (ttype == ETokenType.Spawn)
                {
                    if (opNode.nodeType == ENodeType.Key && opNode.token?.type == ETokenType.Function)
                    {
                        // spawn 匿名闭包: spawn function( params ) { body }
                        if (opIndex + 2 >= pNodeList.Count
                            || pNodeList[opIndex + 1].nodeType != ENodeType.Par
                            || pNodeList[opIndex + 2].nodeType != ENodeType.Brace)
                        {
                            // Log.AddNodeLog(LID.ShowExtendMessage, cnode.token,
                            //     "Error spawn 匿名闭包语法应为: spawn function( 参数 ) { 闭包体 }");
                            return;
                        }
                        var curInfo = currentNodeInfo;
                        if (curInfo == null ||
                            (curInfo.parseType != EParseNodeType.Statements && curInfo.parseType != EParseNodeType.Function))
                        {
                            // Log.AddNodeLog(LID.ShowExtendMessage, cnode.token,
                            //     "Error spawn 匿名闭包只能出现在方法体内!");
                            return;
                        }
                        // 1. 提升为具名闭包定义语句 (先于 spawn 调用语句发射)
                        string tmpName = "spawnClosureTmp" + (m_SpawnClosureCounter++);
                        Token nameToken = new Token(cnode.token);
                        nameToken.SetLexeme(tmpName, ETokenType.Identifier);
                        List<FileMetaParamterDefine> paramList = ParseClosureParamList(pNodeList[opIndex + 1]);
                        Node braceNode = pNodeList[opIndex + 2];
                        FileMetaBlockSyntax closureBlock = new FileMetaBlockSyntax(m_FileMeta, braceNode.token, braceNode.endToken);
                        FileMetaDefineClosureSyntax fmdcs = new FileMetaDefineClosureSyntax(m_FileMeta,
                            opNode.token, nameToken, false, paramList, closureBlock);
                        AddParseSyntaxNodeInfo(fmdcs);
                        ParseCurrentNodeInfo pcnicClosure = new ParseCurrentNodeInfo(closureBlock);
                        m_CurrentNodeInfoStack.Push(pcnicClosure);
                        ParseSyntax(braceNode);
                        m_CurrentNodeInfoStack.Pop();

                        // 2. 替换为 Coroutine.spawnClosure0( tmpName )
                        Token tmpToken = new Token(nameToken);
                        Node tmpRefNode = new Node(tmpToken);
                        tmpRefNode.nodeType = ENodeType.IdentifierLink;
                        Node callNode = CreateCoroutineCallNode(cnode.token, "spawnClosure0",
                            new List<Node> { tmpRefNode });
                        pNodeList.RemoveRange(i, opIndex + 3 - i);
                        pNodeList.Insert(i, callNode);
                    }
                    else if (opNode.nodeType == ENodeType.IdentifierLink)
                    {
                        // spawn 函数变量调用: spawn f(a,b) -> Coroutine.spawnClosureN( f, a, b )
                        var linkList = opNode.GetLinkNodeList(true);
                        var lastLinkNode = linkList[linkList.Count - 1];
                        var parNode = lastLinkNode.parNode;
                        if (parNode == null)
                        {
                            // Log.AddNodeLog(LID.ShowExtendMessage, cnode.token,
                            //     "Error spawn 后必须是带参数列表的函数调用, 例如: spawn f( 1, 2 )");
                            return;
                        }
                        lastLinkNode.SetParNode(null);
                        // 实参数量: parNode childList 按逗号分段
                        int argCount = 0;
                        for (int p = 0; p < parNode.childList.Count; p++)
                        {
                            var pn = parNode.childList[p];
                            if (pn == null) continue;
                            if (pn.nodeType == ENodeType.Comma) argCount++;
                            else if (pn.nodeType == ENodeType.LineEnd || pn.nodeType == ENodeType.Comment) continue;
                            else if (argCount == 0) argCount = 1;
                        }
                        if (argCount > 3)
                        {
                            // Log.AddNodeLog(LID.ShowExtendMessage, cnode.token,
                            //     "Error spawn 目前最多支持 3 个参数!");
                            return;
                        }
                        // 新实参 childList: [ f, Comma, 原实参节点... ] (原 childList 自带 Comma 分隔)
                        List<Node> newArgNodes = new List<Node> { opNode };
                        Token splitCommaToken = new Token(cnode.token);
                        splitCommaToken.SetLexeme(",", ETokenType.Comma);
                        Node splitCommaNode = new Node(splitCommaToken);
                        splitCommaNode.nodeType = ENodeType.Comma;
                        newArgNodes.Add(splitCommaNode);
                        foreach (var pn in parNode.childList)
                        {
                            if (pn == null) continue;
                            if (pn.nodeType == ENodeType.Comment) continue;
                            newArgNodes.Add(pn);
                        }
                        Node callNode = CreateCoroutineCallNode(cnode.token, "spawnClosure" + argCount.ToString(), newArgNodes);
                        pNodeList.RemoveRange(i, opIndex + 1 - i);
                        pNodeList.Insert(i, callNode);
                    }
                    else
                    {
                        // Log.AddNodeLog(LID.ShowExtendMessage, cnode.token,
                        //     "Error spawn 后必须是函数调用或匿名闭包, 例如: spawn f( 1, 2 ) 或 spawn function(){...}");
                        return;
                    }
                }
                else // await
                {
                    // await expr -> Coroutine.awaitFunction( expr )
                    // 收集操作数直到顶层二元符号/赋值/换行结束 (await 结合力高于二元运算符)
                    List<Node> operandNodes = new List<Node>();
                    int end = opIndex;
                    for (; end < pNodeList.Count; end++)
                    {
                        var n = pNodeList[end];
                        if (n == null) break;
                        if (n.nodeType == ENodeType.Symbol || n.nodeType == ENodeType.Assign
                            || n.nodeType == ENodeType.LineEnd || n.nodeType == ENodeType.SemiColon
                            || n.nodeType == ENodeType.Comment || n.nodeType == ENodeType.Colon
                            || n.nodeType == ENodeType.QuestionMark || n.nodeType == ENodeType.DoubleQuestion
                            || n.nodeType == ENodeType.Comma)
                            break;
                        // as/is/isnot 二元关键字同样是操作数边界:
                        // await h as int -> Coroutine.awaitFunction(h) as int
                        if (n.nodeType == ENodeType.Key
                            && (n.token?.type == ETokenType.As || n.token?.type == ETokenType.Is
                                || n.token?.type == ETokenType.IsNot))
                            break;
                        operandNodes.Add(n);
                    }
                    if (operandNodes.Count == 0)
                    {
                        // Log.AddNodeLog(LID.ShowExtendMessage, cnode.token, "Error await 后缺少表达式!");
                        return;
                    }
                    Node callNode = CreateCoroutineCallNode(cnode.token, "awaitFunction", operandNodes);
                    pNodeList.RemoveRange(i, end - i);
                    pNodeList.Insert(i, callNode);
                }
            }
        }
        // 闭包参数解析: 把 Par 节点 childList 按逗号切分, 每段生成 FileMetaParamterDefine
        private List<FileMetaParamterDefine> ParseClosureParamList( Node parNode )
        {
            List<FileMetaParamterDefine> paramList = new List<FileMetaParamterDefine>();
            if (parNode == null) return paramList;

            List<List<Node>> tparamList = new List<List<Node>>();
            List<Node> tempList = new List<Node>();
            for (int i = 0; i < parNode.childList.Count; i++)
            {
                var pnode = parNode.childList[i];
                if (pnode == null) continue;
                if (pnode.nodeType == ENodeType.Comma)
                {
                    if (tempList.Count > 0)
                    {
                        tparamList.Add(tempList);
                        tempList = new List<Node>();
                    }
                }
                else if (pnode.nodeType == ENodeType.Comment || pnode.nodeType == ENodeType.LineEnd)
                {
                    continue;
                }
                else
                {
                    tempList.Add(pnode);
                }
            }
            if (tempList.Count > 0)
            {
                tparamList.Add(tempList);
            }

            for (int i = 0; i < tparamList.Count; i++)
            {
                FileMetaParamterDefine fmp = new FileMetaParamterDefine(m_FileMeta, tparamList[i]);
                paramList.Add(fmp);
            }
            return paramList;
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
                    if (fms != null)
                    {
                        AddParseSyntaxNodeInfo(fms);
                    }
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
                else if (akss.tokenType == ETokenType.Try)
                {
                    // Gather follow-up catch / finally nodes (like if/elif/else)
                    while (true)
                    {
                        Condition condition = new Condition(ETokenType.Catch);
                        condition.AddTokenTypeList(ETokenType.Finally);
                        SyntaxNodeStruct cakss = GetOneSyntax(pnode, condition);
                        if (cakss == null) break;

                        if (cakss.tokenType == ETokenType.Catch)
                        {
                            pnode.parseIndex += cakss.moveIndex;
                            akss.followKeySyntaxStructList.Add(cakss);
                            continue;
                        }
                        else if (cakss.tokenType == ETokenType.Finally)
                        {
                            pnode.parseIndex += cakss.moveIndex;
                            akss.followKeySyntaxStructList.Add(cakss);
                        }
                        break;
                    }

                    // Build FileMetaKeyTrySyntax
                    FileMetaKeyTrySyntax fmts = new FileMetaKeyTrySyntax(m_FileMeta);
                    if (akss.keyNode != null)
                    {
                        fmts.SetToken(akss.keyNode.token);
                    }
                    FileMetaBlockSyntax tryBlock = new FileMetaBlockSyntax(m_FileMeta, akss.blockNode.token, akss.blockNode.endToken);
                    fmts.SetTryBlock(tryBlock);
                    AddParseSyntaxNodeInfo(fmts);
                    fms = fmts;

                    // Parse try body
                    ParseCurrentNodeInfo pcnicTry = new ParseCurrentNodeInfo(tryBlock);
                    m_CurrentNodeInfoStack.Push(pcnicTry);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();

                    // Parse each catch / finally follow-key
                    foreach (var csns in akss.followKeySyntaxStructList)
                    {
                        if (csns.tokenType == ETokenType.Catch)
                        {
                            // Parse catch clause: only supports "catch e" or "catch Type e" or "catch"
                            // Parentheses like catch (Type e) are NOT supported
                            Token typeToken = null;
                            Token varToken = null;
                            var catchContent = csns.keyContent;
                            for (int ci = 0; ci < catchContent.Count; ci++)
                            {
                                var cn = catchContent[ci];
                                if (cn.token == null) continue;
                                var tt = cn.token.type;
                                // Only accept Identifier or Data tokens as type/var
                                if (tt != ETokenType.Identifier && tt != ETokenType.Data)
                                    continue;
                                if (typeToken == null)
                                {
                                    typeToken = cn.token;
                                }
                                else if (varToken == null)
                                {
                                    varToken = cn.token;
                                }
                            }
                            // If only one identifier, it's a variable name not a type
                            if (typeToken != null && varToken == null && catchContent.Count == 1)
                            {
                                varToken = typeToken;
                                typeToken = null;
                            }

                            FileMetaBlockSyntax catchBlock = new FileMetaBlockSyntax(m_FileMeta, csns.blockNode.token, csns.blockNode.endToken);
                            var clause = new FileMetaCatchClause(csns.keyNode.token, typeToken, varToken, catchBlock);
                            fmts.AddCatchClause(clause);

                            ParseCurrentNodeInfo pcnicCatch = new ParseCurrentNodeInfo(catchBlock);
                            m_CurrentNodeInfoStack.Push(pcnicCatch);
                            ParseSyntax(csns.blockNode);
                            m_CurrentNodeInfoStack.Pop();
                        }
                        else if (csns.tokenType == ETokenType.Finally)
                        {
                            FileMetaBlockSyntax finallyBlock = new FileMetaBlockSyntax(m_FileMeta, csns.blockNode.token, csns.blockNode.endToken);
                            fmts.SetFinallyBlock(finallyBlock);

                            ParseCurrentNodeInfo pcnicFinally = new ParseCurrentNodeInfo(finallyBlock);
                            m_CurrentNodeInfoStack.Push(pcnicFinally);
                            ParseSyntax(csns.blockNode);
                            m_CurrentNodeInfoStack.Pop();
                        }
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
                else if (akss.tokenType == ETokenType.Function)
                {
                    // 具名闭包: function name( 参数列表 ) { 闭包体 }
                    // 闭包只能出现在方法体内
                    var curInfo = currentNodeInfo;
                    if (curInfo == null ||
                        (curInfo.parseType != EParseNodeType.Statements && curInfo.parseType != EParseNodeType.Function))
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, akss.keyNode?.token, "Error 闭包(function)只能定义在方法体内!");
                    }

                    Token closureNameToken = null;
                    Node closureParNode = null;
                    foreach (var cnode in akss.keyContent)
                    {
                        if (cnode == null) continue;
                        if (cnode.nodeType == ENodeType.IdentifierLink)
                        {
                            closureNameToken = cnode.token;
                            closureParNode = cnode.parNode;
                        }
                        else if (cnode.nodeType == ENodeType.Comment || cnode.nodeType == ENodeType.LineEnd
                            || cnode.nodeType == ENodeType.SemiColon)
                        {
                            continue;
                        }
                        else
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, cnode.token, "Error 闭包定义语法不正确 应为 function name( 参数 ) { 闭包体 }");
                        }
                    }
                    if (closureNameToken == null)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, akss.keyNode?.token, "Error 闭包定义缺少名称");
                    }
                    else if (akss.blockNode != null)
                    {
                        List<FileMetaParamterDefine> paramList = ParseClosureParamList(closureParNode);
                        FileMetaBlockSyntax closureBlock = new FileMetaBlockSyntax(m_FileMeta, akss.blockNode.token, akss.blockNode.endToken);
                        FileMetaDefineClosureSyntax fmdcs = new FileMetaDefineClosureSyntax(m_FileMeta,
                            akss.keyNode.token, closureNameToken, false, paramList, closureBlock);
                        AddParseSyntaxNodeInfo(fmdcs);
                        fms = fmdcs;

                        ParseCurrentNodeInfo pcnicClosure = new ParseCurrentNodeInfo(closureBlock);
                        m_CurrentNodeInfoStack.Push(pcnicClosure);
                        ParseSyntax(akss.blockNode);
                        m_CurrentNodeInfoStack.Pop();
                    }
                }
                else if (akss.tokenType == ETokenType.Yield)
                {
                    // yield; 语句语法糖: 展开为 Coroutine.yieldNow()
                    // 挂起当前协程, 让出执行权给调度器 (等价于旧写法 Coroutine.yieldNow())
                    Node yieldCallNode = CreateCoroutineCallNode(akss.keyNode.token, "yieldNow", new List<Node>());
                    var yieldExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta,
                        new List<Node> { yieldCallNode }, FileMetaTermExpress.EExpressType.Common);
                    if (yieldExpress != null)
                    {
                        FileMetaCallSyntax fmcs = new FileMetaCallSyntax(yieldExpress);
                        AddParseSyntaxNodeInfo(fmcs);
                        fms = fmcs;
                    }
                    else
                    {
                        // Log.AddNodeLog(LID.ShowExtendMessage, akss.keyNode.token, "Error yield 语句展开为 Coroutine.yieldNow() 失败!");
                    }
                }
                else if (akss.tokenType == ETokenType.Return
                    || akss.tokenType == ETokenType.Transience)
                {
                    FileMetaBaseTerm conditionExpress = null;

                    if (akss.keyContent.Count > 0)
                    {
                        // ret spawn f(a,b) / ret await h -> 展开协程关键字后再生成表达式
                        TransformCoroutineKeywordNodes(akss.keyContent);
                        conditionExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, akss.keyContent, FileMetaTermExpress.EExpressType.Common);
                    }

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
                else if (akss.tokenType == ETokenType.Throw)
                {
                    FileMetaBaseTerm throwExpress = null;
                    if (akss.keyContent.Count > 0)
                    {
                        throwExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, akss.keyContent, FileMetaTermExpress.EExpressType.Common);
                    }
                    FileMetaKeyThrowSyntax fmks = new FileMetaKeyThrowSyntax(m_FileMeta, akss.keyNode.token, throwExpress);
                    AddParseSyntaxNodeInfo(fmks);
                    fms = fmks;
                }
                else if (akss.tokenType == ETokenType.Defer
                    || akss.tokenType == ETokenType.ErrDefer
                    || akss.tokenType == ETokenType.Checked
                    || akss.tokenType == ETokenType.Unchecked)
                {
                    FileMetaBlockSyntax deferBlock = new FileMetaBlockSyntax(m_FileMeta, akss.blockNode.token, akss.blockNode.endToken);
                    FileMetaKeyOnlySyntax fmkis = new FileMetaKeyOnlySyntax(m_FileMeta, akss.keyNode.token, deferBlock);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(deferBlock);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.Label
                    || akss.tokenType == ETokenType.Goto)
                {
                    Token labelToken = null;
                    // Label name may be in keyContent or commonContent depending on AddContent routing
                    var labelContent = akss.keyContent.Count > 0 ? akss.keyContent : akss.commonContent;
                    if (labelContent.Count != 1)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析Goto Label语法，只支持 goto id;的语法!!");
                    }
                    else
                    {
                        labelToken = labelContent[0].token;
                        if (labelToken.type != ETokenType.Identifier)
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析GotoLabel中 后边必须使用普通字符");
                        }
                    }

                    // Check for catch/finally follow-up -> treat as try-catch block
                    var tryBlockNode = akss.blockNode ?? akss.keyNode?.blockNode;
                    bool hasCatchFinally = false;
                    if (tryBlockNode != null && akss.tokenType == ETokenType.Label)
                    {
                        Condition catchCondition = new Condition(ETokenType.Catch);
                        catchCondition.AddTokenTypeList(ETokenType.Finally);
                        SyntaxNodeStruct cakss = GetOneSyntax(pnode, catchCondition);
                        if (cakss != null && cakss.moveIndex > 0)
                        {
                            hasCatchFinally = true;

                            // Build FileMetaKeyTrySyntax
                            FileMetaKeyTrySyntax fmts = new FileMetaKeyTrySyntax(m_FileMeta);
                            fmts.SetToken(akss.keyNode.token);
                            if (m_PendingCheckedLabel)
                            {
                                fmts.SetIsChecked(true);
                                m_PendingCheckedLabel = false;
                            }
                            FileMetaBlockSyntax tryBlock = new FileMetaBlockSyntax(m_FileMeta, tryBlockNode.token, tryBlockNode.endToken);
                            fmts.SetTryBlock(tryBlock);
                            AddParseSyntaxNodeInfo(fmts);
                            fms = fmts;

                            // Parse try body
                            ParseCurrentNodeInfo pcnicTry = new ParseCurrentNodeInfo(tryBlock);
                            m_CurrentNodeInfoStack.Push(pcnicTry);
                            ParseSyntax(tryBlockNode);
                            m_CurrentNodeInfoStack.Pop();

                            // Gather remaining catch/finally
                            akss.followKeySyntaxStructList.Add(cakss);
                            pnode.parseIndex += cakss.moveIndex;
                            while (true)
                            {
                                Condition cond = new Condition(ETokenType.Catch);
                                cond.AddTokenTypeList(ETokenType.Finally);
                                SyntaxNodeStruct cakss2 = GetOneSyntax(pnode, cond);
                                if (cakss2 == null) break;
                                if (cakss2.tokenType != ETokenType.Catch && cakss2.tokenType != ETokenType.Finally) break;
                                pnode.parseIndex += cakss2.moveIndex;
                                akss.followKeySyntaxStructList.Add(cakss2);
                                if (cakss2.tokenType == ETokenType.Finally) break;
                            }

                            // Parse each catch / finally
                            foreach (var csns in akss.followKeySyntaxStructList)
                            {
                                if (csns.tokenType == ETokenType.Catch)
                                {
                                    Token typeToken = null;
                                    Token varToken = null;
                                    var catchContent = csns.keyContent;
                                    for (int ci = 0; ci < catchContent.Count; ci++)
                                    {
                                        var cn = catchContent[ci];
                                        if (cn.token == null) continue;
                                        var tt = cn.token.type;
                                        if (tt != ETokenType.Identifier && tt != ETokenType.Data)
                                            continue;
                                        if (typeToken == null)
                                            typeToken = cn.token;
                                        else if (varToken == null)
                                            varToken = cn.token;
                                    }
                                    if (typeToken != null && varToken == null && catchContent.Count == 1)
                                    {
                                        varToken = typeToken;
                                        typeToken = null;
                                    }

                                    FileMetaBlockSyntax catchBlock = new FileMetaBlockSyntax(m_FileMeta, csns.blockNode.token, csns.blockNode.endToken);
                                    var clause = new FileMetaCatchClause(csns.keyNode.token, typeToken, varToken, catchBlock);
                                    fmts.AddCatchClause(clause);

                                    ParseCurrentNodeInfo pcnicCatch = new ParseCurrentNodeInfo(catchBlock);
                                    m_CurrentNodeInfoStack.Push(pcnicCatch);
                                    ParseSyntax(csns.blockNode);
                                    m_CurrentNodeInfoStack.Pop();
                                }
                                else if (csns.tokenType == ETokenType.Finally)
                                {
                                    FileMetaBlockSyntax finallyBlock = new FileMetaBlockSyntax(m_FileMeta, csns.blockNode.token, csns.blockNode.endToken);
                                    fmts.SetFinallyBlock(finallyBlock);

                                    ParseCurrentNodeInfo pcnicFinally = new ParseCurrentNodeInfo(finallyBlock);
                                    m_CurrentNodeInfoStack.Push(pcnicFinally);
                                    ParseSyntax(csns.blockNode);
                                    m_CurrentNodeInfoStack.Pop();
                                }
                            }
                        }
                    }

                    if (!hasCatchFinally)
                    {
                        FileMetaKeyGotoLabelSyntax fmkis = new FileMetaKeyGotoLabelSyntax(m_FileMeta, akss.keyNode.token, labelToken);
                        AddParseSyntaxNodeInfo(fmkis);
                        fms = fmkis;

                        ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fms);
                        m_CurrentNodeInfoStack.Push(pcnic);
                        if (tryBlockNode != null)
                        {
                            ParseSyntax(tryBlockNode);
                        }
                        m_CurrentNodeInfoStack.Pop();
                    }
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
            }
            if (defineVariableSyntax == null)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error 解析for 第一部分错误，解析语句出错，不是定义类型语句!!");
                return fms;
            }
            defineVariableSyntax.isAppendSemiColon = false;
            fms.SetFileMetaClassDefine(defineVariableSyntax);
            if (inToken != null)
            {
                if(conditionExpressNodeList.Count > 0 )
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
            }
            else
            {
                if (conditionExpressNodeList.Count > 0)
                {
                    var cfe = FileMetatUtil.CreateFileMetaExpress(fm, conditionExpressNodeList, FileMetaTermExpress.EExpressType.Common);
                    if (cfe == null)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, conditionExpressNodeList[0]?.token, "Error 解析for 第二部分错误!!");
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
            FileMetaBaseTerm sourceExpress = null;
            if (cnode.parNode != null && cnode.parNode.childList?.Count > 0)
            {
                fmcl = new FileMetaCallLink(fm, cnode.parNode.childList[0]);
            }
            if( fmcl == null )
            {
                // switch( i ) / switch( x + y ): switch 的 Key 节点没有 identifierNode,
                // 括号作为普通子节点进入 keyContent[0]，需要剥开括号取内部内容
                var srcNodes = sns.keyContent;
                if (srcNodes.Count > 0 && srcNodes[0].nodeType == ENodeType.Par)
                {
                    var innerList = new List<Node>();
                    var parChildren = srcNodes[0].childList;
                    if (parChildren != null)
                    {
                        for (int i = 0; i < parChildren.Count; i++)
                        {
                            var pn = parChildren[i];
                            if (pn == null
                                || pn.nodeType == ENodeType.LineEnd
                                || pn.nodeType == ENodeType.Comment
                                || pn.nodeType == ENodeType.SemiColon)
                            {
                                continue;
                            }
                            innerList.Add(pn);
                        }
                    }
                    srcNodes = innerList;
                }
                if (srcNodes.Count == 1)
                {
                    // switch( i ): 单标识符源，与无括号形式一致
                    fmcl = new FileMetaCallLink(fm, srcNodes[0]);
                }
                else if (srcNodes.Count > 1)
                {
                    // switch( x + y ): 表达式源
                    sourceExpress = FileMetatUtil.CreateFileMetaExpress(fm, srcNodes, FileMetaTermExpress.EExpressType.Common);
                }
            }
            if( fmcl == null && sourceExpress == null )
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error 创建 FileMetaCallLink 失败");
            }
            var fms = new FileMetaKeySwitchSyntax(fm, cnode.token, sns.blockNode.token, sns.blockNode.endToken, fmcl);
            if (sourceExpress != null)
            {
                fms.SetSourceExpress(sourceExpress);
            }

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
            bool isMulti = false;
            for (int i = 0; i < parlist.Count; i++)
            {
                var tt = parlist[i].token?.type;
                // 逗号和 | 都作为多值分隔符: case 1|2|3{}/case 1,2,3{}
                if (tt == ETokenType.Comma || tt == ETokenType.InclusiveOr)
                {
                    isMulti = true;
                    continue;
                }
                childList.Add(parlist[i]);
            }

            // `case is ClassA {}` / `case is ClassA c1 {}` 类型模式匹配（参考 C# 的 is 用法）
            if (childList.Count >= 1 && childList[0].token?.type == ETokenType.Is)
            {
                if (childList.Count == 2)
                {
                    fcase.SetDefineClassNode(childList[1]);
                }
                else if (childList.Count == 3)
                {
                    fcase.SetDefineClassNode(childList[1]);
                    fcase.SetVariableToken(childList[2].token);
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error case is 类型匹配的格式为: case is ClassName [变量名]!!");
                }
                return;
            }

            if (isMulti)
            {
                bool isSame = true;
                for (int i = 0; i < childList.Count - 1; i++)
                {
                    var curNode = childList[i];
                    var nextNode = childList[i + 1];
                    var type = curNode.token.type;
                    if (type != ETokenType.Number && type != ETokenType.String && type != ETokenType.BoolValue)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 多值分割(|/)只允许number,string,bool");
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
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 使用|或逗号切割开后，类型不相同!!");
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
                        || ttype == ETokenType.String
                        || ttype == ETokenType.BoolValue)
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