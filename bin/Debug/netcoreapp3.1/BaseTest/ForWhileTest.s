ForWhileTest
{   
    static Fun()
    {
        forfun()
        #whilefun()
    }
    static forfun()
    {
        i = 20
        for
        {
            if i > 22
            {
                break
            }
            CSharp.System.Console.WriteLine("for{} i= $i ")
        }
        #!
        for i = 123
        {
            if i >= 130
            {
                break
            }
            i++
            CSharp.System.Console.WriteLine("i= $i ")
        }
        !#
        
        #!
        for i = 0, i < 10
        {
            CSharp.System.Console.WriteLine("i= $i ")
            i++            
        }
        !#
        #!
        for i = 0, i <= 2, i+=2
        {            
            CSharp.System.Console.WriteLine("i= $i ");
            n = i * 10;
        }
        !#
        #!
        for i = 0, i < 30, i++
        {
            CSharp.System.Console.WriteLine("i= $i ");
            n = i * 10;
            #if n == 200{ break }

            if n % 2 == 0 {CSharp.System.Console.WriteLine("这是一个偶数 = $i ");continue }
        }
        !#
        #!
        int i2 = 0;
        List list = List();        
        for it in list
        {

        }

        Array arr = [1,2,3];
        for v in a{
            System.Console.Write(" v= $v ")
        }   
        Array b = [{a=1}, {a=2}, {a = 3} ];
        
        for v in b{

        }
        for v in 1..10
        {
        }
        !#
    }    
    static whilefun()
    {
        int i = 0;
        while i < 30
        {
            #CSharp.System.Console.Write(" i= $i ");
            if( i == 5 ){continue;}
            if i > 10{ break;}
            i++;
        }
        while true{ m = 20; break;}
        while true{ m = 20;}
        i = 30;
        dowhile i < 20
        {
            #CSharp.System.Console.Write("dowhile execute" );
            x = 30;
        }
        dowhile false
        {
            m =  20;
        }
        while true { i+= 20; if i == 20{ break; } }
    }
}