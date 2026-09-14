# ============================================================================
# Core/IO/Stream.sl — L1 元素层：推/拉双模事件流
# 设计契约：md/design/STREAM_DESIGN.md §6 / §11.1 / §17(P1)
#
# P1 范围（可测子集 C/C2/D/E/F）：
#   - abstract Stream<T>：listen 推模式 + IIterable<T> 拉模式
#   - StreamController / StreamSink / StreamSubscription
#   - 惰性变换 map / where / take / skip / transform + 终端 forEach / toList
#   - 生产流 fromIterable / generate / empty / error / fromByteStream
#   - StreamTransformer<S,T> 抽象（具象 Transformer 为 P3）
#   合流 / 广播 / 分支（§6.2 / §6.3 的 P2/P3 项）不落本文件。
#
# 语义要点：
#   - 推模式：listen 启动一个分发协程，从 controller 的事件通道逐个取出
#     _StreamEvent 分发给回调。回调在分发协程内串行同步执行。
#   - 拉模式：iterator 惰性引流 —— moveNext 首调时才 listen，事件写入
#     无缓冲上限 Channel<T>（send 永不挂起），recv 空则挂起（§11.1 E 组）。
#   - 单订阅语义：Stream 默认单订阅（多 listen 会分流事件），
#     广播流为 P3（StreamController.broadcast）。
#   - add：事件通道容量满时挂起（§11.1 F 组，容量 1 controller 可测）；
#     默认构造为无缓冲上限（add 永不挂起）。
#   - cancel：置取消标记 + 关闭事件通道。分发循环在下一轮 recv / 标记
#     检查处退出；上游生产端对已关闭通道 send 抛异常而终止（粗粒度级联）。
#   - 错误不走异常通道：onError 回调接收（§14），throw 仅在 L0 字节层。
#
# 语言设施注记（p1-5 编译验证点，均为全库首例或稀有用法）：
#   - 普通方法重载 Stream.listen（文档 md/syntax/class.md 支持，
#     全库现有重载仅 _init_ 构造）
#   - try? 带返回值 + null 判定（先例 TryTest.sl：string r = try? f(...)）
#   - 泛型方法 map<U> / transform<U> 与泛型基类继承 extends Stream<U>
#     （Codec.sl 同款：_FusedCodec<S,T,M> extends Codec<S,M>）
#   - 闭包捕获限制（doc/closure-design.md：仅外层方法局部变量/参数）：
#     所有 this._x 一律先拷局部再进闭包（ReceivePort.listen 同款模式）
#   - 闭包内局部变量捕获 + 调用捕获对象的方法 / getter
#     （先例 CoroutineTest 闭包体 g_sum / g_order 字段访问，更宽松）
#   - getter 无括号调用（ev.kind / flag.value / ctrl.isClosed）
#
# 契约偏差（与 §6 对照，P4 精细化清单）：
#   - 回调类型用 Function（设计稿 Func<void,T>，全库零命中弃用；
#     先例 Array.forEach / Coroutine.waitUntil / ReceivePort.listen）
#   - StreamSubscription 非泛型、无 onData/onError/onDone setter
#     （P1 无回调重挂场景）
#   - controller.done 为轮询哨兵协程（P4 换事件通知机制）
#   - take 取满 N 靠闭包捕获 sub 延迟绑定实现 cancel；若捕获实现为
#     值拷贝则降级为仅调 onDone 不 cancel，语义仍正确
#   - cancel 粗暴关闭通道：已缓冲的错误对象会丢失（P4 精细化）
#   - StreamIterator 不透传错误：onError 仅关闭引流通道，元素流以
#     null 短路结束（P4 换 Result<T> 元素模型）
# ============================================================================

# ============================================================================
# _StreamEvent — 事件信封（非泛型）
# 嵌套泛型（Channel<StreamEvent<T>> 之类）全库无先例，故 payload/error
# 收敛为 object；kind: 0=data 1=error 2=done。getter 名 payload：data 是 SL 关键字（ETokenType.Data）不可作标识符。
# ============================================================================

public class _StreamEvent extends Object
{
    object _data = null
    object _error = null
    int _kind = 0

    public void markData( object v )
    {
        this._data = v
        this._error = null
        this._kind = 0
    }

    public void markError( object e )
    {
        this._error = e
        this._data = null
        this._kind = 1
    }

    public void markDone()
    {
        this._data = null
        this._error = null
        this._kind = 2
    }

