using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Compile 
{
    public static class CompilerUtil
    {
        public static StringBuilder tempBuild = new StringBuilder();

        public static bool CheckNameList(string ns, List<string> list = null)
        {
            var nsArr = ns.Split('.');
            if (nsArr.Length == 0)
            {
                Debug.Write("�����ռ����Ʋ���Ϊ���ַ�");
                return false;
            }
            if (nsArr.Length == 1)
            {
                bool isSuc = FileMetatUtil.IdentifierCheck(nsArr[0]);
                if (isSuc && list != null)
                {
                    list.Add(nsArr[0]);
                }
                return isSuc;
            }
            bool success = true;
            for (int i = 0; i < nsArr.Length; i++)
            {
                if (nsArr[i] == null)
                {
                    success = false;
                    break;
                }
                if (!FileMetatUtil.IdentifierCheck(nsArr[i]))
                {
                    success = false;
                    break;
                }
                list?.Add(nsArr[i]);
            }
            return success;
        }
        public static string ToFormatString( this EPermission permission )
        {
            switch( permission )
            {
                // export 关键字已移除：类级显式导出标记由 extern 接替（extern class → EPermission.Export）
                case EPermission.Export: return "extern";
                case EPermission.Public: return "public";
                case EPermission.Protected: return "protected";
                case EPermission.Private: return "private";
            }
            return "_public";
        }
        public static EPermission GetPerMissionByType( ETokenType type )
        {
            switch (type)
            {
                // ETokenType.Export 映射已删除：export 关键字不再作为权限标记。
                // 注意：ETokenType.Extern 不能映射到公共权限表——成员级 extern 是 FFI 外部实现语义，
                // 类级 extern 的导出标记在 MetaClass.BindFileMetaClass 处特判为 EPermission.Export。
                case ETokenType.Public: return EPermission.Public;
                case ETokenType.Projected: return EPermission.Protected;
                case ETokenType.Private: return EPermission.Private;
                default:return EPermission.Null;
            }
        }
    }
}
