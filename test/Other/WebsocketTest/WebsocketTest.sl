﻿import Std;
import Core;

# ============================================================================
# WebsocketTest.sl -- RFC 6455 WebSocket 集成测试（独立工程，本机回环）
#
# 客户端以 Net.WebSocketClient 回调模式为主（onOpen / onMessage / onClose
# 事件驱动，Python WebSocketApp 风格）；G/H 两组保留协议层 WebSocketStream
# 的 messages() Stream / connectAsync 协程用法。
#
# 回调观测约定：回调闭包只写全局静态观测变量（g_open / g_msg / g_lastText /
#   g_close / g_cli* 等），主流程经 waitOpen / waitMsg 轮询等齐后再 check
#   （CodecFrameTest I 组主流程轮询同款；每组开始 resetObs 重置）。
#
# 覆盖功能点（A-J 组）：
#   A   握手与文本回显       回调模式 connect：onOpen 恰一次 / state / isOpen /
#                            protocol getter；ASCII + UTF-8 文本经 onMessage 往返
#   B   子协议协商           客户端 protocols 配置字段 "chat,echo" x 服务端支持
#                            echo -> 双方协商结果 echo（Sec-WebSocket-Protocol）
#   C   二进制消息           sendBinary(ByteBuffer)（含 0x00/0xFF 非文本字节）
#                            + sendBinary(UInt8Array) 重载，onMessage 内校验
#   D   分片重组             服务端裸帧发 Text fin=0 / Cont fin=0 / Cont fin=1
#                            三帧，onMessage 收到重组完整消息
#   E   Ping/Pong 心跳       服务端裸帧发 Ping -> 事件循环 receive 内部
#                            autoPong 自动回 Pong -> 服务端裸读校验 Pong 帧
#   F   Close 协商           客户端 close(1001,"going away") 优雅关闭；onClose
#                            收到协商 closeInfo；双方 code/reason/wasClean 收敛
#   G   messages() Stream    协议层：消息流引流协程 + 迭代器逐条消费（顺序校验）
#   H   connectAsync 协程    协议层：两路并发异步连接 waitAll2 聚合 + echo 往返
#   I   isolate worker       3 个 worker isolate 内各自 WebSocketClient 回调
#                            connect/echo（isolate 内协程可用，IsolateTest H1
#                            同款；流不可 Sendable，只把结果 int 传回）
#   J   错误路径             非 101 响应 -> Handshake(1)；http scheme ->
#                            UnsupportedScheme(2)；连接被拒 -> ConnectFailed(2)；
#                            Closed 后发送 -> Closed(4)
#
# 端口：19371-19381（NetTest 19301-19370 段之后）。
# 网络健壮性：每个组独立 label{}...catch{}——异常整组记一条 FAIL 后继续，
#   不阻断其余组（HttpWebTest 同款取舍）。
# 错误码取值依据：WsError.Handshake=1 / Closed=4；HttpError.UnsupportedScheme=2；
#   NetError.ConnectFailed=2。
# 编写约定同 HttpWebTest.sl / NetTest/NetIsolateTest.sl。
# ============================================================================

