//****************************************************************************
//  File:      MemberDataLayout.cs
//  Description: 类实例 / 类型静态成员在 ClassObject.m_MemberData 与 RuntimeType.m_MemberData
//               中的紧凑布局：标量按原生宽度，其余类型固定 4 字节存对象指针 Id。
//****************************************************************************

using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    internal static class MemberDataLayout
    {
        /// <summary>
        /// 单一成员在 m_MemberData 中占用的字节数。
        /// </summary>
        public static int GetSlotByteLength(RuntimeType? rt)
        {
            if (rt == null)
                return sizeof(int);

            return rt.eType switch
            {
                EVMType.UInt8 or EVMType.Int8 or EVMType.Boolean => 1,
                EVMType.Int16 or EVMType.UInt16 => 2,
                EVMType.Int32 or EVMType.UInt32 or EVMType.Float32 => 4,
                EVMType.Int64 or EVMType.UInt64 or EVMType.Float64 => 8,
                _ => sizeof(int),
            };
        }
    }
}
