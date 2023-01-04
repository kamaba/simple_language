
ProjectEnter
{
    static Main()
    {
       #Class1.Print();
       BindCSharpTest.Fun();       
    }
    static Test()
    {
       #TempTest.Fun();
    }
}
ProjectDll
{
    static LoadStart()
    {
        #动静态库加载完成后，开始执行
    }
    static LoadEnd()
    {
        #动静态库卸载完成后，开始执行
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

ProjectConfig
{
    name = "Test1";
    desc = "这是一个测试用例";
    compileFileList
    {
        #!
        {
            path = "NamespaceTest1.s";
            option = {};
            group = "namespace";
        },
        {
            "path":"NamespaceTest2.s",
            "group":"namespace",
            "priority":0
        },
        {
            "path":"Class1Test.s",
            "group":"class"
        },
        {
            "path":"Class2Test.s",
            "group":"class",
            "tag":"myc"
        },        
        {
            "path":"MemberFunction1Test.s",
            "group":"function",
            "tag":"myc"
        },        
        {
            "path":"MemberFunction2Test.s",
            "group":"function",
            "tag":"myc"
        },        
        {
            "path":"MemberVartialbe1Test.s",
            "group":"MemberVartialbe",
            "tag":"myc"
        },        
        {
            "path":"MemberVartialbe2Test.s",
            "group":"MemberVartialbe",
            "tag":"myc"
        },        
        {
            "path":"TempTest.s",
            "group":"temp",
            "tag":"TempTest"
        },
        {
            "path":"ParTest.s",
            "group":"temppar1",
            "tag":"ParTest"
        },
        {
            "path":"statements_test.s",
            "group":"temp",
            "tag":"temp2"
        },
        {
            "path":"IfElseTest.s",
            "group":"temp",
            "tag":"IfelseTest"
        },
        {
            "path":"SwitchTest.s",
            "group":"temp",
            "tag":"SwitchTest"
        },
        {
            "path":"ForWhileTest.s",
            "group":"temp",
            "tag":"ForWhileTest"
        },
        {
            "path":"GoToLabelTest.s",
            "group":"temp",
            "tag":"GoToLabelTest"
        },
        {
            "path":"ObjectTest.s",
            "group":"temp",
            "tag":"ObjectTest"
        },
        {
            "path":"CastObject.s",
            "group":"temp",
            "tag":"CastObject"
        },
        {
            "path":"TemplateClass.s",
            "group":"temp",
            "tag":"TemplateClass"
        },
        {
            "path":"ArrayTest.s",
            "group":"temp",
            "tag":"ArrayTest"
        },
        {
            "path":"BlockTest.s",
            "group":"temp",
            "tag":"BlockTest"
        },
        {
            "path":"GetSetVariable.s",
            "group":"temp",
            "tag":"GetSetVariable"
        },
        {
            "path":"ExpressTest.s",
            "group":"temp",
            "tag":"ExpressTest"
        },
        {
            "path":"CSharpTest.s",
            "group":"csharp",
            "tag":"CSharpTest"
        },
        {
            "path":"BindCSharpTest.s",
            "group":"csharp",
            "tag":"BindCSharpTest"
        },
        {
            "path":"NumberTest.s",
            "group":"temp",
            "tag":"NumberTest"
        },
        {
            "path":"StringTest.s",
            "group":"temp",
            "tag":"StringTest"
        },
        {
            "path":"Core/BaseType/Int32.s",
            "group":"core",
            "tag":"Int32"
        },
        !#
        {
            path = "EnumTest.s";
            group = "temp";
            tag = "EnumTest";
            use = true;
        }
    }
    option
    {
        isForceUseClassKey = true;    # 是否强制使用class关键字
        isSupportDoublePlus = true;     #是否支持i++的 ++符号
    }
    filter
    {
        group = ["all"]
        tag = ["EnumTest"]
    }
    importModule     #导入模块
    {
        Core
        {
            "name":"Core",
            "path":"module/Corelib.dll"
        }
    }
    global            #全局设置
    {
        namespace          #命名空间设计
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
        }
        import         #全局导入
        {
            "CSharp.System",
            "CSharp.System.File",
            "Application.Math",
            "Application.Util"
        }
        replace        #全局宏替换
        {
            {"print":"Console.System"},
            {"println":"Console.System"},
            {"new $":"$"}
        }
    }
    export               #导出dll
    {
        Core.Class1
        {
            variable { allNotStatic,allStatic }
            method{"ToString", "CalcMusic","allStatic"}
        }
        Application.Core.Test1
        {
            variable = all;
            method = all;
        }
    }
}
