
interface InterfaceClass1
{
    string interfaceFun1()  #必须定义返回值
}
interface InterfaceClass2
{
    int interfaceFun2()
}
interface InterfaceClass3  #如果被接口了，发现没有接口，需要报错
{
    interface3(){};
}

ImmplementClass1_1 interface InterfaceClass1,InterfaceClass2,InterfaceClass3
{
    x1 = 0;
    y1 = 0;
    z1 = 0;

    _init_( int _x1, int _y1 )
    {
        this._init_(_x1+1);
    }

    _init_(int _z1 )
    {
        this.z1 = _z1
    }

    # 必须实现 Fun函数  自动变成public
    override string interfaceFun1()    
    {  
        ret (this.x1 + this.y1 + this.z1).toString()
    }
    override int interfaceFun2(){
        ret int(this.x1*this.y1 + this.z1);
    }
    string fun2()
    {
        ret "fun1";
    }
    override void interface3(){}
}


InterfaceTest
{
    static fun()
    {
        InterfaceClass1 c1 = ImmplementClass1_1(1,2);
        v1 = c1.interfaceFun1()
        global.println("-------------" + v1 );

        InterfaceClass2 c2 = ImmplementClass1_1(100);
        v2 = c2.interfaceFun2()
        global.println("-------------" + v2 );

        ImmplementClass1_1 c3 = ImmplementClass1_1(1,2);
        v2 = c3.interfaceFun2();
        global.println("-------------" + v2 );

        #!
        List<InterfaceClass1> listc1 = new()
        listc1.add(v1)
        listc1.add(c3)

        for cc in listc1
        {
            global.println( "if1" + cc.interfaceFun1() )
        }
        !#

    }
}


# 接口，必须以inetreface 定义结构，后， 在使用接口时使用interface 接口调用
# 如果有extends 类，则要看父类中是否有 interface 的方法， 如果有，则必须实现父类的方式，否则，子类不允许使用，父类如果是实例时，则不允许调用该方法  在实现的时候，必须使用override字段，标名，是个要实现的方法。