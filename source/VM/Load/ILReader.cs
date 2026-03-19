using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SimpleLanguage.VM.InnerCLRRuntime.IL
{
    public static class ILReader
    {
        private static readonly OpCode[] s_OneByteOpCodes = new OpCode[0x100];
        private static readonly OpCode[] s_TwoByteOpCodes = new OpCode[0x100];

        static ILReader()
        {
            foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType != typeof(OpCode)) continue;
                var op = (OpCode)f.GetValue(null);
                var v = (ushort)op.Value;
                if (v < 0x100)
                {
                    s_OneByteOpCodes[v] = op;
                }
                else if ((v & 0xFF00) == 0xFE00)
                {
                    s_TwoByteOpCodes[v & 0xFF] = op;
                }
            }
        }

        public static List<Instruction> Read(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            var body = method.GetMethodBody();
            if (body == null) return new();
            var il = body.GetILAsByteArray();
            return Read(method.Module, il);
        }

        public static List<Instruction> Read(Module module, byte[] ilBytes)
        {
            if (ilBytes == null || ilBytes.Length == 0) return new();
            module ??= typeof(ILReader).Module;

            var list = new List<Instruction>(ilBytes.Length / 2);
            var i = 0;
            while (i < ilBytes.Length)
            {
                var offset = i;
                var op = ReadOpCode(ilBytes, ref i);
                var operand = ReadOperand(module, op, ilBytes, ref i);

                list.Add(new Instruction {  });
            }
            return list;
        }

        private static OpCode ReadOpCode(byte[] il, ref int i)
        {
            var b = il[i++];
            if (b != 0xFE) return s_OneByteOpCodes[b];
            var b2 = il[i++];
            return s_TwoByteOpCodes[b2];
        }

        private static object ReadOperand(Module module, OpCode op, byte[] il, ref int i)
        {
            switch (op.OperandType)
            {
                case OperandType.InlineNone:
                    return null;

                case OperandType.ShortInlineI:
                    return (sbyte)il[i++];

                case OperandType.InlineI:
                    {
                        var v = BitConverter.ToInt32(il, i);
                        i += 4;
                        return v;
                    }
                case OperandType.InlineI8:
                    {
                        var v = BitConverter.ToInt64(il, i);
                        i += 8;
                        return v;
                    }
                case OperandType.ShortInlineR:
                    {
                        var v = BitConverter.ToSingle(il, i);
                        i += 4;
                        return v;
                    }
                case OperandType.InlineR:
                    {
                        var v = BitConverter.ToDouble(il, i);
                        i += 8;
                        return v;
                    }

                case OperandType.ShortInlineVar:
                    return il[i++];

                case OperandType.InlineVar:
                    {
                        var v = BitConverter.ToUInt16(il, i);
                        i += 2;
                        return v;
                    }

                case OperandType.ShortInlineBrTarget:
                    {
                        var delta = (sbyte)il[i++];
                        return i + delta;
                    }
                case OperandType.InlineBrTarget:
                    {
                        var delta = BitConverter.ToInt32(il, i);
                        i += 4;
                        return i + delta;
                    }

                case OperandType.InlineString:
                    {
                        var token = BitConverter.ToInt32(il, i);
                        i += 4;
                        try { return module.ResolveString(token); }
                        catch { return $"string(0x{token:X8})"; }
                    }

                case OperandType.InlineType:
                    {
                        var token = BitConverter.ToInt32(il, i);
                        i += 4;
                        try { return module.ResolveType(token); }
                        catch { return $"type(0x{token:X8})"; }
                    }
                case OperandType.InlineField:
                    {
                        var token = BitConverter.ToInt32(il, i);
                        i += 4;
                        try { return module.ResolveField(token); }
                        catch { return $"field(0x{token:X8})"; }
                    }
                case OperandType.InlineMethod:
                    {
                        var token = BitConverter.ToInt32(il, i);
                        i += 4;
                        try { return module.ResolveMethod(token); }
                        catch { return $"method(0x{token:X8})"; }
                    }
                case OperandType.InlineTok:
                    {
                        var token = BitConverter.ToInt32(il, i);
                        i += 4;
                        try { return module.ResolveMember(token); }
                        catch { return $"tok(0x{token:X8})"; }
                    }

                case OperandType.InlineSig:
                    {
                        var token = BitConverter.ToInt32(il, i);
                        i += 4;
                        return $"sig(0x{token:X8})";
                    }

                case OperandType.InlineSwitch:
                    {
                        var count = BitConverter.ToInt32(il, i);
                        i += 4;
                        var targets = new int[count];
                        for (var t = 0; t < count; t++)
                        {
                            var delta = BitConverter.ToInt32(il, i);
                            i += 4;
                            targets[t] = i + delta;
                        }
                        return targets;
                    }

                default:
                    return null;
            }
        }
    }
}
