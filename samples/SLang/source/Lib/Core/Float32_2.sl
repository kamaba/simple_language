
public class Core.Float32_2 extends Object
{    
    float a = 0.0f
    float b = 0.0f

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