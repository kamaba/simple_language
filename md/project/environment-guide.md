# Environment 平台环境指南（API + jsonc platform 段全量关键字）

> 本文是 Environment 命名空间（P1.7 namespace 形态）与 `.jsonc` 工程配置 `platform` 段的完整参考。
> 真源：`source/Front/Lib/Core/Environment.sl`（SL API）、`source/Front/Lib/Core/Environment/Platform_*.sl`（定义枚举）、`source/Front/Project/ProjectJsoncLoader.cs`（jsonc 解析）、设计文档 `md/design/PLATFORM_CAPABILITY_DESIGN.md`。
> 本文以**实现为准**（与设计稿有差异处已标注）。

---

## 1. 概述

Environment 解决"同一份模块在不同平台/环境下如何声明要求、探测能力、做分支"的问题。链路：

```
.sl 源码（Environment.* 查询）
    ↑ 运行期由 CVM 探测一次并分类（sl_environment 惰性单例，O(1) 查询）
*.jsonc 工程配置 "platform" 段（声明层：require / override / variants / targets / when / fallbackHint）
    ↓ Front 编译期解析 + 校验（非法值 Error 20036，编译中止）
*.module.json（SLIR 包，platform 段随包导出）
    ↓ CVM 加载期判定 require、运行期应用 override
csimple_lang run [--disable/--enable/--set/--override-file/--no-override] <module.json>
```

三区结构（`namespace Environment`）：

| 组成 | 形态 | 职责 |
|------|------|------|
| `Environment.Platform.*` | 定义枚举 + Defs 注册表 | **编译期定义常量**（如 `Platform.os.linux`），成员名 = jsonc key |
| `Environment.current.*` | static getter / 方法 | **运行期有效值**（CVM 探测 + 受 override 影响），用于分支判断 |
| `Environment.probe.*` | static getter / 方法 | **原始探测值**（永不受 override 影响），仅诊断用 |
| `Environment.Override.*` | static 方法 | 覆盖查询（isDisabled / source / getValue） |
| `Environment.env.*` | static 方法 | 进程环境变量（get/set/exists） |
| `Environment.custom.*` | static 方法 | 自定义条件值（读 jsonc `require.custom` 声明） |
| `Environment.sys.*` | static 方法/getter | 目录 / 换行 / 计时 |
| `Environment.legacy.*` | static 方法 | P1.6 兼容壳（getVariable / setVariable，已弃用） |

---

## 2. 核心概念

### 2.1 定义 vs 运行时

`Environment.Platform.os.linux` 是**编译期定义常量**（枚举值固定写死在 IR 里），`Environment.current.os` 是**运行时探测值**。平台分支 = 二者做相等/包含比较：

```sl
if Environment.current.os == Environment.Platform.os.window
{
    # Windows 分支
}
if Environment.current.isaHas( Environment.Platform.isa.avx2 )
{
    # AVX2 分支（isa 是位集合，用 isaHas 查询）
}
```

### 2.2 current vs probe（§8.5.6）

| | current（有效值） | probe（原始值） |
|---|---|---|
| override 影响 | 受 `--disable/--enable/--set` 影响 | **永不**受影响 |
| 用途 | **判断分支**（代码"看到什么"） | 诊断/报告（机器"真有什么"） |
| 成员 | 完全镜像（含六大类详情对象） | 完全镜像（Probe* 前缀类） |

### 2.3 P1.6 → P1.7 迁移表

| 旧（P1.6） | 新（P1.7） |
|---|---|
| `Environment.current().os` | `Environment.current.os`（无括号） |
| `OS.window` | `Environment.Platform.os.window` |
| `ISA.avx2` | `Environment.Platform.isa.avx2` |
| `Environment.getVariable(n)` | `Environment.env.getValue(n)` |
| `Environment.setVariable(n, v)` | `Environment.env.setValue(n, v)` |
| `Environment.currentDirectory()` | `Environment.sys.currentDirectory()` |
| `Environment.tickCount()/tickCount64()/now` | `Environment.sys.tickCount()/tickCount64()/nowMillis()` |

