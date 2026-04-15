import Std
import CSharp.System

StringTest
{
    Class2
    {
        int a = 0
    }

    Class1
    {
        a1 = 20
        Class2 c2 = null

        static string printf(string x)
        {
            global.println("StringTest.printf: " + x)
            ret "m"
        }
    }

    ClassT
    {
        string name = "xxx"
    }

    static literalAndFormatSmoke()
    {
        global.println("----- literalAndFormatSmoke -----")
        a3 = '{"name":"okr","age":13}' + 3
        Class1.printf(a3)

        a41 = "{0} this is {1} that a {2}".format("mum", "skirt", "big")
        Class1.printf(a41)

        Class1 c1 = Class1()
        a7 = "c1.a1 as concat -> " + c1.a1.toString()
        Class1.printf(a7)

        c4 = 'a' + 'b'
        global.println("char-like + -> " + c4.toString())
        c6 = "aaaaaa"
        Class1.printf(c6)

        # f"" 空占位在部分实现中可能非法，保留占位字面
        fstr = f"prefix"
        global.println("f-string sample -> " + fstr)
        # 多字符单引号字面量在语言中通常非法，保留为说明：# c5 = 'aaaaa'
    }

    static dollarAndFormatSmoke()
    {
        global.println("----- dollarAndFormatSmoke -----")
        name = "QuTa"
        score = 55
        a = 100
        b = 300

        ct = ClassT(){ name = "mmm" }

        str3 = "{} Name:$ct.name {} Score:$score a+b=${(a + b).toString()} bb = {}"
        str33 = str3.format(name, score, "bb")
        global.println(str3.format(name + "---", score - 50, "bbxxxx"))
        global.println(str33)

        str4 = "Name:$name Score:$score a[{}]+b[{}]=${(a + b).toString()}".format(a, b)
        global.println(str4)

        str5 = 'Name:\'$name\' NickName="AQ" Score=$score a{}+b{}=${(a + b).toString()}'
        global.println(str5)

        str6 = f"""我是一段话 我叫${name}
        我\n今天 考了${score}分 "大Q$name QQ" 'xml' \" \t
        End"""
        global.println("verbatim f-triple: " + str6)
    }

    static fun()
    {
        global.println("========== StringTest (start) ==========")
        literalAndFormatSmoke()
        dollarAndFormatSmoke()
        global.println("========== StringTest (end) ==========")
    }
}

# 测试面向：双引号/单引号/原始 JSON 片段、.format 占位 {0} 与空 {}、$var 与 ${expr} 插值、三引号 f 字符串。
# 预期：无重复 static fun、无在赋值前使用 c1 的插值；输出中含 format 结果与 a+b=400 等；多字符 char 字面量保持为注释负例。
