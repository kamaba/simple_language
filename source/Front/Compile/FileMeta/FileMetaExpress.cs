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
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 多重逗号，导致解析无法解析!!");
                        break;
                    }
                    if (fmbtList.Count == 0)
                    {
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 首符号不能为逗号");
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
            isDirty = true;
        }
        public virtual void AddRangeFileMetaTerm(List<FileMetaBaseTerm> fmn)
        {
            for( int i = 0; i < fmn.Count; i++ )
            {
                fmn[i].SetFileMeta(m_FileMeta);
            }
            m_FileMetaExpressList.AddRange(fmn);
            isDirty = true;
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
                    {
                        priority = SignComputePriority.Level3_Hight_Compute;
                    }
                    break;
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
                case ETokenType.EmptyRet:        // ??
                    {
                        priority = SignComputePriority.Level9_AsOsIs;
                    }
                    break;
                case ETokenType.PlusAssign:             // +=
                case ETokenType.MinusAssign:            // -=
                case ETokenType.MultiplyAssign:         // *=
                case ETokenType.DivideAssign:           // /=
                case ETokenType.ModuloAssign:           // %=
                case ETokenType.InclusiveOrAssign:      // |=
                case ETokenType.XORAssign:              // ^=
                case ETokenType.ShiAssign:              // <<=
                case ETokenType.ShrAssign:              // >>=
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
        private Token m_AsOrIsToken = null;
        private FileMetaClassDefine m_DefineType = null;
        private Token m_ConvertIsTypeNameToken = null;

        // 1. var1 as Class1  2. var1 is Class1   3. var1 is Class1 var2
        public FileMetaAsOrIsTerm(FileMeta fm, FileMetaCallLink leftCallLink, Token asOrisToken, List<Node> typeNodes, Node optionalVarNode)
        {
            m_FileMeta = fm;
            m_Root = this;

            if (typeNodes == null || typeNodes.Count == 0 || asOrisToken == null)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "Error FileMetaAsOrIsTerm 参数不合法，无法构造 as/is 表达式");
                return;
            }
            m_AsOrIsToken = asOrisToken;
            m_Token = m_AsOrIsToken;

            m_VariableCallLink = leftCallLink;

            // 右侧类型（支持简单类型节点列表）
            if (typeNodes.Count == 1)
            {
                m_DefineType = new FileMetaClassDefine(fm, typeNodes[0], null);
            }
            else
            {
                // 多节点类型（例如泛型 Array<Object> 或命名空间前缀）：
                // 优先选择真实的类型根节点（IdentifierLink），避免使用临时节点导致 linkTokenList 为空。
                Node typeRoot = null;
                for (int i = 0; i < typeNodes.Count; i++)
                {
                    var tn = typeNodes[i];
                    if (tn == null) continue;
                    var tlist = tn.GetLinkTokenList();
                    if (tn.nodeType == ENodeType.IdentifierLink
                        || (tlist?.Count > 0))
                    {
                        typeRoot = tn;
                        break;
                    }
                }
                if (typeRoot == null)
                {
                    typeRoot = typeNodes[0];
                }
                m_DefineType = new FileMetaClassDefine(fm, typeRoot, null);
            }

            // is / isnot 表达式最后可能还有一个变量名： var1 is Class1 var2
            if ((asOrisToken.type == ETokenType.Is || asOrisToken.type == ETokenType.IsNot) && optionalVarNode != null)
            {
                m_ConvertIsTypeNameToken = optionalVarNode.token;
            }
        }
        public override bool BuildAST()
        {
            m_Root = this;
            return true;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VariableCallLink != null)
            {
                var leftText = m_VariableCallLink.ToFormatString();
                sb.Append(leftText);
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
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VariableCallLink != null)
            {
                var leftText = m_VariableCallLink.ToTokenString();
                sb.Append(leftText);
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

            if(m_PlusOrMinusToken != null )
            {
                sb.Append(m_PlusOrMinusToken?.ToString());
            }
            sb.Append(m_Token?.ToLexemeAllString());

            return sb.ToString();
        }
        public override string ToString()
        {
            return m_Token?.ToConstString();
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
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_CallLink != null)
                sb.Append(m_CallLink.ToFormatString());
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
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error nodeList.Count == 0 ");
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
        public override string ToString()
        {
            return ToFormatString();
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
                Log.AddFileMetaLog(LID.ShowExtendMessage, "Error FileMetaBraceTerm--");
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
                            Log.AddFileMetaLog(LID.ShowExtendMessage, " Errorr FileMetaBraceTerm.HandleBraceTerm 解析{ a = ?} 时，多个=号 Token: " + assignToken.ToLexemeAllString() );
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
                            Log.AddFileMetaLog(LID.ShowExtendMessage, " Errorr FileMetaBraceTerm.HandleBraceTerm 解析{ a:'aaa'} 时，多个:号 Token: " + assignToken.ToLexemeAllString());
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
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 在解析为{}中，数组形式 解析有问题!!");
                        continue;
                    }
                }
                else if( assignToken != null && defineNodeList.Count > 0 && valueNodeList.Count > 0 )
                {
                    if ( (defineNodeList.Count != 1 && defineNodeList.Count != 2 ) || valueNodeList.Count < 1)
                    {
                        //Debug.Assert(false, "");
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 在解析为{}中，赋值= 解析有问题!!");
                        continue;
                    }
                    if( defineNodeList.Count == 2 )
                    {
                        Token nameToken = defineNodeList[1].token;
                        var classRef = new FileMetaClassDefine(m_FileMeta, defineNodeList[0]); 
                        FileMetaBaseTerm fmel = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, valueNodeList, FileMetaTermExpress.EExpressType.Common); 
                        FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef, nameToken, assignToken, null, null, fmel );
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
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 在解析为{}中，出现了不该出现的格式");
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
            if (m_FileMetaAssignSyntaxList.Count > 0)
            {
                for (int i = 0; i < m_FileMetaAssignSyntaxList.Count; i++)
                {
                    stringBuilder.Append(m_FileMetaAssignSyntaxList[i].ToFormatString());
                    if (i < m_FileMetaAssignSyntaxList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
            }
            else if (m_FileMetaCallLinkList.Count > 0)
            {
                for (int i = 0; i < m_FileMetaCallLinkList.Count; i++)
                {
                    stringBuilder.Append(m_FileMetaCallLinkList[i].ToFormatString());
                    if (i < m_FileMetaCallLinkList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
            }
            else
            {
                for (int i = 0; i < m_FileMetaExpressList.Count; i++)
                {
                    stringBuilder.Append(m_FileMetaExpressList[i].ToFormatString());
                    if (i < m_FileMetaAssignSyntaxList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
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
        public override string ToString()
        {
            return ToFormatString();
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
                if (cnode == null)
                {
                    continue;
                }
                if (cnode.nodeType == ENodeType.LineEnd || cnode.nodeType == ENodeType.Comment)
                {
                    continue;
                }
                if (cnode.nodeType == ENodeType.Comma )
                {
                //    var fileMetaSymbolTerm = new FileMetaSymbolTerm(m_FileMeta, cnode.token);
                //    AddFileMetaTerm(fileMetaSymbolTerm);
                //    if (i == node.childList.Count - 1)
                //    {
                //        Log.AddFileMetaLog(LID.ShowExtendMessage, "Warning [1,2,3,]有多余逗号出现??");
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
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Warning [1,2,3,]有多余逗号出现??");
                }
                continue;
            }
            else if (cnode.nodeType == ENodeType.Par)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 不支持在[]中解析()的逻辑!!");
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
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 不支持在[]中解析Key的逻辑!!");
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
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Warning [1,2,3,]有多余逗号出现??");
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
                    //FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, list);

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
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
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
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                        continue;
                    }

                    type = 3;

                    //FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, global::SimpleLanguage.Compile.EMemberDataType.Array);

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
                if (i < m_FileMetaExpressList.Count - 1)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }
        public override string ToString()
        {
            return ToFormatString();
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
        public override string ToString()
        {
            return ToFormatString();
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
        private Token m_ColonToken = null;
        public FileMetaThreeItemSyntaxTerm(FileMeta fm, Token token1, Token token2, FileMetaBaseTerm conditionTerm, FileMetaBaseTerm return1Term, FileMetaBaseTerm return2Term)
        {
            m_FileMeta = fm;

            m_Token = token1;
            m_ColonToken = token2;

            m_ConditionTerm = conditionTerm;
            m_Return1Term = return1Term;
            m_Return2Term = return2Term;
        }

        public override bool BuildAST()
        {
            m_ConditionTerm.BuildAST();
            m_Return1Term.BuildAST();
            m_Return2Term.BuildAST();

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
    // express ? var/const : var2/const2
    public class FileMetaEmptyRetSyntaxTerm : FileMetaBaseTerm
    {
        public FileMetaBaseTerm return1Term => m_Return1Term;
        public FileMetaBaseTerm return2Term => m_Return2Term;

        private FileMetaBaseTerm m_Return1Term = null;
        private FileMetaBaseTerm m_Return2Term = null;

        public FileMetaEmptyRetSyntaxTerm(FileMeta fm, Token sign, FileMetaBaseTerm return1fmbt, FileMetaBaseTerm return2fmbt)
        {
            m_FileMeta = fm;

            m_Token = sign;
            m_Return1Term = return1fmbt;
            m_Return2Term = return2fmbt;
        }
        public override bool BuildAST()
        {
            m_Return1Term.BuildAST();
            m_Return2Term.BuildAST();

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

            sb.Append(m_Return1Term.ToFormatString());
            sb.Append(" ");
            sb.Append(m_Token.lexeme.ToString());
            sb.Append(" ");
            sb.Append(m_Return2Term.ToFormatString());


            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Return1Term.ToString());
            sb.Append(m_Token.lexeme.ToString());
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
        public override string ToString()
        {
            return ToFormatString();
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
       
        public FileMetaTermExpress( FileMeta fm, List<FileMetaBaseTerm> childList, EExpressType _expressType = EExpressType.Common )
        {
            m_FileMeta = fm;
            
            expressType = _expressType;

            m_FileMetaExpressList = childList;
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
            Token ctoken = null;
            int maxLevel = int.MaxValue;
            int index = -1;
            string extendMessage = "";
            for (int i = 0; i < list.Count; i++)
            {
                if (maxLevel > list[i].priority && list[i].isDirty == false )
                {
                    maxLevel = list[i].priority;
                    index = i;
                }
                if(ctoken == null )
                {
                    ctoken = list[i].token;
                }
                extendMessage = extendMessage + "  " + list[i].ToTokenString();
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
                if(currentTerm.priority == SignComputePriority.Level2_LinkOp )
                {
                    ETokenType ett = currentTerm.token.type;

                    if( ett == ETokenType.DoubleMinus || ett == ETokenType.DoublePlus )
                    {
                        bool con1 = listFrontTerm != null &&
                            (!(listFrontTerm is FileMetaSymbolTerm) || listFrontTerm.isDirty);    
                        if ( listNextTerm == null && con1)// 只允许i++; 或者是(i++/i--)的实现 限制其它语法
                        {
                            currentTerm.left = listFrontTerm;
                            list.RemoveAt(index - 1);
                        }
                        else
                        {
                            Log.AddFileMetaLog(LID.ShowExtendMessage, list[0].token, "Error not allow " + currentTerm.token.lexeme.ToString() + " !");
                            return false;
                        }
                    }
                    else if( ett == ETokenType.Minus || ett == ETokenType.Plus || ett == ETokenType.Not || ett == ETokenType.Negative
                        || ett == ETokenType.TryQuestion || ett == ETokenType.TryExclamation || ett == ETokenType.Try
                        || ett == ETokenType.Checked )
                    {
                        if (listNextTerm == null)
                        {
                            Log.AddFileMetaLog(LID.ShowExtendMessage, list[0].token, "Error 表达式解析错误!! FileMetaExpress 575" + extendMessage );
                            return false;
                        }

                        // 一元前缀统一形态：left 放符号节点，right 放目标表达式
                        // 例如：!DataAllEqual(a,b) => root='!' , left='!' , right=DataAllEqual(a,b)
                        if (currentTerm.left == null)
                        {
                            var unarySymbol = new FileMetaSymbolTerm(m_FileMeta, currentTerm.token);
                            unarySymbol.priority = SignComputePriority.Level2_LinkOp;
                            currentTerm.left = unarySymbol;
                        }

                        currentTerm.right = listNextTerm;
                        if (listNextTerm != null)
                        {
                            list.RemoveAt(index+1);
                        }
                        if (!m_CanUseDoublePlusOrMinus && (ett == ETokenType.DoubleMinus || ett == ETokenType.DoublePlus))
                        {
                            Log.AddFileMetaLog(LID.ShowExtendMessage, extendMessage + "Error 只有在语句中，可以使用i++ 等语法，变量与传参是禁止使用i++" +
                                "Token 位置:" + currentTerm.token.ToAllString());
                            return false;
                        }
                    }
                    else
                    {
                        Log.AddFileMetaLog(LID.ShowExtendMessage, extendMessage + "Error 不能使用错误符号 !! FileMetaExpress 698" + currentTerm.token.ToLexemeAllString());
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
                        Log.AddFileMetaLog(LID.ShowExtendMessage, extendMessage + "Error BuildTst 表达式解析错误!! 604");
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
                Log.AddFileMetaLog(LID.FileExpressFormatError, ctoken, extendMessage );
                return false;
            }
            return BuildTst(list);
        }
        public override bool BuildAST()
        {
            // BuildAST 在语法流程中可能会被重复调用；
            // 若当前表达式树未发生变更且已有根节点，则直接复用。
            if (!isDirty && m_Root != null)
            {
                return true;
            }

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
                    else if (ttoken?.type == ETokenType.Not || ttoken?.type == ETokenType.Negative
                        || ttoken?.type == ETokenType.TryQuestion || ttoken?.type == ETokenType.TryExclamation
                        || ttoken?.type == ETokenType.Checked)
                    {
                        // ! / ~ are unary-prefix operators in this grammar
                        fmst.priority = SignComputePriority.Level2_LinkOp;
                    }
                }
                if (buildASTList[i].isDirty || buildASTList[i].root == null)
                {
                    buildASTList[i].BuildAST();
                }

            }
            m_Root = null;
            var flag = BuildTst(buildASTList);
            if (flag)
            {
                ClearDirty();
            }
            return flag;
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
            if (m_Root != null && !ReferenceEquals(m_Root, this))
            {
                return FormatByAstNode(m_Root);
            }

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

        private static string FormatByAstNode(FileMetaBaseTerm node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            if (node is FileMetaSymbolTerm symbol)
            {
                var op = symbol.token?.lexeme?.ToString() ?? string.Empty;
                var left = symbol.left;
                var right = symbol.right;

                if (left == null && right != null)
                {
                    return op + FormatByAstNode(right);
                }
                if (left != null && right == null)
                {
                    return FormatByAstNode(left) + op;
                }
                if (left != null && right != null)
                {
                    return FormatByAstNode(left) + " " + op + " " + FormatByAstNode(right);
                }
            }

            return node.ToFormatString();
        }
        public override string ToString()
        {
            return ToFormatString();
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
