import CSharp.SimpleLanguage.Core

public class String extends Object
{
    private String _value = null
    #!
    _init_( Int8 aa )
    {
        this._value = aa.toString()
    }
    _init_( Int32 aa )
    {
        this._value = aa.toString()
    }
    !#
    _init_( String aa )
    {
        this._value = aa
    }
    #!
    Int32 toInt32()
    {
        if( Int32.tryInt32( this._value, Int32 int32val ) )
        {
            ret int32val
        }
        ret null
    }
    Array<Int8> toInt8Array()
    {
        ret null
    }
    ListInt8 toListInt8()
    {
        ret null
    }
    List16 toListInt16()
    {
        ret null
    }
    Int32 getStringByIndex( int _index )
    {
        ret 0
    }
    static Int32 StringtoInt32( String value )
    {
        ret Int32.Parse( value );
    }
    !#    
    String toString()
    {
        ret this;
    }
    public static string toFormat( string _fomrat, parmas object[] para )
    {
        ret ""
    }
}