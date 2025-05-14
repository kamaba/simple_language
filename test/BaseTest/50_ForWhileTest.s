ForWhileTest
{   
    static Fun()
    {
        #forfun()
        whilefun()
    }
    enum EItType
    {
        It1 = 1
        It2 = 2
    }
    static forfun()
    {
        #!
        i = 20
        for i = 1
        {
            if i > 22
            {
                break
            }
            i = i+2
            CSharp.System.Console.WriteLine("for i= $i ")
        }
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
        #! 暂不支持 List,Map,Set,Queue,Link
        int i2 = 0;
        List list = List();        
        for it in list
        {

        }
        !#
        #!  暂不支持Array<int>
        Array<int> arr = [1,2,3];
        for v in arr{
            CSharp.System.Console.Write(" v= $v ")
        }
        !# 
        #!  暂不支持 Array<object>
        Array b = [{a=1}, {a=2}, {a = 3} ];        
        for v in b{

        }  
        暂不支持 for in range
        for v in 1..10
        {
        }
        暂不支持 for in range
        for v in EItType
        {

        }        
        !#
    }    
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
            CSharp.System.Console.WriteLine(" ioooo = $i ");
        }
        !#
        i = 10
        while
        {
            i++;
            CSharp.System.Console.WriteLine(" ioooo = $i ");
            if( i > 13 ){break;}
        }
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
        !#
    }
}