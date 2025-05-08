#import CSharp.System;

const static data RenBook
{
   name = "人";
   desc = "描述不可描述的事情";
   data static rent
   {
        rent_person = "刘";
        rent_time = "2011-12-20";
        rent_day = 7;
   }
   r2 = {
        renp1 = "liu",
        time = "2011-12-12"
   }
   buy =
   [
    {
        user = "liu";
        time = "2012-12-12";
        price = 20;
    },
    {

    }
   ]
}
data SimBook
{
    name = "ih nnh ";
    desc = "dscess";
}
data Book extends SimBook
{
    Rent = 
    {
        RentPerson = "";
        RentTime = "";
        RentDay = "";
    }
}
data BookCatelogy
{
    Array<Book> bookArray;
}
#!
BookClass bind BookCatelogy,SimBook
{
    toJson()
    {
        return '{ "name":$this.name ,"desc":$this.desc }'
    }
}
!#

data Vector3
{
    float x = 0.0f
    float y = 0.0f
    float z = 0.0f
}

Class1{
    int a = 20;
    int b = 20;
}
#@api = webapi()
DataTest
{
    #@api.get("/")
    root()
    {
        return '{"a":20,"b":30,"message":"ok"}';
    }
    #@api.get("/readbook" )
    string readBook()
    {
        ret Book.simbook.json.toString()  
    }

    static GetD1()
    {
        return { a= 20, b = Class1() }
    }
    static GetD2()
    {
        return {a=100, b = 200}   # 返回的是匿名data数据
    }

    static Vector3 Input( BookCatelogy bc )
    {
        Vector3 v3 = Vector3()   #和 Vector3.clone()  效果一样
        v3ref = v3;             #引用指针
        v3ref2 = v3.ref         #效果一样
        v3data = v3.data;       #数据实体拷贝

        ret v3.data             # 返回 的为实体
    }
    static Func()
    {
        #println( RenBook );               #为直接数据

        Input( BookCatelogy.data );         #直接传参为数据，而非引用指针  直接传BookCatelogy

        #renBookClass = RenBook.toClass<Book>();  #转化为Book Class;
        #Json bookJson = RenBook.toJson();       #转化为Json格式
        #Xml bookXml = RenBook.toXml();
        #Yaml bookYaml = RenBook.toYaml();
        #Bin bookBin = RenBook.toBin();
        SimBook rb              # 使用data rb 是一个新的SimBook 的实体 等同于

        rb1 = SimBook.clone()       # 对SimBook进行复刻

        rb2 = rb1.ref               #对于rb1的引用，不复刻，只是引用数加1

        #RenBook2 = RenBook.copy();             #支持Clone

        RenBook3 = RenBook.ref();                     #引用RenBook  RenBook3 是RenBook的引用

        #RenBook rb = RenBook.clone();          #数据只能使用Clone方法复制

        #RenBook rrv1 = renBookClass.toData<RenBook>();  #数据也可以使用ToData<T>()方法转回，如果有不对应问题，则报错，如果发现数据不全多覆盖少覆盖问题，则提示!

        RenBook.name = "啥啥";   #如果data前不带const标识，则可以修改里边的成员

        RenBook3.name = "是否";

        # 如果data数据向函数传参，则会自动变为匿名类，然后传值,所以每个data也会生成一个匿名类

        if RenBook.name == "liu"
        {

        }
        data data1 = {a = 20,b = 30, c = [{ a = 1, cc = 3}, { n = 1 }, array(1,2,3,4,5,6) ] };  
        #与dynamic 区别 data里边，可以使用[]的符号,不允许特殊表达式的写入
        

        str = Json({ a = 20, b = 30, message = "ok" }).toString() #匿名数据 可以通过传给json转string
        # 想当于 {"a":20,"b":30,"message":"ok"} 
        # 如果是 Json5({ a = 20, b = 30, message = "ok" }).toString() 
        # 相当于 { a : 20, b : 30, message:"ok" }
    }
}


## data 写法  {  var1name = 内容 }
    - 内容支持 const  number/