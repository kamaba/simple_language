//****************************************************************************
//  File:      IRData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR.Types;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SimpleLanguage.IR
{
    public class IRData
    {
        public int       id = 0;
        public EIROpCode opCode;                 //指令类型
        // 指令原始对象值（兼容旧代码）
        // 注释掉直接使用 opValue，优先使用 Payload 以便导出/打包。保留字段以便调试。
        //public object    opValue;                //指令值
        private object _opValue;
        public object opValue { get => _opValue; set => SetOpValue(value); }
        // 序列化后的原始数据（仅用于值类型或需要内嵌的常量）
        public byte[]    Payload = null;
        // Track whether EmbedIndexInPayload has been applied (prevents double-embedding).
        private bool _indexEmbedded = false;
        // 当前 IRData 的字节长度（包括 Payload 的长度）--用于导出序列化时参考
        // ByteLength currently holds the payload length. The final serialized
        // instruction length is computed as (1 + ByteLength) by IRMethod (1 byte for opcode)
        public int       ByteLength = 0;
        public int index { get; set; }                  //索引
        public DebugInfo debugInfo;              //调试信息

        /// <summary>
        /// IR 文本导出用：静态字段所属类的 <see cref="IRMetaClass.irName"/>（如 <c>Core.Project</c>）。
        /// 运行时时 <see cref="opValue"/> 仍为字段类型的 <see cref="IRMetaType"/>，不是声明类。
        /// </summary>
        public string debugStaticOwnerIrName { get; set; }

        public IRData()
        {

        }

        // 设置 opValue 并尝试将值类型序列化到 Payload，同时更新 ByteLength
        public void SetOpValue(object v)
        {
            // keep a copy for debug but use Payload for runtime consumption
            // Cast v to the correct CLR type based on opCode so PackOpValue
            // produces the right number of bytes (e.g. Int8 -> 1 byte, not 4).
            _opValue = CastOpValueByOpCode(v);
            PackOpValue();
            UpdateByteLength();
        }

        /// <summary>
        /// Cast a boxed numeric value to the CLR type that matches the current
        /// opCode's LoadConst variant.  C# integer literals default to int,
        /// so without this cast a LoadConstInt8 with value 1 would pack as
        /// 4 bytes (Int32) instead of 1 byte (SByte).
        /// </summary>
        private object CastOpValueByOpCode(object v)
        {
            if (v == null) return null;
            switch (opCode)
            {
                case EIROpCode.LoadConstUInt8:
                    try { return Convert.ToByte(v); } catch { return v; }
                case EIROpCode.LoadConstInt8:
                    try { return Convert.ToSByte(v); } catch { return v; }
                case EIROpCode.LoadConstInt16:
                    try { return Convert.ToInt16(v); } catch { return v; }
                case EIROpCode.LoadConstUInt16:
                    try { return Convert.ToUInt16(v); } catch { return v; }
                case EIROpCode.LoadConstInt32:
                    try { return Convert.ToInt32(v); } catch { return v; }
                case EIROpCode.LoadConstUInt32:
                    try { return Convert.ToUInt32(v); } catch { return v; }
                case EIROpCode.LoadConstInt64:
                    try { return Convert.ToInt64(v); } catch { return v; }
                case EIROpCode.LoadConstUInt64:
                    try { return Convert.ToUInt64(v); } catch { return v; }
                case EIROpCode.LoadConstFloat8_E4M3:
                case EIROpCode.LoadConstFloat8_E5M2:
                    // float8 常量保存位模式（byte）
                    try { return Convert.ToByte(v); } catch { return v; }
                case EIROpCode.LoadConstFloat16:
                case EIROpCode.LoadConstFloat16_Brain:
                    // float16/bfloat16 常量保存位模式（ushort）
                    try { return Convert.ToUInt16(v); } catch { return v; }
                case EIROpCode.LoadConstFloat32:
                    try { return Convert.ToSingle(v); } catch { return v; }
                case EIROpCode.LoadConstFloat64:
                    try { return Convert.ToDouble(v); } catch { return v; }
                case EIROpCode.LoadConstBoolean:
                    try { return Convert.ToBoolean(v); } catch { return v; }
                default:
                    return v;
            }
        }

        // 将基础值类型序列化到 Payload（仅支持常见原始类型和字符串）
        private void PackOpValue()
        {
            Payload = null;
            if (opValue == null)
            {
                return;
            }

            if (opValue is SLSystemMethodCallPackage smcp)
            {
                Payload = EncodeBinarySystemCall(smcp);
                return;
            }

            // atsign label fast-call payload: binary SLAtSignLabelCallPackage
            // (entryIndex/paramCount/tryCatch/methodName/isVoid); the CVM
            // assembly build locates the module atSignLabel[] entry by
            // entryIndex and rewrites the payload into a 4-byte binding
            // index once resolved. Missing this branch makes FinalizePack
            // fall back to ToString(), exporting the bare type name and
            // forcing the VM to report -86.
            if (opValue is SLAtSignLabelCallPackage labelPkg)
            {
                Payload = EncodeBinaryAtSignLabel(labelPkg);
                return;
            }

            // runtime def type payload: write full IRMetaType tree at SetOpValue stage
            if (opValue is IRMetaType imt)
            {
                var exportType = CreateRuntimeDefTypeExport(imt);
                Payload = EncodeBinaryDefTypePayload(exportType);
                return;
            }

            // call payload fallback: write method id at SetOpValue stage
            if (opValue is IRMethodCall imc)
            {
                var export = CreateRuntimeCallExport(imc);
                Payload = EncodeBinaryCallPayload(export);
                return;
            }

            switch (Type.GetTypeCode(opValue.GetType()))
            {
                case TypeCode.Boolean:
                    /* C# BitConverter.GetBytes(bool) produces 4 bytes, but the VM
                       expects 1 byte for LoadConstBoolean. Pack as a single byte. */
                    Payload = new byte[] { (byte)(((bool)opValue) ? 1 : 0) };
                    break;
                case TypeCode.Byte:
                    Payload = new byte[] { (byte)opValue };
                    break;
                case TypeCode.SByte:
                    Payload = new byte[] { (byte)((sbyte)opValue) };
                    break;
                case TypeCode.Int16:
                    Payload = BitConverter.GetBytes((short)opValue);
                    break;
                case TypeCode.UInt16:
                    Payload = BitConverter.GetBytes((ushort)opValue);
                    break;
                case TypeCode.Int32:
                    Payload = BitConverter.GetBytes((int)opValue);
                    break;
                case TypeCode.UInt32:
                    Payload = BitConverter.GetBytes((uint)opValue);
                    break;
                case TypeCode.Int64:
                    Payload = BitConverter.GetBytes((long)opValue);
                    break;
                case TypeCode.UInt64:
                    Payload = BitConverter.GetBytes((ulong)opValue);
                    break;
                case TypeCode.Single:
                    Payload = BitConverter.GetBytes((float)opValue);
                    break;
                case TypeCode.Double:
                    Payload = BitConverter.GetBytes((double)opValue);
                    break;
                case TypeCode.String:
                    {
                        var b = Encoding.UTF8.GetBytes((string)opValue);
                        // store raw bytes (no length prefix here; exporter can add metadata)
                        Payload = b;
                    }
                    break;
                default:
                    // 不对复杂类型序列化，保持 Payload=null
                    Payload = null;
                    break;
            }
        }

        private static RuntimeCallExport CreateRuntimeCallExport(IRMethodCall call)
        {
            if (call == null) return null;
            return new RuntimeCallExport
            {
                methodId = call.irMethod != null ? ClassManager.GetMethodId(call.irMethod.id) : 0,
                methodName = call.methodName ?? string.Empty,
                methodNameHash = ClassManager.GetMethodId(call.methodName),
                paramCount = call.paramCount,
                tryCatch = call.tryCatch,
                runtimeDefType = CreateRuntimeDefTypeExport(call.metaType),
                templateRuntimeDefTypeList = CreateRuntimeDefTypeExportList(call.irTemplateMetaType),
            };
        }

        private static List<RuntimeDefTypeExport> CreateRuntimeDefTypeExportList(List<IRMetaType> list)
        {
            var ret = new List<RuntimeDefTypeExport>();
            if (list == null) return ret;
            for (int i = 0; i < list.Count; i++)
            {
                var item = CreateRuntimeDefTypeExport(list[i]);
                if (item != null) ret.Add(item);
            }
            return ret;
        }

        private static RuntimeDefTypeExport CreateRuntimeDefTypeExport(IRMetaType mt)
        {
            if (mt == null) return null;

            return new RuntimeDefTypeExport
            {
                classId = mt.irMetaClass?.id ?? 0,
                className = mt.irMetaClass?.irName ?? string.Empty,
                ownerClassId = mt.irOwnerMetaClass?.id ?? 0,
                ownerClassName = mt.irOwnerMetaClass?.irName ?? string.Empty,
                templateIndex = mt.templateIndex,
                runtimeDefTypeList = CreateRuntimeDefTypeExportList(mt.irMetaTypeList),
            };
        }

        private sealed class RuntimeCallExport
        {
            public RuntimeDefTypeExport runtimeDefType { get; set; }
            public List<RuntimeDefTypeExport> templateRuntimeDefTypeList { get; set; } = new();
            public int methodId { get; set; }
            public string methodName { get; set; } = string.Empty;
            /// <summary>裸方法名 FNV-1a 哈希（ClassManager.GetMethodId(methodName)）。
            /// C VM CallDynamic 接口分发以此 int 优先匹配 receiver 类的
            /// virtual_function_name_hash（ASCII 名两侧一致；非 ASCII 名或
            /// 哈希碰撞落 methodName strcmp 兜底）。methodName 保留用于 Debug 显示。</summary>
            public int methodNameHash { get; set; }
            public int paramCount { get; set; }
            public bool tryCatch { get; set; }
        }

        /* 可见性 internal（原 private，2026-10-17）：FrontJsonContext 的 source-gen
           需跨类型登记本类型（AOT 兼容 JSON 反序列化，IRData 遗留 JSON payload 回退路径）。 */
        internal sealed class RuntimeDefTypeExport
        {
            public int classId { get; set; }
            public string className { get; set; } = string.Empty;
            public int ownerClassId { get; set; }
            public string ownerClassName { get; set; } = string.Empty;
            public int templateIndex { get; set; } = -1;
            //public bool isTemplate { get; set; }
            public List<RuntimeDefTypeExport> runtimeDefTypeList { get; set; } = new();
        }

        // ====================================================================
        // Binary payload v1 (write side).
        //
        // Wire contract, mirrored by the C VM decoder
        // (csimple_lang/src/vm/runtime/payload/runtime_payload_binary.c);
        // full spec: csimple_lang/md/design/PAYLOAD_BINARY_FORMAT.md.
        //
        //   Format discrimination: payload[0] == 0x01 => binary v1.
        //   Legacy JSON payloads always start with '{' (0x7B), so old modules
        //   stay loadable by the same VM (dual-format decode).
        //
        //   Primitives (all little-endian):
        //     i32  : 4 bytes        u16 : 2 bytes
        //     u8   : 1 byte         bool: 1 byte (0/1)
        //     str  : [len:u16][utf8 bytes]  (len = byte length)
        //
        //   DefType node (recursive, no magic byte):
        //     [classId:i32][ownerClassId:i32][templateIndex:i32]
        //     [className:str][ownerClassName:str][childCount:u16][child nodes...]
        //
        //   Type payload (LoadConstType/CastClass/NewTemplateObject/NewArray;
        //   also the owner suffix of LoadStaticField/StoreStaticField after
        //   the leading 4-byte field index):
        //     [0x01][node]
        //
        //   Call payload (CallStatic/CallDynamic/CallVirt/CallClosure/NewClosure;
        //   CallVirt embeds it after its leading 4-byte iri.index):
        //     [0x01][methodId:i32][methodNameHash:i32][paramCount:i32][tryCatch:u8]
        //     [methodName:str]
        //     [hasTargetType:u8]([node] when 1)
        //     [templateCount:u16][node * templateCount]
        //
        //   SystemMethod call payload (CallSystemMethod):
        //     [0x01][id:i32][paramCount:i32][systemMethodKind:i32][name:str]
        //
        //   FFI static call payload (CallFFIStatic, legacy pre-P10 format):
        //     Front 已停产（P10 起调用点由 cvm 装配期 attribute 注册表从
        //     CallStatic 改写而来，payload 恒为 4 字节绑定索引）；cvm 解码器
        //     (runtime_payload_binary.c vm_payload_parse_ffi) 保留双格式解析
        //     以兼容存量 module.json：
        //     [0x01][methodId:i32][paramCount:i32][tryCatch:u8]
        //     [lib:str][symbol:str][sig:str][methodName:str]
        //
        //   AtSign label call payload (CallAtSignLabel, pre-rewrite):
        //     [0x01][entryIndex:i32][paramCount:i32][tryCatch:u8][isVoid:u8]
        //     [methodName:str]
        // ====================================================================
        private const byte BinFormatV1 = 0x01;

        private static void BinWriteI32(List<byte> buf, int v)
        {
            buf.Add((byte)v);
            buf.Add((byte)(v >> 8));
            buf.Add((byte)(v >> 16));
            buf.Add((byte)(v >> 24));
        }

        private static void BinWriteU16(List<byte> buf, int v)
        {
            buf.Add((byte)(v & 0xFF));
            buf.Add((byte)((v >> 8) & 0xFF));
        }

        private static void BinWriteStr(List<byte> buf, string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                buf.Add(0);
                buf.Add(0);
                return;
            }
            var b = Encoding.UTF8.GetBytes(s);
            if (b.Length > 0xFFFF)
            {
                throw new InvalidOperationException(
                    "Binary payload v1 string exceeds u16 length limit: " + b.Length);
            }
            buf.Add((byte)(b.Length & 0xFF));
            buf.Add((byte)((b.Length >> 8) & 0xFF));
            buf.AddRange(b);
        }

        private static void BinWriteDefTypeNode(List<byte> buf, RuntimeDefTypeExport node)
        {
            BinWriteI32(buf, node.classId);
            BinWriteI32(buf, node.ownerClassId);
            BinWriteI32(buf, node.templateIndex);
            BinWriteStr(buf, node.className);
            BinWriteStr(buf, node.ownerClassName);
            var list = node.runtimeDefTypeList;
            int n = (list != null) ? list.Count : 0;
            BinWriteU16(buf, n);
            for (int i = 0; i < n; i++)
            {
                BinWriteDefTypeNode(buf, list[i]);
            }
        }

        private static byte[] EncodeBinaryDefTypePayload(RuntimeDefTypeExport node)
        {
            if (node == null)
            {
                return new byte[] { BinFormatV1 };
            }
            var buf = new List<byte>(64);
            buf.Add(BinFormatV1);
            BinWriteDefTypeNode(buf, node);
            return buf.ToArray();
        }

        private static byte[] EncodeBinaryCallPayload(RuntimeCallExport call)
        {
            if (call == null)
            {
                return new byte[] { BinFormatV1 };
            }
            var buf = new List<byte>(96);
            buf.Add(BinFormatV1);
            BinWriteI32(buf, call.methodId);
            BinWriteI32(buf, call.methodNameHash);
            BinWriteI32(buf, call.paramCount);
            buf.Add(call.tryCatch ? (byte)1 : (byte)0);
            BinWriteStr(buf, call.methodName);
            if (call.runtimeDefType != null)
            {
                buf.Add(1);
                BinWriteDefTypeNode(buf, call.runtimeDefType);
            }
            else
            {
                buf.Add(0);
            }
            var tl = call.templateRuntimeDefTypeList;
            int n = (tl != null) ? tl.Count : 0;
            BinWriteU16(buf, n);
            for (int i = 0; i < n; i++)
            {
                BinWriteDefTypeNode(buf, tl[i]);
            }
            return buf.ToArray();
        }

        private static byte[] EncodeBinarySystemCall(SLSystemMethodCallPackage pkg)
        {
            var buf = new List<byte>(48);
            buf.Add(BinFormatV1);
            BinWriteI32(buf, pkg.id);
            BinWriteI32(buf, pkg.paramCount);
            BinWriteI32(buf, pkg.systemMethodKind);
            BinWriteStr(buf, pkg.name);
            return buf.ToArray();
        }

        private static byte[] EncodeBinaryAtSignLabel(SLAtSignLabelCallPackage pkg)
        {
            var buf = new List<byte>(64);
            buf.Add(BinFormatV1);
            BinWriteI32(buf, pkg.entryIndex);
            BinWriteI32(buf, pkg.paramCount);
            buf.Add(pkg.tryCatch ? (byte)1 : (byte)0);
            buf.Add(pkg.isVoid ? (byte)1 : (byte)0);
            BinWriteStr(buf, pkg.methodName);
            return buf.ToArray();
        }

        // ---- Binary payload v1 decode (IR.txt / UnpackOpValueFromPayload) ----

        private static bool BinPeekIsV1(byte[] payload)
        {
            return payload != null && payload.Length >= 1 && payload[0] == BinFormatV1;
        }

        private sealed class BinReader
        {
            private readonly byte[] _buf;
            private int _pos;
            public BinReader(byte[] buf) { _buf = buf; _pos = 0; }
            public bool AtEnd { get { return _pos >= _buf.Length; } }
            private bool Ensure(int n) { return _pos + n <= _buf.Length; }
            public bool TryReadU8(out byte v)
            {
                v = 0;
                if (!Ensure(1)) return false;
                v = _buf[_pos]; _pos++;
                return true;
            }
            public bool TryReadU16(out int v)
            {
                v = 0;
                if (!Ensure(2)) return false;
                v = _buf[_pos] | (_buf[_pos + 1] << 8);
                _pos += 2;
                return true;
            }
            public bool TryReadI32(out int v)
            {
                v = 0;
                if (!Ensure(4)) return false;
                v = _buf[_pos] | (_buf[_pos + 1] << 8) | (_buf[_pos + 2] << 16) | (_buf[_pos + 3] << 24);
                _pos += 4;
                return true;
            }
            public bool TryReadStr(out string s)
            {
                s = null;
                if (!TryReadU16(out int len) || !Ensure(len)) return false;
                s = Encoding.UTF8.GetString(_buf, _pos, len);
                _pos += len;
                return true;
            }
        }

        private static bool TryBinDecodeDefTypeNode(BinReader r, out RuntimeDefTypeExport node)
        {
            node = null;
            if (!r.TryReadI32(out int classId)) return false;
            if (!r.TryReadI32(out int ownerClassId)) return false;
            if (!r.TryReadI32(out int templateIndex)) return false;
            if (!r.TryReadStr(out string className)) return false;
            if (!r.TryReadStr(out string ownerClassName)) return false;
            if (!r.TryReadU16(out int childCount)) return false;

            node = new RuntimeDefTypeExport
            {
                classId = classId,
                ownerClassId = ownerClassId,
                templateIndex = templateIndex,
                className = className,
                ownerClassName = ownerClassName,
            };
            for (int i = 0; i < childCount; i++)
            {
                if (!TryBinDecodeDefTypeNode(r, out var child)) return false;
                node.runtimeDefTypeList.Add(child);
            }
            return true;
        }

        private static bool TryBinDecodeDefTypePayload(byte[] payload, out RuntimeDefTypeExport node)
        {
            node = null;
            if (!BinPeekIsV1(payload)) return false;
            var r = new BinReader(payload);
            if (!r.TryReadU8(out _)) return false; /* magic */
            if (r.AtEnd) return true; /* bare [0x01]: null node */
            return TryBinDecodeDefTypeNode(r, out node) && r.AtEnd;
        }

        private static bool TryBinDecodeSystemCall(byte[] payload, out SLSystemMethodCallPackage pkg)
        {
            pkg = null;
            if (!BinPeekIsV1(payload)) return false;
            var r = new BinReader(payload);
            if (!r.TryReadU8(out _)) return false; /* magic */
            if (!r.TryReadI32(out int id)) return false;
            if (!r.TryReadI32(out int paramCount)) return false;
            if (!r.TryReadI32(out int kind)) return false;
            if (!r.TryReadStr(out string name)) return false;
            if (!r.AtEnd) return false;
            pkg = new SLSystemMethodCallPackage
            {
                id = id,
                paramCount = paramCount,
                systemMethodKind = kind,
                name = name,
            };
            return true;
        }

        // 更新 ByteLength（仅计算 Payload 长度，未来可扩展为包含头部字段）
        public void UpdateByteLength()
        {
            ByteLength = (Payload != null) ? Payload.Length : 0;
        }

        // Returns true if this opcode uses the `index` field (slot, target, class id, etc.)
        // The index is embedded as the first 4 bytes of Payload during finalization.
        public static bool UsesIndex(EIROpCode opCode)
        {
            switch (opCode)
            {
                case EIROpCode.LoadConstString:
                case EIROpCode.LoadArgument:
                case EIROpCode.LoadLocal:
                case EIROpCode.StoreLocal:
                case EIROpCode.StoreArgument:
                case EIROpCode.StoreReturn:
                case EIROpCode.LoadGlobal:
                case EIROpCode.StoreGlobal:
                case EIROpCode.LoadArrayIndex:
                case EIROpCode.LoadNotStaticField:
                case EIROpCode.StoreNotStaticField1:
                case EIROpCode.StoreNotStaticField2:
                case EIROpCode.LoadStaticField:
                case EIROpCode.StoreStaticField:
                case EIROpCode.CallVirt:
                case EIROpCode.Jmp:
                case EIROpCode.Br:
                case EIROpCode.Break:
                case EIROpCode.BrFalse:
                case EIROpCode.BrTrue:
                case EIROpCode.Beq:
                case EIROpCode.Bne:
                case EIROpCode.Bgt:
                case EIROpCode.Bge:
                case EIROpCode.Ble:
                case EIROpCode.BrLabel:
                case EIROpCode.StoreArrayIndex:
                case EIROpCode.LeaveTry:
                case EIROpCode.AllocClosureContext:
                // O3 const-fused stores: index embedded first, then [etype][value]
                case EIROpCode.StoreLocalConstValue:
                case EIROpCode.StoreArgumentConstValue:
                case EIROpCode.StoreReturnConstValue:
                case EIROpCode.StoreGlobalConstValue:
                case EIROpCode.StoreNotStaticField1ConstValue:
                case EIROpCode.StoreNotStaticField2ConstValue:
                case EIROpCode.StoreArrayIndexConstValue:
                case EIROpCode.StoreStaticFieldConstValue:
                // Null-check fast branches: target index embedded first (same as BrFalse)
                case EIROpCode.BrIsNull:
                case EIROpCode.BrNotNull:
                case EIROpCode.BrIsNullPeek:
                    return true;
                default:
                    return false;
            }
        }

        // Embed the `index` field as the first 4 bytes of Payload.
        // Idempotent: tracked by _indexEmbedded flag.
        // Called after FinalizePack() to merge index into payload for serialization.
        public void EmbedIndexInPayload()
        {
            if (!UsesIndex(opCode)) return;
            if (_indexEmbedded) return;

            byte[] indexBytes = BitConverter.GetBytes(this.index);
            if (Payload == null || Payload.Length == 0)
            {
                Payload = indexBytes;
            }
            else
            {
                byte[] newPayload = new byte[indexBytes.Length + Payload.Length];
                indexBytes.CopyTo(newPayload, 0);
                Payload.CopyTo(newPayload, indexBytes.Length);
                Payload = newPayload;
            }
            _indexEmbedded = true;
            UpdateByteLength();
        }

        // Finalize packaging for complex opValue types (labels, methods, meta types)
        // Convert remaining opValue into Payload bytes so IRData is self-contained for serialization.
        public void FinalizePack()
        {
            if (Payload != null) return;
            if (_opValue == null) return;

            // If opValue is an IRData (label/branch reference), just clear it.
            // The index is embedded into Payload by EmbedIndexInPayload() later.
            if (_opValue is IRData idRef)
            {
                _opValue = null;
                return;
            }

            // TryScopeData (BeginTry): serialize catch + finally target indices
            if (_opValue is TryScopeData tsd)
            {
                int catchIdx = tsd.catchTarget != null ? tsd.catchTarget.id : -1;
                int finallyIdx = tsd.finallyTarget != null ? tsd.finallyTarget.id : -1;
                Payload = new byte[8];
                System.BitConverter.GetBytes(catchIdx).CopyTo(Payload, 0);
                System.BitConverter.GetBytes(finallyIdx).CopyTo(Payload, 4);
                UpdateByteLength();
                _opValue = null;
                return;
            }

            // IRMethod -> serialize method id string
            if (_opValue is IRMethod irm)
            {
                var b = Encoding.UTF8.GetBytes(irm.id ?? string.Empty);
                Payload = b;
                UpdateByteLength();
                _opValue = null;
                return;
            }

            // IRMethodCall -> serialize callee method id string for cross-layer fallback
            if (_opValue is IRMethodCall imc)
            {
                var methodId = imc.irMethod?.id ?? string.Empty;
                Payload = Encoding.UTF8.GetBytes(methodId);
                UpdateByteLength();
                // keep methodId in opValue so exporters can still bind runtimeCall metadata
                _opValue = methodId;
                return;
            }

            // Fallback: try string form of object
            try
            {
                var s = _opValue.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    Payload = Encoding.UTF8.GetBytes(s);
                    UpdateByteLength();
                    _opValue = null;
                    return;
                }
            }
            catch { }

            // leave as-is if cannot pack
        }

        // Try to unpack payload back into opValue for debugging/inspection
        public void UnpackOpValueFromPayload()
        {
            if (Payload == null || Payload.Length == 0)
            {
                _opValue = null;
                return;
            }
            switch (opCode)
            {
                case EIROpCode.LoadConstBoolean:
                    if (TryGetBoolean(out var bv)) { _opValue = bv; return; }
                    break;
                case EIROpCode.LoadConstUInt8:
                    if (TryGetByte(out var bb)) { _opValue = bb; return; }
                    break;
                case EIROpCode.LoadConstInt8:
                    if (TryGetSByte(out var sb)) { _opValue = sb; return; }
                    break;
                case EIROpCode.LoadConstInt16:
                    if (TryGetInt16(out var s16)) { _opValue = s16; return; }
                    break;
                case EIROpCode.LoadConstUInt16:
                    if (TryGetUInt16(out var u16)) { _opValue = u16; return; }
                    break;
                case EIROpCode.LoadConstInt32:
                    if (TryGetInt32(out var i32)) { _opValue = i32; return; }
                    break;
                case EIROpCode.LoadConstUInt32:
                    if (TryGetUInt32(out var ui32)) { _opValue = ui32; return; }
                    break;
                case EIROpCode.LoadConstInt64:
                    if (TryGetInt64(out var i64)) { _opValue = i64; return; }
                    break;
                case EIROpCode.LoadConstUInt64:
                    if (TryGetUInt64(out var ui64)) { _opValue = ui64; return; }
                    break;
                case EIROpCode.LoadConstFloat8_E4M3:
                case EIROpCode.LoadConstFloat8_E5M2:
                    if (TryGetByte(out var f8bits)) { _opValue = f8bits; return; }
                    break;
                case EIROpCode.LoadConstFloat16:
                case EIROpCode.LoadConstFloat16_Brain:
                    if (TryGetUInt16(out var f16bits)) { _opValue = f16bits; return; }
                    break;
                case EIROpCode.LoadConstFloat32:
                    if (TryGetSingle(out var f)) { _opValue = f; return; }
                    break;
                case EIROpCode.LoadConstFloat64:
                    if (TryGetDouble(out var d)) { _opValue = d; return; }
                    break;
                case EIROpCode.LoadConstString:
                    if (TryGetString(out var s)) { _opValue = s; return; }
                    break;
                case EIROpCode.CallSystemMethod:
                    // Symmetric with PackOpValue(SLSystemMethodCallPackage): JSON object in Payload.
                    if (TryUnpackSystemMethodCallPackage(out var sysPkg) && sysPkg != null)
                    {
                        _opValue = sysPkg;
                        return;
                    }
                    break;
                default:
                    // fallback: try string then numeric interpretations
                    if (TryGetString(out var ss)) { _opValue = ss; return; }
                    if (TryGetInt32(out var ii)) { _opValue = ii; return; }
                    break;
            }
            // if still not set, keep raw bytes
            _opValue = Payload;
        }

        private bool TryUnpackSystemMethodCallPackage(out SLSystemMethodCallPackage pkg)
        {
            pkg = null;
            try
            {
                if (Payload == null || Payload.Length == 0) return false;
                /* Dual-format: binary v1 (payload[0] == 0x01) or legacy JSON ('{'). */
                if (TryBinDecodeSystemCall(Payload, out var binPkg))
                {
                    pkg = binPkg;
                    return pkg != null;
                }
                var text = Encoding.UTF8.GetString(Payload);
                if (string.IsNullOrWhiteSpace(text) || text[0] != '{') return false;
                pkg = JsonSerializer.Deserialize<SLSystemMethodCallPackage>(text, FrontJsonOptions.Plain);
                return pkg != null;
            }
            catch
            {
                return false;
            }
        }

        // Helpers to read payload as common types (fall back to opValue if present)
        public bool TryGetBoolean(out bool v)
        {
            if (Payload != null && Payload.Length >= 1)
            {
                v = BitConverter.ToBoolean(Payload, 0);
                return true;
            }
            if (opValue is bool b)
            {
                v = b; return true;
            }
            v = default; return false;
        }
        public bool TryGetInt32(out int v)
        {
            if (Payload != null && Payload.Length >= 4)
            {
                v = BitConverter.ToInt32(Payload, 0); return true;
            }
            if (opValue is int i) { v = i; return true; }
            v = default; return false;
        }
        public bool TryGetUInt32(out uint v)
        {
            if (Payload != null && Payload.Length >= 4)
            {
                v = BitConverter.ToUInt32(Payload, 0); return true;
            }
            if (opValue is uint ui) { v = ui; return true; }
            v = default; return false;
        }
        public bool TryGetInt64(out long v)
        {
            if (Payload != null && Payload.Length >= 8)
            {
                v = BitConverter.ToInt64(Payload, 0); return true;
            }
            if (opValue is long l) { v = l; return true; }
            v = default; return false;
        }
        public bool TryGetUInt64(out ulong v)
        {
            if (Payload != null && Payload.Length >= 8)
            {
                v = BitConverter.ToUInt64(Payload, 0); return true;
            }
            if (opValue is ulong ul) { v = ul; return true; }
            v = default; return false;
        }
        public bool TryGetInt16(out short v)
        {
            if (Payload != null && Payload.Length >= 2)
            {
                v = BitConverter.ToInt16(Payload, 0); return true;
            }
            if (opValue is short s) { v = s; return true; }
            v = default; return false;
        }
        public bool TryGetUInt16(out ushort v)
        {
            if (Payload != null && Payload.Length >= 2)
            {
                v = BitConverter.ToUInt16(Payload, 0); return true;
            }
            if (opValue is ushort us) { v = us; return true; }
            v = default; return false;
        }
        public bool TryGetByte(out byte v)
        {
            if (Payload != null && Payload.Length >= 1)
            {
                v = Payload[0]; return true;
            }
            if (opValue is byte by) { v = by; return true; }
            v = default; return false;
        }
        public bool TryGetSByte(out sbyte v)
        {
            if (Payload != null && Payload.Length >= 1)
            {
                v = (sbyte)Payload[0]; return true;
            }
            if (opValue is sbyte sb) { v = sb; return true; }
            v = default; return false;
        }
        public bool TryGetSingle(out float v)
        {
            if (Payload != null && Payload.Length >= 4)
            {
                v = BitConverter.ToSingle(Payload, 0); return true;
            }
            if (opValue is float f) { v = f; return true; }
            v = default; return false;
        }
        public bool TryGetDouble(out double v)
        {
            if (Payload != null && Payload.Length >= 8)
            {
                v = BitConverter.ToDouble(Payload, 0); return true;
            }
            if (opValue is double d) { v = d; return true; }
            v = default; return false;
        }
        public bool TryGetString(out string s)
        {
            if (Payload != null && Payload.Length > 0)
            {
                s = Encoding.UTF8.GetString(Payload); return true;
            }
            if (opValue is string ss) { s = ss; return true; }
            s = null; return false;
        }
        public void SetDebugInfoByValue( DebugInfo info )
        {
            debugInfo = info;
        }
        /// <param name="token">Source location (e.g. keyword, call name, operator).</param>
        /// <param name="info">Extra context for export/VM debug (opcode role, synthetic lowering label).</param>
        public void SetDebugInfoByToken(Token token, string info = null)
        {
            if (token != null)
            {
                debugInfo.path = token.path;
                debugInfo.name = token.lexeme?.ToString();
                debugInfo.beginLine = token.sourceBeginLine;
                debugInfo.beginChar = token.sourceBeginChar;
                debugInfo.endLine = token.sourceEndLine;
                debugInfo.endChar = token.sourceEndChar;
            }
            if (!string.IsNullOrEmpty(info))
                debugInfo.info = info;
        }
        public override string ToString()
        {
            StringBuilder m_StringBuilder = new StringBuilder();
            m_StringBuilder.Append( id + "   [ " + debugInfo.path + ":" + debugInfo.beginLine.ToString() + "]" + " [" + opCode.ToString() + "] index:[" + index.ToString() + "]");
            if (!string.IsNullOrEmpty(debugInfo.info))
            {
                m_StringBuilder.Append(" info:[");
                m_StringBuilder.Append(debugInfo.info);
                m_StringBuilder.Append("]");
            }
            if (opValue != null)
            {
                MetaType mt = opValue as MetaType;
                IRMetaType irmt = opValue as IRMetaType;
                IRMethod irm = opValue as IRMethod;
                if ( opValue.GetType() == typeof( Int32 ) )
                {
                    m_StringBuilder.Append(" val: int32[" + opValue + "] ");
                }
                else
                {
                    if (mt != null)
                    {
                        m_StringBuilder.Append(" val mt:[" + mt.name + "] ");
                    }
                    else if (irmt != null)
                    {
                        // Static field ops: opValue is the *field* type (e.g. Array<Int32>), not the declaring class.
                        if (opCode == EIROpCode.LoadStaticField || opCode == EIROpCode.StoreStaticField)
                        {
                            m_StringBuilder.Append(" fieldTypeIrmt:[" + irmt.ToString() + "] ");
                        }
                        else
                        {
                            m_StringBuilder.Append(" val irmt:[" + irmt.ToString() + "] ");
                        }
                    }
                    else if( irm != null )
                    {
                        m_StringBuilder.Append(" val irm:[" + irm.id + "] ");
                    }
                    else
                    {
                        m_StringBuilder.Append(" val:[" + opValue.ToString() + "] ");
                    }
                }
            }
            else if (TryGetRuntimeDefTypeDebugString(out var runtimeTypeText))
            {
                if (opCode == EIROpCode.LoadStaticField || opCode == EIROpCode.StoreStaticField)
                {
                    m_StringBuilder.Append(" fieldTypeIrmt:[");
                    m_StringBuilder.Append(runtimeTypeText);
                    m_StringBuilder.Append("] ");
                }
                else
                {
                    m_StringBuilder.Append(" val irmt:[");
                    m_StringBuilder.Append(runtimeTypeText);
                    m_StringBuilder.Append("] ");
                }
            }
            else if (opCode == EIROpCode.LoadConstString)
            {
                var resolved = IRManager.instance.GetStringIRStack(index);
                if (resolved != null)
                {
                    m_StringBuilder.Append(" val: string[");
                    m_StringBuilder.Append(EscapeForIRDebug(resolved));
                    m_StringBuilder.Append("] ");
                }
            }
            if ((opCode == EIROpCode.LoadStaticField || opCode == EIROpCode.StoreStaticField)
                && !string.IsNullOrEmpty(debugStaticOwnerIrName))
            {
                m_StringBuilder.Append("staticOwner:[" + debugStaticOwnerIrName + "] ");
            }
            return m_StringBuilder.ToString();
        }

        private bool TryGetRuntimeDefTypeDebugString(out string text)
        {
            text = null;

            if (Payload == null || Payload.Length == 0)
            {
                return false;
            }

            // 仅对携带 RuntimeDefType 的指令尝试解析，避免误把其他 payload 当作类型。
            if (opCode != EIROpCode.LoadStaticField
                && opCode != EIROpCode.StoreStaticField
                && opCode != EIROpCode.CastClass
                && opCode != EIROpCode.LoadConstType
                && opCode != EIROpCode.NewArray
                && opCode != EIROpCode.NewTemplateObject)
            {
                return false;
            }

            try
            {
                /* Dual-format: binary v1 (payload[0] == 0x01) or legacy JSON ('{'). */
                if (TryBinDecodeDefTypePayload(Payload, out var binNode))
                {
                    text = FormatRuntimeDefTypeExport(binNode);
                    return !string.IsNullOrEmpty(text);
                }

                var json = Encoding.UTF8.GetString(Payload);
                if (string.IsNullOrWhiteSpace(json) || json[0] != '{')
                {
                    return false;
                }

                var pkg = JsonSerializer.Deserialize<RuntimeDefTypeExport>(json, FrontJsonOptions.Plain);
                if (pkg == null)
                {
                    return false;
                }

                text = FormatRuntimeDefTypeExport(pkg);
                return !string.IsNullOrEmpty(text);
            }
            catch
            {
                return false;
            }
        }

        private static string FormatRuntimeDefTypeExport(RuntimeDefTypeExport item)
        {
            if (item == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(string.IsNullOrEmpty(item.className) ? "<null>" : item.className);

            if (item.runtimeDefTypeList != null && item.runtimeDefTypeList.Count > 0)
            {
                sb.Append('<');
                for (int i = 0; i < item.runtimeDefTypeList.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatRuntimeDefTypeExport(item.runtimeDefTypeList[i]));
                }
                sb.Append('>');
            }

            if (item.templateIndex >= 0)
            {
                sb.Append("[T:");
                sb.Append(item.templateIndex);
                sb.Append(']');
            }

            return sb.ToString();
        }

        private static string EscapeForIRDebug(string value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            return value
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