    get int kind()
    {
        ret this._kind
    }

    get object payload()
    {
        ret this._data
    }

    # getter 名 errorValue：error 为 Result 语义保留名（见 md/syntax/result.md），避让
    get object errorValue()
    {
        ret this._error
    }
}

# ============================================================================
# _CancelFlag — 取消标记（闭包内可写的布尔载体）
# ============================================================================

public class _CancelFlag extends Object
{
    bool _v = false

    get bool value()
    {
        ret this._v
    }

    public void setValue( bool v )
    {
        this._v = v
    }
}

# ============================================================================
# StreamSubscription — 订阅句柄（非泛型）
# cancel：置取消标记 + 关闭事件通道，级联终止分发协程与上游生产。
# ============================================================================

public class StreamSubscription extends Object
{
    _CancelFlag _flag = null
    Task _task = null
    Channel<object> _chan = null

    _init_( _CancelFlag flag, Task task, Channel<object> chan )
    {
        this._flag = flag
        this._task = task
        this._chan = chan
    }

    public bool cancel()
    {
        this._flag.setValue( true )
        if this._chan.isClosed == false
        {
            this._chan.close()
        }
        ret true
    }

    get bool isCanceled()
    {
        ret this._flag.value
    }
}

# ============================================================================
# Stream<T> — 元素流抽象（L1 核心）
# ============================================================================

public abstract class Stream<T> extends Object interface Core.IIterable<T>
{
    # ── 消费端核心：注册回调，返回订阅 ──
    # onData(v) / onError(e) / onDone() 均为 Function；
    # onError 触发后若 cancelOnError 为 true 则自动 cancel。

    public abstract StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError );

    # 重载：只关心数据的最简订阅。
    public StreamSubscription listen( Function onData )
    {
        ret this.listen( onData, null, null, false )
    }

    # ── 拉模式（IIterable<T>）──

    override get Core.IIterator<T> iterator()
    {
        var it = StreamIterator<T>( this )
        ret it
    }

    # ── 惰性变换（单订阅链：包装流把回调转发给上游）──

    # 方法名 mapEach：map 是小写容器构造糖（map() => Map），类成员名须避让
    public Stream<U> mapEach<U>( Function mapper )
    {
        var s = _MapStream<T,U>( this, mapper )
        ret s
    }

    public Stream<T> where( Function predicate )
    {
        var s = _WhereStream<T>( this, predicate )
        ret s
    }

    public Stream<T> take( int count )
    {
        var s = _TakeStream<T>( this, count )
        ret s
    }

    public Stream<T> skip( int count )
    {
        var s = _SkipStream<T>( this, count )
        ret s
    }

    public Stream<U> transform<U>( StreamTransformer<T,U> transformer )
    {
        Stream<T> src = this
        ret transformer.apply( src )
    }

    # ── 终端操作（异步，返回 Task；闭包内经 iterator 拉取）──

    public Task forEach( Function action )
    {
        Stream<T> src = this
        var act = action
        function f = function()
        {
            var it = src.iterator
            while it.moveNext()
            {
                act( it.current )
            }
        }
        Task task = Coroutine.spawnClosure0( f )
        ret task
    }

    # 结果经 Task.awaitHandle() 取回（object as List<T>）。
    public Task toListThenTask()
    {
        Stream<T> src = this
        function f = function()
        {
            var collected = List<T>()
            var it = src.iterator
            while it.moveNext()
            {
                collected.add( it.current )
            }
            ret collected
        }
        Task task = Coroutine.spawnClosure0( f )
        ret task
    }

    # ── 生产流工厂 ──

    public static Stream<T> fromIterable( Array<T> items )
    {
        var s = _FromIterableStream<T>( items )
        ret s
    }

    # gen( int i ) 依序产出第 i 个元素（i 从 0 起）。
    public static Stream<T> generate( int count, Function gen )
    {
        var s = _GenerateStream<T>( count, gen )
        ret s
    }

    # 周期流：每 millis 毫秒产出一个 tick() 元素；
    # 无自然终态，订阅 cancel 后泵协程在下一个唤醒点退出（不产多余元素）。
    public static Stream<T> periodic( Int64 millis, Function tick )
    {
        var s = _PeriodicStream<T>( millis, tick )
        ret s
    }

    # 字节流 → 元素流：L0×L2×L1 三层桥接。
    # codec: Codec<T, ByteBuf>（decode 方向 ByteBuf -> T）。
    public static Stream<T> fromByteStream( ByteStream src, Codec<T, ByteBuf> codec )
    {
        var s = _FromByteStreamStream<T>( src, codec )
        ret s
    }

    public static Stream<T> empty()
    {
        var s = _EmptyStream<T>()
        ret s
    }

    # 工厂名 failed：error 为 Result 语义保留名，类成员名须避让
    public static Stream<T> failed( object errInfo )
    {
        var s = _ErrorStream<T>( errInfo )
        ret s
    }
}

