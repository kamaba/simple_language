using System;
using System.IO;
using System.Linq;
using System.Text;
using SimpleLanguage.IR;
using System.Diagnostics;

namespace SimpleLanguage.Export
{
    // Very small IR -> C# emitter and project generator.
    // Goal: generate a C# project with one class per IRMethod and compile to DLL using `dotnet build`.
    public static class CSharpEmitter
    {
        // Rewrite: emit textual IL files (one .il per method) instead of C# sources.
        // This writes human-readable IL that mirrors the IR instruction stream. Assembling
        // to a real DLL is environment-dependent (requires ilasm or other tooling) and
        // is left to the user. The method returns the directory containing generated .il files.
        public static string EmitModuleAsCSharpAndBuild(IRMethod[] methods, string outDir, string assemblyName)
        {
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            var an = new System.Reflection.AssemblyName(assemblyName);
            // create in-memory dynamic assembly
            var ab = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(an, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule(assemblyName + "Module");

            var listType = typeof(System.Collections.Generic.List<object>);
            var listAdd = listType.GetMethod("Add");

            // helper method infos
            var miBinaryAdd = typeof(CSharpEmitter).GetMethod(nameof(BinaryAdd));
            var miBinarySub = typeof(CSharpEmitter).GetMethod(nameof(BinarySub));
            var miBinaryMul = typeof(CSharpEmitter).GetMethod(nameof(BinaryMul));
            var miBinaryDiv = typeof(CSharpEmitter).GetMethod(nameof(BinaryDiv));
            var miPop = typeof(CSharpEmitter).GetMethod(nameof(PopObject));

            // locate potential entry method (Main)
            string entryMethodId = null;
            foreach (var mm in methods)
            {
                if (!string.IsNullOrEmpty(mm.onlyFunctionName) && mm.onlyFunctionName == "Main") { entryMethodId = mm.id; break; }
                if (!string.IsNullOrEmpty(mm.id) && (mm.id.EndsWith(".Main") || mm.id.EndsWith("::Main") || mm.id == "Main")) { entryMethodId = mm.id; break; }
            }

            var createdMethods = new System.Collections.Generic.Dictionary<string, System.Reflection.MethodInfo>();

            foreach (var m in methods)
            {
                string tname = SanitizeTypeName(m.id);
                var tb = mb.DefineType(tname, System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.BeforeFieldInit);
                var mbMethod = tb.DefineMethod("Run", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, typeof(object), new Type[] { typeof(object[]) });
                var il = mbMethod.GetILGenerator();

                // locals: List<object> stack; object[] locals;
                var localStack = il.DeclareLocal(listType);
                var localLocals = il.DeclareLocal(typeof(object[]));

                // stack = new List<object>();
                il.Emit(System.Reflection.Emit.OpCodes.Newobj, listType.GetConstructor(Type.EmptyTypes));
                il.Emit(System.Reflection.Emit.OpCodes.Stloc, localStack);
                // locals = new object[localCount]
                int localCount = Math.Max(1, m.methodLocalVariableList?.Count ?? 0);
                il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, localCount);
                il.Emit(System.Reflection.Emit.OpCodes.Newarr, typeof(object));
                il.Emit(System.Reflection.Emit.OpCodes.Stloc, localLocals);

                foreach (var d in m.IRDataList)
                {
                    switch (d.opCode)
                    {
                        case EIROpCode.LoadConstInt32:
                            if (d.TryGetInt32(out var iv))
                            {
                                il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                                il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, iv);
                                il.Emit(System.Reflection.Emit.OpCodes.Box, typeof(int));
                                il.EmitCall(System.Reflection.Emit.OpCodes.Callvirt, listAdd, null);
                            }
                            break;
                        case EIROpCode.LoadConstDouble:
                            if (d.TryGetDouble(out var dv))
                            {
                                il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                                il.Emit(System.Reflection.Emit.OpCodes.Ldc_R8, dv);
                                il.Emit(System.Reflection.Emit.OpCodes.Box, typeof(double));
                                il.EmitCall(System.Reflection.Emit.OpCodes.Callvirt, listAdd, null);
                            }
                            break;
                        case EIROpCode.LoadConstFloat:
                            if (d.TryGetSingle(out var fv))
                            {
                                il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                                il.Emit(System.Reflection.Emit.OpCodes.Ldc_R4, fv);
                                il.Emit(System.Reflection.Emit.OpCodes.Box, typeof(float));
                                il.EmitCall(System.Reflection.Emit.OpCodes.Callvirt, listAdd, null);
                            }
                            break;
                        case EIROpCode.Add:
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Call, miBinaryAdd, null);
                            break;
                        case EIROpCode.Minus:
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Call, miBinarySub, null);
                            break;
                        case EIROpCode.Multiply:
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Call, miBinaryMul, null);
                            break;
                        case EIROpCode.Divide:
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Call, miBinaryDiv, null);
                            break;
                        case EIROpCode.LoadArgument:
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, d.index);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Callvirt, listAdd, null);
                            break;
                        case EIROpCode.LoadLocal:
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localLocals);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, d.index);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Callvirt, listAdd, null);
                            break;
                        case EIROpCode.StoreLocal:
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localLocals);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, d.index);
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Call, miPop, null);
                            il.Emit(System.Reflection.Emit.OpCodes.Stelem_Ref);
                            break;
                        case EIROpCode.Ret:
                            // return PopObject(stack)
                            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, localStack);
                            il.EmitCall(System.Reflection.Emit.OpCodes.Call, miPop, null);
                            il.Emit(System.Reflection.Emit.OpCodes.Ret);
                            break;
                        default:
                            // unsupported: emit comment via nop
                            il.Emit(System.Reflection.Emit.OpCodes.Nop);
                            break;
                    }
                }

                // default return null
                il.Emit(System.Reflection.Emit.OpCodes.Ldnull);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);

                var createdType = tb.CreateType();
                var mi = createdType.GetMethod("Run", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (mi != null)
                {
                    createdMethods[m.id] = mi;
                }
            }

            // If an entry method was found, create a Program.Main that calls it
            if (!string.IsNullOrEmpty(entryMethodId) && createdMethods.ContainsKey(entryMethodId))
            {
                var programTb = mb.DefineType("Program", System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.BeforeFieldInit);
                var mainMethod = programTb.DefineMethod("Main", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, typeof(int), new Type[] { typeof(string[]) });
                var il = mainMethod.GetILGenerator();
                // push null for args and call the entry Run
                il.Emit(System.Reflection.Emit.OpCodes.Ldnull);
                il.EmitCall(System.Reflection.Emit.OpCodes.Call, createdMethods[entryMethodId], null);
                // ignore return, return 0
                il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4_0);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);
                programTb.CreateType();
            }

            // Return a path-like indicator (dynamic assembly not saved to disk in .NET Core)
            return "[dynamic assembly created in-memory: " + assemblyName + "]";
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
