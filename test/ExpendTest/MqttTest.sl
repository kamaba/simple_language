import Std;
import Core;

# ============================================================================
# MqttTest.sl -- MQTT 3.1.1 协议测试（Net.MqttCodec / Net.MqttClient）
#
# 覆盖功能点（A-H 组）：
#   A   剩余长度编码      varint 边界向量（0/127/128/16383/16384/2097151/
#                        2097152/268435455）+ remainingLengthSize 边界 +
#                        趟界值（-1 / 268435456）抛 Protocol(1)
#   B   剩余长度解码      A 组向量反向 + 规范例 c102->321 + 数据不足(-1) +
#                        超 4 字节上限(-2)
#   C   CONNECT 编码      默认 flags(cleanSession|2) / 用户名密码(c2) /
#                        默认 clientId(slmqtt) / cleanSession=false+keepAlive=0
#   D   PUBLISH/SUB/UNSUB/PING/DISCONNECT 编码
#                        qos0 / qos1+retain / SUBSCRIBE / UNSUBSCRIBE /
#                        PINGREQ(c000) / DISCONNECT(e000)
#   E   服务端侧编码      CONNACK / SUBACK / PUBACK / UNSUBACK / PINGRESP(d000)
#   F   编解码往返        encodePublish -> parsePacket -> parsePublish 字段
#                        逐项校验 + 不完整包回滚 null + type0 抛 Protocol +
#                        剩余长度超限抛 Protocol + qos3 抛 Protocol
#   G   mock broker 回环  本机回环 broker（G 组端口 18901）：CONNECT 握手 /
#                        SUBSCRIBE 授予 / QoS0 与 QoS1 echo 往返 / PING /
#                        broker 侧观测（clientId / subscribe / last publish）/
#                        DISCONNECT 关闭
#   H   CONNACK 拒绝      returnCode=5 -> MqttError.ConnackRejected(2)
#
# 端口：18901（G 组 echo broker）、18902（H 组拒绝 broker）。
# 风格约定：A-F 纯编解码组平铺（TcpBasicTest / Base64Test 同款）；G/H 网络
#   组组级 label{}...catch{} 兜底（WebsocketTest 同款），预期异常用例经
#   辅助函数包 label/catch（无嵌套 label 先例，规避遮蔽风险）。
# 错误码取值依据：MqttError.Protocol=1 / ConnackRejected=2。
# ============================================================================