# ============================================================================
# StreamIterator<T> — 拉模式迭代器
# moveNext 首调时才 listen 引流：事件写入无缓冲上限 Channel<T>，
# 之后每次 moveNext 从通道 recv（空则挂起，§11.1 E 组）。
# ============================================================================

public class StreamIterator<T> extends Object interface Core.IIterator<T>
{
    Stream<T> _stream = null
    Channel<T> _chan = null
    bool _started = false
    T _current = null

    _init_( Stream<T> stream )
    {
        this._stream = stream
    }

    override bool moveNext()
    {
        if this._started == false
        {
            this._started = true
            this._chan = Channel<T>()
            Channel<T> ch = this._chan
            function onData = function( object v )
            {
                ch.send( v as T )
            }
            function onDone = function()
            {
                ch.close()
            }
            # 错误不透传：仅关闭引流通道，元素流以 null 短路（文件头偏差注记）
            this._stream.listen( onData, null, onDone, false )
        }
        T v = this._chan.recv()
        if v == null
        {
            ret false
        }
        this._current = v
        ret true
    }

    override get T current()
    {
        ret this._current
    }

    override void reset()
    {
        # 流不可重放：reset 为空实现
    }
}

# ============================================================================
# _ControllerStream<T> — controller.stream 的实现
# listen 启动分发协程：从事件通道取 _StreamEvent 逐个分发给回调。
# ============================================================================

public class _ControllerStream<T> extends Stream<T>
{
    StreamController<T> _ctrl = null

    _init_( StreamController<T> ctrl )
    {
        this._ctrl = ctrl
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        Channel<object> ch = this._ctrl.events
        var h = onData
        var e = onError
        var d = onDone
        bool co = cancelOnError
        _CancelFlag flag = _CancelFlag()
        function f = function()
        {
            while true
            {
                object o = ch.recv()
                if o == null
                {
                    break
                }
                if flag.value
                {
                    break
                }
                _StreamEvent ev = o as _StreamEvent
                if ev.kind == 2
                {
                    if d != null
                    {
                        d()
                    }
                    break
                }
                elif ev.kind == 1
                {
                    if e != null
                    {
                        e( ev.errorValue )
                    }
                    if co
                    {
                        flag.setValue( true )
                        break
                    }
                }
                else
                {
                    if h != null
                    {
                        h( ev.payload )
                    }
                }
            }
        }
        Task task = Coroutine.spawnClosure0( f )
        var sub = StreamSubscription( flag, task, ch )
        ret sub
    }
}

# ============================================================================
# StreamController<T> — 流的写入端
# 默认无缓冲上限（add 永不挂起）；_init_(capacity) 指定容量后
# add 在通道满时挂起（§11.1 F 组背压）。
# ============================================================================

public class StreamController<T> extends Object
{
    Channel<object> _channel = null
    Stream<T> _stream = null
    bool _closed = false

    _init_()
    {
        this._channel = Channel<object>()
    }

    _init_( int capacity )
    {
        this._channel = Channel<object>( capacity )
    }

    # 对应的读取端（惰性创建，重复读取返回同一实例）。
    get Stream<T> stream()
    {
        if this._stream == null
        {
            this._stream = _ControllerStream<T>( this )
        }
        ret this._stream
    }

    # 分块转换输出端（衔接 Codec 的 chunked API）。
    get StreamSink<T> sink()
    {
        var s = StreamSink<T>( this )
        ret s
    }

    # 原始事件通道（_ControllerStream 分发协程的数据源）。
    get Channel<object> events()
    {
        ret this._channel
    }

    get bool isClosed()
    {
        ret this._closed
    }

    # 投递一个数据事件；通道满时挂起。
    public void add( T value )
    {
        if this._closed
        {
            ret
        }
        var ev = _StreamEvent()
        ev.markData( value )
        this._channel.send( ev )
    }

