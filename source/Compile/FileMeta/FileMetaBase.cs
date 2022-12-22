//****************************************************************************
//  File:      FileMetaBase.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile.CoreFileMeta
{
    public class FileMetaBase
    {
        public Token token => m_Token;
        public int deep => m_Deep;
        public virtual string name
        {
            get
            {
                if (m_Token != null)
                {
                    return m_Token.lexeme.ToString();
                }
                return "";
            }
        }
        public FileMeta fileMeta => m_FileMeta;


        protected int m_Id = 0;
        protected int m_Deep = 0;
        protected FileMeta m_FileMeta;
        protected Token m_Token = null;
        protected static int s_IdCount = 0;
        public void SetFileMeta( FileMeta fm )
        {
            m_FileMeta = fm;
        }
        public virtual void SetDeep( int _deep )
        {
            m_Deep = _deep;
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
}
