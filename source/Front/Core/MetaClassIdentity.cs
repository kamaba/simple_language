//****************************************************************************
//  File:      MetaClassIdentity.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/08/03 12:00:00
//  Description: 类身份的确定型 Int32 哈希规则。
//              规则：以类的全名（moduleName.namespaceName.className.childClassName，
//              即 MetaBase.allName，通过 MetaNode.GetAllName() 构建）
//              计算 FNV-1a 32-bit 哈希，跨会话稳定。
//              这样导出端写入包的 classId / baseClassId / interfaceId / relatedClassId
//              与导入端按 allName 计算的 id 完全一致，不再依赖 Object.GetHashCode()
//              （引用身份，跨会话不稳定）。
//****************************************************************************

namespace SimpleLanguage.Core
{
    public static class MetaClassIdentity
    {
        /// <summary>
        /// 按类全名计算确定型 classId。
        /// null/空串返回 0（表示“无类”哨兵，用于 baseClassId=0 等）。
        /// 非空串返回 FNV-1a 32-bit；若哈希结果恰为 0，回退为 1 以避开哨兵。
        /// </summary>
        public static int GetClassId(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            uint hash = 2166136261u;
            for (int i = 0; i < fullName.Length; i++)
            {
                hash ^= fullName[i];
                hash *= 16777619u;
            }
            int id = (int)hash;
            return id == 0 ? 1 : id;
        }
    }
}
