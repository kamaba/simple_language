import Std
import CSharp.SimpleLanguage
import CSharp.System

public class Class1
{
    public int a = 20
    public int b = 10


    # C1 + C2  内置函数，传参，必须是Object类型，或者是不可以定义的类型  要在代码里边进行类型判断
    override Class1 _add_( Object obj1 )
    {
        if obj1 is Class1 c1
        {
            this.a += c1.a
            #this.b += c1.b + 10
            ret this
        }
        else
        {
            #输出错误 然后中断
            ret this
        }
    }
    # C1 - C2
    override Class1 _sub_( obj1 )
    {
        #this.a -= obj1.a
        #this.b -= obj1.b + 10
        ret this
    }
    # C1 * C2
    override Class1 _mul_( obj1 )
    {
        #this.a *= obj1.a
        ret this
    }
    # C1 / C2
    override Class1 _truediv_( obj1 )
    {
        #this.a /= obj1.a
        ret this
    }
    # C1 % C2
    override Class1 _mod_( obj1 )
    {
        #this.a %= obj1.a
        ret this
    }
    # C1 += C2
    override Class1 _iadd_( obj1 )
    {
        #!
        if obj1 != null && obj1.a == 30
        {
            ret true
        }
        !#
        ret null
    }
    # C1 *= C2
    override Class1 _imul_( obj1 )
    {
        ret null
    }
    # C1 /= C2
    override Class1 _itruediv_( obj1 )
    {
        ret null
    }
    # C1 < C2
    override bool _lt_( obj1 )
    {
        ret false
    }
    # C1 <= C2
    override bool _le_( obj1 )
    {
        ret false
    }
    # C1 > C2
    override bool _gt_( obj1 )
    {
        ret false
    }    
    # C1 >= C2
    override bool _ge_( obj1 )
    {
        ret false
    }
    # C1 == C2
    override bool _eq_( obj1 )
    {
        if obj1 is Class1 c1
        {
            if c1.hashCode == this.hashCode
            {
                ret true
            }
            ret false
        }
        else
        {
            ret true            
        }
    }
    # C1 != C2
    override bool _ne_( obj1 )
    {
        if obj1 is Class1 c1
        {
            if c1.hashCode == this.hashCode
            {
                ret false
            }
            ret true
        }
        else
        {
            ret true            
        }
    }

    override string toString()
    {
        ret (this.a + this.b).toString()
    }
}

ReloadSignTest
{
    static fun()
    {
        global.println("========== ReloadSignTest (start) ==========")
        Class1 c1 = new(){ a = 100}
        Class1 c2 = new(){ a = 300, b = 2000 }
        var c3 = c1 + c2

        global.println("c1 + c2 -> " + c3.toString())
        global.println("========== ReloadSignTest (end) ==========")
    }
}


# 系统重载符号  使用 _add_,_sub_ 等函数进行重载   
# 可以重载 的符号有 + - * / % ** // += -= *= /= %= & |
# 重载行为是动态的行为，所以在重载传参，必须定义为Object类型，如果需要，要在代码里边进行类型判断后，再进行操作
# 重载函数，需要进行语句解析时，进行多维函数生成
# Array类型，可能比较特殊，因为有些代码要在底层操作，所以需要单独出来处理

# static fun() 测试说明：Class1 上 _add_ 合并两个实例字段；toString 输出 a+b 之和。
# 预期：c3.a 为 400（100+300），toString 为 "400" 或实现定义格式；无未实现运算符路径。
