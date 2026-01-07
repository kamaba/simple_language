# TokenToFileMeta - 快速开始指南

## ?? 5 分钟快速理解

### 是什么？
**TokenToFileMeta** 是一个 DFA（词法分析）+ 递归下降解析器，用于将 Token 流直接转换为 FileMeta 结构。

### 怎么用？
```csharp
// 1. 创建实例
var parser = new TokenToFileMeta(fileMeta, tokenList);

// 2. 调用 ParseTokensToFileMeta()
parser.ParseTokensToFileMeta();

// 3. 完成！fileMeta 已填充
```

### 核心概念

| 概念 | 说明 | 类比 |
|------|------|------|
| **DFA** | 状态机（Initial → InImport → Initial） | 汽车的档位 |
| **递归下降** | 递归调用 Parse* 方法解析各级规则 | 手工组装零件 |
| **ParseContext** | 维护当前状态和括号深度 | 工作台 |
| **Token** | 最小的语言单元 | 字母 |

---

## ?? 解析流程（可视化）

```
Token Stream
   ↓
┌──────────────────────────┐
│ ParseCompilationUnit()   │ ← 入口
└────────┬─────────────────┘
         │
    ┌────┴────┬─────────┬──────────┐
    ↓         ↓         ↓          ↓
 import   namespace   class      (其他)
    │         │         │
    ↓         ↓         ↓
 Parse...  Parse...  Parse...
    │         │         │
    ↓         ↓         ↓
 Create   Create    Create
 FileMeta FileMeta  FileMeta
    │         │         │
    └─────────┴─────────┘
         ↓
    FileMeta (完整)
```

---

## ?? 主要方法一览

### 公共接口
```csharp
// 主入口 - 调用这个方法！
public void ParseTokensToFileMeta()
```

### DFA 状态管理
```csharp
// 内部使用 - 状态转移
private void TransitionState(DFAState newState)
private void PopState()
```

### 编译单元级别
```csharp
// 按顺序调用各个解析方法
private void ParseImportDirective()        // import Std;
private void ParseNamespaceDeclaration()   // namespace A.B;
private void ParseClassDeclaration()       // class Foo { }
```

### Token 操作（工具）
```csharp
// 最常用的三个
private Token CurrentToken              // 看当前 Token
private Token Consume()                 // 用掉当前 Token
private bool Match(ETokenType)          // 检查类型匹配
```

---

## ?? 用例：解析一个简单程序

### 输入代码
```dart
import Std;
namespace App;
public class MyClass { }
```

### 对应的 Token 流
```
[import] [Std] [;] [namespace] [App] [;] [public] [class] [MyClass] [{] [}]
```

### 执行步骤

#### 1?? ParseCompilationUnit() 启动
```
CurrentIndex = 0
CurrentToken = import
→ 调用 ParseImportDirective()
```

#### 2?? ParseImportDirective()
```
Match(import) ? → YES → Consume()
ParseQualifiedName() → 读取 [Std]
Match(;) ? → YES → Consume()
创建 FileMetaImportSyntax([Std])
返回
```

#### 3?? ParseCompilationUnit() 继续
```
CurrentIndex = 3
CurrentToken = namespace
→ 调用 ParseNamespaceDeclaration()
```

#### 4?? ParseNamespaceDeclaration()
```
Match(namespace) ? → YES → Consume()
ParseQualifiedName() → 读取 [App]
Match(;) ? → YES → Consume()
创建 NamespaceStatementBlock([App])
返回
```

#### 5?? ParseCompilationUnit() 继续
```
CurrentIndex = 6
CurrentToken = public
IsClassDeclarationStart() ? → YES
→ 调用 ParseClassDeclaration()
```

#### 6?? ParseClassDeclaration()
```
ParseModifiers() → 读取 [public]
className = MyClass
ParseTypeParameters() → (无泛型)
Match(extends) ? → NO (跳过)
Match(interface) ? → NO (跳过)
Match({) ? → YES → SkipClassBody()
  ├─ braceDepth++ (1)
  ├─ Consume() → }
  └─ braceDepth-- (0)
返回
```

#### 7?? ParseCompilationUnit() 完成
```
CurrentIndex = 11 (EOF)
循环结束，返回
```

### 最终结果
```
FileMeta 包含：
  ├─ FileMetaImportSyntax([Std])
  ├─ NamespaceStatementBlock([App])
  └─ FileMetaClass(public, MyClass, ...)
```

---

## ?? DFA 状态一览表

