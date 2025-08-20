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
        public static IRNew CreateNew(IRMethod irMethod, IRMetaClass irmc, bool isTemplate)
        {
            IRNew irnew = new IRNew(irMethod);

            if( isTemplate )
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.NewTemplateClass;
                data.opValue = irmc;
                data.debugInfo = new DebugInfo() { name = "", info = "NewCallClass" };
                irnew.AddIRData(data);
            }
            else
            {
                IRData data = new IRData();
                data.opCode = EIROpCode.NewObject;
                data.opValue = irmc;
                data.debugInfo = new DebugInfo() { name = irmc.allName, info = "IRNew" };
                irnew.AddIRData(data);
            }
            return irnew;
        }
        public IRNew(IRMethod irMethod) :base( irMethod )
        {
        }
    }
}
