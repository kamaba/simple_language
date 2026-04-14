using System;
using System.IO;
using System.Text;

namespace SimpleLanguage.VM
{
    /// <summary>
    /// Mirrors SL/VM console output (SystemPrint / SystemPrintln) to a UTF-8 <c>Result.txt</c> for inspection.
    /// <para><b>Path resolution</b> (first match wins):</para>
    /// <list type="number">
    /// <item><c>SIMPLELANG_VM_RESULT_DIR</c> — directory to place <c>Result.txt</c> (created if missing).</item>
    /// <item>Else <c>Path.Combine(SIMPLELANG_EXPORT_OUTDIR, "vm-results")</c> when <c>SIMPLELANG_EXPORT_OUTDIR</c> is set.</item>
    /// <item>Else <c>{CurrentDirectory}/out/vm-results</c>.</item>
    /// </list>
    /// The absolute path is written to the top of <c>Result.txt</c> and printed once at VM startup.
    /// </summary>
    public static class VmRunResultSink
    {
        static readonly object Gate = new object();
        static StreamWriter? s_Writer;
        static string? s_ResolvedDirectory;

        public static string? ResultDirectory => s_ResolvedDirectory;
        public static string? ResultFilePath { get; private set; }

        public static void Initialize()
        {
            lock (Gate)
            {
                CloseWriterUnlocked();
                s_ResolvedDirectory = ResolveResultDirectory();
                Directory.CreateDirectory(s_ResolvedDirectory);
                var path = Path.Combine(s_ResolvedDirectory, "Result.txt");
                ResultFilePath = Path.GetFullPath(path);
                s_Writer = new StreamWriter(ResultFilePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
                {
                    AutoFlush = true,
                };
                s_Writer.WriteLine("# SimpleLanguage VM — mirrored console output");
                s_Writer.WriteLine("# Written: " + DateTime.Now.ToString("o"));
                s_Writer.WriteLine("# Result file (absolute): " + ResultFilePath);
                s_Writer.WriteLine("# Directory env: SIMPLELANG_VM_RESULT_DIR → optional override; else SIMPLELANG_EXPORT_OUTDIR/vm-results; else ./out/vm-results");
                s_Writer.WriteLine();
            }
        }

        static string ResolveResultDirectory()
        {
            var env = Environment.GetEnvironmentVariable("SIMPLELANG_VM_RESULT_DIR");
            if (!string.IsNullOrWhiteSpace(env))
                return Path.GetFullPath(env.Trim());

            var export = Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
            if (!string.IsNullOrWhiteSpace(export))
                return Path.GetFullPath(Path.Combine(export.Trim(), "vm-results"));

            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "out", "vm-results"));
        }

        /// <summary>Append the same text as <see cref="Console.Write"/> / WriteLine from SL print/println.</summary>
        public static void MirrorConsole(string? text, bool newLine)
        {
            lock (Gate)
            {
                if (s_Writer == null) return;
                if (text != null && text.Length > 0)
                    s_Writer.Write(text);
                if (newLine)
                    s_Writer.WriteLine();
            }
        }

        static void CloseWriterUnlocked()
        {
            if (s_Writer != null)
            {
                try { s_Writer.Dispose(); } catch { /* ignore */ }
                s_Writer = null;
            }
        }

        public static void Shutdown()
        {
            lock (Gate)
            {
                CloseWriterUnlocked();
            }
        }
    }
}
