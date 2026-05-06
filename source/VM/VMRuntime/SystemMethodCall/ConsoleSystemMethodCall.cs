using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using SimpleLanuageVM.Load;
using SimpleLanguage.VM;

namespace SimpleLanguage.VM.Runtime
{
    internal static class ConsoleSystemMethodCall
    {
        public static void ExecuteSystemPrint(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int paramCount = sysPkg.paramCount;
            if (paramCount <= 0)
            {
                Console.Write(string.Empty);
                VmRunResultSink.MirrorConsole(string.Empty, newLine: false);
                return;
            }
            if (!vm.TrySystemCallPopArgs(paramCount, out var args))
            {
                Debug.Assert(false, $"SystemPrint stack underflow, need={paramCount}");
                return;
            }

            var text = FormatConsoleValue(ref args[0]);
            Console.Write(text);
            VmRunResultSink.MirrorConsole(text, newLine: false);
        }

        public static void ExecuteSystemPrintln(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int paramCount = sysPkg.paramCount;
            if (paramCount <= 0)
            {
                Console.WriteLine();
                VmRunResultSink.MirrorConsole(null, newLine: true);
                return;
            }
            if (!vm.TrySystemCallPopArgs(paramCount, out var args))
            {
                Debug.Assert(false, $"SystemPrintln stack underflow, need={paramCount}");
                return;
            }

            var text = FormatConsoleValue(ref args[0]);
            Console.WriteLine(text);
            VmRunResultSink.MirrorConsole(text, newLine: true);
        }

        private static string FormatConsoleValue(ref SValue value)
        {
            if (value.isNull)
                return string.Empty;

            if (value.sobject is TypeObject typeObject && typeObject.currentRT?.runtimeClass?.metaClassKind == 2)
            {
                return FormatDataRuntimeType(typeObject.currentRT, new HashSet<int>());
            }

            if (value.sobject is ClassObject dataObject && dataObject.runtimeClass?.metaClassKind == 2)
            {
                return FormatDataObject(dataObject, new HashSet<int>());
            }

            var textObj = value.GetValueObject();
            return textObj?.ToString() ?? string.Empty;
        }

        private static string FormatDataObject(ClassObject dataObject, HashSet<int> visitPath)
        {
            if (!visitPath.Add(dataObject.id))
                return QuoteJsonString("<cycle>");

            try
            {
                var runtimeClass = dataObject.runtimeClass;
                var fieldList = runtimeClass?.nonStaticIRMetaVariableList;
                if (fieldList == null || fieldList.Count == 0)
                    return "{}";

                var sb = new StringBuilder();
                sb.Append('{');
                for (int i = 0; i < fieldList.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var field = fieldList[i];
                    var memberValue = default(SValue);
                    dataObject.GetMemberVariableSValue(i, ref memberValue);

                    sb.Append('"');
                    sb.Append(EscapeJsonString(field?.name ?? string.Empty));
                    sb.Append("\": ");
                    sb.Append(FormatNestedValue(ref memberValue, visitPath));
                }
                sb.Append('}');
                return sb.ToString();
            }
            finally
            {
                visitPath.Remove(dataObject.id);
            }
        }

        private static string FormatDataRuntimeType(RuntimeType runtimeType, HashSet<int> visitPath)
        {
            int visitKey = GetRuntimeTypeVisitKey(runtimeType);
            if (!visitPath.Add(visitKey))
                return QuoteJsonString("<cycle>");

            try
            {
                runtimeType.EnsureStaticMemberObjectsInitialized();

                var runtimeClass = runtimeType.runtimeClass;
                var fieldList = runtimeClass?.staticIRMetaVariableList;
                if (fieldList == null || fieldList.Count == 0)
                    return "{}";

                var sb = new StringBuilder();
                sb.Append('{');
                for (int i = 0; i < fieldList.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var field = fieldList[i];
                    var memberValue = default(SValue);
                    runtimeType.GetStaticMemberVariableSValue(i, ref memberValue);

                    sb.Append('"');
                    sb.Append(EscapeJsonString(field?.name ?? string.Empty));
                    sb.Append("\": ");
                    sb.Append(FormatNestedValue(ref memberValue, visitPath));
                }
                sb.Append('}');
                return sb.ToString();
            }
            finally
            {
                visitPath.Remove(visitKey);
            }
        }

        private static string FormatArrayObject(ArrayObject arrayObject, HashSet<int> visitPath)
        {
            if (!visitPath.Add(arrayObject.id))
                return QuoteJsonString("<cycle>");

            try
            {
                var sb = new StringBuilder();
                sb.Append('[');
                for (int i = 0; i < arrayObject.length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    var itemValue = default(SValue);
                    arrayObject.LoadValue(i, ref itemValue);
                    sb.Append(FormatNestedValue(ref itemValue, visitPath));
                }
                sb.Append(']');
                return sb.ToString();
            }
            finally
            {
                visitPath.Remove(arrayObject.id);
            }
        }

        private static string FormatNestedValue(ref SValue value, HashSet<int> visitPath)
        {
            if (value.isNull)
                return "null";

            if (value.sobject is ClassObject dataObject && dataObject.runtimeClass?.metaClassKind == 2)
                return FormatDataObject(dataObject, visitPath);

            if (value.sobject is TypeObject typeObject && typeObject.currentRT?.runtimeClass?.metaClassKind == 2)
                return FormatDataRuntimeType(typeObject.currentRT, visitPath);

            if (value.sobject is ArrayObject arrayObject)
                return FormatArrayObject(arrayObject, visitPath);

            switch (value.eType)
            {
                case EVMType.String:
                    return QuoteJsonString(value.stringValue ?? string.Empty);
                case EVMType.Boolean:
                    return value.uint8Value == 1 ? "true" : "false";
                case EVMType.UInt8:
                case EVMType.Int8:
                case EVMType.Int16:
                case EVMType.UInt16:
                case EVMType.Int32:
                case EVMType.UInt32:
                case EVMType.Int64:
                case EVMType.UInt64:
                case EVMType.Float32:
                case EVMType.Float64:
                case EVMType.Num:
                    return Convert.ToString(value.GetValueObject(), CultureInfo.InvariantCulture) ?? "null";
                default:
                    var raw = value.GetValueObject();
                    return raw?.ToString() ?? "null";
            }
        }

        private static string QuoteJsonString(string text)
        {
            return "\"" + EscapeJsonString(text) + "\"";
        }

        private static string EscapeJsonString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var sb = new StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(text[i]); break;
                }
            }
            return sb.ToString();
        }

        private static int GetRuntimeTypeVisitKey(RuntimeType runtimeType)
        {
            return int.MinValue + runtimeType.id;
        }

        public static void ExecuteSystemReadLine(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopDiscard(pc))
            {
                Debug.Assert(false, $"SystemReadLine stack underflow, need={pc}");
                return;
            }
            string line = Console.ReadLine() ?? string.Empty;
            var sv = default(SValue);
            sv.SetStringValue(line);
            vm.PushSValueSynced(sv);
        }

        public static void ExecuteSystemReadKey(RuntimeVM vm, SLSystemMethodCallPackage sysPkg)
        {
            int pc = sysPkg.paramCount;
            if (!vm.TrySystemCallPopDiscard(pc))
            {
                Debug.Assert(false, $"SystemReadKey stack underflow, need={pc}");
                return;
            }
            var k = Console.ReadKey(intercept: true);
            var svk = default(SValue);
            svk.SetStringValue(k.KeyChar.ToString());
            vm.PushSValueSynced(svk);
        }
    }
}