    # 投递一个错误事件；通道满时挂起。
    # 参数名 errInfo：error 为 Result 语义保留名，避免使用
    public void addError( object errInfo )
    {
        if this._closed
        {
            ret
        }
        var ev = _StreamEvent()
        ev.markError( errInfo )
        this._channel.send( ev )
    }

    # 投递 done 事件并关闭通道（已缓冲事件仍可被取尽）。
    public void close()
    {
        if this._closed
        {
            ret
        }
        var ev = _StreamEvent()
        ev.markDone()
        this._channel.send( ev )
        this._channel.close()
        this._closed = true
    }

    # 写入端收尾等待（P1 轮询实现，P4 换事件通知）。
    get Task done()
    {
        StreamController<T> ctrl = this
        function f = function()
        {
            while ctrl.isClosed == false
            {
                Coroutine.sleep( 1 )
            }
        }
        Task task = Coroutine.spawnClosure0( f )
        ret task
    }
}

# ============================================================================
# StreamSink<T> — controller 的 sink 视图（extends Codec 的分块输出端）
# fromByteStream 桥接关键：decoder 的 chunked 输出直接喂给 controller。
# ============================================================================

public class StreamSink<T> extends ChunkedConversionSink<T>
{
    StreamController<T> _ctrl = null

    _init_( StreamController<T> ctrl )
    {
        this._ctrl = ctrl
    }

    override public void add( T chunk )
    {
        this._ctrl.add( chunk )
    }

    override public void close()
    {
        this._ctrl.close()
    }
}

# ============================================================================
# _MapStream<S,U> — 惰性映射：fn(v) 结果转发给下游 onData
# ============================================================================

public class _MapStream<S,U> extends Stream<U>
{
    Stream<S> _source = null
    Function _mapper = null

    _init_( Stream<S> source, Function mapper )
    {
        this._source = source
        this._mapper = mapper
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        Stream<S> src = this._source
        var fn = this._mapper
        var h = onData
        function fwd = function( object v )
        {
            h( fn( v ) )
        }
        ret src.listen( fwd, onError, onDone, cancelOnError )
    }
}

# ============================================================================
# _WhereStream<T> — 惰性过滤：fn(v) as bool 为 true 才放行
# ============================================================================

public class _WhereStream<T> extends Stream<T>
{
    Stream<T> _source = null
    Function _predicate = null

    _init_( Stream<T> source, Function predicate )
    {
        this._source = source
        this._predicate = predicate
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        Stream<T> src = this._source
        var fn = this._predicate
        var h = onData
        function fwd = function( object v )
        {
            bool keep = fn( v ) as bool
            if keep
            {
                h( v )
            }
        }
        ret src.listen( fwd, onError, onDone, cancelOnError )
    }
}

# ============================================================================
# _TakeStream<T> — 惰性截断：取满 N 后 cancel 上游 + 触发 onDone
# sub 延迟绑定：闭包先声明捕获，listen 返回后再赋值（见文件头偏差注记）。
# ============================================================================

public class _TakeStream<T> extends Stream<T>
{
    Stream<T> _source = null
    int _count = 0

    _init_( Stream<T> source, int count )
    {
        this._source = source
        this._count = count
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        Stream<T> src = this._source
        int n = this._count
        var h = onData
        var d = onDone
        int taken = 0
        StreamSubscription sub = null
        function fwd = function( object v )
        {
            if taken < n
            {
                h( v )
                taken = taken + 1
                if taken >= n
                {
                    if sub != null
                    {
                        sub.cancel()
                    }
                    if d != null
                    {
                        d()
                    }
                }
            }
        }
        sub = src.listen( fwd, onError, onDone, cancelOnError )
        ret sub
    }
}

# ============================================================================
# _SkipStream<T> — 惰性跳过：丢弃前 N 个后透传
# ============================================================================

public class _SkipStream<T> extends Stream<T>
{
    Stream<T> _source = null
    int _count = 0

    _init_( Stream<T> source, int count )
    {
        this._source = source
        this._count = count
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        Stream<T> src = this._source
        int n = this._count
        var h = onData
        int skipped = 0
        function fwd = function( object v )
        {
            if skipped < n
            {
                skipped = skipped + 1
            }
            else
            {
                h( v )
            }
        }
        ret src.listen( fwd, onError, onDone, cancelOnError )
    }
}

# ============================================================================
# StreamTransformer<S,T> — 流到流的变换器（transform 的目标类型）
# ============================================================================

