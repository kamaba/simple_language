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
                Console.WriteLine("���� !!1 AddParseClassNodeInfo");
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
                Console.WriteLine("���� !!1 AddParseVariableInfo");
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
                Console.WriteLine("���� !!1 AddParseFunctionNodeInfo");
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
                Console.WriteLine("���� !!1 AddParseFunctionNodeInfo");
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
                Console.WriteLine("���� !!1 AddParseFunctionNodeInfo");
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

                Console.WriteLine("������Code����ṹ�ļ��ɹ�!!! ��һ������������Meta�ļ���");

                Console.WriteLine("����FileMeta�ļ��ɹ�!!! ��һ�������� ���л���� ");
            }
            else
            {
                Console.WriteLine("�������ִ��� ParseFile : " + currentNodeInfo.parseType.ToString());
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
                    ParseClassOrClassContent(pnode, isFile, isNamespace, isClass );
                }
                else if (isFunction || isStatements )
                {
                    ParseSyntax(pnode);
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
                            if (next3Node?.nodeType != ENodeType.SemiColon )
                            {
                                Console.WriteLine("Error Ӧ��ʹ��;�������!!");
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
                        Console.WriteLine("Error Data������ []�У���֧�ָ����͵�����" + curNode?.token?.ToLexemeAllString() );
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
                                    else if( next2Node.nodeType == ENodeType.Symbol )
                                    {
                                        var next3Node = curParentNode.childList[++index];

                                        if( next2Node.token?.type == ETokenType.Minus)
                                        {
                                            index++;
                                            int val = -(int)(next3Node.token?.lexeme);
                                            next3Node.token.SetLexeme( val );
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
                                                Console.WriteLine("Error Ӧ��ʹ��;�������!!");
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }
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
                                        if(ProjectManager.isUseForceSemiColonInLineEnd)
                                        {
                                            var next3Node = curParentNode.childList[++index];
                                            if( next3Node.nodeType != ENodeType.SemiColon )
                                            {
                                                Console.WriteLine("Error Ӧ��ʹ��;�������!!");
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }
                                    }
                                    else if( next2Node.nodeType == ENodeType.IdentifierLink )
                                    {
                                        index++;
                                        string name = next2Node.token?.lexeme.ToString();
                                        //���������ʱ����ȷ�����Ƿ���data��߿���ֱ������class���������� data����
                                        FileMetaMemberData fmmd = new FileMetaMemberData(m_FileMeta, curNode, next2Node, FileMetaMemberData.EMemberDataType.Data );

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
                                                Console.WriteLine("Error Ӧ��ʹ��;�������!!");
                                            }
                                            else
                                            {
                                                index++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Error ������=�ź�߷�Constֵ!!");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Error ������=�ź��ûֵ!!");
                                }

                            }
                        }
                    }
                    else if (curNode.nodeType == ENodeType.Comma)
                    {
                        continue;
                    }
                    else if ( curNode.nodeType == ENodeType.SemiColon)
                    {
                        continue;
                    }
                    else if( curNode.nodeType == ENodeType.LineEnd )
                    {
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Error Data�����У�������ʹ�ó��Զ����Ժ���ֶ�!!" + curNode?.token?.ToLexemeAllString() );
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
                        if (next2Node?.nodeType == ENodeType.Brace)  //Class1(){}�Ľṹ
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
                                Console.WriteLine("Error ��������enum���к����Ĵ���!!");
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
                if (curNode?.nodeType == ENodeType.Par)  //���еĴ�()�Ľṹ
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
                        Console.WriteLine("Error ��������Class1()�� ����������������!!");
                        break;
                    }
                }
                else if (curNode?.nodeType == ENodeType.Brace)     //��������
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
                        Console.WriteLine("Error �������ֱ��ʹ��{}�������﷨Ҫ��!!!");
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
                Console.WriteLine("Error Enum����token���� û�ҷ��ֿɽ�����NodeList λ����" + curnode.token?.ToLexemeAllString());
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
                    Console.WriteLine("Error ����token���� û�ҷ��ֿɽ�����NodeList λ����" + curnode.token?.ToLexemeAllString());
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
                    Console.WriteLine("Error ��Enum���ܰ�������������ݣ�ֻ��������enum!!");
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
                        Console.WriteLine("Error �ڽ���namespace �У���߸��Ų������������﷨!!");
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
                    Console.WriteLine("Error �ڽ���namespace �У�û���ҵ�namespace���õ�����!!");
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
                    Console.WriteLine("Error �ݲ�����ʹ��namespace ���������ռ�!!!" + ist.ToFormatString() + " λ��: " + currentNode.token.ToLexemeAllString());

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
                int lineEndCount = 0;
                var curNode = pnode.childList[index++];
                Node nextNode = null;
                if (index < pnode.childList.Count)
                {
                    nextNode = pnode.childList[index];
                }

                if (curNode.nodeType == ENodeType.IdentifierLink  )  //Class1
                {
                    if (nextNode.nodeType == ENodeType.LineEnd)   // Class\n(remove){nextNode}
                    {
                        nextNode = pnode.childList[index+1];
                        lineEndCount++;
                    }

                    if ( nextNode?.nodeType == ENodeType.Brace )  //Class1{}
                    {
                        index += (lineEndCount + 1);
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
                            if (next2Node.nodeType == ENodeType.LineEnd) //Class1()\n
                            {
                                next2Node = pnode.childList[index+2];
                                lineEndCount++;
                            }
                            if (next2Node?.nodeType == ENodeType.Brace)  //  Func(){}
                            {
                                index += (lineEndCount + 2);
                                type = 2;
                                curNode.parNode = nextNode;
                                curNode.blockNode = next2Node;
                                blockNode = curNode;
                                nodeList.Add(curNode);
                                break;
                            }
                        }
                    }
                    else if( nextNode?.nodeType == ENodeType.Angle )   //Class1<>
                    {
                        var next2Node = pnode.childList[index + 1];
                        if (next2Node.nodeType == ENodeType.LineEnd)
                        {
                            next2Node = pnode.childList[index + 2];
                            lineEndCount++;
                        }
                        if ( next2Node?.nodeType == ENodeType.Brace )  // Class1<>{}
                        {
                            index += (lineEndCount + 2);
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
                            if (next3Node.nodeType == ENodeType.LineEnd)
                            {
                                next3Node = pnode.childList[index + 3];
                                lineEndCount++;
                            }
                            if (next3Node.nodeType == ENodeType.Brace )   // Func<>(){}
                            {
                                index += (lineEndCount + 3);
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
                            index +=(lineEndCount+1);
                            curNode.angleNode = nextNode;
                            Console.WriteLine("�¼����Ž�β����黹û�н�����ȫ����!!");
                        }
                    }
                }
                
                if( isClass )//isClass
                {
                    if (curNode?.nodeType == ENodeType.Par )  //���еĴ�()�� һ���ڱ���ʽ��  xxx = a()
                    {
                        if (nextNode.nodeType == ENodeType.LineEnd)
                        {
                            nextNode = pnode.childList[index + 1];
                            lineEndCount++;
                        }
                        if ( nextNode?.nodeType == ENodeType.Brace )  //���д�(){}�Ľṹ
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
                                index+= (lineEndCount + 1);
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

                                    if(isLineEnd)
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
                    else 
                    {
                        if( ProjectManager.isUseForceSemiColonInLineEnd )
                        {
                            if( curNode.nodeType == ENodeType.SemiColon )
                            {
                                type = 1;
                                break;
                            }
                        }
                        else
                        {
                            if (curNode.nodeType == ENodeType.SemiColon)
                            {
                                type = 1;
                                break;
                            }
                            if (curNode.nodeType == ENodeType.LineEnd)
                            {
                                type = 1;
                                break;
                            }
                        }
                    }
                }

                nodeList.Add(curNode);
            }
            if( nodeList.Count == 0 )
            {
                var curnode = pnode.parseCurrent;
                Console.WriteLine("Error ����token���� û�ҷ��ֿɽ�����NodeList λ����" + curnode.token?.ToLexemeAllString() );
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
                    Console.WriteLine("Error ����token���� û�ҷ��ֿɽ�����NodeList λ����" + curnode.token?.ToLexemeAllString());
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
        //���� ident <> () {} [] . �Ľ�� ����Ԫ�ص�ͳһ����
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