
@Nickname("Vector3")
public class Float32_3
{
    public float[3] _value = new()

    public static Float32_3 zero = new(0.0f, 0.0f, 0.0f )
    public static Float32_3 one = new(1.0f, 1.0f, 1.0f )
    public static Float32_3 xAxis = new(1.0f, 0.0f, 0.0f )
    public static Float32_3 yAxis = new(0.0f, 1.0f, 0.0f )
    public static Float32_3 zAxis = new(0.0f, 0.0f, 1.0f )

    @Nickname("a")
    public get Float32 x(){
        ret this._value[0]
    }

    _init_()
    {
    }
    _init_( Float32 _x, Float32 _y, Float32 _z )
    {
        this._value[0] = _x;
        this._value[1] = _y;
        this._value[2] = _z;
    }
    _init_( Float32[] _value )
    {
        if _value == null 
        {
            ret
        }
        if _value.length < 3
        {
            ret
        }
        this._value = _value
    }

    void scale( Float32_3 _scale )
    {
        this._value[0] *= _scale[0]
        this._value[1] *= _scale[1]
        this._value[2] *= _scale[2]
    }

    @AotCompile()
    Float32 dot( Float32_3 lhs, Float32_3 rhs )
    {
        ret lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z 
    }
}

@Nickname("Material3x3")
public class Float32_3x3
{
    float[3][3] _value = new()

    _init_()
    {

    }
}

public class FloatTest
{
    public static fun()
    {
        Float32_3x3
    }
}