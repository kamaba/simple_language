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
        public static bool Execute(CommandInputArgs inputArgs)
        {
            if (inputArgs == null)
            {
                return false;
            }
            LogManager.Initialize("");

            switch (inputArgs.commandType)
            {
                case CommandInputArgs.ECommandType.NewProject:
                    return ExecuteNewProject(inputArgs);
                case CommandInputArgs.ECommandType.NewClassFile:
                    return ExecuteNewClassFile(inputArgs);
                case CommandInputArgs.ECommandType.Compile:
                    return ExecuteCompile(inputArgs);
                default:
                    return false;
            }
        }

        static bool ExecuteNewProject(CommandInputArgs inputArgs)
        {
            if (string.IsNullOrWhiteSpace(inputArgs.newProjectName))
            {
                Console.WriteLine("Usage: sl new project -p [path] [name]");
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
            var mainSlPath = Path.Combine(projectDir, "Main.sl");

            if (!File.Exists(spPath))
            {
                File.WriteAllText(spPath,
                    "Project\n{\n    _main_()\n    {\n    }\n\n    _test_()\n    {\n    }\n\n    CompileBefore()\n    {\n    }\n\n    CompileAfter()\n    {\n    }\n}\n",
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
                    "    \"root\": \".\",\n" +
                    "    \"entryFile\": \"Main.sl\"\n" +
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

            if (!File.Exists(mainSlPath))
            {
                File.WriteAllText(mainSlPath,
                    "Main\n{\n    fun()\n    {\n    }\n}\n",
                    new UTF8Encoding(true));
            }

            ProjectClass.ExportProjectGuideMarkdown(spPath, jsoncPath);

            return true;
        }

        static bool ExecuteNewClassFile(CommandInputArgs inputArgs)
        {
            if (string.IsNullOrWhiteSpace(inputArgs.newClassFileName))
            {
                Console.WriteLine("Usage: sl new classfile [filename]");
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
                return true;
            }

            var projectName = Path.GetFileNameWithoutExtension(spPath);
            var jsoncPath = Path.Combine(Path.GetDirectoryName(spPath) ?? cwd, projectName + ".jsonc");
            if (!File.Exists(jsoncPath))
            {
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

            return true;
        }

        static bool ExecuteCompile(CommandInputArgs inputArgs)
        {
            var spPath = ResolveProjectSp(inputArgs.projectSpPath);
            if (string.IsNullOrWhiteSpace(spPath) || !File.Exists(spPath))
            {
                Log.AddProjectLog( LID.Unknown, "Project .sp file not found.", spPath );
                return true;
            }

            ProjectManager.Run(spPath, inputArgs);
            if (inputArgs.exportIR)
            {
                ExportLangManager.Export(ExportKind.SLIR);
            }

            return true;
        }

        static string ResolveProjectSp(string explicitSpPath)
        {
            if (!string.IsNullOrWhiteSpace(explicitSpPath))
            {
                // already normalized by CommandInputArgs (-p parsing) to <dir>/<name>.sp when possible
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
    }
}
