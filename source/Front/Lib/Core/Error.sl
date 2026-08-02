public class Error extends Object
{
    # ── Core fields ──

    # Numeric error code
    Int32 code = 0

    # Human-readable error description
    string message = ""

    # ── Constructors ──

    _init_()
    {
        this.code = 0
        this.message = ""
    }

    _init_( Int32 code )
    {
        this.code = code
        this.message = ""
    }

    _init_( Int32 code, string msg )
    {
        this.code = code
        this.message = msg
    }

    # ── Property getters ──

    get Int32 getErrorCode()
    {
        ret this.code
    }

    get string getMessage()
    {
        ret this.message
    }

    # ── Methods ──

    override string toString()
    {
        string s = "Error"
        if (this.code != 0)
        {
            s = s + " (code=" + this.code.toString() + ")"
        }
        if (this.message.length() > 0)
        {
            s = s + ": " + this.message
        }
        ret s
    }
}

public enum MathOpError extends Error
{
    Overflow = 1,
    Underflow = 2,
    DivisionByZero = 3,
    InvalidArgument = 4,
    InvalidOperation = 5
}

