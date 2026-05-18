using System;
using System.Text.Json;

namespace SimpleLanguage.Project
{
    public static class ProjectJsoncLoader
    {
        public static ProjectConfig FromJsonc(string jsoncText)
        {
            var cfg = new ProjectConfig();
            if (string.IsNullOrWhiteSpace(jsoncText))
            {
                return cfg;
            }

            using var doc = JsonDocument.Parse(jsoncText, new JsonDocumentOptions()
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            var root = doc.RootElement;

            if (TryGetObj(root, "project", out var project))
            {
                cfg.Project.Name = GetStr(project, "name", cfg.Project.Name);
                cfg.Project.Desc = GetStr(project, "desc", cfg.Project.Desc);
                cfg.Export.VersionMain = GetInt(project, "mainVersion", cfg.Export.VersionMain);
                cfg.Export.VersionSub = GetInt(project, "subVersion", cfg.Export.VersionSub);
                cfg.Export.VersionPatch = GetInt(project, "buildVersion", cfg.Export.VersionPatch);
            }

            if (TryGetObj(root, "source", out var source))
            {
                cfg.Source.Root = GetStr(source, "root", cfg.Source.Root);
                cfg.Source.EntryFile = GetStr(source, "entryFile", cfg.Source.EntryFile);
            }

            if (TryGetObj(root, "compile", out var compile))
            {
                cfg.Compile.Optimize = GetBool(compile, "optimize", cfg.Compile.Optimize);
                cfg.Compile.Target = GetStr(compile, "target", cfg.Compile.Target);
                cfg.Compile.Debug = GetBool(compile, "debug", cfg.Compile.Debug);
                cfg.Compile.IsUseForceSemiColonInLineEnd = GetBool(compile, "isUseForceSemiColonInLineEnd", cfg.Compile.IsUseForceSemiColonInLineEnd);
                cfg.Compile.IsForceUseKeyClass = GetBool(compile, "isForceUseClassKey", cfg.Compile.IsForceUseKeyClass);
                cfg.Compile.IsSupportDoublePlus = GetBool(compile, "isSupportDoublePlus", cfg.Compile.IsSupportDoublePlus);
                cfg.Compile.RequireSameNumericTypes = GetBool(compile, "requireSameNumericTypes", cfg.Compile.RequireSameNumericTypes);
            }

            if (TryGetObj(root, "compileFiles", out var compileFiles)
                && compileFiles.TryGetProperty("files", out var files)
                && files.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in files.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var f = new ProjectConfig.CompileFileItem();
                    f.Path = GetStr(item, "path", f.Path);
                    f.Group = GetStr(item, "group", f.Group);
                    f.Tag = GetStr(item, "tag", f.Tag);
                    f.Ignore = GetBool(item, "ignore", f.Ignore);
                    f.Priority = GetInt(item, "priority", f.Priority);
                    if (!string.IsNullOrWhiteSpace(f.Path))
                    {
                        cfg.CompileFiles.Files.Add(f);
                    }
                }

            }

            if (TryGetObj(root, "compileFilter", out var filter))
            {
                cfg.CompileFilter.IsAllGroup = GetBool(filter, "isAllGroup", cfg.CompileFilter.IsAllGroup);
                cfg.CompileFilter.IsAllTag = GetBool(filter, "isAllTag", cfg.CompileFilter.IsAllTag);

                if (filter.TryGetProperty("groups", out var groups) && groups.ValueKind == JsonValueKind.Array)
                {
                    foreach (var g in groups.EnumerateArray())
                    {
                        if (g.ValueKind == JsonValueKind.String)
                        {
                            cfg.CompileFilter.Groups.Add(g.GetString() ?? string.Empty);
                        }
                    }
                }
                if (filter.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in tags.EnumerateArray())
                    {
                        if (t.ValueKind == JsonValueKind.String)
                        {
                            cfg.CompileFilter.Tags.Add(t.GetString() ?? string.Empty);
                        }
                    }
                }

            }

            if (TryGetObj(root, "data", out var rootProjectData))
            {
                foreach (var kv in rootProjectData.EnumerateObject())
                {
                    cfg.JsoncProjectData[kv.Name] = kv.Value.Clone();
                }
            }

