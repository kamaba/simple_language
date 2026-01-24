import CSharp.System
import Std

#Runtime tests for `type` operator
TypeTest
{
    static fun()
    {
        #!
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
        !#

        #generic type
        tg = Array<int>.type
        #System.Console.WriteLine("List<int>.type -> " + tg.toString())

        #compare with different instantiation
        tg2 = Array<Array<int> >.type
        #System.Console.WriteLine("Array<string>.type -> " + tg2.toString())

        if tg == tg2
        {
            System.Console.WriteLine("Array<int>.type == Array<string>.type : true")
        }
        else
        {
            System.Console.WriteLine("Array<int>.type == Array<string>.type : false")
        }
    }
}
