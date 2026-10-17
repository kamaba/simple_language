# SimpleLanguage 插件标准模板（cvm / frontend / slang 三目录）

> 蓝本：`simple_language_plugins/csharp_mono`（首个按本模板落地的完整实例，2026-09-26 全链路验收）；
> 第二实例：`simple_language_plugins/java_hotspot`（HotSpot JVM via JNI，2026-09-29 代码侧 P3 收官——「接第二种运行时」的同构验证：JNI 233 槽表自带声明不依赖 JDK 头、javac 探测 + 无 JDK 降级、build 脚本含 slang refModule 编译步；设计 = `csimple_lang/md/design/HOTSPOT_JNI_INTEGRATION_DESIGN.md`）；
> 最小范本：`simple_language_plugins/echo`（六件套单文件 C99，§8.1 骨架的提取源）。
> 设计总纲：`csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md`（Part I 插件系统 / Part II `@tag{}` AtSignLabel 详规）。
> 适用：给 SL 接入**一种语言 / 一个运行时 / 一台设备 / 一类工具**——新增插件目录即可，编译器与 VM 零改动。
> 新写插件直接跳 **§8**（接口真源索引 + 六件套 C 骨架 + frontend 双契约骨架 + slang 样例 + 从零生成流程），再回 §2 查字段、§5 对 checklist、§7 避坑。

## 1. 三目录布局（目录名固定，Front / CVM 两侧按此解析）

```
simple_language_plugins/<plugin_id>/        ← 推荐目录名 = plugin.id（路由按清单 id，非目录名）
├── plugin.jsonc          ★ 权威清单（Front 编译期 + CVM 装配期都读它）
├── README.md             插件说明（可选 README.en.md）
├── cvm/                  C99 侧全部——编成 dll 给 CVM 运行期加载
│   ├── src/<name>/       源码（六件套 ABI 入口 + 插件逻辑；平台代码禁入，走 OS 抽象层）
│   ├── include/          对外头文件（ABI 头 / vendored 三方头，随包分发）
│   ├── lib/<os>-<arch>/  ★ 平台产物（插件 dll + 同目录依赖树；CVM 从这里原位加载）
│   ├── probe/            探针工程（可选，验证外部运行时；插件化落地后可删）
│   ├── <name>.vcxproj    MSVC 工程（挂 simple_language\SimpleLanguage.sln「plugins」文件夹，仅 x64）
│   └── build-<name>.ps1  ★ 一键构建（cvm C 源 + frontend .NET 工程 + 部署到测试模块包）
├── frontend/             Front 层 .NET 工程——编成 dll 供 Front AtSignLabel 反射转调
│   ├── <Name>Frontend.csproj  netstandard2.0，OutputPath="."（产物落本目录）
│   ├── SLLabelParser.cs       Parse 契约：`ParseLabel(requestJson)` 块体解析
│   ├── SLLabelBuilder.cs      Build 契约：`BuildLabels(requestJson)` 合并编译 + 部署
│   └── MiniJson.cs            单 JSON 序列化（零依赖，不引第三方库）
└── slang/                ref module——绑定在插件上的 SL 引用模块
    ├── <Module>.jsonc    ★ 系统方法声明（systemCalls[].cvmFunction = "plugin:<capability>"）
    ├── <Module>.sl       SL 侧声明源码（不写桩方法体）
    ├── <Module>.sp       自测工程入口（_main_ 空壳）
    └── <Module>.md       说明
```

| 目录 | 工具链 | 消费方 | 职责 |
|------|--------|--------|------|
| `cvm/` | MSVC cl / clang → dll | CVM 运行期 | 六件套 ABI + capability 实现（systemCall / labelExec） |
| `frontend/` | dotnet（csc）→ dll | Front 编译期 | `@<tag>(){}` 块体解析与目标产物构建（Parse/Build 单 JSON 契约） |
| `slang/` | Front 导出（`e -p`）→ module.json | 引用方工程 | SL 侧类型与方法声明，调用方经 refModule 自动引入 |

## 2. plugin.jsonc 关键字段

