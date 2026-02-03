// See https://aka.ms/new-console-template for more information
using SimpleLanguage.VM.Lib;
using SimpleLanguage.VM.Runtime;

Console.WriteLine("Hello, World!");

// Register VM builtins into the front-side registry so Front can discover them without referencing VM.
try
{
    CallMethodJsonExporter.Export("../../../../Front/bin/Debug/net8.0/ImportCSharpLang.json");

    //SimpleLanguage.VM.Runtime.LocalRuntimeVM.Instance.RegisterBuiltinsAndBridge();

    //CLRVM.Init();
}
catch( Exception e ) {

    Console.WriteLine("" + e.Message);
}
