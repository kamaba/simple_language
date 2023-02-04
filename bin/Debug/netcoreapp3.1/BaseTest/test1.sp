
const data ProjectConfig
{
    name = "Test1";
    compileFileList = 
    [
        {
            path = "EnumTest.s";
            group = "temp";
            tag = "EnumTest";
        }
    ]
}

ProjectEnter
{
    static Main()
    {
       #Class1.Print();
       EnumTest.Fun();       
    }
    static Test()
    {
       #TempTest.Fun();
    }
}