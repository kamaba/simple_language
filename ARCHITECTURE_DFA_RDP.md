# TokenToFileMeta - DFA + 递归下降解析器详细设计文档

## 文档目录
1. [整体架构](#整体架构)
2. [DFA 详解](#dfa-详解)
3. [递归下降解析器](#递归下降解析器)
4. [解析流程演示](#解析流程演示)
5. [扩展指南](#扩展指南)

---

## 整体架构

### 架构图

```
Token Stream (tokenList)
         ↓
    ┌────────────────────┐
    │  TokenToFileMeta   │
    │  (DFA + RDP)       │
    └────────────────────┘
         ↓
    ┌────────────────────┐
    │  ParseContext      │
    │  - DFA State       │
    │  - Bracket Depth   │
    │  - State Stack     │
    └────────────────────┘
         ↓
    ┌────────────────────┐
    │ Recursive Descent  │
    │ Parser Methods     │
    └────────────────────┘
         ↓
    FileMeta Structure
```

### 三层设计

#### 第一层：Token 流管理
```csharp
private int m_TokenIndex              // 当前位置
private List<Token> m_TokenList       // Token 列表
private Token CurrentToken            // 当前 Token
private Token Consume()               // 消费并前进
```

#### 第二层：DFA 状态机
```csharp
enum DFAState { Initial, InImport, InNamespace, InClass, ... }
ParseContext m_Context               // 维护状态栈和深度
TransitionState(newState)            // 状态转移
PopState()                           // 返回上一状态
```

#### 第三层：递归下降解析
```csharp
ParseCompilationUnit()               // 递归入口
ParseImportDirective()               // 各级别解析
ParseClassDeclaration()
...
```

---

## DFA 详解

### 状态定义

```
┌─────────┐
│ Initial │ (初始状态 - 等待任何顶级声明)
└────┬────┘
     │
     ├──→ import → InImport → Initial
     ├──→ namespace → InNamespace → Initial
     ├──→ class/interface/enum/data → InClass → Initial
     │
     └──→ Finished/EOF (终止)
```

### 状态转移表

| 当前状态 | 输入符号 | 下一状态 | 动作 |
|---------|---------|---------|------|
| Initial | import | InImport | 调用 ParseImportDirective() |
| Initial | namespace | InNamespace | 调用 ParseNamespaceDeclaration() |
| Initial | class/interface | InClass | 调用 ParseClassDeclaration() |
| InImport | ; | Initial | 创建 FileMetaImportSyntax，返回 |
| InNamespace | ; | Initial | 创建 NamespaceStatementBlock，返回 |
| InClass | { | InClass | 进入 SkipClassBody() |
| InClass | } | Initial | 完成类定义，返回 |

### 括号深度追踪

```csharp
class ParseContext
{
    int braceDepth = 0;     // { } 计数 - 类/函数体
    int parenDepth = 0;     // ( ) 计数 - 参数列表
    int bracketDepth = 0;   // < > 计数 - 泛型参数
}

// 使用示例
ParseTypeParameters():
  遇到 '<' → bracketDepth++
  遇到 '>' → bracketDepth--
  当 bracketDepth == 0 → 完成解析
```

### 状态栈管理

```csharp
Stack<DFAState> stateStack;

TransitionState(newState):
  stateStack.Push(currentState)
  currentState = newState

PopState():
  currentState = stateStack.Pop()
```

**用途**：支持嵌套结构（如函数内的语句块）

---

## 递归下降解析器

### 解析方法命名约定

- `Parse<Rule>()` - 解析语法规则
- `Skip<Structure>()` - 跳过结构
- `Match<Type>()` / `Is<Type>()` - 检查类型

### 方法层次结构

```
ParseCompilationUnit()              ← 最高层：编译单元
  ├─ ParseImportDirective()         ← 导入语句
  ├─ ParseNamespaceDeclaration()    ← 命名空间
  └─ ParseClassDeclaration()        ← 类定义
       ├─ ParseModifiers()          ← 修饰符
       ├─ ParseQualifiedName()      ← 限定名
       ├─ ParseTypeParameters()     ← 泛型参数
       ├─ ParseInterfaceList()      ← 接口列表
       └─ SkipClassBody()           ← 跳过类体
```

### EBNF 转 递归下降

#### 规则 1：序列（顺序执行）

**EBNF:**
```ebnf
import_directive = 'import' qualified_name ';'
```

**递归下降:**
```csharp
private void ParseImportDirective()
{
    Match(Import) || return;        // 期望 'import'
    ParseQualifiedName();           // 期望 qualified_name
    Match(SemiColon) || return;     // 期望 ';'
}
```

#### 规则 2：选择（or 分支）

**EBNF:**
```ebnf
class_keyword = 'class' | 'interface' | 'enum' | 'data'
```

**递归下降:**
```csharp
if (MatchAny(Class, Interface, Enum, Data))
{
    Token classKeyword = Consume();
}
```

#### 规则 3：重复（*）

**EBNF:**
```ebnf
qualified_name = identifier ('.' identifier)*
```

**递归下降:**
```csharp
private List<Token> ParseQualifiedName()
{
    List<Token> names = new List<Token>();
    while (Match(Identifier))           // 重复
    {
        names.Add(Consume());
        if (Match(Period)) Consume();
        else break;
    }
    return names;
}
```

#### 规则 4：可选（？）

**EBNF:**
```ebnf
class_declaration = ... ('extends' qualified_name)? ...
```

**递归下降:**
```csharp
if (Match(Extends))                 // 可选
{
    Consume();
    baseClass = ParseQualifiedName();
}
```

### 错误处理策略

```csharp
private void ParseImportDirective()
{
    try
    {
        if (!Match(ETokenType.Import))
            return;                      // 预测失败，返回
        
        Consume();
        List<Token> path = ParseQualifiedName();
        
        if (Match(ETokenType.SemiColon))
            Consume();
        // 成功
    }
    catch (Exception ex)
    {
        Log.AddInStructFileMeta(EError.None, 
            $"Import 解析错误: {ex.Message}");
    }
}
```

---

## 解析流程演示

### 示例代码

```dart
import Std;
import CSharp.System;

namespace Application.Core;

public class MyClass<T> extends BaseClass interface IInterface {
    int x = 0;
}
```

### 解析轨迹

```
1. TokenIndex=0, Current=import
   → TransitionState(InImport)
   → ParseImportDirective()
       ? Match(import) ? Consume()
       ? ParseQualifiedName() → [Std]
       ? Match(;) ? Consume()
       ? 创建 FileMetaImportSyntax([Std])
   → TransitionState(Initial)

2. TokenIndex=2, Current=import
   → 同上，ParseImportDirective()
       ? ParseQualifiedName() → [CSharp, System]
       ? 创建 FileMetaImportSyntax([CSharp, System])

3. TokenIndex=5, Current=namespace
   → TransitionState(InNamespace)
   → ParseNamespaceDeclaration()
       ? Match(namespace) ? Consume()
       ? ParseQualifiedName() → [Application, Core]
       ? Match(;) ? Consume()
       ? 创建 NamespaceStatementBlock([Application, Core])
   → TransitionState(Initial)

4. TokenIndex=9, Current=public
   → IsClassDeclarationStart() = true
   → TransitionState(InClass)
   → ParseClassDeclaration()
       ? ParseModifiers()
           ? MatchAny(public, private, ...) ? public
           ? modifiers = [public]
       ? Match(class) ? className = MyClass
       ? Match(<) ? ParseTypeParameters()
           ? bracketDepth = 1
           ? Match(T) ? typeParams = [T]
           ? Match(>) bracketDepth = 0
       ? Match(extends) ? baseClass = [BaseClass]
       ? Match(interface) ? ParseInterfaceList()
           ? interfaces = [[IInterface]]
       ? Match({) ? SkipClassBody()
           ? braceDepth = 1
           ? 扫过 int x = 0
           ? Match(}) braceDepth = 0
   → TransitionState(Initial)

5. TokenIndex=30, Current=Finished
   → 循环结束，解析完成
```

---

## 扩展指南

### 扩展 1：添加语句解析

```csharp
// 在 DFAState 中添加
enum DFAState {
    // ...existing...
    InStatement = 7,   // 新增
}

// 添加解析方法
private void ParseStatement()
{
    TransitionState(DFAState.InStatement);
    
    Token current = CurrentToken;
    
    if (Match(ETokenType.If))
        ParseIfStatement();
    else if (Match(ETokenType.While))
        ParseWhileStatement();
    else if (Match(ETokenType.For))
        ParseForStatement();
    // ... 其他语句
    
    TransitionState(DFAState.InBlock);
}

private void ParseIfStatement()
{
    Consume();  // 'if'
    Match(ETokenType.LeftPar) && Consume();
    // 解析条件表达式
    Match(ETokenType.RightPar) && Consume();
    // 解析语句块
}
```

### 扩展 2：添加表达式解析

```csharp
private FileMetaBaseTerm ParseExpression()
{
    TransitionState(DFAState.InExpression);
    
    FileMetaBaseTerm left = ParseAssignment();
    
    // 处理三元运算符等
    if (Match(ETokenType.QuestionMark))
    {
        // 解析三元表达式
    }
    
    PopState();
    return left;
}

private FileMetaBaseTerm ParseAssignment()
{
    FileMetaBaseTerm expr = ParseConditional();
    
    if (MatchAny(ETokenType.Assign, ETokenType.PlusAssign))
    {
        Token op = Consume();
        FileMetaBaseTerm right = ParseAssignment();
        // 创建赋值节点
    }
    
    return expr;
}

private FileMetaBaseTerm ParseConditional()
{
    // ... 继续递归下降
    return ParseLogicalOr();
}
```

### 扩展 3：错误恢复

```csharp
private void Synchronize()
{
    Consume();
    
    while (!IsAtEnd())
    {
        // 寻找语句边界
        if (Previous().type == ETokenType.SemiColon)
            return;
        
        // 寻找下一个语句开始
        if (MatchAny(
            ETokenType.Class,
            ETokenType.Function,
            ETokenType.Var,
            ETokenType.For,
            ETokenType.If,
            ETokenType.While,
            ETokenType.Return))
        {
            return;
        }
        
        Consume();
    }
}

// 使用
try
{
    ParseStatement();
}
catch (Exception)
{
    Synchronize();
}
```

---

## 性能特性

| 特性 | 性能 | 说明 |
|------|------|------|
| 单遍扫描 | O(n) | 线性时间复杂度 |
| 内存占用 | O(d) | d = 嵌套深度 |
| 状态栈 | O(d) | 支持嵌套结构 |
| 回溯 | 否 | 无需回溯的 LL(1) 解析器 |

---

## 相关规范

- [Dart Language Specification](https://dart.dev/guides/language/spec)
- [EBNF Notation](https://en.wikipedia.org/wiki/Extended_Backus%E2%80%93Naur_form)
- [Recursive Descent Parsing](https://en.wikipedia.org/wiki/Recursive_descent_parser)
- [Deterministic Finite Automaton](https://en.wikipedia.org/wiki/Deterministic_finite_automaton)

---

## 文件信息

- **文件路径**: `source/Compile/Parse/TokenToFileMeta.cs`
- **行数**: ~500 行（含注释）
- **创建日期**: 2025-01-15
- **最后修改**: 2025-01-15
- **编译状态**: ? 成功

---

## 许可证

Copyright (c) kamaba233@gmail.com

与项目其他文件一致。
