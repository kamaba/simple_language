import Std;
import Core;

# SLang.Log / SLang.Logger / SLang.Profiling 下沉 C VM 原生系统方法
# （SystemLogEmit / SystemLogAssert，core: log_system_method.c）后的语义验证用例。
# 覆盖：
#   1) 静态 Log：info/warning/logError 走原生 SystemLogEmit（时间戳+级别前缀+双路输出）
#   2) 静态 Log.assert：通过热路径零输出；失败输出断言信息
#   3) 级别开关 setMinLevel/setLogType/reset 与原生调用的门控联动
#   4) Logger 实例：_init_/info/warning/logError/assert（实例状态独立于全局 Log）
#   5) Profiling 计时：time/timeEnd（原 SLang.Debug 计时拆分至此；断言直接用 Log.assert）
#   5b) Trace 追踪：trace/Enter/Step/Exit/DumpStack/Break（原 SLang.Debug 追踪拆分至此）
#   6) 显示配置：setShowTime/setShowLevel/setTimeFormat（行格式 [时间][LEVEL] msg 各块独立开关）
#   7) 文件落盘：setFlushInterval 缓冲模式（间隔内不写盘）+ Log.save() 手动落盘 + 读回核验
# 每条日志格式：[yyyy:MM:dd hh:mm:ss ffff] [LEVEL] message

