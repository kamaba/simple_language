//****************************************************************************
//  File:      FileMetaCommon.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using SimpleLanguage.Compile.Parse;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public partial class NamespaceStatementBlock
    {
        private string m_NamespaceString = null;
        private List<string> m_NamespaceList = null;
        private List<string> m_NamespaceStackList = null;//层叠命名空间名称
        public string namespaceString
        {
            get
            {
                if (m_NamespaceString == null)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < m_TokenList.Count; i++)
                    {
                        sb.Append(m_TokenList[i].lexeme.ToString());
                        if (i != m_TokenList.Count - 1)
                        {
                            sb.Append(".");
                        }
                    }
                    m_NamespaceString = sb.ToString();
                }
                return m_NamespaceString;
            }
        }
        public List<string> namespaceStackList
        {
            get
            {
                if (m_NamespaceStackList == null)
                {
                    m_NamespaceStackList = new List<string>();
                    string add = "";
                    for (int i = 0; i < m_TokenList.Count; i++)
                    {
                        m_NamespaceStackList.Add(add + m_TokenList[i].lexeme.ToString());
                        if (string.IsNullOrEmpty(add))
                        {
                            add = m_TokenList[i].lexeme.ToString() + ".";
                        }
                        else
                        {
                            add = add + m_TokenList[i].lexeme.ToString() + ".";
                        }
                    }
                }
                return m_NamespaceStackList;
            }
        }
        public List<string> namespaceList
        {
            get
            {
                if (m_NamespaceList == null)
                {
                    m_NamespaceList = new List<string>();
                    for (int i = 0; i < m_TokenList.Count; i++)
                    {
                        m_NamespaceList.Add(m_TokenList[i].lexeme.ToString());
                    }
                }
                return m_NamespaceList;
            }
        }
        public List<Token> tokenList => m_TokenList;
        public List<MetaNamespace> metaNamespaceList => m_MetaNamespaceList;
        protected List<Token> m_TokenList = new List<Token>();

        protected List<MetaNamespace> m_MetaNamespaceList = new List<MetaNamespace>();
        public MetaNamespace lastMetaNamespace
        {
            get
            {
                if (m_MetaNamespaceList.Count <= 0) return null;
                return m_MetaNamespaceList[m_MetaNamespaceList.Count - 1];
            }
        }

        protected NamespaceStatementBlock(List<Token> token)
        {
            m_TokenList = token;
        }
        public void AddMetaNamespace( MetaNamespace metaNamespace)
        {
            m_MetaNamespaceList.Add( metaNamespace );
        }
        public static NamespaceStatementBlock CreateStateBlock( List<Token> token )
        {
            bool isIdentifier = true;
            List<Token> tokenList = new List<Token>();
            for (int i = 0; i < token.Count; i++)
            {
                if (isIdentifier)
                {
                    if (token[i].type == ETokenType.Identifier || token[i].type == ETokenType.Type )
                    {
                        tokenList.Add(token[i]);
                        isIdentifier = false;
                    }
                    else
                    {
                        Console.WriteLine("Error 命名空间有误，必须为X.xx.X 类似的格式!");
                        return null;
                    }
                }
                else
                {
                    if( token[i].type != ETokenType.Period )
                    {
                        Console.WriteLine("Error 命名空间有误，必须为X.xx.X 类似的格式!");
                        return null;
                    }
                    isIdentifier = true;
                }
            }

            NamespaceStatementBlock nsb = new NamespaceStatementBlock(tokenList);

            return nsb;
        }
        public override string ToString()
        {
            if (m_NamespaceString == null)
                return namespaceString;
            return m_NamespaceString;
        }
        public string ToFormatString()
        {
            return ToString();
        }
    }    
    public class FileInputParamNode
    {
        public FileMetaBaseTerm express => m_Express;

        private FileMetaBaseTerm m_Express = null;     
        public FileInputParamNode( FileMetaBaseTerm fmbt )
        {
            m_Express = fmbt;
            m_Express.ClearDirty();
            m_Express.BuildAST();
        }
        public string ToFormatString()
        {
            return m_Express?.ToFormatString();
        }
    }
    public class FileInputTemplateNode
    {
        public FileMeta fileMeta => m_FileMeta;
        public FileMetaCallLink defineClassCallLink => m_DefineClassCallLink;
        public List<string> nameList
        {
            get
            {
                List<String> _nameList = new List<string>();
                if(m_DefineClassCallLink != null )
                {
                    for( int i = 0; i < m_DefineClassCallLink.callNodeList.Count; i++ )
                    {
                        _nameList.Add(m_DefineClassCallLink.callNodeList[i].name);
                    }
                }
                return _nameList;
            }
        }

        private FileMetaCallLink m_DefineClassCallLink;
        private FileMeta m_FileMeta = null;
        private Node m_Node = null;
        public FileInputTemplateNode( FileMeta fm, Node node )
        {
            m_FileMeta = fm;
            m_Node = node;
            m_DefineClassCallLink = new FileMetaCallLink(fm, node);
        }
        public string ToFormatString()
        {
            return m_DefineClassCallLink?.ToFormatString();
        }
    }
    public class FileMetaCallNode
    {
        public string name
        {
            get
            {
                return m_Token?.lexeme.ToString();
            }
        }
        public bool isBrace => m_FileMetaBraceTerm != null;
        public List<FileInputTemplateNode> inputTemplateNodeList => m_InputTemplateNodeList;
        public FileMetaParTerm fileMetaParTerm => m_FileMetaParTerm;
        public FileMetaBraceTerm fileMetaBraceTerm => m_FileMetaBraceTerm;
        public FileMetaBracketTerm fileMetaBracketTerm => m_FileMetaBracketTerm;
        public List<FileMetaCallLink> arrayNodeList => m_ArrayNodeList;

        public Token token => m_Token;
        public Token atToken => m_AtToken;
        public bool isCallFunction { get; set; } = false;
        public bool isTemplate { get; set; } = false;
        public bool isArray { get; set; } = false;
        public FileMeta fileMeta => m_FileMeta;

        private Node m_Node = null;
        private Token m_Token = null;
        private Token m_AtToken = null;
        private FileMeta m_FileMeta = null;
        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaBraceTerm m_FileMetaBraceTerm = null;
        private FileMetaBracketTerm m_FileMetaBracketTerm = null;
        private Token m_BeginParToken = null;
        private Token m_EndParToken = null;
        private Token m_BeginAngleToken = null;
        private Token m_EndAngleToken = null;
        private Token m_BeginBracketToken = null;
        private Token m_EndBracketToken = null;
        private List<FileInputTemplateNode> m_InputTemplateNodeList = new List<FileInputTemplateNode>();//< template1,template2 >
        private List<FileMetaCallLink> m_ArrayNodeList = new List<FileMetaCallLink>();// [calllink1, calllink2 ]

        public FileMetaCallNode( FileMeta fm, Node _node )
        {
            m_FileMeta = fm;
            m_Node = _node;
            isCallFunction = false;

            CreateFileMetaCallNode();
        }
        void CreateFileMetaCallNode()
        {
            m_Token = m_Node.token;
            m_AtToken = m_Node.atToken;

            if( m_Node.nodeType == ENodeType.Par )
            {
                m_FileMetaParTerm = new FileMetaParTerm(m_FileMeta, m_Node, FileMetaTermExpress.EExpressType.Common);

                m_BeginParToken = m_FileMetaParTerm.token;
                m_EndParToken = m_FileMetaParTerm.endToken;
            }
            if(m_Node.parNode != null )
            {
                isCallFunction = true;

                m_FileMetaParTerm = new FileMetaParTerm(m_FileMeta, m_Node.parNode, FileMetaTermExpress.EExpressType.Common);

                m_BeginParToken = m_FileMetaParTerm.token;
                m_EndParToken = m_FileMetaParTerm.endToken;
            }
            if ( m_Node.angleNode != null )      // LinkCall.Call<int,string, NS.Class1>()
            {
                isTemplate = true;
                m_BeginAngleToken = m_Node.angleNode.token;
                m_EndAngleToken = m_Node.angleNode.endToken;
                List<Node> list = m_Node.angleNode.childList;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].nodeType == ENodeType.Comma)
                    {                       
                        continue;
                    }
                    var aa = new FileInputTemplateNode(m_FileMeta, list[i] );
                    m_InputTemplateNodeList.Add(aa);
                }
            }
            if( m_Node.bracketNode != null )     //[1,2,3,4]
            {
                isArray = true;
                m_FileMetaBracketTerm = new FileMetaBracketTerm(m_FileMeta, m_Node);
                m_BeginBracketToken = m_FileMetaBracketTerm.token;
                m_EndBracketToken = m_FileMetaBracketTerm.endToken;
                
                List<Node> list = m_Node.bracketNode.childList;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].nodeType == ENodeType.Comma)
                    {
                        if( i == list.Count -1 )
                        {
                            Console.WriteLine("Warning [1,2,3,]有多余逗号出现??");
                        }
                        continue;
                    }
                    var aa = new FileMetaCallLink(m_FileMeta, list[i]);
                    m_ArrayNodeList.Add(aa);
                }
                
            }
            if (m_Node.blockNode != null)     // { 1,2,3,4 }
            {
                m_FileMetaBraceTerm = new FileMetaBraceTerm(m_FileMeta, m_Node.blockNode );
            }
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_FileMetaParTerm != null)
            {
                sb.Append(token?.lexeme.ToString());
                sb.Append(m_FileMetaParTerm.ToFormatString());
                if (m_FileMetaBraceTerm != null)
                {
                    sb.Append(m_FileMetaBraceTerm.ToFormatString());
                }
            }
            else if(m_FileMetaBracketTerm != null )
            {
                sb.Append(m_FileMetaBracketTerm.ToFormatString());
            }
            else if( m_FileMetaBraceTerm != null )
            {
                sb.Append(m_FileMetaBraceTerm.ToFormatString());
            }
            else
            {
                sb.Append(m_AtToken?.lexeme.ToString());
                sb.Append(token?.ToConstString());
                if(isArray )
                {
                    sb.Append(m_BeginBracketToken.lexeme?.ToString());
                    for (int i = 0; i < m_ArrayNodeList.Count; i++)
                    {
                        sb.Append(m_ArrayNodeList[i].ToFormatString());
                        if (i < m_ArrayNodeList.Count - 1)
                            sb.Append(",");
                    }                    
                    sb.Append(m_EndBracketToken.lexeme?.ToString());
                }
                if (isTemplate)
                {
                    sb.Append( m_BeginAngleToken.lexeme?.ToString());
                    for (int i = 0; i < m_InputTemplateNodeList.Count; i++)
                    {
                        sb.Append(m_InputTemplateNodeList[i].ToFormatString());
                        if (i < m_InputTemplateNodeList.Count - 1)
                            sb.Append(",");
                    }
                    sb.Append( m_EndAngleToken.lexeme?.ToString());
                }
                if (isCallFunction)
                {
                    sb.Append( m_BeginParToken?.lexeme.ToString());
                    //for (int i = 0; i < m_ParamList.Count; i++)
                    //{
                    //    sb.Append(" " + m_ParamList[i].ToFormatString());
                    //    if (i < m_ParamList.Count - 1)
                    //        sb.Append(",");
                    //    else
                    //        sb.Append(" ");
                    //}
                    sb.Append( m_EndParToken?.lexeme.ToString());
                }
                if (m_FileMetaBraceTerm != null)
                {
                    sb.Append(m_FileMetaBraceTerm.ToFormatString());
                }
            }
            return sb.ToString();
        }
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_FileMetaParTerm != null)
            {
                //sb.Append(token?.lexeme.ToString());
                sb.Append(m_FileMetaParTerm.ToFormatString());
                //if (m_FileMetaBlockTermExpress != null)
                //{
                //    sb.Append(m_FileMetaBlockTermExpress.ToFormatString());
                //}
            }
            else
            {
                sb.Append(token?.lexeme.ToString());
                sb.Append("在文件:" + token?.path + " 行: " + token?.sourceBeginLine + " 位置: " + token.sourceBeginChar);
                if (isCallFunction)
                {
                    sb.Append( m_BeginParToken?.lexeme.ToString());
                    //for (int i = 0; i < m_ParamList.Count; i++)
                    //{
                    //    sb.Append(" " + m_ParamList[i].ToFormatString());
                    //    if (i < m_ParamList.Count - 1)
                    //        sb.Append(",");
                    //    else
                    //        sb.Append(" ");
                    //}
                    sb.Append( m_EndParToken?.lexeme.ToString());
                }
                if (m_FileMetaBraceTerm != null)
                {
                    sb.Append(m_FileMetaBraceTerm.ToFormatString());
                }
            }
            return sb.ToString();
        }
    }
    public class FileMetaCallLink
    {
        public bool isOnlyName
        {
            get
            {
                if (m_CallNodeList.Count == 1)
                {
                    if (m_CallNodeList[0].isCallFunction == false)
                        return true;
                }
                return false;
            }
        }
        public string name
        {
            get
            {
                if( m_CallNodeList.Count >= 1 )
                {
                    return m_CallNodeList[0].name;
                }
                return "";
            }
        }
        public List<FileMetaCallNode> callNodeList => m_CallNodeList;

        private FileMeta m_FileMeta = null;
        private Node m_Node = null;
        private List<FileMetaCallNode> m_CallNodeList = new List<FileMetaCallNode>();
        public FileMetaCallLink( FileMeta fm, Node node )
        {
            m_Node = node;
            m_FileMeta = fm;
            AddChildExtendLinkList(m_Node, true);
        }
        void AddChildExtendLinkList( Node cnode, bool isIncludeSelf )
        {
            List<Node> childNodeList = cnode.GetLinkNodeList( isIncludeSelf );
            for (int i = 0; i < childNodeList.Count; i++)
            {
                var cnode1 = childNodeList[i];
                FileMetaCallNode fmcn = new FileMetaCallNode(m_FileMeta, cnode1);
                m_CallNodeList.Add(fmcn);
                if( i == childNodeList.Count - 1 )
                {
                    if( cnode1.extendLinkNodeList.Count > 0 )
                    {
                        AddChildExtendLinkList(cnode1, false );
                    }
                }
            }
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                sb.Append(m_CallNodeList[i].ToFormatString());
            }
            return sb.ToString();
        }
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < m_CallNodeList.Count; i++)
            {
                sb.Append(m_CallNodeList[i].ToTokenString());
            }
            return sb.ToString();
        }
    }    
    public class FileMetaClassDefine
    {
        public List<string> stringList
        {
            get
            {
                return FileMetatUtil.GetLinkStringMidPeriodList(m_TokenList);
            }
        }
        public string allName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < m_TokenList.Count; i++)
                {
                    sb.Append(m_TokenList[i]?.lexeme?.ToString());
                }
                return sb.ToString();
            }
        }
        public string name
        {
            get
            {
                if (m_ClassNameToken != null)
                {
                    return m_ClassNameToken.lexeme.ToString();
                }
                return "";
            }
        }

        public FileMeta fileMeta => m_FileMeta;
        public Token classNameToken => m_ClassNameToken;
        public bool isInputTemplateData => m_IsInputTemplateData;
        public bool isArray { get; set; } = false;
        public List<FileInputTemplateNode> inputTemplateNodeList => m_InputTemplateNodeList;
        public List<Token> arrayTokenList => m_ArrayTokenList;

        private FileMeta m_FileMeta = null;
        private Token m_ClassNameToken = null;
        private Token m_AngleTokenBegin = null;
        private Token m_AngleTokenEnd = null;
        private Token m_BracketTokenBegin = null;
        private Token m_BracketTokenEnd = null;
        private List<Token> m_ArrayTokenList = new List<Token>();
        private List<FileInputTemplateNode> m_InputTemplateNodeList = new List<FileInputTemplateNode>();

        private List<Token> m_TokenList = new List<Token>();
        private bool m_IsInputTemplateData = false;
        public FileMetaClassDefine( FileMeta fm, List<Token> _tokenList, Node angleNode = null )
        {
            m_FileMeta = fm;
            m_TokenList = _tokenList;
            m_ClassNameToken = _tokenList[_tokenList.Count-1];
            if (angleNode != null)
            {
                m_IsInputTemplateData = true;
                m_AngleTokenBegin = angleNode.token;
                m_AngleTokenEnd = angleNode.endToken;
                for (int i = 0; i < angleNode.childList.Count; i++)
                {
                    var cnode = angleNode.childList[i];
                    if (cnode.nodeType == ENodeType.Comma)
                        continue;
                    FileInputTemplateNode fmcn = new FileInputTemplateNode(fm, cnode);
                    m_InputTemplateNodeList.Add(fmcn);
                }
            }
        }
        public FileMetaClassDefine( FileMeta fm, Node node )
        {
            m_FileMeta = fm;
            m_TokenList = node.linkTokenList;
            m_ClassNameToken = m_TokenList[m_TokenList.Count - 1];

            if( node.angleNode != null )
            {
                m_IsInputTemplateData = true;
                m_AngleTokenBegin = node.angleNode.token;
                m_AngleTokenEnd = node.angleNode.endToken;
                for( int i = 0; i < node.angleNode.childList.Count; i++ )
                {
                    var cnode = node.angleNode.childList[i];
                    if (cnode.nodeType == ENodeType.Comma )
                        continue;
                    FileInputTemplateNode fmcn = new FileInputTemplateNode(fm, cnode);
                    m_InputTemplateNodeList.Add(fmcn);
                }
            }
            if( node.bracketNode != null )
            {
                isArray = true;
                m_BracketTokenBegin = node.bracketNode.token;
                m_BracketTokenEnd = node.bracketNode.endToken;

                Token frontToken = m_BracketTokenBegin;
                var cl = node.bracketNode.childList;
                if ( cl.Count == 0 )
                {
                    var token = new Token(frontToken);
                    token.SetLexeme(-1, ETokenType.Number );
                    token.SetExtend(EType.Int32);
                    m_ArrayTokenList.Add(token);
                }
                else
                {
                    List<Token> tlist = new List<Token>();
                    bool isOnlyComma = true;
                    for (int i = 0; i < cl.Count; i++)
                    {
                        var cnode = cl[i];
                        if (cnode.nodeType == ENodeType.Comma)
                        {
                            if( isOnlyComma )
                            {
                                Token t = new Token(cnode.token);
                                t.SetLexeme(-1, ETokenType.Number);
                                t.SetExtend(EType.Int32);
                                m_ArrayTokenList.Add(t);
                            }
                        }
                        else
                        {
                            isOnlyComma = false;
                            m_ArrayTokenList.Add(cnode.token);
                        }
                    }
                }

            }
        }
        public MetaBase GetChildrenMetaBase(MetaBase mb)
        {
            if (mb == null) return null;
            MetaBase mb2 = mb;
            var list = stringList;
            for (int i = 0; i < list.Count; i++)
            {
                mb2 = mb2.GetChildrenMetaBaseByName(list[i]);
                if (mb2 == null)
                    break;
            }
            return mb2;
        }       
        public string ToTokenString()
        {
            return allName + " Token File:[" + m_ClassNameToken.path + "] Line:[" + m_ClassNameToken.sourceBeginLine.ToString() + "]  Char:[" + m_ClassNameToken.sourceBeginChar.ToString() + "]";
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(allName);
            if(m_IsInputTemplateData)
            {
                sb.Append(m_AngleTokenBegin?.lexeme.ToString());

                for( int i = 0; i < m_InputTemplateNodeList.Count; i++ )
                {
                    sb.Append(m_InputTemplateNodeList[i].ToFormatString());
                    if( i < m_InputTemplateNodeList.Count - 1 )
                    {
                        sb.Append(",");
                    }
                }

                sb.Append(m_AngleTokenEnd?.lexeme?.ToString());
            }
            if( isArray )
            {
                sb.Append(m_BracketTokenBegin?.lexeme.ToString());
                for( int i = 0; i < m_ArrayTokenList.Count; i++ )
                {
                    sb.Append(m_ArrayTokenList[i].lexeme.ToString());
                    if (i < m_ArrayTokenList.Count - 1)
                        sb.Append(",");
                }
                sb.Append(m_BracketTokenEnd?.lexeme.ToString());
            }
            return sb.ToString();
        }
        public void AddError(int errorId)
        {
            Console.WriteLine(" CheckExtendAndInterface 在判断接口的时候，发没的:" + allName + "  类");
        }
        public void AddError2(int errorId, [CallerFilePath] string pfile = "", [CallerMemberName] string pfunction = "",
            [CallerLineNumber] int line = 0)
        {
            string str = "";
            switch (errorId)
            {
                case 0:
                    {
                        str = "判断接口的时候没有发现[" + allName + "]类";
                    }
                    break;
                default: break;
            }
            str = str + " \n Token: 在文件:" + m_ClassNameToken.path + " 开始行号:" + m_ClassNameToken.sourceBeginLine.ToString() + "开始位置: "
                + m_ClassNameToken.sourceBeginChar.ToString();
            str = str + " \n 在代码中文件:" + pfile + "   函数:" + pfunction + "行号: " + line.ToString();
            //Trace.WriteLine( "" )
            Console.WriteLine(str);
        }
    }
    public class FileMetaTemplateDefine : FileMetaBase
    {
        public Token inToken => m_InToken;
        public Node getNode => m_Node;
        public List<FileInputTemplateNode> inClassNameTokenList => m_InClassNameTokenList;

        private Token m_InToken = null;
        private List<FileInputTemplateNode> m_InClassNameTokenList = new List<FileInputTemplateNode>();
        private Node m_Node = null;
        public FileMetaTemplateDefine( FileMeta fm, Node node )
        {
            m_FileMeta = fm;
            m_Node = node;
            m_Token = node.token;
        }
        public FileMetaTemplateDefine( FileMeta fm, List<Node> nodeList )
        {
            m_FileMeta = fm;
            if ( nodeList.Count == 0 )
            {
                Console.WriteLine("Error 在<>中没有发现元素!!");
                return;
            }
            m_Token = nodeList[0].token;
            if( nodeList.Count > 2 )
            {
                m_InToken = nodeList[1].token;
                for( int i = 2; i <= nodeList.Count; i++ )
                {
                    FileInputTemplateNode fitn = new FileInputTemplateNode(fm, nodeList[i]);
                    m_InClassNameTokenList.Add(fitn);
                }
            }
            else if( nodeList.Count == 2 )
            {
                Console.WriteLine("Error 在<T in> or <T []> or <T ClassName> 使用方法不正确,请使用 <T in []>或者是 <T in ClassName> !!");
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Token?.lexeme.ToString());
            if( m_InToken != null )
            {
                sb.Append( " " + m_InToken?.lexeme.ToString() + " ");
            }
            if(m_InClassNameTokenList.Count == 1 )
            {
                sb.Append(m_InClassNameTokenList[0].ToFormatString());
            }
            else if(m_InClassNameTokenList.Count > 1 )
            {
                sb.Append("[");
                for (int i = 0; i < m_InClassNameTokenList.Count; i++)
                {
                    sb.Append(m_InClassNameTokenList[i].ToFormatString());
                    if (i < m_InClassNameTokenList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
            }
            return sb.ToString();
        }
    }
}
