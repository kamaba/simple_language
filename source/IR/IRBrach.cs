//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRBranch : IRBase
    {
        public IRData data = new IRData();
        public IRBranch( IRMethod _irMethod, EIROpCode type, IRData brIRData )
        {
            data.opCode = type;
            data.opValue = brIRData;
            AddIRData( data );
        }
        public void SetOpValue(IRData opValue)
        {
            data.opValue = opValue;
        }
        public void SetDebugInfoByToken(Token token)
        {
            data.SetDebugInfoByToken(token);
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.ToIRString());

            return sb.ToString();
        }
    }
    public class IRLabel : IRBase
    {
        IRData data = new IRData();
        public IRLabel(IRMethod _irMethod, string _label, bool isGogo )
        {
            data = new IRData();
            data.opCode = isGogo ? EIROpCode.Label : EIROpCode.BrLabel;
            data.opValue = _label;
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
