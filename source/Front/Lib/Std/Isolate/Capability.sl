#!
 * Std/Isolate/Capability.sl
 * 能力令牌：不可伪造语义的 Int64 id 包装，用于 pause/resume/kill 等
 * 敏感操作的授权校验（C VM 侧比对 id，不匹配静默无效）。
 * 可跨 isolate 发送（按值深拷贝，用于把控制权转交给其它 isolate）。
!#

public class Capability extends Object
{
    #! 能力 id（由 C VM 分配；跨 isolate 传递时由 C VM 按值重建）。 !#
    Int64 _capId = 0

    public void _init_( Int64 capId )
    {
        this._capId = capId
    }

    #! 能力 id（诊断用途）。 !#
    get Int64 capId()
    {
        ret this._capId
    }
}
