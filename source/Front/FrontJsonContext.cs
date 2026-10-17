#nullable enable
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimpleLanguage.Export.SLIR.Types;

namespace SimpleLanguage
{
    /// <summary>
    /// Front 全仓 JSON source-gen 序列化上下文（Native AOT 兼容，2026-10-17）。
    ///
    /// 背景：SlangAllInOne 以 -p:PublishAot=true 发布时，System.Text.Json 的反射
    /// 序列化在运行期需要动态展开泛型（DefaultJsonTypeInfoResolver 内部
    /// MakeGenericType 创建 JsonPropertyInfo&lt;T&gt;），Native AOT 不支持运行期
    /// 新建泛型实例化 → MissingMetadataException（"missing native code or
    /// metadata"）。官方唯一解：source generator 在编译期生成序列化元数据。
    ///
    /// 本上下文只登记"根类型"（Serialize/Deserialize 的 TValue）：属性图可达的
    /// 嵌套类型由生成器自动递归覆盖。byte[] payload 的 Latin1 字符串编码契约
    /// 由 SLIRTypes.SLIRInstructionPackage.payload 属性上的
    /// [JsonConverter(InstructionPayloadByteArrayJsonConverter)] 保持。
    ///
    /// 模型内无枚举类型（opCode/stage/slot 等全部 int/byte 编码），不涉及
    /// JsonStringEnumConverter 兼容问题。
    /// </summary>
    [JsonSourceGenerationOptions(WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true)]
    [JsonSerializable(typeof(SLModulePackage))]
    [JsonSerializable(typeof(SLPackageRootJson))]
    [JsonSerializable(typeof(SLSystemMethodCallPackage))]
    [JsonSerializable(typeof(IR.IRData.RuntimeDefTypeExport))]
    [JsonSerializable(typeof(Compile.PluginLabelRequest))]
    [JsonSerializable(typeof(Compile.PluginLabelParse))]
    [JsonSerializable(typeof(Compile.PluginLabelBuildRequest))]
    [JsonSerializable(typeof(Compile.PluginLabelBuild))]
    internal sealed partial class FrontJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// 三个预置 JsonSerializerOptions（全部挂 FrontJsonContext，AOT 走 source-gen
    /// 元数据而非反射）：
    ///   PackageWrite —— SLIR 包导出（SLModulePackageWriter.SerializePackage）：
    ///     与旧反射配置逐项一致（WriteIndented + WhenWritingNull +
    ///     UnsafeRelaxedJsonEscaping），产物字节级兼容。
    ///   PackageRead —— SLIR 包读回（Read/ReadModulePackageJson）：
    ///     CaseInsensitive + 跳注释 + 尾逗号，兼容旧反射配置。
    ///   Plain —— 插件契约 / IRData 遗留 JSON payload：纯默认（原调用点不传
    ///     options，保持输出形态不变，仅切换元数据来源）。
    /// </summary>
    internal static class FrontJsonOptions
    {
        public static readonly JsonSerializerOptions PackageWrite = Create(write: true);

        public static readonly JsonSerializerOptions PackageRead = Create(write: false);

        public static readonly JsonSerializerOptions Plain = new JsonSerializerOptions
        {
            TypeInfoResolver = FrontJsonContext.Default,
        };

        private static JsonSerializerOptions Create(bool write)
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = FrontJsonContext.Default,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            if (write)
            {
                options.WriteIndented = true;
                options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            }
            return options;
        }
    }
}
