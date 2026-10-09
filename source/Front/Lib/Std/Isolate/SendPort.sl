#!
 * Std/Isolate/SendPort.sl
 * 端口的发送端视图。
 * 同一 port_id 在同一 VM 内始终对应同一 wrapper 实例（C VM 侧身份缓存
 * 保证），因此 == 判等跨 isolate 稳定（收到同一端口的两次消息后用 ==
 * 比较为 true）。
 * 可跨 isolate 发送（序列化只携带 port_id，接收端重建同一 wrapper）。
!#

public class SendPort extends Object
{
    #! 端口 id（由 C VM 单调递增分配；由 C VM 直接写入）。 !#
    Int64 _portId = 0

    public void _init_( Int64 portId )
    {
        this._portId = portId
    }

    #!
     * 异步发送一条消息（深拷贝语义，永不阻塞当前协程）。
     * 消息内容须可发送：null / 数值 / bool / 字符串 / SendPort /
     * Capability / TransferableData / List / Map / Set / 闭包
     * （捕获环境须全部可发送）。
    !#
    public void send( object message )
    {
        SystemPortSend( this._portId, message )
    }

    #! 端口 id（诊断用途）。 !#
    get Int64 portId()
    {
        ret this._portId
    }
}
