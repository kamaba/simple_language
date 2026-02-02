//****************************************************************************
//  File:      IRStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;

namespace SimpleLanguage.IR
{
    public class IRStatementsManager
    {
        public static IRMetaVariable CreateMethodIRVariable( IRMethod _irmethod, MetaVariable mv )
        {
            IRMetaVariable irmv = new IRMetaVariable(mv);

            return null;
        }
    }
}
