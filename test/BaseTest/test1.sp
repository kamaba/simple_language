
const data ProjectConfig
{
    name = "Test1";
    compileFileList = 
    [
        {
            path = "7_CallLinkTest.sl";
            group = "temp";
            tag = "all";
            a = 200
        }
    ]
    globalVariable = 
    {
        testclass =
        {
            a = 20
            b = 30
        }
        pi = 3.1415f;
        minInt = -1i;
        maxInt = 23232323i;
        #xc = XC;
    }
    proojectStruct =          #命名空间设计
    {
        Std =
        {
            type = "namespace"
            child = 
            {
                Collection =
                {
                    type = "namespace"
                }
                Math =
                {
                    type = "class"
                }
                Layer1_1 =
                {
                    type = "namespace"
                    child ={
                        Layer2_1 =
                        {
                            type="namespace"
                        }
                        Layer2_2 =
                        {
                            type="namespace"
                        }
                    }
                }
                Layer1_2 =
                {
                    type = "namespace"
                }
            }
        } 
        Core = 
        {
            type = "namespace"
            child = 
            {                
                Object =
                {
                    type = "class"
                }
            }
        }
    }
}

Project
{
    #!
    Test1{
        a = 20;
    }
    !#
    static Main()
    {        
        #global.pi = 3.1415f;
        #global.xc = { a = 20, b = 15 }
       #Class1.Print();
       #ObjectTest.Fun();    
       #EnumTest.fun()   
       #CommitTest.fun();
       #NumberTest.fun()
       #StringTest.fun()
       GenClass.fun()
       #ArrayTest.fun()
       #OverrideFunction.fun()
       CallLinkTest.fun()
    }
    static Test()
    {
       #TempTest.Fun();
    }
    static Global()
    {
    }
    #!
    static void SetXC( int a )
    {
        #global.xc.a = a;
    }
    !#
}


# 可能 在这里边进行全局性的设置，比较命名空间，类名，还有接口的设计，设计完成后，  可以告诉类的文件位置，这样更好的布置



## 最近的变化思路，把token 整齐的 放到node节点里边，  相当于  普通 关键字 {} [] ()  为同一级别