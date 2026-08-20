# =========================================================================
# Environment - Environment variable and runtime information API.
#
# Inspired by:
#   CLR  : System.Environment (GetEnvironmentVariable, SetEnvironmentVariable)
#   Go   : os.Getenv, os.Setenv, os.Getwd
#   Rust : std::env
#
# Provides access to process environment variables, the current working
# directory, and a high-resolution tick counter for timing.
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
    # Timing
    # ---------------------------------------------------------------

    # High-resolution monotonic clock in milliseconds.
    # Useful for measuring elapsed time.
    public static Int64 tickCount()
    {
        ret SystemTimerClock()
    }

    # Unix timestamp in milliseconds since epoch.
    public static Int64 nowMillis()
    {
        ret SystemTimerNowMillis()
    }

    # ---------------------------------------------------------------
    # Object overrides
    # ---------------------------------------------------------------

    override string toString()
    {
        ret "Environment"
    }
}
