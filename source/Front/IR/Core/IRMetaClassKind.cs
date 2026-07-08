//****************************************************************************
//  File:      IRMetaClassKind.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  Description: Discriminator for exported IR meta class shape (class / enum / data).
//****************************************************************************

namespace SimpleLanguage.IR
{
    /// <summary>User-defined type category mirrored from <see cref="SimpleLanguage.Core.MetaClass"/> hierarchy.</summary>
    public enum IRMetaClassKind : int
    {
        Class = 0,
        Enum = 1,
        Data = 2,
        /// <summary>Declared <c>interface</c> type (not an inheritable class).</summary>
        Interface = 3,
    }
}
