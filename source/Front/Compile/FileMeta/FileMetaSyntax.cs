//****************************************************************************
//  File:      FileMetaSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using System;
using System.Collections.Generic;
using System.Text;

using SimpleLanguage.Logging;

namespace SimpleLanguage.Compile
{    
    public class FileMetaSyntax : FileMetaBase
    {
        public int parseIndex { get; private set; } = 0;
        public bool isAppendSemiColon { get; set; } = true;
        public List<FileMetaSyntax> fileMetaSyntax => m_FileMetaSyntax;

        public bool IsNotEnd()
        {
            return parseIndex < m_FileMetaSyntax.Count;
        }
        public FileMetaSyntax GetCurrentSyntaxAndMove( int moveIndex = 1 )
        {
            if (parseIndex < m_FileMetaSyntax.Count )
            {
                FileMetaSyntax fms =  m_FileMetaSyntax[parseIndex];
                parseIndex += moveIndex;
                return fms;
            }
            return null;
        }

        protected List<FileMetaSyntax> m_FileMetaSyntax = new List<FileMetaSyntax>();
        protected FileMetaSyntax()
        {

        }
        private List<Node> m_NodeList = new List<Node>();
        public void AddFileMetaSyntax( FileMetaSyntax fms )
        {
            m_FileMetaSyntax.Add(fms);
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
    public class FileMetaConditionExpressSyntax : FileMetaSyntax
    {
        // if condition{}  elif condition{}  while condition{}  dowhile condition{}

        public FileMetaBaseTerm conditionExpress => m_ConditionExpress;
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;



        private FileMetaBaseTerm m_ConditionExpress = null;
        private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;

        public FileMetaConditionExpressSyntax( FileMeta fm, Token _ifToken, FileMetaBaseTerm _condition, FileMetaBlockSyntax _executeBlockSyntax )
        {
            m_FileMeta = fm;
            m_Token = _ifToken;

            if( _condition is FileMetaParTerm fmpt )
            {
                if( fmpt.fileMetaExpressList.Count == 1 )
                {
                    m_ConditionExpress = fmpt.fileMetaExpressList[0];
                }
                else
                {
                    //Debug.Assert(false, "");
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "FileMetaConditionExpressSyntax init");
                }
            }
            else
            {
                m_ConditionExpress = _condition;
            }
            m_ExecuteBlockSyntax = _executeBlockSyntax;
        }

        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_ExecuteBlockSyntax?.SetDeep(_deep);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if (m_Token != null)
            {
                sb.Append(m_Token.lexeme.ToString() + " ");
            }
            sb.Append(m_ConditionExpress?.ToFormatString());

            sb.Append(Environment.NewLine);
            sb.Append(m_ExecuteBlockSyntax?.ToFormatString());

            return sb.ToString();
        }
    }
    public class FileMetaKeyIfSyntax : FileMetaSyntax
    {
        public FileMetaConditionExpressSyntax ifExpressSyntax => m_IfExpressSyntax;
        public List<FileMetaConditionExpressSyntax> elseIfExpressSyntax => m_ElseIfExpressSyntax;
        public FileMetaKeyOnlySyntax elseExpressSyntax => m_ElseExpressSyntax;


        private FileMetaConditionExpressSyntax m_IfExpressSyntax = null;
        private List<FileMetaConditionExpressSyntax> m_ElseIfExpressSyntax = new List<FileMetaConditionExpressSyntax>();
        private FileMetaKeyOnlySyntax m_ElseExpressSyntax = null;


        public static FileMetaKeyIfSyntax ParseIfSyntax( FileMeta fm, StructParse.SyntaxNodeStruct sns)
        {
            FileMetaKeyIfSyntax ifSyntax = new FileMetaKeyIfSyntax(fm);
            FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, sns.keyContent, FileMetaTermExpress.EExpressType.Common);
            FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(fm, sns.blockNode.token, sns.blockNode.endToken);
            var fms = new FileMetaConditionExpressSyntax(fm, sns.keyNode.token, conditionExpress, executeBlock);

            if (sns.keyNode.token.type == ETokenType.If)
            {
                if (ifSyntax.ifExpressSyntax != null)
                {
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 不能有多个if语句!!");
                }
                ifSyntax.SetFileMetaConditionExpressSyntax(fms);
                ifSyntax.SetToken(sns.keyNode.token);
            }

