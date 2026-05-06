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
        private Stack<Node> m_CurrentNodeStack = new Stack<Node>();
        private Node m_CurrentNode = null;

        public TokenParse(FileMeta fm, List<Token> list)
        {
            m_FileMeta = fm;
            m_TokensList = list;
            m_TokenCount = list.Count;
            m_RootNode.nodeType = ENodeType.Root;
            m_CurrentNode = m_RootNode;
            m_CurrentNodeStack.Push(m_RootNode);
        }
        public void BuildStruct()
        {
            while (true)
            {
                var tempToken = m_TokensList[m_TokenIndex];
                if (tempToken.type == ETokenType.Finished) { break; }
                ParseDetailToken(tempToken);
                if (m_TokenIndex >= m_TokenCount)
                {
                    break;
                }
            }
            return;
        }
        public void AddIdentifier(Token code)      //Print/Function
        {
            Node node = new Node(code);
            node.nodeType = ENodeType.IdentifierLink;

            if ( m_CurrentNode.linkToken != null)
            {
                Node node2 = new Node(m_CurrentNode.linkToken);
                node2.nodeType = ENodeType.Period;

                m_CurrentNode.AddLinkNode(node2);
                m_CurrentNode.AddLinkNode(node );
                if(m_CurrentNode.atToken != null )
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
            //tempNode.lastNode = node;
            m_TokenIndex++;
        }   
        private Node AddKeyNode(Token token )
        {
            Node node = new Node(token);
            node.nodeType = ENodeType.Key;
            m_CurrentNode.AddChild(node);
            m_TokenIndex++;
            return node;
        }
        private Node AddAtOpSign( Token token )
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

                Node node = new Node( ntoken );
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
        private Node AddSymbol( Token token )
        {
            Node node = new Node(token);
            node.nodeType = ENodeType.Symbol;
            m_CurrentNode.AddChild(node);
            m_TokenIndex++;
            var cnode = m_CurrentNodeStack.Peek();
            return node;
        }
        private void AddBitMoveOperatorSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level5_BitMoveOp;
        }
        /*
         * 
         * 解析  让token解析成  identifer {} [] () <> identifer identfier + - * 20 30.1 s.toString () 的结构
         */
        void ParseDetailToken( Token token )
        {
            if (m_CurrentNode == null)
            {
                Log.AddTokenLog(LID.ShowExtendMessage, "Error CurrentNode is NULL!!" + token?.ToLexemeAllString());
                return;
            }
            switch (token.type)
            {
                case ETokenType.Identifier:  //Identifier
                case ETokenType.Type:
                    {
                        AddIdentifier(token);
                    }
                    break;
                case ETokenType.LeftBrace: //{
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Brace;
                        m_TokenIndex++;
                        m_CurrentNodeStack.Push(node);
                        
                        m_CurrentNode.AddChild(node);
                        m_CurrentNode = node;
                    }
                    break;
                case ETokenType.RightBrace: //}
                    {
                        var cnode = m_CurrentNodeStack.Pop();
                        

                        if (cnode.nodeType == ENodeType.Brace )
                        {
                            cnode.endToken = token;
                            m_TokenIndex++;
                            m_CurrentNode = cnode.parent;
                        }
                        else
                        {
                            Log.AddTokenLog(LID.MetaCoreAssertShowMessage, "Error 不对称{}");
                        }
                    }
                    break;
                case ETokenType.Less:         // <
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.LeftAngle;
                        m_TokenIndex++;
                        m_CurrentNode.AddChild(node);
                    }
                    break;
                case ETokenType.Greater:            // >
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.RightAngle;
                        m_CurrentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.LeftPar: //(
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Par;
                        m_TokenIndex++;
                        m_CurrentNodeStack.Push(node);

                        m_CurrentNode.AddChild(node);
                        m_CurrentNode = node;
                    }
                    break;
                case ETokenType.RightPar: //)
                    {
                        var cnode = m_CurrentNodeStack.Pop();
                        if( cnode != null && cnode.nodeType == ENodeType.Par)
                        {
                            cnode.endToken = token;
                            m_TokenIndex++;
                            m_CurrentNode = cnode.parent;
                            //currentNode.SetLastNode( currentNode );
                        }
                        else
                        {
                            Log.AddTokenLog(LID.ShowExtendMessage, "Error 不对称()");
                        }
                    }
                    break;
                case ETokenType.LeftBracket://[
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Bracket;
                        m_TokenIndex++;
                        m_CurrentNodeStack.Push(node);

                        m_CurrentNode.AddChild(node);
                        m_CurrentNode = node;
                    }
                    break;
                case ETokenType.RightBracket://]
                    {
                        var cnode = m_CurrentNodeStack.Pop();
                        if (cnode != null && cnode.nodeType == ENodeType.Bracket)
                        {
                            cnode.endToken = token;
                            m_TokenIndex++;
                            m_CurrentNode = cnode.parent;
                        }
                        else
                        {
                            Log.AddTokenLog(LID.ShowExtendMessage, "Error 不对称[]");
                        }
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
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.QuestionMark: //?
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.QuestionMark;
                        m_CurrentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Colon:       //:
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Colon;
                        m_CurrentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.SemiColon:      //;
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.SemiColon;
                        m_CurrentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.LineEnd:      // \n
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.LineEnd;
                        m_CurrentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Assign:             //=
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Assign;
                        m_CurrentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Plus:            //+
                case ETokenType.Minus:           //-
                    {
                        var node = AddSymbol(token);
                        node.priority = SignComputePriority.Level2_LinkOp;
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
                case ETokenType.Shr:               //  >>
                    {
                        // In nested generics like Map<List<int>,string>> the lexer produces Shr.
                        // If we're currently inside an unclosed generic angle sequence, treat this
                        // as two closing '>' tokens.
                        if (IsInsideGenericAngleContext())
                        {
                            var t1 = new Token(token);
                            t1.SetType(ETokenType.Greater);
                            t1.SetLexeme(">");
                            var n1 = new Node(t1) { nodeType = ENodeType.RightAngle };
                            m_CurrentNode.AddChild(n1);

                            var t2 = new Token(token);
                            t2.SetType(ETokenType.Greater);
                            t2.SetLexeme(">");
                            var n2 = new Node(t2) { nodeType = ENodeType.RightAngle };
                            m_CurrentNode.AddChild(n2);

                            m_TokenIndex++;
                        }
                        else
                        {
                            AddBitMoveOperatorSymbol(token);
                        }
                    }
                    break;
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
                case ETokenType.Number:
                case ETokenType.NumberReal:
                case ETokenType.String:
                case ETokenType.BoolValue:
                case ETokenType.NumberArrayLink:
                case ETokenType.Null:
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.ConstValue;
                        if ( m_CurrentNode.linkToken != null)
                        {
                            Node node2 = new Node(m_CurrentNode.linkToken);
                            node2.nodeType = ENodeType.Period;

                            m_CurrentNode.AddLinkNode(node2);
                            m_CurrentNode.AddLinkNode(node);
                            m_CurrentNode.linkToken = null;
                            if(m_CurrentNode.atToken != null )
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
                case ETokenType.Object:
                case ETokenType.Boolean:
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
                case ETokenType.In:
                case ETokenType.Switch:
                case ETokenType.Continue:
                case ETokenType.Break:
                case ETokenType.Default:
                case ETokenType.Var:
                case ETokenType.Next:
                case ETokenType.Params:
                case ETokenType.New:
                    {
                        AddKeyNode(token);
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
                            token.sourceBeginChar) );
                        throw new Exception( "不支持的语法 " );
                    }
            }
        }
        /// <summary>
        /// Best-effort generic context detection: if there's an unclosed '<' in the current node list,
        /// prefer treating '>>' as two generic closing tokens instead of a shift operator.
        /// </summary>
        private bool IsInsideGenericAngleContext()
        {
            // Scan current node's direct children and compute a simple depth for angle brackets.
            // This intentionally ignores nested node stacks (Par/Brace/Bracket), because '>>' that
            // tokenizes inside those should still typically behave as shift.
            int depth = 0;
            var list = m_CurrentNode?.childList;
            if (list == null) return false;

            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                if (n == null) continue;
                if (n.nodeType == ENodeType.LeftAngle) depth++;
                else if (n.nodeType == ENodeType.RightAngle && depth > 0) depth--;
            }
            return depth > 0;
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

    }
}