```jsonc
{
  "plugin":   { "id": "<plugin_id>", "name": "...", "version": "0.1.0", "vendor": "...", "description": "..." },
  "abi":      { "cvm": 1, "frontend": 1 },
  "entry": {
    "lib":    { "windows-x64": "<name>.dll" },   // 相对 cvm/lib/<os>-<arch>/ 的文件名
    "prefix": "sl_<name>_"                       // ⚠ 必须含尾下划线（CVM resolve_symbol 直接 prefix+suffix 拼接）
  },
  // —— signLabel（@<tag>(){} 内联块）反射入口，三键都指向 frontend/ 产物程序集 ——
  "frontendLibs":  [ "<Name>Frontend.dll" ],      // 程序集位于 frontend/（Front: pluginRoot/frontend/<frontendLibs[0]>）
  "parserType":    "<Name>Frontend.SLLabelParser",     // Parse 契约：ParseLabel(requestJson)
  "builderType":   "<Name>Frontend.SLLabelBuilder",    // Build 契约宿主类型
  "builderMethod": "BuildLabels",                      // Build 契约：BuildLabels(requestJson)
  "capabilities": [
    { "type": "systemCall", "name": "<cap>" },    // cvmFunction = "plugin:<cap>" 的路由名
    { "type": "signLabel",  "name": "signLabel" } // 名固定；每插件至多一个 labelExec 入口
  ],
  "refModule": { "path": "slang", "channelClassPath": "<Ns>.<Module>" },
  // path：三目录布局下固定指向 slang/；无 refModule 可整段省略。
  // channelClassPath：@<tag>(){} 通道包装函数宿主类全名（带 <- 通道的块必填，
  // 缺失报 LID 20058）——Front 脱糖按本路径生成 ChannelIn/Out 调用
  "platform":  { "os": [ "windows" ], "arch": [ "x64" ] },
  "onUnavailable": "disable"                      // disable / warn / error（平台不满足时降级）
}
```

| 字段 | 谁读 | 说明 |
|------|------|------|
| `entry.lib` + `entry.prefix` | CVM 装配期 | dll 文件名 + 六件套符号前缀；六件套 = `{prefix}{abi_version,get_info,enter,get_capability,exit,unload}` |
| `frontendLibs` / `parserType` / `builderType` / `builderMethod` | Front 编译期 | `PluginFrontendParser` 按 `.sl` 源文件上溯定位本清单后 `Assembly.LoadFrom` 反射转调 |
| `capabilities[]` | 两侧 | systemCall 名即 `cvmFunction` 的 `plugin:<name>`；signLabel 承接 `@<tag>(){}` |
| `refModule.path` | Front RefModule 阶段 | 自动把 slang/ 模块追加为引用方工程 reference，其 systemCalls 随之注册 |
| `refModule.channelClassPath` | Front 脱糖 | `@<tag>(){}` 通道包装函数宿主类全名——Front 按 `<channelClassPath>.ChannelIn/Out<Kind>( entry, ... )` 脱糖（`<-` 赋值不写死进 opcode 124）；带通道的块缺失本字段 → LID 20058 |
| `platform` / `onUnavailable` | 两侧 | 编译为条件 AST 随 module.json 导出，CVM 装配期求值，未通过按策略降级 |

## 3. 各目录内部要点

### cvm/（C99）
- **六件套 ABI**：`{prefix}abi_version/get_info/enter/get_capability/exit/unload`——CVM 插件 registry 按此生命周期管理；符号导出用 `__declspec(dllexport)`（或 def 文件）。
- **systemCall capability**：实现一个 exec 入口（栈协议：VM 栈 ↔ 插件编组，参考 `cvm_csharp_mono.c` 的 `csharp_exec`）。
- **labelExec capability**：`@<tag>(){}` 运行期接收端（ABI 见 `csimple_lang/src/vm/plugin/sl_plugin_abi.h`，`SLLabelValue[]` 编组）。
- 平台代码禁入源文件（同 C VM 的 R3 纪律）；外部运行时经 `LoadLibrary`+`GetProcAddress` 函数指针表动态解析，不 .lib 链接。
- 产物与同目录依赖（运行时 dll / BCL 等）整体落 `lib/<os>-<arch>/`，**原位加载**，标准安装语义（prefix = dll 目录）。

### frontend/（.NET，netstandard2.0）
- Front 只转调不解析——**全部块体解析 + 目标语言编译部署逻辑归插件**。块体三分：头区入通道行与尾行出通道由 Front 初判（双出通道 → 20056），中间**代码段是目标语言原文，Front 零处理整段透传**，插件自决整编（段首指令行吸收、代码区 `<-`/`$` 前缀/指令行报错、字符串/注释内 `<-` 放行）：
  - **Parse 契约** `ParseLabel(requestJson)`：请求 `{label, entryName, paramsText, bodyText, headLineCount, tailLineCount, inChannels, outChannel}` → 响应 `{ok, error, errorLine, source, dll, entry, entryMethod, labelParams}`。
  - **Build 契约** `BuildLabels(requestJson)`：请求 `{label, outDir, libDir, entries[{entryName, source}]}` → 响应 `{ok, kind, error, dllPath, count}`（kind 五态 success/notFound/buildFailed/deployFailed/contract → Front 分流 LID 22135~22138）。
