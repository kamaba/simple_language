
public class Console
{
    #Print without newline
    static void write(string text, params object[] param )
    {
        SystemCallExternalFunction("Console.write", text, param);
    }

    #Print without newline (alias)
    static void print(string text, params object[] param )
    {
        SystemCallExternalFunction("Console.print", text, param);
    }

    #Print with newline
    static void println(string text, params object[] param )
    {
        SystemCallExternalFunction("Console.println", text, param);
    }

    #Read a line from stdin and return as string
    static string input()
    {
        object ret = SystemCallExternalFunction("Console.input");
        ret ret
    }

    #Alias for input
    static string readLine()
    {
        object ret = SystemCallExternalFunction("Console.readLine");
        ret ret
    }

    #Read a single key
    static string readKey()
    {
        object ret = SystemCallExternalFunction("Console.readKey");
        ret ret
    }
}
