# Tensora —— AI 常用类库

目录：`simple_language/source/Front/Lib/Tensora`
- 工程入口：`Tensora.sp`
- 工程配置：`Tensora.jsonc`
- 依赖：`Core`（Array / List / Map / String / 系统调用）、`Math`（Mathf：Float32 数学）

## 一、分层总览

| 分层 | 文件 | 主要类 |
|------|------|--------|
| 基础 | `Shape.sl` `Tensor.sl` `Math.sl` `Random.sl` `Init.sl` `Ops.sl` | `Shape` `Tensor` `TensorMath` `Rng` `Init` `Ops` |
| 自动微分 | `Autograd.sl` | `Variable` `Graph` |
| 激活 / 损失 | `Activation.sl` `Loss.sl` | `Activation`（`EActivation` 枚举）`Loss` |
| 网络层 | `Layer.sl` `Dense.sl` `Conv.sl` `Pooling.sl` `Normalization.sl` `Dropout.sl` `Module.sl` `Embedding.sl` `Recurrent.sl` | `Layer` `Dense` `Conv2D` `MaxPool2D` `AvgPool2D` `BatchNorm` `LayerNorm` `Dropout` `Module` `Sequential` `Embedding` `RNNCell` `LSTMCell` `GRUCell` `RNN` |
| Transformer | `Attention.sl` `Transformer.sl` `TransformerModel.sl` | `Attention` `MultiHeadAttention` `PositionalEncoding` `TransformerBlock` `Transformer` / `SeqPool` `TransformerClassifier`（`EPoolKind` 枚举） |
| 训练 | `Optimizer.sl` `Scheduler.sl` `Metrics.sl` `Trainer.sl` | `Optimizer` `SGD` `Adam` `AdamW` `RMSProp` `Adagrad` / `LRScheduler` `StepLR` `ExponentialLR` `CosineLR` `WarmupLR` `ReduceLROnPlateau` / `Metrics` / `History` `Trainer` |
| 数据 / 文本 | `Dataset.sl` `Tokenizer.sl` | `Dataset` `DataLoader` `Batch` `Normalizer` / `Vocab` `Tokenizer` |
| 设备 | `Device.sl` | `Device`（`EDeviceKind` 枚举） |

## 二、核心约定

1. **数据类型**：统一 `Float32`（`0.0f`），与 Math 库的 `Mathf` 对齐。
2. **存储**：`Tensor` 用一维 `Array<Float32>` 扁平存储 + `Shape`（行主序，最后一维连续）。
3. **权重形状**：2D 权重统一为 `[fanOut, fanIn]`，`Dense.forward = x · Wᵀ + b`。
4. **参数与梯度**：`Layer.parameters()` / `gradients()` 下标一一对应；复合层（`MultiHeadAttention` / `TransformerBlock`）重写这两个方法聚合子层。
5. **原地更新**：优化器与反向一律用 `addInPlace / subInPlace / scaleInPlace / copyFrom`，不替换张量对象。

## 三、最小示例

```sl
# 1) 搭网络
Module model = Sequential()
model.add( Dense( 784, 256, EActivation.Relu ) )
model.add( Dense( 256, 10 ) )

# 2) 优化器 + 训练器
Optimizer opt = Adam( 0.001f )
Trainer t = Trainer( model, opt, "crossEntropy" )
t.epochs = 10
t.batchSize = 64
t.fit( trainSet, valSet )

# 3) 推理
Array<Int32> pred = t.predictLabels( testX )
Float32 acc = Metrics.accuracy( model.forward( testX ), testLabels )
```

## 四、自动微分（Variable）

```sl
Variable a = Variable.parameter( Tensor.randn( Shape.matrix( 2, 3 ) ) )
Variable b = Variable.parameter( Tensor.randn( Shape.matrix( 3, 1 ) ) )
Variable y = a.matmul( b ).relu().sum()
y.backward()
# a.grad / b.grad 即为梯度
```

支持算子：`add sub mul div matmul scale relu sigmoid tanh exp log pow sum mean softmax reshape transpose`。
广播产生的梯度由 `Ops.reduceTo` 还原；`Variable.setNoGrad(true)` 可关闭构图。

## 五、Transformer 分类模型（TransformerClassifier）

BERT 风格编码器分类器。因本库 Tensor 为 2D（无 batch 维），**一次 forward 处理一条序列**：

```sl
TransformerClassifier model = TransformerClassifier( 1000, 3 )   # vocab / 类别数
# 或全参：TransformerClassifier( vocab, cls, dModel, heads, layers, maxLen, poolKind, dropRate )

Optimizer opt = Adam( 0.001f )
model.fit( seqs, labels, opt, 20, 5 )        # seqs: Array<Tensor>，每条 [T,1] 的 token id

Array<Int32> pred = model.predictLabels( seqs )
Float32 acc = model.evaluate( seqs, labels )
```

- 结构：`Embedding -> PositionalEncoding -> N × TransformerBlock -> LayerNorm -> SeqPool -> Dense`
- 池化：`EPoolKind.Mean`（整条序列平均）/ `EPoolKind.Cls`（取第 0 个位置）
- 批量：`forwardBatch( Array<Tensor> )` 逐条前向再 `Ops.stack` 成 `[N, C]`
- 因 `Trainer` 只认 `[N, F]` 特征矩阵，模型自带 `fitOnce / fit / evaluate` 训练循环

## 六、已知简化（后续可完善）

- `matmul` 仅实现 2D × 2D；未做 batched matmul 与 conv 的 im2col 加速路径。
- `Conv2D` 反向为"教学版"实现，`dx` 用全卷积近似。
- `RNN / LSTMCell / GRUCell` 采用截断 BPTT，只回传当前步梯度。
- `TransformerBlock` 反向为残差近似（Pre-LN 结构，非严格数学等价）。
- `Autograd` 只覆盖上表算子，未实现高阶导数与动态控制流。
- `Tokenizer` 依赖的 `String` 目前只有 `length / range / front / end`，分词为逐字符扫描实现，`lower` 选项待标准库补齐 `toLower` 后启用。
- `Device` 目前只做记录，实际计算仍在 CPU。

## 七、工程约定（与 Core / Math / Render 一致）

- `_main_` / `_test_` / `CompileBefore` / `CompileAfter` 定义在 `Tensora.sp`
- `global.imports` 注入 `Std.Console`
- 新增 `.sl` 文件必须同步登记到 `Tensora.jsonc` 的 `compileFiles.files` 与 `struct`

> 注意：Front 编译时会自动重写本文件为工程模板，上述内容可能被覆盖。
