//****************************************************************************
//  File:      IRTryStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/07/23 12:00:00
//  Description: IR generation for try/catch/finally and throw statements.
//****************************************************************************

using SimpleLanguage.Core;
using System.Collections.Generic;

namespace SimpleLanguage.IR
{
    /// <summary>
    /// Carries catch/finally target IRData references for BeginTry opcode.
    /// Resolved to instruction indices during IRMethod.Parse().
    /// </summary>
    public class TryScopeData
    {
        public IRData catchTarget;     // null if no catch
        public IRData finallyTarget;   // null if no finally
    }

    /// <summary>Wraps a raw IRData in an IRBase for inclusion in IRStatements list.</summary>
    public class IRRawData : IRBase
    {
        public IRRawData(IRMethod irMethod, IRData irData) : base(irMethod)
        {
            m_IRDataList.Add(irData);
        }
    }

    public class IRTryStatements : IRStatements
    {
        public IRTryStatements(IRMethod method)
        {
            this.irMethod = method;
        }

        public void ParseIRStatements(MetaTryStatements ms)
        {
            // Create label targets
            IRNop endNop = new IRNop(irMethod);          // end of try/catch/finally
            IRNop finallyNop = null;                      // start of finally block
            if (ms.finallyBlockStatements != null)
                finallyNop = new IRNop(irMethod);

            // --- BeginTry ---
            IRData beginTryData = new IRData();
            beginTryData.opCode = EIROpCode.BeginTry;
            TryScopeData tsd = new TryScopeData();
            // Set catch target to first catch label (if any), else null
            IRNop firstCatchNop = null;
            List<IRNop> catchNops = new List<IRNop>();
            foreach (var clause in ms.catchClauses)
            {
                IRNop catchNop = new IRNop(irMethod);
                catchNops.Add(catchNop);
                if (firstCatchNop == null) firstCatchNop = catchNop;
            }
            tsd.catchTarget = firstCatchNop?.data;
            tsd.finallyTarget = finallyNop?.data;
            beginTryData.SetOpValue(tsd);
            m_IRStatements.Add(new IRRawData(irMethod, beginTryData));

            // --- try body ---
            if (ms.tryBlockStatements != null)
            {
                IRBlockStatements irTry = new IRBlockStatements(irMethod);
                irTry.ParseIRStatements(ms.tryBlockStatements);
                m_IRStatements.AddRange(irTry.irStatements);
            }

            // --- LeaveTry (try completed normally) ---
            IRData leaveTryData = new IRData();
            leaveTryData.opCode = EIROpCode.LeaveTry;
            leaveTryData.SetOpValue(finallyNop?.data ?? endNop.data);
            m_IRStatements.Add(new IRRawData(irMethod, leaveTryData));

            // --- catch bodies ---
            for (int ci = 0; ci < ms.catchClauses.Count; ci++)
            {
                var clause = ms.catchClauses[ci];

                // Catch label
                m_IRStatements.Add(catchNops[ci]);

                // Catch body
                if (clause.bodyStatements != null)
                {
                    IRBlockStatements irCatch = new IRBlockStatements(irMethod);
                    irCatch.ParseIRStatements(clause.bodyStatements);
                    m_IRStatements.AddRange(irCatch.irStatements);
                }

                // LeaveTry after catch (jump to finally or end)
                IRData leaveCatchData = new IRData();
                leaveCatchData.opCode = EIROpCode.LeaveTry;
                leaveCatchData.SetOpValue(finallyNop?.data ?? endNop.data);
                m_IRStatements.Add(new IRRawData(irMethod, leaveCatchData));
            }

            // --- finally body ---
            if (finallyNop != null)
            {
                // Finally label
                m_IRStatements.Add(finallyNop);

                // Finally body
                if (ms.finallyBlockStatements != null)
                {
                    IRBlockStatements irFinally = new IRBlockStatements(irMethod);
                    irFinally.ParseIRStatements(ms.finallyBlockStatements);
                    m_IRStatements.AddRange(irFinally.irStatements);
                }

                // EndFinally
                IRData endFinallyData = new IRData();
                endFinallyData.opCode = EIROpCode.EndFinally;
                m_IRStatements.Add(new IRRawData(irMethod, endFinallyData));
            }

            // --- end label ---
            m_IRStatements.Add(endNop);
        }
    }

    public class IRThrowStatements : IRStatements
    {
        public IRThrowStatements(IRMethod method)
        {
            this.irMethod = method;
        }

        public void ParseIRStatements(MetaThrowStatements ms)
        {
            if (ms.express != null)
            {
                // Emit the throw expression onto the IR stream (value on stack).
                var throwExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                if (throwExpress != null)
                {
                    m_IRStatements.Add(throwExpress);
                }
            }
            // Emit Throw opcode
            IRData throwData = new IRData();
            throwData.opCode = EIROpCode.Throw;
            m_IRStatements.Add(new IRRawData(irMethod, throwData));
        }
    }
}
