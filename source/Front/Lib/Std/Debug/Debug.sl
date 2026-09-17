# ============================================================================
# Std/Debug/Debug.sl — 日志与调试工具模块
#
# 本文件包含三个类，对应三种使用场景：
#   1. Log    ：静态全局日志（级别化：info/warn/error + 时间戳 + 可选文件落盘）
#   2. Logger ：非静态日志器（每个实例独立配置，可在 isolate/协程中分别持有）
#   3. Debug  ：静态调试工具（断言/作用域追踪/计时/断点，面向开发期定位）
#
# 与 Console 的区别：
#   - Console 是「裸打印」（write/println/input），无级别、无文件落盘；
#   - 这里的 Log/Logger/Debug 是「带级别/追踪 + 可选文件落盘」的结构化输出。
#
# 每条日志格式：[yyyy-MM-dd HH:mm:ss][LEVEL] message
# 实时输出：默认每条日志都会 SystemPrintln 实时打印到控制台（受 setConsoleOutput 控制）。
# ============================================================================

namespace SLang
{

    # ========================================================================
    # 3) 静态调试工具集（与 Log 的定位不同）
    #
    # 参考主流框架（Unity Debug / .NET Debug）的职责划分：
    #   Log / Logger ：「级别化日志」—— info/warn/error + 时间戳 + 文件落盘，
    #                  面向「运行期记录」，产物是给人/系统看的流水日志。
    #   Debug       ：「开发期调试手段」—— 断言(Assert)、作用域追踪(trace)、
    #                  计时(time/timeEnd)、断点(Break)，面向「开发期定位问题」。
    #
    # 关于 trace：当前 VM 未暴露「调用栈回溯 / 真实行号」API，因此 trace 采用
    # 「显式作用域标注」模型——用 Debug.Enter/Exit 维护一个模拟调用栈，配合
    # Debug.trace / Step 输出带缩进的追踪流，等效于主流框架的调用链追踪。
    #
    # 用法：
    #   Debug.enabled = true                     # 调试总开关（= isDebugBuild）
    #   Debug.trace("reached step 3")
    #   Debug.Enter("ProcessData")
    #       Debug.Step("load")
    #       Debug.Step("compute")
    #   Debug.Exit("ProcessData")
    #   Debug.Assert(x > 0, "x must be positive")
    #   Debug.time("phase1"); ... ; Debug.timeEnd("phase1")
    #   Debug.Break()
    # ========================================================================
    public class Debug
    {
        # 调试总开关：false 时所有 Debug 输出/断言静默（等价 Release 剔除）
        static bool enabled = true

        # ---- 作用域追踪（模拟调用栈）----
        static bool         _inited     = false
        static List<string> _scopeStack = null
        static int          _depth      = 0

        # ---- 计时器（key 列表 + 起始毫秒列表，并行存储）----
        static List<string> _timerKeys  = null
        static List<Int64>  _timerVals  = null

        static void _ensure()
        {
            if !_inited
            {
                _scopeStack = List<string>()
                _timerKeys  = List<string>()
                _timerVals  = List<Int64>()
                _inited = true
            }
        }

        # 调试总开关
        public static void setEnabled( bool v ) { enabled = v }
        public static bool isEnabled() { ret enabled }

        # ---- 基础日志（复用全局 Log，保持单一输出格式）----
        public static void log( string msg )      { SLang.Log.info(msg) }
        public static void warn( string msg )     { SLang.Log.warning(msg) }
        public static void error( string msg )    { SLang.Log.logError(msg) }
        public static void logError( string msg ) { SLang.Log.logError(msg) }

        # ---- 追踪 ----
        # 一条追踪信息（带时间戳），标记"走到这里了"
        public static void trace( string msg )
        {
            if !enabled { ret }
            string ts = SystemDateTimeFormat(SystemDateTimeNowMillis(), "HH:mm:ss", 0)
            SLang.Log.print("[" + ts + "][TRACE] " + _indent() + msg)
        }

