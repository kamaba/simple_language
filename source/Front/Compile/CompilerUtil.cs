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
                Debug.Write("命名空间名称不能为空字符");
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
                case EPermission.Export: return "export";
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
                case ETokenType.Export: return EPermission.Export;
                case ETokenType.Public: return EPermission.Public;
                case ETokenType.Projected: return EPermission.Protected;
                case ETokenType.Private: return EPermission.Private;
                default:return EPermission.Null;
            }
        }
    }
}
