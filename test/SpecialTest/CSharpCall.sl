import Application.Core;


class CSharpCall
{
    public static void fun()
    {
        @csharp()
        {
            using System;
            Console.WriteLine("Hello from C#!");
        }
    }
}