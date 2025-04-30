import Application;

namespace N1
{
    namespace N2.N3
    {
        partial Class3 extends Class1
        {
            X2 = 100;
        }
    }

    Class4 extends N2.N3.Class3
    {
        X41 = 100;
        X42 = 100;
    }
    namespace N3
    {
        Class5 extends Class4
        {
            X52 = 100;
            X53 = 100;
        }
    }
}