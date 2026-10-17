import Tensora

# TransformerModelTest —— BERT 风格 Transformer 编码器分类模型
#
#   shapeTest     ：Embedding + 位置编码 + Block 堆叠 + 池化 + 分类头，输出 [1, C]
#   poolTest      ：SeqPool 的 Mean / Cls 两种池化及其梯度形状
#   trainTest     ：几条可分的短序列跑 fit，观察 loss 与准确率
#   inferenceTest ：predictProba / predictLabels

TransformerModelTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[TransformerModelTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[TransformerModelTest] " + name + " : FAIL" )
        }
    }

    # 造一条 token id 序列，形状 [T, 1]
    static Tensor makeSeq( Array<Int32> ids )
    {
        Shape sp = Shape.matrix( ids.length, 1 )
        Tensor t = Tensor( sp )
        for i = 0, i < ids.length, i++
        {
            t.set( i, 0, SystemConvertFloat32( ids[i] ) )
        }
        ret t
    }

    # ── 1. 形状与参数 ────────────────────────────────────
    static shapeTest()
    {
        global.println( "===== shapeTest =====" )
        TransformerClassifier model = TransformerClassifier( 50, 3, 16, 4, 2, 16, EPoolKind.Mean, 0.1f )
        Tensor seq = TransformerModelTest.makeSeq( [ 1, 2, 3, 4 ] )
        Tensor logits = model.forward( seq )
        check( "logits == (1,3)", logits.rows() == 1 && logits.cols() == 3 )
        check( "params > 0", model.totalParams() > 0 )
        global.println( model.summary() )
    }

    # ── 2. 序列池化 ──────────────────────────────────────
    static poolTest()
    {
        global.println( "===== poolTest =====" )
        Shape sp = Shape.matrix( 4, 8 )
        Tensor x = Tensor.random( sp )

        SeqPool meanPool = SeqPool( EPoolKind.Mean )
        Tensor m = meanPool.forward( x )
        check( "mean pool == (1,8)", m.rows() == 1 && m.cols() == 8 )
        Tensor gm = meanPool.backward( m )
        check( "mean pool grad == (4,8)", gm.rows() == 4 && gm.cols() == 8 )

        SeqPool clsPool = SeqPool( EPoolKind.Cls )
        Tensor c = clsPool.forward( x )
        check( "cls pool == (1,8)", c.rows() == 1 && c.cols() == 8 )
        Tensor gc = clsPool.backward( c )
        check( "cls pool grad == (4,8)", gc.rows() == 4 && gc.cols() == 8 )
    }

    # ── 3. 训练 ──────────────────────────────────────────
    static trainTest()
    {
        global.println( "===== trainTest =====" )
        Array<Tensor> seqs = Array<Tensor>( 4 )
        seqs[0] = TransformerModelTest.makeSeq( [ 1, 2, 3 ] )
        seqs[1] = TransformerModelTest.makeSeq( [ 1, 2, 4 ] )
        seqs[2] = TransformerModelTest.makeSeq( [ 7, 8, 9 ] )
        seqs[3] = TransformerModelTest.makeSeq( [ 7, 8, 10 ] )
        Array<Int32> labels = [ 0, 0, 1, 1 ]

        TransformerClassifier model = TransformerClassifier( 20, 2, 16, 2, 1, 16, EPoolKind.Mean, 0.0f )
        Optimizer opt = Adam( 0.01f )
        Float32 loss0 = model.fitOnce( seqs, labels, opt )
        model.fit( seqs, labels, opt, 40, 20 )
        Float32 acc = model.evaluate( seqs, labels )
        global.println( "first loss = " + SystemConvertString( loss0 ) )
        global.println( "train acc = " + SystemConvertString( acc ) )
        check( "acc >= 0.5", acc >= 0.5f )
    }

    # ── 4. 推理 ──────────────────────────────────────────
    static inferenceTest()
    {
        global.println( "===== inferenceTest =====" )
        TransformerClassifier model = TransformerClassifier( 20, 2, 16, 2, 1, 16, EPoolKind.Cls, 0.0f )
        Array<Tensor> seqs = Array<Tensor>( 2 )
        seqs[0] = TransformerModelTest.makeSeq( [ 1, 2, 3 ] )
        seqs[1] = TransformerModelTest.makeSeq( [ 4, 5, 6 ] )

        Tensor proba = model.predictProba( seqs[0] )
        Tensor colSum = proba.sumAlong( 1 )
        check( "proba sum ~ 1", colSum.get( 0, 0 ) > 0.99f )

        Array<Int32> pred = model.predictLabels( seqs )
        check( "predict count == 2", pred.length == 2 )
    }

    static fun()
    {
        global.println( "========== TransformerModelTest (start) ==========" )
        shapeTest()
        poolTest()
        trainTest()
        inferenceTest()
        global.println( "========== TransformerModelTest (end) ==========" )
    }
}
