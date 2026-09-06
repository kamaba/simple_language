#!
 * Std/Isolate/RawReceivePort.sl
 * 底层接收端（对标 Dart 的 RawReceivePort）：与 ReceivePort 共用同一套
 * 端口机制，但不提供消息缓冲语义糖——仅 handler 注册（listen）与
 * 显式 recv/tryRecv。适合需要完全控制取消息时机的底层代码。
 * 同样不可跨 isolate 发送；传递请使用 sendPort。
!#

public class RawReceivePort extends Object
{
    #! 端口 id（构造时由 C VM 分配；由 C VM 直接写入）。 !#
    Int64 _portId = 0

    #! 创建底层接收端并自动分配对应端口。 !#
    public void _init_()
    {
        this._portId = SystemReceivePortCreate()
    }

    #!
     * 对应的发送端。同一端口重复读取返回同一 SendPort 实例
     * （C VM 侧身份缓存保证），== 判等可靠。
    !#
    get SendPort sendPort()
    {
        ret SystemPortSendPort( this._portId ) as SendPort
    }

    #!
     * 注册消息处理器：内部起一个分发协程阻塞收消息并回调 handler。
     * handler 建议签名 Func<void, object>；端口关闭且消息耗尽后
     * 分发协程自动退出。
    !#
    public void listen( Function handler )
    {
        Int64 pid = this._portId
        var h = handler
        function disp = function()
        {
            while ( true )
            {
                object msg = SystemPortRecv( pid )
                if ( msg == null )
                {
                    break
                }
                h( msg )
            }
        }
        Coroutine.spawnClosure0( disp )
    }

    #! 阻塞当前协程直到收到一条消息；端口关闭且无消息后返回 null。 !#
    public object recv()
    {
        ret SystemPortRecv( this._portId )
    }

    #! 非阻塞取一条消息；无消息返回 null。 !#
    public object tryRecv()
    {
        ret SystemPortTryRecv( this._portId )
    }

    #! 关闭端口。 !#
    public void close()
    {
        SystemPortClose( this._portId )
    }

    #! 当前队列中的消息数（诊断用途）。 !#
    get Int32 count()
    {
        ret SystemPortCount( this._portId )
    }

    #! 端口是否已关闭。 !#
    get bool isClosed()
    {
        ret SystemPortIsClosed( this._portId )
    }
}
