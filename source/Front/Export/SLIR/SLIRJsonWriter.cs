using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export.SLIR
{
    public static class SLIRJsonWriter
    {
        public static void WriteModule(IRManager ir, string outputPath)
        {
            if (ir == null) throw new ArgumentNullException(nameof(ir));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            var model = BuildModel(ir);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            options.Converters.Add(new JsonStringEnumConverter());

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllText(outputPath, JsonSerializer.Serialize(model, options));
        }

        private static ModuleModel BuildModel(IRManager ir)
        {
            var m = new ModuleModel();

            foreach (var kv in ir.IRStringDict)
            {
                m.irStringDict.Add(new IRStringItem { id = kv.Key, value = kv.Value ?? string.Empty });
            }

            var classes = ir.GetIRMetaClassList();
            foreach (var c in classes)
            {
                if (c == null) continue;
                var cm = new ClassModel
                {
                    id = c.id,
                    name = c.irName ?? string.Empty,
                    sourcePath = c.sourcePath ?? string.Empty,
                };

                if (c.localIRMetaVariableList != null)
                {
                    foreach (var v in c.localIRMetaVariableList)
                    {
                        if (v == null) continue;
                        cm.fields.Add(new FieldModel
                        {
                            name = v.name ?? string.Empty,
                            type = v.irMetaType?.ToString() ?? string.Empty,
                            isStatic = false,
                            index = v.index,
                        });
                    }
                }

                if (c.staticIRMetaVariableList != null)
                {
                    foreach (var v in c.staticIRMetaVariableList)
                    {
                        if (v == null) continue;
                        cm.fields.Add(new FieldModel
                        {
                            name = v.name ?? string.Empty,
                            type = v.irMetaType?.ToString() ?? string.Empty,
                            isStatic = true,
                            index = v.index,
                        });
                    }
                }

                m.classes.Add(cm);
            }

            foreach (var kv in ir.IRMethodDict)
            {
                var meth = kv.Value;
                if (meth == null) continue;

                var mm = new MethodModel
                {
                    id = meth.id ?? string.Empty,
                    onlyName = meth.onlyFunctionName ?? string.Empty,
                    ownerClassId = meth.irOwnerMetaClass?.id ?? 0,
                    argumentCount = meth.methodArgumentList?.Count ?? 0,
                    localCount = meth.methodLocalVariableList?.Count ?? 0,
                    returnCount = meth.methodReturnVariableList?.Count ?? 0,
                };

                var code = meth.IRDataList;
                if (code != null)
                {
                    foreach (var ins in code)
                    {
                        if (ins == null) continue;
                        mm.instructions.Add(new InstructionModel
                        {
                            opCode = ins.opCode.ToString(),
                            index = ins.index,
                            offset = ins.offset,
                            payloadBase64 = ins.Payload != null && ins.Payload.Length > 0 ? Convert.ToBase64String(ins.Payload) : null,
                        });
                    }
                }

                m.methods.Add(mm);
            }

            return m;
        }

        private sealed class ModuleModel
        {
            public List<IRStringItem> irStringDict { get; set; } = new();
            public List<ClassModel> classes { get; set; } = new();
            public List<MethodModel> methods { get; set; } = new();
        }

        private sealed class IRStringItem
        {
            public int id { get; set; }
            public string value { get; set; } = string.Empty;
        }

        private sealed class ClassModel
        {
            public int id { get; set; }
            public string name { get; set; } = string.Empty;
            public string sourcePath { get; set; } = string.Empty;
            public List<FieldModel> fields { get; set; } = new();
        }

        private sealed class FieldModel
        {
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public bool isStatic { get; set; }
            public int index { get; set; }
        }

        private sealed class MethodModel
        {
            public string id { get; set; } = string.Empty;
            public string onlyName { get; set; } = string.Empty;
            public int ownerClassId { get; set; }
            public int argumentCount { get; set; }
            public int localCount { get; set; }
            public int returnCount { get; set; }
            public List<InstructionModel> instructions { get; set; } = new();
        }

        private sealed class InstructionModel
        {
            public string opCode { get; set; } = string.Empty;
            public int index { get; set; }
            public int offset { get; set; }
            public string? payloadBase64 { get; set; }
        }
    }
}
