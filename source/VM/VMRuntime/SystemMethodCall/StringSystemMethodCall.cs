using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SimpleLanuageVM.Load;

namespace SimpleLanguage.VM.Runtime
{
    internal static class StringSystemMethodCall
    {
        public static void ExecuteStringConvert(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemConvertString stack underflow, need={pc}");
                return;
            }
            var outv = SystemMethodConvertHelper.ConvertValue(ref args[0], ESystemMethodCall.SystemConvertString);
            vm.PushSValueSynced(outv);
        }

        /// <summary>
        /// String format helper for SL:
        /// - supports {} (auto-index) and {0}/{1} (explicit index)
        /// - if params array is passed as a single <see cref="ArrayObject"/>, expands it
        /// - if args are passed individually, uses all args after format string
        /// </summary>
        public static void ExecuteStringFormat(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemStringFormat stack underflow, need={pc}");
                return;
            }

            string format = ToInvariantString(ref args[0]);
            var parts = new List<string>();

            if (pc == 2 && args[1].sobject is ArrayObject arrObj)
            {
                int len = arrObj.length;
                for (int i = 0; i < len; i++)
                    parts.Add(CoerceArrayItemToString(arrObj.GetValue(i)));
            }
            else
            {
                for (int i = 1; i < pc; i++)
                    parts.Add(ToInvariantString(ref args[i]));
            }

            string result = FormatLikeLegacy(format, parts);
            var outv = default(SValue);
            outv.SetStringValue(result);
            vm.PushSValueSynced(outv);
        }

        private static string ToInvariantString(ref SValue v)
        {
            var sv = SystemMethodConvertHelper.ConvertValue(ref v, ESystemMethodCall.SystemConvertString);
            return sv.stringValue ?? sv.GetValueObject()?.ToString() ?? string.Empty;
        }

        private static string CoerceArrayItemToString(object? value)
        {
            if (value == null) return string.Empty;
            if (value is SObject sobj) return sobj.value?.ToString() ?? string.Empty;
            if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
            return value.ToString() ?? string.Empty;
        }

        private static string FormatLikeLegacy(string format, List<string> args)
        {
            if (string.IsNullOrEmpty(format) || args.Count == 0) return format ?? string.Empty;

            int autoIndex = 0;
            int searchIndex = 0;
            var result = new StringBuilder(format.Length + 32);

            while (searchIndex < format.Length)
            {
                int braceStart = format.IndexOf('{', searchIndex);
                if (braceStart < 0)
                {
                    result.Append(format, searchIndex, format.Length - searchIndex);
                    break;
                }

                if (braceStart > searchIndex)
                    result.Append(format, searchIndex, braceStart - searchIndex);

                int braceEnd = format.IndexOf('}', braceStart + 1);
                if (braceEnd < 0)
                {
                    result.Append(format, braceStart, format.Length - braceStart);
                    break;
                }

                string placeholder = format.Substring(braceStart + 1, braceEnd - braceStart - 1);
                int argIndex;
                if (string.IsNullOrEmpty(placeholder))
                    argIndex = autoIndex++;
                else if (!int.TryParse(placeholder, NumberStyles.Integer, CultureInfo.InvariantCulture, out argIndex))
                {
                    result.Append('{').Append(placeholder).Append('}');
                    searchIndex = braceEnd + 1;
                    continue;
                }

                if (argIndex >= 0 && argIndex < args.Count)
                    result.Append(args[argIndex]);
                else
                    result.Append('{').Append(placeholder).Append('}');

                searchIndex = braceEnd + 1;
            }

            return result.ToString();
        }
    }
}
