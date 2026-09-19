using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

//****************************************************************************
//  LidIndexer - LID 索引生成工具
//  1. 扫描 source/Front 下所有 .cs 中的
//     Log.AddXxxLog( LID.ShowExtendMessage / LID.MetaCoreAssertShowMessage, ... ) 调用点
//  2. 按日志 API + 文件路径判定模块（Token/Node/File/MetaCore/IR/Export/Project/Process）
//     按文件名判定子模块（Class/Express/Statement/Enum/Call/...）
//  3. 为每个调用点生成唯一 LID（枚举名 + 分段 ID + ErrorDefinitions.csv 行）
//  4. -apply 时改写：LID.cs 追加枚举段、CSV 追加行、源文件替换调用点
//     默认 dry-run，仅输出统计与 report.txt
//****************************************************************************
class Program
{
    const string FrontRoot = @"f:\project\lang\simple_language\source\Front";
    const string LidFile = FrontRoot + @"\Log\LID.cs";
    const string CsvFile = FrontRoot + @"\Log\ErrorDefinitions.csv";
    const string ReportFile = @"f:\project\lang\simple_language\tools\LidIndexer\report.txt";

    // ---------------- 模块前缀与 ID 分段 ----------------
    static readonly Dictionary<string, string> ModulePrefix = new()
    {
        ["Token"] = "Token",
        ["Node"] = "Node",
        ["File"] = "FileMeta",
        ["MetaCore"] = "MetaCore",
        ["IR"] = "IR",
        ["Export"] = "Export",
        ["Project"] = "Project",
        ["Process"] = "Process",
    };

    static readonly Dictionary<string, int> ModuleIdBase = new()
    {
        ["Project"] = 20000,
        ["Process"] = 20050,
        ["Token"] = 20100,
        ["Node"] = 20200,
        ["File"] = 20300,
        ["MetaCore"] = 21000,
        ["IR"] = 22000,
        ["Export"] = 22100,
    };

