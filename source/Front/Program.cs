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
            CommandInputArgs inputArgs = new CommandInputArgs(args);
            if (CommandExecutor.Execute(inputArgs))
            {
                return;
            }
        }
    }
}