> 命名注意：类名用大写 `Override`——`override` 是 SL 方法重载关键字，词法器大小写敏感（同 get/set 规则，不允许裸关键字标识符）。

---

## 3. Environment.Platform 定义枚举全表（成员名 = jsonc key）

> ★ **单一真相源**：枚举成员名与 jsonc `platform.require` 里写的字符串同名，无需记映射表。
> 枚举值与 CVM `sl_environment.h` / `sl_platform_def.h` 共享（§13.9 一致性）。
> 别名（如 `win64`、`x86_64`）**仅 CVM 容错接受**；Front jsonc 侧校验以成员名为准。
> 分文件：`Platform_OS.sl` / `Platform_Cpu.sl` / `Platform_Dev.sl` / `Platform_Misc.sl` / `Platform_Defs.sl`。

### 3.1 `os`（Platform_OS.sl）— 操作系统

| 成员 | 值 | 备注 / 别名 |
|------|----|------|
| unknown | 0 | |
| window | 1 | 别名 windows / win / win32 / win64 |
| linux | 2 | |
| mac | 3 | 别名 macos / osx / darwin |
| unix | 4 | 其他 Unix-like |
| freeBSD | 5 | 别名 bsd |
| android | 10 | |
| ios | 11 | 别名 iphone / ipad |
| ps4 / ps5 | 20 / 21 | 游戏主机 |
| xboxOne / xboxSeries | 22 / 23 | |
| nintendoSwitch | 24 | |
| browser | 30 | wasm 浏览器宿主 |
| wasi | 31 | |
| bareMetal | 40 | 无 OS 裸机 |
| rtos | 41 | 实时 OS |

### 3.2 `osVersion`（数值层，用于 >= 比较）

unknown=0；window7=100 / window8=101 / window10=102 / window11=103 / windowServer2019=110 / windowServer2022=111；linuxGeneric=200、linuxDebian=201、linuxUbuntu=202、linuxArch=203、linuxAlpine=204、linuxCentos=205、linuxRhel=206、linuxFedora=207、linuxOpenSuse=208、linuxGentoo=209、linuxKali=210；linuxKeil=250 / linuxYocto=251 / linuxBuildroot=252 / linuxOpenWrt=253；macos13=300 / macos14=301 / macos15=302；ios16=400 / ios17=401 / ios18=402；androidApi31=500 / androidApi33=501 / androidApi34=502；ps4Sdk=600 / ps5Sdk=601 / xboxGdk=602 / switchSdk=603；wasiPreview1=700 / wasiP2=701

### 3.3 `form`（环境形态）

unknown=0、desktop=1、server=2、mobile=3、embedded=4、web=5、console=6、container=7（docker/k8s）

### 3.4 `arch`（CPU 架构）

unknown=0、x86=1、x64=2（别名 x86_64/amd64）、arm32=3（别名 arm/armv7）、arm64=4（别名 aarch64）、riscv64=5、loongArch64=6、mips64=7、sw64=8、ppc64=9、sparc64=10、wasm32=11

### 3.5 `cpu`（CPU 拓扑）

unknown=0、single=1、smp=2、bigLittle=3、hybrid=4、numa=5

### 3.6 `isa`（指令集扩展，★位集合）

> 值是 **bit index** 不是 bit 值——用 `Environment.current.isaHas( Platform.isa.avx2 )` 查询，不要拿值做位运算。

none=0、sse=1、sse2=2、sse42=3（对应 jsonc 写法 `"sse4.2"`）、avx=4、avx2=5、avx512=6、fma=7、neon=10、sve=11、sve2=12、rvv=13、lsx=14、lasx=15、mmi=16、altivec=17、vsx=18

### 3.7 `device`（计算设备，★位集合）

> 用 `deviceHas` / `deviceCount` 查询。

none=0、cpu=1、gpu=2、npu=3、dsp=4、fpga=5

