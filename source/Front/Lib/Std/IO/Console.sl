
public class Console
{
    #Print without newline
    static void write(string text, params object[] param )
    {
        SystemPrint(text, param);
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

    #Read a line from stdin and return as string
    static string input()
    {
        ret SystemInput();
    }

    #Alias for input
    static string readLine()
    {
        ret SystemReadLine();
    }

    #Read a single key
    static string readKey()
    {
        ret SystemReadKey();
    }
}
