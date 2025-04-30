
Class1
{
    a1func(){ }
    b1func(){ }
    c1func(){}
    override d1func(){}
}

Class2 :: Class1
{
    Class2()
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
Class3 :: Class2 
{
    b1func(){}

    c1func(){}

    final override d1func(){}
}

Class4 :: Class1
{
    static b1func(){}
    virtual c1func(){}
    override d1func(){}
}


