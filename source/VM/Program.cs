// See https://aka.ms/new-console-template for more information


using SimpleLanguage.Parse;
using SimpleLanguage.VM;

Console.WriteLine("SimpleLanguage VM");

Environment.SetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR", "E:\\project\\lang\\simple_language\\source\\Front\\bin\\Debug\\net8.0\\out\\export");

try
{
    //CallMethodJsonExporter.Export("../../../../Front/bin/Debug/net8.0/ImportCSharpLang.json");

    // If first arg is a JSON module package, load it. Otherwise try default exported path.
    string? pkgPath = SLIRJsonModuleLoader.ResolveJsonPath(args);

    if (!string.IsNullOrEmpty(pkgPath))
    {
        if (!pkgPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Unsupported json slir input for runtime parse: {pkgPath}");
            return;
        }

        var graph = SLIRJsonModuleLoader.ReadPackagesInExecutionOrder(pkgPath);
        var parseResult = SLIRModuleParse.Parse(graph, args);
        if (parseResult == null)
        {
            Console.WriteLine("No valid module package loaded.");
            return;
        }

        var currentPkg = parseResult.currentPackage;
        var asmList = parseResult.assemblyList;
        var slAsm = parseResult.assembly;

        var moduleCount = asmList.Sum(a => a.moduleList.Count);
        var nsCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Count));
        var slTypeCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Count)));
        var slMethodCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Sum(t => t.methodList.Count))));

        Console.WriteLine($"SLAssembly: {slAsm?.id}");
        Console.WriteLine($"Modules: {moduleCount}, Namespaces: {nsCount}, Types: {slTypeCount}, Methods: {slMethodCount}");

        var sampleMethod = slAsm?.moduleList
            .SelectMany(m => m.namespaceList)
            .SelectMany(n => n.typeList)
            .SelectMany(t => t.methodList)
            .FirstOrDefault(m => m.instructionList != null && m.instructionList.Count > 0);

        if (sampleMethod != null)
        {
            Console.WriteLine($"IR Sample (SLIR package instruction): {sampleMethod.id}");
            foreach (var ins in sampleMethod.instructionList.Take(40))
            {
                Console.WriteLine($"{ins.id} {ins.opCode} payloadLen={ins.Payload?.Length ?? 0}");
            }
        }

        Console.WriteLine($"GlobalStaticVariableList: {parseResult.globalVariableCount}, GlobalInitInstructions: {parseResult.globalInitInstructionCount}");

        var entryId = parseResult.entryMethodId;

        // 2) run Main/entry of current module
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

    Console.WriteLine("No module package found. Pass a module package JSON or export one to out/export/module.package.json.");
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}
