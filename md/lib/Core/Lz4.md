# Lz4（LZ4 块压缩）

`Lz4` 是 LZ4 块压缩 / 解压的静态工具类，位于 `Core` 标准库的 `Text` 命名空间下（`Core/Text/Lz4.sl`），底座是 `ByteBuffer`（L0 字节层）。

本体在 C VM 端实现，SL 层无状态纯转发：

```
Lz4.compress / decompress            （SL，Lib/Core/Text/Lz4.sl）
  └─ SystemLz4Compress / Decompress  （syscall，byte_buffer_system_method.c §10，栈↔转发）
       └─ sl_core_lz4_*              （机制层，csimple_lang/src/core/sl_core_lz4.c）
            └─ LZ4_compress_default / LZ4_decompress_safe（third_party/lz4）
```

设计契约：`md/design/STREAM_DESIGN.md` §15.6。

---

## 1. 容器格式

产物是**自包含容器**，解压无需外部尺寸提示：

```
[ 4 字节小端原始长度 ][ LZ4 块 ]
```

## 2. API

| 方法 | 说明 |
|------|------|
| `static Int32 compressBound( Int32 inputSize )` | 压缩 `inputSize` 字节最坏情况下的**容器**尺寸（含 4 字节头）；`inputSize` 为负或超出 LZ4 单块上限（约 2GB）时返回 `0` |
| `static ByteBuffer compress( ByteBuffer src ) throws` | 压缩 `src` 的可读区，返回**新**缓冲；`src` 索引不变 |
| `static ByteBuffer decompress( ByteBuffer src ) throws` | 解压 `compress` 的产物，返回**新**缓冲；`src` 索引不变 |

错误枚举（`Lz4Error extends Error`）：

| 枚举值 | code | 触发场景 |
|--------|------|---------|
| `Lz4Error.CompressFailed` | 1 | 压缩失败（`compress` 返回 0 时抛出） |
| `Lz4Error.DecompressFailed` | 2 | 数据损坏 / 截断 / 头部非法（长度超出可读区、LZ4 解压返回负值） |

入参缓冲已释放（`Released`）时先触发 ByteBuffer 的存活守卫（`_ensureLive`）。

## 3. 用法

```sl
# 压缩
var raw = ByteBuffer.fromString( "aaaaaaaabbbbcccc" )
var packed = Lz4.compress( raw )
global.println( packed.readableBytes.toString() + " bytes" )

# 预估最坏容器尺寸（用于预分配）
Int32 worst = Lz4.compressBound( raw.readableBytes )

# 解压
var back = Lz4.decompress( packed )
global.println( back.toString() )     # aaaaaaaabbbbcccc

raw.release()
packed.release()
back.release()
```

## 4. 语义要点

- **不修改源**：`compress` / `decompress` 均不动 `src` 的 readerIndex / writerIndex，只读可读区。
- **返回新缓冲**：调用方负责释放返回的 ByteBuffer。
- **不可压缩数据**：LZ4 是无损压缩，随机 / 已压缩数据产物可能**变大**（上界由 `compressBound` 保证）。
- **空数据**：`compress(空缓冲)` 产出仅含 4 字节头的空容器（hex `00000000`），`decompress` 可正常还原为空缓冲。
- **命名说明**：设计文档原计划通用 `Compress`/`Decompress`/`CompressBound`（GZip 与 Lz4 共用 algo 参数）；实现按「机制优先、不做推测性参数」采用 LZ4 专用命名 `SystemLz4*`，GZip（zlib）落地时再扩展。

## 5. 测试

`test/BaseTest/ByteBufferTest.sl` I 组（14 项断言）：bound 边界（空 / 255 / 负数）、可压缩数据收缩与往返、不可压缩数据不缩与往返、空容器 hex 与尺寸、损坏数据（短头部 / 谎报长度 / 释放后调用）抛错。