### 3.8 `ai`（AI 推理栈，★位集合）

none=0、onnxruntime=1、tensorRT=2（jsonc 写 `"tensorrt"`）、openVINO=3、tflite=4、coreML=5、cann=6、rknn=7、cambricon=8、qnn=9、snpe=10、horizonBpu=11、sophon=12、ane=13、torch=14、tensorFlow=15

### 3.9 `render`（图形 API，★位集合）

none=0、d3d11=1、d3d12=2、vulkan=3、metal=4、openGL=5、openGLES=6、webGPU=7、software=8（无 GPU 宿主兜底报告 software 而非失败）

### 3.10 `shaderModel`

unknown=0、sm_5_0=50、sm_6_0=60、sm_6_5=65、sm_6_6=66、sm_6_7=67（jsonc 里写 `"5_0"` / `"6_0"` 形式）

### 3.11 `network`（网络连接种类，★位集合）

none=0、loopback=1、ethernet=2、wifi=3、cellular=4、unknownNet=5

### 3.12 `link`（链路状态）

down=0、lan=1、online=2
> ⚠️ 设计原名 `local` 是 SL 保留字（ETokenType.Local），枚举成员改名 `lan`；jsonc 字符串值 `"local"` 由 C 侧 sl_def_link 别名表兼容（`ProjectTest.jsonc` 即写 `"minLink": "local"`）。

### 3.13 `embedded`（MCU 家族）

none=0、cortexM=1、cortexA=2、cortexR=3、xtensa=4、riscvMcu=5、mcs51=6、avr=7、pic=8、msp430=9、rl78=10、rx=11、triCore=12、hc08=13

### 3.14 `rtos`（实时操作系统）

none=0、freeRTOS=50、rtThread=51、zephyr=52、threadX=53、ucos=54、mbed=55、liteOS=56、aliosThings=57

### 3.15 `runtime`（宿主运行时，CVM 恒报 slvm）

slvm=0、clr=1、jvm=2、aot=3、wasm=4

### 3.16 `build`（构建模式，来自入口模块 jsonc compile.optimize）

debug=0、release=1

### 3.17 `endian`（字节序）

unknown=0、little=1、big=2

### 3.18 Defs 注册表（Platform_Defs.sl）

| 成员 | 说明 |
|------|------|
| `DefKind` 枚举 | OS=0、OSVersion=1、Arch=2、Cpu=3、ISA=4、Device=5、AI=6、Gfx=7、ShaderModel=8、Network=9、Link=10、McuFamily=11、Rtos=12、Runtime=13、BuildMode=14、Endian=15 |
| `DefItem` 类 | `{ value, name, aliases, describe }`——单个定义成员描述 |
| `defs.lookup( kind, name )` | 按类查成员（未知返回 null） |
| `defs.contains( kind, name )` | 定义表是否含该名 |
| `defs.names( kind )` | 该类全部成员名（jsonc key），按枚举序 |

表数据在 CVM 侧 `sl_platform_def.c`，Front jsonc 校验与 CVM 环境比较共用一份（"一把尺子"）。

---

## 4. current / probe 成员全清单

`probe` 与 `current` 成员完全镜像（读 `SystemPlatformEnvProbe*` 系列系统方法，永不受 override 影响），下表只列 current。

### 4.1 顶层标量成员（static getter）

| 成员 | 类型 | 说明 |
|------|------|------|
| `os` | Int32 | 枚举值，与 `Platform.os.*` 比较 |
| `osName` | string | 人类可读 OS 名 |
| `osFamily` | Int32 | 0=unknown 1=window 2=unix（快捷家族判断） |
| `osVersionNumber` | OSVersionNumber | `{ major, minor, build, patch }`（数值层，用于 >=） |
| `form` | Int32 | 见 Platform.form |
| `arch` / `archBits` | Int32 | 架构 / 位数（32/64/0=unknown） |
| `endian` | Int32 | 字节序 |
| `triple` | string | 目标三元组 |
| `cpuCount` | Int32 | 逻辑核数 |
| `cpuVendor` / `cpuModel` | string | CPU 厂商 / 型号 |
| `runtime` / `runtimeVersion` | Int32 / string | 宿主运行时 |
| `build` | Int32 | debug=0 / release=1 |
| `totalMemMB` / `availMemMB` | Int64 | 总内存 / 可用内存（MB） |

