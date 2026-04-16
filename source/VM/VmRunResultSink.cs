using System;
using System.IO;
using System.Text;
using SimpleLanguage.Logging;

namespace SimpleLanguage.VM
{
    /// <summary>
    /// Mirrors SL/VM console output (SystemPrint / SystemPrintln) to a UTF-8 <c>Result.txt</c> for inspection.
    /// <para><b>Path resolution</b> (first match wins):</para>
    /// <list type="number">
    /// <item><c>SIMPLELANG_VM_RESULT_DIR</c> — directory to place <c>Result.txt</c> (created if missing).</item>
    /// <item>Else fixed path <see cref="Log.VmRunResultFilePath"/> (same <c>Logs</c> folder as <see cref="Log.VmLogFilePath"/>, default <c>…/export/&lt;module&gt;/Logs/Result.txt</c>).</item>
    /// </list>
    /// The absolute path is written to the top of <c>Result.txt</c> and printed once at <see cref="Initialize"/>.
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
                ResultFilePath = ResolveResultFilePath();
                s_ResolvedDirectory = Path.GetDirectoryName(ResultFilePath);
                if (!string.IsNullOrEmpty(s_ResolvedDirectory))
                    Directory.CreateDirectory(s_ResolvedDirectory);
                s_Writer = new StreamWriter(ResultFilePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
                {
                    AutoFlush = true,
                };
                s_Writer.WriteLine("# SimpleLanguage VM — mirrored console output");
                s_Writer.WriteLine("# Written: " + DateTime.Now.ToString("o"));
                s_Writer.WriteLine("# Result file (absolute): " + ResultFilePath);
                s_Writer.WriteLine("# Override directory: env SIMPLELANG_VM_RESULT_DIR=<dir>  (file name remains Result.txt)");
                s_Writer.WriteLine();
                Console.WriteLine("[VMResult] OutputPath: " + ResultFilePath);
                Console.WriteLine("[VMResult] Override: set SIMPLELANG_VM_RESULT_DIR to a directory to write Result.txt elsewhere.");
            }
        }

        static string ResolveResultFilePath()
        {
            var env = Environment.GetEnvironmentVariable("SIMPLELANG_VM_RESULT_DIR");
            if (!string.IsNullOrWhiteSpace(env))
                return Path.GetFullPath(Path.Combine(env.Trim(), "Result.txt"));

            return Path.GetFullPath(Log.VmRunResultFilePath);
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
