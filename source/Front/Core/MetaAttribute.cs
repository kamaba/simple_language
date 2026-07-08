//****************************************************************************
//  File:      MetaAttribute.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/3/1 12:00:00
//  Description: attribute metadata
//****************************************************************************

using SimpleLanguage.Compile;

namespace SimpleLanguage.Core
{
    public sealed class MetaAttribute
    {
        public string name { get; }
        public FileMetaAttributeSyntax fileMetaAttribute { get; }
        public MetaAttribute(FileMetaAttributeSyntax attr)
        {
            fileMetaAttribute = attr;
            name = attr?.name;
        }
    }
}
