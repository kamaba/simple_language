import CSharp.System
import CSharp.SimpleLanguage.Core

ObjectTest
{
    static fun()
    {
        Object obj = new()
        obj2 = obj
        obj3 = new()
        obj4 = Object()

        bool obj_eq_obj2 = obj == obj2
        bool eq2 = obj.equals( obj2 )
        eq3 = Object.objectEquals( obj3, obj4 )
        #objweak = obj3.refWeak;
        int refc = obj3.refCount

        System.Console.WriteLine("Object= ref:" + refc.toString() )
    }
}