- `OutputPath="."`：产物 dll 落 frontend/ 本目录，与 `frontendLibs[0]` 对应。
- 只接**块原文 + 参数 JSON**，不碰 SL 的 AST/符号表（与语义层解耦，升级不碎）。

### slang/（ref module）
- `<Module>.jsonc` 的 `systemCalls[]` 带 `"class": "<Ns>.<Module>"` 归属标记 → 调用方以 `<Ns>.<Module>.<Method>(...)` 类路径调用（全局裸调用亦可）。
- `cvmFunction = "plugin:<capability name>"`：CVM 装配期经 plugin registry 隐式 load+enter 插件后直注册 exec 入口。
- **通道包装函数（`@<tag>(){}` 带通道的插件必须提供，函数名固定）**：
  `ChannelInInt( entry, value )` / `ChannelInString( entry, value )`（void）+
  `ChannelOutInt( entry )`（ret int）+ `ChannelOutString( entry )`（ret string）——
  函数体内部转调 Core 域系统方法 `AtSignChannelIn` / `AtSignChannelOutInt` /
  `AtSignChannelOutString`（Core.jsonc 声明，CVM `sl_atsign_channel` 会话管理器实现，
  执行环境三态路由 + 出通道 int/string 判型）。Front 脱糖按
  `refModule.channelClassPath` 生成对这组函数的调用，`<-` 赋值不写死进 opcode 124；
  范本见 `csharp_mono/slang/CSharpMono.sl`。
- `references` 指向 Core/Std 等编译包（`../../../simple_language/out/export/<模块>`，相对 jsonc 目录）；`export.outputDir` 同理指向工作区 `out/export`。
- 导出命令：`dotnet run --project source\Front\SimpleLanguageFront.csproj -- e -p <Module>.sp`（⚠ `compile` 默认不导出，须 `e`/`export` 命令或 `compile -e ir`）。

## 4. 调用方工程接入（测试/业务工程的 .jsonc）

```jsonc
"plugins": {
  "<plugin_id>": {
    "name": "...", "version": "0.1.0",
    "path": "simple_language_plugins/<plugin_id>",      // 相对工作区根（上溯定位）
    "enabled": true, "abi": 1,
    "prefix": "sl_<name>_",                              // ⚠ 必须显式 = plugin.jsonc entry.prefix（缺省 id+"_" 解析不到六件套）
    "lib": "../../../../simple_language_plugins/<plugin_id>/cvm/lib/windows-x64/<name>.dll",  // 字符串直给
    "platform": { "os": "windows", "arch": "x86_64" },
    "onUnavailable": "warn",
    "capabilities": [ { "type": "systemCall", "name": "<cap>" } ]
  }
},
"systemCalls": [
  // @<tag>(){} 内联块哨兵——调用方工程必须可见（registry 只装载当前工程 systemCalls）：
  { "name": "AtSignLabelCall",     "returnType": "Int32", "params": ["Int32"], "isVariadic": true, "cvmFunction": "atsign:label" },
  { "name": "AtSignLabelCallVoid", "returnType": "Void",  "params": ["Int32"], "isVariadic": true, "cvmFunction": "atsign:label" }
  // 插件系统方法不在调用方重复声明——refModule（slang/）在 RefModule 阶段自动注册
]
```

`lib` 两种形态：
- **字符串直给**（csharp_mono 用）：相对包路径，CVM 从插件原位加载——适合依赖树深/需同目录 BCL 的插件。
- **对象形态** `{ "dir": "lib", "select": "auto" }`（echo 用）：`PluginLibExportManager` 四级回退求值平台库并**拷贝**到 `out/export/<模块>/plugins/<id>/`——适合单 dll 轻量插件（⚠ `TopDirectoryOnly` 不递归，多级依赖树会缺文件）。对象形态成功路径上，**同趟 `AtSignLabelBuildManager` 构建部署的 frontend 产物（如 `SLAtSign.dll`）也会被拷入 `plugins/<id>/` 并追加进导出 module.json 的 `plugins[].libs` 做路径关联**——纯关联记录（CVM 装配只消费 `lib` 主库）：运行期插件 dll 从包内加载后，同目录的 frontend 产物即可被其运行时按裸名定位；字符串直给（原位加载）不触发该关联。

## 5. 新插件接入 checklist

