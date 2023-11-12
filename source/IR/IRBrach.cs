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
        public IRBranch( IRMethod _irMethod, EIROpCode type, IRData brIRData )
        {
            brData.opCode = type;
            brData.opValue = brData;
        }
        public void SetOpValue(IRData opValue)
        {
            brData.opValue = opValue;
        }
        public override void SetCodeFileLine(int line)
        {
            brData.line = line;
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("branch");
            sb.Append(base.ToIRString());

            return sb.ToString();
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
            StringBuilder sb = new StringBuilder();

            sb.Append("labal");
            sb.Append(base.ToIRString());

            return sb.ToString();
        }
    }
}
