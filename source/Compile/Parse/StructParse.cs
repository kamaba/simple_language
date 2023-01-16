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

namespace SimpleLanguage.Compile.Parse
{
    public class StructParse
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
            public ParseCurrentNodeInfo(FileMetaMemberFunction nsf)
            {
                codeFunction = nsf;
                parseType = EParseNodeType.Function;
            }
            public ParseCurrentNodeInfo( FileMetaMemberData fmmd )
            {
                codeData = fmmd;
                parseType = EParseNodeType.Data;
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
        public void AddParseDataInfo( FileMetaMemberData fmmd )
        {
            if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMemberData(fmmd);
            }
            else if( currentNodeInfo.parseType == EParseNodeType.Data )
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
            else
            {
                bool isFile = currentNodeInfo.parseType == EParseNodeType.File;
                bool isNamespace = currentNodeInfo.parseType == EParseNodeType.Namespace;
                bool isClass = currentNodeInfo.parseType == EParseNodeType.Class;
                bool isFunction = currentNodeInfo.parseType == EParseNodeType.Function;
                bool isStatements = currentNodeInfo.parseType == EParseNodeType.Statements;
                if (isFile || isNamespace || isClass  )
                {
                    ParseClassOrClassContent(pnode, isFile, isNamespace, isClass );
                }
                else if (isFunction || isStatements )
                {
                    ParseSyntax(pnode);
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
                    else
                    {
                        Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString() );
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
                                    if (next2Node.nodeType == ENodeType.Bracket)
                                    {
                                        index++;

                                        curNode.bracketNode = next2Node;

                                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, null, FileMetaMemberData.EMemberDataType.Array);

                                        AddParseDataInfo(fmmd);

                                        ParseDataNode(curNode);

                                        m_CurrentNodeInfoStack.Pop();
                                    }
                                    else if( next2Node.nodeType == ENodeType.ConstValue )
                                    {
                                        index++;

                                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, next2Node, FileMetaMemberData.EMemberDataType.KeyValue );

                                        if (currentNodeInfo.parseType == EParseNodeType.Class)
                                        {
                                            currentNodeInfo.codeClass.AddFileMemberData(fmmd);
                                        }
                                        else if (currentNodeInfo.parseType == EParseNodeType.Data)
                                        {
                                            currentNodeInfo.codeData.AddFileMemberData(fmmd);
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
                    else
                    {
                        Console.WriteLine("Error Data数据中，不允许使用除自定义以后的字段!!" + curNode?.token?.ToLexemeAllString() );
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

            int index = pnode.parseIndex;
            for (index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                var curNode = pnode.childList[index++];
                Node nextNode = null;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }

                if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    if (nextNode?.nodeType == ENodeType.Brace)  //Class1{}
                    {
                        index++;
                        type = 0;
                        curNode.blockNode = nextNode;
                        blockNode = curNode;
                        nodeList.Add(curNode);
                        break;
                    }                   
                    else if (nextNode?.nodeType == ENodeType.Angle)
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
                    if (nextNode?.nodeType == ENodeType.Brace)  //类中带(){}的结构
                    {
                        Console.WriteLine("Error Enum的值赋值过程，要使用()的方式来进行!!");
                        /*
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
                            index++;
                            type = 2;
                            curNode.blockNode = nextNode;
                            blockNode = curNode;
                            nodeList.Add(curNode);
                            break;
                        }
                        else
                        {
                            if (index + 2 < pnode.childList.Count)
                            {
                                var n2Node = pnode.childList[index + 2];
                                if (n2Node.nodeType == ENodeType.SemiColon)
                                {
                                    index += 2;
                                    type = 1;
                                    curNode.blockNode = nextNode;
                                    blockNode = curNode;
                                    nodeList.Add(curNode);
                                    break;
                                }
                            }
                        }
                        */
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
                        type = 0;
                        index++;
                        curNode.blockNode = nextNode;
                        blockNode = nextNode;
                        nodeList.Add(curNode);
                        break;
                    }
                }
                else if (curNode.nodeType == ENodeType.SemiColon)
                {
                    type = 1;
                    break;
                }               

                nodeList.Add(curNode);
            }
            if (nodeList.Count == 0)
            {
                var curnode = pnode.parseCurrent;
                Console.WriteLine("Error Enum解析token错误 没找发现可解析的NodeList 位置在" + curnode.token?.ToLexemeAllString());
                return;
            }
            pnode.parseIndex = index;

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
            while (pnode.parseIndex < pnode.childList.Count)
            {
                Node nextNode = pnode.GetParseNode();
                if (nextNode == null)
                {
                    break;
                }
                if (nextNode.nodeType == ENodeType.SemiColon)
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
        public List<Node> HandleCreateFileMetaSyntaxByPNode( Node pnode )
        {
            List<Node> conNode = new List<Node>();            
                        
            Node forceAddNode = null;
            bool isMeetIdentiferThenExit = false;
            List<Node> tempNodeList = new List<Node>();

            while (pnode.parseIndex < pnode.childList.Count)
            {
                Node nextNode = pnode.GetParseNode();
                if (nextNode == null)
                {
                    break;
                }
                if (forceAddNode != null)
                {
                    if (nextNode.nodeType == ENodeType.Angle)
                    {
                        for (int i = 0; i < nextNode.childList.Count; i++)
                        {
                            conNode.Add(nextNode.childList[i]);
                        }
                        continue;
                    }
                    else if(nextNode.nodeType == ENodeType.Brace)
                    {
                        if( forceAddNode.token?.type == ETokenType.Switch
                            || forceAddNode.token?.type == ETokenType.For
                            || forceAddNode.token?.type == ETokenType.While
                            || forceAddNode.token?.type == ETokenType.DoWhile )
                        {
                            forceAddNode.SetParList(tempNodeList);
                            forceAddNode.blockNode = nextNode;
                            forceAddNode = null;
                            break;
                        }
                        else if( forceAddNode.token?.type == ETokenType.If 
                            || forceAddNode.token?.type == ETokenType.ElseIf )
                        {
                            forceAddNode.SetParList(tempNodeList);
                            forceAddNode.blockNode = nextNode;
                            forceAddNode = null;
                            isMeetIdentiferThenExit = true;
                            continue;
                        }
                        else if( forceAddNode.token?.type == ETokenType.Else )
                        {
                            forceAddNode.SetParList(tempNodeList);
                            forceAddNode.blockNode = nextNode;
                            break;
                        }
                        else if( forceAddNode.token?.type == ETokenType.Case )
                        {
                            forceAddNode.SetParList(tempNodeList);
                            for( int i = 0; i < conNode.Count; i++ )
                            {
                                conNode[i].blockNode = nextNode;
                            }
                            break;
                        }
                        else if( forceAddNode?.token?.type == ETokenType.Default )
                        {
                            forceAddNode.SetParList(tempNodeList);
                            forceAddNode.blockNode = nextNode;
                            break;
                        }
                    }
                    else
                    {
                        if (forceAddNode.token?.type == ETokenType.Case
                            && nextNode.token?.type == ETokenType.Case )
                        {
                            forceAddNode.SetParList(tempNodeList);
                            forceAddNode = nextNode;
                            conNode.Add(nextNode);
                            tempNodeList = new List<Node>();
                        }
                        else
                        {
                            tempNodeList.Add(nextNode);
                        }
                        continue;
                    }
                }
                
                if (nextNode.nodeType == ENodeType.SemiColon)
                {
                    break;
                }
                if( nextNode.nodeType == ENodeType.Brace )
                {
                    conNode.Add(nextNode);
                }
                else if (nextNode.nodeType == ENodeType.Key)
                {
                    if (isMeetIdentiferThenExit)        //如果是key{}后，则检查是否是elseif/else等联动语法
                    {
                        if (nextNode.token?.type == ETokenType.ElseIf)
                        {
                            tempNodeList = new List<Node>();
                            forceAddNode = nextNode;
                            isMeetIdentiferThenExit = false;
                            conNode.Add(nextNode);
                            continue;
                        }
                        else if (nextNode.token?.type == ETokenType.Else)
                        {
                            tempNodeList = new List<Node>();
                            forceAddNode = nextNode;
                            conNode.Add(nextNode);
                            continue;
                        }
                        else
                        {
                            pnode.parseIndex--;
                            break;
                        }
                    }
                    if(forceAddNode != null )
                    {
                        Console.WriteLine("Error 已解析过一个key,在这个相中不允许其它key的出现!!");
                    }
                    if (nextNode.token?.type == ETokenType.If)
                    {
                        forceAddNode = nextNode;
                    }
                    else if (nextNode.token?.type == ETokenType.Switch)
                    {
                        forceAddNode = nextNode;
                    }
                    else if (nextNode.token?.type == ETokenType.For)
                    {
                        forceAddNode = nextNode;
                    }
                    else if (nextNode.token?.type == ETokenType.While || nextNode.token?.type == ETokenType.DoWhile)
                    {
                        forceAddNode = nextNode;
                    }
                    else if (nextNode.token?.type == ETokenType.Case)
                    {
                        forceAddNode = nextNode;
                    }
                    else if (nextNode.token?.type == ETokenType.Default)
                    {
                        forceAddNode = nextNode;
                    }

                    if (forceAddNode != null)
                    {
                        conNode.Add(nextNode);
                    }
                    if (nextNode.token?.type == ETokenType.Return 
                        || nextNode.token?.type == ETokenType.Transience
                        || nextNode.token?.type == ETokenType.Next
                        || nextNode.token?.type == ETokenType.Break
                        || nextNode.token?.type == ETokenType.Continue
                        || nextNode.token?.type == ETokenType.Label 
                        || nextNode.token?.type == ETokenType.Goto )
                    {
                        conNode.Add(nextNode);
                    }
                }
                else
                {
                    if (isMeetIdentiferThenExit)
                    {
                        pnode.parseIndex--;
                        break;
                    }
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
                if (nextNode.nodeType == ENodeType.SemiColon)
                {
                    break;
                }
                else if( nextNode.nodeType == ENodeType.Brace )
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
                    Console.WriteLine("Error 暂不允许使用namespace 定义命名空间!!!" + ist.ToFormatString() + " 位置: " + currentNode.token.ToLexemeAllString()  );

                }
                ParseCommon(pnode);
            }

        }
        public void ParseClassOrClassContent(Node pnode, bool isFile, bool isNamespace, bool isClass )
        {
            Node blockNode = null;

            int type = -1;       // 0 class 1 variable 2 function content
            List<Node> nodeList = new List<Node>();

            int index = pnode.parseIndex;
            for ( index = pnode.parseIndex; index < pnode.childList.Count;  )
            {
                var curNode = pnode.childList[index++];
                Node nextNode = null;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }
                
                if (curNode.nodeType == ENodeType.IdentifierLink  )  //Class1
                {
                    if( nextNode?.nodeType == ENodeType.Brace )  //Class1{}
                    {
                        index++;
                        type = 0;
                        curNode.blockNode = nextNode;
                        blockNode = curNode;
                        nodeList.Add(curNode);
                        break;
                    }
                    else if( nextNode?.nodeType == ENodeType.Par )   //Class1()
                    {
                        if( isClass )
                        {
                            var next2Node = pnode.childList[index + 1];
                            if (next2Node?.nodeType == ENodeType.Brace)  //  Func(){}
                            {
                                index += 2;
                                type = 2;
                                curNode.parNode = nextNode;
                                curNode.blockNode = next2Node;
                                blockNode = curNode;
                                nodeList.Add(curNode);
                                break;
                            }
                        }
                    }
                    else if( nextNode?.nodeType == ENodeType.Angle )
                    {
                        var next2Node = pnode.childList[index + 1];
                        if( next2Node?.nodeType == ENodeType.Brace )  // Class1<>{}
                        {
                            index += 2;
                            type = 0;
                            curNode.angleNode = nextNode;
                            curNode.blockNode = next2Node;
                            blockNode = curNode;
                            nodeList.Add(curNode);
                            break;
                        }
                        else if( next2Node.nodeType == ENodeType.Par ) // Func<>()?
                        {
                            var next3Node = pnode.childList[index + 2];
                            if (next3Node.nodeType == ENodeType.Brace )   // Func<>(){}
                            {
                                index += 3;
                                type = 2;
                                curNode.angleNode = nextNode;
                                curNode.parNode = next2Node;
                                curNode.blockNode = next3Node;
                                blockNode = curNode;
                                nodeList.Add(curNode);
                                break;
                            }
                        }
                        else
                        {
                            index++;
                            curNode.angleNode = nextNode;
                        }
                    }
                }
                
                if( isClass )//isClass
                {
                    if (curNode?.nodeType == ENodeType.Par )  //类中的带()的结构
                    {
                        if ( nextNode?.nodeType == ENodeType.Brace )  //类中带(){}的结构
                        {
                            bool isAssign = false;
                            for( int m = 0; m < nodeList.Count; m++ )
                            {
                                if( nodeList[m].nodeType == ENodeType.Assign )
                                {
                                    isAssign = true;
                                    break;
                                }
                            }
                            if (curNode.nodeType == ENodeType.Assign) isAssign = true;

                            if(!isAssign)
                            {
                                index++;
                                type = 2;
                                curNode.blockNode = nextNode;
                                blockNode = curNode;
                                nodeList.Add(curNode);
                                break;
                            }
                            else
                            {
                                if( index + 2 < pnode.childList.Count )
                                {
                                    var n2Node = pnode.childList[index + 2];
                                    if( n2Node.nodeType == ENodeType.SemiColon )
                                    {
                                        index+=2;
                                        type = 1;
                                        curNode.blockNode = nextNode;
                                        blockNode = curNode;
                                        nodeList.Add(curNode);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if ( curNode?.nodeType == ENodeType.Brace )
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
                        if( !isAssign )
                        {
                            type = 0;
                            index++;
                            curNode.blockNode = nextNode;
                            blockNode = nextNode;
                            nodeList.Add(curNode);
                            break;
                        }                        
                    }
                    else if( curNode.nodeType == ENodeType.SemiColon )
                    {
                        type = 1;
                        break;
                    }
                }

                nodeList.Add(curNode);
            }
            if( nodeList.Count == 0 )
            {
                var curnode = pnode.parseCurrent;
                Console.WriteLine("Error 解析token错误 没找发现可解析的NodeList 位置在" + curnode.token?.ToLexemeAllString() );
                return;
            }
            if( type == -1 )
            {
                var curnode = pnode.parseCurrent;
                if (blockNode != null )
                {
                    type = 0;
                }
                else
                {
                    Console.WriteLine("Error 解析token错误 没找发现可解析的NodeList 位置在" + curnode.token?.ToLexemeAllString());
                    return;
                }
            }
            pnode.parseIndex = index;

            if (type == 1 )
            {
                FileMetaMemberVariable cpv = new FileMetaMemberVariable( m_FileMeta, nodeList);

                AddParseVariableInfo(cpv);

                ParseCommon(pnode);
            }
            else if( type == 2 )
            {
                FileMetaMemberFunction cpf = new FileMetaMemberFunction(m_FileMeta, nodeList);

                AddParseFunctionNodeInfo(cpf);

                ParseSyntax(blockNode.blockNode);

                m_CurrentNodeInfoStack.Pop();

                ParseCommon(pnode);                
            }
            else
            {
                FileMetaClass cpc = new FileMetaClass(m_FileMeta, nodeList);
                
                AddParseClassNodeInfo(cpc);
                
                if( cpc.isEnum )
                {
                    ParseEnumNode(blockNode.blockNode);
                }
                else if( cpc.isData )
                {
                    ParseDataNode(blockNode);
                }
                else
                {
                    ParseCommon(blockNode.blockNode);
                }

                m_CurrentNodeInfoStack.Pop();

                ParseCommon(pnode);
            }
        }
       
        public FileMetaSyntax CreateFileMetaKeySyntaxByNodesList( List<Node> nodesList )
        {
            if (nodesList.Count < 0)
            {
                Console.WriteLine("Error CreateFileMetaKeySyntaxByNodesList 解析语句不能为空!!");
                return null;
            }

            if (nodesList[0].token?.type == ETokenType.If)
            {
                FileMetaKeyIfSyntax ifSyntax = null;
                for (int i = 0; i < nodesList.Count; i++)
                {
                    var cnode = nodesList[i];
                    Token token = cnode.token;
                    var nlist = cnode.parNode.childList;
                    if (token.type == ETokenType.If
                        || token.type == ETokenType.ElseIf)
                    {
                        FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, nlist, FileMetaTermExpress.EExpressType.Common);
                        FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, cnode.blockNode.token, cnode.blockNode.endToken);
                        var fms = new FileMetaConditionExpressSyntax(m_FileMeta, token, conditionExpress, executeBlock);

                        if (ifSyntax == null)
                        {
                            ifSyntax = new FileMetaKeyIfSyntax(m_FileMeta);
                        }

                        ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(executeBlock);
                        m_CurrentNodeInfoStack.Push(pcni);
                        ParseSyntax(cnode.blockNode);
                        m_CurrentNodeInfoStack.Pop();

                        if (token.type == ETokenType.If)
                        {
                            if (ifSyntax.ifExpressSyntax != null)
                            {
                                Console.WriteLine("Error 不能有多个if语句!!");
                            }
                            ifSyntax.SetFileMetaConditionExpressSyntax( fms );
                        }
                        else
                        {
                            ifSyntax.AddElseIfExpressSyntax(fms);
                        }
                    }
                    else if (token.type == ETokenType.Else)
                    {
                        FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, cnode.blockNode.token, cnode.blockNode.endToken);
                        var fms = new FileMetaKeyOnlySyntax(m_FileMeta, token, executeBlock);

                        if (ifSyntax == null)
                        {
                            Console.WriteLine("Error 前边没有if语句!!");
                        }
                        ifSyntax.SetElseExpressSyntax( fms );

                        ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(executeBlock);
                        m_CurrentNodeInfoStack.Push(pcni);
                        ParseSyntax(cnode.blockNode);
                        m_CurrentNodeInfoStack.Pop();
                    }
                }
                return ifSyntax;
            }
            else if (nodesList[0].token?.type == ETokenType.Switch)
            {
                if (nodesList.Count > 1)
                {
                    Console.WriteLine("Error 解析switch 后边主允许有其它内容!!");
                }
                var cnode = nodesList[0];
                FileMetaCallLink fmcl = null;
                if (cnode.parNode != null && cnode.parNode.childList?.Count > 0)
                {
                    fmcl = new FileMetaCallLink(m_FileMeta, cnode.parNode.childList[0]);
                }
                var fms = new FileMetaKeySwitchSyntax(m_FileMeta, cnode.token, cnode.blockNode.token, cnode.blockNode.endToken, fmcl);

                ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fms);
                m_CurrentNodeInfoStack.Push(pcni);

                while (cnode.blockNode != null && cnode.blockNode.parseIndex < cnode.blockNode.childList.Count)
                {
                    var castlist = HandleCreateFileMetaSyntaxByPNode(cnode.blockNode);

                    for (int j = 0; j < castlist.Count; j++)
                    {
                        var castnode = castlist[j];

                        if (castnode.token?.type == ETokenType.Case)
                        {
                            var fmkcs = new FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax(m_FileMeta, castnode.token,null,null);

                            var parlist = castnode.parNode.childList;
                            if (parlist.Count == 0)
                            {
                                Console.WriteLine("Error Case语句不允许没有检查值!!");
                            }
                            List<Node> childList = new List<Node>();
                            bool isComma = false;
                            for (int i = 0; i < parlist.Count; i++)
                            {
                                if (parlist[i].token?.type == ETokenType.Comma)
                                {
                                    isComma = true;
                                    continue;
                                }
                                childList.Add(parlist[i]);
                            }
                            if (isComma)
                            {
                                bool isSame = true;//是否通过,号切后的类型是相同的
                                for (int i = 0; i < childList.Count - 1; i++)
                                {
                                    var curNode = childList[i];
                                    var nextNode = childList[i + 1];
                                    var type = curNode.token.type;
                                    if (type != ETokenType.Number && type != ETokenType.String)
                                    {
                                        Console.WriteLine("Error 逗号分割只允许number,string");
                                        break;
                                    }
                                    if (type != nextNode.token.type)
                                    {
                                        isSame = false;
                                        break;
                                    }
                                }
                                if (!isSame)
                                {
                                    Console.WriteLine("Error 使用逗号切割开后，类型不相同!!");
                                }
                                for (int i = 0; i < childList.Count; i++)
                                {
                                    fmkcs.AddConstValueTokenList(new FileMetaConstValueTerm(m_FileMeta, childList[i].token));
                                }
                            }
                            else
                            {
                                if (parlist.Count == 2)
                                {
                                    if (parlist[0].token?.type == ETokenType.Identifier
                                        || parlist[1].token?.type == ETokenType.Identifier)
                                    {
                                        fmkcs.SetDefineClassToken( parlist[0].token );
                                        fmkcs.SetVariableToken( parlist[1].token );
                                    }
                                }
                                else if (parlist.Count == 1)
                                {
                                    var ttype = parlist[0].token?.type;
                                    if (ttype == ETokenType.Type
                                        || ttype == ETokenType.Identifier)
                                    {
                                        fmkcs.SetDefineClassToken( parlist[0].token );
                                    }
                                    else if (ttype == ETokenType.Number
                                        || ttype == ETokenType.String)
                                    {
                                        fmkcs.AddConstValueTokenList(new FileMetaConstValueTerm(m_FileMeta, parlist[0].token));
                                    }
                                }
                            }
                            FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, castnode.blockNode.token, castnode.blockNode.endToken);
                            fmkcs.SetExecuteBlockSyntax( executeBlock );
                            ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(executeBlock);
                            m_CurrentNodeInfoStack.Push(pcnic);
                            ParseSyntax(castnode.blockNode);
                            m_CurrentNodeInfoStack.Pop();

                            if (j != castlist.Count - 1)
                                fmkcs.isContinueNextCastSyntax = true;

                            fms.AddFileMetaKeyCaseSyntaxList(fmkcs);
                        }
                        else if (castnode.token?.type == ETokenType.Default)
                        {
                            if (fms.defaultExecuteBlockSyntax != null)
                            {
                                Console.WriteLine("Error 不允许有两个default处理!!");
                            }

                            var fmkcs = new FileMetaBlockSyntax(m_FileMeta, castnode.blockNode.token, castnode.blockNode.endToken);

                            ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkcs);
                            m_CurrentNodeInfoStack.Push(pcnic);
                            ParseSyntax(castnode.blockNode);
                            m_CurrentNodeInfoStack.Pop();

                            fms.SetDefaultExecuteBlockSyntax( fmkcs );
                        }
                        else
                        {
                            Console.WriteLine("Error 不允许在switch中有其它语句!!");
                        }
                    }
                }

                m_CurrentNodeInfoStack.Pop();

                return fms;
            }
            else if (nodesList[0].token?.type == ETokenType.For)
            {
                if (nodesList.Count > 1)
                {
                    Console.WriteLine("Error 解析for 后边主允许有其它内容!!");
                }
                var cnode = nodesList[0];

                FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, cnode.blockNode.token, cnode.blockNode.endToken);

                ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(executeBlock);
                m_CurrentNodeInfoStack.Push(pcnic);
                ParseSyntax(cnode.blockNode);
                m_CurrentNodeInfoStack.Pop();


                var fms = new FileMetaKeyForSyntax(m_FileMeta, cnode.token, executeBlock);

                var parlist = cnode.parNode.childList;
                if (parlist.Count == 0)
                {
                    Console.WriteLine("Error For语句中，条件区域没有相关的值!!");
                }
                List<Node> defineVariableSyntaxNodeList = new List<Node>();
                List<Node> conditionExpressNodeList = new List<Node>();
                List<Node> stepExecuteSyntaxNodeList = new List<Node>();

                int syntax = 0;         // 0 defineVariable include( a in array) 1 conditionExpress use one comma a, b 2 stepExecuteSyntax a,b,c
                Token inToken = null;
                for (int i = 0; i < parlist.Count; i++)
                {
                    if (parlist[i].token?.type == ETokenType.Comma)
                    {
                        if (syntax == 0)
                        {
                            syntax = 1;
                        }
                        else if (syntax == 1)
                        {
                            syntax = 2;
                        }
                        continue;
                    }
                    else if( parlist[i].token?.type == ETokenType.In )
                    {
                        inToken = parlist[i].token;
                        continue;
                    }
                    
                    if (syntax == 0)
                    {
                        if( inToken != null )
                        {
                            conditionExpressNodeList.Add(parlist[i]);
                        }
                        else
                        {
                            defineVariableSyntaxNodeList.Add(parlist[i]);
                        }
                    }
                    else if (syntax == 1)
                    {
                        conditionExpressNodeList.Add(parlist[i]);
                    }
                    else if (syntax == 2)
                    {
                        stepExecuteSyntaxNodeList.Add(parlist[i]);
                    }
                }

                FileMetaSyntax defineVariableSyntax = null;
                if (defineVariableSyntaxNodeList.Count > 0)
                {
                    defineVariableSyntax = CrateFileMetaSyntaxNoKey(defineVariableSyntaxNodeList);
                    fms.SetFileMetaClassDefine(defineVariableSyntax );                   
                }
                if (defineVariableSyntax == null)
                {
                    Console.WriteLine("Error 解析for 第一部分错误，解析语句出错，不是定义类型语句!!");
                }
                if (inToken != null)
                {
                    var cfe = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, conditionExpressNodeList, FileMetaTermExpress.EExpressType.Common);
                    if (cfe == null)
                    {
                        Console.WriteLine("Error 解析for 第二部分错误!!");
                    }
                    else
                    {
                        fms.SetInKeyAndArrayVariable(inToken, cfe);
                    }
                }
                else
                {
                    if (conditionExpressNodeList.Count > 0)
                    {
                        var cfe = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, conditionExpressNodeList, FileMetaTermExpress.EExpressType.Common);
                        if (cfe == null)
                        {
                            Console.WriteLine("Error 解析for 第二部分错误!!");
                        }
                        else
                        {
                            fms.SetConditionExpress(cfe);
                        }                     
                    }
                    if (stepExecuteSyntaxNodeList.Count > 0)
                    {
                        var fms2 = CrateFileMetaSyntaxNoKey(stepExecuteSyntaxNodeList);
                        if (fms2 is FileMetaOpAssignSyntax)
                        {
                            fms.SetStepFileMetaOpAssignSyntax(fms2 as FileMetaOpAssignSyntax);
                        }
                        else
                        {
                            Console.WriteLine("Error 解析for 第三部分错误!!");
                        }
                    }
                }
                return fms;
            }
            else if (nodesList[0].token?.type == ETokenType.DoWhile
                || nodesList[0].token?.type == ETokenType.While)
            {
                if (nodesList.Count > 1)
                {
                    Console.WriteLine("Error 解析switch 后边主允许有其它内容!!");
                }
                var cnode = nodesList[0];
                FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, cnode.parNode.childList, FileMetaTermExpress.EExpressType.Common);
                FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, cnode.blockNode.token, cnode.blockNode.endToken);
                var fms = new FileMetaConditionExpressSyntax(m_FileMeta, cnode.token, conditionExpress, executeBlock);

                ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(executeBlock);
                m_CurrentNodeInfoStack.Push(pcni);
                ParseSyntax(cnode.blockNode);
                m_CurrentNodeInfoStack.Pop();

                return fms;
            }
            else if (nodesList[0].token?.type == ETokenType.Return
                || nodesList[0].token?.type == ETokenType.Transience)
            {
                if (nodesList.Count == 1)
                {
                    Console.WriteLine("Error 解析return/tr 必须有表达示内容!!");
                }
                var cnode = nodesList[0];

                List<Node> list = new List<Node>();
                list.AddRange(nodesList.GetRange(1, nodesList.Count - 1));
                FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, list, FileMetaTermExpress.EExpressType.Common);

                var fms = new FileMetaKeyReturnSyntax(m_FileMeta, cnode.token, conditionExpress);

                return fms;
            }
            else if (nodesList[0].token?.type == ETokenType.Next)
            {
                var fms = new FileMetaKeyOnlySyntax(m_FileMeta, nodesList[0].token, null);

                return fms;
            }
            else if (nodesList[0].token?.type == ETokenType.Break )
            {
                var fms = new FileMetaKeyOnlySyntax(m_FileMeta, nodesList[0].token, null);

                return fms;
            }
            else if (nodesList[0].token?.type == ETokenType.Continue)
            {
                var fms = new FileMetaKeyOnlySyntax(m_FileMeta, nodesList[0].token, null);

                return fms;
            }
            else if( nodesList[0].token?.type == ETokenType.Goto || nodesList[0].token?.type == ETokenType.Label )
            {
                Token labelToken = null;
                if( nodesList.Count != 2 )
                {
                    Console.WriteLine("Error 解析Goto Label语法，只支持 goto id;的语法!!");
                }
                else
                {
                    labelToken = nodesList[1].token;
                    if( labelToken.type != ETokenType.Identifier )
                    {
                        Console.WriteLine("Error 解析GotoLabel中 后边必须使用普通字符");
                    }
                }
                var fms = new FileMetaKeyGotoLabelSyntax(m_FileMeta, nodesList[0].token, labelToken );

                return fms;
            }
            else
            {
                Console.WriteLine("Error 解析没有解析Key:[" + nodesList[0].token?.ToLexemeAllString() + "]的处理!!");
            }
            return null;
        }
        public FileMetaSyntax CreateFileMetaSyntax(List<Node> pNodeList)
        {            
            if( pNodeList.Count > 0 && pNodeList[0].nodeType == ENodeType.Key )
            {
                var fmet1 = CreateFileMetaKeySyntaxByNodesList(pNodeList);
                return fmet1;
            }
            else
            {
                return CrateFileMetaSyntaxNoKey(pNodeList);
            }
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
        private FileMetaSyntax CrateFileMetaSyntaxNoKey( List<Node> pNodeList)
        {
            List<Node> beforeNodeList = new List<Node>();
            Node assignNode = null;
            Node opAssignNode = null;
            ETokenType tet = ETokenType.None;
            List<Node> afterNodeList = new List<Node>();
            for (int j = 0; j < pNodeList.Count; j++)
            {
                var cnode = pNodeList[j];
                if (cnode.nodeType == ENodeType.Assign)
                {
                    if (assignNode == null && opAssignNode == null)
                    {
                        assignNode = cnode;
                        continue;
                    }
                }
                else
                {
                    tet = cnode.token.type;
                    if (tet == ETokenType.PlusAssign
                        || tet == ETokenType.MinusAssign
                        || tet == ETokenType.MultiplyAssign
                        || tet == ETokenType.DivideAssign
                        || tet == ETokenType.ModuloAssign
                        || tet == ETokenType.InclusiveOrAssign
                        || tet == ETokenType.CombineAssign
                        || tet == ETokenType.XORAssign
                        || tet == ETokenType.DoublePlus
                        || tet == ETokenType.DoubleMinus )
                    {
                        if (assignNode == null && opAssignNode == null)
                        {
                            opAssignNode = cnode;
                            continue;
                        }
                    }
                }
                if (assignNode != null || opAssignNode != null)
                    afterNodeList.Add(cnode);
                else
                {
                    beforeNodeList.Add(cnode);
                }
            }
            if (beforeNodeList.Count == 0)
            {
                Console.WriteLine("Error 发生错误，有符号的情况，必须有前置变量");
                return null;
            }
            else if (beforeNodeList.Count > 5 )
            {
                Console.WriteLine("Error 发生错误，前置不允许超过4个");
                return null;
            }

            Token staticToken = null;
            Token nameToken = null;
            FileMetaClassDefine classRef = null;
            FileMetaCallLink varRef = null;

            Node parseNode = new Node(null);
            parseNode.childList = beforeNodeList;
            parseNode.parseIndex = 0;
            HandleLinkNode( parseNode );
            var handleBeforeList = parseNode.childList;

            List<Node> defineNodeList = new List<Node>();
            for (int i = 0; i < handleBeforeList.Count; i++)
            {
                var cnode = handleBeforeList[i];
                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    defineNodeList.Add(cnode);
                }
                else
                {
                    Token token = cnode.token;
                    if (token?.type == ETokenType.Static)
                    {
                        if (staticToken != null)
                        {
                            Console.WriteLine("Error 多个Static!!");
                        }
                        staticToken = token;
                    }
                    else if (token?.type == ETokenType.Type
                        || token?.type == ETokenType.String)
                    {
                        defineNodeList.Add(cnode);
                    }
                    else
                    {
                        Console.WriteLine("Error 解析发现没有该节点!!");
                        //new Exception("Error 解析发现没有该节点");
                    }
                }
            }
            if (defineNodeList.Count == 0 || defineNodeList.Count > 3)
            {
                Console.WriteLine("Error 定义类型少于1");
                return null;
            }
            else if (defineNodeList.Count == 1)
            {
                nameToken = defineNodeList[0].token;
                varRef = new FileMetaCallLink(m_FileMeta, defineNodeList[0]);
            }
            else if (defineNodeList.Count == 2)
            {
                classRef = new FileMetaClassDefine(m_FileMeta, defineNodeList[0]);
                var node2 = defineNodeList[1];
                if (node2.linkTokenList.Count != 1)
                {
                    Console.WriteLine("Error 定义名称只允许一个字符串!!");
                    return null;
                }
                nameToken = node2.token;
            }

            FileMetaBaseTerm fme = null;
            if (assignNode != null && afterNodeList.Count > 0 && afterNodeList[0].nodeType == ENodeType.Key)
            {
                var fme22 = CreateFileMetaKeySyntaxByNodesList(afterNodeList);
                if ((afterNodeList[0].token.type == ETokenType.If
                    || afterNodeList[0].token.type == ETokenType.Switch)
                    )
                {
                    if (fme22 is FileMetaKeyIfSyntax)
                    {
                        fme = new FileMetaIfSyntaxTerm(m_FileMeta, fme22 as FileMetaKeyIfSyntax);

                    }
                    else if (fme22 is FileMetaKeySwitchSyntax)
                    {
                        fme = new FileMetaSwitchSyntaxTerm(m_FileMeta, fme22 as FileMetaKeySwitchSyntax );
                    }
                    else
                    {
                        Console.WriteLine("Error 生成if/switch语句失败!!");
                    }
                }
                else
                {
                    Console.WriteLine("Error 不允许嵌套除if/switch以外的语句!!");
                }
            }
            if (fme == null)
            {
                fme = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, afterNodeList, FileMetaTermExpress.EExpressType.Common);
            }

            if (assignNode != null)
            {
                if (nameToken == null)
                {
                    Console.WriteLine("Error 当为定义变量时，名称不能为空!!");
                    return null;
                }
                if (classRef != null)
                {
                    FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef,
                        nameToken, assignNode.token, staticToken, fme );
                    return fmdvs;
                }
                if (varRef != null)
                {
                    FileMetaOpAssignSyntax fms = new FileMetaOpAssignSyntax(varRef, assignNode.token, fme, true );
                    return fms;
                }
            }
            else if (opAssignNode != null)
            {
                if (varRef == null)
                {
                    Console.WriteLine("Error 当为定义变量时，名称不能为空!!");
                    return null;
                }
                FileMetaOpAssignSyntax fms = new FileMetaOpAssignSyntax(varRef, opAssignNode.token, fme);
                return fms;
            }
            else
            {
                if (nameToken == null)
                {
                    Console.WriteLine("Error 当为定义变量时，名称不能为空!!");
                    return null;
                }
                if (classRef != null)
                {
                    FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef, nameToken, staticToken, null, null );
                    return fmdvs;
                }
                else
                {
                    FileMetaCallSyntax fmcs = new FileMetaCallSyntax(varRef);

                    return fmcs;
                }
            }
            return null;
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
                var list = HandleCreateFileMetaSyntaxByPNode(pnode);

                if( list.Count > 0 )
                {
                    var fms = CreateFileMetaSyntax(list);

                    AddParseSyntaxNodeInfo(fms);
                }
                else
                {
                    Console.WriteLine("Error list.Count == 0 ");
                }

            }
            ParseSyntax(pnode);
        }
    }
}