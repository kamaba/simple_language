import Application.Core;
import Application;

#namespace Application.P2;

 namespace OK
 {
    
 }  
namespace Application.N1
{
    namespace N2.N3
    {
        namespace N4.N5
        {
            Class1
            {
                X = 100;
                Class1_1
                {
                    Class_1_1
                    {
                        XX = 2;
                    }
                    XX = 100;
                }
            }
        }
        N4.ClassN4_1
        {
            
        }
    }
    namespace N2
    {
        Class1 extends N1.N2.N3.N4.N5.Class1
        {
            X2 = 200;
        }
    }
    Class1 extends Application.N1.N2.N3.N4.ClassTest22
    {

    }
    Class2 extends Application.N1.N2.N3.N4.N5.ClassTest15
    {

    }
    Class3 extends Application.N1.N2.N3.N4.N5.Class1.Class1_1
    {

    }
}