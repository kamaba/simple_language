using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Reflection;

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
                // Try to access the RegisterCallMethodManager from front assembly via reflection
                var frontAssembly = Assembly.GetEntryAssembly();
                var cmType = frontAssembly?.GetType("SimpleLanguage.Lib.RegisterCallMethodManager");
                List<object> frontList = null;
                if (cmType != null)
                {
                    var field = cmType.GetField("callMethodList", BindingFlags.Public | BindingFlags.Static);
                    if (field != null)
                    {
                        var raw = field.GetValue(null) as System.Collections.IEnumerable;
                        if (raw != null)
                        {
                            frontList = new List<object>();
                            foreach (var o in raw) frontList.Add(o);
                        }
                    }
                }
                if (frontList == null)
                {
                    Console.WriteLine("CallMethodJsonExporter: cannot locate RegisterCallMethodManager.callMethodList");
                    return false;
                }
                var dtoList = new List<CallMethodDto>(frontList.Count);
                foreach (var cm in frontList)
                {
                    // reflect into the CallMethod instance
                    var dto = new CallMethodDto();
                    var lmField = cm.GetType().GetField("callMethodLanuage");
                    var nsField = cm.GetType().GetField("namespaceNameList");
                    var topField = cm.GetType().GetField("topClassNameList");
                    var clsField = cm.GetType().GetField("className");
                    var mthField = cm.GetType().GetField("methodName");
                    var retField = cm.GetType().GetField("returnType");
                    var argField = cm.GetType().GetField("argumentListType");

                    dto.callMethodLanguage = lmField?.GetValue(cm)?.ToString() ?? string.Empty;
                    dto.namespaceNameList = (nsField?.GetValue(cm) as IEnumerable<string>)?.ToArray() ?? Array.Empty<string>();
                    dto.topClassNameList = (topField?.GetValue(cm) as IEnumerable<string>)?.ToArray() ?? Array.Empty<string>();
                    dto.className = clsField?.GetValue(cm)?.ToString() ?? string.Empty;
                    dto.methodName = mthField?.GetValue(cm)?.ToString() ?? string.Empty;
                    var ret = retField?.GetValue(cm);
                    if (ret != null)
                    {
                        var et = ret.GetType().GetField("eType")?.GetValue(ret)?.ToString();
                        var tn = ret.GetType().GetField("typeName")?.GetValue(ret)?.ToString();
                        dto.returnType = new CallTypeDto { eType = et, typeName = tn };
                    }
                    var args = argField?.GetValue(cm) as System.Collections.IEnumerable;
                    if (args != null)
                    {
                        foreach (var a in args)
                        {
                            var et = a.GetType().GetField("eType")?.GetValue(a)?.ToString();
                            var tn = a.GetType().GetField("typeName")?.GetValue(a)?.ToString();
                            dto.argumentListType.Add(new CallTypeDto { eType = et, typeName = tn });
                        }
                    }
                    dtoList.Add(dto);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(dtoList, options);
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
        public class CallMethodDto
        {
            public string callMethodLanguage { get; set; }
            public string[] namespaceNameList { get; set; }
            public string[] topClassNameList { get; set; }
            public string className { get; set; }
            public string methodName { get; set; }
            public CallTypeDto returnType { get; set; }
            public List<CallTypeDto> argumentListType { get; set; } = new List<CallTypeDto>();
        }
        public class CallTypeDto
        {
            public string eType { get; set; }
            public string typeName { get; set; }
        }
    }
}
