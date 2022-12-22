SwitchTest
{
    Class2
    {
        
    }
    Class3 :: Class2
    {

    }
    Class4 :: Class2
    {

    }

    static Func()
    {
        #!  等丰富类型后，加入类型匹配
        t = Class2();
        switch t.Type()
        {
            case int{ idok = 20; }
            case float{}
            default{}
        }

        int a = 10;
        x = 12;
        x2 = 140;
        m = 200;

        # switch 匹配原则  1. 如果匹配过一个过，将不继续匹配，即自带一个break; 如果要继续匹配下一个，
        # 需要使用next字段  switch 如果使用了赋值过程，则不能在下列中使用next语句，即，在switch中，
        # 没有使用=号时，可以使用next往后继续执行;
        #switch 语法 则 switch [valuepoption] { } 使用方式其它 case condition{} 即是完整过程，在case或者是default中可使用tr返回临时值
       
        
        # 类型可以使用 switch Type{ 进行匹配 }
        obj = Class2();
        robj2 = switch obj
        {
            case Class3 obj2{
                tr obj2;
            }
            case Class4 obj3
            {
                tr obj3;
            }
        }
        type1 = 20;
        rootType = switch type1{
            case 1{ tr "ems"; }
            case 2{ tr "room1";}
            case 10{ tr "rmi1"; }
        }

        # 可以对obj的类型进行强制转化，并且判断是否为空，如果不为空，则可以执行里边的内容
        !#

        mxx = 30;
        mxxx3 = switch mxx 
        {
            case 20 
            case 40{ mxx = 100; tr mxx;}
            case 30,31,32{ mxx= 300; tr mxx; }
            #case 34..50{ mxx = 100; }   #等加入数据后，再使用这种匹配方式            
            case 100 {mxx = 200; tr mxx;}
            default{ tr 100;}
        }
        # 可以对正常值进行匹配 
        
    }
}