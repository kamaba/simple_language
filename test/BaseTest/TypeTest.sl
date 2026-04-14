TypeTest
{
    ArrClass
    {
        int i = 0;
    }
    Level<T> 
    {
        static fun()
        {
            type1 = Level<T>.type
            type2 = Level<int>.type
            Console.WriteLine("levelT.type" + type1.toString() );
        }
    }
    static bool IsNType( Type t )
    {
        return t == int.type;
    }
    static fun()
    {  
        t = int.type()
        int i2 = 20
        t2 = i2.type

        if t == t2 
        {
            System.Console.WriteLine("22222222= " t2.toString() )
        }

        #!
        bool a = IsNType( ArrClass.type )
        var t = List<int>.type
        ArrayClass.type()
        var mcname mmi = t.metaClass.name

        System.Console.WriteLine("22222222= " t2.toString() )
        !#

        
        #primitive type via function
        t = float.type()
        global.println("int.type() -> " + t.toString())


        #instance .type
        int i2 = 20
        t2 = i2.type
         global.println("i2.type -> " + t2.toString())

        
        if t == t2
        {
            global.println("int.type == i2.type : true")
        }
        else
        {
            global.println("int.type == i2.type : false")
        }

        #generic type
        tg = Array<int>.type
        global.println("Array<int>.type -> " + tg.toString())

        #compare with different instantiation
        #tg2 = Array<Array<int>>.type
        #global.println("Array<Array<int>>.type -> " + tg2.toString())

        #!
        if tg == tg2
        {
            global.println("Array<int>.type == Array<string>.type : true")
        }
        else
        {
            global.println("Array<int>.type == Array<string>.type : false")
        }
        !#
        
        #!
        obj = Object()
        Object obj3 = new()
        t1 = obj.type
        t3 = obj3.type
        global.println("[9] type: obj.type -> " + t1.toString())
        global.println("[10] type equality obj.type==obj3.type (same class) -> " + (t1 == t3).toString())
        !#
    }
}