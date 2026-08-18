
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

    #Print just a newline (C# Console.WriteLine() / Dart print(""))
    static void newLine()
    {
        SystemPrintln("", null);
    }

    #C# style alias for println (Console.WriteLine)
    static void writeLine(string text, params object[] param )
    {
        SystemPrintln(text, param);
    }

    #Read a line from stdin and return as string
    static string input()
    {
        ret SystemInput();
    }

    #Read with prompt: print prompt (no newline) then read (Dart stdout.write + stdin.readLineSync)
    static string input(string prompt)
    {
        SystemPrint(prompt, null);
        ret SystemInput();
    }

    #Alias for input
    static string readLine()
    {
        ret SystemReadLine();
    }

    #Read line with prompt
    static string readLine(string prompt)
    {
        SystemPrint(prompt, null);
        ret SystemReadLine();
    }

    #Read a single key
    static string readKey()
    {
        ret SystemReadKey();
    }

    #Read a single key with prompt
    static string readKey(string prompt)
    {
        SystemPrint(prompt, null);
        ret SystemReadKey();
    }

    #Read line and parse to Int32 (C# int.Parse(Console.ReadLine()))
    static int readInt()
    {
        ret Int32.parse(SystemReadLine());
    }

    #Read line with prompt and parse to Int32
    static int readInt(string prompt)
    {
        SystemPrint(prompt, null);
        ret Int32.parse(SystemReadLine());
    }

    #Read line and parse to Float64 (C# double.Parse(Console.ReadLine()))
    static double readDouble()
    {
        ret SystemConvertFloat64(SystemReadLine());
    }

    #Read line with prompt and parse to Float64
    static double readDouble(string prompt)
    {
        SystemPrint(prompt, null);
        ret SystemConvertFloat64(SystemReadLine());
    }
}
