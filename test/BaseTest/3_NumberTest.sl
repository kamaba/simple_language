import CSharp.SimpleLanguage.Core.SelfMeta
import CSharp.System

data XC
{
    a = 2
    b = 10
}

public class Int32 
{
    public string toString()
    {
        string str = ""
        str = Int32MetaClass.MetaToString( this )
        ret str
    }
}
public class Float
{
}
public class String
{
}
NumberTest
{
    static fun()
    {
        #i1 = 1;
        #i2 = 2i;
        #i3 = 3i;
        #ui1 = 10ui;
        #f0 = 2.0;         #  end:null point:1
        #f1 = 2.321f;      #  end:f point:1
        #f2 = 2f;          # end:f point:0  报错
        #f4 = 2i.toString();   # end:t point:1
        #f5 = 2.0f.toString();    #
        #f6 = 23.223d.toFloat();
        #ul111 = 0xff22.toString();
        c = 1+1.3;
        d = 2.0 * 3.2132123123123 / c    
        d1 = 3.0d + d
        #s1 = 1s;
        #s2 = 2us;        
        #L1 = 10L;
        #UL1 = 12123123123123123123uL;
        #h1 = 0x123f;
        #h2 = 0xaff;
        #h3 = 0x1abfe;    #报错
        #o1 = 0o1132;
        #o2 = 0o23;      #报错
        #bin1 = 0b1100_1111;

        Debug.Write( "printlfn===: " + d1 )

        #!
        !#
    }
}

#! 测试通过
1. 分为 byte,sbyte, int16, uint16, int32, uint32, int64 uint64 string 这几种基础类型
2. byte-> byte  sbyte-> sbyte int16-> short uint16-> ushort   int32-> int uint32 -> uint   int64->long uint64-> ulong 可使用这几种方式定义类型，兼容c/c++
3. 可以通过后缀，直接定义类型，  如果是数字 默认为int型，如果 有 后边i 也认为不int32   如果有ui认为是uint32  如果为L 认为是int64 如果 是uL认为是UInt64  如果后边是s则认为是int16如果是us认为是uint16型
4. 如果定义带小数点，默认为float 型，如果超出了float最大长度，后边的将被截段，如果后边跟f认为是float型，如果跟d认为是double型
5. 如果定义0x开头，为十六进制数字表达   0o开头的，八进制 表达  ob开头的，为二进制表达，如果超出进制数，将会报错
6. 数字 的维度，一般是 byte->sbyte->int16->uint16->int32->uint32->float->int64->uint64->double->string 这样的排序，如果在计算的时候，会往后升级权重 
7. 在数字 类，可以直接使用 2i.toString() 调用内置的一些函数 具体需要看类里国的函数定义
!#
