# typealias (类型别名)

## 概述

`typealias` 用于为一个类型声明一个新的别名名称（类似 Swift 的 `typealias`）。别名只发生在 **编译期**，不会生成新的运行时类型。

该特性用于简化长泛型类型、容器类型或嵌套类型的书写，并提升可读性。

---

## 语法

```sl
typealias AliasName = TypeExpression
```

- `AliasName`：新的类型名称（标识符）
- `TypeExpression`：任意合法类型表达式（包括泛型与嵌套泛型）

示例：

```sl
typealias ArrayByte = Array<Byte>
typealias Map2String = Map<string, string>
typealias IntList = List<int>
```

---

## 作用域与限制

### 仅允许出现在 `Project{ Global(){} }` 中

`typealias` **仅允许**在 `.sp` 工程文件的 `Project{ Global(){} }` 函数体内定义。

例如：

```sl
Project
{
    Global()
    {
        typealias ArrayByte = Array<Byte>
        typealias Map2String = Map<string,string>
    }
}
```

禁止：

- 在普通 `.sl` 脚本文件中定义
- 在类/函数内部定义
- 在非 `Global()` 函数中定义

---

## 解析与替换规则

### 编译期替换

编译器在解析类型时，若遇到 `AliasName`，将其替换为对应的 `TypeExpression`，等价于直接书写原类型。

例如：

```sl
typealias ArrayByte = Array<Byte>

ArrayByte a = ArrayByte()
```

等价于：

```sl
Array<Byte> a = Array<Byte>()
```

### 不生成新类型

- `typealias` 不产生新的 `MetaClass` / `RuntimeClass`
- `AliasName` 仅作为解析阶段的符号映射存在

---

## 冲突规则

- `AliasName` 不能与现有 `class` / `data` / `enum` / `namespace` / 已有 `typealias` 同名
- 不允许循环引用：
  - 直接循环：`typealias A = A`
  - 间接循环：`typealias A = B; typealias B = A`

---

## 泛型与模板类型

`TypeExpression` 可以为：

- 普通类型：`int`、`string`
- 容器类型：`Array<T>`、`List<T>`、`Map<K,V>`
- 多层嵌套：`Array<Map<string,string>>`

例如：

```sl
typealias KV = Map<string,string>
typealias KVList = List<KV>
```

---

## 错误提示（建议）

- 非 `.sp` 文件或非 `Global()` 中使用：
  - `Error typealias 只允许在 Project{ Global(){} } 中定义`
- 重名：
  - `Error typealias 名称已存在`
- 未知类型：
  - `Error typealias 目标类型不存在`
- 循环引用：
  - `Error typealias 存在循环引用`
