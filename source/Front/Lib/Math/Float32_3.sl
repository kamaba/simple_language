

@Nickname("Point3D") 
@Nickname("Vector3")
@Nickname("Vec3")
public class Float32_3
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f;
    public Float32 z = 0.0f;

    public void _init_( Float32 _x, Float32 _y , Float32 _z )
    {
        this.x = _x;
        this.y = _y;
        this.z = _z;
    }

    publci override string toString()
    {
        ret string.toFormat( "x={0} y ={1} z={2}", this.x, this.y, this.z )
    }

}