| # | 步骤 | 要点 |
|---|------|------|
| 1 | 复制本模板目录为 `simple_language_plugins/<目录名>/` | 目录名推荐 = plugin.id；`plugin.jsonc` 必须声明 `"id"`（`@<tag>` 按它路由）；清空 cvm/src、frontend、slang 换成自己的实现 |
| 2 | 写 `plugin.jsonc` | §2 骨架；prefix 含尾下划线；capabilities 与 cvm 实现对齐 |
| 3 | 实现 `cvm/src/` 六件套 + capability | 符号导出名 = `{prefix}{六件套名}`；exec/labelExec 栈协议 |
| 4 | 构建 cvm dll → `cvm/lib/<os>-<arch>/` | MSVC 走 vcxproj（挂 sln「plugins」文件夹）或 `build-<name>.ps1`；clang 亦可 |
| 5 | 实现 `frontend/` 双契约 | netstandard2.0 + OutputPath="."；Parse/Build 均**单 JSON 进出**（小驼峰键） |
| 6 | 构建 frontend dll | `dotnet build frontend\<Name>Frontend.csproj -c Release`（或 build 脚本内代跑） |
| 7 | 写 `slang/` 四件套并导出 | `e -p slang\<Module>.sp` → `out/export/<Module>/<Module>.module.json` |
| 8 | 调用方工程 jsonc 挂 plugins 段 + AtSign 哨兵 | §4；`path` 相对工作区根；lib 指向 `cvm/lib/<os>-<arch>/` |
| 9 | 写测试用例 + 验证 | 编译 `compile -p <工程>.sp -e ir` → 运行 `csimple_lang.exe run` → `passed=N failed=0` |
| 10 | 文档同步 | 插件 README；语法篇 `simple_language/md/syntax/<tag>.md`（如暴露 @ 标签）；`PLUGIN_SYSTEM_DESIGN.md` §2.2 现状表 |

## 6. 构建与验证命令（以 csharp_mono 为例）

```powershell
# ① cvm dll + frontend dll + 部署到测试模块包（一键）
powershell -ExecutionPolicy Bypass -File simple_language_plugins\csharp_mono\cvm\build-csharp-mono.ps1

# ② slang ref module 导出（cwd = simple_language）
dotnet run --project source\Front\SimpleLanguageFront.csproj -- e -p d:\project\lang\simple_language_plugins\csharp_mono\slang\CSharpMono.sp

# ③ 调用方工程编译（cwd = simple_language；-e ir 才导出 module.json）
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\SpecialTest\ProjectTest.sp -e ir
#   成功标志：Front.txt 出现 `AtSignLabel: build success (N block(s))` + `export module success`

# ④ 运行验证
csimple_lang\build\Debug\bin\csimple_lang.exe run --no-banner simple_language\out\export\SpecialTest\SpecialTest.module.json
#   成功标志：`passed=N failed=0`，退出码 0
```

## 7. 硬约定与易错点

