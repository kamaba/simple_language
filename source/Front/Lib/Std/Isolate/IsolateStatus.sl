#!
 * Std/Isolate/IsolateStatus.sl
 * isolate 状态常量类。与 C VM 的 isolate 状态枚举一一对应。
 * 由 SystemIsolateStatus / Isolate.status 返回。
!#

public class IsolateStatus extends Object
{
    public static const Int32 Created  = 0
    public static const Int32 Ready    = 1
    public static const Int32 Running  = 2
    public static const Int32 Paused   = 3
    public static const Int32 Exiting  = 4
    public static const Int32 Dead     = 5
}
