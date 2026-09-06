#!
 * Std/Isolate/TransferableData.sl
 * 零拷贝转移块：一段字节数据的独占所有权句柄。
 * 跨 isolate 发送时不深拷贝内容，只转移所有权（发送后本句柄失效）；
 * 接收方 materialize 取回字节内容（一次性，取出后句柄同样失效）。
!#

public class TransferableData extends Object
{
    #! 转移块句柄（由 C VM 分配；跨 isolate 传递时由 C VM 转移所有权）。 !#
    Int64 _handle = 0

    public void _init_( Int64 handle )
    {
        this._handle = handle
    }

    #!
     * 从字节数组创建转移块（拷贝一次进 C 侧 blob）。
     * 之后把本对象发往其它 isolate 即为零拷贝所有权转移。
    !#
    public static TransferableData fromBytes( Array<UInt8> bytes )
    {
        ret TransferableData( SystemTransferFromBytes( bytes ) )
    }

    #!
     * 取回字节内容。取出后本句柄失效（isValid 变 false）；
     * 句柄无效（已转移 / 已取出）时返回 null。
    !#
    public Array<UInt8> materialize()
    {
        ret SystemTransferMaterialize( this._handle ) as Array<UInt8>
    }

    #! 转移块字节数（句柄失效后为 0）。 !#
    get Int32 size()
    {
        ret SystemTransferSize( this._handle )
    }

    #! 句柄是否仍然有效（未转移且未取出）。 !#
    get bool isValid()
    {
        ret SystemTransferIsValid( this._handle )
    }
}
