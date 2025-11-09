
@Nickname("VectorF32")
public class Core.Float32_3 extends Object
{
    @Nickname("x")
    public float a = 0.0f
    @Nickname("y")
    public float b = 0.0f
    @Nickname("z")
    public float c = 0.0f

    _init_( float _x, float _y, float _z )
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