public abstract class StreamTransformer<S,T> extends Object
{
    # bind 为 SL 保留 token（类头 extends/interface/bind），方法名改用 apply
    public abstract Stream<T> apply( Stream<S> source );
}

# ============================================================================
# _FromIterableStream<T> — 数组生产流：生产协程逐个 add 后 close
# ============================================================================

public class _FromIterableStream<T> extends Stream<T>
{
    Array<T> _items = null

    _init_( Array<T> items )
    {
        this._items = items
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        StreamController<T> ctrl = StreamController<T>()
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        Array<T> items = this._items
        int n = items.length
        function f = function()
        {
            int i = 0
            while i < n
            {
                ctrl.add( items[ i ] )
                i = i + 1
            }
            ctrl.close()
        }
        Coroutine.spawnClosure0( f )
        ret sub
    }
}

# ============================================================================
# _GenerateStream<T> — 生成生产流：gen(i) 产出 i ∈ [0, count)
# ============================================================================

public class _GenerateStream<T> extends Stream<T>
{
    int _count = 0
    Function _gen = null

    _init_( int count, Function gen )
    {
        this._count = count
        this._gen = gen
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        StreamController<T> ctrl = StreamController<T>()
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        int n = this._count
        var gen = this._gen
        function f = function()
        {
            int i = 0
            while i < n
            {
                ctrl.add( gen( i ) as T )
                i = i + 1
            }
            ctrl.close()
        }
        Coroutine.spawnClosure0( f )
        ret sub
    }
}

# ============================================================================
# _PeriodicStream<T> — 周期流：每 millis 毫秒产出一个 tick() 元素
# ============================================================================

public class _PeriodicStream<T> extends Stream<T>
{
    Int64 _millis = 0
    Function _tick = null

    _init_( Int64 millis, Function tick )
    {
        this._millis = millis
        this._tick = tick
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        StreamController<T> ctrl = StreamController<T>()
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        Int64 ms = this._millis
        var tick = this._tick
        function f = function()
        {
            while sub.isCanceled == false
            {
                Coroutine.sleep( ms )
                if sub.isCanceled
                {
                    break
                }
                ctrl.add( tick() as T )
            }
            ctrl.close()
        }
        Coroutine.spawnClosure0( f )
        ret sub
    }
}

# ============================================================================
# _EmptyStream<T> — 空流：订阅即 done
# ============================================================================

public class _EmptyStream<T> extends Stream<T>
{
    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        StreamController<T> ctrl = StreamController<T>()
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        ctrl.close()
        ret sub
    }
}

# ============================================================================
# _ErrorStream<T> — 错误流：订阅即 error + done
# ============================================================================

public class _ErrorStream<T> extends Stream<T>
{
    object _error = null

    _init_( object errInfo )
    {
        this._error = errInfo
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        StreamController<T> ctrl = StreamController<T>()
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        ctrl.addError( this._error )
        ctrl.close()
        ret sub
    }
}

# ============================================================================
# _FromByteStreamStream<T> — 字节流解码生产流（L0×L2×L1 三层桥接）
# 生产协程循环 read → decoder 分块转换 → sink 写入 controller；
# read 异常（try? 返 null）转投 StreamIOError.Timeout 事件（P4 透传原始错误）。
# ============================================================================

public class _FromByteStreamStream<T> extends Stream<T>
{
    ByteStream _src = null
    Codec<T, ByteBuf> _codec = null

    _init_( ByteStream src, Codec<T, ByteBuf> codec )
    {
        this._src = src
        this._codec = codec
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        StreamController<T> ctrl = StreamController<T>( 64 )
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        ByteStream src = this._src
        Codec<T, ByteBuf> codec = this._codec
        function f = function()
        {
            Converter<ByteBuf, T> decoder = codec.decoder
            StreamSink<T> sk = ctrl.sink
            var dec = decoder.startChunkedConversion( sk )
            ByteBuf buf = ByteBuf( 4096 )
            bool failed = false
            while true
            {
                Int32 got = try? src.read( buf )
                if got == null
                {
                    failed = true
                    break
                }
                if got <= 0
                {
                    break
                }
                # buf 复用：decoder 实现须在 add 内拷贝（分块所有权约定）
                dec.add( buf )
                buf.clear()
            }
            if failed
            {
                ctrl.addError( StreamIOError.Timeout )
            }
            dec.close()
            ctrl.close()
        }
        Coroutine.spawnClosure0( f )
        ret sub
    }
}
