public class Exception extends Object
{
    # ── Core fields (CLR/Java/Dart convergence) ──

    # Human-readable error description (CLR: Message, Java: detailMessage)
    string message = ""

    # Numeric error code (CLR: HResult)
    Int32 code = 0

    # The exception that caused this one, for exception chaining (CLR: InnerException, Java: cause)
    # null if this is the root cause
    Exception cause = null

    # Preformatted call-stack string captured at throw time (CLR/Dart: string-based StackTrace)
    string stackTrace = ""

    # Name of the application or object that threw the error (CLR: Source)
    string source = ""

    # ── Constructors ──

    _init_()
    {
        this.message = ""
        this.code = 0
        this.cause = null
        this.stackTrace = ""
        this.source = ""
    }

    _init_( string msg )
    {
        this.message = msg
        this.code = 0
        this.cause = null
        this.stackTrace = ""
        this.source = ""
    }

    _init_( string msg, Int32 code )
    {
        this.message = msg
        this.code = code
        this.cause = null
        this.stackTrace = ""
        this.source = ""
    }

    # Chaining constructor: wrap a lower-level exception (CLR: Exception(msg, innerException), Java: Throwable(msg, cause))
    _init_( string msg, Exception innerCause )
    {
        this.message = msg
        this.code = 0
        this.cause = innerCause
        this.stackTrace = ""
        this.source = ""
    }

    # Full constructor
    _init_( string msg, Int32 code, Exception innerCause )
    {
        this.message = msg
        this.code = code
        this.cause = innerCause
        this.stackTrace = ""
        this.source = ""
    }

    # ── Property getters ──

    get string getMessage()
    {
        ret this.message
    }

    get Int32 getErrorCode()
    {
        ret this.code
    }

    get Exception getCause()
    {
        ret this.cause
    }

    get string getStackTrace()
    {
        ret this.stackTrace
    }

    get string getSource()
    {
        ret this.source
    }

    # ── Methods ──

    # Walk the cause chain to find the root exception (CLR: GetBaseException)
    Exception getBaseException()
    {
        Exception current = this
        while (current.cause != null)
        {
            current = current.cause
        }
        ret current
    }

    # Check whether this exception has an inner cause (Java: hasCause pattern)
    bool hasCause()
    {
        ret this.cause != null
    }

    # Print the full exception with cause chain and stack trace (Java: printStackTrace, CLR: ToString)
    void printStackTrace()
    {
        global.println(this.toString())
        if (this.stackTrace.length() > 0)
        {
            global.println(this.stackTrace)
        }
        Exception c = this.cause
        while (c != null)
        {
            global.println("Caused by: " + c.toString())
            if (c.stackTrace.length() > 0)
            {
                global.println(c.stackTrace)
            }
            c = c.cause
        }
    }

    # Full string representation including type, message, and code
    override string toString()
    {
        string s = "Exception"
        if (this.message.length() > 0)
        {
            s = s + ": " + this.message
        }
        if (this.code != 0)
        {
            s = s + " (code=" + this.code.toString() + ")"
        }
        ret s
    }
}
