
public class Float32_2 extends Object
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f;

    public void _init_( Float32 _x, Float32 _y )
    {
        this.x = _x;
        this.y = _y;
    }

    publci override string toString()
    {
        ret string.toFormat( "x={0} y ={1}", this.x, this.y )
    }
}