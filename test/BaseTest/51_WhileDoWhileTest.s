WhileTest
{   
    static whilefun()
    {
        int i = 0;
        #!
        while i < 14
        {
            i++
            if( i == 5 ){
            continue;}
            if i > 10{ break;}
            CSharp.System.Debug.Write(" ioooo = $i ");
        }
        !#
        i = 10
        while
        {
            i++;
            CSharp.System.Debug.Write(" ioooo = $i ");
            if( i > 13 ){break;}
        }
        while true{ m = 20;}
        i = 30;
        dowhile i < 20
        {
            #CSharp.System.Debug.Write("dowhile execute" );
            x = 30;
        }
        dowhile false
        {
            m =  20;
        }
        while true { i+= 20; if i == 20{ break; } }
        !#
    }
}