//****************************************************************************
//  File:      FileMetaAttributeSyntax.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/3/1 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Compile;

namespace SimpleLanguage.Compile
{
    public sealed class FileMetaAttributeSyntax
    {
        public FileMeta fileMeta { get; }
        public Token token { get; }
        public string name { get; }
        public FileMetaParTerm fileMetaParTerm { get; }

        public FileMetaAttributeSyntax(FileMeta fm, Token atToken, string attrName, FileMetaParTerm parTerm)
        {
            fileMeta = fm;
            token = atToken;
            name = attrName;
            fileMetaParTerm = parTerm;
        }
    }
}
