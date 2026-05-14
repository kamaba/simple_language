//****************************************************************************
//  File:      ProjectFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description: project class manager
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SimpleLanguage.Project
{
    public class ProjectClass
    {
        public static MetaClass projectEnter = null;
        public static MetaClass projectDll = null;
        public static MetaClass compile = null;

        public static MetaFunction s_MainFunction = null;
        public static MetaFunction s_TestFunction = null;
        public static MetaFunction s_LoadStartFunction = null;
        public static MetaFunction s_LoadEndFunction = null;
        public static MetaFunction s_CompileBeforeFunction = null;
        public static MetaFunction s_CompileAfterFunction = null;
        public static void ParseCompileClass()
        {
            FileMetaClass fmc = ProjectCompile.projectFileMeta.GetFileMetaClassByName("Compile");

            if (fmc == null) return;

            ClassManager.instance.AddClass(fmc);
            
            compile = fmc.metaClass;
            if (compile == null) return;

            //compile.Parse();
            compile.ParseDefineComplete();
            var flist = compile.staticMetaMemberFunctionList;
            for( int i = 0; i < flist.Count; i++ )
            {
                flist[i].ParseStatements();
            }
            MetaInputParamCollection mipc = new MetaInputParamCollection(compile, null);
            s_CompileBeforeFunction = compile.GetMetaDefineGetSetMemberFunctionByName("_before_", mipc, false,false);
            s_CompileAfterFunction = compile.GetMetaDefineGetSetMemberFunctionByName("_after_", mipc, false, false);

        }
        // Build MetaNode / MetaNamespace tree from StructTreeNode description.
        // parentRoot: existing MetaNode root (通常是 ModuleManager.instance.selfModule.metaNode)
        // node: StructTreeNode from ProjectConfig.StructTree (Root/Namespace/Class)
        public static void AddDefineNamespace(MetaNode parentRoot, ProjectConfig.StructTreeNode node, bool isAddCurrent = true)
        {
            if (parentRoot == null || node == null)
                return;

            // Root 节点只作为逻辑起点，不对应具体 namespace/class，本身不创建 MetaNamespace/MetaClass。
            if (node.Type == ProjectConfig.StructTreeNode.NodeType.Root)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    AddDefineNamespace(parentRoot, node.Children[i], true);
                }
                return;
            }

            MetaNode parMS = null;

            if (node != null)
            {
                if (isAddCurrent)
                {
                    // 尝试在当前父节点下查找同名子节点
                    var cfindNode = parentRoot.GetChildrenMetaNodeByName(node.Name);
                    if (cfindNode == null)
                    {
                        // 不存在同名节点，按 StructTreeNode 类型创建新的 MetaClass / MetaNamespace
                        if (node.Type == ProjectConfig.StructTreeNode.NodeType.Class)
                        {
                            // class: 先尝试从 CoreMetaClassManager 获取内置类，否则创建普通 StructDefine 类
                            var gcmc = CoreMetaClassManager.GetCoreMetaClass(node.Name);
                            if (gcmc != null)
                            {
                                parMS = parentRoot.AddMetaClass(gcmc.GetMetaClassByTemplateCount(0));
                            }
                            else
                            {
                                var nodens = new MetaClass(node.Name, EClassDefineType.StructDefine);
                                parMS = parentRoot.AddMetaClass(nodens);
                            }
                        }
                        else
                        {
                            // namespace / 其它类型一律按命名空间处理
                            var nodeNs = new MetaNamespace(node.Name);
                            parMS = parentRoot.AddMetaNamespace(nodeNs);
                        }
                    }
                    else
                    {
                        // 已有同名子节点，要求其必须是命名空间节点
                        if (!cfindNode.isMetaNamespace)
                        {
                            Log.AddProjectLog(LID.AutoPorjectClassL108, "Error 解析namespace添加命名空间节点时，发现已有定义类!!");
                            return;
                        }
                        // 复用已有命名空间节点
                        parMS = parentRoot.AddMetaNamespace(cfindNode.metaNamespace);
                    }
                }
                else
                {
                    parMS = parentRoot;
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    AddDefineNamespace(parMS, node.Children[i]);
                }
            }
        }
        public static void ProjectCompileBefore()
        {
            NamespaceManager.instance.metaNamespaceDict.Clear();

            // 使用 TOML 基于的 ProjectConfig 填充编译文件列表
            var cfg = ProjectManager.config;
            if (cfg == null)
                return;

            AddDefineNamespace(ModuleManager.instance.selfModule.metaNode, cfg.StructTree, false);

            var fileList = cfg.CompileFiles.Files;
            var filter = cfg.CompileFilter;

            Log.AddProjectLog( LID.ProjectShowCompileFiles, $"",fileList.Count );
 
            for (int i = 0; i < fileList.Count; i++)
            {
                var fld = fileList[i];

                if (IsCanAddFile(filter, fld))
                {
                    ProjectCompile.AddFileParse(fld.Path);
                }
            }

            if (s_CompileBeforeFunction!=null)
            {
                //InnerCLRRuntimeVM.Init();
                //InnerCLRRuntimeVM.RunIRMethod(s_CompileBeforeFunction.irMethod);
            }
        }
        public static bool IsCanAddFile(ProjectConfig.CompileFilterSection cfd, ProjectConfig.CompileFileItem fileData )
        {
            if (cfd == null) return true;

            // group 过滤
            if (!cfd.IsAllGroup)
            {
                if (cfd.Groups.Count > 0 && !cfd.Groups.Contains(fileData.Group))
                    return false;
            }

            // tag 过滤
            if (!cfd.IsAllTag)
            {
                if (cfd.Tags.Count > 0 && !cfd.Tags.Contains(fileData.Tag))
                    return false;
            }

            // ignore 标志
            if (fileData.Ignore)
                return false;

            return true;
        }
        public static void ProjectCompileAfter()
        {
            if (s_CompileBeforeFunction != null)
            {
                //InnerCLRRuntimeVM.Init();
                //InnerCLRRuntimeVM.RunIRMethod(s_CompileBeforeFunction.irMethod);
            }
        }

        public static void InjectProjectGlobalDataFromConfig()
        {
            var dataMap = ProjectManager.config?.Global?.Data;
            if (dataMap == null || dataMap.Count == 0)
            {
                return;
            }

            var projectMc = ClassManager.instance.GetClassByName("S.Project", 0)
                ?? ClassManager.instance.GetClassByName("Core.Project", 0)
                ?? ClassManager.instance.GetClassByName("Project", 0);
            if (projectMc == null)
            {
                return;
            }

            int index = 0;
            foreach (var kv in dataMap)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    continue;
                }

                AddProjectGlobalDataMember(projectMc, kv.Key, kv.Value, index++);
            }
        }

        static void AddProjectGlobalDataMember(MetaClass projectMc, string name, JsonElement element, int index)
        {
            if (projectMc.GetMetaMemberVariableByName(name) != null)
            {
                return;
            }

            var mmv = new MetaMemberVariable(projectMc, name);
            mmv.SetIsStatic(true);
            mmv.SetIsConst(false);

            if (element.ValueKind == JsonValueKind.String)
            {
                mmv.SetIsConst(true);
                mmv.SetMetaDefineType(new MetaType(CoreMetaClassManager.stringMetaClass));
                mmv.SetRealMetaType(new MetaType(CoreMetaClassManager.stringMetaClass));
                mmv.SetIsDefineMetaType(true);
                mmv.SetExpress(new MetaConstExpressNode(EType.String, element.GetString() ?? string.Empty));
                FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                return;
            }

            if (element.ValueKind == JsonValueKind.Number)
            {
                if (element.TryGetInt32(out var i32))
                {
                    mmv.SetIsConst(true);
                    mmv.SetMetaDefineType(new MetaType(CoreMetaClassManager.int32MetaClass));
                    mmv.SetRealMetaType(new MetaType(CoreMetaClassManager.int32MetaClass));
                    mmv.SetIsDefineMetaType(true);
                    mmv.SetExpress(new MetaConstExpressNode(EType.Int32, i32));
                    FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                    return;
                }

                if (element.TryGetDouble(out var f64))
                {
                    mmv.SetIsConst(true);
                    mmv.SetMetaDefineType(new MetaType(CoreMetaClassManager.float64MetaClass));
                    mmv.SetRealMetaType(new MetaType(CoreMetaClassManager.float64MetaClass));
                    mmv.SetIsDefineMetaType(true);
                    mmv.SetExpress(new MetaConstExpressNode(EType.Float64, f64));
                    FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                    return;
                }
            }

            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            {
                mmv.SetIsConst(true);
                mmv.SetMetaDefineType(new MetaType(CoreMetaClassManager.booleanMetaClass));
                mmv.SetRealMetaType(new MetaType(CoreMetaClassManager.booleanMetaClass));
                mmv.SetIsDefineMetaType(true);
                mmv.SetExpress(new MetaConstExpressNode(EType.Boolean, element.GetBoolean()));
                FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                return;
            }

            if (element.ValueKind == JsonValueKind.Null)
            {
                mmv.SetIsConst(true);
                mmv.SetMetaDefineType(new MetaType(CoreMetaClassManager.objectMetaClass));
                mmv.SetRealMetaType(new MetaType(CoreMetaClassManager.objectMetaClass));
                mmv.SetIsDefineMetaType(true);
                mmv.SetExpress(new MetaConstExpressNode(EType.Null, "null"));
                FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                return;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                var dataClass = CreateMetaDataByJsonObject($"___ProjectGlobalData_{name}___", element, index);
                mmv.SetIsDefineMetaType(true);
                mmv.SetMetaDefineType(new MetaType(dataClass));
                mmv.SetRealMetaType(new MetaType(dataClass));
                mmv.SetExpress(new MetaNewObjectExpressNode(new MetaType(dataClass), projectMc, null));
                FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                return;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                if (TryCreateArrayExpressNodeFromJsonArray(projectMc, element, out var arrExpress, out var arrMetaType))
                {
                    mmv.SetMetaDefineType(arrMetaType);
                    mmv.SetRealMetaType(new MetaType(arrMetaType));
                    mmv.SetIsDefineMetaType(true);
                    mmv.SetExpress(new MetaNewObjectExpressNode(arrExpress, projectMc, null, mmv));
                    FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                    return;
                }

                Log.AddProjectLog(LID.ShowExtendMessage, $"global.data array '{name}' contains unsupported element types (only primitive/array supported).");
                return;
            }

            Log.AddProjectLog(LID.ShowExtendMessage, $"Unsupported global.data value kind for '{name}': {element.ValueKind}");
        }

        static void FinalizeInjectedProjectGlobalMember(MetaClass projectMc, MetaMemberVariable mmv)
        {
            projectMc.AddMetaMemberVariable(mmv);

            // Project global.data is injected after the normal ParseMetaClassMemberExpress pass.
            // Re-run expression parse pipeline for injected members so array/data initializers
            // are lowered to NewArray/NewObject IR instead of only carrying raw meta definitions.
            mmv.ParseMetaExpress();
            mmv.CalcReturnType();
        }

        static MetaData CreateMetaDataByJsonObject(string dataName, JsonElement element, int seed)
        {
            var md = new MetaData(dataName, false, false, true);
            int idx = 0;
            foreach (var kv in element.EnumerateObject())
            {
                var child = CreateMetaMemberDataByJson(md, kv.Name, kv.Value, seed * 1000 + idx);
                if (child != null)
                {
                    md.AddMetaMemberData(child, false );
                }
                idx++;
            }

            ClassManager.instance.AddAnonymousMetaData(md);
            return md;
        }

        static MetaMemberData CreateMetaMemberDataByJson(MetaData owner, string name, JsonElement element, int index)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return MetaMemberData.CreateConst(owner, name, index, new MetaConstExpressNode(EType.String, element.GetString() ?? string.Empty));
            }
            if (element.ValueKind == JsonValueKind.Number)
            {
                if (element.TryGetInt32(out var i32))
                {
                    return MetaMemberData.CreateConst(owner, name, index, new MetaConstExpressNode(EType.Int32, i32));
                }
                if (element.TryGetDouble(out var f64))
                {
                    return MetaMemberData.CreateConst(owner, name, index, new MetaConstExpressNode(EType.Float64, f64));
                }
            }
            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            {
                return MetaMemberData.CreateConst(owner, name, index, new MetaConstExpressNode(EType.Boolean, element.GetBoolean()));
            }
            if (element.ValueKind == JsonValueKind.Null)
            {
                return MetaMemberData.CreateConst(owner, name, index, new MetaConstExpressNode(EType.Null, "null"));
            }
            if (element.ValueKind == JsonValueKind.Object)
            {
                var objNode = MetaMemberData.CreateObject(owner, name, index);
                int childIndex = 0;
                foreach (var kv in element.EnumerateObject())
                {
                    var child = CreateMetaMemberDataByJson(owner, kv.Name, kv.Value, index * 1000 + childIndex);
                    if (child != null)
                    {
                        objNode.AddMetaMemberData(child, false);
                    }
                    childIndex++;
                }
                return objNode;
            }
            if (element.ValueKind == JsonValueKind.Array)
            {
                var arrNode = MetaMemberData.CreateArray(owner, name, index, new MetaType(CoreMetaClassManager.objectMetaClass), element.GetArrayLength());
                int childIndex = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var child = CreateMetaMemberDataByJson(owner, childIndex.ToString(), item, index * 1000 + childIndex);
                    if (child != null)
                    {
                        arrNode.AddMetaMemberData(child, false );
                    }
                    childIndex++;
                }
                return arrNode;
            }
            return null;
        }

        static bool TryCreateArrayExpressNodeFromJsonArray(MetaClass ownerMc, JsonElement arrayElement, out MetaArrayExpressNode arrayExpress, out MetaType arrayMetaType)
        {
            arrayExpress = new MetaArrayExpressNode(ownerMc, null, null, null);
            arrayMetaType = null;

            foreach (var item in arrayElement.EnumerateArray())
            {
                if (!TryCreateJsonPrimitiveOrArrayExpressNode(ownerMc, item, out var childExpress))
                {
                    return false;
                }
                arrayExpress.metaCallArray.Add(childExpress);
            }

            arrayMetaType = arrayExpress.GetReturnMetaDefineType();
            return arrayMetaType != null;
        }

        static bool TryCreateJsonPrimitiveOrArrayExpressNode(MetaClass ownerMc, JsonElement element, out MetaExpressNode expressNode)
        {
            expressNode = null;

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    expressNode = new MetaConstExpressNode(EType.String, element.GetString() ?? string.Empty);
                    return true;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var i32))
                    {
                        expressNode = new MetaConstExpressNode(EType.Int32, i32);
                        return true;
                    }
                    if (element.TryGetDouble(out var f64))
                    {
                        expressNode = new MetaConstExpressNode(EType.Float64, f64);
                        return true;
                    }
                    return false;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    expressNode = new MetaConstExpressNode(EType.Boolean, element.GetBoolean());
                    return true;
                case JsonValueKind.Null:
                    expressNode = new MetaConstExpressNode(EType.Null, "null");
                    return true;
                case JsonValueKind.Array:
                    if (TryCreateArrayExpressNodeFromJsonArray(ownerMc, element, out var nestedArray, out _))
                    {
                        expressNode = nestedArray;
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static void ExportProjectGuideMarkdown(string spFilePath, string jsoncPath)
        {
            if (string.IsNullOrWhiteSpace(spFilePath))
            {
                return;
            }

            try
            {
                var dir = Path.GetDirectoryName(spFilePath);
                var projectName = Path.GetFileNameWithoutExtension(spFilePath);
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(projectName))
                {
                    return;
                }

                var mdPath = Path.Combine(dir, projectName + ".md");
                var sb = new StringBuilder();

                sb.AppendLine("# " + projectName + " Project Guide");
                sb.AppendLine();
                sb.AppendLine("- Project Entry File: `" + Path.GetFileName(spFilePath) + "`");
                sb.AppendLine("- Project Config File: `" + Path.GetFileName(jsoncPath) + "`");
                sb.AppendLine();

                sb.AppendLine("## Project Entry Conventions");
                sb.AppendLine();
                sb.AppendLine("### `_main_`\n");
                sb.AppendLine("Primary runtime entry. Normal execution starts here.");
                sb.AppendLine();
                sb.AppendLine("### `_test_`\n");
                sb.AppendLine("Test entry. Used when test mode is enabled.");
                sb.AppendLine();
                sb.AppendLine("### `_before_`\n");
                sb.AppendLine("Compile pre-hook entry (from `Compile` class). Executed before compile core flow when configured.");
                sb.AppendLine();
                sb.AppendLine("### `_after_`\n");
                sb.AppendLine("Compile post-hook entry (from `Compile` class). Executed after compile core flow when configured.");
                sb.AppendLine();

                sb.AppendLine("## Global Integration");
                sb.AppendLine();
                sb.AppendLine("`global.xxx` / `global.func()` is integrated with `Project{}` semantic source.");
                sb.AppendLine();
                sb.AppendLine("### `global.data` from JSONC");
                sb.AppendLine();
                sb.AppendLine("When `global.data` is configured in project JSONC:");
                sb.AppendLine();
                sb.AppendLine("- Primitive values (`int32`/`string`/`float`/`bool`/`null`) are injected as direct static members on `Project` and can be accessed by `global.<name>`. ");
                sb.AppendLine("- Array values are supported (primitive and nested arrays), e.g. `global.arr[0]`, `global.arr2[1][0]`. ");
                sb.AppendLine("- Object values are converted into `MetaData` trees, then injected into `Project` members, e.g. `global.vardata2.a`. ");
                sb.AppendLine();

                var dataMap = ProjectManager.config?.Global?.Data;
                if (dataMap != null && dataMap.Count > 0)
                {
                    sb.AppendLine("### Current configured `global.data` keys");
                    sb.AppendLine();
                    foreach (var kv in dataMap)
                    {
                        sb.AppendLine("- `" + kv.Key + "` (`" + kv.Value.ValueKind + "`)");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("## Example");
                sb.AppendLine();
                sb.AppendLine("```jsonc");
                sb.AppendLine("\"global\": {");
                sb.AppendLine("  \"data\": {");
                sb.AppendLine("    \"var1\": 12,");
                sb.AppendLine("    \"arr\": [1,2,3],");
                sb.AppendLine("    \"arr2\": [[1,2],[3,4]],");
                sb.AppendLine("    \"vardata2\": { \"a\": 10, \"b\": 20, \"flags\": [true,false] }");
                sb.AppendLine("  }");
                sb.AppendLine("}");
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("Access:");
                sb.AppendLine();
                sb.AppendLine("- `global.var1`");
                sb.AppendLine("- `global.arr[0]`");
                sb.AppendLine("- `global.arr2[1][0]`");
                sb.AppendLine("- `global.vardata2.a`");
                sb.AppendLine("- `global.vardata2.b`");

                File.WriteAllText(mdPath, sb.ToString(), new UTF8Encoding(true));
            }
            catch (System.Exception ex)
            {
                Log.AddProjectLog(LID.AutoPorjectClassL554, "Export project guide markdown failed: " + ex.Message);
            }
        }
    }
}
