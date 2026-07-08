
# 简介

本目录记录 S 语言的语法要点与使用示例（Program Syntax）。S 语言受 Dart/C# 启发，目标是提供简洁的脚本语法、面向对象特性、泛型/模板与一个可编译到 IR 的管线。

本文档集合覆盖：
- 基本类型与字面量（数字、字符串、布尔）
- 变量声明与赋值规则
- 控制流（if/for/while/switch）
- 类、接口、抽象与继承
- 函数定义、重载、override 与修饰符（static/abstract/final）
- 集合类型（Array/List/Map/Set/Queue/Stack/Tuple）
- 模板与泛型语法
- 对象创建与构造（_init_）、匿名对象字面量

此外，编译器会把语言结构降低为中间表示（IR），常见 IR 操作示例：NewArray、CallVirt、Cast、Pop、Convert、Switch 等。语言层面请优先参考本目录下各章节的语法说明与示例。

约定：示例代码使用 .s 风格伪代码；关键字与标识采用小写（例如 `class`、`if`、`for`）。
