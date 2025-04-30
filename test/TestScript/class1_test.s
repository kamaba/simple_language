import Application.Core;

namespace Application.MFC;

Class1
{

}
Application.Class1
{

}

namespace C1
{
    namespce C2.C2_2
    {
        Class1
        {
            Class1_1
            {

            }
        }
        namespace C3
        {
            namespace C3_1
            {
                Class3 :: C2.C2_2.Class1.Class1_1
                {

                }
            }
        }
    }
    Class2
    {

    }
}

partial CC1.C3.C3_1.Class3
{
    X1 = 100;
}