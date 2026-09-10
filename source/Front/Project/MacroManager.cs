//****************************************************************************
//  File:      MacroManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/10 12:00:00
//  Description: static if 编译期宏值管理器（global.macro）
//****************************************************************************
//  数据来源（优先级从低到高）：
//    1. project jsonc 的 global.macro 段（ProjectConfig.Global.Macro）；
//    2. 外部注入（编译前由外部环境设置）：
//       a. 环境变量 SL_MACRO_<name>=<value>；
//       b. 宿主/CLI 显式注册（CLI --macro name=value → ProjectManager.Run → SetExternalMacro）；
//    3. CompileBefore() 预扫描（唯一合法的源码内修改入口）。
//  生命周期：
//    1. 编译开始（InjectProjectData 步骤）LoadFromConfig 重置为 jsonc 初始值并应用外部宏；
//    2. CompileBefore() 预扫描通过 SetMacroValue 修改（源码内唯一合法修改入口）；
//    3. MetaCore 层 static if 用 EvaluateStaticCondition 编译期求值，
//       求值结果决定分支裁剪——static 判断不进入 runtime / IR 层。
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SimpleLanguage.Project
{
    public class MacroManager
    {
        public static MacroManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new MacroManager();
                return s_Instance;
            }
        }
        private static MacroManager s_Instance = null;

        /// <summary>操作数类别（编译期求值只支持这三类基础值）。</summary>
        private enum EOperandKind
        {
            None = 0,
            Boolean,
            Number,
            String,
        }
        /// <summary>编译期操作数统一表示。</summary>
        private struct OperandValue
        {
            public EOperandKind kind;
            public bool boolValue;
            public double numberValue;
            public string stringValue;

            public static OperandValue OfBool(bool v) { OperandValue o; o.kind = EOperandKind.Boolean; o.boolValue = v; o.numberValue = 0; o.stringValue = null; return o; }
            public static OperandValue OfNumber(double v) { OperandValue o; o.kind = EOperandKind.Number; o.boolValue = false; o.numberValue = v; o.stringValue = null; return o; }
            public static OperandValue OfString(string v) { OperandValue o; o.kind = EOperandKind.String; o.boolValue = false; o.numberValue = 0; o.stringValue = v; return o; }
        }

        private readonly Dictionary<string, JsonElement> m_MacroValues = new Dictionary<string, JsonElement>();

        private MacroManager() { }

        // ============ 宏值存取 ============

        /// <summary>
        /// 编译开始时调用：重置为 jsonc global.macro 段的初始值，
        /// 随后应用外部宏（环境变量 SL_MACRO_* 与 SetExternalMacro 注册的 CLI/宿主值）。
        /// 优先级：jsonc &lt; 外部宏 &lt; CompileBefore()。
        /// </summary>
        public void LoadFromConfig(ProjectConfig config)
        {
            m_MacroValues.Clear();
            if (config?.Global?.Macro != null)
            {
                foreach (var kv in config.Global.Macro)
                {
                    m_MacroValues[kv.Key] = kv.Value.Clone();
                }
            }
            ApplyExternalMacros();
        }

        // ============ 外部宏注入（编译前由外部环境设置） ============

        /// <summary>环境变量宏前缀：SL_MACRO_&lt;name&gt;=value（优先级高于 jsonc，低于 CLI --macro 与 CompileBefore()）。</summary>
        public const string EnvMacroPrefix = "SL_MACRO_";

        private static readonly Dictionary<string, string> s_ExternalMacros = new Dictionary<string, string>();

        /// <summary>
        /// 外部接口：编译前设置宏值。供 CLI --macro name=value（ProjectManager.Run 注入）
        /// 以及宿主程序（IDE/构建脚本/CI）直接调用。
        /// 值为字符串形式，按 true/false→布尔、数字→数值、其它→字符串 推断类型。
        /// 可以覆盖 jsonc 里已定义的宏，也可以追加新宏。
        /// </summary>
        public static void SetExternalMacro(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
                return;
            s_ExternalMacros[name] = value ?? string.Empty;
        }
        /// <summary>清空通过 <see cref="SetExternalMacro"/> 注册的外部宏（每次编译开始前重置）。</summary>
        public static void ClearExternalMacros()
        {
            s_ExternalMacros.Clear();
        }

        /// <summary>LoadFromConfig 后半段：jsonc 初值装载完成后应用外部宏。环境变量先应用，显式注册（CLI）后应用。</summary>
        private void ApplyExternalMacros()
        {
            // 1. 环境变量 SL_MACRO_<name>=<value>
            foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                var key = de.Key as string;
                if (string.IsNullOrEmpty(key) || !key.StartsWith(EnvMacroPrefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var name = key.Substring(EnvMacroPrefix.Length);
                ApplyOneExternalMacro(name, de.Value as string, "env");
            }
            // 2. 显式注册（CLI --macro / 宿主 SetExternalMacro），优先级高于环境变量
            foreach (var kv in s_ExternalMacros)
            {
                ApplyOneExternalMacro(kv.Key, kv.Value, "cli");
            }
        }
        private void ApplyOneExternalMacro(string name, string rawValue, string source)
        {
            if (string.IsNullOrEmpty(name) || !TryParseExternalValue(rawValue, out var value))
                return;
            m_MacroValues[name] = value;
            Log.AddProjectLog(LID.ProjectMacroManagerExternalMacroApplied,
                name + " = " + rawValue + " (" + source + ")");
        }
        /// <summary>
        /// 外部宏值类型推断：true/false → 布尔；数字 → 数值；其它一律按字符串原样处理。
        /// </summary>
        private static bool TryParseExternalValue(string text, out JsonElement value)
        {
            value = default;
            if (text == null)
                return false;
            try
            {
                using (var doc = JsonDocument.Parse(text))
                {
                    var root = doc.RootElement.Clone();
                    var kind = root.ValueKind;
                    if (kind == JsonValueKind.True || kind == JsonValueKind.False || kind == JsonValueKind.Number)
                    {
                        value = root;
                        return true;
                    }
                }
            }
            catch (JsonException) { }
            // 非 JSON 字面量（平台名、路径等）→ 字符串
            value = JsonSerializer.SerializeToElement(text);
            return true;
        }
        public void Clear()
        {
            m_MacroValues.Clear();
        }
        public bool IsMacroDefined(string name)
        {
            return m_MacroValues.ContainsKey(name);
        }
        public bool TryGetMacroValue(string name, out JsonElement value)
        {
            value = default;
            if (string.IsNullOrEmpty(name))
                return false;
            return m_MacroValues.TryGetValue(name, out value);
        }
        /// <summary>当前全部宏值（只读视图）。注入层遍历最终宏值生成 Project.macro 成员用。</summary>
        public IReadOnlyDictionary<string, JsonElement> GetAllMacroValues()
        {
            return m_MacroValues;
        }
        /// <summary>
        /// 修改宏值。仅供 CompileBefore() 预扫描调用（PorjectClass 负责），
        /// 其它任何路径都不允许触达本方法——Meta 层对 global.macro 赋值直接报错。
        /// </summary>
        public bool SetMacroValue(string name, JsonElement value, Token token)
        {
            if (string.IsNullOrEmpty(name) || !m_MacroValues.ContainsKey(name))
            {
                Log.AddMetaCoreLog(LID.ProjectMacroManagerMacroUndefined, token,
                    "Error static if 宏未定义: global.macro." + (name ?? "?"));
                return false;
            }
            // 值类别校验：只接受布尔/数值/字符串
            if (!TryJsonElementToOperand(value, token, out _))
            {
                return false;
            }
            m_MacroValues[name] = value.Clone();
            return true;
        }

        /// <summary>
        /// 判断一个调用链是否为 global.macro.X 形式（X 后允许继续往下挂，由调用方决定语义）。
        /// 供 CompileBefore 预扫描与 Meta 层修改限制检查共用。
        /// </summary>
        public static bool TryGetMacroRefName(FileMetaCallLink link, out string macroName)
        {
            macroName = null;
            if (link == null)
                return false;
            var nodeList = link.callNodeList;
            if (nodeList == null || nodeList.Count < 3)
                return false;
            var nameList = GetCallLinkNameList(link);
            if (nameList == null || nameList.Count < 3)
                return false;
            if (nameList[0] != "global" || nameList[1] != "macro")
                return false;
            macroName = nameList[2];
            return !string.IsNullOrEmpty(macroName);
        }

        /// <summary>
        /// 提取调用链的名字序列（跳过 '.' 分隔节点）。
        /// TokenParse 构链时 '.'（Period/'?.'）也会作为节点进入 extendLinkNodeList，
        /// 例如 global.macro.platform 的 callNodeList 实际是 [global, '.', macro, '.', platform]，
        /// 过滤后才是 [global, macro, platform]。
        /// </summary>
        private static List<string> GetCallLinkNameList(FileMetaCallLink link)
        {
            var nodeList = link?.callNodeList;
            if (nodeList == null)
                return null;
            var nameList = new List<string>(nodeList.Count);
            for (int i = 0; i < nodeList.Count; i++)
            {
                var tt = nodeList[i].token?.type;
                if (tt == ETokenType.Period || tt == ETokenType.QuestionMarkDot)
                    continue;
                nameList.Add(nodeList[i].name);
            }
            return nameList;
        }

        // ============ static if 条件编译期求值 ============

        /// <summary>
        /// static if 条件编译期求值入口。
        /// 支持：宏引用/常量的 ==/!=/</<=/>/>=、布尔宏直接引用、&amp;&amp;/||/!、括号。
        /// 求值失败（语法/类型/未定义宏）时记录错误并返回 false。
        /// </summary>
        public bool EvaluateStaticCondition(FileMetaBaseTerm term, out bool result)
        {
            result = false;
            var root = GetEvalRoot(term);
            if (root == null)
            {
                Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, term?.token,
                    "Error static if 条件表达式无法解析: " + (term?.ToFormatString() ?? "null"));
                return false;
            }
            return EvaluateBoolTerm(root, out result);
        }

        /// <summary>把 TermExpress/ParTerm 归一到其 AST 根；多元素 ParTerm（root==this）不支持。</summary>
        private FileMetaBaseTerm GetEvalRoot(FileMetaBaseTerm term)
        {
            if (term is FileMetaTermExpress || term is FileMetaParTerm)
            {
                if (term.root == null)
                {
                    if (!term.BuildAST())
                        return null;
                }
                var r = term.root;
                if (r != null && !ReferenceEquals(r, term))
                    return r;
                return null;
            }
            return term;
        }

        /// <summary>求值一个必须为布尔的子表达式。</summary>
        private bool EvaluateBoolTerm(FileMetaBaseTerm term, out bool result)
        {
            result = false;
            if (term == null)
            {
                Log.AddMetaCoreLog(LID.ProjectMacroManagerIssue, (Token)null, "Error static if 条件子表达式为空!!");
                return false;
            }
            if (term is FileMetaSymbolTerm symbol)
            {
                return EvaluateSymbolTerm(symbol, out result);
            }
            if (term is FileMetaConstValueTerm || term is FileMetaCallTerm)
            {
                if (!TryEvaluateOperand(term, out var operand))
                    return false;
                if (operand.kind != EOperandKind.Boolean)
                {
                    Log.AddMetaCoreLog(LID.ProjectMacroManagerTypeMismatch, term.token,
                        "Error static if 条件必须是布尔值: " + term.ToFormatString());
                    return false;
                }
                result = operand.boolValue;
                return true;
            }
            if (term is FileMetaParTerm || term is FileMetaTermExpress)
            {
                var root = GetEvalRoot(term);
                if (root == null)
                {
                    Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, term.token,
                        "Error static if 条件表达式无法解析: " + term.ToFormatString());
                    return false;
                }
                return EvaluateBoolTerm(root, out result);
            }
            Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, term.token,
                "Error static if 条件不支持该语法: " + term.ToFormatString());
            return false;
        }

        /// <summary>求值符号节点（运算符）。</summary>
        private bool EvaluateSymbolTerm(FileMetaSymbolTerm symbol, out bool result)
        {
            result = false;
            var opType = symbol.symBolType;
            switch (opType)
            {
                case ETokenType.Not:    // ! 一元
                    {
                        if (!IsUnarySymbol(symbol) || symbol.right == null)
                        {
                            Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, symbol.token,
                                "Error static if 条件中 '!' 只能作为一元前缀使用!!");
                            return false;
                        }
                        if (!EvaluateBoolTerm(symbol.right, out bool rv))
                            return false;
                        result = !rv;
                        return true;
                    }
                case ETokenType.And:    // &&
                case ETokenType.Or:     // ||
                    {
                        if (symbol.left == null || symbol.right == null)
                        {
                            Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, symbol.token,
                                "Error static if 条件中 '" + symbol.token?.lexeme?.ToString() + "' 缺少操作数!!");
                            return false;
                        }
                        if (!EvaluateBoolTerm(symbol.left, out bool lv))
                            return false;
                        // && / || 求值：先求左边，必要时再求右边（编译期无副作用，直接都求也可）
                        if (!EvaluateBoolTerm(symbol.right, out bool rv2))
                            return false;
                        result = (opType == ETokenType.And) ? (lv && rv2) : (lv || rv2);
                        return true;
                    }
                case ETokenType.Equal:          // ==
                case ETokenType.NotEqual:       // !=
                case ETokenType.Less:           // <
                case ETokenType.LessOrEqual:    // <=
                case ETokenType.Greater:        // >
                case ETokenType.GreaterOrEqual: // >=
                    {
                        if (symbol.left == null || symbol.right == null)
                        {
                            Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, symbol.token,
                                "Error static if 条件中 '" + symbol.token?.lexeme?.ToString() + "' 缺少操作数!!");
                            return false;
                        }
                        if (!TryEvaluateOperand(symbol.left, out var lv))
                            return false;
                        if (!TryEvaluateOperand(symbol.right, out var rv))
                            return false;
                        return CompareOperand(symbol, lv, rv, out result);
                    }
                default:
                    {
                        Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportOperate, symbol.token,
                            "Error static if 条件中不支持的运算符: " + (symbol.token?.lexeme?.ToString() ?? "?"));
                        return false;
                    }
            }
        }

        /// <summary>
        /// 一元前缀判定：BuildTst 构树时一元符号的 left 是新建的同 token 的 isOnlyOne 符号节点；
        /// 二元符号的 left/right 是操作数。据此区分一元/二元。
        /// </summary>
        private static bool IsUnarySymbol(FileMetaSymbolTerm symbol)
        {
            return symbol.left is FileMetaSymbolTerm lst
                && lst.isOnlyOne
                && ReferenceEquals(lst.token, symbol.token);
        }

        /// <summary>求值比较/逻辑运算的操作数（宏引用或常量）。</summary>
        private bool TryEvaluateOperand(FileMetaBaseTerm term, out OperandValue operand)
        {
            operand = default;
            if (term == null)
                return false;
            if (term is FileMetaConstValueTerm cvt)
            {
                return TryConstToOperand(cvt, out operand);
            }
            if (term is FileMetaCallTerm call)
            {
                return TryMacroRefToOperand(call, out operand);
            }
            if (term is FileMetaParTerm || term is FileMetaTermExpress)
            {
                var root = GetEvalRoot(term);
                if (root == null)
                {
                    Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, term.token,
                        "Error static if 条件表达式无法解析: " + term.ToFormatString());
                    return false;
                }
                return TryEvaluateOperand(root, out operand);
            }
            Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, term.token,
                "Error static if 条件的操作数只支持宏引用与常量: " + term.ToFormatString());
            return false;
        }

        /// <summary>常量 token → 操作数。String/Number/NumberReal/BoolValue。</summary>
        private bool TryConstToOperand(FileMetaConstValueTerm cvt, out OperandValue operand)
        {
            operand = default;
            var token = cvt.token;
            if (token == null)
                return false;
            switch (token.type)
            {
                case ETokenType.String:
                    {
                        // lexeme.ToString() 是去引号的内容
                        operand = OperandValue.OfString(token.lexeme?.ToString() ?? string.Empty);
                        return true;
                    }
                case ETokenType.Number:
                case ETokenType.NumberReal:
                    {
                        // lexeme 可能是数值对象（NumberReal/部分 Number）或数字串
                        try
                        {
                            double v = Convert.ToDouble(token.lexeme, System.Globalization.CultureInfo.InvariantCulture);
                            // 一元负号：FileMetaConstValueTerm 自带 plusMinusToken
                            if (cvt.plusMinusToken != null && cvt.plusMinusToken.type == ETokenType.Minus)
                                v = -v;
                            operand = OperandValue.OfNumber(v);
                            return true;
                        }
                        catch
                        {
                            Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, token,
                                "Error static if 条件中的数字常量无法解析: " + token.ToLexemeAllString());
                            return false;
                        }
                    }
                case ETokenType.BoolValue:
                    {
                        // "true"/"false" 关键字，lexeme 是字符串
                        if (bool.TryParse(token.lexeme?.ToString(), out bool bv))
                        {
                            operand = OperandValue.OfBool(bv);
                            return true;
                        }
                        Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, token,
                            "Error static if 条件中的布尔常量无法解析: " + token.ToLexemeAllString());
                        return false;
                    }
                default:
                    {
                        Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, token,
                            "Error static if 条件只支持字符串/数字/布尔常量: " + token.ToLexemeAllString());
                        return false;
                    }
            }
        }

        /// <summary>宏引用 global.macro.X → 操作数。链名字序列必须恰为 [global, macro, X] 且无调用/下标/模板。</summary>
        private bool TryMacroRefToOperand(FileMetaCallTerm call, out OperandValue operand)
        {
            operand = default;
            var link = call.callLink;
            var nodeList = link?.callNodeList;
            if (nodeList != null)
            {
                for (int i = 0; i < nodeList.Count; i++)
                {
                    if (nodeList[i].isCallFunction || nodeList[i].isArray || nodeList[i].isTemplate)
                    {
                        Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, call.token,
                            "Error static if 条件中的宏引用不允许函数调用/下标/模板: " + call.ToFormatString());
                        return false;
                    }
                }
            }
            var nameList = GetCallLinkNameList(link);
            if (nameList == null || nameList.Count != 3
                || nameList[0] != "global" || nameList[1] != "macro")
            {
                Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportExpress, call.token,
                    "Error static if 条件中只允许引用 global.macro.宏名: " + call.ToFormatString());
                return false;
            }
            string macroName = nameList[2];
            if (!TryGetMacroValue(macroName, out var value))
            {
                Log.AddMetaCoreLog(LID.ProjectMacroManagerMacroUndefined, call.token,
                    "Error static if 引用了未定义的宏: global.macro." + macroName);
                return false;
            }
            return TryJsonElementToOperand(value, call.token, out operand);
        }

        /// <summary>jsonc 宏值 → 操作数。只支持 String/Number/True/False。</summary>
        private bool TryJsonElementToOperand(JsonElement element, Token token, out OperandValue operand)
        {
            operand = default;
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    operand = OperandValue.OfString(element.GetString());
                    return true;
                case JsonValueKind.Number:
                    {
                        if (element.TryGetDouble(out double d))
                        {
                            operand = OperandValue.OfNumber(d);
                            return true;
                        }
                        break;
                    }
                case JsonValueKind.True:
                case JsonValueKind.False:
                    operand = OperandValue.OfBool(element.GetBoolean());
                    return true;
                default:
                    break;
            }
            Log.AddMetaCoreLog(LID.ProjectMacroManagerMacroValueInvalid, token,
                "Error global.macro 宏值只支持布尔/数值/字符串，当前类别: " + element.ValueKind);
            return false;
        }

        /// <summary>编译期比较两个操作数。类别不一致报错；Boolean/String 只支持 ==/!=。</summary>
        private bool CompareOperand(FileMetaSymbolTerm symbol, OperandValue lv, OperandValue rv, out bool result)
        {
            result = false;
            if (lv.kind != rv.kind)
            {
                Log.AddMetaCoreLog(LID.ProjectMacroManagerTypeMismatch, symbol.token,
                    "Error static if 条件比较的操作数类型不一致: " + lv.kind + " vs " + rv.kind);
                return false;
            }
            switch (symbol.symBolType)
            {
                case ETokenType.Equal:
                    result = EqualOperand(lv, rv);
                    return true;
                case ETokenType.NotEqual:
                    result = !EqualOperand(lv, rv);
                    return true;
                case ETokenType.Less:
                case ETokenType.LessOrEqual:
                case ETokenType.Greater:
                case ETokenType.GreaterOrEqual:
                    {
                        if (lv.kind != EOperandKind.Number)
                        {
                            Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportOperate, symbol.token,
                                "Error static if 条件中 '" + symbol.token?.lexeme?.ToString() + "' 只支持数值比较!!");
                            return false;
                        }
                        switch (symbol.symBolType)
                        {
                            case ETokenType.Less: result = lv.numberValue < rv.numberValue; break;
                            case ETokenType.LessOrEqual: result = lv.numberValue <= rv.numberValue; break;
                            case ETokenType.Greater: result = lv.numberValue > rv.numberValue; break;
                            case ETokenType.GreaterOrEqual: result = lv.numberValue >= rv.numberValue; break;
                        }
                        return true;
                    }
                default:
                    {
                        Log.AddMetaCoreLog(LID.ProjectMacroManagerNotSupportOperate, symbol.token,
                            "Error static if 条件中不支持的比较运算符: " + (symbol.token?.lexeme?.ToString() ?? "?"));
                        return false;
                    }
            }
        }
        private static bool EqualOperand(OperandValue lv, OperandValue rv)
        {
            switch (lv.kind)
            {
                case EOperandKind.Boolean: return lv.boolValue == rv.boolValue;
                case EOperandKind.Number: return lv.numberValue == rv.numberValue;
                case EOperandKind.String: return lv.stringValue == rv.stringValue;
                default: return false;
            }
        }

        // ============ CompileBefore 赋值右值求值 ============

        /// <summary>
        /// CompileBefore() 中 global.macro.X = 右值 的右值求值。
        /// 只允许常量（布尔/数值/字符串）或另一个宏引用 global.macro.Y。
        /// </summary>
        public bool TryEvaluateMacroConstExpress(FileMetaBaseTerm term, out JsonElement value)
        {
            value = default;
            var root = GetEvalRoot(term);
            if (root == null)
            {
                Log.AddMetaCoreLog(LID.ProjectMacroManagerMacroNotConst, term?.token,
                    "Error CompileBefore 中给 global.macro 赋的值必须是常量或宏引用: " + (term?.ToFormatString() ?? "null"));
                return false;
            }
            if (root is FileMetaConstValueTerm cvt)
            {
                if (!TryConstToOperand(cvt, out var operand))
                    return false;
                switch (operand.kind)
                {
                    case EOperandKind.Boolean: value = JsonSerializer.SerializeToElement(operand.boolValue); return true;
                    case EOperandKind.Number: value = JsonSerializer.SerializeToElement(operand.numberValue); return true;
                    case EOperandKind.String: value = JsonSerializer.SerializeToElement(operand.stringValue); return true;
                    default: return false;
                }
            }
            if (root is FileMetaCallTerm)
            {
                if (!TryMacroRefToOperand(root as FileMetaCallTerm, out var operand2))
                    return false;
                switch (operand2.kind)
                {
                    case EOperandKind.Boolean: value = JsonSerializer.SerializeToElement(operand2.boolValue); return true;
                    case EOperandKind.Number: value = JsonSerializer.SerializeToElement(operand2.numberValue); return true;
                    case EOperandKind.String: value = JsonSerializer.SerializeToElement(operand2.stringValue); return true;
                    default: return false;
                }
            }
            Log.AddMetaCoreLog(LID.ProjectMacroManagerMacroNotConst, root.token,
                "Error CompileBefore 中给 global.macro 赋的值必须是常量或宏引用: " + root.ToFormatString());
            return false;
        }
    }
}
