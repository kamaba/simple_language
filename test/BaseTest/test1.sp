
#!
#关于 global 的定义，到时候 和local是一对，local/global 两个，分析是定义 文件局部代码，和全局的代码，格式一样，并且里边有一些内容，可以执行
global
{

    pi = 3.14
    Level1 = Level<int>(200)
    int minInt = -1i
}
!#

Project
{
    static Main()
    {
        #typedef Array = Array<Object>
        #typedef ArrayOK<T> = Array<T>
        
        #GenClass.fun()
        #GenFunction.fun();
        #TypeTest.fun()
        #ClassAs_Is.fun()
        ArrayTest.fun()
        #IfelseTest.fun()
        #ReloadSignTest.fun()
    }
    static Test()
    {
       #TempTest.Fun();
    }    
    CompileBefore()
    {        
    }
    CompileAfter()
    {        
    }
}

# 在newproject后，会生成两个文件 。一个是 项目名称.sp 是扫描的入口 还有一个是项目名称.config这个是通过json配置的工程
# 在.sp中，可以放global,也可以通过配置json的方式配置global.sl
# 在.sp中，有Main是函数的正式入口 有Test是测试入口 ，如果在cmd中，可以加-test 可以就走test入口
# 在Project中，可以写CompileBefore即编译前的先执行函数，和CompileAfter的函数，也可以设计 ImportDll, ExportDll的导入插件相关的函数