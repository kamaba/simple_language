//****************************************************************************
//  File:      Node.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.Parse
{
    public class SignComputePriority
    {
        public const int Level1 = 1;                         //(a+b) [] . 优先操作，对象操作等
        public const int Level2_LinkOp = 2;                        // -负号 (int)强转 ++x x++ -- ! ~ 
        public const int Level3_Hight_Compute = 3;                 // / * % 
        public const int Level3_Low_Compute = 4;                   // + - 
        public const int Level5_BitMoveOp = 5;              // << >> 
        public const int Level6_Compare = 6;                //< > <= >=
        public const int Level7_EqualAb = 7;                // == !=
        public const int Level8_BitAndOp = 81;           // &
        public const int Level8_BitXOrOp = 82;            // ^
        public const int Level8_BitOrOp = 83;               // |
        public const int Level9_And = 91;                   // &&
        public const int Level9_Or = 92;                    // ||
        public const int Level10_ThirdOp = 100;             // ? : 
        public const int Level11_Assign = 120;              // = /= *= %= += -= <<= >>= &= ^= |= 
        public const int Level12_Split = 130;                //,
    }
    public enum ENodeType
    {
        Null,
        Root,
        Brace,
        Angle,
        Par,
        Bracket,
        Symbol,
        Period,
        Comma,
        SemiColon,
        LineEnd,
        Assign,
        ConstValue,
        IdentifierLink,
        Key,
        End,
    }
        
    public class Node
    {
        /*
         *   class{ static printf(){} a(){ return a } int b; } printf() a() 和b 就是子内容节点
         */
        public int priority { get; set; } = -1;

        public Token token = null;              // 
        public Token endToken = null;           // )}]
        public Node parent = null;              //父节点
        public Node parNode { get; set; } = null;             //(小括号的节点
        public Node blockNode { get; set; } = null;           //{大括号的节点
        public Node bracketNode { get; set; } = null;         //[中括号的节点
        public Node angleNode { get; set; } = null;           //<尖括号的节点
        public Token linkToken;                 //.节点
        public Token atToken;                   // @节点
        public Node lastNode = null;            // 最后处理的节点

        public ENodeType nodeType = ENodeType.Null;
        private List<Node> m_ExtendLinkNodeList = new List<Node>();
        public List<Node> childList = new List<Node>();    //子内容节点

        public int parseIndex = 0;

        public Node parseCurrent
        {
            get
            {
                if (parseIndex >= childList.Count)
                    return null;
                return childList[parseIndex];
            }
        }
        public Node finalNode
        {
            get
            {
                if ( m_ExtendLinkNodeList.Count > 0)
                {
                    Node t = m_ExtendLinkNodeList[m_ExtendLinkNodeList.Count - 1];
                    return t;
                }
                return this;
            }
        }
        public List<Node> extendLinkNodeList => m_ExtendLinkNodeList;
        public List<Node> GetLinkNodeList(bool isIncludeSelf = true)
        {
            List<Node> tlist = new List<Node>();
            if (isIncludeSelf) tlist.Add(this);
            tlist.AddRange(m_ExtendLinkNodeList);
            return tlist;
        }
        public List<Token> linkTokenList
        {
            get
            {
                List<Token> tlist = new List<Token>();
                tlist.Add(this.token);
                for( int i = 0; i < m_ExtendLinkNodeList.Count; i++ )
                {
                    tlist.Add(m_ExtendLinkNodeList[i].token);
                }
                return tlist;
            }
        }
        public Node(Token _token)
        {
            token = _token;
        }
        public Node GetParseNode(int index = 1, bool isAddIndex = true )
        {
            if (parseIndex >= childList.Count)
                return null;
            var node = childList[parseIndex];
            if( isAddIndex )
            {
                parseIndex += index;
            }
            return node;
        }
        public void AddLinkNode(Node node )
        {
            if (lastNode == null) return;

            lastNode.m_ExtendLinkNodeList.Add(node);
        }
        public void SetLinkNode( List<Node> nodeList )
        {
            m_ExtendLinkNodeList = nodeList;
        }
        public void SetParList( List<Node> nodes )
        {
            if( nodes.Count == 1 )
            {
                if( nodes[0].nodeType == ENodeType.Par )
                {
                    parNode = nodes[0];
                    return;
                }
            }
            if (parNode == null)
                parNode = new Node(null);
            parNode.childList = nodes;
        }
        public void SetPar( Node node )
        {
            parNode = node;
        }
        public void AddChild(Node c, bool setParent = true)
        {
            if (setParent)
                c.parent = this;
            this.childList.Add(c);
            lastNode = c;
        }
        public void AddSyntax( Node c )
        {
            this.childList.Add(c);
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if( nodeType == ENodeType.IdentifierLink )
            {
                sb.Append(this.token?.lexeme.ToString());
                for( int i = 0; i < m_ExtendLinkNodeList.Count; i++ )
                {
                    var tnode = m_ExtendLinkNodeList[i];
                    sb.Append(m_ExtendLinkNodeList[i].token?.lexeme.ToString());
                    if ( tnode.parNode != null )
                    {
                        sb.Append(tnode.parNode.ToFormatString());
                    }
                }
                if (this.parNode != null)
                {
                    sb.Append(this.parNode.ToFormatString());
                }
                if (this.blockNode != null)
                {
                    sb.Append(" " + this.blockNode.ToFormatString());
                }
            }
            else if( nodeType == ENodeType.ConstValue )
            {
                if( this.token.type == ETokenType.String )
                {
                    sb.Append("\"" + this.token?.lexeme.ToString() + "\"" );
                }
                else
                {
                    sb.Append(this.token?.lexeme.ToString());
                }
                for (int i = 0; i < m_ExtendLinkNodeList.Count; i++)
                {
                    var tnode = m_ExtendLinkNodeList[i];
                    sb.Append(m_ExtendLinkNodeList[i].token?.lexeme.ToString());
                    if (tnode.parNode != null)
                    {
                        sb.Append(tnode.parNode.ToFormatString());
                    }
                }
            }
            else if( nodeType == ENodeType.Brace )
            {
                sb.Append(token?.lexeme.ToString());
                if (this.parNode != null)
                {
                    sb.Append(this.parNode.ToFormatString());
                }
                for (int i = 0; i < childList.Count; i++)
                {
                    sb.Append(childList[i].ToFormatString() + " ");
                }
                sb.Append(endToken?.lexeme.ToString());
            }
            else if( nodeType == ENodeType.Bracket )
            {
                sb.Append(token?.lexeme.ToString() + " ");
                for (int i = 0; i < childList.Count; i++)
                {
                    sb.Append(childList[i].ToFormatString() + " ");
                }
                sb.Append(endToken?.lexeme.ToString());
                for (int i = 0; i < m_ExtendLinkNodeList.Count; i++)
                {
                    var tnode = m_ExtendLinkNodeList[i];
                    sb.Append(m_ExtendLinkNodeList[i].token?.lexeme.ToString());
                    if (tnode.parNode != null)
                    {
                        sb.Append(tnode.parNode.ToFormatString());
                    }
                }
            }
            else if (nodeType == ENodeType.Par)
            {
                sb.Append(token?.lexeme.ToString() + " ");
                for (int i = 0; i < childList.Count; i++)
                {
                    sb.Append(childList[i].ToFormatString());
                }
                sb.Append(endToken?.lexeme.ToString());
                for (int i = 0; i < m_ExtendLinkNodeList.Count; i++)
                {
                    var tnode = m_ExtendLinkNodeList[i];
                    sb.Append(m_ExtendLinkNodeList[i].token?.lexeme.ToString());
                    if (tnode.parNode != null)
                    {
                        sb.Append(tnode.parNode.ToFormatString());
                    }
                }
            }
            else if (nodeType == ENodeType.Angle )
            {
                sb.Append(token?.lexeme.ToString() + " ");
                for (int i = 0; i < childList.Count; i++)
                {
                    sb.Append(childList[i].ToFormatString());
                }
                sb.Append(endToken?.lexeme.ToString());
            }
            else if( nodeType == ENodeType.Key )
            {
                sb.Append( this.token?.lexeme.ToString() );
                if( this.blockNode != null )
                {
                    sb.Append( " " + this.blockNode.ToFormatString());
                }
                for (int i = 0; i < childList.Count; i++)
                {
                    sb.Append(childList[i].ToFormatString());
                }
            }
            else if( nodeType == ENodeType.LineEnd )
            {
                //sb.Append(" ");
            }
            else
            {
                sb.Append(this.token?.lexeme.ToString() + " ");
                for (int i = 0; i < childList.Count; i++)
                {
                    sb.Append(childList[i].ToFormatString());
                }

            }
            sb.Append(" ");

            return sb.ToString();
        }       
    }
}