        # 进入一个作用域（入栈），后续追踪增加缩进
        public static void Enter( string scope )
        {
            if !enabled { ret }
            _ensure()
            _depth = _depth + 1
            _scopeStack.add(scope)
            string ts = SystemDateTimeFormat(SystemDateTimeNowMillis(), "HH:mm:ss", 0)
            SLang.Log.print("[" + ts + "][TRACE] " + _indent() + "-> Enter " + scope)
        }

        # 退出一个作用域（出栈）
        public static void Exit( string scope )
        {
            if !enabled { ret }
            _ensure()
            string ts = SystemDateTimeFormat(SystemDateTimeNowMillis(), "HH:mm:ss", 0)
            SLang.Log.print("[" + ts + "][TRACE] " + _indent() + "<- Exit " + scope)
            if _scopeStack.length > 0
            {
                _scopeStack.removeAt(_scopeStack.length - 1)
            }
            if _depth > 0 { _depth = _depth - 1 }
        }

        # 逐步追踪（常用于循环/长流程中标记进度）
        public static void Step( string msg )
        {
            if !enabled { ret }
            string ts = SystemDateTimeFormat(SystemDateTimeNowMillis(), "HH:mm:ss", 0)
            SLang.Log.print("[" + ts + "][TRACE] " + _indent() + ". " + msg)
        }

        # 打印当前模拟调用栈（Enter/Exit 累积的作用域）
        public static void DumpStack()
        {
            if !enabled { ret }
            _ensure()
            SLang.Log.print("[TRACE] -- scope stack (depth=" + _depth + ") --")
            int i = 0
            while i < _scopeStack.length
            {
                SLang.Log.print("[TRACE]   " + i + ": " + _scopeStack[i])
                i = i + 1
            }
        }

        # ---- 断言 ----
        public static void Assert( bool condition, string msg )
        {
            if !enabled { ret }
            if !condition
            {
                SLang.Log.asset(false, "Debug.Assert failed: " + msg)
                DumpStack()
            }
        }

        public static void AssertNotNull( object obj, string msg )
        {
            Assert(obj != null, msg)
        }

        # ---- 计时（性能分析）----
        public static void time( string key )
        {
            if !enabled { ret }
            _ensure()
            Int64 now = SystemDateTimeNowMillis()
            int idx = _findTimer(key)
            if idx >= 0
            {
                _timerVals[idx] = now
            }
            else
            {
                _timerKeys.add(key)
                _timerVals.add(now)
            }
        }

        public static void timeEnd( string key )
        {
            if !enabled { ret }
            _ensure()
            int idx = _findTimer(key)
            if idx >= 0
            {
                Int64 dt = SystemDateTimeNowMillis() - _timerVals[idx]
                SLang.Log.print("[TIME] " + key + ": " + dt + " ms")
                _timerKeys.removeAt(idx)
                _timerVals.removeAt(idx)
            }
            else
            {
                SLang.Log.print("[TIME] " + key + ": <not started>")
            }
        }

        # ---- 断点 ----
        # 注：VM 当前未暴露真实断点/异常 API，Break 仅打印断点标记与作用域栈，便于定位；
        #     若后续 VM 提供断点 system call，可在此接管（如抛调试异常/挂起）。
        public static void Break()
        {
            if !enabled { ret }
            SLang.Log.print("[BREAK] Debug.Break() hit")
            DumpStack()
        }

        # ---- 内部 ----
        static int _findTimer( string key )
        {
            int i = 0
            while i < _timerKeys.length
            {
                if _timerKeys[i] == key
                {
                    ret i
                }
                i = i + 1
            }
            ret -1
        }

        static string _indent()
        {
            string s = ""
            int i = 0
            while i < _depth
            {
                s = s + "  "
                i = i + 1
            }
            ret s
        }
    }
}