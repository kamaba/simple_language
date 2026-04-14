// See https://aka.ms/new-console-template for more information


using SimpleLanguage.Logging;
using SimpleLanguage.VM;

Console.WriteLine("---------------------------SimpleLanguage VM---------------------------");

LogManager.Initialize("");

VmRunResultSink.Initialize();
if (!string.IsNullOrEmpty(VmRunResultSink.ResultFilePath))
{
    Console.WriteLine("[VM] Result.txt → " + VmRunResultSink.ResultFilePath);
    Console.WriteLine("[VM] Override directory: env SIMPLELANG_VM_RESULT_DIR=<dir>  (default: %SIMPLELANG_EXPORT_OUTDIR%\\vm-results or .\\out\\vm-results)");
}

try
{
    //CallMethodJsonExporter.Export("../../../../Front/bin/Debug/net8.0/ImportCSharpLang.json");

    // If first arg is a JSON module package, load it. Otherwise try default exported path.
    string? pkgPath = SLIRJsonModuleLoader.ResolveJsonPath(args);

    if (!string.IsNullOrEmpty(pkgPath))
    {
        if (!pkgPath.EndsWith(".module.json", StringComparison.OrdinalIgnoreCase)
            && !pkgPath.EndsWith(".package.json", StringComparison.OrdinalIgnoreCase))
        {
            Log.AddProjectLog(LID.NotFoundRuntimeIRFile, pkgPath );
            return;
        }

        var graph = SLIRJsonModuleLoader.ReadPackagesInExecutionOrder(pkgPath);
        var parseResult = SLIRModuleParse.Parse(graph, args);
        if (parseResult == null)
        {
            Log.AddProjectLog(LID.RuntimeIRParseError, pkgPath );
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

        Log.AddProjectLog( LID.ShowMessageInfo, $"SLAssembly: {slAsm?.id}");
        Log.AddProjectLog(LID.ShowMessageInfo, $"Modules: {moduleCount}, Namespaces: {nsCount}, Types: {slTypeCount}, Methods: {slMethodCount}");

        var sampleMethod = slAsm?.moduleList
            .SelectMany(m => m.namespaceList)
            .SelectMany(n => n.typeList)
            .SelectMany(t => t.methodList)
            .FirstOrDefault(m => m.instructionList != null && m.instructionList.Count > 0);

        if (sampleMethod != null)
        {
            Log.AddProjectLog(LID.ShowMessageInfo, $"IR Sample (SLIR package instruction): {sampleMethod.id}");
            foreach (var ins in sampleMethod.instructionList.Take(40))
            {
                Log.AddProjectLog(LID.ShowMessageInfo, $"{ins.id} {ins.opCode} payloadLen={ins.Payload?.Length ?? 0}");
            }
        }

        Log.AddProjectLog(LID.ShowMessageInfo, $"GlobalStaticVariableList: {parseResult.globalVariableCount}, GlobalInitInstructions: {parseResult.globalInitInstructionCount}");


        return;
    }

    Log.AddProjectLog(LID.ShowMessageInfo, "No module package found. Pass a *.module.json path or export one to configured export.outputDir.");
}
catch (Exception e)
{
    Log.AddProjectLog(LID.ShowMessageError, e.ToString());
}
finally
{
    VmRunResultSink.Shutdown();
}
