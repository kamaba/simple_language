

Main()
{
    
}
Test()
{
    
}

CompileBefore()
{

}
CompileAfter()
{

}

ProjectConfigBegin{
    "name":"test1",
    "desc":"这是一个测试用例",
    "compileFileList":
    [ 
        {
            "path":"namespace_test1.s",
            "option":{},
            "group":"namespace",
            "priority":0

        },
        {
            "path":"namespace_test2.s",
            "group":"namespace",
            "priority":0
        },
        {
            "path":"class1_test.s",
            "group":"class"
        },
        {
            "path":"class2_test.s",
            "group":"class",
            "tag":"myc"
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
        "group":["namespace"],
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