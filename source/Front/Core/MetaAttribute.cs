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

        private MetaClass m_AttributeMetaClass = null;
        public MetaAttribute(FileMetaAttributeSyntax attr)
        {
            fileMetaAttribute = attr;
            name = attr?.name;
        }

        public void Parse()
        {
            // 这里查找Attribute类是否有相关的所属类 然后注册后
        }
    }
}
