//****************************************************************************
//  File:      StructParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Runtime.Intrinsics.X86;
using System.Reflection;

namespace SimpleLanguage.Compile.Parse
{
    public partial class StructParse
    {
        public enum EParseNodeType
        {
            Null,
            File,
            Namespace,
            Class,
            Function,
            Statements,
            Data,
        }
        public class ParseCurrentNodeInfo
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
                parseType = EParseNodeType.Data;
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
        protected Stack<ParseCurrentNodeInfo> m_CurrentNodeInfoStack = new Stack<ParseCurrentNodeInfo>();

        protected Node m_RootNode = null;
        public StructParse(FileMeta fm, Node node)
        {
            m_FileMeta = fm;
            m_RootNode = node;
        }
        private void AddParseFileNodeInfo()
        {
            ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(m_FileMeta);
            m_CurrentNodeInfoStack.Push(pcni);
        }
        public void AddParseNamespaceNodeInfo(FileMetaNamespace fmn)
        {
            if (currentNodeInfo.parseType == EParseNodeType.File)
            {
                currentNodeInfo.codeFile.AddFileMetaNamespace(fmn);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.Namespace)
            {
                currentNodeInfo.codeNamespace.AddFileNamespace(fmn);
            }
            m_FileMeta.AddFileMetaAllNamespace(fmn);

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
                Console.WriteLine("错误 !!1 AddParseClassNodeInfo");
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
                Console.WriteLine("错误 !!1 AddParseVariableInfo");
                return;
            }
        }
        public void AddParseDataInfo(FileMetaMemberData fmmd)
        {
            if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMemberData(fmmd);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.Data)
            {
                currentNodeInfo.codeData.AddFileMemberData(fmmd);
            }
            else
            {
                Console.WriteLine("错误 !!1 AddParseFunctionNodeInfo");
                return;
            }

            ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fmmd);

            m_CurrentNodeInfoStack.Push(pcni);
        }
        public void AddParseFunctionNodeInfo(FileMetaMemberFunction fmmf, bool isPush = true)
        {
            if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMemberFunction(fmmf);
            }
            else
            {
                Console.WriteLine("错误 !!1 AddParseFunctionNodeInfo");
                return;
            }

            if (isPush)
            {
                ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fmmf);

                m_CurrentNodeInfoStack.Push(pcni);
            }
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
                Console.WriteLine("错误 !!1 AddParseFunctionNodeInfo");
                return;
            }

            if (isAddParseCurrentNNode)
            {
                ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fms);
                m_CurrentNodeInfoStack.Push(pcni);
            }
        }        
        public void ParseRootNodeToFileMeta()
        {
            AddParseFileNodeInfo();

            ParseCommon(m_RootNode);

            var fileCode = m_CurrentNodeInfoStack.Pop();

            if (fileCode.parseType == EParseNodeType.File)
            {
#if DEBUG
                m_FileMeta.SetDeep(0);
#endif

                Console.WriteLine("解析成Code代码结构文件成功!!! 下一步，可以生产Meta文件了");

                Console.WriteLine("生成FileMeta文件成功!!! 下一步，可以 进行混合了 ");
            }
            else
            {
                Console.WriteLine("解析出现错误 ParseFile : " + currentNodeInfo.parseType.ToString());
                return;
            }
            return;
        }
        public static bool CheckEnd(Node pnode)
        {
            if (pnode.parseIndex >= pnode.childList.Count)
            {
                return true;
            }
            return false;
        }
        public void ParseCommon(Node pnode)
        {
            if (CheckEnd(pnode)) return;

            Node node = pnode.parseCurrent;

            if (node.nodeType == ENodeType.Key)
            {
                if (node.token?.type == ETokenType.Import)
                {
                    ParseImport(pnode);
                }
                else if( node.token?.type == ETokenType.Namespace )
                {
                    ParseNamespace(pnode);
                }
                else
                {
                    ParseSyntax(pnode);
                }
            }
            else if( node.nodeType == ENodeType.SemiColon || node.nodeType == ENodeType.LineEnd )
            {
                pnode.parseIndex++;
                ParseCommon(pnode);
            }
            else
            {
                bool isFile = currentNodeInfo.parseType == EParseNodeType.File;
                bool isNamespace = currentNodeInfo.parseType == EParseNodeType.Namespace;
                bool isClass = currentNodeInfo.parseType == EParseNodeType.Class;
                bool isFunction = currentNodeInfo.parseType == EParseNodeType.Function;
                bool isStatements = currentNodeInfo.parseType == EParseNodeType.Statements;
                if (isFile || isNamespace || isClass  )
                {
                    ParseClass( pnode );
                }
                else if (isFunction || isStatements )
                {
                    ParseSyntax( pnode );
                }
                else
                {
                    Console.WriteLine("Error -------------StructParse");
                }
            }
        }

        public void ParseDataNode(Node pnode)
        {
            Node bracketNode = pnode.bracketNode;
            Node braceNode = pnode.blockNode;

            if (bracketNode != null)
            {
                int index = bracketNode.parseIndex;
                for (index = bracketNode.parseIndex; index < bracketNode.childList.Count;)
                {
                    var curNode = bracketNode.childList[index++];
                    if (curNode.nodeType == ENodeType.Brace)  //Class1 [{},{}]
                    {
                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, null, FileMetaMemberData.EMemberDataType.NoNameClass);

                        AddParseDataInfo(fmmd);

                        ParseDataNode(curNode);

                        m_CurrentNodeInfoStack.Pop();
                    }
                    else if (curNode.nodeType == ENodeType.ConstValue)   // ["stringValue","Stvlue"]
                    {
                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, null, FileMetaMemberData.EMemberDataType.Value);

                        currentNodeInfo.codeData.AddFileMemberData(fmmd);

                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                        {
                            var next3Node = bracketNode.childList[index + 1];
                            if (next3Node?.nodeType != ENodeType.SemiColon)
                            {
                                Console.WriteLine("Error 应该使用;结束语句!!");
                            }
                        }
                    }
                    else if (curNode?.nodeType == ENodeType.Bracket) // [[],[]]
                    {
                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, null, FileMetaMemberData.EMemberDataType.Array);

                        AddParseDataInfo(fmmd);

                        ParseDataNode(curNode);

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
                        Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                        continue;
                    }
                }
                bracketNode.parseIndex = index;

                ParseDataNode(bracketNode);
            }
            else
            {
                Node curParentNode = braceNode != null ? braceNode : pnode;
                int index = curParentNode.parseIndex;
                for (index = curParentNode.parseIndex; index < curParentNode.childList.Count;)
                {
                    var curNode = curParentNode.childList[index++];

                    if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                    {
                        if (index < curParentNode.childList.Count)
                        {
                            Node nextNode = curParentNode.childList[index];
                            if (nextNode.nodeType == ENodeType.LineEnd) //
                            {
                                nextNode = curParentNode.childList[++index];
                            }
                            if (nextNode.nodeType == ENodeType.Brace)  //Class1{}
                            {
                                index++;
                                curNode.blockNode = nextNode;
                                FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, null, FileMetaMemberData.EMemberDataType.NameClass);

                                AddParseDataInfo(fmmd);

                                ParseDataNode(curNode);

                                m_CurrentNodeInfoStack.Pop();

                            }
                            else if (nextNode?.nodeType == ENodeType.Assign) //Class1 = 
                            {
                                index++;
                                if (index < curParentNode.childList.Count)
                                {
                                    var next2Node = curParentNode.childList[index];
                                    if (next2Node.nodeType == ENodeType.LineEnd) //
                                    {
                                        next2Node = curParentNode.childList[++index];
                                    }
                                    if (next2Node.nodeType == ENodeType.Bracket)
                                    {
                                        index++;

                                        curNode.bracketNode = next2Node;

                                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, null, FileMetaMemberData.EMemberDataType.Array);

                                        AddParseDataInfo(fmmd);

                                        ParseDataNode(curNode);

                                        m_CurrentNodeInfoStack.Pop();
                                    }
                                    else if (next2Node.nodeType == ENodeType.Symbol)
                                    {
                                        var next3Node = curParentNode.childList[++index];

                                        if (next2Node.token?.type == ETokenType.Minus)
                                        {
                                            index++;
                                            int val = -(int)(next3Node.token?.lexeme);
                                            next3Node.token.SetLexeme(val);
                                        }

                                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, next3Node, FileMetaMemberData.EMemberDataType.KeyValue);

                                        if (currentNodeInfo.parseType == EParseNodeType.Class)
                                        {
                                            currentNodeInfo.codeClass.AddFileMemberData(fmmd);
                                        }
                                        else if (currentNodeInfo.parseType == EParseNodeType.Data)
                                        {
                                            currentNodeInfo.codeData.AddFileMemberData(fmmd);
                                        }
                                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                                        {
                                            var next4Node = curParentNode.childList[++index];
                                            if (next4Node.nodeType != ENodeType.SemiColon)
                                            {
                                                Console.WriteLine("Error 应该使用;结束语句!!");
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }
                                    }
                                    else if (next2Node.nodeType == ENodeType.ConstValue)
                                    {
                                        index++;
                                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, next2Node, FileMetaMemberData.EMemberDataType.KeyValue);

                                        if (currentNodeInfo.parseType == EParseNodeType.Class)
                                        {
                                            currentNodeInfo.codeClass.AddFileMemberData(fmmd);
                                        }
                                        else if (currentNodeInfo.parseType == EParseNodeType.Data)
                                        {
                                            currentNodeInfo.codeData.AddFileMemberData(fmmd);
                                        }
                                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                                        {
                                            var next3Node = curParentNode.childList[++index];
                                            if (next3Node.nodeType != ENodeType.SemiColon)
                                            {
                                                Console.WriteLine("Error 应该使用;结束语句!!");
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }
                                    }
                                    else if (next2Node.nodeType == ENodeType.IdentifierLink)
                                    {
                                        index++;
                                        string name = next2Node.token?.lexeme.ToString();
                                        //这块现在暂时还不确定，是否在data里边可以直接生成class或者是引用 data数据
                                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, next2Node, FileMetaMemberData.EMemberDataType.Data);

                                        if (currentNodeInfo.parseType == EParseNodeType.Class)
                                        {
                                            currentNodeInfo.codeClass.AddFileMemberData(fmmd);
                                        }
                                        else if (currentNodeInfo.parseType == EParseNodeType.Data)
                                        {
                                            currentNodeInfo.codeData.AddFileMemberData(fmmd);
                                        }
                                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                                        {
                                            var next3Node = curParentNode.childList[++index];
                                            if (next3Node.nodeType != ENodeType.SemiColon)
                                            {
                                                Console.WriteLine("Error 应该使用;结束语句!!");
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error 不允许=号后边非Const值!!");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Error 不允许=号后边没值!!");
                                }

                            }
                        }
                    }
                    else if (curNode.nodeType == ENodeType.Comma)
                    {
                        continue;
                    }
                    else if (curNode.nodeType == ENodeType.SemiColon)
                    {
                        continue;
                    }
                    else if (curNode.nodeType == ENodeType.LineEnd)
                    {
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Error Data数据中，不允许使用除自定义以后的字段!!" + curNode?.token?.ToLexemeAllString());
                    }
                }
                curParentNode.parseIndex = index;
            }
        }
        public void ParseEnumNode(Node pnode)
        {
            if (pnode.parseIndex >= pnode.childList.Count)
                return;
            Node blockNode = null;

            int type = -1;       // 0 enum 1 variable
            List<Node> nodeList = new List<Node>();

            bool isLineEnd = false;
            int index = pnode.parseIndex;
            for (index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                var curNode = pnode.childList[index++];
                Node nextNode = null;
                int lineCount = 0;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }

                if (curNode.nodeType == ENodeType.IdentifierLink)  //Enum1
                {
                    if (nextNode.nodeType == ENodeType.LineEnd)
                    {
                        nextNode = pnode.childList[index + 1];
                        lineCount++;
                    }
                    if (nextNode?.nodeType == ENodeType.Brace)  //Enum1{}
                    {
                        index+= (lineCount+1);
                        type = 0;
                        curNode.blockNode = nextNode;
                        blockNode = curNode;
                        nodeList.Add(curNode);
                        break;
                    }

                    if (nextNode?.nodeType == ENodeType.Par)  //Enum1()
                    {
                        Node next2Node = null;
                        bool isLineEnd2 = false;
                        if( index + 1 < pnode.childList.Count )
                        {
                            next2Node = pnode.childList[index + 1];
                            if (next2Node.nodeType == ENodeType.LineEnd)
                            {
                                next2Node = pnode.childList[index + 2];
                                lineCount++;
                                isLineEnd2 = true;
                            }
                        }
                        if (next2Node?.nodeType == ENodeType.Brace)  //Class1(){}的结构
                        {
                            bool isAssign = false;
                            for (int m = 0; m < nodeList.Count; m++)
                            {
                                if (nodeList[m].nodeType == ENodeType.Assign)
                                {
                                    isAssign = true;
                                    break;
                                }
                            }
                            if (curNode.nodeType == ENodeType.Assign) isAssign = true;

                            if (isAssign)
                            {
                                index+= (lineCount+2);
                                type = 1;
                                curNode.parNode = nextNode;
                                curNode.blockNode = next2Node;
                                blockNode = curNode;
                                nodeList.Add(curNode);
                                break;
                            }
                            else
                            {
                                Console.WriteLine("Error 不允许在enum中有函数的存在!!");
                                break;
                            }
                        }
                        else if (next2Node?.nodeType == ENodeType.SemiColon || isLineEnd2 )
                        {
                            index+=(lineCount+2); 
                            type = 1;
                            curNode.finalNode.parNode = nextNode;
                            nodeList.Add(curNode);
                            break;
                        }
                    }
                    else if (nextNode?.nodeType == ENodeType.Angle)    // Class1<>
                    {
                        var next2Node = pnode.childList[index + 1];
                        if (next2Node?.nodeType == ENodeType.Brace)  // Class1<>{}
                        {
                            index += 2;
                            type = 0;
                            curNode.angleNode = nextNode;
                            curNode.blockNode = next2Node;
                            blockNode = curNode;
                            nodeList.Add(curNode);
                            break;
                        }                       
                        else
                        {
                            index++;
                            curNode.angleNode = nextNode;
                        }
                    }
                }
                if (curNode?.nodeType == ENodeType.Par)  //类中的带()的结构
                {
                    if (nextNode?.nodeType == ENodeType.SemiColon)
                    {
                        index+= (lineCount+1);
                        type = 1;
                        curNode.finalNode.parNode = nextNode;
                        nodeList.Add(curNode);
                        break;
                    }
                    else if (nextNode?.nodeType == ENodeType.LineEnd)
                    {
                        index++;
                        type = 1;
                        curNode.finalNode.parNode = nextNode;
                        nodeList.Add(curNode);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Error 不允许在Class1()后 不能增加其它内容!!");
                        break;
                    }
                }
                else if (curNode?.nodeType == ENodeType.Brace)     //匿名对象
                {
                    bool isAssign = false;
                    for (int m = 0; m < nodeList.Count; m++)
                    {
                        if (nodeList[m].nodeType == ENodeType.Assign)
                        {
                            isAssign = true;
                            break;
                        }
                    }
                    if (curNode?.nodeType == ENodeType.Assign) isAssign = true;
                    if (isAssign)
                    {
                        type = 1;
                        blockNode = curNode;
                        nodeList.Add(curNode);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Error 在语句中直接使用{}不符合语法要求!!!");
                    }
                }
                else if (curNode.nodeType == ENodeType.SemiColon)
                {
                    if (nodeList.Count == 0)
                    {
                        isLineEnd = true;
                        break;
                    }
                    else
                    {
                        type = 1;
                        break;
                    }
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    if(nodeList.Count == 0 )
                    {
                        isLineEnd = true;
                        break;
                    }
                    else
                    {
                        if( ProjectManager.isUseForceSemiColonInLineEnd )
                        {
                            continue;
                        }
                        else
                        {
                            type = 1;
                            break;
                        }
                    }
                }

                nodeList.Add(curNode);
            }
            pnode.parseIndex = index;
            if (isLineEnd)
            {
                ParseEnumNode(pnode);
                return;
            }
            if (nodeList.Count == 0 )
            {
                var curnode = pnode.parseCurrent;
                Console.WriteLine("Error Enum解析token错误 没找发现可解析的NodeList 位置在" + curnode.token?.ToLexemeAllString());
                return;
            }

            if (type == -1)
            {
                var curnode = pnode.parseCurrent;
                if (blockNode != null)
                {
                    type = 0;
                }
                else
                {
                    Console.WriteLine("Error 解析token错误 没找发现可解析的NodeList 位置在" + curnode.token?.ToLexemeAllString());
                    return;
                }
            }
            if (type == 1)
            {
                FileMetaMemberVariable cpv = new FileMetaMemberVariable(m_FileMeta, nodeList);

                AddParseVariableInfo(cpv);

                ParseEnumNode(pnode);
            }
            else
            {
                FileMetaClass cpc = new FileMetaClass(m_FileMeta, nodeList);

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
                    Console.WriteLine("Error 在Enum不能包含类或者是数据，只允许包含enum!!");
                    return;
                }

                m_CurrentNodeInfoStack.Pop();

                ParseEnumNode(pnode);
            }
            ParseCommon(pnode);
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
                        || nextNode.nodeType == ENodeType.LineEnd )
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
        public void ParseImport( Node pnode )
        {
            List<Node> nodeList = GetAllNodeToSemiColon(pnode);
            FileMetaImportSyntax ist = new FileMetaImportSyntax(nodeList);
            m_FileMeta.AddFileImportSyntax(ist);
            ParseCommon(pnode);
        }
        public void ParseNamespace( Node pnode )
        {
            Node currentNode = pnode.GetParseNode();

            List<Node> conNode = new List<Node>();
            bool isBlock = false;
            Node namespaceNode = null;
            while (pnode.parseIndex < pnode.childList.Count)
            {
                Node nextNode = pnode.GetParseNode();
                if (nextNode == null)
                {
                    break;
                }
                
                if( nextNode.nodeType == ENodeType.Brace )
                {
                    currentNode.blockNode = nextNode;
                    isBlock = true;
                    break;
                }
                else if( nextNode.nodeType == ENodeType.IdentifierLink )
                {
                    if( namespaceNode != null )
                    {
                        Console.WriteLine("Error 在解析namespace 中，后边跟着参数多于正常语法!!");
                    }
                    namespaceNode = nextNode;
                    conNode.Add(namespaceNode);
                }
                else if(nextNode.nodeType == ENodeType.LineEnd )
                {
                    continue;
                }
                else
                {
                    conNode.Add(nextNode);
                }
            }

            if (isBlock)
            {
                if(namespaceNode == null )
                {
                    Console.WriteLine("Error 在解析namespace 中，没有找到namespace设置的名称!!");
                }
                FileMetaNamespace fmn = new FileMetaNamespace(currentNode, namespaceNode);

                AddParseNamespaceNodeInfo(fmn);

                ParseCommon(currentNode.blockNode);

                m_CurrentNodeInfoStack.Pop();

                ParseCommon(pnode);
            }
            else   
            {
                conNode.Insert(0, currentNode);
                FileDefineNamespace ist = new FileDefineNamespace(conNode);
                if (ProjectManager.isUseDefineNamespace)
                {
                    m_FileMeta.AddFileDefineNamespace(ist);
                }
                else
                {
                    Console.WriteLine("Error 暂不允许使用namespace 定义命名空间!!!" + ist.ToFormatString() + " 位置: " + currentNode.token.ToLexemeAllString());

                }
                ParseCommon(pnode);
            }

        }
        /*
        public void ParseParContrent(Node pnode)
        {
            // [] 解析中括号里边的内容
            Node bracketNode = pnode.bracketNode;
            int index1 = bracketNode.parseIndex;
            for (index1 = bracketNode.parseIndex; index1 < bracketNode.childList.Count;)
            {
                var curNode = bracketNode.childList[index1++];
                if (curNode.nodeType == ENodeType.Brace)  //Class1 [{},{}]
                {
                    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.NoNameClass);

                    AddParseVariableInfo(fmmd);

                    ParseCommon(curNode);

                    m_CurrentNodeInfoStack.Pop();
                }
                else if (curNode.nodeType == ENodeType.ConstValue)   // ["stringValue","Stvlue"]
                {
                    //FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.Value);

                    //currentNodeInfo.codeData.AddFileMemberData(fmmd);

                    if (ProjectManager.isUseForceSemiColonInLineEnd)
                    {
                        var next3Node = bracketNode.childList[index1 + 1];
                        if (next3Node?.nodeType != ENodeType.SemiColon)
                        {
                            Console.WriteLine("Error 应该使用;结束语句!!");
                        }
                    }
                }
                else if (curNode?.nodeType == ENodeType.Bracket) // [[],[]]
                {
                    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.Array);

                    //AddParseDataInfo(fmmd);
                    AddParseVariableInfo(fmmd);

                    //ParseDataNode(curNode);
                    ParseCommon(curNode);

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
                    Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                    continue;
                }
            }
            bracketNode.parseIndex = index1;

            //ParseDataNode(bracketNode);
            ParseCommon(bracketNode);
        }
        void AddFileMetaMemberVariable( Node pnode, List<Node> nodeList )
        {
            FileMetaMemberVariable cpv = new FileMetaMemberVariable(m_FileMeta, nodeList);

            AddParseVariableInfo(cpv);

            ParseCommon(pnode);
        }
        */
        void AddFileMetaFunctionVariable(Node pnode, Node blockNode, List<Node> nodeList )
        {
            FileMetaMemberFunction cpf = new FileMetaMemberFunction(m_FileMeta, nodeList);

            AddParseFunctionNodeInfo(cpf);

            ParseSyntax(blockNode);

            m_CurrentNodeInfoStack.Pop();
        }
        void AddFileMetaClasss(Node pnode, Node blockNode, List<Node> nodeList)
        {
            FileMetaClass cpc = new FileMetaClass(m_FileMeta, nodeList);

            AddParseClassNodeInfo(cpc);

            if (cpc.isEnum)
            {
                ParseEnumNode(blockNode);
            }
            else if( cpc.isData )
            {
                ParseDataNode(blockNode);
            }
            else
            {
                ParseClass(blockNode);
            }

            m_CurrentNodeInfoStack.Pop();
        }
        public void ParseClass( Node pnode )
        {
            Node braceNode = pnode.blockNode;

            List<Node> nodeList = new List<Node>();

            int index = pnode.parseIndex;
            for ( index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                int lineEndCount = 0;
                var curNode = pnode.childList[index++];
                if (curNode.nodeType == ENodeType.LineEnd)
                {
                    continue;
                }

                Node nextNode = null;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }
                if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    if (nextNode.nodeType == ENodeType.LineEnd)   // Class\n(remove){nextNode}
                    {
                        nextNode = pnode.childList[index + 1];
                        lineEndCount++;
                    }
                    else if( nextNode.nodeType == ENodeType.Key )
                    {
                        nodeList.Add(curNode);
                        Console.WriteLine("Error-------------------------");
                        continue;
                    }
                    else if( nextNode.nodeType == ENodeType.IdentifierLink )
                    {
                        nodeList.Add(curNode);
                        continue;
                    }

                    if (nextNode.nodeType == ENodeType.Brace)  //Class1{}
                    {
                        index += (lineEndCount + 1);
                        curNode.blockNode = nextNode;
                        nodeList.Add(curNode);

                        AddFileMetaClasss(pnode, curNode.blockNode, nodeList);
                        nodeList.Clear();
                    }
                    else if (nextNode?.nodeType == ENodeType.Par)   //Class1()
                    {
                        var next2Node = pnode.childList[index + 1];
                        if (next2Node.nodeType == ENodeType.LineEnd) //Class1()\n
                        {
                            next2Node = pnode.childList[index + 2];
                            lineEndCount++;
                        }
                        if (next2Node?.nodeType == ENodeType.Brace)  //  Func(){}
                        {
                            index += (lineEndCount + 2);
                            curNode.parNode = nextNode;
                            curNode.blockNode = next2Node;
                            nodeList.Add(curNode);
                            AddFileMetaFunctionVariable(pnode, curNode.blockNode, nodeList);
                            nodeList.Clear();
                        }
                    }
                    else if (nextNode?.nodeType == ENodeType.Angle)   //Class1<>
                    {
                        var next2Node = pnode.childList[index + 1];
                        if (next2Node.nodeType == ENodeType.LineEnd)
                        {
                            next2Node = pnode.childList[index + 2];
                            lineEndCount++;
                        }
                        if (next2Node?.nodeType == ENodeType.Brace)  // Class1<>{}
                        {
                            index += (lineEndCount + 2);
                            curNode.angleNode = nextNode;
                            curNode.blockNode = next2Node;
                            nodeList.Add(curNode);
                            AddFileMetaFunctionVariable(pnode, curNode.blockNode, nodeList);
                        }
                        else if (next2Node.nodeType == ENodeType.Par) // Func<>()?
                        {
                            var next3Node = pnode.childList[index + 2];
                            if (next3Node.nodeType == ENodeType.LineEnd)
                            {
                                next3Node = pnode.childList[index + 3];
                                lineEndCount++;
                            }
                            if (next3Node.nodeType == ENodeType.Brace)   // Func<>(){}
                            {
                                index += (lineEndCount + 3);
                                curNode.angleNode = nextNode;
                                curNode.parNode = next2Node;
                                curNode.blockNode = next3Node;
                                nodeList.Add(curNode);
                                AddFileMetaFunctionVariable(pnode, curNode.blockNode, nodeList);
                            }
                        }
                        else
                        {
                            index += (lineEndCount + 1);
                            curNode.angleNode = nextNode;
                            Console.WriteLine("新加入封号结尾，这块还没有进行完全测试!!");
                        }
                    }
                    else if (nextNode?.nodeType == ENodeType.Assign) //Class1 = "test1", [],{}, (2+3); Class2(10);
                    {
                        index++;
                        if (index < pnode.childList.Count)
                        {
                            var next2Node = pnode.childList[index];
                            if (next2Node.nodeType == ENodeType.LineEnd) //只允许有一次回车  name = \n
                            {
                                next2Node = pnode.childList[++index];
                            }
                            if (next2Node.nodeType == ENodeType.Symbol
                                || next2Node.nodeType == ENodeType.ConstValue
                                || next2Node.nodeType == ENodeType.Par
                                || next2Node.nodeType == ENodeType.Bracket
                                || next2Node.nodeType == ENodeType.IdentifierLink  )                                 
                            {
                                nodeList.Add(curNode);
                                nodeList.Add(nextNode);
                                int j = 0;
                                for (j = index; j < pnode.childList.Count; j++)
                                {
                                    if ((pnode.childList[j].nodeType == ENodeType.LineEnd
                                        || pnode.childList[j].nodeType == ENodeType.SemiColon))
                                    {
                                        j++;
                                        break;
                                    }
                                    nodeList.Add(pnode.childList[j]);
                                }
                                index = j;
                                FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, nodeList );
                                nodeList.Clear();

                                if (currentNodeInfo.parseType == EParseNodeType.Class)
                                {
                                    currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error 解析： “ + " + next2Node?.token?.ToLexemeAllString());
                            }
                        }
                    }
                }
                else if (curNode?.nodeType == ENodeType.Par)  //类中的带()的 一般在表达式中  xxx = a()
                {
                    if (nextNode.nodeType == ENodeType.LineEnd)
                    {
                        nextNode = pnode.childList[index + 1];
                        lineEndCount++;
                    }
                    if (nextNode?.nodeType == ENodeType.Brace)  //类中带(){}的结构
                    {
                        bool isAssign = false;
                        for (int m = 0; m < nodeList.Count; m++)
                        {
                            if (nodeList[m].nodeType == ENodeType.Assign)
                            {
                                isAssign = true;
                                break;
                            }
                        }
                        if (curNode.nodeType == ENodeType.Assign) isAssign = true;

                        if (!isAssign)
                        {
                            index += (lineEndCount + 1);
                            curNode.blockNode = nextNode;
                            nodeList.Add(curNode);
                            break;
                        }
                        else
                        {
                            if (index + 2 < pnode.childList.Count)
                            {
                                var n2Node = pnode.childList[index + 2];

                                bool isLineEnd = false;
                                if (ProjectManager.isUseForceSemiColonInLineEnd)
                                {
                                    if (n2Node.nodeType == ENodeType.SemiColon)
                                    {
                                        isLineEnd = true;
                                    }
                                }
                                else
                                {
                                    if (n2Node.nodeType == ENodeType.LineEnd
                                        || n2Node.nodeType == ENodeType.SemiColon)
                                    {
                                        isLineEnd = true;
                                    }
                                }

                                if (isLineEnd)
                                {
                                    index += 2;
                                    curNode.blockNode = nextNode;
                                    nodeList.Add(curNode);
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (curNode?.nodeType == ENodeType.Brace)
                {
                    bool isAssign = false;
                    for (int m = 0; m < nodeList.Count; m++)
                    {
                        if (nodeList[m].nodeType == ENodeType.Assign)
                        {
                            isAssign = true;
                            break;
                        }
                    }
                    if (curNode?.nodeType == ENodeType.Assign) isAssign = true;
                    if (!isAssign)
                    {
                        index++;
                        curNode.blockNode = nextNode;
                        nodeList.Add(curNode);
                        break;
                    }
                }
                else if( curNode.nodeType == ENodeType.Key )
                {
                    nodeList.Add(curNode);
                }
                else
                {
                    if (ProjectManager.isUseForceSemiColonInLineEnd)
                    {
                        if (curNode.nodeType == ENodeType.SemiColon)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (curNode.nodeType == ENodeType.SemiColon)
                        {
                            FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, nodeList);
                            nodeList.Clear();

                            if (currentNodeInfo.parseType == EParseNodeType.Class)
                            {
                                currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
                            }
                            break;
                        }
                    }
                    Console.WriteLine("Error 不允许--------------------");
                }
            }
            pnode.parseIndex = index;
        }       
        //处理 ident <> () {} [] . 的结合 与子元素的统一处理
        public static void HandleLinkNode(Node node, Node inputFinaleNode =null )
        {
            int index = node.parseIndex;
            if (index < 0 || index >= node.childList.Count)
                return;

            Node currentExpressNode = node.parseCurrent;
            if( inputFinaleNode == null )
            {
                node.parseIndex++;
                HandleLinkNode(node, currentExpressNode );
                return;
            }

            Node finalNode = inputFinaleNode.finalNode;
            if (finalNode?.nodeType == ENodeType.IdentifierLink) //Class1???
            {
                if (currentExpressNode.nodeType == ENodeType.Angle)       //Class<>??
                {
                    finalNode.angleNode = currentExpressNode;            //Class<>
                    node.childList.Remove(currentExpressNode);

                    HandleLinkNode(currentExpressNode);

                    if (index < node.childList.Count)
                    {
                        var next1ExpressNode = node.childList[index];
                        if (next1ExpressNode?.nodeType == ENodeType.Par)      //Class<>()
                        {
                            finalNode.parNode = next1ExpressNode;
                            node.childList.Remove(next1ExpressNode);
                            HandleLinkNode(next1ExpressNode);

                            if (next1ExpressNode.extendLinkNodeList.Count > 0)
                            {
                                finalNode.SetLinkNode(next1ExpressNode.extendLinkNodeList);
                                HandleLinkNode(node, finalNode);
                                return;
                            }
                            else
                            {
                                finalNode = finalNode.finalNode;
                                if (index  < node.childList.Count)
                                {
                                    var next2ExpressNode = node.childList[index];
                                    if (next2ExpressNode.nodeType == ENodeType.Brace)   //Class<>()?{}
                                    {
                                        finalNode.blockNode = next2ExpressNode;
                                        HandleLinkNode(node, finalNode);           //Q.Map<>(){}
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            HandleLinkNode(node, finalNode);
                            return;
                        }
                    }
                }
                else if (currentExpressNode.nodeType == ENodeType.Par)             //Class()?
                {
                    finalNode.parNode = currentExpressNode;
                    node.childList.Remove(currentExpressNode);
                    HandleLinkNode(currentExpressNode);

                    if (currentExpressNode.extendLinkNodeList.Count > 0)
                    {
                        finalNode.SetLinkNode(currentExpressNode.extendLinkNodeList);   // Q.Map()[.Cast]
                        HandleLinkNode(node, finalNode);           //Q.Map().[Cast]
                        return;
                    }
                    else
                    {
                        HandleLinkNode(node, finalNode);
                        return;
                    }
                }
                else if( currentExpressNode.nodeType == ENodeType.Brace )           // M.Class(){}
                {
                    finalNode.blockNode = currentExpressNode;
                    node.childList.Remove(currentExpressNode);
                    HandleLinkNode(currentExpressNode);
                    HandleLinkNode(node, finalNode);
                    return;
                }
                else if( currentExpressNode.nodeType == ENodeType.Bracket )         //Class[]
                {
                    finalNode.bracketNode = currentExpressNode;
                    node.childList.Remove(currentExpressNode);
                    HandleLinkNode(currentExpressNode);

                    if( currentExpressNode.extendLinkNodeList.Count > 0 )
                    {
                        finalNode.SetLinkNode(currentExpressNode.extendLinkNodeList);   // Array[1].20;
                        HandleLinkNode(node, finalNode);                        // Array[1].Fun( 1, 2 );
                        return;
                    }
                    else
                    {
                        HandleLinkNode(node, finalNode);
                        return;
                    }
                }
            }
            node.parseIndex++;
            HandleLinkNode( node, currentExpressNode);
        }
        public void ParseSyntax(Node pnode)
        {
            Node node = pnode.parseCurrent;
            if (node == null) return;

            if ( node.nodeType == ENodeType.Brace )
            {
                FileMetaBlockSyntax cps = new FileMetaBlockSyntax( m_FileMeta, node.token, node.endToken);

                AddParseSyntaxNodeInfo(cps, true);

                ParseCommon(node);

                m_CurrentNodeInfoStack.Pop();

                pnode.parseIndex++;
            }
            else
            {
                HandleCreateFileMetaSyntaxByPNode(pnode);
            }
            ParseSyntax(pnode);
        }
    }
}