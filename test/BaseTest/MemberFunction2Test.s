
Class1
{
    a1func(){ }
    b1func(){ }
    c1func(){}
    override d1func(){}
}

Class2 extneds Class1
{
    _init_()
    {

    }
    Class1()
    {
        #不允许
    }
    Class3()
    {
        #不允许与类名相同
    }

    a1func(){ }

    final b1func(){ }

    c1func(){}

    d1func(){}
}
Class3 extends Class2 
{
    b1func(){}   #报错

    c1func(){}   #报错，应该有override标记

    final override d1func(){}   #成功
}

Class4 extends Class1
{
    static b1func(){}   #报错 已有该函数名
    virtual c1func(){}   # 报错，已有函数名
    override d1func(){}      # 成功 带override标记，可以覆盖
}


