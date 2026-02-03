// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

            // Register VM builtins into the front-side registry so Front can discover them without referencing VM.
            try
            {
                SimpleLanguage.VM.Runtime.LocalRuntimeVM.Instance.RegisterBuiltinsAndBridge();
            }
            catch { }
