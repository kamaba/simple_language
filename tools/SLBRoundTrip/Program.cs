//****************************************************************************
//  File:      Program.cs (SLBRoundTrip)
// ------------------------------------------------
//  Description:  T1 round-trip 验证（SLB_DESIGN.md §12-P0）：
//                module.json → SLModulePackageWriter.Read → SLBWriter.WriteToBytes
//                → SLBReader.FromBytes → 与原包做 JSON 深度对比（压缩/非压缩各一轮）。
//****************************************************************************

using SimpleLanguage.Export.SLIR;
using SimpleLanguage.Export.SLIR.Binary;
using SimpleLanguage.Export.SLIR.Types;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length == 0)
{
    Console.WriteLine("用法: SLBRoundTrip <module.json | module.slb> [文件 ...]");
    Console.WriteLine(".json 输入: Read → SLB WriteToBytes → FromBytes → JSON 深度对比（compressed/raw 各一轮）。");
    Console.WriteLine(".slb  输入: FromBytes（四道校验门）→ WriteToBytes → FromBytes → JSON 深度对比（同模式两轮）。");
    return 2;
}

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};
options.Converters.Add(new JsonStringEnumConverter());
options.Converters.Add(new InstructionPayloadByteArrayJsonConverter());

int failedCount = 0;
foreach (var file in args)
{
    if (!File.Exists(file))
    {
        Console.WriteLine($"[FAIL] {file}: 文件不存在");
        failedCount++;
        continue;
    }

    SLModulePackage originalPackage;
    if (string.Equals(Path.GetExtension(file), ".slb", StringComparison.OrdinalIgnoreCase))
    {
        // .slb 输入：直接从磁盘二进制读回（四道校验门全过即为有效产物）
        var slbBytes = File.ReadAllBytes(file);
        originalPackage = SLBReader.FromBytes(slbBytes);
        Console.WriteLine($"[INFO] {Path.GetFileName(file)}: 磁盘 .slb 读回成功（{slbBytes.Length} bytes，含头部/解压/段表/段四道校验）");
    }
    else
    {
        originalPackage = SLModulePackageWriter.Read(file);
    }

    foreach (var compressed in new[] { true, false })
    {
        var modeName = compressed ? "compressed" : "raw";
        try
        {
            var bytes = SLBWriter.WriteToBytes(originalPackage, compressed);
            var roundTripped = SLBReader.FromBytes(bytes);

            var jsonOriginal = JsonSerializer.Serialize(originalPackage, options);
            var jsonRoundTripped = JsonSerializer.Serialize(roundTripped, options);

            if (string.Equals(jsonOriginal, jsonRoundTripped, StringComparison.Ordinal))
            {
                Console.WriteLine($"[PASS] {Path.GetFileName(file)} ({modeName}, {bytes.Length} bytes): round-trip 一致");
            }
            else
            {
                failedCount++;
                ReportFirstDifference(file, modeName, jsonOriginal, jsonRoundTripped);
            }
        }
        catch (Exception ex)
        {
            failedCount++;
            Console.WriteLine($"[FAIL] {Path.GetFileName(file)} ({modeName}): {ex.GetType().Name}: {ex.Message}");
        }
    }
}

Console.WriteLine(failedCount == 0 ? "全部 PASS" : $"{failedCount} 项 FAIL");
return failedCount == 0 ? 0 : 1;

static void ReportFirstDifference(string file, string modeName, string jsonOriginal, string jsonRoundTripped)
{
    var minLength = Math.Min(jsonOriginal.Length, jsonRoundTripped.Length);
    int diffIndex = 0;
    while (diffIndex < minLength && jsonOriginal[diffIndex] == jsonRoundTripped[diffIndex])
    {
        diffIndex++;
    }

    Console.WriteLine($"[FAIL] {Path.GetFileName(file)} ({modeName}): JSON 不一致，首个差异 @char {diffIndex}（原 {jsonOriginal.Length} chars / 往返 {jsonRoundTripped.Length} chars）");
    const int contextRadius = 160;
    var begin = Math.Max(0, diffIndex - contextRadius);
    var endOriginal = Math.Min(jsonOriginal.Length, diffIndex + contextRadius);
    var endRoundTripped = Math.Min(jsonRoundTripped.Length, diffIndex + contextRadius);
    Console.WriteLine("  original   ...[" + jsonOriginal.Substring(begin, endOriginal - begin).Replace("\r\n", "\\n").Replace("\n", "\\n") + "]...");
    Console.WriteLine("  roundTrip  ...[" + jsonRoundTripped.Substring(begin, endRoundTripped - begin).Replace("\r\n", "\\n").Replace("\n", "\\n") + "]...");
}
