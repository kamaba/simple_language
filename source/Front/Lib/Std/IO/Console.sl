

public class Console
{
    #Read a line from stdin and return as string
    static string input()
    {     
        #!   
        string str = "";
        BridgeObject bo = new( "string" )
        NativeBridge.Call( BridgeObject.CLR, "System", "Console", "ReadLine", bo, null )
        ret bo.toString()
        !#
        ret ""
        #ret CSharp.System.Console.ReadLine();
    }

   #Alias for input
    static string readLine()
    {
        ret "";
    }

    #Print without newline
    static void write(string text, params object[] param )
    {
        
    }

    #Print without newline (alias)
    static void print(string text, params object[] param )
    {
        SystemPrint(text, param);
    }

    #Print with newline
    static void println(string text, params object[] param )
    {
        SystemPrintln(text, param);
    }
}
