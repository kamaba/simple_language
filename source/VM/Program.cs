// See https://aka.ms/new-console-template for more information


using SimpleLanguage.Logging;
using SimpleLanguage.VM;

Console.WriteLine("---------------------------SimpleLanguage VM---------------------------");

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
            Log.AddVM(LID.Unknown, $"Unsupported json slir input for runtime parse: {pkgPath}");
            return;
        }

        var graph = SLIRJsonModuleLoader.ReadPackagesInExecutionOrder(pkgPath);
        var parseResult = SLIRModuleParse.Parse(graph, args);
        if (parseResult == null)
        {
            Log.AddVM(LID.Unknown, "No valid module package loaded.");
            return;
        }
        SLIRModuleParse.EntryPoint( parseResult );

        var currentPkg = parseResult.currentPackage;
        var asmList = parseResult.assemblyList;
        var slAsm = parseResult.assembly;

        var moduleCount = asmList.Sum(a => a.moduleList.Count);
        var nsCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Count));
        var slTypeCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Count)));
        var slMethodCount = asmList.Sum(a => a.moduleList.Sum(m => m.namespaceList.Sum(n => n.typeList.Sum(t => t.methodList.Count))));

        Log.AddVM( LID.Unknown, $"SLAssembly: {slAsm?.id}");
        Log.AddVM(LID.Unknown, $"Modules: {moduleCount}, Namespaces: {nsCount}, Types: {slTypeCount}, Methods: {slMethodCount}");

        var sampleMethod = slAsm?.moduleList
            .SelectMany(m => m.namespaceList)
            .SelectMany(n => n.typeList)
            .SelectMany(t => t.methodList)
            .FirstOrDefault(m => m.instructionList != null && m.instructionList.Count > 0);

        if (sampleMethod != null)
        {
            Log.AddVM(LID.Unknown, $"IR Sample (SLIR package instruction): {sampleMethod.id}");
            foreach (var ins in sampleMethod.instructionList.Take(40))
            {
                Log.AddVM(LID.Unknown, $"{ins.id} {ins.opCode} payloadLen={ins.Payload?.Length ?? 0}");
            }
        }

        Log.AddVM(LID.Unknown, $"GlobalStaticVariableList: {parseResult.globalVariableCount}, GlobalInitInstructions: {parseResult.globalInitInstructionCount}");


        return;
    }

    Log.AddVM(LID.Unknown, "No module package found. Pass a module package JSON or export one to out/export/module.package.json.");
}
catch (Exception e)
{
    Log.AddVM(LID.Unknown, e.ToString());
}
