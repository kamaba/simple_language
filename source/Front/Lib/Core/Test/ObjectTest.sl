import CSharp.System
import CSharpLang.SimpleLanguage.Core

ObjectTest
{
    static fun()
    {
        // create instances
        Object obj = new()
        Object obj2 = obj
        Object obj3 = new()
        Object obj4 = new()

        bool obj_eq_obj2 = obj == obj2
        System.Console.WriteLine("obj_eq_obj2:" + obj_eq_obj2.toString() )

        bool eq2 = obj.equals( obj2 )
        System.Console.WriteLine("eq2:" + eq2.toString() )

        bool eq3 = Object.objectEquals( obj3, obj4 )
        System.Console.WriteLine("eq3:" + eq3.toString() )

        bool refEq = Object.refEquals(obj3, obj4)
        System.Console.WriteLine("refEquals obj3,obj4:" + refEq.toString())

        int refc = obj2.refCount
        System.Console.WriteLine("Object refCount:" + refc.toString() )

        // clone example
        Object cloned = obj.clone()
        System.Console.WriteLine("cloned != null:" + (cloned != null).toString())
    }
}