### 4.2 位集合与函数成员

| 成员 | 签名 | 说明 |
|------|------|------|
| `isaHas` | `bool isaHas( Int32 isa )` | 参数传 isa 成员值 |
| `deviceHas` | `bool deviceHas( Int32 dev )` | 设备类是否存在 |
| `deviceCount` | `Int32 deviceCount( Int32 dev )` | 该类设备数量 |
| `libExists` | `bool libExists( string name )` | 动态库是否存在 |
| `libVersion` | `string libVersion( string name )` | 库版本（""=缺席/未注册符号表） |
| `libHas` | `bool libHas( string name, string minVersion )` | 存在且版本 >= min；空 minVersion 只查存在；版本未知不判负（§9.2 无假阴性） |
| `libPath` | `string libPath( string name )` | 探测到的完整路径（""=缺席） |

### 4.3 六大类详情对象（§12.12-12.17）

| 成员 | 返回类 | 专有成员 |
|------|--------|----------|
| `cpu` | `Cpu` | topology、physicalCores、logicalCores、hasHyperThreading、numaNodes、freqMHz、maxFreqMHz、cacheL1KB/L2KB/L3KB、perfCores、efficiencyCores |
| `ai` | `Ai` | `has( stack )`、`version( stack )`、`any()`、preferred（最佳可用栈名，""=无） |
| `render` | `Render` | api（首选后端）、`apiList()`（全部后端名）、shaderModel、hasRayTracing、hasMeshShader、hasCompute、hasHdr、maxTextureSize、displays、primaryWidth、primaryHeight、refreshHz |
| `network` | `Network` | netType、linkState、isOnline、isLocalOnly、interfaces、hasProxy、proxyUrl |
| `script` | `Script` | `has( name )`、`has( name, minVersion )`（名字大小写不敏感；空 minVersion 只查存在；版本未知不判负）、`version( name )`、`path( name )`、`available()` |
| `embedded` | `Embedded` | family、chip、vendor、coreName、bareMetal、rtos、rtosVersion、flashKB、ramKB、stackFreeKB、heapFreeKB、hasFpu/hasMmu/hasMpu/hasDsp、cpuFreqMHz、toolchain、`peripherals()`、`has( peripheral )`、`isConstrained()`（flash<512KB 或 ram<256KB 或无 MMU 或裸机 → 受限；通用平台 0=unknown 计为受限） |
| `device` | `Device` | kind（主设备类：gpu > npu > dsp > fpga > cpu）、`has( dev )`、`count( dev )`、`info( dev [, index] )` → `DeviceInfo { vendor, name, computeCapability, memoryMB }` |

---

## 5. ★ jsonc `platform` 段全量关键字（核心）

`platform` 是 `.jsonc` 工程配置的顶层段之一（与 `project` / `compile` / `compileFiles` 等并列），由 `ProjectJsoncLoader.ParsePlatform` 解析。顶层共 **6 个键**：

| 键 | 形态 | 说明 |
|----|------|------|
| `targets` | `string[]` | 目标标签（纯元数据，编译器不消费） |
| `require` | 对象 | **能力要求声明**（核心，见 5.1）→ 编译为条件 AST，随 module.json 导出，CVM 加载期判定 |
| `when` | `string` | 表达式糖（当前仅存储不解析） |
| `fallbackHint` | `string` | 要求不满足时的提示文本（诊断用） |
| `variants` | `object[]` | **多目标变体**（见 5.4）：数组顺序即优先级，CVM 取第一个 `require` 通过的变体 |
| `override` | 对象 | **运行期覆盖表**（见 5.5）：jsonc 通道的预置覆盖 |

### 5.1 `require` 的 18 个字段

