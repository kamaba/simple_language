//****************************************************************************
//  File:      IRCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;

using System.Text;

namespace SimpleLanguage.IR.Statements
{
    public class IRCallStatements : IRStatements
    {
        IRMetaCallLink m_IRMc = null;
        public IRCallStatements( IRMethod _iRMethod)
        {
            irMethod = _iRMethod;
        }
        public void ParseIRStatements(MetaCallStatements ms)
        {
            if (ms.expressNode != null)
            {
                // Expression statement (e.g. "try riskyFunc()")
                var irExpress = IRExpressManager.CreateExpress(irMethod, ms.expressNode);
                if (irExpress != null)
                {
                    m_IRStatements.Add(irExpress);
                    // Discard return value if any - but skip a void call: the
                    // VM pushes nothing back for a void return (vm_frame_pop
                    // skips void return slots), so an unconditional Pop would
                    // underflow the eval stack (OpCode_Pop assert, e.g. the
                    // "yield" keyword sugar expanding to Coroutine.yieldNow()).
                    var expType = ms.expressNode.GetReturnMetaType();
                    if (expType?.metaClass?.eType != EType.Void)
                    {
                        m_IRStatements.Add(new IRPop(irMethod));
                    }
                }
                return;
            }
            m_IRMc = new IRMetaCallLink();
            m_IRMc.ParseToIRDataList(irMethod, ms.metaCallLink.visitNodeList);
            m_IRStatements.AddRange(m_IRMc.irList);

            if( ms.isHasReturnMetaVariable )
            {
                IRPop irpop = new IRPop(irMethod);
                m_IRStatements.Add(irpop);
            }
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("call");
            sb.Append(m_IRMc?.ToIRString());
            return sb.ToString();
        }
    }
}
