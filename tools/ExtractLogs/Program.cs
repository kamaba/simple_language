using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

// Small tool to extract literal log messages from source files and
// write a CSV with columns: id,messageTemplate,severity,paramCount,module,abortCurrent,abortLater,displayType,fixHint

class Program
{
    static void Main(string[] args)
    {
        var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var outPath = Path.Combine(root, "extracted_logs.csv");
        // write with BOM to ensure proper display in Excel/Notepad
        using var sw = new StreamWriter(outPath, false, new UTF8Encoding(true));
        sw.WriteLine("id,messageTemplate,severity,paramCount,module,abortCurrent,abortLater,displayType,fixHint");

        int id = 10000;

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string text = ReadAllTextDetectEncoding(file);
            if (string.IsNullOrEmpty(text)) continue;
            // match Log.Add... calls with a literal string as the next parameter
            var regex = new Regex(@"Log\.(AddInStructFileMeta|AddInStructMeta|AddInHandleToken|AddInHandleNode|AddGenIR|AddVM)\s*\([^,]*,\s*\""(?<msg>(?:[^\""\\]|\\.)*)\""", RegexOptions.Singleline);
            foreach(Match m in regex.Matches(text))
            {
                var raw = m.Groups["msg"].Value;
                // unescape common escapes for nicer CSV
                var msg = raw.Replace("\\\"", "\"").Replace("\\r", "").Replace("\\n", "\\n");
                // escape double quotes for CSV
                var safe = msg.Replace("\"", "\"\"");
                sw.WriteLine($"{id},\"{safe}\",Error,0,FileMeta,false,false,Direct,\"");
                id++;
            }
        }

        Console.WriteLine("extracted to " + outPath);
    }

    static string ReadAllTextDetectEncoding(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            }
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }

            // try utf8
            var utf8 = Encoding.UTF8.GetString(bytes);
            if (!utf8.Contains("\uFFFD"))
                return utf8;

            // fallback to GB18030 (covers GBK/GB2312)
            try
            {
                var gb = Encoding.GetEncoding("GB18030");
                return gb.GetString(bytes);
            }
            catch
            {
                return utf8; // give up
            }
        }
        catch
        {
            return null;
        }
    }
}