各字段**默认 AND** 组合；数组每元素各成一个 atom。字段级别可写 `"optional": true/false`（语义见 5.6；不满足时仅告警降级，不判失败）。

#### A. 单值对象字段（`{eq}/{ne}/{any}`，或字符串简写 = eq）

适用于：**`os`、`arch`**（默认 optional=false）

| 子键 | 形态 | 语义 |
|------|------|------|
| （字符串简写） | `"os": "linux"` | 等价 `eq` |
| `eq` | `string` | 等于（有效值须是枚举成员名或 CVM 别名） |
| `ne` | `string` | 不等于 |
| `any` | `string[]` | 集合内任一（OR） |
| `optional` | `bool` | 缺省 false |

#### B. 版本/数值字段（`{min}`→ge，亦支持 `eq`/`ne`）

适用于：**`osVersion`、`cpuCount`、`memory`、`runtime`**（默认 optional=false）

| 子键 | 形态 | 语义 |
|------|------|------|
| `min` | `string` 或数字 | >=（数字版本号 min/eq/ne 统一转字符串；cpuCount/memory 传数字） |
| `eq` | `string` | 等于 |
| `ne` | `string` | 不等于 |
| `optional` | `bool` | 缺省 false |

#### C. `network`（默认 optional=false）

| 子键 | 形态 | 语义 |
|------|------|------|
| `minLink` | `string` | 链路状态定序比较 >=（`"local"`=lan / `"online"`），产 atom |
| `probe` | `bool` | **行为开关**（不产 atom）：L3 加载期在线探测开关，落 section.NetworkProbe |
| `optional` | `bool` | 缺省 false |

#### D. `cpu`（复合对象，默认 optional=false；子键各产一个 atom）

| 子键 | 形态 | 语义 |
|------|------|------|
| `all` | `string[]` | isa 全部具备（AND） |
| `any` | `string[]` | isa 任一具备（OR） |
| `minCores` | 数字 | 核数 >=（转成 cpuCount ge atom） |
| `topology` | `{ any: string[], optional? }` | 拓扑任一匹配 |
| `optional` | `bool` | 对象级，作用于全部子 atom，缺省 false |

#### E. `render`（默认 optional=true）

| 子键 | 形态 | 语义 |
|------|------|------|
| `api` | `{ any: string[], optional? }` | 图形 API 任一（OR） |
| `minShaderModel` | `string` | 着色器模型 >=（如 `"6_0"`） |
| `optional` | `bool` | 缺省 **true** |

#### F. `embedded`（默认 optional=true）

| 子键 | 形态 | 语义 |
|------|------|------|
| `family` | `{ any: string[], optional? }` | MCU 家族任一 |
| `minFlashKB` | 数字 | Flash >= KB |
| `minRamKB` | 数字 | RAM >= KB |
| `optional` | `bool` | 缺省 **true** |

#### G. 数组字段（每元素一个 atom）

| 字段 | 默认 optional | 比较子键 | 无比较子键时 |
|------|---------------|----------|--------------|
| `lib` | false | `min`（版本 >=） | 存在性 EXISTS |
| `sdk` | true | `min` | EXISTS |
| `ai` | true | `min` | EXISTS |
| `script` | true | `min` | EXISTS |
| `custom` | false | `eq`（值相等） | EXISTS |
| `env` | false | `eq`（环境变量值相等） | EXISTS |
| `device` | true | `minCompute`（算力 >=） | EXISTS |
| `environment` | true | `minCompute` | EXISTS |

**数组元素的关键字**：

| 子键 | 适用字段 | 说明 |
|------|----------|------|
| `name` | lib/sdk/ai/script/custom/env | 条目名（**实现实际用 `name`；兼容读 `key`**——设计文档 §6.1 写 `key`，以实现为准） |
| `kind` | device/environment | 条目名（兼容读 `name`） |
| `min` | lib/sdk/ai/script | 最低版本（字符串或数字；缺省 → 只查存在） |
| `eq` | custom/env | 期望值（缺省 → 只查存在） |
| `minCompute` | device/environment | 最低算力（数字；缺省 → 只查存在） |
| `optional` | 全部 | 覆盖字段级默认 |

