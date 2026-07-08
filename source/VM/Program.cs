// See https://aka.ms/new-console-template for more information


using SimpleLanguage.Logging;
using SimpleLanguage.VM;

Console.WriteLine("---------------------------SimpleLanguage VM---------------------------");

LogManager.Initialize("");
Log.ResetFixedLogFileForNewSession();

VmRunResultSink.Initialize();

try
{
    //CallMethodJsonExporter.Export("../../../../Front/bin/Debug/net8.0/ImportCSharpLang.json");

    // Resolve SLIR input path by loader abstraction (json now, binary later).
    string? pkgPath = SLIRRuntimeLoader.ResolveInputPath(args);

    if (!string.IsNullOrEmpty(pkgPath))
    {
        var loadModel = SLIRRuntimeLoader.LoadInExecutionOrder(pkgPath);
        var parseResult = SLIRModuleParse.Parse(loadModel, args);
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

    Log.AddProjectLog(LID.ShowMessageInfo, "No SLIR input found. Pass a *.module.json/*.package.json (binary loader can be plugged in later).");
}
catch (CompilationAbortException)
{
    // 取消执行由 Log 系统触发并已记录对应日志，这里直接结束。
}
catch (Exception e)
{
    Log.AddProjectLog(LID.ShowMessageError, e.ToString());
}
finally
{
    VmRunResultSink.Shutdown();
}
