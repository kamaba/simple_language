import CSharp.System
import Std
import CSharpLang

#Runtime tests for `type` operator
TypeTest
{
    static fun()
    {
        #primitive type via function
        t = int.type()
        #System.Console.WriteLine("int.type() -> " + t.toString())

        #instance .type
        int i2 = 20
        t2 = i2.type
        #System.Console.WriteLine("i2.type -> " + t2.toString())

        
        if t == t2
        {
            System.Console.WriteLine("int.type == i2.type : true")
        }
        else
        {
            System.Console.WriteLine("int.type == i2.type : false")
        }

        #generic type
        tg = Array<int>.type
        System.Console.WriteLine("List<int>.type -> " + tg.toString())

        #compare with different instantiation
        tg2 = Array<Array<int> >.type
        System.Console.WriteLine("Array<Array<Array<int> >.type -> " + tg2.toString())

        if tg == tg2
        {
            System.Console.WriteLine("Array<int>.type == Array<string>.type : true")
        }
        else
        {
            System.Console.WriteLine("Array<int>.type == Array<string>.type : false")
        }

        
        #!
        for v in BridgeKind
        {
            #int index = v.index;
            #name = v.name
            if v == BridgeKind.SELF
            {

            }
        }
        for v in BridgeKind.values
        {

        }
        
        a = 200;
        switch kind
        {
            case BridgeKind.SELF
            {
                a = 1
                next
            }
            case BridgeKind.CLR
            {

            }
            case BridgeKind.JVM
            {

            }
            case BridgeKind.NATIVE
            {

            }     
            default{
                a = 20;
            }       
        }
        !#

        t1 = obj.type
        t3 = obj3.type
        global.println("[9] type: obj.type -> " + t1.toString())
        global.println("[10] type equality obj.type==obj3.type (same class) -> " + (t1 == t3).toString())
    }
}
