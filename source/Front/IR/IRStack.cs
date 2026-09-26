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
        public IRNop(IRMethod irMethod, Token token, string info = null) : base(irMethod)
        {
            data.opCode = EIROpCode.Nop;
            data.SetDebugInfoByToken(token, info);
            m_IRDataList.Add(data);
        }
    }
    public class IRDup : IRBase
    {
        public IRData data = new IRData();
        public IRDup(IRMethod irMethod) : base(irMethod )
        {
            data.opValue = null;
            data.opCode = EIROpCode.Dup;
            m_IRDataList.Add(data);
        }
        public IRDup(IRMethod irMethod, int dupcount ):base(irMethod )
        {
            data.opValue = dupcount;
            data.opCode = EIROpCode.Dup;
            m_IRDataList.Add(data);
        }
        public IRDup(IRMethod irMethod, int dupcount, Token token, string info = null) : base(irMethod)
        {
            data.opValue = dupcount;
            data.opCode = EIROpCode.Dup;
            data.SetDebugInfoByToken(token, info);
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
        public IRPop(IRMethod irMethod, Token token, string info = null) : base(irMethod)
        {
            data.opCode = EIROpCode.Pop;
            data.SetDebugInfoByToken(token, info);
            m_IRDataList.Add(data);
        }
    }
}
