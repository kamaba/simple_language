using SimpleLanguage.Core;
using SimpleLanguage.Project;
using SimpleLanguage.Compile;
using System;
using System.Collections.Generic;

namespace SimpleLanguage
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            if( args.Length == 0 )
            {
                args = new string[6];
                args[0] = "compile";
                args[1] = "-e";
                args[2] = "ir";
                args[3] = "-p";
                args[4] = "F:\\project\\lang\\simple_language\\source\\Front\\Lib\\Core\\Core";
                args[5] = "--no-banner";
            }
#endif

            var inputArgs = new CommandInputArgs(args);
            _ = CommandExecutor.Execute(inputArgs);
        }
    }
}
