# SimpleLanguage 工程配置（JSONC）完全参考

> 适用范围：Front 工程编译配置（`<ProjectName>.jsonc`）。
> 解析真源：`source/Front/Project/ProjectJsoncLoader.cs`（全部键名与容错行为以该文件为准）、默认值 `source/Front/Project/ProjectConfig.cs`。
> 相关文档：`project_sp-guide.md`（`.sp` 入口与 `Project{}`/`_main_`/`_test_`）、`project-system-method-call-guide.md`（systemCalls / vmDlls / dllImports 绑定链）、`environment-guide.md`（`platform` 段全量关键字与封闭值域）、`static-if.md`（`global.macro` 与 `_before_()` 钩子）、`md/ai/EXPORT_PATHS.md`（导出目录与环境变量）、`md/ai/DEBUG_WORKFLOW.md`（DebugCode 快照语义）。

## 1. 配置文件命名与加载

- 工程入口文件：`<ProjectName>.sp`
- 配置文件：`<ProjectName>.jsonc`
- 二者必须同名并位于同一目录（目录下有多个 `.sp` 时默认取第一个，建议单目录单工程）。

加载链：`.sp` 路径 → `ProjectCompile.LoadProject` 同名替换扩展名 → `File.ReadAllText` → `ProjectJsoncLoader.FromJsonc()` → `ProjectManager.SetConfig` → `InitSelfModuleManager(project.name)` → `ProjectOutputEnvironment.ApplyFromConfig`（计算导出目录并写环境变量）。

## 2. 解析机制（必读）

1. **JSONC 语法**：`System.Text.Json` 解析，开启 `AllowTrailingCommas` + `CommentHandling.Skip` —— 支持 `//` 行注释与尾逗号。
2. **键名全部大小写敏感**：拼错或大小写不对即**静默忽略、落入默认值**，不报错。本文所有键名逐字来自解析器。
3. **类型容错**：值类型不符（如布尔位写了字符串）时**静默采用默认值**；块不是 Object 时整块忽略；数组条目不是 Object/String 时跳过。
4. **值的大小写**：
   - 不敏感 + 别名归一：`compile.target`（如 `x64`/`X64`）、`platform.require` 封闭值域字段（如 `win64`→`window`）；
   - **敏感**：`export.publicExport` / `export.format` / `plugins.<id>.onUnavailable` 的取值（非法值报 Error）。
5. **校验时机**：`compile.target` 在加载期即校验（Error 20033）；`platform` 值域 / `override` key / `aot.features` 与 `cpu` 一致性在 MetaCore 阶段 `ValidatePlatformConfig` 步骤校验（此时才计入 errorCount 中止编译）。
6. **数组条目丢弃规则**：`compileFiles.files[]` 缺 `path`、`references[]` 缺 `path`、`vmDlls[]` 缺 `project` 或 `name` 时整条丢弃；`systemCalls[]` 条目不校验 name 非空（缺省为空串仍会加入）。

## 3. 全字段骨架

以下骨架穷举了当前解析器读取的**全部**键（拷走按需删减；`//` 后为默认值/说明）：

