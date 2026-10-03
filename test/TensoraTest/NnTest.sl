import Tensora

# NnTest —— 神经网络训练示例
#
#   mlpTrainTest         ：MLP 分类（Trainer + Adam + 交叉熵）
#   optimizerCompareTest ：SGD / Adam / RMSProp / Adagrad 对比
#   schedulerTest        ：学习率调度打印
#   rnnTest              ：RNNCell / LSTMCell / GRUCell / RNN 序列
#   transformerTest      ：Embedding + 位置编码 + 多头注意力 + Transformer

NnTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[NnTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[NnTest] " + name + " : FAIL" )
        }
    }

    # 构造可分数据：第 c 类样本围绕均值 c*2 分布
    static Tensor makeClassData( Array<Int32> labels )
    {
        Rng rng = Rng( 42 )
        int n = labels.length
        Tensor x = Tensor( Shape.matrix( n, 4 ) )
        for i = 0, i < n, i++
        {
            Float32 center = SystemConvertFloat32( labels[i] ) * 2.0f
            for j = 0, j < 4, j++
            {
                x.set( i, j, center + rng.normal( 0.0f, 0.3f ) )
            }
        }
        ret x
    }

    # ── 1. MLP 分类训练 ──────────────────────────────────
    static mlpTrainTest()
    {
        global.println( "===== mlpTrainTest =====" )

        Array<Int32> labels = [ 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2 ]
        Tensor x = NnTest.makeClassData( labels )
        Tensor y = Ops.oneHot( labels, 3 )

        Normalizer nz = Normalizer()
        nz.fit( x )
        Dataset train = Dataset( nz.transform( x ), y, "toy-3class" )

        Module model = Sequential()
        model.add( Dense( 4, 8, EActivation.Relu ) )
        model.add( Dense( 8, 3 ) )

        Optimizer opt = Adam( 0.05f )
        Trainer t = Trainer( model, opt, "crossEntropy" )
        t.epochs = 30
        t.batchSize = 4
        t.verbose = false
        t.fit( train, train )

        Float32 acc = t.evaluate( train )
        global.println( "train acc = " + SystemConvertString( acc ) )
        global.println( "last loss = " + SystemConvertString( t.history.lastTrainLoss() ) )
        check( "acc > 0.5", acc > 0.5f )

        Array<Int32> pred = t.predictLabels( train.x )
        check( "predict count == 12", pred.length == 12 )
    }

    # ── 2. 优化器对比（回归：y = 2*x0 + 3*x1）─────────────
    static Float32 runOptimizer( string tag, Optimizer opt, Int32 steps )
    {
        Tensor x = Tensor.random( Shape.matrix( 16, 2 ) )
        Tensor w = Tensor.vector( [ 2.0f, 3.0f ] ).reshape( Shape.matrix( 2, 1 ) )
        Tensor y = x.matmul( w )

        Dense fc = Dense( 2, 1 )
        for s = 0, s < steps, s++
        {
            fc.zeroGrad()
            Tensor pred = fc.forward( x )
            Float32 loss = Loss.mse( pred, y )
            fc.backward( Loss.mseGrad( pred, y ) )
            opt.step( fc.parameters(), fc.gradients() )
            if s == steps - 1
            {
                global.println( tag + " final mse = " + SystemConvertString( loss ) )
            }
        }
        ret Loss.mse( fc.forward( x ), y )
    }

    static optimizerCompareTest()
    {
        global.println( "===== optimizerCompareTest =====" )
        NnTest.runOptimizer( "SGD     ", SGD( 0.02f ), 30 )
        NnTest.runOptimizer( "SGD-mom ", SGD( 0.02f, 0.9f ), 30 )
        NnTest.runOptimizer( "Adam    ", Adam( 0.05f ), 30 )
        NnTest.runOptimizer( "AdamW   ", AdamW( 0.05f, 0.01f ), 30 )
        NnTest.runOptimizer( "RMSProp ", RMSProp( 0.02f ), 30 )
        NnTest.runOptimizer( "Adagrad ", Adagrad( 0.05f ), 30 )
    }

    # ── 3. 学习率调度 ────────────────────────────────────
    static schedulerTest()
    {
        global.println( "===== schedulerTest =====" )
        Optimizer opt = SGD( 0.1f )

        LRScheduler cos = CosineLR( opt, 10 )
        for i = 0, i < 4, i++
        {
            cos.step()
            global.println( "cosine step " + i.toString() + " lr = " + SystemConvertString( opt.lr ) )
        }

        Optimizer opt2 = SGD( 0.1f )
        LRScheduler warm = WarmupLR( opt2, 5 )
        for i = 0, i < 6, i++
        {
            warm.step()
            global.println( "warmup step " + i.toString() + " lr = " + SystemConvertString( opt2.lr ) )
        }

        Optimizer opt3 = SGD( 0.1f )
        LRScheduler step = StepLR( opt3, 3, 0.5f )
        for i = 0, i < 7, i++
        {
            step.step()
        }
        global.println( "steplr after 7 steps lr = " + SystemConvertString( opt3.lr ) )
    }

    # ── 4. 循环网络 ──────────────────────────────────────
    static rnnTest()
    {
        global.println( "===== rnnTest =====" )
        Tensor seq = Tensor.random( Shape.matrix( 5, 3 ) )

        RNN rnn = RNN( RNNCell( 3, 4 ) )
        Tensor out = rnn.forward( seq )
        check( "rnn out == (5,4)", out.rows() == 5 && out.cols() == 4 )

        LSTMCell lstm = LSTMCell( 3, 4 )
        Tensor h = null
        for t = 0, t < 5, t++
        {
            h = lstm.forward( seq.row( t ) )
        }
        check( "lstm h == (1,4)", h.rows() == 1 && h.cols() == 4 )

        GRUCell gru = GRUCell( 3, 4 )
        for t = 0, t < 5, t++
        {
            h = gru.forward( seq.row( t ) )
        }
        check( "gru h == (1,4)", h.rows() == 1 && h.cols() == 4 )
    }

    # ── 5. Embedding + Transformer ──────────────────────
    static transformerTest()
    {
        global.println( "===== transformerTest =====" )

        Array<Int32> ids = [ 1, 2, 3, 4 ]
        Tensor idTensor = Tensor( Shape.matrix( 4, 1 ) )
        for i = 0, i < 4, i++
        {
            idTensor.set( i, 0, SystemConvertFloat32( ids[i] ) )
        }

        Embedding emb = Embedding( 50, 16 )
        Tensor e = emb.forward( idTensor )
        check( "embedding == (4,16)", e.rows() == 4 && e.cols() == 16 )

        PositionalEncoding pe = PositionalEncoding( 16, 16 )
        Tensor x = pe.forward( e )

        # 单头：因果掩码的 scaled dot-product
        Tensor mask = Attention.causalMask( 4 )
        Tensor attn1 = Attention.scaledDotProduct( x, x, x, mask )
        check( "causal attn == (4,16)", attn1.rows() == 4 && attn1.cols() == 16 )

        MultiHeadAttention mha = MultiHeadAttention( 16, 4 )
        Tensor attn = mha.forward( x )
        check( "mha == (4,16)", attn.rows() == 4 && attn.cols() == 16 )

        TransformerBlock block = TransformerBlock( 16, 4 )
        Tensor bo = block.forward( x )
        check( "block == (4,16)", bo.rows() == 4 && bo.cols() == 16 )

        Transformer tf = Transformer( 16, 4, 2 )
        Tensor to = tf.forward( x )
        check( "transformer == (4,16)", to.rows() == 4 && to.cols() == 16 )
        global.println( tf.summary() )
    }

    static fun()
    {
        global.println( "========== NnTest (start) ==========" )
        mlpTrainTest()
        optimizerCompareTest()
        schedulerTest()
        rnnTest()
        transformerTest()
        global.println( "========== NnTest (end) ==========" )
    }
}
