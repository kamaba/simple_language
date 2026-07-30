using SimpleLanguage.ExportLanguage;
using SimpleLanguage.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleLanguage.Project
{
    public static class CommandExecutor
    {
        public const string VersionString = "1.0.0";

        public static bool Execute(CommandInputArgs inputArgs)
        {
            if (inputArgs == null)
            {
                return false;
            }
            LogManager.Initialize("");

            if (!inputArgs.noBanner)
            {
                PrintBanner();
            }

            switch (inputArgs.commandType)
            {
                case CommandInputArgs.ECommandType.NewProject:
                    return ExecuteNewProject(inputArgs);
                case CommandInputArgs.ECommandType.NewClassFile:
                    return ExecuteNewClassFile(inputArgs);
                case CommandInputArgs.ECommandType.Compile:
                    return ExecuteCompile(inputArgs);
                case CommandInputArgs.ECommandType.Run:
                    return ExecuteRun(inputArgs);
                case CommandInputArgs.ECommandType.Export:
                    return ExecuteExport(inputArgs);
                case CommandInputArgs.ECommandType.Clean:
                    return ExecuteClean(inputArgs);
                case CommandInputArgs.ECommandType.Version:
                    return ExecuteVersion();
                case CommandInputArgs.ECommandType.Help:
                    return ExecuteHelp();
                default:
                    return ExecuteHelp();
            }
        }

        #region Banner / Version / Help

        static void PrintBanner()
        {
            Console.WriteLine("SimpleLanguage Frontend Compiler v" + VersionString);
            Console.WriteLine();
        }

        static bool ExecuteVersion()
        {
            Console.WriteLine("SimpleLanguage Frontend Compiler");
            Console.WriteLine("  Version:    " + VersionString);
            Console.WriteLine("  Runtime:    " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            Console.WriteLine("  OS:         " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            return true;
        }

        static bool ExecuteHelp()
        {
            Console.WriteLine(@"
Usage: sl <command> [options]

Commands:
  compile, c, build, b   Compile a project
  run, r                 Compile and run (VM not included in Frontend)
  export, e              Export IR only (no full compile pipeline)
  clean                  Clean build output directory
  new project [name]     Create a new project
  new class [name]       Create a new .sl class file
  version, v             Show version information
  help, h                Show this help message

Options:
  -p, --project <path>   Project path (directory or .sp file)
  -o, --output <dir>      Output directory
  -e ir, --export ir      Export IR during compile
  -t, --test              Run in test mode
  --release              Build in release mode
  --debug                Build in debug mode (default)
  --verbose              Verbose output
  --no-banner            Suppress banner
  --token                Print tokens during compile

Examples:
  sl c -p ../MyProject/MyProject
  sl compile -p ./Core/Core -e ir
  sl run -p ./MyProject --test
  sl new project -p ./projects MyApp
  sl new class MyClass
  sl clean -p ./Core/Core
  sl version
  sl help
");
            return true;
        }

        #endregion

        #region Compile

        static bool ExecuteCompile(CommandInputArgs inputArgs)
        {
            var spPath = ResolveProjectSp(inputArgs.projectSpPath);
            if (string.IsNullOrWhiteSpace(spPath) || !File.Exists(spPath))
            {
                Log.AddProjectLog(LID.ProjectSPFilePathNotFound, "", spPath);
                return true;
            }

            Console.WriteLine($"Compiling: {spPath}");
            ProjectManager.Run(spPath, inputArgs);
            if (inputArgs.exportIR)
            {
                ExportLangManager.Export(ExportKind.SLIR);
            }

            Console.WriteLine("Compile completed.");
            return true;
        }

        static bool ExecuteRun(CommandInputArgs inputArgs)
        {
            var spPath = ResolveProjectSp(inputArgs.projectSpPath);
            if (string.IsNullOrWhiteSpace(spPath) || !File.Exists(spPath))
            {
                Log.AddProjectLog(LID.ProjectSPFilePathNotFound, "", spPath);
                return true;
            }

            Console.WriteLine($"Compiling: {spPath}");
            ProjectManager.Run(spPath, inputArgs);
            if (inputArgs.exportIR)
            {
                ExportLangManager.Export(ExportKind.SLIR);
            }

            Console.WriteLine("Compile completed. (VM run is handled by the host application)");
            return true;
        }

        static bool ExecuteExport(CommandInputArgs inputArgs)
        {
            var spPath = ResolveProjectSp(inputArgs.projectSpPath);
            if (string.IsNullOrWhiteSpace(spPath) || !File.Exists(spPath))
            {
                Log.AddProjectLog(LID.ProjectSPFilePathNotFound, "", spPath);
                return true;
            }

            Console.WriteLine($"Exporting IR: {spPath}");
            ProjectManager.Run(spPath, inputArgs);
            ExportLangManager.Export(ExportKind.SLIR);
            Console.WriteLine("Export completed.");
            return true;
        }

        #endregion

        #region Clean

        static bool ExecuteClean(CommandInputArgs inputArgs)
        {
            var spPath = ResolveProjectSp(inputArgs.projectSpPath);
            if (string.IsNullOrWhiteSpace(spPath))
            {
                Console.WriteLine("No project specified. Use: sl clean -p <projectPath>");
                return true;
            }

            // Determine export directory
            string exportDir = null;
            var envExportDir = Environment.GetEnvironmentVariable(ProjectOutputEnvironment.ExportOutDirEnv);
            if (!string.IsNullOrWhiteSpace(envExportDir))
            {
                exportDir = envExportDir;
            }
            else
            {
                var repoRoot = FindRepoRoot(spPath);
                if (repoRoot != null)
                {
                    exportDir = Path.Combine(repoRoot, "out", "export");
                }
            }

            if (string.IsNullOrWhiteSpace(exportDir) || !Directory.Exists(exportDir))
            {
                Console.WriteLine("Nothing to clean (export directory not found).");
                return true;
            }

            var projectName = !string.IsNullOrWhiteSpace(inputArgs.compileProjectName)
                ? inputArgs.compileProjectName
                : Path.GetFileNameWithoutExtension(spPath);

            var projectExportDir = Path.Combine(exportDir, projectName);
            if (Directory.Exists(projectExportDir))
            {
                Console.WriteLine($"Cleaning: {projectExportDir}");
                Directory.Delete(projectExportDir, recursive: true);
                Console.WriteLine("Clean completed.");
            }
            else
            {
                Console.WriteLine($"No build output found at: {projectExportDir}");
            }
            return true;
        }

        static string FindRepoRoot(string startPath)
        {
            var dir = new DirectoryInfo(Path.GetDirectoryName(startPath) ?? startPath);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "SimpleLanguage.sln")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        #endregion

        #region New Project / New Class

        static bool ExecuteNewProject(CommandInputArgs inputArgs)
        {
            if (string.IsNullOrWhiteSpace(inputArgs.newProjectName))
            {
                Console.WriteLine("Usage: sl new project [-p <path>] <name>");
                return true;
            }

            var root = Path.GetFullPath(string.IsNullOrWhiteSpace(inputArgs.newProjectBasePath)
                ? Directory.GetCurrentDirectory()
                : inputArgs.newProjectBasePath);
            Directory.CreateDirectory(root);

            var projectName = inputArgs.newProjectName.Trim();
            var projectDir = Path.Combine(root, projectName);
            Directory.CreateDirectory(projectDir);

            var spPath = Path.Combine(projectDir, projectName + ".sp");
            var jsoncPath = Path.Combine(projectDir, projectName + ".jsonc");

            if (!File.Exists(spPath))
            {
                File.WriteAllText(spPath,
                    "Project\n{\n    _main_()\n    {\n    }\n\n    _test_()\n    {\n    }\n\n    _compile_before_()\n    {\n    }\n\n    _compile_after_()\n    {\n    }\n}\n",
                    new UTF8Encoding(true));
            }

            if (!File.Exists(jsoncPath))
            {
                File.WriteAllText(jsoncPath,
                    "{\n" +
                    "  // SimpleLanguage project config (.jsonc)\n" +
                    "  \"project\": {\n" +
                    $"    \"name\": \"{projectName}\",\n" +
                    "    \"desc\": \"SimpleLanguage project\",\n" +
                    "    \"mainVersion\": 1,\n" +
                    "    \"subVersion\": 0,\n" +
                    "    \"buildVersion\": 0\n" +
                    "  },\n" +
                    "  \"source\": {\n" +
                    "    \"root\": \".\"\n" +
                    "  },\n" +
                    "  \"compile\": {\n" +
                    "    \"optimize\": false,\n" +
                    "    \"target\": \"x64\",\n" +
                    "    \"debug\": true\n" +
                    "  },\n" +
                    "  \"compileFiles\": {\n" +
                    "    \"files\": [\n" +
                    "      {\n" +
                    "        \"path\": \"Main.sl\",\n" +
                    "        \"group\": \"default\",\n" +
                    "        \"tag\": \"source\",\n" +
                    "        \"ignore\": false,\n" +
                    "        \"priority\": 0\n" +
                    "      }\n" +
                    "    ]\n" +
                    "  },\n" +
                    "  \"compileFilter\": {\n" +
                    "    \"isAllGroup\": true,\n" +
                    "    \"isAllTag\": true\n" +
                    "  }\n" +
                    "}\n",
                    new UTF8Encoding(true));
            }

            ProjectClass.ExportProjectGuideMarkdown(spPath, jsoncPath);
            Console.WriteLine($"Project '{projectName}' created at: {projectDir}");
            return true;
        }

        static bool ExecuteNewClassFile(CommandInputArgs inputArgs)
        {
            if (string.IsNullOrWhiteSpace(inputArgs.newClassFileName))
            {
                Console.WriteLine("Usage: sl new class <filename>");
                return true;
            }

            var cwd = Directory.GetCurrentDirectory();
            var inputName = inputArgs.newClassFileName.Trim();
            var fileName = inputName.EndsWith(".sl", StringComparison.OrdinalIgnoreCase) ? inputName : inputName + ".sl";
            var className = Path.GetFileNameWithoutExtension(fileName);
            var classPath = Path.Combine(cwd, fileName);
            var classDir = Path.GetDirectoryName(classPath);

            if (!string.IsNullOrWhiteSpace(classDir))
            {
                Directory.CreateDirectory(classDir);
            }

            if (!File.Exists(classPath))
            {
                File.WriteAllText(classPath, className + "\n{\n}\n", new UTF8Encoding(true));
            }

            var spPath = FindCurrentProjectSp(cwd);
            if (string.IsNullOrWhiteSpace(spPath))
            {
                Console.WriteLine($"Class file created: {classPath}");
                return true;
            }

            var projectName = Path.GetFileNameWithoutExtension(spPath);
            var jsoncPath = Path.Combine(Path.GetDirectoryName(spPath) ?? cwd, projectName + ".jsonc");
            if (!File.Exists(jsoncPath))
            {
                Console.WriteLine($"Class file created: {classPath}");
                return true;
            }

            string relPath = fileName.Replace("\\", "/");
            var jsoncText = File.ReadAllText(jsoncPath);
            if (!jsoncText.Contains($"\"path\": \"{relPath}\"", StringComparison.OrdinalIgnoreCase))
            {
                var insert = "\n      {\n" +
                             $"        \"path\": \"{relPath}\",\n" +
                             "        \"group\": \"default\",\n" +
                             "        \"tag\": \"source\",\n" +
                             "        \"ignore\": false,\n" +
                             "        \"priority\": 0\n" +
                             "      }";

                var marker = "\"files\": [";
                var markerIndex = jsoncText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex >= 0)
                {
                    var arrayStart = jsoncText.IndexOf('[', markerIndex);
                    var arrayEnd = FindArrayEnd(jsoncText, arrayStart);
                    if (arrayStart > 0 && arrayEnd > arrayStart)
                    {
                        var content = jsoncText.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                        var hasItems = content.IndexOf('{') >= 0;
                        var payload = hasItems ? "," + insert : insert;
                        jsoncText = jsoncText.Insert(arrayEnd, payload + "\n    ");
                        File.WriteAllText(jsoncPath, jsoncText, new UTF8Encoding(true));
                    }
                }
            }

            Console.WriteLine($"Class file created: {classPath}");
            return true;
        }

        #endregion

        #region Helpers

        static string ResolveProjectSp(string explicitSpPath)
        {
            if (!string.IsNullOrWhiteSpace(explicitSpPath))
            {
                if (File.Exists(explicitSpPath))
                {
                    return Path.GetFullPath(explicitSpPath);
                }
            }

            var cwdSp = FindCurrentProjectSp(Directory.GetCurrentDirectory());
            if (!string.IsNullOrWhiteSpace(cwdSp))
            {
                return cwdSp;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Project.sp"));
        }

        static string FindCurrentProjectSp(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                return null;
            }

            var files = Directory.GetFiles(dir, "*.sp", SearchOption.TopDirectoryOnly)
                .Where(p => !string.Equals(Path.GetFileNameWithoutExtension(p), "Project", StringComparison.OrdinalIgnoreCase)
                            || Directory.GetFiles(dir, "*.sp").Length == 1)
                .ToList();
            if (files.Count == 0)
            {
                return null;
            }

            return Path.GetFullPath(files[0]);
        }

        static int FindArrayEnd(string text, int startIndex)
        {
            if (startIndex < 0 || startIndex >= text.Length || text[startIndex] != '[')
            {
                return -1;
            }

            int depth = 0;
            for (int i = startIndex; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '[') depth++;
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        #endregion
    }
}
