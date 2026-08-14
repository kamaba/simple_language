@Nickname("Point") @Nickname("Vector2") @Condition()
@Nickname("Vec2")
public class Float32_2 extends Object
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f;

    public void _init_( Float32 _x, Float32 _y )
    {
        this.x = _x;
        this.y = _y;
    }
    Int32 _getItem_( int index )
    {
        if( index == 0 )
        {
            ret this.x
        }
        else
        {
            ret this.y
        }
    }
    void _setItem_( int index, Int32 value )
    {
        if( index == 0 )
        {
            this.x = value
        }
        else
        {
            this.y = value
        }
    }

    publci override string toString()
    {
        ret string.toFormat( "x={0} y ={1}", this.x, this.y )
    }
}