//****************************************************************************
//  File:      FileMetaGlobalSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaImportSyntax : FileMetaBase
    {
        public NamespaceStatementBlock namespaceStatement => m_NamespaceStatement;
        public NamespaceStatementBlock asNameStatement => m_AsNameStatement;
        public string moduleName => m_ModuleName;
        public string importNamespaceName => m_ImportNamespaceName;
        public string aliasName => m_AliasName;



        private Token m_AsToken;
        private Token m_AsNameToken;
        private List<Token> m_ImportNameListToken = new List<Token>();
        private List<Node> m_NodeList = new List<Node>();
        private List<Token> m_TokenList = new List<Token>();
        private NamespaceStatementBlock m_NamespaceStatement = null;
        private string m_ModuleName = string.Empty;
        private string m_ImportNamespaceName = string.Empty;
        private string m_AliasName = string.Empty;
#pragma warning disable CS0649
        private NamespaceStatementBlock m_AsNameStatement = null;
#pragma warning restore CS0649
        public FileMetaImportSyntax(List<Node> _nodeList)
        {
            m_NodeList = _nodeList;
        }
        public FileMetaImportSyntax(List<Token> _tokenList)
        {
            m_TokenList = _tokenList;
        }
        private bool ParseImportSyntax()
        {
            if (m_NodeList.Count < 2)
            {
                Log.AddFileMetaLog(LID.FileMetaGlobalSyntaxImport, "Error import必须有2个节点!!");
                return false;
            }
            var namespaceNode = m_NodeList[0];
            if (namespaceNode?.token?.type == ETokenType.Import)
            {
                m_Token = namespaceNode.token;
            }
            var namespaceNameNode = m_NodeList[1];

            if (namespaceNameNode?.token?.type == ETokenType.String)
            {
                if (!ParseStringImportTarget(namespaceNameNode.token))
                {
                    return false;
                }
            }
            else
            {
                m_ImportNameListToken = namespaceNameNode.GetLinkTokenList();
                m_ImportNamespaceName = TokensToNamespaceText(m_ImportNameListToken);
            }

            if (m_NodeList.Count == 4)
            {
                m_AsToken = m_NodeList[2].token;
                var asNameNode = m_NodeList[3];
                m_AsNameToken = asNameNode.token;
                m_AliasName = m_AsNameToken?.lexeme?.ToString() ?? string.Empty;
            }

            m_NamespaceStatement = NamespaceStatementBlock.CreateStateBlock(m_ImportNameListToken);
            if (m_NamespaceStatement == null)
            {
                // CreateStateBlock 内部已打出命名空间格式错误日志（FileMetaCommonX/X2），此处不再重复
                return false;
            }
            return true;
        }

        private bool ParseStringImportTarget(Token targetToken)
        {
            var text = targetToken?.lexeme?.ToString() ?? string.Empty;
            text = text.Trim();

            int colonIndex = text.IndexOf(':');
            if (colonIndex >= 0)
            {
                m_ModuleName = text.Substring(0, colonIndex).Trim();
                if( colonIndex + 1 < text.Length )
                {
                    // "Std:IO" → module=Std, namespace=IO；冒号在末尾（"Std:"）时保持空，走下方空检查报错
                    m_ImportNamespaceName = text.Substring(colonIndex + 1).Trim();
                }
            }
            else
            {
                m_ModuleName = string.Empty;
                m_ImportNamespaceName = text;
            }

            if (string.IsNullOrWhiteSpace(m_ImportNamespaceName))
            {
                Log.AddFileMetaLog(LID.FileMetaGlobalSyntaxImport2, targetToken, "Error import 字符串中没有命名空间名称");
                return false;
            }

            m_ImportNameListToken = BuildNamespaceTokens(targetToken, m_ImportNamespaceName);
            return true;
        }

        private static List<Token> BuildNamespaceTokens(Token sourceToken, string namespaceName)
        {
            List<Token> tokens = new List<Token>();
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return tokens;
            }

            var parts = namespaceName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    tokens.Add(new Token(sourceToken.path, ETokenType.Period, ".", sourceToken.sourceBeginLine - 1, sourceToken.sourceBeginChar));
                }
                tokens.Add(new Token(sourceToken.path, ETokenType.Identifier, parts[i], sourceToken.sourceBeginLine - 1, sourceToken.sourceBeginChar));
            }
            return tokens;
        }

        private static string TokensToNamespaceText(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                var text = tokens[i]?.lexeme?.ToString();
                if (string.IsNullOrEmpty(text)) continue;
                sb.Append(text);
            }
            return sb.ToString();
        }

        public void Parse()
        {
            if (!ParseImportSyntax())
            {
                // import 语句形态非法，错误已在 ParseImportSyntax 内打出（Error 且 pass=FALSE，计入 errorCount 阻断编译）
                return;
            }

            MetaNode mb = ResolveImportMetaNode();
            if (mb == null)
            {
                return;
            }

            if (mb.isMetaNamespace)
            {
                m_FileMeta.AddImportMetaNamespace(mb.metaNamespace);
                if (!string.IsNullOrWhiteSpace(m_AliasName))
                {
                    m_FileMeta.AddImportAliasMetaNamespace(m_AliasName, mb.metaNamespace);
                }
                return;
            }

            // import Std; 解析到的是模块根节点（isMetaModule），也注册它
            if (mb.isMetaModule)
            {
                m_FileMeta.AddImportMetaNode(mb);
                // import Std as S：模块别名注册到 FileMeta，供 S.X.Y 链首兜底解析
                // （类型位置 TypeManager→GetMetaClassByRef→FileMeta、表达式位置 MetaCallNode→GetFirstNode 同源）
                if (!string.IsNullOrWhiteSpace(m_AliasName))
                {
                    m_FileMeta.AddImportAliasMetaNode(m_AliasName, mb);
                }
                return;
            }

            Log.AddFileMetaLog(LID.FileMetaGlobalSyntaxImport3, m_NamespaceStatement.tokenList[0], "解析Import语句发生错误，没有找到对应的命名空间路径: " + (m_ImportNamespaceName ?? string.Empty));
        }

        private MetaNode ResolveImportMetaNode()
        {
            if (m_NamespaceStatement == null || m_NamespaceStatement.tokenList.Count == 0)
            {
                // 此前无任何日志的静默失败路径（如 import 目标为空），补首发错误提示
                Log.AddFileMetaLog(LID.FileMetaGlobalSyntaxImport5, m_Token,
                    "import 语句没有可解析的命名空间目标: " + (m_ImportNamespaceName ?? string.Empty));
                return null;
            }

            MetaNode mb = null;
            int startIndex = 0;

            if (!string.IsNullOrWhiteSpace(m_ModuleName))
            {
                var module = ModuleManager.instance.GetMetaModuleByName(m_ModuleName);
                if (module == null)
                {
                    Log.AddFileMetaLog(LID.FileMetaGlobalSyntaxImport4, m_Token, "没有找到 import 模块: " + m_ModuleName);
                    return null;
                }
                mb = module.metaNode;
            }
            else
            {
                // Check if the first token is a module name (e.g. "import Core")
                var firstName = m_NamespaceStatement.tokenList[0].lexeme.ToString();
                var module = ModuleManager.instance.GetMetaModuleByName(firstName);
                if (module != null)
                {
                    mb = module.metaNode;
                    startIndex = 1;
                }
                else
                {
                    mb = ModuleManager.instance.selfModule.metaNode;
                }
            }

            for (int i = startIndex; i < m_NamespaceStatement.tokenList.Count; i++)
            {
                string name = m_NamespaceStatement.tokenList[i].lexeme.ToString();
                var findmb = mb.GetChildrenMetaNodeByName(name);
                if (findmb == null)
                {
                    // 首段不是已加载模块时回退到自身模块搜索，误报为"自身模块下不存在"；
                    // 补充提示首段可能是未引用/未导出的模块名，避免误导
                    string hint = (i == 0 && string.IsNullOrWhiteSpace(m_ModuleName))
                        ? $"（若 '{name}' 是模块名，请确认它已在工程 .jsonc 的 references 中声明并完成编译导出）"
                        : string.Empty;
                    Log.AddFileMetaLog(LID.FileMetaGlobalSyntaxNotFound, m_NamespaceStatement.tokenList[i],
                        $"文件:{m_NamespaceStatement.tokenList[i].path} 没有找到:{mb.allName} 下的:{name}{hint}");
                    return null;
                }
                mb = findmb;
            }

            return mb;
        }

        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.m_Token?.lexeme.ToString() + " " + m_NamespaceStatement?.ToFormatString());
            if (m_AsToken != null)
            {
                sb.Append(" " + m_AsToken.lexeme.ToString());
                sb.Append(" " + m_AsNameToken.lexeme.ToString());
            }
            sb.Append(";");
            return sb.ToString();
        }
    }

}
