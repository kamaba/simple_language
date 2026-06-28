using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM;

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
                {
                    parts.Add(CoerceArrayItemToString(arrObj.GetValue(i)));
                }
            }
            else
            {
                for (int i = 1; i < pc; i++)
                    parts.Add(ToInvariantString(ref args[i]));
            }

            string result = FormatLikeLegacy(format, parts);
            var outv = default(RuntimeValue);
            outv.SetStringValue(result);
            vm.PushSValueSynced(outv);
        }

        private static string ToInvariantString(ref RuntimeValue v)
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

        public static void ExecuteStringFront(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemStringFront stack underflow, need={pc}");
                return;
            }

            string s = ToInvariantString(ref args[0]);
            int n = ToInt32Arg(ref args[1]);
            int len = s?.Length ?? 0;
            if (len == 0 || n <= 0)
            {
                PushEmptyString(vm);
                return;
            }
            int take = n > len ? len : n;
            var outv = default(RuntimeValue);
            outv.SetStringValue(s.Substring(0, take));
            vm.PushSValueSynced(outv);
        }

        public static void ExecuteStringEnd(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 2 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemStringEnd stack underflow, need={pc}");
                return;
            }

            string s = ToInvariantString(ref args[0]);
            int n = ToInt32Arg(ref args[1]);
            int len = s?.Length ?? 0;
            if (len == 0 || n <= 0)
            {
                PushEmptyString(vm);
                return;
            }
            int take = n > len ? len : n;
            var outv = default(RuntimeValue);
            outv.SetStringValue(s.Substring(len - take, take));
            vm.PushSValueSynced(outv);
        }

        /// <summary>半开区间 [start, end)，与 C# <c>Substring(start, end - start)</c> 一致�?/summary>
        public static void ExecuteStringRange(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 3 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemStringRange stack underflow, need={pc}");
                return;
            }

            string s = ToInvariantString(ref args[0]);
            int start = ToInt32Arg(ref args[1]);
            int end = ToInt32Arg(ref args[2]);
            int len = s?.Length ?? 0;
            if (len == 0)
            {
                PushEmptyString(vm);
                return;
            }
            if (start < 0) start = 0;
            if (start > len) start = len;
            if (end < start) end = start;
            if (end > len) end = len;
            var outv = default(RuntimeValue);
            outv.SetStringValue(s.Substring(start, end - start));
            vm.PushSValueSynced(outv);
        }

        /// <summary>UTF-8 编码的字节序列，装入 <c>Array&lt;Byte&gt;</c>�?/summary>
        public static void ExecuteStringToByteArray(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (pc < 1 || !vm.TrySystemCallPopArgs(pc, out var args))
            {
                Debug.Assert(false, $"SystemStringToByteArray stack underflow, need={pc}");
                return;
            }

            string s = ToInvariantString(ref args[0]) ?? string.Empty;
            byte[] raw = Encoding.UTF8.GetBytes(s);

            RuntimeTypeManager.EnsureCoreRuntimeTypesRegistered();
            var byteRt = RuntimeTypeManager.uint8RuntimeType;
            if (byteRt == null)
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }

            RuntimeClass? arrayRc = RuntimeClassManager.GetRuntimeClassByName("Core.Array<T>")
                ?? RuntimeClassManager.GetRuntimeClassByName("Core.Array")
                ?? RuntimeClassManager.GetRuntimeClassByName("Array");
            if (arrayRc == null)
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }

            var arrRt = RuntimeTypeManager.GetRuntimeTypeByRuntimeClassAndRuntimeTypeList(arrayRc, new List<RuntimeType> { byteRt })
                ?? RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(arrayRc, new List<RuntimeType> { byteRt });
            if (arrRt == null)
            {
                var nz = default(RuntimeValue);
                nz.SetNull();
                vm.PushSValueSynced(nz);
                return;
            }

            var arr = new ArrayObject(arrRt, raw.Length);
            arr.CreateObject();
            ObjectManager.AddClassObject(arr);
            for (int i = 0; i < raw.Length; i++)
            {
                var sv = default(RuntimeValue);
                sv.SetUInt8Value(raw[i]);
                arr.StoreValue(i, sv);
            }

            var outv = default(RuntimeValue);
            outv.SetValueBySObject(arr);
            vm.PushSValueSynced(outv);
        }

        private static void PushEmptyString(RuntimeVM vm)
        {
            var outv = default(RuntimeValue);
            outv.SetStringValue(string.Empty);
            vm.PushSValueSynced(outv);
        }

        private static int ToInt32Arg(ref RuntimeValue v)
        {
            var iv = SystemMethodConvertHelper.ConvertValue(ref v, ESystemMethodCall.SystemConvertInt32);
            return iv.int32Value;
        }
    }
}
