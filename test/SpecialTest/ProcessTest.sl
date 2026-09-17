import Std;

# ============================================================================
# ProcessTest — Std.OS.Process 外部进程执行功能测试
#
# 设计文档：csimple_lang/md/design/PROCESS_DESIGN.md
# 覆盖范围：
#   M1 便捷层：Process.run（变长 argv / 完整 info）、Process.shell
#   M2 控制层：Process.start + wait / kill / terminate / exitCode / hasExited / id
#   M3 管道：stdout/stderr 分别 Pipe 捕获、Null 重定向、env 注入
#
# 运行环境：Windows（以 cmd / ping 作为外部被启程序）
# 注意：本文件禁用行内注释（Lexer 单行 # 注释会吞换行，导致成员丢失）
# ============================================================================

class ProcessTest
{
    # 断言计数（供 fun 尾部汇总）
    static int passed = 0
    static int failed = 0

    # 简单断言辅助：打印 PASS / FAIL
    static check( string name, bool cond )
    {
        if ( cond )
        {
            passed = passed + 1
            Console.println( "  [PASS] " + name )
        }
        else
        {
            failed = failed + 1
            Console.println( "  [FAIL] " + name )
        }
    }

    # 朴素子串查找（string 无 contains 系统方法，用 charCodeAt 逐位比对）
    static bool strContains( string s, string sub )
    {
        int sn = s.length
        int pn = sub.length
        int i = 0
        int j = 0
        bool matched = true
        if ( pn == 0 )
        {
            ret true
        }
        if ( sn < pn )
        {
            ret false
        }
        for i = 0, i <= sn - pn, i += 1
        {
            matched = true
            for j = 0, j < pn, j += 1
            {
                if ( SystemStringCharCodeAt( s, i + j ) != SystemStringCharCodeAt( sub, j ) )
                {
                    matched = false
                    break
                }
            }
            if ( matched )
            {
                ret true
            }
        }
        ret false
    }

    # ---------------- M1：run 变长 argv（默认捕获 stdout） ----------------
    static testRunBasic()
    {
        Console.println( "===== ProcessTest.testRunBasic =====" )
        var r = OS.Process.run( "cmd", "/c", "echo sl-proc-hello" )
        check( "run argv: exitCode == 0", r.exitCode == 0 )
        check( "run argv: ok == true", r.ok )
        check( "run argv: stdOut contains sl-proc-hello", strContains( r.stdOut, "sl-proc-hello" ) )
    }

    # ---------------- M1：非零退出码不是失败（P5 语义） ----------------
    static testRunExitCode()
    {
        Console.println( "===== ProcessTest.testRunExitCode =====" )
        var r = OS.Process.run( "cmd", "/c", "exit 3" )
        check( "run exit 3: exitCode == 3", r.exitCode == 3 )
        check( "run exit 3: ok == false", r.ok == false )
    }

    # ---------------- M3：stdout / stderr 分别捕获 ----------------
    static testRunStdErr()
    {
        Console.println( "===== ProcessTest.testRunStdErr =====" )
        var info = OS.ProcessStartInfo.create( "cmd", "/c", "echo out-marker & echo err-marker 1>&2" )
        var r = OS.Process.run( info )
        check( "run pipe: exitCode == 0", r.exitCode == 0 )
        check( "run pipe: stdOut contains out-marker", strContains( r.stdOut, "out-marker" ) )
        check( "run pipe: stdErr contains err-marker", strContains( r.stdErr, "err-marker" ) )
    }

    # ---------------- M1：shell 显式路径（cmd /c） ----------------
    static testShell()
    {
        Console.println( "===== ProcessTest.testShell =====" )
        var r = OS.Process.shell( "echo shell-out-marker & echo shell-err-marker 1>&2" )
        check( "shell: exitCode == 0", r.exitCode == 0 )
        check( "shell: stdOut contains shell-out-marker", strContains( r.stdOut, "shell-out-marker" ) )
        check( "shell: stdErr contains shell-err-marker", strContains( r.stdErr, "shell-err-marker" ) )
    }

    # ---------------- M3：环境变量注入（env 链式） ----------------
    static testEnv()
    {
        Console.println( "===== ProcessTest.testEnv =====" )
        var info = OS.ProcessStartInfo.create( "cmd", "/c", "echo %SLPROC_VAR%" )
        info.env( "SLPROC_VAR", "env-marker-42" )
        var r = OS.Process.run( info )
        check( "env: exitCode == 0", r.exitCode == 0 )
        check( "env: stdOut contains env-marker-42", strContains( r.stdOut, "env-marker-42" ) )
    }

    # ---------------- arg() 链式追加参数 ----------------
    static testArgChain()
    {
        Console.println( "===== ProcessTest.testArgChain =====" )
        var info = OS.ProcessStartInfo.create( "cmd" )
        info.arg( "/c" )
        info.arg( "echo chained-arg-marker" )
        var r = OS.Process.run( info )
        check( "arg chain: exitCode == 0", r.exitCode == 0 )
        check( "arg chain: stdOut contains chained-arg-marker", strContains( r.stdOut, "chained-arg-marker" ) )
    }