            if (TryGetObj(root, "global", out var global))
            {
                if (global.TryGetProperty("imports", out var imports) && imports.ValueKind == JsonValueKind.Array)
                {
                    foreach (var i in imports.EnumerateArray())
                    {
                        if (i.ValueKind == JsonValueKind.String)
                        {
                            cfg.Global.Imports.Add(i.GetString() ?? string.Empty);
                        }
                    }
                }

                if (TryGetObj(global, "data", out var dataObj))
                {
                    foreach (var kv in dataObj.EnumerateObject())
                    {
                        cfg.Global.Data[kv.Name] = kv.Value.Clone();
                    }
                }

                if (TryGetObj(global, "replace", out var replace))
                {
                    foreach (var kv in replace.EnumerateObject())
                    {
                        if (kv.Value.ValueKind == JsonValueKind.String)
                        {
                            cfg.Global.Replace[kv.Name] = kv.Value.GetString() ?? string.Empty;
                        }
                    }
                }
            }

            if (TryGetObj(root, "struct", out var structObj)
                && structObj.TryGetProperty("tree", out var tree)
                && tree.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in tree.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var ns = GetStr(item, "namespace", null);
                    if (!string.IsNullOrWhiteSpace(ns))
                    {
                        cfg.StructTree.EnsurePath(ns, ProjectConfig.StructTreeNode.NodeType.Namespace);
                    }

                    var cls = GetStr(item, "class", null);
                    if (!string.IsNullOrWhiteSpace(cls))
                    {
                        cfg.StructTree.EnsurePath(cls, ProjectConfig.StructTreeNode.NodeType.Class);
                    }
                }
            }

            if (TryGetObj(root, "export", out var exportObj))
            {
                cfg.Export.ModuleName = GetStr(exportObj, "moduleName", cfg.Export.ModuleName);
                cfg.Export.OutputDir = GetStr(exportObj, "outputDir", cfg.Export.OutputDir);
                cfg.Export.StringPoolAsBlob = GetBool(exportObj, "stringPoolAsBlob", cfg.Export.StringPoolAsBlob);
                cfg.Export.ExportPublicOnly = GetBool(exportObj, "exportPublicOnly", cfg.Export.ExportPublicOnly);
                cfg.Export.IncludeMetadata = GetBool(exportObj, "includeMetadata", cfg.Export.IncludeMetadata);

                cfg.Export.VersionMain = GetInt(exportObj, "versionMain", cfg.Export.VersionMain);
                cfg.Export.VersionSub = GetInt(exportObj, "versionSub", cfg.Export.VersionSub);
                cfg.Export.VersionPatch = GetInt(exportObj, "versionPatch", cfg.Export.VersionPatch);

                if (TryGetObj(exportObj, "debugText", out var debugTextObj))
                {
                    cfg.Export.DebugText.OutputDir = GetStr(debugTextObj, "outputDir", cfg.Export.DebugText.OutputDir);
                    cfg.Export.DebugText.Code = GetBool(debugTextObj, "code", cfg.Export.DebugText.Code);
                    cfg.Export.DebugText.Token = GetBool(debugTextObj, "token", cfg.Export.DebugText.Token);
                    cfg.Export.DebugText.Node = GetBool(debugTextObj, "node", cfg.Export.DebugText.Node);
                    cfg.Export.DebugText.File = GetBool(debugTextObj, "file", cfg.Export.DebugText.File);
                    cfg.Export.DebugText.Meta = GetBool(debugTextObj, "meta", cfg.Export.DebugText.Meta);
                    cfg.Export.DebugText.IR = GetBool(debugTextObj, "ir", cfg.Export.DebugText.IR);
                }
            }

            if (root.TryGetProperty("references", out var refs) && refs.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in refs.EnumerateArray())
                {
                    if (r.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }
                    var path = GetStr(r, "path", string.Empty);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        cfg.References.Add(new ProjectConfig.ReferenceSection() { Path = path });
                    }
                }
            }

            return cfg;
        }

        static bool TryGetObj(JsonElement root, string name, out JsonElement obj)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out obj) && obj.ValueKind == JsonValueKind.Object)
            {
                return true;
            }
            obj = default;
            return false;
        }

        static string GetStr(JsonElement obj, string name, string @default)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
            {
                return v.GetString() ?? @default;
            }
            return @default;
        }

        static bool GetBool(JsonElement obj, string name, bool @default)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False))
            {
                return v.GetBoolean();
            }
            return @default;
        }

        static int GetInt(JsonElement obj, string name, int @default)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number)
            {
                if (v.TryGetInt32(out var i))
                {
                    return i;
                }
            }
            return @default;
        }
    }
}