| # | 约定 | 违反后果 |
|---|------|----------|
| T1 | 三目录名固定 `cvm/` `frontend/` `slang/`——Front 常量 `PluginFrontendParser.FrontendDirName = "frontend"`、libDir 解析优先 `cvm/lib/<os>-<arch>`（旧扁平 `lib/<os>-<arch>` 仅存量兼容） | Front 找不到 frontend 程序集（LID 20057）/ libDir 解析落空 |
| T2 | `entry.prefix` 与调用方 jsonc `prefix` 都必须**显式**且含尾下划线 | 六件套符号解析失败（插件加载即败） |
| T3 | `@<tag>` 路由真源 = **插件清单声明的 `plugin.id`**（`PluginFrontendParser.FindPluginRootById`：上溯定位 plugins 目录 → 扫描各子目录清单建 id 索引，三处共用：改写器 `IsPluginLabel` / `EnsureLoaded` / `ResolvePluginLibDir`）；目录名推荐与 id 一致，仅清单**缺** `plugin.id` 时退回目录名匹配（旧格式兼容） | 声明了别的 id：`@<tag>` 路由不到（块透传报词法/Node 层错）；改一处不改三处：路由不一致 |
| T4 | 调用方工程必须声明 AtSign 哨兵两条 systemCalls（`atsign:label`）；插件系统方法则**只**在 slang/ 声明（refModule 自动注册），勿在调用方重复 | 哨兵不拦截（块透传报词法错）/ 声明漂移 |
| T5 | `compile` 命令默认**不导出** module.json——须 `e` 命令或 `-e ir`；成功判据 = module.json 时间戳被重写（dotnet 退出码恒 0 不可靠） | 以为编译成功实际跑的是旧包 |
| T6 | frontend 工程禁依赖 SL 程序集、禁缓存 SL 对象进 static；只接块原文 + 参数 JSON | 与 Front 版本强耦合、卸载泄漏 |
| T7 | 新系统方法/能力点变更时四处同步：slang jsonc `systemCalls[]` ↔ cvm 实现 ↔ `plugin.jsonc` capabilities ↔ 调用方测试 | 声明与实现漂移（运行期 -87/-13） |
| T8 | 产物只落 `cvm/lib/<os>-<arch>/`（原位加载语义，依赖树同目录齐备）；勿让 PluginLibExportManager 对象形态拷贝深依赖树 | BCL/子依赖缺失（TopDirectoryOnly 不递归） |
| T9 | 带通道的 `@<tag>(){}` 插件：slang/ 必须提供四个通道包装函数（`ChannelInInt/ChannelInString/ChannelOutInt/ChannelOutString`，名字固定）且 `plugin.jsonc` 声明 `refModule.channelClassPath` | 脱糖调用解析不到（LID 20058 / 未定义函数）——`<-` 赋值是 SL 函数调系统方法，不是 opcode 内置 |
| T10 | **refModule 依赖 compiled module.json**：调用方加载插件 refModule 优先取 `out/export/<M>/<M>.module.json`；只有 source `.jsonc` 时虽日志显示 `loaded (source): structCount=N` 但**类链不建立**，Meta 层连锁报 `解析链 节点[<Module>] 没有找到`（每个 @块一行）。故 **build 脚本应内含 slang refModule 编译步**（java_hotspot build 脚本 2.6 步：`compile -p <绝对路径>.sp -e ir` + module.json 重写时间戳判据）——csharp_mono 的 build 脚本无此步，靠历史产物存在 | 换机/清 out 缓存后调用方工程编译连环报错，且 Front 退出码 0 掩盖失败 |

## 8. 最小实现骨架（代码怎么写、接口怎么定）

> 本章回答两个问题：**① 接口在哪里定义（真源）**；**② 照着什么写（骨架）**。
> 骨架全部提炼自两个在库实例：`echo`（cvm 六件套最小范本）与 `csharp_mono`（frontend 双契约 + labelExec 完整范本）——照抄后改前缀与业务函数即可。

### 8.0 接口真源索引（先读这些，再写代码）

| 接口 | 真源文件 | 内容 |
|------|---------|------|
| **C 侧插件 ABI**（六件套原型 + 服务表 + 值编组） | `csimple_lang/src/vm/plugin/sl_plugin_abi.h` | 唯一真源：`SL_PLUGIN_ABI_VERSION`、六件套符号名与原型、`SLPluginHost` 服务表（V7 `push_string` / V8 `throw_error` 在表尾）、`SLLabelValue` 编组、`SLLabelExecFn`（labelExec 原型）、错误码 0/-1~-9 |
| **C 侧栈协议**（systemCall exec 进出栈） | `csimple_lang/src/vm/system_method_call/sl_vm_ext_api.h` | `slvm_pop_f64/push_f64/pop_i32/push_i32/push_ptr...`（槽型/位宽/深度语义）；`SLVmExtSystemFunc` = `int32 (*)(VM*, int32 param_count)` |
| **C 侧管理器/注册表 API** | `csimple_lang/src/vm/plugin/sl_plugin.h` | `SLPluginPackage`（manifest C 结构）、registry `register/find_by_id/find_by_capability`、mgr 四阶段 `load/enter/exit/unload`、`SLPluginState` 状态机 |
| **CVM 激活链**（`plugin:` 前缀 → capability） | `csimple_lang/src/vm/assembly/sl_runtime_assembly.c:850` | `sl_runtime_assembly_register_plugin_system_call`：`cvmFunction = "plugin:<cap>"` → registry 查 capability → 隐式 enter → `get_capability` 取 exec → 直注册系统函数 |
| **Front 侧单 JSON 契约**（DTO 字段权威） | `simple_language/source/Front/Compile/Parse/PluginFrontendParser.cs` | `PluginLabelRequest`/`PluginLabelParse`/`PluginLabelBuild` 三个 DTO（小驼峰键逐字对齐）；`FindPluginRootById` id 索引路由 |
| **Front 侧 lib 解析/拷贝** | `simple_language/source/Front/Export/PluginLibExportManager.cs` | lib 对象形态 `{dir, select}` 四级回退 + 拷入包内 `plugins/<id>/` + `plugins[].libs` 关联 |
| **设计总纲**（每个字段的为什么） | `csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md` | Part I §5 manifest / §6.4 四阶段 / §7 ABI 契约 / §9 降级 / §10.1 激活；Part II §A20 `@tag{}` 全规 |

