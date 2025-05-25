import Application;

public class Class1
{
    public int a = 20

    public static Class1 default = Class1(){a = 0}

    public static Class1 _reload_( Class1 left, Class1 right, string sign )
    {
        Class1 c = Class1(){a = 0}
        if( sign == "+" )
        {
            c.a = left.a + right.a
        }
        ret c
    }
}


public class Level1<T> 
{
    T t1 = T.default;
    T t2 = T.default;

    public T add()
    {
        ret this.t1 + this.t2
    }    

    public static T2 min<T2>( T2 t1, T2 t2 )
    {
        ret t1 ? t1 > t2 : t2
    }
}

GenClass
{
    static fun()
    {
        Level1<int> l1 = Level1<int>()
        l2 = l1.add()
        Console.Write( "Addresult: " + l2 )

        float a = Level1.min<float>( 1.3, 2.5 )

        Console.Write("Flaoat" + a )
    }
}
#!
public class ListC<T>
{
    Array<T> m_Array = Array<T>(4);
    
    public T getIndex( int index )
    {
        if index < 0 || index > count
        {
            ret null
        }

        ret m_Array[index]
    }
}
!#


# 系统重载符号  使用_reload_ 进行重载， 基本都是 left,right, sign 
# 可以重载 的符号有 + - * / % ** // += -= *= /= %= &&
# 重载需要进行类生成 
# 重载函数，需要进行语句解析时，进行多维函数生成