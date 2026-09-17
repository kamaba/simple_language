# ============================================================================
# Std/Debug/Debug.sl — 日志工具（Unity Debug 风格）
#
# 与 Console 的区别：
#   - Console 是「裸打印」（write/println/input），无级别、无文件落盘；
#   - Debug.Log 是「带级别 + 时间戳 + 可选文件落盘」的结构化日志。
#
# 用法：
#   SLang.Log.info("start")
#   SLang.Log.warning("low memory")
#   SLang.Log.setFilePath("run.log")        # 之后日志同时写入 run.log
#   SLang.Log.setLogType(true, true, true)  # (info级, warning级, error级) 开关
#   SLang.Log.setConsoleOutput(false)       # 关闭实时控制台输出（仅落盘）
#   SLang.Log.print("raw message")          # 实时原始打印（无级别前缀）
#
# 每条日志格式：[yyyy-MM-dd HH:mm:ss][LEVEL] message
# 实时输出：默认每条日志都会 SystemPrintln 实时打印到控制台（受 setConsoleOutput 控制）。
# 级别开关：Verbose/Debug 跟随 info；Warning、Error 各自独立；Fatal/断言 始终输出。
# ============================================================================

namespace SLang
{
    public class Log
    {
        # ---- 级别开关 ----
        static bool _enableInfo    = true   # 同时控制 Verbose / Debug
        static bool _enableWarning = true
        static bool _enableError   = true   # 同时控制 Fatal / Exception

        # ---- 文件输出 ----
        static string _logFilePath = ""
        static bool   _writeToFile = false

        # ---- 实时控制台输出 ----
        static bool _enableConsole = true

        # 设置各级别开关
        #   f  : 启用 Info 级（含 Verbose / Debug）
        #   f2 : 启用 Warning 级
        #   f3 : 启用 Error 级（含 Fatal / Exception）
        public static void setLogType( bool f, bool f2, bool f3 )
        {
            _enableInfo = f
            _enableWarning = f2
            _enableError = f3
        }

        # 设置日志输出文件路径；start=true 立即启用文件输出（文件不存在则创建空文件）
        public static void setFilePath( string path, bool start = true )
        {
            _logFilePath = path
            if start
            {
                _writeToFile = true
                if !SystemFileExists(path)
                {
                    SystemFileWriteAllText(path, "")
                }
            }
        }

        # 开关实时控制台输出（默认开启）
        #   enabled=false：关闭 SystemPrintln 实时打印，日志仅写入文件
        public static void setConsoleOutput( bool enabled )
        {
            _enableConsole = enabled
        }

        # 关闭文件输出（不再落盘，控制台仍照常输出）
        public static void closeFile()
        {
            _writeToFile = false
        }

        # 清空日志文件内容（仅在已设置路径时有效）
        public static void clearFile()
        {
            if _logFilePath != ""
            {
                SystemFileWriteAllText(_logFilePath, "")
            }
        }

        # 恢复默认：全部级别开启、停止文件输出、恢复实时控制台输出
        public static void reset()
        {
            _enableInfo = true
            _enableWarning = true
            _enableError = true
            _writeToFile = false
            _logFilePath = ""
            _enableConsole = true
        }

        # 按最小级别一次性开关：level <= 阈值才输出
        #   level: 0=verbose/debug, 1=info, 2=warning, 3=error, 4=fatal
        public static void setMinLevel( int level )
        {
            _enableInfo    = level <= 1
            _enableWarning = level <= 2
            _enableError   = level <= 3
        }

        # 查询某级别是否启用
        #   level: 0=debug/verbose, 1=info, 2=warning, 3=error, 4=fatal
        public static bool isEnabled( int level )
        {
            if level <= 1
            {
                ret _enableInfo
            }
            if level == 2
            {
                ret _enableWarning
            }
            ret _enableError
        }

        # ---- 输出方法 ----

        # 实时原始打印（不加级别前缀，受 _enableConsole 开关控制）
        public static void print( string msg )
        {
            if _enableConsole
            {
                SystemPrintln(msg, null)
            }
        }

        # 最细粒度（= debug）
        public static void verbose( string msg )
        {
            _emit("VERBOSE", msg, _enableInfo)
        }

        public static void debug( string msg )
        {
            _emit("DEBUG", msg, _enableInfo)
        }

        public static void info( string msg )
        {
            _emit("INFO", msg, _enableInfo)
        }

        public static void warning( string msg )
        {
            _emit("WARN", msg, _enableWarning)
        }

        # 方法名 logError：error 为 Result 语义保留名，类成员名须避让
        public static void logError( string msg )
        {
            _emit("ERROR", msg, _enableError)
        }

        # 致命错误：始终输出（无视级别开关，但受 _enableConsole 控制）
        public static void fatal( string msg )
        {
            _emit("FATAL", msg, true)
        }

        # 异常信息（含可选堆栈/详情）
        public static void exception( string msg )
        {
            _emit("EXCEPTION", msg, _enableError)
        }
        public static void exception( string msg, string detail )
        {
            _emit("EXCEPTION", msg + "\n    " + detail, _enableError)
        }

        # 通用：按级别输出（level 映射见 isEnabled）
        #   level: 0=debug/verbose, 1=info, 2=warning, 3=error, 4=fatal
        public static void log( int level, string msg )
        {
            string tag = "INFO"
            if level <= 1
            {
                tag = "DEBUG"
            }
            if level == 2
            {
                tag = "WARN"
            }
            if level == 3
            {
                tag = "ERROR"
            }
            if level >= 4
            {
                tag = "FATAL"
            }
            bool on = _enableInfo
            if level == 2
            {
                on = _enableWarning
            }
            if level >= 3
            {
                on = _enableError
            }
            _emit(tag, msg, on)
        }

