
const data ProjectConfig
{
    name = "Test1";
    compileFileList = 
    [
        {
            path = "ArrayTest.s";
            group = "temp";
            tag = "ArrayTest";
        }
    ]
    globalVariable
    {
        pi = 3.1415f;
        minInt = -1i;
        maxInt = 23232323i;
        #xc = XC;
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
       ArrayTest.Fun();       
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