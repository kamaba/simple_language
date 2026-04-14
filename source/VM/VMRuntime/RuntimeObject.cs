using SimpleLanguage.VM.Runtime;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeObject
    {
        public RuntimeType runtimeType => m_RuntimeType;
        public EVMType eType => m_RuntimeType != null ? m_RuntimeType.eType : EVMType.Null;
        public SObject sobject => m_SObject;
        public RuntimeVariable runtimeVariable => m_RuntimeVariable;
        public bool isNull => IsMemberDataDirectType(eType) && hasMemberDataSlice ? false : m_SObject == null;

        /// <summary>IR / Meta 渚ф垚鍛樺彉閲?id锛堜笌 <see cref="RuntimeVariable.id"/> 涓€鑷达級锛涙棤鍏宠仈鍙橀噺鏃朵负 0銆?/summary>
        public int memberVariableId => m_RuntimeVariable?.id ?? 0;

        /// <summary>鍦ㄦ墍灞?<see cref="ClassObject"/> 鎴愬憳琛ㄤ腑鐨勪笅鏍囥€?/summary>
        public int memberIndex => m_Index;
        /// <summary>鍦ㄧ揣鍑戞垚鍛樼紦鍐插尯涓殑璧峰鍋忕Щ锛堝疄渚嬶細<see cref="ClassObject.memberData"/>锛涢潤鎬侊細<see cref="RuntimeType.memberData"/>锛夈€?/summary>
        public int memberDataStart => m_Start;
        /// <summary>绱у噾鎴愬憳缂撳啿鍖轰腑鏈Ы浣嶅瓧鑺傞暱搴︺€?/summary>
        public int memberDataLength => m_Length;
        public bool hasMemberDataSlice => m_MemberDataBuffer != null && m_Length > 0;

        private RuntimeVariable m_RuntimeVariable = null;
        private RuntimeType m_RuntimeType = null;
        private SObject m_SObject = null;
        private int m_Index = 0;
        private int m_Start = 0;
        private int m_Length = 0;
        private byte[]? m_MemberDataBuffer = null;
        public RuntimeObject( RuntimeType rt, SObject sobj )
        {
            m_RuntimeType = rt;
            m_SObject = sobj;
        }
        public RuntimeObject( RuntimeType rt, RuntimeVariable rv, SObject sobj )
        {
            m_RuntimeVariable = rv;
            m_RuntimeType = rt;
            m_SObject = sobj;
        }

        internal void AttachMemberDataSlice(byte[]? classMemberData, int start, int length, int memberIndex)
        {
            m_MemberDataBuffer = classMemberData;
            m_Start = start;
            m_Length = length;
            m_Index = memberIndex;
        }

        public bool TryReadMemberDataToSValue(ref SValue svalue)
        {
            if (m_MemberDataBuffer == null || m_Length <= 0 || m_RuntimeType == null)
                return false;
            if (m_Start + m_Length > m_MemberDataBuffer.Length)
                return false;

            ReadSpanToSValue(m_MemberDataBuffer.AsSpan(m_Start, m_Length), m_RuntimeType.eType, ref svalue);
            return true;
        }

        private static void ReadSpanToSValue(ReadOnlySpan<byte> span, EVMType evmType, ref SValue svalue)
        {
            switch (evmType)
            {
                case EVMType.Boolean:
                    svalue.SetBoolValue(span.Length >= 4 && BinaryPrimitives.ReadInt32LittleEndian(span) != 0);
                    break;
                case EVMType.Byte:
                    svalue.SetInt8Value(span.Length > 0 ? span[0] : (byte)0);
                    break;
                case EVMType.SByte:
                    svalue.SetSInt8Value(span.Length > 0 ? unchecked((sbyte)span[0]) : (sbyte)0);
                    break;
                case EVMType.Int16:
                    svalue.SetInt16Value(span.Length >= 2 ? BinaryPrimitives.ReadInt16LittleEndian(span) : (short)0);
                    break;
                case EVMType.UInt16:
                    svalue.SetUInt16Value(span.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(span) : (ushort)0);
                    break;
                case EVMType.Int32:
                    svalue.SetInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(span) : 0);
                    break;
                case EVMType.UInt32:
                    svalue.SetUInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadUInt32LittleEndian(span) : 0u);
                    break;
                case EVMType.Int64:
                    svalue.SetInt64Value(span.Length >= 8 ? BinaryPrimitives.ReadInt64LittleEndian(span) : 0L);
                    break;
                case EVMType.UInt64:
                    svalue.SetUInt64Value(span.Length >= 8 ? BinaryPrimitives.ReadUInt64LittleEndian(span) : 0uL);
                    break;
                case EVMType.Float32:
                    svalue.SetFloatValue(span.Length >= 4 ? BinaryPrimitives.ReadSingleLittleEndian(span) : 0f);
                    break;
                case EVMType.Float64:
                    svalue.SetDoubleValue(span.Length >= 8 ? BinaryPrimitives.ReadDoubleLittleEndian(span) : 0d);
                    break;
                default:
                    svalue.SetInt32Value(span.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(span) : 0);
                    break;
            }
        }

        private static bool IsMemberDataDirectType(EVMType evmType)
        {
            return evmType == EVMType.Boolean
                || evmType == EVMType.Byte
                || evmType == EVMType.SByte
                || evmType == EVMType.Int16
                || evmType == EVMType.UInt16
                || evmType == EVMType.Int32
                || evmType == EVMType.UInt32
                || evmType == EVMType.Int64
                || evmType == EVMType.UInt64
                || evmType == EVMType.Float32
                || evmType == EVMType.Float64;
        }

        private bool TryGetMemberDataSpan(out Span<byte> span)
        {
            span = default;
            if (m_MemberDataBuffer == null || m_Length <= 0)
                return false;
            if (m_Start < 0 || m_Start + m_Length > m_MemberDataBuffer.Length)
                return false;

            span = m_MemberDataBuffer.AsSpan(m_Start, m_Length);
            return true;
        }

        private static void WriteSValueToMemberDataSpan(Span<byte> span, EVMType evmType, ref SValue sval)
        {
            if (span.Length <= 0)
                return;

            switch (evmType)
            {
                case EVMType.Boolean:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteInt32LittleEndian(span, sval.int8Value == 1 ? 1 : 0);
                    break;
                case EVMType.Byte:
                    span[0] = sval.int8Value;
                    break;
                case EVMType.SByte:
                    span[0] = unchecked((byte)sval.sint8Value);
                    break;
                case EVMType.Int16:
                    if (span.Length >= 2)
                        BinaryPrimitives.WriteInt16LittleEndian(span, sval.int16Value);
                    break;
                case EVMType.UInt16:
                    if (span.Length >= 2)
                        BinaryPrimitives.WriteUInt16LittleEndian(span, sval.uint16Value);
                    break;
                case EVMType.Int32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteInt32LittleEndian(span, sval.int32Value);
                    break;
                case EVMType.UInt32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteUInt32LittleEndian(span, sval.uint32Value);
                    break;
                case EVMType.Int64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteInt64LittleEndian(span, sval.int64Value);
                    break;
                case EVMType.UInt64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteUInt64LittleEndian(span, sval.uint64Value);
                    break;
                case EVMType.Float32:
                    if (span.Length >= 4)
                        BinaryPrimitives.WriteSingleLittleEndian(span, sval.floatValue);
                    break;
                case EVMType.Float64:
                    if (span.Length >= 8)
                        BinaryPrimitives.WriteDoubleLittleEndian(span, sval.doubleValue);
                    break;
            }
        }

        private void ClearMemberDataSlice()
        {
            if (m_MemberDataBuffer == null || m_Length <= 0)
                return;
            if (m_Start + m_Length <= m_MemberDataBuffer.Length)
                m_MemberDataBuffer.AsSpan(m_Start, m_Length).Clear();
        }

        private void WriteCurrentValueToMemberData()
        {
            if (m_MemberDataBuffer == null || m_Length <= 0 || m_RuntimeType == null)
                return;
            if (m_Start + m_Length > m_MemberDataBuffer.Length)
                return;

            Span<byte> span = m_MemberDataBuffer.AsSpan(m_Start, m_Length);
            var evm = m_RuntimeType.eType;

            if (m_SObject == null)
            {
                span.Clear();
                if (span.Length >= 4)
                    BinaryPrimitives.WriteInt32LittleEndian(span, 0);
                return;
            }

            switch (evm)
            {
                case EVMType.Boolean:
                    BinaryPrimitives.WriteInt32LittleEndian(span, (m_SObject as BoolObject)?.value == true ? 1 : 0);
                    break;
                case EVMType.Byte:
                    if (span.Length > 0)
                        span[0] = (m_SObject as Int8Object)?.value ?? 0;
                    break;
                case EVMType.SByte:
                    if (span.Length > 0)
                        span[0] = unchecked((byte)((m_SObject as SInt8Object)?.value ?? 0));
                    break;
                case EVMType.Int16:
                    BinaryPrimitives.WriteInt16LittleEndian(span, (m_SObject as Int16Object)?.value ?? 0);
                    break;
                case EVMType.UInt16:
                    BinaryPrimitives.WriteUInt16LittleEndian(span, (m_SObject as UInt16Object)?.value ?? 0);
                    break;
                case EVMType.Int32:
                    BinaryPrimitives.WriteInt32LittleEndian(span, (m_SObject as Int32Object)?.value ?? 0);
                    break;
                case EVMType.UInt32:
                    if (m_SObject is UInt32Object u32o)
                        BinaryPrimitives.WriteUInt32LittleEndian(span, u32o.value);
                    else if (m_SObject is Int32Object i32u)
                        BinaryPrimitives.WriteUInt32LittleEndian(span, unchecked((uint)i32u.value));
                    else
                        BinaryPrimitives.WriteUInt32LittleEndian(span, 0u);
                    break;
                case EVMType.Int64:
                    BinaryPrimitives.WriteInt64LittleEndian(span, (m_SObject as Int64Object)?.value ?? 0L);
                    break;
                case EVMType.UInt64:
                    if (m_SObject is UInt64Object u64o)
                        BinaryPrimitives.WriteUInt64LittleEndian(span, u64o.value);
                    else if (m_SObject is Int64Object i64u)
                        BinaryPrimitives.WriteUInt64LittleEndian(span, unchecked((ulong)i64u.value));
                    else
                        BinaryPrimitives.WriteUInt64LittleEndian(span, 0uL);
                    break;
                case EVMType.Float32:
                    BinaryPrimitives.WriteSingleLittleEndian(span, (m_SObject as Float32Object)?.value ?? 0f);
                    break;
                case EVMType.Float64:
                    BinaryPrimitives.WriteDoubleLittleEndian(span, (m_SObject as Float64Object)?.value ?? 0d);
                    break;
                case EVMType.String:
                case EVMType.Class:
                case EVMType.Array:
                case EVMType.Object:
                case EVMType.Type:
                case EVMType.Member:
                default:
                    BinaryPrimitives.WriteInt32LittleEndian(span, m_SObject.GetHashCode());
                    break;
            }
        }

        public bool GetBoolean()
        {
            if (m_RuntimeType != null && m_RuntimeType.eType == EVMType.Boolean)
            {
                var tmp = default(SValue);
                if (TryReadMemberDataToSValue(ref tmp))
                    return tmp.int8Value == 1;
            }
            if( m_SObject is BoolObject bl )
            {
                return bl.value;
            }
            return false;
        }
        public void SetNull()
        {
            m_SObject = null;
            ClearMemberDataSlice();
        }
        public void SetSObject( SObject sobj )
        {
            m_SObject = sobj;
            WriteCurrentValueToMemberData();
        }
        public void SetSObjectBySValue( ref SValue sval )
        {
            /*
            SObject anyobj = null;
            bool isAny = false;
            if (mro.eType == EVMType.Object)
            {
                isAny = true;
                if( mro.sobject == null )
                {
                    mro.SetSObjectBySValue(ref svalue);
                    return;
                }
                anyobj = mro.sobject;
            }
            else
            {
                if( mro.sobject == null )
                {
                    SObject sobj = ObjectManager.CreateObjectByRuntimeType(mro.runtimeType, true);
                    mro.SetSObject(sobj);
                }
            }
            switch (svalue.eType)
            {
                case EVMType.Null:
                    {
                        mro.SetNull();
                    }
                    break;
                case EVMType.Boolean:
                //case EVMType.RawBoolean:
                    {
                        BoolObject boolObj = null;
                        if (anyobj != null)
                        {
                            //boolObj = new BoolObject(svalue.int8Value == 1);
                            //anyobj.SetValue(boolObj);
                            //m_MemberObjectArray[index] = boolObj;
                            anyobj.SetValueByType( EVMType.Boolean, svalue.int8Value == 1 );
                            return;
                        }

                        boolObj = mro.sobject as BoolObject;
                        if (boolObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Boolean 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                            return;
                        }
                        boolObj.SetValue(svalue.int8Value == 1);

                    }
                    break;
                case EVMType.Byte:
                //case EVMType.RawByte:
                    {
                        Int8Object byteObj = null;
                        if (anyobj != null)
                        {
                            //byteObj = new Int8Object(svalue.int8Value);
                            //anyobj.SetValue(byteObj);
                            //m_MemberObjectArray[index] = byteObj;
                            anyobj.SetValueByType(EVMType.Byte, svalue.int8Value );
                            return;
                        }


                        byteObj = mro.sobject as Int8Object;
                        if (byteObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Byte 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                case EVMType.SByte:
                //case EVMType.RawSByte:
                    {
                        SInt8Object sbyteObj = null;
                        if (anyobj != null)
                        {
                            //sbyteObj = new SInt8Object(svalue.sint8Value);
                            //anyobj.SetValue(sbyteObj);
                            //m_MemberObjectArray[index] = sbyteObj;
                            anyobj.SetValueByType(EVMType.SByte, svalue.sint8Value);
                            return;
                        }

                        sbyteObj = mro.sobject as SInt8Object;
                        if (sbyteObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Sbyte 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                            return;
                        }
                        sbyteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                case EVMType.Int16:
                //case EVMType.RawInt16:
                    {
                        Int16Object int16Obj = null;
                        if (anyobj != null)
                        {
                            //int16Obj = new Int16Object(svalue.int16Value);
                            ////anyobj.SetValue(int16Obj);
                            //m_MemberObjectArray[index] = int16Obj;
                            anyobj.SetValueByType(EVMType.Int16, svalue.int16Value);
                            return;
                        }

                        int16Obj = mro.sobject as Int16Object;
                        if (int16Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Int16 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                            return;
                        }
                        int16Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EVMType.UInt16:
                //case EVMType.RawUInt16:
                    {
                        UInt16Object uint16Obj = null;
                        if (anyobj != null)
                        {
                            //uint16Obj = new UInt16Object(svalue.uint16Value);
                            ////anyobj.SetValue(uint16Obj);
                            //m_MemberObjectArray[index] = uint16Obj;
                            anyobj.SetValueByType(EVMType.UInt16, svalue.uint16Value);
                            return;
                        }

                        uint16Obj = mro.sobject as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "UInt16 璇ョ被鍨嬩笉鏄疘nt16绫诲瀷!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                case EVMType.Int32:
                //case EVMType.RawInt32:
                    {
                        Int32Object int32Obj = null;
                        if (anyobj != null)
                        {
                            //int32Obj = new Int32Object(svalue.int32Value);
                            //anyobj.SetValue(int32Obj);
                            //m_MemberObjectArray[index] = int32Obj;
                            anyobj.SetValueByType(EVMType.Int32, svalue.int32Value);
                            return;
                        }
                        int32Obj = mro.sobject as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Int32 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EVMType.UInt32:
                //case EVMType.RawUInt32:
                    {
                        UInt32Object uint32Obj = null;
                        if (anyobj != null)
                        {
                            //uint32Obj = new UInt32Object(svalue.uint32Value);
                            ////anyobj.SetValue(uint32Obj);
                            //m_MemberObjectArray[index] = uint32Obj;
                            anyobj.SetValueByType(EVMType.UInt32, svalue.uint32Value);
                            return;
                        }

                        uint32Obj = mro.sobject as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "UInt32 璇ョ被鍨嬩笉鏄疷Int32绫诲瀷!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                case EVMType.Int64:
                //case EVMType.RawInt64:
                    {
                        Int64Object int64Obj = null;
                        if (anyobj != null)
                        {
                            //int64Obj = new Int64Object(svalue.int64Value);
                            ////anyobj.SetValue(int64Obj);
                            //m_MemberObjectArray[index] = int64Obj;
                            anyobj.SetValueByType(EVMType.Int64, svalue.int64Value);
                            return;
                        }

                        int64Obj = mro.sobject as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Int64 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EVMType.UInt64:
                //case EVMType.RawUInt64:
                    {
                        UInt64Object uint64Obj = null;
                        if (anyobj != null)
                        {
                            //uint64Obj = new UInt64Object(svalue.uint64Value);
                            //anyobj.SetValueByType(uint64Obj);
                            //m_MemberObjectArray[index] = uint64Obj;
                            anyobj.SetValueByType(EVMType.UInt64, svalue.uint64Value);
                            return;
                        }

                        uint64Obj = mro.sobject as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "UInt64 璇ョ被鍨嬩笉鏄疘nt64绫诲瀷!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                case EVMType.Float32:
                //case EVMType.RawFloat32:
                    {
                        Float32Object floatObj = null;
                        if (anyobj != null)
                        {
                            //floatObj = new Float32Object(svalue.floatValue);
                            ////anyobj.SetValue(floatObj);
                            //m_MemberObjectArray[index] = floatObj;
                            anyobj.SetValueByType(EVMType.Float32, svalue.floatValue);
                            return;
                        }

                        floatObj = mro.sobject as Float32Object;
                        if (floatObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Float 璇ョ被鍨嬩笉鏄痜loat绫诲瀷!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                case EVMType.Float64:
                //case EVMType.RawFloat64:
                    {
                        Float64Object doubleObj = null;
                        if (anyobj != null)
                        {
                            //doubleObj = new Float64Object(svalue.doubleValue);
                            ////anyobj.SetValue(doubleObj);
                            //m_MemberObjectArray[index] = doubleObj;
                            anyobj.SetValueByType(EVMType.Float64, svalue.doubleValue);
                            return;
                        }

                        doubleObj = mro.sobject as Float64Object;
                        if (doubleObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "Double 璇ョ被鍨嬩笉鏄疍ouble绫诲瀷!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                case EVMType.String:
                //case EVMType.RawString:
                    {
                        StringObject stringObj = null;
                        if (anyobj != null)
                        {
                            //stringObj = new StringObject(svalue.stringValue);
                            ////anyobj.SetValue(stringObj);
                            //m_MemberObjectArray[index] = stringObj;
                            anyobj.SetValueByType(EVMType.String, svalue.stringValue);
                            return;
                        }

                        stringObj = mro.sobject as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Assert(false);
                            Log.AddVM(LID.Unknown, "String 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EVMType.Object:
                    {
                        if( anyobj != null )
                        {
                            anyobj.SetValueByType(mro.sobject.eType, svalue.sobject.value);
                            //anyobj.SetValue(svalue.sobject.value as SObject);
                        }
                        else
                        {
                            Debug.Assert(false, "娌℃湁閫傚綋鐨勫尮閰嶇被鍨");
                        }
                    }break;
                case EVMType.Class:
                case EVMType.Array:
                    {
                        var mva = mro;

                        
                        //if (mva.eType == EVMType.Byte)
                        //{
                        //    Int8Object byteObj = mva as Int8Object;
                        //    if (byteObj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "Class Int8Object 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                        //        return;
                        //    }
                        //    byteObj.SetValue(svalue.int8Value);
                        //}
                        //else if (mva.eType == EVMType.SByte)
                        //{

                        //    SInt8Object sbyteObj = mva as SInt8Object;
                        //    if (sbyteObj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "Class SInt8Object 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                        //        return;
                        //    }
                        //    sbyteObj.SetValue(svalue.sint8Value);
                        //}
                        //else if (mva.eType == EVMType.Int16)
                        //{

                        //    Int16Object int16Obj = mva as Int16Object;
                        //    if (int16Obj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "Class Int16Object 璇ョ被鍨嬩笉鏄疘nt16绫诲瀷!!");
                        //        return;
                        //    }
                        //    int16Obj.SetValue(svalue.int16Value);
                        //}
                        //else if (mva.eType == EVMType.UInt16)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "Class UInt32Object 璇ョ被鍨嬩笉鏄疷Int32绫诲瀷!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EVMType.Int32)
                        //{
                        //    Int32Object int32Obj = mva as Int32Object;
                        //    if (int32Obj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "Class Int32Object 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                        //        return;
                        //    }
                        //    int32Obj.SetValue(svalue.int32Value);
                        //}
                        //else if (mva.eType == EVMType.UInt32)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "Class UInt32Object 璇ョ被鍨嬩笉鏄疘nt32绫诲瀷!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EVMType.Int64)
                        //{

                        //    Int64Object int64Obj = mva as Int64Object;
                        //    if (int64Obj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "璇ョ被鍨嬩笉鏄疘nt64绫诲瀷!!");
                        //        return;
                        //    }
                        //    int64Obj.SetValue(svalue.int64Value);
                        //}
                        //else if (mva.eType == EVMType.UInt64)
                        //{

                        //    UInt64Object uint64Obj = mva as UInt64Object;
                        //    if (uint64Obj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "璇ョ被鍨嬩笉鏄疘nt64绫诲瀷!!");
                        //        return;
                        //    }
                        //    uint64Obj.SetValue(svalue.uint64Value);
                        //}
                        //else if (mva.eType == EVMType.String)
                        //{

                        //    StringObject stringObj = mva as StringObject;
                        //    if (stringObj == null)
                        //    {
                        //        Log.AddVM(LID.Unknown, "璇ョ被鍨嬩笉鏄痵tringObj绫诲瀷!!");
                        //        return;
                        //    }
                        //    stringObj.SetValue(svalue.stringValue);
                        //}
                        ////else if( mva.eType == EVMType.Object )
                        ////{
                        ////    AnyObject anyObj = m_MemberObjectArray[index] as AnyObject;
                        ////    if (anyObj == null)
                        ////    {
                        ////        anyObj.SetValue(EVMType.Class, svalue.sobject);
                        ////        return;
                        ////    }
                        ////    //classObj.SetValue(svalue.sobject as ClassObject);
                        ////    m_MemberObjectArray[index] = svalue.sobject;
                        ////}
                        //else
                        {
                            ClassObject classObj = null;
                            if (anyobj != null)
                            {
                                //m_MemberObjectArray[index] = svalue.sobject;
                                anyobj.SetValueByType( EVMType.Class, svalue.sobject );
                                return;
                            }
                            classObj = mro.sobject as ClassObject;
                            if (classObj == null)
                            {
                                classObj.SetSValue(svalue.sobject as ClassObject);
                                //AnyObject anyObj = m_MemberObjectArray[index] as AnyObject;
                                //if( anyObj != null )
                                //{
                                //    anyObj.SetValue(EVMType.Class, svalue.sobject);
                                //    return;
                                //}
                                //Debug.Assert(false);
                                //Log.AddVM(LID.Unknown, "璇ョ被鍨嬩笉鏄痗lassObj绫诲瀷!!");
                                return;
                            };
                            mro.SetSObject( svalue.sobject );
                            //m_MemberObjectArray[index].SetValueByType( EVMType.Class, svalue.sobject );
                        }
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
            */


            if (sval.isNull)
            {
                m_SObject = null;
                ClearMemberDataSlice();
                return;
            }

            if (m_RuntimeType != null
                && IsMemberDataDirectType(m_RuntimeType.eType)
                && TryGetMemberDataSpan(out var directSpan))
            {
                WriteSValueToMemberDataSpan(directSpan, m_RuntimeType.eType, ref sval);
                m_SObject = null;
                return;
            }

            if (eType == EVMType.Object)
            {
                if ( m_SObject == null)
                {
                    m_SObject = sval.GetSObject();
                    WriteCurrentValueToMemberData();
                    return;
                }
            }
            else
            {
                if (m_SObject == null && RuntimeTypeManager.IsCoreRuntimeType( m_RuntimeType ) )
                {
                    m_SObject = ObjectManager.CreateObjectByRuntimeType(runtimeType, true);
                }
            }

            switch(m_RuntimeType.eType)
            {
                case EVMType.Boolean:
                    {
                        m_SObject.SetValueByType(EVMType.Boolean, sval.int8Value==1);
                    }
                    break;
                case EVMType.Byte:
                    {
                        m_SObject.SetValueByType(EVMType.Byte, sval.int8Value);
                    }
                    break;
                case EVMType.SByte:
                    {
                        m_SObject.SetValueByType(EVMType.SByte, sval.sint8Value);
                    }
                    break;
                case EVMType.Int16:
                    {
                        m_SObject.SetValueByType(EVMType.Int16, sval.int16Value);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        m_SObject.SetValueByType(EVMType.UInt16, sval.uint16Value);
                    }
                    break;
                case EVMType.Int32:
                    {
                        m_SObject.SetValueByType(EVMType.Int32, sval.int32Value);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int32Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.uint32Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.UInt32, sval.uint32Value);
                        }
                    }
                    break;
                case EVMType.Int64:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int64Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.int64Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Int64, sval.int64Value);
                        }
                    }
                    break;
                case EVMType.UInt64:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Int64Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.uint64Value);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.UInt64, sval.uint64Value);
                        }
                    }
                    break;
                case EVMType.Float32:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Float32Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.floatValue);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Float32, sval.floatValue);
                        }
                    }
                    break;
                case EVMType.Float64:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as Float64Object;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.doubleValue);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.Float64, sval.doubleValue);
                        }
                    }
                    break;
                case EVMType.String:
                    {
                        if (m_SObject == null)
                        {
                            var byteObj = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType) as StringObject;
                            m_SObject = byteObj;
                            byteObj.SetValue(sval.stringValue);
                        }
                        else
                        {
                            m_SObject.SetValueByType(EVMType.String, sval.stringValue);
                        }
                    }
                    break;
                case EVMType.Class:
                case EVMType.Array:
                    {
                        m_SObject = sval.sobject;
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
            WriteCurrentValueToMemberData();
        }

        public void SetSValueBySObjct(ref SValue svalue)
        {
            if (m_RuntimeType != null
                && IsMemberDataDirectType(m_RuntimeType.eType)
                && TryReadMemberDataToSValue(ref svalue))
            {
                return;
            }

            if ( m_SObject == null )
            {
                svalue.SetNull();
                return;
            }
            if( eType == EVMType.Object )
            {
                svalue.SetSObject(m_SObject);
                return;
            }
            switch (m_SObject)
            {
                case BoolObject bo:
                    {
                        svalue.SetBoolValue(bo.value);
                    }
                    break;
                case Int8Object byteob:
                    {
                        svalue.SetInt8Value(byteob.value);
                    }
                    break;
                case SInt8Object sbyteobj:
                    {
                        svalue.SetSInt8Value(sbyteobj.value);
                    }
                    break;
                case Int16Object int16Obj:
                    {
                        svalue.SetInt16Value(int16Obj.value);
                    }
                    break;
                case UInt16Object uint16Obj:
                    {
                        svalue.SetUInt16Value(uint16Obj.value);
                    }
                    break;
                case Int32Object int32Obj:
                    {
                        svalue.SetInt32Value(int32Obj.value);
                    }
                    break;
                case UInt32Object uint32Obj:
                    {
                        svalue.SetUInt32Value(uint32Obj.value);
                    }
                    break;
                case Int64Object int64Obj:
                    {
                        svalue.SetInt64Value(int64Obj.value);
                    }
                    break;
                case UInt64Object uint64Obj:
                    {
                        svalue.SetUInt64Value(uint64Obj.value);
                    }
                    break;
                case Float32Object floatobj:
                    {
                        svalue.SetFloatValue(floatobj.value);
                    }
                    break;
                case Float64Object doubleobj:
                    {
                        svalue.SetDoubleValue(doubleobj.value);
                    }
                    break;
                case StringObject stringObj:
                    {
                        svalue.SetStringValue(stringObj.value);
                    }
                    break;
                case ClassObject classObj:
                    {
                        svalue.SetSObject(classObj);
                    }
                    break;
                case TemplateObject templateObj:
                    {
                        svalue.SetSObject(templateObj.instnceObject);
                        Debug.Assert(false);
                    }
                    break;
                default:
                    {
                        Debug.Assert(false);
                    }
                    break;
            }
        }
        public SObject CreateObjectByRuntimeType()
        {
            if (m_SObject == null)
            {
                m_SObject = ObjectManager.CreateObjectByRuntimeType(m_RuntimeType, true);
            }
            return m_SObject;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if( m_RuntimeType != null )
            {
                sb.Append(m_RuntimeType.ToString());
            }
            if( m_SObject != null )
            {
                sb.Append(m_SObject.ToString());
            }

            return sb.ToString();
        }
    }
}
