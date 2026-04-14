import CSharp.System
import CSharpLang.SimpleLanguage

ForTest
{
    static fun()
    {
        
        #a = [1,2,"222",1000L,[1,2,3,4]]
        #!
        a = [1981,"mmmmm", 0xef, 33333L,[1988,2045]]
        for v in a
        {
            #vs = v.toString()
            println("------------------$v.toString() ")
        }
        !#     

        #!
        for v in BridgeKind
        {
            if v == BridgeKind.SELF 
            {
                println( "BridgeKind--------------SELF " )
                continue
            }
            elif v == BridgeKind.JVM
            {
                println( "BridgeKind--------------JVM " )
                break
            }
            println( "BridgeKind= $v.name.toString() " )
            printBridgeKind(v)
        }

        r1 = 1..100;    #快速int range  以后再支持 1..n   n..200的方式    相当于   range( 1, 100, 1 )的调用        
        r2 = range( 1.0f, 200.0f, 1.0f );
        Range<double> r3 = (3.2d, 54.3d, 0.22d );
        r4 = Range<short>( 1s, 100s, 2s );

        r1.step( 1 )   #设置r1的步进
        r4.step = 2       #设置r4的步进 与SetStep方法一样
        for v in r1   
        {
            CSharp.System.Debug.Write("value=$v");
        }
        !#
            
        sum = 0
        for i in range(1,10, 2 )
        {
            sum += 10
            println( i )
            for n in range( 3, 6 )
            {
               sum += n
               println( "n=$n" )
            }
        }
        println("sum:"  + sum)
        
    }
}