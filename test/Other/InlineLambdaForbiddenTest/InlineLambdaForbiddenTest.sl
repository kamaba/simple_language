# 内联 lambda 负例集 (M2 限制拦截验证, 设计文档 §4.1 禁止总表)
# 本工程预期编译失败: 每个负例应命中 LID 21449-21452 / 21453 / 21456-21458 之一
# 验证方式: 编译后查 out/export/InlineLambdaForbiddenTest/Logs/Front.txt 中的内联体拦截错误
InlineLambdaForbiddenTest
{
    static string safeThrowsFn() throws
    {
        ret "safe"
    }

    # 负例9 [21453]: inline 方法体 > 10 条顶层语句 —— M1b 定义点规模检查拦截
    static inline Int32 bigBody( Int32 x )
    {
        Int32 s = 0
        s = s + 1
        s = s + 2
        s = s + 3
        s = s + 4
        s = s + 5
        s = s + 6
        s = s + 7
        s = s + 8
        s = s + 9
        ret s + x
    }

    # 负例10 [21457]: inline + throws 标签互斥 —— 体替换与异常帧语义冲突, 定义点拦截
    static inline Int32 badThrows( Int32 a ) throws
    {
        ret a
    }

    # 负例11 [21458]: 字段初始化器调用 inline 方法 —— 只允许在其它函数体内调用
    static inline Int32 baseVal()
    {
        ret 7
    }
    Int32 fieldBad = baseVal()

    static fun()
    {
        global.println("========== InlineLambdaForbiddenTest (start) ==========")

        function add2Fn = function( int a, int b )
        {
            ret a + b
        }

        # 负例1 [21449]: spawn 顶层 —— 脱糖为 Coroutine.spawnClosureN 进入体内, 定义点拦截
        var f1 = ( x ) => spawn add2Fn( x, 1 )

        # 负例2 [21449]: await 顶层 —— 脱糖为 Coroutine.awaitTask 进入体内, 定义点拦截
        Task h0 = spawn add2Fn( 1, 2 )
        var f2 = ( x ) => await h0 as int

        # 负例3 [21449]: Isolate.run —— 脱糖为 SystemIsolateRun 进入体内, 定义点拦截
        var f3 = ( x ) => Isolate.run( add2Fn, x, 2 )

        # 负例4 [21452]: 自引用 —— 定义点扫描直接命中
        var f4 = ( x ) => f4( x ) + 1

        # 负例5 [21452]: 互递归 —— 定义点不报 (引用的是对方), 调用点展开栈检出
        var g1 = ( x ) => g2( x ) + 1
        var g2 = ( x ) => g1( x ) + 1
        Int32 r5 = g1( 1 )

        # 负例6 [21451]: try? 表达式前缀 —— 表达式位置异常帧形态, 定义点拦截
        # (try/try?/try! 均为表达式前缀, 可出现在 => 体内; 21450 的 break/return 等与
        #  21451 的块级 try/catch 无法在表达式位置合法出现, 扫描器作为防御保留)
        var f6 = ( x ) => try? safeThrowsFn()

        # 负例7 [21456]: M3 实参类型与标注不匹配 —— 调用点类型校验拦截
        # (定义点正常注册, 调用点 t7("x", 2) 首实参 String != Int32 标注)
        var t7 = ( Int32 a, Int32 b ) => a + b
        Int32 r7 = t7( "x", 2 )

        # 负例8 [Node层]: M3 参数默认值表达式 —— 解析层拦截 (展开时实参必须全部提供)
        var t8 = ( Int32 a = 10 ) => a + 1
        Int32 r8 = t8( 1 )

        global.println("========== InlineLambdaForbiddenTest (end) ==========")
    }
}
