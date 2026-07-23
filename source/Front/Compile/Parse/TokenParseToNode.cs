//****************************************************************************
//  File:      TokenParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SimpleLanguage.Compile
{
    /// <summary> 解析token </summary>
    public class TokenParse
    {
        public Node rootNode => m_RootNode;

        private FileMeta m_FileMeta = null;
        private List<Token> m_TokensList = null;
        private int m_TokenIndex = 0;
        private int m_TokenCount = 0;

        private Node m_RootNode = new Node(null);
        private Node m_CurrentNode = null;

        public TokenParse(FileMeta fm, List<Token> list)
        {
            m_FileMeta = fm;
            m_TokensList = list;
            m_TokenCount = list.Count;
            m_RootNode.nodeType = ENodeType.Root;
            m_CurrentNode = m_RootNode;
        }
        public void BuildStruct()
        {
            while (m_TokenIndex < m_TokenCount)
            {
                var tempToken = m_TokensList[m_TokenIndex];
                if (tempToken.type == ETokenType.Finished) { break; }
                ParseTokenConvertNode(tempToken);
            }
            return;
        }

        public void AddIdentifier(Token code)      //Print/Function
        {
            Node node = new Node(code);
            node.nodeType = ENodeType.IdentifierLink;

            if (m_CurrentNode.linkToken != null)
            {
                Node node2 = new Node(m_CurrentNode.linkToken);
                node2.nodeType = ENodeType.Period;

                if( m_CurrentNode.identifierNode == null )
                {
                    if( m_CurrentNode.childList.Count > 0 )
                    {
                        var ccc = m_CurrentNode.childList[m_CurrentNode.childList.Count - 1];
                        if( ccc.nodeType == ENodeType.Par && ccc?.endToken?.type == ETokenType.RightPar )
                        {
                            ccc.extendLinkNodeList.Add(node2);
                            ccc.extendLinkNodeList.Add(node);
                        }

                    }
                }
                else
                {

                    m_CurrentNode.AddLinkNode(node2);
                    m_CurrentNode.AddLinkNode(node);
                    
                }
                if (m_CurrentNode.atToken != null)
                {
                    node.atToken = m_CurrentNode.atToken;
                    m_CurrentNode.atToken = null;
                }
                m_CurrentNode.linkToken = null;
            }
            else
            {
                m_CurrentNode.AddChild(node);
            }
            m_CurrentNode.SetIdentifierNode(node);

            m_TokenIndex++;
        }
        void RestoreAngleNode()
        {
            string res = m_CurrentNode.token?.extend?.ToString();
            if (res == "1")
            {
                m_CurrentNode.parent.childList.Remove(m_CurrentNode);

                Token token = new Token(m_CurrentNode.token);
                token.SetType(ETokenType.Less);
                Node node2 = new Node(token);
                node2.nodeType = ENodeType.Symbol;
                node2.priority = SignComputePriority.Level6_Compare;
                m_CurrentNode.parent.AddChild(node2);

                m_CurrentNode.parent.childList.AddRange(m_CurrentNode.childList);

                m_CurrentNode = m_CurrentNode.parent;
            }
            else if (res == "2")
            {
                m_CurrentNode.parent.childList.Remove(m_CurrentNode);

                Token token = new Token(m_CurrentNode.token);
                token.SetType(ETokenType.Shi);
                token.SetLexeme("<<");
                Node node2 = new Node(token);
                node2.nodeType = ENodeType.Symbol;
                node2.priority = SignComputePriority.Level5_BitMoveOp;
                m_CurrentNode.parent.AddChild(node2);

                m_CurrentNode.parent.childList.AddRange(m_CurrentNode.childList);

                m_CurrentNode = m_CurrentNode.parent;
            }
            else
            {
                Log.AddTokenLog(LID.ShowExtendMessage, "", m_CurrentNode.token, "现在$符必须使用.$方式!!");
            }
        }
        private Node AddKeyNode(Token token)
        {
            if (m_CurrentNode.nodeType == ENodeType.Angle)
            {
                RestoreAngleNode();
            }
            Node node = new Node(token);
            node.nodeType = ENodeType.Key;
            m_CurrentNode.AddChild(node);
            if( token.type == ETokenType.Base || token.type == ETokenType.This
                || token.type == ETokenType.Local || token.type == ETokenType.Global )
            {
                m_CurrentNode.SetIdentifierNode(node);
            }
            m_TokenIndex++;
            return node;
        }
        private Node AddKeyNodeInAngle(Token token)
        {
            Node node = new Node(token);
            node.nodeType = ENodeType.Key;
            m_CurrentNode.AddChild(node);
            m_TokenIndex++;
            return node;
        }
        private Node AddAtOpSign(Token token)
        {
            if (m_CurrentNode.linkToken != null)
            {
                if (token.type == ETokenType.At)
                {
                    // `.@` is no longer supported; reserve '@' for attribute syntax.
                    Log.AddTokenLog(LID.ShowExtendMessage, "不再支持 a.@b 语法，请使用 a.$b / a.$0 形式");
                    m_CurrentNode.linkToken = null;
                    m_TokenIndex++;
                    return null;
                }

                var ntoken = new Token(token);
                string nvar = token.extend.ToString();

                Node node = new Node(ntoken);
                if (Regex.IsMatch(nvar, @"^\d+$"))
                {
                    int lex = 0;
                    int.TryParse(nvar, out lex);
                    ntoken.SetLexeme(lex);
                    ntoken.SetType(ETokenType.Number);
                    ntoken.SetExtend(EType.Int32);
                    node.nodeType = ENodeType.ConstValue;
                }
                else
                {
                    ntoken.SetLexeme(token.extend);
                    ntoken.SetType(ETokenType.Identifier);
                    node.nodeType = ENodeType.IdentifierLink;
                }


                Node node2 = new Node(m_CurrentNode.linkToken);
                node2.nodeType = ENodeType.Period;

                m_CurrentNode.AddLinkNode(node2);
                m_CurrentNode.AddLinkNode(node);
                node.atToken = token;

                m_CurrentNode.linkToken = null;
                //tempNode.lastNode = node;

            }
            else
            {
                Log.AddTokenLog(LID.ShowExtendMessage, "现在$符必须使用.$方式!!");
            }
            m_TokenIndex++;

            return null;
        }
        private Node AddSymbol(Token token )
        {
            if (m_CurrentNode.nodeType == ENodeType.Angle)
            {
                RestoreAngleNode();
            }

            m_CurrentNode.SetIdentifierNode(null);

            Node node = new Node(token);
            node.nodeType = ENodeType.Symbol;
            m_CurrentNode.AddChild(node);
            m_TokenIndex++;
            return node;

        }
        public void AddLessSign(Token token)
        {
            var angleNode = new Node(token);
            angleNode.nodeType = ENodeType.Angle;
            m_CurrentNode.AddChild(angleNode);

            m_CurrentNode = angleNode;

            m_TokenIndex++;
        }
        public void AddGreaterSign(Token token)
        {
            if (m_CurrentNode.nodeType == ENodeType.Angle)
            {
                int count = (int)token.extend;
                if( count == 0 )
                {
                    Log.AddNodeLog(LID.MetaCoreAssertShowMessage, token, "greater count is zero");
                    return;
                }

                while( count > 0 )
                {
                    m_CurrentNode.endToken = token;
                    var angleNode = m_CurrentNode;
                    m_CurrentNode = m_CurrentNode.parent;

                    if (m_CurrentNode.identifierNode != null)
                    {
                        m_CurrentNode.childList.Remove(angleNode);
                        m_CurrentNode.identifierNode.SetAngleNode(angleNode);
                    }
                    count--;
                }
            }
            else
            {
                int extend = 1;
                if( int.TryParse( token?.extend?.ToString(), out int oint ) )
                {
                    extend = oint;
                }
                if(token.extend?.ToString() == "2" )
                {
                    Token token2 = new Token(token);
                    token2.SetLexeme(">>", ETokenType.Shr);
                    Node node = new Node(token2);
                    node.priority = SignComputePriority.Level5_BitMoveOp;
                    node.nodeType = ENodeType.Symbol;
                    m_CurrentNode.AddChild(node);
                }
                else
                {
                    Node node = new Node(token);
                    node.priority = SignComputePriority.Level6_Compare;
                    node.nodeType = ENodeType.Symbol;
                    m_CurrentNode.AddChild(node);
                }
            }
            m_TokenIndex++;
        }
        public void AddParBegin(Token token)
        {
            var newNode = new Node(token);
            newNode.nodeType = ENodeType.Par;

            if (m_CurrentNode.identifierNode != null)
            {
                newNode.SetParentNode(m_CurrentNode);
                m_CurrentNode.identifierNode.SetParNode(newNode);
            }
            else
            {
                m_CurrentNode.AddChild(newNode);
            }
            m_CurrentNode = newNode;
            m_TokenIndex++;
        }
        public void AddParEnd(Token token)
        {
            m_TokenIndex++;
            if (m_CurrentNode.nodeType == ENodeType.Par)
            {
                m_CurrentNode.endToken = token;
                m_CurrentNode = m_CurrentNode.parent;
                return;
            }
            else if( m_CurrentNode.nodeType == ENodeType.Angle )
            {
                RestoreAngleNode();
                if( m_CurrentNode.nodeType == ENodeType.Par )
                {
                    m_CurrentNode.endToken = token;
                    m_CurrentNode = m_CurrentNode.parent;
                    return;
                }
            }
            
            Log.AddNodeLog(LID.MetaCoreAssertShowMessage, token, "() 符号没有对称! 原符号:" + m_CurrentNode.token.ToLexemeAllString() + " 新符号n:" + token.ToLexemeAllString());
            
        }
        public void AddBracketBegin(Token token)
        {
            var newNode = new Node(token);
            newNode.nodeType = ENodeType.Bracket;

            if (m_CurrentNode.identifierNode != null)
            {
                newNode.SetParentNode(m_CurrentNode);
                m_CurrentNode.identifierNode.AddBracketNode(newNode);
            }
            else
            {
                m_CurrentNode.AddChild(newNode);
            }
            m_CurrentNode = newNode;
            m_TokenIndex++;
        }
        public void AddBracketEnd(Token token)
        {
            if (m_CurrentNode.nodeType == ENodeType.Bracket)
            {
                m_CurrentNode.endToken = token;
                m_CurrentNode = m_CurrentNode.parent;
            }
            else
            {
                Log.AddNodeLog(LID.MetaCoreAssertShowMessage, token, "() 符号没有对称! 原符号:" + m_CurrentNode.token.ToLexemeAllString() + " 新符号n:" + token.ToLexemeAllString());
            }
            m_TokenIndex++;
        }
        public void AddBraceBegin(Token token)
        {
            var newNode = new Node(token);
            newNode.nodeType = ENodeType.Brace;
            m_CurrentNode.AddChild(newNode);

            m_CurrentNode = newNode;
            m_TokenIndex++;
        }
        public void AddBraceEnd(Token token)
        {
            if (m_CurrentNode.nodeType == ENodeType.Brace)
            {
                m_CurrentNode.endToken = token;
                m_CurrentNode = m_CurrentNode.parent;
            }
            else
            {
                m_CurrentNode.endToken = token;


                if (m_CurrentNode.identifierNode != null)
                {
                    m_CurrentNode.identifierNode.SetAngleNode(m_CurrentNode);
                }
                m_CurrentNode = m_CurrentNode.parent;
            }
            m_TokenIndex++;
        }
        void ParseTokenConvertNode(Token token)
        {
            //if (m_CurrentNode == null)
            //{
            //    Log.AddTokenLog(LID.ShowExtendMessage, "", token, "Error CurrentNode is NULL!!" + token?.ToLexemeAllString());
            //    return;
            //}
            switch (token.type)
            {
                case ETokenType.Identifier:  //Identifier
                case ETokenType.Type:
                case ETokenType.Object:
                case ETokenType.Boolean:
                    {
                        AddIdentifier(token);
                    }
                    break;
                case ETokenType.Number:
                case ETokenType.NumberReal:
                case ETokenType.String:
                case ETokenType.BoolValue:
                case ETokenType.NumberArrayLink:
                case ETokenType.Null:
                    {
                        if (m_CurrentNode.nodeType == ENodeType.Angle)
                        {
                            RestoreAngleNode();
                        }


                        Node node = new Node(token);
                        node.nodeType = ENodeType.ConstValue;
                        if (m_CurrentNode.linkToken != null)
                        {
                            Node node2 = new Node(m_CurrentNode.linkToken);
                            node2.nodeType = ENodeType.Period;

                            m_CurrentNode.AddLinkNode(node2);
                            m_CurrentNode.AddLinkNode(node);
                            m_CurrentNode.linkToken = null;
                            if (m_CurrentNode.atToken != null)
                            {
                                node.atToken = m_CurrentNode.atToken;
                                m_CurrentNode.atToken = null;
                            }
                        }
                        else
                        {
                            m_CurrentNode.AddChild(node);
                        }

                        //tempNode.lastNode = node;

                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.LeftPar: //(
                    {
                        AddParBegin(token);
                    }
                    break;
                case ETokenType.RightPar: //)
                    {
                        AddParEnd(token);
                    }
                    break;
                case ETokenType.LeftBracket://[
                    {
                        AddBracketBegin(token);
                    }
                    break;
                case ETokenType.RightBracket://]
                    {
                        AddBracketEnd(token);
                    }
                    break;
                case ETokenType.LeftBrace: //{
                    {
                        AddBraceBegin(token);
                    }
                    break;
                case ETokenType.RightBrace: //}
                    {
                        AddBraceEnd(token);
                    }
                    break;
                case ETokenType.Less:         // <
                    {
                        AddLessSign(token);
                    }
                    break;
                case ETokenType.Greater:            // >
                    {
                        AddGreaterSign(token);
                    }
                    break;
                case ETokenType.Period:  //.
                    {
                        m_CurrentNode.linkToken = token;
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.QuestionMarkDot: // ?.
                    {
                        // treat null-conditional operator like a linking token (similar to '.')
                        m_CurrentNode.linkToken = token;
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Comma:   //,
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Comma;
                        m_CurrentNode.AddChild(node);
                        m_CurrentNode.SetIdentifierNode(null);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.QuestionMark: //?
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.QuestionMark;
                        m_CurrentNode.AddChild(node);
                        m_CurrentNode.SetIdentifierNode(null);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.EmptyRet: //??
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.DoubleQuestion;
                        m_CurrentNode.AddChild(node);
                        m_CurrentNode.SetIdentifierNode(null);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Colon:       //:
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Colon;
                        m_CurrentNode.AddChild(node);
                        m_CurrentNode.SetIdentifierNode(null);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.SemiColon:      //;
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.SemiColon;
                        m_CurrentNode.AddChild(node);
                        m_CurrentNode.SetIdentifierNode(null);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.LineEnd:      // \n
                    {
                        if (m_CurrentNode.nodeType == ENodeType.Angle)
                        {
                            RestoreAngleNode();
                        }
                        Node node = new Node(token);
                        node.nodeType = ENodeType.LineEnd;
                        m_CurrentNode.AddChild(node);
                        m_CurrentNode.SetIdentifierNode(null);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Assign:             //=
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Assign;
                        m_CurrentNode.AddChild(node);
                        m_TokenIndex++;
                        m_CurrentNode.SetIdentifierNode(null);
                    }
                    break;
                case ETokenType.Plus:            //+
                case ETokenType.Minus:           //-
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level3_Low_Compute;
                    }
                    break;
                case ETokenType.As:
                case ETokenType.Is:
                case ETokenType.IsNot:
                    {
                        // as 有两个作用一个是import 里边 代名   一个是 as 类
                        var node = AddKeyNode(token);
                        //var node = AddSymbol(code);
                        node.priority = SignComputePriority.Level9_AsOsIs;
                    }
                    break;
                case ETokenType.DoublePlus:     //++
                case ETokenType.DoubleMinus:    //--
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level2_LinkOp;
                    }
                    break;
                case ETokenType.Multiply:        // *
                case ETokenType.Divide:          // /
                case ETokenType.Modulo:          // %
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level3_Hight_Compute;
                    }
                    break;
                case ETokenType.Not:             // !
                case ETokenType.Negative:        // ~
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level2_LinkOp;
                    }
                    break;
                case ETokenType.Shi:               //  <<
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level5_BitMoveOp;
                    }
                    break;
                //case ETokenType.Shr:               //  >>
                //    {
                //        // In nested generics like Map<List<int>,string>> the lexer produces Shr.
                //        // If we're currently inside an unclosed generic angle sequence, treat this
                //        // as two closing '>' tokens.
                //        if (IsInsideGenericAngleContext())
                //        {
                //            var t1 = new Token(token);
                //            t1.SetType(ETokenType.Greater);
                //            t1.SetLexeme(">");
                //            var n1 = new Node(t1) { nodeType = ENodeType.RightAngle };
                //            m_CurrentNode.AddChild(n1);

                //            var t2 = new Token(token);
                //            t2.SetType(ETokenType.Greater);
                //            t2.SetLexeme(">");
                //            var n2 = new Node(t2) { nodeType = ENodeType.RightAngle };
                //            m_CurrentNode.AddChild(n2);

                //            m_TokenIndex++;
                //        }
                //        else
                //        {
                //            AddBitMoveOperatorSymbol(token);
                //        }
                //    }
                //    break;
                case ETokenType.GreaterOrEqual:  // >=
                case ETokenType.LessOrEqual:     // <=
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level6_Compare;
                    }
                    break;
                case ETokenType.Equal:           // ==
                case ETokenType.NotEqual:        // !=
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level7_EqualAb;
                    }
                    break;
                case ETokenType.Combine:                // &
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level8_BitAndOp;
                    }
                    break;
                case ETokenType.InclusiveOr:            // |
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level8_BitOrOp;
                    }
                    break;
                case ETokenType.XOR:                    //  ^
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level8_BitXOrOp;
                    }
                    break;
                case ETokenType.Or:              // ||
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level9_Or;
                    }
                    break;
                case ETokenType.And:             // &&  
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level9_And;
                    }
                    break;
                case ETokenType.PlusAssign:             // +=
                case ETokenType.MinusAssign:            // -=
                case ETokenType.MultiplyAssign:         // *=
                case ETokenType.DivideAssign:           // /=
                case ETokenType.ModuloAssign:           // %=
                case ETokenType.CombineAssign:          // &=
                case ETokenType.InclusiveOrAssign:      // |=
                case ETokenType.XORAssign:              // ^=
                case ETokenType.ShiAssign:              // <<=
                case ETokenType.ShrAssign:              // >>=
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level11_Assign;
                    }
                    break;
                case ETokenType.Sharp:   //#
                    {
                        m_TokenIndex++;
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Comment;
                        m_CurrentNode.AddChild(node);
                    }
                    break;

                case ETokenType.Import:
                case ETokenType.TypeAlias:
                    {
                        var nnode = new Node(token);
                        nnode.nodeType = ENodeType.Key;
                        m_TokenIndex++;
                        m_CurrentNode.AddChild(nnode);
                    }
                    break;
                case ETokenType.Namespace:
                    {
                        var nnode = new Node(token);
                        nnode.nodeType = ENodeType.Key;
                        m_TokenIndex++;
                        m_CurrentNode.AddChild(nnode);
                    }
                    break;
                case ETokenType.Enum:
                case ETokenType.Data:
                case ETokenType.Class:
                case ETokenType.Dynamic:
                case ETokenType.Extern:
                case ETokenType.Public:
                case ETokenType.Projected:
                case ETokenType.Private:
                //case ETokenType.Operator:
                case ETokenType.Base:         //base
                case ETokenType.This:           //this
                case ETokenType.Local:
                case ETokenType.Global:
                case ETokenType.Range:
                case ETokenType.Const:
                case ETokenType.Mut:
                case ETokenType.Final:
                case ETokenType.Static:
                case ETokenType.Override:
                case ETokenType.Partial:
                case ETokenType.Void:
                case ETokenType.Get:
                case ETokenType.Set:
                case ETokenType.Interface:
                case ETokenType.Abstract:
                case ETokenType.Extends:
                case ETokenType.If:
                case ETokenType.ElseIf:
                case ETokenType.For:
                case ETokenType.While:
                case ETokenType.DoWhile:
                case ETokenType.Case:
                case ETokenType.Return:
                case ETokenType.Goto:
                case ETokenType.Transience:
                case ETokenType.Label:
                case ETokenType.Else:
                case ETokenType.Switch:
                case ETokenType.Continue:
                case ETokenType.Break:
                case ETokenType.Default:
                case ETokenType.Var:
                case ETokenType.Next:
                case ETokenType.Params:
                case ETokenType.Try:
                case ETokenType.Catch:
                case ETokenType.Finally:
                case ETokenType.Throw:
                    {
                        AddKeyNode(token);
                    }
                    break;
                case ETokenType.New:
                    {
                        var node = AddKeyNode(token);
                        m_CurrentNode.SetIdentifierNode(node);
                    }
                    break;
                case ETokenType.In:
                case ETokenType.Out:
                    {
                        AddKeyNodeInAngle(token);
                        // 清除标识符节点，避免紧跟 in/out 之后的 [ 被误当作索引访问 a[...]
                        // 例如 for a in [...] 中 in 之前刚解析过标识符 a，残留的 identifierNode 会使 [ 走索引分支
                        m_CurrentNode.SetIdentifierNode(null);
                    }
                    break;
                case ETokenType.At:             //@
                    {
                        AddAtOpSign(token);
                    }
                    break;
                case ETokenType.Dollar:
                    {
                        AddAtOpSign(token);
                    }
                    break;
                case ETokenType.Space:
                    {
                        m_TokenIndex++;
                    }
                    break;
                default:
                    {
                        Log.AddFileMetaLog(LID.ShowExtendMessage, string.Format("Path:{0} Line:{1} Source: {2}", token.path, token.sourceBeginLine,
                            token.sourceBeginChar));
                        throw new Exception("不支持的语法 ");
                    }
            }
        }
        public void WriteNodeString(bool isWriteFile)
        {
            try
            {
                if (m_RootNode == null) return;
                if (isWriteFile)
                {
                    if (!Common.ShouldExportDebugText("Node.txt")) return;
                    var outFile = Common.GetDebugCodeFilePath(m_FileMeta.path, "Node.txt");
                    string content = m_RootNode.ToFormatString();
                    File.WriteAllText(outFile, content);
                }
                else
                {
                    Console.Write($"---------------File:{m_FileMeta.path}  Token节点  开始:-------------------------");
                    Console.Write(m_RootNode.ToFormatString());
                    Console.Write($"---------------File:{m_FileMeta.path}  Token节点  结束:-------------------------");
                }
            }
            catch (Exception e)
            {
                Log.AddTokenLog(LID.ShowExtendMessage, "" + e.Message);
                // ignore debug dump errors
            }
        }

        /// <summary>
        /// Best-effort generic context detection: if there's an unclosed '<' in the current node list,
        /// prefer treating '>>' as two generic closing tokens instead of a shift operator.
        /// </summary>
        //private bool IsInsideGenericAngleContext()
        //{
        //    // Scan current node's direct children and compute a simple depth for angle brackets.
        //    // This intentionally ignores nested node stacks (Par/Brace/Bracket), because '>>' that
        //    // tokenizes inside those should still typically behave as shift.
        //    int depth = 0;
        //    var list = m_CurrentNode?.childList;
        //    if (list == null) return false;

        //    for (int i = 0; i < list.Count; i++)
        //    {
        //        var n = list[i];
        //        if (n == null) continue;
        //        if (n.nodeType == ENodeType.LeftAngle) depth++;
        //        else if (n.nodeType == ENodeType.RightAngle && depth > 0) depth--;
        //    }
        //    return depth > 0;
        //}
        //private void CheckUnclosedPairNodes()
        //{
        //    while (m_CurrentNodeStack.Count > 0)
        //    {
        //        var node = m_CurrentNodeStack.Pop();
        //        if (node == null || node.nodeType == ENodeType.Root)
        //        {
        //            continue;
        //        }

        //        Log.AddNodeLog(LID.MetaCoreAssertShowMessage, node.token, GetUnclosedPairMessage(node));
        //    }

        //    m_CurrentNodeStack.Push(m_RootNode);
        //    m_CurrentNode = m_RootNode;
        //}

        //private static string GetUnclosedPairMessage(Node node)
        //{
        //    return node.nodeType switch
        //    {
        //        ENodeType.Brace => "Error Node层花括号不对称，缺少右花括号 '}'",
        //        ENodeType.Par => "Error Node层圆括号不对称，缺少右圆括号 ')'",
        //        ENodeType.Bracket => "Error Node层中括号不对称，缺少右中括号 ']'",
        //        _ => "Error Node层括号不对称，缺少闭合符号",
        //    };
        //}

        //private bool TryPopPairNode(ENodeType expectedNodeType, Token endToken, string pairText)
        //{
        //    if (m_CurrentNodeStack.Count <= 1)
        //    {
        //        Log.AddNodeLog(LID.ShowExtendMessage, endToken, "Error Node层括号不对称，多余的右符号 " + pairText);
        //        m_TokenIndex++;
        //        return false;
        //    }

        //    var cnode = m_CurrentNodeStack.Pop();
        //    if (cnode != null && cnode.nodeType == expectedNodeType)
        //    {
        //        cnode.endToken = endToken;
        //        m_TokenIndex++;
        //        m_CurrentNode = cnode.parent ?? m_RootNode;
        //        return true;
        //    }

        //    Log.AddNodeLog(LID.ShowExtendMessage, endToken, "Error Node层括号不对称，期望闭合 " + expectedNodeType.ToString() + " 但遇到 " + pairText);
        //    if (cnode != null)
        //    {
        //        m_CurrentNodeStack.Push(cnode);
        //    }
        //    m_TokenIndex++;
        //    return false;
        //}
    }
}
