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

    public class EVMTypeUtils
    {
        public static int GetScalarUnitLength(EVMType t)
        {
            return t switch
            {
                EVMType.Boolean => 1,
                EVMType.UInt8 or EVMType.Int8 => 1,
                EVMType.Int16 or EVMType.UInt16 => 2,
                EVMType.Int32 or EVMType.UInt32 or EVMType.Float32 => 4,
                EVMType.Int64 or EVMType.UInt64 or EVMType.Float64 or EVMType.Num => 8,
                _ => 4,
            };
        }
    }
}
