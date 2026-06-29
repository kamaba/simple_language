//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleLanguage.Logging;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM
{
    public class DebugInfo
    {
        [JsonInclude] public string path = "";
        /// <summary>Source lexeme / symbol text (from Token), optional.</summary>
        [JsonInclude] public string name = "";
        [JsonInclude] public int beginLine = 0;
        [JsonInclude] public int beginChar = 0;
        [JsonInclude] public int endLine = 0;
        [JsonInclude] public int endChar = 0;
        /// <summary>Optional IR hint (e.g. opcode role), optional.</summary>
        [JsonInclude] public string info = "";

        /// <summary>Single-line location for logs (file:line:col and optional symbol).</summary>
        public string FormatDiagnosticLine()
        {
            if (string.IsNullOrEmpty(path) && beginLine <= 0 && string.IsNullOrEmpty(name))
                return "";
            var namePart = string.IsNullOrEmpty(name) ? "" : " `" + name + "`";
            var infoPart = string.IsNullOrEmpty(info) ? "" : " (" + info + ")";
            if (string.IsNullOrEmpty(path) && beginLine <= 0)
                return (name ?? string.Empty).Trim() + infoPart;
            return path + "(" + beginLine + "," + beginChar + ")-(" + endLine + "," + endChar + ")" + namePart + infoPart;
        }
    }
    public class Instruction
    {
        [JsonInclude] public int id = 0;
        [JsonInclude] public EIROpCode opCode;                 //指令类型
        // 与 Front 的 IRData 一致：单一存储 _opValue，经 SetOpValue 同步到 Payload。
        private object _opValue;
        public object opValue { get => _opValue; set => SetOpValue(value); }

        // 序列化后的原始数据（仅用于值类型或需要内嵌的常量）
        [JsonInclude] public byte[] Payload = null;
        // 当前 IRData 的字节长度（包括 Payload 的长度）——用于导出序列化时参考
        [JsonInclude] public int ByteLength = 0;
        /// <summary>Byte offset in the serialized instruction stream (IR/export).</summary>
        [JsonInclude] public int offset = 0;
        [JsonInclude] public int index;                  //索引
        [JsonInclude] public DebugInfo debugInfo;              //调试信息

        public Instruction()
        {

        }

        /// <summary>设置 opValue 并同步写入 Payload / ByteLength（与 IRData.SetOpValue 对称）。</summary>
        public void SetOpValue(object? v)
        {
            _opValue = v;
        }
        // Try to unpack payload back into opValue for debugging/inspection
        //public void UnpackOpValueFromPayload()
        //{
        //    if (Payload == null || Payload.Length == 0)
        //    {
        //        _opValue = null;
        //        return;
        //    }
        //    switch (opCode)
        //    {
        //        case EIROpCode.LoadConstBoolean:
        //            if (TryGetBoolean(out var bv)) { _opValue = bv; return; }
        //            break;
        //        case EIROpCode.LoadConstUInt8:
        //            if (TryGetByte(out var bb)) { _opValue = bb; return; }
        //            break;
        //        case EIROpCode.LoadConstInt8:
        //            if (TryGetSByte(out var sb)) { _opValue = sb; return; }
        //            break;
        //        case EIROpCode.LoadConstInt16:
        //            if (TryGetInt16(out var s16)) { _opValue = s16; return; }
        //            break;
        //        case EIROpCode.LoadConstUInt16:
        //            if (TryGetUInt16(out var u16)) { _opValue = u16; return; }
        //            break;
        //        case EIROpCode.LoadConstInt32:
        //            if (TryGetInt32(out var i32)) { _opValue = i32; return; }
        //            break;
        //        case EIROpCode.LoadConstUInt32:
        //            if (TryGetUInt32(out var ui32)) { _opValue = ui32; return; }
        //            break;
        //        case EIROpCode.LoadConstInt64:
        //            if (TryGetInt64(out var i64)) { _opValue = i64; return; }
        //            break;
        //        case EIROpCode.LoadConstUInt64:
        //            if (TryGetUInt64(out var ui64)) { _opValue = ui64; return; }
        //            break;
        //        case EIROpCode.LoadConstFloat32:
        //            if (TryGetSingle(out var f)) { _opValue = f; return; }
        //            break;
        //        case EIROpCode.LoadConstFloat64:
        //            if (TryGetDouble(out var d)) { _opValue = d; return; }
        //            break;
        //        case EIROpCode.LoadConstString:
        //            if (TryGetString(out var s)) { _opValue = s; return; }
        //            break;
        //        case EIROpCode.CallSystemMethod:
        //            // Symmetric with Front IRData.PackOpValue(SLSystemMethodCallPackage): JSON in Payload.
        //            if (TryGetSystemMethodCallPackage(out var sysPkg) && sysPkg != null)
        //            {
        //                _opValue = sysPkg;
        //                return;
        //            }
        //            break;
        //        default:
        //            // fallback: try string then numeric interpretations
        //            if (TryGetString(out var ss)) { _opValue = ss; return; }
        //            if (TryGetInt32(out var ii)) { _opValue = ii; return; }
        //            break;
        //    }
        //    // if still not set, keep raw bytes
        //    _opValue = Payload;
        //}

        // Helpers to read payload as common types (fall back to opValue if present)
        public bool TryGetBoolean(out bool v)
        {
            if (Payload != null && Payload.Length >= 1)
            {
                v = BitConverter.ToBoolean(Payload, 0);
                return true;
            }
            v = false;
            //if (opValue is bool b)
            //{
            //    v = b; return true;
            //}
            //v = default; 
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get boolean", debugInfo.name );
            return false;
        }
        public bool TryGetInt32(out int v)
        {
            if (Payload != null && Payload.Length >= 4)
            {
                v = BitConverter.ToInt32(Payload, 0); return true;
            }
            //if (opValue is int i) { v = i; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get int32", debugInfo.name);
            return false;
        }
        public bool TryGetUInt32(out uint v)
        {
            if (Payload != null && Payload.Length >= 4)
            {
                v = BitConverter.ToUInt32(Payload, 0); return true;
            }
            //if (opValue is uint ui) { v = ui; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get uint32", debugInfo.name);
            return false;
        }
        public bool TryGetInt64(out long v)
        {
            if (Payload != null && Payload.Length >= 8)
            {
                v = BitConverter.ToInt64(Payload, 0); return true;
            }
            //if (opValue is long l) { v = l; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get int64", debugInfo.name);
            return false;
        }
        public bool TryGetUInt64(out ulong v)
        {
            if (Payload != null && Payload.Length >= 8)
            {
                v = BitConverter.ToUInt64(Payload, 0); return true;
            }
            //if (opValue is ulong ul) { v = ul; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get uint64", debugInfo.name); 
            return false;
        }
        public bool TryGetInt16(out short v)
        {
            if (Payload != null && Payload.Length >= 2)
            {
                v = BitConverter.ToInt16(Payload, 0); return true;
            }
            //if (opValue is short s) { v = s; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get int16", debugInfo.name);
            return false;
        }
        public bool TryGetUInt16(out ushort v)
        {
            if (Payload != null && Payload.Length >= 2)
            {
                v = BitConverter.ToUInt16(Payload, 0); return true;
            }
            //if (opValue is ushort us) { v = us; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get uint16", debugInfo.name); 
            return false;
        }
        public bool TryGetUInt8(out byte v)
        {
            if (Payload != null && Payload.Length >= 1)
            {
                v = Payload[0]; return true;
            }
            //if (opValue is byte by) { v = by; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get uint8", debugInfo.name); 
            return false;
        }
        public bool TryGetInt8(out sbyte v)
        {
            if (Payload != null && Payload.Length >= 1)
            {
                v = (sbyte)Payload[0]; return true;
            }
            //if (opValue is sbyte sb) { v = sb; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get int8", debugInfo.name);
            return false;
        }
        public bool TryGetFloat32(out float v)
        {
            if (Payload != null && Payload.Length >= 4)
            {
                v = BitConverter.ToSingle(Payload, 0); return true;
            }
            //if (opValue is float f) { v = f; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get float32", debugInfo.name);
            return false;
        }
        public bool TryGetFloat64(out double v)
        {
            if (Payload != null && Payload.Length >= 8)
            {
                v = BitConverter.ToDouble(Payload, 0); return true;
            }
            //if (opValue is double d) { v = d; return true; }
            v = default;
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get float64", debugInfo.name);
            return false;
        }
        public bool TryGetString(out string s)
        {
            if (Payload != null && Payload.Length > 0)
            {
                s = Encoding.UTF8.GetString(Payload); return true;
            }
            //if (opValue is string ss) { s = ss; return true; }
            s = "";
            Log.AddRuntimeLog(LID.RuntimeVMInstructPayLoadGetValueError, debugInfo, "get float64", debugInfo.name);
            return false;
        }
        public bool TryGetRuntimeCallPackage(out SLRuntimeCallPackage pkg)
        {
            return TryGetJsonObject(out pkg);
        }

        public bool TryGetRuntimeDefTypePackage(out SLRuntimeDefTypePackage pkg)
        {
            return TryGetJsonObject(out pkg);
        }

        public bool TryGetSystemMethodCallPackage(out SLSystemMethodCallPackage pkg)
        {
            return TryGetJsonObject(out pkg);
        }

        private bool TryGetJsonObject<T>(out T value) where T : class
        {
            value = null;

            if (opValue is T direct)
            {
                value = direct;
                return true;
            }

            if (opValue is JsonElement je)
            {
                try
                {
                    if (je.ValueKind == JsonValueKind.Object)
                    {
                        var fromElement = je.Deserialize<T>();
                        if (fromElement != null)
                        {
                            value = fromElement;
                            return true;
                        }
                    }
                    else if (je.ValueKind == JsonValueKind.String)
                    {
                        var text = je.GetString();
                        if (!string.IsNullOrWhiteSpace(text) && text[0] == '{')
                        {
                            var fromStringElement = JsonSerializer.Deserialize<T>(text);
                            if (fromStringElement != null)
                            {
                                value = fromStringElement;
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            if (opValue is string s && !string.IsNullOrWhiteSpace(s) && s[0] == '{')
            {
                try
                {
                    var fromString = JsonSerializer.Deserialize<T>(s);
                    if (fromString != null)
                    {
                        value = fromString;
                        return true;
                    }
                }
                catch
                {
                }
            }

            if (Payload != null && Payload.Length > 0)
            {
                try
                {
                    var text = Encoding.UTF8.GetString(Payload);
                    if (!string.IsNullOrWhiteSpace(text) && text[0] == '{')
                    {
                        var fromPayload = JsonSerializer.Deserialize<T>(text);
                        if (fromPayload != null)
                        {
                            value = fromPayload;
                            return true;
                        }
                    }
                }
                catch
                {
                }
            }

            return false;
        }
        public override string ToString()
        {
            StringBuilder m_StringBuilder = new StringBuilder();
            var di = debugInfo;
            var path = di != null ? di.path : "";
            var line = di != null ? di.beginLine : 0;
            m_StringBuilder.Append(id + "   [ " + path + ":" + line.ToString() + "]" + " [" + opCode.ToString() + "] index:[" + index.ToString() + "]");
            if (opValue != null)
            {
                //MetaType mt = opValue as MetaType;
                //IRMethod irm = opValue as IRMethod;
                //if (opValue.GetType() == typeof(Int32))
                //{
                //    m_StringBuilder.Append(" val: int32[" + opValue + "] ");
                //}
                //else
                //{
                //    if (mt != null)
                //    {
                //        m_StringBuilder.Append(" val mt:[" + mt.name + "] ");
                //    }
                //    else if (irm != null)
                //    {
                //        m_StringBuilder.Append(" val irm:[" + irm.id + "] ");
                //    }
                //    else
                //    {
                //        m_StringBuilder.Append(" val:[" + opValue.ToString() + "] ");
                //    }
                //}
            }
            return m_StringBuilder.ToString();
        }
    }
}
