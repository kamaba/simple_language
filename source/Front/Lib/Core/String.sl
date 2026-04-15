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
    public string format( params object[] _parmas )
    {
        ret SystemStringFormat(this, _parmas)
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
    public static string toFormat( string _format, params object[] _parmas )
    {
        ret SystemStringFormat(_format, _parmas)
    }

    # 截取前 index 个字符；index<=0 为空串；大于长度则整串
    string front( int index )
    {
        ret SystemStringFront( this, index )
    }

    # 截取末尾 index 个字符（从后往前数）
    string end( int index )
    {
        ret SystemStringEnd( this, index )
    }

    # 半开区间 [start, end)，等价 Substring(start, end - start)
    string range( int start, int end )
    {
        ret SystemStringRange( this, start, end )
    }

    # UTF-8 字节序列，Array<Byte>（ByteArray）
    ByteArray toByteArray()
    {
        ret SystemStringToByteArray( this )
    }
}