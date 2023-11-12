//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRBranch : IRBase
    {
        public IRData brData = new IRData();
        public IRBranch( IRMethod _irMethod, EIROpCode type )
        {
            brData.opCode = type;
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
    public class IRLabel : IRBase
    {
        IRData labelIRData = new IRData();
        public IRLabel(IRMethod _irMethod, string _label, bool isGogo )
        {
            labelIRData = new IRData();
            labelIRData.opCode = isGogo ? EIROpCode.Label : EIROpCode.BrLabel;
            labelIRData.opValue = _label;
        }
        public override string ToIRString()
        {
            return base.ToIRString();
        }
    }
}
