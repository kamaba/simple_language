using System.Runtime.InteropServices;
using SimpleLanguage.VM.Runtime;
using System;

namespace SimpleLanguage.VM
{
    // Blittable raw numeric-only value for unsafe stacks
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RawSValue
    {
        public EVMType eType; // small, but keep for simplicity
        public ulong u64; // union storage

        // Accessors
        public byte Int8 { get => (byte)u64; set => u64 = (u64 & ~0xFFUL) | value; }
        public sbyte SInt8 { get => (sbyte)(u64 & 0xFF); set => u64 = (u64 & ~0xFFUL) | (ulong)(byte)value; }
        public short Int16 { get => (short)(u64 & 0xFFFF); set => u64 = (u64 & ~0xFFFFUL) | (ulong)(ushort)value; }
        public ushort UInt16 { get => (ushort)(u64 & 0xFFFF); set => u64 = (u64 & ~0xFFFFUL) | value; }
        public int Int32 { get => (int)(u64 & 0xFFFFFFFF); set => u64 = (u64 & ~0xFFFFFFFFUL) | (uint)value; }
        public uint UInt32 { get => (uint)(u64 & 0xFFFFFFFF); set => u64 = (u64 & ~0xFFFFFFFFUL) | value; }
        public long Int64 { get => (long)u64; set => u64 = (ulong)value; }
        public ulong UInt64 { get => u64; set => u64 = value; }
        public float Float32
        {
            get => BitConverter.Int32BitsToSingle((int)(u64 & 0xFFFFFFFF));
            set
            {
                uint bits = (uint)BitConverter.SingleToInt32Bits(value);
                u64 = (u64 & ~0xFFFFFFFFUL) | bits;
            }
        }
        public double Float64
        {
            get => BitConverter.Int64BitsToDouble((long)u64);
            set => u64 = (ulong)BitConverter.DoubleToInt64Bits(value);
        }

        public static RawSValue FromSValue(ref SValue v)
        {
            RawSValue r = new RawSValue();
            r.eType = v.eType;
            switch (v.eType)
            {
                case EVMType.Byte: r.Int8 = v.int8Value; break;
                case EVMType.SByte: r.SInt8 = v.sint8Value; break;
                case EVMType.Int16: r.Int16 = v.int16Value; break;
                case EVMType.UInt16: r.UInt16 = v.uint16Value; break;
                case EVMType.Int32: r.Int32 = v.int32Value; break;
                case EVMType.UInt32: r.UInt32 = v.uint32Value; break;
                case EVMType.Int64: r.Int64 = v.int64Value; break;
                case EVMType.UInt64: r.UInt64 = v.uint64Value; break;
                case EVMType.Float32: r.Float32 = v.floatValue; break;
                case EVMType.Float64: r.Float64 = v.doubleValue; break;
                default:
                    r.u64 = 0;
                    break;
            }
            return r;
        }
        public void ApplyToSValue(ref SValue v)
        {
            v.eType = eType;
            switch (eType)
            {
                case EVMType.Byte: v.int8Value = Int8; break;
                case EVMType.SByte: v.sint8Value = SInt8; break;
                case EVMType.Int16: v.int16Value = Int16; break;
                case EVMType.UInt16: v.uint16Value = UInt16; break;
                case EVMType.Int32: v.int32Value = Int32; break;
                case EVMType.UInt32: v.uint32Value = UInt32; break;
                case EVMType.Int64: v.int64Value = Int64; break;
                case EVMType.UInt64: v.uint64Value = UInt64; break;
                case EVMType.Float32: v.floatValue = Float32; break;
                case EVMType.Float64: v.doubleValue = Float64; break;
                default:
                    break;
            }
            v.isNull = false;
        }
    }
}
