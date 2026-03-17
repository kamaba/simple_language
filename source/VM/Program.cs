// See https://aka.ms/new-console-template for more information
using System;
using System.Linq;
using System.Collections.Generic;
using SimpleLanguage.VM;
using SimpleLanguage.VM.Lib;
using SimpleLanguage.VM.LanguageRuntime;

Console.WriteLine("SimpleLanguage VM");

try
{
    //CallMethodJsonExporter.Export("../../../../Front/bin/Debug/net8.0/ImportCSharpLang.json");

    // If first arg is a JSON module package, load it. Otherwise try default exported path.
    string? pkgPath = null;
    if (args.Length > 0 && args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        pkgPath = args[0];
    }
    else
    {
        var outDir = "E:\\project\\lang\\simple_language\\source\\Front\\bin\\Debug\\net8.0\\out\\export";// Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
        if (string.IsNullOrWhiteSpace(outDir))
            outDir = System.IO.Path.Combine(Environment.CurrentDirectory, "out", "export");
        var defaultPath = System.IO.Path.Combine(outDir, "module.package.json");
        if (System.IO.File.Exists(defaultPath))
            pkgPath = defaultPath;
    }

    if (!string.IsNullOrEmpty(pkgPath))
    {
        var packageList = LoadPackagesInExecutionOrder(pkgPath);
        if (packageList.Count == 0)
        {
            Console.WriteLine("No valid module package loaded.");
            return;
        }

        var currentPkg = packageList[packageList.Count - 1];

        SLRuntimeModuleRegistry.LoadFromPackages(packageList);
        var asmList = packageList.Select(SLModulePackageLoader.BuildRuntimeModel).ToList();
        var slAsm = asmList[asmList.Count - 1];

        // Load C# bridge metadata (optional)
        var bridgePath = Environment.GetEnvironmentVariable("SIMPLELANG_BRIDGE_JSON");
        if (string.IsNullOrWhiteSpace(bridgePath))
        {
            var dir = System.IO.Path.GetDirectoryName(pkgPath);
            if (!string.IsNullOrEmpty(dir))
            {
                var guess = System.IO.Path.Combine(dir, "ImportCSharpLang.json");
                if (System.IO.File.Exists(guess)) bridgePath = guess;
            }
        }
        if (!string.IsNullOrWhiteSpace(bridgePath) && System.IO.File.Exists(bridgePath))
        {
            SimpleLanguage.VM.Runtime.CSharpBridgeRegistry.LoadFromJson(bridgePath);
        }

        var moduleCount = asmList.Sum(a => a.moduleList.Count);
        var nsCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Count));
        var slTypeCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Count)));
        var slMethodCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Sum(t => t.methodList.Count))));

        Console.WriteLine($"SLAssembly: {slAsm.id}");
        Console.WriteLine($"Modules: {moduleCount}, Namespaces: {nsCount}, Types: {slTypeCount}, Methods: {slMethodCount}");

        var sampleMethod = slAsm.moduleList
            .SelectMany(m => m.namespaceList)
            .SelectMany(n => n.typeList)
            .SelectMany(t => t.methodList)
            .FirstOrDefault(m => m.vmInstructionList.Count > 0);

        if (sampleMethod != null)
        {
            Console.WriteLine($"IR Sample (VM Instruction): {sampleMethod.id}");
            foreach (var ins in sampleMethod.vmInstructionList.Take(40))
            {
                Console.WriteLine($"{ins.id} {ins.opCode} {ins.opValue}");
            }
        }

        // Optional execution: SIMPLELANG_ENTRY_METHOD=<methodId>
        var entryId = Environment.GetEnvironmentVariable("SIMPLELANG_ENTRY_METHOD");
        bool runTest = args.Any(a => string.Equals(a, "-test", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(entryId))
        {
            if (runTest)
            {
                entryId = currentPkg.methodList?.FirstOrDefault(m => string.Equals(m.name, "_test_", StringComparison.OrdinalIgnoreCase))?.id;
            }
            else
            {
                entryId = currentPkg.entryMethodId;
            }
        }

        // 1) run Main/entry of current module
        if (!string.IsNullOrWhiteSpace(entryId))
        {
            var rm = SLRuntimeModuleRegistry.GetMethod(entryId);
            if (rm == null)
            {
                Console.WriteLine($"Runtime method not found: {entryId}");
            }
            else
            {
                var vm = SimpleLanguage.VM.Runtime.CLRVM.CreateCLRRuntime(new List<RuntimeType>(), rm);
                vm.Run(true);
                SimpleLanguage.VM.Runtime.CLRVM.PopCLRRuntime();
            }
        }

        return;
    }

static List<SLModulePackage> LoadPackagesInExecutionOrder(string rootPackagePath)
{
    var result = new List<SLModulePackage>();
    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    void LoadRecursive(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        if (!System.IO.File.Exists(fullPath)) return;
        if (!visited.Add(fullPath)) return;

        var pkg = SLModulePackageLoader.LoadFromJson(fullPath);
        var dir = System.IO.Path.GetDirectoryName(fullPath) ?? string.Empty;

        var refs = pkg.moduleReferences ?? new List<string>();
        for (int i = 0; i < refs.Count; i++)
        {
            var rp = refs[i];
            if (string.IsNullOrWhiteSpace(rp)) continue;
            var refPath = System.IO.Path.IsPathRooted(rp) ? rp : System.IO.Path.Combine(dir, rp);
            LoadRecursive(refPath);
        }

        result.Add(pkg);
    }

    LoadRecursive(rootPackagePath);

    if (result.Count == 1)
    {
        var rootFullPath = System.IO.Path.GetFullPath(rootPackagePath);
        var dir = System.IO.Path.GetDirectoryName(rootFullPath) ?? string.Empty;
        var siblings = System.IO.Directory.Exists(dir)
            ? System.IO.Directory.GetFiles(dir, "*.package.json")
            : Array.Empty<string>();

        Array.Sort(siblings, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < siblings.Length; i++)
        {
            var sp = System.IO.Path.GetFullPath(siblings[i]);
            if (string.Equals(sp, rootFullPath, StringComparison.OrdinalIgnoreCase)) continue;
            if (!visited.Add(sp)) continue;
            result.Insert(result.Count - 1, SLModulePackageLoader.LoadFromJson(sp));
        }
    }

    return result;
}

    Console.WriteLine("No module package found. Pass a module package JSON or export one to out/export/module.package.json.");
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}
