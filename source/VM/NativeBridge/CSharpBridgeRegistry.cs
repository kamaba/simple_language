using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleLanguage.VM.Runtime
{
    internal static class CSharpBridgeRegistry
    {
        private static readonly Dictionary<string, CallMethodModel> s_MethodMap = new(StringComparer.Ordinal);

        public static void LoadFromJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException(path);

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            options.Converters.Add(new JsonStringEnumConverter());

            var list = JsonSerializer.Deserialize<List<CallMethodModel>>(json, options) ?? new();
            s_MethodMap.Clear();
            foreach (var m in list)
            {
                var id = m?.GetMethodId();
                if (string.IsNullOrEmpty(id)) continue;
                s_MethodMap[id] = m;
            }
        }

        public static bool TryResolve(string id, out CallMethodModel model)
        {
            return s_MethodMap.TryGetValue(id, out model!);
        }

        public static MethodInfo? ResolveMethod(CallMethodModel model)
        {
            if (model == null) return null;

            var ns = model.namespaceNameList != null && model.namespaceNameList.Count > 0
                ? string.Join(".", model.namespaceNameList)
                : string.Empty;
            var typeFullName = string.IsNullOrEmpty(ns) ? model.className : ns + "." + model.className;

            var t = Type.GetType(typeFullName, throwOnError: false);
            if (t == null)
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = a.GetType(typeFullName, throwOnError: false);
                    if (t != null) break;
                }
            }
            if (t == null) return null;

            var argTypes = (model.argumentListType ?? new List<CallTypeModel>())
                .Select(a => ResolveClrType(a?.eType, a?.typeName))
                .ToArray();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            return t.GetMethod(model.methodName, flags, binder: null, types: argTypes, modifiers: null);
        }

        private static Type ResolveClrType(string? fullName, string? shortName)
        {
            var n = fullName;
            if (string.IsNullOrWhiteSpace(n)) n = shortName;
            if (string.IsNullOrWhiteSpace(n)) return typeof(object);

            return n switch
            {
                "System.Void" or "Void" => typeof(void),
                "System.Boolean" or "Boolean" => typeof(bool),
                "System.Byte" or "Byte" => typeof(byte),
                "System.SByte" or "SByte" => typeof(sbyte),
                "System.Int16" or "Int16" => typeof(short),
                "System.UInt16" or "UInt16" => typeof(ushort),
                "System.Int32" or "Int32" => typeof(int),
                "System.UInt32" or "UInt32" => typeof(uint),
                "System.Int64" or "Int64" => typeof(long),
                "System.UInt64" or "UInt64" => typeof(ulong),
                "System.Single" or "Single" => typeof(float),
                "System.Double" or "Double" => typeof(double),
                "System.String" or "String" => typeof(string),
                _ => Type.GetType(n, throwOnError: false) ?? typeof(object),
            };
        }

        internal enum RegisterCallMethodLanguage
        {
            None,
            CSharpLang,
            JavaLang,
            CLang,
            CPlusPlusLang
        }

        internal sealed class CallMethodModel
        {
            public RegisterCallMethodLanguage callMethodLanguage { get; set; }
            public List<string> namespaceNameList { get; set; } = new();
            public List<string> topClassNameList { get; set; } = new();
            public string className { get; set; } = string.Empty;
            public string methodName { get; set; } = string.Empty;
            public CallTypeModel returnType { get; set; } = new();
            public List<CallTypeModel> argumentListType { get; set; } = new();

            public string GetMethodId()
            {
                var ns = namespaceNameList != null && namespaceNameList.Count > 0
                    ? string.Join(".", namespaceNameList)
                    : string.Empty;
                var typeFullName = string.IsNullOrEmpty(ns) ? className : ns + "." + className;
                return typeFullName + "." + methodName;
            }
        }

        internal sealed class CallTypeModel
        {
            public string eType { get; set; } = string.Empty;
            public string typeName { get; set; } = string.Empty;
        }
    }
}
