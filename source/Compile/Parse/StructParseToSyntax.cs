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
using System.Collections;
using System.Xml.Linq;
using System.Reflection;
using SimpleLanguage.Core.Statements;

namespace SimpleLanguage.Compile.Parse
{
    public partial class StructParse
    {
        public enum ESyntaxNodeStructType
        {
            None = 0,
            KeySyntax,
            CommonSyntax,
        }
        public class SyntaxNodeStruct
        {
            public ESyntaxNodeStructType eSyntaxNodeType = ESyntaxNodeStructType.None;

            public ETokenType tokenType = ETokenType.None;
            public ENodeType curNodeType = ENodeType.Null;
            public int moveIndex = 0;
            public Node keyNode { get; private set; } = null;
            public List<Node> keyContent = new List<Node>();                //关键字后跟条件语句  if () switch() for()
            public Node blockNode = null;

            public List<Node> commonContent = new List<Node>();             //普通语句区间    Class.CalFun()

            public List<SyntaxNodeStruct> childrenKeySyntaxStructList = new List<SyntaxNodeStruct>();//关键字内嵌子语句, 像switch
            public List<SyntaxNodeStruct> followKeySyntaxStructList = new List<SyntaxNodeStruct>();//关键字跟随语句if/elif/elif/else  

            public SyntaxNodeStruct()
            {
            }
            public void SetMainKeyNode( Node _keyNode )
            {
                keyNode = _keyNode;
                tokenType = _keyNode.token.type;
                eSyntaxNodeType = ESyntaxNodeStructType.KeySyntax;
            }
            public void AddContent( Node node )
            {
                if (tokenType == ETokenType.If
                    || tokenType == ETokenType.ElseIf
                    || tokenType == ETokenType.Switch
                    || tokenType == ETokenType.For
                    || tokenType == ETokenType.While
                    || tokenType == ETokenType.DoWhile
                    || tokenType == ETokenType.Return
                    || tokenType == ETokenType.Transience
                    || tokenType == ETokenType.Case)
                {
                    keyContent.Add(node);
                }
                else if( tokenType == ETokenType.Else
                    || tokenType == ETokenType.Next
                    || tokenType == ETokenType.Break
                    || tokenType == ETokenType.Continue )
                {
                    Console.WriteLine("Error 不允许在Else后增加任何代码" + node.token?.ToLexemeAllString() );
                }
                else
                {
                    commonContent.Add(node);
                    eSyntaxNodeType = ESyntaxNodeStructType.CommonSyntax;
                }
            }
            public void SetBraceNode( Node node )
            {
                blockNode = node;
                if (tokenType == ETokenType.If
                    || tokenType == ETokenType.ElseIf
                    || tokenType == ETokenType.Else
                    || tokenType == ETokenType.Switch
                    || tokenType == ETokenType.For
                    || tokenType == ETokenType.While || tokenType == ETokenType.DoWhile
                    || tokenType == ETokenType.Case
                    || tokenType == ETokenType.Default
                    || tokenType == ETokenType.Return
                    || tokenType == ETokenType.Transience
                    || tokenType == ETokenType.Label
                    || tokenType == ETokenType.Goto)
                {
                }
                else
                {
                    Console.WriteLine("Error 结束{}错误，关键字不允许或者是其它错误" + node?.token?.ToLexemeAllString());
                }
            }
            public bool IsLineEndBreak()
            {
                if(tokenType == ETokenType.If
                    || tokenType == ETokenType.ElseIf
                    || tokenType == ETokenType.Else
                    || tokenType == ETokenType.Switch
                    || tokenType == ETokenType.For
                    || tokenType == ETokenType.While
                    || tokenType == ETokenType.DoWhile
                    || tokenType == ETokenType.Label )
                {
                    if( blockNode == null )
                    {
                        return false;
                    }
                    return !ProjectManager.isUseForceSemiColonInLineEnd;
                }
                return true;
            }
        }
        public class Condition
        {
            public bool isFirstKey = false;
            public bool isCheck = true;
            public List<ETokenType> eTokenTypeList = new List<ETokenType>();

