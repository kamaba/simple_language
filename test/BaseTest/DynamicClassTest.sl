import Std;
import CSharp.SimpleLanguage
import CSharp.System

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
        Class1 c1 = null;
        Class2 c2 = new();

        _init_( Class1 x1 )
        {
            this.c1 = x1
        }
    }
    DynamicClassTest
    {
        static fun()
        {
            global.println("========== DynamicClassTest (start) ==========")
            dynamic c1 = { a = 10, c1 = Class1(), Class1_2 c2 = Class1_2(Class1()){ c2 = Class2() } }
            #data c2 = {a=2,b="ace",N1.Class1 c1 = (){ a2 = 10}, arr1 = [1,2,3,4,5] }
            re1 = c1.c1.a2
            global.println("dynamic nested Class1.a2 -> " + re1.toString())
            global.println("========== DynamicClassTest (end) ==========")
        }
    }
}


#! 动态类说明
1. 动态类，必须以dynamic关键字作为开头
2. 如果没有开头直接写，则认为 是data 匿名数据为开头的处理
3. 动态类可以通过 DynamicClass.addMemberVariable( c1, int, a,  10 );  或者 是定义结构体 
    MemberVariable cv;
    cv.defineType = int.type;
    cv.name = "hingo"
    cv.isStatic = false;
    cv.isConst = false;
    cv.pression = pression.public; 
    cv.express = new ConstExpress(10);
    DynamicClassaddMemeberVariable( c1, cv );
4. 匿名数据，同样的，可以通过 DynamicData.addMemeberData( c2, MemberVariable(int.type, 10 ) )
5. 匿名基本是可改，和操作的 基本都是通过 DynamicClass.removeMemeberData( c1, "hingo" );
!#