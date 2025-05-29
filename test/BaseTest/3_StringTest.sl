import CSharp.System
import CSharp.SimpleLanguage.Core

public class Float
{    
    public string toString()
    {
        string str = ""
        str = SelfMeta.FloatMetaClass.MetaToString( this )
        ret str
    }
}
public class String
{
    format( params object[] obj )
    {
        ret "mm"
    }
}
StringTest
{
    Class2
    {
        int a = 0;
    }
    Class1{
        a1 = 20;
        Class2 c2 = null
        static string printf( string x )
        {
            Console.WriteLine("---------------------: " + x );
            ret "m";
        }
    }
    static fun()
    {
        #a1 = "aabcc";
        #a2 = "aaacc" + "deee";
        a3 = '{"name":"okr", "age":13, "info":{ "map":[1,2,3], "seq":"\n\n"        
        } }' + 3;
        #a4 = "aa" + 20.0f.toString();
        #Class1.printf(Class1.printf( a1 ));
        #Class1.printf( a2 );
        #Class1.printf( a3 );
        #Class1.printf( a4 );
        
        #int a = 1;
        #int b = 2;
        
        a41 = "{0} this is {1} that a {2}".format( "mum", "skirt", "big" )
        Class1.printf( a41 );
        #a42 = "{} this a {} ".format("qq", "girl" )

        #a5 = "${ a+b+(3+10+"aadf").toString() }";    #{}任何时刻，都表示可以执行自己的内部语句  

        #!
        a6 = "print a=$a4 ";
        

        Class1 c1;
        a7 = "print c1.a1=$c1.a1 ";
        a8 = " /nskemsikeaae/t/r' ";
        a9 = "asdfasdf{a4} $c1.a1 ";
               
        c1 = 'a';
        c2 = "a";
        string c3 = 'a';   #相当于'a'.Cast<String>();
        c4 = 'a' + 'b';   # 相当于  (int)a + (int)b;
        c5 = 'aaaaa';       #报错
        c6 = "aaaaaa";   
        #!
        !#
    }
}

# 一般使用"" 符号定义该段是字符串类型
# 也同时可以使用'' 的方式  如果使用这种方式，一般支持使用 "" 这种格式的直接输出 比如 ' { "name":"kewang", "age":20, "to":"nime" }'
# 如果使用 "${}"  则括号内 一般和外边的语句类型，需要提到外边一个参数，或者变量，然后执行
# 如果使用 " $a1 " 这种方式，则直接可以读取某位置的变量
# 一般格式化字符可以使用 " {0} this is {1} that a {2}".format( "mum", "skirt", "big" ); 没有使用$里边添写 {0}, {1}, {2} 可以进行替换  如果是空值，则自动添加顺序 