```jsonc
{
  "project": {                        // 工程身份与版本
    "name": "MyProject",              // 缺省 ""
    "desc": "描述",                    // 缺省 ""（当前无编译期消费点）
    "mainVersion": 0,                  // → SLIR 版本主号，缺省 0
    "subVersion": 0,                   // → 次号，缺省 1
    "buildVersion": 0                  // → 修订号，缺省 0。注意没有 buildSubVersion 键（不解析）
  },
  "source": {                         // ⚠ 已解析、当前无下游消费（源码根实际由 .sp/文件清单决定）
    "root": ".",
    "entryFile": "Main.sl"
  },
  "compile": {
    "optimize": false,                // → module.json buildMode: true="release" / false="debug"
    "target": "x64",                  // AnyCPU(缺省)/x86/x64/arm64/wasm32，加载期校验
    "debug": true,                    // 缺省 true，影响 IR 调用生成路径
    "isUseForceSemiColonInLineEnd": true,   // ⚠ 已解析未消费
    "isForceUseClassKey": false,            // ⚠ 已解析未消费
    "isSupportDoublePlus": false,           // ⚠ 已解析未消费
    "requireSameNumericTypes": false,       // true=混合数值运算报错（byte+Int32 等）
    "isAllowSystemMethodCall": true         // false=禁止调用系统方法（println 等；int()/Int32() 等类型转换映射不受限），见 §4.3
  },
  "compileFiles": {
    "files": [                        // 显式编译文件清单
      { "path": "Main.sl",            // 必填；空/空白=条目丢弃
        "group": "core",              // 分组，配合 compileFilter.groups
        "tag": "base",                // 标签，配合 compileFilter.tags
        "ignore": false,             // true=不参与编译
        "priority": 0 }               // 编译顺序权重，越大越先编
    ]
  },
  "compileFilter": {
    "isAllGroup": false,              // true=放行所有 group
    "isAllTag": false,                // true=放行所有 tag
    "groups": [],                     // 启用分组白名单，空=不过滤
    "tags": []                        // 启用标签白名单，空=不过滤
  },
  "global": {
    "imports": ["Std.Console"],       // ⚠ 已解析未消费
    "data": {                         // 数据注入面（根->global->data，唯一位置，见 §4.6；根级 "data" 已废弃 LID 20041）
      "var1": 12,
      "arrvar1": [1, 2, 3],
      "vardata2": { "a": 10, "b": 20.1 }
    },
    "macro": { "platform": "Win32", "maxThreads": 8 },  // 编译期宏数据面（见 §4.7）
    "replace": { "DEBUG": "true" }    // ⚠ 已解析未消费（值为 string，非 string 静默跳过）
  },
  "struct": [                         // 命名空间/类型预声明树，⚠ 顶层数组（旧 {tree:[]} 形态无效）
    { "namespace": "MyProject", "children": [] },
    { "namespace": "Std", "children": [
        { "class": "Std.Console" },
        { "data": "Std.Config" },
        { "enum": "Std.Color" }
      ] }
  ],
  "references": [                     // 引用模块
    { "path": "../../out/export/Core",       // 必填；空=条目丢弃；相对本 jsonc 目录
      "uuid": "123123123213",               // 随 moduleReferences 导出
      "name": "Core.module.json" }          // 缺失时从已加载包回填
  ],
  "dllImports": [                      // FFI 导入表（旧顶层 "lib" 数组同构兼容）
    { "path": "../../x64/Debug/CLangdll.dll",  // 必填；空=条目丢弃
      "name": "CLangdll",                     // 库名
      "alias": "CLangdll",                    // 缺省=name；@DllImport("别名") 经此解析 path
      "static": "CLangdll",                   // 非空=cvm 加载期预载并注册进 FFI.StaticLibrary
      "functions": [
        { "name": "libaddfunc",               // 注入 global.<name> 的函数变量名；与 symbol 同时空才跳过
          "symbol": "simplelanguage_addtest", // DLL 内导出符号名
          "sig": "i32,i32->i32" }             // FFI 签名，缺省 "->void"
      ] }
  ],
  "vmDlls": [                          // 导出期用 MSBuild 编译并拷 DLL 到 module.json 同目录
    { "project": "../../../../project/cvm_math_lib/cvm_math_lib.vcxproj", // 必填（相对 jsonc 目录）
      "name": "math_lib.dll",           // 必填；产出 DLL 文件名
      "configuration": "Debug",        // MSBuild Configuration，缺省 "Debug"
      "platform": "x64" }               // MSBuild Platform，缺省 "x64"
  ],
  "systemCalls": [                     // 系统方法注册表（R7 四处同步的第一处）
    { "name": "SystemMathMat3Mul",      // 方法名
      "returnType": "void",             // 返回类型名（ResolveTypeName 支持的写法）
      "params": ["object", "object", "object"],  // 参数类型名数组
      "isVariadic": false,              // ⚠ 缺省是 true！固定参数务必显式写 false
      "cvmFunction": "math_lib.dll!mathvm_mat3f_mul",  // C VM 侧符号（见 §4.12 四种形态）
      "class": "" }                     // 归属类路径；空=全局裸调用
  ],
  "plugins": {                          // ⚠ 顶层对象（key=插件 id），不是数组
    "echo": {
      "name": "echo_test_plugin",       // 全名（诊断展示）
      "version": "0.1.0",              // ⚠ 已解析未消费（P1 起以 plugin.jsonc 为权威）
      "path": "simple_language_plugins/echo",  // ⚠ 已解析未消费（P0 仅存储）
      "enabled": true,                  // 缺省 true；false=整插件不参与
      "abi": 1,                         // C VM ABI 版本，缺省 1
      "prefix": "sl_csharp_mono_",      // 系统方法路由前缀，缺省 id+"_"，必须含尾下划线
      "onUnavailable": "warn",          // disable(缺省)/warn/error，平台不满足三级策略
      "platform": { "os": "windows", "arch": "x86_64" },  // 插件级平台条件（短写形）
      "lib": { "dir": "lib",            // 库根（相对插件目录），缺省 "lib"
               "select": "auto",       // auto=宿主 OS/arch 四级回退；或显式 "<os>-<arch>"
               "delay": "activate",     // activate(缺省,惰性)|load(装载即载)
               "files": ["*"] },        // 选中目录内的库文件，缺省 ["*"]
      "capabilities": [                  // 能力点直申清单
        { "type": "systemCall", "name": "echo" }  // type: signLabel/systemCall/device/backend/tool/custom
      ],
      "signLabelHandle": { "name": "signLabel" },  // 语法糖=capabilities[] 中 type="signLabel" 一项
      "references": [ "CsExtra/ExtraMathLib.dll" ],  // 额外程序集引用（语义由插件 frontend 自决）
      "sources": [ "CsExtra/SlExtraUtil.cs" ]        // 额外源码文件（同上）
    }
  },
  "export": {
    "moduleName": "MyProject",         // 模块名；导出目录名第一优先级
    "outputDir": "..\\..\\out\\export", // 导出根目录（相对 jsonc 目录），缺省 "Export/SLVMCode"
    "publicExport": "public",          // none/public(缺省)/extern，public 类导出门槛三态（见 §4.14）
    "format": "json",                  // json(缺省)/binary，导出格式二选一；CLI --format 可覆盖
    "binary": {                        // 仅 format="binary" 生效（SLB，见 csimple_lang/md/design/SLB_DESIGN.md）
      "compressed": true,              // 缺省 true，DataArea 整体 zlib 压缩
      "includeDebugInfo": true,        // 缺省 true，是否写 DEBUG 段
      "strictDigest": false,           // 缺省 false，P5 预留
      "exportJson": false },           // 缺省 false，binary 导出时附带写一份 <M>.module.json 调试副本（slb 必产物恒导出）；CLI --export-json/--no-export-json 覆盖
    "versionMain": 0,                  // 与 project.mainVersion 同字段（export 块后解析，同写时覆盖 project 块）
    "versionSub": 0,                   // 同上（project.subVersion）
    "versionPatch": 0,                 // 同上（project.buildVersion）
    "nativeDll": "",                   // 原生 DLL 文件名，写入 module.json，VM 同目录查找
    "guide": false,                    // 项目向导 Markdown 开关；CLI --guide/--no-guide 覆盖
    "includeMetadata": true,           // ⚠ 已解析未消费
    "stringPoolAsBlob": true,         // ⚠ 已解析未消费
    "aot": {                           // MLIR AOT 导出子块
      "enabled": true,                 // 缺省 true（false=全走解释执行）
      "buildDll": true,                // 缺省 true（false=只导出 aot.mlir）
      "triple": "",                    // llc -mtriple 目标三元组
      "cpu": "",                       // llc -mcpu 目标 CPU
      "features": "" },                // llc -mattr 特性表，如 "+sse4.2,+avx2"；与 platform.require.cpu 一致性校验
    "debugText": {                     // DebugCode 调试快照开关
      "outputDir": "DebugCode",        // 快照输出目录名，缺省 "DebugCode"
      "code": true, "token": true, "node": true,   // 六个快照开关均缺省 true
      "file": true, "meta": true, "ir": true }
  },
  "platform": {                        // 平台条件（也兼容顶层数组形态）；全量字段见 environment-guide.md
    "targets": ["windows"],            // 目标环境标签，⚠ 仅元数据不参与求值
    "when": "",                        // 表达式糖原文，⚠ 仅存储（v2 才校验；同给时以 require 为准）
    "fallbackHint": "",                 // 条件不满足时给用户的建议文案
    "require": {                        // 运行条件 → 编译为条件 AST 随 module.json 导出；字段间 AND、数组内 OR
      "os": "windows",                  // 封闭值域，大小写不敏感+别名归一（win64→window）
      "arch": "x86_64",
      "osVersion": { "min": "10.0" },
      "cpuCount": { "min": 4 },
      "memory": { "min": 512 },
      "runtime": { "min": "1.0.0" },
      "network": { "minLink": 1, "probe": true },   // probe=装载期在线探测开关（不进 AST）
      "cpu": { "all": ["sse4.2"], "minCores": 2, "topology": ["x86_64"] },
      "render": { "api": ["vulkan"], "minShaderModel": "6_0" },     // 缺省 optional=true
      "embedded": { "family": ["stm32"], "minFlashKB": 128, "minRamKB": 32 },  // 缺省 optional=true
      "lib":    [ { "name": "Std", "min": "1.0" } ],
      "sdk":    [ { "key": "windows-sdk", "min": "10" } ],          // 缺省 optional=true
      "ai":     [ { "name": "npu", "min": "2" } ],                 // 缺省 optional=true
      "script": [ { "name": "lua", "min": "5.4" } ],               // 缺省 optional=true
      "custom": [ { "name": "vendorFlag", "eq": "true" } ],
      "env":    [ { "name": "CONTAINER", "eq": "docker" } ],
      "device": [ { "kind": "gpu", "minCompute": 3 } ],             // 缺省 optional=true
      "environment": [ { "name": "serverless", "optional": true } ] // 缺省 optional=true
    },
    "variants": [                       // 多目标变体，数组顺序即优先级，CVM 取第一个通过的变体
      { "target": "linux-native", "require": { "os": "linux" } },
      { "target": "win-native",   "require": { "os": "window" } },
      { "target": "any-interp", "aot": "" }   // 无 require=兜底变体；aot=该变体 AOT 产物名（空=解释器）
    ],
    "override": {                       // jsonc 内嵌默认降级通道；key 形如 "<大类>.<成员>"/"<大类>"/裸成员名
      "console.enabled": true           // 值只接受 true/false/number/string；MetaCore 期校验+别名归一
    }
  }
}
```

