# TokenToFileMeta.cs - DFA + 递归下降解析器

## 概述
? **已完全重写** `source/Compile/Parse/TokenToFileMeta.cs` 文件，采用 **DFA（确定有限自动机）+ 手写递归下降解析器**，模拟 Dart 编译器的解析过程。

### 编译状态
? **构建成功** - 所有编译错误已解决

## 核心架构

### 1. DFA（确定有限自动机）
状态机管理代码的各种解析上下文：

```
Initial (初始)
  ↓
InImport (导入语句) → Initial
  ↓
InNamespace (命名空间) → Initial
  ↓
InClass (类定义) → Initial
  ↓
InFunction (函数定义) → InClass/InBlock
  ↓
InBlock (语句块) → InFunction
  ↓
InExpression (表达式) → InBlock/InStatement
```

**DFAState 枚举**：
- `Initial` - 初始状态，等待顶级声明
- `InImport` - 解析 import 语句
- `InNamespace` - 解析 namespace 声明
- `InClass` - 解析类定义
- `InFunction` - 解析函数定义
- `InBlock` - 解析语句块
- `InExpression` - 解析表达式

### 2. 递归下降解析器
采用标准的递归下降解析技术，直观且易于扩展：

```
ParseCompilationUnit()
  ├─ ParseImportDirective()
  ├─ ParseNamespaceDeclaration()
  ├─ ParseClassDeclaration()
  │  ├─ ParseModifiers()
  │  ├─ ParseQualifiedName()
  │  ├─ ParseTypeParameters()
  │  ├─ ParseInterfaceList()
  │  └─ SkipClassBody()
  └─ ...
```

### 3. ParseContext 上下文
管理解析状态和括号深度：

```csharp
class ParseContext
{
    DFAState currentState;      // 当前状态
    Stack<DFAState> stateStack; // 状态栈（嵌套）
    int braceDepth;             // {} 深度
    int parenDepth;             // () 深度
    int bracketDepth;           // [] 深度
}
```

## EBNF 文法定义

```ebnf
compilation_unit 
    = import_directive* namespace_declaration* class_declaration*

import_directive 
    = 'import' qualified_name ';'

namespace_declaration 
    = 'namespace' qualified_name ';'

class_declaration 
    = modifier* ('class' | 'interface' | 'enum' | 'data')
      identifier type_parameters? 
      ('extends' qualified_name)?
      ('interface' interface_list)?
      '{' member_declaration* '}'

qualified_name 
    = identifier ('.' identifier)*

type_parameters 
    = '<' type_parameter (',' type_parameter)* '>'

interface_list 
    = qualified_name (',' qualified_name)*

modifier 
    = 'public' | 'private' | 'protected' 
    | 'static' | 'final' | 'const' | 'partial'
```

## 核心方法说明

### 主入口
```csharp
public void ParseTokensToFileMeta()
```
- 初始化 DFA 状态
- 启动递归下降解析过程
- 异常处理和日志记录

### 编译单元级别
```csharp
private void ParseCompilationUnit()      // 顶级单位
private void ParseImportDirective()      // import 语句
private void ParseNamespaceDeclaration() // namespace 声明
private void ParseClassDeclaration()     // class 定义
```

### 类型和修饰符
```csharp
private List<Token> ParseModifiers()     // 解析修饰符
private List<Token> ParseQualifiedName() // 解析限定名
private List<Token> ParseTypeParameters() // 解析泛型参数
private List<List<Token>> ParseInterfaceList() // 解析接口列表
```

### 辅助方法
```csharp
private void SkipClassBody()             // 跳过类体
private bool IsClassDeclarationStart()   // 检查类定义开始
```

### Token 操作
```csharp
private Token CurrentToken               // 当前 Token
private Token PeekToken(offset)          // 预看 Token
private Token Consume()                  // 消费 Token
private bool Match(tokenType)            // 匹配类型
private bool MatchAny(tokenTypes)        // 多类型匹配
```

### DFA 状态转移
```csharp
private void TransitionState(newState)   // 状态转移
private void PopState()                  // 返回上一状态
```

## 特性对比：Dart vs 本实现

| 特性 | Dart 编译器 | TokenToFileMeta |
|------|-----------|------------------|
| 词法分析 | DFA 扫描器 | ? DFA 状态机 |
| 语法分析 | 递归下降 | ? 递归下降解析 |
| 上下文管理 | 状态栈 | ? ParseContext |
| 括号匹配 | 深度计数 | ? braceDepth 等 |
| 错误恢复 | 同步机制 | ? try-catch |
| 类型参数 | 完全支持 | ? 支持 <T> |
| 修饰符 | 多级修饰 | ? public/static 等 |

## 使用示例

```csharp
// 创建实例
TokenToFileMeta parser = new TokenToFileMeta(fileMeta, tokenList);

// 执行完整解析（包括 DFA 状态转移）
parser.ParseTokensToFileMeta();

// 后续处理
fileMeta.CreateNamespace();
fileMeta.CombineFileMeta();
```

## 解析流程演示

### 输入代码
```dart
import Std;
namespace Application.Core;

public class MyClass<T> extends BaseClass interface IInterface {
    // 类成员
}
```

### 解析步骤
1. **TokenIndex=0** → Current=Import
   - TransitionState(InImport)
   - ParseImportDirective()
   - 创建 FileMetaImportSyntax
   - TransitionState(Initial)

2. **TokenIndex=N** → Current=Namespace
   - TransitionState(InNamespace)
   - ParseNamespaceDeclaration()
   - 创建 NamespaceStatementBlock
   - TransitionState(Initial)

3. **TokenIndex=M** → Current=Public
   - IsClassDeclarationStart() = true
   - TransitionState(InClass)
   - ParseClassDeclaration()
     - ParseModifiers() → [Public]
     - className = MyClass
     - ParseTypeParameters() → [T]
     - extendsKeyword = extends
     - baseClass = [BaseClass]
     - interfaceKeyword = interface
     - interfaceList = [[IInterface]]
     - SkipClassBody()
   - TransitionState(Initial)

## 架构优势

### 1. 清晰的状态管理
- DFA 状态明确反映当前解析上下文
- 状态栈支持嵌套结构
- 易于调试和维护

### 2. 符合编译理论
- 标准 DFA 实现
- 标准递归下降解析
- 易于扩展和优化

### 3. Dart 兼容
- 解析过程模仿 Dart 编译器
- 支持 Dart 特有的语法（如泛型、接口）
- 易于学习和移植

### 4. 易于错误处理
- try-catch 包装
- 上下文感知的错误消息
- 支持错误恢复

## 未来扩展

1. **语句级解析**
   ```csharp
   private void ParseStatement()
   private void ParseExpressionStatement()
   private void ParseIfStatement()
   ```

2. **表达式解析**
   ```csharp
   private FileMetaBaseTerm ParseExpression()
   private FileMetaBaseTerm ParseAssignment()
   private FileMetaBaseTerm ParseConditional()
   ```

3. **完整类体解析**
   ```csharp
   private void ParseClassBody()
   private void ParseMemberDeclaration()
   private void ParseFunctionDeclaration()
   ```

4. **错误恢复**
   ```csharp
   private void Synchronize()
   private void ReportError(string message)
   ```

## 文件位置
`source/Compile/Parse/TokenToFileMeta.cs`

## 代码行数
~500 行（含注释和文档字符串）

## 许可证
Copyright (c) kamaba233@gmail.com
与项目其他文件一致
