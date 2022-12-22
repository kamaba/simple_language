using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
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
                Console.WriteLine("命名空间名称不能为空字符");
                return false;
            }
            if (nsArr.Length == 1)
            {
                bool isSuc = GrammerUtil.IdentifierCheck(nsArr[0]);
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
                if (!GrammerUtil.IdentifierCheck(nsArr[i]))
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
                case EPermission.Public: return "public";
                case EPermission.Internal: return "internal";
                case EPermission.Protected: return "protected";
                case EPermission.Private: return "private";
            }
            return "_public";
        }
        public static EPermission GetPerMissionByString( string str )
        {
            switch (str)
            {
                case "public": return EPermission.Public;
                case "internal": return EPermission.Internal;
                case "protected": return EPermission.Protected;
                case "private": return EPermission.Private;
                default:return EPermission.Null;
            }
        }
    }
}
