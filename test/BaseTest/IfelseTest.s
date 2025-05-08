IfelseTest
{
    Class1
    {}
    Class2
    {
        i = 0
    }
    static Func()
    {
        m = 0;
        if m >= 0 && m < 100 && m != 50
        { 
            m = 20; 
            {
                if m == 14{ m = 10 }
                if m== 20 { m = 20}
                else{ m = 30 } 
            }
        } 
        else
        { m = 20 }
        
        if true 
        {
            int x1 = 200;
        }
        elif 2 == 120 && false 
        {
            x2 = 300;
        } 
               
        a = 30

        if a==25 
        { 
            {
                m = 20
            }
        } 
        elif a==30 && a < 35 
        {  } 
        elif a==31
        { m = 100 }
        
        if true
        {
            int ax = 2000;
        }
        
        
        if false 
        {
            int xxx = 100;
        }
        ab = 1;
        if  ( true )
        {
            a = 20;
        } 
        else
        {
            b = 30;
        }   
        a = 10;
        if( true )
        {
            a = 20;
        }    
        elif( a == 20 )
        {
            if( a/10 == 2 )
            {
                a = 30;
            }
        }    
        elif( a == 40 )
        {
            a = 300;
        }    
        else
        {
            a = 10;
        }
        
    }
    static FunIfCondition()
    {
        #这部分还没有实现
        a = null;
        if a       #等于 if a != null
        { 
        }
        if !a    #等于 if a == null
        {
            a = Class1();
        }

        c1 = Class1();
        Class2 c2 = c1;
        if c2
        {
            c2.i = 10;
        }
        if c2.i    #相当于 c2 != null && c2.i != 0
        {

        }
        int ai = 0;
        if ai    #如果数字型 则判断是否为int/short/long != 0 float/double = 0.0 string = ""  先判断是否有对象 ai != null 确定int后查看 0
        {

        }
        var a = Claas1();

        int a = if true
        { tr 20 }
        else
        {
             tr 10
        }    #局部返回 tr  像 if/switch/while/dowhile/for/for in/

        a = 100 ? a == 10 : -100;
    }
}
       