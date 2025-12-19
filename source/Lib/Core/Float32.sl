
public class Core.Float32 extends Object
{
    public const Epsilen = 4.9123123213d;
    public const MaxValue = 20d;
    public const MinValue = -1d;

    Float32 _value = 0.0f

    public void _init_( Float32 f )
    {
        this._value = f
    }


    public static bool IsFinite( Float32 f )
    {
        return false;
    }


    public override String toString( string format )
    {
        return string.format( this._value );
    }
    T cast<T>()
    {
        Type _type = T.type
        if _type == Int32.type
        {
            return Int32.ParseFloat( value );
        }
        elif _type == Int64.type
        {
            return Int64.ParseFloat( value );
        }
        elif _type == String.type
        {
            return String.ParseFloat( value );
        }
        return new T()
    }

    public Int32 toInt32()
    {
        ret FloatConvertInt32( this._value )
    }
    public static Int32 FloatConvertInt32( Float _value )
    {
        return Int32.Parse( _value );
    }
}