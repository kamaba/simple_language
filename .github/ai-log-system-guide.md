# AI Log System Guide (Front + VM)

本文件用于约束 AI 在本仓库中编写日志代码的统一规则，优先级高。

## 1) 总体原则（必须遵守）
- 任何新增日志都必须走 `SimpleLanguage.Logging.Log`，禁止直接使用 `Debug.Write*` / `Console.WriteLine` 作为业务日志通道。
- 日志定义以 `ErrorDefinitions.csv` 为准，代码只通过 `LID` 引用。
- 新增日志时，必须同步更新：
  1. 对应项目的 `Log/ErrorDefinitions.csv`
  2. 对应项目的 `Log/LID.cs`
  3. 代码中的 `Log.AddXXXLog(...)` 调用
- `LID` 命名应使用可读英文语义名，不使用纯数字风格命名。

## 2) Front 与 VM 的分层
- Front 使用：`source/Front/Log/*`
  - 常用入口：`AddProjectLog` / `AddProcessLog` / `AddTokenLog` / `AddNodeLog` / `AddFileMetaLog` / `AddMetaCoreLog` / `AddIRLog`
  - 错误类型分层：Project / Process / ParseToken / ParseNode / ParseFile / ParseMeta / GenIR
- VM 使用：`source/VM/Log/*`
  - 常用入口：`AddProjectLog` / `AddProcessLog` / `AddParseIRLog` / `AddRuntimeLog` / `AddOtherLog`
  - 错误类型分层：Project / Process / ParseIR / Runtime / Other

## 3) CSV 驱动规则
`ErrorDefinitions.csv` 列结构：
- `id, logType, enableAssert, blockOnErrorAssert, paramCount, domestic_tips, cn_message, , cn_fixed_tips, en_message, en_fixed_tips`

新增日志定义时：
- `id`：在对应项目内保持唯一。
- `logType`：`Assert | Error | Warning | Info | Trace`。
- `paramCount`：必须与 `Log.AddXXXLog` 传入格式化参数数量一致。
- `cn_message/en_message`：模板占位符数量必须与 `paramCount` 对应。
- `cn_fixed_tips/en_fixed_tips`：给出可执行修复建议。

## 4) 新增日志的标准步骤（AI 必须自动执行）
1. 判断修改位于 Front 还是 VM。
2. 在对应 `ErrorDefinitions.csv` 追加一条定义。
3. 在对应 `LID.cs` 追加或维护同 `id` 的枚举成员。
4. 在业务代码中调用对应 `Log.AddXXXLog(...)`。
5. 检查参数数量与 CSV `paramCount` 一致。
6. 编译验证，确保无 `LID` 或格式化相关错误。

## 5) 质量门禁
- 不允许只写 `Log.AddXXXLog(LID.Unknown, ...)` 作为新增正式日志；`Unknown` 仅用于兜底。
- 不允许新增日志而不补 CSV 与 `LID`。
- 若消息需要定位源位置，优先传入 `Token`（Front）或 `DebugInfo`（VM）。

## 6) 推荐实践
- 文案短句化，先结论后上下文。
- `Error`/`Assert` 用于失败路径；`Warning` 用于可恢复异常；`Info/Trace` 用于观察信息。
- 同类错误保持同一 `LID`，避免重复造号。
