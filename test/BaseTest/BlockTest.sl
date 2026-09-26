

namespace NSBlockTest
{
    Class2
    {
        static int m2 = 10;
        m = 10;
        _init_( int x )
        {
            this.m = 10;
        }
    }
    Class1
    {
        Class2 c3 = { m = 20 };
        print()
        {
            int a = 10;
            label _
            {
                b = 20;
                label innerPrint
                {
                    a = 15;
                    a2 = 13;
                    label _
                    {
                        m = 10;
                    }
                }
                global.println( "a2 = " + b );
            }
        }

        # 有效用例1：多层块级作用域 + 同名变量遮蔽（label 命名块）
        static scopeShadowCase()
        {
            int a = 1;
            label middleBlock
            {
                a = 2;
                label innerBlock
                {
                    a = 3;
                    global.println("inner a = " + a);
                }
                global.println("middle a = " + a);
            }
            global.println("outer a = " + a);
        }

        # 有效用例2：空块、独立块（label _ 自动命名）、条件块
        static emptyAndStandaloneBlockCase()
        {
            label empty { }

            label _
            {
                int onlyInBlock = 100;
                global.println("onlyInBlock = " + onlyInBlock);
            }

            if true
            {
                int inIf = 200;
                global.println("inIf = " + inIf);
            }
        }

        # 有效用例3：初始化器里的 {}（不受影响）与 label 命名块 {} 混合
        static initAndStatementMixedCase()
        {
            Class2 c = { m = 88 };
            label mixBlock
            {
                c.m = c.m + 1;
                global.println("c.m = " + c.m);
            }
        }

        # 有效用例4：goto 跳转到 label 命名块（label Name{} 的标签可被 goto 使用）
        static gotoIntoLabelBlockCase()
        {
            int a = 0
            goto targetBlock
            a = 999
            label targetBlock
            {
                a += 1
            }
            global.println("gotoIntoLabelBlock a -> " + a.toString())   # 1
        }

        # 错误用例（用于语法/语义校验，默认注释避免影响正常回归）
        # 打开任一段后应触发对应错误

        # 错误用例1：函数内裸块 {}（应触发 BlockMustHaveLabel 编译错误）
        #static err_bare_block()
        #{
        #    int x = 10;
        #    {
        #        int y = 20;
        #    }
        #}

        # 错误用例2：块外访问块内变量
        #static err_out_of_scope_access()
        #{
        #    label _
        #    {
        #        int inner = 1;
        #    }
        #    global.println(inner);
        #}

        # 错误用例3：初始化器缺少右花括号
        #static err_bad_initializer()
        # {
        #     Class2 c = { m = 1;
        # }
    }
}

BlockTest
{
    static fun()
    {
        NSBlockTest.Class1.scopeShadowCase();
        NSBlockTest.Class1.emptyAndStandaloneBlockCase();
        NSBlockTest.Class1.initAndStatementMixedCase();
        NSBlockTest.Class1.gotoIntoLabelBlockCase();
    }
}
