
public class Core.String extends Object
{
    private String _value = null

    _init_( object aa )
    {
    }
    _init_( String aa )
    {

    }
    String toString()
    {
        ret this;
    }
    static Int32 toInt32( String value )
    {
        ret Int32.Parse( value );
    }
}