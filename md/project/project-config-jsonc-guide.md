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
  }
}
```

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
