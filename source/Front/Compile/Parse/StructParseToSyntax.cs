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
                    || tokenType == ETokenType.ErrDefer)
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
                    || tokenType == ETokenType.Unchecked)
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
                    || tokenType == ETokenType.ErrDefer )
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
                || tokenType == ETokenType.Unchecked;
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
                                || ttt == ETokenType.ErrDefer ) // ClassName(){}
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
                        || ttt == ETokenType.Const)
                    {
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
                else if (akss.tokenType == ETokenType.Return
                    || akss.tokenType == ETokenType.Transience)
                {
                    FileMetaBaseTerm conditionExpress = null;

                    if (akss.keyContent.Count > 0)
                    {
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