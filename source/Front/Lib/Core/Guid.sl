# =========================================================================
# Guid - Globally Unique Identifier.
#
# Inspired by:
#   CLR  : System.Guid (NewGuid, ToString)
#   Rust : uuid::Uuid (new_v4, to_string)
#   Go   : google/uuid (New, String)
#
# Represents a 128-bit unique identifier stored as a string in standard
# format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
# =========================================================================
public class Guid extends Object
{
    private string _value = null

    # ---------------------------------------------------------------
    # Constructors
    # ---------------------------------------------------------------

    # Default constructor: generate a new GUID.
    override _init_()
    {
        this._value = SystemGuidNewGuid()
    }

    # Construct from an existing GUID string.
    _init_( string value )
    {
        if (value != null)
        {
            this._value = value
        }
        else
        {
            this._value = SystemGuidNewGuid()
        }
    }

    # ---------------------------------------------------------------
    # Static factory
    # ---------------------------------------------------------------

    # Generate a new GUID string and return it.
    public static string newGuid()
    {
        ret SystemGuidNewGuid()
    }

    # ---------------------------------------------------------------
    # Instance accessors
    # ---------------------------------------------------------------

    # The GUID string value.
    public get string value()
    {
        ret this._value
    }

    # ---------------------------------------------------------------
    # Formatting
    # ---------------------------------------------------------------

    # Format the GUID using a format specifier (mirrors C# Guid.ToString(format)):
    #   "N" - 32 digits, no hyphens:        00000000000000000000000000000000
    #   "D" - 32 digits with hyphens:       00000000-0000-0000-0000-000000000000  (default)
    #   "B" - hyphens, braces:              {00000000-0000-0000-0000-000000000000}
    #   "P" - hyphens, parentheses:        (00000000-0000-0000-0000-000000000000)
    # Any other value falls back to "D".
    public string toString( string format )
    {
        if (this._value == null)
        {
            ret ""
        }
        if (format == null || format.length == 0)
        {
            ret this._value
        }

        string f = format
        # Normalize to upper case for single-char comparison
        if (f == "n") { f = "N" }
        if (f == "d") { f = "D" }
        if (f == "b") { f = "B" }
        if (f == "p") { f = "P" }

        if (f == "N")
        {
            # Strip all hyphens
            string s = ""
            int i = 0
            while (i < this._value.length)
            {
                string ch = this._value.range(i, i + 1)
                if (ch != "-")
                {
                    s = s + ch
                }
                i = i + 1
            }
            ret s
        }
        if (f == "B")
        {
            ret "{" + this._value + "}"
        }
        if (f == "P")
        {
            ret "(" + this._value + ")"
        }
        # "D" or unknown: default format
        ret this._value
    }

    # ---------------------------------------------------------------
    # Object overrides
    # ---------------------------------------------------------------

    override string toString()
    {
        ret this._value
    }
}
