using System;
using System.IO;

namespace SimpleLanguage.Project
{
    /// <summary>
    /// Under <c>export.outputDir</c> creates <c>{export.moduleName}/</c> containing:
    /// <c>Logs/</c> (Front.txt, VM.txt, Result.txt), <c>DebugCode/</c> (Front stage dumps), and the VM <c>*.module.json</c> export.
    /// <para>Module folder name: <c>export.moduleName</c>, else <c>project.name</c>, else .sp stem.</para>
    /// </summary>
    public static class ProjectOutputEnvironment
    {
        public const string ExportOutDirEnv = "SIMPLELANG_EXPORT_OUTDIR";
        public const string DebugCodeRootEnv = "SIMPLELANG_DEBUGCODE_ROOT";
        public const string LogsDirEnv = "SIMPLELANG_LOGS_DIR";

        public const string LogsDirectoryName = "Logs";
        public const string DebugCodeDirectoryName = "DebugCode";
        public const string FrontLogFileName = "Front.txt";
        public const string VmLogFileName = "VM.txt";
        public const string VmRunResultFileName = "Result.txt";

        /// <summary>Sanitize a single path segment (directory name).</summary>
        public static string SanitizePathSegment(string? name, string fallback)
        {
            var raw = !string.IsNullOrWhiteSpace(name) ? name.Trim() : fallback;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "_module";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = raw.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                    chars[i] = '_';
            }
            return new string(chars);
        }

        /// <summary>Resolve <c>export.moduleName</c> folder segment for the current config.</summary>
        public static string GetModuleFolderName(ProjectConfig? config, string projectNameFromSpFile)
        {
            if (config == null)
                return SanitizePathSegment(null, projectNameFromSpFile);
            var prefer = !string.IsNullOrWhiteSpace(config.Export?.ModuleName)
                ? config.Export.ModuleName
                : config.Project?.Name;
            return SanitizePathSegment(prefer, projectNameFromSpFile);
        }

        /// <summary>
        /// After loading <c>.jsonc</c>, set <see cref="ExportOutDirEnv"/>, <see cref="DebugCodeRootEnv"/>, <see cref="LogsDirEnv"/>.
        /// Clears each variable when the corresponding config path is missing.
        /// </summary>
        public static void ApplyFromConfig(ProjectConfig? config, string projectDir, string projectNameFromSpFile)
        {
            if (config == null)
            {
                Environment.SetEnvironmentVariable(ExportOutDirEnv, null);
                Environment.SetEnvironmentVariable(DebugCodeRootEnv, null);
                Environment.SetEnvironmentVariable(LogsDirEnv, null);
                return;
            }

            var moduleSeg = GetModuleFolderName(config, projectNameFromSpFile);

            if (string.IsNullOrWhiteSpace(config.Export?.OutputDir))
            {
                Environment.SetEnvironmentVariable(ExportOutDirEnv, null);
                Environment.SetEnvironmentVariable(LogsDirEnv, null);
                Environment.SetEnvironmentVariable(DebugCodeRootEnv, null);
            }
            else
            {
                var exportBaseRoot = Path.IsPathRooted(config.Export.OutputDir)
                    ? Path.GetFullPath(config.Export.OutputDir)
                    : Path.GetFullPath(Path.Combine(projectDir, config.Export.OutputDir));
                exportBaseRoot = TrimTrailingDirSeparators(exportBaseRoot);
                var exportMod = Path.Combine(exportBaseRoot, moduleSeg);
                Directory.CreateDirectory(exportMod);
                Environment.SetEnvironmentVariable(ExportOutDirEnv, exportMod);

                var logsDir = Path.Combine(exportMod, LogsDirectoryName);
                Directory.CreateDirectory(logsDir);
                Environment.SetEnvironmentVariable(LogsDirEnv, logsDir);

                var debugCodeDir = Path.Combine(exportMod, DebugCodeDirectoryName);
                Directory.CreateDirectory(debugCodeDir);
                Environment.SetEnvironmentVariable(DebugCodeRootEnv, debugCodeDir);
            }
        }

        /// <summary>Export directory when env is unset but <paramref name="config"/> has <c>export.outputDir</c>.</summary>
        public static string? ResolveExportDirectoryFromConfig(ProjectConfig? config, string projectDir, string projectNameFromSpFile)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.Export?.OutputDir))
                return null;
            var exportBaseRoot = Path.IsPathRooted(config.Export.OutputDir)
                ? Path.GetFullPath(config.Export.OutputDir)
                : Path.GetFullPath(Path.Combine(projectDir, config.Export.OutputDir));
            exportBaseRoot = TrimTrailingDirSeparators(exportBaseRoot);
            var moduleSeg = GetModuleFolderName(config, projectNameFromSpFile);
            return Path.Combine(exportBaseRoot, moduleSeg);
        }

        static string TrimTrailingDirSeparators(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            return p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