        # 断言：flag 为 false 时输出断言失败（始终输出，无视级别开关）
        public static void asset( bool flag, string msg )
        {
            if !flag
            {
                _emit("ASSERT", "Assertion failed: " + msg, true)
            }
        }

        # ---- 内部 ----

        # 输出一条带时间戳与级别前缀的日志；enabled=false 时静默
        #   实时控制台输出受 _enableConsole 控制；文件输出受 _writeToFile 控制
        static void _emit( string level, string msg, bool enabled )
        {
            if !enabled
            {
                ret
            }
            string ts = SystemDateTimeFormat(SystemDateTimeNowMillis(), "yyyy-MM-dd HH:mm:ss", 0)
            string line = "[" + ts + "][" + level + "] " + msg
            if _enableConsole
            {
                SystemPrintln(line, null)
            }
            if _writeToFile && _logFilePath != ""
            {
                SystemFileAppendText(_logFilePath, line + "\n")
            }
        }
    }

    # ============================================================================
    # 非静态日志器：每个实例独立配置，可在 isolate / 协程中分别持有，
    # 互不干扰（与全局静态 Log 正好相反）。
    #
    # 用法：
    #   Logger lg = new Logger()
    #   lg._init_(true)                  # true = 开启实时控制台输出（默认）
    #   lg.setFilePath("iso1.log")
    #   lg.info("from isolate #1")
    # ============================================================================
    public class Logger
    {
        # ---- 级别开关 ----
        bool _enableInfo    = true
        bool _enableWarning = true
        bool _enableError   = true

        # ---- 文件输出 ----
        string _logFilePath = ""
        bool   _writeToFile = false

        # ---- 实时控制台输出 ----
        bool _enableConsole = true

        # 显式初始化（new 之后调用，确保状态干净）；consoleOutput 控制实时控制台输出
        public void _init_( bool consoleOutput = true )
        {
            _enableInfo = true
            _enableWarning = true
            _enableError = true
            _writeToFile = false
            _logFilePath = ""
            _enableConsole = consoleOutput
        }

        public void setLogType( bool f, bool f2, bool f3 )
        {
            _enableInfo = f
            _enableWarning = f2
            _enableError = f3
        }

        public void setFilePath( string path, bool start = true )
        {
            _logFilePath = path
            if start
            {
                _writeToFile = true
                if !SystemFileExists(path)
                {
                    SystemFileWriteAllText(path, "")
                }
            }
        }

        public void setConsoleOutput( bool enabled )
        {
            _enableConsole = enabled
        }

        public void closeFile()
        {
            _writeToFile = false
        }

        public void clearFile()
        {
            if _logFilePath != ""
            {
                SystemFileWriteAllText(_logFilePath, "")
            }
        }

        public void reset()
        {
            _enableInfo = true
            _enableWarning = true
            _enableError = true
            _writeToFile = false
            _logFilePath = ""
            _enableConsole = true
        }

        public void setMinLevel( int level )
        {
            _enableInfo    = level <= 1
            _enableWarning = level <= 2
            _enableError   = level <= 3
        }

        public bool isEnabled( int level )
        {
            if level <= 1
            {
                ret _enableInfo
            }
            if level == 2
            {
                ret _enableWarning
            }
            ret _enableError
        }

        # ---- 输出方法 ----

        public void print( string msg )
        {
            if _enableConsole
            {
                SystemPrintln(msg, null)
            }
        }

        public void verbose( string msg )
        {
            _emit("VERBOSE", msg, _enableInfo)
        }

        public void debug( string msg )
        {
            _emit("DEBUG", msg, _enableInfo)
        }

        public void info( string msg )
        {
            _emit("INFO", msg, _enableInfo)
        }

        public void warning( string msg )
        {
            _emit("WARN", msg, _enableWarning)
        }

        public void logError( string msg )
        {
            _emit("ERROR", msg, _enableError)
        }

        public void fatal( string msg )
        {
            _emit("FATAL", msg, true)
        }

        public void exception( string msg )
        {
            _emit("EXCEPTION", msg, _enableError)
        }
        public void exception( string msg, string detail )
        {
            _emit("EXCEPTION", msg + "\n    " + detail, _enableError)
        }

        public void log( int level, string msg )
        {
            string tag = "INFO"
            if level <= 1
            {
                tag = "DEBUG"
            }
            if level == 2
            {
                tag = "WARN"
            }
            if level == 3
            {
                tag = "ERROR"
            }
            if level >= 4
            {
                tag = "FATAL"
            }
            bool on = _enableInfo
            if level == 2
            {
                on = _enableWarning
            }
            if level >= 3
            {
                on = _enableError
            }
            _emit(tag, msg, on)
        }

        public void asset( bool flag, string msg )
        {
            if !flag
            {
                _emit("ASSERT", "Assertion failed: " + msg, true)
            }
        }

        void _emit( string level, string msg, bool enabled )
        {
            if !enabled
            {
                ret
            }
            string ts = SystemDateTimeFormat(SystemDateTimeNowMillis(), "yyyy-MM-dd HH:mm:ss", 0)
            string line = "[" + ts + "][" + level + "] " + msg
            if _enableConsole
            {
                SystemPrintln(line, null)
            }
            if _writeToFile && _logFilePath != ""
            {
                SystemFileAppendText(_logFilePath, line + "\n")
            }
        }
    }
}
