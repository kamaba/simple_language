import Application.Core;

#namespace Application.MFC;


namespace N1
{
    Class1
    {
        a2 = 10;
    }
    Class2
    {
        a1 = 20;
    }
    Class1_2
    {
        Class1 c1;
        Class2 c2;

        _init_( Class1 x1 )
        {
            c1 = x1
        }
    }
    DynamicClass
    {
        static Fun()
        {
            dynamic c1 = {a = 10; c1 = Class1(); Class1_2 c2 = Class1_2( Class1() ){ c2 = Class2() } };

            re1 = c2.c2.c2.a1

            Console.Write("ret=" +　re1 );
        }
    }
}


!# 动态类说明


#!