    # ---------------- M2：start + wait + id + exitCode 生命周期 ----------------
    static testStartWait()
    {
        Console.println( "===== ProcessTest.testStartWait =====" )
        var info = OS.ProcessStartInfo.create( "cmd", "/c", "echo start-wait-marker" )
        info.stdout = OS.ProcessStdio.Pipe
        var p = OS.Process.start( info )
        check( "start: id > 0", p.id > 0 )
        int code = p.wait()
        check( "wait: returns 0", code == 0 )
        check( "wait: hasExited == true", p.hasExited )
        check( "wait: exitCode == 0", p.exitCode == 0 )
        check( "wait: readStdOut contains start-wait-marker", strContains( p.readStdOut(), "start-wait-marker" ) )
    }

    # ---------------- M2：未退出时读 exitCode 抛 NotExited（-83） ----------------
    static testExitCodeNotExited()
    {
        Console.println( "===== ProcessTest.testExitCodeNotExited =====" )
        # ping -n 30 保活约 29 秒，确保查询时进程仍在运行
        var info = OS.ProcessStartInfo.create( "ping", "-n", "30", "127.0.0.1" )
        info.stdout = OS.ProcessStdio.Null
        info.stderr = OS.ProcessStdio.Null
        var p = OS.Process.start( info )
        int caught = 0
        label exitCodeBlock
        {
            try p.exitCode
        }
        catch
        {
            caught = 1
        }
        check( "exitCode before exit: throws (NotExited -83)", caught == 1 )
        p.kill()
        check( "cleanup: kill after check", p.hasExited )
    }

    # ---------------- M2：kill 强制终止 ----------------
    static testKill()
    {
        Console.println( "===== ProcessTest.testKill =====" )
        var info = OS.ProcessStartInfo.create( "ping", "-n", "30", "127.0.0.1" )
        info.stdout = OS.ProcessStdio.Null
        info.stderr = OS.ProcessStdio.Null
        var p = OS.Process.start( info )
        check( "kill: running (hasExited == false)", p.hasExited == false )
        p.kill()
        check( "kill: hasExited == true", p.hasExited )
        check( "kill: exitCode == 1 (TerminateProcess code)", p.exitCode == 1 )
    }

    # ---------------- M2：terminate 友好终止（Windows 等同 kill） ----------------
    static testTerminate()
    {
        Console.println( "===== ProcessTest.testTerminate =====" )
        var info = OS.ProcessStartInfo.create( "ping", "-n", "30", "127.0.0.1" )
        info.stdout = OS.ProcessStdio.Null
        info.stderr = OS.ProcessStdio.Null
        var p = OS.Process.start( info )
        p.terminate()
        check( "terminate: hasExited == true", p.hasExited )
    }

    # ---------------- M3：Null 重定向（无捕获，输出进空设备） ----------------
    static testNullRedirect()
    {
        Console.println( "===== ProcessTest.testNullRedirect =====" )
        var info = OS.ProcessStartInfo.create( "cmd", "/c", "echo null-redirect-marker" )
        info.stdout = OS.ProcessStdio.Null
        info.stderr = OS.ProcessStdio.Null
        var p = OS.Process.start( info )
        p.wait()
        check( "null: exitCode == 0", p.exitCode == 0 )
        check( "null: readStdOut empty", p.readStdOut() == "" )
        check( "null: readStdErr empty", p.readStdErr() == "" )
    }

    # ---------------- 启动失败抛异常（SpawnFailed -81，P5） ----------------
    static testSpawnFail()
    {
        Console.println( "===== ProcessTest.testSpawnFail =====" )
        int caught = 0
        label spawnFailBlock
        {
            try OS.Process.run( "no-such-program-slproc-xyz" )
        }
        catch
        {
            caught = 1
        }
        check( "run missing program: throws (SpawnFailed -81)", caught == 1 )

        caught = 0
        label spawnFailStartBlock
        {
            var info = OS.ProcessStartInfo.create( "no-such-program-slproc-xyz" )
            try OS.Process.start( info )
        }
        catch
        {
            caught = 1
        }
        check( "start missing program: throws (SpawnFailed -81)", caught == 1 )
    }

    # ---------------- ProcessResult.toString 冒烟 ----------------
    static testToString()
    {
        Console.println( "===== ProcessTest.testToString =====" )
        var r = OS.Process.run( "cmd", "/c", "echo tostring-marker" )
        var s = r.toString()
        check( "toString: contains ProcessResult", strContains( s, "ProcessResult" ) )
        check( "toString: contains exitCode", strContains( s, "exitCode" ) )
    }

    # ---------------- 测试入口（由 ProjectTest.sp 调用） ----------------
    static fun()
    {
        Console.println( "========== ProcessTest start ==========" )
        testRunBasic()
        testRunExitCode()
        testRunStdErr()
        testShell()
        testEnv()
        testArgChain()
        testStartWait()
        testExitCodeNotExited()
        testKill()
        testTerminate()
        testNullRedirect()
        testSpawnFail()
        testToString()
        Console.println( "========== ProcessTest end: passed=" + passed.toString() + " failed=" + failed.toString() + " ==========" )
    }
}
