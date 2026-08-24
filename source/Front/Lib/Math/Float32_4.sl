

@Nickname("F4") 
@Nickname("Vector4")
@Nickname("Vec4")
public class Float32_4
{
    public float x = 0.0f;
    public float y = 0.0f;
    public float z = 0.0f;
    public float w = 0.0f;

    
    public void _init_( Float32 _x, Float32 _y , Float32 _z, Float32 _w )
    {
        this.x = _x;
        this.y = _y;
        this.z = _z;
        this.w = _w;
    }

    publci override string toString()
    {
        ret string.toFormat( "x={0} y ={1} z={2}", this.x, this.y, this.z )
    }
}