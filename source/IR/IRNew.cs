//****************************************************************************
//  File:      IRWhileStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRNew : IRBase
    {
        public IRNew( IRMethod irMethod ):base( irMethod )
        {
        }
        public void Parse( MetaType mt )
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewObject;
            data.opValue = mt;
            data.debugInfo = new DebugInfo() { name = mt.metaClass.allName, info = "IRNew" };
            m_IRDataList.Add(data);
        }
    }
}
