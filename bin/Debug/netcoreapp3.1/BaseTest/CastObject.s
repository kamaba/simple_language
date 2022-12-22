
public class Class1
{
    public static int a = 20;
    x1 = 0;
    Class11
    {
        public static Int32 GetX()
        {
            ret 20.0f.Cast<Int32>();
        }
    }
    public Cast( Template t )
    {
        if t.GetType() == Int32 
        {
            ret 300i;
        }
        elif t.GetType() == long
        {
            ret 0.4f;
        }
        elif t.GetType() == String
        {
            ret "abc";
        }
        ret this;
    }
}
class Class2 :: Class1
{
    x2 = 0;
    y2 = 0;
}

Class3
{

}

CastTObject
{
    static Fun()
    {
        #数字类型转换
        int a1 = 10;
        float a2 = 10.0f;
        double a3 = 10.0d;
        long a4 = 1000000;
        string a5 = "2323";
        string a6 = "mdme";
        b1 = a1.Cast<float>();  #在int类中，找Cast重载，查看Template 为float的方式
        b2 = a2.Cast<String>(); # 在float中找Cast重载, 查看Template == String的方式
        c = a1;   
        int c2 = c;  #相当于c.Cast<Int32>();
        double c3 = b2;  #相当于b.Cast<Double>();

        #类的转换
        Class1 cc1 = {};
        cf = cc1.Cast<Float>();    #查看Class1中的Template为Float型时的方法
        Class2 cc2 = {};
        Class1 cc3 = cc2;             # 父子类转掀 c2.Cast<Class1>(); 
        Class2 cc4 = c3.Cast<Class2>();     #从父类向子类转换，一般为空
        Class3 qst = {};
        Class2 cc5 = qst;    # 相当于qst.Cast<Class2>();
        int ga = GetX();   # 相当于  object.Cast<Int>();
        
    }
    GetX()
    {
        return 1.0f;
    }
}