## 4. 顶层块详解

### 4.1 `project` —— 工程身份与版本

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `name` | string | `""` | 模块自名（`InitSelfModuleManager` 用它注册本模块）；导出目录名第二优先级（`export.moduleName` > `project.name` > `.sp` 文件名） |
| `desc` | string | `""` | 项目描述（当前无编译期消费点） |
| `mainVersion` | int | 0 | SLIR 包版本主号，随 module.json 导出 |
| `subVersion` | int | 1 | 次版本号 |
| `buildVersion` | int | 0 | 修订号（写入 VersionPatch） |

> ⚠ 各工程模板里的 `"buildSubVersion"` **不被解析器读取**，写了无效（历史残留字段）。

### 4.2 `source` —— 已解析、当前无下游消费

`source.root`（缺省 `"source"`）与 `source.entryFile`（缺省 `"Main.sl"`）已被加载进配置对象，但当前无任何消费点——源码根实际由 `.sp` 与 `compileFiles.files` 清单决定。

### 4.3 `compile` —— 编译行为

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `optimize` | bool | false | → module.json `buildMode`：`true`="release"、`false`="debug"（CVM 注入 `Environment.current.build`）；优化级别另有 CLI `-O<n>` 写 `optimizeLevel`（无 jsonc 键） |
| `target` | string | `"AnyCPU"` | 编译目标架构，受控枚举 `AnyCPU/x86/x64/arm64/wasm32`；非法值 Error 20033；与 `platform.require.arch` 无交集仅 Warning 20034；值大小写不敏感并归一 |
| `debug` | bool | true | Debug 模式开关，消费于 IR 调用生成（`IRCall.cs`） |
| `isUseForceSemiColonInLineEnd` | bool | true | 已解析未消费（注意拼写：SemiColon 双大写、InLineEnd） |
| `isForceUseClassKey` | bool | false | 已解析未消费（键名含 "Class"，属性名是 `IsForceUseKeyClass`） |
| `isSupportDoublePlus` | bool | false | 已解析未消费 |
| `requireSameNumericTypes` | bool | false | 数值类型严格检查：true 时 `byte + Int32` 等混合数值运算报错（消费于 `MetaExpressOperator.cs`） |
| `isAllowSystemMethodCall` | bool | true | 系统方法调用开关：false 时 Front 语义解析层拒绝解析到 `systemCalls` 注册表声明的调用（Error 21473），仅约束当前工程自身源码；类型转换映射族（`int()`/`Int32()`/`sbyte()` 等别名写法及对应 `SystemConvert*` 目标声明）属语言级纯值转换，恒可用（消费于 `MetaCallNode.cs` 两处 systemCall 解析点） |

