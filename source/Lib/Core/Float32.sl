
public class Core.Float32 extends Object
{
    public const Epsilen = 4.9123123213d;
    public const MaxValue = 20d;
    public const MinValue = -1d;

    Float32 value;


    public static bool IsFinite( Float32 f )
    {
        return false;
    }
    public override get Type type()
    {
        ret null
    }


    public override String toString()
    {
        return this;
    }
    Cast<T>()
    {
        if T == Int32 
        {
            return Int32.ParseFloat( value );
        }
        elif T == Long
        {
            return Int64.ParseFloat( value );
        }
        elif T == String
        {
            return String.ParseFloat( value );
        }
        return 0.0f;
    }
    public static Int32 toInt32( Float _value )
    {
        return Int32.Parse( _value );
    }
}