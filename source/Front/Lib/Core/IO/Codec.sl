# ============================================================================
# Core/IO/Codec.sl — L2 转换层：Converter / Codec 框架
# 设计契约：md/design/STREAM_DESIGN.md §7
#
# P1 范围：纯框架（ChunkedConversionSink / Converter / Codec + 组合辅助类
# _FusedConverter / _FusedCodec）。具象 Codec（Base64 / Hex / GZip / Utf8 /
# ProtoBuf …）为 P2 / P3，不落本文件；P1 测试用自定义子类验证框架。
#
# 语义要点：
#   - L2 永不挂起（§11.1）：全部方法无 throws，实现方也不应引入挂起点。
#   - 方向约定：Codec<S,T> 的 encode 走 S -> T（即 Converter<S,T> 的 convert
#     同向），decode 走 T -> S（decoder 为 Converter<T,S>）。
#   - fuse 顺向组合：Converter<S,T>.fuse(Converter<T,M>) -> Converter<S,M>；
#     fuseCodec 同理，其中 decoder 逆向串接（M -> T -> S）。
#   - ChunkedConversionSink 是分块转换的输出回调：startChunkedConversion 把
#     下游 sink 包装成接收上游分块的 sink（流式管道），实现方在 close 时
#     flush 中间态。
#
# 语法注记（全库无先例、留 p1-5 编译验证）：
#   - 泛型基类继承 `extends Converter<S,M>`：md/syntax/template.md「模板类
#     继承」有文档（PairList<T> extends List<Pair<T,T>>），解析层
#     FileMetaClassDefine.inputTemplateNodeList 支持，仅无 .sl 实例。
#   - Converter.fuse / Codec.fuseCodec 方法体引用后置的 _FusedConverter /
#     _FusedCodec：同文件前向引用。Front 为两阶段（CombineFileMeta 先注册
#     全部类型，ParseMemberExpress 后解析方法体），引用安全。
# ============================================================================

# ============================================================================
# ChunkedConversionSink<T> — 分块转换输出端
# ============================================================================

public abstract class ChunkedConversionSink<T> extends Object
{
    # 接收一个分块（所有权语义由实现决定：转交或拷贝）
    public abstract void add( T chunk );

    # 关闭：flush 中间态后不再接收数据
    public abstract void close();
}

# ============================================================================
# Converter<S,T> — 单向转换器（S -> T）
# ============================================================================

public abstract class Converter<S,T> extends Object
{
    # 一次性转换
    public abstract T convert( S input );

    # 分块转换：返回接收 S 分块的 sink，输出接入下游 sink
    public abstract ChunkedConversionSink<S> startChunkedConversion( ChunkedConversionSink<T> sink );

    # 顺向组合：this 之后接 other，得到 S -> M
    public Converter<S,M> fuse<M>( Converter<T,M> other )
    {
        ret _FusedConverter<S,T,M>( this, other )
    }
}

# ============================================================================
# _FusedConverter<S,T,M> — fuse 的组合实现（内部辅助类）
# ============================================================================

public class _FusedConverter<S,T,M> extends Converter<S,M>
{
    Converter<S,T> _first = null
    Converter<T,M> _second = null

    _init_( Converter<S,T> first, Converter<T,M> second )
    {
        this._first = first
        this._second = second
    }

    override public M convert( S input )
    {
        ret this._second.convert( this._first.convert( input ) )
    }

    override public ChunkedConversionSink<S> startChunkedConversion( ChunkedConversionSink<M> sink )
    {
        # 先包装第二级（接收 M 分块），再让第一级输出接入它
        ret this._first.startChunkedConversion( this._second.startChunkedConversion( sink ) )
    }
}

# ============================================================================
# Codec<S,T> — 编解码器（encode: S -> T，decode: T -> S）
# ============================================================================

public abstract class Codec<S,T> extends Converter<S,T>
{
    # 编码器：S -> T
    abstract get Converter<S,T> encoder();

    # 解码器：T -> S
    abstract get Converter<T,S> decoder();

    # Converter 契约：convert 即编码方向（Codec 可直接当 Converter 用）
    override public T convert( S input )
    {
        ret this.encoder.convert( input )
    }

    public T encode( S input )
    {
        ret this.encoder.convert( input )
    }

    public S decode( T output )
    {
        ret this.decoder.convert( output )
    }

    # 顺向组合：this 之后接 other，得到 Codec<S,M>
    public Codec<S,M> fuseCodec<M>( Codec<T,M> other )
    {
        ret _FusedCodec<S,T,M>( this, other )
    }
}

# ============================================================================
# _FusedCodec<S,T,M> — fuseCodec 的组合实现（内部辅助类）
# ============================================================================

public class _FusedCodec<S,T,M> extends Codec<S,M>
{
    Codec<S,T> _first = null
    Codec<T,M> _second = null

    _init_( Codec<S,T> first, Codec<T,M> second )
    {
        this._first = first
        this._second = second
    }

    # encoder 顺向：S -> T -> M
    # 注：getter 返回值上直接链式调用模板方法（fuse<M>）时，Front 泛型
    # 实例化的 owner 类回溯不到 MetaVariable/MetaClass（getter 调用节点两者
    # 皆空），会 NRE；先落局部变量再调用（既有库先例均为变量前缀形式）。
    override get Converter<S,M> encoder()
    {
        Converter<S,T> e1 = this._first.encoder
        Converter<T,M> e2 = this._second.encoder
        ret e1.fuse<M>( e2 )
    }

    # decoder 逆向：M -> T -> S
    override get Converter<M,S> decoder()
    {
        Converter<M,T> d1 = this._second.decoder
        Converter<T,S> d2 = this._first.decoder
        ret d1.fuse<S>( d2 )
    }

    # Converter 契约：分块转换顺向串接（与 _FusedConverter 同构，
    # _first: Codec<S,T> 即 Converter<S,T>，_second 同理）
    override public ChunkedConversionSink<S> startChunkedConversion( ChunkedConversionSink<M> sink )
    {
        ret this._first.startChunkedConversion( this._second.startChunkedConversion( sink ) )
    }
}