### 4.4 `compileFiles.files[]` —— 显式编译文件清单

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `path` | string | `""` | 源文件路径；**空/空白时整条丢弃** |
| `group` | string | `""` | 分组，配合 `compileFilter.groups` |
| `tag` | string | `""` | 标签，配合 `compileFilter.tags` |
| `ignore` | bool | false | true 时该文件不参与编译（常用于条件排除历史用例） |
| `priority` | int | 0 | 编译顺序权重，越大越先编 |

`sl new classfile` 会向该数组做**文本插入**注册新文件，要求数组结构存在且格式基本正常。

### 4.5 `compileFilter` —— 编译过滤

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `isAllGroup` | bool | false | true=放行所有 group |
| `isAllTag` | bool | false | true=放行所有 tag |
| `groups` | string[] | 空 | 启用分组白名单，空=不过滤 |
| `tags` | string[] | 空 | 启用标签白名单，空=不过滤 |

### 4.6 `global.data` —— 数据注入面（根->global->data）

- 位置：**`根 -> global -> data`**（jsonc 的 `"global"."data"`）为唯一数据注入面；编译期注入为 `Project` 类静态成员，运行期 `global.<name>` 读取。
- 类型映射：String→`const string`、整数→`Int32`、浮点→`Float64`、`true/false`→`Boolean`、`null`、Object→匿名 `MetaData` 对象（`global.obj.field` 链式访问）、Array→object 数组（`global.arr[i]`，支持嵌套）。
- ⚠ **根级 `"data"` 已废弃（2026-10-12）**：loader 检测到即输出迁移告警（LID 20041）并**忽略其内容**（不再参与注入），请把该块整体移入 `global.data`。
- `.sp` 的 `Project{}` 显式定义的同名成员优先于注入值；`global._inputArgs`（CLI `--` 之后传参）见 `project_arguments.md`。

### 4.7 `global` —— 全局面

| 键 | 类型 | 说明 |
|----|------|------|
| `imports` | string[] | 已解析未消费 |
| `data` | object | **数据注入面（唯一位置，见 §4.6）**：编译期注入为 Project 静态成员，运行期 `global.<name>` 读取；根级 `"data"` 已废弃（LID 20041） |
| `macro` | object | **编译期宏数据面**：值为布尔/数值/字符串，编译期定稿，运行期只读 `global.macro.X`。旧 `static if` 已废弃收编为 `@Exclude`（写 `static if` 报 LID 21467）。宏值只能在 `.sp` 的 `Project._before_(){}` 钩子中修改（见 `static-if.md`）。外部覆盖优先级：**jsonc 初值 < 环境变量 `SL_MACRO_<name>=<value>` < CLI `--macro name=value`（可重复）< `_before_()` 钩子** |
| `replace` | object | 已解析未消费（值必须是 string，非 string 静默跳过；历史 DEBUG→true 替换面） |

### 4.8 `struct[]` —— 命名空间/类型预声明树

⚠ 顶层是**数组**（旧文档的 `"struct": { "tree": [...] }` 对象形态**无效**，解析器只认数组）。

每条目可用键：

| 键 | 类型 | 说明 |
|----|------|------|
| `namespace` | string | 声明命名空间节点（支持点分路径，中间段自动 Namespace） |
| `class` | string | 声明类节点 |
| `data` | string | 声明 data 节点 |
| `enum` | string | 声明 enum 节点 |
| `children` | array | 嵌套子节点（递归同构） |

一条目可同时含 `namespace` + `class`（依次登记，后者挂前者下）。预声明树在编译前注册进 ModuleManager，供 `.sl` 引用尚未编译声明的类路径。

### 4.9 `references[]` —— 引用模块

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `path` | string | `""` | 引用模块路径（相对 jsonc 目录）；**空则条目丢弃**；loader 按 module.json / plugin.jsonc 三级回退加载 |
| `uuid` | string | `""` | 随导出 `moduleReferences[].uuid` |
| `name` | string | `""` | 引用模块名（缺失时从已加载包回填） |

引用插件（plugin.jsonc）时，其 `refModule` 段声明的模块与 systemCalls 会在 RefModule 阶段自动加载，无需在本工程重复声明。

⚠ 引用模块未在本段声明、或声明了但尚未完成编译导出（其 `export.outputDir` 下无 `*.module.json`）时，源码中对该模块的 `import` 会报 **Error**（「没有找到 import 模块」）并**阻断编译、不导出 module.json**（2026-10-13 起，详见 `md/syntax/namespace.md`「import 导入失败的处理」）——请先编译被引用模块再编译本工程。

