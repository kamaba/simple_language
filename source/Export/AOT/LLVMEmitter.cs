using System;
using System.IO;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export.AOT
{
    // Minimal LLVM emitter scaffold. This will be extended to walk IR and emit LLVM IR text.
    public class LLVMEmitter
    {
        public LLVMEmitter()
        {
        }

        // map VM EVMType to LLVM textual type
        private string LlvmTypeForEVMType(SimpleLanguage.VM.Runtime.EVMType t)
        {
            switch (t)
            {
                case SimpleLanguage.VM.Runtime.EVMType.Int32:
                case SimpleLanguage.VM.Runtime.EVMType.UInt32:
                    return "i32";
                case SimpleLanguage.VM.Runtime.EVMType.Int64:
                case SimpleLanguage.VM.Runtime.EVMType.UInt64:
                    return "i64";
                case SimpleLanguage.VM.Runtime.EVMType.Float32:
                    return "float";
                case SimpleLanguage.VM.Runtime.EVMType.Float64:
                case SimpleLanguage.VM.Runtime.EVMType.Num:
                    return "double";
                case SimpleLanguage.VM.Runtime.EVMType.Boolean:
                case SimpleLanguage.VM.Runtime.EVMType.Byte:
                case SimpleLanguage.VM.Runtime.EVMType.SByte:
                case SimpleLanguage.VM.Runtime.EVMType.Int16:
                case SimpleLanguage.VM.Runtime.EVMType.UInt16:
                    return "i32"; // promote small ints to i32 for simplicity
                default:
                    return "i8*"; // opaque object pointer
            }
        }

        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "fn_unknown";
            var sb = new System.Text.StringBuilder();
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c); else sb.Append('_');
            }
            return sb.ToString();
        }

        // Emit a whole IR method as LLVM IR text placeholder
        public void EmitMethod(IRMethod method, string outputPath)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("; Generated LLVM IR (auto-export)");
            sb.AppendLine($"; Method: {method.id}");

            string fnName = SanitizeName(method.id);
            int tmpId = 0;
            Func<string> tmp = () => "%t" + (tmpId++).ToString();

            // map IRMetaType to LLVM textual type (simple heuristics on irName)
            System.Func<IRMetaType, string> LlvmTypeForIRMetaType = (irmt) =>
            {
                if (irmt == null || irmt.irMetaClass == null) return "i8*";
                var nm = irmt.irMetaClass.irName ?? "";
                if (nm.IndexOf("Float64") >= 0 || nm.IndexOf("Double") >= 0 || nm.IndexOf("Num") >= 0) return "double";
                if (nm.IndexOf("Float32") >= 0 || nm.IndexOf("Single") >= 0) return "float";
                if (nm.IndexOf("Int32") >= 0 || nm.IndexOf("UInt32") >= 0 || nm.IndexOf("Int") >= 0) return "i32";
                if (nm.IndexOf("Int64") >= 0 || nm.IndexOf("UInt64") >= 0) return "i64";
                // fallback to pointer
                return "i8*";
            };
            // Build function signature: map arguments to double for now (numeric fast-path)
            int argCount = method.methodArgumentList?.Count ?? 0;
            bool hasReturn = (method.methodReturnVariableList?.Count ?? 0) > 0;
            // map return type
            string retType = "void";
            if (hasReturn)
            {
                var rmt = method.methodReturnVariableList[0].irMetaType;
                retType = LlvmTypeForIRMetaType(rmt);
            }
            var argListSb = new System.Text.StringBuilder();
            var currentMethodArgTypes = new System.Collections.Generic.List<string>();
            for (int ai = 0; ai < argCount; ai++)
            {
                if (ai > 0) argListSb.Append(", ");
                var atype = LlvmTypeForIRMetaType(method.methodArgumentList[ai].irMetaType);
                currentMethodArgTypes.Add(atype);
                argListSb.Append($"{atype} %arg{ai}");
            }
            sb.AppendLine($"define {retType} @{fnName}({argListSb}) {{");
            sb.AppendLine("entry:");

            int capacity = Math.Max(128, method.IRDataList.Count + 4);
            sb.AppendLine($"  %stack = alloca [{capacity} x double]");
            sb.AppendLine("  %sp = alloca i32");
            sb.AppendLine("  store i32 0, i32* %sp");
            // initialize stack with incoming arguments (push args in order)
            for (int ai = 0; ai < argCount; ai++)
            {
                // compute destination stack slot
                sb.AppendLine($"  %argidx{ai} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {ai}");
                var atype = currentMethodArgTypes[ai];
                // convert incoming arg to double for the value stack if needed
                if (atype == "double")
                {
                    sb.AppendLine($"  store double %arg{ai}, double* %argidx{ai}");
                }
                else if (atype == "float")
                {
                    var conv = tmp(); sb.AppendLine($"  {conv} = fpext float %arg{ai} to double");
                    sb.AppendLine($"  store double {conv}, double* %argidx{ai}");
                }
                else if (atype == "i32" || atype == "i64")
                {
                    var conv = tmp(); sb.AppendLine($"  {conv} = sitofp {atype} %arg{ai} to double");
                    sb.AppendLine($"  store double {conv}, double* %argidx{ai}");
                }
                else
                {
                    // pointer or unsupported: store 0.0 and leave a comment
                    sb.AppendLine($"  ; unsupported arg type {atype}, storing 0.0 as placeholder");
                    sb.AppendLine($"  store double 0.0, double* %argidx{ai}");
                }
            }
            if (argCount > 0)
            {
                sb.AppendLine($"  store i32 {argCount}, i32* %sp");
            }
            // locals area
            sb.AppendLine($"  %locals = alloca [{Math.Max(1, method.methodLocalVariableList.Count)} x double]");


            // build label map for IR labels and branch targets
            var labelMap = new System.Collections.Generic.Dictionary<int,string>();
            for (int i = 0; i < method.IRDataList.Count; i++)
            {
                var dd = method.IRDataList[i];
                if (dd.opCode == EIROpCode.Label)
                {
                    labelMap[i] = "L" + i.ToString();
                }
                else if (dd.opCode == EIROpCode.Br || dd.opCode == EIROpCode.BrFalse || dd.opCode == EIROpCode.BrTrue)
                {
                    // target index already resolved into dd.index by IRMethod.Parse
                    int tidx = dd.index;
                    if (tidx >= 0 && !labelMap.ContainsKey(tidx)) labelMap[tidx] = "L" + tidx.ToString();
                }
            }

            // helper to emit comment for each IR insn
            for (int i = 0; i < method.IRDataList.Count; i++)
            {
                var d = method.IRDataList[i];
                // emit label if present
                if (labelMap.ContainsKey(i))
                {
                    sb.AppendLine();
                    sb.AppendLine(labelMap[i] + ":");
                }
                sb.AppendLine($"  ; [{i}] {d.opCode} opValue={d.opValue}");
                switch (d.opCode)
                {
                    case EIROpCode.LoadArgument:
                        {
                            // push function argument with given index
                            int argIndex = d.index; // IRMetaVariable.index
                            var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                            var idx = tmp(); sb.AppendLine($"  {idx} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spv}");
                            sb.AppendLine($"  store double %arg{argIndex}, double* {idx}");
                            var spn = tmp(); sb.AppendLine($"  {spn} = add i32 {spv}, 1");
                            sb.AppendLine($"  store i32 {spn}, i32* %sp");
                        }
                        break;
                    case EIROpCode.CallStatic:
                        {
                            if (d.opValue is IRMethodCall imc && imc.irMethod != null)
                            {
                                string callee = SanitizeName(imc.irMethod.id);
                                int argc = imc.paramCount;
                                // Pop argc args from stack into temp regs (right-to-left)
                                var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                                var newsp = tmp(); sb.AppendLine($"  {newsp} = sub i32 {spv}, {argc}");
                                // load each arg into temps
                                var argTemps = new System.Collections.Generic.List<string>();
                                for (int ai = 0; ai < argc; ai++)
                                {
                                    var idx = tmp(); sb.AppendLine($"  {idx} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {newsp}  ");
                                    var off = tmp(); sb.AppendLine($"  {off} = add i32 {newsp}, {ai}");
                                    // compute element ptr with offset
                                    var gep = tmp(); sb.AppendLine($"  {gep} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {newsp}");
                                    var val = tmp(); sb.AppendLine($"  {val} = load double, double* {gep}");
                                    argTemps.Add(val);
                                }
                                // update sp
                                sb.AppendLine($"  store i32 {newsp}, i32* %sp");

                                // build call args string
                                var callArgsSb = new System.Text.StringBuilder();
                                for (int ai = 0; ai < argc; ai++)
                                {
                                    if (ai > 0) callArgsSb.Append(", ");
                                    callArgsSb.Append($"double {argTemps[ai]}");
                                }

                                bool calleeReturns = true; // assume double
                                if (calleeReturns)
                                {
                                    var callRes = tmp();
                                // use IRMethod signature if available
                                    var calleeMethod = imc.irMethod;
                                    string calleeRet = "double";
                                    if (calleeMethod.methodReturnVariableList != null && calleeMethod.methodReturnVariableList.Count > 0)
                                    {
                                        calleeRet = LlvmTypeForIRMetaType(calleeMethod.methodReturnVariableList[0].irMetaType);
                                    }
                                    var calleeArgTypes = new System.Collections.Generic.List<string>();
                                    for (int k = 0; k < calleeMethod.methodArgumentList.Count; k++)
                                    {
                                        var at = LlvmTypeForIRMetaType(calleeMethod.methodArgumentList[k].irMetaType);
                                        calleeArgTypes.Add(at);
                                    }
                                    // build typed arg list
                                    var typedArgsSb = new System.Text.StringBuilder();
                                    for (int ai = 0; ai < argc; ai++)
                                    {
                                        if (ai > 0) typedArgsSb.Append(", ");
                                        string t = ai < calleeArgTypes.Count ? calleeArgTypes[ai] : "double";
                                        typedArgsSb.Append($"{t} {argTemps[ai]}");
                                    }
                                    sb.AppendLine($"  {callRes} = call {calleeRet} @{callee}({typedArgsSb})");
                                    // push return value to stack at index newsp (convert to double if needed)
                                    var dst = tmp(); sb.AppendLine($"  {dst} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {newsp}");
                                    if (calleeRet == "double")
                                    {
                                        sb.AppendLine($"  store double {callRes}, double* {dst}");
                                    }
                                    else if (calleeRet == "float")
                                    {
                                        var ext = tmp(); sb.AppendLine($"  {ext} = fpext float {callRes} to double");
                                        sb.AppendLine($"  store double {ext}, double* {dst}");
                                    }
                                    else if (calleeRet == "i32" || calleeRet == "i64")
                                    {
                                        var conv = tmp(); sb.AppendLine($"  {conv} = sitofp {calleeRet} {callRes} to double");
                                        sb.AppendLine($"  store double {conv}, double* {dst}");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"  ; unsupported callee return type {calleeRet}, storing 0.0");
                                        sb.AppendLine($"  store double 0.0, double* {dst}");
                                    }
                                    var spn = tmp(); sb.AppendLine($"  {spn} = add i32 {newsp}, 1");
                                    sb.AppendLine($"  store i32 {spn}, i32* %sp");
                                }
                                else
                                {
                                    sb.AppendLine($"  call void @{callee}({callArgsSb})");
                                }
                            }
                            else
                            {
                                sb.AppendLine($"  ; callstatic: missing target");
                            }
                        }
                        break;
                    case EIROpCode.CallVirt:
                    case EIROpCode.CallDynamic:
                        {
                            if (d.opValue is IRMethodCall imc2 && imc2.irMethod != null)
                            {
                                // If target IRMethod is available, emit direct call similar to static
                                string callee2 = SanitizeName(imc2.irMethod.id);
                                int argc2 = imc2.paramCount;
                                var spv2 = tmp(); sb.AppendLine($"  {spv2} = load i32, i32* %sp");
                                var newsp2 = tmp(); sb.AppendLine($"  {newsp2} = sub i32 {spv2}, {argc2}");
                                var argTemps2 = new System.Collections.Generic.List<string>();
                                for (int ai = 0; ai < argc2; ai++)
                                {
                                    var gep = tmp(); sb.AppendLine($"  {gep} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {newsp2}");
                                    var val = tmp(); sb.AppendLine($"  {val} = load double, double* {gep}");
                                    argTemps2.Add(val);
                                }
                                sb.AppendLine($"  store i32 {newsp2}, i32* %sp");
                                var callArgsSb2 = new System.Text.StringBuilder();
                                for (int ai = 0; ai < argc2; ai++)
                                {
                                    if (ai > 0) callArgsSb2.Append(", ");
                                    callArgsSb2.Append($"double {argTemps2[ai]}");
                                }
                                var callRes2 = tmp();
                                sb.AppendLine($"  {callRes2} = call double @{callee2}({callArgsSb2})");
                                var dst2 = tmp(); sb.AppendLine($"  {dst2} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {newsp2}");
                                sb.AppendLine($"  store double {callRes2}, double* {dst2}");
                                var spn2 = tmp(); sb.AppendLine($"  {spn2} = add i32 {newsp2}, 1");
                                sb.AppendLine($"  store i32 {spn2}, i32* %sp");
                            }
                            else
                            {
                                if (d.opValue is IRMethodCall imc3)
                                    sb.AppendLine($"  ; dynamic/virt call placeholder: {imc3.irMethod?.id} paramCount={imc3.paramCount}");
                                else
                                    sb.AppendLine($"  ; call opcode placeholder: {d.opCode}");
                            }
                        }
                        break;
                    case EIROpCode.Br:
                        {
                            int target = d.index;
                            string lbl = labelMap.ContainsKey(target) ? labelMap[target] : "L" + target;
                            sb.AppendLine($"  br label %{lbl}");
                        }
                        break;
                    case EIROpCode.BrFalse:
                        {
                            int target = d.index;
                            string lbl = labelMap.ContainsKey(target) ? labelMap[target] : "L" + target;
                            // pop top value and branch if false
                            var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                            var spm = tmp(); sb.AppendLine($"  {spm} = sub i32 {spv}, 1");
                            var idx = tmp(); sb.AppendLine($"  {idx} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spm}");
                            var val = tmp(); sb.AppendLine($"  {val} = load double, double* {idx}");
                            var cond = tmp(); sb.AppendLine($"  {cond} = fcmp oeq double {val}, 0.0");
                            var bcond = tmp(); sb.AppendLine($"  {bcond} = zext i1 {cond} to i1");
                            sb.AppendLine($"  br i1 {cond}, label %{lbl}, label %" + (labelMap.ContainsKey(i+1) ? labelMap[i+1] : "L" + (i+1)) );
                            sb.AppendLine($"  store i32 {spm}, i32* %sp");
                        }
                        break;
                    case EIROpCode.BrTrue:
                        {
                            int target = d.index;
                            string lbl = labelMap.ContainsKey(target) ? labelMap[target] : "L" + target;
                            // pop top value and branch if true
                            var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                            var spm = tmp(); sb.AppendLine($"  {spm} = sub i32 {spv}, 1");
                            var idx = tmp(); sb.AppendLine($"  {idx} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spm}");
                            var val = tmp(); sb.AppendLine($"  {val} = load double, double* {idx}");
                            var cond = tmp(); sb.AppendLine($"  {cond} = fcmp one double {val}, 0.0");
                            sb.AppendLine($"  br i1 {cond}, label %{lbl}, label %" + (labelMap.ContainsKey(i+1) ? labelMap[i+1] : "L" + (i+1)) );
                            sb.AppendLine($"  store i32 {spm}, i32* %sp");
                        }
                        break;
                    case EIROpCode.Ceq:
                    case EIROpCode.Cne:
                    case EIROpCode.Cgt:
                    case EIROpCode.Cge:
                    case EIROpCode.Clt:
                    case EIROpCode.Cle:
                        {
                            // pop two values and compare (double)
                            var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                            var spm = tmp(); sb.AppendLine($"  {spm} = sub i32 {spv}, 1");
                            var idx_r = tmp(); sb.AppendLine($"  {idx_r} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spm}");
                            var val_r = tmp(); sb.AppendLine($"  {val_r} = load double, double* {idx_r}");
                            var spm2 = tmp(); sb.AppendLine($"  {spm2} = sub i32 {spv}, 2");
                            var idx_l = tmp(); sb.AppendLine($"  {idx_l} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spm2}");
                            var val_l = tmp(); sb.AppendLine($"  {val_l} = load double, double* {idx_l}");
                            var cmp = tmp();
                            switch (d.opCode)
                            {
                                case EIROpCode.Ceq: sb.AppendLine($"  {cmp} = fcmp oeq double {val_l}, {val_r}"); break;
                                case EIROpCode.Cne: sb.AppendLine($"  {cmp} = fcmp one double {val_l}, {val_r}"); break;
                                case EIROpCode.Cgt: sb.AppendLine($"  {cmp} = fcmp ogt double {val_l}, {val_r}"); break;
                                case EIROpCode.Cge: sb.AppendLine($"  {cmp} = fcmp oge double {val_l}, {val_r}"); break;
                                case EIROpCode.Clt: sb.AppendLine($"  {cmp} = fcmp olt double {val_l}, {val_r}"); break;
                                case EIROpCode.Cle: sb.AppendLine($"  {cmp} = fcmp ole double {val_l}, {val_r}"); break;
                            }
                            var boolz = tmp(); sb.AppendLine($"  {boolz} = zext i1 {cmp} to i32");
                            // store result as 0.0/1.0 in stack
                            var resd = tmp(); sb.AppendLine($"  {resd} = uitofp i32 {boolz} to double");
                            sb.AppendLine($"  store double {resd}, double* {idx_l}");
                            sb.AppendLine($"  store i32 {spm}, i32* %sp");
                        }
                        break;
                    case EIROpCode.LoadLocal:
                        {
                            int localIndex = d.index;
                            var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                            var src = tmp(); sb.AppendLine($"  {src} = getelementptr inbounds [{Math.Max(1, Math.Max(capacity, method.methodLocalVariableList.Count))} x double], [{Math.Max(1, Math.Max(capacity, method.methodLocalVariableList.Count))} x double]* %locals, i32 0, i32 {localIndex}");
                            var val = tmp(); sb.AppendLine($"  {val} = load double, double* {src}");
                            var dst = tmp(); sb.AppendLine($"  {dst} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spv}");
                            sb.AppendLine($"  store double {val}, double* {dst}");
                            var spn = tmp(); sb.AppendLine($"  {spn} = add i32 {spv}, 1");
                            sb.AppendLine($"  store i32 {spn}, i32* %sp");
                        }
                        break;
                    case EIROpCode.StoreLocal:
                        {
                            int localIndex = d.index;
                            var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                            var spm = tmp(); sb.AppendLine($"  {spm} = sub i32 {spv}, 1");
                            var src = tmp(); sb.AppendLine($"  {src} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spm}");
                            var val = tmp(); sb.AppendLine($"  {val} = load double, double* {src}");
                            var dst = tmp(); sb.AppendLine($"  {dst} = getelementptr inbounds [{Math.Max(1, Math.Max(capacity, method.methodLocalVariableList.Count))} x double], [{Math.Max(1, Math.Max(capacity, method.methodLocalVariableList.Count))} x double]* %locals, i32 0, i32 {localIndex}");
                            sb.AppendLine($"  store double {val}, double* {dst}");
                            sb.AppendLine($"  store i32 {spm}, i32* %sp");
                        }
                        break;
                    case EIROpCode.LoadConstDouble:
                        {
                            if (d.TryGetDouble(out var dv))
                            {
                                // load sp
                                var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                                // element ptr
                                var idx = tmp(); sb.AppendLine($"  {idx} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spv}");
                                // store constant
                                sb.AppendLine($"  store double {dv:R}, double* {idx}");
                                // increment sp
                                var spn = tmp(); sb.AppendLine($"  {spn} = add i32 {spv}, 1");
                                sb.AppendLine($"  store i32 {spn}, i32* %sp");
                            }
                        }
                        break;
                    case EIROpCode.LoadConstFloat:
                        {
                            if (d.TryGetSingle(out var fv))
                            {
                                // convert float to double by literal
                                var dv = ((double)fv).ToString("R");
                                var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                                var idx = tmp(); sb.AppendLine($"  {idx} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spv}");
                                sb.AppendLine($"  store double {dv}, double* {idx}");
                                var spn = tmp(); sb.AppendLine($"  {spn} = add i32 {spv}, 1");
                                sb.AppendLine($"  store i32 {spn}, i32* %sp");
                            }
                        }
                        break;
                    case EIROpCode.LoadConstInt32:
                        {
                            if (d.TryGetInt32(out var iv))
                            {
                                var dv = ((double)iv).ToString("R");
                                var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                                var idx = tmp(); sb.AppendLine($"  {idx} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spv}");
                                sb.AppendLine($"  store double {dv}, double* {idx}");
                                var spn = tmp(); sb.AppendLine($"  {spn} = add i32 {spv}, 1");
                                sb.AppendLine($"  store i32 {spn}, i32* %sp");
                            }
                        }
                        break;
                    case EIROpCode.Add:
                    case EIROpCode.Minus:
                    case EIROpCode.Multiply:
                    case EIROpCode.Divide:
                    case EIROpCode.Modulo:
                        {
                            // pop two values and push result (double)
                            var spv = tmp(); sb.AppendLine($"  {spv} = load i32, i32* %sp");
                            var spm = tmp(); sb.AppendLine($"  {spm} = sub i32 {spv}, 1");
                            var idx_r = tmp(); sb.AppendLine($"  {idx_r} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spm}");
                            var val_r = tmp(); sb.AppendLine($"  {val_r} = load double, double* {idx_r}");
                            var spm2 = tmp(); sb.AppendLine($"  {spm2} = sub i32 {spv}, 2");
                            var idx_l = tmp(); sb.AppendLine($"  {idx_l} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spm2}");
                            var val_l = tmp(); sb.AppendLine($"  {val_l} = load double, double* {idx_l}");
                            var res = tmp();
                            switch (d.opCode)
                            {
                                case EIROpCode.Add: sb.AppendLine($"  {res} = fadd double {val_l}, {val_r}"); break;
                                case EIROpCode.Minus: sb.AppendLine($"  {res} = fsub double {val_l}, {val_r}"); break;
                                case EIROpCode.Multiply: sb.AppendLine($"  {res} = fmul double {val_l}, {val_r}"); break;
                                case EIROpCode.Divide: sb.AppendLine($"  {res} = fdiv double {val_l}, {val_r}"); break;
                                case EIROpCode.Modulo: sb.AppendLine($"  {res} = frem double {val_l}, {val_r}"); break;
                            }
                            // store result into idx_l
                            sb.AppendLine($"  store double {res}, double* {idx_l}");
                            // update sp = sp -1
                            sb.AppendLine($"  store i32 {spm}, i32* %sp");
                        }
                        break;
                    case EIROpCode.Ret:
                        {
                            // load sp, top value and return
                            var spv2 = tmp(); sb.AppendLine($"  {spv2} = load i32, i32* %sp");
                            var idxres = tmp(); sb.AppendLine($"  {idxres} = getelementptr inbounds [{capacity} x double], [{capacity} x double]* %stack, i32 0, i32 {spv2}");
                            var topv = tmp(); sb.AppendLine($"  {topv} = load double, double* {idxres}");
                            sb.AppendLine($"  ret double {topv}");
                        }
                        break;
                    default:
                        {
                            // other opcodes: emit comment only
                        }
                        break;
                }
            }

            // default return 0.0 if no explicit return emitted
            sb.AppendLine("  ret double 0.0");
            sb.AppendLine("}");
            File.WriteAllText(outputPath, sb.ToString());
        }

        // Simple module emitter (multiple methods)
        public void EmitModule(IRMethod[] methods, string outputPath)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("; LLVM module placeholder generated by SimpleLanguage Exporter");
            sb.AppendLine("; Methods:");
            foreach (var m in methods)
            {
                sb.AppendLine($"; - {m.id} (ir count={m.IRDataList.Count})");
            }
            File.WriteAllText(outputPath, sb.ToString());
        }
    }
}
