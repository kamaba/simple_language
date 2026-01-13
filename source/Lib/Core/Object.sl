import CSharp.SimpleLanguage.Core

public class Object
{    
    public static bool objectEquals(object objA, object objB)
    {
        if (objA == null || objB == null)
        {
            ret false;
        }
        ret objA.equals(objB);
    }
    #!
    public static bool refEquals(object objA, object objB)
    {
        ret objA.ref == objB.ref;
    }
    !#
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
    #!
    Object clone()
    {
        ret SimpleLanguage.Lib.ObjectClass.CloneObject(this);
    } 
    object get ref()
    {
        ret SimpleLanguage.Lib.ObjectClass.ObjectRef(this);
    }  
    get object refWeak()
    {
        ret SimpleLanguage.Lib.ObjectClass.ObjectWeakRef(this);
    } 
    !# 
    get Int32 refCount()
    {
        ret SimpleLanguage.Lib.ObjectClass.RefCount(this);
    }
    #!
    free()
    {

    }
    release()
    {

    }
    public T cast<T>()
    {
        ret this as T;
    }
    !#
    string toString()
    {
        ret "Object"
    }
}