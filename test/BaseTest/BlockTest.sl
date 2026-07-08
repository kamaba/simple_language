

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
            {
                b = 20;
                {
                    a = 15;
                    a2 = 13;
                    {
                        m = 10;
                    }
                }
                global.println( "a2 = " + b );
            }
        }

        # 有效用例1：多层块级作用域 + 同名变量遮蔽
        static scopeShadowCase()
        {
            int a = 1;
            {
                a = 2;
                {
                    a = 3;
                    global.println("inner a = " + a);
                }
                global.println("middle a = " + a);
            }
            global.println("outer a = " + a);
        }

        # 有效用例2：空块、独立块、条件块
        static emptyAndStandaloneBlockCase()
        {
            { }

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

        # 有效用例3：初始化器里的 {} 与语句块 {} 混合
        static initAndStatementMixedCase()
        {
            Class2 c = { m = 88 };
            {
                c.m = c.m + 1;
                global.println("c.m = " + c.m);
            }
        }

        # 错误用例（用于语法/语义校验，默认注释避免影响正常回归）
        # 打开任一段后应触发对应错误

        # 错误用例1：缺少右花括号
        static err_missing_right_brace()
        {
            int x = 10;
            {
                int y = 20;
            }
        }

        # 错误用例2：块外访问块内变量
        #static err_out_of_scope_access()
        #{
        #     {
        #         int inner = 1;
        #     }
        #     global.println(inner);
        # }

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
    }
}