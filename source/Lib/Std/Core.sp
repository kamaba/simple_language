
ProjectEnter
{
    static Main()
    {
       #Class1.Print();
       CastTObject.Fun();
    }
    static Test()
    {
       #Class1.Print();        
    }
}
Compile
{
    CompileBefore()
    {
        
    }
    CompileAfter()
    {
        
    }
}

ProjectConfigBegin{
    "name":"Core",
    "desc":"CoreLib",
    "compileFileList":
    [ 
        {
            "path":"Object.s",
            "option":{},
            "group":"namespace",
            "priority":0

        }
     ],
    "globalNamespace":
     [
            "Application",
            "Application.Core"
    ],
    "option": 
    {
        "isForceUseClassKey":true
    },
    "filter":
    {
        "group":["all"],
        "tag":["all"]
    },
    "module": 
    [
        {
            "name":"Core",
            "path":"module/Corelib.dll"
        }
    ]
}ProjectConfigEnd

ClassConfig
{

}