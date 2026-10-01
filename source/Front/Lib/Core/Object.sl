public class Object
{

    #static helper: null-safe equality
    public static bool refEquals(object objA, object objB)
    {
        ret SystemEqualObject( objA, objB);
    }


    #basic constructor
    void _init_()
    {
    }

    #resource management hooks (attribute / with semantics)
    #_init_ 同款传递模式: 根类真实声明, 子类走普通 override/final 检查流程
    public Object _enter_()
    {
        ret this
    }
    public void _exit_()
    {
    }

    final get Type type()
    {
        ret SystemObjectGetType(this)
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
    #string representation
    public string toString()
    {
        hc = SystemObjectGetHashCode(this);
        ret "Object:" + hc.toString();
    }
}
