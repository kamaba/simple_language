using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using SimpleLanguage.Project;
using SimpleLanguage.Compile;
using System;
using System.IO;
using SimpleLanguage.IR;
using SimpleLanguage.Export.AOT;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using SimpleLanguage.Export;

namespace SimpleLanguage
{
//    public class C1
//    {
//#pragma warning disable CS0414 // 字段"C1.CV"已被赋值，但从未使用过它的值
//        int CV = 1;
//#pragma warning restore CS0414 // 字段"C1.CV"已被赋值，但从未使用过它的值
//        const int j20 = 11 + 20;
//        int CV2 = C2.CV_1;
//        public C1()
//        {

//        }
//    }
//    public class C2
//    {
//        public static object a = 20.ToString() + C3.m / 30 * 100;
//        public static int CV_1 = 10 + CV2;
//        public static int CV2 = CV_1 + CV3;
//        public static int CV33 = CV3;
//        public static int CV4 = GetInt();
//        public static int CV3 = 20;
//        public static object abc = "aaa" + CV3.ToString() + 1.2;
//        static int CV10 = C3.m / 11 + 15;

//        public static int GetInt()
//        {
//            return CV3 + 20;
//        }
//    }
//    public class C3
//    {
//        public static int m = 20;
//    }
//    public class L<T>
//    {
//        public T m;
//        public T laa( T t )
//        {
//            T mw = t;
//            if( typeof( T ) == typeof( Int32) )
//            {
                
//            }
//            return mw;
//        }
//    }
    class Program
    {
        static void Main(string[] args)
        {
            // Test ReadString method
            //TestReadString();
            
            // Original code
            CommandInputArgs inputArgs = new CommandInputArgs(args);
            ProjectManager.Run("../../../source/Lib/Core/Core.sp", inputArgs );

            // after compilation, try exporting IR methods to LLVM IR using the AOT exporter
            try
            {
                //IRManager.instance.TranslateIR();
                var methodList = new System.Collections.Generic.List<IRMethod>(IRManager.instance.IRMethodDict.Values);
                string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Export", "AOT", "out");
                //ExportAot.Export(methodList.ToArray(), outDir);

                // Export SLVM module to Export/SLVMCode
                var cfg = ProjectManager.config.Export;
                var moduleName = string.IsNullOrEmpty(cfg.ModuleName) ? "SimpleLanguageMain" : cfg.ModuleName;
                /*
                var slvmModule = SimpleLanguage.Export.SLVM.SLVMSerializer.FromIRMethods(methodList.ToArray(), moduleName);
                string slvmDir = Path.Combine(Directory.GetCurrentDirectory(), cfg.OutputDir ?? "Export/SLVMCode");
                if (!Directory.Exists(slvmDir)) Directory.CreateDirectory(slvmDir);
                string slvmPath = Path.Combine(slvmDir, moduleName + ".slvm");
                SimpleLanguage.Export.SLVM.SLVMSerializer.WriteModule(slvmModule, slvmPath, cfg);
                Console.WriteLine($"Exported {methodList.Count} methods to SLVM file: {slvmPath}");
                try
                {
                    // Direct IR execution path: translate IR and run NumberTest.fun if present
                    try
                    {
                        // ensure runtime root
                        if (SimpleLanguage.VM.Runtime.InnerCLRRuntimeVM.clrRuntimeStack.Count == 0)
                        {
                            var root = new SimpleLanguage.VM.Runtime.RuntimeVM(new List<SimpleLanguage.IR.IRData>());
                            root.id = "__ir_root__";
                            SimpleLanguage.VM.Runtime.InnerCLRRuntimeVM.PushCLRRuntime(root);
                        }
                        var directMethod = IRManager.instance.IRMethodDict.Values.FirstOrDefault(m => m.onlyFunctionName == "fun" && m.id.StartsWith("NumberTest"));
                        if (directMethod != null)
                        {
                            Console.WriteLine($"Running IR method directly: {directMethod.id}");
                            SimpleLanguage.VM.Runtime.InnerCLRRuntimeVM.RunIRMethod(new List<RuntimeType>(), directMethod);
                            // pop temporary root
                            if (SimpleLanguage.VM.Runtime.InnerCLRRuntimeVM.clrRuntimeStack.Count > 0)
                            {
                                SimpleLanguage.VM.Runtime.InnerCLRRuntimeVM.PopCLRRuntime();
                            }
                        }
                    }
                    catch (Exception exDirect)
                    {
                        Console.WriteLine("Direct IR run failed: " + exDirect.ToString());
                    }

                }
                catch (Exception) { }
                try
                {
                    // load module into runtime and run NumberTest.fun if present
                    var mod = SimpleLanguage.Export.SLVM.SLVMSerializer.ReadModule(slvmPath);
                    if (mod != null)
                    {
                        SimpleLanguage.VM.Runtime.InnerCLRRuntimeVM.LoadSLVMModule(slvmPath);
                        var target = mod.methods.Find(mm => mm.onlyFunctionName == "fun" && (mm.id?.StartsWith("NumberTest") ?? false));
                        if (target != null)
                        {
                            Console.WriteLine($"Running SLVM method: {target.id}");
                            SimpleLanguage.VM.Runtime.InnerCLRRuntimeVM.RunSLVMMethodFile(slvmPath, target.id);
                        }
                    }
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("SLVM run failed: " + ex2.ToString());
                }
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine("AOT export failed: " + ex.ToString());
            }
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        
        static void TestReadString()
        {
            Console.WriteLine("Testing LexerParse.ReadString method...");
            
            string testString = "Name:$name Score:$score a+b=${(a+b).toString()}";
            string testCode = "var str = \"" + testString + "\";";
            
            Console.WriteLine("Test code: " + testCode);
            Console.WriteLine();
            
            // Create lexer parse instance
            LexerParse lexer = new LexerParse("test.sl", testCode.ToCharArray() );
            
            // Parse to token list
            lexer.ParseToTokenList();
            
            // Print tokens
            Console.WriteLine("Generated tokens:");
            List<Token> tokens = lexer.listTokens;
            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                Console.WriteLine($"Token {i}: Type={token.type}, Lexeme={token.lexeme}, Line={token.sourceBeginLine}, Char={token.sourceBeginChar}");
                
                /*
                // Print children tokens if any
                if (token.childrenTokensList != null && token.childrenTokensList.Count > 0)
                {
                    Console.WriteLine("  Children tokens:");
                    foreach (Token childToken in token.childrenTokensList)
                    {
                        Console.WriteLine($"    Child: Type={childToken.type}, Lexeme={childToken.lexeme}");
                        
                        // Print grandchildren if any (for expressions)
                        if (childToken.childrenTokensList != null && childToken.childrenTokensList.Count > 0)
                        {
                            Console.WriteLine("      Grandchildren:");
                            foreach (Token grandChildToken in childToken.childrenTokensList)
                            {
                                Console.WriteLine($"        Grandchild: Type={grandChildToken.type}, Lexeme={grandChildToken.lexeme}");
                            }
                        }
                    }
                }
                */
            }
            
            Console.WriteLine();
            Console.WriteLine("Test ReadString completed.");
            Console.WriteLine();
        }
    }
}