//****************************************************************************
//  File:      FileMetaExpress.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class FileMetaBaseTerm : FileMetaBase
    {
        public bool isDirty { get; set; } = false;
        public int priority { get; set; } = int.MaxValue;
        public bool isOnlyOne
        {
            get
            {
                return left == null && right == null;
            }
        }
        public List<FileMetaBaseTerm> fileMetaExpressList => m_FileMetaExpressList;
        public FileMetaBaseTerm left
        {
            get { return m_Left; }
            set
            {
                m_Left = value;
                isDirty = true;
            }
        }
        public FileMetaBaseTerm right
        {
            get { return m_Right; }
            set
            {
                m_Right = value;
                isDirty = true;
            }
        }
        public virtual FileMetaBaseTerm root
        {
            get
            {
                return m_Root;
            }
        }

        protected List<FileMetaBaseTerm> m_FileMetaExpressList = new List<FileMetaBaseTerm>();
        protected FileMetaBaseTerm m_Left = null;
        protected FileMetaBaseTerm m_Right = null;
        protected FileMetaBaseTerm m_Root = null;

        public List<FileMetaBaseTerm> SplitParamList()
        {
            List<FileMetaBaseTerm> ParamFileMetaTermList = new List<FileMetaBaseTerm>();

            List<List<FileMetaBaseTerm>> fmbtListList = new List<List<FileMetaBaseTerm>>();
            List<FileMetaBaseTerm> fmbtList = new List<FileMetaBaseTerm>();

            bool isComma = false;
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fmen = m_FileMetaExpressList[i];
                var fmst = fmen as FileMetaSymbolTerm;
                if (fmst != null && fmst.token.type == ETokenType.Comma)
                {
                    if (isComma)
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error 多重逗号，导致解析无法解析!!");
                        break;
                    }
                    if (fmbtList.Count == 0)
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error 首符号不能为逗号");
                        break;
                    }
                    isComma = true;
                    fmbtListList.Add(fmbtList);
                    fmbtList = new List<FileMetaBaseTerm>();
                }
                else
                {
                    isComma = false;
                    fmbtList.Add(fmen);
                }
            }
            if (fmbtList.Count == 0)
            {
                return ParamFileMetaTermList;
            }
            fmbtListList.Add(fmbtList);

            for (int i = 0; i < fmbtListList.Count; i++)
            {
                var fmbt2 = fmbtListList[i];

                if (fmbt2.Count == 1)
                {
                    ParamFileMetaTermList.Add(fmbt2[0]);
                }
                else
                {
                    //FileMetaTermExpress fmte = new FileMetaTermExpress(fileMeta);
                    //m_ParamFileMetaTermList.Add(fmte);
                    //fmte.AddRangeFileMetaTerm(fmbt2);
                    ParamFileMetaTermList.AddRange(fmbt2);
                }
            }
            return ParamFileMetaTermList;
        }
        public virtual void ClearDirty()
        {
            isDirty = false;
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fme = m_FileMetaExpressList[i];
                fme.ClearDirty();
            }
        }
        public virtual void AddFileMetaTerm(FileMetaBaseTerm fmn)
        {
            fmn.SetFileMeta(m_FileMeta);
            m_FileMetaExpressList.Add(fmn);
        }
        public virtual void AddRangeFileMetaTerm(List<FileMetaBaseTerm> fmn)
        {
            for( int i = 0; i < fmn.Count; i++ )
            {
                fmn[i].SetFileMeta(m_FileMeta);
            }
            m_FileMetaExpressList.AddRange(fmn);
        }
        public virtual List<Token> GetTokens()
        {
            List<Token> tokens = new List<Token>() { m_Token };

            return tokens;
        }
        public virtual bool BuildAST()
        {
            return true;
        }
        public override string ToFormatString()
        {
            return token.lexeme.ToString();
        }
        public virtual string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_Token != null)
                sb.Append(m_Token.ToLexemeAllString());

            if( left != null )
            {
            }
            if( m_Right != null )
            {
            }
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fme = m_FileMetaExpressList[i];
                sb.Append(" " + fme.ToTokenString());
            }
            return sb.ToString();
        }
    }
    // + - * / >> << >= == 
    public class FileMetaSymbolTerm : FileMetaBaseTerm
    {
        public ETokenType symBolType
        {
            get
            {
                if( m_Token != null )
                {
                    return m_Token.type;
                }
                return ETokenType.None;
            }
        }
        public FileMetaSymbolTerm( FileMeta fm, Token _token)
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_Root = this;
            SetPriory();
        }
        private void SetPriory()
        {
            switch( m_Token.type )
            {

                case ETokenType.Plus:
                case ETokenType.Minus:
                    {
                        // keep plus/minus as link-op (unary prefix) priority so unary - is recognized
                        priority = SignComputePriority.Level2_LinkOp;
                    }
                    break;                   
                case ETokenType.Multiply:
                case ETokenType.Divide:
                    {
                        // * / are high-precedence binary ops
                        priority = SignComputePriority.Level3_Hight_Compute;
                    }
                    break;
                case ETokenType.DoublePlus:     //++
                case ETokenType.DoubleMinus:    //--
                    {
                        priority = SignComputePriority.Level2_LinkOp;
                    }
                    break;
                case ETokenType.Modulo:          // %
                case ETokenType.Not:             // !
                case ETokenType.Negative:        // ~
                    {
                        priority = SignComputePriority.Level3_Hight_Compute;
                    }
                    break;
                case ETokenType.Shi:               //  <<
                case ETokenType.Shr:               //  >>
                    {
                        priority = SignComputePriority.Level5_BitMoveOp;
                    }
                    break;
                case ETokenType.Less:            // >
                case ETokenType.GreaterOrEqual:  // >=
                case ETokenType.Greater:         // <
                case ETokenType.LessOrEqual:     // <=
                    {
                        priority = SignComputePriority.Level6_Compare;
                    }
                    break;
                case ETokenType.Equal:           // ==
                case ETokenType.NotEqual:        // !=
                    {
                        priority = SignComputePriority.Level7_EqualAb;
                    }
                    break;
                case ETokenType.Combine:         // &
                    {
                        priority = SignComputePriority.Level8_BitAndOp;
                    }
                    break;
                case ETokenType.InclusiveOr:     // |
                    {
                        priority = SignComputePriority.Level8_BitOrOp;
                    }
                    break;
                case ETokenType.XOR:             //  ^
                    {
                        priority = SignComputePriority.Level8_BitXOrOp;
                    }
                    break;
                case ETokenType.Or:              // ||
                    {
                         priority = SignComputePriority.Level9_Or;
                    }
                    break;
                case ETokenType.And:             // &&  
                    {
                        priority = SignComputePriority.Level9_And;
                    }
                    break;
                case ETokenType.PlusAssign:             // +=
                case ETokenType.MinusAssign:            // -=
                case ETokenType.MultiplyAssign:         // *=
                case ETokenType.DivideAssign:           // /=
                case ETokenType.ModuloAssign:           // %=
                case ETokenType.InclusiveOrAssign:      // |=
                case ETokenType.XORAssign:              // ^=
                //case ETokenType.ShiAssign:              // <<=
                //case ETokenType.ShrAssign:              // >>=
                    {
                        priority = SignComputePriority.Level3_Hight_Compute;
                    }
                    break;
                case ETokenType.Comma:                  // ,
                    {
                        priority = SignComputePriority.Level12_Split;
                    }
                    break;
                case ETokenType.Colon:                  // :
                    {
                        priority = SignComputePriority.Level12_Split;
                    }
                    break;
            }
            // leave priority as set by switch; do not override with a blanket default
        }
        public override bool BuildAST()
        {
            return true;
        }
        public override string ToFormatString()
        {
            return token.lexeme.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(token?.lexeme.ToString());

            return sb.ToString();
        }
    }
    public class FileMetaAsOrIsTerm : FileMetaBaseTerm
    {
        public bool isAsTerm => m_AsOrIsToken?.type == ETokenType.As;
        public bool isIsNotTerm => m_AsOrIsToken?.type == ETokenType.IsNot;
        public FileMetaCallLink variableCallLink => m_VariableCallLink;
        public FileMetaClassDefine defineType => m_DefineType;
        public Token convertIsTypeNameToken => m_ConvertIsTypeNameToken;
        public Token asOrIsToken => m_AsOrIsToken;

        private FileMetaCallLink m_VariableCallLink = null;
        private List<Node> m_LeftNodes = null;
        private Token m_AsOrIsToken = null;
        private FileMetaClassDefine m_DefineType = null;
        private Token m_ConvertIsTypeNameToken = null;

        // 1. var1 as Class1  2. var1 is Class1   3. var1 is Class1 var2
        public FileMetaAsOrIsTerm(FileMeta fm, List<Node> leftNodes, Token asOrisToken, List<Node> typeNodes, Node optionalVarNode)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (leftNodes == null || leftNodes.Count == 0 || typeNodes == null || typeNodes.Count == 0 || asOrisToken == null)
            {
                Log.AddFileMetaLog(LID.Unknown, "Error FileMetaAsOrIsTerm 参数不合法，无法构造 as/is 表达式");
                return;
            }

            m_AsOrIsToken = asOrisToken;
            m_LeftNodes = leftNodes;

            // 左侧变量调用链
            // as/is 左侧在某些语法形态会被拆成 [IdentifierNode, ParNode, ...]，
            // 这里把 Par 节点回挂到首节点，保证后续 Meta 解析也能识别为函数调用。
            var leftRoot = leftNodes[0];
            if (leftRoot != null && leftRoot.parNode == null && leftNodes.Count > 1)
            {
                for (int i = 1; i < leftNodes.Count; i++)
                {
                    var ln = leftNodes[i];
                    if (ln?.nodeType == ENodeType.Par)
                    {
                        leftRoot.SetParNode(ln);
                        break;
                    }
                }
            }
            m_VariableCallLink = new FileMetaCallLink(fm, leftRoot);

            // 右侧类型（支持简单类型节点列表）
            if (typeNodes.Count == 1)
            {
                m_DefineType = new FileMetaClassDefine(fm, typeNodes[0], null);
            }
            else
            {
                // 多节点类型（例如命名空间前缀），简单合成为一个临时 Node 再交给 FileMetaClassDefine
                Node typeRoot = new Node(null);
                typeRoot.SetChildList(typeNodes);
                m_DefineType = new FileMetaClassDefine(fm, typeRoot, null);
            }

            // is / isnot 表达式最后可能还有一个变量名： var1 is Class1 var2
            if ((asOrisToken.type == ETokenType.Is || asOrisToken.type == ETokenType.IsNot) && optionalVarNode != null)
            {
                m_ConvertIsTypeNameToken = optionalVarNode.token;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VariableCallLink != null)
            {
                var leftText = m_VariableCallLink.ToFormatString();
                sb.Append(leftText);

                // 当 as 左侧是函数调用时，某些解析路径把 par 节点单独放在 leftNodes 中，
                // 这里补回参数显示，避免导出为 `Func as T`。
                if (!string.IsNullOrEmpty(leftText)
                    && leftText.IndexOf('(') < 0
                    && m_LeftNodes != null)
                {
                    for (int i = 0; i < m_LeftNodes.Count; i++)
                    {
                        var ln = m_LeftNodes[i];
                        if (ln?.nodeType == ENodeType.Par)
                        {
                            var p = new FileMetaParTerm(m_FileMeta, ln, FileMetaTermExpress.EExpressType.Common);
                            p.ClearDirty();
                            p.BuildAST();
                            sb.Append(p.ToFormatString());
                            break;
                        }
                    }
                }
                sb.Append(" ");
            }

            sb.Append(m_AsOrIsToken?.ToConstString());

            if (m_DefineType != null)
            {
                sb.Append(" ");
                sb.Append(m_DefineType.ToFormatString());
            }

            if (m_ConvertIsTypeNameToken != null)
            {
                sb.Append(" ");
                sb.Append(m_ConvertIsTypeNameToken.ToConstString());
            }

            return sb.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VariableCallLink != null)
            {
                var leftText = m_VariableCallLink.ToTokenString();
                sb.Append(leftText);
                if (!string.IsNullOrEmpty(leftText)
                    && leftText.IndexOf('(') < 0
                    && m_LeftNodes != null)
                {
                    for (int i = 0; i < m_LeftNodes.Count; i++)
                    {
                        var ln = m_LeftNodes[i];
                        if (ln?.nodeType == ENodeType.Par)
                        {
                            var p = new FileMetaParTerm(m_FileMeta, ln, FileMetaTermExpress.EExpressType.Common);
                            p.ClearDirty();
                            p.BuildAST();
                            sb.Append(p.ToTokenString());
                            break;
                        }
                    }
                }
                sb.Append(" ");
            }

            sb.Append(m_AsOrIsToken?.ToLexemeAllString());

            if (m_DefineType != null)
            {
                sb.Append(" ");
                sb.Append(m_DefineType.ToTokenString());
            }

            if (m_ConvertIsTypeNameToken != null)
            {
                sb.Append(" ");
                sb.Append(m_ConvertIsTypeNameToken.ToLexemeAllString());
            }

            return sb.ToString();
        }
    }
    public class FileMetaConstValueTerm : FileMetaBaseTerm
    {
        private Token m_PlusOrMinusToken = null;
        public Token plusMinusToken => m_PlusOrMinusToken;
        public FileMetaConstValueTerm( FileMeta fm, Token _token, Token plusMinusToken = null )
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_PlusOrMinusToken = plusMinusToken;
            m_Root = this;
        }
        public override string ToFormatString()
        {
            return m_Token?.ToConstString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Token?.ToLexemeAllString());

            return sb.ToString();
        }
    }
    // 
    public class FileMetaCallTerm : FileMetaBaseTerm
    {
        public FileMetaCallLink callLink => m_CallLink;

        private FileMetaCallLink m_CallLink = null;
        public FileMetaCallTerm( FileMeta fm, Node node )
        {
            m_FileMeta = fm;
            m_Root = this;
            m_CallLink = new FileMetaCallLink(fileMeta, node);
        }
        public override bool BuildAST()
        {
            for (int j = 0; j < m_CallLink.callNodeList.Count; j++)
            {
                var clc = callLink.callNodeList[j];
                if (clc.fileMetaParTerm != null)
                {
                    bool flag = clc.fileMetaParTerm.BuildAST();
                    if (flag == false)
                        return false;
                }
            }
            return true;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            if(m_CallLink != null)
                sb.Append(m_CallLink.ToFormatString());
            return sb.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            if( callLink != null )
            {
                sb.Append(callLink.ToTokenString());
            }
            return sb.ToString();
        }
    }
    //(a+b-(2*100)) (1,(a+2),3)
    public class FileMetaParTerm : FileMetaBaseTerm
    {
        public Token endToken => m_EndToken;

        private Node m_Node = null;
        private Token m_EndToken = null;

        // Array a = ( 1,2 3,4 );  Class c = ( 1,2 );    int a = ( 1 + 2 + GetX() )  Enum e = Enum.Value( {} );
        public FileMetaParTerm( FileMeta fm, Node node, FileMetaTermExpress.EExpressType expressType )
        {
            m_FileMeta = fm;
            m_Token = node.token;
            m_EndToken = node.endToken;
            m_Node = node;

            var childList = node.childList;

            List<List<Node>> nodeListList = new List<List<Node>>();
            List<Node> tempNodeList = new List<Node>();
            for (int j = 0; j < childList.Count; j++)
            {
                var c2node = childList[j];
                if (c2node.nodeType == ENodeType.Comma
                    || c2node.nodeType == ENodeType.SemiColon)
                {
                    nodeListList.Add(tempNodeList);
                    tempNodeList = new List<Node>();
                }
                else if( c2node.nodeType == ENodeType.LineEnd )
                {
                    continue;
                }
                else
                {
                    tempNodeList.Add(c2node);
                }
            }
            if (tempNodeList.Count > 0)
            {
                nodeListList.Add(tempNodeList);
            }

            for (int i = 0; i < nodeListList.Count; i++)
            {
                var nodeList = nodeListList[i];
                if( nodeList.Count == 0 )
                {
                    Log.AddFileMetaLog(LID.Unknown, "Error nodeList.Count == 0 ");
                    Debug.Assert(false, "");
                    continue;
                }
                else if( nodeList.Count == 1 )
                {
                    var cnode = nodeList[0];
                    if (cnode.nodeType == ENodeType.ConstValue)     //Fun( 1 )
                    {
                        var fileMetaConstValueTerm = new FileMetaConstValueTerm(m_FileMeta, cnode.token);
                        fileMetaConstValueTerm.priority = cnode.priority;
                        AddFileMetaTerm(fileMetaConstValueTerm);
                    }
                    else if (cnode.nodeType == ENodeType.Bracket)       // Fun( [1] )
                    {
                        var fileMetaBracketTerm = new FileMetaBracketTerm(m_FileMeta, cnode);
                        fileMetaBracketTerm.priority = SignComputePriority.Level1;
                        AddFileMetaTerm(fileMetaBracketTerm);
                    }
                    else if (cnode.nodeType == ENodeType.Comma)
                    {
                        var fileMetaSymbolTerm = new FileMetaSymbolTerm(m_FileMeta, cnode.token);
                        fileMetaSymbolTerm.priority = SignComputePriority.Level12_Split;
                        AddFileMetaTerm(fileMetaSymbolTerm);
                    }
                    else if( cnode.nodeType == ENodeType.Brace )  // Enum.Value( {} );
                    {
                        var fileMetaBraceTerm = new FileMetaBraceTerm(m_FileMeta, cnode);
                        AddFileMetaTerm(fileMetaBraceTerm);
                    }
                    else
                    {
                        var fileMetaCallTerm = new FileMetaCallTerm(m_FileMeta, cnode);
                        fileMetaCallTerm.priority = SignComputePriority.Level1;
                        AddFileMetaTerm(fileMetaCallTerm);
                    }
                }
                else
                {
                    var fileMetaCallTerm = FileMetatUtil.CreateFileMetaExpress(fm, nodeList, FileMetaTermExpress.EExpressType.Common);
                    fileMetaCallTerm.priority = SignComputePriority.Level1;
                    AddFileMetaTerm(fileMetaCallTerm);
                }
            }
        }
        public override void ClearDirty()
        {
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                m_FileMetaExpressList[i].ClearDirty();
            }
        }
        public override bool BuildAST()
        {
            if ( m_FileMetaExpressList.Count == 1 )
            {
                FileMetaBaseTerm fmbt = m_FileMetaExpressList[0];
                if (fmbt == null) return false;
                if( fmbt.root != null )
                {
                    isDirty = true;
                    m_Root = fmbt.root;
                    return true;
                }
                else
                {
                    if (fmbt.BuildAST())
                    {
                        isDirty = true;
                        m_Root = fmbt.root;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                bool flag = true;
                for (int i = 0; i < m_FileMetaExpressList.Count; i++)
                {
                    var fme = m_FileMetaExpressList[i];
                    if( !fme.BuildAST() )
                    {
                        flag = false;
                    }
                }

                m_Root = this;
                return flag;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(m_Token.lexeme.ToString());
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                stringBuilder.Append(m_FileMetaExpressList[i].ToFormatString());
                if (i < m_FileMetaExpressList.Count - 1)
                   stringBuilder.Append(",");
            }
            stringBuilder.Append(m_EndToken?.lexeme.ToString());
            return stringBuilder.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append( "BeginParToken:" + m_Token?.ToLexemeAllString());
            sb.Append("EndParToken:" + m_EndToken?.ToLexemeAllString());

            return sb.ToString();
        }
    }
    public class FileMetaBraceTerm : FileMetaBaseTerm
    {
        public List<FileMetaSyntax> fileMetaAssignSyntaxList => m_FileMetaAssignSyntaxList;
        public List<FileMetaCallLink> fileMetaCallLinkList => m_FileMetaCallLinkList;
        private List<FileMetaSyntax> m_FileMetaAssignSyntaxList = new List<FileMetaSyntax>();
        private List<FileMetaCallLink> m_FileMetaCallLinkList = new List<FileMetaCallLink>();
        private Token m_BraceEndToken = null;
        private Node m_Node = null;
        // { a = 10, b = 20, c = Class1(), 10, [1,2,3], [[1,2],[3,4],100], Class1():100, (1,2,3) }
        // 支持大括号的内容 有 赋值语句 一般在动态类赋值里边使用 有直接的常量值, 有内嵌数组, 还有可能支持 Map的kv形式  (1,2,3) 这个不确定支不支持
        public FileMetaBraceTerm( FileMeta fm, Node node )
        {
            m_FileMeta = fm;
            m_Root = this;
            m_Node = node;
            m_Token = m_Node.token;
            m_BraceEndToken = m_Node.endToken;
            HandleBraceTerm();

            if (m_BraceEndToken == null )
            {
                Log.AddFileMetaLog(LID.Unknown, "Error FileMetaBraceTerm--");
            }
        }
        private void HandleBraceTerm()
        {
            // { a = 10, b = 20, c = Class1() }
            List<List<Node>> nodeListList = new List<List<Node>>();
            List<Node> tempNodeList = new List<Node>();
            for (int j = 0; j < m_Node.childList.Count; j++)
            {
                var c2node = m_Node.childList[j];
                if (c2node.nodeType == ENodeType.Comma )
                {
                    nodeListList.Add(tempNodeList);
                    tempNodeList = new List<Node>();
                }
                else if( c2node.nodeType == ENodeType.LineEnd )
                {
                    continue;
                }
                //else if( c2node.nodeType == ENodeType.Bracket )
                //{
                //    nodeListList.Add(c2node);
                //}
                else
                {
                    tempNodeList.Add(c2node);
                }
            }
            if(tempNodeList.Count > 0 )
            {
                nodeListList.Add(tempNodeList);
            }

            int nodeListCount = nodeListList.Count;
            for (int i = 0; i < nodeListCount; i++)
            {
                var nodeList = nodeListList[i];
                List<Node> defineNodeList = new List<Node>();
                List<Node> valueNodeList = new List<Node>();
                Token assignToken = null;
                for (int j = 0; j < nodeList.Count; j++)
                {
                    var nl2 = nodeList[j];
                    if (nl2.nodeType == ENodeType.Assign) // a= 100
                    {
                        if (assignToken == null)
                        {
                            assignToken = nl2.token;
                            continue;
                        }
                        else
                        {
                            Log.AddFileMetaLog(LID.Unknown, " Errorr FileMetaBraceTerm.HandleBraceTerm 解析{ a = ?} 时，多个=号 Token: " + assignToken.ToLexemeAllString() );
                        }
                    }
                    else if( nl2.nodeType == ENodeType.Key && nl2.token.type == ETokenType.Colon ) // Map<int,string>(){ 100:"aaa", 200:"bbb" }
                    {
                        if (assignToken == null)
                        {
                            assignToken = nl2.token;
                            continue;
                        }
                        else
                        {
                            Log.AddFileMetaLog(LID.Unknown, " Errorr FileMetaBraceTerm.HandleBraceTerm 解析{ a:'aaa'} 时，多个:号 Token: " + assignToken.ToLexemeAllString());
                        }
                    }
                    else
                    {
                        if (assignToken == null)
                        {
                            defineNodeList.Add(nl2);
                        }
                        else
                        {
                            valueNodeList.Add(nl2);
                        }
                    }
                }

                if(defineNodeList.Count > 0 && valueNodeList.Count == 0 && assignToken == null )
                {
                    if (defineNodeList[0].nodeType == ENodeType.Bracket && defineNodeList.Count == 1 )
                    {
                        FileMetaBracketTerm tmbt = new FileMetaBracketTerm(m_FileMeta, defineNodeList[0]);
                        AddFileMetaTerm(tmbt);
                    }
                    else if (defineNodeList[0].nodeType == ENodeType.Brace && defineNodeList.Count == 1)
                    {
                        FileMetaBraceTerm tmbt = new FileMetaBraceTerm(m_FileMeta, defineNodeList[0]);
                        AddFileMetaTerm(tmbt);
                    }
                    else if (defineNodeList[0].nodeType == ENodeType.IdentifierLink)
                    {
                        var valueNodeTerm = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, defineNodeList, FileMetaTermExpress.EExpressType.Common);  //这种方式只允许在
                        AddFileMetaTerm(valueNodeTerm);
                    }
                    else if (defineNodeList[0].nodeType == ENodeType.ConstValue && defineNodeList.Count == 1)
                    {
                        var tmbt = new FileMetaConstValueTerm(m_FileMeta, defineNodeList[0].token );
                        AddFileMetaTerm(tmbt);
                    }
                    else
                    {
                        Debug.Assert(false, "");
                        Log.AddFileMetaLog(LID.Unknown, "Error 在解析为{}中，数组形式 解析有问题!!");
                        continue;
                    }
                }
                else if( assignToken != null && defineNodeList.Count > 0 && valueNodeList.Count > 0 )
                {
                    if ( (defineNodeList.Count != 1 && defineNodeList.Count != 2 ) || valueNodeList.Count < 1)
                    {
                        Debug.Assert(false, "");
                        Log.AddFileMetaLog(LID.Unknown, "Error 在解析为{}中，赋值= 解析有问题!!");
                        continue;
                    }
                    if( defineNodeList.Count == 2 )
                    {
                        Token nameToken = defineNodeList[1].token;
                        var classRef = new FileMetaClassDefine(m_FileMeta, defineNodeList[0]); 
                        FileMetaBaseTerm fmel = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, valueNodeList, FileMetaTermExpress.EExpressType.Common); 
                        FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef, nameToken, assignToken, null, fmel );
                        fmdvs.isAppendSemiColon = false;
                        m_FileMetaAssignSyntaxList.Add(fmdvs);
                    }
                    else
                    {
                        FileMetaCallLink fmcl = new FileMetaCallLink(m_FileMeta, defineNodeList[0]);
                        FileMetaBaseTerm fmel = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, valueNodeList, FileMetaTermExpress.EExpressType.Common);  //这种方式只允许在
                        FileMetaOpAssignSyntax fmoas = new FileMetaOpAssignSyntax(fmcl, assignToken, null, null, null, fmel, true);
                        fmoas.isAppendSemiColon = false;
                        m_FileMetaAssignSyntaxList.Add(fmoas);
                    }
                    //FileMetaBaseTerm defineNodeTerm = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, defineNodeList, FileMetaTermExpress.EExpressType.Common);  //这种方式只允许在
                    //FileMetaBaseTerm valueNodeTerm = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, valueNodeList, FileMetaTermExpress.EExpressType.Common);  //这种方式只允许在
                    //FileMetaSymbolTerm fst = new FileMetaSymbolTerm(m_FileMeta, assignToken) {  left = defineNodeTerm, right = valueNodeTerm };
                    //AddFileMetaTerm (fst);
                }
                else
                {
                    Log.AddFileMetaLog(LID.Unknown, "Error 在解析为{}中，出现了不该出现的格式");
                }
            }
        }
        public override void ClearDirty()
        {
            base.ClearDirty();
        }
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(m_Token?.lexeme.ToString());            
            foreach( var v in m_FileMetaExpressList )
            {
                stringBuilder.Append(v.ToFormatString());
            }
            stringBuilder.Append(m_BraceEndToken?.lexeme.ToString());
            return stringBuilder.ToString();
        }

        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("BeginBraceToken:" + m_Token?.ToLexemeAllString());
            sb.Append("EndBraceToken:" + m_BraceEndToken?.ToLexemeAllString());

            return sb.ToString();
        }
    }
    // [1,2,3,[1,2,3]]  [i][1]
    public class FileMetaBracketTerm : FileMetaBaseTerm
    {
        public Token beginToken => m_BeginBracketToken;
        public Token endToken => m_EndBracketetToken;

        Token m_BeginBracketToken = null;
        Token m_EndBracketetToken = null;
        // = [1][2][var1.index]                               
        public FileMetaBracketTerm(FileMeta fm, Node node )
        {
            m_FileMeta = fm;
            m_Root = this;
            m_Token = node.token;
            m_BeginBracketToken = node.token;
            m_EndBracketetToken = node.endToken;

            List<List<Node>> nodeListList = new List<List<Node>>();

            List<Node> tnodeList = new List<Node>();
            for ( int i = 0; i < node.childList.Count; i++ )
            {
                var cnode = node.childList[i];
                if (cnode.nodeType == ENodeType.Comma )
                {
                //    var fileMetaSymbolTerm = new FileMetaSymbolTerm(m_FileMeta, cnode.token);
                //    AddFileMetaTerm(fileMetaSymbolTerm);
                //    if (i == node.childList.Count - 1)
                //    {
                //        Log.AddFileMetaLog(LID.Unknown, "Warning [1,2,3,]有多余逗号出现??");
                //        Debug.Assert(false);
                //    }
                    nodeListList.Add(tnodeList);
                    tnodeList = new List<Node>();
                    continue;
                }
                else
                {
                    tnodeList.Add(cnode);
                }
            }
            if(tnodeList.Count > 0 )
                nodeListList.Add(tnodeList);

            for ( int i = 0; i < nodeListList.Count; i++ )
            {
                var cnodelist = nodeListList[i];

                var fvt = FileMetatUtil.CreateFileMetaExpress(fm, cnodelist, FileMetaTermExpress.EExpressType.Common);

                AddFileMetaTerm(fvt);
            }

            /*
            if( cnode.nodeType == ENodeType.ConstValue )
            {
                var fileMetaConstValueTerm = new FileMetaConstValueTerm(m_FileMeta,cnode.token);
                AddFileMetaTerm(fileMetaConstValueTerm);
            }
            else if( cnode.nodeType == ENodeType.Bracket )
            {
                var fileMetaBracketTerm = new FileMetaBracketTerm(m_FileMeta, cnode);
                AddFileMetaTerm(fileMetaBracketTerm);
            }
            else if( cnode.nodeType == ENodeType.Comma )
            {
                var fileMetaSymbolTerm = new FileMetaSymbolTerm(m_FileMeta, cnode.token);
                AddFileMetaTerm(fileMetaSymbolTerm);
                if (i == node.childList.Count - 1)
                {
                    Log.AddFileMetaLog(LID.Unknown, "Warning [1,2,3,]有多余逗号出现??");
                }
                continue;
            }
            else if (cnode.nodeType == ENodeType.Par)
            {
                Log.AddFileMetaLog(LID.Unknown, "Error 不支持在[]中解析()的逻辑!!");
                continue;
            }
            else if (cnode.nodeType == ENodeType.Key)
            {
                if( cnode.token.type == ETokenType.This 
                    || cnode.token.type == ETokenType.Base )
                {
                    var fileMetaCallTerm = new FileMetaCallTerm(m_FileMeta, cnode);
                    AddFileMetaTerm(fileMetaCallTerm);
                }
                else
                {
                    Log.AddFileMetaLog(LID.Unknown, "Error 不支持在[]中解析Key的逻辑!!");
                    continue;
                }
            }
            else if (cnode.nodeType == ENodeType.Brace )
            {
                var fileMetaBraceTerm = new FileMetaBraceTerm(m_FileMeta, cnode);
                AddFileMetaTerm(fileMetaBraceTerm);
                continue;
            }
            else if( cnode.nodeType == ENodeType.Symbol )
            {
                var fileMetaSymbolTerm = new FileMetaSymbolTerm(m_FileMeta, cnode.token);
                AddFileMetaTerm(fileMetaSymbolTerm);
                if (i == node.childList.Count - 1)
                {
                    Log.AddFileMetaLog(LID.Unknown, "Warning [1,2,3,]有多余逗号出现??");
                    Debug.Assert(false);
                }
                continue;
            }
            else
            {
                var fileMetaCallTerm = new FileMetaCallTerm(m_FileMeta, cnode);
                AddFileMetaTerm(fileMetaCallTerm);
            }
            */
        }
        // = [{a=20;b="aaa";},{a=30;b="ccc";}]  在data里边，有这样使用的过程
        public FileMetaBracketTerm( FileMeta fm, Node node, int a )
        {
            int type = -1;
            List<Node> list = new List<Node>();
            for (int index = 0; index < node.childList.Count; index++)
            {
                var curNode = node.childList[index];
                if (curNode.nodeType == ENodeType.LineEnd
                    || curNode.nodeType == ENodeType.SemiColon
                    || curNode.nodeType == ENodeType.Comma)
                {
                    if (list.Count == 0)
                    {
                        continue;
                    }
                    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, list);

                    //AddFileMemberVariable(fmmd);

                    list = new List<Node>();
                    continue;
                }
                if (curNode.nodeType == ENodeType.IdentifierLink)      //aaa(){},aaa(){}
                {

                }
                if (curNode.nodeType == ENodeType.Brace)  //Class1 [{},{}]
                {
                    if (type == 2 || type == 3)
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                        continue;
                    }

                    type = 1;

                    //FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, EMemberDataType.NoNameClass);

                    //AddFileMemberVariable(fmmd);
                }
                else if (curNode?.nodeType == ENodeType.Bracket) // [[],[]]
                {
                    if (type == 1 || type == 2)
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                        continue;
                    }

                    type = 3;

                    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.Array);

                    //AddFileMemberVariable(fmmd);
                }
                else if (curNode?.nodeType == ENodeType.IdentifierLink
                    || curNode?.nodeType == ENodeType.Assign
                    || curNode?.nodeType == ENodeType.ConstValue
                    )
                {
                    list.Add(curNode);
                }
            }
        }
        // = [{},{},{}]
        public override string ToFormatString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                stringBuilder.Append(m_FileMetaExpressList[i].ToFormatString());
                //if (i < m_FileMetaAssignSyntaxList.Count - 1)
                //    stringBuilder.Append(", ");
            }
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }
        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("BeginBraceToken:" + m_BeginBracketToken?.ToLexemeAllString());
            sb.Append("EndBraceToken:" + m_EndBracketetToken?.ToLexemeAllString());

            return sb.ToString();
        }
    }
    //
    public class FileMetaIfSyntaxTerm : FileMetaBaseTerm
    {
        public FileMetaKeyIfSyntax ifSyntax => m_IfSyntax;

        private FileMetaKeyIfSyntax m_IfSyntax = null;
        public FileMetaIfSyntaxTerm(FileMeta fm, FileMetaKeyIfSyntax _ifSyntax)
        {
            m_FileMeta = fm;
            m_IfSyntax = _ifSyntax;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            if (m_IfSyntax != null)
            {
                m_IfSyntax.SetDeep(_deep);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.Append(m_IfSyntax.ToFormatString());
            return sb.ToString();
        }
    }
    // express ? var/const : var2/const2
    public class FileMetaThreeItemSyntaxTerm : FileMetaBaseTerm
    {
        public FileMetaBaseTerm return1Term => m_Return1Term;
        public FileMetaBaseTerm return2Term => m_Return2Term;
        public FileMetaBaseTerm conditionTerm => m_ConditionTerm;

        private FileMetaBaseTerm m_Return1Term = null;
        private FileMetaBaseTerm m_Return2Term = null;
        private FileMetaBaseTerm m_ConditionTerm = null;
        public FileMetaThreeItemSyntaxTerm(FileMeta fm, List<Node> conditionNodeList,
            List<Node> returnNode1List, List<Node> returnNode2List )
        {
            m_FileMeta = fm;

            m_ConditionTerm = FileMetatUtil.CreateFileMetaExpress(fm, conditionNodeList, FileMetaTermExpress.EExpressType.Common);
            m_Return1Term = FileMetatUtil.CreateFileMetaExpress(fm, returnNode1List, FileMetaTermExpress.EExpressType.Common);
            m_Return2Term = FileMetatUtil.CreateFileMetaExpress(fm, returnNode2List, FileMetaTermExpress.EExpressType.Common);
        }

        public override bool BuildAST()
        {
            m_Root = this;
            return true;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_ConditionTerm.ToFormatString());
            sb.Append(" ? ");
            sb.Append(m_Return1Term.ToFormatString());
            sb.Append(" : ");
            sb.Append(m_Return2Term.ToFormatString());


            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_ConditionTerm.ToString());
            sb.Append(" ? ");
            sb.Append(m_Return1Term.ToString());
            sb.Append(" : ");
            sb.Append(m_Return2Term.ToString());
   

            return sb.ToString();
        }
    }
    public class FileMetaMatchSyntaxTerm : FileMetaBaseTerm
    {
        public FileMetaKeySwitchSyntax switchSyntax => m_SwitchSyntax;

        private FileMetaKeySwitchSyntax m_SwitchSyntax = null;
        public FileMetaMatchSyntaxTerm(FileMeta fm, FileMetaKeySwitchSyntax _switchSyntax)
        {
            m_FileMeta = fm;
            m_SwitchSyntax = _switchSyntax;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            if (m_SwitchSyntax != null)
            {
                m_SwitchSyntax.SetDeep(_deep);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.Append(m_SwitchSyntax.ToFormatString());
            return sb.ToString();
        }
    }

    // a + b - 30 + (200/20).toInt()
    public class FileMetaTermExpress : FileMetaBaseTerm
    {
        public enum EExpressType
        {
            Common,
            MemberVariable,
            ParamVariable,
        }
        public bool m_CanUseDoublePlusOrMinus = false;
        public EExpressType expressType = EExpressType.Common; //0 普通语句  1 成员变量  2参数变量
       
        public FileMetaTermExpress( FileMeta fm, List<Node> nodeList, EExpressType _expressType = EExpressType.Common )
        {
            m_FileMeta = fm;
            //Node tn = new Node( null );
            //tn.SetChildList( nodeList );
            //var childList = StructParse.HandleExpressNode(tn);
            var childList = StructParse.HandleNodeSingleLine(nodeList);

            expressType = _expressType;

            CreateFileMetaExpressByChildList(childList);
        }
        void CreateFileMetaExpressByChildList(List<Node> nodeList)
        {
            if (nodeList.Count == 0) return;
            FileMetaBaseTerm fmbt = null;
            //FileMetaCallLink fileMetaCallLink = null;
            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = nodeList[i];
                if (node.nodeType == ENodeType.Symbol)
                {
                    FileMetaSymbolTerm fmn = new FileMetaSymbolTerm(m_FileMeta, node.token);
                    fmn.priority = node.priority;
                    AddFileMetaTerm(fmn);
                    fmbt = null;
                }
                else if (node.nodeType == ENodeType.LeftAngle
                    || node.nodeType == ENodeType.RightAngle)
                {
                    FileMetaSymbolTerm fmn = new FileMetaSymbolTerm(m_FileMeta, node.token);
                    fmn.priority = SignComputePriority.Level6_Compare;
                    AddFileMetaTerm(fmn);
                    fmbt = null;
                }
                else if (node.nodeType == ENodeType.Brace)
                {
                    if (fmbt != null)
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error 表达式不允许多个自定义元素存在!!" + fmbt.ToTokenString());
                    }
                    fmbt = new FileMetaCallTerm(m_FileMeta, node);
                    fmbt.priority = int.MaxValue;
                    AddFileMetaTerm(fmbt);
                }
                else if (node.nodeType == ENodeType.ConstValue)
                {
                    if (node.extendLinkNodeList.Count > 0)
                    {
                        fmbt = new FileMetaCallTerm(m_FileMeta, node);
                        fmbt.priority = int.MaxValue;
                    }
                    else
                    {
                        fmbt = new FileMetaConstValueTerm(m_FileMeta, node.token);
                        fmbt.priority = int.MaxValue;
                    }
                    AddFileMetaTerm(fmbt);
                }
                else if (node.nodeType == ENodeType.Key )
                {
                    if(node.token?.type == ETokenType.As
                    || node.token?.type == ETokenType.Is )
                    {
                        FileMetaSymbolTerm fmn = new FileMetaSymbolTerm(m_FileMeta, node.token);
                        fmn.priority = node.priority;
                        AddFileMetaTerm(fmn);
                        fmbt = null;
                    }
                    else if(node.token?.type == ETokenType.This
                    || node.token?.type == ETokenType.Base
                    || node.token?.type == ETokenType.New)
                    {
                        fmbt = new FileMetaCallTerm(m_FileMeta, node);
                        fmbt.priority = int.MaxValue;
                        AddFileMetaTerm(fmbt);
                    }
                    else
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error --------------------------------------!!" + fmbt.ToTokenString());
                    }
                }
                else if( node.nodeType == ENodeType.IdentifierLink )
                {
                    if(fmbt != null )
                    {
                        Log.AddFileMetaLog( LID.FileMetaExpressBeforeDefine, fmbt.token );
                    }
                    fmbt = new FileMetaCallTerm(m_FileMeta, node);
                    fmbt.priority = int.MaxValue;
                    AddFileMetaTerm(fmbt);
                }
                else if( node.nodeType == ENodeType.Par )
                {
                    if (node.extendLinkNodeList.Count > 0)
                    {
                        fmbt = new FileMetaCallTerm(m_FileMeta, node);
                        fmbt.priority = int.MaxValue;
                    }
                    else
                    {
                        fmbt = new FileMetaParTerm(m_FileMeta, node, expressType);
                        fmbt.priority = SignComputePriority.Level1;
                    }
                    AddFileMetaTerm(fmbt);
                }
                else if( node.nodeType == ENodeType.Key && node.token?.type == ETokenType.QuestionMark )
                {
                    // 三元表达式在 FileMetatUtil.CreateFileMetaExpress 中统一处理，这里不再直接创建
                    Log.AddFileMetaLog(LID.Unknown, "Warning 在表达式中检测到三元运算符'?'，请通过 CreateFileMetaExpress 入口创建表达式");
                }
                else if( node.nodeType == ENodeType.Bracket )
                {
                    var fileMetaBracketTerm = new FileMetaBracketTerm(m_FileMeta, node );
                    fileMetaBracketTerm.priority = SignComputePriority.Level1;
                    AddFileMetaTerm(fileMetaBracketTerm);
                }
                else
                {
                    Debug.Assert(false);
                    Log.AddFileMetaLog(LID.Unknown, "没有找到该类型: " + node.token.type.ToString() + " 位置: " + node.token.ToLexemeAllString());
                }
            }
        }
        private bool BuildTst(List<FileMetaBaseTerm> list)
        {
            if (list.Count == 0)
                return false;
            if (list.Count == 1)
            {
                m_Root = list[0];
                return true;
            }
            int maxLevel = int.MaxValue;
            int index = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (maxLevel > list[i].priority && list[i].isDirty == false )
                {
                    maxLevel = list[i].priority;
                    index = i;
                }
            }
            if (index >= 0 && index < list.Count)
            {
                FileMetaBaseTerm currentTerm = list[index];
                FileMetaBaseTerm listFrontTerm = null;
                FileMetaBaseTerm listNextTerm = null;
                if ( index > 0 )
                {
                    listFrontTerm = list[index - 1].root;
                }
                if( index < list.Count - 1 )
                {
                    listNextTerm = list[index + 1].root;
                }
                if( currentTerm.priority == SignComputePriority.Level2_LinkOp )
                {
                    ETokenType ett = currentTerm.token.type;
                    if (!m_CanUseDoublePlusOrMinus && (ett == ETokenType.DoubleMinus || ett == ETokenType.DoublePlus) )
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error 只有在语句中，可以使用i++ 等语法，变量与传参是禁止使用i++" +
                            "Token 位置:" + currentTerm.token.ToAllString());
                        return false;
                    }

                    if( ett == ETokenType.DoubleMinus || ett == ETokenType.DoublePlus )
                    {
                        bool con1 = listFrontTerm != null &&
                            (!(listFrontTerm is FileMetaSymbolTerm) || listFrontTerm.isDirty);    
                        if ( listNextTerm == null && con1)// 只允许i++; 或者是(i++/i--)的实现 限制其它语法
                        {
                            currentTerm.left = listFrontTerm;
                            list.RemoveAt(index - 1);
                        }
                    }
                    else if( ett == ETokenType.Minus || ett == ETokenType.Plus || ett == ETokenType.Not || ett == ETokenType.Negative )
                    {
                        if (listNextTerm == null)
                        {
                            Log.AddFileMetaLog(LID.Unknown, "Error 表达式解析错误!! FileMetaExpress 575");
                            return false;
                        }
                        currentTerm.right = listNextTerm;
                        if (listNextTerm != null)
                        {
                            list.RemoveAt(index+1);
                        }
                    }
                    else
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error 不能使用错误符号 !! FileMetaExpress 698" + currentTerm.token.ToLexemeAllString());
                        return false;
                    }
                }
                else
                {
                    if(listFrontTerm != null && listNextTerm != null )
                    {
                        currentTerm.left = listFrontTerm;
                        currentTerm.right = listNextTerm;
                        if (listFrontTerm != null)
                        {
                            list.RemoveAt(index - 1);
                        }
                        if (listNextTerm != null)
                        {
                            list.RemoveAt( index );
                        }
                    }
                    else
                    {
                        Log.AddFileMetaLog(LID.Unknown, "Error BuildTst 表达式解析错误!! 604");
                        return false;
                    }
                }
                if (list.Count == 1)
                {
                    m_Root = list[0];
                    return true;
                }                
            }
            else
            {
                Log.AddFileMetaLog(LID.Unknown, "选择已经超出来范围!!");
                return false;
            }
            return BuildTst(list);
        }
        public override bool BuildAST()
        {
            List<FileMetaBaseTerm> buildASTList = new List<FileMetaBaseTerm>(m_FileMetaExpressList);

            // Check length
            for (int i = 0; i < buildASTList.Count; i++)
            {
                var fmst = buildASTList[i] as FileMetaSymbolTerm;
                if (fmst != null)
                {
                    var ttoken = fmst.token;
                    if (ttoken?.type == ETokenType.Plus || ttoken?.type == ETokenType.Minus)
                    {
                        // decide unary (prefix) vs binary based on previous term
                        // unary when at start or previous term is an operator (FileMetaSymbolTerm)
                        if (i == 0)
                        {
                            fmst.priority = SignComputePriority.Level2_LinkOp; // unary
                        }
                        else
                        {
                            var prev = buildASTList[i - 1] as FileMetaSymbolTerm;
                            if (prev != null)
                                fmst.priority = SignComputePriority.Level2_LinkOp; // unary
                            else
                                fmst.priority = SignComputePriority.Level3_Low_Compute; // binary
                        }
                    }
                }
                buildASTList[i].BuildAST();

            }
            m_Root = null;
            return BuildTst(buildASTList);
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                m_FileMetaExpressList[i].SetDeep(_deep);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            FileMetaBaseTerm beforeFMTE = null;
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var cur = m_FileMetaExpressList[i];
                if (beforeFMTE != null)
                    sb.Append(" ");
                sb.Append(cur.ToFormatString());
                beforeFMTE = cur;
            }
            return sb.ToString();
        }

        public override string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Express Tokens: ");
            for (int i = 0; i < m_FileMetaExpressList.Count; i++)
            {
                var fme = m_FileMetaExpressList[i];
                sb.Append(" " + fme.ToTokenString());
            }

            return sb.ToString();
        }
    }
}