            public bool IsMatchTokenType( ETokenType tokenType )
            {
                return eTokenTypeList.Contains( tokenType );
            }
            public Condition( ETokenType tokenType )
            {
                eTokenTypeList.Add( tokenType );
                if( tokenType == ETokenType.Else || tokenType == ETokenType.ElseIf )
                {
                    isFirstKey = true;
                }
                isCheck = true;
            }
            public void AddTokenTypeList(ETokenType tokenType )
            {
                eTokenTypeList.Add(tokenType);
            }
        }
        public SyntaxNodeStruct GetOneSyntax( Node pnode, Condition condition = null )
        {
            SyntaxNodeStruct keynodeStruct = new SyntaxNodeStruct();
            if (pnode.parseIndex >= pnode.childList.Count)
            {
                return keynodeStruct;
            }
            int index = 0;
            int tCurIndex = 0;
            Node curNode = null;
            Token curToken = null;
            ENodeType curNodeType = ENodeType.Null;
            while (pnode.parseIndex < pnode.childList.Count)
            {
                tCurIndex = pnode.parseIndex + index++;
                curNode = null;
                if( tCurIndex < pnode.childList.Count )
                {
                    curNode = pnode.childList[tCurIndex];
                }
                if (curNode == null)
                {
                    break;
                }
                curNodeType = curNode.nodeType;
                curToken = curNode.token;
                if (curToken == null)
                {
                    break;
                }


                if (curNodeType == ENodeType.LineEnd)
                {
                    if (keynodeStruct.IsLineEndBreak())
                    {
                        if (ProjectManager.isUseForceSemiColonInLineEnd)
                        {
                            Console.WriteLine("warning 使用的是强制封号结束语句方式，注意这个节点会继承往下查找语句"
                                + curToken?.ToLexemeAllString());
                        }
                        break;
                    }
                }
                else if (curNodeType == ENodeType.SemiColon)
                {
                    break;
                }
                else if (curNodeType == ENodeType.Brace)
                {
                    Node nextNode = null;
                    bool isMustContactBrace = false;
                    ETokenType ttt = keynodeStruct.tokenType;
                    if (ttt == ETokenType.If
                                || ttt == ETokenType.ElseIf
                                || ttt == ETokenType.Else
                                || ttt == ETokenType.For
                                || ttt == ETokenType.While
                                || ttt == ETokenType.DoWhile
                                || ttt == ETokenType.Switch
                                || ttt == ETokenType.Case
                                || ttt == ETokenType.Label)
                    {

                        isMustContactBrace = true;
                    }

                    if (isMustContactBrace)
                    {
                        keynodeStruct.SetBraceNode(curNode);
                    }
                    break;
                }
                else if (curNodeType == ENodeType.Key)
                {
                    if (condition != null && condition.isCheck)
                    {
                        if (!condition.isFirstKey)
                        {
                            index = 0;
                            break;
                        }
                        if (!condition.IsMatchTokenType(curToken.type))
                        {
                            index = 0;
                            break;
                        }
                        condition.isCheck = false;
                    }

                    ETokenType ttt = curNode.token.type;
                    if (ttt == ETokenType.If
                        || ttt == ETokenType.ElseIf
                        || ttt == ETokenType.Else
                        || ttt == ETokenType.Switch
                        || ttt == ETokenType.For
                        || ttt == ETokenType.While || curNode.token?.type == ETokenType.DoWhile
                        || ttt == ETokenType.Case
                        || ttt == ETokenType.Default
                        || ttt == ETokenType.Return
                        || ttt == ETokenType.Transience
                        || ttt == ETokenType.Next
                        || ttt == ETokenType.Break
                        || ttt == ETokenType.Continue
                        || ttt == ETokenType.Label
                        || ttt == ETokenType.Goto
                        || ttt == ETokenType.Const )
                    {
                        keynodeStruct.SetMainKeyNode(curNode);
                    }
                    else if(  ttt == ETokenType.Data
                        || ttt == ETokenType.Class
                        || ttt == ETokenType.Interface
                        || ttt == ETokenType.Extends
                        || ttt == ETokenType.Public
                        || ttt == ETokenType.Private
                        || ttt == ETokenType.Projected
                        || ttt == ETokenType.Internal )
                    {
                        keynodeStruct.SetMainKeyNode(curNode);
                    }
                    else if( ttt == ETokenType.In )
                    {
                        keynodeStruct.AddContent(curNode);
                    }
                    else
                    {
                        Console.WriteLine("Error 解析异常关键字" + curNode.token.ToLexemeAllString());
                    }
                }
                else
                {
                    if (condition != null && condition.isCheck)
                    {
                        if (condition.isFirstKey)
                        {
                            index = 0;
                            break;
                        }
                    }
                    keynodeStruct.AddContent(curNode);
                }
            }
            keynodeStruct.moveIndex = index;
            return keynodeStruct;
        }

