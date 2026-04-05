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
    public class StringObjectData //: BaseObjectData
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

            int autoIndex = 0; // auto-incrementing index for {} placeholders
            int searchIndex = 0;
            var result = new System.Text.StringBuilder(_format.Length + 32);

            while (searchIndex < _format.Length)
            {
                int braceStart = _format.IndexOf('{', searchIndex);
                if (braceStart < 0)
                {
                    // no more placeholders, append the rest
                    result.Append(_format, searchIndex, _format.Length - searchIndex);
                    break;
                }

                // append text before placeholder
                if (braceStart > searchIndex)
                {
                    result.Append(_format, searchIndex, braceStart - searchIndex);
                }

                // find closing brace
                int braceEnd = _format.IndexOf('}', braceStart + 1);
                if (braceEnd < 0)
                {
                    // malformed: no closing brace, append the rest as-is
                    result.Append(_format, braceStart, _format.Length - braceStart);
                    break;
                }

                // extract content between braces
                string placeholder = _format.Substring(braceStart + 1, braceEnd - braceStart - 1);
                int argIndex;

                if (string.IsNullOrEmpty(placeholder))
                {
                    // {} -> use auto-incrementing index
                    argIndex = autoIndex++;
                }
                else if (int.TryParse(placeholder, out int parsedIndex))
                {
                    // {0}, {1}, etc. -> use explicit index
                    argIndex = parsedIndex;
                }
                else
                {
                    // unrecognized placeholder content, keep as literal
                    result.Append('{');
                    result.Append(placeholder);
                    result.Append('}');
                    searchIndex = braceEnd + 1;
                    continue;
                }

                // append the argument if index is valid
                if (argIndex >= 0 && argIndex < obj.Length)
                {
                    var value = obj[argIndex];
                    string str = "";
                    if (value is SObject sobj)
                    {
                        str = sobj.value?.ToString() ?? "";
                    }
                    else if (value != null)
                    {
                        str = value.ToString();
                    }
                    result.Append(str);
                }
                else
                {
                    // index out of range, keep placeholder as-is
                    result.Append('{');
                    result.Append(placeholder);
                    result.Append('}');
                }

                // move past this placeholder
                searchIndex = braceEnd + 1;
            }

            return result.ToString();
        }
    }
}
