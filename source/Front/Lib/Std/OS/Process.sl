namespace OS
{
    # ============================================================================
    # OS/Process.sl — 外部进程执行（设计：csimple_lang/md/design/PROCESS_DESIGN.md）
    #
    # 用法:
    #   import Std;
    #   # 1) 便捷：变长 argv，一次性拿结果（run 默认捕获 stdout/stderr）
    #   var r = OS.Process.run("cmd", "/c", "echo hello")
    #   Console.println(r.exitCode, r.stdOut)
    #   # 2) 显式 shell（Windows: cmd /c；Unix: /bin/sh -c）
    #   var r2 = OS.Process.shell("echo hello")
    #   # 3) 控制：start 返回句柄对象，wait/kill/readStdOut
    #   var info = OS.ProcessStartInfo.create("cmd", "/c", "echo hi")
    #   info.stdout = OS.ProcessStdio.Pipe
    #   info.stderr = OS.ProcessStdio.Pipe
    #   var p = OS.Process.start(info)
    #   p.wait()
    #   Console.println(p.exitCode, p.readStdOut())
    #
    # 语义要点（与设计文档 §3 对应）:
    #   - P1 双层 API：run/shell 便捷 + start 句柄控制
    #   - P2 argv 优先防注入；shell 路径显式开启
    #   - P3 句柄对象稳定：_handle 为 C VM 注册表自增 id（同 Core.Task），
    #     SL 侧 == / null 检查稳定
    #   - P5 启动失败（找不到程序等）由 C VM 抛异常（错误码 -81..-85）；
    #     退出非零不视为失败，体现在 exitCode
    # ============================================================================

    # stdio 重定向策略（stdin/stdout/stderr 各自独立配置）
    # File(path) 文件重定向与 stdin Pipe 写入留待二期（设计 §4.1/§8）
    public enum ProcessStdio extends Int32
    {
        # 继承 VM 进程的标准流
        Inherit = 0
        # 捕获到内存（stdout/stderr 分别可读取）
        Pipe = 1
        # 重定向到空设备（Windows: NUL / Unix: /dev/null）
        Null = 2
    }

    # Process 错误码（镜像 C VM 负数段 VM_PROC_ERR_*，-81..-85）
    # 注：设计稿 §7 原定 -71..-79，与 isolate 的 VM_ISO_ERR_*(-70..-77) 冲突，整体后移
    public enum ProcessError extends Error
    {
        # -81 进程启动失败（文件不存在/权限不足）
        SpawnFailed = { code = 1 }
        # -82 句柄无效或已关闭
        InvalidHandle = { code = 2 }
        # -83 尚未退出时读取 exitCode
        NotExited = { code = 3 }
        # -84 kill/terminate 失败
        KillFailed = { code = 4 }
        # -85 输出读取失败（非 Pipe 模式/管道已关闭）
        ReadFailed = { code = 5 }
    }

    # 进程启动配置（builder 对象，字段与 C# ProcessStartInfo 对应）
    public class ProcessStartInfo extends Object
    {
        # 可执行文件路径或命令名（走 PATH 解析）
        public string program = ""
        # argv 列表（不含 program 自身）；null 视为无参数
        public Array<string> arguments = null
        # 工作目录；空串 = 继承 VM 进程当前目录
        public string workingDirectory = ""
        # 追加/覆盖的环境变量（key=value）；null 视为不修改
        public Map<string,string> environment = null
        # true: 在父进程环境上追加 environment；false: 仅用 environment
        public bool inheritEnvironment = true
        # 三个标准流各自的重定向策略（一期 stdin 仅支持 Inherit/Null）
        public ProcessStdio stdin = ProcessStdio.Inherit
        public ProcessStdio stdout = ProcessStdio.Inherit
        public ProcessStdio stderr = ProcessStdio.Inherit

        # 便捷工厂：program + 变长 argv（第二个参数起即 argv，勿再写 info.arguments.add）
        public static ProcessStartInfo create( string program, params string[] args )
        {
            ProcessStartInfo info = new()
            info.program = program
            if args != null
            {
                int count = args.length
                info.arguments = Array<string>( count )
                for i = 0, i < count, i++
                {
                    SystemArraySetValueThis( info.arguments, i, SystemArrayGetValueThis( args, i ) )
                }
            }
            ret info
        }

        # 追加单个 argv 参数（链式便利方法）
        public ProcessStartInfo arg( string value )
        {
            if this.arguments == null
            {
                this.arguments = Array<string>( 1 )
                SystemArraySetValueThis( this.arguments, 0, value )
                ret this
            }
            int oldLen = this.arguments.length
            Array<string> grown = Array<string>( oldLen + 1 )
            for i = 0, i < oldLen, i++
            {
                SystemArraySetValueThis( grown, i, SystemArrayGetValueThis( this.arguments, i ) )
            }
            SystemArraySetValueThis( grown, oldLen, value )
            this.arguments = grown
            ret this
        }

        # 设置/覆盖单个环境变量（链式便利方法）
        public ProcessStartInfo env( string key, string value )
        {
            if this.environment == null
            {
                this.environment = Map<string,string>()
            }
            this.environment[key] = value
            ret this
        }
    }

    # run() 的聚合结果（exitCode + 捕获的输出）
    public class ProcessResult extends Object
    {
        # 进程退出码（非零不视为失败，由调用方判断 ok）
        public int exitCode = 0
        # Pipe 模式下捕获的标准输出；未捕获为空串
        public string stdOut = ""
        # Pipe 模式下捕获的标准错误；未捕获为空串
        public string stdErr = ""

        # 是否成功退出（exitCode == 0）
        public get bool ok()
        {
            ret this.exitCode == 0
        }

        override string toString()
        {
            ret "ProcessResult{exitCode:" + this.exitCode.toString() + ",stdOut:" + this.stdOut + ",stdErr:" + this.stdErr + "}"
        }
    }

    # 进程句柄对象（SL 侧不持有任何平台结构，只存 _handle；行为经系统方法下探）
    public class Process extends Object
    {
        # VM 内部进程句柄（注册表 id，由 C VM 单调递增分配并直接写入；同 Core.Task 模式）
        Int64 _handle = 0

        # ------------------------------------------------------------------
        # 便捷路径：启动并阻塞直到结束，返回聚合结果
        # （stdout/stderr 为 Inherit 时自动升级为 Pipe 以便捕获）
        # ------------------------------------------------------------------

        # 变长 argv 便捷入口
        public static ProcessResult run( string program, params string[] args ) throws
        {
            ProcessStartInfo info = new()
            info.program = program
            if args != null
            {
                int count = args.length
                info.arguments = Array<string>( count )
                for i = 0, i < count, i++
                {
                    SystemArraySetValueThis( info.arguments, i, SystemArrayGetValueThis( args, i ) )
                }
            }
            ret OS.Process.run( info )
        }

        # 完整配置入口
        public static ProcessResult run( ProcessStartInfo info ) throws
        {
            ret SystemProcessRun( info )
        }

        # ------------------------------------------------------------------
        # 显式 shell 路径：经 cmd /c 或 /bin/sh -c（stdout/stderr 均捕获）
        # ------------------------------------------------------------------
        public static ProcessResult shell( string command ) throws
        {
            ret SystemProcessShell( command )
        }

        # ------------------------------------------------------------------
        # 控制路径：启动后返回句柄对象（stdio 按配置原样生效）
        # ------------------------------------------------------------------
        public static Process start( ProcessStartInfo info ) throws
        {
            ret SystemProcessStart( info )
        }

        # ------------------------------------------------------------------
        # 实例属性/方法
        # ------------------------------------------------------------------

        # 平台进程 id（Windows 为 dwProcessId）
        public get Int64 id() throws
        {
            ret SystemProcessGetId( this )
        }

        # 是否已退出（非阻塞查询）
        public get bool hasExited() throws
        {
            ret SystemProcessHasExited( this )
        }

        # 退出码（尚未退出时 C VM 抛 ProcessError.NotExited，先用 hasExited 判断）
        public get int exitCode() throws
        {
            ret SystemProcessExitCode( this )
        }

        # 阻塞等待直到结束，返回 exitCode
        public int wait() throws
        {
            ret SystemProcessWait( this )
        }

        # 强制终止（Windows TerminateProcess / Unix SIGKILL）
        public void kill() throws
        {
            SystemProcessKill( this )
        }

        # 友好终止（Unix SIGTERM；Windows 等同 kill）
        public void terminate() throws
        {
            SystemProcessTerminate( this )
        }

        # 读取捕获的标准输出（等待结束后整体读取；非 Pipe 模式返回空串）
        public string readStdOut() throws
        {
            ret SystemProcessReadStdOut( this )
        }

        # 读取捕获的标准错误（同上）
        public string readStdErr() throws
        {
            ret SystemProcessReadStdErr( this )
        }

        override string toString()
        {
            ret "Process-" + this._handle.toString()
        }
    }
}