        private FileMetaSyntax CrateFileMetaSyntaxNoKey(List<Node> pNodeList)
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
                        || tet == ETokenType.DoubleMinus)
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
            else if (beforeNodeList.Count > 5)
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
            HandleLinkNode(parseNode);
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
                        Console.WriteLine("Error 解析发现没有该节点!!" + token?.ToLexemeAllString());
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
                //var fme22 = HandleCreateFileMetaSyntaxByPNode(afterNodeList);
                //if ((afterNodeList[0].token.type == ETokenType.If
                //    || afterNodeList[0].token.type == ETokenType.Switch)
                //    )
                //{
                //    if (fme22 is FileMetaKeyIfSyntax)
                //    {
                //        fme = new FileMetaIfSyntaxTerm(m_FileMeta, fme22 as FileMetaKeyIfSyntax);

                //    }
                //    else if (fme22 is FileMetaKeySwitchSyntax)
                //    {
                //        fme = new FileMetaSwitchSyntaxTerm(m_FileMeta, fme22 as FileMetaKeySwitchSyntax);
                //    }
                //    else
                //    {
                //        Console.WriteLine("Error 生成if/switch语句失败!!");
                //    }
                //}
                //else
                //{
                //    Console.WriteLine("Error 不允许嵌套除if/switch以外的语句!!");
                //}
                Console.WriteLine("Error 暂不支持 a = if/switch{}语法");
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
                        nameToken, assignNode.token, staticToken, fme);
                    return fmdvs;
                }
                if (varRef != null)
                {
                    FileMetaOpAssignSyntax fms = new FileMetaOpAssignSyntax(varRef, assignNode.token, fme, true);
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
                    FileMetaDefineVariableSyntax fmdvs = new FileMetaDefineVariableSyntax(m_FileMeta, classRef, nameToken, staticToken, null, null);
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
        public FileMetaSyntax HandleCreateFileMetaSyntaxByPNode( Node pnode )
        {
            FileMetaSyntax fms = null;
            SyntaxNodeStruct akss = GetOneSyntax(pnode);
            pnode.parseIndex += akss.moveIndex;
            if( akss.eSyntaxNodeType == ESyntaxNodeStructType.None )
            {
            }
            else if( akss.eSyntaxNodeType == ESyntaxNodeStructType.CommonSyntax )
            {
                if( akss.commonContent.Count > 0 )
                {
                    fms = CrateFileMetaSyntaxNoKey(akss.commonContent);
                    AddParseSyntaxNodeInfo(fms);
                }
            }
            else if( akss.eSyntaxNodeType == ESyntaxNodeStructType.KeySyntax )
            {
                if (akss.tokenType == ETokenType.If)
                {
                    while (true)
                    {
                        Condition condition = new Condition(ETokenType.ElseIf);
                        condition.AddTokenTypeList(ETokenType.Else);
                        SyntaxNodeStruct cakss = GetOneSyntax(pnode, condition);
                        if (cakss.eSyntaxNodeType == ESyntaxNodeStructType.None )
                        {
                            if (cakss.moveIndex == 0)
                                break;
                            pnode.parseIndex += cakss.moveIndex;
                            continue;
                        }

                        if (cakss.tokenType == ETokenType.ElseIf)
                        {
                            pnode.parseIndex += cakss.moveIndex;
                            akss.followKeySyntaxStructList.Add(cakss);
                            continue;
                        }
                        else if (cakss.tokenType == ETokenType.Else)
                        {
                            pnode.parseIndex += cakss.moveIndex;
                            akss.followKeySyntaxStructList.Add(cakss);
                        }
                        break;
                    }
                    FileMetaKeyIfSyntax fmkis = FileMetaKeyIfSyntax.ParseIfSyntax(m_FileMeta, akss);
                    fms = fmkis;
                    AddParseSyntaxNodeInfo(fmkis);

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis.ifExpressSyntax.executeBlockSyntax);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();


                    for (int i = 0; i < akss.followKeySyntaxStructList.Count; i++)
                    {
                        FileMetaBlockSyntax fmbs = null;
                        if (i < fmkis.elseIfExpressSyntax.Count)
                        {
                            fmbs = fmkis.elseIfExpressSyntax[i].executeBlockSyntax;
                        }
                        else
                        {
                            fmbs = fmkis.elseExpressSyntax?.executeBlockSyntax;
                        }

                        ParseCurrentNodeInfo pcnic2 = new ParseCurrentNodeInfo(fmbs);
                        m_CurrentNodeInfoStack.Push(pcnic2);
                        ParseSyntax(akss.followKeySyntaxStructList[i].blockNode);
                        m_CurrentNodeInfoStack.Pop();
                    }
                }
                else if (akss.tokenType == ETokenType.Switch)
                {
                    while (true)
                    {

                        Condition condition = new Condition(ETokenType.Case);
                        condition.AddTokenTypeList(ETokenType.Default);
                        SyntaxNodeStruct cakss = GetOneSyntax(akss.blockNode, condition);
                        if (cakss == null)
                            break;
                        akss.childrenKeySyntaxStructList.Add(cakss);
                    }

                    FileMetaKeySwitchSyntax fmkis = ParseSwitchSyntax(m_FileMeta, akss);
                    fms = fmkis;
                    AddParseSyntaxNodeInfo(fmkis);

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.keyNode.blockNode);
                    for (int i = 0; i < akss.childrenKeySyntaxStructList.Count; i++)
                    {
                        FileMetaBlockSyntax fmbs = null;
                        if (i >= fmkis.fileMetaKeyCaseSyntaxList.Count)
                        {
                            fmbs = fmkis.fileMetaKeyCaseSyntaxList[i].executeBlockSyntax;
                        }
                        else
                        {
                            fmbs = fmkis.defaultExecuteBlockSyntax;
                        }
                        ParseCurrentNodeInfo pcnic2 = new ParseCurrentNodeInfo(fmbs);
                        m_CurrentNodeInfoStack.Push(pcnic2);
                        ParseSyntax(akss.followKeySyntaxStructList[i].blockNode);
                        m_CurrentNodeInfoStack.Pop();
                    }
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.For)
                {
                    FileMetaKeyForSyntax fmkis = ParseForSyntax(m_FileMeta, akss);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis.executeBlockSyntax);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.While
                    || akss.tokenType == ETokenType.DoWhile)
                {
                    FileMetaConditionExpressSyntax fmkis = ParseConditionSyntax(m_FileMeta, akss);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis.executeBlockSyntax);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.blockNode);
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.Return
                    || akss.tokenType == ETokenType.Transience)
                {
                    FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(m_FileMeta, akss.keyContent, FileMetaTermExpress.EExpressType.Common);

                    FileMetaKeyReturnSyntax fmkis = new FileMetaKeyReturnSyntax(m_FileMeta, akss.keyNode.token, conditionExpress);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fmkis);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    if( akss.keyNode.blockNode != null )
                    {
                        ParseSyntax(akss.keyNode.blockNode);
                    }
                    m_CurrentNodeInfoStack.Pop();
                }
                else if (akss.tokenType == ETokenType.Label
                    || akss.tokenType == ETokenType.Goto)
                {
                    Token labelToken = null;
                    if (akss.keyContent.Count != 1)
                    {
                        Console.WriteLine("Error 解析Goto Label语法，只支持 goto id;的语法!!");
                    }
                    else
                    {
                        labelToken = akss.keyContent[0].token;
                        if (labelToken.type != ETokenType.Identifier)
                        {
                            Console.WriteLine("Error 解析GotoLabel中 后边必须使用普通字符");
                        }
                    }

                    FileMetaKeyGotoLabelSyntax fmkis = new FileMetaKeyGotoLabelSyntax(m_FileMeta, akss.keyNode.token, labelToken);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                    ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(fms);
                    m_CurrentNodeInfoStack.Push(pcnic);
                    ParseSyntax(akss.keyNode.blockNode);
                    m_CurrentNodeInfoStack.Pop();
                }
                else if(akss.tokenType == ETokenType.Break || akss.tokenType == ETokenType.Continue )
                {
                    FileMetaKeyOnlySyntax fmkis = new FileMetaKeyOnlySyntax(m_FileMeta, akss.keyNode.token, null);
                    AddParseSyntaxNodeInfo(fmkis);
                    fms = fmkis;

                }
            }
            return fms;
        }

        public FileMetaKeyForSyntax ParseForSyntax(FileMeta fm, SyntaxNodeStruct sns)
        {
            var cnode = sns.keyNode;

            FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(fm, sns.blockNode.token, sns.blockNode.endToken);
            var fms = new FileMetaKeyForSyntax(fm, cnode.token, executeBlock);

            var parlist = sns.keyContent;
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
                else if (parlist[i].token?.type == ETokenType.In)
                {
                    inToken = parlist[i].token;
                    continue;
                }

                if (syntax == 0)
                {
                    if (inToken != null)
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
                defineVariableSyntax.isAppendSemiColon = false;
                fms.SetFileMetaClassDefine(defineVariableSyntax);
            }
            if (defineVariableSyntax == null)
            {
                Console.WriteLine("Error 解析for 第一部分错误，解析语句出错，不是定义类型语句!!");
            }
            if (inToken != null)
            {
                var cfe = FileMetatUtil.CreateFileMetaExpress(fm, conditionExpressNodeList, FileMetaTermExpress.EExpressType.Common);
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
                    var cfe = FileMetatUtil.CreateFileMetaExpress(fm, conditionExpressNodeList, FileMetaTermExpress.EExpressType.Common);
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
                    FileMetaSyntax fms2 = CrateFileMetaSyntaxNoKey(stepExecuteSyntaxNodeList);
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

        public FileMetaKeySwitchSyntax ParseSwitchSyntax(FileMeta fm, SyntaxNodeStruct sns)
        {
            var cnode = sns.keyNode;
            FileMetaCallLink fmcl = null;
            if (cnode.parNode != null && cnode.parNode.childList?.Count > 0)
            {
                fmcl = new FileMetaCallLink(fm, cnode.parNode.childList[0]);
            }
            var fms = new FileMetaKeySwitchSyntax(fm, cnode.token, cnode.blockNode.token, cnode.blockNode.endToken, fmcl);

            for (int i = 0; i < sns.childrenKeySyntaxStructList.Count; i++)
            {
                var caseMS = sns.childrenKeySyntaxStructList[i];
                if (caseMS.tokenType == ETokenType.Case)
                {
                    var fcase = new FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax(fm, caseMS.keyNode.token);
                    fms.AddFileMetaKeyCaseSyntaxList(fcase);
                }
                else if (caseMS.tokenType == ETokenType.Default)
                {
                    var fdefault = new FileMetaBlockSyntax(fm, caseMS.keyNode.token, caseMS.keyNode.endToken);
                    fms.SetDefaultExecuteBlockSyntax(fdefault);
                }
                else
                {
                    Console.WriteLine("Error switch中不能出现除case/default子外的语句!!");
                }
            }

            return fms;
        }

        public FileMetaConditionExpressSyntax ParseConditionSyntax(FileMeta fm, SyntaxNodeStruct sns)
        {
            var cnode = sns.keyNode;
            FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, sns.keyContent, FileMetaTermExpress.EExpressType.Common);
            FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(fm, sns.blockNode.token, sns.blockNode.endToken);
            var fms = new FileMetaConditionExpressSyntax(fm, cnode.token, conditionExpress, executeBlock);

            return fms;
        }
    }
}