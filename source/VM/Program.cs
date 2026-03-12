// See https://aka.ms/new-console-template for more information
using System;
using System.Linq;
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
        var pkg = SLModulePackageLoader.LoadFromJson(pkgPath);
        SLRuntimeModuleRegistry.LoadFromPackage(pkg);
        var slAsm = SLModulePackageLoader.BuildRuntimeModel(pkg);

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

        var moduleCount = slAsm.moduleList.Count;
        var nsCount = slAsm.moduleList.Sum(m => m.namespaceList.Count);
        var slTypeCount = slAsm.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Count));
        var slMethodCount = slAsm.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Sum(t => t.methodList.Count)));

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
        if (string.IsNullOrWhiteSpace(entryId))
        {
            entryId = pkg.entryMethodId;
        }
        if (!string.IsNullOrWhiteSpace(entryId))
        {
            var entry = slAsm.moduleList
                .SelectMany(m => m.namespaceList)
                .SelectMany(n => n.typeList)
                .SelectMany(t => t.methodList)
                .FirstOrDefault(m => string.Equals(m.id, entryId, StringComparison.Ordinal));

            if (entry != null)
            {
                var rm = SLRuntimeModuleRegistry.GetMethod(entry.id);
                if (rm == null)
                {
                    Console.WriteLine($"Runtime method not found: {entry.id}");
                }
                else
                {
                    var vm = SimpleLanguage.VM.Runtime.CLRVM.CreateCLRRuntime(new System.Collections.Generic.List<RuntimeType>(), rm);
                    vm.Run(true);
                }
            }
            else
            {
                Console.WriteLine($"Entry method not found: {entryId}");
            }
        }

        return;
    }

    Console.WriteLine("No module package found. Pass a module package JSON or export one to out/export/module.package.json.");
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}
