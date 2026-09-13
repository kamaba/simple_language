import Tensora

# TensorTest —— 基础：Shape / Tensor 构造、索引、广播、matmul、归约、数学函数
TensorTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[TensorTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[TensorTest] " + name + " : FAIL" )
        }
    }

    # ── Shape ────────────────────────────────────────────
    static shapeTest()
    {
        global.println( "===== shapeTest =====" )
        Shape s = Shape( 2, 3, 4 )
        global.println( "rank=" + s.rank().toString() + " size=" + s.size().toString() )

        Array<Int32> st = s.strides()
        global.println( "strides=" + st[0].toString() + "," + st[1].toString() + "," + st[2].toString() )

        Array<Int32> idx = [ 1, 2, 3 ]
        int off = s.offset( idx )
        check( "offset[1,2,3]==23", off == 23 )

        Array<Int32> back = s.unflatten( 23 )
        check( "unflatten(23)==[1,2,3]", back[0] == 1 && back[1] == 2 && back[2] == 3 )

        Shape b = Shape.broadcastShape( Shape( 4, 1 ), Shape( 1, 3 ) )
        check( "broadcast(4,1)x(1,3)==(4,3)", b.dims[0] == 4 && b.dims[1] == 3 )
    }

    # ── 构造 / 索引 ──────────────────────────────────────
    static createTest()
    {
        global.println( "===== createTest =====" )
        Tensor a = Tensor.matrix( [ 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f ], 2, 3 )
        check( "a[0,2]==3", a.get( 0, 2 ) == 3.0f )
        check( "a[1,0]==4", a.get( 1, 0 ) == 4.0f )

        Tensor z = Tensor.zeros( Shape.matrix( 2, 2 ) )
        check( "zeros sum==0", z.sum() == 0.0f )

        Tensor one = Tensor.ones( Shape.vector( 5 ) )
        check( "ones sum==5", one.sum() == 5.0f )

        Tensor ident = Tensor.identity( 3 )
        check( "identity trace==3", ident.get( 0, 0 ) + ident.get( 1, 1 ) + ident.get( 2, 2 ) == 3.0f )

        Tensor rv = Tensor.random( Shape.matrix( 4, 4 ) )
        global.println( "random mean ~= " + SystemConvertString( rv.mean() ) )
    }

    # ── 逐元素 + 广播 ────────────────────────────────────
    static broadcastTest()
    {
        global.println( "===== broadcastTest =====" )
        Tensor a = Tensor.matrix( [ 1.0f, 2.0f, 3.0f, 4.0f ], 2, 2 )
        Tensor col = Tensor.matrix( [ 10.0f, 20.0f ], 2, 1 )

        Tensor r = a.add( col )
        check( "broadcast add [0,0]==11", r.get( 0, 0 ) == 11.0f )
        check( "broadcast add [1,1]==24", r.get( 1, 1 ) == 24.0f )

        Tensor s = a.scale( 2.0f )
        check( "scale sum==20", s.sum() == 20.0f )

        Tensor t = a.transpose()
        check( "transpose [0,1]==3", t.get( 0, 1 ) == 3.0f )
    }

    # ── 矩阵乘 ───────────────────────────────────────────
    static matmulTest()
    {
        global.println( "===== matmulTest =====" )
        Tensor a = Tensor.matrix( [ 1.0f, 2.0f, 3.0f, 4.0f ], 2, 2 )
        Tensor b = Tensor.matrix( [ 5.0f, 6.0f, 7.0f, 8.0f ], 2, 2 )
        Tensor c = a.matmul( b )
        # [1*5+2*7, 1*6+2*8] = [19, 22]
        check( "matmul [0,0]==19", c.get( 0, 0 ) == 19.0f )
        check( "matmul [0,1]==22", c.get( 0, 1 ) == 22.0f )
        check( "matmul [1,1]==50", c.get( 1, 1 ) == 50.0f )
    }

    # ── 归约 / 数学 ──────────────────────────────────────
    static reduceTest()
    {
        global.println( "===== reduceTest =====" )
        Tensor a = Tensor.matrix( [ 1.0f, 2.0f, 3.0f, 4.0f ], 2, 2 )
        check( "sum==10", a.sum() == 10.0f )
        check( "mean==2.5", a.mean() == 2.5f )
        check( "max==4", a.max() == 4.0f )
        check( "argmax==3", a.argmax() == 3 )

        Tensor colSum = a.sumAlong( 0 )
        check( "sumAlong(0)==[4,6]", colSum.get( 0, 0 ) == 4.0f && colSum.get( 0, 1 ) == 6.0f )

        Tensor rowSum = a.sumAlong( 1 )
        check( "sumAlong(1)==[3,7]", rowSum.get( 0, 0 ) == 3.0f && rowSum.get( 1, 0 ) == 7.0f )

        Tensor sm = Tensor.matrix( [ 1.0f, 2.0f, 3.0f, 4.0f ], 1, 4 ).softmax()
        global.println( "softmax sum ~= " + SystemConvertString( sm.sum() ) )
        check( "softmax argmax==3", sm.argmax() == 3 )
    }

    static mathTest()
    {
        global.println( "===== mathTest =====" )
        Tensor a = Tensor.vector( [ 1.0f, 4.0f, 9.0f ] )
        check( "sqrt [2]==3", a.sqrt().get( 2 ) == 3.0f )
        check( "relu of -1 == 0", TensorMath.relu( 0.0f - 1.0f ) == 0.0f )
        check( "sigmoid(0)==0.5", TensorMath.sigmoid( 0.0f ) == 0.5f )

        Tensor norm = Ops.normalize( Tensor.vector( [ 3.0f, 4.0f ] ) )
        global.println( "normalize -> " + norm.toString() )

        Tensor st = Ops.standardize( a, a.mean(), 1.0f )
        check( "standardize mean ~= 0", TensorMath.abs( st.mean() ) < 0.0001f )
    }

    static fun()
    {
        global.println( "========== TensorTest (start) ==========" )
        shapeTest()
        createTest()
        broadcastTest()
        matmulTest()
        reduceTest()
        mathTest()
        global.println( "========== TensorTest (end) ==========" )
    }
}
