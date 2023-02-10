//****************************************************************************
//  File:      TokenParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using System;
using System.Collections.Generic;

namespace SimpleLanguage.Compile.Parse
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
        Node tempNode = null;

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
            ParseToken();

            BuildEnd();
        }
        public void BuildEnd()
        {
            Console.WriteLine(m_RootNode.ToFormatString());
            Console.WriteLine("----------------------------------------");
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

            if (tempNode?.linkToken != null)
            {
                Node node2 = new Node(tempNode.linkToken);
                node2.nodeType = ENodeType.Period;

                tempNode.AddLinkNode(node2);
                tempNode.AddLinkNode(node );
                if( tempNode.atToken != null )
                {
                    node.atToken = tempNode.atToken;
                    tempNode.atToken = null;
                }
                tempNode.linkToken = null;
            }
            else
            {
                currentNode.AddChild(node);
                tempNode = node;
                tempNode.lastNode = node;
            }
            //tempNode.lastNode = node;
            m_TokenIndex++;
        }        
        private void AddAnnotation(Token code)
        {
            m_TokenIndex++;
        }
        private Node AddNode(Token token, bool isEndAngleSign = true )
        {
            if( isEndAngleSign )
            {
                EndAngleSign();
            }
            Node node = new Node(token);
            currentNode.AddChild(node);
            m_TokenIndex++;
            return node;
        }
        private Node AddKeyNode(Token token, bool isEndAngleSign = true )
        {
            if (isEndAngleSign)
            {
                EndAngleSign();
            }
            Node node = new Node(token);
            node.nodeType = ENodeType.Key;
            currentNode.AddChild(node);
            m_TokenIndex++;
            return node;
        }
        private Node AddAtOpSign( Token token )
        {
            if (tempNode?.linkToken != null)
            {
                tempNode.atToken = token;
            }
            else
            {
                Console.WriteLine("现在@符必须使用.@方式!!");
            }
            m_TokenIndex++;

            return null;
        }
        private Node AddSymbol( Token token )
        {
            EndAngleSign();
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
        private void EndAngleSign()
        {
            Node cnode = null;
            if (currentNodeStack.Count > 0)
            {
                cnode = currentNodeStack.Peek();
                if (cnode.nodeType == ENodeType.Angle)
                {
                    currentNodeStack.Pop();
                    currentNode = cnode.parent;
                    cnode.nodeType = ENodeType.Symbol;
                    //currentNode.AddChild(cnode);
                    for( int i = 0; i < cnode.childList.Count; i++ )
                    {
                        currentNode.AddChild(cnode.childList[i]);
                    }
                    cnode.childList.Clear();

                }
            }
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
        /// <summary> 解析脚本 </summary>
        private void ParseToken()
        {
            while( true )
            {
                var tempToken = m_TokensList[m_TokenIndex];
                if (tempToken.type == ETokenType.Finished) { break; }
                ParseDetailToken(tempToken);
            }
            return;
        }
        /*
         * 
         * 解析  让token解析成  identifer {} [] () <> identifer identfier + - * 20 30.1 s.toString () 的结构
         */
        void ParseDetailToken( Token token )
        {
            if (currentNode == null)
            {
                Console.WriteLine("Error CurrentNode is NULL!!" + token?.ToLexemeAllString());
                return;
            }
            switch (token.type)
            {
                case ETokenType.Identifier:  //Identifier
                case ETokenType.Base:         //parent
                case ETokenType.This:           //this
                case ETokenType.Object:
                case ETokenType.Boolean:
                case ETokenType.Global:
                    {
                        AddIdentifier(token);
                    }
                    break;
                case ETokenType.Type:
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.IdentifierLink;
                        currentNode.AddChild(node); 
                        m_TokenIndex++;
                    }
                    break;
                case ETokenType.LeftBrace: //{
                    {
                        EndAngleSign();

                        Node node = new Node(token);
                        node.nodeType = ENodeType.Brace;
                        m_TokenIndex++;
                        currentNodeStack.Push(node);
                        
                        currentNode.AddChild(node);
                        currentNode = node;
                        tempNode = null;
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
                            Console.WriteLine("Error 不对称{}");
                        }
                    }
                    break;
                case ETokenType.Less:         // <
                    {
                        Node node = new Node(token);
                        node.nodeType = ENodeType.Angle;
                        m_TokenIndex++;
                        currentNodeStack.Push(node);

                        currentNode.AddChild(node);
                        currentNode = node;
                        tempNode = null;
                    }
                    break;
                case ETokenType.Greater:            // >
                    {
                        m_TokenIndex++;

                        bool isPair = false;
                        Node cnode = null;
                        if( currentNodeStack.Count > 0 )
                        {
                            cnode = currentNodeStack.Peek();
                            if (cnode.nodeType == ENodeType.Angle)
                            {
                                currentNodeStack.Pop();
                                cnode.endToken = token;
                                currentNode = cnode.parent;
                                isPair = true;
                            }
                        }

                        if( !isPair )
                        {
                            Node node = new Node(token);
                            node.nodeType = ENodeType.Symbol;
                            currentNode.AddChild(node);
                        }
                    }
                    break;
                case ETokenType.LeftPar: //(
                    {
                        EndAngleSign();

                        Node node = new Node(token);
                        node.nodeType = ENodeType.Par;
                        m_TokenIndex++;
                        currentNodeStack.Push(node);

                        currentNode.AddChild(node);
                        currentNode = node;
                        tempNode = null;
                    }
                    break;
                case ETokenType.RightPar: //)
                    {
                        var cnode = currentNodeStack.Pop();
                        if( cnode != null && cnode.nodeType == ENodeType.Par)
                        {
                            cnode.endToken = token;
                            m_TokenIndex++;
                            tempNode = cnode;
                            tempNode.lastNode = cnode;
                            currentNode = cnode.parent;
                        }
                        else
                        {
                            Console.WriteLine("Error 不对称()");
                        }
                    }
                    break;
                case ETokenType.LeftBracket://[
                    {
                        EndAngleSign();

                        Node node = new Node(token);
                        node.nodeType = ENodeType.Bracket;
                        m_TokenIndex++;
                        currentNodeStack.Push(node);

                        currentNode.AddChild(node);
                        currentNode = node;
                        tempNode = null;
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
                            tempNode = cnode;
                            tempNode.lastNode = cnode;
                        }
                        else
                        {
                            Console.WriteLine("Error 不对称[]");
                        }
                    }
                    break;
                case ETokenType.Period:  //.
                    {
                        tempNode.linkToken = token;
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
                case ETokenType.Colon:       //:
                    {
                        AddNode(token);
                    }
                    break;
                case ETokenType.ColonDouble:    //::
                    {
                        AddNode(token);
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
                case ETokenType.Shi:               //  <<
                    {
                        AddBitMoveOperatorSymbol(token);
                    }
                    break;
                case ETokenType.Shr:               //  >>
                    {
                        if (currentNodeStack.Count > 1)
                        {
                            var cnode1 = currentNodeStack.Pop();
                            var cnode2 = currentNodeStack.Pop();
                            if (cnode1.nodeType == ENodeType.Angle && cnode2.nodeType == ENodeType.Angle )
                            {
                                m_TokenIndex++;
                                Token token1 = new Token(token.path, ETokenType.Greater, ">", token.sourceBeginLine,
                                    token.sourceBeginChar);
                                Token token2 = new Token(token.path, ETokenType.Greater, ">", token.sourceBeginLine,
                                    token.sourceBeginChar + 1 );
                                cnode1.endToken = token1;
                                cnode2.endToken = token2;
                                currentNode = cnode2.parent;
                            }
                            else
                            {
                                currentNodeStack.Push(cnode2);
                                currentNodeStack.Push(cnode1);
                                AddBitMoveOperatorSymbol(token);
                            }
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
                case ETokenType.Import:
                    {
                        AddImportNode(token);
                    }
                    break;
                case ETokenType.As:
                    {
                        AddKeyNode(token);
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
                    {
                        AddNode(token);
                    }
                    break;
                case ETokenType.Public:
                case ETokenType.Internal:
                case ETokenType.Projected:
                case ETokenType.Private:
                    {
                        AddNode(token);
                    }
                    break;
                //case ETokenType.New:
                case ETokenType.Const:
                case ETokenType.Final:
                case ETokenType.Static:
                case ETokenType.Override:
                case ETokenType.Partial:
                case ETokenType.Void:
                case ETokenType.Get:
                case ETokenType.Set:
                    {
                        AddNode(token);
                    }
                    break;
                case ETokenType.Interface:
                    {
                        AddNode(token);
                    }
                    break;
                case ETokenType.Null:
                case ETokenType.Number:
                case ETokenType.String:
                case ETokenType.BoolValue:
                case ETokenType.NumberArrayLink:
                    {
                        EndAngleSign();
                        Node node = new Node(token);
                        node.nodeType = ENodeType.ConstValue;
                        if (tempNode?.linkToken != null)
                        {
                            Node node2 = new Node(tempNode.linkToken);
                            node2.nodeType = ENodeType.Period;

                            tempNode.AddLinkNode(node2);
                            tempNode.AddLinkNode(node);
                            tempNode.linkToken = null;
                            if( tempNode.atToken != null )
                            {
                                node.atToken = tempNode.atToken;
                                tempNode.atToken = null;
                            }
                        }
                        else
                        {
                            currentNode.AddChild(node);
                            tempNode = node;
                            tempNode.lastNode = node;
                        }
                        //tempNode.lastNode = node;

                        m_TokenIndex++;
                    }
                    break;
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
                    {
                        AddKeyNode(token);
                    }
                    break;
                case ETokenType.Else:
                case ETokenType.In:
                case ETokenType.Switch:
                case ETokenType.Continue:
                case ETokenType.Break:
                case ETokenType.Default:
                case ETokenType.Next:
                    {
                        AddKeyNode(token);
                    }
                    break;
                case ETokenType.At:
                    {
                        AddAtOpSign(token);
                    }
                    break;
                default:
                    {
                        Console.WriteLine("Line:{0} Source: {1}", token.sourceBeginLine, 
                            token.sourceBeginChar);
                        throw new Exception( "不支持的语法 " );
                    }
            }
        }
    }
}
