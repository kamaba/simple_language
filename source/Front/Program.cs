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
                args = new string[5];
                args[0] = "c";
                args[1] = "-e";
                args[2] = "ir";
                args[3] = "-p";
                args[4] = "E:\\project\\lang\\simple_language\\source\\Front\\Lib\\Core\\Core";
            }
#endif

            CommandInputArgs inputArgs = new CommandInputArgs(args);
            if (CommandExecutor.Execute(inputArgs))
            {
                return;
            }
        }
    }
}