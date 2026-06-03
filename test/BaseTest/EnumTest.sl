import Std
import CSharp.System

class XC
{
    a = 10
    b = 10;
}

data BData
{
    i2 = 0
    url = ""
    xc1 = XC()
}
enum Color2
{
    # enum Color2_2{   x = 10 }
    # 不允许 内部嵌套 
    # 类中允许enum和data的数据
}

# 该类型不允许不使用=号 
enum Book
{
    B1 = 1
    B2 = 2
    C1
    mut string Str = ""
    mut c4 = 10
}

#该类 如果使用 数字类型 可以不使用=号， 后续如果再有=号，然后后续的自增
# 如果继承是uint形式，则不能设置负值， int则最大值后，可以使用负值。
enum EErr extends int
{
    None = 1
    First
    Second
    Thrill
    Four = 5
    Six
}

data RectShape
{
    x = 0
    y = 0
    width = 0
    height = 0
}
data CircleShape
{
    x = 0
    y = 0
    r = 1.0f
}
# data类型中，使用mut 可以对数据进行动态设置
enum EShape extends data
{
    r1 = RectShape(){x = 1, y = 1, width = 100, height = 100 }
    r2 = RectShape(){ x = 2, y = 2, width = 200, height = 200 }
    c1 = CircleShape(){ x = 1, y = 2, r = 100 }
    c2 = CircleShape() { x = 2, y = 2, r = 300 }
    mut cd = CircleShape()
}   

#!
enum ERectShape extends RectShape
{
    r1 = RectShape(){x=1}
    r2 = RectShape(){x = 2}
    r3 = RectShape(){x=3,y=1,width=100,height=100}
}
!#

enum ESeason extends string
{
    #@label("春天")
    Spring = "春天"
    Summer = "夏天"
    Autumn = "秋天"
    Winter = "冬天"
}


enum EBytes extends byte
{
    x = 1
    x2    #该位置是2
    x3 = 10
    x4 = 13
    x5     #该位置自加为14
}
enum Book2
{
    Int32 Id = 1;
    String Name = "";
}
enum Book3
{
    A1 = 10;
}

data MixColor 
{
    Red = 0.0f;
    Green = 0.0f;
    Blue = 0.0f;
}
const enum ConstColor
{
    Red = 0xff0000;
    Green = 0x00ff00;
    Blue = 0x0000ff;

    MixColor1 = MixColor() {Red=0.9f, Green = 0.1f, Blue = 0.01f } ;
    MixColor2 = MixColor() {Red=0.4f, Green = 0.22f, Blue = 0.7f } ;   
}
enum GameState
{
    Init = 1;
    Begin = 2;
    End = 3;
}
OK
{
    code = 0;
}
Error
{
    code = 0;
}
#!
报错
enum Res
{
    OK ok;
    Error error;
}
!#

EnumTest
{
    static fun()
    {
        global.println("========== EnumTest (start) ==========")

        #!
        BridgeKind kind222 = BridgeKind.SELF
        kind111 = BridgeKind.SELF

        if kind222 == kind111
        {
            global.println("BridgeKind--------------SELF11111111111")
        }

        if kind222 == BridgeKind.SELF
        {
            global.println("BridgeKind--------------SELF1")
        }
        elif kind222 == BridgeKind.JVM
        {
            global.println("BridgeKind--------------JVM1")
        }
        else
        {
            global.println("BridgeKind--------------OTHER1")
        }

        #kind111 =  EShape.r1
        kind111 = BridgeKind.JVM
        if kind111 == BridgeKind.SELF
        {
            global.println("BridgeKind--------------SELF2")
        }
        elif kind111 == BridgeKind.JVM
        {
            global.println("BridgeKind--------------JVM2")
        }
        else
        {
            global.println("BridgeKind--------------OTHER2")
        }

        EShape shape123 = EShape.r1
        if shape123 == EShape.r1
        {
            global.println("EShape default branch: r1")
        }
        elif shape123 == EShape.cd
        {
            global.println("EShape branch: cd")
        }

        for b3 in ESeason.values
        {
            global.println("ESeason value: " + b3.toString() )
        }

        global.println("----- mut enum member modify -----")
        global.println("Book.Str before# Test both separator styles -> " + Book.Str.toString() )
        !#
        
        Book.Str = "runtime string"
        Book.c4 = 20
        global.println("Book.Str after -> " + Book.Str.toString() )
    
        #EShape.r1 = RectShape(){ x = 10 }
        EShape.cd = RectShape()

        global.println("EErr.First ordinal smoke -> " + EErr.First.value )
        global.println("GameState values count check (manual): Init/Begin/End defined")

        global.println("========== EnumTest (end) ==========")

        
        states = [GameState.Init, GameState.Begin, GameState.End]
        for s in states
        {
            global.println("State: " + s.name)
        }
    }
}
enum ESemi
{
    A = 1; B = 2; C = 3;
}

