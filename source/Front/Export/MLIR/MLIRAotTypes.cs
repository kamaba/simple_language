//****************************************************************************
//  File:      MLIRAotTypes.cs
// ------------------------------------------------
//  Description: AOT data-type registry (design doc §4):
//  - Mirrors the C VM's data-object layout rules (memory_system_method.c
//    vm_mem_data_measure / vm_mem_data_slot_span / vm_mem_data_member_slot)
//    so the native buffer built here is layout-compatible with the CVM side.
//  - Produces the !sl_t_* llvm.struct aliases and the manifest typeList
//    (module.json "aot.typeList") consumed by the C marshal code.
//  Native layout of a data value:
//      [ 32-byte !sl_meta header ][ naturally aligned member region ]
//  In AOT IR a Struct value is the i64 bit pattern of a pointer to this
//  whole buffer (kind=2 on the C ABI); member offsets already include the
//  32-byte header.
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SimpleLanguage.IR;
using SimpleLanguage.Export.SLIR.Types;

namespace SimpleLanguage.Export.MLIR
{
    /// <summary>
    /// Member slot classification (numeric values align with the C VM's
    /// VMD_SLOT_* constants in vm_meta.h).
    /// </summary>
    internal enum AotMemberSlot
    {
        Scalar = 1,   // fixed-width numeric member (i8/i16/i32/i64/f32/f64/bool)
        String = 2,   // Core.String reference slot (8-byte pointer; access fails)
        Data = 3,     // nested data value, inlined WITHOUT its own meta header
        Enum = 4,     // reserved (enum members are rejected at registration)
        Ptr = 5,      // opaque VM pointer slot (class / interface / array / T)
    }

    /// <summary>Per-member layout record (one entry per manifest layout row).</summary>
    internal sealed class AotMemberLayout
    {
        /// <summary>Field index = position in IRMetaClass.localIRMetaVariableList
        /// (the index carried by LoadNotStaticField / StoreNotStaticField*).</summary>
        public int Index;
        /// <summary>Short member name (no owner prefix).</summary>
        public string Name = "";
        public AotMemberSlot Slot;
        /// <summary>Native offset from the start of the buffer (includes the
        /// 32-byte meta header).</summary>
        public int Offset;
        /// <summary>Native size in bytes (nested data = inner size, no header).</summary>
        public int Size;
        /// <summary>Native alignment in bytes.</summary>
        public int Align;
        /// <summary>VM member_data packed offset (reference; fastPath check).</summary>
        public int VmOffset;
        /// <summary>Slot==Data: classId of the nested data type; else 0.</summary>
        public int NestedTypeId;
        /// <summary>Scalar byte width (1/2/4/8); 0 for non-scalar slots.</summary>
        public int Width;
        /// <summary>Scalar: signed integer (sext on load).</summary>
        public bool Signed;
        /// <summary>Scalar: floating (f32/f64 domain).</summary>
        public bool IsFloat;
        /// <summary>Full IR type name (diagnostics).</summary>
        public string TypeName = "";
        /// <summary>Leaf type name (codegen decisions).</summary>
        public string LeafName = "";
    }

    /// <summary>One registered data type (manifest typeList entry source).</summary>
    internal sealed class AotTypeInfo
    {
        public const int MetaHeaderSize = 32;

        public int ClassId;
        public string FullName = "";
        /// <summary>0=Class 1=Enum 2=Data 3=Interface (always 2 here).</summary>
        public int MetaClassKind;
        public int BaseClassId;
        public int TemplateParameterCount;
        /// <summary>Aligned size of the member region (no header).</summary>
        public int InnerSize;
        /// <summary>Max member alignment inside the inner region.</summary>
        public int InnerMaxAlign;
        /// <summary>All members scalar and VM packed offsets == native offsets
        /// → C marshal may use one whole-block memcpy.</summary>
        public bool FastPath;
        public List<AotMemberLayout> Layout = new();
        /// <summary>Bare alias name (no '!' prefix), unique per module.</summary>
        public string AliasName = "";
        public IRMetaClass SourceClass = null!;

