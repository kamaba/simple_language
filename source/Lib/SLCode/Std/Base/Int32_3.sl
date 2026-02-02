
public class Core.Int32_3 extends Object
{    
    Int32 a = 0f
    Int32 b = 0f
    Int32 c = 0;

    _init_( float _a, float _b )
    {
        this.a = _a;
        this.b = _b;
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