public class String extends Object
{
    private String _value = null
    private Int32 _length = -1

    _init_( Int8 aa )
    {
        this._value = SystemConvertString( aa )
    }
    _init_( Int32 aa )
    {
        this._value = SystemConvertString( aa )
    }
    _init_( String aa )
    {
        this._value = aa
    }
    public string format( params object[] _parmas )
    {
        ret SystemStringFormat(this, _parmas)
    }
    public get int length()
    {
        if (this._length == -1)
        {
            this._length = SystemStringLength(this)
        }
        ret this._length
    }
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
    UInt8Array toUInt8Array()
    {
        ret SystemStringToUInt8Array( this )
    }
}