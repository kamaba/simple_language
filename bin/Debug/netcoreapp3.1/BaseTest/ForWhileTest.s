forwhile_test
{   
    static forfun()
    {
        int j = 0;
        for i = 0, i < 30, i+=2
        {
            j++;
            #System.Console.Write("i= @i ");
            n = i * 10;
        }
        for i = 0, i < 30, i++
        {
            #System.Console.Write("i= @i ");
            n = i * 10;
            if n == 400{ break; }

            if n % 2 == 0 { n=40; continue; }
        }
        #!
        int i2 = 0;
        List list = List();
        for( it = list.begin(); it != list.end(); it++ )
        {

        }
        for it in list
        {

        }
    
        Array a = (1,2,3);
        for v in a{
            System.Console.Write(" v= @v ")
        }   
        Array b = ({a=1}, {a=2}, {a = 3} );
        
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
            #CSharp.System.Console.Write(" i= @i ");
            if( i == 5 ){continue;}
            if i > 10{ break;}
            i++;
        }
        while{ m = 20; break;}
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
        while{ i+= 20; if i == 20{ break; } }
    }
}