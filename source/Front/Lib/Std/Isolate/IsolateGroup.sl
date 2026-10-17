#!
 * Std/Isolate/IsolateGroup.sl
 * isolate 组句柄：同组 isolate 共享代码与类型（闭包可按 method_id
 * 解析），组是闭包跨 isolate 传递的前提。
 * current 组内至少含当前 isolate；exit 请求组内全部 isolate 退出。
!#

public class IsolateGroup extends Object
{
    #! 组 id（纯数据句柄，由 C VM 分配）。 !#
    Int64 _handle = 0

    public void _init_( Int64 groupId )
    {
        this._handle = groupId
    }

    #! 当前 isolate 所属组。 !#
    public static IsolateGroup current()
    {
        ret IsolateGroup( SystemIsolateGroupCurrent() )
    }

    #! 请求组内全部 isolate 退出（含当前 isolate）。 !#
    public void exit()
    {
        SystemIsolateGroupExit( this._handle )
    }

    #! 组内存活 isolate 数（诊断用途）。 !#
    get Int32 isolateCount()
    {
        ret SystemIsolateGroupCount( this._handle )
    }

    #! 组 id（诊断用途）。 !#
    get Int64 id()
    {
        ret this._handle
    }
}
