//****************************************************************************
//  File:      FileMetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile.Parse;
using static SimpleLanguage.Compile.CoreFileMeta.FileMetaMemberVariable;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    //public class FileMetaMemberData : FileMetaBase
    //{
    //    public enum EMemberDataType
    //    {
    //        None,
    //        NameClass,
    //        NoNameClass,
    //        Array,
    //        KeyValue,
    //        Value,
    //        Data,
    //    }

    //    public Token nameToken => m_Token;
    //    public List<FileMetaMemberData> fileMetaMemberData => m_FileMetaMemberData;
    //    public FileMetaConstValueTerm fileMetaConstValue => m_FileMetaConstValue;
    //    public FileMetaCallTerm fileMetaCallTermValue => m_FileMetaCallTermValue;
    //    public EMemberDataType DataType => m_MemberDataType;

    //    private Token m_AssignToken = null;
    //    private List<Node> m_NodeList = new List<Node>();
    //    private List<FileMetaMemberData> m_FileMetaMemberData = new List<FileMetaMemberData>();
    //    private EMemberDataType m_MemberDataType = EMemberDataType.None;
    //    private FileMetaConstValueTerm m_FileMetaConstValue = null;
    //    private FileMetaCallTerm m_FileMetaCallTermValue = null;

    //    public FileMetaMemberData(FileMeta fm, Node _beforeNode, Node _afterNode, EMemberDataType dataType )
    //    {
    //        m_MemberDataType = dataType;

    //        m_FileMeta = fm;

    //        if( dataType == EMemberDataType.NameClass)
    //        {
    //            m_Token = _beforeNode.token;
    //        }
    //        else if( dataType == EMemberDataType.KeyValue )
    //        {
    //            m_Token = _beforeNode.token;
    //            m_FileMetaConstValue = new FileMetaConstValueTerm(m_FileMeta, _afterNode.token);
    //        }
    //        else if( dataType == EMemberDataType.Array )
    //        {
    //            m_Token = _beforeNode.token;
    //        }
    //        else if( dataType == EMemberDataType.Value )
    //        {
    //            m_FileMetaConstValue = new FileMetaConstValueTerm(m_FileMeta, _beforeNode.token);
    //        }
    //        else if( dataType == EMemberDataType.Data )
    //        {
    //            m_Token = _beforeNode.token;
    //            m_FileMetaCallTermValue = new FileMetaCallTerm(m_FileMeta, _afterNode);
    //        }
    //    }
    //    public void AddFileMemberData( FileMetaMemberData fmmd )
    //    {
    //        m_FileMetaMemberData.Add(fmmd);
    //    }
    //    public override void SetDeep(int _deep)
    //    {
    //        m_Deep = _deep;
    //        for (int i = 0; i < m_FileMetaMemberData.Count; i++)
    //        {
    //            m_FileMetaMemberData[i].SetDeep(m_Deep + 1);
    //        }
    //    }
    //    public void SetMetaMemberData(MetaMemberData fmmd )
    //    {

    //    }
    //    public FileMetaMemberData GetFileMetaMemberDataByName( string name )
    //    {
    //        FileMetaMemberData fmmd = m_FileMetaMemberData.Find(a => a.name == name);

    //        return fmmd;
    //    }
    //    public override string ToFormatString()
    //    {
    //        StringBuilder sb = new StringBuilder();
          
            
    //        if( m_MemberDataType == EMemberDataType.NameClass)
    //        {
    //            for (int i = 0; i < deep; i++)
    //                sb.Append(Global.tabChar);
    //            sb.AppendLine(name);
    //            for (int i = 0; i < deep; i++)
    //                sb.Append(Global.tabChar);
    //            sb.AppendLine("{");
    //            for (int i = 0; i < m_FileMetaMemberData.Count; i++)
    //            {
    //                sb.AppendLine(m_FileMetaMemberData[i].ToFormatString());
    //            }
    //            for (int i = 0; i < deep; i++)
    //                sb.Append(Global.tabChar);
    //            sb.Append("}");
    //        }
    //        else if( m_MemberDataType == EMemberDataType.NoNameClass )
    //        {
    //            sb.AppendLine();
    //            for (int i = 0; i < deep; i++)
    //                sb.Append(Global.tabChar);
    //            sb.AppendLine("{");
    //            for (int i = 0; i < m_FileMetaMemberData.Count; i++)
    //            {
    //                sb.AppendLine(m_FileMetaMemberData[i].ToFormatString());
    //            }
    //            for (int i = 0; i < deep; i++)
    //                sb.Append(Global.tabChar);
    //            sb.Append("}");
    //        }
    //        else if( m_MemberDataType == EMemberDataType.KeyValue )
    //        {
    //            for (int i = 0; i < deep; i++)
    //                sb.Append(Global.tabChar);
    //            sb.Append(name + " = ");
    //            sb.Append(m_FileMetaConstValue.ToFormatString());
    //            sb.Append(";");
    //        }
    //        else if (m_MemberDataType == EMemberDataType.Array)
    //        {
    //            for (int i = 0; i < deep; i++)
    //                sb.Append(Global.tabChar);
    //            sb.Append(name + " = [");
    //            for ( int i = 0; i < m_FileMetaMemberData.Count; i++ )
    //            {
    //                sb.Append(m_FileMetaMemberData[i].ToFormatString());
    //                if( i < m_FileMetaMemberData.Count - 1 )
    //                {
    //                    sb.Append(",");
    //                }
    //            }
    //            sb.Append("]");
    //            sb.Append(";");
    //        }
    //        else if (m_MemberDataType == EMemberDataType.Value )
    //        {
    //            sb.Append(m_FileMetaConstValue.ToFormatString());
    //        }

    //        return sb.ToString();
    //    }
    //}
    public partial class FileMetaMemberVariable : FileMetaBase
    {
        public enum EMemberDataType
        {
            None,
            NameClass,
            //NoNameClass,
            Array,
            KeyValue,
            ConstVariable,
        }

        public FileMetaClassDefine classDefineRef => m_ClassDefineRef;
        public Token permissionToken => m_PermissionToken;
        public Token staticToken => m_StaticToken;
        public Token nameToken => m_Token;
        public FileMetaBaseTerm express =>m_Express;

        private MetaMemberVariable m_MetaMemberVariable = null;

        private FileMetaClassDefine m_ClassDefineRef;
        private Token m_AssignToken = null;
        private Token m_PermissionToken = null;
        private Token m_StaticToken = null;        
        private FileMetaBaseTerm m_Express;
        public List<FileMetaMemberVariable> fileMetaMemberVariable => m_FileMetaMemberVariableList;
        //public FileMetaConstValueTerm fileMetaConstValue => m_FileMetaConstValue;
        public FileMetaCallTerm fileMetaCallTermValue => m_FileMetaCallTermValue;
        public EMemberDataType DataType => m_MemberDataType;

        private List<FileMetaMemberVariable> m_FileMetaMemberVariableList = new List<FileMetaMemberVariable>();
        private EMemberDataType m_MemberDataType = EMemberDataType.None;
        //private FileMetaConstValueTerm m_FileMetaConstValue = null;
        private FileMetaCallTerm m_FileMetaCallTermValue = null;

        private List<Node> m_NodeList = null;
        public FileMetaMemberVariable( FileMeta fm, List<Node> list)
        {
            m_FileMeta = fm;

            m_NodeList = list;

            ParseBuildMetaVariable();
        }
        public FileMetaMemberVariable( FileMeta fm, Node brace )
        {
            //分析 {}中的内容，一般用于解析匿名函数
            m_FileMeta = fm;
            m_Token = brace.token;


            List<Node> list2 = new List<Node>();
            for ( int i = 0; i < brace.childList.Count; i++ )
            {
                Node c = brace.childList[i];
                if( c.nodeType == ENodeType.LineEnd || c.nodeType == ENodeType.SemiColon )
                {
                    if( list2.Count == 0 )
                    {
                        continue;
                    }
                    FileMetaMemberVariable cfm = new FileMetaMemberVariable(m_FileMeta, list2);
                    list2 = new List<Node>();

                    AddFileMemberVariable(cfm);
                    continue;
                }
                list2.Add(c);
            }
        }
        public FileMetaMemberVariable(FileMeta fm, Node _beforeNode, Node _afterNode, EMemberDataType dataType)
        {
            m_MemberDataType = dataType;

            m_FileMeta = fm;
            m_Token = _beforeNode.token;

            bool isParseBuild = true;
            if (dataType == EMemberDataType.NameClass)
            {
            }
            else if (dataType == EMemberDataType.KeyValue)
            {
                m_Express = new FileMetaConstValueTerm(m_FileMeta, _afterNode.token);
            }
            else if (dataType == EMemberDataType.Array)
            {
                isParseBuild = false;

                m_Express = new FileMetaBracketTerm(m_FileMeta, _beforeNode, 1);
            }
            else if (dataType == EMemberDataType.ConstVariable )
            {
                m_Express = new FileMetaConstValueTerm(m_FileMeta, _beforeNode.token);
            }

            if(isParseBuild )
            {
                ParseBuildMetaVariable();
            }
            
        }

        public bool ParseBuildMetaVariable()
        {
            var bedoreNodeList = new List<Node>();
            var afterNodeList = new List<Node>();

            if (!FileMetatUtil.SplitNodeList(m_NodeList, bedoreNodeList, afterNodeList, ref m_AssignToken))
            {
                Console.WriteLine("Error 解析NodeList出现错误~~~");
                return false;
            }
            if (bedoreNodeList.Count < 1)
            {
                Console.WriteLine("Error listDefieNode 不能为空~");
                return false;
            }

            Node beforeNode = new Node( null );
            beforeNode.childList = bedoreNodeList;
            beforeNode.parseIndex = 0;
            StructParse.HandleLinkNode(beforeNode);
            var defineNodeList = beforeNode.childList;


            Node nameNode = null;
            Node typeNode = null;
            if (!GetNameAndTypeToken(defineNodeList, ref typeNode, ref nameNode, ref m_PermissionToken, ref m_StaticToken))
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }

            if (nameNode == null)
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }
            if (nameNode.extendLinkNodeList.Count > 0)
            {
                Console.WriteLine("Error 没有找到该定义名称 必须使用例: X = 10; 的格式");
                return false;
            }
            m_Token = nameNode.token;

            if (typeNode != null)
                m_ClassDefineRef = new FileMetaClassDefine(m_FileMeta, typeNode);

            if(afterNodeList[0].nodeType == ENodeType.Bracket )
            {
                m_MemberDataType = EMemberDataType.Array;
                ParseBracketContrent(afterNodeList[0]);
            }
            else
            {
                m_MemberDataType = EMemberDataType.ConstVariable;
                //m_FileMetaConstValue = new FileMetaConstValueTerm(m_FileMeta, _beforeNode.token);
                m_Express = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, afterNodeList, FileMetaTermExpress.EExpressType.MemberVariable);
            }

            return true;
        }
        public void ParseBracketContrent(Node pnode)
        {
            int type = -1;
            for ( int index = 0; index < pnode.childList.Count; index++ )
            {
                var curNode = pnode.childList[index];
                if( curNode.nodeType == ENodeType.LineEnd 
                    || curNode.nodeType == ENodeType.SemiColon
                    || curNode.nodeType == ENodeType.Comma)
                {
                    continue;
                }
                if( curNode.nodeType == ENodeType.IdentifierLink )      //aaa(){},aaa(){}
                {

                }
                if (curNode.nodeType == ENodeType.Brace)  //Class1 [{},{}]
                {
                    if( type == 2 || type == 3 )
                    {
                        Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                        continue;
                    }

                    type = 1;

                    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, EMemberDataType.Array );

                    AddFileMemberVariable(fmmd);
                }
                else if (curNode.nodeType == ENodeType.ConstValue)   // ["stringValue","Stvlue"]
                {
                    if (type == 1 || type == 3)
                    {
                        Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                        continue;
                    }

                    type = 2;

                    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.ConstVariable );

                    AddFileMemberVariable(fmmd);
                }
                else if (curNode?.nodeType == ENodeType.Bracket) // [[],[]]
                {
                    if (type == 1 || type == 2 )
                    {
                        Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                        continue;
                    }

                    type = 3;

                    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.Array);

                    AddFileMemberVariable(fmmd);
                }
                else
                {
                    Console.WriteLine("Error Data数据中 []中，不支持该类型的数据" + curNode?.token?.ToLexemeAllString());
                    continue;
                }
            }
        }

        public void ParseFileMemberVariableContent()
        {
            //Node curParentNode = braceNode != null ? braceNode : pnode;
            //index++;
            //if (index < curParentNode.childList.Count)
            //{
            //    var next2Node = curParentNode.childList[index];
            //    if (next2Node.nodeType == ENodeType.LineEnd) //只允许有一次回车  name = \n
            //    {
            //        next2Node = curParentNode.childList[++index];
            //    }
            //    // name = []  在定义数组中，可以使用数字也可以使用{}
            //    // name = ["a",b"]
            //    // name = [{a=1/nb=2}, {a=3\nb=4}]
            //    if (next2Node.nodeType == ENodeType.Bracket)
            //    {
            //        index++;

            //        curNode.bracketNode = next2Node;

            //        FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.Array);

            //        AddParseVariableInfo(fmmd);

            //        ParseBracketContrent(curNode);

            //        m_CurrentNodeInfoStack.Pop();
            //    }
            //    //else if( next2Node.nodeType == ENodeType.Par )    // val1 = (10+31);  //普通变量
            //    //{
            //    //    index++;

            //    //    curNode.blockNode = next2Node;

            //    //    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, null, FileMetaMemberVariable.EMemberDataType.Array);

            //    //    AddParseVariableInfo(fmmd);

            //    //    m_CurrentNodeInfoStack.Pop();

            //    //    ParseParContrent(curNode);
            //    //}
            //    //else if (next2Node.nodeType == ENodeType.Symbol)    // val1 = +32-10;
            //    //{
            //    //    var next3Node = curParentNode.childList[++index];

            //    //    if (next2Node.token?.type == ETokenType.Minus)
            //    //    {
            //    //        index++;
            //    //        int val = -(int)(next3Node.token?.lexeme);
            //    //        next3Node.token.SetLexeme(val);
            //    //    }

            //    //    FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, next3Node, FileMetaMemberVariable.EMemberDataType.KeyValue);

            //    //    if (currentNodeInfo.parseType == EParseNodeType.Class)
            //    //    {
            //    //        currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
            //    //    }
            //    //    if (ProjectManager.isUseForceSemiColonInLineEnd)
            //    //    {
            //    //        var next4Node = curParentNode.childList[++index];
            //    //        if (next4Node.nodeType != ENodeType.SemiColon)
            //    //        {
            //    //            Console.WriteLine("Error 应该使用;结束语句!!");
            //    //        }
            //    //        else
            //    //        {
            //    //            index++;
            //    //        }
            //    //    }
            //    //}
            //    else if (next2Node.nodeType == ENodeType.Symbol
            //        || next2Node.nodeType == ENodeType.ConstValue
            //        || next2Node.nodeType == ENodeType.Par)
            //    {
            //        List<Node> expressNode = new List<Node>();
            //        expressNode.Add(curNode);
            //        expressNode.Add(nextNode);
            //        int j = 0;
            //        for (j = index; j < pnode.childList.Count; j++)
            //        {
            //            if (!(pnode.childList[j].nodeType == ENodeType.LineEnd
            //                || pnode.childList[j].nodeType == ENodeType.SemiColon))
            //            {
            //                expressNode.Add(pnode.childList[j]);
            //            }
            //        }
            //        index = j;
            //        FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, expressNode);

            //        if (currentNodeInfo.parseType == EParseNodeType.Class)
            //        {
            //            currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
            //        }
            //        if (ProjectManager.isUseForceSemiColonInLineEnd)
            //        {
            //            var next3Node = curParentNode.childList[++index];
            //            if (next3Node.nodeType != ENodeType.SemiColon)
            //            {
            //                Console.WriteLine("Error 应该使用;结束语句!!");
            //            }
            //            else
            //            {
            //                index++;
            //            }
            //        }
            //    }
            //    else if (next2Node.nodeType == ENodeType.IdentifierLink)        // c2 = Class2(20); //定义类变量
            //    {
            //        index++;
            //        string name = next2Node.token?.lexeme.ToString();
            //        //这块现在暂时还不确定，是否在data里边可以直接生成class或者是引用 data数据
            //        FileMetaMemberVariable fmmd = new FileMetaMemberVariable(m_FileMeta, curNode, next2Node, FileMetaMemberVariable.EMemberDataType.NameClass);

            //        if (currentNodeInfo.parseType == EParseNodeType.Class)
            //        {
            //            currentNodeInfo.codeClass.AddFileMemberVariable(fmmd);
            //        }
            //        if (ProjectManager.isUseForceSemiColonInLineEnd)
            //        {
            //            var next3Node = curParentNode.childList[++index];
            //            if (next3Node.nodeType != ENodeType.SemiColon)
            //            {
            //                Console.WriteLine("Error 应该使用;结束语句!!");
            //            }
            //            else
            //            {
            //                index++;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        Console.WriteLine("Error 不允许=号后边非Const值!!");
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("Error 不允许=号后边没值!!");
            //}
        }
        public bool GetNameAndTypeToken(List<Node> defineNodeList, ref Node typeNode, ref Node nameNode, ref Token permissionToken, ref Token staticToken)
        {
            bool isError = false;

            List<Node> nodeList = new List<Node>();
            for (int i = 0; i < defineNodeList.Count; i++)
            {
                var cnode = defineNodeList[i];

                if (cnode.nodeType == ENodeType.IdentifierLink)
                {
                    nodeList.Add(cnode);
                }
                else
                {
                    var token = cnode.token;
                    if (token.type == ETokenType.Public
                        || token.type == ETokenType.Projected
                        || token.type == ETokenType.Internal
                        || token.type == ETokenType.Private)
                    {
                        if (permissionToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 多重定义名称的权限定义!!");
                        }
                        permissionToken = token;
                    }
                    else if (token.type == ETokenType.Static)
                    {
                        if (staticToken != null)
                        {
                            isError = true;
                            Console.WriteLine("Error 多重定义名称的静态定义!!");
                        }
                        staticToken = token;
                    }
                    else if( token.type == ETokenType.Type )
                    {
                        nodeList.Add(cnode);
                    }
                    else
                    {
                        Console.WriteLine("Error 解析变量中，不允许的类型存在!!");
                    }
                }
            }

            if (nodeList.Count == 1)
            {
                nameNode = nodeList[0];
            }
            else if (nodeList.Count == 2)
            {
                typeNode = nodeList[0];
                nameNode = nodeList[1];
            }

            return !isError;
        }
        public void SetMetaMemberVariable( MetaMemberVariable mmv )
        {
            m_MetaMemberVariable = mmv;
        }
        public void AddFileMemberVariable(FileMetaMemberVariable fmmd)
        {
            m_FileMetaMemberVariableList.Add(fmmd);
        }
        public FileMetaMemberVariable GetFileMetaMemberDataByName(string name)
        {
            FileMetaMemberVariable fmmd = m_FileMetaMemberVariableList.Find(a => a.name == name);

            return fmmd;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;

            for (int i = 0; i < m_FileMetaMemberVariableList.Count; i++)
            {
                m_FileMetaMemberVariableList[i].SetDeep(m_Deep + 1);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            EPermission permis = CompilerUtil.GetPerMissionByString(m_PermissionToken?.lexeme.ToString());
            if( permis == EPermission.Null )
            {
                sb.Append("_public ");
            }
            else
            {
                sb.Append(permis.ToFormatString());
            }
            if(m_StaticToken!= null )
            {
                sb.Append(" " + m_StaticToken.lexeme.ToString());
            }

            if (m_MemberDataType == EMemberDataType.NameClass)
            {
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.AppendLine(name);
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.AppendLine("{");
                for (int i = 0; i < m_FileMetaMemberVariableList.Count; i++)
                {
                    sb.AppendLine(m_FileMetaMemberVariableList[i].ToFormatString());
                }
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append("}");

                if (m_ClassDefineRef != null)
                    sb.Append(" " + m_ClassDefineRef.ToFormatString());
                sb.Append(" " + name);
                if (m_AssignToken != null)
                {
                    sb.Append(" " + m_AssignToken.lexeme.ToString());
                    sb.Append(" " + m_Express?.ToFormatString());
                }
                sb.Append(";");
            }
            else if (m_MemberDataType == EMemberDataType.KeyValue)
            {
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append(name + " = ");
                sb.Append(m_Express.ToFormatString());
                sb.Append(";");
            }
            else if (m_MemberDataType == EMemberDataType.Array)
            {
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append(name + " = [");
                for (int i = 0; i < m_FileMetaMemberVariableList.Count; i++)
                {
                    sb.Append(m_FileMetaMemberVariableList[i].ToFormatString());
                    if (i < m_FileMetaMemberVariableList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
                sb.Append(";");
            }
            else if (m_MemberDataType == EMemberDataType.ConstVariable )
            {
                sb.Append(m_ClassDefineRef?.ToFormatString());
                sb.Append( name + " = ");
                sb.Append(m_Express?.ToFormatString());
            }


            return sb.ToString();
        }
    }
}