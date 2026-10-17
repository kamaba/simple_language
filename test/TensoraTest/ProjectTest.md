# TensoraTest

Tensora 库（AI 常用类）的测试用例与示例集合。

- 工程入口：`ProjectTest.sp`（`_main_` 依次调用各用例的 `fun()`）
- 工程配置：`ProjectTest.jsonc`
- 依赖模块：`Core`、`Math`、`Tensora`（从 `out/export/<模块>` 装载，需先编译并导出这三个库）

## 用例清单

| 文件 | 内容 |
|------|------|
| `TensorTest.sl` | `Shape` / `Tensor` 构造、索引、广播、`matmul`、归约、数学函数、softmax |
| `AutogradTest.sl` | `Variable` 自动微分：标量链、矩阵链、广播回传、softmax 梯度 |
| `CnnTest.sl` | **ONNX 风格 CNN**：节点列表 + 逐个 dispatch（Conv→Relu→MaxPool→Flatten→Gemm→Softmax）；再用 `Module` 组装同构网络跑 forward/backward；两层卷积迷你 LeNet；BatchNorm/LayerNorm/Dropout |
| `NnTest.sl` | MLP 分类训练（`Trainer` + `Adam` + 交叉熵）；SGD/Adam/AdamW/RMSProp/Adagrad 对比；学习率调度；RNN/LSTM/GRU；Embedding + 位置编码 + 多头注意力 + Transformer |
| `ClassicMlTest.sl` | 传统机器学习示例：线性回归、逻辑回归、KNN、KMeans、高斯朴素贝叶斯、决策树桩、PCA（算法直接写在测试里，可提升进库） |
| `DataTest.sl` | `Dataset` / `DataLoader` / `Normalizer` / `Tokenizer` / `Metrics` / `Device` |
| `TtsTest.sl` | 极简 TTS：Conv1d 栈 + Transformer（Wav2Vec2 风格编码器）+ 时长预测 / 长度调节 + Mel 解码 + 谐波声码器，端到端「文本 -> mel -> 波形」 |
| `TransformerModelTest.sl` | BERT 风格 `TransformerClassifier`：形状与参数量、`SeqPool` 的 Mean/Cls 池化及梯度、小样本训练、推理（概率 / 批量预测） |

## 运行

```powershell
# 1) 先编译并导出 Core / Math / Tensora 三个库
dotnet run --project source/Front/SimpleLanguageFront.csproj -- compile -p source/Front/Lib/Tensora/Tensora.sp

# 2) 再编译并运行本测试工程
dotnet run --project source/CSimpleVMTest1/CSimpleVMTest.csproj -- test/TensoraTest/ProjectTest.sp
```

## 约定

- 每个用例为 `XxxTest { static fun() { ... } }`，`fun()` 内分小节函数
- 输出统一走 `global.println`（定义在 `ProjectTest.sp` 的 `Project{}` 里）
- 断言用各文件内的 `check( name, cond )`，只打印 OK/FAIL，不抛异常
- 本目录不写业务代码，算法示例若稳定下来再迁移进 `source/Front/Lib/Tensora`