| 状态 | 含义 | 触发条件 | 下一个状态 |
|------|------|---------|----------|
| **Initial** | 等待声明 | - | InImport/InNamespace/InClass |
| **InImport** | 解析 import | 见到 import | Initial（见到 ;） |
| **InNamespace** | 解析 namespace | 见到 namespace | Initial（见到 ;） |
| **InClass** | 解析 class | 见到 class/interface/enum | Initial（见到 }） |
| **InFunction** | 解析函数 | 见到 function 关键字 | InBlock/InClass |
| **InBlock** | 解析语句块 | 见到 { | InFunction |
| **InExpression** | 解析表达式 | 见到表达式 | InBlock |

---

## ?? Token 类型速查

### 声明关键字
- `import` - 导入声明
- `namespace` - 命名空间
- `class`, `interface`, `enum`, `data` - 类定义

### 修饰符
- `public`, `private`, `protected` - 访问修饰符
- `static`, `final`, `const` - 修饰符
- `partial` - 部分定义

### 符号
- `;` - 语句结束
- `.` - 点号（限定名）
- `,` - 逗号（列表分隔）
- `<`, `>` - 泛型参数
- `{`, `}` - 代码块
- `(`, `)` - 参数列表
- `[`, `]` - 数组

### 其他
- `extends` - 继承
- `interface` - 接口实现
- `Identifier` - 标识符（变量/类名）

---

## ?? 配置选项

### 当前无配置选项
（DFA 状态机配置是硬编码的，可根据需要修改）

### 如需修改，编辑这些部分：
```csharp
// DFA 状态定义
enum DFAState { ... }  // Line 38

// ParseContext 深度限制
m_Context.braceDepth   // 可设置最大值检查

// Token 匹配规则
MatchAny(...) 方法中的参数列表
```

---

## ?? 调试技巧

### 1. 打印当前状态
```csharp
Debug.WriteLine($"State: {m_Context.currentState}");
Debug.WriteLine($"Token: {CurrentToken?.lexeme}");
Debug.WriteLine($"Index: {m_TokenIndex}");
```

### 2. 追踪状态转移
```csharp
private void TransitionState(DFAState newState)
{
    Debug.WriteLine($"Transition: {m_Context.currentState} → {newState}");
    // ... 原代码
}
```

### 3. 检查括号深度
```csharp
Debug.WriteLine($"Brace: {m_Context.braceDepth}, " +
                $"Paren: {m_Context.parenDepth}, " +
                $"Bracket: {m_Context.bracketDepth}");
```

### 4. 查看 Token 流
```csharp
// 调用前
foreach (var token in m_TokenList.Take(20))
{
    Debug.WriteLine($"{token.type}: {token.lexeme}");
}
```

---

## ?? 常见问题

### Q1: 为什么要用 DFA？
**A:** 明确管理解析状态，避免混乱。就像开车时，档位决定了你能做什么。

### Q2: 为什么用递归下降？
**A:** 简单直观，易于实现和维护。每个语法规则对应一个方法。

### Q3: 可以处理错误代码吗？
**A:** 可以。使用 try-catch 捕获异常，记录错误到日志，继续解析下一个语句。

### Q4: 性能如何？
**A:** O(n) 时间复杂度（n = Token 数），O(d) 空间复杂度（d = 嵌套深度）。非常高效。

### Q5: 支持哪些语言特性？
**A:** 目前支持：
- [x] Import 语句
- [x] Namespace 声明
- [x] Class/Interface/Enum/Data 定义
- [x] 泛型参数（`<T>`）
- [x] 继承（`extends`）
- [x] 接口实现（`interface`）
- [x] 修饰符（`public`, `static` 等）

### Q6: 如何添加语句解析？
**A:** 在 `DFAState` 中添加 `InStatement`，然后创建 `ParseStatement()` 方法。（见扩展指南）

---

## ?? 下一步

### 已完成 ?
- [x] DFA + RDP 框架
- [x] 编译单元解析
- [x] Import/Namespace/Class 解析

### 待做 ??
1. **语句解析** - If/While/For/Return 等
2. **表达式解析** - 算术、逻辑、赋值表达式
3. **函数体解析** - 函数参数和返回类型
4. **错误恢复** - 更好的错误定位和恢复
5. **优化** - 性能改进和内存优化

---

## ?? 文档导航

| 文档 | 内容 | 读者 |
|------|------|------|
| **TOKENTOFILEMETA_README.md** | 概览和主要方法 | 入门用户 |
| **ARCHITECTURE_DFA_RDP.md** | 详细设计和扩展 | 架构师/开发者 |
| **IMPLEMENTATION_SUMMARY.md** | 实现细节总结 | 维护人员 |
| **QUICK_START.md** (本文) | 快速上手 | 新手 |

---

## ?? 支持

有问题？
1. 查看详细架构文档：`ARCHITECTURE_DFA_RDP.md`
2. 查看代码注释：`source/Compile/Parse/TokenToFileMeta.cs`
3. 运行示例代码
4. 检查日志输出

---

**Happy Parsing! ??**

最后更新: 2025-01-15  
编译状态: ? 成功