LogTest
{
    static check( string title, bool ok )
    {
        if ok
        {
            Console.println("[PASS] " + title)
        }
        else
        {
            Console.println("[FAIL] " + title)
        }
    }

    static fun()
    {
        Console.println("---- LogTest start ----")

        # 1) 静态 Log：基础三级别（原生 SystemLogEmit）
        SLang.Log.info("static info message")
        SLang.Log.warning("static warning message")
        SLang.Log.logError("static error message")

        # 2) assert：通过不输出；失败输出（原生 SystemLogAssert）
        SLang.Log.assert(true, "this line must NOT appear")
        SLang.Log.assert(false, "static assert failed (expected)")

        # 3) 级别开关联动
        SLang.Log.setMinLevel(2)
        SLang.Log.info("this info must be muted")
        SLang.Log.warning("warning after setMinLevel(2)")
        check("setMinLevel(2) mutes info", SLang.Log.isEnabled(1) == false)
        check("setMinLevel(2) keeps warning", SLang.Log.isEnabled(2) == true)

        SLang.Log.setLogType(false, false, true)
        SLang.Log.warning("this warning must be muted")
        SLang.Log.logError("error after setLogType(false,false,true)")
        SLang.Log.reset()
        SLang.Log.info("info restored after reset")

        # 4) Logger 实例：独立配置，不污染全局 Log
        SLang.Logger lg = new()
        lg.info("logger info message")
        lg.warning("logger warning message")
        lg.logError("logger error message")
        lg.assert(true, "logger assert pass (must NOT appear)")
        lg.assert(false, "logger assert failed (expected)")

        # 实例级开关不影响全局
        lg.setLogType(false, false, false)
        lg.info("logger info muted by instance switch")
        SLang.Log.info("global info still alive")

        # 5) Profiling 计时（断言/错误日志直接用 Log，不再经 Debug 中转）
        SLang.Profiling.enabled = true
        SLang.Log.logError("debug err message")
        SLang.Log.logError("debug logError message")
        SLang.Log.assert(true, "debug assert pass")
        SLang.Log.assert(false, "debug assert failed (expected)")
        SLang.Profiling.time("logtest-phase")
        SLang.Profiling.timeEnd("logtest-phase")

        # 5b) Trace 追踪（原 SLang.Debug 追踪拆分至此）：
        #     trace/Enter/Step/Exit 模拟调用栈缩进流 + DumpStack + Break 断点标记
        Console.println("")
        Console.println("-- 5b) Trace scope tracking --")
        SLang.Trace.setEnabled(true)
        check("Trace enabled", SLang.Trace.isEnabled() == true)
        SLang.Trace.trace("reached step 3")
        SLang.Trace.Enter("ProcessData")
        SLang.Trace.Step("load")
        SLang.Trace.Step("compute")
        SLang.Trace.DumpStack()
        SLang.Trace.Exit("ProcessData")
        SLang.Trace.Break()
        # 关开关后静默
        SLang.Trace.setEnabled(false)
        check("Trace disabled", SLang.Trace.isEnabled() == false)
        SLang.Trace.trace("this trace must NOT appear")
        SLang.Trace.Break()
        SLang.Trace.setEnabled(true)

        # 6) 显示配置：[时间] / [LEVEL] 两个前缀块独立开关 + 自定义时间格式
        Console.println("")
        Console.println("-- 6) showTime / showLevel / timeFormat --")

        # 默认格式（yyyy:MM:dd hh:mm:ss ffff）+ 双前缀块
        SLang.Log.reset()
        SLang.Log.info("default format: [yyyy:MM:dd hh:mm:ss ffff] [INFO] expected")

        # 关时间块 -> [LEVEL] msg
        SLang.Log.setShowTime(false)
        SLang.Log.info("no time block: [INFO] ... expected")
        SLang.Log.warning("no time block: [WARN] ... expected")

        # 关级别块 -> [time] msg
        SLang.Log.setShowTime(true)
        SLang.Log.setShowLevel(false)
        SLang.Log.info("no level block: [time] ... expected")

        # 双关 -> 纯 msg
        SLang.Log.setShowTime(false)
        SLang.Log.logError("no blocks: plain message expected")

        # 自定义短格式
        SLang.Log.setShowTime(true)
        SLang.Log.setShowLevel(true)
        SLang.Log.setTimeFormat("hh:mm:ss")
        SLang.Log.info("short time format: [hh:mm:ss] [INFO] expected")

        # Logger 实例：显示配置独立生效
        SLang.Logger lg2 = new()
        lg2.setShowTime(false)
        lg2.setShowLevel(true)
        lg2.info("logger no time block: [INFO] ... expected")
        lg2.setTimeFormat("MM/dd hh:mm")
        lg2.setShowTime(true)
        lg2.info("logger custom format: [MM/dd hh:mm] [INFO] expected")

        # 7) 文件落盘：缓冲模式 + 手动 save
        Console.println("")
        Console.println("-- 7) flushInterval + save --")

        string logPath = "LogTest_flush.log"
        if SystemFileExists(logPath)
        {
            SystemFileDelete(logPath)
        }

        # 缓冲模式：60 秒间隔内不落盘
        SLang.Log.reset()
        SLang.Log.setFilePath(logPath)
        SLang.Log.setFlushInterval(60)
        SLang.Log.setConsoleOutput(false)
        SLang.Log.info("buffered line 1 (must be in buffer, not on disk yet)")
        SLang.Log.info("buffered line 2 (must be in buffer, not on disk yet)")
        string before = SystemFileReadAllText(logPath)
        Console.println("before save, file content = [" + before + "]")
        check("buffered mode holds writes before interval", before == "")

        # 手动落盘：save() 后缓冲内容全部进文件
        SLang.Log.save()
        string after = SystemFileReadAllText(logPath)
        Console.println("after save, file content =")
        Console.println(after)
        check("save() flushes buffer to file", after != "")

        # save 后继续写 -> 新一轮缓冲 -> 再 save
        SLang.Log.info("buffered line 3 (after first save)")
        SLang.Log.save()
        string after2 = SystemFileReadAllText(logPath)
        Console.println("after second save, file contains line3 too")
        check("buffer restarts after save", after2 != after)

        # Logger 实例 save(path)：缓冲写入指定路径并切换后续落盘路径
        string logPath2 = "LogTest_flush2.log"
        if SystemFileExists(logPath2)
        {
            SystemFileDelete(logPath2)
        }
        lg2.setConsoleOutput(false)
        lg2.setFilePath(logPath2)
        lg2.setFlushInterval(60)
        lg2.info("logger buffered line (instance buffer)")
        lg2.save()
        string instAfter = SystemFileReadAllText(logPath2)
        Console.println("instance log file content =")
        Console.println(instAfter)
        check("Logger.save() flushes instance buffer", instAfter != "")

        SLang.Log.reset()

        Console.println("---- LogTest done ----")
        check("LogTest smoke", true)
    }
}