    // ---------------- 文件名 -> 子模块 ----------------
    static readonly Dictionary<string, string> SubModuleMap = new()
    {
        ["LexerParseToToken.cs"] = "Lexer",
        ["TokenParseToNode.cs"] = "ParseNode",
        ["StructParseToSyntax.cs"] = "StructParse",
        ["StructParseFrame.cs"] = "StructFrame",
        ["FileParse.cs"] = "FileParse",
        ["DllImportSourceRewriter.cs"] = "DllImport",
        ["FileMetaClass.cs"] = "Class",
        ["FileMetaExpress.cs"] = "Express",
        ["FileMetaMemberVariable.cs"] = "MemberVariable",
        ["FileMetatUtil.cs"] = "Util",
        ["FileMetaGlobalSyntax.cs"] = "GlobalSyntax",
        ["FileMetaCommon.cs"] = "Common",
        ["FileMetaMemberFunction.cs"] = "MemberFunction",
        ["FileMetaSyntax.cs"] = "Syntax",
        ["FileMetaNamespace.cs"] = "Namespace",
        ["FileMeta.cs"] = "File",
        ["BindExpandManager.cs"] = "BindExpand",
        ["MetaEnum.cs"] = "Enum",
        ["MetaMemberEnum.cs"] = "MemberEnum",
        ["MetaCallNode.cs"] = "Call",
        ["MetaCallLink.cs"] = "CallLink",
        ["MetaVisitCall.cs"] = "VisitCall",
        ["MetaClass.cs"] = "Class",
        ["ClassManager.cs"] = "ClassManager",
        ["MetaExpressNewObject.cs"] = "ExpressNewObject",
        ["MetaExpressOperator.cs"] = "ExpressOperator",
        ["MetaExpressAnonData.cs"] = "ExpressAnonData",
        ["MetaExpressArray.cs"] = "ExpressArray",
        ["MetaExpressAsIs.cs"] = "ExpressAsIs",
        ["ExpressManager.cs"] = "ExpressManager",
        ["MetaAssignStatements.cs"] = "AssignStatement",
        ["TypeManager.cs"] = "Type",
        ["MetaType.cs"] = "Type",
        ["AttributeManager.cs"] = "Attribute",
        ["MetaAttribute.cs"] = "Attribute",
        ["RuntimeAttributeRegistry.cs"] = "AttributeRegistry",
        ["MetaMemberFunction.cs"] = "MemberFunction",
        ["MetaMethod.cs"] = "Method",
        ["MetaMemberVariable.cs"] = "MemberVariable",
        ["MetaVariable.cs"] = "Variable",
        ["MetaParam.cs"] = "Param",
        ["MetaData.cs"] = "Data",
        ["MetaMemberData.cs"] = "MemberData",
        ["MetaTemplateClass.cs"] = "TemplateClass",
        ["MetaGenTemplateClass.cs"] = "GenTemplateClass",
        ["LocalManager.cs"] = "Local",
        ["MetaNode.cs"] = "MetaNode",
        ["ModuleManager.cs"] = "Module",
        ["NumberManager.cs"] = "Number",
        ["MLIRExportManager.cs"] = "MLIR",
        ["SLModulePackageWriter.cs"] = "SLModulePackage",
        ["IRVariable.cs"] = "Variable",
        ["IRExpress.cs"] = "Express",
        ["IRManager.cs"] = "Manager",
        ["IRMetaCallLink.cs"] = "CallLink",
        ["IRMetaClass.cs"] = "Class",
        ["ProjectReferenceModuleLoader.cs"] = "ReferenceModule",
        ["PorjectClass.cs"] = "ProjectClass",
        ["ProjectCompile.cs"] = "Compile",
        ["SystemMethodCallDeclaration.cs"] = "SystemMethodCall",
        ["ProcessManager.cs"] = "Manager",
        // Core/Statements
        ["MetaBlockStatements.cs"] = "BlockStatement",
        ["MetaBreakContinueGoStatements.cs"] = "BreakContinueGoStatement",
        ["MetaClosureDefineStatements.cs"] = "ClosureDefineStatement",
        ["MetaDefineVarStatements.cs"] = "DefineVarStatement",
        ["MetaReturnStatements.cs"] = "ReturnStatement",
        ["MetaSwitchStatements.cs"] = "SwitchStatement",
        ["MetaTryCatchStatements.cs"] = "TryCatchStatement",
        ["MetaWhileDoWhileStatements.cs"] = "WhileDoWhileStatement",
        // Core/ExpressNode
        ["MetaExpressChecked.cs"] = "ExpressChecked",
        ["MetaExpressConst.cs"] = "ExpressConst",
        ["MetaExpressTry.cs"] = "ExpressTry",
        // IR
        ["IRCall.cs"] = "Call",
        ["IRMethod.cs"] = "Method",
        ["IRAssignStatements.cs"] = "AssignStatement",
        ["IRClosureDefineStatements.cs"] = "ClosureDefineStatement",
        ["IRReturnStatements.cs"] = "ReturnStatement",
        ["IRUtil.cs"] = "Util",
    };

    // ---------------- 消息语义提取 ----------------
    // 常见英文短语优先合并为单词
    static readonly (string phrase, string word)[] EnPhrases =
    {
        ("not found", "NotFound"),
        ("notfound", "NotFound"),
        ("not support", "NotSupport"),
        ("notsupport", "NotSupport"),
        ("not allow", "NotAllow"),
        ("notallow", "NotAllow"),
        ("is null", "IsNull"),
        ("isnull", "IsNull"),
        ("== null", "IsNull"),
        ("null!", "IsNull"),
        ("can't", "Cannot"),
        ("cannot", "Cannot"),
        ("don't support", "NotSupport"),
    };

