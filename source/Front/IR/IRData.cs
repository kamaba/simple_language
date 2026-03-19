//****************************************************************************
//  File:      IRData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;

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
        // 当前 IRData 的字节长度（包括 Payload 的长度）——用于导出序列化时参考
        // ByteLength currently holds the payload length. The final serialized
        // instruction length is computed as (1 + ByteLength) by IRMethod (1 byte for opcode)
        // The instruction stream offset (start position) is stored in `offset`.
        public int       ByteLength = 0;
        // byte offset in the serialized instruction stream
        public int       offset = 0;
        public int       index;                  //索引
        public DebugInfo debugInfo;              //调试信息

        public IRData()
        {

        }

        // 设置 opValue 并尝试将值类型序列化到 Payload，同时更新 ByteLength
        public void SetOpValue(object v)
        {
            // keep a copy for debug but use Payload for runtime consumption
            _opValue = v;
            PackOpValue();
            UpdateByteLength();
        }

        // 将基础值类型序列化到 Payload（仅支持常见原始类型和字符串）
        private void PackOpValue()
        {
            Payload = null;
            if (opValue == null) return;

            // call payload fallback: write method id at SetOpValue stage
            if (opValue is IRMethodCall imc)
            {
                var export = CreateRuntimeCallExport(imc);
                Payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(export));
                return;
            }

            switch (Type.GetTypeCode(opValue.GetType()))
            {
                case TypeCode.Boolean:
                    Payload = BitConverter.GetBytes((bool)opValue);
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
                methodId = call.irMethod?.id ?? string.Empty,
                methodName = call.methodName ?? string.Empty,
                paramCount = call.paramCount,
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
                isTemplate = mt.templateIndex >= 0,
                runtimeDefTypeList = CreateRuntimeDefTypeExportList(mt.irMetaTypeList),
            };
        }

        private sealed class RuntimeCallExport
        {
            public RuntimeDefTypeExport runtimeDefType { get; set; }
            public List<RuntimeDefTypeExport> templateRuntimeDefTypeList { get; set; } = new();
            public string methodId { get; set; } = string.Empty;
            public string methodName { get; set; } = string.Empty;
            public int paramCount { get; set; }
        }

        private sealed class RuntimeDefTypeExport
        {
            public int classId { get; set; }
            public string className { get; set; } = string.Empty;
            public int ownerClassId { get; set; }
            public string ownerClassName { get; set; } = string.Empty;
            public int templateIndex { get; set; } = -1;
            public bool isTemplate { get; set; }
            public List<RuntimeDefTypeExport> runtimeDefTypeList { get; set; } = new();
        }

        // 更新 ByteLength（仅计算 Payload 长度，未来可扩展为包含头部字段）
        public void UpdateByteLength()
        {
            ByteLength = (Payload != null) ? Payload.Length : 0;
        }

        // Finalize packaging for complex opValue types (labels, methods, meta types)
        // Convert remaining opValue into Payload bytes so IRData is self-contained for serialization.
        public void FinalizePack()
        {
            if (Payload != null) return;
            if (_opValue == null) return;

            // If opValue is an IRData (label reference), serialize its resolved index
            if (_opValue is IRData idRef)
            {
                int idx = idRef.index;
                Payload = BitConverter.GetBytes(idx);
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
                case EIROpCode.LoadConstByte:
                    if (TryGetByte(out var bb)) { _opValue = bb; return; }
                    break;
                case EIROpCode.LoadConstSByte:
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
                case EIROpCode.LoadConstFloat32:
                    if (TryGetSingle(out var f)) { _opValue = f; return; }
                    break;
                case EIROpCode.LoadConstFloat64:
                    if (TryGetDouble(out var d)) { _opValue = d; return; }
                    break;
                case EIROpCode.LoadConstString:
                    if (TryGetString(out var s)) { _opValue = s; return; }
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

        // Get serialized instruction length. If `next` is provided, length is the
        // distance between this.offset and next.offset (as used by CLR/JVM style
        // instruction layouts). If `next` is null, fall back to 1 + payload length
        // (1 byte for opcode + payload bytes).
        public int GetSerializedLength(IRData next)
        {
            if (next != null)
            {
                return next.offset - this.offset;
            }
            return 1 + (Payload != null ? Payload.Length : 0);
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
        public void SetDebugInfoByToken( Token token )
        {
            if(token != null )
            {
                debugInfo.path = token.path;
                debugInfo.name = token.lexeme?.ToString();
                debugInfo.beginLine = token.sourceBeginLine;
                debugInfo.beginChar = token.sourceBeginChar;
                debugInfo.endLine = token.sourceEndLine;
                debugInfo.endChar = token.sourceEndChar;
            }
        }
        public override string ToString()
        {
            StringBuilder m_StringBuilder = new StringBuilder();
            m_StringBuilder.Append( id + "   [ " + debugInfo.path + ":" + debugInfo.beginLine.ToString() + "]" + " [" + opCode.ToString() + "] index:[" + index.ToString() + "]");
            if (opValue != null)
            {
                MetaType mt = opValue as MetaType;
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
            return m_StringBuilder.ToString();
        }
    }
}
