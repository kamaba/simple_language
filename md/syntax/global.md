# global 关键字

`global` 用于访问工程级的全局能力。当前语义由两部分组成：

- `global.xxx` / `global.func()`：映射到 `.sp` 里的 `Project{}` 静态成员与函数
- `global.data`：来自工程 `jsonc` 配置中的 `global.data` 注入数据

## 1. `.sp` 中 `Project{}` 成员调用

示例（`Core.sp`）：

```s
Project
{
    float Pi = 3.14f

    print(object str)
    {
        SystemPrint(str)
    }
}
```

在任意业务类中可直接用 `global` 调用：

```s
GlobalTest
{
    static fun()
    {
        global.println("Pi -> " + global.Pi.toString())
        global.print("hello from global.print")
    }
}
```

## 2. `jsonc` 的 `global.data` 注入

示例（`Core.jsonc`）：

```jsonc
"global": {
  "data": {
    "var1": 12,
    "arrvar1": [1, 2, 3, 4, 5],
    "vardata2": {
      "a": 10,
      "b": 20.1
    }
  }
}
```

访问方式：

- 基础值：`global.var1`
- 数组：`global.arrvar1[0]`
- 对象：`global.vardata2.a`

## 3. 推荐测试用例

仓库内已提供 `test/BaseTest/GlobalTest.sl`，覆盖：

- `.sp Project{}` 暴露成员：`global.Pi`、`global.print`、`global.println`
- `jsonc global.data` 基础值：`global.var1`
- `jsonc global.data` 数组：`global.arrvar1[0]`、`global.arrvar1[4]`
- `jsonc global.data` 对象：`global.vardata2.a`、`global.vardata2.b`

## 4. 常见注意点

- 访问 `global.xxx` 前，需确保对应字段在 `Project{}` 或 `global.data` 已定义。
- `global.data` 的对象会转换为内部 `MetaData` 树，支持链式访问。
- 若工程内多个配置文件同名项冲突，以当前编译目标工程的配置为准。