**契约总原则**（写任何一侧代码前记住）：
- **V3** 跨边界内存一律 `host->alloc/free`，插件不得 malloc 后交宿主释放（反之亦然）；
- **V4** 值传递只用 `SLLabelValue`（systemCall 栈协议除外——那是宿主已固化的 `SLVmExtSystemFunc` 约定）；
- **V7** string 返回压栈走 `host->push_string`（string wrapper 只能由宿主创建）；
- **V8** 失败上抛走 `host->throw_error`（返回 FALSE 且未上抛时 CallSystemMethod 记 Assert）；
- **C9** 异常/崩溃不跨界：插件内部自行兜底，永不把 SEH/C++ 异常抛回宿主。

### 8.1 cvm 六件套 C 骨架（范本：`echo/cvm_echo.c`，177 行可直接照抄）

假设插件 id = `myplug`、前缀 = `myplug_`（单文件 `cvm_myplug.c`，放 `cvm/src/myplug/`）：

```c
/* cvm_myplug.c —— 最小 systemCall 插件骨架（照抄 echo 改前缀与 exec）。
 * 独立 DLL，不参与 csimple_lang 主工程构建（R6 不适用）。
 * include：csimple_lang/src/vm/plugin（ABI）+ .../system_method_call（栈协议） */
#include <string.h>

#include "sl_plugin_abi.h"     /* csimple_lang/src/vm/plugin/            */
#include "sl_vm_ext_api.h"     /* csimple_lang/src/vm/system_method_call/ */

/* ── ① capability exec 入口（先写业务，再挂声明） ───────────────────────
 * SLVmExtSystemFunc 约定：实参已在求值栈上（最后一个在栈顶）；实现弹出
 * 参数、自行压回返回值；返回 TRUE 成功 / FALSE 失败（失败前若宿主有 V8
 * 应先 host->throw_error 上抛，否则 CallSystemMethod 记 Assert）。 */
static int32 myplug_exec(VM* vm, int32 param_count)
{
    float64 value;
    if (param_count != 1)
    {
        return FALSE;
    }
    if (!slvm_pop_f64(vm, &value))       /* 整数槽按位宽自动拓宽为 f64 */
    {
        return FALSE;
    }
    return slvm_push_f64(vm, value);     /* TODO: 换成你的业务与压栈形态 */
}

/* ── ② load 期静态声明（get_info 只填静态信息不跑逻辑：C1） ─────────────
 * type 用枚举；name 必须与 plugin.jsonc capabilities[].name 逐字一致。 */
static SLCapabilityDecl s_myplug_caps[] =
{
    { SL_CAP_SYSTEM_CALL, "myplug", "", 0, NULL }
};

static SLPluginInfo s_myplug_info =
{
    SL_PLUGIN_ABI_VERSION,  /* abiVersion                      */
    "myplug",               /* id（= plugin.jsonc plugin.id）  */
    "my_plugin_full_name",  /* name                            */
    "0.1.0",                /* version                         */
    1,                      /* capCount                        */
    s_myplug_caps           /* caps                            */
};

/* ── ③ enter 会话私有状态（按需扩展；只缓存 host 供对称释放） ─────────── */
typedef struct MyPlugSession
{
    SLPluginHost* host;
} MyPlugSession;

/* ── ④ 六件套（符号名 = {prefix}{名字}，导出宏必加） ──────────────────── */
SL_PLUGIN_EXPORT int32 myplug_abi_version(void)
{
    return SL_PLUGIN_ABI_VERSION;
}

SL_PLUGIN_EXPORT int32 myplug_get_info(SLPluginInfo** out_info)
{
    if (out_info == NULL)
    {
        return SL_PLUGIN_ERR_GENERIC;
    }
    *out_info = &s_myplug_info;
    return SL_PLUGIN_OK;
}

SL_PLUGIN_EXPORT int32 myplug_enter(SLPluginHost* host, const char* config_json,
                                    SLPluginRuntime** out_rt)
{
    SLPluginRuntime* rt;
    MyPlugSession* session;

    if (host == NULL || out_rt == NULL
        || host->alloc == NULL || host->free == NULL)
    {
        return SL_PLUGIN_ERR_GENERIC;
    }
    if (host->abiVersion != SL_PLUGIN_ABI_VERSION)
    {
        return SL_PLUGIN_ERR_ABI;
    }

    /* V3：rt 与会话都从宿主堆分配（跨界内存一律 host->alloc/free） */
    session = (MyPlugSession*)host->alloc(sizeof(MyPlugSession));
    if (session == NULL)
    {
        return SL_PLUGIN_ERR_NOMEM;
    }
    session->host = host;

    rt = (SLPluginRuntime*)host->alloc(sizeof(SLPluginRuntime));
    if (rt == NULL)
    {
        host->free(session);
        return SL_PLUGIN_ERR_NOMEM;
    }
    rt->magic = SL_PLUGIN_RT_MAGIC;
    rt->impl  = session;
    rt->owner = NULL;      /* 宿主 mgr_enter 回填 */

    *out_rt = rt;
    (void)config_json;     /* 需要读配置时解析本参数（jsonc plugins 段原文） */
    return SL_PLUGIN_OK;
}

SL_PLUGIN_EXPORT int32 myplug_get_capability(SLPluginRuntime* rt, const char* type,
                                             const char* name, void** out_entry)
{
    if (rt == NULL || rt->magic != SL_PLUGIN_RT_MAGIC)
    {
        return SL_PLUGIN_ERR_STATE;   /* 野指针 / 已 exit */
    }
    if (type == NULL || name == NULL || out_entry == NULL)
    {
        return SL_PLUGIN_ERR_GENERIC;
    }
    /* 跨界字符串名协议：type 是类型名 "systemCall"（labelExec 同理比对
     * SL_PLUGIN_CAP_NAME_SIGN_LABEL） */
    if (strcmp(type, SL_PLUGIN_CAP_NAME_SYSTEM_CALL) != 0
        || strcmp(name, "myplug") != 0)
    {
        return SL_PLUGIN_ERR_SYMBOL;
    }
    *out_entry = (void*)myplug_exec;
    return SL_PLUGIN_OK;
}

SL_PLUGIN_EXPORT int32 myplug_exit(SLPluginRuntime* rt)
{
    MyPlugSession* session;
    SLPluginHost* host;

    if (rt == NULL || rt->magic != SL_PLUGIN_RT_MAGIC)
    {
        return SL_PLUGIN_ERR_STATE;   /* 重复 exit 由宿主状态机先拦（C3） */
    }

    session = (MyPlugSession*)rt->impl;
    host = (session != NULL) ? session->host : NULL;

    rt->magic = 0;                    /* 先失效再释放，防 use-after-free */
    if (host != NULL)
    {
        if (session != NULL)
        {
            host->free(session);      /* TODO: 会话内自有资源先逐项释放 */
        }
        host->free(rt);
    }
    return SL_PLUGIN_OK;
}

SL_PLUGIN_EXPORT void myplug_unload(void)
{
    /* 无静态资源时不做事（C4：unload 前宿主必已调 exit） */
}

/* 可选：SL_PLUGIN_EXPORT int32 myplug_thread_attach(void) / thread_detach(void)
 * —— 插件将在 isolate worker 线程被调用时必须实现（mono 等带线程亲和的运行时） */
```

