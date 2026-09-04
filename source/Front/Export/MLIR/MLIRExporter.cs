//****************************************************************************
//  File:      MLIRExporter.cs
// ------------------------------------------------
//  Description: Export SimpleLanguage IR to MLIR (stage 2: real emission)
//  - Every @AOT() method becomes a func.func with the sl_value ABI:
//      (ctx: !llvm.ptr, args: !llvm.ptr, argc: i32, ret: !llvm.ptr) -> i64
//  - Stack-machine IR is linearized into SSA via per-block stack profiles.
//  - Failed methods are skipped and recorded in <name>_manifest.json
//    (status=failed), so aot.mlir always stays valid MLIR.
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export.MLIR
{
    public static class MLIRExporter
    {
        public sealed class ExportOptions
        {
            public bool RunToolchain { get; set; } = false;
            public string? NativeOutputPath { get; set; }
        }

        /// <summary>Per-method AOT manifest entry (module.json "aot.methods"
        /// and the standalone &lt;name&gt;_manifest.json share this shape).</summary>
        public sealed class AotMethodManifest
        {
            public string Id { get; set; } = "";
            public string Symbol { get; set; } = "";
            public string Status { get; set; } = "";
            public string? Reason { get; set; }
        }

        /// <summary>
        /// Successful per-method export info used by the caller to drive the
        /// aot.dll link step (stage 3).
        /// </summary>
        public sealed class AotExportResult
        {
            public string MlirPath { get; set; } = "";
            public IReadOnlyList<string> OkSymbols { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> FailedIds { get; set; } = Array.Empty<string>();
            /// <summary>True when at least one emitted method calls back into
            /// the CVM interpreter (stage-5 reverse bridge): the module then
            /// contains @sl_aot_bridge_init, which the host must export.</summary>
            public bool NeedsBridgeInit { get; set; }
            /// <summary>Per-method manifest entries (merged into module.json's
            /// "aot" field by MLIRExportManager).</summary>
            public IReadOnlyList<AotMethodManifest> Methods { get; set; } = Array.Empty<AotMethodManifest>();
            /// <summary>dll 文件名（由调用方在 stage-3 构建成功后回填；
            /// 用于单方法导出接口 TryExportMethods 的结果合并）。</summary>
            public string? DllFileName { get; set; }
        }

        /// <summary>
        /// 模块级 AOT 导出：把一批 @AOT() 候选方法写入同一个 aot.mlir（每模块一个 aot.dll）。
        /// 单个方法导出失败不影响其它方法：失败项记入 manifest（status=failed），
        /// 成功项照常生成 func.func。
        /// </summary>
        /// <param name="writeManifest">是否单独写 &lt;name&gt;_manifest.json
        /// （合并到 module.json 的场景可置 false，默认 true 兼容旧 CVM）。</param>
        public static AotExportResult ExportModuleToFile(IReadOnlyList<IRMethod> methods, string outputPath,
            bool writeManifest = true)
        {
            if (methods == null) throw new ArgumentNullException(nameof(methods));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            var manifest = new ManifestDoc { Mlir = Path.GetFileName(outputPath) };
            var usedSymbols = new HashSet<string>();
            var okSymbols = new List<string>();
            var failedIds = new List<string>();
            /* callee method-id -> module-level string global, shared by all
             * methods of this module (stage-5 reverse bridge). */
            var bridgeIds = new Dictionary<string, string>();

            var sb = new StringBuilder();
            sb.AppendLine("// SimpleLanguage AOT module");
            sb.AppendLine("!slv = !llvm.struct<(i32, i64)>");
            sb.AppendLine("module {");
            // MSVC CRT: the moment aot.obj contains real float arithmetic
            // (truncf/extf/addf), the CRT emits a reference to _fltused;
            // define it here so the aot.dll link never needs the CRT import.
            sb.AppendLine("  llvm.mlir.global constant @_fltused(39029 : i32) : i32");

            foreach (var m in methods)
            {
                if (m == null) continue;

                var entry = new ManifestMethod { Id = m.id };
                try
                {
                    var emitter = new MethodEmitter(m, usedSymbols, bridgeIds);
                    sb.Append(emitter.Emit());
                    entry.Symbol = emitter.Symbol;
                    entry.Status = "ok";
                    okSymbols.Add(emitter.Symbol);
                }
                catch (EmitFailException ex)
                {
                    entry.Status = "failed";
                    entry.Reason = ex.Message;
                    failedIds.Add(m.id);
                    sb.Append("  // FAILED aot method id: ").Append(m.id)
                      .Append(" -- ").Append(ex.Message.Replace('\n', ' '))
                      .AppendLine();
                }
                manifest.Methods.Add(entry);
            }

            if (bridgeIds.Count > 0)
            {
                sb.Append(EmitBridgePlumbing(bridgeIds));
            }

            sb.AppendLine("}");

            File.WriteAllText(outputPath, sb.ToString());
            if (writeManifest)
                WriteManifest(outputPath, manifest);

            return new AotExportResult
            {
                MlirPath = outputPath,
                OkSymbols = okSymbols,
                FailedIds = failedIds,
                NeedsBridgeInit = bridgeIds.Count > 0,
                Methods = manifest.Methods.Select(e => new AotMethodManifest
                {
                    Id = e.Id,
                    Symbol = e.Symbol,
                    Status = e.Status,
                    Reason = e.Reason,
                }).ToList(),
            };
        }

        /// <summary>
        /// Module-level stage-5 plumbing: the zero-initialized invoke-VM
        /// function pointer, the exported initializer the host calls right
        /// after LoadLibrary, and one string global per bridge callee id.
        /// </summary>
        private static string EmitBridgePlumbing(Dictionary<string, string> bridgeIds)
        {
            var sb = new StringBuilder();
            sb.Append("  // stage-5 reverse bridge: AOT -> CVM interpreter\n");
            sb.Append("  llvm.mlir.global internal @sl_g_invoke_vm_ptr() : !llvm.ptr\n");
            sb.Append("  func.func @sl_aot_bridge_init(%fn: !llvm.ptr) -> i32 {\n");
            sb.Append("    %gp = llvm.mlir.addressof @sl_g_invoke_vm_ptr : !llvm.ptr\n");
            sb.Append("    llvm.store %fn, %gp : !llvm.ptr, !llvm.ptr\n");
            sb.Append("    %zero = llvm.mlir.constant(0 : i32) : i32\n");
            sb.Append("    return %zero : i32\n");
            sb.Append("  }\n");
            foreach (var kv in bridgeIds)
            {
                int byteLen = Encoding.UTF8.GetByteCount(kv.Key);
                sb.Append("  llvm.mlir.global constant ").Append(kv.Value)
                  .Append('(').Append(EscapeCString(kv.Key))
                  .Append(") : !llvm.array<").Append(byteLen.ToString(CultureInfo.InvariantCulture))
                  .Append(" x i8>\n");
            }
            return sb.ToString();
        }

        /// <summary>C string literal escaping for llvm.mlir.global constants.</summary>
        private static string EscapeCString(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            sb.Append('"');
            foreach (var b in Encoding.UTF8.GetBytes(s))
            {
                switch (b)
                {
                    case (byte)'\\': sb.Append("\\\\"); break;
                    case (byte)'"': sb.Append("\\\""); break;
                    case (byte)'\n': sb.Append("\\0A"); break;
                    case (byte)'\r': sb.Append("\\0D"); break;
                    case (byte)'\t': sb.Append("\\09"); break;
                    default:
                        if (b < 0x20 || b > 0x7E)
                            sb.Append("\\").Append(b.ToString("X2", CultureInfo.InvariantCulture));
                        else
                            sb.Append((char)b);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        public static void ExportToFile(IRMethod method, string outputPath)
        {
            ExportModuleToFile(new[] { method }, outputPath);
        }

        public static void ExportAndOptionallyLower(IRMethod method, string mlirOutputPath, ExportOptions? options)
        {
            ExportToFile(method, mlirOutputPath);

            if (options?.RunToolchain == true)
            {
                if (string.IsNullOrWhiteSpace(options.NativeOutputPath))
                {
                    throw new ArgumentException("NativeOutputPath is required when RunToolchain is true", nameof(options));
                }

                MLIRToolchain.LowerToNative(mlirOutputPath, options.NativeOutputPath);
            }
        }

        private static string SanitizeSymbol(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        // Manifest (consumed by stage-4 CVM native registry)
        // ------------------------------------------------------------------

        private sealed class ManifestMethod
        {
            public string Id { get; set; } = "";
            public string Symbol { get; set; } = "";
            public string Status { get; set; } = "";
            public string? Reason { get; set; }
        }

        private sealed class ManifestDoc
        {
            public string Mlir { get; set; } = "";
            public List<ManifestMethod> Methods { get; set; } = new List<ManifestMethod>();
        }

        private static void WriteManifest(string mlirPath, ManifestDoc doc)
        {
            string manifestPath = ManifestPathOf(mlirPath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(doc, options));
        }

        /// <summary>
        /// Record the built dll name into the manifest next to the mlir file
        /// (consumed by the stage-4 CVM native registry).
        /// </summary>
        public static void SetManifestDll(string mlirPath, string dllFileName)
        {
            string manifestPath = ManifestPathOf(mlirPath);
            if (!File.Exists(manifestPath)) return;

            try
            {
                var root = JsonNode.Parse(File.ReadAllText(manifestPath));
                if (root == null) return;
                root["dll"] = dllFileName;
                File.WriteAllText(manifestPath,
                    root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (JsonException)
            {
                // leave manifest as-is on parse errors
            }
        }

        private static string ManifestPathOf(string mlirPath)
            => Path.Combine(
                Path.GetDirectoryName(mlirPath) ?? ".",
                Path.GetFileNameWithoutExtension(mlirPath) + "_manifest.json");

        // ------------------------------------------------------------------
        // Internal types
        // ------------------------------------------------------------------

        private sealed class EmitFailException : Exception
        {
            public EmitFailException(string message) : base(message) { }
        }

        /// <summary>
        /// Two-value abstraction: everything is carried as i64 bits.
        /// Array types carry the VMArray object pointer in the i64; the
        /// element kind is part of the enum so stack-profile equality
        /// keeps distinguishing e.g. ArrayI64 from ArrayF64.
        /// F32 mirrors the C VM's float32 evaluation domain (runtime_value_compute):
        /// a binary op with a Float32 left operand is computed in f64 and the
        /// result is rounded back to f32. An F32 value's i64 bits always hold
        /// the exact f64 widening of the f32 value, so bitcasts to f64 are
        /// always valid - the type tag only records the rounding domain.
        /// </summary>
        private enum SLType { I64, F64, F32, ArrayI32, ArrayI64, ArrayF64 }

        private static bool IsArrayType(SLType t)
            => t == SLType.ArrayI32 || t == SLType.ArrayI64 || t == SLType.ArrayF64;

        private static bool IsScalarType(SLType t)
            => t == SLType.I64 || t == SLType.F64 || t == SLType.F32;

        private static bool IsFloatType(SLType t)
            => t == SLType.F64 || t == SLType.F32;

        /// <summary>Element scalar type of an array slot type.</summary>
        private static SLType ElemTypeOf(SLType t) => t == SLType.ArrayF64 ? SLType.F64 : SLType.I64;

        /// <summary>C element width in bytes (VMArray.unit_length).</summary>
        private static int ElemWidthOf(SLType t) => t == SLType.ArrayI32 ? 4 : 8;

        /// <summary>
        /// A stack value. Name always refers to an i64-typed SSA value;
        /// for F64 values the i64 holds the double bit pattern.
        /// </summary>
        private readonly struct Val
        {
            public readonly string Name;
            public readonly SLType Type;
            public Val(string name, SLType type) { Name = name; Type = type; }
        }

        private static readonly HashSet<string> s_I64TypeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "UInt8", "Int8", "Int16", "UInt16", "Int32", "UInt32",
            "Int64", "UInt64", "Char", "Boolean",
        };

        private static readonly HashSet<EIROpCode> s_SupportedOps = new HashSet<EIROpCode>
        {
            EIROpCode.Nop,
            EIROpCode.Label,
            EIROpCode.LoadConstUInt8,
            EIROpCode.LoadConstInt8,
            EIROpCode.LoadConstInt16,
            EIROpCode.LoadConstUInt16,
            EIROpCode.LoadConstInt32,
            EIROpCode.LoadConstUInt32,
            EIROpCode.LoadConstInt64,
            EIROpCode.LoadConstUInt64,
            EIROpCode.LoadConstFloat32,
            EIROpCode.LoadConstFloat64,
            EIROpCode.LoadConstBoolean,
            EIROpCode.LoadArgument,
            EIROpCode.LoadLocal,
            EIROpCode.StoreLocal,
            EIROpCode.StoreArgument,
            EIROpCode.StoreReturn,
            EIROpCode.Ret,
            EIROpCode.Add,
            EIROpCode.Minus,
            EIROpCode.Multiply,
            EIROpCode.Divide,
            EIROpCode.Modulo,
            EIROpCode.InclusiveOr,
            EIROpCode.Combine,
            EIROpCode.XOR,
            EIROpCode.Shr,
            EIROpCode.Shi,
            EIROpCode.Not,
            EIROpCode.Neg,
            EIROpCode.Ceq,
            EIROpCode.Cne,
            EIROpCode.Cgt,
            EIROpCode.Cge,
            EIROpCode.Clt,
            EIROpCode.Cle,
            EIROpCode.And,
            EIROpCode.Or,
            EIROpCode.Br,
            EIROpCode.BrLabel,
            EIROpCode.BrFalse,
            EIROpCode.BrTrue,
            EIROpCode.Dup,
            EIROpCode.Pop,
            EIROpCode.Convert_I8,
            EIROpCode.Convert_SI8,
            EIROpCode.Convert_I16,
            EIROpCode.Convert_UI16,
            EIROpCode.Convert_I32,
            EIROpCode.Convert_UI32,
            EIROpCode.Convert_I64,
            EIROpCode.Convert_UI64,
            EIROpCode.Convert_R8,
            EIROpCode.CallStatic,    /* stage 5: reverse bridge into the CVM */
            EIROpCode.LoadArrayIndex,      /* constant index: [array] -> [element] */
            EIROpCode.LoadArrayIndexField, /* variable index: [array, index] -> [element] */
            EIROpCode.CallVirt,            /* inlined array getters (arr.length) */
        };

        /// <summary>
        /// Static slot typing: every argument/local slot maps to I64 or F64,
        /// resolved from the IRMetaVariable's IRMetaClass name.
        /// </summary>
        private sealed class SlotTable
        {
            public readonly Dictionary<int, SLType> ArgTypes = new Dictionary<int, SLType>();
            public readonly Dictionary<int, SLType> LocalTypes = new Dictionary<int, SLType>();
            public readonly List<int> ArgSlotList = new List<int>();
            public int ArgSlots = 1;
            public int LocalSlots = 1;
            public bool HasRet = false;
            public SLType RetType = SLType.I64;

            private readonly string m_OwnerId;

            private SlotTable(string ownerId) { m_OwnerId = ownerId; }

            public static SlotTable Resolve(IRMethod m)
            {
                var t = new SlotTable(m?.id ?? "");

                if (m.methodArgumentList != null)
                {
                    var slots = new SortedSet<int>();
                    foreach (var v in m.methodArgumentList)
                    {
                        if (v == null) continue;
                        int slot = v.index;
                        if (slot < 0) throw t.Fail($"argument '{v.name}' has negative slot index {slot}");
                        t.ArgTypes[slot] = ResolveSLType(t, v);
                        slots.Add(slot);
                    }
                    if (slots.Count > 0)
                    {
                        t.ArgSlotList.AddRange(slots);
                        t.ArgSlots = Math.Max(1, slots.Max + 1);
                    }
                }

                if (m.methodLocalVariableList != null)
                {
                    int max = -1;
                    foreach (var v in m.methodLocalVariableList)
                    {
                        if (v == null) continue;
                        int slot = v.index;
                        if (slot < 0) throw t.Fail($"local '{v.name}' has negative slot index {slot}");
                        t.LocalTypes[slot] = ResolveSLType(t, v);
                        if (slot > max) max = slot;
                    }
                    t.LocalSlots = Math.Max(1, max + 1);
                }

                if (m.methodReturnVariableList != null && m.methodReturnVariableList.Count > 0)
                {
                    if (m.methodReturnVariableList.Count > 1)
                        throw t.Fail("multiple return values are not supported");
                    var rv = m.methodReturnVariableList[0];
                    if (rv == null) throw t.Fail("null return variable");
                    if (rv.index != 0) throw t.Fail($"return variable '{rv.name}' must use slot 0 (got {rv.index})");
                    t.HasRet = true;
                    t.RetType = ResolveSLType(t, rv);
                    /* Array returns are rejected: the CVM side derives the
                     * return etype from the class-name hint, and
                     * "Array<Double>" would be misclassified as Float64
                     * (the "Double" substring matches first), turning the
                     * pointer bit pattern into garbage. The method stays on
                     * the interpreter instead. */
                    if (IsArrayType(t.RetType))
                        throw t.Fail("array return values are not supported");
                }

                return t;
            }

            public static SLType ResolveSLType(SlotTable t, IRMetaVariable v)
            {
                var cls = v.irMetaType?.irMetaClass;
                string n = cls?.irName;
                if (string.IsNullOrEmpty(n))
                    throw t.Fail($"cannot resolve IR type of variable '{v.name}'");
                int dot = n.LastIndexOf('.');
                string leaf = dot >= 0 ? n.Substring(dot + 1) : n;
                if (s_I64TypeNames.Contains(leaf)) return SLType.I64;
                if (leaf == "Double" || leaf == "Float64") return SLType.F64;
                SLType? arr = TryResolveArrayType(t, v, leaf);
                if (arr.HasValue) return arr.Value;
                throw t.Fail($"unsupported slot type '{n}' for variable '{v.name}'");
            }

            /// <summary>
            /// Array&lt;T&gt; slot types: the gen-template class name is
            /// "Array&lt;Elem&gt;"; the element IRMetaType sits in
            /// irMetaTypeList[0]. Only 4/8-byte scalar elements are
            /// supported (they match VMArray.unit_length).
            /// </summary>
            private static SLType? TryResolveArrayType(SlotTable t, IRMetaVariable v, string leaf)
            {
                if (leaf != "Array" && !leaf.StartsWith("Array<", StringComparison.Ordinal))
                    return null;

                string elemLeaf = null;
                var args = v.irMetaType?.irMetaTypeList;
                if (args != null && args.Count > 0)
                {
                    string en = args[0].irMetaClass?.irName;
                    if (!string.IsNullOrEmpty(en))
                    {
                        int ed = en.LastIndexOf('.');
                        elemLeaf = ed >= 0 ? en.Substring(ed + 1) : en;
                    }
                }
                if (elemLeaf == null)
                    throw t.Fail($"cannot resolve element type of array variable '{v.name}'");

                if (elemLeaf == "Double" || elemLeaf == "Float64") return SLType.ArrayF64;
                if (elemLeaf == "Int32" || elemLeaf == "UInt32") return SLType.ArrayI32;
                if (elemLeaf == "Int64" || elemLeaf == "UInt64") return SLType.ArrayI64;
                throw t.Fail($"unsupported array element type '{elemLeaf}' for variable '{v.name}'");
            }

            public SLType GetArgType(int slot)
            {
                if (ArgTypes.TryGetValue(slot, out var t)) return t;
                throw Fail($"unknown argument slot {slot}");
            }

            public SLType GetLocalType(int slot)
            {
                if (LocalTypes.TryGetValue(slot, out var t)) return t;
                throw Fail($"unknown local slot {slot}");
            }

            private EmitFailException Fail(string message)
                => new EmitFailException("[" + m_OwnerId + "] " + message);
        }

        /// <summary>A basic block: [Start, End) with successors and entry stack profile.</summary>
        private sealed class Block
        {
            public int Start;
            public int End;
            public List<int> Succs = new List<int>();
            public List<SLType>? Entry;      // null = not yet reached
            public bool Reachable;
        }

        // ------------------------------------------------------------------
        // MethodEmitter
        // ------------------------------------------------------------------

        private sealed class MethodEmitter
        {
            private const int ExitId = -1;

            private readonly IRMethod m_Method;
            private readonly SlotTable m_Slots;
            private readonly List<IRData> m_Code;
            private readonly int m_Count;
            private readonly List<Block> m_Blocks = new List<Block>();
            private readonly Dictionary<int, int> m_BlockOfPos = new Dictionary<int, int>();
            private readonly List<Val> m_Stack = new List<Val>();
            private int m_MaxDepth = 0;

            private readonly StringBuilder m_ConstSb = new StringBuilder(); // hoisted to entry, dominates all
            private readonly StringBuilder m_BodySb = new StringBuilder();
            private readonly Dictionary<long, string> m_I64Consts = new Dictionary<long, string>();
            private readonly Dictionary<int, string> m_IndexConsts = new Dictionary<int, string>();
            private readonly Dictionary<int, string> m_K32Consts = new Dictionary<int, string>();
            private bool m_F64ZeroEmitted;
            private string m_F64ZeroName = "";
            private bool m_ArrayDummyEmitted;
            private string m_ArrayDummyName = "";
            private int m_TmpCounter = 0;
            /* Stage-5 bridge: module-level callee-id -> string-global map
             * (shared across all methods of the module). */
            private readonly Dictionary<string, string> m_BridgeIds;

            public string Symbol { get; }

            public MethodEmitter(IRMethod method, HashSet<string> usedSymbols,
                Dictionary<string, string> bridgeIds)
            {
                m_Method = method ?? throw new ArgumentNullException(nameof(method));
                if (method.IRDataList == null) throw Fail("IRDataList is null");
                m_Code = method.IRDataList;
                m_Count = m_Code.Count;
                if (m_Count == 0) throw Fail("empty method body");
                m_Slots = SlotTable.Resolve(method);
                Symbol = MakeUniqueSymbol(method, usedSymbols);
                m_BridgeIds = bridgeIds ?? throw new ArgumentNullException(nameof(bridgeIds));
            }

            public string Emit()
            {
                CheckSupported();
                AnalyzeBlocks();
                ComputeProfiles();
                EmitEntry();
                for (int i = 0; i < m_Blocks.Count; i++)
                {
                    var b = m_Blocks[i];
                    if (b.Reachable) EmitBlock(b);
                }
                EmitExit();

                var sb = new StringBuilder();
                sb.Append("  // aot method id: ").Append(m_Method.id).Append('\n');
                sb.Append("  func.func @").Append(Symbol)
                  .Append("(%ctx: !llvm.ptr, %args: !llvm.ptr, %argc: i32, %ret: !llvm.ptr) -> i64 {\n");
                sb.Append(m_ConstSb);
                sb.Append(m_BodySb);
                sb.Append("  }\n\n");
                return sb.ToString();
            }

            private static string MakeUniqueSymbol(IRMethod m, HashSet<string> used)
            {
                string id = string.IsNullOrEmpty(m.id) ? m.onlyFunctionName : m.id;
                string baseName = "sl_aot_" + SanitizeSymbol(id);
                string name = baseName;
                int suffix = 2;
                while (!used.Add(name))
                {
                    name = baseName + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                    suffix++;
                }
                return name;
            }

            private static bool IsTerminator(EIROpCode op)
                => op == EIROpCode.Br || op == EIROpCode.BrLabel
                || op == EIROpCode.BrFalse || op == EIROpCode.BrTrue
                || op == EIROpCode.Ret;

            private void CheckSupported()
            {
                for (int i = 0; i < m_Count; i++)
                {
                    var ir = m_Code[i];
                    if (ir == null) throw Fail($"[{i}] null instruction");
                    if (!s_SupportedOps.Contains(ir.opCode))
                        throw Fail($"[{i}] unsupported opcode '{ir.opCode}'");
                }
            }

            // ---- CFG construction ------------------------------------------------

            private void AnalyzeBlocks()
            {
                var leaders = new SortedSet<int> { 0 };

                for (int i = 0; i < m_Count; i++)
                {
                    var ir = m_Code[i];
                    if (ir.opCode == EIROpCode.Label) leaders.Add(i);
                    if (IsTerminator(ir.opCode) && i + 1 < m_Count) leaders.Add(i + 1);
                    if (ir.opCode == EIROpCode.Br || ir.opCode == EIROpCode.BrLabel
                        || ir.opCode == EIROpCode.BrFalse || ir.opCode == EIROpCode.BrTrue)
                    {
                        int target = ir.index;
                        if (target < 0 || target >= m_Count)
                            throw Fail($"[{i}] {ir.opCode} target out of range: {target}");
                        leaders.Add(target);
                    }
                }

                foreach (int start in leaders)
                {
                    if (m_Blocks.Count > 0) m_Blocks[m_Blocks.Count - 1].End = start;
                    m_Blocks.Add(new Block { Start = start, End = m_Count });
                    m_BlockOfPos[start] = m_Blocks.Count - 1;
                }

                foreach (var b in m_Blocks)
                {
                    var last = m_Code[b.End - 1];
                    switch (last.opCode)
                    {
                        case EIROpCode.Br:
                        case EIROpCode.BrLabel:
                            b.Succs.Add(m_BlockOfPos[last.index]);
                            break;
                        case EIROpCode.BrTrue:
                        case EIROpCode.BrFalse:
                            if (b.End >= m_Count)
                                throw Fail($"[{b.End - 1}] conditional branch at end of method");
                            b.Succs.Add(m_BlockOfPos[last.index]);
                            b.Succs.Add(m_BlockOfPos[b.End]);
                            break;
                        case EIROpCode.Ret:
                            b.Succs.Add(ExitId);
                            break;
                        default:
                            if (b.End >= m_Count) b.Succs.Add(ExitId);
                            else b.Succs.Add(m_BlockOfPos[b.End]);
                            break;
                    }
                }
            }

            // ---- stack profile analysis -------------------------------------------

            private void ComputeProfiles()
            {
                var work = new Queue<Block>();
                var first = m_Blocks[0];
                first.Entry = new List<SLType>();
                first.Reachable = true;
                work.Enqueue(first);

                while (work.Count > 0)
                {
                    var b = work.Dequeue();
                    var sim = new List<SLType>(b.Entry!);

                    for (int i = b.Start; i < b.End; i++)
                    {
                        var ir = m_Code[i];
                        if (IsTerminator(ir.opCode))
                        {
                            if (ir.opCode == EIROpCode.BrFalse || ir.opCode == EIROpCode.BrTrue)
                                ProfilePop(sim, i);
                            break;
                        }
                        StepProfile(sim, ir, i);
                        if (sim.Count > m_MaxDepth) m_MaxDepth = sim.Count;
                    }

                    foreach (int s in b.Succs)
                    {
                        if (s == ExitId) continue;
                        var succ = m_Blocks[s];
                        if (!succ.Reachable)
                        {
                            succ.Reachable = true;
                            succ.Entry = new List<SLType>(sim);
                            work.Enqueue(succ);
                        }
                        else if (!ProfileEquals(succ.Entry!, sim))
                        {
                            throw Fail($"inconsistent stack profile at block ^b{succ.Start}");
                        }
                    }
                }
            }

            private static bool ProfileEquals(List<SLType> a, List<SLType> b)
            {
                if (a.Count != b.Count) return false;
                for (int i = 0; i < a.Count; i++)
                    if (a[i] != b[i]) return false;
                return true;
            }

            private SLType ProfilePop(List<SLType> sim, int pos)
            {
                if (sim.Count < 1) throw Fail($"[{pos}] stack underflow");
                var t = sim[sim.Count - 1];
                sim.RemoveAt(sim.Count - 1);
                return t;
            }

            private void StepProfile(List<SLType> sim, IRData ir, int pos)
            {
                switch (ir.opCode)
                {
                    case EIROpCode.Nop:
                    case EIROpCode.Label:
                        return;

                    case EIROpCode.LoadConstUInt8:
                    case EIROpCode.LoadConstInt8:
                    case EIROpCode.LoadConstInt16:
                    case EIROpCode.LoadConstUInt16:
                    case EIROpCode.LoadConstInt32:
                    case EIROpCode.LoadConstUInt32:
                    case EIROpCode.LoadConstInt64:
                    case EIROpCode.LoadConstUInt64:
                    case EIROpCode.LoadConstBoolean:
                        sim.Add(SLType.I64);
                        return;

                    case EIROpCode.LoadConstFloat32:
                        sim.Add(SLType.F32);
                        return;

                    case EIROpCode.LoadConstFloat64:
                        sim.Add(SLType.F64);
                        return;

                    case EIROpCode.LoadArgument:
                        sim.Add(m_Slots.GetArgType(ir.index));
                        return;

                    case EIROpCode.LoadLocal:
                        sim.Add(m_Slots.GetLocalType(ir.index));
                        return;

                    case EIROpCode.StoreLocal:
                    case EIROpCode.StoreArgument:
                    case EIROpCode.StoreReturn:
                        ProfilePop(sim, pos);
                        return;

                    case EIROpCode.Add:
                    case EIROpCode.Minus:
                    case EIROpCode.Multiply:
                    case EIROpCode.Divide:
                    case EIROpCode.Modulo:
                    {
                        var bt = ProfilePop(sim, pos);
                        var at = ProfilePop(sim, pos);
                        if (!IsScalarType(at) || !IsScalarType(bt))
                            throw Fail($"[{pos}] arithmetic op on array operand");
                        // VM float rule (runtime_value_compute): compute in f64;
                        // result keeps f64 only when the LEFT operand is f64,
                        // otherwise it is rounded to f32.
                        if (IsFloatType(at) || IsFloatType(bt))
                            sim.Add(at == SLType.F64 ? SLType.F64 : SLType.F32);
                        else
                            sim.Add(SLType.I64);
                        return;
                    }

                    case EIROpCode.InclusiveOr:
                    case EIROpCode.Combine:
                    case EIROpCode.XOR:
                    case EIROpCode.Shr:
                    case EIROpCode.Shi:
                    {
                        var bt = ProfilePop(sim, pos);
                        var at = ProfilePop(sim, pos);
                        if (IsFloatType(at) || IsFloatType(bt))
                            throw Fail($"[{pos}] bitwise/shift op on float operand");
                        if (IsArrayType(at) || IsArrayType(bt))
                            throw Fail($"[{pos}] bitwise/shift op on array operand");
                        sim.Add(SLType.I64);
                        return;
                    }

                    case EIROpCode.Ceq:
                    case EIROpCode.Cne:
                    case EIROpCode.Cgt:
                    case EIROpCode.Cge:
                    case EIROpCode.Clt:
                    case EIROpCode.Cle:
                    case EIROpCode.And:
                    case EIROpCode.Or:
                    {
                        var bt = ProfilePop(sim, pos);
                        var at = ProfilePop(sim, pos);
                        if (IsArrayType(at) || IsArrayType(bt))
                            throw Fail($"[{pos}] comparison/logical op on array operand");
                        sim.Add(SLType.I64);
                        return;
                    }

                    case EIROpCode.Not:
                    {
                        var t = ProfilePop(sim, pos);
                        if (IsArrayType(t))
                            throw Fail($"[{pos}] Not on array operand");
                        sim.Add(SLType.I64);
                        return;
                    }

                    case EIROpCode.Neg:
                    {
                        var t = ProfilePop(sim, pos);
                        if (IsArrayType(t))
                            throw Fail($"[{pos}] Neg on array operand");
                        sim.Add(t);
                        return;
                    }

                    case EIROpCode.Convert_I8:
                    case EIROpCode.Convert_SI8:
                    case EIROpCode.Convert_I16:
                    case EIROpCode.Convert_UI16:
                    case EIROpCode.Convert_I32:
                    case EIROpCode.Convert_UI32:
                    case EIROpCode.Convert_I64:
                    case EIROpCode.Convert_UI64:
                    {
                        var t = ProfilePop(sim, pos);
                        if (IsArrayType(t))
                            throw Fail($"[{pos}] integer convert on array operand");
                        sim.Add(SLType.I64);
                        return;
                    }

                    case EIROpCode.Convert_R8:
                    {
                        var t = ProfilePop(sim, pos);
                        if (IsArrayType(t))
                            throw Fail($"[{pos}] f64 convert on array operand");
                        sim.Add(SLType.F64);
                        return;
                    }

                    case EIROpCode.Dup:
                    {
                        int n = ReadCount(ir);
                        if (n < 1) n = 1;
                        if (n > sim.Count) throw Fail($"[{pos}] Dup underflow (need {n}, have {sim.Count})");
                        int baseIdx = sim.Count - n;
                        for (int k = 0; k < n; k++) sim.Add(sim[baseIdx + k]);
                        return;
                    }

                    case EIROpCode.Pop:
                    {
                        int n = ReadCount(ir);
                        if (n < 0) n = 0;
                        if (n > sim.Count) throw Fail($"[{pos}] Pop underflow (need {n}, have {sim.Count})");
                        sim.RemoveRange(sim.Count - n, n);
                        return;
                    }

                    case EIROpCode.CallStatic:
                    {
                        var ci = ResolveCallee(ir, pos);
                        for (int k = 0; k < ci.Argc; k++) ProfilePop(sim, pos);
                        if (ci.HasRet) sim.Add(ci.RetType);
                        return;
                    }

                    case EIROpCode.LoadArrayIndex:
                    {
                        // constant index: [array] -> [element]
                        var at = ProfilePop(sim, pos);
                        if (!IsArrayType(at))
                            throw Fail($"[{pos}] LoadArrayIndex receiver is {at}, expected an array");
                        sim.Add(ElemTypeOf(at));
                        return;
                    }

                    case EIROpCode.LoadArrayIndexField:
                    {
                        // variable index: [array, index] -> [element]
                        var it = ProfilePop(sim, pos);
                        var at = ProfilePop(sim, pos);
                        if (!IsArrayType(at))
                            throw Fail($"[{pos}] LoadArrayIndexField receiver is {at}, expected an array");
                        if (it != SLType.I64)
                            throw Fail($"[{pos}] LoadArrayIndexField index is {it}, expected an integer");
                        sim.Add(ElemTypeOf(at));
                        return;
                    }

                    case EIROpCode.CallVirt:
                    {
                        // Only array trivial getters (arr.length) are inlined.
                        var vi = ResolveVirtCallee(ir, pos);
                        for (int k = 0; k < vi.Argc; k++) ProfilePop(sim, pos);
                        var rt = ProfilePop(sim, pos);
                        if (!IsArrayType(rt))
                            throw Fail($"[{pos}] CallVirt receiver is {rt}, only array getters are inlinable");
                        if (!IsTrivialArrayLengthGetter(vi.Method))
                            throw Fail($"[{pos}] CallVirt to '{vi.Name}' is not an inlinable array getter (id={vi.Method?.id ?? "<null>"} body=[{DumpIRShape(vi.Method)}])");
                        sim.Add(SLType.I64);
                        return;
                    }

                    default:
                        throw Fail($"[{pos}] unsupported opcode '{ir.opCode}'");
                }
            }

            /// <summary>
            /// Dup/Pop payload is a plain little-endian int32 count (no index prefix:
            /// they are not in IRData.UsesIndex). Missing/short payload defaults to 1.
            /// </summary>
            private static int ReadCount(IRData ir)
            {
                if (ir.Payload == null || ir.Payload.Length < 4) return 1;
                return BitConverter.ToInt32(ir.Payload, 0);
            }

            // ---- emission: entry ---------------------------------------------------

            private string StackType => "memref<" + Math.Max(1, m_MaxDepth).ToString(CultureInfo.InvariantCulture) + "xi64>";

            private void EmitEntry()
            {
                EmitBody("    %stack = memref.alloca() : {0}", StackType);
                EmitBody("    %locals = memref.alloca() : memref<{0}xi64>", m_Slots.LocalSlots);
                EmitBody("    %argmem = memref.alloca() : memref<{0}xi64>", m_Slots.ArgSlots);
                EmitBody("    %retval = memref.alloca() : memref<1xi64>");

                // argument prologue: args[slot] -> %argmem[slot] (i64 bit pattern only)
                foreach (int slot in m_Slots.ArgSlotList)
                {
                    string c = CI64(slot);
                    string p = NV();
                    EmitBody("    {0} = llvm.getelementptr %args[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, !slv", p, c);
                    string s = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> !slv", s, p);
                    string d = NV();
                    EmitBody("    {0} = llvm.extractvalue {1}[1] : !slv", d, s);
                    EmitBody("    memref.store {0}, %argmem[{1}] : memref<{2}xi64>", d, CIDX(slot), m_Slots.ArgSlots);
                }

                // locals zero-init + retval zero-init
                string zero = CI64(0);
                for (int i = 0; i < m_Slots.LocalSlots; i++)
                    EmitBody("    memref.store {0}, %locals[{1}] : memref<{2}xi64>", zero, CIDX(i), m_Slots.LocalSlots);
                EmitBody("    memref.store {0}, %retval[{1}] : memref<1xi64>", zero, CIDX(0));

                EmitBody("    cf.br ^b{0}", m_Blocks[0].Start);
            }

            // ---- emission: blocks -------------------------------------------------

            private void EmitBlock(Block b)
            {
                m_Stack.Clear();
                EmitBody("  ^b{0}:", b.Start);

                // reload the entry stack profile from %stack
                var entry = b.Entry!;
                for (int i = 0; i < entry.Count; i++)
                {
                    string v = NV();
                    EmitBody("    {0} = memref.load %stack[{1}] : {2}", v, CIDX(i), StackType);
                    m_Stack.Add(new Val(v, entry[i]));
                }

                for (int i = b.Start; i < b.End; i++)
                {
                    var ir = m_Code[i];
                    EmitBody("    // [{0}] {1}", i, ir.opCode);
                    if (IsTerminator(ir.opCode))
                    {
                        EmitTerminator(b, ir, i);
                        return;
                    }
                    EmitInstruction(ir, i);
                }

                // fell off the end of the block: spill and continue
                SpillAll();
                if (b.End >= m_Count) EmitBody("    cf.br ^exit");
                else EmitBody("    cf.br ^b{0}", b.End);
            }

            private void EmitTerminator(Block b, IRData ir, int pos)
            {
                switch (ir.opCode)
                {
                    case EIROpCode.Br:
                    case EIROpCode.BrLabel:
                        SpillAll();
                        EmitBody("    cf.br ^b{0}", ir.index);
                        return;

                    case EIROpCode.BrTrue:
                    {
                        Val cond = Pop(pos);
                        string t = Truthy(cond);
                        SpillAll();
                        EmitBody("    cf.cond_br {0}, ^b{1}, ^b{2}", t, ir.index, b.End);
                        return;
                    }

                    case EIROpCode.BrFalse:
                    {
                        Val cond = Pop(pos);
                        string t = Truthy(cond);
                        SpillAll();
                        EmitBody("    cf.cond_br {0}, ^b{1}, ^b{2}", t, b.End, ir.index);
                        return;
                    }

                    case EIROpCode.Ret:
                        EmitBody("    cf.br ^exit");
                        return;

                    default:
                        throw Fail($"[{pos}] '{ir.opCode}' is not a terminator");
                }
            }

            // ---- emission: exit ---------------------------------------------------

            private void EmitExit()
            {
                EmitBody("  ^exit:");
                string r = NV();
                EmitBody("    {0} = memref.load %retval[{1}] : memref<1xi64>", r, CIDX(0));
                int kind = m_Slots.HasRet && m_Slots.RetType == SLType.F64 ? 1 : 0;
                string u0 = NV();
                EmitBody("    {0} = llvm.mlir.undef : !slv", u0);
                string u1 = NV();
                EmitBody("    {0} = llvm.insertvalue {1}, {2}[0] : !slv", u1, CK32(kind), u0);
                string u2 = NV();
                EmitBody("    {0} = llvm.insertvalue {1}, {2}[1] : !slv", u2, r, u1);
                EmitBody("    llvm.store {0}, %ret : !slv, !llvm.ptr", u2);
                EmitBody("    return {0} : i64", CI64(0));
            }

            /// <summary>Persist the virtual stack into %stack (0 = bottom).</summary>
            private void SpillAll()
            {
                for (int i = 0; i < m_Stack.Count; i++)
                    EmitBody("    memref.store {0}, %stack[{1}] : {2}", m_Stack[i].Name, CIDX(i), StackType);
            }

            // ---- emission: instructions -------------------------------------------

            private void EmitInstruction(IRData ir, int pos)
            {
                switch (ir.opCode)
                {
                    case EIROpCode.Nop:
                    case EIROpCode.Label:
                        return;

                    // integer-like constants: all carried as i64 (no instruction)
                    case EIROpCode.LoadConstUInt8:
                    case EIROpCode.LoadConstInt8:
                    case EIROpCode.LoadConstInt16:
                    case EIROpCode.LoadConstUInt16:
                    case EIROpCode.LoadConstInt32:
                    case EIROpCode.LoadConstUInt32:
                    case EIROpCode.LoadConstInt64:
                    case EIROpCode.LoadConstUInt64:
                    case EIROpCode.LoadConstBoolean:
                        m_Stack.Add(new Val(CI64(ConstI64(ir)), SLType.I64));
                        return;

                    // float constants: both are carried as the exact f64 bit
                    // pattern (LoadConstFloat32 widens exactly, matching the
                    // VM's promote-on-use). The TYPE TAG differs: an f32
                    // constant keeps the F32 rounding domain so downstream
                    // arithmetic replicates the VM's left-etype rounding.
                    case EIROpCode.LoadConstFloat32:
                        m_Stack.Add(new Val(CI64(ConstF64Bits(ir)), SLType.F32));
                        return;

                    case EIROpCode.LoadConstFloat64:
                        m_Stack.Add(new Val(CI64(ConstF64Bits(ir)), SLType.F64));
                        return;

                    case EIROpCode.LoadArgument:
                    {
                        SLType ty = m_Slots.GetArgType(ir.index);
                        string v = NV();
                        EmitBody("    {0} = memref.load %argmem[{1}] : memref<{2}xi64>", v, CIDX(ir.index), m_Slots.ArgSlots);
                        m_Stack.Add(new Val(v, ty));
                        return;
                    }

                    case EIROpCode.LoadLocal:
                    {
                        SLType ty = m_Slots.GetLocalType(ir.index);
                        string v = NV();
                        EmitBody("    {0} = memref.load %locals[{1}] : memref<{2}xi64>", v, CIDX(ir.index), m_Slots.LocalSlots);
                        m_Stack.Add(new Val(v, ty));
                        return;
                    }

                    case EIROpCode.StoreLocal:
                    {
                        Val v = Pop(pos);
                        string c = CoerceStore(v, m_Slots.GetLocalType(ir.index), $"StoreLocal {ir.index}");
                        EmitBody("    memref.store {0}, %locals[{1}] : memref<{2}xi64>", c, CIDX(ir.index), m_Slots.LocalSlots);
                        return;
                    }

                    case EIROpCode.StoreArgument:
                    {
                        Val v = Pop(pos);
                        string c = CoerceStore(v, m_Slots.GetArgType(ir.index), $"StoreArgument {ir.index}");
                        EmitBody("    memref.store {0}, %argmem[{1}] : memref<{2}xi64>", c, CIDX(ir.index), m_Slots.ArgSlots);
                        return;
                    }

                    case EIROpCode.StoreReturn:
                    {
                        if (ir.index != 0)
                            throw Fail($"[{pos}] StoreReturn with index {ir.index} (only slot 0 is supported)");
                        if (!m_Slots.HasRet)
                            throw Fail($"[{pos}] StoreReturn in a method without a return value");
                        Val v = Pop(pos);
                        string c = CoerceStore(v, m_Slots.RetType, "StoreReturn");
                        EmitBody("    memref.store {0}, %retval[{1}] : memref<1xi64>", c, CIDX(0));
                        return;
                    }

                    case EIROpCode.Add:
                    case EIROpCode.Minus:
                    case EIROpCode.Multiply:
                    case EIROpCode.Divide:
                    case EIROpCode.Modulo:
                        EmitArith(ir.opCode, pos);
                        return;

                    case EIROpCode.InclusiveOr:
                    case EIROpCode.Combine:
                    case EIROpCode.XOR:
                    case EIROpCode.Shr:
                    case EIROpCode.Shi:
                        EmitBitwise(ir.opCode, pos);
                        return;

                    case EIROpCode.And:
                    case EIROpCode.Or:
                        EmitLogical(ir.opCode, pos);
                        return;

                    case EIROpCode.Ceq:
                    case EIROpCode.Cne:
                    case EIROpCode.Cgt:
                    case EIROpCode.Cge:
                    case EIROpCode.Clt:
                    case EIROpCode.Cle:
                        EmitCompare(ir.opCode, pos);
                        return;

                    case EIROpCode.Not:
                        EmitNot(pos);
                        return;

                    case EIROpCode.Neg:
                        EmitNeg(pos);
                        return;

                    // Convert semantics (matches C VM VM_CASE_CONVERT_OP):
                    // I8=(8,unsigned) SI8=(8,signed) I16=(16,signed) UI16=(16,unsigned)
                    // I32=(32,signed) UI32=(32,unsigned) I64/UI64=(64,-) R8=F64
                    case EIROpCode.Convert_I8: EmitConvert(pos, 8, false); return;
                    case EIROpCode.Convert_SI8: EmitConvert(pos, 8, true); return;
                    case EIROpCode.Convert_I16: EmitConvert(pos, 16, true); return;
                    case EIROpCode.Convert_UI16: EmitConvert(pos, 16, false); return;
                    case EIROpCode.Convert_I32: EmitConvert(pos, 32, true); return;
                    case EIROpCode.Convert_UI32: EmitConvert(pos, 32, false); return;
                    case EIROpCode.Convert_I64:
                    case EIROpCode.Convert_UI64:
                        EmitConvert(pos, 64, false);
                        return;
                    case EIROpCode.Convert_R8:
                        EmitConvertR8(pos);
                        return;

                    case EIROpCode.Dup:
                    {
                        int n = ReadCount(ir);
                        if (n < 1) n = 1;
                        if (n > m_Stack.Count) throw Fail($"[{pos}] Dup underflow (need {n}, have {m_Stack.Count})");
                        int baseIdx = m_Stack.Count - n;
                        for (int k = 0; k < n; k++) m_Stack.Add(m_Stack[baseIdx + k]);
                        return;
                    }

                    case EIROpCode.Pop:
                    {
                        int n = ReadCount(ir);
                        if (n < 0) n = 0;
                        if (n > m_Stack.Count) throw Fail($"[{pos}] Pop underflow (need {n}, have {m_Stack.Count})");
                        m_Stack.RemoveRange(m_Stack.Count - n, n);
                        return;
                    }

                    case EIROpCode.CallStatic:
                        EmitCallStatic(ir, pos);
                        return;

                    case EIROpCode.LoadArrayIndex:
                    case EIROpCode.LoadArrayIndexField:
                        EmitArrayLoad(ir, pos);
                        return;

                    case EIROpCode.CallVirt:
                        EmitCallVirt(ir, pos);
                        return;

                    default:
                        throw Fail($"[{pos}] unsupported opcode '{ir.opCode}'");
                }
            }

            // ---- CallStatic: stage-5 reverse bridge --------------------------------
            //
            // Calls an interpreter-side (non-AOT) method through the host
            // function pointer injected via sl_aot_bridge_init:
            //   int64 fn(void* ctx, const char* method_id,
            //            SLAotValue* args, int32 argc, SLAotValue* ret)
            // The ctx passed at the outer boundary is the VM*, so the host
            // bridge can push args and run the callee with
            // vm_execute_method_by_id.

            private readonly struct CalleeInfo
            {
                public readonly string Id;
                public readonly int Argc;
                public readonly bool HasRet;
                public readonly SLType RetType;
                public readonly SLType[] ArgTypes;
                public CalleeInfo(string id, int argc, bool hasRet, SLType retType, SLType[] argTypes)
                {
                    Id = id; Argc = argc; HasRet = hasRet; RetType = retType; ArgTypes = argTypes;
                }
            }

            private CalleeInfo ResolveCallee(IRData ir, int pos)
            {
                if (!(ir.opValue is IRMethodCall imc) || imc.irMethod == null)
                    throw Fail($"[{pos}] CallStatic without resolvable method metadata");

                IRMethod callee = imc.irMethod;
                int argc = imc.paramCount;
                if (argc < 0)
                    throw Fail($"[{pos}] CallStatic with negative paramCount {argc}");
                string calleeId = callee.id;
                if (string.IsNullOrEmpty(calleeId))
                    throw Fail($"[{pos}] CallStatic callee has no method id");

                if (callee.methodReturnVariableList != null && callee.methodReturnVariableList.Count > 1)
                    throw Fail($"[{pos}] callee '{calleeId}' has multiple return values");
                bool hasRet = callee.methodReturnVariableList != null
                    && callee.methodReturnVariableList.Count == 1;
                SLType retType = SLType.I64;
                if (hasRet)
                {
                    var rv = callee.methodReturnVariableList[0];
                    if (rv == null)
                        throw Fail($"[{pos}] callee '{calleeId}' has null return variable");
                    retType = SlotTable.ResolveSLType(m_Slots, rv);
                }

                var argTypes = new SLType[argc];
                if (argc > 0)
                {
                    if (callee.methodArgumentList == null)
                        throw Fail($"[{pos}] callee '{calleeId}' has no argument list");
                    for (int slot = 0; slot < argc; slot++)
                    {
                        IRMetaVariable av = null;
                        foreach (var v in callee.methodArgumentList)
                        {
                            if (v != null && v.index == slot) { av = v; break; }
                        }
                        if (av == null)
                            throw Fail($"[{pos}] cannot resolve argument slot {slot} of callee '{calleeId}'");
                        argTypes[slot] = SlotTable.ResolveSLType(m_Slots, av);
                    }
                }

                return new CalleeInfo(calleeId, argc, hasRet, retType, argTypes);
            }

            private void EmitCallStatic(IRData ir, int pos)
            {
                var ci = ResolveCallee(ir, pos);

                // Pop args (stack top = last declared parameter).
                var vals = new Val[ci.Argc];
                for (int i = ci.Argc - 1; i >= 0; --i)
                {
                    vals[i] = Pop(pos);
                }

                // Module-level string global for the callee id.
                if (!m_BridgeIds.TryGetValue(ci.Id, out string globalName))
                {
                    globalName = "@sl_mid_" + m_BridgeIds.Count.ToString(CultureInfo.InvariantCulture);
                    m_BridgeIds[ci.Id] = globalName;
                }

                // Pack each arg into !slv (kind 1 = f64 bit pattern).
                // Args are coerced to the callee's declared slot type first
                // (the VM converts arguments on call-in, e.g. Float32 -> f64).
                var slvNames = new string[ci.Argc];
                for (int i = 0; i < ci.Argc; i++)
                {
                    int kind = ci.ArgTypes[i] == SLType.F64 ? 1 : 0;
                    string c = CoerceStore(vals[i], ci.ArgTypes[i], $"CallStatic arg {i}");
                    string u0 = NV();
                    EmitBody("    {0} = llvm.mlir.undef : !slv", u0);
                    string u1 = NV();
                    EmitBody("    {0} = llvm.insertvalue {1}, {2}[0] : !slv", u1, CK32(kind), u0);
                    string u2 = NV();
                    EmitBody("    {0} = llvm.insertvalue {1}, {2}[1] : !slv", u2, c, u1);
                    slvNames[i] = u2;
                }

                // Argument array + return cell.
                string cargs = NV();
                EmitBody("    {0} = llvm.alloca {1} x !slv : (i32) -> !llvm.ptr", cargs, CK32(ci.Argc));
                string cret = NV();
                EmitBody("    {0} = llvm.alloca {1} x !slv : (i32) -> !llvm.ptr", cret, CK32(1));
                for (int i = 0; i < ci.Argc; i++)
                {
                    string p = NV();
                    EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, !slv", p, cargs, CI64(i));
                    EmitBody("    llvm.store {0}, {1} : !slv, !llvm.ptr", slvNames[i], p);
                }

                // Callee id -> const char* (first element of the array global).
                string pg = NV();
                EmitBody("    {0} = llvm.mlir.addressof {1} : !llvm.ptr", pg, globalName);
                int idLen = Encoding.UTF8.GetByteCount(ci.Id);
                string pid = NV();
                EmitBody("    {0} = llvm.getelementptr {1}[{2}, {2}] : (!llvm.ptr, i32, i32) -> !llvm.ptr, !llvm.array<{3} x i8>",
                    pid, pg, CK32(0), idLen);

                // Load the injected bridge pointer (null = host never
                // injected: return a zeroed slv instead of crashing).
                string gp = NV();
                EmitBody("    {0} = llvm.mlir.addressof @sl_g_invoke_vm_ptr : !llvm.ptr", gp);
                string fp = NV();
                EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> !llvm.ptr", fp, gp);
                string nullp = NV();
                EmitBody("    {0} = llvm.mlir.zero : !llvm.ptr", nullp);
                string nz = NV();
                EmitBody("    {0} = llvm.icmp \"ne\" {1}, {2} : !llvm.ptr", nz, fp, nullp);

                int tag = m_TmpCounter++;
                EmitBody("    cf.cond_br {0}, ^cs{1}_call, ^cs{1}_zero", nz, tag);
                EmitBody("  ^cs{0}_call:", tag);
                string rc = NV();
                EmitBody("    {0} = llvm.call {1}({2}, {3}, {4}, {5}, {6}) : !llvm.ptr, (!llvm.ptr, !llvm.ptr, !llvm.ptr, i32, !llvm.ptr) -> i64",
                    rc, fp, "%ctx", pid, cargs, CK32(ci.Argc), cret);
                EmitBody("    cf.br ^cs{0}_post", tag);
                EmitBody("  ^cs{0}_zero:", tag);
                string z0 = NV();
                EmitBody("    {0} = llvm.mlir.undef : !slv", z0);
                string z1 = NV();
                EmitBody("    {0} = llvm.insertvalue {1}, {2}[0] : !slv", z1, CK32(0), z0);
                string z2 = NV();
                EmitBody("    {0} = llvm.insertvalue {1}, {2}[1] : !slv", z2, CI64(0), z1);
                EmitBody("    llvm.store {0}, {1} : !slv, !llvm.ptr", z2, cret);
                EmitBody("    cf.br ^cs{0}_post", tag);
                EmitBody("  ^cs{0}_post:", tag);

                // Unpack the return value (declaration order: caller side
                // keeps carrying it as an i64 bit pattern).
                if (ci.HasRet)
                {
                    string rv = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> !slv", rv, cret);
                    string d = NV();
                    EmitBody("    {0} = llvm.extractvalue {1}[1] : !slv", d, rv);
                    m_Stack.Add(new Val(d, ci.RetType));
                }
            }

            // ---- array access + virtual getter inlining --------------------------
            //
            // VMArray (x64 release) layout behind the object pointer:
            //   +0  VMObject header (48 bytes)
            //   +48 uint32 length          <- authoritative (arr->length)
            //   +52 uint32 unit_length
            //   +56 void*  element_runtime_type
            //   +64 void*  data            <- element buffer: data + idx*unit_length
            // Out-of-bounds or null-receiver loads yield 0, matching
            // vm_array_try_load_value returning FALSE (the interpreter then
            // pushes i32 0). All selects stay in the i64 domain; pointers
            // only cross through inttoptr/ptrtoint (no-ops on x64).

            private void EmitArrayLoad(IRData ir, int pos)
            {
                Val? idxVal = null;
                if (ir.opCode == EIROpCode.LoadArrayIndexField)
                {
                    idxVal = Pop(pos);
                    if (idxVal.Value.Type != SLType.I64)
                        throw Fail($"[{pos}] array index is {idxVal.Value.Type}, expected an integer");
                }
                Val arr = Pop(pos);
                if (!IsArrayType(arr.Type))
                    throw Fail($"[{pos}] {ir.opCode} receiver is {arr.Type}, expected an array");

                string idx = idxVal.HasValue ? idxVal.Value.Name : CI64(ir.index);

                // null guard: route a null receiver through the zeroed sentinel
                string okp = NV();
                EmitBody("    {0} = arith.cmpi ne, {1}, {2} : i64", okp, arr.Name, CI64(0));
                string basei = NV();
                EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", basei, okp, arr.Name, ArraySentinelI64());
                string bas = NV();
                EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", bas, basei);

                // bounds check: (uint64)idx < length (negative indices wrap and fail)
                string lenp = NV();
                EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                    lenp, bas, CI64(48));
                string len32 = NV();
                EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i32", len32, lenp);
                string len = NV();
                EmitBody("    {0} = arith.extui {1} : i32 to i64", len, len32);
                string okb = NV();
                EmitBody("    {0} = arith.cmpi ult, {1}, {2} : i64", okb, idx, len);

                // element address: data + idx * unit_length; the OOB address
                // is computed but never selected, so it is never dereferenced
                string datap = NV();
                EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                    datap, bas, CI64(64));
                string dataptr = NV();
                EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> !llvm.ptr", dataptr, datap);
                string dati = NV();
                EmitBody("    {0} = llvm.ptrtoint {1} : !llvm.ptr to i64", dati, dataptr);
                string off = NV();
                EmitBody("    {0} = arith.muli {1}, {2} : i64", off, idx, CI64(ElemWidthOf(arr.Type)));
                string epi = NV();
                EmitBody("    {0} = arith.addi {1}, {2} : i64", epi, dati, off);
                string eps = NV();
                EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", eps, okb, epi, ArraySentinelI64());
                string ep = NV();
                EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", ep, eps);

                // raw load + conversion to the i64 carrying type
                string val;
                if (arr.Type == SLType.ArrayF64)
                {
                    string f = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> f64", f, ep);
                    val = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", val, f);
                }
                else if (arr.Type == SLType.ArrayI32)
                {
                    string r32 = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i32", r32, ep);
                    val = NV();
                    EmitBody("    {0} = arith.extsi {1} : i32 to i64", val, r32);
                }
                else
                {
                    val = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i64", val, ep);
                }

                // null/OOB -> 0 (bit pattern 0 == 0.0 for the f64 case)
                string ok = NV();
                EmitBody("    {0} = arith.andi {1}, {2} : i1", ok, okp, okb);
                string sel = NV();
                EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", sel, ok, val, CI64(0));
                m_Stack.Add(new Val(sel, ElemTypeOf(arr.Type)));
            }

            private void EmitCallVirt(IRData ir, int pos)
            {
                var vi = ResolveVirtCallee(ir, pos);
                for (int i = vi.Argc - 1; i >= 0; --i) Pop(pos);
                Val recv = Pop(pos);
                if (!IsArrayType(recv.Type))
                    throw Fail($"[{pos}] CallVirt receiver is {recv.Type}, only array getters are inlinable");
                if (!IsTrivialArrayLengthGetter(vi.Method))
                    throw Fail($"[{pos}] CallVirt to '{vi.Name}' is not an inlinable array getter");

                // inline of Array<T>.get length(): (int64)*(int32*)(recv + 48),
                // null-safe (a null array reports length 0)
                string okp = NV();
                EmitBody("    {0} = arith.cmpi ne, {1}, {2} : i64", okp, recv.Name, CI64(0));
                string basei = NV();
                EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", basei, okp, recv.Name, ArraySentinelI64());
                string bas = NV();
                EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", bas, basei);
                string lenp = NV();
                EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                    lenp, bas, CI64(48));
                string len32 = NV();
                EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i32", len32, lenp);
                string len = NV();
                EmitBody("    {0} = arith.extui {1} : i32 to i64", len, len32);
                m_Stack.Add(new Val(len, SLType.I64));
            }

            private readonly struct VirtCalleeInfo
            {
                public readonly IRMethod Method;
                public readonly string Name;
                public readonly int Argc;
                public VirtCalleeInfo(IRMethod method, string name, int argc)
                { Method = method; Name = name; Argc = argc; }
            }

            private VirtCalleeInfo ResolveVirtCallee(IRData ir, int pos)
            {
                if (!(ir.opValue is IRMethodCall imc) || imc.irMethod == null)
                    throw Fail($"[{pos}] CallVirt without resolvable method metadata");
                int argc = imc.paramCount;
                if (argc < 0)
                    throw Fail($"[{pos}] CallVirt with negative paramCount {argc}");
                return new VirtCalleeInfo(imc.irMethod, imc.methodName, argc);
            }

            /// <summary>
            /// Trivial receiver-field getter (ignoring Nop/Label/BrLabel):
            /// real frontend getters end with BrLabel -&gt; Label (FunEndLabel)
            /// instead of Ret, e.g. Core.Array&lt;T&gt;.length is
            /// [Nop, LoadArgument 0, LoadNotStaticField 0, StoreReturn 0, BrLabel, Label].
            /// On Array&lt;T&gt; field slot 0 is _length, and the receiver-type
            /// check in the caller guarantees the callee is Array's own
            /// getter, so the inlined result is the authoritative
            /// arr-&gt;length at +48.
            /// </summary>
            private static bool IsTrivialArrayLengthGetter(IRMethod m)
            {
                if (m == null) return false;
                // Ref-module stubs carry no IR body (empty IRDataList): the
                // real getter lives in the referenced module and is only
                // resolved by method id at CVM runtime. The canonical
                // Core.Array<T>.length stub is known to be a plain
                // field-0 getter, and the caller has already type-checked
                // the receiver as an array, so the inline stays authoritative.
                if (m.IRDataList == null || m.IRDataList.Count == 0)
                    return m.id == "Core.Array<T>.length";
                var ops = new List<EIROpCode>(5);
                var idxs = new List<int>(5);
                foreach (var d in m.IRDataList)
                {
                    if (d == null) return false;
                    if (d.opCode == EIROpCode.Nop || d.opCode == EIROpCode.Label || d.opCode == EIROpCode.BrLabel) continue;
                    ops.Add(d.opCode);
                    idxs.Add(d.index);
                    if (ops.Count > 4) return false;
                }
                // Legacy defensive form with an explicit trailing Ret.
                if (ops.Count == 4 && ops[3] == EIROpCode.Ret) ops.RemoveAt(3);
                if (ops.Count != 3) return false;
                return ops[0] == EIROpCode.LoadArgument && idxs[0] == 0
                    && ops[1] == EIROpCode.LoadNotStaticField && idxs[1] == 0
                    && ops[2] == EIROpCode.StoreReturn && idxs[2] == 0;
            }

            private static string DumpIRShape(IRMethod m)
            {
                if (m == null) return "<null method>";
                if (m.IRDataList == null) return "<null body>";
                var parts = new List<string>();
                foreach (var d in m.IRDataList)
                    parts.Add(d == null ? "<null>" : $"{d.opCode} {d.index}");
                return string.Join(", ", parts);
            }

            private void EmitArith(EIROpCode op, int pos)
            {
                Val b = Pop(pos);
                Val a = Pop(pos);

                if (IsFloatType(a.Type) || IsFloatType(b.Type))
                {
                    string fa = ToF64(a);
                    string fb = ToF64(b);
                    string fop = op switch
                    {
                        EIROpCode.Add => "addf",
                        EIROpCode.Minus => "subf",
                        EIROpCode.Multiply => "mulf",
                        EIROpCode.Divide => "divf",
                        _ => "remf",
                    };
                    string f = NV();
                    EmitBody("    {0} = arith.{1} {2}, {3} : f64", f, fop, fa, fb);

                    // VM float rule (runtime_value_compute): when the LEFT
                    // operand is not f64 (f32 constant or int + float), the
                    // f64 result is rounded back through f32 before being
                    // widened again. A Float64 left operand keeps full f64.
                    string bits = f;
                    if (a.Type != SLType.F64)
                    {
                        string t32 = NV();
                        EmitBody("    {0} = arith.truncf {1} : f64 to f32", t32, f);
                        string w = NV();
                        EmitBody("    {0} = arith.extf {1} : f32 to f64", w, t32);
                        bits = w;
                    }
                    string r = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", r, bits);
                    m_Stack.Add(new Val(r, a.Type == SLType.F64 ? SLType.F64 : SLType.F32));
                }
                else
                {
                    // NOTE: divsi/remsi are signed; UInt64 division deviates (stage-2 limitation)
                    string iop = op switch
                    {
                        EIROpCode.Add => "addi",
                        EIROpCode.Minus => "subi",
                        EIROpCode.Multiply => "muli",
                        EIROpCode.Divide => "divsi",
                        _ => "remsi",
                    };
                    string r = NV();
                    EmitBody("    {0} = arith.{1} {2}, {3} : i64", r, iop, a.Name, b.Name);
                    m_Stack.Add(new Val(r, SLType.I64));
                }
            }

            private void EmitBitwise(EIROpCode op, int pos)
            {
                Val b = Pop(pos);
                Val a = Pop(pos);
                if (IsFloatType(a.Type) || IsFloatType(b.Type))
                    throw Fail($"[{pos}] bitwise/shift op on float operand");

                string bop = op switch
                {
                    EIROpCode.InclusiveOr => "ori",
                    EIROpCode.Combine => "andi",
                    EIROpCode.XOR => "xori",
                    EIROpCode.Shr => "shrsi",
                    _ => "shli",
                };
                string r = NV();
                EmitBody("    {0} = arith.{1} {2}, {3} : i64", r, bop, a.Name, b.Name);
                m_Stack.Add(new Val(r, SLType.I64));
            }

            private void EmitLogical(EIROpCode op, int pos)
            {
                Val b = Pop(pos);
                Val a = Pop(pos);
                string ta = Truthy(a);
                string tb = Truthy(b);
                string l = NV();
                EmitBody("    {0} = arith.{1} {2}, {3} : i1", l, op == EIROpCode.And ? "andi" : "ori", ta, tb);
                string r = NV();
                EmitBody("    {0} = arith.extui {1} : i1 to i64", r, l);
                m_Stack.Add(new Val(r, SLType.I64));
            }

            private void EmitCompare(EIROpCode op, int pos)
            {
                Val b = Pop(pos);
                Val a = Pop(pos);
                string q = NV();
                if (a.Type == SLType.I64 && b.Type == SLType.I64)
                {
                    EmitBody("    {0} = arith.cmpi {1}, {2}, {3} : i64", q, CmpIntPred(op), a.Name, b.Name);
                }
                else
                {
                    string fa = ToF64(a);
                    string fb = ToF64(b);
                    EmitBody("    {0} = arith.cmpf {1}, {2}, {3} : f64", q, CmpFloatPred(op), fa, fb);
                }
                string r = NV();
                EmitBody("    {0} = arith.extui {1} : i1 to i64", r, q);
                m_Stack.Add(new Val(r, SLType.I64));
            }

            private void EmitNot(int pos)
            {
                Val v = Pop(pos);
                string q = NV();
                if (v.Type == SLType.I64)
                {
                    EmitBody("    {0} = arith.cmpi eq, {1}, {2} : i64", q, v.Name, CI64(0));
                }
                else
                {
                    string f = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f, v.Name);
                    EmitBody("    {0} = arith.cmpf oeq, {1}, {2} : f64", q, f, F64Zero());
                }
                string r = NV();
                EmitBody("    {0} = arith.extui {1} : i1 to i64", r, q);
                m_Stack.Add(new Val(r, SLType.I64));
            }

            private void EmitNeg(int pos)
            {
                Val v = Pop(pos);
                if (v.Type == SLType.I64)
                {
                    string r = NV();
                    EmitBody("    {0} = arith.subi {1}, {2} : i64", r, CI64(0), v.Name);
                    m_Stack.Add(new Val(r, SLType.I64));
                }
                else
                {
                    // sign flip is exact in both rounding domains, so the F32
                    // payload (an exact f64 widening) flows through unchanged
                    string f = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f, v.Name);
                    string g = NV();
                    EmitBody("    {0} = arith.negf {1} : f64", g, f);
                    string r = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", r, g);
                    m_Stack.Add(new Val(r, v.Type));
                }
            }

            /// <summary>
            /// Integer convert: float sources (F64/F32 payloads are both valid
            /// f64 bits) go through fptosi; then truncate to the target width
            /// and sign/zero-extend back to i64 (the carrying type).
            /// </summary>
            private void EmitConvert(int pos, int bits, bool unsigned)
            {
                Val v = Pop(pos);
                string w = v.Name;

                if (v.Type != SLType.I64)
                {
                    string f = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f, v.Name);
                    w = NV();
                    EmitBody("    {0} = arith.fptosi {1} : f64 to i64", w, f);
                }

                if (bits == 64)
                {
                    m_Stack.Add(new Val(w, SLType.I64));
                    return;
                }

                string t = NV();
                EmitBody("    {0} = arith.trunci {1} : i64 to i{2}", t, w, bits);
                string r = NV();
                EmitBody("    {0} = arith.{1} {2} : i{3} to i64", r, unsigned ? "extui" : "extsi", t, bits);
                m_Stack.Add(new Val(r, SLType.I64));
            }

            private void EmitConvertR8(int pos)
            {
                Val v = Pop(pos);
                if (v.Type != SLType.I64)
                {
                    // both F64 and F32 payloads are exact f64 bit patterns;
                    // f32 -> f64 widening is exact, so identity (typed F64,
                    // matching the VM's slot-type coercion on convert)
                    m_Stack.Add(new Val(v.Name, SLType.F64));
                    return;
                }
                string f = NV();
                EmitBody("    {0} = arith.sitofp {1} : i64 to f64", f, v.Name);
                string r = NV();
                EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", r, f);
                m_Stack.Add(new Val(r, SLType.F64));
            }

            private static string CmpIntPred(EIROpCode op)
            {
                switch (op)
                {
                    case EIROpCode.Ceq: return "eq";
                    case EIROpCode.Cne: return "ne";
                    case EIROpCode.Cgt: return "sgt";
                    case EIROpCode.Cge: return "sge";
                    case EIROpCode.Clt: return "slt";
                    case EIROpCode.Cle: return "sle";
                }
                throw new InvalidOperationException(op.ToString());
            }

            private static string CmpFloatPred(EIROpCode op)
            {
                switch (op)
                {
                    case EIROpCode.Ceq: return "oeq";
                    case EIROpCode.Cne: return "une";
                    case EIROpCode.Cgt: return "ogt";
                    case EIROpCode.Cge: return "oge";
                    case EIROpCode.Clt: return "olt";
                    case EIROpCode.Cle: return "ole";
                }
                throw new InvalidOperationException(op.ToString());
            }

            // ---- coercion / conversion helpers ------------------------------------

            /// <summary>
            /// Coerce a stack value to a slot's static type before storing.
            /// I64 -> F64 slots convert numerically (sitofp); F32 -> F64 slots
            /// store the bits as-is (the payload is already the exact f64
            /// widening, matching the VM's slot-type coercion in
            /// vm_runtime_object_try_write_value); anything else fails.
            /// </summary>
            private string CoerceStore(Val v, SLType slotTy, string what)
            {
                if (v.Type == slotTy) return v.Name;
                if (slotTy == SLType.F64 && v.Type == SLType.F32)
                    return v.Name;
                if (slotTy == SLType.F64 && v.Type == SLType.I64)
                {
                    string f = NV();
                    EmitBody("    {0} = arith.sitofp {1} : i64 to f64", f, v.Name);
                    string r = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", r, f);
                    return r;
                }
                throw Fail($"type mismatch at {what}: value is {v.Type}, slot is {slotTy}");
            }

            /// <summary>Materialize an f64 SSA value from a stack Val.</summary>
            private string ToF64(Val v)
            {
                string f = NV();
                if (v.Type != SLType.I64)
                    EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f, v.Name);
                else
                    EmitBody("    {0} = arith.sitofp {1} : i64 to f64", f, v.Name);
                return f;
            }

            /// <summary>Reduce a stack Val to an i1 truth value (NaN counts as true).</summary>
            private string Truthy(Val v)
            {
                string q = NV();
                if (v.Type == SLType.I64)
                {
                    EmitBody("    {0} = arith.cmpi ne, {1}, {2} : i64", q, v.Name, CI64(0));
                }
                else
                {
                    string f = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f, v.Name);
                    EmitBody("    {0} = arith.cmpf une, {1}, {2} : f64", q, f, F64Zero());
                }
                return q;
            }

            // ---- constant decoding ------------------------------------------------

            private static long ConstI64(IRData ir)
            {
                object v = ir.opValue;
                if (v is bool b) return b ? 1 : 0;
                if (v is ulong u) return unchecked((long)u);
                if (v != null)
                {
                    try { return Convert.ToInt64(v, CultureInfo.InvariantCulture); }
                    catch { }
                }
                if (ir.Payload != null)
                {
                    if (ir.Payload.Length >= 8) return BitConverter.ToInt64(ir.Payload, 0);
                    if (ir.Payload.Length >= 4) return BitConverter.ToInt32(ir.Payload, 0);
                    if (ir.Payload.Length >= 2) return BitConverter.ToInt16(ir.Payload, 0);
                    if (ir.Payload.Length >= 1) return (sbyte)ir.Payload[0];
                }
                return 0;
            }

            private static long ConstF64Bits(IRData ir)
            {
                object v = ir.opValue;
                if (v is double d) return BitConverter.DoubleToInt64Bits(d);
                if (v is float ff) return BitConverter.DoubleToInt64Bits(ff);
                if (v != null)
                {
                    try { return BitConverter.DoubleToInt64Bits(Convert.ToDouble(v, CultureInfo.InvariantCulture)); }
                    catch { }
                }
                if (ir.Payload != null && ir.Payload.Length >= 8)
                    return BitConverter.ToInt64(ir.Payload, 0);
                return 0;
            }

            // ---- SSA naming + constant hoisting ------------------------------------

            private static string ConstSuffix(long v)
                => v >= 0 ? v.ToString(CultureInfo.InvariantCulture)
                          : "m" + ((ulong)(-v)).ToString(CultureInfo.InvariantCulture);

            /// <summary>Hoisted i64 constant (entry block, dominates all uses).</summary>
            private string CI64(long v)
            {
                if (m_I64Consts.TryGetValue(v, out string name)) return name;
                name = "%c_" + ConstSuffix(v);
                EmitConst("    {0} = arith.constant {1} : i64", name, v.ToString(CultureInfo.InvariantCulture));
                m_I64Consts[v] = name;
                return name;
            }

            /// <summary>Hoisted index constant for memref subscripts.</summary>
            private string CIDX(int v)
            {
                if (m_IndexConsts.TryGetValue(v, out string name)) return name;
                name = "%x_" + ConstSuffix(v);
                EmitConst("    {0} = arith.constant {1} : index", name, v.ToString(CultureInfo.InvariantCulture));
                m_IndexConsts[v] = name;
                return name;
            }

            /// <summary>Hoisted i32 constant (llvm dialect, sl_value.kind).</summary>
            private string CK32(int v)
            {
                if (m_K32Consts.TryGetValue(v, out string name)) return name;
                name = "%k_" + ConstSuffix(v);
                EmitConst("    {0} = llvm.mlir.constant({1} : i32) : i32", name, v.ToString(CultureInfo.InvariantCulture));
                m_K32Consts[v] = name;
                return name;
            }

            /// <summary>0.0 as f64, materialized once (used by NaN-safe comparisons).</summary>
            private string F64Zero()
            {
                if (!m_F64ZeroEmitted)
                {
                    EmitConst("    %fz_zero = llvm.bitcast {0} : i64 to f64", CI64(0));
                    m_F64ZeroEmitted = true;
                    m_F64ZeroName = "%fz_zero";
                }
                return m_F64ZeroName;
            }

            /// <summary>
            /// Lazily materialize a 72-byte dummy VMArray-like buffer and return it as an
            /// i64 pointer bit-pattern. Null/OOB array accesses are routed here so that
            /// the subsequent bounds check (cmpi ult against length) always fails and the
            /// result select folds to 0 - matching the VM's "push i32 0" fallback.
            /// Layout: [0..47] VMObject header (garbage, never inspected) | [48] length
            /// (u32, must be zeroed - otherwise a stale length would make the bounds check
            /// pass and dereference a wild data pointer) | [52] unit_length (never read)
            /// | [56] element_runtime_type (never read) | [64] data (never dereferenced
            /// because ok=false keeps the select on the safe path).
            /// Note: constants referenced here are hoisted into m_ConstSb before the
            /// alloca/GEP lines, and the entry block dominates all uses.
            /// </summary>
            private string ArraySentinelI64()
            {
                if (!m_ArrayDummyEmitted)
                {
                    EmitConst("    %arrdummy = llvm.alloca {0} x i8 : (i32) -> !llvm.ptr", CK32(72));
                    // zero the +48 length field so null arrays fail the bounds check
                    EmitConst("    %ad_len = llvm.getelementptr %arrdummy[{0}] : (!llvm.ptr, i64) -> !llvm.ptr, i8", CI64(48));
                    EmitConst("    llvm.store {0}, %ad_len : i32, !llvm.ptr", CK32(0));
                    EmitConst("    %arrdummy_i = llvm.ptrtoint %arrdummy : !llvm.ptr to i64");
                    m_ArrayDummyEmitted = true;
                    m_ArrayDummyName = "%arrdummy_i";
                }
                return m_ArrayDummyName;
            }

            private string NV() => "%v" + (m_TmpCounter++).ToString(CultureInfo.InvariantCulture);

            private Val Pop(int pos)
            {
                if (m_Stack.Count < 1) throw Fail($"[{pos}] stack underflow");
                Val v = m_Stack[m_Stack.Count - 1];
                m_Stack.RemoveAt(m_Stack.Count - 1);
                return v;
            }

            private EmitFailException Fail(string message)
                => new EmitFailException("[" + (m_Method.id ?? m_Method.onlyFunctionName ?? "?") + "] " + message);

            private void EmitBody(string format, params object?[] args)
            {
                m_BodySb.AppendFormat(CultureInfo.InvariantCulture, format, args).Append('\n');
            }

            private void EmitConst(string format, params object?[] args)
            {
                m_ConstSb.AppendFormat(CultureInfo.InvariantCulture, format, args).Append('\n');
            }
        }
    }
}
