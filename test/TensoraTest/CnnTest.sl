import Tensora

# CnnTest —— ONNX 风格的 CNN 示例
#
# 两部分：
#   1) onnxRuntimeTest：模拟 ONNX 图的「节点列表 + 逐个 dispatch」执行方式
#      input[1,8,8] -> Conv(1->4,k=3,pad=1) -> Relu -> MaxPool(2) -> Flatten -> Gemm(64->10) -> Softmax
#   2) moduleTest：用库里的 Module 组装同样的结构，跑一次 forward + backward
#
# 说明：本库的 Conv2D / 池化使用 [C,H,W]（不含 batch 维），因此这里的 NCHW 省掉 N

CnnTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[CnnTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[CnnTest] " + name + " : FAIL" )
        }
    }

    # ── 1. 模拟 ONNX runtime ─────────────────────────────
    static onnxRuntimeTest()
    {
        global.println( "===== onnxRuntimeTest =====" )

        # 图定义：算子名 + 输出别名（对应 ONNX 的 node.op_type / node.output）
        Array<string> opTypes = [ "Conv", "Relu", "MaxPool", "Flatten", "Gemm", "Softmax" ]
        Array<string> outNames = [ "conv1", "relu1", "pool1", "flat1", "gemm1", "prob" ]

        # 权重（对应 ONNX 的 initializer）
        Conv2D conv = Conv2D( 1, 4, 3, 1, 1, EActivation.None )
        Dense gemm = Dense( 64, 10 )

        Tensor x = Tensor.random( Shape.cube( 1, 8, 8 ) )
        global.println( "input shape = " + x.shape().toString() )

        Tensor cur = x
        for i = 0, i < opTypes.length, i++
        {
            string op = opTypes[i]
            if op == "Conv"
            {
                cur = conv.forward( cur )
            }
            elif op == "Relu"
            {
                cur = Activation.relu( cur )
            }
            elif op == "MaxPool"
            {
                cur = Ops.maxPool2d( cur, 2, 2 )
            }
            elif op == "Flatten"
            {
                cur = cur.flatten().reshape( Shape.matrix( 1, cur.size() ) )
            }
            elif op == "Gemm"
            {
                cur = gemm.forward( cur )
            }
            elif op == "Softmax"
            {
                cur = cur.softmax()
            }
            global.println( "node[" + i.toString() + "] " + op + " -> " + outNames[i] + " " + cur.shape().toString() )
        }

        check( "output shape == (1,10)", cur.rows() == 1 && cur.cols() == 10 )
        check( "softmax sum ~= 1", TensorMath.abs( cur.sum() - 1.0f ) < 0.0001f )
        global.println( "pred class = " + cur.argmax().toString() )
    }

    # ── 2. 用 Module 组装同样的网络 ──────────────────────
    static moduleTest()
    {
        global.println( "===== moduleTest =====" )

        Sequential net = Sequential()
        net.add( Conv2D( 1, 4, 3, 1, 1, EActivation.Relu ) )
        net.add( MaxPool2D( 2, 2 ) )
        net.add( Flatten() )
        net.add( Dense( 64, 10 ) )

        global.println( net.summary() )
        global.println( "paramCount = " + net.paramCount().toString() )

        Tensor x = Tensor.random( Shape.cube( 1, 8, 8 ) )
        Tensor logits = net.forward( x )
        check( "logits shape == (1,10)", logits.rows() == 1 && logits.cols() == 10 )

        # 反向：从交叉熵梯度回传到输入
        Array<Int32> labels = [ 3 ]
        Tensor g = Loss.crossEntropyGrad( logits, labels )
        Tensor gx = net.backward( g )
        check( "grad wrt input shape == (1,8,8)", gx.shape().equals( x.shape() ) )

        Array<Tensor> ps = net.parameters()
        Array<Tensor> gs = net.gradients()
        check( "params == grads count", ps.length == gs.length )

        # 用 Adam 更新一步
        Optimizer opt = Adam( 0.001f )
        opt.step( ps, gs )
        net.zeroGrad()
        global.println( "one Adam step done" )
    }

    # ── 3. 两层卷积（LeNet 迷你版）────────────────────────
    static twoLayerCnnTest()
    {
        global.println( "===== twoLayerCnnTest =====" )

        Sequential net = Sequential()
        net.add( Conv2D( 1, 2, 3, 1, 1, EActivation.Relu ) )   # 8x8  -> 2x8x8
        net.add( AvgPool2D( 2, 2 ) )                           #      -> 2x4x4
        net.add( Conv2D( 2, 4, 3, 1, 1, EActivation.Relu ) )   #      -> 4x4x4
        net.add( MaxPool2D( 2, 2 ) )                           #      -> 4x2x2
        net.add( Flatten() )                                   #      -> 1x16
        net.add( Dense( 16, 3 ) )                              #      -> 1x3

        Tensor x = Tensor.random( Shape.cube( 1, 8, 8 ) )
        Tensor out = net.forward( x )
        global.println( "out = " + out.toString() )
        check( "two-layer out == (1,3)", out.rows() == 1 && out.cols() == 3 )

        Tensor prob = out.softmax()
        global.println( "prob sum ~= " + SystemConvertString( prob.sum() ) )
    }

    # ── 4. 归一化 / Dropout 小例子 ───────────────────────
    static normDropoutTest()
    {
        global.println( "===== normDropoutTest =====" )

        Tensor x = Tensor.random( Shape.matrix( 4, 5 ) )

        BatchNorm bn = BatchNorm( 5 )
        Tensor bnOut = bn.forward( x )
        global.println( "batchnorm out cols mean ~= " + SystemConvertString( bnOut.sumAlong( 0 ).get( 0, 0 ) ) )

        LayerNorm ln = LayerNorm( 5 )
        Tensor lnOut = ln.forward( x )
        check( "layernorm shape keep", lnOut.shape().equals( x.shape() ) )

        Dropout drop = Dropout( 0.5f )
        drop.train( true )
        Tensor dOut = drop.forward( x )
        drop.train( false )
        Tensor eOut = drop.forward( x )
        check( "eval mode == input", eOut.get( 0, 0 ) == x.get( 0, 0 ) )
        global.println( "dropout train mean ~= " + SystemConvertString( dOut.mean() ) )
    }

    static fun()
    {
        global.println( "========== CnnTest (start) ==========" )
        onnxRuntimeTest()
        moduleTest()
        twoLayerCnnTest()
        normDropoutTest()
        global.println( "========== CnnTest (end) ==========" )
    }
}
