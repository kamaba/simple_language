import Std
import CSharp.System

namespace Core
{
    class Object
    {
        public void _init_()
        {

        }

        public string toString()
        {
            ret ""
        }
    }
    class Byte
    {
        
    }
    class Boolean
    {

    }
    class SByte
    {
        
    }
    class Int16
    {
        
    }
    class UInt16
    {
        
    }
    class Int32
    {
        
    }
    class UInt32
    {
        
    }
    class Int64
    {
        
    }
    class UInt64
    {
        
    }
    class Float32
    {
        
    }
    class Float64
    {
        _init_(Float64 f)
        {

        }
    }
    class String
    {
        _init_( String str )
        {

        }
    }

}

Level1
{
    Level1_var1 = 20
}
Level2 extends Level1
{
    Level2_var1 = 30;
}
Level3 extends Level2
{
    Level3_var3 = 40
}

LevelT<T1>
{
    T1 levelT1_var1 = new()
}
LevelT2<T21,T22> extends LevelT<T22>
{
    T22 levelT2_var1 = new()
}
LevelT3<T31,T32> extends LevelT2<T32,T31>
{
    T31 levelT3_var1 = new()
    #!
    _cast_<TargetT>()
    {
        ret this as TargetT
    }
    !#
}


ClassAs_Is{
    static fun()
    {
        int a = 100
        b = "abc"
        if a is string str 
        {
            System.Console.WriteLine("_int is  " + str )
        }
        elif b is string str2 
        {
            System.Console.WriteLine("_int is2  " + str2 )
        }
        #!
        if a is string str || b is string str2  #这种语法错误 ，如果使用as/is 必须只有一句处理
        {
            System.Console.WriteLine("_int is  " + str )
            System.Console.WriteLine("_int is2  " + str2 )
        }
        !#
        #!
        LevelT3<int,string> levelt3 = new()
        levelt3.levelT3_var1 = 100
        #LevelT3<int,int> levelt3_2 = levelt3 as LevelT3<int,string>        
        levelt3_2 = levelt3 as LevelT3<int,string>
        if levelt3_2 != null
        {
            System.Console.WriteLine("_this_——————————————————————————————  " +levelt3_2.levelT3_var1 )
        }
        else
        {
            System.Console.WriteLine("aanonnnhooooooooo  "  )
        }

        LevelT2<int,int> levelt2 = new()
        levelt2.levelT2_var1 = 1112
        if levelt2 is LevelT2<int,int> levelt3_22
        {
            System.Console.WriteLine("_this_——————————2222222222  " + levelt3_22.levelT2_var1 )
        }

        Level3 level3 = new()
        Level1 level1 = level3
        level2 = level1 as Level2
        
        #var aaa = level1.Level3_var3        #该语句需要报错，因为已经有定义过的类型，所以即使可以计算出来真实的类型，也不能直接使用
        
        if level2 != null
        {
            System.Console.WriteLine("_this_——————————————————————————————  " +level2.Level2_var1 )
        }
        else
        {
            System.Console.WriteLine("aanonnnhooooooooo  " + level1.Level1_var1 )
        }

        bool flag = level2 is Level3 level3ttt
        if flag
        {
            System.Console.WriteLine("is ok  " + level3ttt.Level3_var3 )
        }
        else
        {
            System.Console.WriteLine("is No  " )
        }

        if level1 is Level3 ll3if
        {
            System.Console.WriteLine("yes l3 ="  + ll3if.Level3_var3 );
        }

        #!
        l1castl3 = level1.cast<Level3>()
        if l1castl3 != null
        {
            Console.WriteLine("yes l1cast l1castl3=" );
        }
        !#
    }
}

#!
关于 as is 的处理
1. 如果使用as 则动态的，检查 是否是继承类的关系，如果是，则运行当前值，不进行为Null处理，否则相反处理 当前值为Null
2. 使用Is 分两种情况，一种是 不带后边的变量，这种的，直接返回一个boolean形的类型
3. 如果使用带后边变量的，逻辑，则是，先进行as 处理，变成当前变量，然后进行赋值，再去判断当前的赋值是否为Null，如果是，则返回boolean变量的true 相反为false
4. 如果cast函数，要从系统函数中，进行 as操作 返回返回到当前的变量 如果已经定义过cast则要优先走cast函数，

以后的是以后再去考滤的问题，现阶段先不动
5. 该函数为_cast_，是个系统定义方法，返回值必须是当前的类类型，如果定义是不是当前，则为出错，该方法是无法final的方法
，里边可以处理一些内容，也必须要有返回值 如果没有检测到系统的，则直接进行as处理
6. 比如如果 我这个类是继承的A类，A类里边有个值是Uint16的，这时候，我转成B类，可以要对A类的

发现的问题:
1. 如果前置定义过类型，在编译阶段，需要先检查定义类型里边的方法，而不是实际的方法
2. 在括号内的语句没有执行
!#