要点：
- **`get_capability` 的 `entry`**：systemCall 给 `(void*)exec`；`signLabel` 给 `SLLabelExecFn`（`int32 (*)(SLLabelExecCtx* ctx)`，进出值走 `ctx->in_values/out_values` 的 `SLLabelValue`，见 `sl_plugin_abi.h`；完整接收端范本 = `cvm_csharp_mono.c` 的 `labelExec` 分支）。
- **多 capability**：`s_caps[]` 加条目 + `get_capability` 加匹配分支即可。
- **构建**：抄 `echo/build-echo.ps1`（探测 MSVC x64 → `cl /LD` → 产物落 `cvm/lib/windows-x64/`，`-DeployDir` 一键拷进测试模块包）；或挂 vcxproj 走 sln（csharp_mono 形态）。

### 8.2 frontend Parse/Build 双契约骨架（范本：`csharp_mono/frontend/`）

frontend 工程两个**静态类、静态方法、单 JSON 进出**（netstandard2.0，零第三方依赖，MiniJson 自带）；插件命名空间自定，`plugin.jsonc` 三键声明入口：

```csharp
// 契约铁律（Front 侧 PluginFrontendParser 反射约定）：
// 1. public static string 方法、单 string 参数、返回 string——永不返回 null、永不抛异常
//    （任何失败折叠为 JSON 响应里的 ok=false / kind=...，由 Front 分流 LID）；
// 2. 键名小驼峰，与 PluginLabelRequest / PluginLabelParse / PluginLabelBuild DTO 逐字对齐；
// 3. 只接块原文与通道表，不碰 SL 的 AST/符号表（T6）。

namespace MyPlugFrontend
{
    public static class SLLabelParser          // plugin.jsonc parserType 指向本类
    {
        public static string ParseLabel( string requestJson )   // parserMethod 缺省即本名
        {
            // 请求：{label, entryName, paramsText, bodyText, headLineCount,
            //        tailLineCount, inChannels[{target,slVar,slType}], outChannel}
            //   代码段 = bodyText 按 '\n' 切分后的 [headLineCount, 行数-tailLineCount)，
            //   是目标语言原文——Front 已初判通道并随请求下发，本层不得重复解析 <-；
            // 响应：{ok, error, errorLine, source, dll, entry, entryMethod, labelParams}
            //   source 非空才进构建器；errorLine 用 bodyText 1-based 行号（0=与行无关）。
            ...
        }
    }

    public static class SLLabelBuilder        // plugin.jsonc builderType 指向本类
    {
        public static string BuildLabels( string requestJson )  // builderMethod
        {
            // 请求：{label, outDir, libDir, entries[{entryName,source}]}（Front 导出期
            //        分组下发全部条目；libDir = 插件 lib 部署目录，由 Front 解析好）
            // 响应：{ok, kind, error, dllPath, count}
            //   kind 五态：success / notFound（外部工具链缺失，Info 级跳过）/
            //             buildFailed / deployFailed / contract（契约破裂）
            //   → Front 分流 LID 22135~22138，错误只报一次不中断导出。
            ...
        }
    }
}
```

