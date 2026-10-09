//****************************************************************************
//  File:      MetaAttribute.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/3/1 12:00:00
//  Description: attribute metadata - CLR style attribute system for MetaCore
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public sealed class MetaAttribute
    {
        // ---- 四时点 × 五注册点契约（与 Lib/Core/Attribute.sl 的 EAttributeStage/EAttributeTarget 逐项对齐）----
        public const int StagePreCompile = 0;   // 编译前: Front Meta 层入口回调
        public const int StageCompiling = 1;    // 编译中: Front IR/SLIR 物化
        public const int StagePreload = 2;      // 运行前: cvm 装载/装配期注册
        public const int StageRuntime = 3;      // 运行中: cvm 执行期触发/查询
        public const int TargetClass = 1;       // class 声明
        public const int TargetData = 2;        // 模块级 data
        public const int TargetEnum = 4;        // enum 声明
        public const int TargetField = 8;       // 成员变量
        public const int TargetMethod = 16;     // 成员函数
        public const int TargetAll = 31;        // 全部注册点

        public string name { get; }
        public FileMetaAttributeSyntax fileMetaAttribute { get; }

        /// <summary>此 attribute 挂载的宿主（MetaClass / MetaData / MetaEnum / MetaMemberFunction / MetaMemberVariable）</summary>
        public MetaBase ownerMetaBase => m_OwnerMetaBase;
        /// <summary>解析出的 Attribute 子类 MetaClass（如 Nickname 类），未解析时为 null</summary>
        public MetaClass attributeMetaClass => m_AttributeMetaClass;
        /// <summary>从参数列表提取的字符串参数</summary>
        public List<string> stringArgs => m_StringArgs;
        /// <summary>生效时点（EAttributeStage）：静态解释自 Attribute 子类 _init_；-1 = 未解析</summary>
        public int attributeStage => m_Stage;
        /// <summary>合法注册点位掩码（EAttributeTarget）：静态解释自 Attribute 子类 _init_；0 = 未解析</summary>
        public int attributeTargets => m_Targets;

        private MetaBase m_OwnerMetaBase = null;
        private MetaClass m_AttributeMetaClass = null;
        private List<string> m_StringArgs = new List<string>();
        private int m_Stage = -1;        // -1 = 未解析（未声明 / 未跑 Parse）
        private int m_Targets = 0;       // 0 = 未解析
        private bool m_IsParsed = false;

        /// <summary>
        /// 使用侧跨模块契约兜底表：本模块 .sl 里写的 @X（如 SpecialTest 写 @DllImport），
        /// Parse() 查到的 X 是 ref 模块壳类（无 FileMeta 解析树），_init_ 静态解释必失败，
        /// 按名称回退取契约（值与各 .sl 子类 _init_ 定稿一致）。
        /// 注意：这不是 SLIR 恢复链的兜底——恢复构造直接携带 module.json 导出的 stage/targets。
        /// </summary>
        private static readonly Dictionary<string, int[]> s_BuiltInContracts =
            new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Nickname",        new int[] { StageCompiling, TargetAll } },
                { "AOT",             new int[] { StageCompiling, TargetAll } },
                { "GPU",             new int[] { StageCompiling, TargetMethod } },
                { "DllImport",       new int[] { StagePreload, TargetMethod } },
                { "DllStaticImport", new int[] { StagePreload, TargetMethod } },
                { "Route",           new int[] { StagePreload, TargetAll } },
                { "Serializable",    new int[] { StageRuntime, TargetClass } },
                { "SerializeField",  new int[] { StageRuntime, TargetField } },
                { "NonSerialized",   new int[] { StageRuntime, TargetField } },
                { "Exclude",         new int[] { StagePreCompile, TargetAll } },
            };

        public MetaAttribute(FileMetaAttributeSyntax attr)
        {
            fileMetaAttribute = attr;
            name = attr?.name;
        }

        /// <summary>
        /// 跨模块恢复构造：从 module.json 导出的 attribute 包（SLAttributePackage）
        /// 重建，无 FileMeta 解析树。splitStringArgs 即导出端的字符串实参列表
        /// （GetSplitStringArgs 语义）；stage/targets 即导出端解析好的契约值，
        /// 直接透传（再次导出时保持原值，无需契约表）。m_IsParsed 置位防止
        /// Parse() 重跑（fileMetaAttribute 为 null 时 ExtractStringArgs 会清空
        /// m_StringArgs）。
        /// </summary>
        public MetaAttribute(string attrName, List<string> splitStringArgs,
            int stageValue = -1, int targetsValue = 0)
        {
            name = attrName;
            if (splitStringArgs != null)
            {
                foreach (var a in splitStringArgs)
                    m_StringArgs.Add(a ?? string.Empty);
            }
            m_Stage = stageValue;
            m_Targets = targetsValue;
            m_IsParsed = true;
        }

        public void SetOwner(MetaBase owner)
        {
            m_OwnerMetaBase = owner;
        }

        /// <summary>
        /// 解析 attribute：查找 Attribute 子类 MetaClass，提取字符串参数，
        /// 静态解释子类 _init_ 中的 _attributeStage/_attributeTargets，
        /// 最后按 targets 校验放置合法性。在 AttributeManager.ParseAllAttributes 阶段统一调用。
        /// </summary>
        public void Parse()
        {
            if (m_IsParsed) return;
            m_IsParsed = true;

            if (string.IsNullOrEmpty(name))
            {
                Log.AddMetaCoreLog(LID.MetaCoreAttributeIsNullMetaAttributeAttribute, "MetaAttribute.Parse: attribute name is null or empty");
                return;
            }

            // 从 ClassManager 查找 Attribute 子类
            m_AttributeMetaClass = ClassManager.instance.GetClassByName(name, 0);
            if (m_AttributeMetaClass == null)
            {
                m_AttributeMetaClass = ClassManager.instance.GetClassByName("Core." + name, 0)
                                       ?? ClassManager.instance.GetClassByName("Std." + name, 0);
            }
            if (m_AttributeMetaClass == null)
            {
                // 本模块自定义 attribute 子类兜底：本模块类注册 key 为全名
                // （如 ProjectTest.LogTrace_0），上述短名/Core./Std. 查找均 miss；
                // 按短名遍历（过滤引用 shell）兜底命中，使 _init_ 静态解释可进行。
                m_AttributeMetaClass = ClassManager.instance.FindSelfMetaClassByShortName(name, 0);
            }

            // 提取字符串参数
            ExtractStringArgs();

            // P2/P3: 静态解释 _attributeStage/_attributeTargets → 放置校验
            // （SLIR 契约直接导出 stage/targets，不再派生 handleType）
            ResolveStageAndTargets();
            ValidatePlacement();

            if (m_AttributeMetaClass == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAttributeNotFoundMetaAttributeAttribute,
                    $"MetaAttribute.Parse: attribute class '{name}' not found, args: {m_StringArgs.Count}, stage: {m_Stage}, targets: {m_Targets}");
            }
        }

        /// <summary>
        /// 解析 _attributeStage/_attributeTargets：
        /// 1. 沿继承链静态解释子类 _init_ 中的赋值（FileMeta 层语法树）；
        /// 2. 失败时查内建契约兜底表（跨模块 ref 壳类无解析树，必走此路）；
        /// 3. 仍未命中：类未找到（拼写/旧语法残留）静默回退 Compiling+All（与旧默认一致）；
        ///    类已找到但未声明则报 Error（设计强制声明）。
        /// </summary>
        private void ResolveStageAndTargets()
        {
            if (ResolveFromInitBlocks())
                return;
            if (LookupBuiltInContract())
                return;

            if (m_AttributeMetaClass != null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAttributeStageTargetsNotFound,
                    $"MetaAttribute.Parse: attribute 子类 '{name}' 的 _init_ 中未声明 _attributeStage/_attributeTargets, 已回退 Compiling+All, owner='{m_OwnerMetaBase?.allName}'");
            }
            m_Stage = StageCompiling;
            m_Targets = TargetAll;
        }

        /// <summary>
        /// 沿继承链（子类优先，guard 16 层）在各类自身定义的 _init_ 方法体中
        /// 静态解释 _attributeStage/_attributeTargets 赋值。此时方法体仅有
        /// FileMeta 层语法（ParseStatements 未跑），只能按语法树模式匹配。
        /// </summary>
        private bool ResolveFromInitBlocks()
        {
            var mc = m_AttributeMetaClass;
            int guard = 0;
            while (mc != null && guard++ < 16)
            {
                foreach (var mmf in mc.nonStaticVirtualMetaMemberFunctionList)
                {
                    if (mmf == null || mmf.name != "_init_") continue;
                    var block = mmf.fileMetaMemberFunction?.fileMetaBlockSyntax;
                    if (block == null) continue;
                    InterpretAttributeInitBlock(block);
                    if (m_Stage >= 0 && m_Targets > 0)
                        return true;
                }
                mc = mc.extendClass;
            }
            return m_Stage >= 0 && m_Targets > 0;
        }

        /// <summary>
        /// 静态解释 _init_ 方法体：匹配 this._attributeStage / this._attributeTargets = 常量
        /// 形式的赋值语句（FileMetaOpAssignSyntax），右侧支持内建枚举成员与整数字面量。
        /// 其余语句（如 this._nickname = nickname）因右侧非常量被跳过。
        /// 注：调用链中的点号是独立节点（this._x → [this][.][_x]），需过滤后再匹配。
        /// </summary>
        private void InterpretAttributeInitBlock(FileMetaBlockSyntax block)
        {
            if (block == null) return;
            foreach (var syntax in block.fileMetaSyntax)
            {
                if (!(syntax is FileMetaOpAssignSyntax opAssign)) continue;
                var names = FilterLinkNames(opAssign.variableRef);
                if (names.Count != 2 || names[0] != "this") continue;
                var memberName = names[1];
                int val = InterpretAttributeConstValue(opAssign.express);
                if (val < 0) continue;
                if (memberName == "_attributeStage")
                    m_Stage = val;
                else if (memberName == "_attributeTargets")
                    m_Targets = val;
            }
        }

        /// <summary>解释赋值右侧常量：EAttributeStage/EAttributeTarget 枚举成员或 int 字面量；无法解释返回 -1</summary>
        private int InterpretAttributeConstValue(FileMetaBaseTerm term)
        {
            if (term is FileMetaCallTerm ct)
            {
                var names = FilterLinkNames(ct.callLink);
                if (names.Count == 2)
                    return LookupBuiltInEnumValue(names[0], names[1]);
                return -1;
            }
            if (term is FileMetaConstValueTerm cvt)
            {
                var lex = cvt.token?.lexeme?.ToString();
                if (int.TryParse(lex, out var v)) return v;
                return -1;
            }
            return -1;
        }

        /// <summary>调用链节点名列表（过滤点号占位节点：this._x → [this][_x]）</summary>
        private static List<string> FilterLinkNames(FileMetaCallLink link)
        {
            var names = new List<string>();
            if (link == null) return names;
            foreach (var node in link.callNodeList)
            {
                if (node == null) continue;
                var n = node.name;
                if (n == "." || string.IsNullOrEmpty(n)) continue;
                names.Add(n);
            }
            return names;
        }

        /// <summary>EAttributeStage/EAttributeTarget 枚举成员值查表（与 Attribute.sl 定义逐项对齐）</summary>
        private static int LookupBuiltInEnumValue(string enumName, string memberName)
        {
            if (string.IsNullOrEmpty(enumName) || string.IsNullOrEmpty(memberName)) return -1;
            if (enumName.Equals("EAttributeStage", StringComparison.Ordinal))
            {
                switch (memberName)
                {
                    case "PreCompile": return StagePreCompile;
                    case "Compiling": return StageCompiling;
                    case "Preload": return StagePreload;
                    case "Runtime": return StageRuntime;
                    default: return -1;
                }
            }
            if (enumName.Equals("EAttributeTarget", StringComparison.Ordinal))
            {
                switch (memberName)
                {
                    case "Class": return TargetClass;
                    case "Data": return TargetData;
                    case "Enum": return TargetEnum;
                    case "Field": return TargetField;
                    case "Method": return TargetMethod;
                    case "All": return TargetAll;
                    default: return -1;
                }
            }
            return -1;
        }

        /// <summary>查内建契约兜底表（跨模块场景），命中则填入 stage/targets</summary>
        private bool LookupBuiltInContract()
        {
            if (s_BuiltInContracts.TryGetValue(name, out var contract))
            {
                m_Stage = contract[0];
                m_Targets = contract[1];
                return true;
            }
            return false;
        }

        /// <summary>按 targets 位掩码校验放置合法性（跨模块恢复 stage 未解析时跳过）</summary>
        private void ValidatePlacement()
        {
            if (m_Stage < 0 || m_Targets <= 0) return;
            if (m_OwnerMetaBase == null) return;
            int hostBit = GetOwnerTargetBit();
            if (hostBit == 0) return;
            if ((m_Targets & hostBit) == 0)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAttributePlacementInvalid,
                    $"MetaAttribute.Parse: attribute '{name}' 不允许标注在 {OwnerKindName()} (stage={m_Stage}, targets={m_Targets}, owner='{m_OwnerMetaBase.allName}')");
            }
        }

        /// <summary>宿主类型对应的注册点位（is 判断子类在前）</summary>
        private int GetOwnerTargetBit()
        {
            if (m_OwnerMetaBase is MetaMemberFunction) return TargetMethod;
            if (m_OwnerMetaBase is MetaMemberVariable) return TargetField;
            if (m_OwnerMetaBase is MetaEnum) return TargetEnum;
            if (m_OwnerMetaBase is MetaData) return TargetData;
            if (m_OwnerMetaBase is MetaClass) return TargetClass;
            return 0;
        }

        private string OwnerKindName()
        {
            if (m_OwnerMetaBase is MetaMemberFunction) return "成员函数";
            if (m_OwnerMetaBase is MetaMemberVariable) return "成员变量";
            if (m_OwnerMetaBase is MetaEnum) return "enum 声明";
            if (m_OwnerMetaBase is MetaData) return "data 声明";
            if (m_OwnerMetaBase is MetaClass) return "class 声明";
            return "未知宿主";
        }

        /// <summary>从 FileMetaParTerm 提取字符串参数列表</summary>
        private void ExtractStringArgs()
        {
            m_StringArgs.Clear();
            if (fileMetaAttribute?.fileMetaParTerm == null) return;

            var parTerm = fileMetaAttribute.fileMetaParTerm;
            foreach (var term in parTerm.fileMetaExpressList)
            {
                if (term == null) continue;
                var str = ExtractStringFromTerm(term);
                if (str != null)
                    m_StringArgs.Add(str);
            }
        }

        private string ExtractStringFromTerm(FileMetaBaseTerm term)
        {
            if (term is FileMetaConstValueTerm cvt)
            {
                var tok = cvt.token;
                if (tok == null) return null;
                if (tok.type == ETokenType.String)
                    return tok.lexeme?.ToString();
                return tok.lexeme?.ToString();
            }
            if (term is FileMetaCallTerm cct)
            {
                return cct.callLink?.ToFormatString();
            }
            return term.ToFormatString();
        }

        public string GetStringArg(int index)
        {
            if (m_StringArgs == null || index < 0 || index >= m_StringArgs.Count)
                return null;
            return m_StringArgs[index];
        }

        /// <summary>
        /// 按逗号拆分后的字符串实参列表（stringArgs 未拆分逗号符号项，
        /// 多实参时中间会混入 "," 项）。直接从 FileMetaParTerm 提取，
        /// 不依赖 Parse() 是否已执行（@DllImport 注入早于 ParseAttributes 阶段）。
        /// 非字符串实参被跳过。
        /// </summary>
        public List<string> GetSplitStringArgs()
        {
            var result = new List<string>();
            var fmpt = fileMetaAttribute?.fileMetaParTerm;
            if (fmpt == null)
            {
                /* 跨模块恢复的实例（无 FileMeta 解析树）：stringArgs 由恢复
                 * 构造预填充，语义与导出端一致，直接透传。 */
                result.AddRange(m_StringArgs);
                return result;
            }
            var plist = fmpt.SplitParamList();
            for (int i = 0; i < plist.Count; i++)
            {
                if (plist[i] is FileMetaConstValueTerm cvt && cvt.token?.type == ETokenType.String)
                {
                    var s = StringTokenContent(cvt.token);
                    if (s != null)
                        result.Add(s);
                }
            }
            return result;
        }

        /// <summary>String 常量 token -> 内容。子 token 优先（与
        /// MetaConstExpressNode 同源），兜底 lexeme 去引号。</summary>
        internal static string StringTokenContent(Token tok)
        {
            if (tok == null) return null;
            var cdlist = tok.childrenTokensList;
            if (cdlist.Count == 1 && cdlist[0].Count == 1 && cdlist[0][0].type == ETokenType.String)
                return cdlist[0][0].lexeme?.ToString();
            var s = tok.lexeme?.ToString();
            if (s != null && s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
                s = s.Substring(1, s.Length - 2);
            return s;
        }

        /// <summary>
        /// 按 Comma 拆分后的全部实参文本列表（数值/字符串/bool 均保留原文）。
        /// 字符串实参返回去引号内容，其余返回 token lexeme 文本。
        /// 用于数值型 attribute（如 GPU 的 tileSizeWidth 等）。
        /// </summary>
        public List<string> GetSplitRawArgs()
        {
            var result = new List<string>();
            var fmpt = fileMetaAttribute?.fileMetaParTerm;
            if (fmpt == null)
                return result;
            var plist = fmpt.SplitParamList();
            for (int i = 0; i < plist.Count; i++)
            {
                if (plist[i] is FileMetaConstValueTerm cvt)
                {
                    var tok = cvt.token;
                    if (tok == null) { result.Add(null); continue; }
                    if (tok.type == ETokenType.String)
                        result.Add(StringTokenContent(tok));
                    else
                        result.Add(tok.lexeme?.ToString());
                }
                else
                {
                    result.Add(plist[i].ToFormatString());
                }
            }
            return result;
        }

        /// <summary>
        /// 提取 int 实参（按 Comma 拆分后的位置索引）。
        /// 越界、空值或非数值文本返回 defaultValue。
        /// </summary>
        public int GetIntArg(int index, int defaultValue = 0)
        {
            if (index < 0) return defaultValue;
            var raw = GetSplitRawArgs();
            if (index >= raw.Count || raw[index] == null) return defaultValue;
            if (int.TryParse(raw[index].Trim(), out var v)) return v;
            return defaultValue;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("@").Append(name);
            if (m_StringArgs.Count > 0)
            {
                sb.Append("(");
                for (int i = 0; i < m_StringArgs.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append("\"").Append(m_StringArgs[i]).Append("\"");
                }
                sb.Append(")");
            }
            return sb.ToString();
        }
    }
}
