import CSharp.System;

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
    #enum Color2_2{   x = 10 }
    # 不允许 内部嵌套 
    # 类中允许enum和data的数据
}

# 该类型不允许不使用=号 
enum Book
{
    B1 = BData()
    {
         i2 = 20, 
         url = "http://www.baidu.com", xc1= XC() 
    }
    B2 = BData(){ i2 = 10}
    C1 = 1
    mut string Str = ""
    C4 = 10
}

#该类 如果使用 数字类型 可以不使用=号， 后续如果再有=号，然后后续的自增
# 如果继承是uint形式，则不能设置负值， int则最大值后，可以使用负值。
enum EErr extends int
{
    None = 1
    First
    Second;Thrill;
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

enum reson extends string
{    
    #@label("春天")
    Spring = "春天"
    Summer = "夏天"
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

#! 暂不支持该模式，以后可考滤，直接继承某个数据结构
enum ERectShape extends RectShape
{
    r1 = RectShape(){x=1}
    r2 = RectShape(){x = 2}
    r3 = RectShape(){x=3,y=1,width=100,height=100}
}
!#


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
    static func()
    {
        EShape shape = EShape.r1;

        if shape == EShape.r1
        {

        }
        elif shape == EShape.cd
        {
            EShape.cd = CircleShape(){ x = 100, y = 100, r = 1000 }
            #这里变了，则shape也会变  不能直接使用shape复制
            #r = shape.cast<CircleShape>().r  # r=1000
        }

        
        for b3 in reson.values
        {
            Debug.Write("Book3: " + b3 );
        }
        #这里要进行重写values 要解析完GameState后， 调用values时，需要进行重建 array<GameState extends> 然后存一个变量
        

        
    }
}

#! 进度
1. 上边除了T的enum没有实现，其实的都验证通过
2. 没有在vm中没有实现enum相关的内容
3. enum的new没有具体的分配相关的逻辑
4. enum的.ToString() 没有实现。
5. 如果使用enum 未使用extends ，则内部可以直接使用除enum,class之外 的任意类型，比如 byte/sbyte/int/uint/short/ushort/long/ulong/data
6. 如果使用上边的某种类形，则必须使用该类型，如果使用byte-ulong之间的类型，则里边可以不使用=号，然后自增
7. 如果使用了data,则内部必须都使用定义过的data类型
8. 使用for in 可以遍历 enum内部
9. 

!#