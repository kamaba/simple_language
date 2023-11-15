
const data ProjectConfig
{
    name = "Test1";
    compileFileList = 
    [
        {
            path = "ObjectTest.s";
            group = "temp";
            tag = "all";
        }
    ]
    globalVariable
    {
        pi = 3.1415f;
        minInt = -1i;
        maxInt = 23232323i;
        #xc = XC;
    }
    globalNamespace          #命名空间设计
    {
        Application
        {
            Core
            {
                Game{}
                Instance{}
                UI{}
            }
            Math
            {
                Util{}
                Ext{}
            }
            Render
            {
                Camera{}
                Mass{}
                Entry{}
            }
            Util
            {
            }
        }
        QT
        {
            Math{}
            Express{}
        }
        QS{}
    }
}

Project
{
    Test1{
        a = 20;
    }
    static Main()
    {        
        #global.pi = 3.1415f;
        #global.xc = { a = 20, b = 15 }
       #Class1.Print();
       ObjectTest.Fun();       
    }
    static Test()
    {
       #TempTest.Fun();
    }
    static Global()
    {
    }
    static void SetXC( int a )
    {
        #global.xc.a = a;
    }
}