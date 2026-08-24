@nickname("Quat")
public class Quaternion extends Object
{
    @nickname("a")
    public Int32 x = 0
    
    @nickname("b")
    public Int32 y = 0;

    public void _init_( Int32 _x, Int32 _y )
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