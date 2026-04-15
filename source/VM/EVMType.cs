namespace SimpleLanguage.VM.Runtime
{
    public enum EVMType : byte
    {
        // primitive & special types used by the VM
        Null = 0,
        Void = 1,
        Boolean = 2,
        UInt8 = 3,
        Int8 = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,
        Int64 = 9,
        UInt64 = 10,
        Float32 = 11,
        Float64 = 12,
        Num = 13,
        String = 14,
        Object = 15,
        Class = 16,
        Type = 17,
        Array,
        Member,
    }
}
