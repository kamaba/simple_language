//****************************************************************************
//  File:      FileMetaCommon.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using SimpleLanguage.Parse;

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
                        Log.AddInStructFileMeta(EError.None, "Error 命名空间有误，必须为X.xx.X 类似的格式!");
                        return null;
                    }
                }
                else
                {
                    if( token[i].type != ETokenType.Period )
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 命名空间有误，必须为X.xx.X 类似的格式!");
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
        public List<FileInputTemplateNode> inputTemplateNodeList => m_InputTemplateNodeList;
        public FileMetaParTerm fileMetaParTerm => m_FileMetaParTerm;
        public FileMetaBraceTerm fileMetaBraceTerm => m_FileMetaBraceTerm;
        public List<FileMetaBracketTerm> fileMetaBracketTermList => m_FileMetaBracketTermList;

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
        private Token m_BeginParToken = null;
        private Token m_EndParToken = null;
        private Token m_BeginAngleToken = null;
        private Token m_EndAngleToken = null;
        private List<FileMetaBracketTerm> m_FileMetaBracketTermList = new List<FileMetaBracketTerm>();
        private List<FileInputTemplateNode> m_InputTemplateNodeList = new List<FileInputTemplateNode>();//< template1,template2 >

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

            // 处理括号参数：Level<T>() 的 () 部分
            if( m_Node.nodeType == ENodeType.Par )
            {
                m_FileMetaParTerm = new FileMetaParTerm(m_FileMeta, m_Node, FileMetaTermExpress.EExpressType.Common);
                m_BeginParToken = m_FileMetaParTerm.token;
                m_EndParToken = m_FileMetaParTerm.endToken;
            }
            
            // 处理函数调用：Level<T>.Method() 中的 () 部分
            if(m_Node.parNode != null )
            {
                isCallFunction = true;
                m_FileMetaParTerm = new FileMetaParTerm(m_FileMeta, m_Node.parNode, FileMetaTermExpress.EExpressType.Common);
                m_BeginParToken = m_FileMetaParTerm.token;
                m_EndParToken = m_FileMetaParTerm.endToken;
            }
            
            // 关键修复：angleNode 必须完整保留，以支持 List<int>、Level<T>() 等泛型调用
            // angleNode 是 < 和 > 包围的泛型参数部分，不应被合并到其他节点中
            if (m_Node.angleNode != null)
            {
                isTemplate = true;
                m_BeginAngleToken = m_Node.angleNode.token;
                m_EndAngleToken = m_Node.angleNode.endToken;
                
                // 遍历 angleNode 内的所有子节点（不包括逗号）
                List<Node> list = m_Node.angleNode.childList;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].nodeType == ENodeType.Comma)
                    {
                        continue;
                    }
                    // 为每个泛型参数创建 FileInputTemplateNode
                    var aa = new FileInputTemplateNode(m_FileMeta, list[i]);
                    m_InputTemplateNodeList.Add(aa);
                }
            }
            
            // 处理数组维度：Array[1][2] 中的 [] 部分
            if ( m_Node.bracketNode != null )
            {
                isArray = true;
                for( int i = 0; i < m_Node.bracketNodeList.Count; i++ )
                {
                    var fileMetaBracketTerm = new FileMetaBracketTerm(m_FileMeta, m_Node.bracketNodeList[i] );
                    m_FileMetaBracketTermList.Add(fileMetaBracketTerm);
                }           
            }
            
            // 处理初始化块：{...} 部分
            if (m_Node.blockNode != null)
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
                            if (i < fmbt?.fileMetaExpressList.Count - 1)
                                sb.Append(",");
                        }
                        sb.Append(fmbt.endToken?.lexeme?.ToString());
                    }
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
        private Node m_Node = null;  // 保留以支持向后兼容
        private List<Token> m_TokenList = null;  // Token 版本
        private List<FileMetaCallNode> m_CallNodeList = new List<FileMetaCallNode>();

        // Node 版本构造方法（保留向后兼容）
        public FileMetaCallLink( FileMeta fm, Node node, bool isIncludeSelf = true )
        {
            m_Node = node;
            m_FileMeta = fm;
            AddChildExtendLinkList(m_Node, isIncludeSelf );
        }

        // Token 版本构造方法（新）
        public FileMetaCallLink(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_TokenList = tokenList ?? new List<Token>();
            BuildFromTokenList(m_TokenList);
        }

        // 从 Token 列表构建 CallNode 链
        private void BuildFromTokenList(List<Token> tokenList)
        {
            if (tokenList == null || tokenList.Count == 0)
                return;

            // 按点号（Period）拆分 token 序列，构建链式调用
            // 例如：a.b.c() 拆成 [a] [b] [c()]
            List<List<Token>> callSegments = new List<List<Token>>();
            List<Token> currentSegment = new List<Token>();

            int parenDepth = 0;
            int angleDepth = 0;
            int bracketDepth = 0;

            for (int i = 0; i < tokenList.Count; i++)
            {
                var t = tokenList[i];

                // 追踪括号深度，避免在嵌套中误认为是分隔符
                if (t.type == ETokenType.LeftPar) parenDepth++;
                else if (t.type == ETokenType.RightPar && parenDepth > 0) parenDepth--;
                else if (t.type == ETokenType.Less) angleDepth++;
                else if (t.type == ETokenType.Greater && angleDepth > 0) angleDepth--;
                else if (t.type == ETokenType.LeftBracket) bracketDepth++;
                else if (t.type == ETokenType.RightBracket && bracketDepth > 0) bracketDepth--;

                // 顶层点号标志分隔
                if (t.type == ETokenType.Period && parenDepth == 0 && angleDepth == 0 && bracketDepth == 0)
                {
                    if (currentSegment.Count > 0)
                    {
                        callSegments.Add(new List<Token>(currentSegment));
                        currentSegment.Clear();
                    }
                }
                else
                {
                    currentSegment.Add(t);
                }
            }

            // 添加最后一段
            if (currentSegment.Count > 0)
            {
                callSegments.Add(currentSegment);
            }

            // 为每一段创建 FileMetaCallNode
            for (int i = 0; i < callSegments.Count; i++)
            {
                var segmentTokens = callSegments[i];
                if (segmentTokens.Count == 0) continue;

                // 简单版：直接用第一个 token 作为名称，后续可扩展
                // 对应 a、b、c 等标识符，或 c() 等函数调用
                var callNode = CreateCallNodeFromTokens(m_FileMeta, segmentTokens);
                if (callNode != null)
                {
                    m_CallNodeList.Add(callNode);
                }
            }
        }

        // 从一组 token 创建单个 FileMetaCallNode
        private FileMetaCallNode CreateCallNodeFromTokens(FileMeta fm, List<Token> segmentTokens)
        {
            if (segmentTokens == null || segmentTokens.Count == 0)
                return null;

            // 构造临时 Node，用于兼容现有 FileMetaCallNode 的逻辑
            // 这里我们创建一个简单的代理 Node
            Token nameToken = segmentTokens[0];
            Node proxyNode = new Node(nameToken)
            {
                nodeType = ENodeType.IdentifierLink
            };

            // 扫描是否有括号、角度、方括号、大括号
            int parenStart = -1, angleStart = -1, bracketStart = -1, braceStart = -1;

            for (int i = 1; i < segmentTokens.Count; i++)
            {
                if (segmentTokens[i].type == ETokenType.LeftPar && parenStart == -1)
                    parenStart = i;
                else if (segmentTokens[i].type == ETokenType.Less && angleStart == -1)
                    angleStart = i;
                else if (segmentTokens[i].type == ETokenType.LeftBracket && bracketStart == -1)
                    bracketStart = i;
                else if (segmentTokens[i].type == ETokenType.LeftBrace && braceStart == -1)
                    braceStart = i;
            }

            // 简化：当前版本只支持基本名称或带括号的函数调用
            // 泛型、数组、初始化块的支持需要扩展 FileMetaCallNode 的 Token 版构造
            // 或在这里构造合适的 proxy Node

            return new FileMetaCallNode(fm, proxyNode);
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
        public List<FileMetaBracketTerm> fileMetaBracketTermList => m_FileMetaBracketTermList;
        public List<int> arrayDimsionLengthList => m_ArrayDimsionLengthList;

        private FileMeta m_FileMeta = null;
        private Token m_ClassNameToken = null;
        private Token m_AngleTokenBegin = null;
        private Token m_AngleTokenEnd = null;
        private Token m_MutToken = null;
        private List<FileInputTemplateNode> m_InputTemplateNodeList = new List<FileInputTemplateNode>();
        private List<FileMetaBracketTerm> m_FileMetaBracketTermList = new List<FileMetaBracketTerm>();
        private List<int> m_ArrayDimsionLengthList = new List<int>();

        private List<Token> m_TokenList = new List<Token>();
        private List<Token> m_AngleTokenList = new List<Token>();  // < ... > 中的 token 列表
        private List<List<Token>> m_BracketTokenListList = new List<List<Token>>();  // [ ... ] 中的 token 列表
        private bool m_IsInputTemplateData = false;

        // Token 版本构造方法（完整逻辑）
        public FileMetaClassDefine(FileMeta fm, List<Token> _tokenList)
        {
            m_FileMeta = fm;
            m_TokenList = _tokenList ?? new List<Token>();
            
            if (m_TokenList.Count > 0)
            {
                m_ClassNameToken = m_TokenList[m_TokenList.Count - 1];
            }

            // 处理泛型模板：< ... >
            ExtractAndProcessGenericTemplate();

            // 处理数组维度：[ ... ]
            ExtractAndProcessArrayDimensions();
        }

        // Node 版本构造方法（保留向后兼容）
        public FileMetaClassDefine(FileMeta fm, Node node, Node mutNode = null)
        {
            m_FileMeta = fm;
            m_TokenList = node.linkTokenList;
            m_ClassNameToken = m_TokenList[m_TokenList.Count - 1];
            m_MutToken = mutNode?.token;

            // 关键修复：angleNode 必须完整保留，无论是否作为 linkTokenList 的一部分
            // 这样可以正确处理 List<int>、Level<T>() 等泛型调用
            if (node.angleNode != null)
            {
                m_IsInputTemplateData = true;
                m_AngleTokenBegin = node.angleNode.token;
                m_AngleTokenEnd = node.angleNode.endToken;

                // 遍历angleNode内的所有子节点，构建FileInputTemplateNode
                List<Node> list = node.angleNode.childList;
                for (int i = 0; i < list.Count; i++)
                {
                    var cnode = list[i];
                    if (cnode.nodeType == ENodeType.Comma)
                        continue;

                    // 为每个泛型参数创建FileInputTemplateNode
                    FileInputTemplateNode fmcn = new FileInputTemplateNode(fm, cnode);
                    m_InputTemplateNodeList.Add(fmcn);
                }
            }

            // 处理数组维度（如果有）
            if (node.bracketNode != null)
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

        // 从 Token 列表中提取并处理泛型模板 < ... >
        private void ExtractAndProcessGenericTemplate()
        {
            // 在 m_TokenList 中查找 < 和 > 的配对
            // 假设格式类似于：List < int >，我们需要提取 < int >

            int angleStart = -1;
            int angleEnd = -1;
            int depth = 0;

            for (int i = 0; i < m_TokenList.Count; i++)
            {
                if (m_TokenList[i].type == ETokenType.Less)
                {
                    if (depth == 0)
                        angleStart = i;
                    depth++;
                }
                else if (m_TokenList[i].type == ETokenType.Greater)
                {
                    depth--;
                    if (depth == 0)
                    {
                        angleEnd = i;
                        break;
                    }
                }
            }

            // 如果找到完整的 < ... > 对
            if (angleStart != -1 && angleEnd != -1 && angleStart < angleEnd)
            {
                m_IsInputTemplateData = true;
                m_AngleTokenBegin = m_TokenList[angleStart];
                m_AngleTokenEnd = m_TokenList[angleEnd];

                // 提取 < 和 > 之间的 token
                m_AngleTokenList = m_TokenList.GetRange(angleStart + 1, angleEnd - angleStart - 1);

                // 按逗号拆分泛型参数
                List<List<Token>> templateParams = SplitTokensByComma(m_AngleTokenList);

                // 为每个泛型参数创建 FileInputTemplateNode
                foreach (var paramTokens in templateParams)
                {
                    if (paramTokens.Count > 0)
                    {
                        // 构造临时 Node 用于兼容 FileInputTemplateNode
                        Node paramNode = new Node(paramTokens[0]) { nodeType = ENodeType.IdentifierLink };
                        FileInputTemplateNode fmcn = new FileInputTemplateNode(m_FileMeta, paramNode);
                        m_InputTemplateNodeList.Add(fmcn);
                    }
                }
            }
        }

        // 从 Token 列表中提取并处理数组维度 [ ... ]
        private void ExtractAndProcessArrayDimensions()
        {
            // 查找所有 [ ... ] 对
            int bracketStart = -1;
            int depth = 0;

            for (int i = 0; i < m_TokenList.Count; i++)
            {
                if (m_TokenList[i].type == ETokenType.LeftBracket)
                {
                    if (depth == 0)
                        bracketStart = i;
                    depth++;
                }
                else if (m_TokenList[i].type == ETokenType.RightBracket)
                {
                    depth--;
                    if (depth == 0 && bracketStart != -1)
                    {
                        // 找到一个完整的 [ ... ] 对
                        isArray = true;
                        var bracketTokens = m_TokenList.GetRange(bracketStart + 1, i - bracketStart - 1);
                        m_BracketTokenListList.Add(bracketTokens);

                        // 为每个 [ ... ] 创建 FileMetaBracketTerm
                        // 这里我们需要构造一个临时的包含这些 token 的 Node 来兼容 FileMetaBracketTerm
                        if (bracketTokens.Count > 0)
                        {
                            // 简化：用第一个和最后一个 token 创建占位符
                            Node bracketNode = new Node(m_TokenList[bracketStart]) { nodeType = ENodeType.Bracket };
                            bracketNode.endToken = m_TokenList[i];
                            FileMetaBracketTerm fmbt = new FileMetaBracketTerm(m_FileMeta, bracketNode);
                            m_FileMetaBracketTermList.Add(fmbt);
                        }

                        bracketStart = -1;
                    }
                }
            }

            // 如果有数组维度，获取其数值
            if (isArray)
            {
                GetBracketListInt32Value();
            }
        }

        // 按逗号拆分 Token 列表
        private List<List<Token>> SplitTokensByComma(List<Token> tokens)
        {
            List<List<Token>> result = new List<List<Token>>();
            List<Token> current = new List<Token>();
            int depth = 0;

            foreach (var token in tokens)
            {
                // 追踪嵌套深度
                if (token.type == ETokenType.LeftPar || token.type == ETokenType.Less || token.type == ETokenType.LeftBracket || token.type == ETokenType.LeftBrace)
                    depth++;
                else if (token.type == ETokenType.RightPar || token.type == ETokenType.Greater || token.type == ETokenType.RightBracket || token.type == ETokenType.RightBrace)
                    depth--;

                // 顶层逗号作为分隔符
                if (token.type == ETokenType.Comma && depth == 0)
                {
                    if (current.Count > 0)
                    {
                        result.Add(new List<Token>(current));
                        current.Clear();
                    }
                }
                else
                {
                    current.Add(token);
                }
            }

            // 添加最后一段
            if (current.Count > 0)
            {
                result.Add(current);
            }

            return result;
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
                            m_ArrayDimsionLengthList.Add((int)fmcvt.token.lexeme);
                    }
                }
                else
                {
                    m_ArrayDimsionLengthList.Add(-1);
                }
            }
            if (m_ArrayDimsionLengthList.Count != m_FileMetaBracketTermList.Count)
            {
                Log.AddInStructFileMeta(EError.None, "数组获取长度文件的时候，有异常!");
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
            Log.AddInStructFileMeta(EError.None, str);
        }
    }
    public class FileMetaTemplateDefine : FileMetaBase
    {
        public Token inToken => m_InToken;
        public FileInputTemplateNode inClassNameTemplateNode => m_InClassNameTemplateNode;

        public Node extendNode => m_ExtendsNode;

        private Token m_InToken = null;
        private FileInputTemplateNode m_InClassNameTemplateNode = null;
        private Node m_Node = null;
        private Node m_ExtendsNode = null;
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
        public FileMetaTemplateDefine(FileMeta fm, Node node, Node extendsNode )
        {
            m_FileMeta = fm;
            m_Node = node;
            m_Token = node.token;
            m_ExtendsNode = extendsNode;
        }
        public FileMetaTemplateDefine( FileMeta fm, List<Node> nodeList )
        {
            m_FileMeta = fm;
            if ( nodeList.Count == 0 )
            {
                Log.AddInStructFileMeta(EError.None, "Error 在<>中没有发现元素!!");
                return;
            }
            m_Token = nodeList[0].token;
            if( nodeList.Count == 2 )
            {
                m_InToken = nodeList[1].token;
                m_InClassNameTemplateNode = new FileInputTemplateNode(fm, nodeList[2] );
            }
            else if( nodeList.Count == 2 )
            {
                Log.AddInStructFileMeta(EError.None, "Error 在<T in> or <T []> or <T ClassName> 使用方法不正确,请使用 <T in []>或者是 <T in ClassName> !!");
            }
        }
        public void Parse()
        {

        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_Token?.lexeme.ToString());
            if( m_InToken != null )
            {
                sb.Append( " " + m_InToken?.lexeme.ToString() + " ");
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
