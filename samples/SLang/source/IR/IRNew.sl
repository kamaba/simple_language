//****************************************************************************
//  File:      IRNew.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

namespace SimpleLanguage.IR
{
    public class IRNew : IRBase
    {
        public IRNew(IRMethod irMethod, IRMetaClass irmc) : base(irMethod)
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewObject;
            data.opValue = irmc;
            data.debugInfo = new DebugInfo() { name = irmc.irName, info = "IRNew" };
            AddIRData(data);
        }
        public IRNew(IRMethod irMethod, IRMetaType opvalue ) : base(irMethod)
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewTemplateClass;
            data.opValue = opvalue;
            data.debugInfo = new DebugInfo() { name = "", info = "NewCallClass" };
            AddIRData(data);
        }
    }
}
