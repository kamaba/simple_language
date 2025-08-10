
C1
{
    C2 c3 = new();
    X = 1;
    Y = 2;

    static public int a1 = 100;
    private a2 = 20;
    private long a3;    #不允许使用没有等号的情况 除了class之外 ，其它的类型都需要有=号
    C2 a4;              #报错，需要有=号，要不为null，要不new()
    static C2 a5;       #报错，同上
    a6 = C2();      
    a7 = 10 / 5 + 2 * 10 - C2.x2;
    C2 a8 = new();
    
    a = 10;
    b = 20;
}

partial C2
{
    x = 1;
    static int x2 = 10;

    _init_( int a, float b )
    {
        this.x = 10;
        this.x1 = C2.x2;
        x2 = 30         #这种方式，也可以读取静态变量
    }
}

C3 extends C1
{
    x = 1;
    z = 2;
}

Class2
{
    static int m2 = 20;
}

C4 extends C3
{
    bool ab = 10 <= 10 / (20+1 * 35 - (32/15) );
    bx = -20;
    int a = 10 + (1 - ( 3 * ( 3 / ( -Class2.m2 - 100 ) )  * 30 ) ) / 10 * (15 - (22/2 + 1 ) ) - 20 / -10;
    int x1 = 10 + (1-20) * 33/20 + 10/20 - 22 * 2/1;
    C2 c2 = C2( 20 );
    C2 c3 = new();
}


# 在定义变量时，优先结构const节点，并且该触点必须 是实体定义
# 在解析const后，再解析static 节点，该节点，可以引入 定义的const类型，与已结算static类型，最终在解析时，一般情况会结算出 const值 
# 再定义nonStatic变量，该变量，也必须 使用=号 ，不得空定义，定义后，需要动态计算
# 每个变量中，在调用时，会自动生成default节点，定义，在类定义中，如果定义，必须为静态的，赋值的类型。
# 静态变量 使用，要不带正常的类名称，要不直接在函数体内写入   虚拟机暂是没有写入默认静态变化，静态变量现在已存入到具体类中，相当于与类绑定了