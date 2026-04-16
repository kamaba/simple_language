using System;
using System.Collections.Generic;
using System.IO;
using SimpleLanguage.Project;


namespace SimpleLanguage
{
    public static class Common
    {
        public static bool ShouldExportDebugText(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return true;

            var cfg = ProjectManager.config?.Export?.DebugText;
            if (cfg == null) return true;

            return fileName switch
            {
                "Code.txt" => cfg.Code,
                "Token.txt" => cfg.Token,
                "Node.txt" => cfg.Node,
                "File.txt" => cfg.File,
                "Meta.txt" => cfg.Meta,
                "IR.txt" => cfg.IR,
                _ => true,
            };
        }

        public static string GetDebugCodeRootDir()
        {
            var envRoot = Environment.GetEnvironmentVariable(ProjectOutputEnvironment.DebugCodeRootEnv);
            if (!string.IsNullOrWhiteSpace(envRoot))
            {
                var full = Path.GetFullPath(envRoot.Trim());
                if (!Directory.Exists(full))
                    Directory.CreateDirectory(full);
                return full;
            }

            // Same tree as LoadProject: {export.outputDir}/{moduleName}/DebugCode/ when export is configured.
            var spPath = ProjectManager.projectPath;
            if (!string.IsNullOrEmpty(spPath) &&
                spPath.EndsWith(".sp", StringComparison.OrdinalIgnoreCase))
            {
                var projectDir = Path.GetDirectoryName(spPath) ?? "";
                var stem = Path.GetFileNameWithoutExtension(spPath);
                var exportMod = ProjectOutputEnvironment.ResolveExportDirectoryFromConfig(ProjectManager.config, projectDir, stem);
                if (exportMod != null)
                {
                    var dc = Path.Combine(exportMod, ProjectOutputEnvironment.DebugCodeDirectoryName);
                    if (!Directory.Exists(dc))
                        Directory.CreateDirectory(dc);
                    return dc;
                }
            }

            var configured = ProjectManager.config?.Export?.DebugText?.OutputDir;
            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = "DebugCode";
            }

            if (Path.IsPathRooted(configured))
            {
                if (!Directory.Exists(configured))
                {
                    Directory.CreateDirectory(configured);
                }
                return configured;
            }

            var baseDir = !string.IsNullOrWhiteSpace(ProjectManager.projectPath)
                ? ProjectManager.projectPath
                : Directory.GetCurrentDirectory();
            var rootDir = Path.Combine(baseDir, configured);
            if (!Directory.Exists(rootDir))
            {
                Directory.CreateDirectory(rootDir);
            }
            return rootDir;
        }

        public static string SetDebugCode(string path)
        {
            return GetDebugCodeDir(path);
        }

        public static string GetDebugCodeDir(string path)
        {
            var rootDir = GetDebugCodeRootDir();

            var relativePath = path ?? string.Empty;
            relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

            var noExtPath = Path.ChangeExtension(relativePath, null) ?? relativePath;
            if (noExtPath.EndsWith("."))
            {
                noExtPath = noExtPath.Substring(0, noExtPath.Length - 1);
            }

            string outDir = Path.Combine(rootDir, noExtPath);

            if( !Directory.Exists( outDir ) )
            {
                Directory.CreateDirectory(outDir);
            }

            return outDir;
        }

        public static string GetDebugCodeFilePath(string path, string fileName)
        {
            var outDir = GetDebugCodeDir(path);
            return Path.Combine(outDir, fileName);
        }
    }
}