public class Object
{
    #basic constructor
    void _init_()
    {
    }

    final get Type type()
    {
        ret SystemObjectGetType(this)
    }

    #static helper: null-safe equality
    public static bool objectEquals(object objA, object objB)
    {
        ret SystemEqualObject( objA, objB);
    }

    #reference equality
    public static bool refEquals(object objA, object objB)
    {
        ret SystemEqualObject( objA, objB);
    }
    #hashCode getter - delegated to runtime helper
    public Int32 get hashCode()
    {
        ret SystemObjectGetHashCode(this)
    }

    #equality: default delegates to runtime equality helper which may compare by reference
    public bool equals(object obj)
    {
        ret SystemEqualObject(this, obj)
    }
    #runtime internal reference (object identity)
    object get ref()
    {
        ret SystemObjectRef(this)
    }

    #weak reference getter
    object get refWeak()
    {
        ret SystemObjectRefWeak(this)
    }

    #reference count (for debugging)
    public Int32 get refCount()
    {
        ret SystemObjectRefCount(this)
    }

    #free/release placeholders - runtime manages lifecycle, expose for API completeness
    public void free()
    {
        SystemObjectFree(this)
    }
    public void release()
    {
        SystemObjectRelease(this)
    }
    #string representation
    public string toString()
    {
        hc = SystemObjectGetHashCode(this);
        ret "Object:" + hc.toString();
    }
}
