# ============================================================================
# Std/Debug/Profiling.sl — 计时性能分析类
#
# 职责（从原 Debug 类拆分，原文件已删除）：
#   Profiling ：「性能分析」—— time/timeEnd 轻量计时器（console.time 风格），
#               key 列表 + 起始毫秒列表并行存储，timeEnd 输出耗时并移除计时器。
#
# 追踪（trace/Enter/Exit/Step）请用 SLang.Trace；
# 基础日志（info/warn/error）与断言（assert）请直接用 SLang.Log。
#
# 用法：
#   Profiling.enabled = true
#   Profiling.time("phase1")
#   ...耗时操作...
#   Profiling.timeEnd("phase1")     # 输出 [TIME] phase1: N ms
# ============================================================================

namespace SLang
{
    public class Profiling
    {
        # 性能分析总开关：false 时计时静默
        static bool enabled = true

        # ---- 计时器（key 列表 + 起始毫秒列表，并行存储）----
        static bool         _inited    = false
        static List<string> _timerKeys = null
        static List<Int64>  _timerVals = null

        static void _ensure()
        {
            if !_inited
            {
                _timerKeys = List<string>()
                _timerVals = List<Int64>()
                _inited = true
            }
        }

        # 总开关
        public static void setEnabled( bool v ) { enabled = v }
        public static bool isEnabled() { ret enabled }

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
                Int64 t0 = _timerVals[idx]
                Int64 dt = SystemDateTimeNowMillis() - t0
                SLang.Log.print("[TIME] " + key + ": " + dt + " ms")
                _timerKeys.removeAt(idx)
                _timerVals.removeAt(idx)
            }
            else
            {
                SLang.Log.print("[TIME] " + key + ": <not started>")
            }
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
    }
}
