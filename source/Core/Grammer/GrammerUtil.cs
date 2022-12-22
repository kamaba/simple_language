using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleLanguage.Compile.Grammer
{
    public class GrammerUtil
    {
        public static bool IdentifierCheck( string name )
        {
            if (name == null )
            {
                Console.WriteLine("自定义字符错误, 参数不可以为空!!");
                return false;
            }

            if ( name.Equals( string.Empty ) || name.Length == 0 )
            {
                Console.WriteLine("自定义字符错误, 不可以为空!!");
                return false;
            }
            string pattern = @"^[A-Za-z0-9_]+$";

            Match match = Regex.Match(name, pattern);

            if( !match.Success )
            {
                return false;
            }
            return true;
        }

    }
}
