# 语言基础（Base Syntax）

本节概述 S 语言最基础的使用与约定，包括项目结构、文件组织、注释、标识符与关键字。

概览
- 源文件通常以 `.s` 或 `.sp` 组织；`.sp` 用作项目入口配置文件（ProjectEnter）。
- 语言受 C#/Dart 启发，支持类、data、enum、interface、函数、泛型与模块化。

项目与文件
- 使用 `ProjectEnter { ... }` 作为项目入口和静态方法的容器（例如 `static Main()`）。
- `ProjectConfig`（常声明为 `const data`）用于列出编译文件与全局变量配置。

注释
- 单行注释: `// 注释内容` 或 `# 注释内容`。
- 多行/块注释: 使用 `#! ... !#` 形式，支持嵌套与 markdown 标记（例如 `#!md ... !#md`）。

标识符与关键字
- 标识符必须以字母、下划线或 `@` 开头，后续允许字母、数字和下划线。
- 标识符区分大小写，关键字不可作为标识符使用。
- 常用关键字示例：`namespace, import, class, enum, data, interface, abstract, override, static, public, private, if, elif, else, for, while, dowhile, switch, case, break, continue, ret, is`。

示例（最小工程）

```s
// test_project.sp
ProjectEnter {
    static Main() {
        var r = Rectangle(10.0f, 20.0f);
        r.Display();
    }
}

// test.s
Rectangle {
    float length = 0.0f;
    float width = 0.0f;
    _init_(float l, float w) { this.length = l; this.width = w; }
    Display() { Debug.Write("Area: " + this.length * this.width); }
}
```

更多语法细节请参见本目录下的主题文档（函数、类、表达式、控制流、集合等）。