        /// <summary>Total native buffer size: header + inner, 8-aligned.</summary>
        public int NativeSize => MetaHeaderSize + ((InnerSize + 7) & ~7);
        /// <summary>MLIR alias reference form ("!sl_t_...").</summary>
        public string AliasRef => "!" + AliasName;

        public AotMemberLayout? FindMember(int fieldIndex)
        {
            foreach (var m in Layout)
                if (m.Index == fieldIndex) return m;
            return null;
        }
    }

    /// <summary>
    /// Module-level registry of all data types referenced by the emitted
    /// AOT methods (slot types, nested members, struct returns). Shared by
    /// every MethodEmitter of one ExportModuleToFile call; registration is
    /// idempotent by classId and lazily recursive for nested data members.
    /// </summary>
    internal sealed class AotTypeTable
    {
        private readonly Dictionary<int, AotTypeInfo> m_ByClassId = new();
        private readonly List<AotTypeInfo> m_Order = new();
        private readonly HashSet<int> m_Visiting = new();

        public int Count => m_Order.Count;
        public IReadOnlyList<AotTypeInfo> Types => m_Order;

        /// <summary>
        /// Register a data type (idempotent). Throws EmitFailException when
        /// the type is not a data type, has classId 0, contains a recursive
        /// member cycle, or contains a blacklisted member type (enum / Char /
        /// Num / Member / Float8* / Float16*). The failure propagates up to
        /// the per-method try/catch in ExportModuleToFile: the method is
        /// marked failed and falls back to the CVM interpreter.
        /// </summary>
        public AotTypeInfo Register(IRMetaClass irmc)
        {
            if (irmc == null)
                throw new MLIRExporter.EmitFailException("[aot-types] cannot register a null IRMetaClass");

            int classId = irmc.id;
            if (classId == 0)
                throw Fail(irmc, "classId is 0 (type not registered in ClassManager); cannot be used as an AOT struct type");

            if (m_ByClassId.TryGetValue(classId, out var existing))
                return existing;

            if (irmc.metaClassKind != IRMetaClassKind.Data)
                throw Fail(irmc, $"meta class kind {irmc.metaClassKind} does not map to !llvm.struct (only data types do)");

            if (m_Visiting.Contains(classId))
                throw Fail(irmc, "contains a recursive data-member cycle, which is not supported");

            m_Visiting.Add(classId);
            try
            {
                var info = BuildInfo(irmc);
                m_ByClassId[classId] = info;
                m_Order.Add(info);
                return info;
            }
            finally
            {
                m_Visiting.Remove(classId);
            }
        }

        public AotTypeInfo? Find(int classId)
            => m_ByClassId.TryGetValue(classId, out var info) ? info : null;

        /// <summary>
        /// Emit the alias block that precedes the module body:
        /// the shared !sl_meta header type plus one alias per registered
        /// data type (registration order = dependency order, so nested
        /// aliases are always defined before their uses).
        /// </summary>
        public void EmitAliases(StringBuilder sb)
        {
            if (m_Order.Count == 0) return;
            sb.AppendLine("// AOT data types: 32-byte !sl_meta header + naturally aligned member region");
            sb.AppendLine("// sl_meta: (classId, kindFlags, name_ptr, template_ptr, tmpl_cnt, base_class_id)");
            sb.AppendLine("!sl_meta = !llvm.struct<(i32, i32, i64, i64, i32, i32)>");
            foreach (var info in m_Order)
                sb.AppendLine(info.AliasRef + " = !llvm.struct<(!sl_meta" + FieldList(info) + ")>");
        }

