
public class Core.Float32_2 extends Object
{    
    @Nickname("x")
    float a = 0.0f
    
    @Nickname("y")
    float b = 0.0f

    _init_( float _a, float _b )
    {
        this.a = _a;
        this.b = _b;
    }
    override String toString()
    {
        ret this;
    }

    
    static Int32 Float32_2ToInt32_2( Float32_2 f2 )
    {
        ret Int32.Parse( value );
    }
}