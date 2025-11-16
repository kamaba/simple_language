
public class Core.Object
{
    void _init_()
    {

    }
    public Int32 get hashCode()
    {
        return 0;
    }
    public Object clone()
    {
        ret Object();
    }
    public get Type type()
    {
        ret Object.type();
    }

    public bool equals(object obj)
    {
        ret RuntimeHelpers.Equals(this, obj);
    }
    public static bool equals(object objA, object objB)
    {
        if (objA == objB)
        {
            ret true;
        }
        if (objA == null || objB == null)
        {
            ret false;
        }
        ret objA.equals(objB);
    }
    public static bool ReferenceEquals(object? objA, object? objB)
    {
        ret objA == objB;
    }
    #!
    以下是系统方法
    public Object cast<T>()
    {
        ret this;
    }
    public T get ref()
    {
        ret null
    }
    T get refWeak()
    {

    } 
    public int get refCount()
    {
        ret 0;
    }
    free()
    {

    }
    release()
    {

    }
    !#
}