完整可抄实现：`SLLabelParser.cs`（块体三段扫描 + `using/import` 指令行吸收 + 词法感知 `<-` 三态判定 + 目标源码生成）与 `SLLabelBuilder.cs`（csc 探测 → 临时目录写 .cs（UTF-8 BOM）→ 合并编译 → 部署 libDir + kind 五态回传）。`MapCSType`（slType → 目标类型映射白名单）与 `IsIdent` 是契约防御的样板。

### 8.3 slang 四件套样例（最小可编译）

```
slang/
├── MyPlug.jsonc   systemCalls[] 声明（见下）+ references Core + struct 归属
├── MyPlug.sl      声明源码（无方法体）
├── MyPlug.sp      自测工程入口（_main_ 空壳）
└── MyPlug.md      说明
```

```jsonc
// MyPlug.jsonc 关键段（完整骨架抄 csharp_mono/slang/CSharpMono.jsonc）
"systemCalls": [
  { "name": "MyPlugEcho", "class": "MyNs.MyPlug",
    "returnType": "Float64", "params": [ "Float64" ],
    "cvmFunction": "plugin:myplug" }        // ← 路由键：plugin:<capability name>
],
"references": [ { "path": "../../../simple_language/out/export/Core" } ]
```

`cvmFunction = "plugin:myplug"` 是整条链路的缝合点：CVM 装配期见此前缀 → registry 查 `systemCall/myplug` capability → 隐式 load+enter → `get_capability` 取 `myplug_exec` → 直注册为该系统函数（§8.0 激活链真源）。

### 8.4 从零生成一个插件的完整流程（串起 §5 checklist）

```
① 定能力点    我要给 SL 加什么？systemCall（SL 侧可调函数）/ signLabel（@tag 块）
              / device / backend / tool —— 只做 systemCall 则 frontend/、slang/
              可极简（echo 甚至全免：声明由调用方 jsonc systemCalls[] 自带）
② 建目录      simple_language_plugins/<id>/，按 §1 三目录起骨架
③ plugin.jsonc §2 骨架填 id/prefix（含尾下划线！）/capabilities/platform
④ cvm 代码    §8.1 骨架改前缀 → exec 业务 → build-<name>.ps1 产 dll 到
              cvm/lib/<os>-<arch>/
⑤ （signLabel 才需要）frontend 工程：§8.2 双契约 + plugin.jsonc 四键
              （frontendLibs/parserType/builderType/builderMethod）
⑥ slang       §8.3 四件套 → `e -p slang\<Module>.sp` 导出 ref module
⑦ 接入        调用方工程 jsonc：plugins 段（§4，lib 两形态择一）+ AtSign 哨兵两条
⑧ 验证        compile -e ir → run module.json → passed=N failed=0
              （成功判据 = module.json 时间戳被重写，T5）
⑨ 收尾        插件 README + PLUGIN_SYSTEM_DESIGN.md §2.2 现状表 + 语法篇（如暴露 @ 标签）
```
