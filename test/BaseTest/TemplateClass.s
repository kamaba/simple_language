#import Application.Core;
#namespace Application.MFC;
import Q;

public class Class1
{
    public static int a = 20;
    x1 = 0;
}
Interface1
{
    interface void itf1(){}
}
Interface2
{
    interface void itf2(){}
}
class Class2 :: Class1 interface Interface1, Interface2
{
    void itf1(){}
    void itf2(){}
    x2 = 0;
    y2 = 0;
}
ClassT2<T>
{
    T t;
    #List<T> tlist;
}

ClassT2_2<T> :: ClassT2<T> interface Interface1, Interface2
{
    T t2;
    void itf1(){}
    void itf2(){}
}


Q.Map<T1,T2>
{
    T1 t1;
    T2 t2;
    a = 0;
    Init( T1 key, T2 value )
    {
        this.t1 = key;
        this.t2 = value;
        this.a = 15;
        b = 20;
        c = 25;
    }
}

List<T>
{
    List( T _t )
    {
        this.t = _t;
    }
    a = "";
    T t;
    #Array<T> marr;

    string ToTest( T t )
    {
        a = 20;
        if true   #T.GetType() == Int32.GetType()
        {
            return "Int32";
        }
        else if( false )
        {
            return "Float";
        }        
        return 2i.ToString();
    }

    Cast( Template t )
    {
        
    }
}
Test<T>
{
    T t1;
    int cn = 0;
    _init_()
    {

    }
    _init_( int n )
    {
        this.cn = n;
    }
}

TemplateObject
{
    static Fun()
    {
        static List<List<List<int>>> alist = List<List<List<int>>>();
        Map< string, Map<int, Map<int, int> > > amap = Map<string, Map<int, Map<int, int> > >();   
        ClassT2_2<int> ct22 = ClassT2_2<int>(); 
        ct2 = ClassT2<string>();
        ClassT2 glct2 = ClassT2_2<Class2>();

        
        Test<int> te1 = Test<int>();
        te2 = Test<float>(2);
        Test<Test<int>> te2 = Test<Test<int>>(2);
        Test<double> te3 = (30);  
        
    }
}