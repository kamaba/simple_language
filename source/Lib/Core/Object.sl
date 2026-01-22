import CSharp.SimpleLanguage.Core

public class Object
{
    #basic constructor
    void _init_()
    {
    }

    final get Type type()
    {
        ret SimpleLanguage.Lib.ObjectClass.GetObjectType(this);
    }

    #static helper: null-safe equality
    public static bool objectEquals(object objA, object objB)
    {
        if (objA == null && objB == null)
        {
            ret true;
        }
        if (objA == null || objB == null)
        {
            ret false;
        }
        ret objA.equals(objB);
    }

    #reference equality
    public static bool refEquals(object objA, object objB)
    {
        if (objA == null || objB == null){ ret false };
        ret objA.ref == objB.ref;
    }
    #hashCode getter - delegated to runtime helper
    public Int32 get hashCode()
    {
        ret SimpleLanguage.Lib.ObjectClass.GetHashCodeBySObject(this);
    }

    #equality: default delegates to runtime equality helper which may compare by reference
    public bool equals(object obj)
    {
        if (obj == null){ ret false; }
        ret SimpleLanguage.Lib.ObjectClass.EqualObject(this, obj);
    }
    #runtime internal reference (object identity)
    object get ref()
    {
        ret SimpleLanguage.Lib.ObjectClass.ObjectRef(this);
    }

    #weak reference getter
    object get refWeak()
    {
        ret SimpleLanguage.Lib.ObjectClass.ObjectWeakRef(this);
    }

    #reference count (for debugging)
    public Int32 get refCount()
    {
        ret SimpleLanguage.Lib.ObjectClass.RefCount(this);
    }

    #free/release placeholders - runtime manages lifecycle, expose for API completeness
    public void free()
    {
        SimpleLanguage.Lib.ObjectClass.FreeObject(this);
    }

    public void release()
    {
        SimpleLanguage.Lib.ObjectClass.ReleaseObject(this);
    }
    #string representation
    public string toString()
    {
        ret "Object";
    }
}