### 4.10 `dllImports[]`（兼容旧 `lib[]`）—— FFI 导入表

顶层 `dllImports` 与旧顶层 `lib` 数组同构兼容（同一解析函数）。Front 侧合成持有类 `___DllImportGlobal___` + `global.dllImport.<alias>` 成员 + `global.<name>` 静态函数变量（经 `FFI.StaticLibrary.bindFunction` 程序化构造），并随 module.json `dllImports` 导出供引用方复用。

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `path` | string | `""` | 外部 DLL 路径；**空则条目丢弃**；运行期相对进程 CWD 解析 |
| `name` | string | `""` | 库名 |
| `alias` | string | =name | 别名：`@DllImport("别名",...)` 三参形式按它解析 path |
| `static` | string | `""` | 静态绑定名：非空时 cvm 加载模块即预载并注册进 `FFI.StaticLibrary` |
| `functions[]` | array | 空 | 库函数清单 |
| `functions[].name` | string | `""` | 函数变量名（注入 `global.<name>`）；name 或 symbol 空=跳过 |
| `functions[].symbol` | string | `""` | DLL 内导出符号名 |
| `functions[].sig` | string | `"->void"` | FFI 签名（如 `"i32,i32->i32"`） |

实际范例见 `source/Front/Lib/Math/Math.jsonc`（math_lib 静态绑定）与 `test/SpecialTest/ProjectTest.jsonc`（CLangdll）。

### 4.11 `vmDlls[]` —— 导出期构建的 VM DLL

导出时 Front 用 MSBuild 编译 `project` 指向的 VS 工程，并把产出 DLL 拷到 module.json 同目录；随 module.json `vmDllImports` 导出，cvm 按 package 目录预加载，供 `systemCalls` 的 `"DllName!symbol"` 形式解析。

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `project` | string | `""` | VS 工程（.vcxproj）路径，相对 jsonc 目录；**空则条目丢弃** |
| `name` | string | `""` | 产出 DLL 文件名；**空则条目丢弃**；须与 systemCalls 里 `DllName!` 前缀一致 |
| `configuration` | string | `"Debug"` | MSBuild Configuration |
| `platform` | string | `"x64"` | MSBuild Platform |

### 4.12 `systemCalls[]` —— 系统方法注册表

R7 四处同步的第一处（`.sl` 声明 + jsonc 注册 + C VM 实现 + `ESystemMethodCall`）。

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `name` | string | `""` | 系统方法名（如 `println`） |
| `returnType` | string | `""` | 返回类型名（须是 ResolveTypeName 支持的写法，如 `Float64`；`double` 不识别） |
| `params` | string[] | 空 | 参数类型名数组；非字符串元素静默置 `""` |
| `isVariadic` | bool | **true** | ⚠ 缺省是真！固定参数方法务必显式写 `false` |
| `cvmFunction` | string | `""` | C VM 侧实现符号 |
| `class` | string | `""` | 归属类路径（类路径形态调用 `SLang.Plugin.<Class>.<Method>`）；空=全局裸调用 |

`cvmFunction` 四种形态：

1. **C VM 内建符号名**：如 `"vm_sys_ptr_alloc"`（C VM 编译进 exe/dll 的 builtin）；
2. **`"DllName!symbol"`**：DllName 须与 `vmDlls[].name` 一致，cvm 预载 DLL 后按符号解析派发（如 `"math_lib.dll!mathvm_mat3f_mul"`）；
3. **`"plugin:<capability name>"`**：插件系统方法，装配期 CVM 经 plugin registry 隐式 load+enter 插件后直注册（如 `"plugin:echo"`）；
4. **`"atsign:label"`**：`@<tag>(){}` 内联块哨兵（`AtSignLabelCall` / `AtSignLabelCallVoid`），CVM 经模块 `atSignLabel[]` 绑定 entryIndex 走插件 labelExec capability。

### 4.13 `plugins` —— 插件声明

⚠ 顶层是**对象**（key=插件 id），不是数组。总纲见 `csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md`。

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `<id>` | object key | — | 插件 id 即对象键名 |
| `name` | string | `""` | 插件全名（诊断展示） |
| `version` | string | `""` | 期望版本（P0 骨架仅存储，P1 起以插件目录 plugin.jsonc 为权威） |
| `path` | string | `""` | 插件目录（P0 骨架仅存储） |
| `enabled` | bool | true | 总开关（false=导出 enabled=false，整插件不参与） |
| `abi` | int | 1 | C VM ABI 版本 |
| `prefix` | string | `id + "_"` | 系统方法路由前缀，**必须含尾下划线**（CVM 直接拼接符号名，缺省值不对时须显式写，如 `"sl_csharp_mono_"`） |
| `onUnavailable` | string | `"disable"` | 平台不满足三级策略：`disable`=摘能力静默降级 / `warn`=降级+诊断 / `error`=拒绝装载；非法值报 Error 并回退 disable |
| `platform` | object | — | 插件级平台条件**短写形**（字段与模块级 `require` 同构，String=eq 简写、数组=any 集合）；编译为条件 AST 随包导出，CVM 装配期求值 |
| `lib` | string 或 object | `""` | **字符串**=库相对包路径直给（原位加载，不求值不拷贝，如 csharp_mono/java_hotspot）；**对象**=LibSpec 四级目录回退求值（`lib/<os>-<arch>/` → `<os>/` → `<arch>/` → `lib/`）并拷贝写回包内 |
| `lib.dir` | string | `"lib"` | 库根（相对插件目录） |
| `lib.select` | string | `"auto"` | auto=宿主 OS/arch 四级回退；或显式 `"<os>-<arch>"` |
| `lib.delay` | string | `"activate"` | 加载时机：`activate`（惰性激活）\| `load`（装载即载） |
| `lib.files` | string[] | `["*"]` | 选中目录内的库文件（平台后缀过滤、字典序） |
| `capabilities[]` | array | 空 | 能力点直申清单 |
| `capabilities[].type` | string | `""` | 能力点类型：`signLabel`/`systemCall`/`device`/`backend`/`tool`/`custom` |
| `capabilities[].name` | string | `""` | 能力点名（如 `@csharp{}` 的 "csharp"）；**空则跳过** |
| `signLabelHandle` | object | — | 语法糖：等价 capabilities[] 中 `type="signLabel"` 一项 |
| `signLabelHandle.name` | string | `""` | 能力点名 |
| `references[]` | string[] | 空 | 额外程序集引用（相对 jsonc 目录或绝对路径；语义由插件 frontend 自决——csharp_mono 为块内 C# 引用的 managed dll） |
| `sources[]` | string[] | 空 | 额外源码文件（csharp_mono=与 @csharp_mono(){} 块体同批合并编译的 .cs） |

