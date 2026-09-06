//****************************************************************************
//  File:      MLIRExporter.cs
// ------------------------------------------------
//  Description: Export SimpleLanguage IR to MLIR (stage 2: real emission)
//  - Every @AOT() method becomes a func.func with the sl_value ABI:
//      (ctx: !llvm.ptr, args: !llvm.ptr, argc: i32, ret: !llvm.ptr) -> i64
//  - Stack-machine IR is linearized into SSA via per-block stack profiles.
//  - Failed methods are skipped and recorded in the per-method manifest
//    entries (module.json "aot" field, status=failed), so aot.mlir always
//    stays valid MLIR.
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Export.SLIR.Types;

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
        /// array element).</summary>
        public sealed class AotMethodManifest
        {
            public string Id { get; set; } = "";
            public string Symbol { get; set; } = "";
            public string Status { get; set; } = "";
            public string? Reason { get; set; }
            /// <summary>Per-argument ABI packages in slot order; slot is the
            /// C ABI kind: 0=i64 bits, 1=f64 bits, 2=struct native buffer,
            /// 3=objref VMObject*. typeId is the data classId for slot 2/3
            /// (0 for scalars).</summary>
            public List<SLAotParamPackage> ParamList { get; set; } = new();
            /// <summary>Return ABI slot: -1=void, 0=i64, 1=f64, 2=struct
            /// (ret prefill protocol §5.6), 3=objref.</summary>
            public int RetSlot { get; set; } = -1;
            /// <summary>Struct return type classId (0 otherwise).</summary>
            public int RetTypeId { get; set; }
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
            /// <summary>模块中是否含成功发射的 @GPU 方法（stage-3 需走 GPU
            /// 降级 pass 链并链接 sl_gpu_runtime）。</summary>
            public bool HasGpuMethods { get; set; }
            /// <summary>Module-level data-type registry (design §4): emits
            /// the !sl_t_* aliases and feeds the manifest "aot.typeList"
            /// consumed by the C marshal code. Null only when the export
            /// threw before any method was emitted.</summary>
            internal AotTypeTable? TypeTable { get; set; }
        }

        /// <summary>
        /// 模块级 AOT 导出：把一批 @AOT() 候选方法写入同一个 aot.mlir（每模块一个 aot.dll）。
        /// 单个方法导出失败不影响其它方法：失败项记入返回的 manifest 条目
        /// （status=failed，由 MLIRExportManager 合并进 module.json 的 "aot" 字段），
        /// 成功项照常生成 func.func。
        /// </summary>
        public static AotExportResult ExportModuleToFile(IReadOnlyList<IRMethod> methods, string outputPath)
        {
            if (methods == null) throw new ArgumentNullException(nameof(methods));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            var methodEntries = new List<AotMethodManifest>();
            var usedSymbols = new HashSet<string>();
            var okSymbols = new List<string>();
            var failedIds = new List<string>();
            /* callee method-id -> module-level string global, shared by all
             * methods of this module (stage-5 reverse bridge). */
            var bridgeIds = new Dictionary<string, string>();

            bool anyGpu = false;
            foreach (var m in methods)
                if (m != null && m.isGpu) { anyGpu = true; break; }
            bool anyGpuOk = false;
            int gpuModuleIndex = 0;

            /* Module-level data-type registry: method emission lazily
             * registers !sl_t_* aliases into it, so the alias block can
             * only be emitted after all methods have run. Assembly is
             * therefore two-phase: header + aliases, then the module
             * body collected below. */
            var typeTable = new AotTypeTable();
            var header = new StringBuilder();
            header.AppendLine("// SimpleLanguage AOT module");
            header.AppendLine("!slv = !llvm.struct<(i32, i64)>");

            var body = new StringBuilder();
            body.AppendLine(anyGpu
                ? "module attributes {gpu.container_module} {"
                : "module {");
            // MSVC CRT: the moment aot.obj contains real float arithmetic
            // (truncf/extf/addf), the CRT emits a reference to _fltused;
            // define it here so the aot.dll link never needs the CRT import.
            body.AppendLine("  llvm.mlir.global constant @_fltused(39029 : i32) : i32");

            foreach (var m in methods)
            {
                if (m == null) continue;

                var entry = new AotMethodManifest { Id = m.id };
                try
                {
                    MethodEmitter emitter = m.isGpu
                        ? new GpuMethodEmitter(m, usedSymbols, bridgeIds, typeTable, gpuModuleIndex++)
                        : new MethodEmitter(m, usedSymbols, bridgeIds, typeTable);
                    body.Append(emitter.Emit());
                    entry.Symbol = emitter.Symbol;
                    entry.Status = "ok";
                    entry.ParamList = emitter.BuildParamPackages();
                    entry.RetSlot = emitter.RetSlot;
                    entry.RetTypeId = emitter.RetTypeId;
                    okSymbols.Add(emitter.Symbol);
                    if (m.isGpu) anyGpuOk = true;
                }
                catch (EmitFailException ex)
                {
                    entry.Status = "failed";
                    entry.Reason = ex.Message;
                    failedIds.Add(m.id);
                    body.Append("  // FAILED aot method id: ").Append(m.id)
                       .Append(" -- ").Append(ex.Message.Replace('\n', ' '))
                       .AppendLine();
                }
                methodEntries.Add(entry);
            }

            if (bridgeIds.Count > 0)
            {
                body.Append(EmitBridgePlumbing(bridgeIds));
            }
            if (anyGpu)
            {
                // GPU staging runtime (implemented in sl_gpu_runtime.c):
                // host wrappers call these to malloc / copy / free device
                // buffers around gpu.launch_func.
                body.Append("  llvm.func @slgpuMalloc(i64) -> !llvm.ptr\n");
                body.Append("  llvm.func @slgpuMemcpyHtoD(!llvm.ptr, !llvm.ptr, i64)\n");
                body.Append("  llvm.func @slgpuMemcpyDtoH(!llvm.ptr, !llvm.ptr, i64)\n");
                body.Append("  llvm.func @slgpuFree(!llvm.ptr)\n");
            }

            body.AppendLine("}");

            var sb = new StringBuilder();
            sb.Append(header);
            typeTable.EmitAliases(sb);
            sb.Append(body);

            File.WriteAllText(outputPath, sb.ToString());

            return new AotExportResult
            {
                MlirPath = outputPath,
                OkSymbols = okSymbols,
                FailedIds = failedIds,
                NeedsBridgeInit = bridgeIds.Count > 0,
                HasGpuMethods = anyGpuOk,
                Methods = methodEntries,
                TypeTable = typeTable,
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
                // +1: MLIR string arrays do not auto-append a NUL, and the
                // C side reads these with strlen-family lookups. Without it
                // an id whose length lands on an alignment boundary bleeds
                // into the next global (observed: find_method_by_id got a
                // concatenation of two ids and failed).
                int byteLen = Encoding.UTF8.GetByteCount(kv.Key) + 1;
                sb.Append("  llvm.mlir.global constant ").Append(kv.Value)
                  .Append('(').Append(EscapeCString(kv.Key))
                  .Append(") : !llvm.array<").Append(byteLen.ToString(CultureInfo.InvariantCulture))
                  .Append(" x i8>\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// C string literal escaping for llvm.mlir.global constants. Always
        /// appends an explicit NUL byte: MLIR string arrays do not add one
        /// implicitly, and the C side reads these with strlen-family lookups.
        /// </summary>
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
            sb.Append("\\00");
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

        internal static string SanitizeSymbol(string name)
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
        // Internal types
        // ------------------------------------------------------------------

        internal sealed class EmitFailException : Exception
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
        private enum SLType
        {
            I64, F64, F32, ArrayI32, ArrayI64, ArrayF64,
            /// <summary>data 值类型：i64 位模式持有完整原生缓冲指针
            ///（含 32B !sl_meta 头）；具体 data 类型由 Val/ProfileVal.TypeId
            ///（= IRMetaClass.id）区分。</summary>
            Struct,
            /// <summary>引用类型（String / 用户 class / 接口 / Core.Object /
            /// 泛型 T）：i64 位模式持有 VMObject* 纯透传（kind=3），
            /// AOT 体内不可解引用。</summary>
            ObjRef,
        }

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
        /// TypeId carries the data type classId for Struct values (0 otherwise).
        /// </summary>
        private readonly struct Val
        {
            public readonly string Name;
            public readonly SLType Type;
            public readonly int TypeId;
            public Val(string name, SLType type, int typeId = 0)
            {
                Name = name; Type = type; TypeId = typeId;
            }
        }

        /// <summary>
        /// Profile-stack entry: SLType plus the Struct type id. Implicit
        /// conversions keep existing push sites (plain SLType) and
        /// comparisons (ProfileVal -> SLType) working unchanged.
        /// </summary>
        private readonly struct ProfileVal
        {
            public readonly SLType Type;
            public readonly int TypeId;
            public ProfileVal(SLType type, int typeId = 0)
            {
                Type = type; TypeId = typeId;
            }
            public static implicit operator ProfileVal(SLType t) => new ProfileVal(t, 0);
            public static implicit operator SLType(ProfileVal v) => v.Type;
            public override string ToString()
                => TypeId != 0 ? Type.ToString() + "#" + TypeId.ToString(CultureInfo.InvariantCulture) : Type.ToString();
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
            EIROpCode.Convert_R4,
            EIROpCode.CallStatic,    /* stage 5: reverse bridge into the CVM */
            EIROpCode.LoadArrayIndex,      /* constant index: [array] -> [element] */
            EIROpCode.LoadArrayIndexField, /* variable index: [array, index] -> [element] */
            EIROpCode.StoreArrayIndex,     /* constant index store: arr[3] = v (payload [flag:1]) */
            EIROpCode.StoreArrayIndexField,/* variable index store: arr[i] = v */
            EIROpCode.CallVirt,            /* inlined array getters (arr.length) */
            EIROpCode.LoadNotStaticField,     /* data struct member load (native offset GEP) */
            EIROpCode.StoreNotStaticField1,   /* data brace init: [inst,val] -> [inst] */
            EIROpCode.StoreNotStaticField2,   /* data member store: [inst,val] -> [] */
        };

        /// <summary>
        /// Static slot typing: every argument/local slot maps to I64 or F64,
        /// resolved from the IRMetaVariable's IRMetaClass name.
        /// </summary>
        private sealed class SlotTable
        {
            public readonly Dictionary<int, SLType> ArgTypes = new Dictionary<int, SLType>();
            public readonly Dictionary<int, SLType> LocalTypes = new Dictionary<int, SLType>();
            /// <summary>Arg slot → data classId (Struct slots; 0 otherwise).
            /// Carried in Val/ProfileVal so member-access emission can look
            /// the layout up in the module type table.</summary>
            public readonly Dictionary<int, int> ArgTypeIds = new Dictionary<int, int>();
            public readonly Dictionary<int, int> LocalTypeIds = new Dictionary<int, int>();
            public readonly List<int> ArgSlotList = new List<int>();
            public int ArgSlots = 1;
            public int LocalSlots = 1;
            public bool HasRet = false;
            public SLType RetType = SLType.I64;
            /// <summary>Struct return type classId (0 otherwise).</summary>
            public int RetTypeId;
            /// <summary>Module-level registry data types are lazily
            /// registered into (never null after Resolve).</summary>
            internal AotTypeTable TypeTable = null!;

            private readonly string m_OwnerId;

            private SlotTable(string ownerId) { m_OwnerId = ownerId; }

            public static SlotTable Resolve(IRMethod m, AotTypeTable typeTable)
            {
                var t = new SlotTable(m?.id ?? "");
                t.TypeTable = typeTable ?? throw new ArgumentNullException(nameof(typeTable));

                if (m.methodArgumentList != null)
                {
                    var slots = new SortedSet<int>();
                    foreach (var v in m.methodArgumentList)
                    {
                        if (v == null) continue;
                        int slot = v.index;
                        if (slot < 0) throw t.Fail($"argument '{v.name}' has negative slot index {slot}");
                        var vt = ResolveVarType(t, v);
                        t.ArgTypes[slot] = vt.Type;
                        t.ArgTypeIds[slot] = vt.TypeId;
                        slots.Add(slot);
                    }
                    if (slots.Count > 0)
                    {
                        /* The C marshal walks ParamList[i] for arg slot i,
                         * so the argument slot range must be dense. */
                        if (slots.Min != 0 || slots.Max != slots.Count - 1)
                            throw t.Fail("argument slot indices are not dense (expected 0..n-1)");
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
                        var vt = ResolveVarType(t, v);
                        t.LocalTypes[slot] = vt.Type;
                        t.LocalTypeIds[slot] = vt.TypeId;
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
                    /* void methods still carry a '.return' variable typed
                     * Core.Void — treat it as "no return slot" (HasRet stays
                     * false, downstream emitters are already conditional on
                     * HasRet). */
                    if (IsVoidType(rv)) return t;
                    t.HasRet = true;
                    var rt = ResolveVarType(t, rv);
                    t.RetType = rt.Type;
                    t.RetTypeId = rt.TypeId;
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

            /// <summary>
            /// void methods carry a '.return' variable whose type resolves to
            /// the Void class (e.g. "Core.Void", MetaClass.eType == Void).
            /// </summary>
            public static bool IsVoidType(IRMetaVariable v)
            {
                var irmc = v.irMetaType?.irMetaClass;
                if (irmc == null) return false;
                var mc = irmc.OwnerMetaClass;
                if (mc != null && mc.eType == EType.Void) return true;
                string n = irmc.irName;
                if (string.IsNullOrEmpty(n)) return false;
                int dot = n.LastIndexOf('.');
                string leaf = dot >= 0 ? n.Substring(dot + 1) : n;
                return leaf == "Void";
            }

            /// <summary>
            /// Slot type resolution (design §4.2): scalars/arrays keep the
            /// stage-1 mapping; data types map to Struct (kind=2, registering
            /// the type and its nested types into the module type table);
            /// everything else (String / user class / interface / Core.Object
            /// / generic T) maps to ObjRef (kind=3 pass-through). Enum slots
            /// are rejected: the VM packs them as class references, which the
            /// AOT side cannot reconstruct.
            /// </summary>
            public static ProfileVal ResolveVarType(SlotTable t, IRMetaVariable v)
            {
                var cls = v.irMetaType?.irMetaClass;
                string n = cls?.irName;
                if (string.IsNullOrEmpty(n))
                    throw t.Fail($"cannot resolve IR type of variable '{v.name}'");
                int dot = n.LastIndexOf('.');
                string leaf = dot >= 0 ? n.Substring(dot + 1) : n;
                if (s_I64TypeNames.Contains(leaf)) return SLType.I64;
                if (leaf == "Double" || leaf == "Float64") return SLType.F64;
                /* Float32 keeps its own rounding-domain tag, but its i64 bits
                 * always hold the exact f64 widening (SLType doc above), so
                 * it crosses the ABI with kind=1 exactly like F64. Without
                 * this branch the leaf would fall through to ObjRef (kind=3)
                 * and corrupt the marshaling. */
                if (leaf == "Float32" || leaf == "Single") return SLType.F32;
                SLType? arr = TryResolveArrayType(t, v, leaf);
                if (arr.HasValue) return arr.Value;

                if (cls!.metaClassKind == IRMetaClassKind.Data)
                {
                    var info = t.TypeTable.Register(cls);
                    return new ProfileVal(SLType.Struct, info.ClassId);
                }
                if (cls.metaClassKind == IRMetaClassKind.Enum)
                    throw t.Fail($"enum-typed variable '{v.name}' ('{n}') is not AOT-compatible as a slot (use Int64 arithmetic instead)");
                if (leaf == "Member")
                    throw t.Fail($"member-typed variable '{v.name}' ('{n}') is not AOT-compatible as a slot");

                /* String / user class / interface / Core.Object / generic T:
                 * opaque VMObject reference, passed through as kind=3. */
                return new ProfileVal(SLType.ObjRef, cls.id);
            }

            /// <summary>ABI kind of a slot type: 0=i64 bits (also arrays,
            /// whose i64 holds the VMArray*), 1=f64 bits, 2=struct native
            /// buffer, 3=objref VMObject*.</summary>
            public static int AbiKindOf(SLType t)
            {
                switch (t)
                {
                    /* F32 keeps its own rounding-domain tag, but its i64
                     * bits hold the exact f64 widening, so it crosses the
                     * ABI with kind=1 exactly like F64 (see SLType doc). */
                    case SLType.F64:
                    case SLType.F32: return 1;
                    case SLType.Struct: return 2;
                    case SLType.ObjRef: return 3;
                    default: return 0;
                }
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

            public int GetArgTypeId(int slot)
                => ArgTypeIds.TryGetValue(slot, out var id) ? id : 0;

            public int GetLocalTypeId(int slot)
                => LocalTypeIds.TryGetValue(slot, out var id) ? id : 0;

            private EmitFailException Fail(string message)
                => new EmitFailException("[" + m_OwnerId + "] " + message);
        }

        /// <summary>A basic block: [Start, End) with successors and entry stack profile.</summary>
        private sealed class Block
        {
            public int Start;
            public int End;
            public List<int> Succs = new List<int>();
            public List<ProfileVal>? Entry;      // null = not yet reached
            public bool Reachable;
        }

        // ------------------------------------------------------------------
        // MethodEmitter
        //
        // Host (plain @AOT) emission. GpuMethodEmitter subclasses this and
        // overrides the emission hooks (parameter mapping, array access,
        // spill syntax, entry/exit) to translate the same IR inside a
        // gpu.func kernel; all analysis (blocks, stack profiles) is shared.
        // ------------------------------------------------------------------

        private class MethodEmitter
        {
            private const int ExitId = -1;

            protected readonly IRMethod m_Method;
            protected readonly SlotTable m_Slots;
            protected readonly List<IRData> m_Code;
            protected readonly int m_Count;
            protected readonly List<Block> m_Blocks = new List<Block>();
            protected readonly Dictionary<int, int> m_BlockOfPos = new Dictionary<int, int>();
            protected readonly List<Val> m_Stack = new List<Val>();
            protected int m_MaxDepth = 0;

            protected readonly StringBuilder m_ConstSb = new StringBuilder(); // hoisted to entry, dominates all
            protected readonly StringBuilder m_BodySb = new StringBuilder();
            protected readonly Dictionary<long, string> m_I64Consts = new Dictionary<long, string>();
            private readonly Dictionary<int, string> m_IndexConsts = new Dictionary<int, string>();
            protected readonly Dictionary<int, string> m_K32Consts = new Dictionary<int, string>();
            protected bool m_F64ZeroEmitted;
            protected string m_F64ZeroName = "";
            private bool m_ArrayDummyEmitted;
            private string m_ArrayDummyName = "";
            protected int m_TmpCounter = 0;
            /* Stage-5 bridge: module-level callee-id -> string-global map
             * (shared across all methods of the module). */
            private readonly Dictionary<string, string> m_BridgeIds;
            /* Module-level data-type registry (shared by all emitters of
             * one ExportModuleToFile call; member access resolves Struct
             * layouts through it). */
            protected readonly AotTypeTable m_TypeTable;

            public string Symbol { get; }

            /// <summary>Return ABI slot for the manifest (-1 = void).</summary>
            public int RetSlot => !m_Slots.HasRet ? -1 : SlotTable.AbiKindOf(m_Slots.RetType);
            /// <summary>Struct return type classId for the manifest.</summary>
            public int RetTypeId => m_Slots.RetTypeId;

            public MethodEmitter(IRMethod method, HashSet<string> usedSymbols,
                Dictionary<string, string> bridgeIds, AotTypeTable typeTable)
            {
                m_Method = method ?? throw new ArgumentNullException(nameof(method));
                if (method.IRDataList == null) throw Fail("IRDataList is null");
                m_Code = method.IRDataList;
                m_Count = m_Code.Count;
                if (m_Count == 0) throw Fail("empty method body");
                m_TypeTable = typeTable ?? throw new ArgumentNullException(nameof(typeTable));
                m_Slots = SlotTable.Resolve(method, m_TypeTable);
                Symbol = MakeUniqueSymbol(method, usedSymbols);
                m_BridgeIds = bridgeIds ?? throw new ArgumentNullException(nameof(bridgeIds));
            }

            /// <summary>
            /// Manifest param packages (slot order = argument slot order,
            /// validated dense by SlotTable.Resolve). typeName is the full
            /// IR type name for diagnostics on the C side.
            /// </summary>
            public List<SLAotParamPackage> BuildParamPackages()
            {
                var list = new List<SLAotParamPackage>(m_Slots.ArgSlotList.Count);
                foreach (int slot in m_Slots.ArgSlotList)
                {
                    SLType ty = m_Slots.GetArgType(slot);
                    var arg = m_Method.methodArgumentList?.Find(a => a != null && a.index == slot);
                    list.Add(new SLAotParamPackage
                    {
                        slot = SlotTable.AbiKindOf(ty),
                        typeId = m_Slots.GetArgTypeId(slot),
                        typeName = arg?.irMetaType?.irMetaClass?.irName ?? "",
                    });
                }
                return list;
            }

            public virtual string Emit()
            {
                CheckSupported();
                AnalyzeBlocks();
                ComputeProfiles();
                EmitEntry();
                EmitAllBlocks();
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

            protected void EmitAllBlocks()
            {
                for (int i = 0; i < m_Blocks.Count; i++)
                {
                    var b = m_Blocks[i];
                    if (b.Reachable) EmitBlock(b);
                }
            }

            protected static string MakeUniqueSymbol(IRMethod m, HashSet<string> used)
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

            protected static bool IsTerminator(EIROpCode op)
                => op == EIROpCode.Br || op == EIROpCode.BrLabel
                || op == EIROpCode.BrFalse || op == EIROpCode.BrTrue
                || op == EIROpCode.Ret;

            /// <summary>True for the LoadConst* family (no strings).</summary>
            protected static bool IsLoadConstOp(EIROpCode op)
                => op == EIROpCode.LoadConstUInt8
                || op == EIROpCode.LoadConstInt8
                || op == EIROpCode.LoadConstInt16
                || op == EIROpCode.LoadConstUInt16
                || op == EIROpCode.LoadConstInt32
                || op == EIROpCode.LoadConstUInt32
                || op == EIROpCode.LoadConstInt64
                || op == EIROpCode.LoadConstUInt64
                || op == EIROpCode.LoadConstFloat32
                || op == EIROpCode.LoadConstFloat64
                || op == EIROpCode.LoadConstBoolean;

            protected virtual void CheckSupported()
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

            protected void AnalyzeBlocks()
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

            protected void ComputeProfiles()
            {
                var work = new Queue<Block>();
                var first = m_Blocks[0];
                first.Entry = new List<ProfileVal>();
                first.Reachable = true;
                work.Enqueue(first);

                while (work.Count > 0)
                {
                    var b = work.Dequeue();
                    var sim = new List<ProfileVal>(b.Entry!);

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
                            succ.Entry = new List<ProfileVal>(sim);
                            work.Enqueue(succ);
                        }
                        else if (!ProfileEquals(succ.Entry!, sim))
                        {
                            throw Fail($"inconsistent stack profile at block ^b{succ.Start}");
                        }
                    }
                }
            }

            private static bool ProfileEquals(List<ProfileVal> a, List<ProfileVal> b)
            {
                if (a.Count != b.Count) return false;
                for (int i = 0; i < a.Count; i++)
                    if (a[i].Type != b[i].Type || a[i].TypeId != b[i].TypeId) return false;
                return true;
            }

            protected ProfileVal ProfilePop(List<ProfileVal> sim, int pos)
            {
                if (sim.Count < 1) throw Fail($"[{pos}] stack underflow");
                var t = sim[sim.Count - 1];
                sim.RemoveAt(sim.Count - 1);
                return t;
            }

            protected void StepProfile(List<ProfileVal> sim, IRData ir, int pos)
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
                        sim.Add(new ProfileVal(m_Slots.GetArgType(ir.index), m_Slots.GetArgTypeId(ir.index)));
                        return;

                    case EIROpCode.LoadLocal:
                        sim.Add(new ProfileVal(m_Slots.GetLocalType(ir.index), m_Slots.GetLocalTypeId(ir.index)));
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

                    case EIROpCode.Convert_R4:
                    {
                        var t = ProfilePop(sim, pos);
                        if (IsArrayType(t))
                            throw Fail($"[{pos}] f32 convert on array operand");
                        sim.Add(SLType.F32);
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

                    case EIROpCode.StoreArrayIndexField:
                    {
                        // variable index store: stack (top-down) value, idx, array
                        ProfilePop(sim, pos);             // value
                        var it = ProfilePop(sim, pos);    // idx
                        var at = ProfilePop(sim, pos);    // array
                        if (!IsArrayType(at))
                            throw Fail($"[{pos}] StoreArrayIndexField receiver is {at}, expected an array");
                        if (it != SLType.I64)
                            throw Fail($"[{pos}] StoreArrayIndexField index is {it}, expected an integer");
                        return;
                    }

                    case EIROpCode.StoreArrayIndex:
                    {
                        // constant index store; payload flag decides the pop
                        // order (EStoreArrayIndexFlag): 0 = [.., value, array]
                        // (array on top), else [.., array, value] (value on top)
                        int flag = StoreIndexFlagOf(ir);
                        var t1 = ProfilePop(sim, pos);
                        var t2 = ProfilePop(sim, pos);
                        var at = flag == 0 ? t1 : t2;
                        if (!IsArrayType(at))
                            throw Fail($"[{pos}] StoreArrayIndex receiver is {at}, expected an array");
                        return;
                    }

                    case EIROpCode.CallVirt:
                    {
                        // Only array trivial getters (arr.length) are inlined.
                        var vi = ResolveVirtCallee(ir, pos);
                        for (int k = 0; k < vi.Argc; k++) ProfilePop(sim, pos);
                        var rt = ProfilePop(sim, pos);
                        if (!IsArrayType(rt))
                            throw Fail($"[{pos}] CallVirt receiver is {rt.Type}, only array getters are inlinable");
                        if (!IsTrivialArrayLengthGetter(vi.Method))
                            throw Fail($"[{pos}] CallVirt to '{vi.Name}' is not an inlinable array getter (id={vi.Method?.id ?? "<null>"} body=[{DumpIRShape(vi.Method)}])");
                        sim.Add(SLType.I64);
                        return;
                    }

                    case EIROpCode.LoadNotStaticField:
                    {
                        // [receiver] -> [member value]. The IRData carries
                        // only the member field index (no operand type), so
                        // the member type is derived from the receiver's
                        // struct layout in the module type table.
                        var rt = ProfilePop(sim, pos);
                        var mem = ResolveMemberAccess(rt, ir.index, pos, "LoadNotStaticField");
                        switch (mem.Slot)
                        {
                            case AotMemberSlot.Scalar:
                                sim.Add(mem.IsFloat
                                    ? (mem.Width == 4 ? SLType.F32 : SLType.F64)
                                    : SLType.I64);
                                break;
                            case AotMemberSlot.Data:
                                sim.Add(new ProfileVal(SLType.Struct, mem.NestedTypeId));
                                break;
                            default:
                                // String / opaque Ptr slots: the load would
                                // need the refcount/view bridge (§5.10), not
                                // supported in this version.
                                throw Fail($"[{pos}] LoadNotStaticField of reference member '{mem.Name}' (slot kind {(int)mem.Slot}) is not supported");
                        }
                        return;
                    }

                    case EIROpCode.StoreNotStaticField1:
                    {
                        // [inst, val] -> [inst] (brace-init keeps receiver)
                        var vt = ProfilePop(sim, pos);
                        var rt = ProfilePop(sim, pos);
                        CheckMemberStore(rt, vt, ir.index, pos);
                        sim.Add(rt);
                        return;
                    }

                    case EIROpCode.StoreNotStaticField2:
                    {
                        // [inst, val] -> []
                        var vt = ProfilePop(sim, pos);
                        var rt = ProfilePop(sim, pos);
                        CheckMemberStore(rt, vt, ir.index, pos);
                        return;
                    }

                    default:
                        throw Fail($"[{pos}] unsupported opcode '{ir.opCode}'");
                }
            }

            /// <summary>
            /// Resolve the member accessed by LoadNotStaticField /
            /// StoreNotStaticField* (ir.index = field index = position in the
            /// owner's localIRMetaVariableList, which the layout mirrors).
            /// The receiver must be a Struct value (kind=2); ObjRef receivers
            /// would need the VMObject view bridge (§5.10), unsupported here.
            /// </summary>
            private AotMemberLayout ResolveMemberAccess(ProfileVal rt, int fieldIndex, int pos, string op)
            {
                if (rt.Type != SLType.Struct)
                {
                    if (rt.Type == SLType.ObjRef)
                        throw Fail($"[{pos}] {op} on an objref receiver ({rt}): object member access needs the VMObject view bridge and is not supported");
                    throw Fail($"[{pos}] {op} receiver is {rt.Type}, expected a data struct");
                }
                var info = m_TypeTable.Find(rt.TypeId);
                if (info == null)
                    throw Fail($"[{pos}] {op} receiver struct type {rt} is not registered in the type table");
                var mem = info.FindMember(fieldIndex);
                if (mem == null)
                    throw Fail($"[{pos}] {op} member index {fieldIndex} out of range for '{info.FullName}' ({info.Layout.Count} members)");
                return mem;
            }

            /// <summary>
            /// Profile-time check of a member store: scalar members take the
            /// matching scalar stack type, nested data members take a Struct
            /// value of exactly the nested type id, reference members are
            /// rejected (refcount bridge §5.10 not in this version).
            /// </summary>
            private void CheckMemberStore(ProfileVal rt, ProfileVal vt, int fieldIndex, int pos)
            {
                var mem = ResolveMemberAccess(rt, fieldIndex, pos, "member store");
                switch (mem.Slot)
                {
                    case AotMemberSlot.Scalar:
                        var want = mem.IsFloat
                            ? (mem.Width == 4 ? SLType.F32 : SLType.F64)
                            : SLType.I64;
                        if (vt.Type != want)
                            throw Fail($"[{pos}] member store '{mem.Name}' expects {want}, got {vt}");
                        break;
                    case AotMemberSlot.Data:
                        if (vt.Type != SLType.Struct || vt.TypeId != mem.NestedTypeId)
                            throw Fail($"[{pos}] member store '{mem.Name}' expects Struct#{mem.NestedTypeId}, got {vt}");
                        break;
                    default:
                        throw Fail($"[{pos}] member store to reference member '{mem.Name}' (slot kind {(int)mem.Slot}) is not supported");
                }
            }

            /// <summary>
            /// Dup/Pop payload is a plain little-endian int32 count (no index prefix:
            /// they are not in IRData.UsesIndex). Missing/short payload defaults to 1.
            /// </summary>
            protected static int ReadCount(IRData ir)
            {
                if (ir.Payload == null || ir.Payload.Length < 4) return 1;
                return BitConverter.ToInt32(ir.Payload, 0);
            }

            /// <summary>
            /// StoreArrayIndex operand-order flag (EStoreArrayIndexFlag).
            /// In-memory IRData payloads carry just the flag byte; a
            /// serialized payload embeds the index first ([index:4][flag:1]).
            /// 0 = stack [.., value, array] (array on top), 1 = [.., array, value].
            /// </summary>
            protected static int StoreIndexFlagOf(IRData ir)
            {
                if (ir.Payload == null || ir.Payload.Length == 0) return 0;
                return ir.Payload.Length >= 5 ? ir.Payload[4] : ir.Payload[0];
            }

            // ---- emission: entry ---------------------------------------------------

            protected string StackType => "memref<" + Math.Max(1, m_MaxDepth).ToString(CultureInfo.InvariantCulture) + "xi64>";

            /// <summary>Block label used for method exit (host ^exit / kernel ^kexit).</summary>
            protected virtual string ExitLabel => "^exit";

            protected virtual void EmitEntry()
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

            protected void EmitBlock(Block b)
            {
                m_Stack.Clear();
                EmitBody("  ^b{0}:", b.Start);

                ReloadStack(b);

                for (int i = b.Start; i < b.End; i++)
                {
                    var ir = m_Code[i];
                    EmitBody("    // [{0}] {1}", i, ir.opCode);
                    if (EmitInstructionOverride(ir, i)) continue;
                    if (IsTerminator(ir.opCode))
                    {
                        EmitTerminator(b, ir, i);
                        return;
                    }
                    EmitInstruction(ir, i);
                }

                // fell off the end of the block: spill and continue
                SpillAll();
                if (b.End >= m_Count) EmitBody("    cf.br {0}", ExitLabel);
                else EmitBody("    cf.br ^b{0}", b.End);
            }

            /// <summary>
            /// Reload the block's entry stack profile from the spill area
            /// (host: memref %stack, kernel: llvm GEP into %kstk).
            /// </summary>
            protected virtual void ReloadStack(Block b)
            {
                var entry = b.Entry!;
                for (int i = 0; i < entry.Count; i++)
                {
                    string v = NV();
                    EmitBody("    {0} = memref.load %stack[{1}] : {2}", v, CIDX(i), StackType);
                    m_Stack.Add(new Val(v, entry[i].Type, entry[i].TypeId));
                }
            }

            /// <summary>
            /// Kernel hook: return true when the instruction at pos is dropped
            /// or rewritten by GPU loop parallelization (loop init / step).
            /// </summary>
            protected virtual bool EmitInstructionOverride(IRData ir, int pos) => false;

            protected void EmitTerminator(Block b, IRData ir, int pos)
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
                        EmitBody("    cf.br {0}", ExitLabel);
                        return;

                    default:
                        throw Fail($"[{pos}] '{ir.opCode}' is not a terminator");
                }
            }

            // ---- emission: exit ---------------------------------------------------

            protected virtual void EmitExit()
            {
                EmitBody("  ^exit:");
                string r = NV();
                EmitBody("    {0} = memref.load %retval[{1}] : memref<1xi64>", r, CIDX(0));

                // struct return: ret prefill protocol (design §5.6). The C
                // caller already set ret->kind=2 and ret->data=<stack buffer>;
                // flatten-copy NativeSize bytes from the returned buffer
                // pointer straight into that buffer. Never rewrite %ret and
                // never touch kind (both are owned by the caller).
                if (m_Slots.HasRet && m_Slots.RetType == SLType.Struct)
                {
                    var info = m_TypeTable.Find(m_Slots.RetTypeId)
                        ?? throw Fail("return struct type is not registered");
                    string rv = NV();
                    EmitBody("    {0} = llvm.load %ret : !llvm.ptr -> !slv", rv);
                    string rd = NV();
                    EmitBody("    {0} = llvm.extractvalue {1}[1] : !slv", rd, rv);
                    // null guards: a null buffer or a null prefill degrades to
                    // a zero-length copy routed through %ret itself (always a
                    // valid address) instead of a wild memcpy.
                    string rn = NV();
                    EmitBody("    {0} = arith.cmpi ne, {1}, {2} : i64", rn, r, CI64(0));
                    string dn = NV();
                    EmitBody("    {0} = arith.cmpi ne, {1}, {2} : i64", dn, rd, CI64(0));
                    string ok = NV();
                    EmitBody("    {0} = arith.andi {1}, {2} : i1", ok, rn, dn);
                    string reti = NV();
                    EmitBody("    {0} = llvm.ptrtoint %ret : !llvm.ptr to i64", reti);
                    string rs = NV();
                    EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", rs, rn, r, reti);
                    string ds = NV();
                    EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", ds, dn, rd, reti);
                    string sz = NV();
                    EmitBody("    {0} = arith.select {1}, {2}, {3} : i64",
                        sz, ok, CI64(info.NativeSize), CI64(0));
                    string sp = NV();
                    EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", sp, rs);
                    string dp = NV();
                    EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", dp, ds);
                    /* LLVM 21+: llvm.intr.memcpy dropped the volatile flag
                     * operand (dst, src, len only); it became the
                     * isVolatile bool attribute instead. */
                    EmitBody("    \"llvm.intr.memcpy\"({0}, {1}, {2}) <{{isVolatile = false}}> : (!llvm.ptr, !llvm.ptr, i64) -> ()",
                        dp, sp, sz);
                    EmitBody("    return {0} : i64", CI64(0));
                    return;
                }

                /* The emitted kind must equal the manifest RetSlot
                 * (SlotTable.AbiKindOf): F32 returns carry the f64
                 * widening bit pattern, so they cross as kind=1 (the C
                 * side unpacks via memcpy, never via an integer coerce);
                 * ObjRef returns cross as kind=3 (opaque VMObject*).
                 * Struct (kind=2) returned via the prefill branch above. */
                int kind = !m_Slots.HasRet ? 0 : SlotTable.AbiKindOf(m_Slots.RetType);
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
            protected virtual void SpillAll()
            {
                for (int i = 0; i < m_Stack.Count; i++)
                    EmitBody("    memref.store {0}, %stack[{1}] : {2}", m_Stack[i].Name, CIDX(i), StackType);
            }

            // ---- emission: instructions -------------------------------------------

            protected void EmitInstruction(IRData ir, int pos)
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
                        EmitLoadArgument(ir, pos);
                        return;

                    case EIROpCode.LoadLocal:
                        EmitLoadLocal(ir, pos);
                        return;

                    case EIROpCode.StoreLocal:
                        EmitStoreLocal(ir, pos);
                        return;

                    case EIROpCode.StoreArgument:
                        EmitStoreArgument(ir, pos);
                        return;

                    case EIROpCode.StoreReturn:
                        EmitStoreReturn(ir, pos);
                        return;

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
                    case EIROpCode.Convert_R4:
                        EmitConvertR4(pos);
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

                    case EIROpCode.StoreArrayIndex:
                    case EIROpCode.StoreArrayIndexField:
                        EmitArrayStore(ir, pos);
                        return;

                    case EIROpCode.CallVirt:
                        EmitCallVirt(ir, pos);
                        return;

                    case EIROpCode.LoadNotStaticField:
                        EmitMemberLoad(ir, pos);
                        return;

                    case EIROpCode.StoreNotStaticField1:
                    case EIROpCode.StoreNotStaticField2:
                        EmitMemberStore(ir, pos);
                        return;

                    default:
                        throw Fail($"[{pos}] unsupported opcode '{ir.opCode}'");
                }
            }

            // ---- data struct member access (design §5.9) -------------------------
            //
            // A Struct value on the stack is the i64 bit pattern of a
            // pointer to the full native buffer ([32B !sl_meta header][naturally
            // aligned member region]); member offsets already include the
            // header. Receivers are never null: struct values only enter the
            // frame as marshalled arguments (kind=2) or as inlined nested
            // members, both backed by live buffers.

            /// <summary>
            /// LoadNotStaticField: [receiver] -> [member value]. Scalar
            /// members are widened onto the i64/f64-bit-pattern stack;
            /// nested data members use the "ptr-32 trick": the pushed value
            /// is (buf + memberOffset) - 32, so a following member access
            /// (+32 + innerOffset) lands exactly on buf + memberOffset +
            /// innerOffset.
            /// </summary>
            protected virtual void EmitMemberLoad(IRData ir, int pos)
            {
                Val recv = Pop(pos);
                var info = m_TypeTable.Find(recv.TypeId)
                    ?? throw Fail($"[{pos}] LoadNotStaticField receiver struct type {recv.TypeId} is not registered");
                var mem = info.FindMember(ir.index)
                    ?? throw Fail($"[{pos}] LoadNotStaticField member index {ir.index} out of range for '{info.FullName}'");

                string rp = NV();
                EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", rp, recv.Name);
                string p = NV();
                if (mem.Slot == AotMemberSlot.Data)
                {
                    EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                        p, rp, CI64(mem.Offset - AotTypeInfo.MetaHeaderSize));
                    string q = NV();
                    EmitBody("    {0} = llvm.ptrtoint {1} : !llvm.ptr to i64", q, p);
                    m_Stack.Add(new Val(q, SLType.Struct, mem.NestedTypeId));
                    return;
                }

                EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                    p, rp, CI64(mem.Offset));
                string ty = AotTypeTable.ScalarLlvmType(mem.Width, mem.IsFloat);
                string w = NV();
                EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> {2}", w, p, ty);
                if (mem.IsFloat)
                {
                    if (mem.Width == 4)
                    {
                        // promote exactly like the VM (f32 domain value
                        // carried as the exact f64 widening)
                        string d = NV();
                        EmitBody("    {0} = llvm.fpext {1} : f32 to f64", d, w);
                        string b = NV();
                        EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", b, d);
                        m_Stack.Add(new Val(b, SLType.F32));
                    }
                    else
                    {
                        string b = NV();
                        EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", b, w);
                        m_Stack.Add(new Val(b, SLType.F64));
                    }
                }
                else
                {
                    string z = NV();
                    EmitBody("    {0} = llvm.{1} {2} : {3} to i64",
                        z, mem.Signed ? "sext" : "zext", w, ty);
                    m_Stack.Add(new Val(z, SLType.I64));
                }
            }

            /// <summary>
            /// StoreNotStaticField1/2: [inst, val] -> [inst] / []. Scalar
            /// members narrow the stack value to the member width; nested
            /// data members copy the inner region (memberOffset, +InnerSize)
            /// from the value buffer (+32 header) via memcpy.
            /// </summary>
            protected virtual void EmitMemberStore(IRData ir, int pos)
            {
                Val v = Pop(pos);
                Val recv = Pop(pos);
                var info = m_TypeTable.Find(recv.TypeId)
                    ?? throw Fail($"[{pos}] member store receiver struct type {recv.TypeId} is not registered");
                var mem = info.FindMember(ir.index)
                    ?? throw Fail($"[{pos}] member store index {ir.index} out of range for '{info.FullName}'");

                string rp = NV();
                EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", rp, recv.Name);

                if (mem.Slot == AotMemberSlot.Data)
                {
                    string dstp = NV();
                    EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                        dstp, rp, CI64(mem.Offset));
                    string vp = NV();
                    EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", vp, v.Name);
                    string srcp = NV();
                    EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                        srcp, vp, CI64(AotTypeInfo.MetaHeaderSize));
                    EmitBody("    \"llvm.intr.memcpy\"({0}, {1}, {2}) <{{isVolatile = false}}> : (!llvm.ptr, !llvm.ptr, i64) -> ()",
                        dstp, srcp, CI64(mem.Size));
                }
                else
                {
                    string ty = AotTypeTable.ScalarLlvmType(mem.Width, mem.IsFloat);
                    string w;
                    if (mem.IsFloat && mem.Width == 4)
                    {
                        string d = NV();
                        EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", d, v.Name);
                        w = NV();
                        EmitBody("    {0} = llvm.fptrunc {1} : f64 to f32", w, d);
                    }
                    else if (mem.Width == 8)
                    {
                        w = v.Name; // f64: the i64 bits are the f64 pattern
                    }
                    else
                    {
                        w = NV();
                        EmitBody("    {0} = llvm.trunc {1} : i64 to {2}", w, v.Name, ty);
                    }
                    string p = NV();
                    EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                        p, rp, CI64(mem.Offset));
                    EmitBody("    llvm.store {0}, {1} : {2}, !llvm.ptr", w, p, ty);
                }

                // StoreNotStaticField1 keeps the receiver on the stack
                // (data brace-init chains several stores through it).
                if (ir.opCode == EIROpCode.StoreNotStaticField1)
                    m_Stack.Add(recv);
            }

            // ---- slot access emitters (host: memref slabs; kernel: overridden) ----

            protected virtual void EmitLoadArgument(IRData ir, int pos)
            {
                SLType ty = m_Slots.GetArgType(ir.index);
                string v = NV();
                EmitBody("    {0} = memref.load %argmem[{1}] : memref<{2}xi64>", v, CIDX(ir.index), m_Slots.ArgSlots);
                m_Stack.Add(new Val(v, ty, m_Slots.GetArgTypeId(ir.index)));
            }

            protected virtual void EmitLoadLocal(IRData ir, int pos)
            {
                SLType ty = m_Slots.GetLocalType(ir.index);
                string v = NV();
                EmitBody("    {0} = memref.load %locals[{1}] : memref<{2}xi64>", v, CIDX(ir.index), m_Slots.LocalSlots);
                m_Stack.Add(new Val(v, ty, m_Slots.GetLocalTypeId(ir.index)));
            }

            protected virtual void EmitStoreLocal(IRData ir, int pos)
            {
                Val v = Pop(pos);
                string c = CoerceStore(v, m_Slots.GetLocalType(ir.index), m_Slots.GetLocalTypeId(ir.index), $"StoreLocal {ir.index}");
                EmitBody("    memref.store {0}, %locals[{1}] : memref<{2}xi64>", c, CIDX(ir.index), m_Slots.LocalSlots);
            }

            protected virtual void EmitStoreArgument(IRData ir, int pos)
            {
                Val v = Pop(pos);
                string c = CoerceStore(v, m_Slots.GetArgType(ir.index), m_Slots.GetArgTypeId(ir.index), $"StoreArgument {ir.index}");
                EmitBody("    memref.store {0}, %argmem[{1}] : memref<{2}xi64>", c, CIDX(ir.index), m_Slots.ArgSlots);
            }

            protected virtual void EmitStoreReturn(IRData ir, int pos)
            {
                if (ir.index != 0)
                    throw Fail($"[{pos}] StoreReturn with index {ir.index} (only slot 0 is supported)");
                if (!m_Slots.HasRet)
                    throw Fail($"[{pos}] StoreReturn in a method without a return value");
                Val v = Pop(pos);
                string c = CoerceStore(v, m_Slots.RetType, m_Slots.RetTypeId, "StoreReturn");
                EmitBody("    memref.store {0}, %retval[{1}] : memref<1xi64>", c, CIDX(0));
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
                /// <summary>Per-arg data type id (0 for scalar kinds).</summary>
                public readonly int[] ArgTypeIds;
                public CalleeInfo(string id, int argc, bool hasRet, SLType retType,
                    SLType[] argTypes, int[] argTypeIds)
                {
                    Id = id; Argc = argc; HasRet = hasRet; RetType = retType;
                    ArgTypes = argTypes; ArgTypeIds = argTypeIds;
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
                    if (SlotTable.IsVoidType(rv)) hasRet = false;   /* void callee: no return value */
                    else
                    {
                        var rvt = SlotTable.ResolveVarType(m_Slots, rv);
                        // bridge returns are marshalled to a fresh VMObject
                        // only for scalars in this version (§5.7)
                        if (rvt.Type == SLType.Struct || rvt.Type == SLType.ObjRef)
                            throw Fail($"[{pos}] CallStatic callee '{calleeId}' returns a struct/object reference, which the reverse bridge does not support");
                        retType = rvt.Type;
                    }
                }

                var argTypes = new SLType[argc];
                var argTypeIds = new int[argc];
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
                        var avt = SlotTable.ResolveVarType(m_Slots, av);
                        argTypes[slot] = avt.Type;
                        argTypeIds[slot] = avt.TypeId;
                    }
                }

                return new CalleeInfo(calleeId, argc, hasRet, retType, argTypes, argTypeIds);
            }

            protected virtual void EmitCallStatic(IRData ir, int pos)
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

                // Pack each arg into !slv. C ABI kinds (§5.6):
                //   0 = i64 bits, 1 = f64 bits, 2 = struct native buffer,
                //   3 = VMObject opaque reference.
                // Args are coerced to the callee's declared slot type first
                // (the VM converts arguments on call-in, e.g. Float32 -> f64).
                var slvNames = new string[ci.Argc];
                for (int i = 0; i < ci.Argc; i++)
                {
                    SLType at = ci.ArgTypes[i];
                    /* F32 args hold the f64 widening bits as well; kind=1 so
                     * the host bridge pushes an f64 the callee binds to F32. */
                    int kind = (at == SLType.F64 || at == SLType.F32) ? 1
                        : at == SLType.Struct ? 2
                        : at == SLType.ObjRef ? 3 : 0;
                    string c = CoerceStore(vals[i], at, ci.ArgTypeIds[i], $"CallStatic arg {i}");
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

                // Callee id -> const char* (first element of the array global;
                // the array type must match the global, incl. the NUL byte).
                string pg = NV();
                EmitBody("    {0} = llvm.mlir.addressof {1} : !llvm.ptr", pg, globalName);
                int idLen = Encoding.UTF8.GetByteCount(ci.Id) + 1;
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

            protected virtual void EmitArrayLoad(IRData ir, int pos)
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

            /// <summary>
            /// Host array store: mirror of EmitArrayLoad. Null/OOB stores are
            /// silently skipped by routing the address into the sentinel's
            /// data slot (a 72-byte dummy alloca; bytes [64..72) are written
            /// but never read) - matching the VM's behavior of ignoring
            /// vm_array_try_store_value failures.
            /// </summary>
            protected virtual void EmitArrayStore(IRData ir, int pos)
            {
                Val value;
                Val? idxVal = null;
                Val arr;
                if (ir.opCode == EIROpCode.StoreArrayIndexField)
                {
                    value = Pop(pos);                 // top: value
                    idxVal = Pop(pos);                // then: idx
                    arr = Pop(pos);                   // then: array
                    if (idxVal.Value.Type != SLType.I64)
                        throw Fail($"[{pos}] array store index is {idxVal.Value.Type}, expected an integer");
                }
                else // StoreArrayIndex: payload flag decides pop order
                {
                    if (StoreIndexFlagOf(ir) == 0)
                    {
                        arr = Pop(pos);               // top: array
                        value = Pop(pos);             // below: value
                    }
                    else
                    {
                        value = Pop(pos);             // top: value
                        arr = Pop(pos);               // below: array
                    }
                }
                if (!IsArrayType(arr.Type))
                    throw Fail($"[{pos}] {ir.opCode} receiver is {arr.Type}, expected an array");
                string idx = idxVal.HasValue ? idxVal.Value.Name : CI64(ir.index);

                // coerce the value to the array's element type (the VM's
                // vm_array_try_store_value converts numerically)
                string ety = MlirElemTypeOf(arr.Type);
                string val = CoerceToElem(value, arr.Type);

                string okp = NV();
                EmitBody("    {0} = arith.cmpi ne, {1}, {2} : i64", okp, arr.Name, CI64(0));
                string basei = NV();
                EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", basei, okp, arr.Name, ArraySentinelI64());
                string bas = NV();
                EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", bas, basei);

                string lenp = NV();
                EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8",
                    lenp, bas, CI64(48));
                string len32 = NV();
                EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i32", len32, lenp);
                string len = NV();
                EmitBody("    {0} = arith.extui {1} : i32 to i64", len, len32);
                string okb = NV();
                EmitBody("    {0} = arith.cmpi ult, {1}, {2} : i64", okb, idx, len);
                string ok = NV();
                EmitBody("    {0} = arith.andi {1}, {2} : i1", ok, okp, okb);

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
                // bad stores land in the sentinel's data slot (bytes 64..72)
                string sdat = NV();
                EmitBody("    {0} = arith.addi {1}, {2} : i64", sdat, ArraySentinelI64(), CI64(64));
                string eps = NV();
                EmitBody("    {0} = arith.select {1}, {2}, {3} : i64", eps, ok, epi, sdat);
                string ep = NV();
                EmitBody("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", ep, eps);

                EmitBody("    llvm.store {0}, {1} : {2}, !llvm.ptr", val, ep, ety);
            }

            /// <summary>MLIR element type of an array slot (f64 / i32 / i64).</summary>
            protected static string MlirElemTypeOf(SLType arrayType)
                => arrayType == SLType.ArrayF64 ? "f64"
                : arrayType == SLType.ArrayI32 ? "i32" : "i64";

            /// <summary>
            /// Coerce a stack value to an array's raw element type for a
            /// direct store (numeric conversion, matching
            /// vm_array_try_store_value). Returns an SSA value already typed
            /// f64/i64/i32.
            /// </summary>
            protected string CoerceToElem(Val v, SLType arrayType)
            {
                if (arrayType == SLType.ArrayF64)
                    return ToF64(v);
                if (arrayType == SLType.ArrayI32)
                {
                    if (v.Type == SLType.I64)
                    {
                        string ri32 = NV();
                        EmitBody("    {0} = arith.trunci {1} : i64 to i32", ri32, v.Name);
                        return ri32;
                    }
                    string f32 = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f32, v.Name);
                    string r32 = NV();
                    EmitBody("    {0} = arith.fptosi {1} : f64 to i32", r32, f32);
                    return r32;
                }
                // ArrayI64
                if (v.Type == SLType.I64) return v.Name;
                string f = NV();
                EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f, v.Name);
                string r = NV();
                EmitBody("    {0} = arith.fptosi {1} : f64 to i64", r, f);
                return r;
            }

            protected virtual void EmitCallVirt(IRData ir, int pos)
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

            protected readonly struct VirtCalleeInfo
            {
                public readonly IRMethod Method;
                public readonly string Name;
                public readonly int Argc;
                public VirtCalleeInfo(IRMethod method, string name, int argc)
                { Method = method; Name = name; Argc = argc; }
            }

            protected VirtCalleeInfo ResolveVirtCallee(IRData ir, int pos)
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
            protected static bool IsTrivialArrayLengthGetter(IRMethod m)
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

            protected static string DumpIRShape(IRMethod m)
            {
                if (m == null) return "<null method>";
                if (m.IRDataList == null) return "<null body>";
                var parts = new List<string>();
                foreach (var d in m.IRDataList)
                    parts.Add(d == null ? "<null>" : $"{d.opCode} {d.index}");
                return string.Join(", ", parts);
            }

            protected void EmitArith(EIROpCode op, int pos)
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

            protected void EmitBitwise(EIROpCode op, int pos)
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

            protected void EmitLogical(EIROpCode op, int pos)
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

            protected void EmitCompare(EIROpCode op, int pos)
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

            protected void EmitNot(int pos)
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

            protected void EmitNeg(int pos)
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
            protected void EmitConvert(int pos, int bits, bool unsigned)
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

            protected void EmitConvertR8(int pos)
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

            protected void EmitConvertR4(int pos)
            {
                // C VM: runtime_value_convert_by_etype(EVMType_Float32) =
                // (float32)dval, stored with etype=Float32 and the payload
                // being the exact f64 widening of that f32. Mirror that:
                // round via fptrunc f64->f32, re-widen via fpext, keep the
                // f64 bit pattern but type the stack slot as F32.
                Val v = Pop(pos);
                if (v.Type == SLType.F32)
                {
                    m_Stack.Add(new Val(v.Name, SLType.F32));
                    return;
                }
                string w = v.Name;
                if (v.Type == SLType.I64)
                {
                    string f = NV();
                    EmitBody("    {0} = arith.sitofp {1} : i64 to f64", f, v.Name);
                    w = f;
                }
                string t = NV();
                EmitBody("    {0} = arith.fptrunc {1} : f64 to f32", t, w);
                string d = NV();
                EmitBody("    {0} = arith.fpext {1} : f32 to f64", d, t);
                string r = NV();
                EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", r, d);
                m_Stack.Add(new Val(r, SLType.F32));
            }

            protected static string CmpIntPred(EIROpCode op)
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

            protected static string CmpFloatPred(EIROpCode op)
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
            protected string CoerceStore(Val v, SLType slotTy, string what)
                => CoerceStore(v, slotTy, 0, what);

            protected string CoerceStore(Val v, SLType slotTy, int slotTypeId, string what)
            {
                if (v.Type == slotTy)
                {
                    // struct identity: exact data type (no cross-type
                    // reinterpret); objref: opaque pass-through
                    if (slotTy == SLType.Struct && v.TypeId != slotTypeId)
                        throw Fail($"type mismatch at {what}: value is Struct#{v.TypeId}, slot is Struct#{slotTypeId}");
                    return v.Name;
                }
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
            protected string ToF64(Val v)
            {
                string f = NV();
                if (v.Type != SLType.I64)
                    EmitBody("    {0} = llvm.bitcast {1} : i64 to f64", f, v.Name);
                else
                    EmitBody("    {0} = arith.sitofp {1} : i64 to f64", f, v.Name);
                return f;
            }

            /// <summary>Reduce a stack Val to an i1 truth value (NaN counts as true).</summary>
            protected string Truthy(Val v)
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

            protected static long ConstI64(IRData ir)
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

            protected static long ConstF64Bits(IRData ir)
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

            protected static string ConstSuffix(long v)
                => v >= 0 ? v.ToString(CultureInfo.InvariantCulture)
                          : "m" + ((ulong)(-v)).ToString(CultureInfo.InvariantCulture);

            /// <summary>Hoisted i64 constant (entry block, dominates all uses).</summary>
            protected string CI64(long v)
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
            protected string CK32(int v)
            {
                if (m_K32Consts.TryGetValue(v, out string name)) return name;
                name = "%k_" + ConstSuffix(v);
                EmitConst("    {0} = llvm.mlir.constant({1} : i32) : i32", name, v.ToString(CultureInfo.InvariantCulture));
                m_K32Consts[v] = name;
                return name;
            }

            /// <summary>0.0 as f64, materialized once (used by NaN-safe comparisons).</summary>
            protected string F64Zero()
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

            protected string NV() => "%v" + (m_TmpCounter++).ToString(CultureInfo.InvariantCulture);

            protected Val Pop(int pos)
            {
                if (m_Stack.Count < 1) throw Fail($"[{pos}] stack underflow");
                Val v = m_Stack[m_Stack.Count - 1];
                m_Stack.RemoveAt(m_Stack.Count - 1);
                return v;
            }

            protected EmitFailException Fail(string message)
                => new EmitFailException("[" + (m_Method.id ?? m_Method.onlyFunctionName ?? "?") + "] " + message);

            protected void EmitBody(string format, params object?[] args)
            {
                m_BodySb.AppendFormat(CultureInfo.InvariantCulture, format, args).Append('\n');
            }

            protected void EmitConst(string format, params object?[] args)
            {
                m_ConstSb.AppendFormat(CultureInfo.InvariantCulture, format, args).Append('\n');
            }
        }

        // ------------------------------------------------------------------
        // GpuMethodEmitter
        //
        // A @GPU()+@AOT() static void method is lowered to two artifacts:
        //
        //  1. gpu.module @gpu_k{N} [#nvvm.target<chip = "sm_75">] containing
        //     gpu.func @kernel kernel — the method body translated with the
        //     OUTERMOST for-loop parallelized as a grid-stride loop:
        //       i starts at blockIdx.x * blockDim.x + threadIdx.x
        //       i steps by  gridDim.x  * blockDim.x
        //     so the same kernel is correct for any grid the host picks.
        //
        //  2. func.func @sym (sl_value ABI) — a host wrapper that unpacks the
        //     sl_value args, dereferences the VMArrays (length +48 / data +64),
        //     stages device buffers (slgpuMalloc + HtoD), computes
        //     gx = ceil(bound / blockDimX) from the loop bound (or the @GPU
        //     gridDimX attribute), launches the kernel, copies results back
        //     (DtoH) and frees the buffers.
        //
        // Kernel parameter ABI (fixed order):
        //   per array arg slot (ascending): %pa{slot}: !llvm.ptr (device
        //     element buffer), %ln{slot}: i64 (element count)
        //   then per scalar arg slot (ascending): %sa{slot}: i64
        //     (Float64 args pass their bit pattern)
        //
        // v1 constraints (fail with a clear message):
        //   - the method must be void and static
        //   - the body needs a for loop 'for i = 0, i < bound, i += 1' whose
        //     bound is one LoadArgument (scalar), one constant, or
        //     LoadArgument(array) + arr.length
        //   - arrays are only accessed through their parameters (array-typed
        //     locals are rejected); kernel stores are unguarded (indices are
        //     trusted in-range, like the reference compute)
        //   - no CallStatic (VM bridge) inside kernels
        //   - @GPU attr: blockDimX/Y/Z, gridDimX/Y/Z and kernelName are used;
        //     tileSize*/tileNum/groupId/sharedMemorySize/deviceId are carried
        //     by the attribute for future tiled-kernel work
        // ------------------------------------------------------------------

        private sealed class GpuMethodEmitter : MethodEmitter
        {
            private enum BoundKind { ScalarArg, Const, ArrayLength }

            private static string GpuArch
                => Environment.GetEnvironmentVariable("SIMPLELANG_GPU_ARCH") is string a
                   && !string.IsNullOrWhiteSpace(a) ? a : "sm_75";

            private readonly int m_ModuleIndex;

            // resolved launch config (attribute defaults applied)
            private readonly int m_Bx, m_By, m_Bz;
            private readonly int m_Gy, m_Gz;
            private readonly int m_GxAttr;         // <= 0 = auto (from loop bound)
            private readonly string m_KernelName;

            // argument slot groups (ascending)
            private readonly List<int> m_ArraySlots = new List<int>();
            private readonly List<int> m_ScalarSlots = new List<int>();

            // loop-injection analysis
            private readonly HashSet<int> m_SkipInit = new HashSet<int>(); // dropped instrs
            private int m_StepConstPos = -1;        // step LoadConst(1) -> %kstride
            private int m_LoopVar = -1;
            private BoundKind m_BoundKind = BoundKind.Const;
            private int m_BoundSlot = -1;
            private long m_BoundConst;

            // host-wrapper emission state (separate SSA namespace from kernel)
            private readonly StringBuilder m_HostConstSb = new StringBuilder();
            private readonly StringBuilder m_HostSb = new StringBuilder();
            private readonly Dictionary<long, string> m_HostI64Consts = new Dictionary<long, string>();
            private readonly Dictionary<int, string> m_HostK32Consts = new Dictionary<int, string>();
            private bool m_HostSentinelEmitted;
            private string m_HostSentinelName = "";
            private int m_HostTmp = 0;

            public GpuMethodEmitter(IRMethod method, HashSet<string> usedSymbols,
                Dictionary<string, string> bridgeIds, AotTypeTable typeTable, int moduleIndex)
                : base(method, usedSymbols, bridgeIds, typeTable)
            {
                if (m_Slots.HasRet)
                    throw Fail("GPU methods must be void (no return value)");

                m_ModuleIndex = moduleIndex;
                var attr = method.gpuAttribute;

                int bx = attr?.GetIntArg(7) ?? 0;   // blockDimX
                int by = attr?.GetIntArg(8) ?? 0;   // blockDimY
                int bz = attr?.GetIntArg(9) ?? 0;   // blockDimZ
                m_Bx = bx > 0 ? bx : 256;
                m_By = by > 0 ? by : 1;
                m_Bz = bz > 0 ? bz : 1;
                int gy = attr?.GetIntArg(5) ?? 0;   // gridDimY
                int gz = attr?.GetIntArg(6) ?? 0;   // gridDimZ
                m_Gy = gy > 0 ? gy : 1;
                m_Gz = gz > 0 ? gz : 1;
                m_GxAttr = attr?.GetIntArg(4) ?? 0; // gridDimX (<=0: auto)

                string kname = "";
                var raw = attr?.GetSplitRawArgs();
                if (raw != null && raw.Count > 12 && !string.IsNullOrEmpty(raw[12]))
                    kname = raw[12].Trim();
                m_KernelName = string.IsNullOrEmpty(kname)
                    ? SanitizeSymbol(m_Method.onlyFunctionName ?? m_Method.id ?? "kernel")
                    : SanitizeSymbol(kname);

                foreach (int slot in m_Slots.ArgSlotList)
                {
                    SLType at = m_Slots.GetArgType(slot);
                    // struct buffers and VMObject references cannot cross the
                    // kernel boundary (no marshal path on the launch side)
                    if (at == SLType.Struct || at == SLType.ObjRef)
                        throw Fail($"GPU kernels do not support struct/object-reference parameters (slot {slot})");
                    if (IsArrayType(at)) m_ArraySlots.Add(slot);
                    else m_ScalarSlots.Add(slot);
                }
                foreach (var kv in m_Slots.LocalTypes)
                {
                    if (kv.Value == SLType.Struct || kv.Value == SLType.ObjRef)
                        throw Fail($"GPU kernels do not support struct/object-reference locals (slot {kv.Key})");
                }
            }

            public override string Emit()
            {
                CheckSupported();
                AnalyzeLoop();
                AnalyzeBlocks();
                ComputeProfiles();
                EmitEntry();            // kernel prologue
                EmitAllBlocks();
                EmitExit();             // ^kexit: gpu.return

                var sb = new StringBuilder();
                sb.Append("  // gpu kernel for aot method id: ").Append(m_Method.id).Append('\n');
                sb.Append("  gpu.module @gpu_k").Append(m_ModuleIndex.ToString(CultureInfo.InvariantCulture))
                  .Append(" [#nvvm.target<chip = \"").Append(GpuArch).Append("\">] {\n");
                sb.Append("    gpu.func @").Append(m_KernelName).Append('(')
                  .Append(KernelSignature()).Append(") kernel {\n");
                sb.Append(m_ConstSb);
                sb.Append(m_BodySb);
                sb.Append("    }\n");
                sb.Append("  }\n\n");
                sb.Append(EmitHostWrapper());
                return sb.ToString();
            }

            // ---- loop recognition --------------------------------------------------

            private void AnalyzeLoop()
            {
                // collect back edges (Br to an earlier position); parallelize
                // the loop with the smallest head (outermost/first loop), and
                // for equal heads prefer the latest back edge (the natural
                // loop bottom after 'continue' branches)
                var backEdges = new List<(int head, int pos)>();
                for (int i = 0; i < m_Count; i++)
                {
                    var ir = m_Code[i];
                    if (ir.opCode == EIROpCode.Br && ir.index < i)
                        backEdges.Add((ir.index, i));
                }
                if (backEdges.Count == 0)
                    throw Fail("no for-loop found to parallelize (a @GPU method body needs a for loop)");
                backEdges.Sort((a, b) => a.head != b.head ? a.head.CompareTo(b.head) : b.pos.CompareTo(a.pos));

                string lastError = "";
                foreach (var (head, brPos) in backEdges)
                {
                    try { TryAnalyzeLoopAt(head, brPos); return; }
                    catch (EmitFailException ex) { lastError = ex.Message; }
                }
                throw Fail("cannot recognize the for-loop to parallelize; last candidate: " + lastError);
            }

            private void TryAnalyzeLoopAt(int head, int brPos)
            {
                // ---- step: LoadLocal(v), LoadConst(1), Add, StoreLocal(v) ----
                int q = brPos - 1;
                while (q >= 0 && (m_Code[q].opCode == EIROpCode.Nop || m_Code[q].opCode == EIROpCode.Label)) q--;
                if (q < 3
                    || m_Code[q].opCode != EIROpCode.StoreLocal
                    || m_Code[q - 1].opCode != EIROpCode.Add
                    || !IsLoadConstOp(m_Code[q - 2].opCode)
                    || m_Code[q - 3].opCode != EIROpCode.LoadLocal
                    || m_Code[q - 3].index != m_Code[q].index)
                    throw Fail($"[{q}] for-loop step is not a plain 'i += 1'");
                int loopVar = m_Code[q].index;
                if (ConstI64(m_Code[q - 2]) != 1)
                    throw Fail("for-loop step must be '+1' (grid stride is applied automatically)");

                // ---- init: drop every LoadConst + StoreLocal(v) pair before the head ----
                bool anyInit = false;
                int p = head - 1;
                while (p >= 1)
                {
                    if (m_Code[p].opCode == EIROpCode.Nop) { p--; continue; }
                    if (m_Code[p].opCode != EIROpCode.StoreLocal || m_Code[p].index != loopVar) break;
                    if (!IsLoadConstOp(m_Code[p - 1].opCode)) break;
                    m_SkipInit.Add(p);
                    m_SkipInit.Add(p - 1);
                    anyInit = true;
                    p -= 2;
                }
                if (!anyInit)
                    throw Fail("for-loop init (i = <const>) not found before the loop head");

                // ---- condition: LoadLocal(v), bound, Clt|Cle, BrFalse ----
                int c = head + 1;
                while (c < m_Count && (m_Code[c].opCode == EIROpCode.Nop || m_Code[c].opCode == EIROpCode.Label)) c++;
                if (c + 2 >= m_Count
                    || m_Code[c].opCode != EIROpCode.LoadLocal
                    || m_Code[c].index != loopVar)
                    throw Fail("for-loop condition must start with the loop variable (write 'i < n' / 'i <= n')");
                int b = c + 1;
                int cmpPos;
                if (m_Code[b].opCode == EIROpCode.LoadArgument
                    && (m_Code[b + 1].opCode == EIROpCode.Clt || m_Code[b + 1].opCode == EIROpCode.Cle))
                {
                    if (IsArrayType(m_Slots.GetArgType(m_Code[b].index)))
                        throw Fail("for-loop bound cannot be an array value");
                    m_BoundKind = BoundKind.ScalarArg;
                    m_BoundSlot = m_Code[b].index;
                    cmpPos = b + 1;
                }
                else if (IsLoadConstOp(m_Code[b].opCode)
                    && (m_Code[b + 1].opCode == EIROpCode.Clt || m_Code[b + 1].opCode == EIROpCode.Cle))
                {
                    m_BoundKind = BoundKind.Const;
                    m_BoundConst = ConstI64(m_Code[b]);
                    cmpPos = b + 1;
                }
                else if (m_Code[b].opCode == EIROpCode.LoadArgument
                    && m_Code[b + 1].opCode == EIROpCode.CallVirt
                    && (m_Code[b + 2].opCode == EIROpCode.Clt || m_Code[b + 2].opCode == EIROpCode.Cle)
                    && IsArrayType(m_Slots.GetArgType(m_Code[b].index)))
                {
                    // 'i < arr.length'
                    m_BoundKind = BoundKind.ArrayLength;
                    m_BoundSlot = m_Code[b].index;
                    cmpPos = b + 2;
                }
                else
                {
                    throw Fail("for-loop bound must be a parameter, a constant, or 'arr.length'");
                }

                if (cmpPos + 1 >= m_Count || m_Code[cmpPos + 1].opCode != EIROpCode.BrFalse)
                    throw Fail("for-loop exit branch not recognized");

                m_LoopVar = loopVar;
                m_StepConstPos = q - 2;
            }

            protected override bool EmitInstructionOverride(IRData ir, int pos)
            {
                if (m_SkipInit.Contains(pos)) return true;              // dropped: init stores
                if (pos == m_StepConstPos)
                {
                    // rewrite the '+1' of the step into the grid stride
                    m_Stack.Add(new Val("%kstride", SLType.I64));
                    return true;
                }
                return false;
            }

            // ---- kernel emission ---------------------------------------------------

            private string KernelSignature()
            {
                var parts = new List<string>();
                foreach (int s in m_ArraySlots)
                {
                    parts.Add("%pa" + s.ToString(CultureInfo.InvariantCulture) + ": !llvm.ptr");
                    parts.Add("%ln" + s.ToString(CultureInfo.InvariantCulture) + ": i64");
                }
                foreach (int s in m_ScalarSlots)
                    parts.Add("%sa" + s.ToString(CultureInfo.InvariantCulture) + ": i64");
                return string.Join(", ", parts);
            }

            protected override void CheckSupported()
            {
                base.CheckSupported();
                for (int i = 0; i < m_Count; i++)
                {
                    var op = m_Code[i].opCode;
                    if (op == EIROpCode.CallStatic)
                        throw Fail($"[{i}] CallStatic is not supported inside GPU kernels");
                    // struct member access needs the native buffer view,
                    // which does not exist inside gpu.func
                    if (op == EIROpCode.LoadNotStaticField
                        || op == EIROpCode.StoreNotStaticField1
                        || op == EIROpCode.StoreNotStaticField2)
                        throw Fail($"[{i}] {op} is not supported inside GPU kernels");
                }
            }

            protected override string ExitLabel => "^kexit";

            protected override void EmitEntry()
            {
                // ---- parallel dimension: global thread id + grid stride ----
                EmitBody("    %tix = gpu.thread_id x");
                EmitBody("    %bdx = gpu.block_dim x");
                EmitBody("    %bix = gpu.block_id x");
                EmitBody("    %gdx = gpu.grid_dim x");
                EmitBody("    %koff = arith.muli %bix, %bdx : index");
                EmitBody("    %kidx = arith.addi %koff, %tix : index");
                EmitBody("    %ktid = arith.index_cast %kidx : index to i64");
                EmitBody("    %kgs = arith.muli %gdx, %bdx : index");
                EmitBody("    %kstride = arith.index_cast %kgs : index to i64");

                // ---- locals slab (llvm.alloca: no memref inside gpu.func) ----
                string lc = NV();
                EmitBody("    {0} = llvm.mlir.constant({1} : i64) : i64", lc, m_Slots.LocalSlots);
                EmitBody("    %kloc = llvm.alloca {0} x i64 : (i64) -> !llvm.ptr", lc);
                string zero = CI64(0);
                for (int i = 0; i < m_Slots.LocalSlots; i++)
                {
                    string p = NV();
                    EmitBody("    {0} = llvm.getelementptr %kloc[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, i64", p, CI64(i));
                    EmitBody("    llvm.store {0}, {1} : i64, !llvm.ptr", zero, p);
                }
                // the loop variable starts at the global thread id
                string pv = NV();
                EmitBody("    {0} = llvm.getelementptr %kloc[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, i64", pv, CI64(m_LoopVar));
                EmitBody("    llvm.store %ktid, {0} : i64, !llvm.ptr", pv);

                // ---- spill stack for the virtual machine stack ----
                string dc = NV();
                EmitBody("    {0} = llvm.mlir.constant({1} : i64) : i64", dc, Math.Max(1, m_MaxDepth));
                EmitBody("    %kstk = llvm.alloca {0} x i64 : (i64) -> !llvm.ptr", dc);

                EmitBody("    cf.br ^b{0}", m_Blocks[0].Start);
            }

            protected override void EmitExit()
            {
                EmitBody("  ^kexit:");
                EmitBody("    gpu.return");
            }

            protected override void ReloadStack(Block b)
            {
                var entry = b.Entry!;
                for (int i = 0; i < entry.Count; i++)
                {
                    string p = NV();
                    EmitBody("    {0} = llvm.getelementptr %kstk[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, i64", p, CI64(i));
                    string v = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i64", v, p);
                    m_Stack.Add(new Val(v, entry[i].Type, entry[i].TypeId));
                }
            }

            protected override void SpillAll()
            {
                for (int i = 0; i < m_Stack.Count; i++)
                {
                    string p = NV();
                    EmitBody("    {0} = llvm.getelementptr %kstk[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, i64", p, CI64(i));
                    EmitBody("    llvm.store {0}, {1} : i64, !llvm.ptr", m_Stack[i].Name, p);
                }
            }

            // ---- kernel instruction emitters (override the host slots) -------------

            protected override void EmitLoadArgument(IRData ir, int pos)
            {
                SLType ty = m_Slots.GetArgType(ir.index);
                if (IsArrayType(ty))
                    m_Stack.Add(new Val("%pa" + ir.index.ToString(CultureInfo.InvariantCulture), ty));
                else
                    m_Stack.Add(new Val("%sa" + ir.index.ToString(CultureInfo.InvariantCulture), ty));
            }

            protected override void EmitLoadLocal(IRData ir, int pos)
            {
                SLType ty = m_Slots.GetLocalType(ir.index);
                if (IsArrayType(ty))
                    throw Fail($"[{pos}] array-typed locals are not supported in GPU kernels; use the array parameters directly");
                string p = NV();
                EmitBody("    {0} = llvm.getelementptr %kloc[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, i64", p, CI64(ir.index));
                string v = NV();
                EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i64", v, p);
                m_Stack.Add(new Val(v, ty));
            }

            protected override void EmitStoreLocal(IRData ir, int pos)
            {
                SLType ty = m_Slots.GetLocalType(ir.index);
                if (IsArrayType(ty))
                    throw Fail($"[{pos}] array-typed locals are not supported in GPU kernels");
                Val v = Pop(pos);
                string c = CoerceStore(v, ty, $"StoreLocal {ir.index}");
                string p = NV();
                EmitBody("    {0} = llvm.getelementptr %kloc[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, i64", p, CI64(ir.index));
                EmitBody("    llvm.store {0}, {1} : i64, !llvm.ptr", c, p);
            }

            protected override void EmitStoreArgument(IRData ir, int pos)
                => throw Fail($"[{pos}] GPU kernels cannot assign to parameters");

            protected override void EmitStoreReturn(IRData ir, int pos)
                => throw Fail($"[{pos}] GPU kernels must be void");

            protected override void EmitCallStatic(IRData ir, int pos)
                => throw Fail($"[{pos}] CallStatic is not supported inside GPU kernels");

            /// <summary>Kernel array load: raw device GEP, no VMArray header.</summary>
            protected override void EmitArrayLoad(IRData ir, int pos)
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

                string ety = arr.Type == SLType.ArrayF64 ? "f64"
                    : arr.Type == SLType.ArrayI32 ? "i32" : "i64";
                string ep = NV();
                EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, {3}",
                    ep, arr.Name, idx, ety);
                if (arr.Type == SLType.ArrayF64)
                {
                    string f = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> f64", f, ep);
                    string v = NV();
                    EmitBody("    {0} = llvm.bitcast {1} : f64 to i64", v, f);
                    m_Stack.Add(new Val(v, SLType.F64));
                }
                else if (arr.Type == SLType.ArrayI32)
                {
                    string r32 = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i32", r32, ep);
                    string v = NV();
                    EmitBody("    {0} = arith.extsi {1} : i32 to i64", v, r32);
                    m_Stack.Add(new Val(v, SLType.I64));
                }
                else
                {
                    string v = NV();
                    EmitBody("    {0} = llvm.load {1} : !llvm.ptr -> i64", v, ep);
                    m_Stack.Add(new Val(v, SLType.I64));
                }
            }

            /// <summary>Kernel array store: raw device GEP (indices are trusted).</summary>
            protected override void EmitArrayStore(IRData ir, int pos)
            {
                Val value;
                Val? idxVal = null;
                Val arr;
                if (ir.opCode == EIROpCode.StoreArrayIndexField)
                {
                    value = Pop(pos);
                    idxVal = Pop(pos);
                    arr = Pop(pos);
                    if (idxVal.Value.Type != SLType.I64)
                        throw Fail($"[{pos}] array store index is {idxVal.Value.Type}, expected an integer");
                }
                else
                {
                    if (StoreIndexFlagOf(ir) == 0)
                    {
                        arr = Pop(pos);
                        value = Pop(pos);
                    }
                    else
                    {
                        value = Pop(pos);
                        arr = Pop(pos);
                    }
                }
                if (!IsArrayType(arr.Type))
                    throw Fail($"[{pos}] {ir.opCode} receiver is {arr.Type}, expected an array");
                string idx = idxVal.HasValue ? idxVal.Value.Name : CI64(ir.index);

                string ety = MlirElemTypeOf(arr.Type);
                string val = CoerceToElem(value, arr.Type);
                string ep = NV();
                EmitBody("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, {3}",
                    ep, arr.Name, idx, ety);
                EmitBody("    llvm.store {0}, {1} : {2}, !llvm.ptr", val, ep, ety);
            }

            /// <summary>Kernel arr.length: mapped to the %ln{slot} kernel param.</summary>
            protected override void EmitCallVirt(IRData ir, int pos)
            {
                var vi = ResolveVirtCallee(ir, pos);
                for (int i = vi.Argc - 1; i >= 0; --i) Pop(pos);
                Val recv = Pop(pos);
                if (!IsArrayType(recv.Type))
                    throw Fail($"[{pos}] CallVirt receiver is {recv.Type}, only array getters are inlinable");
                if (!IsTrivialArrayLengthGetter(vi.Method))
                    throw Fail($"[{pos}] CallVirt to '{vi.Name}' is not an inlinable array getter");
                if (!recv.Name.StartsWith("%pa", StringComparison.Ordinal)
                    || !int.TryParse(recv.Name.Substring(3), out int slot))
                    throw Fail($"[{pos}] arr.length is only supported directly on array parameters");
                m_Stack.Add(new Val("%ln" + slot.ToString(CultureInfo.InvariantCulture), SLType.I64));
            }

            // ---- host wrapper -------------------------------------------------------

            private string EmitHostWrapper()
            {
                BuildHostWrapperBody();
                var sb = new StringBuilder();
                sb.Append("  // gpu host wrapper for aot method id: ").Append(m_Method.id).Append('\n');
                sb.Append("  func.func @").Append(Symbol)
                  .Append("(%ctx: !llvm.ptr, %args: !llvm.ptr, %argc: i32, %ret: !llvm.ptr) -> i64 {\n");
                sb.Append(m_HostConstSb);
                sb.Append(m_HostSb);
                sb.Append("  }\n\n");
                return sb.ToString();
            }

            private void BuildHostWrapperBody()
            {
                // ---- 1. unpack sl_value args ----
                var argVal = new Dictionary<int, string>();
                foreach (int slot in m_Slots.ArgSlotList)
                {
                    string p = HNV();
                    HEmit("    {0} = llvm.getelementptr %args[{1}] : (!llvm.ptr, i64) -> !llvm.ptr, !slv", p, HCI64(slot));
                    string s = HNV();
                    HEmit("    {0} = llvm.load {1} : !llvm.ptr -> !slv", s, p);
                    string d = HNV();
                    HEmit("    {0} = llvm.extractvalue {1}[1] : !slv", d, s);
                    argVal[slot] = d;
                }

                // ---- 2. deref VMArrays: length (+48) / data (+64), null-safe ----
                var arrLen = new Dictionary<int, string>();
                var arrDat = new Dictionary<int, string>();
                foreach (int slot in m_ArraySlots)
                {
                    string ap = argVal[slot];
                    string okp = HNV();
                    HEmit("    {0} = arith.cmpi ne, {1}, {2} : i64", okp, ap, HCI64(0));
                    string basi = HNV();
                    HEmit("    {0} = arith.select {1}, {2}, {3} : i64", basi, okp, ap, HostSentinelI64());
                    string bas = HNV();
                    HEmit("    {0} = llvm.inttoptr {1} : i64 to !llvm.ptr", bas, basi);
                    string lp = HNV();
                    HEmit("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8", lp, bas, HCI64(48));
                    string l32 = HNV();
                    HEmit("    {0} = llvm.load {1} : !llvm.ptr -> i32", l32, lp);
                    string ln = HNV();
                    HEmit("    {0} = arith.extui {1} : i32 to i64", ln, l32);
                    arrLen[slot] = ln;
                    string dp = HNV();
                    HEmit("    {0} = llvm.getelementptr {1}[{2}] : (!llvm.ptr, i64) -> !llvm.ptr, i8", dp, bas, HCI64(64));
                    string dat = HNV();
                    HEmit("    {0} = llvm.load {1} : !llvm.ptr -> !llvm.ptr", dat, dp);
                    arrDat[slot] = dat;
                }

                // ---- 3. grid size: gx = ceil(bound / blockDimX) ----
                string gx;
                if (m_GxAttr > 0)
                {
                    gx = HCI64(m_GxAttr);
                }
                else
                {
                    string bound;
                    switch (m_BoundKind)
                    {
                        case BoundKind.ScalarArg: bound = argVal[m_BoundSlot]; break;
                        case BoundKind.Const: bound = HCI64(m_BoundConst); break;
                        default: bound = arrLen[m_BoundSlot]; break;
                    }
                    string nb = HNV();
                    HEmit("    {0} = arith.addi {1}, {2} : i64", nb, bound, HCI64(m_Bx - 1));
                    gx = HNV();
                    HEmit("    {0} = arith.divsi {1}, {2} : i64", gx, nb, HCI64(m_Bx));
                }

                // ---- 4. stage device buffers (v1: copy every array in and out) ----
                var devPtr = new Dictionary<int, string>();
                var devBytes = new Dictionary<int, string>();
                foreach (int slot in m_ArraySlots)
                {
                    SLType at = m_Slots.GetArgType(slot);
                    string bytes = HNV();
                    HEmit("    {0} = arith.muli {1}, {2} : i64", bytes, arrLen[slot], HCI64(ElemWidthOf(at)));
                    string dv = HNV();
                    HEmit("    {0} = llvm.call @slgpuMalloc({1}) : (i64) -> !llvm.ptr", dv, bytes);
                    devPtr[slot] = dv;
                    devBytes[slot] = bytes;
                    HEmit("    llvm.call @slgpuMemcpyHtoD({0}, {1}, {2}) : (!llvm.ptr, !llvm.ptr, i64) -> ()",
                        dv, arrDat[slot], bytes);
                }

                // ---- 5. launch (skip when the grid is empty) ----
                string ok = HNV();
                HEmit("    {0} = arith.cmpi sgt, {1}, {2} : i64", ok, gx, HCI64(0));
                HEmit("    cf.cond_br {0}, ^gpu_launch, ^gpu_after", ok);
                HEmit("  ^gpu_launch:");
                var launchArgs = new List<string>();
                foreach (int slot in m_ArraySlots)
                {
                    launchArgs.Add(devPtr[slot] + ": !llvm.ptr");
                    launchArgs.Add(arrLen[slot] + ": i64");
                }
                foreach (int slot in m_ScalarSlots)
                    launchArgs.Add(argVal[slot] + ": i64");
                HEmit("    gpu.launch_func @gpu_k{0}::@{1} blocks in ({2}, {3}, {4}) threads in ({5}, {6}, {7}) : i64 args({8})",
                    m_ModuleIndex.ToString(CultureInfo.InvariantCulture), m_KernelName,
                    gx, HCI64(m_Gy), HCI64(m_Gz), HCI64(m_Bx), HCI64(m_By), HCI64(m_Bz),
                    string.Join(", ", launchArgs));
                HEmit("    cf.br ^gpu_after");
                HEmit("  ^gpu_after:");

                // ---- 6. copy results back + free ----
                foreach (int slot in m_ArraySlots)
                    HEmit("    llvm.call @slgpuMemcpyDtoH({0}, {1}, {2}) : (!llvm.ptr, !llvm.ptr, i64) -> ()",
                        arrDat[slot], devPtr[slot], devBytes[slot]);
                foreach (int slot in m_ArraySlots)
                    HEmit("    llvm.call @slgpuFree({0}) : (!llvm.ptr) -> ()", devPtr[slot]);

                // ---- 7. zeroed sl_value out + return ----
                string u0 = HNV();
                HEmit("    {0} = llvm.mlir.undef : !slv", u0);
                string u1 = HNV();
                HEmit("    {0} = llvm.insertvalue {1}, {2}[0] : !slv", u1, HK32(0), u0);
                string u2 = HNV();
                HEmit("    {0} = llvm.insertvalue {1}, {2}[1] : !slv", u2, HCI64(0), u1);
                HEmit("    llvm.store {0}, %ret : !slv, !llvm.ptr", u2);
                HEmit("    return {0} : i64", HCI64(0));
            }

            /// <summary>Null-safe host-side VMArray sentinel (length zeroed).</summary>
            private string HostSentinelI64()
            {
                if (!m_HostSentinelEmitted)
                {
                    HEmitC("    %hdummy = llvm.alloca {0} x i8 : (i32) -> !llvm.ptr", HK32(72));
                    HEmitC("    %hds_lp = llvm.getelementptr %hdummy[{0}] : (!llvm.ptr, i64) -> !llvm.ptr, i8", HCI64(48));
                    HEmitC("    llvm.store {0}, %hds_lp : i32, !llvm.ptr", HK32(0));
                    HEmitC("    %hds_i = llvm.ptrtoint %hdummy : !llvm.ptr to i64");
                    m_HostSentinelEmitted = true;
                    m_HostSentinelName = "%hds_i";
                }
                return m_HostSentinelName;
            }

            // host-wrapper SSA helpers (independent namespace from the kernel)

            private string HNV() => "%hv" + (m_HostTmp++).ToString(CultureInfo.InvariantCulture);

            private string HCI64(long v)
            {
                if (m_HostI64Consts.TryGetValue(v, out string name)) return name;
                name = "%hc_" + ConstSuffix(v);
                HEmitC("    {0} = arith.constant {1} : i64", name, v.ToString(CultureInfo.InvariantCulture));
                m_HostI64Consts[v] = name;
                return name;
            }

            private string HK32(int v)
            {
                if (m_HostK32Consts.TryGetValue(v, out string name)) return name;
                name = "%hk_" + ConstSuffix(v);
                HEmitC("    {0} = llvm.mlir.constant({1} : i32) : i32", name, v.ToString(CultureInfo.InvariantCulture));
                m_HostK32Consts[v] = name;
                return name;
            }

            private void HEmit(string format, params object?[] args)
            {
                m_HostSb.AppendFormat(CultureInfo.InvariantCulture, format, args).Append('\n');
            }

            private void HEmitC(string format, params object?[] args)
            {
                m_HostConstSb.AppendFormat(CultureInfo.InvariantCulture, format, args).Append('\n');
            }
        }
    }
}
