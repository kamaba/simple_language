using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Reflection;
using System.Text.Json.Serialization; 

namespace SimpleLanguage.VM.Lib
{
    // Export registered call methods (from Front's RegisterCallMethodManager)
    // into a JSON file. The JSON schema follows the CallMethod structure.
    public static class CallMethodJsonExporter
    {
        public static bool Export(string path)
        {
            try
            {
                // enumerate static public methods in this assembly under namespace SimpleLanguage.VM.Lib
                var asm = Assembly.GetExecutingAssembly();
                var types = asm.GetTypes().Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith("SimpleLanguage.Lib"));
                var modelList = new List<CallMethodModel>();
                foreach (var t in types)
                {
                    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        // skip property methods and special names
                        if (m.IsSpecialName) continue;
                        var model = new CallMethodModel();
                        model.callMethodLanguage = RegisterCallMethodLanguage.CSharpLang;
                        model.namespaceNameList = (t.Namespace ?? string.Empty).Split('.').ToList();
                        model.topClassNameList = new List<string>();
                        model.className = t.Name;
                        model.methodName = m.Name;
                        // return type
                        model.returnType = new CallTypeModel { eType = m.ReturnType.FullName ?? m.ReturnType.Name, typeName = m.ReturnType.Name };
                        // parameters
                        foreach (var p in m.GetParameters())
                        {
                            model.argumentListType.Add(new CallTypeModel { eType = p.ParameterType.FullName ?? p.ParameterType.Name, typeName = p.ParameterType.Name });
                        }
                        modelList.Add(model);
                    }
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());
                var json = JsonSerializer.Serialize(modelList, options);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CallMethodJsonExporter.Export failed: " + ex.ToString());
                return false;
            }
        }

        // DTO classes for JSON output
        // Local model duplicating Front's structures so VM doesn't need compile-time dependency
        public enum RegisterCallMethodLanguage
        {
            None,
            CSharpLang,
            JavaLang,
            CLang,
            CPlusPlusLang
        }
        public class CallMethodModel
        {
            public RegisterCallMethodLanguage callMethodLanguage { get; set; }
            public List<string> namespaceNameList { get; set; } = new List<string>();
            public List<string> topClassNameList { get; set; } = new List<string>();
            public string className { get; set; }
            public string methodName { get; set; }
            public CallTypeModel returnType { get; set; }
            public List<CallTypeModel> argumentListType { get; set; } = new List<CallTypeModel>();
        }
        public class CallTypeModel
        {
            // store eType as string to avoid cross-assembly enum dependency
            public string eType { get; set; }
            public string typeName { get; set; }
        }
    }
}
