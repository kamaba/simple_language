#!
 * Std/Isolate/Isolate.sl
 * isolate（隔离岛）句柄：对标 Dart 的 dart:isolate.Isolate。
 * 每个 isolate 拥有独立的 VM 实例（独立堆 / 调度器 / 静态字段），
 * 相互之间只能通过消息传递（深拷贝）或 TransferableData（所有权转移）
 * 通信；同组 isolate 共享代码与类型。
 *
 * 说明：
 *  - Isolate 本身不可跨 isolate 发送；请拆成 controlPort +
 *    pauseCapability + terminateCapability 传递，对端用构造函数重建。
 *  - 生成入口（编译器脱糖为变长系统调用，Dart Isolate.run 语义）：
 *      Isolate.spawn( entry, arg1, ..., argN )  // 函数值直通，返回 Isolate
 *      Isolate.spawnInstance( entry, arg... )    // spawn 的别名形态
 *      Isolate.run( entry, arg1, ..., argN )    // 一次性计算，返回 object
 *    也支持调用糖与匿名闭包：Isolate.run( work( 1, 2 ) )、
 *    Isolate.run( function( x ){ ... }, 42 )（实参/闭包体在 worker 内
 *    以消息深拷贝快照求值，零装箱转发）；入口只允许闭包函数或静态函数
 *    值，this 非静态成员函数编译报错。
 *  - 同一 isolate id 始终对应同一 wrapper 实例（C VM 侧注册表保证），
 *    可用 == 判等。
!#

public class Isolate extends Object
{
    #! isolate id（由 C VM 单调递增分配；由 C VM 直接写入）。 !#
    Int64 _handle = 0
    #! 控制端口 id（发送端视图见 controlPort）。 !#
    Int64 _controlPortId = 0
    #! 恢复能力 id（Capability 视图见 pauseCapability）。 !#
    Int64 _pauseCapabilityId = 0
    #! 终止能力 id（Capability 视图见 terminateCapability）。 !#
    Int64 _terminateCapabilityId = 0

    #!
     * 由 control port + capability 重建 isolate 句柄
     * （C VM 按 control port 反查 owner isolate）。
     * 未知 port（isolate 不存在 / 已销毁）时得到全 0 句柄，
     * status 查询返回 -1。
    !#
    public void _init_( SendPort controlPort, Capability pauseCapability, Capability terminateCapability )
    {
        Isolate rebuilt = SystemIsolateNew( controlPort.portId,
                                            pauseCapability.capId,
                                            terminateCapability.capId ) as Isolate
        if ( rebuilt != null )
        {
            this._handle = rebuilt.handle
            this._controlPortId = rebuilt.controlPort.portId
            this._pauseCapabilityId = rebuilt.pauseCapability.capId
            this._terminateCapabilityId = rebuilt.terminateCapability.capId
        }
    }

    #! ---------- 静态：生成与取回 ---------- !#

    #! 当前 isolate 的句柄。 !#
    public static Isolate current()
    {
        ret SystemIsolateCurrent() as Isolate
    }

    #!
     * 同步终止当前 isolate，并向 port 发送一条终止消息。
    !#
    public static void exit( SendPort port, object message )
    {
        SystemIsolateExit( port.portId, message )
    }

    #! ---------- 实例：生命周期控制 ---------- !#

    #!
     * 请求暂停本 isolate，返回 resumeCapability。
     * 返回 capId 为 0 的 Capability 表示不可暂停（已退出等）。
    !#
    public Capability pause()
    {
        ret Capability( SystemIsolatePause( this._handle ) )
    }

    #! 恢复被暂停的 isolate；capability 不匹配则静默无效。 !#
    public void resume( Capability capability )
    {
        SystemIsolateResume( this._handle, capability.capId )
    }

    #!
     * 请求终止 isolate。priority 取 0=immediate / 1=beforeNextEvent。
    !#
    public void kill( Int32 priority )
    {
        SystemIsolateKill( this._handle, priority )
    }

    #! 存活探测：isolate 存活时向 responsePort 发送 response。 !#
    public void ping( SendPort responsePort, object response, Int32 priority )
    {
        SystemIsolatePing( this._handle, responsePort.portId, response, priority )
    }

    #! 设置未捕获异常是否终止 isolate（fatal=true 终止）。 !#
    public void setErrorsFatal( bool fatal )
    {
        SystemIsolateSetErrorsFatal( this._handle, fatal )
    }

    #! 注册退出监听：isolate 退出时向 port 发送 response。 !#
    public void addOnExitListener( SendPort port, object response )
    {
        SystemIsolateAddOnExitListener( this._handle, port.portId, response )
    }

    #! 注册错误监听：isolate 未捕获异常时向 port 发送错误描述。 !#
    public void addErrorListener( SendPort port )
    {
        SystemIsolateAddErrorListener( this._handle, port.portId )
    }

    #! ---------- 实例：查询 ---------- !#

    #! 当前状态，取 IsolateStatus 常量值；句柄无效返回 -1。 !#
    get Int32 status()
    {
        ret SystemIsolateStatus( this._handle )
    }

    #! 调试名（"Isolate-" + id，诊断用途）。 !#
    get string debugName()
    {
        ret "Isolate-" + this._handle.toString()
    }

    #! 原始 isolate id（诊断用途）。 !#
    get Int64 handle()
    {
        ret this._handle
    }

    #! 控制端口（可发送；接收方凭它与 capability 重建本句柄）。 !#
    get SendPort controlPort()
    {
        ret SystemPortSendPort( this._controlPortId ) as SendPort
    }

    #! 恢复能力（可发送）。 !#
    get Capability pauseCapability()
    {
        ret Capability( this._pauseCapabilityId )
    }

    #! 终止能力（可发送）。 !#
    get Capability terminateCapability()
    {
        ret Capability( this._terminateCapabilityId )
    }
}