            for (int i = 0; i < sns.followKeySyntaxStructList.Count; i++)
            {
                var csns = sns.followKeySyntaxStructList[i];
                var cnode = csns.keyNode;
                Token token = cnode.token;
                if (token.type == ETokenType.ElseIf)
                {
                    FileMetaBaseTerm child_conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, csns.keyContent, FileMetaTermExpress.EExpressType.Common);
                    FileMetaBlockSyntax child_executeBlock = new FileMetaBlockSyntax(fm, csns.blockNode.token, csns.blockNode.endToken);
                    var child_fms = new FileMetaConditionExpressSyntax(fm, token, child_conditionExpress, child_executeBlock);

                    ifSyntax.AddElseIfExpressSyntax(child_fms);
                    child_fms.SetToken(token);
                }
                else if (token.type == ETokenType.Else)
                {
                    FileMetaBlockSyntax executeBlock2 = new FileMetaBlockSyntax(fm, csns.blockNode.token, csns.blockNode.endToken);
                    var fms3 = new FileMetaKeyOnlySyntax(fm, token, executeBlock2);
                    fms3.SetToken(token);

                    ifSyntax.SetElseExpressSyntax(fms3);
                }
            }
            return ifSyntax;
        }

        public FileMetaKeyIfSyntax(FileMeta fm )
        {
            m_FileMeta = fm;
        }
        public void SetFileMetaConditionExpressSyntax(FileMetaConditionExpressSyntax fmces)
        {
            m_IfExpressSyntax = fmces;
        }
        public void SetElseExpressSyntax(FileMetaKeyOnlySyntax fmces)
        {
            m_ElseExpressSyntax = fmces;
        }
        public void AddElseIfExpressSyntax(FileMetaConditionExpressSyntax fmces )
        {
            m_ElseIfExpressSyntax.Add( fmces );
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_IfExpressSyntax?.SetDeep(m_Deep);
            for (int i = 0; i < m_ElseIfExpressSyntax.Count; i++)
            {
                m_ElseIfExpressSyntax[i].SetDeep(m_Deep);
            }
            m_ElseExpressSyntax?.SetDeep(m_Deep);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if(m_IfExpressSyntax != null )
            {
                sb.Append(m_IfExpressSyntax.ToFormatString());
            }
            for( int i = 0; i < m_ElseIfExpressSyntax.Count; i++ )
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ElseIfExpressSyntax[i].ToFormatString());
            }
            if(m_ElseExpressSyntax != null )
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ElseExpressSyntax.ToFormatString());
            }

            return sb.ToString();
        }
    }
    public class FileMetaKeySwitchSyntax : FileMetaSyntax
    {
        public class FileMetaKeyCaseSyntax
        {
            public FileMeta fileMeta => m_FileMeta;
            public Token variableToken => m_VariableToken;
            public FileMetaCallLink defineClassCallLink => m_DefineClassToken;
            public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;
            public List<FileMetaConstValueTerm> constValueTokenList => m_ConstValueTokenList;

            private FileMeta m_FileMeta = null;
            private Token m_Token = null;
            private FileMetaCallLink m_DefineClassToken = null;
            private Token m_VariableToken = null;

            private List<FileMetaConstValueTerm> m_ConstValueTokenList = new List<FileMetaConstValueTerm>();
            private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;
            public int deep { get; set; } = 0;

            public void aaaa()
            {
                /*
                var fmkcs = new FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax(m_FileMeta, castnode.token);

                var parlist = castnode.parNode.childList;
                if (parlist.Count == 0)
                {
                    Debug.Write("Error Case语句不允许没有检查值!!");
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
                            Debug.Write("Error 逗号分割只允许number,string");
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
                        Debug.Write("Error 使用逗号切割开后，类型不相同!!");
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
                            fmkcs.SetDefineClassNode(parlist[0]);
                            fmkcs.SetVariableToken(parlist[1].token);
                        }
                    }
                    else if (parlist.Count == 1)
                    {
                        var ttype = parlist[0].token?.type;
                        if (ttype == ETokenType.Type
                            || ttype == ETokenType.Identifier)
                        {
                            fmkcs.SetDefineClassNode(parlist[0]);
                        }
                        else if (ttype == ETokenType.Number
                            || ttype == ETokenType.String)
                        {
                            fmkcs.AddConstValueTokenList(new FileMetaConstValueTerm(m_FileMeta, parlist[0].token));
                        }
                    }
                }
                FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, castnode.blockNode.token, castnode.blockNode.endToken);
                fmkcs.SetExecuteBlockSyntax(executeBlock);
                ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(executeBlock);
                m_CurrentNodeInfoStack.Push(pcnic);
                ParseSyntax(castnode.blockNode);
                m_CurrentNodeInfoStack.Pop();

                if (j != castlist.Count - 1)
                    fmkcs.isContinueNextCastSyntax = true;

                fms.AddFileMetaKeyCaseSyntaxList(fmkcs);
                */
            }
            public FileMetaKeyCaseSyntax(FileMeta fm, Token castToken)
            {
                m_FileMeta = fm;
                m_Token = castToken;
            }
            public void SetDefineClassNode(Node _defineClassNode)
            {
                m_DefineClassToken = new FileMetaCallLink(m_FileMeta, _defineClassNode);
            }
            public void SetVariableToken(Token _variableToken)
            {
                m_VariableToken = _variableToken;
            }
            public void SetExecuteBlockSyntax(FileMetaBlockSyntax ebs)
            {
                m_ExecuteBlockSyntax = ebs;
            }
            public void AddConstValueTokenList(FileMetaConstValueTerm fmcvt)
            {
                m_ConstValueTokenList.Add(fmcvt);
            }
            public void SetDeep(int _deep)
            {
                deep = _deep;
                m_ExecuteBlockSyntax.SetDeep(_deep);
            }
            public string ToFormatString()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append("case ");
                if (m_DefineClassToken != null)
                {
                    if (defineClassCallLink != null)
                    {
                        sb.Append(defineClassCallLink.ToFormatString() + " ");
                    }
                    if (m_VariableToken != null)
                    {
                        sb.Append(m_VariableToken.lexeme?.ToString() + " ");
                    }
                }
                else
                {
                    for (int i = 0; i < m_ConstValueTokenList.Count; i++)
                    {
                        sb.Append(m_ConstValueTokenList[i].ToFormatString());
                        if (i < m_ConstValueTokenList.Count - 1)
                            sb.Append(",");
                    }
                }

                if (m_ExecuteBlockSyntax != null)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(m_ExecuteBlockSyntax.ToFormatString());
                }
                //if (isContinueNextCastSyntax)
                //{
                //    sb.Append("next;");
                //}
                return sb.ToString();
            }
        }

        public FileMetaCallLink fileMetaVariableRef => m_FileMetaVariableRef;
        /// <summary>表达式源（switch( x + y ) 形式），与 fileMetaVariableRef 二选一。</summary>
        public FileMetaBaseTerm sourceExpress => m_SourceExpress;
        public FileMetaBlockSyntax defaultExecuteBlockSyntax => m_DefaultExecuteBlockSyntax;
        public List<FileMetaKeyCaseSyntax> fileMetaKeyCaseSyntaxList => m_FileMetaKeyCaseSyntaxList;
        public FileMetaBlockSyntax executeBlockSyntax => m_DefaultExecuteBlockSyntax;

        private Token m_LeftBraceToken = null;
        private Token m_RightBraceToken = null;
        private FileMetaCallLink m_FileMetaVariableRef = null;
        private FileMetaBaseTerm m_SourceExpress = null;
        private FileMetaBlockSyntax m_DefaultExecuteBlockSyntax = null;
        private List<FileMetaKeyCaseSyntax> m_FileMetaKeyCaseSyntaxList = new List<FileMetaKeyCaseSyntax>();



        public FileMetaKeySwitchSyntax(FileMeta fm, Token _switchToken, Token _leftBraceToken,
            Token _rightBraceToken, FileMetaCallLink cl)
        {
            m_FileMeta = fm;
            m_Token = _switchToken;
            m_LeftBraceToken = _leftBraceToken;
            m_RightBraceToken = _rightBraceToken;
            m_FileMetaVariableRef = cl;
        }
        public void SetSourceExpress(FileMetaBaseTerm express)
        {
            m_SourceExpress = express;
        }
        public void AddFileMetaKeyCaseSyntaxList(FileMetaKeyCaseSyntax keyCase)
        {
            m_FileMetaKeyCaseSyntaxList.Add(keyCase);
        }
        public void SetDefaultExecuteBlockSyntax(FileMetaBlockSyntax fmbs)
        {
            m_DefaultExecuteBlockSyntax = fmbs;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileMetaKeyCaseSyntaxList.Count; i++)
            {
                m_FileMetaKeyCaseSyntaxList[i].SetDeep(m_Deep + 1);
            }
            m_DefaultExecuteBlockSyntax?.SetDeep(m_Deep + 1);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("switch ");
            if (m_FileMetaVariableRef != null)
            {
                sb.Append(m_FileMetaVariableRef.ToFormatString());
            }
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("{");

            for (int i = 0; i < m_FileMetaKeyCaseSyntaxList.Count; i++)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_FileMetaKeyCaseSyntaxList[i].ToFormatString());
            }
            if (m_DefaultExecuteBlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < deep + 1; i++)
                    sb.Append(Global.tabChar);
                sb.Append("default");
                sb.Append(Environment.NewLine);
                sb.Append(m_DefaultExecuteBlockSyntax.ToFormatString());
            }

            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");

            return sb.ToString();
        }
    }
    public class FileMetaKeyMatchSyntax : FileMetaSyntax
    {
        public class FileMetaKeyCaseSyntax
        {
            public FileMeta fileMeta => m_FileMeta;
            public bool isContinueNextCastSyntax { get; set; } = false;
            public Token variableToken => m_VariableToken;
            public FileMetaCallLink defineClassCallLink => m_DefineClassToken;
            public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;
            public List<FileMetaConstValueTerm> constValueTokenList => m_ConstValueTokenList;

            private FileMeta m_FileMeta = null;
            private Token m_Token = null;
            private FileMetaCallLink m_DefineClassToken = null;
            private Token m_VariableToken = null;

            private List<FileMetaConstValueTerm> m_ConstValueTokenList = new List<FileMetaConstValueTerm>();
            private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;
            public int deep { get; set; } = 0;

            public void aaaa()
            {
                /*
                var fmkcs = new FileMetaKeySwitchSyntax.FileMetaKeyCaseSyntax(m_FileMeta, castnode.token);

                var parlist = castnode.parNode.childList;
                if (parlist.Count == 0)
                {
                    Debug.Write("Error Case语句不允许没有检查值!!");
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
                            Debug.Write("Error 逗号分割只允许number,string");
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
                        Debug.Write("Error 使用逗号切割开后，类型不相同!!");
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
                            fmkcs.SetDefineClassNode(parlist[0]);
                            fmkcs.SetVariableToken(parlist[1].token);
                        }
                    }
                    else if (parlist.Count == 1)
                    {
                        var ttype = parlist[0].token?.type;
                        if (ttype == ETokenType.Type
                            || ttype == ETokenType.Identifier)
                        {
                            fmkcs.SetDefineClassNode(parlist[0]);
                        }
                        else if (ttype == ETokenType.Number
                            || ttype == ETokenType.String)
                        {
                            fmkcs.AddConstValueTokenList(new FileMetaConstValueTerm(m_FileMeta, parlist[0].token));
                        }
                    }
                }
                FileMetaBlockSyntax executeBlock = new FileMetaBlockSyntax(m_FileMeta, castnode.blockNode.token, castnode.blockNode.endToken);
                fmkcs.SetExecuteBlockSyntax(executeBlock);
                ParseCurrentNodeInfo pcnic = new ParseCurrentNodeInfo(executeBlock);
                m_CurrentNodeInfoStack.Push(pcnic);
                ParseSyntax(castnode.blockNode);
                m_CurrentNodeInfoStack.Pop();

                if (j != castlist.Count - 1)
                    fmkcs.isContinueNextCastSyntax = true;

                fms.AddFileMetaKeyCaseSyntaxList(fmkcs);
                */
            }
            public FileMetaKeyCaseSyntax(FileMeta fm, Token castToken)
            {
                m_FileMeta = fm;
                m_Token = castToken;
            }
            public void SetDefineClassNode(Node _defineClassNode)
            {
                m_DefineClassToken = new FileMetaCallLink(m_FileMeta, _defineClassNode);
            }
            public void SetVariableToken(Token _variableToken)
            {
                m_VariableToken = _variableToken;
            }
            public void SetExecuteBlockSyntax(FileMetaBlockSyntax ebs)
            {
                m_ExecuteBlockSyntax = ebs;
            }
            public void AddConstValueTokenList(FileMetaConstValueTerm fmcvt)
            {
                m_ConstValueTokenList.Add(fmcvt);
            }
            public void SetDeep(int _deep)
            {
                deep = _deep;
                m_ExecuteBlockSyntax.SetDeep(_deep);
            }
            public string ToFormatString()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < deep; i++)
                    sb.Append(Global.tabChar);
                sb.Append("case ");
                if (m_DefineClassToken != null)
                {
                    if (defineClassCallLink != null)
                    {
                        sb.Append(defineClassCallLink.ToFormatString() + " ");
                    }
                    if (m_VariableToken != null)
                    {
                        sb.Append(m_VariableToken.lexeme?.ToString() + " ");
                    }
                }
                else
                {
                    for (int i = 0; i < m_ConstValueTokenList.Count; i++)
                    {
                        sb.Append(m_ConstValueTokenList[i].ToFormatString());
                        if (i < m_ConstValueTokenList.Count - 1)
                            sb.Append(",");
                    }
                }

                if (m_ExecuteBlockSyntax != null)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(m_ExecuteBlockSyntax.ToFormatString());
                }
                if (isContinueNextCastSyntax)
                {
                    sb.Append("next;");
                }


                return sb.ToString();
            }
        }

        public FileMetaCallLink fileMetaVariableRef => m_FileMetaVariableRef;
        public FileMetaBlockSyntax defaultExecuteBlockSyntax => m_DefaultExecuteBlockSyntax;
        public List<FileMetaKeyCaseSyntax> fileMetaKeyCaseSyntaxList => m_FileMetaKeyCaseSyntaxList;
        public FileMetaBlockSyntax executeBlockSyntax => m_DefaultExecuteBlockSyntax;

        private Token m_LeftBraceToken = null;
        private Token m_RightBraceToken = null;
        private FileMetaCallLink m_FileMetaVariableRef = null;
        private FileMetaBlockSyntax m_DefaultExecuteBlockSyntax = null;
        private List<FileMetaKeyCaseSyntax> m_FileMetaKeyCaseSyntaxList = new List<FileMetaKeyCaseSyntax>();
        public FileMetaKeyMatchSyntax(FileMeta fm, Token _switchToken, Token _leftBraceToken,
            Token _rightBraceToken, FileMetaCallLink cl)
        {
            m_FileMeta = fm;
            m_Token = _switchToken;
            m_LeftBraceToken = _leftBraceToken;
            m_RightBraceToken = _rightBraceToken;
            m_FileMetaVariableRef = cl;
        }
        public void AddFileMetaKeyCaseSyntaxList(FileMetaKeyCaseSyntax keyCase)
        {
            m_FileMetaKeyCaseSyntaxList.Add(keyCase);
        }
        public void SetDefaultExecuteBlockSyntax(FileMetaBlockSyntax fmbs)
        {
            m_DefaultExecuteBlockSyntax = fmbs;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileMetaKeyCaseSyntaxList.Count; i++)
            {
                m_FileMetaKeyCaseSyntaxList[i].SetDeep(m_Deep + 1);
            }
            m_DefaultExecuteBlockSyntax?.SetDeep(m_Deep + 1);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("switch ");
            if (m_FileMetaVariableRef != null)
            {
                sb.Append(m_FileMetaVariableRef.ToFormatString());
            }
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("{");

            for (int i = 0; i < m_FileMetaKeyCaseSyntaxList.Count; i++)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_FileMetaKeyCaseSyntaxList[i].ToFormatString());
            }
            if (m_DefaultExecuteBlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < deep + 1; i++)
                    sb.Append(Global.tabChar);
                sb.Append("default");
                sb.Append(Environment.NewLine);
                sb.Append(m_DefaultExecuteBlockSyntax.ToFormatString());
            }

            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");

            return sb.ToString();
        }
    }
    public class FileMetaKeyForSyntax : FileMetaSyntax
    {
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;
        public FileMetaSyntax fileMetaClassDefine => m_FileMetaClassDefine;
        public FileMetaBaseTerm conditionExpress => m_ConditionExpress;
        public FileMetaOpAssignSyntax stepFileMetaOpAssignSyntax => m_StepFileMetaOpAssignSyntax;
        public bool isInFor { get { return m_InToken != null;  } }

        private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;
        private FileMetaSyntax m_FileMetaClassDefine = null;
        private FileMetaBaseTerm m_ConditionExpress  = null;
        private FileMetaOpAssignSyntax m_StepFileMetaOpAssignSyntax = null;
        private Token m_InToken = null;

        public FileMetaKeyForSyntax(FileMeta fm, Token _forToken, FileMetaBlockSyntax fmbs )
        {
            m_FileMeta = fm;
            m_Token = _forToken;
            m_ExecuteBlockSyntax = fmbs;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_ExecuteBlockSyntax?.SetDeep(m_Deep);
        }        
        public void SetFileMetaClassDefine(FileMetaSyntax fmdvs)
        {
            m_FileMetaClassDefine = fmdvs;
            if( fmdvs is FileMetaCallSyntax )
            {
                (fmdvs as FileMetaCallSyntax).isAppendSemiColon = false;
            }
        }
        public void SetInKeyAndArrayVariable(Token inToken, FileMetaBaseTerm cn)
        {
            m_InToken = inToken;
            m_ConditionExpress = cn;
        }
        public void SetConditionExpress(FileMetaBaseTerm fmte)
        {
            m_ConditionExpress = fmte;
        }
        public void SetStepFileMetaOpAssignSyntax(FileMetaOpAssignSyntax fmoas)
        {
            m_StepFileMetaOpAssignSyntax = fmoas;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            
            sb.Append(Environment.NewLine);
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("for ");
            if (m_FileMetaClassDefine != null)
            {
                sb.Append(m_FileMetaClassDefine.ToFormatString() );
            }
            if (m_InToken != null)
            {
                sb.Append( " " + m_InToken.lexeme.ToString());
                sb.Append(" " + m_ConditionExpress.ToFormatString());
            }
            else
            {
                if (m_ConditionExpress != null)
                {
                    sb.Append(", " + m_ConditionExpress.ToFormatString() );
                }
                if (m_StepFileMetaOpAssignSyntax != null)
                {
                    sb.Append(", " + m_StepFileMetaOpAssignSyntax.ToFormatString());
                }
            }
            if (m_ExecuteBlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ExecuteBlockSyntax.ToFormatString());
            }

            return sb.ToString();
        }
    }
    public class FileMetaKeyOnlySyntax : FileMetaSyntax
    {
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlockSyntax;

        private FileMetaBlockSyntax m_ExecuteBlockSyntax = null;
        public FileMetaKeyOnlySyntax(FileMeta fm, Token _token, FileMetaBlockSyntax _executeBlockSyntax)
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_ExecuteBlockSyntax = _executeBlockSyntax;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            if(m_ExecuteBlockSyntax != null )
            {
                m_ExecuteBlockSyntax.SetDeep(_deep);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme.ToString() );
            if (m_ExecuteBlockSyntax != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_ExecuteBlockSyntax?.ToFormatString());
            }
            return sb.ToString();
        }
    }
    public class FileMetaKeyReturnSyntax : FileMetaSyntax
    {
        public FileMetaBaseTerm returnExpress =>m_ReturnExpress;

        private FileMetaBaseTerm m_ReturnExpress = null;

        //public static FileMetaKeyReturnSyntax ParseIfSyntax(FileMeta fm, StructParse.SyntaxNodeStruct akss)
        //{           
        //    var cnode = akss.keyNode;
        //    FileMetaBaseTerm conditionExpress = FileMetatUtil.CreateFileMetaExpress(fm, akss.keyContent, FileMetaTermExpress.EExpressType.Common);
        //    var fms = new FileMetaKeyReturnSyntax(fm, cnode.token, conditionExpress);
        //    return fms;
        //}
        public FileMetaKeyReturnSyntax(FileMeta fm, Token _token, FileMetaBaseTerm _express )
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_ReturnExpress = _express;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme.ToString() + " ");
            sb.Append(m_ReturnExpress?.ToFormatString());

            return sb.ToString();
        }
    }
    public class FileMetaKeyGotoLabelSyntax: FileMetaSyntax
    {
        public Token labelToken => m_LabelToken;

        private Token m_LabelToken = null;

        public static FileMetaKeyGotoLabelSyntax ParseIfSyntax(FileMeta fm, StructParse.SyntaxNodeStruct akss)
        {
            var cnode = akss.keyNode;
            Token labelToken = null;
            if (akss.keyContent.Count != 1 )
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 解析Goto Label语法，只支持 goto id;的语法!!");
            }
            else
            {
                labelToken = akss.keyContent[0].token;
                if (labelToken.type != ETokenType.Identifier)
                {
                    Log.AddFileMetaLog(LID.ShowExtendMessage, "Error 解析GotoLabel中 后边必须使用普通字符");
                }
            }
            var fms = new FileMetaKeyGotoLabelSyntax( fm, cnode.token, labelToken);

            return fms;
        }
        public FileMetaKeyGotoLabelSyntax(FileMeta fm, Token _token, Token _label )
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_LabelToken = _label;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme.ToString() + " ");
            sb.Append(m_LabelToken?.lexeme.ToString() );
            return sb.ToString();
        }
    }
    public class FileMetaCallSyntax : FileMetaSyntax
    {
        public FileMetaCallLink variableRef => m_FileMetaVariableRef;
        public FileMetaBaseTerm expressTerm => m_ExpressTerm;

        private FileMetaCallLink m_FileMetaVariableRef = null;
        private FileMetaBaseTerm m_ExpressTerm = null;
        public FileMetaCallSyntax( FileMetaCallLink fmrv )
        {
            m_FileMetaVariableRef = fmrv;
        }
        public FileMetaCallSyntax( FileMetaBaseTerm fme )
        {
            m_ExpressTerm = fme;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append( m_FileMetaVariableRef?.ToFormatString() );
            if(isAppendSemiColon )
            {
                sb.Append(";");
            }
            return sb.ToString();
        }
    }
    public class FileMetaDefineVariableSyntax : FileMetaSyntax
    {
        public Token nameToken => m_Token;
        public Token constToken => m_ConstToken;
        public Token staticToken => m_StaticToken;
        public Token assignToken => m_AssignToken;
        public FileMetaClassDefine fileMetaClassDefine => m_FileMetaClassDefine;       
        public FileMetaBaseTerm express => m_FileMetaExpress;

        private FileMetaClassDefine m_FileMetaClassDefine = null;
        private Token m_ConstToken = null;
        private Token m_StaticToken = null;
        private Token m_AssignToken = null;
        private FileMetaBaseTerm m_FileMetaExpress = null;

        public FileMetaDefineVariableSyntax( FileMeta fm, FileMetaClassDefine fmcd, Token nameToken,
            Token _assignToken, Token _staticToken, Token _constToken, FileMetaBaseTerm _express )
        {
            m_FileMeta = fm;
            m_Token = nameToken;
            m_StaticToken = _staticToken;
            m_ConstToken = _constToken;
            m_AssignToken = _assignToken;
            m_FileMetaClassDefine = fmcd;
            m_FileMetaExpress = _express;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if (m_ConstToken != null)
            {
                sb.Append(m_ConstToken.lexeme.ToString() + " ");
            }
            if (m_StaticToken != null)
            {
                sb.Append(m_StaticToken.lexeme.ToString() + " ");
            }
            if (m_FileMetaClassDefine != null)
                sb.Append(m_FileMetaClassDefine.ToFormatString() + " ");
            sb.Append(m_Token.lexeme.ToString() + " ");
            if (m_AssignToken != null)
            {
                sb.Append(m_AssignToken.lexeme.ToString() + " ");
                sb.Append(m_FileMetaExpress?.ToFormatString());
                if (isAppendSemiColon)
                {
                    sb.Append(";");
                }
            }
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_FileMetaClassDefine != null)
                sb.Append(m_FileMetaClassDefine.ToString() + " ");
            sb.Append(m_Token.lexeme.ToString() + " ");
            if (m_AssignToken != null)
            {
                sb.Append(m_AssignToken.lexeme.ToString() + " ");
                sb.Append(m_FileMetaExpress?.ToString());
            }
            return sb.ToString();
        }
    }
    public class FileMetaOpAssignSyntax : FileMetaSyntax
    {
        public FileMetaCallLink variableRef => m_VariableRef;
        public FileMetaBaseTerm express => m_Express;
        public Token assignToken => m_AssignToken;
        public Token dynamicToken => m_DynamicToken;
        public Token dataToken => m_DataToken;
        public Token constToken => m_ConstToken;
        public Token staticToken => m_StaticToken;
        public Token functionToken => m_FunctionToken;
        public bool hasDefine => m_DynamicToken != null || m_DataToken != null || m_VarToken != null || m_FunctionToken != null;

        private FileMetaCallLink m_VariableRef = null;
        private FileMetaBaseTerm m_Express = null;
        private Token m_AssignToken = null;
        private Token m_DynamicToken = null;
        private Token m_DataToken = null;
        private Token m_VarToken = null;
        private Token m_ConstToken = null;
        private Token m_StaticToken = null;
        private Token m_FunctionToken = null;
        public FileMetaOpAssignSyntax(FileMetaCallLink fileMetaVariableRef, Token _opAssignToken, Token _dynamicClassToken,
            Token _dynamicDataToken, Token _varToken, Token _functionToken,
            FileMetaBaseTerm fme, bool flag = false  )
        {
            m_VariableRef = fileMetaVariableRef;
            m_AssignToken = _opAssignToken;
            m_DynamicToken = _dynamicClassToken;
            m_DataToken = _dynamicDataToken;
            m_VarToken = _varToken;
            m_FunctionToken = _functionToken;
            m_Express = fme;
            m_Token = fileMetaVariableRef.callNodeList[0].token;
            isAppendSemiColon = flag;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_Express?.SetDeep(m_Deep);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_VariableRef.ToFormatString());
            sb.Append(" " + assignToken.lexeme.ToString());
            sb.Append(" " + m_Express?.ToFormatString() );
            if (isAppendSemiColon)
            {
                sb.Append(";");
            }
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_VariableRef != null)
                sb.Append(m_VariableRef.ToString() + " ");
            sb.Append(" " + assignToken.lexeme.ToString());
            sb.Append(" " + m_Express?.ToString());
            return sb.ToString();
        }

    }

    /// <summary>
    /// 闭包定义语法:
    ///   具名: function name( params ) { body }
    ///   匿名: var name = ( params ) { body }
    /// </summary>
    public class FileMetaDefineClosureSyntax : FileMetaSyntax
    {
        public Token nameToken => m_Token;
        public Token functionToken => m_FunctionToken;
        public bool isAnonymous => m_IsAnonymous;
        public List<FileMetaParamterDefine> paramList => m_ParamList;
        public FileMetaBlockSyntax blockSyntax => m_BlockSyntax;

        private Token m_FunctionToken = null;
        private bool m_IsAnonymous = false;
        private List<FileMetaParamterDefine> m_ParamList = new List<FileMetaParamterDefine>();
        private FileMetaBlockSyntax m_BlockSyntax = null;

        public FileMetaDefineClosureSyntax( FileMeta fm, Token functionToken, Token nameToken,
            bool isAnonymous, List<FileMetaParamterDefine> paramList, FileMetaBlockSyntax block )
        {
            m_FileMeta = fm;
            m_FunctionToken = functionToken;
            m_Token = nameToken;
            m_IsAnonymous = isAnonymous;
            if( paramList != null )
            {
                m_ParamList = paramList;
            }
            m_BlockSyntax = block;
        }
        public void AddParam( FileMetaParamterDefine fmp )
        {
            m_ParamList.Add( fmp );
            fmp.SetFileMeta( m_FileMeta );
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_BlockSyntax?.SetDeep( m_Deep + 1 );
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            if (m_IsAnonymous)
            {
                sb.Append("var " + m_Token?.lexeme.ToString() + " = function( ");
            }
            else
            {
                sb.Append("function " + m_Token?.lexeme.ToString() + "( ");
            }
            for (int i = 0; i < m_ParamList.Count; i++)
            {
                sb.Append(m_ParamList[i].ToFormatString());
                if (i < m_ParamList.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(" )" + Environment.NewLine);
            sb.Append(m_BlockSyntax?.ToFormatString());
            return sb.ToString();
        }
    }

    #region Try / Catch / Finally / Throw Syntax

    /// <summary>
    /// One catch clause: optional type filter, optional variable binding, and a block.
    /// Supports: catch { }, catch e { }, catch Type e { }, catch (Type e) { }
    /// </summary>
    public class FileMetaCatchClause
    {
        public Token catchToken => m_CatchToken;
        public Token typeToken => m_TypeToken;       // null for catch-all
        public Token varToken => m_VarToken;         // null if no binding
        public FileMetaBlockSyntax executeBlockSyntax => m_ExecuteBlock;

        private Token m_CatchToken;
        private Token m_TypeToken = null;
        private Token m_VarToken = null;
        private FileMetaBlockSyntax m_ExecuteBlock;

        public FileMetaCatchClause(Token catchToken, Token typeToken, Token varToken, FileMetaBlockSyntax block)
        {
            m_CatchToken = catchToken;
            m_TypeToken = typeToken;
            m_VarToken = varToken;
            m_ExecuteBlock = block;
        }
    }

    /// <summary>
    /// try { } catch [Type e] { } ... finally { }
    /// </summary>
    public class FileMetaKeyTrySyntax : FileMetaSyntax
    {
        public FileMetaBlockSyntax tryBlockSyntax => m_TryBlock;
        public List<FileMetaCatchClause> catchClauses => m_CatchClauses;
        public FileMetaBlockSyntax finallyBlockSyntax => m_FinallyBlock;
        public bool isChecked => m_IsChecked;

        private FileMetaBlockSyntax m_TryBlock = null;
        private List<FileMetaCatchClause> m_CatchClauses = new List<FileMetaCatchClause>();
        private FileMetaBlockSyntax m_FinallyBlock = null;
        private bool m_IsChecked = false;

        public FileMetaKeyTrySyntax(FileMeta fm)
        {
            m_FileMeta = fm;
        }

        public void SetTryBlock(FileMetaBlockSyntax block) { m_TryBlock = block; }
        public void AddCatchClause(FileMetaCatchClause clause) { m_CatchClauses.Add(clause); }
        public void SetFinallyBlock(FileMetaBlockSyntax block) { m_FinallyBlock = block; }
        public void SetIsChecked(bool val) { m_IsChecked = val; }

        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            m_TryBlock?.SetDeep(_deep);
            foreach (var c in m_CatchClauses) c.executeBlockSyntax?.SetDeep(_deep);
            m_FinallyBlock?.SetDeep(_deep);
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
            sb.Append("try");
            if (m_TryBlock != null) { sb.Append(Environment.NewLine); sb.Append(m_TryBlock.ToFormatString()); }
            foreach (var c in m_CatchClauses)
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
                sb.Append("catch");
                if (c.typeToken != null) sb.Append(" " + c.typeToken.lexeme);
                if (c.varToken != null) sb.Append(" " + c.varToken.lexeme);
                if (c.executeBlockSyntax != null) { sb.Append(Environment.NewLine); sb.Append(c.executeBlockSyntax.ToFormatString()); }
            }
            if (m_FinallyBlock != null)
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
                sb.Append("finally");
                sb.Append(Environment.NewLine);
                sb.Append(m_FinallyBlock.ToFormatString());
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// throw expression  (mirrors FileMetaKeyReturnSyntax)
    /// </summary>
    public class FileMetaKeyThrowSyntax : FileMetaSyntax
    {
        public FileMetaBaseTerm throwExpress => m_ThrowExpress;

        private FileMetaBaseTerm m_ThrowExpress = null;

        public FileMetaKeyThrowSyntax(FileMeta fm, Token _token, FileMetaBaseTerm _express)
        {
            m_FileMeta = fm;
            m_Token = _token;
            m_ThrowExpress = _express;
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < deep; i++) sb.Append(Global.tabChar);
            sb.Append(m_Token?.lexeme.ToString() + " ");
            sb.Append(m_ThrowExpress?.ToFormatString());
            return sb.ToString();
        }
    }

    #endregion

    public class FileMetaBlockSyntax : FileMetaSyntax
    {
        public Token beginBlock => m_BeginBlock;
        public Token endBlock => m_EndBlock;

        private Token m_BeginBlock = null;
        private Token m_EndBlock = null;
        public FileMetaBlockSyntax( FileMeta fm, Token _bblock, Token _eblock )
        {
            m_BeginBlock = _bblock;
            m_EndBlock = _eblock;
        }
        public override void SetDeep(int _deep)
        {
            m_Deep = _deep;
            for (int i = 0; i < m_FileMetaSyntax.Count; i++)
            {
                m_FileMetaSyntax[i].SetDeep(m_Deep + 1);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append(beginBlock.lexeme.ToString() + Environment.NewLine);
            for( int i = 0; i < m_FileMetaSyntax.Count; i++ )
            {
                sb.Append(m_FileMetaSyntax[i].ToFormatString());                
                sb.Append( Environment.NewLine );
            }
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");

            return sb.ToString();
        }
    }


}
