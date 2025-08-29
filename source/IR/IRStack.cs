//****************************************************************************
//  File:      IRStack.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: IRNop, IRDup, IRPop
//****************************************************************************

namespace SimpleLanguage.IR
{
    public class IRNop : IRBase
    {
        public IRData data = new IRData();
        public IRNop(IRMethod irMethod) : base(irMethod)
        {
            data.opCode = EIROpCode.Nop;
            m_IRDataList.Add(data);
        }
    }
    public class IRDup : IRBase
    {
        public IRData data = new IRData();
        public IRDup(IRMethod irMethod) : base(irMethod)
        {
            data.opCode = EIROpCode.Dup;
            m_IRDataList.Add(data);
        }
    }
    public class IRPop : IRBase
    {
        public IRData data = new IRData();
        public IRPop(IRMethod irMethod) : base(irMethod)
        {
            data.opCode = EIROpCode.Pop;
            m_IRDataList.Add(data);
        }
    }
}