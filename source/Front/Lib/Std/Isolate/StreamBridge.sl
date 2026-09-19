#!
 * Std/Isolate/StreamBridge.sl
 * 跨 isolate 流桥接（STREAM_DESIGN §12.2 模式 1：元素级，深拷贝）。
 *   - pipeTo：源端泵协程逐元素 send 到目标端口，流终止后发哨兵；
 *   - fromReceivePort：目标端收消息重组为元素流（object），收哨兵或
 *     端口关闭即终态。
 * 哨兵用 string 传递（Sendable 白名单内，object == string 解包按值比较）；
 * 设计稿 StreamDoneSentinel 类实例不可跨岛（自定义类不在 §12.1 白名单）。
 * 保留值 __SL_STREAM_DONE__：元素为 string 的业务流约定避开。
 * 已知边界（P2 机制先行）：
 *   - fromReceivePort 订阅 cancel 在下一条消息边界生效（recv 挂起不被
 *     唤醒；目标端可 close 端口解泵）；
 *   - pipeTo 流终止后发哨兵，若目标端口已 close，send 在发送端抛错。
 *   - fromReceivePort 用 StreamController 组合（ctrl.stream 返回 Core 的
 *     _ControllerStream）：Std 侧不能 extends Stream<object>（Front 跨模块
 *     extends 模板类不支持，同 p2-3 迁移原因）；副作用为工厂调用即起泵，
 *     订阅宜紧跟创建（消息先堆积于 VM 端口队列）。
!#

public class StreamBridge extends Object
{
    #! 跨岛流终止哨兵（保留值）。 !#
    public static string doneSentinel()
    {
        ret "__SL_STREAM_DONE__"
    }

    #!
     * 源端桥接：把 src 的元素逐个 send 到 port，流终止后发哨兵。
     * 元素可发送性由 SystemPortSend 按白名单校验（NotSendable 抛发送端）。
    !#
    public static Task pipeTo<T>( Stream<T> src, SendPort port )
    {
        Stream<T> s = src
        SendPort p = port
        function f = function()
        {
            var it = s.iterator
            while it.moveNext()
            {
                p.send( it.current )
            }
            p.send( StreamBridge.doneSentinel() )
        }
        Task task = Coroutine.spawnClosure0( f )
        ret task
    }

    #!
     * 目标端桥接：把 rp 收到的消息重组为元素流（object），消费端按需 as T。
    !#
    public static Stream<object> fromReceivePort( ReceivePort rp )
    {
        StreamController<object> ctrl = StreamController<object>()
        Stream<object> s = ctrl.stream
        ReceivePort r = rp
        function f = function()
        {
            while true
            {
                object o = r.recv()
                if o == null
                {
                    break
                }
                if o == StreamBridge.doneSentinel()
                {
                    break
                }
                ctrl.add( o )
            }
            ctrl.close()
        }
        Coroutine.spawnClosure0( f )
        ret s
    }
}
