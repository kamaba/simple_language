# InputArgsTest 工程说明

- 工程入口文件：`ProjectTest.sp`
- 工程配置文件：`ProjectTest.jsonc`
- 测试类文件：`InputArgsTest.sl`
- 注意：`ProjectTest.md` 是编译器自动生成的工程指南（每次编译会覆盖），功能文档以本文件为准

本工程是 `global._inputArgs`（系统集成的 CLI 程序参数）的专用测试用例。

---

## `global._inputArgs` 功能说明

### 是什么

`_inputArgs` 是一个**系统集成的静态成员**，类型为 `Array<Object>`，挂在当前运行工程的 `Project` 类上：

```
Project
{
    static Array<Object> _inputArgs = []   # 前端自动注入，源码不可见
}
```

源码中**无需声明**，直接通过 `global._inputArgs` 读取即可：

```sl
inputArgs = global._inputArgs
argCount  = inputArgs.length
first     = inputArgs[0].toString()
```

### 参数从哪里来

C VM（`csimple_lang`）运行时通过命令行传入，规则：

```
csimple_lang run <module.json> -- 参数1 参数2 参数3 ...
```

- `--` 之后的**所有**参数（包括以 `-` 开头的，如 `--mode=fast`、`-v`）都会被收集进 `_inputArgs`
- 模块路径之后的**裸位置参数**同样会被收集（`csimple_lang run mod.json a b` 等价于 `-- a b`）
- **不传任何参数时，`_inputArgs` 是一个空数组**（`length == 0`），不会是 `null`

### 集成链路（对源码透明）

| 阶段 | 行为 |
|---|---|
| 前端编译 | `ProjectClass` 自动为 `Project` 类注入静态成员 `_inputArgs`（`Array<Object>`，初值 `[]`），用户源码无感知 |
| CLI 收集 | `cli.c` 解析 `--` 之后 / 模块路径之后的参数，存入 `CliOptions.program_args` |
| C VM 填充 | `cli_command.c` 定位主模块的 `Project` 类，`vm_fill_input_args` 在静态成员初始化之后、入口方法调度之前，把参数构建为字符串对象填入 `_inputArgs` |
| 运行时读取 | 源码经 `global._inputArgs` 访问该静态槽位 |

> 注意：`_inputArgs` 是**内部使用**的集成成员，所以看不到它的集成过程（源码中不出现声明），但在运行时会自动生成相关逻辑。

### 实现要点（排坑记录）

- **主模块定位**：依赖模块（如 `Core`）也有同名 `Project` 类，填充时按 `SLClassPackage*` 指针精确定位**主模块**（包列表末尾）的 `Project`，不能按短名匹配
- **元素装箱**：字符串元素必须以 `EVMType_Object` 引用形态存入 `Array<Object>` 槽；若以 `EVMType_String` 形态存入会触发 object-scalar 重新装箱，导致包装指针被误读为 `char*`（元素内容变空/错乱）

---

## 测试用例

`InputArgsTest.fun()` 内置断言（`check`，OK / FAIL）：

| 用例 | 场景 | 断言 |
|---|---|---|
| T1 | 任意运行 | `global._inputArgs != null`（数组始终存在） |
| T2 | 无参数运行 | `length == 0`（空数组而非 null） |
| T3 | 带参数运行 | `length` 与传入个数一致 |
| T4 | 带参数运行 | 元素内容与传入顺序一致（`toString()` 逐个比较） |
| T5 | 带参数运行 | 元素可直接参与字符串拼接（隐式 toString / 装箱归一化链路） |
| T6 | 任意运行 | 与 jsonc `data` 注入的静态成员（`global.var1`）共存互不影响 |

## 编译

```bash
# sl = simple_language\source\Front\bin\Debug\net8.0\SimpleLanguageFront.exe
sl compile -p f:\project\lang\simple_language\test\InputArgsTest\ProjectTest -e ir --no-banner
```

产物：`simple_language\out\export\InputArgsTest\ProjectTest.module.json`

## 运行与预期输出

以下用 `$mod` 指代 `f:\project\lang\simple_language\out\export\InputArgsTest\ProjectTest.module.json`，
`$vm` 指代 `f:\project\lang\csimple_lang\build\Debug\bin\csimple_lang.exe`。

### 方式一：无参数（验证空数组）

```bash
$vm run $mod
```

预期输出：

```
===== InputArgsTest _main_ start =====
========== InputArgsTest (start) ==========
[InputArgsTest] T1 global._inputArgs 始终非 null : OK
  (本次运行参数个数: 0)
[InputArgsTest] T2 无参数时 length == 0 : OK
[InputArgsTest] T6 jsonc global.var1 共存正常 : OK
========== InputArgsTest (end) ==========
===== InputArgsTest _main_ end =====
```

### 方式二：带参数（验证长度与内容）

```bash
$vm run $mod -- hello 42 world
```

预期输出：

```
===== InputArgsTest _main_ start =====
========== InputArgsTest (start) ==========
[InputArgsTest] T1 global._inputArgs 始终非 null : OK
  (本次运行参数个数: 3)
[InputArgsTest] T3 有参数时 length == 3 : OK
[InputArgsTest] T4[0] 内容为 hello : OK
[InputArgsTest] T4[1] 内容为 42 : OK
[InputArgsTest] T4[2] 内容为 world : OK
  遍历拼接: [0]=hello [1]=42 [2]=world
[InputArgsTest] T5 元素可直接字符串拼接 : OK
  global._inputArgs[0] -> hello
  global._inputArgs[1] -> 42
  global._inputArgs[2] -> world
[InputArgsTest] T6 jsonc global.var1 共存正常 : OK
========== InputArgsTest (end) ==========
===== InputArgsTest _main_ end =====
```

### 方式三：`-` 开头参数（验证 CLI 收集规则）

```bash
$vm run $mod -- --mode=fast -v extra
```

预期：参数个数 3，元素依次为 `--mode=fast`、`-v`、`extra`（T3/T4 断言 FAIL 属预期，此场景仅观察遍历打印输出）：

```
  global._inputArgs[0] -> --mode=fast
  global._inputArgs[1] -> -v
  global._inputArgs[2] -> extra
```
