//****************************************************************************
//  File:      FileMetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************


using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaMemberVariable : FileMetaBase
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
        public FileMetaBaseTerm express => m_Express;
        public Token assignToken => m_AssignToken;

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
        public FileMetaMemberVariable(FileMeta fm, List<Token> tokens)
        {
            m_FileMeta = fm;

            if (tokens == null || tokens.Count == 0)
            {
                Log.AddInStructFileMeta(EError.None, "Error 成员变量Token列表为空");
                return;
            }

            // 拆分定义和表达式: before = 左边, after = 右边
            int assignIndex = -1;
            int depthPar = 0, depthBrace = 0, depthBracket = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.type == ETokenType.LeftPar) depthPar++;
                else if (t.type == ETokenType.RightPar && depthPar > 0) depthPar--;
                else if (t.type == ETokenType.LeftBrace) depthBrace++;
                else if (t.type == ETokenType.RightBrace && depthBrace > 0) depthBrace--;
                else if (t.type == ETokenType.LeftBracket) depthBracket++;
                else if (t.type == ETokenType.RightBracket && depthBracket > 0) depthBracket--;

                if (depthPar == 0 && depthBrace == 0 && depthBracket == 0 && t.type == ETokenType.Assign)
                {
                    assignIndex = i;
                    break;
                }
            }

            if(assignIndex == -1 )
            {
                Debug.Assert(false, "在成员变量里边，没有定义=号");
            }

            List<Token> defTokens;
            List<Token> exprTokens = null;
            if (assignIndex >= 0)
            {
                defTokens = tokens.GetRange(0, assignIndex);
                m_AssignToken = tokens[assignIndex];
                if (assignIndex + 1 < tokens.Count)
                {
                    exprTokens = tokens.GetRange(assignIndex + 1, tokens.Count - assignIndex - 1);
                }
            }
            else
            {
                defTokens = new List<Token>(tokens);
            }

            // 去掉首尾的空白/换行/分号
            TrimTokenList(defTokens);
            TrimTokenList(exprTokens);

            if (defTokens.Count == 0)
            {
                Log.AddInStructFileMeta(EError.None, "Error 成员变量定义部分为空");
                return;
            }

            // 解析权限/静态/mut 和 类型+名称
            List<Token> idOrTypeTokens = new List<Token>();
            Token mutToken = null;
            for (int i = 0; i < defTokens.Count; i++)
            {
                var t = defTokens[i];
                if (t.type == ETokenType.Public
                    || t.type == ETokenType.Private
                    || t.type == ETokenType.Projected
                    || t.type == ETokenType.Extern)
                {
                    if (m_PermissionToken != null && m_PermissionToken != t)
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 多重定义名称的权限定义!!");
                    }
                    m_PermissionToken = t;
                }
                else if (t.type == ETokenType.Static)
                {
                    if (m_StaticToken != null && m_StaticToken != t)
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 多重定义名称的静态定义!!");
                    }
                    m_StaticToken = t;
                }
                else if (t.type == ETokenType.Mut)
                {
                    if (mutToken != null && mutToken != t)
                    {
                        Log.AddInStructFileMeta(EError.None, "Error 多重定义名称的Mut定义!!");
                    }
                    mutToken = t;
                }
                else if (t.type == ETokenType.Identifier || t.type == ETokenType.Type)
                {
                    idOrTypeTokens.Add(t);
                }
                else
                {
                    // 其他 token 暂时忽略/记录
                    Log.AddInStructFileMeta(EError.None, "Error 解析变量中，不允许的类型存在!!" + t.ToLexemeAllString());
                }
            }

            if (idOrTypeTokens.Count == 0)
            {
                Log.AddInStructFileMeta(EError.None, "Error 没有找到该定义名称");
                return;
            }

            // 最后一个视为名称，其余视为类型
            Token nameTok = idOrTypeTokens[idOrTypeTokens.Count - 1];
            m_Token = nameTok;

            if (idOrTypeTokens.Count > 1)
            {
                // 使用纯 Token 列表构造类型定义：前面的 token 组成类型（可能包含命名空间前缀）
                var typeTokens = idOrTypeTokens.GetRange(0, idOrTypeTokens.Count - 1);
                m_ClassDefineRef = new FileMetaClassDefine(m_FileMeta, typeTokens);
                m_MemberDataType = EMemberDataType.ConstVariable;
            }

            // 解析右侧表达式: 当前仅支持简单常量/表达式，复用 FileMetatUtil.CreateFileMetaExpress
            if (exprTokens != null && exprTokens.Count > 0)
            {
                m_MemberDataType = EMemberDataType.ConstVariable;

                // 直接使用Token版表达式构造，不再人为包装Node
                m_Express = FileMetatUtil.CreateFileMetaExpressFromTokens(
                    m_FileMeta,
                    exprTokens,
                    FileMetaTermExpress.EExpressType.MemberVariable);
            }
        }

        private static void TrimTokenList(List<Token> list)
        {
            if (list == null || list.Count == 0) return;
            int start = 0;
            int end = list.Count - 1;
            while (start <= end && (list[start].type == ETokenType.Space || list[start].type == ETokenType.LineEnd || list[start].type == ETokenType.SemiColon))
                start++;
            while (end >= start && (list[end].type == ETokenType.Space || list[end].type == ETokenType.LineEnd || list[end].type == ETokenType.SemiColon))
                end--;
            if (start == 0 && end == list.Count - 1) return;
            if (end < start)
            {
                list.Clear();
                return;
            }
            var trimmed = list.GetRange(start, end - start + 1);
            list.Clear();
            list.AddRange(trimmed);
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
            //    //            Debug.WriteLine("Error 应该使用;结束语句!!");
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
            //                Debug.WriteLine("Error 应该使用;结束语句!!");
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
            //                Debug.WriteLine("Error 应该使用;结束语句!!");
            //            }
            //            else
            //            {
            //                index++;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        Debug.WriteLine("Error 不允许=号后边非Const值!!");
            //    }
            //}
            //else
            //{
            //    Debug.WriteLine("Error 不允许=号后边没值!!");
            //}
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
        public override string ToString()
        {
            return m_Token.lexeme.ToString();
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
                sb.Append( " " + name + " = ");
                sb.Append(m_Express?.ToFormatString());
            }
            else
            {
                sb.Append("没有差别MemberDataType");
            }
            return sb.ToString();
        }
    }
}