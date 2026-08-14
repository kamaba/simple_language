//****************************************************************************
//  File:      StructParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: struct parse node to code structure, and build code structure file meta data
//****************************************************************************


using SimpleLanguage.Project;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;

namespace SimpleLanguage.Compile
{
    public enum EParseNodeType
    {
        Null,
        File,
        Namespace,
        Class,
        Function,
        Statements,
        DataMemeber,
    }
    public partial class StructParse
    {
        public class ParseCurrentNodeInfo  //1111
        {
            public EParseNodeType parseType;
            public FileMeta codeFile = null;
            public FileMetaNamespace codeNamespace = null;
            public FileMetaClass codeClass = null;
            public FileMetaMemberData codeData = null;
            public FileMetaMemberFunction codeFunction = null;
            public FileMetaSyntax codeSyntax = null;

            public ParseCurrentNodeInfo(FileMeta cf)
            {
                codeFile = cf;
                parseType = EParseNodeType.File;
            }
            public ParseCurrentNodeInfo(FileMetaNamespace nsn)
            {
                codeNamespace = nsn;
                parseType = EParseNodeType.Namespace;
            }
            public ParseCurrentNodeInfo(FileMetaClass nsc)
            {
                codeClass = nsc;
                parseType = EParseNodeType.Class;
            }
            public ParseCurrentNodeInfo(FileMetaMemberData fmmd)
            {
                codeData = fmmd;
                parseType = EParseNodeType.DataMemeber;
            }
            public ParseCurrentNodeInfo(FileMetaMemberFunction nsf)
            {
                codeFunction = nsf;
                parseType = EParseNodeType.Function;
            }
            public ParseCurrentNodeInfo(FileMetaSyntax nss)
            {
                codeSyntax = nss;
                parseType = EParseNodeType.Statements;
            }
        }
        protected ParseCurrentNodeInfo currentNodeInfo
        {
            get
            {
                if (m_CurrentNodeInfoStack.Count == 0) return null;

                return m_CurrentNodeInfoStack.Peek();
            }
        }

        protected FileMeta m_FileMeta;
        protected Node m_RootNode = null;
        protected Stack<ParseCurrentNodeInfo> m_CurrentNodeInfoStack = new Stack<ParseCurrentNodeInfo>();
        // When 'checked' precedes 'label', this flag is set so the label handler
        // knows to enable checked context for the try body.
        protected bool m_PendingCheckedLabel = false;

