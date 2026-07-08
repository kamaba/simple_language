using System;
using System.IO;
using System.Linq;
using System.Text;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export
{
    // Very small IR -> C# emitter and project generator.
    // Goal: generate a C# project with one class per IRMethod and compile to DLL using `dotnet build`.
    public static class CSharpCode
    {
        static string EmitMethodAsCSharp(IRMethod m)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("namespace SimpleLanguage.Generated");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {SanitizeTypeName(m.id)}");
            sb.AppendLine("    {");
            // create a simple signature: static object Run(object[] args)
            sb.AppendLine("        public static object Run(object[] args)");
            sb.AppendLine("        {");
            sb.AppendLine("            // simple stack-based interpreter translated from IR");
            sb.AppendLine("            var stack = new System.Collections.Generic.List<object>();");
            // locals array to support LoadLocal/StoreLocal
            int localCount = Math.Max(1, m.methodLocalVariableList?.Count ?? 0);
            sb.AppendLine($"            var locals = new object[{localCount}];");
            foreach (var d in m.IRDataList)
            {
                switch (d.opCode)
                {
                    case EIROpCode.LoadConstInt32:
                        if (d.TryGetInt32(out var iv)) sb.AppendLine($"            stack.Add({iv});");
                        break;
                    case EIROpCode.LoadConstFloat32:
                        if (d.TryGetSingle(out var fv)) sb.AppendLine($"            stack.Add({fv}f);");
                        break;
                    case EIROpCode.LoadConstFloat64:
                        if (d.TryGetDouble(out var dv)) sb.AppendLine($"            stack.Add({dv:R});");
                        break;
                    case EIROpCode.Add:
                        sb.AppendLine("            { var b = Convert.ToDouble(stack[stack.Count-1]); stack.RemoveAt(stack.Count-1); var a = Convert.ToDouble(stack[stack.Count-1]); stack[stack.Count-1] = a + b; }");
                        break;
                    case EIROpCode.Minus:
                        sb.AppendLine("            { var b = Convert.ToDouble(stack[stack.Count-1]); stack.RemoveAt(stack.Count-1); var a = Convert.ToDouble(stack[stack.Count-1]); stack[stack.Count-1] = a - b; }");
                        break;
                    case EIROpCode.Multiply:
                        sb.AppendLine("            { var b = Convert.ToDouble(stack[stack.Count-1]); stack.RemoveAt(stack.Count-1); var a = Convert.ToDouble(stack[stack.Count-1]); stack[stack.Count-1] = a * b; }");
                        break;
                    case EIROpCode.Divide:
                        sb.AppendLine("            { var b = Convert.ToDouble(stack[stack.Count-1]); stack.RemoveAt(stack.Count-1); var a = Convert.ToDouble(stack[stack.Count-1]); stack[stack.Count-1] = a / b; }");
                        break;
                    case EIROpCode.Ret:
                        sb.AppendLine("            if (stack.Count==0) return null; return stack[stack.Count-1];");
                        break;
                    case EIROpCode.LoadArgument:
                        // push argument by index (IRMetaVariable.index)
                        sb.AppendLine($"            // LoadArgument idx={d.index}");
                        sb.AppendLine($"            stack.Add(args.Length > {d.index} ? args[{d.index}] : null);");
                        break;
                    case EIROpCode.LoadLocal:
                        sb.AppendLine($"            // LoadLocal idx={d.index}");
                        sb.AppendLine($"            stack.Add(locals[{d.index}]);");
                        break;
                    case EIROpCode.StoreLocal:
                        sb.AppendLine($"            // StoreLocal idx={d.index}");
                        sb.AppendLine("            if(stack.Count>0){ locals[" + d.index + "] = stack[stack.Count-1]; stack.RemoveAt(stack.Count-1); }");
                        break;
                    case EIROpCode.CallStatic:
                    case EIROpCode.CallVirt:
                    case EIROpCode.CallDynamic:
                        {
                            if (d.opValue is IRMethodCall imc)
                            {
                                int argc = imc.paramCount;
                                // pop argc args into array (preserve left-to-right order)
                                sb.AppendLine($"            object[] callArgs = new object[{argc}];");
                                for (int ai = argc - 1; ai >= 0; ai--)
                                {
                                    sb.AppendLine($"            callArgs[{ai}] = stack[stack.Count-1]; stack.RemoveAt(stack.Count-1);");
                                }
                                string callee = SanitizeTypeName(imc.irMethod?.id ?? imc.methodName ?? "");
                                sb.AppendLine($"            var callRes = SimpleLanguage.Generated.RuntimeBridge.Call(\"{callee}\", callArgs);");
                                sb.AppendLine("            stack.Add(callRes);");
                            }
                            else
                            {
                                sb.AppendLine($"            // call opcode placeholder: {d.opCode}");
                            }
                        }
                        break;
                    default:
                        sb.AppendLine($"            // unsupported opcode: {d.opCode}");
                        break;
                }
            }
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        // Helpers used by dynamic emitter (simple boxed arithmetic on stack list)
        public static object PopObject(System.Collections.Generic.List<object> stack)
        {
            if (stack == null || stack.Count == 0) return null;
            var v = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return v;
        }
        public static void BinaryAdd(System.Collections.Generic.List<object> stack)
        {
            var b = PopObject(stack);
            var a = PopObject(stack);
            double ad = a == null ? 0.0 : Convert.ToDouble(a);
            double bd = b == null ? 0.0 : Convert.ToDouble(b);
            stack.Add(ad + bd);
        }
        public static void BinarySub(System.Collections.Generic.List<object> stack)
        {
            var b = PopObject(stack);
            var a = PopObject(stack);
            double ad = a == null ? 0.0 : Convert.ToDouble(a);
            double bd = b == null ? 0.0 : Convert.ToDouble(b);
            stack.Add(ad - bd);
        }
        public static void BinaryMul(System.Collections.Generic.List<object> stack)
        {
            var b = PopObject(stack);
            var a = PopObject(stack);
            double ad = a == null ? 0.0 : Convert.ToDouble(a);
            double bd = b == null ? 0.0 : Convert.ToDouble(b);
            stack.Add(ad * bd);
        }
        public static void BinaryDiv(System.Collections.Generic.List<object> stack)
        {
            var b = PopObject(stack);
            var a = PopObject(stack);
            double ad = a == null ? 0.0 : Convert.ToDouble(a);
            double bd = b == null ? 0.0 : Convert.ToDouble(b);
            stack.Add(bd == 0.0 ? 0.0 : ad / bd);
        }

        static string RuntimeBridgeSource()
        {
            return "using System;namespace SimpleLanguage.Generated{ public static class RuntimeBridge{ public static object Call(string type, object[] args){ return null;} } }";
        }

        static string SanitizeFileName(string name)
        {
            var sb = new StringBuilder();
            foreach (var c in name) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }
        static string SanitizeTypeName(string name) => SanitizeFileName(name);
    }
}
