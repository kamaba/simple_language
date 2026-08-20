# =========================================================================
# Environment - Environment variable and runtime information API.
#
# Inspired by:
#   CLR  : System.Environment (GetEnvironmentVariable, SetEnvironmentVariable,
#          TickCount, TickCount64, NewLine, CurrentDirectory)
#   Go   : os.Getenv, os.Setenv, os.Getwd
#   Rust : std::env
#
# Provides access to process environment variables, the current working
# directory, platform newline, and timing counters.
# =========================================================================
public class Environment extends Object
{
    # ---------------------------------------------------------------
    # Environment variable access
    # ---------------------------------------------------------------

    # Get the value of an environment variable by name.
    # Returns the value string, or an empty string if not found.
    public static string getVariable( string name )
    {
        ret SystemEnvironmentGetVariable( name )
    }

    # Set an environment variable to the given value.
    # Returns true on success.
    public static bool setVariable( string name, string value )
    {
        ret SystemEnvironmentSetVariable( name, value )
    }

    # ---------------------------------------------------------------
    # Current working directory
    # ---------------------------------------------------------------

    # Get the current working directory path.
    public static string currentDirectory()
    {
        ret SystemDirectoryGetCurrent()
    }

    # Set the current working directory.
    # Returns true on success.
    public static bool setCurrentDirectory( string path )
    {
        ret SystemDirectorySetCurrent( path )
    }

    # ---------------------------------------------------------------
    # Platform properties
    # ---------------------------------------------------------------

    # The platform newline string ("\n").
    public static get string newLine()
    {
        ret "\n"
    }

    # ---------------------------------------------------------------
    # Timing
    # ---------------------------------------------------------------

    # Monotonic tick count in milliseconds, as a 32-bit integer.
    # Wraps around every ~24.8 days (same semantics as C#
    # Environment.TickCount). Use tickCount64() if you need the
    # full 64-bit value without overflow.
    public static Int32 tickCount()
    {
        ret SystemConvertInt32( SystemTimerClock() )
    }

    # Monotonic tick count in milliseconds, as a 64-bit integer.
    # No overflow (same semantics as C# Environment.TickCount64).
    public static Int64 tickCount64()
    {
        ret SystemTimerClock()
    }

    # Unix timestamp in milliseconds since epoch (1970-01-01 UTC).
    public static Int64 nowMillis()
    {
        ret SystemTimerNowMillis()
    }

    # ---------------------------------------------------------------
    # Object overrides
    # ---------------------------------------------------------------

    # Returns the current working directory so that
    # println(Environment) shows something useful.
    override string toString()
    {
        ret SystemDirectoryGetCurrent()
    }
}