实际范例见 `test/SpecialTest/ProjectTest.jsonc` 的 `plugins` 段（echo 对象形 lib / csharp_mono、java_hotspot 字符串形 lib）。

### 4.14 `export` —— 导出控制

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `moduleName` | string | `""` | 导出模块名（生成 `<moduleName>.module.json`）；导出目录名第一优先级 |
| `outputDir` | string | `"Export/SLVMCode"` | 导出根目录（相对 jsonc 目录解析）；驱动 Logs/DebugCode 目录创建与进程内状态派生（`ProjectOutputEnvironment.Current*`；Front 不再写环境变量，`SIMPLELANG_*` 仅作外部只读覆盖） |
| `publicExport` | string | `"public"` | public 类导出门槛三态：`none`/`public`/`extern`（见下表）；非法值 Error |
| `format` | string | `"json"` | `"json"`=SLIR `<M>.module.json`（现状）；`"binary"`=SLB `<M>.module.slb`（规范 `csimple_lang/md/design/SLB_DESIGN.md`）；一次导出只产一个文件（唯一例外见 `binary.exportJson`）；非法值 Error 20039；CLI `--format` 覆盖 |
| `binary` | object | — | SLB 选项（仅 format="binary" 生效）：`compressed`（默认 true）、`includeDebugInfo`（默认 true）、`strictDigest`（默认 false，P5 预留）、`exportJson`（默认 false） |
| `binary.exportJson` | bool | `false` | binary 导出时是否**附带**写一份 `<M>.module.json` 调试副本（slb 必产物恒导出，副本仅供人工排查，不构成双格式模式）；CLI `--export-json`/`--no-export-json` 覆盖；仅磁盘导出生效（内存模式不落盘） |
| `versionMain` / `versionSub` / `versionPatch` | int | 0 / 1 / 0 | 与 `project.mainVersion/subVersion/buildVersion` 同字段；**export 块后解析，两处同写时以 export 块为准** |
| `nativeDll` | string | `""` | 原生 DLL 文件名，写入 module.json，VM 同目录查找 |
| `guide` | bool | false | 项目向导 Markdown 开关：置 true 编译时在 `.sp`/`.jsonc` 同目录生成 `<ProjectName>.md`（模板 `source/Front/Project/Templates/ProjectGuideTemplate.md`，含 `AI-DESCRIPTION:BEGIN/END` 标记区，重生成时保留区内 AI 描述）；CLI `--guide`/`--no-guide` 覆盖（优先级 CLI > jsonc > 默认 false） |
| `includeMetadata` | bool | true | 已解析未消费 |
| `stringPoolAsBlob` | bool | true | 已解析未消费 |
| `aot` | object | — | MLIR AOT 导出子块（详见 `source/Front/Export/MLIR/MLIR_AOT_实现与待办.md`） |
| `aot.enabled` | bool | true | AOT 总开关（false=全走解释执行） |
| `aot.buildDll` | bool | true | stage-3 dll 构建（false=只导出 aot.mlir） |
| `aot.triple` | string | `""` | llc `-mtriple` 目标三元组 |
| `aot.cpu` | string | `""` | llc `-mcpu` 目标 CPU |
| `aot.features` | string | `""` | llc `-mattr` 特性表（如 `"+avx2,+fma"`、`"-name"`=禁用）；非空时与 `platform.require.cpu` 一致性校验（Error 20037） |
| `debugText` | object | — | DebugCode 调试快照开关（语义见 `md/ai/DEBUG_WORKFLOW.md`） |
| `debugText.outputDir` | string | `"DebugCode"` | 快照输出目录名 |
| `debugText.code/token/node/file/meta/ir` | bool | 全部 true | Code/Token/Node/File/Meta/IR.txt 六个快照开关 |

**publicExport 三态**（源码中类级显式导出标记为 `extern` 关键字；`publicExport` 只决定**未显式标记**的类的导出）：

| 取值 | extern 标记类 | public / 无标记类 | private / protected 类 |
|------|--------------|-------------------|------------------------|
| `"none"` | 导出（permission 记 Export） | **不导出** | 不导出 |
| `"public"`（默认） | 导出（permission 记 Export） | 照常导出（permission 记 Public） | 不导出 |
| `"extern"` | 导出（permission 记 Export） | 照常导出，且 permission 记 Export（对外表现为显式导出） | 不导出 |