        /// <summary>Manifest typeList (module.json "aot.typeList").</summary>
        public List<SLAotTypePackage> ToPackages()
        {
            var list = new List<SLAotTypePackage>(m_Order.Count);
            foreach (var info in m_Order)
            {
                var p = new SLAotTypePackage
                {
                    classId = info.ClassId,
                    fullName = info.FullName,
                    metaClassKind = info.MetaClassKind,
                    baseClassId = info.BaseClassId,
                    templateParameterCount = info.TemplateParameterCount,
                    nativeSize = info.NativeSize,
                    fastPath = info.FastPath,
                };
                foreach (var m in info.Layout)
                {
                    p.layout.Add(new SLAotLayoutEntryPackage
                    {
                        index = m.Index,
                        offset = m.Offset,
                        size = m.Size,
                        slot = (int)m.Slot,
                        name = m.Name,
                        vmOffset = m.VmOffset,
                        nestedTypeId = m.NestedTypeId,
                    });
                }
                list.Add(p);
            }
            return list;
        }

        // ------------------------------------------------------------------
        // Layout computation (mirrors vm_mem_data_measure on the C side)
        // ------------------------------------------------------------------

        private AotTypeInfo BuildInfo(IRMetaClass irmc)
        {
            var info = new AotTypeInfo
            {
                ClassId = irmc.id,
                FullName = irmc.irName ?? "",
                MetaClassKind = (int)irmc.metaClassKind,
                BaseClassId = irmc.OwnerMetaClass?.extendClass?.classId ?? 0,
                TemplateParameterCount = irmc.templateParameterCount,
                SourceClass = irmc,
                AliasName = "sl_t_" + MLIRExporter.SanitizeSymbol(irmc.irName ?? "")
                    + "_" + irmc.id.ToString("X", CultureInfo.InvariantCulture),
            };

            int cur = 0;        // inner-region cursor (natural alignment)
            int maxAlign = 1;   // inner max alignment
            int vmCur = 0;      // VM packed cursor (no alignment)
            bool allScalar = true;
            bool offsetsMatch = true;

            var members = irmc.localIRMetaVariableList;
            for (int i = 0; i < members.Count; i++)
            {
                var v = members[i];
                if (v == null)
                    throw Fail(irmc, $"member slot {i} is null");
                var e = new AotMemberLayout { Index = i, Name = MemberShortName(v) };
                ClassifyMember(irmc, v, e);

                // native: cur = round_up(cur, al); offset = 32 + cur; cur += sz
                cur = (cur + e.Align - 1) / e.Align * e.Align;
                e.Offset = AotTypeInfo.MetaHeaderSize + cur;
                cur += e.Size;
                if (e.Align > maxAlign) maxAlign = e.Align;

                // VM packed: no alignment, scalars take their width, refs take 8
                e.VmOffset = vmCur;
                vmCur += e.Slot == AotMemberSlot.Scalar ? e.Width : 8;

                if (e.Slot != AotMemberSlot.Scalar) allScalar = false;
                if (e.Offset - AotTypeInfo.MetaHeaderSize != e.VmOffset) offsetsMatch = false;

                info.Layout.Add(e);
            }

            // tail-align the inner region to its max member alignment
            info.InnerSize = (cur + maxAlign - 1) / maxAlign * maxAlign;
            info.InnerMaxAlign = maxAlign;
            info.FastPath = allScalar && offsetsMatch && info.InnerSize == vmCur;
            return info;
        }