    static readonly HashSet<string> NoiseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "error", "warning", "parse", "the", "is", "are", "a", "an", "of", "to", "in",
        "and", "or", "with", "that", "this", "for", "it", "you", "be", "was", "has",
        "have", "on", "at", "by", "no", "so", "use", "used", "using", "now", "here",
    };

    // 中文关键词 -> 英文词
    static readonly (string kw, string word)[] CnWords =
    {
        ("找不到", "NotFound"), ("未找到", "NotFound"), ("没有找到", "NotFound"), ("查找不到", "NotFound"),
        ("为空", "IsNull"), ("是空", "IsNull"), ("空!", "IsNull"),
        ("重复", "Duplicate"),
        ("不允许", "NotAllow"), ("不支持", "NotSupport"), ("不能", "NotAllow"), ("禁止", "NotAllow"), ("无法", "Cannot"),
        ("类型", "Type"),
        ("变量", "Variable"),
        ("函数", "Function"),
        ("参数", "Param"),
        ("返回", "Return"),
        ("定义", "Define"),
        ("命名空间", "Namespace"),
        ("表达式", "Express"),
        ("语句", "Statement"),
        ("枚举", "Enum"),
        ("数组", "Array"),
        ("属性", "Attribute"),
        ("继承", "Extend"),
        ("接口", "Interface"),
        ("调用", "Call"),
        ("转换", "Convert"),
        ("超过", "OverLimit"), ("越界", "OverLimit"),
        ("失败", "Failed"),
        ("非法", "Invalid"),
        ("冲突", "Conflict"),
        ("符号", "Sign"),
        ("数据", "Data"),
        ("成员", "Member"),
        ("静态", "Static"),
    };

    class CallSite
    {
        public string FilePath;
        public int Line;
        public int NameStart;   // 旧 LID 枚举名在源文件中的起始位置
        public int NameLen;
        public string Api;
        public string OldLid;
        public string Module;
        public string SubModule;
        public string Message;
        public string NewName;
        public int NewId;
    }

    static readonly Regex CallRegex = new(
        @"Log\s*\.\s*(AddTokenByString|AddTokenLog|AddNodeLog|AddFileMetaLog|AddMetaCoreLog|AddIRLog|AddProjectLog|AddProcessLog)\s*\(\s*LID\s*\.\s*(ShowExtendMessage|MetaCoreAssertShowMessage)\b",
        RegexOptions.Singleline);

    // 宽松统计：所有 LID.ShowExtendMessage / LID.MetaCoreAssertShowMessage 出现（含非调用上下文）
    static readonly Regex LooseRegex = new(@"\bLID\s*\.\s*(ShowExtendMessage|MetaCoreAssertShowMessage)\b");

    static void Main(string[] args)
    {
        bool apply = args.Contains("-apply", StringComparer.OrdinalIgnoreCase);
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 已存在的 LID 枚举名 / ID，防止冲突
        var lidText = File.ReadAllText(LidFile, Encoding.UTF8);
        var usedNames = new HashSet<string>(
            Regex.Matches(lidText, @"^\s*(\w+)\s*=", RegexOptions.Multiline).Select(m => m.Groups[1].Value),
            StringComparer.Ordinal);
        var usedIds = new HashSet<int>(
            Regex.Matches(lidText, @"=\s*(\d+)\s*,?\s*$", RegexOptions.Multiline).Select(m => int.Parse(m.Groups[1].Value)));
        foreach (var line in File.ReadAllLines(CsvFile, Encoding.UTF8))
        {
            var p = line.Split(',');
            if (p.Length > 0 && int.TryParse(p[0], out var id)) usedIds.Add(id);
        }

        // ---------------- 扫描 ----------------
        var sites = new List<CallSite>();
        var looseExtras = new List<string>(); // 非调用上下文的残留引用
        foreach (var file in Directory.EnumerateFiles(FrontRoot, "*.cs", SearchOption.AllDirectories)
                                       .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var enc = DetectEncoding(file, out byte[] bytes);
            if (enc == null) continue;
            string text = enc.GetString(bytes);
            string normPath = file.Replace('\\', '/');

            foreach (Match m in LooseRegex.Matches(text))
            {
                // 检查是否被 CallRegex 覆盖（在匹配位置上）
                bool covered = false;
                foreach (Match c in CallRegex.Matches(text))
                {
                    if (c.Groups[2].Index == m.Groups[1].Index) { covered = true; break; }
                }
                if (!covered)
                {
                    looseExtras.Add($"{file}({GetLine(text, m.Index)}): {m.Value.Trim()}");
                }
            }

            foreach (Match m in CallRegex.Matches(text))
            {
                string api = m.Groups[1].Value;
                string oldLid = m.Groups[2].Value;
                var g = m.Groups[2];

                var site = new CallSite
                {
                    FilePath = file,
                    Line = GetLine(text, m.Index),
                    NameStart = g.Index,
                    NameLen = g.Length,
                    Api = api,
                    OldLid = oldLid,
                    Module = GetModule(api, normPath),
                    SubModule = SubModuleMap.TryGetValue(Path.GetFileName(file), out var sm) ? sm : "",
                    Message = ExtractMessage(text, m.Index + m.Length),
                };
                sites.Add(site);
            }
        }

        // ---------------- 命名 + ID ----------------
        var moduleCounter = ModuleIdBase.ToDictionary(kv => kv.Key, kv => kv.Value);
        var report = new StringBuilder();
        foreach (var s in sites)
        {
            string semantic = ExtractSemantic(s.Message);
            string prefix = ModulePrefix[s.Module];
            string baseName = prefix + s.SubModule + semantic;
            string name = baseName;
            int n = 2;
            while (usedNames.Contains(name)) { name = baseName + n; n++; }
            usedNames.Add(name);
            s.NewName = name;

            int id = moduleCounter[s.Module]++;
            while (usedIds.Contains(id)) { id = moduleCounter[s.Module]++; }
            usedIds.Add(id);
            s.NewId = id;
        }

        // ---------------- 报告 ----------------
        report.AppendLine($"LidIndexer 扫描报告  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"调用点总数: {sites.Count}");
        foreach (var g in sites.GroupBy(s => s.Module).OrderBy(g => ModuleIdBase.ContainsKey(g.Key) ? ModuleIdBase[g.Key] : 99999))
        {
            report.AppendLine($"  [{g.Key}] {g.Count()} 处  (ID {g.Min(x => x.NewId)} - {g.Max(x => x.NewId)})");
            foreach (var sub in g.GroupBy(x => x.SubModule).OrderByDescending(x => x.Count()))
            {
                report.AppendLine($"      {sub.Key,-20} {sub.Count()}");
            }
        }
        if (looseExtras.Count > 0)
        {
            report.AppendLine();
            report.AppendLine($"非调用上下文的残留引用 ({looseExtras.Count}):");
            foreach (var e in looseExtras) report.AppendLine("  " + e);
        }
        report.AppendLine();
        report.AppendLine("---- 明细 ----");
        foreach (var s in sites)
        {
            string msg = s.Message == "" ? "(动态消息)" : s.Message;
            if (msg.Length > 80) msg = msg.Substring(0, 80) + "...";
            report.AppendLine($"{s.NewId,6}  {s.NewName,-55} {s.Module}/{s.SubModule,-16} {Path.GetFileName(s.FilePath)}:{s.Line}  <- {msg}");
        }
        File.WriteAllText(ReportFile, report.ToString(), new UTF8Encoding(true));

        Console.WriteLine($"调用点总数: {sites.Count}");
        foreach (var g in sites.GroupBy(s => s.Module).OrderBy(g => ModuleIdBase.ContainsKey(g.Key) ? ModuleIdBase[g.Key] : 99999))
            Console.WriteLine($"  [{g.Key,-8}] {g.Count(),4} 处 (ID {g.Min(x => x.NewId)}-{g.Max(x => x.NewId)})");
        if (looseExtras.Count > 0)
            Console.WriteLine($"注意: {looseExtras.Count} 处非调用上下文引用（见 report.txt）");
        Console.WriteLine("报告: " + ReportFile);

        if (!apply)
        {
            Console.WriteLine("dry-run 完成（未写任何文件）。加 -apply 执行改写。");
            return;
        }

        // ---------------- 改写源文件 ----------------
        int changedFiles = 0;
        foreach (var fileGroup in sites.GroupBy(s => s.FilePath))
        {
            var enc = DetectEncoding(fileGroup.Key, out byte[] bytes);
            string text = enc.GetString(bytes);
            // 从后往前替换，避免位置失效
            foreach (var s in fileGroup.OrderByDescending(s => s.NameStart))
            {
                text = text.Remove(s.NameStart, s.NameLen).Insert(s.NameStart, s.NewName);
            }
            File.WriteAllBytes(fileGroup.Key, enc.GetPreamble().Concat(enc.GetBytes(text)).ToArray());
            changedFiles++;
        }
        Console.WriteLine($"已改写 {changedFiles} 个源文件。");

        // ---------------- 追加 LID.cs ----------------
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("        // ==== 自动生成的 LID 索引（LidIndexer 生成：模块/子模块/语义） ====");
        foreach (var modGroup in sites.GroupBy(s => s.Module)
                                      .OrderBy(g => ModuleIdBase.ContainsKey(g.Key) ? ModuleIdBase[g.Key] : 99999))
        {
            sb.AppendLine($"        // ---- {modGroup.Key} 模块 ----");
            foreach (var sub in modGroup.GroupBy(s => s.SubModule))
            {
                string label = sub.Key == "" ? "(common)" : sub.Key;
                sb.AppendLine($"        // {label}");
                foreach (var s in sub.OrderBy(x => x.NewId))
                {
                    sb.AppendLine($"        {s.NewName} = {s.NewId},");
                }
            }
        }
        // 插入到枚举最后一个成员之后（找最后一个成员行）
        var lidText2 = File.ReadAllText(LidFile, Encoding.UTF8);
        var lastMember = Regex.Matches(lidText2, @"^\s*\w+\s*=\s*\d+\s*,\s*$", RegexOptions.Multiline).Last();
        int insertAt = lastMember.Index + lastMember.Length;
        lidText2 = lidText2.Insert(insertAt, sb.ToString().TrimEnd() + Environment.NewLine);
        File.WriteAllText(LidFile, lidText2, new UTF8Encoding(true));
        Console.WriteLine("已追加 LID.cs 枚举段。");

        // ---------------- 追加 CSV ----------------
        var csv = new StringBuilder();
        csv.AppendLine();
        foreach (var s in sites.OrderBy(x => x.NewId))
        {
            bool isError = s.OldLid == "MetaCoreAssertShowMessage";
            string logType = isError ? "Error" : "Info";
            string enableAssert = isError ? "TRUE" : "FALSE";
            string pass = isError ? "FALSE" : "TRUE";
            string zh = SanitizeCsv(s.Message);
            csv.AppendLine($"{s.NewId},{logType},{enableAssert},{pass},0,,{zh},,,,");
        }
        File.AppendAllText(CsvFile, csv.ToString(), new UTF8Encoding(true));
        Console.WriteLine($"已追加 ErrorDefinitions.csv {sites.Count} 行。");
    }

    static string GetModule(string api, string normPath)
    {
        if (normPath.Contains("/Export/")) return "Export";
        switch (api)
        {
            case "AddTokenByString":
            case "AddTokenLog": return "Token";
            case "AddNodeLog": return "Node";
            case "AddFileMetaLog": return "File";
            case "AddMetaCoreLog": return "MetaCore";
            case "AddIRLog": return "IR";
            case "AddProjectLog": return "Project";
            case "AddProcessLog": return "Process";
        }
        return "MetaCore";
    }

    static int GetLine(string text, int pos)
    {
        int line = 1;
        for (int i = 0; i < pos && i < text.Length; i++)
            if (text[i] == '\n') line++;
        return line;
    }

    /// <summary>从调用点 LID 名之后提取参数列表，返回其中第一个非空字符串字面量（含 $"" 的静态部分）。</summary>
    static string ExtractMessage(string text, int startPos)
    {
        // 扫到调用结束的右括号
        int i = startPos;
        int depth = 1;
        bool inStr = false;
        int end = Math.Min(text.Length, startPos + 2000);
        while (i < end && depth > 0)
        {
            char c = text[i];
            if (inStr)
            {
                if (c == '\\') { i += 2; continue; }
                if (c == '"') inStr = false;
                i++;
                continue;
            }
            if (c == '"') { inStr = true; i++; continue; }
            if (c == '(') depth++;
            else if (c == ')') { depth--; if (depth == 0) break; }
            i++;
        }
        string args = text.Substring(startPos, Math.Max(0, i - startPos));

        // 在参数区找第一个非空字符串字面量
        for (int j = 0; j < args.Length; j++)
        {
            bool interp = args[j] == '$' && j + 1 < args.Length && args[j + 1] == '"';
            if (args[j] != '"' && !interp) continue;

            int k = interp ? j + 2 : j + 1;
            var sb = new StringBuilder();
            while (k < args.Length)
            {
                char c = args[k];
                if (c == '\\') { k += 2; continue; }
                if (c == '"') break;
                if (interp && c == '{')
                {
                    if (k + 1 < args.Length && args[k + 1] == '{') { sb.Append('{'); k += 2; continue; }
                    int close = args.IndexOf('}', k);
                    if (close < 0) { k++; continue; }
                    sb.Append("{}");
                    k = close + 1;
                    continue;
                }
                sb.Append(c);
                k++;
            }
            string s = sb.ToString().Trim();
            if (s.Length > 0) return s;
            j = k; // 空串，继续找下一个字面量
        }
        return "";
    }

    /// <summary>从消息文本提取语义英文词（PascalCase，最多 3 个）。</summary>
    static string ExtractSemantic(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Issue";

        string lower = message.ToLowerInvariant();
        var words = new List<string>();

        foreach (var (phrase, word) in EnPhrases)
        {
            if (lower.Contains(phrase) && !words.Contains(word)) words.Add(word);
        }

        foreach (var w in Regex.Matches(message, @"[A-Za-z_][A-Za-z0-9_]*"))
        {
            string tok = w.ToString();
            // 过滤纯下划线/下划线+数字（如 __1）与纯重复串（如 aaaa）
            if (Regex.IsMatch(tok, @"^_+$") || Regex.IsMatch(tok, @"^_+\d+$")) continue;
            if (Regex.IsMatch(tok, @"^(.)\1+$")) continue;
            if (NoiseWords.Contains(tok)) continue;
            string p = char.ToUpperInvariant(tok[0]) + tok.Substring(1);
            if (!words.Contains(p) && !words.Contains(tok)) words.Add(p);
            if (words.Count >= 4) break;
        }

        if (words.Count == 0)
        {
            foreach (var (kw, word) in CnWords)
            {
                if (message.Contains(kw) && !words.Contains(word)) words.Add(word);
            }
        }

        if (words.Count == 0) return "Issue";
        return string.Concat(words.Take(3));
    }

    /// <summary>CSV 单元格清洗：去引号、去换行、截断；含逗号则整体加引号包裹。</summary>
    static string SanitizeCsv(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return "";
        string s = msg.Replace("\"", "").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
        if (s.Length > 150) s = s.Substring(0, 150) + "...";
        if (s.Contains(",")) s = "\"" + s + "\"";
        return s;
    }

    static Encoding DetectEncoding(string path, out byte[] bytes)
    {
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch
        {
            bytes = null;
            return null;
        }
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new UTF8Encoding(true);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode;
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode;

        var utf8 = Encoding.UTF8;
        bool valid = true;
        try
        {
            var strict = new UTF8Encoding(false, true);
            strict.GetString(bytes);
        }
        catch { valid = false; }
        if (valid) return new UTF8Encoding(false);

        try { return Encoding.GetEncoding("GB18030"); }
        catch { return Encoding.UTF8; }
    }
}
