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
        fstring = f"{}";
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

        
    ClassT{
        string name = "xxx"
    }
    static fun()
    {
        name = "QuTa"
        score = 55
        a = 100
        b = 300
        #cstr = (a+b).add( (b+(score-20)) ).toString()
        
        #System.Console.WriteLine("name=" + cstr )
        
        #str1 = string.toFormat( "Name:{} Score:{} ", name, score )        
        #System.Console.WriteLine(str1)
        #输出 Name:Quta,Score=55

        
        #str2 = "Name:{1},Score:{0}".format( (score+1)+(3*5), name+"_xx" )
        #System.Console.WriteLine(str2)
        #输出 Name:Quta_xx,Score=71

        ct = ClassT(){ name = "mmm" }
        
        #!
        #在""中对$var 的识别  #像这种的的会被解析成 string.format( "Name:{} Score={} a+b={}", name, score, (a+b).toString() )
        str3 = "{} Name:$ct.name {} Score:$score a+b=${(a+b).toString()} bb = {}" 
        str33 = str3.format(name, score, "bb");
        System.Console.WriteLine(str3.format(name+"---", score-50, "bbxxxx"))
        System.Console.WriteLine(str33)
        #输出 Name:Quta Score=55 a+b=400
        !#

        #!
        #再复杂一点的
        str4 = "Name:$name Score:$score a[{}]+b[{}]=${(a+b).toString()}".format(a, b ) 
        #像这种的的会被解析成 string.format( "Name:{} Score={} a[{}]+b[{}]={}", name, score, a, b, (a+b).toString() )
        System.Console.WriteLine(str4)
        #输出 Name:Quta Score=55 a[100]+b[300]=400
        !#
        
        str5 = 'Name:\'$name\' NickName="AQ" Score=$score a{}+b{}=${(a+b).toString()}'
        System.Console.WriteLine(str5)
        #输出 Name:'$name' NickName="AQ" Score=$score a{}+b{}=${(a+b).toString()}

        str6 = f"""我是一段话 我叫${name}
        我\n今天 考了${score}分 "大Q$name QQ" 'xml' \" \t
        
        End"""    
        System.Console.WriteLine("----- " + str6)

        #!
        输出:我是一段话 我叫Quta
        我\n今天 考了55分 "大QQQ" 'xml' \" \t
        
        End 
        这f""" 这种情况，直接识别里边的所有内容，不对 \符号进行处理，而是直接识别文本编译器中的内容 但对$var ${} 要进行识别
        !#
    }
    }
}

# 一般使用"" 符号定义该段是字符串类型
# 也同时可以使用'' 的方式  如果使用这种方式，一般支持使用 "" 这种格式的直接输出 比如 ' { "name":"kewang", "age":20, "to":"nime" }'
# 如果使用 "${}"  则括号内 一般和外边的语句类型，需要提到外边一个参数，或者变量，然后执行
# 如果使用 " $a1 " 这种方式，则直接可以读取某位置的变量
# 一般格式化字符可以使用 " {0} this is {1} that a {2}".format( "mum", "skirt", "big" ); 没有使用$里边添写 {0}, {1}, {2} 可以进行替换  如果是空值，则自动添加顺序 