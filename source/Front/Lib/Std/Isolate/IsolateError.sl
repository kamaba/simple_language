#!
 * Std/Isolate/IsolateError.sl
 * isolate 错误码常量类。序号与 C VM 的 VM_ISO_ERR_* 负数错误码一一对应
 * （C 码 = -(70 + 本表序号)，如 SpawnFailed -> -70）。
!#

public class IsolateError extends Object
{
    public static const Int32 None              = 0
    public static const Int32 SpawnFailed       = 1   # 入口非法（非函数值 / method_id 解析失败）/ 资源不足
    public static const Int32 NotSendable       = 2   # 消息含不可发送对象
    public static const Int32 CyclicMessage     = 3   # 消息图含环（一期不支持）
    public static const Int32 TransferInvalid   = 4   # 已转移的 TransferableData 被再次使用
    public static const Int32 InvalidHandle     = 5   # port / capability / isolate 句柄无效
    public static const Int32 PortClosed        = 6   # 向已关闭的 port 发送
    public static const Int32 IsolateDead       = 7   # 目标 isolate 已终止
    public static const Int32 PermissionDenied  = 8   # capability 不匹配
}