#### 未知字段

require 下出现上述 18 个之外的键 → LID `ProjectPlatformRequireUnknownField` **告警并忽略**（不判失败），提示合法字段列表。

### 5.2 组合语义

- require 各字段（及产生的全部 atom）之间**默认 AND**；
- `any` 数组内部 **OR**；`cpu.all` 内部 **AND**；
- `optional: true` 的条件不满足时：**告警降级**（Warning），该条件视为通过——不判失败；
- 未声明的 `platform` 段：模块照常运行（旧模块兼容）；
- `custom` 条件未在宿主注册钩子（`sl_require_register_custom`）时 **CUSTOM atom 默认判过**（不误杀，§9.3）。

### 5.3 `require` 值域校验（编译期）

- 非法枚举值（如 `"linuxx"` / `"arm65"` / `"sse5"` / `"vulkn"`）→ **Error 20036，编译中止**；
- 校验时机在 MetaCore 阶段 `ValidatePlatformConfig`（InjectProjectData 之后、ParseStatements 之前），不在配置加载期（加载期的 Error 进不了任何阶段 errorCount）；
- key 校验 + 别名归一（§13.8）同机执行；
- `compile.target`（AnyCPU/x86/x64/arm64/wasm32）：非法值 Error 20033；target 与 require.arch 无交集 Warning 20034。

### 5.4 `variants`（多目标变体，§14）

数组**顺序即优先级**，CVM 取第一个 `require` 通过的变体；全部不通过时用**缺省解释器兜底**（`aot` 缺省）。

| 子键 | 形态 | 说明 |
|------|------|------|
| `target` | `string` | 变体标签（诊断输出） |
| `aot` | `string` | 该变体的 AOT 产物文件名（缺省 = 解释器兜底） |
| `require` | 对象 | 复用 5.1 的字段表，未知字段同样告警忽略 |

```jsonc
"platform": {
  "variants": [
    { "target": "linux-native", "require": { "os": "linux" } },
    { "target": "win-native",   "require": { "os": "window",
        "environment": [ { "name": "docker", "optional": true } ] } },
    { "target": "any-interp" }
  ]
}
```

### 5.5 `override`（jsonc 通道覆盖表）

对象成员的**值形态决定动作**（只此四种，null/array/object 成员跳过）：

| 值形态 | 动作 | 说明 |
|--------|------|------|
| `true` | **ENABLE** | 强制该能力判真（即使探测不到） |
| `false` | **DISABLE** | 屏蔽该能力（强制判否） |
| 字符串 | **SET** | 替换值（如 `"memory": "256"`） |
| 数字 | **SET** | 替换值（存 GetRawText 原文，导出时复原为数字） |

```jsonc
"platform": {
  "override": {
    "memory": 256,           # SET：模拟 256MB 内存
    "cpu.avx2": true,        # ENABLE：强制 AVX2 判真
    "network.online": true,  # ENABLE
    "device.gpu": false      # DISABLE：屏蔽 GPU
  }
}
```

### 5.6 全字段正例（test/BaseTest/ProjectTest.jsonc）

```jsonc
"platform": {
  "require": {
    "os":        { "any": [ "win64", "window" ] },
    "osVersion": { "min": "10.0" },
    "arch":      { "any": [ "x86_64", "arm64" ] },
    "runtime":   { "min": "0.0.01" },
    "cpu": {
      "minCores": 4,
      "all": [ "sse4.2" ],
      "topology": { "any": [ "smp", "biglittle" ] }
    },
    "render":   { "api": { "any": [ "vulkan", "d3d12" ] }, "minShaderModel": "6_0" },
    "network":  { "minLink": "local" },
    "embedded": { "family": { "any": [ "cortexm", "xtensa" ] } },
    "ai":       [ { "name": "tensorrt" } ],
    "device":   [ { "kind": "npu" } ],
    "lib":      [ { "name": "sqlite3" } ],   # 硬要求：缺席则失败
    "sdk":      [ { "name": "cuda" } ],      # 软要求：缺席仅告警
    "custom":   [ { "name": "p2smoke" } ]    # 宿主未注册钩子 → 默认判过
  },
  "override": { "memory": 256, "cpu.avx2": true, "network.online": true, "device.gpu": false }
}
```

