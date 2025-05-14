//****************************************************************************
//  File:      IRStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR.Statements
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
