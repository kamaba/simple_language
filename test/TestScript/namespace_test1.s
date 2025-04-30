import Application.Core;
import Application;

namespace Application.P2;
   
namespace Application.P1
{
    namespace N1.N1_1
    {
        namespace N2.N2_2
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
    }
    namespace N2
    {
        Class1 :: P1.N1.N1_1.N1_1_1.NF11.Class1
        {
            X = 200;
        }
    }
    Class1 :: P2.N1.N1_1.N22.Class1
    {

    }
    Class2 :: Application.P2.N1.N1_1.N22.Class1
    {

    }
    Class3 :: P1.N1.N1_1.N2.N2_2.Class1.Class1_1
    {

    }
}