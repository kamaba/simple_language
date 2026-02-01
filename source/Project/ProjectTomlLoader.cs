using Tomlyn.Model;

namespace SimpleLanguage.Project
{
    // Helper to map TomlTable into ProjectConfig
    public static class ProjectTomlLoader
    {
        public static ProjectConfig FromModel(TomlTable model)
        {
            var cfg = new ProjectConfig();
            if (model == null)
                return cfg;

            // [project]
            if (model.TryGetValue("project", out var projectObj) && projectObj is TomlTable projectTable)
            {
                if (projectTable.TryGetValue("name", out var nameObj) && nameObj is string name)
                    cfg.Project.Name = name;
                if (projectTable.TryGetValue("desc", out var descObj) && descObj is string desc)
                    cfg.Project.Desc = desc;
                if (projectTable.TryGetValue("mainVersion", out var mainVerObj) && mainVerObj is long mainVer)
                    cfg.Export.VersionMain = (int)mainVer;
                if (projectTable.TryGetValue("subVersion", out var subVerObj) && subVerObj is long subVer)
                    cfg.Export.VersionSub = (int)subVer;
                if (projectTable.TryGetValue("buildVersion", out var buildVerObj) && buildVerObj is long buildVer)
                    cfg.Export.VersionPatch = (int)buildVer;
                // buildSubVersion not mapped currently
            }

            // [source]
            if (model.TryGetValue("source", out var sourceObj) && sourceObj is TomlTable sourceTable)
            {
                if (sourceTable.TryGetValue("root", out var rootObj) && rootObj is string root)
                    cfg.Source.Root = root;
                if (sourceTable.TryGetValue("entryFile", out var entryObj) && entryObj is string entryFile)
                    cfg.Source.EntryFile = entryFile;
            }

            // [compile]
            if (model.TryGetValue("compile", out var compileObj) && compileObj is TomlTable compileTable)
            {
                if (compileTable.TryGetValue("optimize", out var optObj) && optObj is bool optimize)
                    cfg.Compile.Optimize = optimize;
                if (compileTable.TryGetValue("target", out var targetObj) && targetObj is string target)
                    cfg.Compile.Target = target;
                if (compileTable.TryGetValue("debug", out var debugObj) && debugObj is bool debug)
                    cfg.Compile.Debug = debug;
                if (compileTable.TryGetValue("isUseForceSemiColonInLineEnd", out var semiObj) && semiObj is bool semi)
                    cfg.Compile.IsUseForceSemiColonInLineEnd = semi;
                if (compileTable.TryGetValue("isForceUseClassKey", out var forceClassObj) && forceClassObj is bool forceClass)
                    cfg.Compile.IsForceUseKeyClass = forceClass;
                if (compileTable.TryGetValue("isSupportDoublePlus", out var doublePlusObj) && doublePlusObj is bool doublePlus)
                    cfg.Compile.IsSupportDoublePlus = doublePlus;
            }

            // [compileFiles] -> [[compileFiles.files]]
            if (model.TryGetValue("compileFiles", out var compileFilesObj) && compileFilesObj is TomlTable compileFilesTable)
            {
                // 注意这里是 TomlTableArray
                if (compileFilesTable.TryGetValue("files", out var filesObj) && filesObj is TomlTableArray filesArray)
                {
                    System.Diagnostics.Debug.WriteLine($"[TOML] compileFiles.files count = {filesArray.Count}");
                    foreach (var item in filesArray)
                    {
                        if (item is TomlTable fileTable)
                        {
                            var f = new ProjectConfig.CompileFileItem();
                            if (fileTable.TryGetValue("path", out var pathObj) && pathObj is string path)
                                f.Path = path;
                            if (fileTable.TryGetValue("group", out var groupObj) && groupObj is string group)
                                f.Group = group;
                            if (fileTable.TryGetValue("tag", out var tagObj) && tagObj is string tag)
                                f.Tag = tag;
                            if (fileTable.TryGetValue("ignore", out var ignoreObj) && ignoreObj is bool ignore)
                                f.Ignore = ignore;
                            if (fileTable.TryGetValue("priority", out var priObj) && priObj is long pri)
                                f.Priority = (int)pri;

                            if (!string.IsNullOrEmpty(f.Path))
                                cfg.CompileFiles.Files.Add(f);
                        }
                    }
                }
            }

            // [compileFilter]
            if (model.TryGetValue("compileFilter", out var filterObj) && filterObj is TomlTable filterTable)
            {
                if (filterTable.TryGetValue("groups", out var groupsObj) && groupsObj is TomlArray groupsArray)
                {
                    foreach (var g in groupsArray)
                    {
                        if (g is string gs)
                            cfg.CompileFilter.Groups.Add(gs);
                    }
                }
                if (filterTable.TryGetValue("tags", out var tagsObj) && tagsObj is TomlArray tagsArray)
                {
                    foreach (var t in tagsArray)
                    {
                        if (t is string ts)
                            cfg.CompileFilter.Tags.Add(ts);
                    }
                }
                if (filterTable.TryGetValue("isAllGroup", out var allGroupObj) && allGroupObj is bool allGroup)
                    cfg.CompileFilter.IsAllGroup = allGroup;
                if (filterTable.TryGetValue("isAllTag", out var allTagObj) && allTagObj is bool allTag)
                    cfg.CompileFilter.IsAllTag = allTag;
            }

            // [global]
            if (model.TryGetValue("global", out var globalObj) && globalObj is TomlTable globalTable)
            {
                if (globalTable.TryGetValue("imports", out var importsObj) && importsObj is TomlArray importsArray)
                {
                    foreach (var item in importsArray)
                    {
                        if (item is string s)
                            cfg.Global.Imports.Add(s);
                    }
                }

                // [global.replace]
                if (globalTable.TryGetValue("replace", out var replaceObj) && replaceObj is TomlTable replaceTable)
                {
                    foreach (var kv in replaceTable)
                    {
                        if (kv.Value is string rep)
                            cfg.Global.Replace[kv.Key] = rep;
                    }
                }
            }

            // [[struct.tree]]
            if (model.TryGetValue("struct", out var structObj) && structObj is TomlTable structTable)
            {
                if (structTable.TryGetValue("tree", out var treeObj) && treeObj is TomlTableArray treeArray)
                {
                    foreach (var item in treeArray)
                    {
                        if (item is TomlTable treeEntry)
                        {
                            // namespace = "Std"
                            if (treeEntry.TryGetValue("namespace", out var nsObj) && nsObj is string nsName)
                            {
                                cfg.StructTree.EnsurePath(nsName, ProjectConfig.StructTreeNode.NodeType.Namespace);
                            }

                            // class = "Std.Console"
                            if (treeEntry.TryGetValue("class", out var classObj) && classObj is string className)
                            {
                                cfg.StructTree.EnsurePath(className, ProjectConfig.StructTreeNode.NodeType.Class);
                            }
                        }
                    }
                }
            }

            // [[references]]
            if (model.TryGetValue("references", out var refsObj) && refsObj is TomlArray refsArray)
            {
                foreach (var item in refsArray)
                {
                    if (item is TomlTable refTable)
                    {
                        var r = new ProjectConfig.ReferenceSection();
                        if (refTable.TryGetValue("path", out var pathObj) && pathObj is string refPath)
                            r.Path = refPath;
                        if (!string.IsNullOrEmpty(r.Path))
                            cfg.References.Add(r);
                    }
                }
            }

            return cfg;
        }
    }

    // Simple wrapper project object with minimal logic
    public class Project
    {
        public ProjectConfig Config { get; }

        public Project(ProjectConfig config)
        {
            Config = config ?? new ProjectConfig();
        }

        public string GetOutputDirectory()
        {
            // Output 现在可以从 Project/Compile 组合计算，这里先用 Source.Root 下面的 bin 目录
            return System.IO.Path.Combine(Config.Source.Root, "bin");
        }

        public string GetSourceRoot()
        {
            return Config.Source.Root;
        }

        public string GetEntryFilePath()
        {
             return System.IO.Path.Combine(GetSourceRoot(), Config.Source.EntryFile);
        }
    }
}
