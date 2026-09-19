typealias CalcFunc = int Function( int, int )

namespace NSClosureTest
{
    ClosureTest
    {
        static int s_counter = 0;

        # 案例1: 具名闭包 + 捕获宿主局部变量
        static captureCase()
        {
            int baseVal = 100;
            function addBase( int a, int b )
            {
                ret a + b + baseVal;
            }
            global.println( "addBase(1,2) = " + addBase(1, 2).toString() );
        }

        # 案例2: 匿名闭包
        static anonymousCase()
        {
            var mul = function( int a, int b )
            {
                ret a * b;
            };
            global.println( "mul(3,4) = " + mul(3, 4).toString() );
        }

        # 案例3: 闭包体内修改捕获变量
        static writeCaptureCase()
        {
            int count = 0;
            function inc( int step )
            {
                count = count + step;
                ret count;
            }
            inc( 5 );
            inc( 7 );
            global.println( "count = " + count.toString() );

            Array<int> counts = [0];

            # 闭包捕获 counts 数组, 合并传入的 arr 并返回
            function Array<int> getCounts( Array<int> arr )
            {
                int totalLen = counts.length + arr.length;
                Array<int> result = Array<int>( totalLen );
                int idx = 0;
                for i = 0, i < counts.length, i++
                {
                    result._setItem_( idx, counts._getItem_( i ) );
                    idx = idx + 1;
                }
                for j = 0, j < arr.length, j++
                {
                    result._setItem_( idx, arr._getItem_( j ) );
                    idx = idx + 1;
                }
                ret result;
            }

            Array<int> merged = getCounts( [10, 20] );
            global.println( "merged = " + merged.toString() );
        }

        # 案例4: 闭包关联类成员函数 (闭包内调用宿主类的静态方法)
        static int helperAdd( int x, int y )
        {
            ret x + y;
        }

        static memberFunctionRelation()
        {
            function caller( int a, int b )
            {
                ret helperAdd( a, b );
            }
            global.println( "caller(10,20) = " + caller(10, 20).toString() );
        }

        # 案例5: 闭包关联类成员变量 (闭包内读写宿主类的静态成员变量)
        static memberVariableRelation()
        {
            function incCounter( int step )
            {
                s_counter = s_counter + step;
                ret s_counter;
            }
            incCounter( 10 );
            incCounter( 5 );
            global.println( "s_counter = " + s_counter.toString() );
        }

        # 案例6: 闭包作为变量 (定义后多次调用)
        static variableAbout()
        {
            var doubler = function( int x )
            {
                ret x * 2;
            };
            Array<int> nums = [1, 2, 3];
            for i = 0, i < nums.length, i++
            {
                int val = nums._getItem_( i );
                global.println( "doubler(" + val.toString() + ") = " + doubler( val ).toString() );
            }
        }

        # 案例7: 函数返回闭包 (makeCounter 模式)
        #   闭包捕获宿主局部变量 count, 函数返回后 count 仍然存活
        static Function makeCounter()
        {
            int count = 0;
            function counter()
            {
                count = count + 1;
                ret count;
            }
            ret counter;
        }

        static returnClosureFunction()
        {
            var c = makeCounter();
            global.println( "c() = " + c().toString() );
            global.println( "c() = " + c().toString() );
            global.println( "c() = " + c().toString() );
        }

        # 案例8: forEach + 闭包回调
        static forEachCase()
        {
            Array<int> arr = [1, 2, 3, 4, 5];
            var printer = function( int i )
            {
                global.println( "i = " + i.toString() );
            };
            arr.forEach( printer );
        }

        # 案例9: 闭包返回类型推断
        #   无显式返回类型的闭包, 默认 Void; 有 ret 语句时自动推断为返回值类型
        static returnTypeInferenceCase()
        {
            # ret int -> 推断为 int
            function adder( int a, int b )
            {
                ret a + b;
            }
            global.println( "adder(3,4) = " + adder(3, 4).toString() );

            # ret string -> 推断为 string
            function greeter( string name )
            {
                ret "hello, " + name;
            }
            global.println( "greeter = " + greeter("world") );

            # 无 ret -> 返回类型保持 Void
            function printer( int x )
            {
                global.println( "printer: " + x.toString() );
            }
            printer( 42 );
        }

        # 案例10: typealias 函数类型
        #   typealias CalcFunc = int Function( int, int ) 定义在文件级
        #   验证函数类型别名可被解析, 并可作为返回类型使用
        static CalcFunc makeCalc()
        {
            var f = function( int a, int b )
            {
                ret a * b;
            };
            ret f;
        }

        static typealiasFuncCase()
        {
            var adder = function( int a, int b )
            {
                ret a + b;
            };
            global.println( "adder(5,6) = " + adder(5, 6).toString() );

            var calc = makeCalc();
            global.println( "calc(3,7) = " + calc(3, 7).toString() );
        }

        # 案例11: this 在闭包中使用 (实例方法 + 实例成员)
        int m_value = 0;

        thisInClosureCase()
        {
            this.m_value = 100;
            # 闭包通过 this 访问实例成员
            function int getValue()
            {
                ret this.m_value;
            }
            function setValue( int v )
            {
                this.m_value = v;
            }
            global.println( "getValue() = " + getValue().toString() );
            setValue( 200 );
            global.println( "after setValue(200), getValue() = " + getValue().toString() );
        }

        # 综合测试: 依次调用所有案例
        static fun()
        {
            captureCase();
            anonymousCase();
            writeCaptureCase();
            memberFunctionRelation();
            memberVariableRelation();
            variableAbout();
            returnClosureFunction();
            forEachCase();
            returnTypeInferenceCase();
            typealiasFuncCase();
            thisInClosureTest();
        }

        # thisInClosureCase 是实例方法, 需通过实例调用
        static thisInClosureTest()
        {
            var test = ClosureTest()
            test.thisInClosureCase()
        }
    }
}
