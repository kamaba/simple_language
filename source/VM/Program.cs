// See https://aka.ms/new-console-template for more information
using System;
using System.Linq;
using SimpleLanguage.VM;
using SimpleLanguage.VM.Lib;
using SimpleLanguage.VM.LanguageRuntime;

Console.WriteLine("SimpleLanguage VM");

try
{
    CallMethodJsonExporter.Export("../../../../Front/bin/Debug/net8.0/ImportCSharpLang.json");

    // If first arg is a JSON module package, load it as SimpleLanguage Assembly/Module model.
    if (args.Length > 0 && args[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        var pkg = SLModulePackageLoader.LoadFromJson(args[0]);
        var slAsm = SLModulePackageLoader.BuildRuntimeModel(pkg);

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
        return;
    }

    var asmMgr = new RuntimeAssemblyManager();
    var ra = asmMgr.Load(typeof(CallMethodJsonExporter).Assembly);

    var modMgr = new RuntimeModuleManager();
    var rm = modMgr.LoadFromAssembly(ra);
    modMgr.BuildMeta(rm);

    var typeCount = rm.namespaceList.Sum(ns => ns.typeList.Count);
    var methodCount = rm.namespaceList.Sum(ns => ns.typeList.Sum(t => t.methodList.Count));
    var ilCount = rm.namespaceList.Sum(ns => ns.typeList.Sum(t => t.methodList.Count(m => m.ilBytes != null && m.ilBytes.Length > 0)));

    Console.WriteLine($"Assembly: {ra.id}");
    Console.WriteLine($"Module: {rm.id}");
    Console.WriteLine($"Namespaces: {rm.namespaceList.Count}, Types: {typeCount}, Methods: {methodCount}, MethodsWithIL: {ilCount}");
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
}
