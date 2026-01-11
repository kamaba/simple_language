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
using System.Diagnostics;
using SimpleLanguage.Logging;

namespace SimpleLanguage.Compile
{
    public sealed class NamespaceStatementBlock
    {
        public string namespaceString => m_NamespaceString;
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

        protected List<Token> m_TokenList = new List<Token>();
        private string m_NamespaceString = null;
        private List<string> m_NamespaceList = null;
        private List<string> m_NamespaceStackList = null;//层叠命名空间名称

        protected NamespaceStatementBlock(List<Token> token)
        {
            m_TokenList = token;

            UpdateNamespace();
        }
        //public void AddMetaNamespace( MetaNamespace metaNamespace)
        //{
        //    m_MetaNamespaceList.Add( metaNamespace );
        //}
        public static NamespaceStatementBlock CreateStateBlock( List<Token> token )
        {
            Debug.Assert(token != null);
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
        public void UpdateNamespace()
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
        // new token-based ctor
        public FileInputTemplateNode(FileMeta fm, List<Token> tokens)
        {
            m_FileMeta = fm;
            m_DefineClassCallLink = tokens != null
                ? new FileMetaCallLink(fm, tokens)
                : null;
        }
        // Node 版本构造方法（legacy，已由 Token 版本取代）
        // public FileInputTemplateNode( FileMeta fm, Node node ) { ... }

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

        // new token-based ctor used by Token pipeline
        public FileMetaCallNode(FileMeta fm, List<Token> segmentTokens)
        {
            m_FileMeta = fm;
            InitializeFromTokens(segmentTokens);
        }
        // core implementation: parse one call segment from tokens
        private void InitializeFromTokens(List<Token> segmentTokens)
        {
            if (segmentTokens == null || segmentTokens.Count == 0)
                return;

            m_Token = segmentTokens[0];

            // scan for top-level (), <>, [] and {}
            int parenStart = -1, parenEnd = -1;
            int angleStart = -1, angleEnd = -1;
            int bracketStart = -1, bracketEnd = -1;
            int braceStart = -1, braceEnd = -1;
            int parenDepth = 0, angleDepth = 0, bracketDepth = 0, braceDepth = 0;

            for (int i = 1; i < segmentTokens.Count; i++)
            {
                var t = segmentTokens[i];
                switch (t.type)
                {
                    case ETokenType.LeftPar:
                        if (parenDepth == 0 && parenStart == -1) parenStart = i;
                        parenDepth++;
                        break;
                    case ETokenType.RightPar:
                        parenDepth--;
                        if (parenDepth == 0 && parenEnd == -1) parenEnd = i;
                        break;
                    case ETokenType.Less:
                        if (angleDepth == 0 && angleStart == -1) angleStart = i;
                        angleDepth++;
                        break;
                    case ETokenType.Greater:
                        angleDepth--;
                        if (angleDepth == 0 && angleEnd == -1) angleEnd = i;
                        break;
                    case ETokenType.LeftBracket:
                        if (bracketDepth == 0 && bracketStart == -1) bracketStart = i;
                        bracketDepth++;
                        break;
                    case ETokenType.RightBracket:
                        bracketDepth--;
                        if (bracketDepth == 0 && bracketEnd == -1) bracketEnd = i;
                        break;
                    case ETokenType.LeftBrace:
                        if (braceDepth == 0 && braceStart == -1) braceStart = i;
                        braceDepth++;
                        break;
                    case ETokenType.RightBrace:
                        braceDepth--;
                        if (braceDepth == 0 && braceEnd == -1) braceEnd = i;
                        break;
                }
            }

            // function call parens
            if (parenStart != -1 && parenEnd > parenStart)
            {
                isCallFunction = true;
                var parTokens = segmentTokens.GetRange(parenStart, parenEnd - parenStart + 1);
                m_FileMetaParTerm = new FileMetaParTerm(m_FileMeta, parTokens, FileMetaTermExpress.EExpressType.Common);
                m_BeginParToken = parTokens[0];
                m_EndParToken = parTokens[parTokens.Count - 1];
            }

            // generic angle args
            if (angleStart != -1 && angleEnd > angleStart)
            {
                isTemplate = true;
                m_BeginAngleToken = segmentTokens[angleStart];
                m_EndAngleToken = segmentTokens[angleEnd];

                var inner = segmentTokens.GetRange(angleStart + 1, angleEnd - angleStart - 1);
                // split generic args by top-level comma
                List<List<Token>> argTokenLists = new List<List<Token>>();
                List<Token> cur = new List<Token>();
                int p = 0, a = 0, b = 0, br = 0;
                for (int i = 0; i < inner.Count; i++)
                {
                    var t = inner[i];
                    switch (t.type)
                    {
                        case ETokenType.LeftPar: p++; break;
                        case ETokenType.RightPar: if (p > 0) p--; break;
                        case ETokenType.Less: a++; break;
                        case ETokenType.Greater: if (a > 0) a--; break;
                        case ETokenType.LeftBracket: b++; break;
                        case ETokenType.RightBracket: if (b > 0) b--; break;
                        case ETokenType.LeftBrace: br++; break;
                        case ETokenType.RightBrace: if (br > 0) br--; break;
                    }
                    if (t.type == ETokenType.Comma && p == 0 && a == 0 && b == 0 && br == 0)
                    {
                        if (cur.Count > 0)
                        {
                            argTokenLists.Add(new List<Token>(cur));
                            cur.Clear();
                        }
                    }
                    else
                    {
                        cur.Add(t);
                    }
                }
                if (cur.Count > 0)
                    argTokenLists.Add(cur);

                foreach (var argTokens in argTokenLists)
                {
                    var tplNode = new FileInputTemplateNode(m_FileMeta, argTokens);
                    m_InputTemplateNodeList.Add(tplNode);
                }
            }

            // single bracket pair (arrays may chain at link level)
            if (bracketStart != -1 && bracketEnd > bracketStart)
            {
                isArray = true;
                var brTokens = segmentTokens.GetRange(bracketStart, bracketEnd - bracketStart + 1);
                var brTerm = new FileMetaBracketTerm(m_FileMeta, brTokens);
                m_FileMetaBracketTermList.Add(brTerm);
            }

            // initializer brace
            if (braceStart != -1 && braceEnd > braceStart)
            {
                var brTokens = segmentTokens.GetRange(braceStart, braceEnd - braceStart + 1);
                m_FileMetaBraceTerm = new FileMetaBraceTerm(m_FileMeta, brTokens);
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
        private List<Token> m_TokenList = null;  // Token 版本
        private List<FileMetaCallNode> m_CallNodeList = new List<FileMetaCallNode>();


        // Token 版本构造方法（新）
        public FileMetaCallLink(FileMeta fm, List<Token> tokenList)
        {
            m_FileMeta = fm;
            m_TokenList = tokenList ?? new List<Token>();
            BuildFromTokenList(m_TokenList);
        }
        // Node 版本构造方法（legacy，已由 Token 版本取代）
        // public FileMetaCallLink( FileMeta fm, Node node, bool isIncludeSelf = true ) { ... }

        // 从 Token 列表构建 CallNode 链
        private void BuildFromTokenList(List<Token> tokenList)
        {
            if (tokenList == null || tokenList.Count == 0)
                return;

            // 按点号（Period）拆分 token 序列，构建链式调用
            // 例如：a.b.c() 拆成 [a] [.] [b] [.] [c()]
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

                // 顶层点号作为单独段落加入，保证 '.' 也能被下游看到
                if (t.type == ETokenType.Period && parenDepth == 0 && angleDepth == 0 && bracketDepth == 0)
                {
                    if (currentSegment.Count > 0)
                    {
                        callSegments.Add(new List<Token>(currentSegment));
                        currentSegment.Clear();
                    }

                    // 将 '.' 本身作为独立的段加入，便于还原完整调用链 token
                    var dotSegment = new List<Token> { t };
                    callSegments.Add(dotSegment);
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

                var callNode = new FileMetaCallNode(m_FileMeta, segmentTokens);
                if (callNode != null)
                {
                    m_CallNodeList.Add(callNode);
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
            
            // 选择类名 token：在出现泛型 '<' 之前最后一个 Identifier/Type
            int nameIndex = -1;
            for (int i = 0; i < m_TokenList.Count; i++)
            {
                if (m_TokenList[i].type == ETokenType.Less)
                    break;
                if (m_TokenList[i].type == ETokenType.Identifier || m_TokenList[i].type == ETokenType.Type)
                    nameIndex = i;
            }
            if (nameIndex >= 0)
            {
                m_ClassNameToken = m_TokenList[nameIndex];
            }
 
            // 处理泛型模板：< ... >
            ExtractAndProcessGenericTemplate();

            // 处理数组维度：[ ... ]
            ExtractAndProcessArrayDimensions();
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
                        FileInputTemplateNode fmcn = new FileInputTemplateNode(m_FileMeta, paramTokens);
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
                        // 包含中括号本身，供 FileMetaBracketTerm(Token-list) 使用
                        var fullBracketTokens = m_TokenList.GetRange(bracketStart, i - bracketStart + 1);
                        // 仅括号内部内容用于计算维度
                        var innerBracketTokens = m_TokenList.GetRange(bracketStart + 1, i - bracketStart - 1);

                        m_BracketTokenListList.Add(innerBracketTokens);

                        // 直接使用 Token 版本的 FileMetaBracketTerm，不再构造临时 Node
                        if (fullBracketTokens.Count > 0)
                        {
                            FileMetaBracketTerm fmbt = new FileMetaBracketTerm(m_FileMeta, fullBracketTokens);
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
            if(m_ClassNameToken != null )
            {
                sb.Append(m_ClassNameToken.lexeme.ToString());
            }
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

        private Token m_InToken = null;
        private FileInputTemplateNode m_InClassNameTemplateNode = null;
        
        // Token-based ctor: <T>, <T in U>, etc. represented as a flat token list
        public FileMetaTemplateDefine(FileMeta fm, List<Token> tokens)
        {
            m_FileMeta = fm;
            if (tokens == null || tokens.Count == 0)
            {
                Log.AddInStructFileMeta(EError.None, "Error 在<>中没有发现元素!!");
                return;
            }

            // 期望格式：T [in ConstraintType]
            // 第一个标识符作为模板参数名
            m_Token = tokens[0];

            int index = 1;
            // 跳过空格/行结束等
            while (index < tokens.Count && (tokens[index].type == ETokenType.Space || tokens[index].type == ETokenType.LineEnd))
            {
                index++;
            }

            // 约束关键字: in
            if (index < tokens.Count && tokens[index].type == ETokenType.Colon )
            {
                m_InToken = tokens[index];
                index++;

                // 跳过空格/行结束
                while (index < tokens.Count && (tokens[index].type == ETokenType.Space || tokens[index].type == ETokenType.LineEnd))
                {
                    index++;
                }

                if (index < tokens.Count)
                {
                    // 剩余 token 视为约束类型，例如 Collections.List<Map<int,string>>
                    var constraintTokens = tokens.GetRange(index, tokens.Count - index);
                    m_InClassNameTemplateNode = new FileInputTemplateNode(fm, constraintTokens);
                }
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