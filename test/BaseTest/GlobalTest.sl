
GlobalTest
{
    static fun()
    {
        global.println("========== GlobalTest (start) ==========")
        
        # .sp Project{} static function call chain
        global.print("project static function print -> ok")
        global.println("")

        # global.data: primitive
        Int32 gv1 = global.var1
        aa = global.vardata2.a
        global.println("jsonc global.var1 -> " + gv1.toString())
        global.println("jsonc global.vardata2.a -> " + aa.toString())
        

        # global.data: array
        arrRef = global.arrvar1
        global.println("global.arrvar1 != null -> " + arrRef.toString())

        # global.data: object
        bb = global.vardata2.b        
        global.println("global.vardata2.b -> " + bb.toString())
        global.println("config data (vardata2.a+b) -> " + (aa + bb).toString())

        # global._inputArgs: system-integrated CLI program args (Array<Object>)
        inputArgs = global._inputArgs
        global.println("global._inputArgs != null -> " + (inputArgs != null).toString())
        argCount = inputArgs.length
        global.println("global._inputArgs.length -> " + argCount.toString())
        i = 0
        while i < argCount
        {
            global.println("global._inputArgs[" + i.toString() + "] -> " + inputArgs[i].toString())
            i = i + 1
        }

        global.println("========== GlobalTest (end) ==========")
    }
}

# 测试说明：
# 1) 依赖 Core.jsonc -> global.data 的 var1 / arrvar1 / vardata2
# 2) 依赖 Core.sp -> Project 的静态字段 Pi 和函数 print/println
# 3) 覆盖 global 在 jsonc 配置注入 + .sp 工程成员调用 两条链路
# 4) 覆盖 global._inputArgs 系统集成的 CLI 程序参数链路
#    （csimple_lang run ProjectTest.module.json -- a b c）
