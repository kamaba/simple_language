import CSharp.System
import CSharp.SimpleLanguage.Core

NumberTest
{
    static boolTest()
    {
        
    }
    static byteTest()
    {
        
    }
    static int32Test()
    {        
        System.Console.WriteLine("intmax:" + int.MaxValue.toString() )

        a = 20
        b = 30
        c = a+b
        uint d = 40
        e = c + d
        #System.Console.WriteLine( "c=" + e )
        str = "a($a )+b($b )=$(a + b)"
        str = 'a($a )+b($b )=$(a + b)'
        System.Console.WriteLine( str )
        #System.Console.WriteLine( "a($a )+b($b )=$(a + b)" )
    }
    static fun()
    {
        int32Test();
    }
}