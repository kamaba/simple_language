//****************************************************************************
//  File:      ProjectFunction.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/1/09 12:00:00
//  Description: project class manager
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SimpleLanguage.Project
{
    public class ProjectClass
    {
        // Build MetaNode / MetaNamespace tree from StructTreeNode description.
        // parentRoot: existing MetaNode root (通常是 ModuleManager.instance.selfModule.metaNode)
        // node: StructTreeNode from ProjectConfig.StructTree (Root/Namespace/Class)
        public static void AddDefineNamespace(MetaNode parentRoot, ProjectConfig.StructTreeNode node)
        {
            if (parentRoot == null || node == null)
            {
                Log.AddProjectLog(LID.ProjectProjectClassIsNullNodeNull, $"Error [{node.Name}] node is null" );
                return;
            }

            // Root 节点只作为逻辑起点，不对应具体 namespace/class，本身不创建 MetaNamespace/MetaClass。
            if (node.Type == ProjectConfig.StructTreeNode.NodeType.Root)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    AddDefineNamespace(parentRoot, node.Children[i] );
                }
                return;
            }

            MetaNode parMS = null;
            if (node != null)
            {
                // 尝试在当前父节点下查找同名子节点
                var cfindNode = parentRoot.GetChildrenMetaNodeByName(node.Name);
                bool isFind = cfindNode != null;
                // 不存在同名节点，按 StructTreeNode 类型创建新的 MetaClass / MetaNamespace
                if (node.Type == ProjectConfig.StructTreeNode.NodeType.Class)
                {
                    if( isFind )
                    {
                        if(cfindNode.IsMetaClass() )
                        {
                            parMS = cfindNode;
                        }
                        else
                        {
                            Log.AddProjectLog(LID.ProjectProjectClassNamespace, "Error 解析namespace添加命名空间节点时，发现已有定义类11!!" + node.Name ); 
                        }
                    }
                    else
                    {
                        var nodens = new MetaClass(node.Name, EClassDefineType.StructDefine);
                        parMS = parentRoot.AddMetaClass(nodens);
                    }
                }
                else if (node.Type == ProjectConfig.StructTreeNode.NodeType.Data)
                {
                    if (isFind)
                    {
                        if (cfindNode.isMetaData )
                        {
                            parMS = cfindNode;
                        }
                        else
                        {
                            Log.AddProjectLog(LID.ProjectProjectClassNamespace2, "Error 解析namespace添加命名空间节点时，发现已有定义类22!!" + node.Name);
                        }
                    }
                    else
                    {
                        var nodens = new MetaClass(node.Name, EClassDefineType.StructDefine);
                        parMS = parentRoot.AddMetaClass(nodens);
                    }
                }
                else if (node.Type == ProjectConfig.StructTreeNode.NodeType.Enum)
                {
                    if (isFind)
                    {
                        if (cfindNode.isMetaEnum )
                        {
                            parMS = cfindNode;
                        }
                        else
                        {
                            Log.AddProjectLog(LID.ProjectProjectClassNamespace3, "Error 解析namespace添加命名空间节点时，发现已有定义类33!!" + node.Name);
                        }
                    }
                    else
                    {
                        // enum 节点必须创建 MetaEnum 壳：
                        // 若错误地创建 MetaClass 壳，AddClass 会把源码中的 enum 定义
                        // 绑定到该 MetaClass 上（innderDefine && !manaualDefine 分支），
                        // 导致 enum 永远不会成为 MetaEnum，ParseExtendsRelation 不执行，
                        // "enum X extends Error { A = { code = 1 } }" 中的 code 成员找不到。
                        var nodeEnum = new MetaEnum(node.Name);
                        parMS = parentRoot.AddMetaEnum(nodeEnum);
                        nodeEnum.SetClassDefineType(EClassDefineType.StructDefine);
                        nodeEnum.UpdateAllName();
                    }
                }
                else
                {
                    if (isFind)
                    {
                        if (cfindNode.isMetaNamespace )
                        {
                            parMS = cfindNode;
                        }
                        else
                        {
                            Log.AddProjectLog(LID.ProjectProjectClassNamespace4, "Error 解析namespace添加命名空间节点时，发现已有定义类44!!" + node.Name);
                        }
                    }
                    else
                    {

                        var nodeNs = new MetaNamespace(node.Name);
                        parMS = parentRoot.AddMetaNamespace(nodeNs);
                    }
                }

                for (int i = 0; i < node.Children.Count; i++)
                {
                    AddDefineNamespace(parMS, node.Children[i]);
                }
            }
        }
        /// <summary>
        /// 编译前管线步骤（File 阶段 PrepareFiles）：清空并按 jsonc 重建命名空间、
        /// 按优先级填充编译文件列表。.sp Project 类的 `_before_()` 编译前钩子语义
        /// 锚定于此时机：其函数体中的 global.macro 赋值由 PreScanCompileBeforeMacroAssign
        /// 在文件解析完成后（MetaCore.InjectProjectData 步骤）编译期求值消费。
        /// </summary>
        public static void ProjectCompileBefore()
        {
            NamespaceManager.instance.metaNamespaceDict.Clear();

            // 使用 TOML 基于的 ProjectConfig 填充编译文件列表
            var cfg = ProjectManager.config;
            if (cfg == null)
                return;

            AddDefineNamespace(ModuleManager.instance.selfModule.metaNode, cfg.StructTree );

            var fileList = cfg.CompileFiles.Files;
            var filter = cfg.CompileFilter;

            Log.AddProjectLog( LID.ProjectShowCompileFiles, $"",fileList.Count );

            // Sort by priority (lower value = earlier in compile/execution order).
            // OrderBy is a stable sort, preserving config order for equal priorities.
            var sortedList = fileList.OrderBy(f => f.Priority).ToList();

            for (int i = 0; i < sortedList.Count; i++)
            {
                var fld = sortedList[i];

                if (IsCanAddFile(filter, fld))
                {
                    ProjectCompile.AddFileParse(fld.Path);
                }
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
        /// <summary>
        /// 编译后管线步骤（MetaCore 阶段末尾、TranslateIR 之前）：.sp Project 类的
        /// `_after_()` 钩子语义锚定于此时机（编译完成后、导出 IR 前）。
        /// 钩子函数体按普通函数编入 IR；当前无编译期消费逻辑，预留为管线扩展点。
        /// </summary>
        public static void ProjectCompileAfter()
        {
        }

        public static void InjectProjectGlobalDataFromConfig()
        {
            var cfg = ProjectManager.config;
            // 数据注入面：jsonc 根->global->data（"global"."data"）为唯一位置；
            // 根级 "data" 已废弃（loader 检测到即告警），不再参与注入。
            var merged = new Dictionary<string, JsonElement>();
            if (cfg?.Global?.Data != null)
            {
                foreach (var kv in cfg.Global.Data)
                {
                    merged[kv.Key] = kv.Value;
                }
            }

            if (merged.Count == 0)
            {
                return;
            }

            var projectMc = ClassManager.instance.TryGetProjectMetaClass();
            if (projectMc == null)
            {
                return;
            }

            int index = 0;
            foreach (var kv in merged)
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

            // Project 成员（含 jsonc 注入）不允许与 Module 根下的名称相同，
            // 理由同 .sp 定义路径（ModuleName.name 限定访问时重名产生歧义）。
            if (MetaClass.IsNameConflictWithModuleRoot(name, "jsonc注入成员变量"))
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
                mmv.SetExpress(new MetaNewObjectExpressNode(new MetaType(dataClass), projectMc, null ) );
                FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                return;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                if (TryCreateArrayExpressNodeFromJsonArray(projectMc, element, out var arrExpress, out var arrMetaType))
                {
                    mmv.SetMetaDefineType(arrMetaType);
                    mmv.SetRealMetaType(arrMetaType);
                    mmv.SetIsDefineMetaType(true);
                    mmv.SetExpress(new MetaNewObjectExpressNode(arrMetaType, arrExpress, projectMc, null ));
                    FinalizeInjectedProjectGlobalMember(projectMc, mmv);
                    return;
                }

                Log.AddProjectLog(LID.ProjectProjectClassJsoncDataArray, $"jsonc data array '{name}' contains unsupported element types (only primitive/array supported).");
                return;
            }

            Log.AddProjectLog(LID.ProjectProjectClassUnsupportedJsoncData, $"Unsupported jsonc data value kind for '{name}': {element.ValueKind}");
        }

        static void FinalizeInjectedProjectGlobalMember(MetaClass projectMc, MetaMemberVariable mmv)
        {
            projectMc.AddMetaMemberVariable(mmv, false );

            // Jsonc root "data" + legacy global.data: injected as Project statics after ParseMetaClassMemberExpress.
            // Re-run expression parse pipeline for injected members so array/data initializers
            // are lowered to NewArray/NewObject IR instead of only carrying raw meta definitions.
            mmv.ParseMetaExpress();
            mmv.ParseRealMetaType();
        }

        // global.macro 宏注入（jsonc 宏数据面 + 运行期只读访问）：
        //   1. LoadFromConfig 载入 jsonc global.macro 段初始值（`_before_` 钩子预扫描在其后修改宏值）；
        //   2. 把最终宏值注入为 Project 静态成员 macro（global.macro.<name>，运行期可读）。
        // 成员定义方式与 jsonc data 的 Object 段一致（匿名 MetaData + MetaNewObjectExpressNode）。
        // 注：static if 编译期条件编译已废弃收编为 @Exclude（ATTRIBUTE_DESIGN §7），
        // 宏数据面（jsonc/环境变量/CLI 注入）与运行期 macro 只读访问保留。
        public static void InjectProjectMacroMember()
        {
            // 编译开始：重置为 jsonc global.macro 段初始值
            CompileBeforeManager.instance.LoadFromConfig(ProjectManager.config);
            // Project 类 `_before_()` 钩子预扫描：编译期唯一允许修改 global.macro 的入口
            PreScanCompileBeforeMacroAssign();

            var macroValues = CompileBeforeManager.instance.GetAllMacroValues();
            if (macroValues.Count == 0)
            {
                return;
            }

            var projectMc = ClassManager.instance.TryGetProjectMetaClass();
            if (projectMc == null)
            {
                return;
            }

            // 用户显式定义过同名成员时不注入（用户优先）
            if (projectMc.GetMetaMemberVariableByName("macro") != null)
            {
                return;
            }
            if (MetaClass.IsNameConflictWithModuleRoot("macro", "jsonc注入成员变量"))
            {
                return;
            }

            // 最终宏值 → 匿名 MetaData（成员格式与 data 定义一致）
            var macroObj = JsonSerializer.SerializeToElement(macroValues);
            var macroData = CreateMetaDataByJsonObject("___ProjectGlobalMacro___", macroObj, 0);

            var mmv = new MetaMemberVariable(projectMc, "macro");
            mmv.SetIsStatic(true);
            mmv.SetIsConst(false);
            mmv.SetIsDefineMetaType(true);
            mmv.SetMetaDefineType(new MetaType(macroData));
            mmv.SetRealMetaType(new MetaType(macroData));
            mmv.SetExpress(new MetaNewObjectExpressNode(new MetaType(macroData), projectMc, null));
            FinalizeInjectedProjectGlobalMember(projectMc, mmv);
        }

        /// <summary>
        /// Project 类 `_before_()` 钩子预扫描：编译期唯一允许修改 global.macro 的入口。
        /// 在 .sp 的 Project 类 `_before_()` 函数体中
        /// 识别 global.macro.X = 常量/宏引用 赋值，编译期求值后通过 MacroManager.SetMacroValue
        /// 更新宏值，并把这些赋值语句从语法列表移除——不参与后续语义分析与 IR（static 不进 runtime）。
        /// 时序：InjectProjectData 步骤内、LoadFromConfig 之后、macro 成员注入之前。
        /// </summary>
        public static void PreScanCompileBeforeMacroAssign()
        {
            var fmc = ProjectCompile.projectFileMeta?.GetFileMetaClassByName("Project");
            if (fmc == null)
            {
                return;
            }

            FileMetaMemberFunction beforeFunc = null;
            foreach (var fmf in fmc.memberFunctionList)
            {
                var fname = fmf?.token?.lexeme?.ToString();
                if (fname == "_before_")
                {
                    beforeFunc = fmf;
                    break;
                }
            }
            if (beforeFunc == null)
            {
                return;
            }

            var block = beforeFunc.fileMetaBlockSyntax;
            if (block == null)
            {
                return;
            }

            List<FileMetaSyntax> removeList = null;
            var syntaxList = block.fileMetaSyntax;
            for (int i = 0; i < syntaxList.Count; i++)
            {
                // 只认普通赋值 (=)：global.macro.X = 右值
                if (syntaxList[i] is FileMetaOpAssignSyntax fmoas
                    && fmoas.assignToken?.type == ETokenType.Assign
                    && CompileBeforeManager.TryGetMacroRefName(fmoas.variableRef, out string macroName))
                {
                    // 右值必须是常量或另一个宏引用，求值失败错误已由 MacroManager 记录
                    if (CompileBeforeManager.instance.TryEvaluateMacroConstExpress(fmoas.express, out var value))
                    {
                        CompileBeforeManager.instance.SetMacroValue(macroName, value, fmoas.assignToken);
                        (removeList ??= new List<FileMetaSyntax>()).Add(fmoas);
                    }
                }
            }

            // 移除已消费的赋值语句：`_before_` 钩子中的 macro 赋值是编译期行为，不进 runtime
            if (removeList != null)
            {
                for (int i = 0; i < removeList.Count; i++)
                {
                    block.fileMetaSyntax.Remove(removeList[i]);
                }
            }
        }

        // 系统集成成员：Project 类静态 Array<Object> _inputArgs。
        // C VM 启动时（vm_scheduler_enter 之前）会把命令行传入的程序参数
        // 自动填充进该数组（见 csimple_lang vm_fill_input_args），源码中通过
        // global._inputArgs 访问。集成过程对用户不可见，无需在 .sp/jsonc 中声明。
        public static void InjectInputArgsMember()
        {
            const string InputArgsMemberName = "_inputArgs";

            var projectMc = ClassManager.instance.TryGetProjectMetaClass();
            if (projectMc == null)
            {
                return;
            }

            // 用户在 .sp 或 jsonc global.data 中显式定义过同名成员时不注入（用户优先）。
            if (projectMc.GetMetaMemberVariableByName(InputArgsMemberName) != null)
            {
                return;
            }

            var mmv = new MetaMemberVariable(projectMc, InputArgsMemberName);
            mmv.SetIsStatic(true);
            mmv.SetIsConst(false);

            // 空数组初值 [] -> Array<Object>, length 0 (MetaArrayExpressNode.CalcReturnType 空数组分支)。
            var arrExpress = new MetaArrayExpressNode(projectMc, null, null, null);
            arrExpress.CalcReturnType();
            var arrMetaType = arrExpress.GetReturnMetaType();
            if (arrMetaType == null)
            {
                Log.AddProjectLog(LID.ProjectProjectClassCannotInject_inputArgs, "Inject _inputArgs failed: cannot build Array<Object> meta type.");
                return;
            }

            mmv.SetMetaDefineType(arrMetaType);
            mmv.SetRealMetaType(arrMetaType);
            mmv.SetIsDefineMetaType(true);
            mmv.SetExpress(new MetaNewObjectExpressNode(arrMetaType, arrExpress, projectMc, null));

            FinalizeInjectedProjectGlobalMember(projectMc, mmv);
        }

        // =====================================================================
        // dllImports（project.jsonc "dllImports"/"lib" 段）：global.dllImport 注入
        // =====================================================================
        // 通过别名直接访问外部 dll 库对象，免在代码里写长路径：
        //     global.dllImport.<alias>  ->  FFI.Library 实例
        //（.isValid / .getFunction(...) 等直接链式使用；随 Project 静态初始化
        // 进程级常驻，符合 C# DllImport 语义）。
        //
        // 分两步注入：
        //  1) File 层（CreateNamespace 前，管线步骤 InjectDllImportHolder）：
        //     按配置源码合成持有类（机制同 BindExpandManager 源码合成先例），
        //     同时把 FFI 命名空间注册进所在 .sp 的 FileMeta（等价 import FFI;）：
        //         ___DllImportGlobal___ extends Object
        //         {
        //             public override void _init_() { }
        //             public FFI.Library <alias> = FFI.Library( "<path>" )
        //         }
        //  2) Meta 层（InjectProjectData，同 global.data 注入步骤）：Project 元类
        //     注入 static 成员 dllImport = ___DllImportGlobal___()（NewObject 初值，
        //     机制同 global.data 对象注入），实现 global.dllImport.<alias> 链式访问。
        public const string DllImportHolderClassName = "___DllImportGlobal___";
        public const string DllImportMemberName = "dllImport";

        /// <summary>
        /// File 层：按 dllImports 配置合成持有类源码并注入编译管线。
        /// 时机在 File 阶段完成后、CreateNamespace 前（与 ExpandBind 同窗口），
        /// 使合成类与普通源码类走相同的命名空间/成员解析管线。
        /// FFI.Library 定义于 Std 模块：项目未引用 Std 时不注入
        /// （@DllImport("别名",...) 的路径查表解析不受影响）。
        /// </summary>
        public static void InjectDllImportHolderClass( List<FileParse> fileParseList )
        {
            var cfg = ProjectManager.config;
            if ( cfg?.DllImports == null || cfg.DllImports.Count == 0 )
                return;
            if ( fileParseList == null || fileParseList.Count == 0 )
                return;

            // FFI.Library 可用性检查（引用模块在 RefModule 阶段已装载）
            var ffiNamespaceNode = ModuleManager.instance.GetChildrenMetaNodeByName( "FFI" );
            var ffiLibNode = ffiNamespaceNode?.GetChildrenMetaNodeByName( "Library" );
            if ( ffiLibNode == null )
            {
                Log.AddProjectLog( LID.ProjectProjectClassNotFoundDllImportsFFI,
                    "dllImports: FFI.Library not found (Std not referenced?), skip global.dllImport injection." );
                return;
            }

            var fm = ProjectCompile.projectFileMeta ?? fileParseList[0].file;
            if ( fm == null )
                return;
            if ( fm.GetFileMetaClassByName( DllImportHolderClassName ) != null )
                return;   // 已注入（防重复）

            // 把 FFI 命名空间注册进本 FileMeta（等价 import FFI;）：合成类所在的
            // .sp 文件没有 import 语句，注册后短名 FFI.Library 才可解析；查找顺序
            // 上用户自有名称优先（import 仅作兜底），不影响 .sp 原有语义。
            fm.AddImportMetaNode( ffiNamespaceNode );

            // 合成持有类源码（成员名 = 别名；跳过非法/重复别名）
            var sb = new StringBuilder();
            sb.AppendLine( DllImportHolderClassName + " extends Object" );
            sb.AppendLine( "{" );
            sb.AppendLine( "    public override void _init_()" );
            sb.AppendLine( "    {" );
            sb.AppendLine( "    }" );
            int count = 0;
            var usedAlias = new HashSet<string>();
            foreach ( var d in cfg.DllImports )
            {
                if ( d == null || string.IsNullOrWhiteSpace( d.Path ) )
                    continue;
                var alias = d.Alias;
                if ( string.IsNullOrWhiteSpace( alias ) )
                    alias = d.Name;
                if ( string.IsNullOrWhiteSpace( alias ) || !usedAlias.Add( alias ) )
                    continue;
                if ( !IsValidSlIdentifier( alias ) )
                {
                    Log.AddProjectLog( LID.ProjectProjectClassDllImportsAliasNot,
                        $"dllImports: alias '{alias}' is not a valid identifier, skipped." );
                    continue;
                }
                sb.AppendLine( $"    public FFI.Library {alias} = FFI.Library( \"{EscapeSlStringLiteral( d.Path )}\" )" );
                count++;
            }
            sb.AppendLine( "}" );
            if ( count == 0 )
                return;

            // 源码文本 -> Token -> Struct -> FileMeta（照 BindExpandManager.InjectSyntheticMembers）
            try
            {
                var lexer = new LexerParse( fm.path, sb.ToString().ToCharArray() );
                lexer.ParseToTokenList();
                var tokenParse = new TokenParse( fm, lexer.listTokens );
                tokenParse.BuildStruct();
                var structParse = new StructParse( fm, tokenParse.rootNode );
                structParse.ParseRootNodeToFileMeta();

                if ( fm.GetFileMetaClassByName( DllImportHolderClassName ) == null )
                {
                    Log.AddProjectLog( LID.ProjectProjectClassDllImportsSyntheticHolder,
                        "dllImports: synthetic holder class parse failed, global.dllImport not injected." );
                }
            }
            catch ( System.Exception e )
            {
                Log.AddProjectLog( LID.ProjectProjectClassDllImportsHolderClass,
                    $"dllImports: holder class synthesis error: {e.Message}" );
            }
        }

        /// <summary>
        /// Meta 层：Project 元类注入 static 成员 dllImport（NewObject 持有类初值），
        /// 时机同 global.data 注入（InjectProjectData 步骤）。用户显式定义过
        /// 同名成员时不注入（用户优先）。
        /// </summary>
        public static void InjectDllImportMember()
        {
            var cfg = ProjectManager.config;
            if ( cfg?.DllImports == null || cfg.DllImports.Count == 0 )
                return;

            var projectMc = ClassManager.instance.TryGetProjectMetaClass();
            if ( projectMc == null )
                return;
            if ( projectMc.GetMetaMemberVariableByName( DllImportMemberName ) != null )
                return;   // 用户定义优先

            // 持有类（File 层步骤注入；FFI 不可用/合成失败时不存在，静默跳过）
            var holderMc = ProjectCompile.projectFileMeta
                ?.GetFileMetaClassByName( DllImportHolderClassName )?.metaClass;
            if ( holderMc == null )
                return;

            // Project 成员不允许与 Module 根下的名称相同（同 jsonc 注入成员约束）
            if ( MetaClass.IsNameConflictWithModuleRoot( DllImportMemberName, "dllImport注入成员" ) )
                return;

            var mmv = new MetaMemberVariable( projectMc, DllImportMemberName );
            mmv.SetIsStatic( true );
            mmv.SetIsConst( false );
            mmv.SetIsDefineMetaType( true );
            mmv.SetMetaDefineType( new MetaType( holderMc ) );
            mmv.SetRealMetaType( new MetaType( holderMc ) );
            mmv.SetExpress( new MetaNewObjectExpressNode( new MetaType( holderMc ), projectMc, null ) );
            FinalizeInjectedProjectGlobalMember( projectMc, mmv );
        }

        /// <summary>
        /// Meta 层：dllImports 项 functions 段注入 Project 静态库函数变量
        /// （C# DllImport 语义）：global.&lt;funcName&gt;(实参...) 直接调用。
        /// 每项 { name, symbol, sig } 等价合成：
        ///     static Func&lt;Ret,P...&gt; name = FFI.StaticLibrary.bindFunction( path, symbol, sig )
        /// sig 短名（"i32,i32-&gt;i32"）映射 Func 模板实参类型；时机同
        /// InjectDllImportMember（InjectProjectData 步骤），用户显式定义过
        /// 同名成员时不注入（用户优先）。
        /// </summary>
        public static void InjectDllImportFunctionMembers()
        {
            var cfg = ProjectManager.config;
            if ( cfg?.DllImports == null || cfg.DllImports.Count == 0 )
                return;

            var projectMc = ClassManager.instance.TryGetProjectMetaClass();
            if ( projectMc == null )
                return;

            // FFI 命名空间须已由 InjectDllImportHolderClass 注册进 projectFileMeta
            // （Std 未引用时 bindFunction 不可解析，整体跳过）
            var fm = ProjectCompile.projectFileMeta;
            if ( fm == null || ModuleManager.instance.GetChildrenMetaNodeByName( "FFI" ) == null )
                return;

            var usedNames = new HashSet<string>();
            foreach ( var d in cfg.DllImports )
            {
                if ( d == null || string.IsNullOrWhiteSpace( d.Path ) || d.Functions == null )
                    continue;
                foreach ( var f in d.Functions )
                {
                    if ( f == null || string.IsNullOrWhiteSpace( f.Name ) || string.IsNullOrWhiteSpace( f.Symbol ) )
                        continue;
                    var funcName = f.Name;
                    if ( !IsValidSlIdentifier( funcName ) )
                    {
                        Log.AddProjectLog( LID.ProjectProjectClassDllImportsFunctionName,
                            $"dllImports: function name '{funcName}' is not a valid identifier, skipped." );
                        continue;
                    }
                    if ( !usedNames.Add( funcName ) )
                        continue;   // 同名函数变量只注入第一个
                    // 用户定义优先
                    if ( projectMc.GetMetaMemberVariableByName( funcName ) != null )
                        continue;
                    if ( MetaClass.IsNameConflictWithModuleRoot( funcName, "dllImports函数注入成员" ) )
                        continue;

                    var funcMt = BuildFuncMetaTypeByFFISig( funcName, f.Sig );
                    if ( funcMt == null )
                    {
                        Log.AddProjectLog( LID.ProjectProjectClassDllImportsFunctionSig,
                            $"dllImports: function '{funcName}' sig '{f.Sig}' has unsupported type, skipped." );
                        continue;
                    }

                    var mmv = new MetaMemberVariable( projectMc, funcName );
                    mmv.SetIsStatic( true );
                    mmv.SetIsConst( false );
                    mmv.SetIsDefineMetaType( true );
                    mmv.SetMetaDefineType( funcMt );
                    mmv.SetRealMetaType( funcMt );

                    // static Func<Ret,P...> name = FFI.StaticLibrary.bindFunction( path, symbol, sig )
                    var bindTerm = BuildBindFunctionCallTerm( fm, d.Path, f.Symbol, f.Sig );
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaBase = projectMc;
                    cep.metaType = funcMt;
                    cep.equalMetaVariable = mmv;
                    cep.parsefrom = EParseFrom.MemberVariableExpress;
                    cep.isConst = false;
                    cep.isStatic = true;
                    cep.fme = bindTerm;
                    var express = ExpressManager.CreateExpressNode( cep );
                    if ( express == null )
                    {
                        Log.AddProjectLog( LID.ProjectProjectClassDllImportsFunctionBind,
                            $"dllImports: function '{funcName}' bind expression build failed, skipped." );
                        continue;
                    }
                    mmv.SetExpress( express );

                    Log.AddProjectLog( LID.ProjectProjectClassDllImportsInjectGlobal,
                        $"dllImports: inject global.{funcName} = FFI.StaticLibrary.bindFunction(\"{d.Path}\", \"{f.Symbol}\", \"{f.Sig}\")" );
                    FinalizeInjectedProjectGlobalMember( projectMc, mmv );
                }
            }
        }

        /// <summary>
        /// FFI sig（"i32,i32-&gt;i32"）→ Func&lt;Ret,P...&gt; 函数签名 MetaType。
        /// 短名映射为 MetaDefineVarStatements.FFISigNameOfMetaType 的反向
        /// （与 cvm 侧 vm_ffi_sl_name_to_ffi 对齐；ptr 按 Int64 地址传递；
        /// void 仅允许出现在返回位）。
        /// </summary>
        static MetaType BuildFuncMetaTypeByFFISig( string aliasName, string sig )
        {
            if ( string.IsNullOrWhiteSpace( sig ) )
                return null;
            string paramPart = sig;
            string retPart = null;
            int idx = sig.LastIndexOf( "->" );
            if ( idx >= 0 )
            {
                paramPart = sig.Substring( 0, idx );
                retPart = sig.Substring( idx + 2 );
            }
            var paramTypes = new List<MetaType>();
            if ( !string.IsNullOrWhiteSpace( paramPart ) )
            {
                foreach ( var p in paramPart.Split( ',' ) )
                {
                    var pt = FFIMetaTypeOfSigName( p.Trim() );
                    if ( pt == null )
                        return null;
                    paramTypes.Add( pt );
                }
            }
            var retType = FFIMetaTypeOfSigName( string.IsNullOrWhiteSpace( retPart ) ? "void" : retPart.Trim() );
            if ( retType == null )
                return null;
            var fsmc = new FunctionSignatureMetaClass( aliasName, retType, paramTypes );
            return new MetaType( fsmc );
        }

        /// <summary>FFI sig 短名 → SL 类型（未识别返回 null）。</summary>
        static MetaType FFIMetaTypeOfSigName( string sigName )
        {
            switch ( sigName )
            {
                case "void":   case "Void":                                  return new MetaType( CoreMetaClassManager.voidMetaClass );
                case "bool":   case "Bool":   case "boolean":                 return new MetaType( CoreMetaClassManager.booleanMetaClass );
                case "i8":     case "Int8":                                   return new MetaType( CoreMetaClassManager.int8MetaClass );
                case "u8":     case "UInt8":  case "byte":    case "Byte":    return new MetaType( CoreMetaClassManager.uint8MetaClass );
                case "i16":    case "Int16":  case "short":                    return new MetaType( CoreMetaClassManager.int16MetaClass );
                case "u16":    case "UInt16": case "ushort":                   return new MetaType( CoreMetaClassManager.uint16MetaClass );
                case "i32":    case "int":    case "Int32":                    return new MetaType( CoreMetaClassManager.int32MetaClass );
                case "u32":    case "UInt32": case "uint":                     return new MetaType( CoreMetaClassManager.uint32MetaClass );
                case "i64":    case "long":   case "Int64":    case "Long":    return new MetaType( CoreMetaClassManager.int64MetaClass );
                case "u64":    case "UInt64": case "ulong":                    return new MetaType( CoreMetaClassManager.uint64MetaClass );
                case "f32":    case "float":  case "Float32":                  return new MetaType( CoreMetaClassManager.float32MetaClass );
                case "f64":    case "double": case "Float64":                  return new MetaType( CoreMetaClassManager.float64MetaClass );
                case "f16":    case "Float16": case "half":                    return new MetaType( CoreMetaClassManager.float16MetaClass );
                case "bf16":   case "Float16_Brain":                          return new MetaType( CoreMetaClassManager.float16_BrainMetaClass );
                case "f8e4m3": case "Float8":                                 return new MetaType( CoreMetaClassManager.float8MetaClass );
                case "f8e5m2": case "Float8_E5M2":                            return new MetaType( CoreMetaClassManager.float8_E5M2MetaClass );
                case "utf8":   case "string": case "String":                   return new MetaType( CoreMetaClassManager.stringMetaClass );
                case "ptr":    case "Ptr":                                    return new MetaType( CoreMetaClassManager.int64MetaClass );
                default:                                                      return null;
            }
        }

        /// <summary>
        /// 合成 FFI.StaticLibrary.bindFunction( libPath, symbol, sig ) 的
        /// FileMetaCallTerm（程序化构造节点链的通用约定：SetIdentifierNode
        /// 必须先设置，否则 AddLinkNode 静默失效）。
        /// </summary>
        static FileMetaCallTerm BuildBindFunctionCallTerm( FileMeta fm, string libPath, string symbol, string sig )
        {
            string path = fm?.path ?? "";
            int line = 0;
            int pos = 0;

            // FFI.StaticLibrary.bindFunction( libPath, symbol, sig )
            var bindNode = MakeIdentLinkNode( path, line, pos, "bindFunction" );
            bindNode.SetParNode( MakeStringArgsParNode( path, line, pos, libPath, symbol, sig ) );

            // FFI -> . -> StaticLibrary -> . -> bindFunction(...)
            var ffiNode = MakeIdentLinkNode( path, line, pos, "FFI" );
            ffiNode.SetIdentifierNode( ffiNode );
            ffiNode.AddLinkNode( MakePeriodNode( path, line, pos ) );
            ffiNode.AddLinkNode( MakeIdentLinkNode( path, line, pos, "StaticLibrary" ) );
            ffiNode.AddLinkNode( MakePeriodNode( path, line, pos ) );
            ffiNode.AddLinkNode( bindNode );

            return new FileMetaCallTerm( fm, ffiNode );
        }

        static Node MakeIdentLinkNode( string path, int line, int pos, string name )
        {
            return new Node( new Token( path, ETokenType.Identifier, name, line, pos ) ) { nodeType = ENodeType.IdentifierLink };
        }

        static Node MakePeriodNode( string path, int line, int pos )
        {
            return new Node( new Token( path, ETokenType.Period, ".", line, pos ) ) { nodeType = ENodeType.Period };
        }

        /// <summary>
        /// 合成 ( "s1", "s2", ... ) Par 节点：childList 为
        /// [ConstValue, Comma, ConstValue, ...]（FileMetaParTerm 按 Comma 拆分）。
        /// String token 形态与 MetaMemberVariable.MakeStringArgsParNode 一致
        /// （lexeme 带引号 + 单子 token 存内容，MetaConstExpressNode 取子 token）。
        /// </summary>
        static Node MakeStringArgsParNode( string path, int line, int pos, params string[] stringArgs )
        {
            var parNode = new Node( new Token( path, ETokenType.LeftPar, "(", line, pos ) ) { nodeType = ENodeType.Par };
            parNode.endToken = new Token( path, ETokenType.RightPar, ")", line, pos );
            for ( int i = 0; i < stringArgs.Length; i++ )
            {
                if ( i > 0 )
                {
                    parNode.AddChild( new Node( new Token( path, ETokenType.Comma, ",", line, pos ) ) { nodeType = ENodeType.Comma } );
                }
                var strToken = new Token( path, ETokenType.String, "\"" + stringArgs[i] + "\"", line, pos );
                strToken.AddChildrenToken( new Token( path, ETokenType.String, stringArgs[i], line, pos ) );
                parNode.AddChild( new Node( strToken ) { nodeType = ENodeType.ConstValue } );
            }
            return parNode;
        }

        static bool IsValidSlIdentifier( string name )
        {
            return !string.IsNullOrEmpty( name )
                && ( char.IsLetter( name[0] ) || name[0] == '_' )
                && name.All( c => char.IsLetterOrDigit( c ) || c == '_' );
        }

        static string EscapeSlStringLiteral( string s )
        {
            return s.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
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
                    md.AddMetaMemberData(child );
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
                var inlineOwner = new MetaData("JsonObject_" + name + "_" + index, false, false, true);
                int childIndex = 0;
                foreach (var kv in element.EnumerateObject())
                {
                    var child = CreateMetaMemberDataByJson(inlineOwner, kv.Name, kv.Value, index * 1000 + childIndex);
                    if (child != null)
                    {
                        inlineOwner.AddMetaMemberData(child);
                    }
                    childIndex++;
                }

                var objNode = MetaMemberData.CreateDeclared(owner, name, index, new MetaType(CoreMetaClassManager.objectMetaClass), false);
                var canon = ClassManager.instance.FindMetaDataByNameAndType(inlineOwner);     
                if( canon == null )
                {
                    ClassManager.instance.AddAnonymousMetaData(inlineOwner);
                    canon = inlineOwner;
                }
                if (canon != null)
                {
                    var newObj = MetaNewObjectExpressNode.CreateFromAnonymousMetaData(canon, owner, null);
                    objNode.SetExpress(newObj);
                    objNode.SetMetaDefineType(new MetaType(canon));
                }
                return objNode;
            }
            if (element.ValueKind == JsonValueKind.Array)
            {
                var arrNode = MetaMemberData.CreateArray(owner, name, index, new MetaType(CoreMetaClassManager.objectMetaClass), element.GetArrayLength());
                var maen = new MetaArrayExpressNode(owner, null, arrNode.defineMetaType, null);
                int childIndex = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var child = CreateMetaMemberDataByJson(owner, childIndex.ToString(), item, index * 1000 + childIndex);
                    if (child?.expressNode != null)
                    {
                        maen.metaCallArray.Add(child.expressNode);
                    }
                    childIndex++;
                }
                maen.CalcReturnType();
                arrNode.SetExpress(ExpressManager.ConvertNewExpress(maen, arrNode.defineMetaType ) ?? maen);
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

            arrayExpress.CalcReturnType();
            arrayMetaType = arrayExpress.GetReturnMetaType();
            return arrayMetaType != null;
        }

        static bool TryCreateJsonPrimitiveOrArrayExpressNode(MetaClass ownerMc, JsonElement element, out MetaExpressNodeBase expressNode)
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

        // ---- 项目向导 Markdown（模板 + AI 描述预留段） ----

        // 模板内嵌资源尾名（SimpleLanguageFront.csproj: Project\Templates\ProjectGuideTemplate.md）
        const string GuideTemplateNameTail = "ProjectGuideTemplate.md";
        // AI 描述区标记：重新生成 .md 时保留两标记之间的内容（AI 填写的项目描述不被覆盖）
        const string AiDescriptionBeginMarker = "<!-- AI-DESCRIPTION:BEGIN -->";
        const string AiDescriptionEndMarker = "<!-- AI-DESCRIPTION:END -->";

        /// <summary>
        /// 生成项目向导 Markdown（&lt;ProjectName&gt;.md，与 .sp/.jsonc 同目录）。
        /// 内容来自内嵌模板 ProjectGuideTemplate.md；若目标已存在且带 AI 描述区标记，
        /// 重新生成时保留 AI 描述区内容，只刷新模板部分。生成与否由调用方门控。
        /// </summary>
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

                var template = LoadProjectGuideTemplate();
                if (string.IsNullOrEmpty(template))
                {
                    Log.AddProjectLog(LID.ProjectProjectClassExportProjectGuide,
                        "Project guide template not found (embedded resource: " + GuideTemplateNameTail + ")");
                    return;
                }

                // 已有 .md 时保留其 AI 描述区内容；无则填占位说明
                var aiDescription = ReadExistingAiDescription(mdPath);
                if (string.IsNullOrWhiteSpace(aiDescription))
                {
                    aiDescription = "> AI TODO: 此区由 AI 在项目向导生成后填写——描述项目用途、模块结构、关键入口等。\n"
                        + "> 重新生成本文件（CLI --guide 或 jsonc \"export\".\"guide\"）时，AI-DESCRIPTION 标记之间的内容会被保留。";
                }

                var content = template
                    .Replace("{{ProjectName}}", projectName)
                    .Replace("{{EntryFile}}", Path.GetFileName(spFilePath))
                    .Replace("{{ConfigFile}}", Path.GetFileName(jsoncPath) ?? string.Empty)
                    .Replace("{{AiDescription}}", aiDescription)
                    .Replace("{{GlobalDataKeys}}", BuildGlobalDataKeysSection());

                // 压缩占位段为空时残留的连续空行
                while (content.Contains("\n\n\n"))
                {
                    content = content.Replace("\n\n\n", "\n\n");
                }

                File.WriteAllText(mdPath, content, new UTF8Encoding(true));
            }
            catch (System.Exception ex)
            {
                Log.AddProjectLog(LID.ProjectProjectClassExportProjectGuide, "Export project guide markdown failed: " + ex.Message);
            }
        }

        /// <summary>加载内嵌的项目向导模板（EndsWith 匹配，兼容不同 RootNamespace 的资源名）。</summary>
        static string LoadProjectGuideTemplate()
        {
            var asm = typeof(ProjectClass).Assembly;
            foreach (var resName in asm.GetManifestResourceNames())
            {
                if (!resName.EndsWith(GuideTemplateNameTail, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream == null)
                {
                    continue;
                }
                using var reader = new StreamReader(stream, new UTF8Encoding(false));
                return reader.ReadToEnd().Replace("\r\n", "\n");
            }
            return null;
        }

        /// <summary>读取已有 .md 的 AI 描述区内容（两标记之间），文件或标记不存在时返回 null。</summary>
        static string ReadExistingAiDescription(string mdPath)
        {
            if (!File.Exists(mdPath))
            {
                return null;
            }

            var text = File.ReadAllText(mdPath).Replace("\r\n", "\n");
            var begin = text.IndexOf(AiDescriptionBeginMarker, System.StringComparison.Ordinal);
            if (begin < 0)
            {
                return null;
            }
            begin += AiDescriptionBeginMarker.Length;
            var end = text.IndexOf(AiDescriptionEndMarker, begin, System.StringComparison.Ordinal);
            if (end < 0 || end <= begin)
            {
                return null;
            }
            return text.Substring(begin, end - begin).Trim('\n', ' ', '\t');
        }

        /// <summary>构建当前 jsonc `global.data` 键清单段（无数据时返回空串）。</summary>
        static string BuildGlobalDataKeysSection()
        {
            var globalData = ProjectManager.config?.Global?.Data;
            if (globalData == null || globalData.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append("### Current `global.data` keys\n\n");
            foreach (var kv in globalData)
            {
                sb.Append("- `" + kv.Key + "` (`" + kv.Value.ValueKind + "`)\n");
            }
            sb.Append('\n');
            return sb.ToString();
        }
    }
}
