import CSharp.System
import CSharp.SimpleLanguage.Core

StringTest
{
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
        System.Console.WriteLine(str6)

        #!
        输出:我是一段话 我叫Quta
        我\n今天 考了55分 "大QQQ" 'xml' \" \t
        
        End 
        这f""" 这种情况，直接识别里边的所有内容，不对 \符号进行处理，而是直接识别文本编译器中的内容 但对$var ${} 要进行识别
        !#
    }
}