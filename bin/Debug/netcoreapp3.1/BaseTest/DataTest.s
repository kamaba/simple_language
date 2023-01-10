#import CSharp.System;

data RenBook
{
   name = "人";
   desc = "描述不可描述的事情";
   rent
   {
        rent_person = "刘";
        rent_time = "2011-12-20";
        rent_day = 7;
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
data Book
{
    name = "bookName";
    desc = "";
    Rent
    {
        RentPerson = "";
        RentTime = "";
        RentDay = "";
    }
}
DataTest
{
    static Func()
    {
        #println( RenBook );               #为直接数据

        #renBookClass = RenBook.ToClass<Book>();  #转化为Book Class;
        #Json bookJson = RenBook.ToJson();       #转化为Json格式
        #Xml bookXml = RenBook.ToXml();
        #Yaml bookYaml = RenBook.ToYaml();
        #Bin bookBin = RenBook.ToBin();

        #RenBook2 = RenBook.Clone();             #支持Clone

        RenBook3 = RenBook;                     #引用RenBook  RenBook3 是RenBook的引用

        #RenBook rb = RenBook.Clone();          #数据只能使用Clone方法复制

        #RenBook rrv1 = renBookClass.ToData<RenBook>();  #数据也可以使用ToData<T>()方法转回，如果有不对应问题，则报错，如果发现数据不全多覆盖少覆盖问题，则提示!

        RenBook.name = "啥啥";   #如果data前不带const标识，则可以修改里边的成员

        RenBook3.name = "是否";

        # 如果data数据向函数传参，则会自动变为匿名类，然后传值,所以每个data也会生成一个匿名类

        if RenBook.name == "liu"
        {

        }
    }
}