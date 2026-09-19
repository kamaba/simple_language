# Watcher: 独立数据监视类（DEBUG_SYSTEM_DESIGN.md §6）
# 纯 SL 实现：不生成 opcode、不走帧缓冲、无系统调用、零 VM 改动，
# 编译/运行与 optimize 等级完全无关。
# 用途：业务代码中的值变化信号器——监视数据变化 → 自动发信号 → 触发绑定
#       回调；也支持 emit() 手动发信号。
# 存储注记：回调用单向链表而非 Array<Function>——闭包对象在 VM 对象层不携带
#       Core.Function 类型信息，Array<T>._getItem_ 的 `as T` 转型会把闭包静默
#       置 null（CastClass 继承检查失败）；链表节点以 Function 字段存储 +
#       var 读取（无转型）+ 直接调用，与 Stream 的 _MapStream._mapper 同模式。

# 回调节点：Function 字段直存闭包，绕开数组 _getItem_ 的 as T 转型
class _WatcherCallbackNode
{
	Function _fn = null
	_WatcherCallbackNode _next = null

	#注：参数/局部变量不可命名 next（语言关键字，与 bind 同类限制）
	_init_( Function fn, _WatcherCallbackNode nxt )
	{
		this._fn = fn
		this._next = nxt
	}
}

public class Watcher<T>
{
	T _value = null
	T _last = null
	bool _changed = false
	#重入保护：回调链执行期间为 true，回调内再 update/emit 本轮不再触发（§6.3）
	bool _inEmit = false
	int _cbCount = 0
	_WatcherCallbackNode _head = null
	_WatcherCallbackNode _tail = null

	#创建（初值不触发信号，§6.2）：_value 与 _last 同置初值
	public static Watcher<T> create( T init )
	{
		var w = Watcher<T>()
		w._value = init
		w._last = init
		ret w
	}

	#更新并检测：值变化 → 触发全部回调并返回 true；未变化返回 false（§6.2）
	#变化判定：== 对数值/字符串/布尔按值、对类实例按引用（§6.3；深比较列 P8）
	public bool update( T value )
	{
		this._last = this._value
		this._value = value
		this._changed = false
		if this._value == this._last
		{
			ret false
		}
		this._changed = true
		this.fireCallbacks()
		ret true
	}

	#绑定回调（无参闭包，可多个，§6.2）
	#注：设计文档 API 名为 bind/unbind，但 bind 为语言保留字（数据绑定语法），
	#故沿用 Debug 门面 P4 的 listen/unlisten 命名（与 stack→stackView 同类偏差）
	public void listen( Function callback )
	{
		if callback == null
		{
			ret
		}
		var node = _WatcherCallbackNode( callback, null )
		if this._head == null
		{
			this._head = node
		}
		else
		{
			this._tail._next = node
		}
		this._tail = node
		this._cbCount++
	}

	#解绑回调：按引用相等移除首个匹配；成功 true，未绑定 false（§6.2）
	public bool unlisten( Function callback )
	{
		if callback == null
		{
			ret false
		}
		_WatcherCallbackNode prev = null
		_WatcherCallbackNode cur = this._head
		while cur != null
		{
			if Object.refEquals( cur._fn, callback )
			{
				if prev == null
				{
					this._head = cur._next
				}
				else
				{
					prev._next = cur._next
				}
				if this._tail == cur
				{
					this._tail = prev
				}
				this._cbCount--
				ret true
			}
			prev = cur
			cur = cur._next
		}
		ret false
	}

	#手动发信号：无视变化，触发全部回调（§6.2）
	public void emit()
	{
		this.fireCallbacks()
	}

	get T value()
	{
		ret this._value
	}
	get T last()
	{
		ret this._last
	}
	get bool changed()
	{
		ret this._changed
	}

	#触发全部回调；重入保护：回调链执行期间再进入直接返回（§6.3）
	#迭代安全：先记下 next 再调用——回调内 update/emit 重入被 _inEmit 挡住，
	#将来回调内 unlisten 也不会让遍历断链
	void fireCallbacks()
	{
		if this._inEmit
		{
			ret
		}
		this._inEmit = true
		_WatcherCallbackNode cur = this._head
		while cur != null
		{
			var cb = cur._fn
			_WatcherCallbackNode nxt = cur._next
			cb()
			cur = nxt
		}
		this._inEmit = false
	}
}