判定优先级：`extern` 恒导出 > `private`/`protected` 恒不导出 > 其余按档位过滤。探针用例：`test/Other/ExportPermTest/`。

> 注意：旧语法 `export class` / `export data` / `export enum` 的 `export` 关键字已移除权限语义（映射为不导出），类级显式导出统一用 `extern` 关键字。

### 4.15 `platform` —— 平台条件

顶层支持**对象**（推荐）与**数组**（兼容多段形态）两种写法；未声明 `platform` 时导出 null=恒通过。全量封闭值域、别名表、override 大类前缀见 `environment-guide.md`；此处为配置面速览。

| 键 | 类型 | 缺省 | 说明 |
|----|------|------|------|
| `targets` | string[] | 空 | 目标环境标签（小写），**仅元数据，不参与条件求值** |
| `when` | string | `""` | 表达式糖原文，**仅存储**（v2 才做与 require 的一致性校验；同给时以 require 为准） |
| `fallbackHint` | string | `""` | 条件不满足时给用户的建议文案（随导出、诊断输出） |
| `require` | object | null | 运行条件，编译为条件 AST 随 module.json `platform` 段导出，CVM 装载期 `sl_require_check` 求值 |
| `variants[]` | array | 空 | 多目标变体，**数组顺序即优先级**，CVM 取第一个通过的变体 |
| `variants[].target` | string | `""` | 变体目标标签（仅诊断展示） |
| `variants[].aot` | string | `""` | 该变体的 AOT 产物文件名（空=解释器兜底变体；选中时覆盖模块级 aot.dll） |
| `variants[].require` | object | null | 同 `require` 字段表 |
| `override` | object | 空 | jsonc 内嵌默认降级通道：key 三形态 `<大类>.<成员>` / `<大类>` / 裸成员名（MetaCore 期校验+别名归一，21 个大类前缀）；**值只接受 true/false/number/string**（null/array/object 成员跳过） |

`require` 字段表（**字段间默认 AND、数组内默认 OR**；未知字段告警忽略；封闭值域大小写不敏感+别名归一）：

| 字段 | 值形态 | optional 缺省 | 说明 |
|------|--------|---------------|------|
| `os` | string=eq 简写 / array=any 集合 / `{eq,ne,any,optional}` | false | 操作系统（封闭值域） |
| `arch` | 同上 | false | 架构（封闭值域） |
| `osVersion` | `{min,eq,ne,optional}`（min→ge） | false | 系统版本（开放值域） |
| `cpuCount` | 同上 | false | CPU 数 |
| `memory` | 同上 | false | 内存 |
| `runtime` | 同上 | false | 运行时版本 |
| `network` | `{minLink,probe,optional}`（probe 是行为开关，不进 AST） | false | 网络（minLink 产 Link 定序 atom） |
| `cpu` | `{all,any,minCores,topology:{any,optional},optional}` | false | ISA 特性集合 all/any、最少核数、CPU 型号集合 |
| `render` | `{api:{any,optional},minShaderModel,optional}` | **true** | 图形 API + 最低 shader model（"6_0" 容错补 sm_ 前缀） |
| `embedded` | `{family:{any,optional},minFlashKB,minRamKB,optional}` | **true** | MCU 家族 + 最低 Flash/Ram KB |
| `lib` | array of `{name\|key,min,optional}`（min→ge，无 min→exists） | false | 库版本 |
| `sdk` | 同 lib 形态 | **true** | SDK 版本 |
| `ai` | 同 lib 形态（Key 封闭值域校验） | **true** | AI 能力 |
| `script` | 同 lib 形态 | **true** | 脚本引擎 |
| `custom` | array of `{name\|key,eq,optional}`（无 eq→exists） | false | 自定义条件 |
| `env` | 同 custom 形态 | false | 环境变量 |
| `device` | array of `{kind\|name,minCompute,optional}` | **true** | 设备（minCompute 数字→ge） |
| `environment` | 同 device 形态 | **true** | 环境类别 |

条件 AST 的 kind/cmp/op 常量与 C 侧 `csimple_lang/src/vm/platform/sl_requirement.c` 逐项对称。

范例：`test/SpecialTest/PlatformAotPosTest/`（require.cpu + aot.features 一致性）、`test/Other/PlatformVariantTest/`（variants 有序选择）、`test/Other/PlatformOverrideNegTest/` 等负例集。

## 5. 已解析但当前无下游消费的键（配置不生效排查表）

以下键会被加载进配置对象，但全仓无任何消费点——**配了也不会改变行为**：

| 键 | 备注 |
|----|------|
| `source.root` / `source.entryFile` | 源码根实际由 .sp 与文件清单决定 |
| `compile.isUseForceSemiColonInLineEnd` / `isForceUseClassKey` / `isSupportDoublePlus` | 语法开关骨架，尚未接线 |
| `global.imports` / `global.replace` | 历史面（DEBUG→true 替换已废弃） |
| `export.stringPoolAsBlob` / `export.includeMetadata` | 保留字段 |
| `project.desc` | 仅展示 |
| `platform.when` / `platform.targets` | when 仅存储；targets 仅元数据 |
| `plugins.<id>.version` / `plugins.<id>.path` | P0 骨架仅存储，P1 起以 plugin.jsonc 为权威 |

另有**根本不被解析**的键（写了即静默丢弃）：`project.buildSubVersion`、旧文档形态的 `struct.tree`、`importFiles`（Core.jsonc 中残留、loader 未消费）。

## 6. CLI 与环境变量联动