WebsocketTest
{
    static Int32 s_pass = 0
    static Int32 s_fail = 0

    # ---- 服务端观测变量（isolate/协程与主线程回传） ----
    static string g_srvProto = "?"
    static Int32 g_srvCloseCode = 0
    static string g_srvCloseReason = ""
    static bool g_srvCloseClean = false
    static Int32 g_ePong = 0

    # ---- 客户端回调观测（WebSocketClient 回调闭包只写这里；每组 resetObs） ----
    static Int32 g_open = 0          # onOpen 触发数
    static Int32 g_msg = 0           # onMessage 触发数
    static string g_lastText = ""    # 最后一条 Text 消息
    static Int32 g_close = 0         # onClose 触发数
    static Int32 g_cliCode = -1      # onClose 收到的 code（-1 = info null）
    static string g_cliReason = ""   # onClose 收到的 reason
    static bool g_cliClean = false   # onClose 收到的 wasClean
    static bool g_c1 = false         # C 组第 1 条二进制校验结果
    static bool g_c2 = false         # C 组第 2 条二进制校验结果

    # 断言辅助：OK/FAIL 单行输出 + 计数（HttpWebTest 同款）
    static check( string name, bool cond )
    {
        if cond
        {
            WebsocketTest.s_pass = WebsocketTest.s_pass + 1
            Console.println( "[WebsocketTest] " + name + " : OK" )
        }
        else
        {
            WebsocketTest.s_fail = WebsocketTest.s_fail + 1
            Console.println( "[WebsocketTest] " + name + " : FAIL" )
        }
    }

    # 网络组异常兜底：整组记一条 FAIL 后继续
    static groupError( string group, Int32 code, string message )
    {
        WebsocketTest.s_fail = WebsocketTest.s_fail + 1
        Console.println( "[WebsocketTest] " + group + " : FAIL (network error code=" + code.toString() + " " + message + ")" )
    }

    # 回调观测重置（每组开始调用）
    static resetObs()
    {
        WebsocketTest.g_open = 0
        WebsocketTest.g_msg = 0
        WebsocketTest.g_lastText = ""
        WebsocketTest.g_close = 0
        WebsocketTest.g_cliCode = -1
        WebsocketTest.g_cliReason = ""
        WebsocketTest.g_cliClean = false
        WebsocketTest.g_c1 = false
        WebsocketTest.g_c2 = false
    }

    # 带超时等待 onOpen 触发（事件循环协程异步推进；超时放弃由后续 check 判 FAIL）
    static waitOpen( Int32 timeoutMs )
    {
        Int32 spin = 0
        while WebsocketTest.g_open < 1 && spin < timeoutMs
        {
            Coroutine.delay( 10 )
            spin = spin + 10
        }
    }

    # 带超时等待 onMessage 累计到 target 条
    static waitMsg( Int32 target, Int32 timeoutMs )
    {
        Int32 spin = 0
        while WebsocketTest.g_msg < target && spin < timeoutMs
        {
            Coroutine.delay( 10 )
            spin = spin + 10
        }
    }

    # ============================================================================
    # 服务端辅助
    # ============================================================================

    # 构造服务端未掩码小帧（FIN|opcode + 7 位长度 + ASCII 载荷，len < 126）
    # 注意：数值变量传参不做自动窄化转换（字面量才可以），先转局部 UInt8
    static ByteBuffer sfFrame( Int32 b0, string payload )
    {
        ByteBuffer p = ByteBuffer.fromString( payload )
        Int32 n = p.readableBytes
        ByteBuffer f = ByteBuffer( n + 2 )
        UInt8 h0 = b0
        UInt8 hn = n
        f.writeU8( h0 )
        f.writeU8( hn )
        if n > 0
        {
            f.writeBytes( p, n )
        }
        ret f
    }

    # 排水到对端关闭（读返回 0 / -1 退出）
    static sfDrain( Net.TcpStream raw ) throws
    {
        while true
        {
            ByteBuffer drain = ByteBuffer( 64 )
            Int32 n = raw.read( drain )
            if n <= 0
            {
                break
            }
        }
    }

    # 通用 echo 服务器：服务 conns 条连接，每条回显所有消息直到对端关闭；
    # protocols 非空时启用子协议协商并把结果记入 g_srvProto
    static Int32 sfEchoServer( Net.TcpServer srv, Int32 conns, string protocols ) throws
    {
        Net.WsOptions opt = null
        if SystemStringLength( protocols ) > 0
        {
            opt = Net.WsOptions()
            opt.protocols = protocols
        }
        Int32 i = 0
        while i < conns
        {
            Net.TcpStream raw = srv.accept()
            Net.WebSocketStream ws = Net.WebSocketStream.accept( raw, opt )
            WebsocketTest.g_srvProto = ws.protocol
            while true
            {
                Net.WsMessage msg = ws.receive()
                if msg == null
                {
                    break
                }
                if msg.isText
                {
                    ws.sendText( msg.text() )
                }
                else
                {
                    ws.sendBinary( msg.binary() )
                }
                msg.release()
            }
            ws.close()
            i = i + 1
        }
        srv.close()
        ret 1
    }

    # 单连接 echo：回显所有消息直到对端关闭（供并发服务端 spawn 的协程复用）
    static Int32 sfEchoOne( Net.WebSocketStream ws ) throws
    {
        while true
        {
            Net.WsMessage msg = ws.receive()
            if msg == null
            {
                break
            }
            if msg.isText
            {
                ws.sendText( msg.text() )
            }
            else
            {
                ws.sendBinary( msg.binary() )
            }
            msg.release()
        }
        ws.close()
        ret 1
    }

    # 并发 echo 服务器：主循环串行 accept，每条连接 spawn 独立协程 echo——
    # 供 H 组两个并发 connectAsync 使用（串行版会死锁：首连接 echo 循环阻塞
    # receive，服务端不进入下一次 accept，后续握手永远等不到 101）
    static Int32 sfConcEchoServer( Net.TcpServer srv, Int32 conns, string protocols ) throws
    {
        Net.WsOptions opt = null
        if SystemStringLength( protocols ) > 0
        {
            opt = Net.WsOptions()
            opt.protocols = protocols
        }
        Int32 i = 0
        while i < conns
        {
            Net.TcpStream raw = srv.accept()
            Net.WebSocketStream ws = Net.WebSocketStream.accept( raw, opt )
            WebsocketTest.g_srvProto = ws.protocol
            # 带参函数值 + spawn：实参在 spawn 点立即求值（循环变量取当次值）
            function ef = function( Net.WebSocketStream c )
            {
                object rr = try? WebsocketTest.sfEchoOne( c )
            }
            Task et = spawn ef( ws )
            i = i + 1
        }
        srv.close()
        ret 1
    }

    # Close 协商服务器：回显一条消息后等 Close，记录对端关闭码/原因
    static Int32 sfCloseServer( Net.TcpServer srv ) throws
    {
        Net.TcpStream raw = srv.accept()
        Net.WebSocketStream ws = Net.WebSocketStream.accept( raw )
        Net.WsMessage m = ws.receive()
        if m != null
        {
            ws.sendText( m.text() )
            m.release()
        }
        Net.WsMessage m2 = ws.receive()
        Net.WsCloseInfo ci = ws.closeInfo
        if ci != null
        {
            WebsocketTest.g_srvCloseCode = ci.code
            WebsocketTest.g_srvCloseReason = ci.reason
            WebsocketTest.g_srvCloseClean = ci.wasClean
        }
        ws.close()
        srv.close()
        ret 1
    }

    # 分片服务器：握手后向裸连接写三帧未掩码分片（Text fin=0 / Cont fin=0 /
    # Cont fin=1），客户端 receive() 应重组完整消息
    static Int32 sfFragServer( Net.TcpServer srv ) throws
    {
        Net.TcpStream raw = srv.accept()
        Net.WebSocketStream ws = Net.WebSocketStream.accept( raw )
        raw.write( WebsocketTest.sfFrame( 0x01, "frag-" ) )
        raw.write( WebsocketTest.sfFrame( 0x00, "ment-" ) )
        raw.write( WebsocketTest.sfFrame( 0x80, "ok" ) )
        WebsocketTest.sfDrain( raw )
        ws.close()
        srv.close()
        ret 1
    }

    # Ping 服务器：握手后裸帧发 Ping -> 读对端回的 Pong 帧（客户端帧掩码
    # 位置位：第二字节 0x80）-> 再发 Text 让客户端事件循环收到
    static Int32 sfPingServer( Net.TcpServer srv ) throws
    {
        Net.TcpStream raw = srv.accept()
        Net.WebSocketStream ws = Net.WebSocketStream.accept( raw )
        raw.write( WebsocketTest.sfFrame( 0x89, "" ) )
        ByteBuffer acc = ByteBuffer( 8 )
        while acc.readableBytes < 2
        {
            ByteBuffer b = ByteBuffer( 8 )
            Int32 n = raw.read( b )
            if n <= 0
            {
                break
            }
            acc.writeBytes( b )
        }
        if acc.readableBytes >= 2
        {
            Int32 b0 = acc.readU8()
            Int32 b1 = acc.readU8()
            if b0 == 0x8A && b1 == 0x80
            {
                WebsocketTest.g_ePong = 1
            }
        }
        raw.write( WebsocketTest.sfFrame( 0x81, "pong-ok" ) )
        WebsocketTest.sfDrain( raw )
        ws.close()
        srv.close()
        ret 1
    }

    # 坏握手服务器：读掉请求后回 HTTP 200（非 101）
    static Int32 sfBadHandshakeServer( Net.TcpServer srv ) throws
    {
        Net.TcpStream c = srv.accept()
        ByteBuffer drain = ByteBuffer( 256 )
        c.read( drain )
        c.write( ByteBuffer.fromString( "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n" ) )
        c.close()
        srv.close()
        ret 1
    }

    # ============================================================================
    # isolate worker（WebSocketStream/Client 不可 Sendable：worker 内自行
    # connect，只把结果 int 传回——NetIsolateTest 同款取舍；isolate 内协程
    # 可用，IsolateTest H1 先例，事件循环跑在 worker 自己的协程调度器上）
    # ============================================================================

    static Int32 sfWsWorker( Int32 port ) throws
    {
        # worker isolate 持有全局观测的独立副本，回调写副本、收尾时判读
        WebsocketTest.g_msg = 0
        WebsocketTest.g_lastText = ""
        Net.WebSocketClient c = Net.WebSocketClient()
        string req = "iso-" + port.toString()
        function hMsg = function( Net.WsMessage m )
        {
            if m.isText
            {
                WebsocketTest.g_lastText = m.text()
            }
            WebsocketTest.g_msg = WebsocketTest.g_msg + 1
            m.release()
        }
        c.onMessage = hMsg
        c.connect( "ws://127.0.0.1:" + port.toString() + "/iso" )
        c.sendText( req )
        c.close()
        c.waitClosed()
        if WebsocketTest.g_lastText == req
        {
            ret 1
        }
        ret 0
    }

    # isolate 入口工厂（闭包工厂：每次调用独立闭包实例，避免共享捕获）
    static Func<int, int> isoWsFn()
    {
        Func<int, int> f = function( int port )
        {
            Int32 ok = 0
            object r = try? WebsocketTest.sfWsWorker( port )
            if r != null
            {
                Int32 v = r as int
                if v == 1
                {
                    ok = 1
                }
            }
            ret ok
        }
        ret f
    }

    # ============================================================================
    # A: 握手与文本回显（回调模式）
    # ============================================================================

    static testGroupA()
    {
        Console.println( "========== A: 握手与文本回显 ==========" )
        label labA
        {
            Net.TcpServer srv = Net.Tcp.listen( 19371 )
            function hf = function()
            {
                label runSrvA
                {
                    Int32 r = WebsocketTest.sfEchoServer( srv, 1, "" )
                }
                catch e
                {
                    Console.println( "A srv coroutine failed" )
                }
            }
            Task st = spawn hf()

            WebsocketTest.resetObs()
            Net.WebSocketClient c = Net.WebSocketClient()
            function hOpen = function()
            {
                WebsocketTest.g_open = WebsocketTest.g_open + 1
            }
            function hMsg = function( Net.WsMessage m )
            {
                if m.isText
                {
                    WebsocketTest.g_lastText = m.text()
                }
                WebsocketTest.g_msg = WebsocketTest.g_msg + 1
                m.release()
            }
            function hClose = function( Net.WsCloseInfo info )
            {
                WebsocketTest.g_close = WebsocketTest.g_close + 1
            }
            c.onOpen = hOpen
            c.onMessage = hMsg
            c.onClose = hClose
            c.connect( "ws://127.0.0.1:19371/hello" )

            WebsocketTest.waitOpen( 3000 )
            check( "A1 onOpen fired once", WebsocketTest.g_open == 1 )
            check( "A2 state open after handshake", c.state == Net.WsState.Open )
            check( "A3 client open flag", c.isOpen == true )
            check( "A4 empty protocol", c.protocol == "" )

            c.sendText( "hello ws" )
            WebsocketTest.waitMsg( 1, 3000 )
            check( "A5 ascii text echo via onMessage", WebsocketTest.g_lastText == "hello ws" )

            c.sendText( "你好，WebSocket" )
            WebsocketTest.waitMsg( 2, 3000 )
            check( "A6 utf-8 text echo via onMessage", WebsocketTest.g_lastText == "你好，WebSocket" )

            c.close()
            c.waitClosed()
            Coroutine.awaitTask( st )
            check( "A7 onClose fired once", WebsocketTest.g_close == 1 )
            check( "A8 state closed", c.state == Net.WsState.Closed )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "A", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "A", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # B: 子协议协商（回调模式 + protocols 配置字段）
    # ============================================================================

    static testGroupB()
    {
        Console.println( "========== B: 子协议协商 ==========" )
        label labB
        {
            Net.TcpServer srv = Net.Tcp.listen( 19372 )
            WebsocketTest.g_srvProto = "?"
            function hf = function()
            {
                object r = try? WebsocketTest.sfEchoServer( srv, 1, "echo" )
            }
            Task st = spawn hf()

            WebsocketTest.resetObs()
            Net.WebSocketClient c = Net.WebSocketClient()
            c.protocols = "chat,echo"
            function hMsg = function( Net.WsMessage m )
            {
                if m.isText
                {
                    WebsocketTest.g_lastText = m.text()
                }
                WebsocketTest.g_msg = WebsocketTest.g_msg + 1
                m.release()
            }
            c.onMessage = hMsg
            c.connect( "ws://127.0.0.1:19372/proto" )

            WebsocketTest.waitOpen( 3000 )
            check( "B1 client negotiated protocol", c.protocol == "echo" )

            c.sendText( "proto-hi" )
            WebsocketTest.waitMsg( 1, 3000 )
            check( "B2 echo on negotiated conn", WebsocketTest.g_lastText == "proto-hi" )

            c.close()
            c.waitClosed()
            Coroutine.awaitTask( st )
            check( "B3 server negotiated protocol", WebsocketTest.g_srvProto == "echo" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "B", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "B", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # C: 二进制消息（回调模式）
    # ============================================================================

    static testGroupC()
    {
        Console.println( "========== C: 二进制消息 ==========" )
        label labC
        {
            Net.TcpServer srv = Net.Tcp.listen( 19373 )
            function hf = function()
            {
                object r = WebsocketTest.sfEchoServer( srv, 1, "" )
            }
            Task st = spawn hf()

            WebsocketTest.resetObs()
            Net.WebSocketClient c = Net.WebSocketClient()
            function hMsg = function( Net.WsMessage m )
            {
                # 第 1 条 = ByteBuffer 回显（含 0x00/0xFF 非文本字节）；第 2 条 = UInt8Array 回显
                if m.isBinary && WebsocketTest.g_msg == 0 && m.size == 4
                {
                    ByteBuffer p = m.binary()
                    Int32 b0 = p.readU8()
                    Int32 b1 = p.readU8()
                    Int32 b2 = p.readU8()
                    Int32 b3 = p.readU8()
                    WebsocketTest.g_c1 = b0 == 0x00 && b1 == 0xFF && b2 == 0x7F && b3 == 0x80
                }
                elif m.isBinary && WebsocketTest.g_msg == 1 && m.size == 7
                {
                    ByteBuffer p2 = m.binary()
                    string s = p2.readString( p2.readableBytes )
                    WebsocketTest.g_c2 = s == "bin-arr"
                }
                WebsocketTest.g_msg = WebsocketTest.g_msg + 1
                m.release()
            }
            c.onMessage = hMsg
            c.connect( "ws://127.0.0.1:19373/bin" )
            WebsocketTest.waitOpen( 3000 )

            # C1: ByteBuffer 重载（含非文本字节 0x00 / 0xFF / 0x7F / 0x80）
            ByteBuffer b = ByteBuffer( 16 )
            b.writeU8( 0x00 )
            b.writeU8( 0xFF )
            b.writeU8( 0x7F )
            b.writeU8( 0x80 )
            c.sendBinary( b )

            # C2: UInt8Array 重载（字符串字面量直调 toUInt8Array 解析不了，经 ByteBuffer 转换）
            ByteBuffer srcArr = ByteBuffer.fromString( "bin-arr" )
            UInt8Array arr = srcArr.toArray()
            c.sendBinary( arr )

            WebsocketTest.waitMsg( 2, 3000 )
            check( "C1 binary byte echo via onMessage", WebsocketTest.g_c1 )
            check( "C2 uint8array binary echo via onMessage", WebsocketTest.g_c2 )

            c.close()
            c.waitClosed()
            Coroutine.awaitTask( st )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "C", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "C", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # D: 分片重组（回调模式）
    # ============================================================================

    static testGroupD()
    {
        Console.println( "========== D: 分片重组 ==========" )
        label labD
        {
            Net.TcpServer srv = Net.Tcp.listen( 19374 )
            function hf = function()
            {
                object r = try? WebsocketTest.sfFragServer( srv )
            }
            Task st = spawn hf()

            WebsocketTest.resetObs()
            Net.WebSocketClient c = Net.WebSocketClient()
            function hMsg = function( Net.WsMessage m )
            {
                if m.isText
                {
                    WebsocketTest.g_lastText = m.text()
                }
                WebsocketTest.g_msg = WebsocketTest.g_msg + 1
                m.release()
            }
            c.onMessage = hMsg
            c.connect( "ws://127.0.0.1:19374/frag" )

            WebsocketTest.waitMsg( 1, 3000 )
            check( "D1 fragmented message reassembly via onMessage", WebsocketTest.g_lastText == "frag-ment-ok" )

            c.close()
            c.waitClosed()
            Coroutine.awaitTask( st )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "D", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "D", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # E: Ping/Pong 心跳（回调模式）
    # ============================================================================

    static testGroupE()
    {
        Console.println( "========== E: Ping/Pong 心跳 ==========" )
        label labE
        {
            Net.TcpServer srv = Net.Tcp.listen( 19375 )
            WebsocketTest.g_ePong = 0
            function hf = function()
            {
                object r = try? WebsocketTest.sfPingServer( srv )
            }
            Task st = spawn hf()

            WebsocketTest.resetObs()
            Net.WebSocketClient c = Net.WebSocketClient()
            function hMsg = function( Net.WsMessage m )
            {
                if m.isText
                {
                    WebsocketTest.g_lastText = m.text()
                }
                WebsocketTest.g_msg = WebsocketTest.g_msg + 1
                m.release()
            }
            c.onMessage = hMsg
            c.connect( "ws://127.0.0.1:19375/ping" )

            # 事件循环 receive 内部消化 Ping（autoPong 自动回 Pong），onMessage 只见后续 Text
            WebsocketTest.waitMsg( 1, 3000 )
            check( "E1 onMessage skips ping/pong", WebsocketTest.g_lastText == "pong-ok" )

            c.close()
            c.waitClosed()
            Coroutine.awaitTask( st )
            check( "E2 server observed pong frame", WebsocketTest.g_ePong == 1 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "E", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "E", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # F: Close 协商（回调模式 + 优雅关闭）
    # ============================================================================

    static testGroupF()
    {
        Console.println( "========== F: Close 协商 ==========" )
        label labF
        {
            Net.TcpServer srv = Net.Tcp.listen( 19376 )
            WebsocketTest.g_srvCloseCode = 0
            WebsocketTest.g_srvCloseReason = ""
            WebsocketTest.g_srvCloseClean = false
            function hf = function()
            {
                object r = try? WebsocketTest.sfCloseServer( srv )
            }
            Task st = spawn hf()

            WebsocketTest.resetObs()
            Net.WebSocketClient c = Net.WebSocketClient()
            function hMsg = function( Net.WsMessage m )
            {
                if m.isText
                {
                    WebsocketTest.g_lastText = m.text()
                }
                WebsocketTest.g_msg = WebsocketTest.g_msg + 1
                m.release()
            }
            function hClose = function( Net.WsCloseInfo info )
            {
                WebsocketTest.g_close = WebsocketTest.g_close + 1
                if info != null
                {
                    WebsocketTest.g_cliCode = info.code
                    WebsocketTest.g_cliReason = info.reason
                    WebsocketTest.g_cliClean = info.wasClean
                }
            }
            c.onMessage = hMsg
            c.onClose = hClose
            c.connect( "ws://127.0.0.1:19376/close" )
            WebsocketTest.waitOpen( 3000 )

            c.sendText( "pre" )
            WebsocketTest.waitMsg( 1, 3000 )
            check( "F1 pre message echoed", WebsocketTest.g_lastText == "pre" )

            # 优雅关闭：发 Close(1001) 等对端回显，onClose 收到协商 closeInfo
            c.close( 1001, "going away" )
            c.waitClosed()
            check( "F2 onClose fired once", WebsocketTest.g_close == 1 )
            check( "F3 client close code echoed", WebsocketTest.g_cliCode == 1001 )
            check( "F4 client close reason echoed", WebsocketTest.g_cliReason == "going away" )
            check( "F5 client clean close", WebsocketTest.g_cliClean == true )
            check( "F6 client state closed", c.state == Net.WsState.Closed )

            Coroutine.awaitTask( st )
            check( "F7 server close code", WebsocketTest.g_srvCloseCode == 1001 )
            check( "F8 server close reason", WebsocketTest.g_srvCloseReason == "going away" )
            check( "F9 server clean close", WebsocketTest.g_srvCloseClean == true )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "F", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "F", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # G: messages() Stream 消费（协议层）
    # ============================================================================

    static testGroupG()
    {
        Console.println( "========== G: messages() Stream 消费 ==========" )
        label labG
        {
            Net.TcpServer srv = Net.Tcp.listen( 19377 )
            function hf = function()
            {
                WebsocketTest.sfEchoServer( srv, 1, "" )
            }
            Task st = spawn hf()

            Net.WebSocketStream ws = Net.WebSocketStream.connect( "ws://127.0.0.1:19377/stream" )
            # 引流协程收消息灌进 controller，消费方经迭代器逐条读取
            var msgs = ws.messages()
            var it = msgs.iterator
            ws.sendText( "s-1" )
            ws.sendText( "s-2" )
            ws.sendText( "s-3" )
            string s1 = ""
            string s2 = ""
            string s3 = ""
            if it.moveNext()
            {
                Net.WsMessage m1 = it.current
                s1 = m1.text()
                m1.release()
            }
            if it.moveNext()
            {
                Net.WsMessage m2 = it.current
                s2 = m2.text()
                m2.release()
            }
            if it.moveNext()
            {
                Net.WsMessage m3 = it.current
                s3 = m3.text()
                m3.release()
            }
            check( "G1 stream messages in order", s1 == "s-1" && s2 == "s-2" && s3 == "s-3" )

            ws.close()
            Coroutine.awaitTask( st )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "G", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "G", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # H: connectAsync 协程（协议层）
    # ============================================================================

    static testGroupH()
    {
        Console.println( "========== H: connectAsync 协程 ==========" )
        label labH
        {
            Net.TcpServer srv = Net.Tcp.listen( 19378 )
            function hf = function()
            {
                object r = try? WebsocketTest.sfConcEchoServer( srv, 2, "" )
            }
            Task st = spawn hf()

            Task t1 = Net.WebSocketStream.connectAsync( "ws://127.0.0.1:19378/async1" )
            Task t2 = Net.WebSocketStream.connectAsync( "ws://127.0.0.1:19378/async2" )
            Coroutine.waitAll2( t1, t2 )
            object r1 = Coroutine.awaitTask( t1 )
            object r2 = Coroutine.awaitTask( t2 )
            Net.WebSocketStream w1 = null
            Net.WebSocketStream w2 = null
            if r1 != null
            {
                w1 = r1 as Net.WebSocketStream
            }
            if r2 != null
            {
                w2 = r2 as Net.WebSocketStream
            }
            check( "H1 both async connects", w1 != null && w2 != null )

            bool h2 = false
            if w1 != null && w2 != null
            {
                w1.sendText( "a1" )
                w2.sendText( "a2" )
                Net.WsMessage m1 = w1.receive()
                Net.WsMessage m2 = w2.receive()
                if m1 != null && m2 != null
                {
                    h2 = m1.text() == "a1" && m2.text() == "a2"
                    m1.release()
                    m2.release()
                }
                w1.close()
                w2.close()
            }
            check( "H2 concurrent echo round-trips", h2 )

            Coroutine.awaitTask( st )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "H", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "H", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # I: isolate worker（回调模式在 isolate 中运行）
    # ============================================================================

    static testGroupI()
    {
        Console.println( "========== I: isolate worker ==========" )
        label labI
        {
            Net.TcpServer srv = Net.Tcp.listen( 19379 )
            function hf = function()
            {
                object r = try? WebsocketTest.sfEchoServer( srv, 3, "" )
            }
            Task st = spawn hf()

            Func<int, int> entry = WebsocketTest.isoWsFn()
            Int32 w1 = Isolate.run( entry, 19379 ) as int
            Int32 w2 = Isolate.run( entry, 19379 ) as int
            Int32 w3 = Isolate.run( entry, 19379 ) as int
            Coroutine.awaitTask( st )
            check( "I1 isolate workers ws echo", w1 == 1 && w2 == 1 && w3 == 1 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                WebsocketTest.groupError( "I", te.code, te.message )
            }
            else
            {
                WebsocketTest.groupError( "I", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # J: 错误路径（WebSocketClient.connect / sendText）
    # ============================================================================

    static testGroupJ()
    {
        Console.println( "========== J: 错误路径 ==========" )

        # J1: 非 101 响应 -> WsError.Handshake(1)（connect 原地抛）
        bool j1 = false
        label labJ1
        {
            Net.TcpServer srv = Net.Tcp.listen( 19380 )
            function hf = function()
            {
                object r = try? WebsocketTest.sfBadHandshakeServer( srv )
            }
            Task st = spawn hf()
            Net.WebSocketClient c = Net.WebSocketClient()
            try c.connect( "ws://127.0.0.1:19380/x" )
        }
        catch e
        {
            Error te = e as Error
            j1 = te != null && te.code == 1
        }
        check( "J1 non-101 response -> Handshake(1)", j1 )

        # J2: http scheme -> HttpError.UnsupportedScheme(2)
        bool j2 = false
        label labJ2
        {
            Net.WebSocketClient c = Net.WebSocketClient()
            try c.connect( "http://127.0.0.1:19371/" )
        }
        catch e
        {
            Error te = e as Error
            j2 = te != null && te.code == 2
        }
        check( "J2 http scheme -> UnsupportedScheme(2)", j2 )

        # J3: 连接被拒（无监听端口）-> NetError.ConnectFailed(2)
        bool j3 = false
        label labJ3
        {
            Net.WebSocketClient c = Net.WebSocketClient()
            try c.connect( "ws://127.0.0.1:19399/" )
        }
        catch e
        {
            Error te = e as Error
            j3 = te != null && te.code == 2
        }
        check( "J3 connection refused -> ConnectFailed(2)", j3 )

        # J4: Closed 后发送 -> WsError.Closed(4)（waitClosed 后 state 已收敛 Closed）
        bool j4 = false
        label labJ4
        {
            Net.TcpServer srv = Net.Tcp.listen( 19381 )
            function hf = function()
            {
                object r = try? WebsocketTest.sfEchoServer( srv, 1, "" )
            }
            Task st = spawn hf()
            Net.WebSocketClient c = Net.WebSocketClient()
            c.connect( "ws://127.0.0.1:19381/closed" )
            c.close()
            c.waitClosed()
            Coroutine.awaitTask( st )
            try c.sendText( "after-close" )
        }
        catch e
        {
            Error te = e as Error
            j4 = te != null && te.code == 4
        }
        check( "J4 send after close -> Closed(4)", j4 )
    }

    # ============================================================================
    # 入口
    # ============================================================================

    static fun()
    {
        Console.println( "===== WebsocketTest start =====" )
        testGroupA()
        testGroupB()
        testGroupC()
        testGroupD()
        testGroupE()
        testGroupF()
        testGroupG()
        testGroupH()
        testGroupI()
        testGroupJ()
        Console.println( "===== WebsocketTest end : pass=" + WebsocketTest.s_pass.toString() + " fail=" + WebsocketTest.s_fail.toString() + " =====" )
    }
}
