//****************************************************************************
//  File:      TokenParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace SimpleLanguage.Compile
{
    /// <summary> 解析token </summary>
    public class TokenParse
    {
        public Node rootNode => m_RootNode;

        private FileMeta m_FileMeta;
        private List<Token> m_TokensList;
        private int m_TokenIndex = 0;
        private int m_TokenCount = 0;

        Node m_RootNode = new Node(null);
        Stack<Node> currentNodeStack = new Stack<Node>();
        Node currentNode = null;

        public TokenParse(FileMeta fm, List<Token> list)
        {
            m_FileMeta = fm;
            m_TokensList = list;
            m_TokenCount = list.Count;
            m_RootNode.nodeType = ENodeType.Root;
            currentNode = m_RootNode;
            currentNodeStack.Push(m_RootNode);
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
        public void AddImportNode( Token token )
        {
            var nnode = new Node(token);
            nnode.nodeType = ENodeType.Key;
            m_TokenIndex++;
            currentNode.AddChild(nnode);
        }
        public void AddNamespaceNode(Token token)
        {
            var nnode = new Node(token);
            nnode.nodeType = ENodeType.Key;
            m_TokenIndex++;
            currentNode.AddChild(nnode);
        }
        public void AddIdentifier(Token code)      //Print/Function
        {
            Node node = new Node(code);
            node.nodeType = ENodeType.IdentifierLink;

            if ( currentNode.linkToken != null)
            {
                Node node2 = new Node(currentNode.linkToken);
                node2.nodeType = ENodeType.Period;

                currentNode.AddLinkNode(node2);
                currentNode.AddLinkNode(node );
                if(currentNode.atToken != null )
                {
                    node.atToken = currentNode.atToken;
                    currentNode.atToken = null;
                }
                currentNode.linkToken = null;


            }
            else
            {
                currentNode.AddChild(node);
            }
            //tempNode.lastNode = node;
            m_TokenIndex++;
        }        
        private void AddAnnotation(Token code)
        {
            m_TokenIndex++;

            Node node = new Node(code);
            node.nodeType = ENodeType.Comment;
            currentNode.AddChild(node);
        }
        private Node AddKeyNode(Token token )
        {
            Node node = new Node(token);
            node.nodeType = ENodeType.Key;
            currentNode.AddChild(node);
            m_TokenIndex++;
            return node;
        }
        private Node AddAtOpSign( Token token )
        {
            if (currentNode.linkToken != null)
            {
                if (token.type == ETokenType.At)
                {
                    // `.@` is no longer supported; reserve '@' for attribute syntax.
                    Debug.Assert(false, "不再支持 a.@b 语法，请使用 a.$b / a.$0 形式");
                    currentNode.linkToken = null;
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

                
                Node node2 = new Node(currentNode.linkToken);
                node2.nodeType = ENodeType.Period;

                currentNode.AddLinkNode(node2);
                currentNode.AddLinkNode(node);
                node.atToken = token;

                currentNode.linkToken = null;
                //tempNode.lastNode = node;

            }
            else
            {
                Debug.Assert(false, "现在$符必须使用.$方式!!");
            }
            m_TokenIndex++;

            return null;
        }
        private Node AddSymbol( Token token )
        {
            Node node = new Node(token);
            node.nodeType = ENodeType.Symbol;
            currentNode.AddChild(node);
            m_TokenIndex++;
            var cnode = currentNodeStack.Peek();
            return node;
        }
        private void AddPlusMinus(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level2_LinkOp;
        }
        private void AddAsOrIs(Token code)
        {
            // as 有两个作用一个是import 里边 代名   一个是 as 类
            var node = AddKeyNode(code);
            //var node = AddSymbol(code);
            node.priority = SignComputePriority.Level9_AsOsIs;
        }
        private void AddDoublePlusMinus(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level2_LinkOp;
        }
        private void AddLeftToRightEqualSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level11_Assign;
        }
        private void AddBitMoveOperatorSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level5_BitMoveOp;
        }
        private void AddDXCompareSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level6_Compare;
        }
        private void AddCompareNotOrEqualSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level7_EqualAb;
        }
        private void AddBitAndSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level8_BitAndOp;
        }
        private void AddBitXOrOpSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level8_BitXOrOp;
        }
        private void AddBitOrOpSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level8_BitOrOp;
        }
        private void AddHightComputeSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level3_Hight_Compute;
        }
        private void AddSingleSign(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level2_LinkOp;
        }
        private void AddAndCompareSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level9_And;
        }
        private void AddOrCompareSymbol(Token code)
        {
            var node = AddSymbol(code);
            node.priority = SignComputePriority.Level9_Or;
        }
        /*
         * 
         * 解析  让token解析成  identifer {} [] () <> identifer identfier + - * 20 30.1 s.toString () 的结构
         */
        void ParseDetailToken( Token token )
        {
            if (currentNode == null)
            {
                Debug.Assert( false, "Error CurrentNode is NULL!!" + token?.ToLexemeAllString());
                return;
            }
            switch (token.type)
            {
                case ETokenType.Identifier:  //Identifier
                    {
                        AddIdentifier(token);
                    }
                    break;
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
                        currentNodeStack.Push(node);
                        
                        currentNode.AddChild(node);
                        currentNode = node;
                    }
                    break;
                case ETokenType.RightBrace: //}
                    {
                        var cnode = currentNodeStack.Pop();
                        

                        if (cnode.nodeType == ENodeType.Brace )
                        {
                            cnode.endToken = token;
                            m_TokenIndex++;
                            currentNode = cnode.parent;
                        }
                        else
                        {
                            Debug.Write("Error 不对称{}");
                        }
                    }
                    break;
                case ETokenType.Less:         // <
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.LeftAngle;
                        m_TokenIndex++;
                        currentNode.AddChild(node);
                    }
                    break;
                case ETokenType.Greater:            // >
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.RightAngle;
                        currentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.LeftPar: //(
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Par;
                        m_TokenIndex++;
                        currentNodeStack.Push(node);

                        currentNode.AddChild(node);
                        currentNode = node;
                    }
                    break;
                case ETokenType.RightPar: //)
                    {
                        var cnode = currentNodeStack.Pop();
                        if( cnode != null && cnode.nodeType == ENodeType.Par)
                        {
                            cnode.endToken = token;
                            m_TokenIndex++;
                            currentNode = cnode.parent;
                            //currentNode.SetLastNode( currentNode );
                        }
                        else
                        {
                            Debug.Assert(false, "Error 不对称()");
                        }
                    }
                    break;
                case ETokenType.LeftBracket://[
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Bracket;
                        m_TokenIndex++;
                        currentNodeStack.Push(node);

                        currentNode.AddChild(node);
                        currentNode = node;
                    }
                    break;
                case ETokenType.RightBracket://]
                    {
                        var cnode = currentNodeStack.Pop();
                        if (cnode != null && cnode.nodeType == ENodeType.Bracket)
                        {
                            cnode.endToken = token;
                            m_TokenIndex++;
                            currentNode = cnode.parent;
                        }
                        else
                        {
                            Debug.Assert( false, "Error 不对称[]");
                        }
                    }
                    break;
                case ETokenType.Period:  //.
                    {
                        currentNode.linkToken = token;
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.QuestionMarkDot: // ?.
                    {
                        // treat null-conditional operator like a linking token (similar to '.')
                        currentNode.linkToken = token;
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Comma:   //,
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Comma;
                        currentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.QuestionMark: //?
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.QuestionMark;
                        currentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Colon:       //:
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Colon;
                        currentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.SemiColon:      //;
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.SemiColon;
                        currentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.LineEnd:      // \n
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.LineEnd;
                        currentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Assign:             //=
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Assign;
                        currentNode.AddChild(node);
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.Plus:            //+
                case ETokenType.Minus:           //-
                    {
                        AddPlusMinus(token);
                    }
                    break;
                case ETokenType.As:
                case ETokenType.Is:
                case ETokenType.IsNot:
                    {   
                        AddAsOrIs(token);
                    }
                    break;
                case ETokenType.DoublePlus:     //++
                case ETokenType.DoubleMinus:    //--
                    {
                        AddDoublePlusMinus(token);
                    }
                    break;
                case ETokenType.Multiply:        // *
                case ETokenType.Divide:          // /
                case ETokenType.Modulo:          // %
                    {
                        AddHightComputeSymbol(token);
                    }
                    break;
                case ETokenType.Not:             // !
                case ETokenType.Negative:        // ~
                    {
                        AddSingleSign(token);
                    }
                    break;
                //case ETokenType.Shi:               //  <<
                //    {
                //        AddBitMoveOperatorSymbol(token);
                //    }
                //    break;
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
                //            currentNode.AddChild(n1);

                //            var t2 = new Token(token);
                //            t2.SetType(ETokenType.Greater);
                //            t2.SetLexeme(">");
                //            var n2 = new Node(t2) { nodeType = ENodeType.RightAngle };
                //            currentNode.AddChild(n2);

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
                        AddDXCompareSymbol(token);
                    }
                    break;
                case ETokenType.Equal:           // ==
                case ETokenType.NotEqual:        // !=
                    {
                        AddCompareNotOrEqualSymbol(token);
                    }
                    break;
                case ETokenType.Combine:                // &
                    {
                        AddBitAndSymbol(token);
                    }
                    break;
                case ETokenType.InclusiveOr:            // |
                    {
                        AddBitOrOpSymbol(token);
                    }
                    break;
                case ETokenType.XOR:                    //  ^
                    {
                        AddBitXOrOpSymbol(token);
                    }
                    break;
                case ETokenType.Or:              // ||
                    {
                        AddOrCompareSymbol(token);
                    }
                    break;
                case ETokenType.And:             // &&  
                    {
                        AddAndCompareSymbol(token);
                    }
                    break;
                case ETokenType.PlusAssign:             // +=
                case ETokenType.MinusAssign:            // -=
                case ETokenType.MultiplyAssign:         // *=
                case ETokenType.DivideAssign:           // /=
                case ETokenType.ModuloAssign:           // %=
                case ETokenType.InclusiveOrAssign:      // |=
                case ETokenType.XORAssign:              // ^=                
                    {
                        AddLeftToRightEqualSymbol(token);
                    }
                    break;
                case ETokenType.Sharp:   //#
                    {
                        AddAnnotation(token);
                    }
                    break;
                case ETokenType.Number:
                case ETokenType.String:
                case ETokenType.BoolValue:
                case ETokenType.NumberArrayLink:
                case ETokenType.Null:
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.ConstValue;
                        if ( currentNode.linkToken != null)
                        {
                            Node node2 = new Node(currentNode.linkToken);
                            node2.nodeType = ENodeType.Period;

                            currentNode.AddLinkNode(node2);
                            currentNode.AddLinkNode(node);
                            currentNode.linkToken = null;
                            if(currentNode.atToken != null )
                            {
                                node.atToken = currentNode.atToken;
                                currentNode.atToken = null;
                            }
                        }
                        else
                        {
                            currentNode.AddChild(node);
                        }
                        //tempNode.lastNode = node;

                        m_TokenIndex++;
                    }
                    break;

                case ETokenType.Import:
                    {
                        AddImportNode(token);
                    }
                    break;
                case ETokenType.Namespace:
                    {
                        AddNamespaceNode(token);
                    }
                    break;
                case ETokenType.Enum:
                case ETokenType.Data:
                case ETokenType.Class:
                case ETokenType.Dynamic:
                    {
                        AddKeyNode(token);
                    }
                    break;
                case ETokenType.Extern:
                case ETokenType.Public:
                case ETokenType.Projected:
                case ETokenType.Private:
                //case ETokenType.Operator:
                    {
                        AddKeyNode(token);
                    }
                    break;
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
                        Debug.Assert( false, string.Format("Line:{0} Source: {1}", token.sourceBeginLine, 
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
            var list = currentNode?.childList;
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
                Debug.Assert(false, "" + e.Message);
                // ignore debug dump errors
            }
        }

    }
}
