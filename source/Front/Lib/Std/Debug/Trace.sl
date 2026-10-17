# ============================================================================
# Std/Debug/Trace.sl — 作用域追踪类（显式作用域标注模型）
#
# 职责（从原 Debug 类拆分，原文件已删除）：
#   Trace ：「数据追踪」—— trace/Enter/Exit/Step/DumpStack 维护一个模拟
#           调用栈，输出带缩进的追踪流；Break 打印断点标记与作用域栈。
#
# 当前 VM 未暴露「调用栈回溯 / 真实行号」API，因此采用「显式作用域标注」
# 模型——用 Trace.Enter/Exit 维护一个模拟调用栈，配合 Trace.trace / Step
# 输出带缩进的追踪流，等效于主流框架的调用链追踪。
#
# 基础日志（info/warn/error）与断言（assert）请直接用 SLang.Log；
# 计时（time/timeEnd）请用 SLang.Profiling。
#
# 用法：
#   Trace.enabled = true
#   Trace.trace("reached step 3")
#   Trace.Enter("ProcessData")
#       Trace.Step("load")
#       Trace.Step("compute")
#   Trace.Exit("ProcessData")
#   Trace.Break()
# ============================================================================

namespace SLang
{
    public class Trace
    {
        # 追踪总开关：false 时所有 Trace 输出静默
        static bool enabled = true

        # ---- 作用域追踪（模拟调用栈）----
        static bool         _inited     = false
        static List<string> _scopeStack = null
        static int          _depth      = 0

        static void _ensure()
        {
            if !_inited
            {
                _scopeStack = List<string>()
                _inited = true
            }
        }

        # 追踪总开关
        public static void setEnabled( bool v ) { enabled = v }
        public static bool isEnabled() { ret enabled }

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

        # ---- 断点 ----
        # 注：VM 当前未暴露真实断点/异常 API，Break 仅打印断点标记与作用域栈，便于定位；
        #     若后续 VM 提供断点 system call，可在此接管（如抛调试异常/挂起）。
        public static void Break()
        {
            if !enabled { ret }
            SLang.Log.print("[BREAK] Trace.Break() hit")
            DumpStack()
        }

        # ---- 内部 ----
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
