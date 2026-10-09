# SimpleLanguage 工程配置（JSONC）说明

> 适用范围：`Front` 工程编译配置
> 
> 当前已从 `*.toml` 迁移为 `*.jsonc`。

## 1. 配置文件命名规则

- 工程入口文件：`<ProjectName>.sp`
- 配置文件：`<ProjectName>.jsonc`
- 二者必须同名并位于同一目录。

例如：

- `Core.sp`
- `Core.jsonc`

## 2. 当前支持的 JSONC 结构

```jsonc
{
  "project": {
    "name": "Core",
    "desc": "Sample project",
    "mainVersion": 1,
    "subVersion": 0,
    "buildVersion": 0
  },
  "source": {
    "root": "samples/SLang/source",
    "entryFile": "Main.sl"
  },
  "compile": {
    "optimize": false,
    "target": "x64",
    "debug": true,
    "isUseForceSemiColonInLineEnd": true,
    "isForceUseClassKey": false,
    "isSupportDoublePlus": false
  },
  "compileFiles": {
    "files": [
      {
        "path": "Object.sl",
        "group": "core",
        "tag": "ir",
        "ignore": false,
        "priority": 0
      }
    ]
  },
  "compileFilter": {
    "isAllGroup": true,
    "isAllTag": true,
    "groups": [],
    "tags": []
  },
  "global": {
    "imports": ["Std.Console"],
    "replace": {
      "DEBUG": "true"
    },
    "data":{
      "pi1":3.15,
      "book":{
        "name":"ashu",
        "price":10
      }
    },
    "macro": {
      "platform": "Win32",
      "useFastMath": true,
      "maxThreads": 8
    }
  },
  "references": [
    { "path": "lib/std" }
  ],
  "struct": {
    "tree": [
      { "namespace": "Core" },
      { "class": "Std.Console" }
    ]
  },
  "export": {
    "moduleName": "Core",
    "outputDir": "../../out/export",
    "publicExport": "public",
    "includeMetadata": true,
    "debugText": {
      "outputDir": "",
      "code": false,
      "token": false,
      "node": false,
      "file": false,
      "meta": false,
      "ir": false
    }
  }
}
```

## 2.1 `global` 段说明

- `global.imports`：全局导入命名空间。
- `global.replace`：文本宏替换表（`$xxx`，见 `md/syntax/marco.md`）。
- `global.data`：注入为 Project 数据成员，运行期通过 `global.<name>` 读取（旧版根级 `"data"` 仍兼容）。
- `global.macro`：**编译期宏数据面**（原 static if 条件编译已废弃收编为 `@Exclude` attribute，写 `static if` 报 LID 21467），
  与 `data` 字段定义方式一样，值为布尔/数值/字符串；宏值编译期定稿，运行期可只读访问 `global.macro.X`。
  宏值只能在 `.sp` 的 `CompileBefore(){}` 中修改，详见 `md/project/static-if.md`。
  编译前也可由外部环境注入覆盖初值：CLI `--macro name=value`（可重复）或环境变量 `SL_MACRO_<name>=<value>`，
  优先级 `jsonc 初值 < 环境变量 < CLI < CompileBefore()`。

## 2.2 `export` 段说明

- `export.moduleName`：导出模块名（生成 `<moduleName>.module.json`）。
- `export.outputDir`：导出目录（相对 `.jsonc` 所在目录）。
- `export.publicExport`：**public 类导出门槛三态**（默认 `public`），见下表。
- `export.includeMetadata`：是否在 SLIR 包内携带元数据。
- `export.debugText`：DebugCode 调试快照开关（code/token/node/file/meta/ir），见 `md/ai/DEBUG_WORKFLOW.md`。

### publicExport 三态

源码中类级显式导出标记为 `extern` 关键字（`extern class` / `extern data` / `extern enum`，语义见 `md/syntax/class.md` 导出权限章节）；
`publicExport` 只决定**未显式标记的类**（`public class` 与无标记 `class`）是否随包导出：

| 取值 | extern 标记类 | public / 无标记类 | private / protected 类 |
|------|--------------|-------------------|------------------------|
| `"none"` | 导出（包内 permission 记 Export） | **不导出** | 不导出 |
| `"public"`（默认） | 导出（permission 记 Export） | 照常导出（permission 记 Public） | 不导出 |
| `"extern"` | 导出（permission 记 Export） | 照常导出，且包内 permission 记 Export（对外表现为显式导出） | 不导出 |

判定优先级：`extern` 标记恒导出 > `private`/`protected` 恒不导出 > 其余按上表档位过滤。
探针用例：`test/Other/ExportPermTest/`（切换 `publicExport` 三档重编译并检查 `module.json` 的 `classList[].permission`）。

> 注意：旧语法 `export class` / `export data` / `export enum` 的 `export` 关键字已移除权限语义（映射为不导出），
> 类级显式导出统一改用 `extern` 关键字声明。

## 3. CLI 与配置联动

### `sl new project -p [path] [name]`

会生成：

- `<name>.sp`
- `<name>.jsonc`
- `Main.sl`

### `sl new classfile [filename]`

会：

1. 创建 `*.sl` 文件
2. 将文件注册到 `<ProjectName>.jsonc` 的 `compileFiles.files`

### `sl c`

编译当前工程（自动查找当前目录 `.sp`）。

### `sl c -e ir`

编译并导出 IR（`SLIR`）。

## 4. 已同步的内置工程示例

- `source/Front/Lib/Core/Core.jsonc`（由原 `Core.toml` 同步转换）

## 5. 迁移中发现的问题 / 注意点

1. `ProjectTomlLoader` 已不再参与流程，当前加载路径使用 `ProjectJsoncLoader`。
2. `importFiles` 字段已在 `Core.jsonc` 保留，但当前 `ProjectJsoncLoader` 暂未消费该字段（如需生效需补 loader 映射）。
3. `sl new classfile` 对 `jsonc` 的注册采用字符串插入方式，要求 `compileFiles.files` 数组结构存在并格式基本正常。
4. 如果工程目录下存在多个 `.sp`，当前默认取第一个，建议保持单工程目录单 `.sp`。

## 6. 建议

- 新工程统一使用 `jsonc`，不再新增 `toml`。
- 后续可增加：
  - `importFiles` 的强类型映射与运行时使用
  - 更稳健的 JSON DOM 写回（替代文本插入）
  - 多 `.sp` 目录下的显式目标选择参数
