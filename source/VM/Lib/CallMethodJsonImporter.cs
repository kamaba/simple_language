using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleLanguage.VM.Lib
{
    public static class CallMethodJsonImporter
    {
        public static bool Import(string path, out List<CallMethodJsonExporter.CallMethodModel> modelList)
        {
            modelList = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return false;
                if (!File.Exists(path)) return false;

                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter());

                modelList = JsonSerializer.Deserialize<List<CallMethodJsonExporter.CallMethodModel>>(json, options) ?? new();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