---

## 6. 运行期覆盖（四通道）

### 6.1 优先级（低 → 高）

| # | 通道 | 载体 |
|---|------|------|
| 1 | jsonc `platform.override` | module.json 随包 |
| 2 | 环境变量 `SL_RT_DISABLE` / `SL_RT_ENABLE` / `SL_RT_SET` | 逗号分隔 key 列表 / `key=value` 列表 |
| 3 | CLI 参数 `--disable` / `--enable` / `--set` / `--override-file` | 命令行 |
| 4 | 宿主 API `sl_runtime_override_set()` | 嵌入宿主注入 |

`--no-override` 整体忽略 1~3（保留宿主 API 注入）。

### 6.2 CLI 参数（csimple_lang）

| 参数 | 说明 |
|------|------|
| `--disable <keys>` | 屏蔽平台能力条件（强制判否），逗号分隔、可重复 |
| `--enable <keys>` | 强制判真（即使探测不到），逗号分隔、可重复 |
| `--set <k>=<v>` | 覆盖探测的数值/版本（模拟环境），可重复 |
| `--override-file <path>` | 从 JSON 文件批量载入覆盖表 |
| `--no-override` | 整体忽略 jsonc/env/CLI 覆盖通道（保留宿主 API） |
| `--force-run` | 跳过硬性平台要求校验（打印警告并继续执行） |

```powershell
csimple_lang run --disable device.gpu --set memory=256 Core.module.json
csimple_lang run --enable cpu.avx2 --override-file overrides.json Core.module.json
```

### 6.3 覆盖 key 命名

| 形态 | 示例 | 说明 |
|------|------|------|
| `<大类>.<成员>` | `cpu.avx2`、`device.gpu`、`lib.sqlite3`、`network.online` | 单个能力 |
| `<大类>` | `--disable ai` | 整类屏蔽 |
| `custom.<k>` | `custom.p2smoke` | 自定义条件 |

- Front 侧 jsonc key：编译期校验，非法 → **Error 20036**；
- CVM 侧 CLI/env key：容错（大小写不敏感归一，如 `Device.GPU` 可匹配；未知 key 仅 Warning + 最相近 key 建议）。

### 6.4 覆盖查询（SL 侧）

```sl
bool   off = Environment.Override.isDisabled( "device.gpu" )   # 仅 DISABLE 算 true
string src = Environment.Override.source( "cpu.avx2" )         # "" / "jsonc" / "env" / "cli" / "host"
string val = Environment.Override.getValue( "memory" )         # SET 的替换值；无覆盖或 ENABLE/DISABLE → ""
```

> 无值约定：`Override.source/getValue` 与 `custom.getValue` 无值时返回 `""`（SL 无 `string?` 语法）。

---

## 7. SL 侧使用示例

