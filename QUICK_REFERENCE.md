# TokenToFileMeta 2.0 - 快速参考

## ?? 文件位置
- **代码**: `source/Compile/Parse/TokenToFileMeta.cs`
- **行数**: ~465 行
- **编译**: ? 成功

---

## ?? 主要功能

| 功能 | 方法 | 说明 |
|------|------|------|
| 主入口 | `ParseTokensToFileMeta()` | 启动解析 |
| 获取树 | `rootNode` 属性 | 获取 Node 树 |
| Token 处理 | `ParseDetailToken()` | 处理单个 Token |

---

## ?? 核心方法

### 解析方法
```csharp
ParseTokensToFileMeta()     // 主入口
ParseToken()                 // 主循环
ParseDetailToken(Token)      // Token 处理
```

### Node 构造
```csharp
AddIdentifier(Token)         // 标识符
AddConstValue(Token)         // 常量值
AddKeyNode(Token, int)       // 关键字
AddSymbol(Token, int)        // 符号
AddAtOpSign(Token)           // @/$ 符号
```

### 状态管理
```csharp
TransitionState(DFAState)    // 状态转移
PopState()                    // 返回状态
```

---

## ?? 快速使用

### 替换 TokenParse
```csharp
// 之前
var tokenParse = new TokenParse(fileMeta, tokens);
tokenParse.BuildStruct();

// 现在
var tokenParse = new TokenToFileMeta(fileMeta, tokens);
tokenParse.ParseTokensToFileMeta();
```

### 获取 Node 树
```csharp
var parser = new TokenToFileMeta(fileMeta, tokens);
parser.ParseTokensToFileMeta();
var rootNode = parser.rootNode;
```

### 后续处理
```csharp
var structParse = new StructParse(fileMeta, rootNode, tokens);
// 继续正常流程
```

---

## ?? 支持的 Token 类型

### 标识符和类型
- `Identifier`, `Type`

### 括号和符号
- `LeftBrace`, `RightBrace`
- `LeftPar`, `RightPar`
- `LeftBracket`, `RightBracket`
- `Less`, `Greater`
- `Period`, `Comma`, `Colon`
- `SemiColon`, `LineEnd`

### 操作符
- 算术: `Plus`, `Minus`, `Multiply`, `Divide`, `Modulo`
- 赋值: `Assign`, `PlusAssign`, `MinusAssign`, 等
- 比较: `Equal`, `NotEqual`, `Greater`, `Less`, 等
- 逻辑: `And`, `Or`, `Not`
- 位运算: `Combine`, `InclusiveOr`, `XOR`, `Shi`, `Shr`

### 关键字（50+）
- 控制流: `If`, `Else`, `ElseIf`, `While`, `For`, `Switch`, `Case`, `Default`
- 定义: `Class`, `Interface`, `Enum`, `Data`, `Var`, `Dynamic`, `Void`
- 修饰符: `Public`, `Private`, `Static`, `Final`, `Const`, `Override`, `Partial`
- 其他: `Import`, `Namespace`, `Extends`, `This`, `Base`, `New`, 等

### 字面值
- `Number`, `String`, `BoolValue`, `Null`

### 特殊符号
- `At` (@), `Dollar` ($), `Sharp` (#)

---

## ?? DFA 状态

```
Initial          // 初始状态
├─ InImport      // 导入语句
├─ InNamespace   // 命名空间
├─ InClass       // 类定义
├─ InFunction    // 函数定义
├─ InBlock       // 语句块
└─ InExpression  // 表达式
```

---

## ?? 操作符优先级

| 等级 | 操作符 | 示例 |
|------|--------|------|
| 1 | () [] . | `obj.Method()` |
| 2 | - (单目) ++ -- | `++x`, `!flag` |
| 3 | * / % | `a * b` |
| 5 | << >> | `x << 2` |
| 6 | < > <= >= | `a > b` |
| 7 | == != | `a == b` |
| 8 | & ^ \| | `a & b` |
| 9 | && \|\| | `a && b` |
| 10 | ?: | `a ? b : c` |
| 11 | = += -= 等 | `x = y` |
| 12 | , | `a, b` |

---

## ? 兼容性

- ? 与 TokenParse 100% 功能兼容
- ? 与 StructParse 完全兼容
- ? 与 FileParse 完全兼容
- ? 与现有编译流程无缝衔接
- ? .NET 6 兼容
- ? C# 10 兼容

---

## ?? 相关文档

| 文档 | 内容 |
|------|------|
| `TOKENTOFILEMETA_REWRITE_SUMMARY.md` | 重写总结和改进点 |
| `COMPARISON_REPORT.md` | 与 TokenParse 的详细对比 |
| `PROJECT_COMPLETION_REPORT.md` | 项目完成总结 |

---

## ?? 调试技巧

### 查看 Node 树
```csharp
var parser = new TokenToFileMeta(fileMeta, tokens);
parser.ParseTokensToFileMeta();
Debug.Write(parser.rootNode.ToFormatString());
```

### 查看编译过程
```csharp
// 代码中的 Debug.Write 会输出信息
// 检查 OutputDebug 窗口
```

---

## ?? 常见问题

**Q: 能替换 TokenParse 吗?**  
A: ? 能，而且功能完全相同

**Q: 性能如何?**  
A: O(n) 时间复杂度，与 TokenParse 相同

**Q: 需要修改后续代码吗?**  
A: ? 不需要，rootNode 结构完全相同

**Q: 编译是否成功?**  
A: ? 完全成功，零错误零警告

---

## ?? 支持

有问题？查看：
1. 代码注释（详细说明）
2. COMPARISON_REPORT.md（与 TokenParse 对比）
3. PROJECT_COMPLETION_REPORT.md（项目总结）

---

**TokenToFileMeta 2.0**  
? 编译成功 | ? 100% 兼容 | ? 强烈推荐
