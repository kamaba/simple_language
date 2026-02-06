using System;
using System.Reflection.Emit;

namespace SimpleLanguage.VM.InnerCLRRuntime.IL
{
    public sealed class ILInstruction
    {
        public int offset { get; init; }
        public OpCode opCode { get; init; }
        public object operand { get; init; }

        public override string ToString()
        {
            if (operand == null) return $"{offset:X4}: {opCode.Name}";
            return $"{offset:X4}: {opCode.Name} {operand}";
        }
    }
}
