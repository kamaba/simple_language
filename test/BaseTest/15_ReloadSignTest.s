import Application;

public class Class1
{
    public int a = 20

    public static Class1 _reloadsign_( Class1 left, Class1 right, string sign )
    {
        Class1 c = Class1(){a = 0}
        if( sign == "+" )
        {
            c.a = left.a + right.a
        }
        else if( sign == "-" )
        {
            c.a = left.a - right.a
        }

        ret c
    }
}

ReloadClass
{
    static fun()
    {
        Class1 c1 = ()
        Class1 c2 = ()
        var c3 = c1 + c2
    }
}


# 系统重载符号  使用_reload_ 进行重载， 基本都是 left,right, sign 
# 可以重载 的符号有 + - * / % ** // += -= *= /= %= & |
# 重载需要进行类生成 
# 重载函数，需要进行语句解析时，进行多维函数生成