MqttTest
{
    static Int32 s_pass = 0
    static Int32 s_fail = 0

    # ---- broker 观测变量（broker 协程写，主流程在同步点之后读） ----
    static string g_brokerClientId = ""
    static string g_lastSubTopic = ""
    static Int32 g_lastSubQos = -1
    static string g_lastPubTopic = ""
    static string g_lastPubText = ""
    static Int32 g_lastPubQos = -1

    # 断言辅助：OK/FAIL 单行输出 + 计数（WebsocketTest 同款）
    static check( string name, bool cond )
    {
        if cond
        {
            MqttTest.s_pass = MqttTest.s_pass + 1
            Console.println( "[MqttTest] " + name + " : OK" )
        }
        else
        {
            MqttTest.s_fail = MqttTest.s_fail + 1
            Console.println( "[MqttTest] " + name + " : FAIL" )
        }
    }

    # 网络组异常兜底：整组记一条 FAIL 后继续
    static groupError( string group, Int32 code, string message )
    {
        MqttTest.s_fail = MqttTest.s_fail + 1
        Console.println( "[MqttTest] " + group + " : FAIL (network error code=" + code.toString() + " " + message + ")" )
    }

    # ============================================================================
    # mock broker
    # ============================================================================

    # echo broker（G 组）：单连接；CONNECT 回 CONNACK(0) 记 clientId；
    # SUBSCRIBE 记观测回 SUBACK(原 qos)；PUBLISH 记观测，QoS1 先回 PUBACK
    # 再回 echo PUBLISH("echo:"+文本, QoS0)；PINGREQ 回 PINGRESP；
    # DISCONNECT / EOF 退出
    static Int32 sfBrokerEcho( Net.TcpServer srv ) throws
    {
        Net.TcpStream raw = srv.accept()
        raw.setReadTimeout( 5000 )
        ByteBuffer buf = ByteBuffer()
        while true
        {
            Net._MqttPacket pkt = Net.MqttCodec.readPacket( raw, buf )
            if pkt == null
            {
                break
            }
            if pkt.type == Net.MqttPacketType.Connect
            {
                # 跳过协议名(U16 前缀+"MQTT")+level+flags+keepalive 共 10 字节
                pkt.payload.skipBytes( 10 )
                MqttTest.g_brokerClientId = Net.MqttCodec.readMqttString( pkt.payload )
                ByteBuffer ack = Net.MqttCodec.encodeConnack( false, 0 )
                raw.write( ack )
                ack.release()
            }
            elif pkt.type == Net.MqttPacketType.Subscribe
            {
                UInt16 pid16 = pkt.payload.readU16Be()
                Int32 pid = pid16
                string topic = Net.MqttCodec.readMqttString( pkt.payload )
                Int32 qos = pkt.payload.readU8()
                MqttTest.g_lastSubTopic = topic
                MqttTest.g_lastSubQos = qos
                ByteBuffer ack = Net.MqttCodec.encodeSuback( pid, qos )
                raw.write( ack )
                ack.release()
            }
            elif pkt.type == Net.MqttPacketType.Publish
            {
                Net.MqttMessage msg = Net.MqttCodec.parsePublish( pkt )
                Int32 inQos = msg.qos
                Int32 inPid = msg.packetId
                string inTopic = msg.topic
                string inText = msg.text()
                msg.release()
                MqttTest.g_lastPubTopic = inTopic
                MqttTest.g_lastPubText = inText
                MqttTest.g_lastPubQos = inQos
                if inQos == 1
                {
                    ByteBuffer pAck = Net.MqttCodec.encodeAck( Net.MqttPacketType.PubAck, inPid )
                    raw.write( pAck )
                    pAck.release()
                }
                ByteBuffer echoBody = ByteBuffer.fromString( "echo:" + inText )
                ByteBuffer echo = Net.MqttCodec.encodePublish( inTopic, echoBody, 0, 0, false )
                echoBody.release()
                raw.write( echo )
                echo.release()
            }
            elif pkt.type == Net.MqttPacketType.PingReq
            {
                ByteBuffer ack = Net.MqttCodec.encodePingResp()
                raw.write( ack )
                ack.release()
            }
            elif pkt.type == Net.MqttPacketType.Disconnect
            {
                pkt.release()
                break
            }
            pkt.release()
        }
        raw.close()
        srv.close()
        ret 1
    }

    # 拒绝 broker（H 组）：读 CONNECT 后回 CONNACK(returnCode=5) 即关流
    static Int32 sfBrokerReject( Net.TcpServer srv ) throws
    {
        Net.TcpStream raw = srv.accept()
        raw.setReadTimeout( 5000 )
        ByteBuffer buf = ByteBuffer()
        Net._MqttPacket pkt = Net.MqttCodec.readPacket( raw, buf )
        if pkt != null
        {
            pkt.release()
        }
        ByteBuffer ack = Net.MqttCodec.encodeConnack( false, 5 )
        raw.write( ack )
        ack.release()
        raw.close()
        srv.close()
        ret 1
    }

    # H1 辅助：连接 18902 拒绝 broker，true = 抛 MqttError.ConnackRejected(2)
    # （TcpBasicTest A4/A5 的 label/catch 单 try 模式包成函数，避免组级嵌套）
    static bool h1TryConnect()
    {
        bool caught = false
        label labH1
        {
            try Net.MqttClient.connect( "127.0.0.1", 18902 )
        }
        catch e
        {
            Error ne = e as Error
            caught = ne != null && ne.code == 2
        }
        ret caught
    }

    # ============================================================================
    # A: 剩余长度编码
    # ============================================================================

    static testGroupA() throws
    {
        Console.println( "========== A: 剩余长度编码 ==========" )
        # 编码字节数边界（128/16384/2097152 三档进位）
        check( "A0 rl size 1-byte bound", Net.MqttCodec.remainingLengthSize( 0 ) == 1 && Net.MqttCodec.remainingLengthSize( 127 ) == 1 )
        check( "A0 rl size 2-byte bound", Net.MqttCodec.remainingLengthSize( 128 ) == 2 && Net.MqttCodec.remainingLengthSize( 16383 ) == 2 )
        check( "A0 rl size 3-byte bound", Net.MqttCodec.remainingLengthSize( 16384 ) == 3 && Net.MqttCodec.remainingLengthSize( 2097151 ) == 3 )
        check( "A0 rl size 4-byte bound", Net.MqttCodec.remainingLengthSize( 2097152 ) == 4 && Net.MqttCodec.remainingLengthSize( 268435455 ) == 4 )

        ByteBuffer a1 = Net.MqttCodec.encodeRemainingLength( 0 )
        check( "A1 encode 0", a1.toHex() == "00" )
        a1.release()
        ByteBuffer a2 = Net.MqttCodec.encodeRemainingLength( 127 )
        check( "A2 encode 127", a2.toHex() == "7f" )
        a2.release()
        ByteBuffer a3 = Net.MqttCodec.encodeRemainingLength( 128 )
        check( "A3 encode 128", a3.toHex() == "8001" )
        a3.release()
        ByteBuffer a4 = Net.MqttCodec.encodeRemainingLength( 16383 )
        check( "A4 encode 16383", a4.toHex() == "ff7f" )
        a4.release()
        ByteBuffer a5 = Net.MqttCodec.encodeRemainingLength( 16384 )
        check( "A5 encode 16384", a5.toHex() == "808001" )
        a5.release()
        ByteBuffer a6 = Net.MqttCodec.encodeRemainingLength( 2097151 )
        check( "A6 encode 2097151", a6.toHex() == "ffff7f" )
        a6.release()
        ByteBuffer a7 = Net.MqttCodec.encodeRemainingLength( 2097152 )
        check( "A7 encode 2097152", a7.toHex() == "80808001" )
        a7.release()
        ByteBuffer a8 = Net.MqttCodec.encodeRemainingLength( 268435455 )
        check( "A8 encode 268435455", a8.toHex() == "ffffff7f" )
        a8.release()

        # 趟界值抛 Protocol(1)
        bool a9 = false
        label labA9
        {
            try Net.MqttCodec.encodeRemainingLength( -1 )
        }
        catch e
        {
            Error ne = e as Error
            a9 = ne != null && ne.code == 1
        }
        check( "A9 encode -1 -> Protocol(1)", a9 )

        bool a10 = false
        label labA10
        {
            try Net.MqttCodec.encodeRemainingLength( 268435456 )
        }
        catch e
        {
            Error ne = e as Error
            a10 = ne != null && ne.code == 1
        }
        check( "A10 encode 268435456 -> Protocol(1)", a10 )
    }

    # ============================================================================
    # B: 剩余长度解码
    # ============================================================================

    static testGroupB() throws
    {
        Console.println( "========== B: 剩余长度解码 ==========" )
        ByteBuffer b1 = ByteBuffer.fromHex( "00" )
        check( "B1 decode 0", Net.MqttCodec.decodeRemainingLength( b1 ) == 0 )
        b1.release()
        ByteBuffer b2 = ByteBuffer.fromHex( "7f" )
        check( "B2 decode 127", Net.MqttCodec.decodeRemainingLength( b2 ) == 127 )
        b2.release()
        ByteBuffer b3 = ByteBuffer.fromHex( "8001" )
        check( "B3 decode 128", Net.MqttCodec.decodeRemainingLength( b3 ) == 128 )
        b3.release()
        ByteBuffer b4 = ByteBuffer.fromHex( "ff7f" )
        check( "B4 decode 16383", Net.MqttCodec.decodeRemainingLength( b4 ) == 16383 )
        b4.release()
        ByteBuffer b5 = ByteBuffer.fromHex( "808001" )
        check( "B5 decode 16384", Net.MqttCodec.decodeRemainingLength( b5 ) == 16384 )
        b5.release()
        ByteBuffer b6 = ByteBuffer.fromHex( "ffff7f" )
        check( "B6 decode 2097151", Net.MqttCodec.decodeRemainingLength( b6 ) == 2097151 )
        b6.release()
        ByteBuffer b7 = ByteBuffer.fromHex( "80808001" )
        check( "B7 decode 2097152", Net.MqttCodec.decodeRemainingLength( b7 ) == 2097152 )
        b7.release()
        ByteBuffer b8 = ByteBuffer.fromHex( "ffffff7f" )
        check( "B8 decode 268435455", Net.MqttCodec.decodeRemainingLength( b8 ) == 268435455 )
        b8.release()

        # MQTT 3.1.1 规范非规范示例：0xC1 0x02 -> 321
        ByteBuffer b9 = ByteBuffer.fromHex( "c102" )
        check( "B9 decode spec example 321", Net.MqttCodec.decodeRemainingLength( b9 ) == 321 )
        b9.release()

        # 数据不足（延续位仍置位但无后续字节）-> -1（读位回滚）
        ByteBuffer b10 = ByteBuffer.fromHex( "80" )
        check( "B10 decode incomplete -> -1", Net.MqttCodec.decodeRemainingLength( b10 ) == -1 )
        b10.release()

        # 超过 4 字节上限 -> -2（读位回滚）
        ByteBuffer b11 = ByteBuffer.fromHex( "ffffffff00" )
        check( "B11 decode over 4 bytes -> -2", Net.MqttCodec.decodeRemainingLength( b11 ) == -2 )
        b11.release()
    }

    # ============================================================================
    # C: CONNECT 编码
    # ============================================================================

    static testGroupC() throws
    {
        Console.println( "========== C: CONNECT 编码 ==========" )
        # C1: clientId=demo，其余默认（cleanSession=true / keepAlive=60）
        Net.MqttConnectOptions opt1 = Net.MqttConnectOptions()
        opt1.clientId = "demo"
        ByteBuffer c1 = Net.MqttCodec.encodeConnect( opt1 )
        check( "C1 connect default flags", c1.toHex() == "101000044d5154540402003c000464656d6f" )
        c1.release()

        # C2: 附用户名密码（flags = cleanSession|username|password = 0xC2）
        Net.MqttConnectOptions opt2 = Net.MqttConnectOptions()
        opt2.clientId = "demo"
        opt2.username = "user"
        opt2.password = "pass"
        ByteBuffer c2 = Net.MqttCodec.encodeConnect( opt2 )
        check( "C2 connect with user/pass", c2.toHex() == "101c00044d51545404c2003c000464656d6f000475736572000470617373" )
        c2.release()

        # C3: 全默认（clientId=slmqtt）
        Net.MqttConnectOptions opt3 = Net.MqttConnectOptions()
        ByteBuffer c3 = Net.MqttCodec.encodeConnect( opt3 )
        check( "C3 connect default clientId", c3.toHex() == "101200044d5154540402003c0006736c6d717474" )
        c3.release()

        # C4: cleanSession=false + keepAlive=0（flags=0）
        Net.MqttConnectOptions opt4 = Net.MqttConnectOptions()
        opt4.clientId = "demo"
        opt4.cleanSession = false
        opt4.keepAliveSeconds = 0
        ByteBuffer c4 = Net.MqttCodec.encodeConnect( opt4 )
        check( "C4 connect no clean / no keepalive", c4.toHex() == "101000044d51545404000000000464656d6f" )
        c4.release()
    }

    # ============================================================================
    # D: PUBLISH / SUBSCRIBE / UNSUBSCRIBE / PINGREQ / DISCONNECT 编码
    # ============================================================================

    static testGroupD() throws
    {
        Console.println( "========== D: PUBLISH/SUB/UNSUB/PING/DISCONNECT 编码 ==========" )
        # D1: PUBLISH qos0（无 packetId）
        ByteBuffer p1 = ByteBuffer.fromString( "hi" )
        ByteBuffer d1 = Net.MqttCodec.encodePublish( "a/b", p1, 0, 0, false )
        p1.release()
        check( "D1 publish qos0", d1.toHex() == "30070003612f626869" )
        d1.release()

        # D2: PUBLISH qos1 + retain + packetId=9（flags = 1*2|1 = 3）
        ByteBuffer p2 = ByteBuffer.fromString( "hi" )
        ByteBuffer d2 = Net.MqttCodec.encodePublish( "a/b", p2, 1, 9, true )
        p2.release()
        check( "D2 publish qos1 retain", d2.toHex() == "33090003612f6200096869" )
        d2.release()

        # D3: SUBSCRIBE（固定头 flags=2）
        ByteBuffer d3 = Net.MqttCodec.encodeSubscribe( 10, "a/b", 1 )
        check( "D3 subscribe", d3.toHex() == "8208000a0003612f6201" )
        d3.release()

        # D4: UNSUBSCRIBE
        ByteBuffer d4 = Net.MqttCodec.encodeUnsubscribe( 10, "a/b" )
        check( "D4 unsubscribe", d4.toHex() == "a207000a0003612f62" )
        d4.release()

        # D5: PINGREQ（c000）
        ByteBuffer d5 = Net.MqttCodec.encodePingReq()
        check( "D5 pingreq", d5.toHex() == "c000" )
        d5.release()

        # D6: DISCONNECT（e000）
        ByteBuffer d6 = Net.MqttCodec.encodeDisconnect()
        check( "D6 disconnect", d6.toHex() == "e000" )
        d6.release()
    }

    # ============================================================================
    # E: CONNACK / SUBACK / ACK / PINGRESP 编码（服务端侧）
    # ============================================================================

    static testGroupE() throws
    {
        Console.println( "========== E: CONNACK/SUBACK/ACK/PINGRESP 编码 ==========" )
        ByteBuffer e1 = Net.MqttCodec.encodeConnack( false, 0 )
        check( "E1 connack accepted", e1.toHex() == "20020000" )
        e1.release()

        ByteBuffer e2 = Net.MqttCodec.encodeConnack( false, 5 )
        check( "E2 connack rejected rc=5", e2.toHex() == "20020005" )
        e2.release()

        ByteBuffer e3 = Net.MqttCodec.encodeSuback( 10, 1 )
        check( "E3 suback", e3.toHex() == "9003000a01" )
        e3.release()

        ByteBuffer e4 = Net.MqttCodec.encodeAck( 4, 9 )
        check( "E4 puback", e4.toHex() == "40020009" )
        e4.release()

        ByteBuffer e5 = Net.MqttCodec.encodePingResp()
        check( "E5 pingresp", e5.toHex() == "d000" )
        e5.release()

        ByteBuffer e6 = Net.MqttCodec.encodeAck( 11, 10 )
        check( "E6 unsuback", e6.toHex() == "b002000a" )
        e6.release()
    }

    # ============================================================================
    # F: 编解码往返
    # ============================================================================

    static testGroupF() throws
    {
        Console.println( "========== F: 编解码往返 ==========" )
        # F1/F2: encodePublish -> parsePacket（固定头）
        ByteBuffer fp = ByteBuffer.fromString( "hi" )
        ByteBuffer fframe = Net.MqttCodec.encodePublish( "a/b", fp, 1, 9, true )
        fp.release()
        Net._MqttPacket pkt = Net.MqttCodec.parsePacket( fframe )
        fframe.release()
        check( "F1 parse type publish", pkt != null && pkt.type == Net.MqttPacketType.Publish )
        check( "F2 parse flags qos1+retain", pkt != null && pkt.flags == 3 )

        # F3-F7: parsePublish 字段逐项
        Net.MqttMessage msg = Net.MqttCodec.parsePublish( pkt )
        pkt.release()
        check( "F3 roundtrip topic", msg.topic == "a/b" )
        check( "F4 roundtrip text", msg.text() == "hi" )
        check( "F5 roundtrip qos", msg.qos == 1 )
        check( "F6 roundtrip packetId", msg.packetId == 9 )
        check( "F7 roundtrip retain", msg.retain == true )
        msg.release()

        # F8: 不完整包（仅固定头 1 字节）-> null（读位回滚）
        ByteBuffer f8 = ByteBuffer.fromHex( "30" )
        Net._MqttPacket p8 = Net.MqttCodec.parsePacket( f8 )
        check( "F8 incomplete -> null", p8 == null )
        f8.release()

        # F9: 包类型 0 -> Protocol(1)
        bool f9 = false
        ByteBuffer f9b = ByteBuffer.fromHex( "00020000" )
        label labF9
        {
            try Net.MqttCodec.parsePacket( f9b )
        }
        catch e
        {
            Error ne = e as Error
            f9 = ne != null && ne.code == 1
        }
        f9b.release()
        check( "F9 type 0 -> Protocol(1)", f9 )

        # F10: 剩余长度超 4 字节上限 -> Protocol(1)
        bool f10 = false
        ByteBuffer f10b = ByteBuffer.fromHex( "30ffffffff" )
        label labF10
        {
            try Net.MqttCodec.parsePacket( f10b )
        }
        catch e
        {
            Error ne = e as Error
            f10 = ne != null && ne.code == 1
        }
        f10b.release()
        check( "F10 rl over 4 bytes -> Protocol(1)", f10 )

        # F11: encodePublish qos=3 -> Protocol(1)
        bool f11 = false
        label labF11
        {
            ByteBuffer f11p = ByteBuffer.fromString( "x" )
            try Net.MqttCodec.encodePublish( "a/b", f11p, 3, 0, false )
            f11p.release()
        }
        catch e
        {
            Error ne = e as Error
            f11 = ne != null && ne.code == 1
        }
        check( "F11 publish qos3 -> Protocol(1)", f11 )
    }

    # ============================================================================
    # G: mock broker 回环（端口 18901）
    # ============================================================================

    static testGroupG()
    {
        Console.println( "========== G: mock broker 回环 ==========" )
        label labG
        {
            Net.TcpServer srv = Net.Tcp.listen( 18901 )
            function hf = function()
            {
                label runSrvG
                {
                    Int32 r = MqttTest.sfBrokerEcho( srv )
                }
                catch e
                {
                    Console.println( "G srv coroutine failed" )
                }
            }
            Task st = spawn hf()

            MqttTest.g_brokerClientId = ""
            MqttTest.g_lastSubTopic = ""
            MqttTest.g_lastSubQos = -1
            MqttTest.g_lastPubTopic = ""
            MqttTest.g_lastPubText = ""
            MqttTest.g_lastPubQos = -1

            Net.MqttClient cli = Net.MqttClient.connect( "127.0.0.1", 18901 )
            check( "G1 connect -> connected", cli.isConnected == true )

            Int32 granted = cli.subscribe( "a/b", 1 )
            check( "G2 subscribe granted qos", granted == 1 )
            check( "G3 broker saw subscribe", MqttTest.g_lastSubTopic == "a/b" && MqttTest.g_lastSubQos == 1 )

            # QoS0 发布 -> broker 回显 echo PUBLISH
            cli.publish( "a/b", "hi" )
            Net.MqttMessage m0 = cli.receive()
            bool g4 = m0 != null && m0.text() == "echo:hi"
            if m0 != null
            {
                m0.release()
            }
            check( "G4 qos0 echo roundtrip", g4 )

            # QoS1 发布（同步等 PUBACK）-> broker 回显
            cli.publish( "a/b", "hello", 1 )
            Net.MqttMessage m1 = cli.receive()
            bool g5 = m1 != null && m1.text() == "echo:hello"
            if m1 != null
            {
                m1.release()
            }
            check( "G5 qos1 echo roundtrip", g5 )

            # PING 往返（同步等 PINGRESP，不抛即过）
            bool g6 = false
            cli.ping()
            g6 = true
            check( "G6 ping roundtrip", g6 )

            check( "G7 broker saw clientId", MqttTest.g_brokerClientId == "slmqtt" )
            check( "G8 broker saw last publish", MqttTest.g_lastPubTopic == "a/b" && MqttTest.g_lastPubText == "hello" && MqttTest.g_lastPubQos == 1 )

            cli.disconnect()
            check( "G9 disconnect -> not connected", cli.isConnected == false )

            Coroutine.awaitTask( st )
            check( "G10 broker exited cleanly", true )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                MqttTest.groupError( "G", te.code, te.message )
            }
            else
            {
                MqttTest.groupError( "G", -1, "unknown error" )
            }
        }
    }

    # ============================================================================
    # H: CONNACK 拒绝（端口 18902）
    # ============================================================================

    static testGroupH()
    {
        Console.println( "========== H: CONNACK 拒绝 ==========" )
        label labH
        {
            Net.TcpServer srv = Net.Tcp.listen( 18902 )
            function hf = function()
            {
                object r = try? MqttTest.sfBrokerReject( srv )
            }
            Task st = spawn hf()

            check( "H1 connack reject -> ConnackRejected(2)", MqttTest.h1TryConnect() )

            Coroutine.awaitTask( st )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                MqttTest.groupError( "H", te.code, te.message )
            }
            else
            {
                MqttTest.groupError( "H", -1, "unknown error" )
            }
        }
    }

    static fun()
    {
        Console.println( "===== MqttTest start =====" )
        testGroupA()
        testGroupB()
        testGroupC()
        testGroupD()
        testGroupE()
        testGroupF()
        testGroupG()
        testGroupH()
        Console.println( "===== MqttTest end : pass=" + MqttTest.s_pass.toString() + " fail=" + MqttTest.s_fail.toString() + " =====" )
    }
}