| 配置面 | jsonc 键 | 覆盖源 |
|--------|----------|--------|
| 导出格式 | `export.format` | CLI `--format json\|binary` |
| 二进制 json 调试副本 | `export.binary.exportJson` | CLI `--export-json` / `--no-export-json`（优先级 CLI > jsonc > 默认 false；仅 `--format binary` 磁盘导出生效） |
| 项目向导 | `export.guide` | CLI `--guide` / `--no-guide`（优先级 CLI > jsonc > 默认 false） |
| 优化级别 | （无 jsonc 键） | CLI `-O<n>` → module.json `optimizeLevel`（jsonc 里只有布尔 `compile.optimize`→buildMode） |
| 宏 | `global.macro` | 环境变量 `SL_MACRO_*` < CLI `--macro name=value`（可重复）< `Project._before_()` 钩子 |
| 输出目录 | `export.outputDir` + 模块名 | 由 jsonc 派生存入进程内状态（`ProjectOutputEnvironment.Current*`，Front 不再写环境变量）；`SIMPLELANG_EXPORT_OUTDIR` / `SIMPLELANG_LOGS_DIR` / `SIMPLELANG_DEBUGCODE_ROOT` 仅作外部只读覆盖。`SIMPLELANG_VM_RESULT_DIR` 为过期条目（当前代码无实现） |
| 导出与否 | — | CLI 必须带 `-e ir` 才执行导出阶段，缺省只编到 IR、module.json 不重写（dotnet 退出码仍 0，成功判据=module.json 文件 mtime 更新） |

常用命令：

```powershell
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p <.sp 路径> -e ir
# CLI 动词：compile | build | run | new | version | help（缩写 c/b/r/e…）
# 常用选项：-p|--project  -o|--output  -e ir  -t|--test  --release  --debug  --verbose  --no-banner  --token
```

### `sl new project -p [path] [name]`

生成 `<name>.sp` + `<name>.jsonc` + `<name>.md`（项目向导；`--no-guide` 跳过）。生成的 `.jsonc` 不含 `"export"."guide"`，后续编译缺省不再生成/覆盖该 `.md`；需刷新模板（保留 AI 描述区）时用 `sl c -p <project> --guide` 或 jsonc 配 `"guide": true`。

### `sl new classfile [filename]`

创建 `*.sl` 并以文本插入方式注册进 `compileFiles.files`。

## 7. 易错点速查

1. **键名大小写敏感**：`systemCalls`（非 systemcalls）、`compileFiles`、`compileFilter`、`dllImports`、`vmDlls`、`isVariadic`、`isAllGroup`/`isAllTag`、`mainVersion`/`subVersion`/`buildVersion`、`publicExport`、`cvmFunction`、`signLabelHandle`、`onUnavailable`、`fallbackHint`、`minShaderModel`/`minFlashKB`/`minRamKB`/`minCores`/`minCompute`/`minLink`——拼错即静默忽略。排查"配置不生效"先查拼写与类型。
2. `systemCalls[].isVariadic` **缺省 true**，与其他 bool 缺省 false 相反；固定参数方法必须显式 `false`。
3. `plugins` 顶层是**对象**（key=插件 id）；`struct`/`references`/`dllImports`/`vmDlls`/`systemCalls`/`capabilities` 是**数组**；`platform` 兼容对象与数组两种形态。
4. `plugins.<id>.prefix` 缺省 `id + "_"` 且**必须含尾下划线**；实际符号前缀与缺省不符时（如 `sl_csharp_mono_`）必须显式写。
5. `struct` 旧文档的 `"struct": { "tree": [...] }` 形态**无效**，当前只认顶层数组。
6. `project.buildSubVersion` 不被解析（模板残留）。
7. 版本号两处同写（`project.*` 与 `export.version*`）时以 **export 块为准**（后解析覆盖）。
8. `dllImports[].alias` 缺省取 `name`；`functions[].sig` 缺省 `"->void"`。
9. 类型不符一律**静默回退默认值**，不报错。
10. `compile.target` 是唯一加载期即校验的键（Error 20033）；platform 相关校验统一延后到 MetaCore 阶段。
11. 导出目录名优先级：`export.moduleName` > `project.name` > `.sp` 文件名。
12. 路径口径：`references[].path`、`vmDlls[].project`、`export.outputDir`、`plugins.references[]`、对象形 `lib` 均相对**本 jsonc 所在目录**解析；`dllImports[].path` 运行期相对**进程 CWD** 解析。
13. 根级 `"data"` 已废弃：不再被读取，检测到即告警（LID 20041）；数据注入只认 `根->global->data`。

## 8. 历史迁移备注

- 配置已从 `*.toml` 全面迁移为 `*.jsonc`；`ProjectTomlLoader` 不再参与流程，加载统一走 `ProjectJsoncLoader`。新工程统一使用 jsonc，不再新增 toml。
- 根级 `"data"` 块已废弃（2026-10-12）：数据注入面统一为 `根->global->data`，loader 检测到根级 `"data"` 即告警（LID 20041）并忽略其内容；全仓 jsonc 已完成迁移（有内容的根级块移入 `global.data`，空根级块删除，`linux-dist/` 分发快照除外）。
- `importFiles` 字段在 `Core.jsonc` 保留，但 loader 未消费（如需生效需补 loader 映射）。
- 工程目录存在多个 `.sp` 时默认取第一个，建议单目录单工程。

---

**本文档维护约定**：`ProjectJsoncLoader.cs` / `ProjectConfig.cs` 键名或默认值变化时，须同步本文 §3 骨架与对应小节（R14）。
