//****************************************************************************
//  File:      NumericUnion.cs
//  Description: �?RuntimeValue / SObject 共用的标量联合体（与 RuntimeValue 字段布局一致）�?
//****************************************************************************

using System.Runtime.InteropServices;

namespace SimpleLanguage.VM
{
    [StructLayout(LayoutKind.Explicit)]
    public struct NumericUnion
    {
        [FieldOffset(0)] public long i64;
        [FieldOffset(0)] public ulong u64;
        [FieldOffset(0)] public double f64;
        [FieldOffset(0)] public float f32;
        [FieldOffset(0)] public int i32;
        [FieldOffset(0)] public uint u32;
        [FieldOffset(0)] public short i16;
        [FieldOffset(0)] public ushort u16;
        [FieldOffset(0)] public byte u8;
        [FieldOffset(0)] public sbyte i8;
    }
}