        public StructParse(FileMeta fm, Node node)
        {
            m_FileMeta = fm;
            m_RootNode = node;
        }
        public void AddParseNamespaceNodeInfo(FileMetaNamespace fmn)
        {
            if (currentNodeInfo.parseType == EParseNodeType.File)
            {
                currentNodeInfo.codeFile.AddFileMetaAllNamespace(fmn);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.Namespace)
            {
                currentNodeInfo.codeNamespace.AddFileNamespace(fmn);
            }
            else
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, fmn.namespaceNode.token, "Error AddParseNamespaceNodeInfo");
            }

            ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fmn);
            m_CurrentNodeInfoStack.Push(pcni);
        }
        public void AddParseClassNodeInfo(FileMetaClass fmc)
        {
            if (currentNodeInfo.parseType == EParseNodeType.File)
            {
                currentNodeInfo.codeFile.AddFileMetaClass(fmc);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.Namespace)
            {
                currentNodeInfo.codeNamespace.AddFileMetaClass(fmc);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMetaClass(fmc);
            }
            else
            {
                Log.AddNodeLog(LID.ShowExtendMessage, fmc.token, "Error AddParseClassNodeInfo");
                return;
            }
            m_FileMeta.AddFileMetaAllClass(fmc);

            ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fmc);
            m_CurrentNodeInfoStack.Push(pcni);
        }
        public void AddParseVariableInfo(FileMetaMemberVariable csv)
        {
            if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMemberVariable(csv);
            }
            else
            {
                Log.AddNodeLog(LID.ShowExtendMessage, csv.token, "Error AddParseVariableInfo");
                return;
            }
        }
        public void AddParseDataInfo(FileMetaMemberData fmmd)
        {
            if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMemberData(fmmd);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.DataMemeber)
            {
                currentNodeInfo.codeData.AddFileMemberData(fmmd);
            }
            else
            {
                Log.AddNodeLog(LID.ShowExtendMessage, fmmd.token, "Error AddParseDataInfo");
                return;
            }

            ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fmmd);

            m_CurrentNodeInfoStack.Push(pcni);
        }
        public void AddParseFunctionNodeInfo(FileMetaMemberFunction fmmf)
        {
            if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMemberFunction(fmmf);
            }
            else
            {
                Log.AddNodeLog(LID.ShowExtendMessage, fmmf.token, "Error AddParseFunctionNodeInfo");
                return;
            }

            ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fmmf);

            m_CurrentNodeInfoStack.Push(pcni);
        }
        public void AddParseSyntaxNodeInfo(FileMetaSyntax fms, bool isAddParseCurrentNNode = false)
        {
            if (currentNodeInfo.parseType == EParseNodeType.Function)
            {
                currentNodeInfo.codeFunction.AddFileMetaSyntax(fms);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.Statements)
            {
                currentNodeInfo.codeSyntax.AddFileMetaSyntax(fms);
            }
            else
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, fms.token, "Error AddParseSyntaxNodeInfo");
                return;
            }

            if (isAddParseCurrentNNode)
            {
                ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fms);
                m_CurrentNodeInfoStack.Push(pcni);
            }
        }
        // Normalize single-line sequences: attach trailing (), [] to the preceding
        // IdentifierLink (or its last extend link). When attached, the bracket/par children
        // are not recursively processed here — they remain as part of the attached node.
        //private void HandleNodeSingleLine(Node root)
        //{
        //    if (root == null) return;

        //    for (int i = 0; i < root.childList.Count; i++)
        //    {
        //        var n = root.childList[i];
        //        if (n == null) continue;

        //        if (n.nodeType == ENodeType.IdentifierLink)
        //        {
        //            int j = i + 1;
        //            while (j < root.childList.Count)
        //            {
        //                var s = root.childList[j];
        //                if (s == null) { j++; continue; }

        //                if (s.nodeType == ENodeType.LineEnd || s.nodeType == ENodeType.SemiColon)
        //                    break;

        //                // target is identifier or its deepest extend link
        //                Node target = n;
        //                if (n.extendLinkNodeList != null && n.extendLinkNodeList.Count > 0)
        //                    target = n.extendLinkNodeList[n.extendLinkNodeList.Count - 1];

        //                if (s.nodeType == ENodeType.Par)
        //                {
        //                    // bind parentheses to target
        //                    target.SetParNode(s);
        //                    // remove from root list so it's not processed as sibling
        //                    root.childList.RemoveAt(j);
        //                    // continue at same index j
        //                    continue;
        //                }
        //                else if (s.nodeType == ENodeType.Bracket)
        //                {
        //                    // add bracket node to target's bracket list
        //                    target.AddBracketNode(s);
        //                    root.childList.RemoveAt(j);
        //                    continue;
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }

        //            // recurse into identifier itself (but not into attached par/bracket children)
        //            HandleNodeSingleLine(n);
        //        }
        //        else
        //        {
        //            // skip recursing into Par/Bracket nodes here
        //            if (n.nodeType == ENodeType.Par || n.nodeType == ENodeType.Bracket) continue;
        //            HandleNodeSingleLine(n);
        //        }
        //    }
        //}
        public void ParseRootNodeToFileMeta()
        {
            ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(m_FileMeta);
            m_CurrentNodeInfoStack.Push(pcni);

            Node pnode = m_RootNode;
            bool hasNamespaceOrClass = false;
            while (pnode.parseIndex < pnode.childList.Count)
            {
                var node = m_RootNode.childList[pnode.parseIndex];
                if (node.nodeType == ENodeType.LineEnd)
                {
                    pnode.parseIndex++;
                    continue;
                }
                else if (node.nodeType == ENodeType.Comment)
                {
                    pnode.parseIndex++;
                    continue;
                }
                else if (node.nodeType == ENodeType.Key)
                {
                    switch (node.token.type)
                    {
                        case ETokenType.TypeAlias:
                            {
                                if (hasNamespaceOrClass)
                                {
                                    Log.AddNodeLog(LID.ShowExtendMessage, node.token, "Error typealias 只能写在 import/local 后、namespace/class/data/enum 前");
                                    pnode.parseIndex++;
                                    break;
                                }
                                int ni = ConsumeTypeAliasAt(m_RootNode, pnode.parseIndex, false);
                                if (ni > pnode.parseIndex)
                                    pnode.parseIndex = ni;
                                else
                                    pnode.parseIndex++;
                            }
                            break;
                        case ETokenType.Import:
                            {
                                ParseImport(pnode);
                            }
                            break;
                        case ETokenType.Local:
                            {
                                // local{} must be after imports and before any namespace/class definitions.
                                if (hasNamespaceOrClass)
                                {
                                    Log.AddFileMetaLog(LID.ShowExtendMessage, node.token, "Error local{} 只能写在 import 后、namespace/class/data/enum 前");
                                    pnode.parseIndex++;
                                    break;
                                }

                                ParseLocal(pnode);
                            }
                            break;
                        case ETokenType.Namespace:
                            {
                                hasNamespaceOrClass = true;
                                ParseNamespace(pnode);
                            }
                            break;
                        case ETokenType.Const:
                        case ETokenType.Data:
                        case ETokenType.Enum:
                        case ETokenType.Class:
                        case ETokenType.Interface:
                        case ETokenType.Extern:
                        case ETokenType.Public:
                        case ETokenType.Private:
                        case ETokenType.Projected:
                        case ETokenType.Partial:
                        case ETokenType.At:
                            {
                                hasNamespaceOrClass = true;
                                ParseNamespaceOrTopClass(pnode);
                            }
                            break;
                        default:
                            {
                                Log.AddNodeLog(LID.ShowExtendMessage, node.token, "Error 不允许 在File头级目录中出现 : " + node.token.lexeme.ToString());
                            }
                            break;
                    }
                }
                else if (node.nodeType == ENodeType.IdentifierLink)
                {
                    ParseNamespaceOrTopClass(pnode);
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, node.token, "Error 不允许 在File头级目录中出现2 : " + node.token?.lexeme.ToString());
                }
            }

            var fileCode = m_CurrentNodeInfoStack.Pop();

            if (fileCode.parseType == EParseNodeType.File)
            {
#if DEBUG
                m_FileMeta.SetDeep(0);
#endif

                //Log.AddNodeLog( LID.ShowExtendMessage, "解析成Code代码结构文件成功!!! 下一步，可以生产Meta文件了  "
                //+ "生成FileMeta文件成功!!! 下一步，可以 进行混合了");
                return;
            }
            else
            {

                Log.AddNodeLog(LID.ShowExtendMessage, $"[{m_FileMeta.path}]解析出现错误 ParseFile : " + currentNodeInfo.parseType.ToString());
                return;
            }
        }
        private void ParseLeadingAttributes(Node pnode, ref int index, List<FileMetaAttributeSyntax> list)
        {
            if (pnode == null) return;

            while (index < pnode.childList.Count)
            {
                // do not consume anything unless we actually parse an attribute
                int start = index;

                var n = pnode.childList[index];
                if (n == null)
                {
                    index++;
                    continue;
                }

                // attributes are prefixes; if current token isn't '@', stop and keep cursor
                if (n.token?.type != ETokenType.At)
                    break;

                // only allow in namespace/class blocks
                if (currentNodeInfo == null &&
                    (currentNodeInfo.parseType == EParseNodeType.Function))
                {
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error @Attribute 只允许写在 namespace{} / class{} 内");
                    // do not consume; let outer parser handle as error or normal token
                    break;
                }

                var atToken = n.token;
                var attrName = atToken.extend != null ? atToken.extend.ToString() : null;
                if (string.IsNullOrEmpty(attrName))
                {
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error @Attribute 名称为空");
                    // invalid attribute; do not consume to avoid breaking outer logic
                    break;
                }

                int tmp = index + 1; // consume '@'

                // optional () param list (can be on next line)
                FileMetaParTerm parTerm = null;
                Node parNode = null;

                if (tmp < pnode.childList.Count)
                {
                    var next = pnode.childList[tmp];
                    if (next != null && next.nodeType == ENodeType.Par)
                    {
                        parNode = next;
                        tmp += 1;
                    }
                    else if (next != null && next.nodeType == ENodeType.LineEnd && tmp + 1 < pnode.childList.Count)
                    {
                        var next2 = pnode.childList[tmp + 1];
                        if (next2 != null && next2.nodeType == ENodeType.Par)
                        {
                            parNode = next2;
                            tmp += 2; // LineEnd + Par
                        }
                    }
                }

                if (parNode != null)
                {
                    parTerm = new FileMetaParTerm(m_FileMeta, parNode, FileMetaTermExpress.EExpressType.Common);
                }

                // allow repeated attributes, including separated by LineEnd
                while (tmp < pnode.childList.Count && pnode.childList[tmp]?.nodeType == ENodeType.LineEnd)
                    tmp++;

                // commit
                list.Add(new FileMetaAttributeSyntax(m_FileMeta, atToken, attrName, parTerm));
                index = tmp;

                if (index <= start)
                {
                    // safety net: never loop without advancing
                    index = start;
                    break;
                }
            }
        }
        public List<Node> GetAllNodeToSemiColon(Node pnode, bool isAddSelf = false)
        {
            Node curNode = pnode.parseCurrent;

            List<Node> conNode = new List<Node>();
            if (isAddSelf)
                conNode.Add(curNode);
            if (curNode.nodeType == ENodeType.SemiColon)
            {
                pnode.parseIndex++;
                return conNode;
            }

            Node node = pnode.GetParseNode();
            conNode.Add(node);
            bool isEnd = false;
            while (pnode.parseIndex < pnode.childList.Count)
            {
                Node nextNode = pnode.GetParseNode();
                if (nextNode == null)
                {
                    break;
                }
                if (ProjectManager.isUseForceSemiColonInLineEnd)
                {
                    if (nextNode.nodeType == ENodeType.SemiColon)
                    {
                        isEnd = true;
                    }
                }
                else
                {
                    if (nextNode.nodeType == ENodeType.SemiColon
                        || nextNode.nodeType == ENodeType.LineEnd)
                    {
                        isEnd = true;
                    }
                }
                if (isEnd)
                {
                    break;
                }
                else
                {
                    conNode.Add(nextNode);
                }

            }
            return conNode;
        }
        public void ParseImport(Node pnode)
        {
            List<Node> nodeList = GetAllNodeToSemiColon(pnode);
            FileMetaImportSyntax ist = new FileMetaImportSyntax(nodeList);
            m_FileMeta.AddFileImportSyntax(ist);
        }
        public void ParseLocal(Node pnode)
        {
            Node localNode = pnode.GetParseNode(); // consume 'local'
            if (localNode == null || localNode.token?.type != ETokenType.Local)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error local 解析失败");
                return;
            }

            if (m_FileMeta.GetFileMetaLocalSyntax() != null)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error local{} 在同一文件中只允许定义一次");
                return;
            }

            Node blockNode = null;
            while (pnode.parseIndex < pnode.childList.Count)
            {
                var next = pnode.childList[pnode.parseIndex];
                if (next != null && next.nodeType == ENodeType.Brace)
                {
                    blockNode = next;
                    pnode.parseIndex++;
                    break;
                }
                if (next.nodeType == ENodeType.Comment)
                {
                    pnode.parseIndex++;
                    continue;
                }
                else if (next != null && next.nodeType == ENodeType.LineEnd)
                {
                    if (pnode.parseIndex + 1 < pnode.childList.Count)
                    {
                        var next2 = pnode.childList[pnode.parseIndex + 1];
                        if (next2 != null && next2.nodeType == ENodeType.Brace)
                        {
                            blockNode = next2;
                            pnode.parseIndex += 2;
                            break;
                        }
                    }
                }
            }

            if (blockNode == null)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error local 后必须跟 {} 块");
                return;
            }

            var fls = new FileMetaLocalSyntax(m_FileMeta, localNode.token, blockNode, true);
            m_FileMeta.SetFileMetaLocalSyntax(fls);

            ParseLocalContent(fls, blockNode, true);
        }
        /// <summary>
        /// 从 parent.childList[startIndex] 为 typealias 起解析一行，登记到 FileMeta，返回下一未消费下标；失败返回 startIndex。
        /// </summary>
        private int ConsumeTypeAliasAt(Node parent, int startIndex, bool projectScope)
        {
            if (parent?.childList == null) return startIndex;
            var ch = parent.childList;
            if (startIndex >= ch.Count) return startIndex;
            var n0 = ch[startIndex];
            if (n0.nodeType != ENodeType.Key || n0.token?.type != ETokenType.TypeAlias)
                return startIndex;

            int i = startIndex + 1;
            while (i < ch.Count && ch[i].nodeType == ENodeType.LineEnd) i++;

            if (i >= ch.Count)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error typealias 后缺少别名与类型");
                return startIndex + 1;
            }

            string aliasName = null;
            var nameNode = ch[i];
            var tlist = nameNode.GetLinkTokenList();
            if (nameNode.nodeType == ENodeType.IdentifierLink && tlist != null && tlist.Count > 0)
                aliasName = tlist[tlist.Count - 1].lexeme.ToString();
            else if (nameNode.token != null && nameNode.token.type == ETokenType.Identifier)
                aliasName = nameNode.token.lexeme.ToString();

            if (string.IsNullOrEmpty(aliasName))
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error typealias 后应为单个标识符别名");
                return i + 1;
            }
            i++;

            while (i < ch.Count && ch[i].nodeType == ENodeType.LineEnd) i++;
            if (i >= ch.Count || ch[i].nodeType != ENodeType.Assign)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error typealias 缺少 = 与目标类型");
                return i;
            }
            i++;

            var typeNodes = new List<Node>();
            while (i < ch.Count)
            {
                var tn = ch[i];
                if (tn.nodeType == ENodeType.SemiColon)
                {
                    i++;
                    break;
                }
                if (tn.nodeType == ENodeType.LineEnd)
                {
                    i++;
                    break;
                }
                typeNodes.Add(tn);
                i++;
            }

            //var handled = FileMetatUtil.HandleClassDefineNodes(typeNodes);
            //typeNodes = typeNodes;
            Node typeRoot = null;
            for (int h = 0; h < typeNodes.Count; h++)
            {
                if (typeNodes[h]?.nodeType == ENodeType.IdentifierLink)
                {
                    typeRoot = typeNodes[h];
                    break;
                }
            }
            if (typeRoot == null)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, "Error typealias 目标类型无法解析");
                return i;
            }

            var fmcd = new FileMetaClassDefine(m_FileMeta, typeRoot);
            m_FileMeta.AddTypeAliasDecl(new FileMetaTypeAliasDecl(aliasName, fmcd, projectScope));
            return i;
        }
        private void ParseLocalContent(FileMetaLocalSyntax syntax, Node blockNode, bool isLocal)
        {
            if (syntax == null || blockNode == null) return;

            bool hasFunction = false;
            List<Node> lineNodes = new List<Node>();

            for (int i = 0; i < blockNode.childList.Count; i++)
            {
                var n = blockNode.childList[i];
                if (n == null) continue;

                if (n.nodeType == ENodeType.LineEnd || n.nodeType == ENodeType.SemiColon)
                {
                    if (lineNodes.Count == 0) continue;

                    if (TryParseGlobalOrLocalFunction(blockNode, syntax, lineNodes, ref hasFunction, ref i, isLocal))
                    {
                        lineNodes.Clear();
                        continue;
                    }

                    if (hasFunction)
                    {
                        Log.AddFileMetaLog(LID.ShowExtendMessage, isLocal
                            ? "Error local{} 中出现函数定义后，后边只允许继续定义函数"
                            : "Error global{} 中出现函数定义后，后边只允许继续定义函数");
                        lineNodes.Clear();
                        continue;
                    }

                    var initSyntax = CrateFileMetaSyntaxNoKey(lineNodes);
                    if (initSyntax != null)
                    {
                        syntax.AddInitSyntax(initSyntax);
                    }

                    lineNodes.Clear();
                    continue;
                }

                lineNodes.Add(n);
            }

            if (lineNodes.Count > 0)
            {
                if (!hasFunction)
                {
                    var initSyntax = CrateFileMetaSyntaxNoKey(lineNodes);
                    if (initSyntax != null)
                    {
                        syntax.AddInitSyntax(initSyntax);
                    }
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, isLocal
                        ? "Error local{} 中出现函数定义后，后边只允许继续定义函数"
                        : "Error global{} 中出现函数定义后，后边只允许继续定义函数");
                }
            }
        }
        private bool TryParseGlobalOrLocalFunction(Node ownerBlock, FileMetaLocalSyntax syntax, List<Node> lineNodes, ref bool hasFunction, ref int contentIndex, bool isLocal)
        {
            if (lineNodes == null || lineNodes.Count == 0) return false;

            //var normalizedNodes = FileMetatUtil.HandleClassDefineNodes(lineNodes);
            var normalizedNodes = lineNodes;

            bool isFunc = false;
            Node sigNode = null;
            for (int i = 0; i < normalizedNodes.Count; i++)
            {
                var n = normalizedNodes[i];
                if (n?.nodeType == ENodeType.IdentifierLink && n.parNode != null)
                {
                    isFunc = true;
                    sigNode = n;
                    break;
                }
            }

            if (!isFunc)
            {
                for (int i = 1; i < normalizedNodes.Count; i++)
                {
                    var cur = normalizedNodes[i];
                    var prev = normalizedNodes[i - 1];
                    if (cur?.nodeType == ENodeType.Par && prev?.nodeType == ENodeType.IdentifierLink)
                    {
                        prev.SetParNode(cur);
                        isFunc = true;
                        sigNode = prev;
                        break;
                    }
                }
            }

            if (!isFunc) return false;

            Node funcBlock = sigNode?.blockNode;
            if (funcBlock == null && contentIndex >= 0)
            {
                int nextIndex = contentIndex + 1;
                while (nextIndex < ownerBlock.childList.Count && ownerBlock.childList[nextIndex]?.nodeType == ENodeType.LineEnd)
                    nextIndex++;

                if (nextIndex < ownerBlock.childList.Count && ownerBlock.childList[nextIndex]?.nodeType == ENodeType.Brace)
                {
                    funcBlock = ownerBlock.childList[nextIndex];
                    contentIndex = nextIndex;
                }
            }

            if (funcBlock == null)
            {
                Log.AddNodeLog(LID.ShowExtendMessage, isLocal
                    ? "Error local{} 函数定义缺少函数体 {}"
                    : "Error global{} 函数定义缺少函数体 {}");
                return true;
            }

            for (int i = 0; i < normalizedNodes.Count; i++)
            {
                if (normalizedNodes[i]?.nodeType == ENodeType.Key && normalizedNodes[i].token?.type == ETokenType.Static)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, isLocal
                        ? "Error local{} 中定义的函数不允许使用 static"
                        : "Error global{} 中定义的函数不允许使用 static");
                    return true;
                }
            }

            hasFunction = true;
            var f = new FileMetaMemberFunction(m_FileMeta, funcBlock, new List<Node>(normalizedNodes));
            syntax.AddFunction(f);
            return true;
        }
        public void ParseNamespace(Node pnode)
        {
            Node currentNode = pnode.GetParseNode();

            bool isBlock = false;
            Node namespaceNode = null;
            while (pnode.parseIndex < pnode.childList.Count)
            {
                Node nextNode = pnode.GetParseNode();
                if (nextNode == null)
                {
                    break;
                }

                if (nextNode.nodeType == ENodeType.Brace)
                {
                    currentNode.SetBlockNode(nextNode);
                    isBlock = true;
                    break;
                }
                else if (nextNode.nodeType == ENodeType.IdentifierLink)
                {
                    if (namespaceNode != null)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 在解析namespace 中，后边跟着参数多于正常语法!!");
                    }
                    namespaceNode = nextNode;

                    if (pnode.parseIndex + 1 < pnode.childList.Count)
                    {
                        var next2Node = pnode.childList[pnode.parseIndex + 1];
                        if (next2Node?.nodeType == ENodeType.LineEnd)
                        {
                            if (pnode.parseIndex + 2 < pnode.childList.Count)
                            {
                                var next3Node = pnode.childList[pnode.parseIndex + 2];
                                if (next3Node?.nodeType == ENodeType.Brace)
                                {
                                    currentNode.SetBlockNode(next2Node);
                                    isBlock = true;
                                    pnode.parseIndex += 3;
                                    break;
                                }
                            }
                        }
                        else if (next2Node?.nodeType == ENodeType.Brace)
                        {
                            currentNode.SetBlockNode(next2Node);
                            isBlock = true;
                            pnode.parseIndex += 2;
                            break;
                        }
                    }
                }
                else if (nextNode.nodeType == ENodeType.LineEnd)
                {
                    if (ProjectManager.isUseForceSemiColonInLineEnd)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 在解析namespace 中，需要强制;号结束");
                        break;
                    }
                    else
                        break;
                }
                else if (nextNode.nodeType == ENodeType.SemiColon)
                {
                    break;
                }
            }
            FileMetaNamespace fmn = new FileMetaNamespace(currentNode, namespaceNode);

            if (isBlock)        //是否使用 namespace N{}的格式 如果不是{}格式，认为是搜索模式
            {
                m_FileMeta.AddFileDefineNamespace(fmn);
                AddParseNamespaceNodeInfo(fmn);
                ParseNamespaceOrTopClass(currentNode.blockNode);
                m_CurrentNodeInfoStack.Pop();
            }
            else
            {
                Log.AddNodeLog(LID.ShowExtendMessage, currentNode.token, $"现在不允许 namespace {fmn.name};这种的语法了");
            }
            //else
            //{
            //    m_FileMeta.AddFileSearchNamespace(fmn);
            //}
        }
        // 只解析 在全局文件下的 namespace 下 的 还有就是文件class
        public void ParseNamespaceOrTopClass(Node pnode)
        {
            Node braceNode = pnode.blockNode;
            List<Node> nodeList = new List<Node>();
            int index = pnode.parseIndex;
            Node curNode = null;
            bool isCanAdd = false;
            Node nextNode = null;
            Node block = null;

            List<FileMetaAttributeSyntax> attrs = new List<FileMetaAttributeSyntax>();
            int isClass = 0;        //0 unknows 1 class 2namespace
            for (index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                ParseLeadingAttributes(pnode, ref index, attrs);

                curNode = pnode.childList[index++];

                //if (curNode.token?.type == ETokenType.At)
                //{
                //    // attributes at file root are not allowed
                //    Log.AddNodeLog(LID.ShowExtendMessage, curNode.token, "Error @Attribute 不允许出现在文件头级(只能在 namespace{} / class{} 内)");
                //    continue;
                //}
                if (curNode.nodeType == ENodeType.Key)
                {
                    if (curNode.token.type == ETokenType.Namespace)
                    {
                        isClass = 2;
                    }
                    else if (curNode.token.type == ETokenType.Class
                        || curNode.token.type == ETokenType.Data
                        || curNode.token.type == ETokenType.Enum)
                    {
                        isClass = 1;
                    }
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Comma)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Colon)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    nextNode = null;
                    if (index < pnode.childList.Count)
                    {
                        nextNode = pnode.childList[index];
                    }
                    if (nextNode?.nodeType == ENodeType.Brace)
                    {
                        if (isClass == 0)
                        {
                            isClass = 1;
                        }
                        block = nextNode;
                        index++;
                        isCanAdd = true;
                        break;
                    }
                }
                else if (curNode.nodeType == ENodeType.Brace)
                {
                    isCanAdd = true;
                    if (isClass == 0)
                    {
                        isClass = 1;
                    }
                    block = curNode;
                    break;
                }
                else if (curNode.nodeType == ENodeType.Comment)
                {
                    continue;
                }
                else if (curNode.nodeType == ENodeType.SemiColon)
                {
                    break;
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, curNode.token, "Error 不允许在解释Class的时候，有错误 的语法--------------------" + curNode.token?.ToLexemeAllString());
                }
            }
            pnode.parseIndex = index;

            if (isCanAdd)
            {
                if (isClass == 1)
                {
                    AddFileMetaClasss(block, nodeList, attrs);
                    ParseNamespaceOrTopClass(pnode);
                }
                else if (isClass == 2)
                {
                    if (nodeList.Count == 2)
                    {
                        bool isFile = currentNodeInfo.parseType == EParseNodeType.File;
                        FileMetaNamespace fmn = new FileMetaNamespace(nodeList[0], nodeList[1]);
                        AddParseNamespaceNodeInfo(fmn);
                        if (block != null)
                        {
                            ParseNamespaceOrTopClass(block);
                        }
                        if (isFile)
                        {
                            if (block != null)
                            {
                                m_FileMeta.AddFileDefineNamespace(fmn);
                            }
                            else
                            {
                                Log.AddNodeLog(LID.ShowExtendMessage, nodeList[0].token, "现在不允许 namespace N1;这种的语法了");
                            }

                        }
                        if(attrs.Count > 0 )
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, nodeList[0].token, "在namespace不允许有attribute");
                        }
                        m_CurrentNodeInfoStack.Pop();
                        ParseNamespaceOrTopClass(pnode);
                    }
                    else
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, curNode.token, "Error 对于 namespace A.B{}的格式 多了一个参数!1");
                    }
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, curNode.token, "Error 没有发现是Class还是Namespace的关键字!");
                }
            }
        }
        public void ParseClassNode(Node pnode)
        {
            if (pnode.parseIndex >= pnode.childList.Count)
            {
                return;
            }

            List<Node> nodeList = new List<Node>();
            Node nextNode = null;
            int index = pnode.parseIndex;

            List<FileMetaAttributeSyntax> attrs = new List<FileMetaAttributeSyntax>();

            int parseType = 0;      // 1->是类class\n{}  2->函数 init()\n{}      3->变量  int a;  int a=20; a = 20; a = {}\n a = {};
            Node block = null;
            for (index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                // parse and stash leading attributes (only valid in class/namespace blocks)
                ParseLeadingAttributes(pnode, ref index, attrs);

                if (index >= pnode.childList.Count)
                {
                    break;
                }

                var peekNode = pnode.childList[index];
                if (peekNode.nodeType == ENodeType.Key && peekNode.token?.type == ETokenType.TypeAlias)
                {
                    if (m_FileMeta.path == null || !m_FileMeta.path.EndsWith(".sp", StringComparison.OrdinalIgnoreCase)
                        || currentNodeInfo?.codeClass == null
                        || !string.Equals(currentNodeInfo.codeClass.name, "Project", StringComparison.Ordinal))
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 类体内的 typealias 仅允许出现在 .sp 工程的 Project 类中");
                        index++;
                        continue;
                    }
                    int ni = ConsumeTypeAliasAt(pnode, index, true);
                    if (ni > index)
                        index = ni;
                    else
                        index++;
                    continue;
                }

                var curNode = pnode.childList[index++];
                if (curNode.nodeType == ENodeType.Key)
                {
                    if (curNode.token.type == ETokenType.Class
                        || curNode.token.type == ETokenType.Enum
                        || curNode.token.type == ETokenType.Data)
                    {
                        parseType = 1;
                    }
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Symbol)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.ConstValue)
                {
                    nodeList.Add(curNode);
                }
                //else if( curNode.nodeType == ENodeType.Angle )
                //{
                //    nodeList.Add(curNode);
                //}
                else if (curNode.nodeType == ENodeType.Assign)
                {
                    nodeList.Add(curNode);
                    parseType = 3;
                }
                else if (curNode.nodeType == ENodeType.Comma)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Colon)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Par)   //Class1()
                {
                    nodeList.Add(curNode);
                    //if (parseType == 0)
                    //    parseType = 2;
                }
                else if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    nodeList.Add(curNode);
                    if (curNode.parNode != null && parseType == 0 )
                    {
                        parseType = 2;
                    }
                }
                else if (curNode.nodeType == ENodeType.SemiColon)
                {
                    if (parseType == 3 || parseType == 2)
                    {
                        break;
                    }
                    else
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, curNode.token, "Error StructParseFrame.ParseClassNode 解析的类后边不用使用;号结尾!! "
                            + "一般是只定义了类变量，没有赋值，正常后边应该可以使用=null赋值");
                        break;
                    }
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    nextNode = null;
                    if (index < pnode.childList.Count)
                    {
                        nextNode = pnode.childList[index];
                        if (nextNode.nodeType == ENodeType.Brace)
                        {
                            block = nextNode;
                            index++;
                            break;
                        }
                        if (!ProjectManager.isUseForceSemiColonInLineEnd)
                        {
                            if (parseType == 3 || parseType == 2)
                            {
                                break;
                            }
                        }
                    }
                }
                else if (curNode.nodeType == ENodeType.Brace)
                {
                    block = curNode;
                    if (parseType == 3 || parseType == 2)
                    {
                        nodeList.Add(curNode);
                    }
                    if (index < pnode.childList.Count)
                    {
                        var nextCurNode = pnode.childList[index];
                        if (nextCurNode.nodeType == ENodeType.SemiColon
                            || nextCurNode.nodeType == ENodeType.LineEnd)
                        {
                            index++;
                        }
                    }
                    break;
                }
                else if (curNode.nodeType == ENodeType.QuestionMark)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Bracket)
                {
                    nodeList[nodeList.Count - 1].AddBracketNode(curNode);
                }
                else if (curNode.nodeType == ENodeType.Comment)
                {
                    break;
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, curNode?.token, "Error ParseClassNode 不允许2在解释Class的时候，有错误 的语法--------------------" + curNode.token?.ToLexemeAllString());
                }
            }
            pnode.parseIndex = index;

            if (parseType == 0)
            {
                parseType = 1;
            }
            if (parseType == 1)
            {
                if (nodeList.Count > 0 && block != null)
                {
                    // attach attributes to class
                    var cpc = new FileMetaClass(m_FileMeta, nodeList);
                    cpc.AddAttributes(attrs);
                    AddParseClassNodeInfo(cpc);

                    if (cpc.isEnum)
                    {
                        ParseEnumNode(block);
                    }
                    else if (cpc.isData)
                    {
                        ParseDataNode(block);
                    }
                    else
                    {
                        ParseClassNode(block);
                    }

                    m_CurrentNodeInfoStack.Pop();
                }
            }
            else if (parseType == 2)
            {
                // Reject `interface` used as a function modifier inside class bodies
                bool hasInterfaceModifier = false;
                Token interfaceTok = null;
                foreach (var n in nodeList)
                {
                    if (n.nodeType == ENodeType.Key && n.token?.type == ETokenType.Interface)
                    {
                        hasInterfaceModifier = true;
                        interfaceTok = n.token;
                        break;
                    }
                }
                if (hasInterfaceModifier)
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, interfaceTok, "Error class 内部不允许使用 interface 修饰函数");
                    m_CurrentNodeInfoStack.Pop();
                    return;
                }

                var cpf = new FileMetaMemberFunction(m_FileMeta, block, nodeList);
                cpf.AddAttributes(attrs);
                AddParseFunctionNodeInfo(cpf);

                if (block != null)
                {
                    ParseSyntax(block);
                }

                m_CurrentNodeInfoStack.Pop();
            }
            else if (parseType == 3)
            {
                FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, nodeList);
                fmmd.AddAttributes(attrs);

                if (currentNodeInfo.parseType == EParseNodeType.Class)
                {
                    currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, nodeList[0]?.token, "在fileMetaMemberVariable 不在class里边");
                }
            }
            ParseClassNode(pnode);
        }
        public void ParseDataBracketNode(Node bracketNode)
        {
            int index = bracketNode.parseIndex;
            for (index = bracketNode.parseIndex; index < bracketNode.childList.Count;)
            {
                var curNode = bracketNode.childList[index++];
                if (curNode.nodeType == ENodeType.Comment
                    || curNode.nodeType == ENodeType.LineEnd)
                {
                    continue;
                }
                if (curNode.nodeType == ENodeType.Brace)  //Class1 [{},{}]
                {
                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, FileMetaMemberData.EMemberDataType.Data);

                    AddParseDataInfo(fmmd);

                    ParseDataNode(curNode, true);

                    m_CurrentNodeInfoStack.Pop();
                }
                else if (curNode.nodeType == ENodeType.Symbol &&
                    (curNode.token.type == ETokenType.Plus
                    || curNode.token.type == ETokenType.Minus))
                {
                    if (index + 1 < bracketNode.childList.Count)
                    {
                        var nextNode = bracketNode.childList[index];
                        if (nextNode.nodeType == ENodeType.ConstValue)   // ["stringValue","Stvlue"]
                        {
                            index++;
                            List<Node> list = new List<Node>() { curNode, nextNode };

                            FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, null, null, list, false, FileMetaMemberData.EMemberDataType.ConstValue);

                            currentNodeInfo.codeData.AddFileMemberData(fmmd);
                        }
                        else
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 在+-符前边不允许有其它非const类型存在!");
                            continue;
                        }
                    }
                }
                else if (curNode.nodeType == ENodeType.ConstValue)   // ["stringValue","Stvlue"]
                {
                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, FileMetaMemberData.EMemberDataType.ConstValue);

                    currentNodeInfo.codeData.AddFileMemberData(fmmd);
                }
                else if (curNode.nodeType == ENodeType.IdentifierLink)   // [Class1(),Class2()]
                {
                    while (index < bracketNode.childList.Count)
                    {
                        var nextNode = bracketNode.childList[index];
                        if (nextNode == null)
                        {
                            index++;
                            continue;
                        }
                        if (nextNode.nodeType == ENodeType.Comment
                            || nextNode.nodeType == ENodeType.LineEnd)
                        {
                            index++;
                            continue;
                        }
                        if (nextNode.nodeType == ENodeType.Par)
                        {
                            curNode.SetParNode(nextNode);
                            index++;
                            continue;
                        }
                        if (nextNode.nodeType == ENodeType.Brace)
                        {
                            curNode.SetBlockNode(nextNode);
                            index++;
                            continue;
                        }
                        break;
                    }

                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, FileMetaMemberData.EMemberDataType.Class);

                    AddParseDataInfo(fmmd);
                    if (curNode.blockNode != null)
                    {
                        ParseDataNode(curNode.blockNode, true);
                    }
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (curNode?.nodeType == ENodeType.Bracket) // [[],[]]
                {
                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, FileMetaMemberData.EMemberDataType.Array);

                    AddParseDataInfo(fmmd);

                    ParseDataBracketNode(curNode);

                    m_CurrentNodeInfoStack.Pop();
                }
                else if (curNode.nodeType == ENodeType.Comma)
                {
                    continue;
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    continue;
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                    continue;
                }
            }
            bracketNode.parseIndex = index;
        }
        private Node GetNextDataMeaningNode(Node parentNode, int startIndex)
        {
            for (int i = startIndex; i < parentNode.childList.Count; i++)
            {
                var node = parentNode.childList[i];
                if (node == null)
                {
                    continue;
                }
                if (node.nodeType == ENodeType.LineEnd || node.nodeType == ENodeType.Comment)
                {
                    continue;
                }
                return node;
            }
            return null;
        }
        /// <summary>
        /// 解析 Data 花括号内成员。
        /// requireCommaSeparator 为 false：顶层 <c>data Name {{ ... }}</c>，可用换行/分号（及可选逗号）分隔；
        /// 为 true：<c>= {{}}</c> 或 <c>[]</c> 内匿名字块，必须用逗号分隔成员，单靠换行不结束上一成员。
        /// </summary>
        public void ParseDataNode(Node pnode, bool requireCommaSeparator = false)
        {
            Node curParentNode = pnode;
            int index = curParentNode.parseIndex;

            bool isParseEnd = false;
            Node assignNode = null;
            List<Node> frontList = new List<Node>();
            List<Node> backList = new List<Node>();
            for (index = curParentNode.parseIndex; index < curParentNode.childList.Count;)
            {
                var curNode = curParentNode.childList[index++];

                if (curNode == null || curNode.nodeType == ENodeType.Comment)
                {
                    continue;
                }

                Node nextNode = null;
                if (index < curParentNode.childList.Count)
                {
                    nextNode = curParentNode.childList[index];
                }

                if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    if (assignNode == null)
                    {
                        frontList.Add(curNode);                 // curNode =(assignNode) 
                    }
                    else
                    {
                        backList.Add(curNode);                  // frontNode =(assignNode)  curNode

                        for (int j = index; j < curParentNode.childList.Count;)
                        {
                            var next2Node = curParentNode.childList[j++];
                            if (next2Node == null) continue;

                            if (next2Node.nodeType == ENodeType.Par)   //Class1()
                            {
                                curNode.SetParNode(next2Node);
                                index = j;
                                isParseEnd = true;
                                if (j < curParentNode.childList.Count)
                                {
                                    var next3Node = curParentNode.childList[j];
                                    if (next3Node == null) continue;
                                    if (next3Node.nodeType == ENodeType.LineEnd)
                                    {
                                        if (j + 1 < curParentNode.childList.Count)
                                        {
                                            var next4Node = curParentNode.childList[j + 1];
                                            if (next4Node == null) continue;
                                            if (next4Node.nodeType == ENodeType.Brace)
                                            {
                                                curNode.SetBlockNode(next4Node);
                                                isParseEnd = true;
                                                index = j + 2;
                                                break;
                                            }
                                            else
                                            {
                                                isParseEnd = true;
                                                index = j + 1;
                                                break;
                                            }
                                        }
                                    }
                                    else if (next3Node.nodeType == ENodeType.Brace)
                                    {
                                        curNode.SetBlockNode(next3Node);
                                        isParseEnd = true;
                                        index = j + 1;
                                        break;
                                    }
                                }

                                continue;
                            }
                            //else if (next2Node.nodeType == ENodeType.Angle)   //Class1<T>   Func<T>( T t );  array<int> arr1;
                            //{
                            //    curNode.angleNode = next2Node;
                            //    index = j;
                            //    continue;
                            //}
                            else if (next2Node.nodeType == ENodeType.Brace)
                            {
                                curNode.SetBlockNode(next2Node);
                                index = j;
                                isParseEnd = true;
                                break;
                            }
                            if (ProjectManager.isUseForceSemiColonInLineEnd)
                            {
                                if (next2Node.nodeType == ENodeType.SemiColon)
                                {
                                    isParseEnd = true;
                                }
                            }
                            else
                            {
                                if (next2Node.nodeType == ENodeType.SemiColon)
                                {
                                    isParseEnd = true;
                                }
                                else if (next2Node.nodeType == ENodeType.LineEnd)
                                {
                                    isParseEnd = true;
                                }
                                else if (next2Node.nodeType == ENodeType.Comma)
                                {
                                    isParseEnd = true;
                                    break;
                                }
                                else
                                {
                                    Log.AddNodeLog(LID.NodeDataParseNotfoundIdentify, curNode?.token, curNode?.token?.ToLexemeAllString());
                                }
                            }
                        }

                        if (isParseEnd)
                        {
                            FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, frontList, assignNode, backList, true, FileMetaMemberData.EMemberDataType.Class);
                            frontList.Clear();
                            backList.Clear();
                            assignNode = null;
                            isParseEnd = false;
                            AddParseDataInfo(fmmd);
                            // TypeName() { ... } 与匿名 / 嵌套 data 一致：花括号内按逗号分隔解析成员（含嵌套 Type2(){}）
                            if (curNode.blockNode != null)
                            {
                                ParseDataNode(curNode.blockNode, true);
                            }
                            m_CurrentNodeInfoStack.Pop();
                        }
                    }
                }
                else if (curNode.nodeType == ENodeType.Key
                    && curNode.token?.type == ETokenType.Const)
                {
                    if (assignNode != null)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error data 成员的 const 关键字只能出现在赋值号前");
                        continue;
                    }
                    if (frontList.Count > 0)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error data 成员定义中 const 必须位于名称之前");
                        continue;
                    }
                    frontList.Add(curNode);
                    continue;
                }
                else if (curNode.nodeType == ENodeType.Symbol)
                {
                    if (assignNode == null)
                    {
                        frontList.Add(curNode);
                    }
                    else
                    {
                        backList.Add(curNode);
                    }
                    continue;
                }
                else if (curNode.nodeType == ENodeType.Assign) //varname = 1/"1"/-20/{}/[]/Class1(){}/Data1(){}
                {
                    assignNode = curNode;
                    Node blockNode = null;

                    if (nextNode == null)
                    {
                        Log.AddNodeLog(LID.FileMetaNeedAssignAfterEqualSyntax, curNode.token, "after", frontList[frontList.Count - 1].token?.ToLexemeAllString());
                        continue;
                    }

                    int parseType = 0;
                    //=号后边第一位，必须是idetifier 或者是 constValue值，  如果折行，只允许 \n{}  \
                    if (nextNode.nodeType == ENodeType.ConstValue) // a = 10 不允许折行  
                    {
                        index++;
                        backList.Add(nextNode);
                    }
                    else if (nextNode.nodeType == ENodeType.IdentifierLink)
                    {
                    }
                    else if (nextNode.nodeType == ENodeType.Symbol &&
                        (nextNode.token.type == ETokenType.Plus
                            || nextNode.token.type == ETokenType.Minus))
                    {
                        if (index + 1 < curParentNode.childList.Count)
                        {
                            var next2Node = curParentNode.childList[index + 1];
                            if (next2Node.nodeType == ENodeType.ConstValue)
                            {
                                index += 2;
                                backList.Add(nextNode);
                                backList.Add(next2Node);
                            }
                            else
                            {
                                Log.AddNodeLog(LID.ShowExtendMessage, "Error 如果是 x=-??的形式，在符号后边");
                            }
                        }
                        else
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 如果是 x=-??的形式，在符号后边");
                        }
                    }
                    else if (nextNode.nodeType == ENodeType.Brace)
                    {
                        index++;
                        parseType = 1;
                        blockNode = nextNode;
                    }
                    else if (nextNode.nodeType == ENodeType.Bracket)
                    {
                        index++;
                        parseType = 2;
                        blockNode = nextNode;
                    }
                    else if (nextNode.nodeType == ENodeType.LineEnd)
                    {
                        index++;
                        var next2Node = index < curParentNode.childList.Count ? curParentNode.childList[index] : null;
                        // a = /n{}  a = /n[]
                        if (next2Node?.nodeType == ENodeType.Brace)
                        {
                            index++;
                            parseType = 1;
                            blockNode = next2Node;
                        }
                        else if (next2Node?.nodeType == ENodeType.Bracket)
                        {
                            index++;
                            parseType = 2;
                            blockNode = next2Node;
                        }
                        else
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "Error 在定义Data数据的时候，如果有折行，只允许 =\n{} =\n[] 两种形式! ");
                        }
                    }
                    else
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "Error 在定义Data数据的时候，不允许=号后边有其它形式的存在");
                    }

                    if (parseType > 0)
                    {
                        FileMetaMemberData.EMemberDataType emdt = parseType switch
                        {
                            1 => FileMetaMemberData.EMemberDataType.Data,
                            2 => FileMetaMemberData.EMemberDataType.Array,
                            _ => FileMetaMemberData.EMemberDataType.Data
                        };
                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, frontList, assignNode, backList, true, emdt);
                        frontList.Clear();
                        isParseEnd = false;
                        assignNode = null;
                        AddParseDataInfo(fmmd);
                        if (parseType == 1)
                        {
                            ParseDataNode(blockNode, true);
                        }
                        else if (parseType == 2)
                        {
                            ParseDataBracketNode(blockNode);
                        }
                        m_CurrentNodeInfoStack.Pop();
                    }
                    continue;
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    // 顶层 data 定义：换行可结束匿名 const 一段；匿名/嵌套 {} 内必须由逗号分隔，换行当空白跳过
                    if (!requireCommaSeparator)
                    {
                        isParseEnd = true;
                    }
                }
                else if (curNode.nodeType == ENodeType.Comma)
                {
                    isParseEnd = true;
                }
                else if (curNode.nodeType == ENodeType.SemiColon)
                {
                    if (!requireCommaSeparator)
                    {
                        isParseEnd = true;
                    }
                    else if (assignNode != null || frontList.Count > 0)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, "匿名或嵌套的 data {{}} 内请使用英文逗号 ',' 分隔成员，不要使用 ';'");
                    }
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, "Error 报错，不允许 解析Data有其它的类型出现!" + curNode.token.ToLexemeAllString());
                }

                if (isParseEnd)
                {
                    if (frontList.Count > 0)
                    {
                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, frontList, assignNode, backList, true, FileMetaMemberData.EMemberDataType.ConstValue);
                        frontList.Clear();
                        backList.Clear();
                        assignNode = null;
                        AddParseDataInfo(fmmd);
                        m_CurrentNodeInfoStack.Pop();
                    }
                    isParseEnd = false;
                }
            }

            if (frontList.Count > 0)
            {
                FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, frontList, assignNode, backList, true, FileMetaMemberData.EMemberDataType.ConstValue);
                frontList.Clear();
                backList.Clear();
                assignNode = null;
                AddParseDataInfo(fmmd);
                m_CurrentNodeInfoStack.Pop();
            }
            curParentNode.parseIndex = index;
        }
        public void ParseEnumNode(Node pnode)
        {
            if (pnode.parseIndex >= pnode.childList.Count)
                return;

            var action = delegate (List<Node> addnode)
            {
                for (int i = 0; i < addnode.Count; i++)
                {
                    var curNodexxx = addnode[i];
                    if (curNodexxx.nodeType == ENodeType.Key
                        && curNodexxx.token.type == ETokenType.Enum)
                    {
                        Log.AddNodeLog(LID.ShowExtendMessage, curNodexxx.token, "error 不允许在enum 内容里边再嵌套enum");
                        return;
                    }
                }
                FileMetaMemberVariable cpv = new FileMetaMemberVariable(m_FileMeta, addnode);

                AddParseVariableInfo(cpv);
            };

            Node blockNode = null;
            List<Node> nodeList = new List<Node>();
            int index = pnode.parseIndex;
            bool isParse = false;
            bool isAssign = false;
            for (index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                var curNode = pnode.childList[index++];

                Node nextNode = null;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }

                if (curNode.nodeType == ENodeType.IdentifierLink)  //Enum1
                {
                    if (isAssign)
                    {
                        nodeList.Add(curNode);
                        if (curNode.parNode != null)  //Enum1()
                        {
                            if (nextNode.nodeType == ENodeType.LineEnd)
                            {
                                index += 1;
                                isParse = true;
                                if (index + 1 < pnode.childList.Count)
                                {
                                    nextNode = pnode.childList[index + 1];
                                    if (nextNode?.nodeType == ENodeType.Brace)
                                    {
                                        index += 2;
                                        curNode.SetBlockNode(nextNode);
                                        blockNode = nextNode;
                                    }
                                }
                            }
                            else if (nextNode?.nodeType == ENodeType.Brace)  //Class1(){}的结构
                            {
                                index += 2;
                                blockNode = nextNode;
                                isParse = true;
                            }
                            else if (nextNode?.nodeType == ENodeType.SemiColon)
                            {
                                index += 1;
                                isParse = true;
                            }
                        }
                        //else if (nextNode?.nodeType == ENodeType.LeftAngle)    // Class1<>
                        //{
                        //    var next2Node = pnode.childList[index + 1];
                        //    if (next2Node?.nodeType == ENodeType.Brace)  // Class1<int>(){}
                        //    {
                        //        index += 2;
                        //        //curNode.angleNode = nextNode;
                        //        curNode.SetBlockNode(next2Node);
                        //        blockNode = curNode;
                        //    }
                        //    else
                        //    {
                        //        index++;
                        //        //curNode.angleNode = nextNode;
                        //    }
                        //}
                        else if (nextNode?.nodeType == ENodeType.LineEnd
                            || nextNode?.nodeType == ENodeType.SemiColon)
                        {
                        }
                        else
                        {
                            Log.AddNodeLog(LID.ShowExtendMessage, "在解析enum member 中 成员变量 如果是identifier格式，则后边不允许跟当前格式");
                        }
                    }
                    else
                    {
                        nodeList.Add(curNode);
                    }
                }
                else if (curNode.nodeType == ENodeType.SemiColon)
                {
                    pnode.parseIndex = index;
                    isParse = true;
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    if (ProjectManager.isUseForceSemiColonInLineEnd)
                    {
                        continue;
                    }
                    else
                    {
                        isParse = true;
                    }
                }
                else if (curNode.nodeType == ENodeType.Assign)
                {
                    nodeList.Add(curNode);
                    isAssign = true;
                }
                else if (curNode.nodeType == ENodeType.ConstValue)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Key && curNode.token.type == ETokenType.Mut)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Comment)
                {

                }
                else if (curNode.nodeType == ENodeType.Brace)
                {
                    if (isAssign)
                    {
                        nodeList.Add(curNode);
                    }
                }
                else
                {
                    Log.AddNodeLog(LID.ShowExtendMessage, curNode?.token, $"不允许有{curNode.token.lexeme.ToString()}其它形式的存在!");
                }

                if (isParse)
                {
                    if (nodeList.Count > 0)
                    {
                        action.Invoke(nodeList);
                        nodeList.Clear();
                    }
                    isParse = false;
                    isAssign = false;
                }

                #region 扩展解析其它方式
                //if (curNode?.nodeType == ENodeType.Par)  //类中的带()的结构
                //{
                //    if (nextNode?.nodeType == ENodeType.SemiColon)
                //    {
                //        index+= (lineCount+1);
                //        curNode.finalNode.parNode = nextNode;
                //        nodeList.Add(curNode);
                //        break;
                //    }
                //    else if (nextNode?.nodeType == ENodeType.LineEnd)
                //    {
                //        index++;
                //        curNode.finalNode.parNode = nextNode;
                //        nodeList.Add(curNode);
                //        break;
                //    }
                //    else
                //    {
                //        Debug.WriteLine("Error 不允许在Class1()后 不能增加其它内容!!");
                //        break;
                //    }
                //}
                //else if (curNode?.nodeType == ENodeType.Brace)     //匿名对象
                //{
                //    bool isAssign = false;
                //    for (int m = 0; m < nodeList.Count; m++)
                //    {
                //        if (nodeList[m].nodeType == ENodeType.Assign)
                //        {
                //            isAssign = true;
                //            break;
                //        }
                //    }
                //    if (curNode?.nodeType == ENodeType.Assign) isAssign = true;
                //    if (isAssign)
                //    {
                //        blockNode = curNode;
                //        nodeList.Add(curNode);
                //        break;
                //    }
                //    else
                //    {
                //        Debug.WriteLine("Error 在语句中直接使用{}不符合语法要求!!!");
                //    }
                //}
                #endregion
            }
        }
        void AddFileMetaClasss(Node blockNode, List<Node> nodeList, List<FileMetaAttributeSyntax> attri )
        {
            FileMetaClass cpc = new FileMetaClass(m_FileMeta, nodeList);
            cpc.AddAttributes(attri);

            AddParseClassNodeInfo(cpc);

            if (cpc.isEnum)
            {
                ParseEnumNode(blockNode);
            }
            else if (cpc.isData)
            {
                ParseDataNode(blockNode);
            }
            else
            {
                ParseClassNode(blockNode);
            }

            m_CurrentNodeInfoStack.Pop();
        }
        public void ParseSyntax(Node pnode)
        {
            Node node = pnode.parseCurrent;
            if (node == null) return;

            if (node.nodeType == ENodeType.Brace)
            {
                FileMetaBlockSyntax cps = new FileMetaBlockSyntax(m_FileMeta, node.token, node.endToken);

                AddParseSyntaxNodeInfo(cps, true);

                ParseSyntax(node);

                m_CurrentNodeInfoStack.Pop();

                pnode.parseIndex++;
            }
            else
            {
                HandleCreateFileMetaSyntaxByPNode(pnode);
            }
            ParseSyntax(pnode);
        }

        /*
        private static void HandleNodeSingleLine_Recursive(Node node)
        {
            if (node == null) return;
            if (node.childList == null || node.childList.Count == 0) return;

            node.parseIndex = 0;
            var newList = HandleNodeSingleLine(node.childList);
            node.SetChildList(newList);
            node.parseIndex = 0;
        }
        public static List<Node> HandleBeforeNode(Node node )
        {
            List<Node> handleBeforeList = new List<Node>();

            _HandleExpressNodeProcess(node, null );
            DelHandleNostList(node);
            handleBeforeList = node.childList;

            return handleBeforeList;
        }
        public static List<Node> HandleExpressNode( Node node )
        {
            List<Node> handleBeforeList = new List<Node>();

            bool flag = IsCommonExpressNode(node);

            if( flag )
            {
                handleBeforeList = node.childList;
            }
            else
            {
                _HandleExpressNodeProcess(node, null );
            }
            DelHandleNostList(node);
            handleBeforeList = node.childList;

            return handleBeforeList;
        }
        public static void DelHandleNostList( Node node )
        {
            List<Node> list = new List<Node>();
            for( int i = 0; i < node.childList.Count; i++ )
            {
                DelHandleNostList(node.childList[i]);
                if ( node.childList[i].isDel == false )
                {
                    list.Add(node.childList[i]);
                }
                else
                {
                    node.childList[i].parseIndex = 0;
                }
            }
            node.SetChildList(list);
        }
        //判断>> 还是> > 具体是否是表达式
        private static bool IsCommonExpressNode( Node node )
        {
            int isAngleFlagIndex = 0;
            for( int i = 0; i < node.childList.Count; i++ )
            {
                var cnode = node.childList[i];
                if(isAngleFlagIndex > 0 )
                {
                    if(cnode.nodeType ==  ENodeType.RightAngle )
                    {
                        if( cnode.extendLinkNodeList.Count > 0 )
                        {
                            return false;
                        }
                        isAngleFlagIndex--;
                    }
                    else if (cnode.nodeType == ENodeType.Key)
                    {
                        return true;
                    }
                    else if (cnode.nodeType == ENodeType.ConstValue)
                    {
                        return true;
                    }
                    else if (cnode.nodeType == ENodeType.Symbol)
                    {
                        return true;
                    }
                    else if (cnode.nodeType == ENodeType.Assign)
                    {
                        return true;
                    }
                }
                else
                {
                    if( cnode.nodeType == ENodeType.RightAngle )
                    {
                        return true;
                    }
                }

                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    if (i + 1 < node.childList.Count)
                    {
                        var nnode = node.childList[i + 1];
                        if (nnode.nodeType == ENodeType.LeftAngle)
                        {
                            isAngleFlagIndex++;
                        }
                    }
                }
                if (cnode.nodeType == ENodeType.Key )
                {
                    if (i + 1 < node.childList.Count)
                    {
                        var nnode = node.childList[i + 1];
                        if (nnode.nodeType == ENodeType.LeftAngle)
                        {
                            isAngleFlagIndex++;
                        }
                    }
                }
            }
            if(isAngleFlagIndex != 0 )
            { return true; }

            return false;
        }
        //处理 ident <> () {} [] . 的结合 与子元素的统一处理
        private static void _HandleExpressNodeProcess(Node node, Node inputFinaleNode )
        {
            if( node.parseIndex < 0 || node.parseIndex >= node.childList.Count )
            {
                return;
            }

            int index = node.parseIndex;
            Node currentExpressNode = node.parseCurrent;
            if( inputFinaleNode == null )
            {
                node.parseIndex++;
                _HandleExpressNodeProcess(node, currentExpressNode);
                return;
            }

            Node finalNode = inputFinaleNode.finalNode;
            if (finalNode?.nodeType == ENodeType.IdentifierLink
                || finalNode?.token?.type == ETokenType.New ) //Class1???;
            {
                if (currentExpressNode.nodeType == ENodeType.LeftAngle )       //Class<>??;
                {
                    HandleAngleExpressNode(node, finalNode);
                    _HandleExpressNodeProcess(node, finalNode);
                    return;
                }
                else if (currentExpressNode.nodeType == ENodeType.Par)             //Class()?;
                {
                    node.parseIndex++;
                    finalNode.SetParNode( currentExpressNode );
                    currentExpressNode.isDel = true;
                    if (currentExpressNode.extendLinkNodeList.Count > 0)
                    {
                        finalNode.SetLinkNode(currentExpressNode.extendLinkNodeList);   // Q.Map()[.Cast];
                        _HandleExpressNodeProcess(node, currentExpressNode);           //Q.Map().[Cast];
                        return;
                    }
                    else
                    {
                        _HandleExpressNodeProcess(currentExpressNode, null );
                        DelHandleNostList(currentExpressNode);
                        _HandleExpressNodeProcess(node, finalNode);
                        return;
                    }
                }
                else if( currentExpressNode.nodeType == ENodeType.Brace )           // M.Class(){};
                {
                    node.parseIndex++;
                    finalNode.SetBlockNode(currentExpressNode);
                    currentExpressNode.isDel = true;
                    _HandleExpressNodeProcess(currentExpressNode, null);
                    return;
                }
                else if( currentExpressNode.nodeType == ENodeType.Bracket )         //Class[][][][](){};
                {
                    node.parseIndex++;
                    finalNode.AddBracketNode( currentExpressNode );
                    currentExpressNode.isDel = true;

                    if( currentExpressNode.extendLinkNodeList.Count > 0 )
                    {
                        finalNode.SetLinkNode(currentExpressNode.extendLinkNodeList);   // Array[1].20;
                        _HandleExpressNodeProcess(currentExpressNode, null );                        // Array[1].Fun( 1, 2 );
                        _HandleExpressNodeProcess(node, finalNode);
                        return;
                    }
                    else
                    {
                        _HandleExpressNodeProcess(currentExpressNode, null);
                        _HandleExpressNodeProcess(node, finalNode);
                        return;
                    }
                }
            }
            node.parseIndex++;
            _HandleExpressNodeProcess( node, currentExpressNode );
        }
        private static void HandleAngleExpressNode(Node node, Node parentNode )
        {
            while (node.parseIndex < node.childList.Count)
            {
                var cnode = node.childList[node.parseIndex];               

                if (cnode.nodeType == ENodeType.LeftAngle)
                {
                    cnode.isDel = true;
                    node.parseIndex++;
                    parentNode.SetAngleNode( cnode );
                    //parentNode.AddAngleNode(cnode);
                }
                else if (cnode.nodeType == ENodeType.RightAngle)
                {
                    cnode.isDel = true;
                    parentNode.angleNode.endToken = ( cnode.token );
                    node.parseIndex++;
                    if (cnode.extendLinkNodeList?.Count > 0)
                    {
                        parentNode.SetLinkNode(cnode.extendLinkNodeList);
                    }
                    return;
                }
                else if( cnode.nodeType == ENodeType.Comma )
                {
                    cnode.isDel = true;
                    node.parseIndex++;
                    continue;
                }
                else if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    node.parseIndex++;
                    if (parentNode.angleNode != null)
                    {
                        cnode.isDel = true;
                        parentNode.angleNode.AddChild(cnode);
                    }
                    if (node.parseIndex < node.childList.Count)
                    {
                        var nextNode = node.childList[node.parseIndex];
                        if (nextNode.nodeType == ENodeType.LeftAngle)
                        {
                            HandleAngleExpressNode(node, cnode);
                        }
                        else if( nextNode.nodeType == ENodeType.Key 
                            && nextNode.token.type == ETokenType.Colon )
                        {
                            if( node.parseIndex + 1 < node.childList.Count )
                            {
                                nextNode.isDel = true;
                                var nextNode2 = node.childList[node.parseIndex + 1];
                                nextNode2.isDel = true;
                                cnode.AddChild(nextNode2);
                                node.parseIndex += 2;
                            }
                        }
                        else if( nextNode.nodeType == ENodeType.RightAngle )
                        {
                            HandleAngleExpressNode(node, parentNode);
                            return;
                        }
                        else if( nextNode.nodeType == ENodeType.Comma )
                        {
                            continue;
                        }
                        else if (nextNode.nodeType == ENodeType.Par)      //Class<>()
                        {
                            parentNode.SetParNode( nextNode );
                            nextNode.isDel = true;
                            _HandleExpressNodeProcess(nextNode, null);

                            if (nextNode.extendLinkNodeList.Count > 0)
                            {
                                parentNode.SetLinkNode(nextNode.extendLinkNodeList);
                                _HandleExpressNodeProcess(node, parentNode );
                                return;
                            }
                            else
                            {
                                parentNode = parentNode.finalNode;
                                if (node.parseIndex < node.childList.Count)
                                {
                                    var next2ExpressNode = node.childList[node.parseIndex];
                                    if (next2ExpressNode.nodeType == ENodeType.Brace)   //Class<>()?{}
                                    {
                                        parentNode.SetBlockNode( next2ExpressNode );
                                        next2ExpressNode.isDel = true;
                                        _HandleExpressNodeProcess(node, parentNode );           //Q.Map<>(){}
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            HandleAngleExpressNode(node, parentNode );
                            return;
                        }
                    }
                }
                else
                {
                    node.parseIndex++;
                    if (parentNode.angleNode != null)
                    {
                        parentNode.angleNode.AddChild(cnode);
                    }
                    else
                    {
                        parentNode.AddChild(cnode);
                    }
                }
            }
        }

        //private static void HandleAngleNode(Node node, Node parentNode)
        //{
        //    while (node.parseIndex < node.childList.Count)
        //    {
        //        var cnode = node.childList[node.parseIndex];
        //        cnode.isDel = true;
        //        if (cnode.nodeType == ENodeType.LeftAngle)
        //        {
        //            node.parseIndex++;
        //            parentNode.angleNode = cnode;
        //        }
        //        else if (cnode.nodeType == ENodeType.RightAngle)
        //        {
        //            parentNode.angleNode.endToken = cnode.token;
        //            node.parseIndex++;
        //            return;
        //        }
        //        else if (cnode.nodeType == ENodeType.IdentifierLink)
        //        {
        //            node.parseIndex++;
        //            parentNode.angleNode.AddChild(cnode);
        //            if (node.parseIndex < node.childList.Count)
        //            {
        //                var nextNode = node.childList[node.parseIndex];
        //                if (nextNode.nodeType == ENodeType.LeftAngle)
        //                {
        //                    HandleAngleNode(node, cnode);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            parentNode.angleNode.AddChild(cnode);
        //            node.parseIndex++;
        //        }
        //    }
        //}

        ////处理 ident <> () {} [] . 的结合 与子元素的统一处理
        //private static void _HandleBeforeNodeProcess(Node node, Node inputFinaleNode = null)
        //{
        //    int index = node.parseIndex;
        //    if (index < 0 || index >= node.childList.Count)
        //        return;

        //    Node currentExpressNode = node.parseCurrent;
        //    if (inputFinaleNode == null)
        //    {
        //        node.parseIndex++;
        //        _HandleBeforeNodeProcess(node, currentExpressNode);
        //        return;
        //    }

        //    Node finalNode = inputFinaleNode.finalNode;
        //    if (finalNode?.nodeType == ENodeType.IdentifierLink) //Class1????
        //    {
        //        if (currentExpressNode.nodeType == ENodeType.LeftAngle )       //Class<>????
        //        {
        //            HandleAngleNode(node, finalNode);
        //            _HandleBeforeNodeProcess(node, inputFinaleNode);
        //            return;
        //        }
        //        else if (currentExpressNode.nodeType == ENodeType.Par)             //Class()?;
        //        {
        //            finalNode.parNode = currentExpressNode;
        //            currentExpressNode.isDel = true;
        //            node.parseIndex++;
        //            _HandleBeforeNodeProcess(currentExpressNode);

        //            if (currentExpressNode.extendLinkNodeList.Count > 0)
        //            {
        //                finalNode.SetLinkNode(currentExpressNode.extendLinkNodeList);   // Q.Map()[.Cast]
        //                _HandleBeforeNodeProcess(node, currentExpressNode);           //Q.Map().[Cast]
        //                return;
        //            }
        //            else
        //            {
        //                _HandleBeforeNodeProcess(node, finalNode);
        //                return;
        //            }
        //        }
        //        else if (currentExpressNode.nodeType == ENodeType.Brace)           // M.Class(){};
        //        {
        //            finalNode.blockNode = currentExpressNode;
        //            currentExpressNode.isDel = true;
        //            node.parseIndex++;
        //            _HandleBeforeNodeProcess(currentExpressNode);
        //            _HandleBeforeNodeProcess(node, finalNode);
        //            return;
        //        }
        //        else if (currentExpressNode.nodeType == ENodeType.Bracket)         //Class[][][][](){};
        //        {
        //            finalNode.bracketNode = currentExpressNode;
        //            currentExpressNode.isDel = true;
        //            node.parseIndex++;
        //            _HandleBeforeNodeProcess(currentExpressNode);

        //            if (currentExpressNode.extendLinkNodeList.Count > 0)
        //            {
        //                finalNode.SetLinkNode(currentExpressNode.extendLinkNodeList);   // Array[1].20;
        //                _HandleBeforeNodeProcess(node, finalNode);                        // Array[1].Fun( 1, 2 );
        //                return;
        //            }
        //            else
        //            {
        //                _HandleBeforeNodeProcess(node, finalNode);
        //                return;
        //            }
        //        }
        //    }
        //    node.parseIndex++;
        //    _HandleBeforeNodeProcess(node, currentExpressNode);
        //}
        */
    }
}