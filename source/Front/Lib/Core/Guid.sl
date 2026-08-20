# =========================================================================
# Guid - Globally Unique Identifier.
#
# Inspired by:
#   CLR  : System.Guid (NewGuid, Parse, ToString, Equals)
#   Rust : uuid::Uuid (new_v4, to_string, parse)
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
    _init_()
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
    # Equality
    # ---------------------------------------------------------------

    # Compare this Guid with another object.
    # Returns true if the other is a Guid with the same value.
    override bool equals( object obj )
    {
        if (obj == null)
        {
            ret false
        }
        Guid other = obj as Guid
        if (other == null)
        {
            ret false
        }
        ret this._value == other._value
    }

    # ---------------------------------------------------------------
    # Object overrides
    # ---------------------------------------------------------------

    override Int32 get hashCode()
    {
        ret SystemObjectGetHashCode(this._value)
    }

    override string toString()
    {
        ret this._value
    }
}
