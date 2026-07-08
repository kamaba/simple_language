

public class Std.Console
{
    #Read a line from stdin and return as string
    static string input()
    {        
        string str = "";
        BridgeObject bo = new( "string" )
        NativeBridge.Call( BridgeObject.CLR, "System", "Console", "ReadLine", bo, null )
        ret bo.toString()
        #ret CSharp.System.Console.ReadLine();
    }

   #Alias for input
    static string readLine()
    {
        ret CSharp.System.Console.ReadLine();
    }

    #Print without newline
    static void write(string text, param object[] params)
    {
        CSharp.System.Console.Write(text, params);
    }

    #Print without newline (alias)
    static void print(string text, param object[] params)
    {
        CSharp.System.Console.Write(text, params);
    }

    #Print with newline
    static void println(string text, param object[] params)
    {
        CSharp.System.Console.WriteLine(text, params);
    }
}
