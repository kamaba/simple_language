//****************************************************************************
//  File:      StructParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Parse;
using SimpleLanguage.source;
using SimpleLanguage.source.Logging;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SimpleLanguage.Compile
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
            DataMemeber,
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
        protected Stack<ParseCurrentNodeInfo> m_CurrentNodeInfoStack = new Stack<ParseCurrentNodeInfo>();

        protected Node m_RootNode = null;
        protected List<Token> m_TokenList = new List<Token>();
        public StructParse(FileMeta fm, Node node, List<Token> tokenList    )
        {
            m_FileMeta = fm;
            m_RootNode = node;
            m_TokenList = tokenList;
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
                currentNodeInfo.codeFile.AddFileMetaAllNamespace(fmn);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.Namespace)
            {
                currentNodeInfo.codeNamespace.AddFileNamespace(fmn);
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
                Log.AddInStructFileMeta(EError.None, "错误 !!1 AddParseClassNodeInfo");
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
                Log.AddInStructFileMeta(EError.None, "错误 !!1 AddParseVariableInfo");
                return;
            }
        }
        public void AddParseDataInfo(FileMetaMemberData fmmd)
        {
            if (currentNodeInfo.parseType == EParseNodeType.Class)
            {
                currentNodeInfo.codeClass.AddFileMemberData(fmmd);
            }
            else if (currentNodeInfo.parseType == EParseNodeType.DataMemeber )
            {
                currentNodeInfo.codeData.AddFileMemberData(fmmd);
            }
            else
            {
                Log.AddInStructFileMeta(EError.None, "错误 !!1 AddParseFunctionNodeInfo");
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
                Log.AddInStructFileMeta(EError.None, "错误 !!1 AddParseFunctionNodeInfo");
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
                Log.AddInStructFileMeta(EError.None, "错误 !!1 AddParseFunctionNodeInfo");
                return;
            }

            if (isAddParseCurrentNNode)
            {
                ParseCurrentNodeInfo pcni = new ParseCurrentNodeInfo(fms);
                m_CurrentNodeInfoStack.Push(pcni);
            }
        }
        public void ParseTokenToFileMeta()
        {
            AddParseFileNodeInfo();

            foreach (var token in m_TokenList)
            {
                switch (token.type)
                {
                    case ETokenType.Import:
                        {
                            //ParseImport(pnode);
                        }
                        break;
                    case ETokenType.Namespace:
                        {
                            //ParseNamespace(pnode);
                        }
                        break;
                    case ETokenType.Const:
                    case ETokenType.Data:
                    case ETokenType.Enum:
                    case ETokenType.Class:
                    case ETokenType.Extern:
                    case ETokenType.Public:
                    case ETokenType.Private:
                    case ETokenType.Projected:
                    case ETokenType.Partial:
                        {
                            //ParseNamespaceOrTopClass(pnode);
                        }
                        break;
                    default:
                        {
                            //Log.AddInStructFileMeta(EError.None, "Error 不允许 在File头级目录中出现 : " + node.token.lexeme.ToString());
                        }
                        break;
                }
            }
        }
        public void ParseRootNodeToFileMeta()
        {
            AddParseFileNodeInfo();

            Node pnode = m_RootNode;
            while(true)
            {
                if (CheckEnd(pnode))
                {
                    break;
                }
                var node = m_RootNode.childList[pnode.parseIndex];
                if( node.nodeType == ENodeType.LineEnd )
                {
                    pnode.parseIndex++;
                    continue;
                }

                if (node.nodeType == ENodeType.Key)
                {
                    switch (node.token.type)
                    {
                        case ETokenType.Import:
                            {
                                ParseImport(pnode);
                            }
                            break;
                        case ETokenType.Namespace:
                            {
                                ParseNamespace(pnode);
                            }
                            break;
                        case ETokenType.Const:
                        case ETokenType.Data:
                        case ETokenType.Enum:
                        case ETokenType.Class:
                        case ETokenType.Extern:
                        case ETokenType.Public:
                        case ETokenType.Private:
                        case ETokenType.Projected:
                        case ETokenType.Partial:
                            {
                                ParseNamespaceOrTopClass(pnode);
                            }
                            break;
                        default:
                            {
                                Log.AddInStructFileMeta( EError.None, "Error 不允许 在File头级目录中出现 : " + node.token.lexeme.ToString());
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
                    Log.AddInStructFileMeta(EError.None, "Error 不允许 在File头级目录中出现2 : " + node.token?.lexeme.ToString());
                }
            }

            var fileCode = m_CurrentNodeInfoStack.Pop();

            if (fileCode.parseType == EParseNodeType.File)
            {
#if DEBUG
                m_FileMeta.SetDeep(0);
#endif

                Log.AddProcess( EProcess.ParseNode, EError.None, "解析成Code代码结构文件成功!!! 下一步，可以生产Meta文件了 \n " +
                    "生成FileMeta文件成功!!! 下一步，可以 进行混合了");
            }
            else
            {

                Log.AddProcess(EProcess.ParseNode, EError.ParseFileError, "解析出现错误 ParseFile : " + currentNodeInfo.parseType.ToString() );
                return;
            }
            return;
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
                    currentNode.blockNode = nextNode;
                    isBlock = true;
                    break;
                }
                else if (nextNode.nodeType == ENodeType.IdentifierLink)
                {
                    if (namespaceNode != null)
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 在解析namespace 中，后边跟着参数多于正常语法!!");
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
                                    currentNode.blockNode = next2Node;
                                    isBlock = true;
                                    pnode.parseIndex += 3;
                                    break;
                                }
                            }
                        }
                        else if (next2Node?.nodeType == ENodeType.Brace)
                        {
                            currentNode.blockNode = next2Node;
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
                        Log.AddInStructFileMeta(EError.None, "Error 在解析namespace 中，需要强制;号结束");
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
                m_FileMeta.AddFileSearchNamespace(fmn);
            }
        }
        public static bool CheckEnd(Node pnode)
        {
            if (pnode.parseIndex >= pnode.childList.Count)
            {
                return true;
            }
            return false;
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

            int isClass = 0;        //0 unknows 1 class 2namespace
            for (index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                curNode = pnode.childList[index++];
                pnode.parseIndex = index;

                if (curNode.nodeType == ENodeType.Key)
                {
                    if (curNode.token.type == ETokenType.Namespace)
                    {
                        isClass = 2;
                    }
                    else if (curNode.token.type == ETokenType.Class)
                    {
                        isClass = 1;
                    }
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.LeftAngle)   //Class1<T> 
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.RightAngle)   //Class1<T>   Func<T>( T t );  array<int> arr1;
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Comma)
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
                        curNode = nextNode;
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
                    break;
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error 不允许在解释Class的时候，有错误 的语法--------------------" + curNode.token?.ToLexemeAllString());
                }
            }
            pnode.parseIndex = index;

            if (isCanAdd)
            {
                if (isClass == 1)
                {
                    AddFileMetaClasss(curNode, nodeList);
                    ParseNamespaceOrTopClass(pnode);
                }
                else if (isClass == 2)
                {
                    if (nodeList.Count == 2)
                    {
                        FileMetaNamespace fmn = new FileMetaNamespace(nodeList[0], nodeList[1]);
                        AddParseNamespaceNodeInfo(fmn);
                        if (nodeList[1].blockNode != null)
                        {
                            m_FileMeta.AddFileDefineNamespace(fmn);
                            ParseNamespaceOrTopClass(nodeList[1].blockNode);
                        }
                        else
                        {
                            m_FileMeta.AddFileSearchNamespace(fmn);
                        }
                        m_CurrentNodeInfoStack.Pop();
                        ParseNamespaceOrTopClass(pnode);
                    }
                    else
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 对于 namespace A.B{}的格式 多了一个参数!1");
                    }
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error 没有发现是Class还是Namespace的关键字!");
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

            int parseType = 0;      // 1->是类class\n{}  2->函数 init()\n{}      3->变量  int a;  int a=20; a = 20; a = {}\n a = {};
            Node block = null;
            for (index = pnode.parseIndex; index < pnode.childList.Count;)
            {
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
                else if( curNode.nodeType == ENodeType.Symbol )
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.ConstValue)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.LeftAngle)   //Class1<T> 
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.RightAngle)   //Class1<T>   Func<T>( T t );  array<int> arr1;
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Assign)
                {
                    nodeList.Add(curNode);
                    parseType = 3;
                }
                else if (curNode.nodeType == ENodeType.Comma)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Par)   //Class1()
                {
                    nodeList.Add(curNode);
                    if (parseType == 0)
                        parseType = 2;
                }
                else if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    nodeList.Add(curNode);
                }
                else if( curNode.nodeType == ENodeType.SemiColon )
                {
                    if (parseType == 3 || parseType == 2 )
                    {
                        break;
                    }
                    else
                    {
                        Log.AddInStructFileMeta(EError.None, "Error StructParseFrame.ParseClassNode 解析的类后边不用使用;号结尾!! ");
                        Log.AddInStructFileMeta(EError.None, "一般是只定义了类变量，没有赋值，正常后边应该可以使用=null赋值");
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
                            if (parseType == 3 || parseType == 2 )
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
                    break;
                }
                else if( curNode.nodeType == ENodeType.Bracket )
                {
                    nodeList[nodeList.Count - 1].AddBracketNode( curNode );
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error ParseClassNode 不允许在解释Class的时候，有错误 的语法--------------------" + curNode.token?.ToLexemeAllString());
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
                    AddFileMetaClasss(block, nodeList);
                }
            }
            else if (parseType == 2)
            {
                AddFileMetaFunctionVariable(pnode, block, nodeList);
            }
            else if (parseType == 3)
            {
                FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, nodeList);

                if (currentNodeInfo.parseType == EParseNodeType.Class)
                {
                    currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error 未111111111111123123123");
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
                if (curNode.nodeType == ENodeType.Brace)  //Class1 [{},{}]
                {
                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, false, FileMetaMemberData.EMemberDataType.Data);

                    AddParseDataInfo(fmmd);

                    ParseDataNode(curNode);

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
                            Log.AddInStructFileMeta(EError.None, "Error 在+-符前边不允许有其它非const类型存在!");
                            continue;
                        }
                    }
                }
                else if (curNode.nodeType == ENodeType.ConstValue)   // ["stringValue","Stvlue"]
                {
                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, false, FileMetaMemberData.EMemberDataType.ConstValue);

                    currentNodeInfo.codeData.AddFileMemberData(fmmd);
                }
                else if (curNode.nodeType == ENodeType.IdentifierLink)   // [Class1(),Class2()]
                {
                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, false, FileMetaMemberData.EMemberDataType.Class);

                    currentNodeInfo.codeData.AddFileMemberData(fmmd);
                }
                else if (curNode?.nodeType == ENodeType.Bracket) // [[],[]]
                {
                    FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, false, FileMetaMemberData.EMemberDataType.Array);

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
                    Log.AddInStructFileMeta(EError.None, "Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                    continue;
                }
            }
            bracketNode.parseIndex = index;
        }
        public void ParseDataNode(Node pnode)
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
                Node nextNode = null;
                if (index < curParentNode.childList.Count)
                {
                    nextNode = curParentNode.childList[index];
                }

                if (curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    if (assignNode == null)
                    {
                        frontList.Add(curNode);
                    }
                    else
                    {
                        backList.Add(curNode);

                        for( int j = index; j < curParentNode.childList.Count; )
                        {
                            var next2Node = curParentNode.childList[j++];
                            if (next2Node == null) continue;

                            if (next2Node.nodeType == ENodeType.Par)   //Class1()
                            {
                                curNode.parNode = next2Node;
                                index = j;
                                isParseEnd = true;
                                if ( j < curParentNode.childList.Count )
                                {
                                    var next3Node = curParentNode.childList[j];
                                    if (next3Node == null) continue;
                                    if( next3Node.nodeType == ENodeType.LineEnd )
                                    {
                                        if( j + 1 < curParentNode .childList.Count )
                                        {
                                            var next4Node = curParentNode.childList[j + 1];
                                            if (next4Node == null) continue;
                                            if (next4Node.nodeType == ENodeType.Brace)
                                            {
                                                curNode.blockNode = next4Node;
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
                                        curNode.blockNode = next3Node;
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
                                curNode.blockNode = next2Node;
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
                                else
                                {
                                    Log.AddInStructFileMeta(EError.None, "Error Data数据中，不允许使用除自定义以后的字段!!" + curNode?.token?.ToLexemeAllString());
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
                            m_CurrentNodeInfoStack.Pop();
                        }
                    }
                }
                else if (curNode.nodeType == ENodeType.Symbol )
                {
                    if( assignNode  == null )
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

                    if( nextNode == null )
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 后边必须有延伸位...");
                        continue;
                    }

                    int parseType = 0;
                    //=号后边第一位，必须是idetifier 或者是 constValue值，  如果折行，只允许 \n{}  \
                    if (nextNode.nodeType == ENodeType.ConstValue) // a = 10 不允许折行  
                    {
                        index++;
                        backList.Add(nextNode);
                    }
                    else if(nextNode.nodeType == ENodeType.IdentifierLink )
                    {
                    }
                    else if (nextNode.nodeType == ENodeType.Symbol && 
                        (nextNode.token.type == ETokenType.Plus
                            || nextNode.token.type == ETokenType.Minus ) )
                    {
                        if( index + 1 < curParentNode.childList.Count )
                        {
                            var next2Node = curParentNode.childList[index + 1];
                            if( next2Node.nodeType == ENodeType.ConstValue )
                            {
                                index+=2;
                                backList.Add(nextNode);
                                backList.Add(next2Node);
                            }
                            else
                            {
                                Log.AddInStructFileMeta(EError.None, "Error 如果是 x=-??的形式，在符号后边");
                            }
                        }
                        else
                        {
                            Log.AddInStructFileMeta(EError.None, "Error 如果是 x=-??的形式，在符号后边");
                        }
                    }
                    else if(nextNode.nodeType == ENodeType.Brace )
                    {
                        index++;
                        parseType = 1;
                        blockNode = nextNode;
                    }
                    else if( nextNode.nodeType == ENodeType.Bracket )
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
                            Log.AddInStructFileMeta(EError.None, "Error 在定义Data数据的时候，如果有折行，只允许 =\n{} =\n[] 两种形式! ");
                        }
                    }
                    else
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 在定义Data数据的时候，不允许=号后边有其它形式的存在");
                    }

                    if( parseType > 0 )
                    {
                        FileMetaMemberData.EMemberDataType emdt = parseType == 1 ? FileMetaMemberData.EMemberDataType.Data : FileMetaMemberData.EMemberDataType.Array;
                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, frontList, assignNode, backList, true, emdt);
                        frontList.Clear();
                        isParseEnd = false;
                        assignNode = null;
                        AddParseDataInfo(fmmd);
                        if( parseType == 1 )
                        {
                            ParseDataNode(blockNode);
                        }
                        else if( parseType == 2 )
                        {
                            ParseDataBracketNode(blockNode);
                        }
                        m_CurrentNodeInfoStack.Pop();
                    }
                    continue;
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    isParseEnd = true;
                }
                else if (curNode.nodeType == ENodeType.SemiColon)
                {
                    isParseEnd = true;
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error 报错，不允许 解析Data有其它的类型出现!" + curNode.token.ToLexemeAllString() );
                }

                if (isParseEnd)
                {
                    if (frontList.Count > 0)
                    {
                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, frontList, assignNode, backList, true, FileMetaMemberData.EMemberDataType.ConstValue );
                        frontList.Clear();
                        backList.Clear();
                        assignNode = null;
                        AddParseDataInfo(fmmd);
                        m_CurrentNodeInfoStack.Pop();
                    }
                    isParseEnd = false;
                }
            }
            curParentNode.parseIndex = index;            
        }
        public void ParseEnumNode(Node pnode)
        {
            if (pnode.parseIndex >= pnode.childList.Count)
                return;

            var action = delegate( List<Node> addnode )
            {
                for (int i = 0; i < addnode.Count; i++)
                {
                    var curNodexxx = addnode[i];
                    if (curNodexxx.nodeType == ENodeType.Key
                        && curNodexxx.token.type == ETokenType.Enum)
                    {
                        Log.AddInStructFileMeta(EError.None, "error 不允许在enum 内容里边再嵌套enum");
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
                    if(isAssign)
                    {
                        nodeList.Add(curNode);
                        if (nextNode?.nodeType == ENodeType.Par)  //Enum1()
                        {
                            curNode.parNode = nextNode;
                            if (index + 1 < pnode.childList.Count)
                            {
                                Node next2Node = pnode.childList[index + 1];
                                if (next2Node.nodeType == ENodeType.LineEnd)
                                {
                                    index += 1;
                                    isParse = true;
                                    if (index + 1 < pnode.childList.Count)
                                    {
                                        next2Node = pnode.childList[index + 1];
                                        if( next2Node?.nodeType == ENodeType.Brace )
                                        {
                                            index += 2;
                                            curNode.blockNode = next2Node;
                                            blockNode = next2Node;
                                        }
                                    }
                                }
                                else if (next2Node?.nodeType == ENodeType.Brace)  //Class1(){}的结构
                                {
                                    index += 2;
                                    blockNode = next2Node;
                                    isParse = true;
                                }
                                else if (next2Node?.nodeType == ENodeType.SemiColon)
                                {
                                    index += 1;
                                    isParse = true;
                                }
                            }
                        }
                        else if (nextNode?.nodeType == ENodeType.LeftAngle)    // Class1<>
                        {
                            var next2Node = pnode.childList[index + 1];
                            if (next2Node?.nodeType == ENodeType.Brace)  // Class1<int>(){}
                            {
                                index += 2;
                                //curNode.angleNode = nextNode;
                                curNode.blockNode = next2Node;
                                blockNode = curNode;
                            }
                            else
                            {
                                index++;
                                //curNode.angleNode = nextNode;
                            }
                        }
                        else if (nextNode?.nodeType == ENodeType.LineEnd
                            || nextNode?.nodeType == ENodeType.SemiColon)
                        {
                        }
                        else
                        {
                            Log.AddInStructFileMeta(EError.None, "在解析enum member 中 成员变量 如果是identifier格式，则后边不允许跟当前格式");
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
                else if( curNode.nodeType == ENodeType.ConstValue )
                {
                    nodeList.Add(curNode);
                }
                else if( curNode.nodeType == ENodeType.Key && curNode.token.type == ETokenType.Mut )
                {
                    nodeList.Add(curNode);
                }
                else
                {
                    Log.AddInStructFileMeta(EError.None, "Error 解析Enum memeber 时，不允许有其它形式的存在!");
                }

                if(isParse )
                {
                    if(nodeList.Count > 0)
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
                            Debug.WriteLine("Error 应该使用;结束语句!!");
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
                    Debug.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
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
            FileMetaMemberFunction cpf = new FileMetaMemberFunction(m_FileMeta, blockNode, nodeList);

            AddParseFunctionNodeInfo(cpf);

            if(blockNode != null )
            {
                ParseSyntax(blockNode);
            }

            m_CurrentNodeInfoStack.Pop();
        }
        void AddFileMetaClasss( Node blockNode, List<Node> nodeList)
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
        public static List<Node> HandleBeforeNode(Node node)
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
                if( node.childList[i].isDel == false )
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
                || finalNode?.token?.type == ETokenType.New ) //Class1???
            {
                if (currentExpressNode.nodeType == ENodeType.LeftAngle )       //Class<>??
                {
                    HandleAngleExpressNode(node, finalNode);
                    _HandleExpressNodeProcess(node, finalNode);
                    return;
                }
                else if (currentExpressNode.nodeType == ENodeType.Par)             //Class()?
                {
                    node.parseIndex++;
                    finalNode.parNode = currentExpressNode;
                    currentExpressNode.isDel = true;
                    if (currentExpressNode.extendLinkNodeList.Count > 0)
                    {
                        finalNode.SetLinkNode(currentExpressNode.extendLinkNodeList);   // Q.Map()[.Cast]
                        _HandleExpressNodeProcess(node, currentExpressNode);           //Q.Map().[Cast]
                        _HandleExpressNodeProcess(node, finalNode);
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
                else if( currentExpressNode.nodeType == ENodeType.Brace )           // M.Class(){}
                {
                    node.parseIndex++;
                    finalNode.blockNode = currentExpressNode;
                    currentExpressNode.isDel = true;
                    _HandleExpressNodeProcess(currentExpressNode, null);
                    return;
                }
                else if( currentExpressNode.nodeType == ENodeType.Bracket )         //Class[][][][](){}
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
                    parentNode.angleNode = cnode;
                    //parentNode.AddAngleNode(cnode);
                }
                else if (cnode.nodeType == ENodeType.RightAngle)
                {
                    cnode.isDel = true;
                    parentNode.angleNode.endToken = cnode.token;
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
                            parentNode.parNode = nextNode;
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
                                        parentNode.blockNode = next2ExpressNode;
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
        //    if (finalNode?.nodeType == ENodeType.IdentifierLink) //Class1???
        //    {
        //        if (currentExpressNode.nodeType == ENodeType.LeftAngle)       //Class<>??
        //        {
        //            HandleAngleNode(node, finalNode);
        //            _HandleBeforeNodeProcess(node, inputFinaleNode);
        //            return;
        //        }
        //        else if (currentExpressNode.nodeType == ENodeType.Par)             //Class()?
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
        //        else if (currentExpressNode.nodeType == ENodeType.Brace)           // M.Class(){}
        //        {
        //            finalNode.blockNode = currentExpressNode;
        //            currentExpressNode.isDel = true;
        //            node.parseIndex++;
        //            _HandleBeforeNodeProcess(currentExpressNode);
        //            _HandleBeforeNodeProcess(node, finalNode);
        //            return;
        //        }
        //        else if (currentExpressNode.nodeType == ENodeType.Bracket)         //Class[]
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
    }
}