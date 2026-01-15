//****************************************************************************
//  File:      Array.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM;

namespace SimpleLanguage.Lib
{
    public class StringObjectData : BaseObjectData
    {

    }
    public static class StringClass
    {
        public static string Int32ToString( int d )
        {
            return d.ToString();
        }
        public static string StringFormat( string _format, ArrayObject ao )
        {
            object[] obj = ao.array as object[];

            if (string.IsNullOrEmpty(_format) || obj == null || obj.Length == 0)
            {
                return _format ?? string.Empty;
            }

            // Simple implementation: replace each occurrence of "{}" with the next argument's ToString().
            // If there are more placeholders than args, keep them as-is.
            int argIndex = 0;
            int searchIndex = 0;
            var result = new System.Text.StringBuilder(_format.Length + 32);

            while (searchIndex < _format.Length)
            {
                int braceIndex = _format.IndexOf("{}", searchIndex, System.StringComparison.Ordinal);
                if (braceIndex < 0 || argIndex >= obj.Length)
                {
                    // no more placeholders or no more args: append the rest and stop
                    result.Append(_format, searchIndex, _format.Length - searchIndex);
                    break;
                }

                // append text before placeholder
                if (braceIndex > searchIndex)
                {
                    result.Append(_format, searchIndex, braceIndex - searchIndex);
                }

                // append formatted argument
                var value = obj[argIndex++];
                string str = "";
                if( value is SObject sobj )
                {
                    str = sobj.value.ToString();
                }
                result.Append(str);

                // move past "{}"
                searchIndex = braceIndex + 2;
            }

            return result.ToString();
        }
    }
}
