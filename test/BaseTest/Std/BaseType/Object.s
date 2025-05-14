
Object
{
    public Int32 GetHashCode()
    {
        return 0;
    }
    public Object Clone()
    {
        return Object();
    }
    public Type GetType()
    {
        
    }

    public Object Cast<T>()
    {
        return this;
    }
    public string ToString()
    {
        return "";
    }
    public UInt16 ToUShort()
    {
        return 0us;
    }
    public Int16 ToShort()
    {
        return 0s;
    }
    public Int32 ToInt()
    {
        return 0i;
    }
    public UInt32 ToUInt()
    {
        return 0ui;
    }
    public Int64 ToLong()
    {
        return 0L;
    }
    public Int64 ToULong()
    {
        return 0uL;
    }
    public Float ToFloat()
    {
        return 0.0f;
    }
    public Double ToDouble()
    {
        return 0.0d;
    }
    public Array ToArray()
    {
        return null;
    }
    public List ToList()
    {
        return null;
    }
    public Map ToMap()
    {
        return null;
    }
    public Tuple ToTuple()
    {
        return null;
    }
}