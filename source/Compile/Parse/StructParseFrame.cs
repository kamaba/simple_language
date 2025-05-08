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
using System.Xml.Linq;
using System.Diagnostics;

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
            else if (currentNodeInfo.parseType == EParseNodeType.DataMemeber )
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
                        case ETokenType.Class:
                        case ETokenType.Public:
                        case ETokenType.Private:
                        case ETokenType.Internal:
                        case ETokenType.Projected:
                            {
                                ParseNamespaceOrTopClass(pnode);
                            }
                            break;
                        default:
                            {
                                Console.WriteLine("Error 不允许 在File头级目录中出现 : " + node.token.lexeme.ToString());
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
                    Console.WriteLine("Error 不允许 在File头级目录中出现2 : " + node.token?.lexeme.ToString());
                }
            }

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
                            Console.WriteLine("Error 在+-符前边不允许有其它非const类型存在!");
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
                    Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
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

                                if( j < curParentNode.childList.Count )
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
                            else if (next2Node.nodeType == ENodeType.Angle)   //Class1<T>   Func<T>( T t );  array<int> arr1;
                            {
                                curNode.angleNode = next2Node;
                                index = j;
                                continue;
                            }
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
                                    Console.WriteLine("Error Data数据中，不允许使用除自定义以后的字段!!" + curNode?.token?.ToLexemeAllString());
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
                        Console.WriteLine("Error 后边必须有延伸位...");
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
                                Console.WriteLine("Error 如果是 x=-??的形式，在符号后边");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error 如果是 x=-??的形式，在符号后边");
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
                            Console.Write("Error 在定义Data数据的时候，如果有折行，只允许 =\n{} =\n[] 两种形式! ");
                        }
                    }
                    else
                    {
                        Console.Write("Error 在定义Data数据的时候，不允许=号后边有其它形式的存在");
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
                    Console.WriteLine("Error 报错，不允许 解析Data有其它的类型出现!" + curNode.token.ToLexemeAllString() );
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
                    if (ProjectManager.isUseForceSemiColonInLineEnd)
                    {
                        Console.WriteLine("Error 在解析namespace 中，需要强制;号结束");
                        break;
                    }
                    else
                        break;
                }
                else if( nextNode.nodeType == ENodeType.SemiColon )
                {
                    break;
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

                ParseNamespaceOrTopClass(currentNode.blockNode);

                m_CurrentNodeInfoStack.Pop();
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
            FileMetaMemberFunction cpf = new FileMetaMemberFunction(m_FileMeta, blockNode, nodeList);

            AddParseFunctionNodeInfo(cpf);

            ParseSyntax(blockNode);

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
                ParseInClass(blockNode);
            }

            m_CurrentNodeInfoStack.Pop();
        }
        // 只解析 在全局文件下的 namespace 下 的 还有就是文件class
        public void ParseNamespaceOrTopClass( Node pnode )
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

                nextNode = null;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }
                
                if (curNode.nodeType == ENodeType.Key)
                {
                    if( curNode.token.type == ETokenType.Namespace )
                    {
                        isClass = 2;
                    }
                    else if( curNode.token.type == ETokenType.Class )
                    {
                        isClass = 1;
                    }
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Comma)
                {
                    nodeList.Add(curNode);
                }
                else if(curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    nodeList.Add(curNode);
                    if( isClass == 1 )
                    {
                        if (nextNode.nodeType == ENodeType.Angle)   //Class1<>
                        {
                            pnode.parseIndex++;
                            index++;
                            curNode.angleNode = nextNode;
                        }
                        else if (nextNode.nodeType == ENodeType.LineEnd)
                        {
                            continue;
                        }
                        else if (nextNode.nodeType == ENodeType.IdentifierLink)  // Class1 c1
                        {
                            Console.WriteLine("Error 不允许在顶级Class里边有变量的存在!!");
                            continue;
                        }
                        else if (nextNode?.nodeType == ENodeType.Assign) //Class1 = "test1", [],{}, (2+3); Class2(10);
                        {
                            Console.WriteLine("Error 不允许在顶级Class里边有 新建Class1的存在");
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Error 不允许在顶级Class里边有 新建Class1的存在1111111");
                            continue;
                        }
                    }
                    else if( isClass == 2  )
                    {
                        if (nextNode?.nodeType == ENodeType.Brace)
                        {
                            curNode.blockNode = nextNode;
                            continue;
                        }
                        else if (nextNode?.nodeType == ENodeType.SemiColon)
                        {
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Error 不允许在顶级namespace里边有 新建namespace的存在1111111");
                            continue;
                        }
                    }
                }
                else if( curNode.nodeType == ENodeType.LineEnd )
                {
                    if( nextNode?.nodeType == ENodeType.Brace )
                    {
                        if(isClass == 0 )
                        {
                            isClass = 1;
                        }
                        curNode.blockNode = nextNode;
                        curNode = nextNode;
                        index++;
                        pnode.parseIndex++;
                        isCanAdd = true;
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Error 该解析只允许 有 classname/namespacename\n{}的结构!");
                    }
                }
                else if (curNode?.nodeType == ENodeType.Brace)
                {
                    curNode.blockNode = nextNode;
                    isCanAdd = true;
                    break;
                }
                else
                {
                    Console.WriteLine("Error 不允许在解释Class的时候，有错误 的语法--------------------" + curNode.token?.ToLexemeAllString() );
                }
            }

            if(isCanAdd )
            {
                if(isClass == 1 )
                {
                    AddFileMetaClasss(curNode, nodeList);
                }
                else if( isClass == 2 )
                {
                    if (nodeList.Count == 2)
                    {
                        FileMetaNamespace fmn = new FileMetaNamespace(nodeList[0], nodeList[1]);
                        AddParseNamespaceNodeInfo(fmn);
                        ParseNamespaceOrTopClass(pnode);
                        m_CurrentNodeInfoStack.Pop();
                    }
                    else
                    {
                        Console.WriteLine("Error 对于 namespace A.B{}的格式 多了一个参数!1");
                    }
                }
                else
                {
                    Console.WriteLine("Error 没有发现是Class还是Namespace的关键字!");
                }
            }
        }
        public void ParseInClass( Node pnode )
        {
            List<Node> nodeList = new List<Node>();

            Node nextNode = null;
            if(pnode.parseIndex >= pnode.childList.Count)
            {
                return;
            }
            int index = pnode.parseIndex;

            int parseType = 0;      // 1->是类class\n{}  2->函数 init()\n{}      3->变量  int a;  int a=20; a = 20; a = {}\n a = {};
            Node block = null;
            for ( index = pnode.parseIndex; index < pnode.childList.Count;)
            {
                var curNode = pnode.childList[index++];

                nextNode = null;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }
                
                if (curNode.nodeType == ENodeType.Key)          // public class int object void
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.ConstValue)
                {
                    nodeList.Add(curNode);
                }
                else if (curNode.nodeType == ENodeType.Comma )
                {
                    nodeList.Add(curNode);
                }
                else if(curNode.nodeType == ENodeType.IdentifierLink)  //Class1
                {
                    nodeList.Add(curNode);
                    if (nextNode?.nodeType == ENodeType.Par)   //Class1()
                    {
                        parseType = 2;
                        index++;
                        curNode.parNode = nextNode;
                    }
                    else if (nextNode?.nodeType == ENodeType.Angle)   //Class1<T>   Func<T>( T t );  array<int> arr1;
                    {
                        curNode.angleNode = nextNode;
                        index++;
                        if( index < pnode.childList.Count)
                        {
                            if(pnode.childList[index].nodeType == ENodeType.IdentifierLink )
                            {
                                parseType = 3;
                            }
                        }
                    }
                    else if( nextNode?.nodeType == ENodeType.IdentifierLink )
                    {
                        parseType = 3;
                    }
                }
                else if( curNode.nodeType == ENodeType.Assign )
                {
                    nodeList.Add(curNode);
                    parseType = 3;
                    bool isLineEnd = false;
                    for (int index2 = index; index2 < pnode.childList.Count; index2++)
                    {
                        var node2 = pnode.childList[index2];
                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                        {
                            if (node2.nodeType == ENodeType.SemiColon)
                            {
                                isLineEnd = true;
                            }
                        }
                        else
                        {
                            if (node2.nodeType == ENodeType.LineEnd)
                            {
                                isLineEnd = true;
                            }
                            else if(node2.nodeType == ENodeType.SemiColon )
                            {
                                if (pnode.childList.Count > index2 + 1)
                                {
                                    if (pnode.childList[index2 + 1].nodeType == ENodeType.LineEnd )
                                    {
                                        index2++;
                                        isLineEnd = true;
                                    }
                                }
                                isLineEnd = true;
                            }
                        }
                        if (isLineEnd)
                        {
                            index = index2 + 1;
                            break;
                        }
                        else
                        {
                            nodeList.Add(node2);
                        }
                    }
                    break;
                    //index++;
                    //if (index < pnode.childList.Count)
                    //{
                    //    var next2Node = pnode.childList[index];
                    //    if (next2Node.nodeType == ENodeType.LineEnd) //只允许有一次回车  name = \n
                    //    {
                    //        next2Node = pnode.childList[++index];
                    //    }
                    //    if (next2Node.nodeType == ENodeType.Symbol
                    //        || next2Node.nodeType == ENodeType.ConstValue
                    //        || next2Node.nodeType == ENodeType.Par
                    //        || next2Node.nodeType == ENodeType.Bracket
                    //        || next2Node.nodeType == ENodeType.IdentifierLink)
                    //    {
                    //        nodeList.Add(curNode);
                    //        nodeList.Add(nextNode);
                    //        int j = 0;
                    //        for (j = index; j < pnode.childList.Count; j++)
                    //        {
                    //            if ((pnode.childList[j].nodeType == ENodeType.LineEnd
                    //                || pnode.childList[j].nodeType == ENodeType.SemiColon))
                    //            {
                    //                j++;
                    //                break;
                    //            }
                    //            nodeList.Add(pnode.childList[j]);
                    //        }
                    //        index = j;
                    //        FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, nodeList);
                    //        nodeList.Clear();

                    //        if (currentNodeInfo.parseType == EParseNodeType.Class)
                    //        {
                    //            currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        Console.WriteLine("Error 解析： “ + " + next2Node?.token?.ToLexemeAllString());
                    //    }
                    //}
                }
                else if (curNode.nodeType == ENodeType.LineEnd)
                {
                    if (nextNode?.nodeType == ENodeType.Brace)
                    {
                        block  = nextNode;
                        index++;
                        break;
                    }
                    if (!ProjectManager.isUseForceSemiColonInLineEnd)
                    {
                        if(parseType == 3 )
                        {
                            break;
                        }
                    }
                }
                else if( curNode.nodeType == ENodeType.Brace )
                {
                    block = curNode;
                    break;
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
                            break;
                        }
                    }
                    Console.WriteLine("Error 不允许--------------------");
                }
            }

            pnode.parseIndex = index;
            if(parseType == 0 )
            {
                parseType = 1;
            }
            if ( parseType == 1 )
            {
                if(nodeList.Count > 0 && block != null )
                {
                    AddFileMetaClasss(block, nodeList);
                }
                else
                {
                    Console.WriteLine("正常处理");
                    return;
                }
            }
            else if( parseType == 2 )
            {
                AddFileMetaFunctionVariable(pnode, block, nodeList);
            }
            else if( parseType == 3 )
            {
                FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, nodeList);

                if (currentNodeInfo.parseType == EParseNodeType.Class)
                {
                    currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
                }
                else
                {
                    Console.WriteLine("Error 未111111111111123123123");
                }
            }
            ParseInClass(pnode);
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