        /// <summary>
        /// Member classification tree (mirrors vm_mem_data_member_slot /
        /// vm_mem_data_slot_span): scalars by width table; blacklisted member
        /// types reject the whole data type; String → pointer slot; nested
        /// data → inlined inner region; everything else → opaque pointer slot.
        /// </summary>
        private void ClassifyMember(IRMetaClass owner, IRMetaVariable v, AotMemberLayout e)
        {
            var cls = v.irMetaType?.irMetaClass;
            if (cls == null || string.IsNullOrEmpty(cls.irName))
                throw Fail(owner, $"member '{v.name}' has no resolvable IR type");

            string full = cls.irName;
            string leaf = LeafOf(full);
            e.TypeName = full;
            e.LeafName = leaf;
            int kind = (int)cls.metaClassKind;

            int width = ScalarWidthOf(leaf);
            if (width > 0)
            {
                e.Slot = AotMemberSlot.Scalar;
                e.Width = width;
                e.Size = width;
                e.Align = width;
                e.IsFloat = leaf == "Double" || leaf == "Float64" || leaf == "Float32";
                e.Signed = leaf == "Int8" || leaf == "Int16" || leaf == "Int32" || leaf == "Int64";
                return;
            }

            // Blacklisted member types: the whole data type fails AOT
            // registration (enum members / Char / Num / Member / float8/16).
            if (kind == (int)IRMetaClassKind.Enum
                || leaf == "Char" || leaf == "Member" || leaf == "Num"
                || leaf == "Float8_E4M3" || leaf == "Float8_E5M2"
                || leaf == "Float16" || leaf == "Float16_Brain")
            {
                throw Fail(owner,
                    $"member '{e.Name}' has unsupported type '{full}' " +
                    "(enum/Char/Num/Member/Float8/Float16 data members are not AOT-compatible)");
            }

            if (leaf == "String")
            {
                e.Slot = AotMemberSlot.String;
                e.Size = 8;
                e.Align = 8;
                return;
            }

            if (kind == (int)IRMetaClassKind.Data)
            {
                var nested = Register(cls);
                e.Slot = AotMemberSlot.Data;
                e.NestedTypeId = nested.ClassId;
                e.Size = nested.InnerSize;
                e.Align = nested.InnerMaxAlign;
                return;
            }

            // class / interface / Array<T> / generic T / Core.Object:
            // opaque VM pointer slot (present in layout, but member access
            // through AOT code fails at emission time).
            e.Slot = AotMemberSlot.Ptr;
            e.Size = 8;
            e.Align = 8;
        }

        private string FieldList(AotTypeInfo info)
        {
            var sb = new StringBuilder();
            foreach (var m in info.Layout)
            {
                sb.Append(", ");
                switch (m.Slot)
                {
                    case AotMemberSlot.Data:
                        sb.Append(m_ByClassId[m.NestedTypeId].AliasRef);
                        break;
                    case AotMemberSlot.String:
                    case AotMemberSlot.Ptr:
                        sb.Append("!llvm.ptr");
                        break;
                    default:
                        sb.Append(ScalarLlvmType(m.Width, m.IsFloat));
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>LLVM scalar type of a fixed-width member (load/store type).</summary>
        internal static string ScalarLlvmType(int width, bool isFloat)
        {
            if (isFloat) return width == 4 ? "f32" : "f64";
            switch (width)
            {
                case 8: return "i64";
                case 4: return "i32";
                case 2: return "i16";
                default: return "i8";
            }
        }

        internal static int ScalarWidthOf(string leaf)
        {
            switch (leaf)
            {
                case "Int64":
                case "UInt64":
                case "Double":
                case "Float64":
                    return 8;
                case "Int32":
                case "UInt32":
                case "Float32":
                    return 4;
                case "Int16":
                case "UInt16":
                    return 2;
                case "Int8":
                case "UInt8":
                case "Boolean":
                    return 1;
                default:
                    return 0;
            }
        }

        internal static string LeafOf(string fullName)
        {
            int dot = fullName.LastIndexOf('.');
            return dot >= 0 ? fullName.Substring(dot + 1) : fullName;
        }

        /// <summary>Short member name: shortName → debugInfo.name → after last '.'.</summary>
        internal static string MemberShortName(IRMetaVariable v)
        {
            if (!string.IsNullOrEmpty(v.shortName)) return v.shortName;
            string dn = v.debugInfo.name ?? "";
            string n = v.name ?? "";
            int dot = n.LastIndexOf('.');
            return dot >= 0 ? n.Substring(dot + 1) : n;
        }

        private static MLIRExporter.EmitFailException Fail(IRMetaClass irmc, string message)
            => new MLIRExporter.EmitFailException("[aot type '" + (irmc?.irName ?? "?") + "'] " + message);
    }
}