```sl
# 平台分支（定义常量 == 运行时值）
if Environment.current.os == Environment.Platform.os.window
{
    Console.writeLine( "windows: " + Environment.current.osName )
}
else if Environment.current.os == Environment.Platform.os.linux
{
    Console.writeLine( "linux: " + Environment.current.triple )
}

# ISA / 设备（位集合 → has 查询）
if Environment.current.isaHas( Environment.Platform.isa.avx2 )
{
    Console.writeLine( "AVX2 available" )
}
if Environment.current.deviceHas( Environment.Platform.device.gpu )
{
    Console.writeLine( "GPU count: " + Environment.current.deviceCount( Environment.Platform.device.gpu ).toString() )
}

# 详情对象链式访问
Console.writeLine( Environment.current.cpu.physicalCores.toString() + " physical cores" )
Console.writeLine( "render api: " + Environment.current.render.apiList().toString() )
if Environment.current.network.isOnline { Console.writeLine( "online" ) }

# 版本下限（数值层 >=）
if Environment.current.osVersionNumber.major >= 10 { Console.writeLine( "modern windows" ) }

# 库探测
if Environment.current.libHas( "sqlite3", "3.40" ) { Console.writeLine( "sqlite3 ok" ) }

# 覆盖感知：被 --disable 后走软渲染
if Environment.Override.isDisabled( "device.gpu" ) || Environment.current.deviceHas( Environment.Platform.device.gpu ) == false
{
    Console.writeLine( "fallback to software render" )
}

# 诊断（原始值，不受覆盖影响）
Console.writeLine( "real gpu: " + Environment.probe.deviceHas( Environment.Platform.device.gpu ).toString() )

# 环境变量 / 目录 / 计时
Environment.env.setValue( "MY_FLAG", "1" )
Console.writeLine( Environment.sys.currentDirectory() )
Console.writeLine( Environment.sys.tickCount64().toString() )
```

---

## 8. 与相关机制分工

| 机制 | 时机 | 说明 |
|------|------|------|
| `platform.require` | **CVM 加载期** | 探测值与声明比对，决定模块能否运行（硬性）/ 告警降级（optional） |
| `platform.override` | **运行期** | 同一份 module.json 换传参换逻辑（测试矩阵、模拟环境） |
| `global.data`（.sp） | 运行期可读数据注入 | 注入**数据**，不做能力判定 |
| `global.macro` + `static if` | **编译期** | 分支裁剪，未选中分支不进 IR（产物更小），见 `md/project/static-if.md` |
| `compile.target` | 编译期目标架构 | 与 require.arch 无交集 → Warning 20034 |

选择：需要产物瘦身用 static if；需要运行时自适应分支用 `Environment.current`；需要拒绝在不满足环境运行用 `platform.require`。

---

## 9. 错误码速查

| LID | 级别 | 触发 |
|-----|------|------|
| 20036 | **Error（编译中止）** | platform.require / override.key 非法值或非法成员名 |
| 20033 | Error | compile.target 非法值（合法：AnyCPU/x86/x64/arm64/wasm32） |
| 20034 | Warning | compile.target 与 require.arch 无交集 |
| ProjectPlatformRequireUnknownField | Warning（忽略） | require 下未知字段 |

---

## 10. 测试用例与参考

| 文件 | 验证点 |
|------|--------|
| `test/BaseTest/ProjectTest.jsonc` | require 全字段正例 + override 四值形态（§5.6） |
| `test/Other/PlatformVariantTest/PlatformVariantTest.jsonc` | variants 顺序选择（§5.4） |
| `test/Other/PlatformNegTest/PlatformNegTest.jsonc` | 非法值（linuxx/arm65/sse5/vulkn）→ Error 20036 |
| `test/SpecialTest/PlatformAotPosTest/PlatformAotPosTest.jsonc` | require 最简形态（cpu.all） |
| `test/Other/PlatformOverrideTest/PlatformOverrideTest.sl` | Override 查询 + 覆盖行为 |
| `csimple_lang/scripts/test-platform-override.ps1` | 覆盖通道回归（25/25） |
| `csimple_lang/scripts/test-platform-lib.ps1` | lib 探测回归（26/26） |

参考源码：`source/Front/Lib/Core/Environment.sl`、`Environment/Platform_*.sl`、`source/Front/Project/ProjectJsoncLoader.cs`（ParsePlatform / ParseRequireField）、`csimple_lang/src/vm/platform/sl_runtime_override.*`、`csimple_lang/src/vm/sl_requirement.c`。设计文档：`md/design/PLATFORM_CAPABILITY_DESIGN.md`（§6.1 Schema / §8.5 覆盖 / §9.2-9.3 lib 与 custom / §13 定义枚举 / §14 变体）。
