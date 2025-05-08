//****************************************************************************
//  File:      FileMetaExpress.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Parse;

namespace SimpleLanguage.Compile.CoreFileMeta
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
                        Console.WriteLine("Error 多重逗号，导致解析无法解析!!");
                        break;
                    }
                    if (fmbtList.Count == 0)
                    {
                        Console.WriteLine("Error 首符号不能为逗号");
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
                        priority = SignComputePriority.Level2_LinkOp;
                    }
                    break;                   
                case ETokenType.Multiply:
                case ETokenType.Divide:
                    {
                        priority = SignComputePriority.Level2_LinkOp;
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
            }
            priority = SignComputePriority.Level2_LinkOp;
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
    public class FileMetaConstValueTerm : FileMetaBaseTerm
    {
        public FileMetaConstValueTerm( FileMeta fm, Token _token )
        {
            m_FileMeta = fm;
            m_Token = _token;
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

            List<List<Node>> nodeListList = new List<List<Node>>();
            List<Node> tempNodeList = new List<Node>();
            for (int j = 0; j < m_Node.childList.Count; j++)
            {
                var c2node = m_Node.childList[j];
                if (c2node.nodeType == ENodeType.Comma
                    || c2node.nodeType == ENodeType.SemiColon)
                {
                    nodeListList.Add(tempNodeList);
                    tempNodeList = new List<Node>();
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
                    Console.WriteLine("Error nodeList.Count == 0 ");
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
                    var fileMetaCallTerm = new FileMetaTermExpress(m_FileMeta, nodeList, expressType);
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
                if( fmbt.BuildAST() )
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
        public bool isArray { get; set; } = false;

        private List<FileMetaSyntax> m_FileMetaAssignSyntaxList = new List<FileMetaSyntax>();
        private List<FileMetaCallLink> m_FileMetaCallLinkList = new List<FileMetaCallLink>();
        private Token m_BraceEndToken = null;
        private Node m_Node = null;
        // { a = 10, b = 20, c = Class1() }
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
                Console.WriteLine("Error FileMetaBraceTerm--");
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
                if (
                    c2node.nodeType == ENodeType.Comma
                    //|| c2node.nodeType == ENodeType.SemiColon
                    )
                {
                    nodeListList.Add(tempNodeList);
                    tempNodeList = new List<Node>();
                }
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
                    if (nl2.nodeType == ENodeType.Assign)
                    {
                        if (assignToken == null)
                        {
                            assignToken = nl2.token;
                            continue;
                        }
                        else
                        {
                            Console.WriteLine(" Errorr FileMetaBraceTerm.HandleBraceTerm 解析{ a = ?} 时，多个=号 Token: " + assignToken.ToLexemeAllString() );
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
                if( i == 0 )
                {
                    if (defineNodeList.Count == 1 && valueNodeList.Count == 0)
                    {
                        isArray = true;
                    }
                    else
                    {
                        isArray = false;
                    }
                }

                if(isArray)
                {
                    if (defineNodeList.Count >= 1 && valueNodeList.Count == 0)
                    {
                        FileMetaCallLink fmcl = new FileMetaCallLink(m_FileMeta, defineNodeList[0]);
                        fileMetaCallLinkList.Add(fmcl);
                    }
                    else
                    {
                        Console.WriteLine("Error 在解析为{}中，数组形式 解析有问题!!");
                        continue;
                    }
                }
                else
                {
                    if ( (defineNodeList.Count != 1 && defineNodeList.Count != 2 ) || valueNodeList.Count < 1)
                    {
                        Console.WriteLine("Error 在解析为{}中，赋值= 解析有问题!!");
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
                        FileMetaOpAssignSyntax fmoas = new FileMetaOpAssignSyntax(fmcl, assignToken, null, fmel, true);
                        fmoas.isAppendSemiColon = false;
                        m_FileMetaAssignSyntaxList.Add(fmoas);
                    }
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
            if( isArray )
            {
                for (int i = 0; i < m_FileMetaCallLinkList.Count; i++)
                {
                    stringBuilder.Append(m_FileMetaCallLinkList[i].ToFormatString());
                    if (i < m_FileMetaCallLinkList.Count - 1)
                        stringBuilder.Append(",");
                }
            }
            else
            {
                for (int i = 0; i < m_FileMetaAssignSyntaxList.Count; i++)
                {
                    stringBuilder.Append(m_FileMetaAssignSyntaxList[i].ToFormatString());
                    if (i < m_FileMetaAssignSyntaxList.Count - 1)
                        stringBuilder.Append(",");
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
    }
    public class FileMetaBracketTerm : FileMetaBaseTerm
    {
        public Token endToken => m_BracketeEndToken;

        Token m_BracketeEndToken = null;
        // = [1,2,3,4,A(),[1,2,3]]
        public FileMetaBracketTerm(FileMeta fm, Node node )
        {
            m_FileMeta = fm;
            m_Root = this;
            m_Token = node.token;
            m_BracketeEndToken = node.endToken;

            for( int i = 0; i < node.childList.Count; i++ )
            {
                var cnode = node.childList[i];
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
                }
                else if (cnode.nodeType == ENodeType.Par)
                {
                    Console.WriteLine("Error 不支持在[]中解析()的逻辑!!");
                    continue;
                }
                else if (cnode.nodeType == ENodeType.Key)
                {
                    Console.WriteLine("Error 不支持在[]中解析Key的逻辑!!");
                    continue;
                }
                else if (cnode.nodeType == ENodeType.Brace )
                {
                    var fileMetaBraceTerm = new FileMetaBraceTerm(m_FileMeta, cnode);
                    AddFileMetaTerm(fileMetaBraceTerm);
                    continue;
                }
                else
                {
                    var fileMetaCallTerm = new FileMetaCallTerm(m_FileMeta, node);
                    AddFileMetaTerm(fileMetaCallTerm);
                }
            }
        }
        // = [{a=20;b="aaa";},{a=30;b="ccc";}] 
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
                        Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
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
                        Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
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

            sb.Append("BeginBraceToken:" + m_Token?.ToLexemeAllString());
            sb.Append("EndBraceToken:" + m_BracketeEndToken?.ToLexemeAllString());

            return sb.ToString();
        }
    }
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
            Node tn = new Node( null );
            tn.childList = nodeList;
            StructParse.HandleLinkNode(tn);
            expressType = _expressType;

            CreateFileMetaExpressByChildList(tn.childList);
        }
        void CreateFileMetaExpressByChildList(List<Node> nodeList )
        {
            if (nodeList.Count == 0) return;
            FileMetaBaseTerm fmbt = null;
            //FileMetaCallLink fileMetaCallLink = null;
            for (int i = 0; i < nodeList.Count; i++)
            {
                var node = nodeList[i];
                if (node.nodeType == ENodeType.Symbol )
                {
                    FileMetaSymbolTerm fmn = new FileMetaSymbolTerm(m_FileMeta, node.token);
                    fmn.priority = node.priority;
                    AddFileMetaTerm(fmn);
                    fmbt = null;
                }
                else if( node.nodeType == ENodeType.ConstValue )
                {
                    if( node.extendLinkNodeList.Count > 0 )
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
                else if( node.nodeType == ENodeType.Key 
                    && (node.token?.type == ETokenType.This 
                    || node.token?.type == ETokenType.Base ) )
                {
                    if (fmbt != null)
                    {
                        Console.WriteLine("Error 表达式不允许多个自定义元素存在!!" + fmbt.ToTokenString());
                    }
                    fmbt = new FileMetaCallTerm(m_FileMeta, node);
                    fmbt.priority = int.MaxValue;
                    AddFileMetaTerm(fmbt);
                }
                else if( node.nodeType == ENodeType.IdentifierLink )
                {
                    if(fmbt != null )
                    {
                        Console.WriteLine("Error 表达式不允许多个自定义元素存在!!" + fmbt.ToTokenString() );
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
                else
                {
                    Console.WriteLine("没有找到该类型: " + node.token.type.ToString()  +  " 位置: "  + node.token.ToLexemeAllString() );
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
                        Console.WriteLine("Error 只有在语句中，可以使用i++ 等语法，变量与传参是禁止使用i++" +
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
                    else if( ett == ETokenType.Minus || ett == ETokenType.Not || ett == ETokenType.Negative )
                    {
                        if (listNextTerm == null)
                        {
                            Console.WriteLine("Error 表达式解析错误!! FileMetaExpress 575");
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
                        Console.WriteLine("Error 不能使用错误符号 !! FileMetaExpress 698" + currentTerm.token.ToLexemeAllString());
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
                        Console.WriteLine("Error BuildTst 表达式解析错误!! 604");
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
                Console.WriteLine("选择已经超出来范围!!");
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
                        if (i > 0 && i != buildASTList.Count - 1)
                        {
                            var fmst1 = buildASTList[i - 1] as FileMetaSymbolTerm;
                            if ((fmst1 != null && fmst1.priority == SignComputePriority.Level2_LinkOp)
                                || (fmst1 == null))
                            {
                                fmst.priority = SignComputePriority.Level3_Low_Compute;
                            }
                        }
                        else if (i < buildASTList.Count - 1 && i != 0)
                        {
                            var fmst1 = buildASTList[i + 1] as FileMetaSymbolTerm;
                            if ((fmst1 != null && fmst1.priority == SignComputePriority.Level2_LinkOp)
                                || (fmst1 == null))
                            {
                                fmst.priority = SignComputePriority.Level3_Low_Compute;
                            }
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
