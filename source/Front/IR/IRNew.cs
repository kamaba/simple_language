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
            // VM side reads NewObject payload as Int32 classId.
            data.SetOpValue(irmc != null ? irmc.id : 0);
            data.debugInfo = new DebugInfo() { name = irmc?.irName ?? string.Empty, info = "IRNew" };
            // Fill source location from the bound method's token so IRNew carries a real path/line.
            data.SetDebugInfoByToken(irMethod?.bindMetaFunction?.token);
            AddIRData(data);
        }
        public IRNew(IRMethod irMethod, IRMetaType opvalue ) : base(irMethod)
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewTemplateObject;
            data.SetOpValue(opvalue);
            data.debugInfo = new DebugInfo() { name = opvalue?.irMetaClass?.irName ?? string.Empty, info = "NewCallClass" };
            data.SetDebugInfoByToken(irMethod?.bindMetaFunction?.token);
            AddIRData(data);
        }
        public IRNew(IRMethod irMethod, IRMetaType opvalue, int type ) : base(irMethod)
        {
            IRData data = new IRData();
            data.opCode = EIROpCode.NewArray;
            data.SetOpValue(opvalue);
            data.debugInfo = new DebugInfo() { name = opvalue?.irMetaClass?.irName ?? string.Empty, info = "NewArray" };
            data.SetDebugInfoByToken(irMethod?.bindMetaFunction?.token);
            AddIRData(data);
        }
    }
}
