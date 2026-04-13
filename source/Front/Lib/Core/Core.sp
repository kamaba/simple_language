
Project
{
    string cpk = "cpkkk"
    float Pi = 3.14f
    print( object str )
    {        
        SystemPrint(str)
    }
    println( object str )
    {
        SystemPrintln(str)
    }
    printBridgeKind( BridgeKind v )
    {

            if v == BridgeKind.SELF 
            {
                println( "BridgeKind--------------SELF " )
            }
            elif v == BridgeKind.JVM
            {
                println( "BridgeKind--------------JVM " )
            }
            else
            {
                println( "BridgeKind--------------NATIVE " )
            }
    }
    _main_()
    {
        #ObjectTest.fun()
        #NumberTest.fun()
        #StringTest.fun()
        #TypeTest.fun()

        #a = 2 + 4
        #global.print(a.toString())
        #SystemPrint(a.toString())
        #Call( BridgeKind.CLR )
        #global.println( "--------------------for enum-------------------" )
        #global.println( "Pi=$global.Pi }" )
        #global.println( "config data $global.var1 " )
        #str = global.arrvar1.toString()
        #global.println( "global arr: $str "  )
        #aa = global.vardata2.a
        #global.println( "config data ${aa + global.vardata2.b} " )

        #a = [1,2,"222",1000L,[1,2,3,4]]
        #!
        a = [1981,"mmmmm", 0xef, 33333L,[1988,2045]]
        for v in a
        {
            #vs = v.toString()
            println("------------------$v.toString() ")
        }
        !#     

        #!
        for v in BridgeKind
        {
            if v == BridgeKind.SELF 
            {
                println( "BridgeKind--------------SELF " )
                continue
            }
            elif v == BridgeKind.JVM
            {
                println( "BridgeKind--------------JVM " )
                break
            }
            println( "BridgeKind= $v.name.toString() " )
            printBridgeKind(v)
        }
        
        r1 = 1..100;    #快速int range  以后再支持 1..n   n..200的方式    相当于   range( 1, 100, 1 )的调用        
        r2 = range( 1.0f, 200.0f, 1.0f );
        Range<double> r3 = (3.2d, 54.3d, 0.22d );
        r4 = Range<short>( 1s, 100s, 2s );

        r1.step( 1 )   #设置r1的步进
        r4.step = 2       #设置r4的步进 与SetStep方法一样
        for v in r1   
        {
            CSharp.System.Debug.Write("value=$v");
        }
        !#
            
        sum = 0
        for i in range(1,10, 2 )
        {
            sum += 10
            println( i )
            for n in range( 3, 6 )
            {
               sum += n
               println( "n=$n" )
            }
        }
        println("sum:"  + sum)
    }
    _test_()
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