# S语言的enum
枚举类，是类的扩展，但它有自己的一些特性，可以理解为纯静态类，但在输入代码时，如果定义了enum类型，则可以在方法中直接使用该定义，可以遍历，可以访问，可以修改，可以新加引用对象等操作。

—————————————————————————————————————————————————————————

## 枚举类的定义 
枚举，需要使用enum关键字，与class/data同级别，可以使用const限制，不允许使用其它限制关键字。

相关代码 in the file test.sp
```python
import CSharp.System;

class XC
{
    a = 10;
    b = 10;
}
enum Book
{
    B1 = { int i2 = 20, string url = "http://www.baidu.com", XC xc1= XC(), anonC = { string name = "xx", age = 20 } };
    C1 = 1;
    string Str;
}
MixColor
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

    MixColor1 = MixColor(){Red=0.9f, Green = 0.1f, Blue = 0.01f };
    MixColor2 = MixColor(){Red=0.4f, Green = 0.22f, Blue = 0.7f };
}
enum Option<T>
{
    T Some;
    None;
}

ProjectEnter
{
    static Main()
    {         
        for b3 in GameState
        {
            Console.Write("Book3: " + b3 );
        }
        
        Book3 b3 = Book3.A1;
        Book3 b3_3 = Book3.A1( 20 );
                
        book = Book.B1({ i2 = 20, url = "mas" });
        switch book
        {
            case Book.B1 b1
            {
                Console.Write( "bi2:" + b1.i2 );
            }
        }

        Book eb = Book.B();
        if eb == Book.B
        {
            Console.Write( eb.ToString() );
        }
        Option op = Option.Some<String>("hello");
        switch op{
            case Option.Some sot:
            {
                Console.Write("something=" + sot.ToString() );
            }
        }        
    }
}
```
### 枚举的使用方式
- 通过enum字段字义该类为数据类型，并且，使用 {}, name = "val"等方式进行赋值，一般{}指的是匿名类
- 可以在enum前边使用const关键字，进行锁定，不允许修改内容
- enum使用方式，例: enum E1{ value1 = 1; value2 = 2; value3 = {a = 10} E1 e1 = E1.value1;的方式直接使用。 
- enum 如果不使用const 可以对其的值进行修改， 例 E1 e1 = E1.value1( 20 ); 如果const，则语句报错.
- enum 中支持动态类型，使用{}表示，内容必须有值，不能为空，否则报错，赋值与普通自带类型相同， E1 e1 = E1.value3( {a=20} );的方法赋值。
- 使用T模版类型，与普通传值一样，但在switch中，有带类型判断。
- for可以对enum直接进行遍历，遍历是enum的定义名称。
- switch 可以对enum的定义名称，甚至是定义名称下的值进行匹配。
- 在enum定义名称中，自带一个index字段，表明其位置，可以在 e1.index的方法查看其值，与定义的value值是不相同的，当然也可以值index来做序列号等作用。