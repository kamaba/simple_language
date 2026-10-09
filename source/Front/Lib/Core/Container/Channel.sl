#!
 * Core/Container/Channel.sl
 * 协程间通信通道（CSP 风格）。
 *
 * 语义（见设计档 4.8）：
 *  - Send：缓冲未满则入队并唤醒一个等待的接收者；缓冲满则挂起发送者直至有空位。
 *    对已关闭的通道 Send 抛出异常。
 *  - Recv：缓冲非空则取队头并唤醒一个等待的发送者；缓冲空且未关闭则挂起接收者；
 *    通道已关闭且缓冲空时 Recv 返回 null。
 *  - Close：关闭通道，唤醒全部等待的发送者与接收者；之后的 Send 抛异常、Recv 返回 null。
 *
 * 说明：
 *  - 通道本体在 C VM 端（文件级静态注册表），SL 对象仅持有 Int64 句柄 _chid。
 *  - capacity <= 0 表示无缓冲上限（unbounded，Send 永不阻塞）。
 !#

public class Channel<T> extends Object
{
    Int64 _chid = 0

    # 默认构造：无缓冲上限通道（capacity = 0 表示 unbounded）
    _init_()
    {
        this._chid = SystemChannelCreate(0)
    }

    # 指定容量构造：capacity <= 0 视为无缓冲上限
    _init_( int capacity )
    {
        this._chid = SystemChannelCreate(capacity)
    }

    public static Channel<T> create()
    {
        var ch = Channel<T>()
        ret ch
    }

    public static Channel<T> create( int capacity )
    {
        var ch = Channel<T>(capacity)
        ret ch
    }

    # 发送一个值。缓冲满时挂起当前协程直至有空位；通道已关闭时抛出异常。
    public void send( T value )
    {
        SystemChannelSend(this._chid, value)
    }

    # 接收一个值。缓冲空且未关闭时挂起当前协程；通道已关闭且缓冲空时返回 null。
    public T recv()
    {
        ret SystemChannelRecv(this._chid) as T
    }

    # 关闭通道。唤醒全部等待中的发送者与接收者。
    public void close()
    {
        SystemChannelClose(this._chid)
    }

    # 缓冲内当前元素个数
    get int count()
    {
        ret SystemChannelCount(this._chid)
    }

    # 通道是否已关闭
    get bool isClosed()
    {
        ret SystemChannelIsClosed(this._chid)
    }
}
