
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
        ret Int32(SystemReadLine());
    }

    #Read line with prompt and parse to Int32
    static int readInt(string prompt)
    {
        SystemPrint(prompt, null);
        ret Int32(SystemReadLine());
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

    # ---- Screen / cursor control ----

    #Clear the whole screen (cursor returns to 0,0)
    static void clear()
    {
        SystemConsoleClear();
    }

    #Move the cursor to (left, top), 0-based
    static void setCursorPosition(int left, int top)
    {
        SystemConsoleSetCursorPosition(left, top);
    }

    #Show or hide the caret
    static void setCursorVisible(bool visible)
    {
        SystemConsoleSetCursorVisible(visible);
    }

    #Hide the caret (games usually want this)
    static void hideCursor()
    {
        SystemConsoleSetCursorVisible(false);
    }

    #Show the caret again
    static void showCursor()
    {
        SystemConsoleSetCursorVisible(true);
    }

    # ---- Color control (use the colorXxx constants below) ----

    static void setForegroundColor(int color)
    {
        SystemConsoleSetForegroundColor(color);
    }

    static void setBackgroundColor(int color)
    {
        SystemConsoleSetBackgroundColor(color);
    }

    #Reset colors to terminal default
    static void resetColor()
    {
        SystemConsoleResetColor();
    }

    # ---- Non-blocking input (real-time loops) ----

    #True when a key is waiting in the console input buffer (never blocks)
    static bool keyAvailable()
    {
        ret SystemConsoleKeyAvailable();
    }

    #Read one key directly from the console buffer (no Enter needed).
    #Returns the key char, "up"/"down"/"left"/"right" for arrow keys, "" when empty.
    #Pair with keyAvailable(): while (Console.keyAvailable()) { k = Console.readKeyDirect() }
    static string readKeyDirect()
    {
        ret SystemConsoleReadKey();
    }

    # ---- Window size ----

    #Current console window width in columns
    static int windowWidth()
    {
        ret SystemConsoleWindowWidth();
    }

    #Current console window height in rows
    static int windowHeight()
    {
        ret SystemConsoleWindowHeight();
    }

    # ---- Frame pacing ----

    #Block the current thread for ms milliseconds (game frame delay)
    static void sleep(int ms)
    {
        SystemSleep(ms);
    }

    # ---- ConsoleColor constants (16 colors) ----
    static int colorBlack       = 0
    static int colorDarkBlue    = 1
    static int colorDarkGreen   = 2
    static int colorDarkCyan    = 3
    static int colorDarkRed     = 4
    static int colorDarkMagenta = 5
    static int colorDarkYellow  = 6
    static int colorGray        = 7
    static int colorDarkGray    = 8
    static int colorBlue        = 9
    static int colorGreen       = 10
    static int colorCyan        = 11
    static int colorRed         = 12
    static int colorMagenta     = 13
    static int colorYellow      = 14
    static int colorWhite       = 15
}
