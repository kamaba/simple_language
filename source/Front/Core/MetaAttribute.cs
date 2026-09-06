//****************************************************************************
//  File:      MetaAttribute.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/3/1 12:00:00
//  Description: attribute metadata - CLR style attribute system for MetaCore
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public sealed class MetaAttribute
    {
        public string name { get; }
        public FileMetaAttributeSyntax fileMetaAttribute { get; }

        /// <summary>此 attribute 挂载的宿主（MetaClass / MetaMemberFunction / MetaMemberVariable）</summary>
        public MetaBase ownerMetaBase => m_OwnerMetaBase;
        /// <summary>解析出的 Attribute 子类 MetaClass（如 Nickname 类），未解析时为 null</summary>
        public MetaClass attributeMetaClass => m_AttributeMetaClass;
        /// <summary>从参数列表提取的字符串参数</summary>
        public List<string> stringArgs => m_StringArgs;
        /// <summary>处理时机：Compile 或 Runtime，从 Attribute 子类的 _attributeHandleType 读取</summary>
        public int handleType => m_HandleType;

        private MetaBase m_OwnerMetaBase = null;
        private MetaClass m_AttributeMetaClass = null;
        private List<string> m_StringArgs = new List<string>();
        private int m_HandleType = 0; // 默认 Compile = 0
        private bool m_IsParsed = false;

        public MetaAttribute(FileMetaAttributeSyntax attr)
        {
            fileMetaAttribute = attr;
            name = attr?.name;
        }

        public void SetOwner(MetaBase owner)
        {
            m_OwnerMetaBase = owner;
        }

        /// <summary>
        /// 解析 attribute：查找 Attribute 子类 MetaClass，提取字符串参数，读取 handleType。
        /// 在 ClassManager.ParseAttributes() 阶段统一调用，此时所有类型已就绪。
        /// </summary>
        public void Parse()
        {
            if (m_IsParsed) return;
            m_IsParsed = true;

            if (string.IsNullOrEmpty(name))
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "MetaAttribute.Parse: attribute name is null or empty");
                return;
            }

            // 从 ClassManager 查找 Attribute 子类
            m_AttributeMetaClass = ClassManager.instance.GetClassByName(name, 0);
            if (m_AttributeMetaClass == null)
            {
                m_AttributeMetaClass = ClassManager.instance.GetClassByName("Core." + name, 0)
                                       ?? ClassManager.instance.GetClassByName("Std." + name, 0);
            }

            // 提取字符串参数
            ExtractStringArgs();

            // 读取 handleType
            m_HandleType = ResolveHandleType();

            if (m_AttributeMetaClass == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage,
                    $"MetaAttribute.Parse: attribute class '{name}' not found, args: {m_StringArgs.Count}, handleType: {m_HandleType}");
            }
        }

        /// <summary>
        /// 从 Attribute 子类的 _attributeHandleType 成员变量读取处理时机。
        /// 先查子类自身的成员变量，再查继承链中的默认值。
        /// 如果无法确定，使用名称回退映射。
        /// </summary>
        private int ResolveHandleType()
        {
            // 1. 尝试从 MetaClass 读取 _attributeHandleType 成员的常量表达式值
            if (m_AttributeMetaClass != null)
            {
                var mmv = FindMemberVariable(m_AttributeMetaClass, "_attributeHandleType");
                if (mmv != null && mmv.constExpressNode != null)
                {
                    var val = mmv.constExpressNode.value;
                    if (val != null)
                    {
                        if (val is int iv) return iv;
                        if (val is long lv) return (int)lv;
                        // 尝试字符串解析
                        if (val is string sv && int.TryParse(sv, out var parsed))
                            return parsed;
                    }
                }
            }

            // 2. 名称回退映射：已知内置属性的 handleType
            switch (name)
            {
                case "Nickname":
                case "AOT":
                case "GPU":
                    return 0; // Compile
                case "Condition":
                case "Route":
                    return 1; // Runtime
                default:
                    return 0; // 默认 Compile
            }
        }

        /// <summary>在 MetaClass 及其继承链中查找成员变量</summary>
        private MetaMemberVariable FindMemberVariable(MetaClass mc, string memberName)
        {
            if (mc == null) return null;
            // 查找自身定义的成员变量
            foreach (var mmv in mc.allMetaMemberVariableList)
            {
                if (mmv != null && mmv.name == memberName)
                    return mmv;
            }
            // 查找继承链
            return FindMemberVariable(mc.extendClass, memberName);
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
                return result;
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
