
public class Core.Object
{
    void _init_()
    {
    }
    public Int32 get hashCode()
    {
        ret SimpleLanguage.Lib.ObjectClass.GetHashCodeBySObject( this )
    }
    public bool equals(object obj)
    {
        ret RuntimeHelpers.Equals(this, obj);
    }
    #!
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
    public static bool referenceEquals(object? objA, object? objB)
    {
        ret objA == objB;
    }
    public Object clone()
    {
        ret Object();
    }
    以下是系统方法
    public Object cast<T>()
    {
        ret this;
    }
    public object get ref()
    {
        ret null
    }
    get object  refWeak()
    {
    } 
    int get refCount()
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