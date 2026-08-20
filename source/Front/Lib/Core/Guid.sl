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
    # Object overrides
    # ---------------------------------------------------------------

    override string toString()
    {
        ret this._value
    }
}
