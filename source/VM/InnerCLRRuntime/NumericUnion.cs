//****************************************************************************
//  File:      NumericUnion.cs
//  Description: 与 SValue / SObject 共用的标量联合体（与 SValue 字段布局一致）。
//****************************************************************************

using System.Runtime.InteropServices;

namespace SimpleLanguage.VM
{
    [StructLayout(LayoutKind.Explicit)]
    public struct NumericUnion
    {
        [FieldOffset(0)] public long i64;
        [FieldOffset(0)] public ulong u64;
        [FieldOffset(0)] public double d;
        [FieldOffset(0)] public float f;
        [FieldOffset(0)] public int i32;
        [FieldOffset(0)] public uint u32;
        [FieldOffset(0)] public short i16;
        [FieldOffset(0)] public ushort ui16;
        [FieldOffset(0)] public byte i8;
        [FieldOffset(0)] public sbyte si8;
    }
}
