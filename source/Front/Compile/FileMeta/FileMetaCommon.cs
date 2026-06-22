//****************************************************************************
//  File:      FileMetaCommon.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class NamespaceStatementBlock
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
        // nullable marker for type definitions (eg `int?`)
        // kept here to match previous behavior for FileMetaClassDefine constructor overload
        // but actual nullable handling is in FileMetaClassDefine.
        //public List<MetaNamespace> metaNamespaceList => m_MetaNamespaceList;
        protected List<Token> m_TokenList = new List<Token>();

        //protected List<MetaNamespace> m_MetaNamespaceList = new List<MetaNamespace>();
        //public MetaNamespace lastMetaNamespace
        //{
        //    get
        //    {
        //        if (m_MetaNamespaceList.Count <= 0) return null;
        //        return m_MetaNamespaceList[m_MetaNamespaceList.Count - 1];
        //    }
        //}

        protected NamespaceStatementBlock(List<Token> token)
        {
            m_TokenList = token;
        }
        //public void AddMetaNamespace( MetaNamespace metaNamespace)
        //{
        //    m_MetaNamespaceList.Add( metaNamespace );
        //}
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
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 命名空间有误，必须为X.xx.X 类似的格式!");
                        return null;
                    }
                }
                else
                {
                    if( token[i].type != ETokenType.Period )
                    {
                        Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 命名空间有误，必须为X.xx.X 类似的格式!");
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
        public int inputTemplateCount
        {
            get
            {
                int templateCount = 0;
                if (m_DefineClassCallLink != null)
                {
                    int cn = m_DefineClassCallLink.callNodeList.Count;
                    if( cn > 0 )
                    {
                        return m_DefineClassCallLink.callNodeList[cn - 1].inputTemplateNodeList.Count;
                    }
                }
                return templateCount;
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
        public bool isCallFunction => m_IsCallFunction;
        public bool isTemplate => m_IsTemplate;
        public bool isArray => m_FileMetaBracketTermList.Count > 0;
        public Token token => m_Token;
        public Token atToken => m_AtToken;
        public FileMeta fileMeta => m_FileMeta;
        public List<FileInputTemplateNode> inputTemplateNodeList => m_InputTemplateNodeList;
        public FileMetaParTerm fileMetaParTerm => m_FileMetaParTerm;
        public FileMetaBraceTerm fileMetaBraceTerm => m_FileMetaBraceTerm;
        public List<FileMetaBracketTerm> fileMetaBracketTermList => m_FileMetaBracketTermList;


        private bool m_IsCallFunction = false;
        private bool m_IsTemplate = false;
        private Token m_Token = null;
        private Token m_AtToken = null;
        private Token m_QuestionMarkDotToken = null;
        private FileMeta m_FileMeta = null;
        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaBraceTerm m_FileMetaBraceTerm = null;
        private Token m_BeginParToken = null;
        private Token m_EndParToken = null;
        private Token m_BeginAngleToken = null;
        private Token m_EndAngleToken = null;
        private List<FileMetaBracketTerm> m_FileMetaBracketTermList = new List<FileMetaBracketTerm>();
        private List<FileInputTemplateNode> m_InputTemplateNodeList = new List<FileInputTemplateNode>();//< template1,template2 >

        public Token questionMarkDotToken => m_QuestionMarkDotToken;
        public void SetQuestionMarkDotToken(Token t)
        {
            m_QuestionMarkDotToken = t;
        }

        public FileMetaCallNode( FileMeta fm, Node _node )
        {
            m_FileMeta = fm;

            m_Token = _node.token;
            m_AtToken = _node.atToken;

            if (_node.nodeType == ENodeType.Par)   // ( (20+(30.0f-x)), x  )
            {
                m_FileMetaParTerm = new FileMetaParTerm(m_FileMeta, _node, FileMetaTermExpress.EExpressType.Common);

                m_BeginParToken = m_FileMetaParTerm.token;
                m_EndParToken = m_FileMetaParTerm.endToken;
                m_FileMetaParTerm.ClearDirty();
                m_FileMetaParTerm.BuildAST();
            }
            if (_node.parNode != null)      //  Func( a, (b+20.0f) )
            {
                //Log.AddFileMetaLog( LID.ShowExtendMessage, m_FileMetaParTerm?.name + "已经有解析()" );

                m_IsCallFunction = true;
                m_FileMetaParTerm = new FileMetaParTerm(m_FileMeta, _node.parNode, FileMetaTermExpress.EExpressType.Common);

                m_BeginParToken = m_FileMetaParTerm.token;
                m_EndParToken = m_FileMetaParTerm.endToken;
                m_FileMetaParTerm.ClearDirty();
                m_FileMetaParTerm.BuildAST();
            }
            if (_node.angleNode != null)      // LinkCall.Call<int,string, NS.Class1>()
            {
                m_IsTemplate = true;
                m_BeginAngleToken = _node.angleNode.token;
                m_EndAngleToken = _node.angleNode.endToken;
                List<Node> list = _node.angleNode.childList;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].nodeType == ENodeType.Comma)
                    {
                        continue;
                    }
                    var aa = new FileInputTemplateNode(m_FileMeta, list[i]);
                    m_InputTemplateNodeList.Add(aa);
                }
            }
            for (int i = 0; i < _node.bracketNodeList.Count; i++) //[1][1][2][]
            {
                var fileMetaBracketTerm = new FileMetaBracketTerm(m_FileMeta, _node.bracketNodeList[i]);
                m_FileMetaBracketTermList.Add(fileMetaBracketTerm);
            }
            
            if (_node.blockNode != null)     // { 1,2,3,4 }  { [1,2,3], [2,3,4] }
            {
                m_FileMetaBraceTerm = new FileMetaBraceTerm(m_FileMeta, _node.blockNode);
            }
            // if this call node was created from a '?.' link, keep the token in m_QuestionMarkDotToken
            // note: in FileMetaCallLink.AddChildExtendLinkList we set this when constructing the call node
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
                    for( int i = 0; i < m_FileMetaBracketTermList.Count; i++ )
                    {
                        var fmbt = m_FileMetaBracketTermList[i];
                        sb.Append(fmbt.beginToken?.lexeme?.ToString());
                        for (int j = 0; j < fmbt?.fileMetaExpressList?.Count; j++)
                        {
                            sb.Append(fmbt?.fileMetaExpressList[j].ToFormatString());
                            if (j < fmbt?.fileMetaExpressList.Count - 1)
                                sb.Append(",");
                        }
                        sb.Append(fmbt.endToken?.lexeme?.ToString());
                    }
                }
                if (m_BeginAngleToken != null )
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
                if (m_BeginParToken != null)
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
        private List<FileMetaCallNode> m_CallNodeList = new List<FileMetaCallNode>();
        public FileMetaCallLink( FileMeta fm, Node node, bool isIncludeSelf = true )
        {
            m_FileMeta = fm;
            AddChildExtendLinkList(node, isIncludeSelf );
        }
        public FileMetaCallLink( FileMeta fm, List<Token> list )
        {
        }
        void AddChildExtendLinkList( Node cnode, bool isIncludeSelf )
        {
            List<Node> childNodeList = cnode.GetLinkNodeList( isIncludeSelf );
            for (int i = 0; i < childNodeList.Count; i++)
            {
                var cnode1 = childNodeList[i];

                // handle null-conditional link '?.' : represented as a Period node whose token.type == QuestionMarkDot
                if (cnode1.nodeType == ENodeType.Period && cnode1.token?.type == ETokenType.QuestionMarkDot)
                {
                    // next node should be the identifier target
                    if (i + 1 < childNodeList.Count)
                    {
                        var targetNode = childNodeList[i + 1];
                        FileMetaCallNode fmcn = new FileMetaCallNode(m_FileMeta, targetNode);
                        fmcn.SetQuestionMarkDotToken(cnode1.token);
                        m_CallNodeList.Add(fmcn);

                        // handle possible bracket children on target
                        if (targetNode.bracketNodeList.Count > 0)
                        {
                            var cnode2 = targetNode.bracketNodeList[targetNode.bracketNodeList.Count - 1];
                            if (cnode2.extendLinkNodeList.Count > 0)
                            {
                                AddChildExtendLinkList(cnode2, false);
                            }
                        }

                        // skip the next node because we've consumed it
                        i++;
                        continue;
                    }
                }

                FileMetaCallNode fmcn2 = new FileMetaCallNode(m_FileMeta, cnode1);
                m_CallNodeList.Add(fmcn2);

                if( cnode1.bracketNodeList.Count > 0 )
                {
                    var cnode2 = cnode1.bracketNodeList[cnode1.bracketNodeList.Count - 1];
                    if( cnode2.extendLinkNodeList.Count > 0 )
                    {
                        AddChildExtendLinkList(cnode2, false);
                    }
                }
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
        // nullable marker for type definitions (eg `int?`)
        public bool isNullable { get; set; } = false;
        public bool isInputTemplateData => m_IsInputTemplateData;
        public bool isArray { get; set; } = false;
        public List<FileInputTemplateNode> inputTemplateNodeList => m_InputTemplateNodeList;
        public List<FileMetaBracketTerm> fileMetaBracketTermList => m_FileMetaBracketTermList;
        public List<int> arrayDimsionLengthList => m_ArrayDimsionLengthList;

        private FileMeta m_FileMeta = null;
        private Token m_ClassNameToken = null;
        private Token m_AngleTokenBegin = null;
        private Token m_AngleTokenEnd = null;
        private List<FileInputTemplateNode> m_InputTemplateNodeList = new List<FileInputTemplateNode>();
        private List<FileMetaBracketTerm> m_FileMetaBracketTermList = new List<FileMetaBracketTerm>();
        private List<int> m_ArrayDimsionLengthList = new List<int>();

        private List<Token> m_TokenList = new List<Token>();
        private bool m_IsInputTemplateData = false;
        public FileMetaClassDefine(FileMeta fm, Node node) : this(fm, node, null)
        {
        }

        public FileMetaClassDefine(FileMeta fm, Node node, Node questionMark)
        {
            m_FileMeta = fm;
            // collect tokens that form the type reference (including dotted names)
            m_TokenList = node?.linkTokenList ?? new List<Token>();

            // support nullable type syntax like `T?` (question mark may be part of link tokens
            // or a separate token node immediately following this node in the parent's child list)
            if (m_TokenList.Count > 0 && m_TokenList[m_TokenList.Count - 1].type == ETokenType.QuestionMark)
            {
                isNullable = true;
                // remove trailing '?' token so subsequent logic sees the real type name
                m_TokenList.RemoveAt(m_TokenList.Count - 1);
            }
            else
            {
                // check if the parse tree contains an explicit '?' node immediately after this node
                if (node.parent != null)
                {
                    var siblings = node.parent.childList;
                    int idx = siblings.IndexOf(node);
                    if (idx >= 0 && idx + 1 < siblings.Count)
                    {
                        var next = siblings[idx + 1];
                        if (next.nodeType == ENodeType.QuestionMark && next.token?.type == ETokenType.QuestionMark)
                        {
                            isNullable = true;
                            // Note: we do not remove token from m_TokenList because it's not part of linkTokenList
                        }
                    }
                }
            }

            // class name token is the last token remaining in the token list
            if (m_TokenList.Count > 0)
                m_ClassNameToken = m_TokenList[m_TokenList.Count - 1];
            else
                m_ClassNameToken = null;

            if (node.angleNode != null)
            {
                m_IsInputTemplateData = true;
                m_AngleTokenBegin = node.angleNode.token;
                m_AngleTokenEnd = node.angleNode.endToken;
                for (int i = 0; i < node.angleNode.childList.Count; i++)
                {
                    var cnode = node.angleNode.childList[i];
                    if (cnode.nodeType == ENodeType.Comma)
                        continue;
                    FileInputTemplateNode fmcn = new FileInputTemplateNode(fm, cnode);
                    m_InputTemplateNodeList.Add(fmcn);
                }
            }
            if (node.bracketNodeList.Count > 0)
            {
                isArray = true;

                for (int i = 0; i < node.bracketNodeList.Count; i++)
                {
                    FileMetaBracketTerm fmbt = new FileMetaBracketTerm(m_FileMeta, node.bracketNodeList[i]);
                    m_FileMetaBracketTermList.Add(fmbt);
                }
                GetBracketListInt32Value();
            }
        }
        public MetaNode GetChildrenMetaNode(MetaNode mb)
        {
            if (mb == null) return null;
            MetaNode mb2 = mb;
            var list = stringList;
            for (int i = 0; i < list.Count; i++)
            {
                mb2 = mb2.GetChildrenMetaNodeByName(list[i]);
                if (mb2 == null)
                    break;
            }
            return mb2;
        }
        public void GetBracketListInt32Value()
        {
            m_ArrayDimsionLengthList.Clear();
            for (int i = 0; i < m_FileMetaBracketTermList.Count; i++)
            {
                var fmbtc = m_FileMetaBracketTermList[i];
                if (fmbtc.fileMetaExpressList.Count == 1)
                {
                    if (fmbtc.fileMetaExpressList[0] is FileMetaConstValueTerm fmcvt)
                    {
                        if (fmcvt.token?.type == ETokenType.Number)
                        {
                            int len = Convert.ToInt32(fmcvt.token.lexeme);
                            m_ArrayDimsionLengthList.Add(len);
                        }
                    }
                }
                else
                {
                    m_ArrayDimsionLengthList.Add(-1);
                }
            }
            if (m_ArrayDimsionLengthList.Count != m_FileMetaBracketTermList.Count)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "数组获取长度文件的时候，有异常!");
            }
        }
        public override string ToString()
        {
            return allName;
        }
        public string ToTokenString()
        {
            return allName + " Token File:[" + m_ClassNameToken.path + "] Line:[" + m_ClassNameToken.sourceBeginLine.ToString() + "]  Char:[" + m_ClassNameToken.sourceBeginChar.ToString() + "]";
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(allName);
            if (m_IsInputTemplateData)
            {
                sb.Append(m_AngleTokenBegin?.lexeme.ToString());

                for (int i = 0; i < m_InputTemplateNodeList.Count; i++)
                {
                    sb.Append(m_InputTemplateNodeList[i].ToFormatString());
                    if (i < m_InputTemplateNodeList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }

                sb.Append(m_AngleTokenEnd?.lexeme?.ToString());
            }
            if (isArray)
            {
                for (int i = 0; i < m_FileMetaBracketTermList.Count; i++)
                {
                    sb.Append(m_FileMetaBracketTermList[i].ToFormatString());
                }
            }
            return sb.ToString();
        }
    }
    public class FileMetaTemplateDefine : FileMetaBase
    {
        public Token covarianceToken => m_CovarianceToken;
        public FileInputTemplateNode inClassNameTemplateNode => m_InClassNameTemplateNode;

        public Node extendNode => m_ExtendsNode;

        private FileInputTemplateNode m_InClassNameTemplateNode = null;
        private Node m_Node = null;
        private Node m_ExtendsNode = null;
        private Token m_CovarianceToken = null;
        public FileMetaTemplateDefine(FileMeta fm, Node node)
        {
            m_FileMeta = fm;
            m_Node = node;
            m_Token = node.token;
            if(node.childList.Count > 0 )
            {
                m_ExtendsNode = node.childList[0];
            }
        }
        public FileMetaTemplateDefine(FileMeta fm, Node covarianceToken, Node node, Node extendsNode )
        {
            m_FileMeta = fm;
            m_Node = node;
            m_Token = node.token;
            m_CovarianceToken = covarianceToken?.token;
            m_ExtendsNode = extendsNode;
        }   
        public FileMetaTemplateDefine( FileMeta fm, List<Node> nodeList )
        {
            m_FileMeta = fm;
            if ( nodeList.Count == 0 )
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 在<>中没有发现元素!!");
                return;
            }
            m_Token = nodeList[0].token;
            //if( nodeList.Count == 2 )
            //{
            //    m_InToken = nodeList[1].token;
            //    m_InClassNameTemplateNode = new FileInputTemplateNode(fm, nodeList[2] );
            //}
            //else if( nodeList.Count == 2 )
            //{
            //    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 在<T in> or <T []> or <T ClassName> 使用方法不正确,请使用 <T in []>或者是 <T in ClassName> !!");
            //}
        }
        public void Parse()
        {

        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Token?.lexeme.ToString());
            if(m_CovarianceToken != null )
            {
                sb.Append( " " + m_CovarianceToken?.lexeme.ToString() + " ");
            }
            if(m_InClassNameTemplateNode != null )
            {
                sb.Append(m_InClassNameTemplateNode.ToFormatString());
            }
            //else if(m_InClassNameTokenList.Count > 1 )
            //{
            //    sb.Append("[");
            //    for (int i = 0; i < m_InClassNameTokenList.Count; i++)
            //    {
            //        sb.Append(m_InClassNameTokenList[i].ToFormatString());
            //        if (i < m_InClassNameTokenList.Count - 1)
            //        {
            //            sb.Append(",");
            //        }
            //    }
            //    sb.Append("]");
            //}
            return sb.ToString();
        }
    }
}
