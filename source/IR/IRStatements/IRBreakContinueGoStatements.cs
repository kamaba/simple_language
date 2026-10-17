//****************************************************************************
//  File:      IRBreakContinueGoStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************



using SimpleLanguage.Core;

namespace SimpleLanguage.IR.Statements
{
    public class IRBreakStatements : IRStatements
    {
        public IRBreakStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public IRBranch irBrach = null;
        public void ParseIRStatements(MetaBreakStatements ms)
        {
            var breakTarget = irMethod?.GetCurrentBreakTarget();
            irBrach = new IRBranch(irMethod,  EIROpCode.Br, breakTarget );
            m_IRStatements.Add(irBrach);
            if (ms.token != null )
            {
                irBrach.data.SetDebugInfoByToken(ms.token );
            }
        }
    }
    public class IRContinueStatements : IRStatements
    {
        public IRContinueStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public IRBranch irBrach = null;
        public void ParseIRStatements(MetaContinueStatements mcs )
        {
            var continueTarget = irMethod?.GetCurrentContinueTarget();
            irBrach = new IRBranch(irMethod, EIROpCode.Br, continueTarget );
            if (mcs.token != null)
            {
                irBrach.SetDebugInfoByToken(mcs.token);
            }
            m_IRStatements.Add(irBrach);
        }
    }
    public class IRGotoLabelStatements : IRStatements
    {
        public IRGotoLabelStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public IRLabel labelIR = null;
        public void ParseIRStatements(MetaGotoLabelStatements mgls )
        {
            // labelData 为 null: goto 引用了未定义标签, Meta 层已报编译错误, 此处跳过
            if (mgls == null || mgls.labelData == null)
                return;

            // 同一函数内同名的 label/goto 共享同一目标 IRData 实例(支持前向跳转占位)
            var targetIRData = irMethod.GetOrAddLabelTargetData(mgls.labelData.label, mgls.labelToken);
            if (targetIRData == null)
                return;

            labelIR = new IRLabel(irMethod, targetIRData, mgls.isLabel, mgls.labelToken);
            m_IRStatements.Add(labelIR);
        }
    }
}