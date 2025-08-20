//****************************************************************************
//  File:      IRNew.cs
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
        public IRNew( IRMethod irMethod, IRMetaClass irmc) :base( irMethod )
        {
            Parse(irmc);
        }
         void Parse( IRMetaClass irmc )
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewObject;
            data.opValue = irmc;
            data.debugInfo = new DebugInfo() { name = irmc.allName, info = "IRNew" };
            m_IRDataList.Add(data);
        }

        public IRNew(IRMethod irMethod) : base(irMethod )
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewTemplateClass;
            data.debugInfo = new DebugInfo() { name =  "", info = "NewCallClass" };
            m_IRDataList.Add(data);
        }
    }
}