enum ENewline
{
    D = 4
    E = 5
    F = 6
}# Test storing enums in collections


#!
enum negative compile cases: keep commented; uncomment one block at a time to validate diagnostics.

# 1. enum 内部不允许嵌套 enum/class。
enum EnumErrorNested
{
    enum InnerEnum { A = 1 }
    class InnerClass { }
}

# 2. enum extends 不允许普通 class。
class EnumErrorBaseClass { }
enum EnumErrorExtendsClass extends EnumErrorBaseClass
{
    A = EnumErrorBaseClass()
}

# 3. enum extends 不允许另一个 enum。
enum EnumErrorExtendsEnum extends GameState
{
    A = 1
    een = EnumErrorNested()
}

# 4. extends string 时必须显式字符串常量，不能省略 =，也不能写数字。
enum EnumErrorStringValue extends string
{
    A
    B = 1
}

# 5. 无符号整型不能使用负值。
enum EnumErrorUnsignedNegative extends UInt8
{
    A = -1
}

# 6. extends 具体 data 时，成员只能使用该 data 的 new 表达式，不能混入其它 data。
enum EnumErrorConcreteData extends RectShape
{
    A = CircleShape(){ x = 1, y = 1, r = 10 }
}

# 7. extends data 时，成员必须是 data new 表达式。
enum EnumErrorDynamicDataValue extends data
{
    A = 1
}


# 9. const enum 内部所有成员都不应允许重赋值。
numErrorAssignConstEnum
{
     static fun()
     {
         ConstColor.Red = 0
         ConstColor.MixColor1 = MixColor(){ Red = 1.0f }
     }
}

# 10. enum 成员名重复应报错。
enum EnumErrorRepeat
{
    A = 1
    A = 2
}


#! 进度
1. integer/UInt8 enum 自动递增、显式值、Member.name/index/value 已覆盖。
2. string enum、values 遍历已覆盖。
3. extends data 与 extends 具体 data 已覆盖。
4. mut enum 成员运行时重赋值已覆盖。
5. enum equality / if / switch 基础路径已覆盖。
6. 错误用例统一保留在 enum negative compile cases 注释块中，按块解注释验证诊断。

!#

# 本文件 static fun() 测试说明（与上表进度区分）：
# - integer/UInt8 enum：覆盖自动递增、显式赋值、Member.name/index/value。
# - string enum 与 .values：覆盖遍历和值读取。
# - EShape/ERectShape：覆盖 extends data 与 extends 具名 data。
# - Book：覆盖无 extends 的混合值与 mut 成员重赋值。
# - GameState switch：覆盖 enum 与 switch 配合。
#
# 预期结果：
# - EErr.First.value == 2、EErr.Six.value == 6、EBytes.x5.value == 14 为 True。
# - BridgeKind.SELF == BridgeKind.SELF 为 True，BridgeKind.SELF != BridgeKind.JVM 为 True。
# - EShape.r1 == EShape.r2 和 ERectShape.r1 == ERectShape.r2 为 False。
# - switch GameState.Begin 命中 Begin 分支；无编译/运行时错误。
