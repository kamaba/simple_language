//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRDup : IRBase
    {
        public IRData data = new IRData();
        public IRDup( IRMethod irMethod ):base( irMethod )
        {
            data.opCode = EIROpCode.Dup;
            m_IRDataList.Add(data);
        }
    }
}
