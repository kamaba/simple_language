
public class Core.String extends Object
{
    private String _value = null

    _init_( object aa )
    {
        this._value = aa
    }
    _init_( String aa )
    {
        this._value = aa
    }
    String toString()
    {
        ret this;
    }
    Int32 toInt32()
    {
        if( Int32.tryInt32( this._value, Int32 int32val ) )
        {
            ret int32val
        }
        ret null
    }
    static Int32 StringtoInt32( String value )
    {
        ret Int32.Parse( value );
    }
    public static string toFormat( string _fomrat, parmas object[] para )
    {
        
    }
}