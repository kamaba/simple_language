//****************************************************************************
//  File:      IRTryStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/07/23 12:00:00
//  Description: IR generation for try/catch/finally and throw statements.
//               Currently emits body blocks sequentially without exception
//               table metadata. Exception handling IR (landing pads, unwind
//               tables) will be layered on top of this scaffolding.
//****************************************************************************

using SimpleLanguage.Core;

namespace SimpleLanguage.IR
{
    public class IRTryStatements : IRStatements
    {
        public IRTryStatements(IRMethod method)
        {
            this.irMethod = method;
        }

        public void ParseIRStatements(MetaTryStatements ms)
        {
            // --- try body ---
            if (ms.tryBlockStatements != null)
            {
                IRBlockStatements irTry = new IRBlockStatements(irMethod);
                irTry.ParseIRStatements(ms.tryBlockStatements);
                m_IRStatements.AddRange(irTry.irStatements);
            }

            // --- catch bodies ---
            foreach (var clause in ms.catchClauses)
            {
                if (clause.bodyStatements != null)
                {
                    IRBlockStatements irCatch = new IRBlockStatements(irMethod);
                    irCatch.ParseIRStatements(clause.bodyStatements);
                    m_IRStatements.AddRange(irCatch.irStatements);
                }
            }

            // --- finally body ---
            if (ms.finallyBlockStatements != null)
            {
                IRBlockStatements irFinally = new IRBlockStatements(irMethod);
                irFinally.ParseIRStatements(ms.finallyBlockStatements);
                m_IRStatements.AddRange(irFinally.irStatements);
            }
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
                // Emit the throw expression onto the IR stream.
                var throwExpress = IRExpressManager.CreateExpress(this.irMethod, ms.express);
                if (throwExpress != null)
                {
                    m_IRStatements.Add(throwExpress);
                }
            }
            // TODO: emit an actual Throw opcode once the VM supports it.
        }
    }
}
