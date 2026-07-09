//****************************************************************************
//  File:      VMObjectHeader.cs
//  Description: 64-bit packed object header (struct layer with property accessors).
//               Mirrors cvm's union _VMObjectHeader in vm_object.h.
//
//  Bit layout (64 bits, MSB -> LSB):
//    [63:58] spare       (6 bits)  reserved
//    [57:56] gc_color    (2 bits)  tri-color GC mark: 0=white 1=gray 2=black
//    [55:50] etype       (6 bits)  EType enum (0–22, room for 64)
//    [49:46] meta_kind   (4 bits)  0=regular 1=enum 2=data 3=type_object
//    [45:32] refcount    (14 bits) reference count (0–16383)
//    [31: 0] hash        (32 bits) identity hash code (0 = not yet computed)
//****************************************************************************

using System.Runtime.InteropServices;

namespace SimpleLanguage.VM
{
    /// <summary>
    /// Packed 64-bit object header. C# uses a struct with property accessors
    /// (cvm uses a union + bitfield for the same layout).
    /// </summary>
    public struct VMObjectHeader
    {
        // --- meta_kind values (replaces old vtable field) ---
        public const byte MetaKindRegular    = 0;  // ordinary class instance
        public const byte MetaKindEnum       = 1;  // enum meta-object
        public const byte MetaKindData       = 2;  // data meta-object
        public const byte MetaKindTypeObject = 3;  // TypeObject

        // --- GC tri-color values ---
        public const byte GcWhite = 0;
        public const byte GcGray  = 1;
        public const byte GcBlack = 2;

        // --- Bit masks and shifts ---
        private const ulong HashMask      = 0xFFFFFFFFUL;        // [31: 0] 32 bits
        private const int   RefcountShift = 32;
        private const ulong RefcountMask  = 0x3FFFUL;            // [45:32] 14 bits
        private const int   MetaKindShift = 46;
        private const ulong MetaKindMask  = 0x0FUL;              // [49:46] 4 bits
        private const int   ETypeShift    = 50;
        private const ulong ETypeMask     = 0x3FUL;              // [55:50] 6 bits
        private const int   GcColorShift  = 56;
        private const ulong GcColorMask   = 0x03UL;              // [57:56] 2 bits

        // --- Raw 64-bit access ---
        private ulong _raw;

        /// <summary>Full 64-bit raw value.</summary>
        public readonly ulong Raw => _raw;

        /// <summary>Identity hash code (0 = not yet computed).</summary>
        public uint Hash
        {
            readonly get => (uint)(_raw & HashMask);
            set => _raw = (_raw & ~HashMask) | value;
        }

        /// <summary>Reference count (0–16383).</summary>
        public ushort RefCount
        {
            readonly get => (ushort)((_raw >> RefcountShift) & RefcountMask);
            set => _raw = (_raw & ~(RefcountMask << RefcountShift)) | ((ulong)(value & (ushort)RefcountMask) << RefcountShift);
        }

        /// <summary>Meta-kind: 0=regular, 1=enum, 2=data, 3=type_object.</summary>
        public byte MetaKind
        {
            readonly get => (byte)((_raw >> MetaKindShift) & MetaKindMask);
            set => _raw = (_raw & ~(MetaKindMask << MetaKindShift)) | ((ulong)(value & (byte)MetaKindMask) << MetaKindShift);
        }

        /// <summary>EType enum value (6 bits).</summary>
        public byte EType
        {
            readonly get => (byte)((_raw >> ETypeShift) & ETypeMask);
            set => _raw = (_raw & ~(ETypeMask << ETypeShift)) | ((ulong)(value & (byte)ETypeMask) << ETypeShift);
        }

        /// <summary>GC tri-color mark: 0=white, 1=gray, 2=black.</summary>
        public byte GcColor
        {
            readonly get => (byte)((_raw >> GcColorShift) & GcColorMask);
            set => _raw = (_raw & ~(GcColorMask << GcColorShift)) | ((ulong)(value & (byte)GcColorMask) << GcColorShift);
        }

        /// <summary>Build a fresh header (hash defaults to 0 = not-yet-computed).</summary>
        public static VMObjectHeader Make(byte etype, byte metaKind, ushort refCount)
        {
            var h = default(VMObjectHeader);
            h.EType    = etype;
            h.MetaKind = metaKind;
            h.RefCount = refCount;
            return h;
        }

        /// <summary>Reset to zero (all fields cleared).</summary>
        public void Clear() => _raw = 0;
    }
}
