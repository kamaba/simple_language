
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
        ret SimpleLanguage.Lib.ObjectClass.EqualObject(this, obj);
    }
    public static bool objectEquals(object objA, object objB)
    {
        if (objA == null || objB == null)
        {
            ret false;
        }
        ret objA.equals(objB);
    }
    public static bool refEquals(object objA, object objB)
    {
        ret objA.ref == objB.ref;
    }
    public Object clone()
    {
        ret SimpleLanguage.Lib.ObjectClass.CloneObject(this);
    }    
    public object get `````````````````````ref`````````````````````()
    {
        ret SimpleLanguage.Lib.ObjectClass.ObjectRef(this);
    }
    get object  refWeak()
    {
        ret SimpleLanguage.Lib.ObjectClass.ObjectWeakRef(this);
    } 
    int get refCount()
    {
        ret SimpleLanguage.Lib.ObjectClass.RefCount(this);
    }
    free()
    {

    }
    release()
    {

    }
    !#
    以下是系统方法
    public Object cast<T>()
    {
        ret this;
    }
}