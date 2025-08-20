
public class Object
{
    public Int32 getHashCode()
    {
        return 0;
    }
    public Object clone()
    {
        ret Object();
    }
    public get Type type()
    {
        
    }
    public Object cast<T>()
    {
        ret this;
    }
    public string toString()
    {
        ret "";
    }
    public UInt16 toUShort()
    {
        ret 0us;
    }
    public Int16 toShort()
    {
        ret 0s;
    }
    public Int32 toInt()
    {
        ret 0i;
    }
    public UInt32 toUInt()
    {
        ret 0ui;
    }
    public Int64 toLong()
    {
        ret 0L;
    }
    public Int64 toULong()
    {
        ret 0uL;
    }
    public Float32 toFloat32()
    {
        ret 0.0f;
    }
    public Float64 toFloat64()
    {
        ret 0.0d;
    }
    #!
    以下的，在有扩展容器后，动态添加到Object类中，通过宏判断，如果已加载模块后，进行添加
    public Array toArray()
    {
        ret null;
    }
    public List toList()
    {
        ret null;
    }
    public Map toMap()
    {
        ret null;
    }
    public Tuple toTuple()
    {
        ret null;
    }
    !#
}