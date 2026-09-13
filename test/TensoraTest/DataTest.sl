import Tensora

# DataTest —— 数据管道 / 分词 / 评估指标 / 设备 的小例子

DataTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[DataTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[DataTest] " + name + " : FAIL" )
        }
    }

    static Tensor makeX( int rows, int cols )
    {
        Rng rng = Rng( 99 )
        Tensor x = Tensor( Shape.matrix( rows, cols ) )
        for i = 0, i < rows, i++
        {
            for j = 0, j < cols, j++
            {
                x.set( i, j, rng.uniform( 0.0f, 1.0f ) )
            }
        }
        ret x
    }

    # ── 1. Dataset ───────────────────────────────────────
    static datasetTest()
    {
        global.println( "===== datasetTest =====" )
        Array<Int32> labels = [ 0, 0, 1, 1, 2, 2 ]
        Tensor x = DataTest.makeX( 6, 3 )
        Tensor y = Ops.oneHot( labels, 3 )
        Dataset ds = Dataset( x, y, "toy" )

        global.println( ds.toString() )
        check( "count == 6", ds.count() == 6 )
        check( "featureCount == 3", ds.featureCount() == 3 )
        check( "classCount == 3", ds.classCount() == 3 )
        check( "labelAt(4) == 2", ds.labelAt( 4 ) == 2 )

        Dataset tr = ds.trainPart( 0.5f )
        Dataset va = ds.valPart( 0.5f )
        check( "split 3/3", tr.count() == 3 && va.count() == 3 )

        ds.shuffle()
        global.println( "shuffled ok" )
    }

    # ── 2. DataLoader ───────────────────────────────────
    static dataLoaderTest()
    {
        global.println( "===== dataLoaderTest =====" )
        Array<Int32> labels = [ 0, 0, 1, 1, 2, 2 ]
        Dataset ds = Dataset( DataTest.makeX( 6, 3 ), Ops.oneHot( labels, 3 ), "toy" )

        DataLoader dl = DataLoader( ds, 4, false, false )
        check( "batchCount == 2", dl.batchCount() == 2 )
        dl.reset()
        int nb = 0
        while dl.hasNext()
        {
            Batch b = dl.next()
            global.println( "batch x=" + b.x.shape().toString() + " size=" + b.size.toString() )
            nb++
        }
        check( "iterated 2 batches", nb == 2 )

        DataLoader dl2 = DataLoader( ds, 4, false, true )
        check( "dropLast batchCount == 1", dl2.batchCount() == 1 )
    }

    # ── 3. Normalizer ───────────────────────────────────
    static normalizerTest()
    {
        global.println( "===== normalizerTest =====" )
        Tensor x = DataTest.makeX( 8, 2 )
        Normalizer nz = Normalizer()
        nz.fit( x )
        Tensor z = nz.transform( x )
        global.println( "mean = " + nz.mean.toString() )
        global.println( "std  = " + nz.std.toString() )
        check( "standardized mean ~= 0", TensorMath.abs( z.sumAlong( 0 ).get( 0, 0 ) ) < 0.001f )

        Tensor back = nz.inverse( z )
        check( "inverse ~= origin", TensorMath.abs( back.get( 0, 0 ) - x.get( 0, 0 ) ) < 0.001f )
    }

    # ── 4. Tokenizer ────────────────────────────────────
    static tokenizerTest()
    {
        global.println( "===== tokenizerTest =====" )
        Array<string> corpus = [ "hello world", "hello tensora", "world of ai" ]

        Tokenizer tk = Tokenizer()
        tk.build( corpus, 1 )
        global.println( "vocab size = " + tk.vocabSize().toString() )

        Array<Int32> ids = tk.encode( "hello world" )
        check( "encode len == 2", ids.length == 2 )
        global.println( "decode -> " + tk.decode( ids ) )

        Array<Int32> padded = tk.pad( ids, 6 )
        check( "padded len == 6", padded.length == 6 )
        tk.maxLen = 6
        global.println( "encodePadded len = " + tk.encodePadded( "hello ai" ).length.toString() )

        Tokenizer ct = Tokenizer()
        ct.buildChars( corpus )
        Array<Int32> cids = ct.encode( "ai" )
        check( "char encode len == 2", cids.length == 2 )
        global.println( "char decode -> " + ct.decode( cids ) )
    }

    # ── 5. Metrics ──────────────────────────────────────
    static metricsTest()
    {
        global.println( "===== metricsTest =====" )
        Tensor logits = Tensor.matrix( [ 2.0f, 1.0f, 0.0f, 0.0f, 3.0f, 0.0f, 1.0f, 1.0f, 5.0f, 0.0f, 2.0f, 1.0f ], 4, 3 )
        Array<Int32> labels = [ 0, 1, 2, 1 ]

        global.println( "accuracy = " + SystemConvertString( Metrics.accuracy( logits, labels ) ) )
        global.println( "top2     = " + SystemConvertString( Metrics.topKAccuracy( logits, labels, 2 ) ) )

        Array<Int32> pred = Metrics.predictLabels( logits )
        global.println( "precision(c1) = " + SystemConvertString( Metrics.precision( pred, labels, 1 ) ) )
        global.println( "recall(c1)    = " + SystemConvertString( Metrics.recall( pred, labels, 1 ) ) )
        global.println( "f1(c1)        = " + SystemConvertString( Metrics.f1( pred, labels, 1 ) ) )
        global.println( "macroF1       = " + SystemConvertString( Metrics.macroF1( pred, labels, 3 ) ) )

        Tensor cm = Metrics.confusionMatrix( pred, labels, 3 )
        check( "confusion diagonal == 4", cm.get( 0, 0 ) + cm.get( 1, 1 ) + cm.get( 2, 2 ) == 4.0f )

        Tensor p = Tensor.vector( [ 1.0f, 2.0f, 3.0f ] )
        Tensor t = Tensor.vector( [ 1.1f, 1.9f, 3.2f ] )
        global.println( "r2   = " + SystemConvertString( Metrics.r2( p, t ) ) )
        global.println( "rmse = " + SystemConvertString( Metrics.rmse( p, t ) ) )
        global.println( "ppl  = " + SystemConvertString( Metrics.perplexity( 2.0f ) ) )
    }

    # ── 6. Device ───────────────────────────────────────
    static deviceTest()
    {
        global.println( "===== deviceTest =====" )
        Device cpu = Device.cpu()
        global.println( cpu.toString() + " isCpu=" + cpu.isCpu().toString() )
        Device gpu = Device.gpu( 0 )
        global.println( gpu.toString() )
        gpu.alloc( 1024 )
        gpu.free( 512 )
        global.println( "gpu bytesUsed = " + SystemConvertString( gpu.bytesUsed ) )
        check( "cpu is cpu", cpu.isCpu() )
    }

    static fun()
    {
        global.println( "========== DataTest (start) ==========" )
        datasetTest()
        dataLoaderTest()
        normalizerTest()
        tokenizerTest()
        metricsTest()
        deviceTest()
        global.println( "========== DataTest (end) ==========" )
    }
}
