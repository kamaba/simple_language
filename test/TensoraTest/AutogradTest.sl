import Tensora

# AutogradTest —— 自动微分：标量链、矩阵链、广播回传
AutogradTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[AutogradTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[AutogradTest] " + name + " : FAIL" )
        }
    }

    # y = (a * b + c) ^ 2  ->  da = 2(a*b+c)*b
    static scalarTest()
    {
        global.println( "===== scalarTest =====" )
        Variable a = Variable.parameter( Tensor.scalar( 2.0f ) )
        Variable b = Variable.parameter( Tensor.scalar( 3.0f ) )
        Variable c = Variable.parameter( Tensor.scalar( 4.0f ) )

        Variable y = a.mul( b ).add( c ).pow( 2.0f )
        y.backward()

        # a*b+c = 10 -> y = 100 ; da = 2*10*3 = 60 ; db = 2*10*2 = 40 ; dc = 2*10 = 20
        check( "y==100", y.data.at( 0 ) == 100.0f )
        check( "da==60", a.grad.at( 0 ) == 60.0f )
        check( "db==40", b.grad.at( 0 ) == 40.0f )
        check( "dc==20", c.grad.at( 0 ) == 20.0f )
    }

    # Y = relu(X · W) 求和
    static matmulTest()
    {
        global.println( "===== matmulTest =====" )
        Variable x = Variable.parameter( Tensor.matrix( [ 1.0f, 2.0f, 3.0f, 4.0f ], 2, 2 ) )
        Variable w = Variable.parameter( Tensor.matrix( [ 1.0f, 0.0f, 0.0f, 1.0f ], 2, 2 ) )

        Variable y = x.matmul( w ).relu().sum()
        y.backward()

        # X·W == X，relu 全通，sum = 1+2+3+4 = 10
        check( "y==10", y.data.at( 0 ) == 10.0f )
        check( "dx all==1", x.grad.at( 0 ) == 1.0f && x.grad.at( 3 ) == 1.0f )
        # dW = Xᵀ · ones = [4,6 ; 4,6]
        check( "dw[0,0]==4", w.grad.get( 0, 0 ) == 4.0f )
        check( "dw[1,1]==6", w.grad.get( 1, 1 ) == 6.0f )
    }

    # 广播回传：把 [2,2] 的梯度还原到 [1,2] 的偏置上
    static broadcastTest()
    {
        global.println( "===== broadcastTest =====" )
        Variable x = Variable.parameter( Tensor.matrix( [ 1.0f, 1.0f, 1.0f, 1.0f ], 2, 2 ) )
        Variable bias = Variable.parameter( Tensor.matrix( [ 1.0f, 2.0f ], 1, 2 ) )

        Variable y = x.mul( bias ).sum()
        y.backward()

        # y = (1*1 + 1*2) * 2 行 = 6 ；dbias 每行贡献 [1,1]，累计 2 行 -> [2,2]
        check( "y==6", y.data.at( 0 ) == 6.0f )
        check( "dbias[0,0]==2", bias.grad.get( 0, 0 ) == 2.0f )
        check( "dbias[0,1]==2", bias.grad.get( 0, 1 ) == 2.0f )
    }

    # softmax + log 的梯度：d logits = softmax - onehot
    static softmaxTest()
    {
        global.println( "===== softmaxTest =====" )
        Variable logits = Variable.parameter( Tensor.matrix( [ 1.0f, 2.0f, 3.0f ], 1, 3 ) )
        Variable p = logits.softmax()
        Variable y = p.log().scale( 1.0f )

        Array<Int32> labels = [ 2 ]
        Tensor g = Loss.crossEntropyGrad( logits.data, labels )
        # 手动把 g 灌进去走一遍反传（模拟训练里的 loss 梯度）
        Variable loss = logits.mul( Tensor.matrix( [ g.get( 0, 0 ), g.get( 0, 1 ), g.get( 0, 2 ) ], 1, 3 ) ).sum()
        loss.backward()

        global.println( "softmax -> " + p.data.toString() )
        check( "grad != null", logits.grad != null )
        check( "grad sum ~= 0", TensorMath.abs( logits.grad.sum() - g.sum() ) < 0.0001f )
    }

    static fun()
    {
        global.println( "========== AutogradTest (start) ==========" )
        scalarTest()
        matmulTest()
        broadcastTest()
        softmaxTest()
        global.println( "========== AutogradTest (end) ==========" )
    }
}
