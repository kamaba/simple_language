# S 语言的数据类
数据类，是类的扩展，但它有自己的一些特性，可以理解为纯静态数据对象，但在输入代码时，比较像json的简化形式，不可以使用外部数据，只允许使用基础支持格式化输入。

—————————————————————————————————————————————————————————

## 数据类的定义 
数据类，需要使用data关键字，与class/enum同级别，可以使用const限制，不允许使用其它限制关键字。

in the file test.sp
```python
data Book  #定义数据
{
    #member variables
    name = "我的天空";
    desc = "描述不可描述的事情!";
    rent = 
    { 
        manager = "X"; 
        time = "2012-10-12"
    }
    author= ["Lif", "Paper", "Mango"];
    buy = 
    [
        {
            time="2022-12-12"; 
            price=25;
        },
        {
            time="2021-10-10"; 
            price = 20;
        },
        {
            time="2020-12-20"; 
            price=22;
            sex = 1;
        }
    ] 
}
class BookClass    #也可以使用 class关键字
{
    class Rent{
        manager = "";
        time = "";
    }
    class BuyData
    {
        time = "";
        print = 0;
        sex = 1;
    }
    name = "书名";
    desc = "描述";
    Rent rent = null;
    string[] author;
    Array<BuyData> buyArr;
}

import CSharp.System;
ProjectEnter
{
    static Main()
    {
        Console.Write( Book );      #把Book当格式化输入字符

        BookRef = Book;                 #把Book 再作用引用 BookRef

        Book.name = "新书名";           #改Book里边的数据

        stream = Book.ToStream();

        BookClass bc = Book.ToClass<BookClass>();

        Book bk = bc.ToData<Book>();

        if Book.name == "OK" 
        {
            Console.Write("书名为OK");
        }    

        #Json bookJson = Book.ToJson();
        #Xml bookXml = Book.ToXml();
        #Yaml bookYaml = Book.ToYaml();

        #Json njson = Json(){"BookJson":{"name":"ok","desc":"hahahahaha"}};
        #Book bdata = njson.ToData<Book>();  有数据损失,使用默认数据。
    }
}
```
### 数据类型的使用方式
- 通过data字段字义该类为数据类型，并且，使用 {},=[], name = "val"等方式进行赋值
- 可以在data前边使用const关键字，进行锁定，不允许修改内容
- data使用后，在语句中可以直接使用例   print( "book name: @Book.name" ); 
- data 如果不使用const 可以对其进行修改 例: Book.name = "新华字典";
- 可以使用引用方式进行赋值 例  BookRef = Book; 此时，BookRef像普通变量一些，和Book使用方式相同;
- 数据，可以转化为数据流方式 stream = Book.ToStream();  stream 是独有的Stream类型，可以直接传入class中，赋值
- 数据可以与类进行转化，但有可能有不匹配的数据丢失， BookClass bc = Book.ToClass<BookClass>(); 
- 类也可以向数据转化 BookClass bc = BookClass(); bd = bc.ToData<Book>(); bd则为数据类的变量。