ForTest
{   
    # forfun 作为入口，统一调用所有分类测试
    static fun()
    {
        ForTest.forInMixedArray()
        ForTest.forInEnumBridgeKind()
        ForTest.forInRange()
        ForTest.forInNestedRange()
        ForTest.forInitOnly()
        ForTest.forConditionOnly()
        ForTest.forFullThreePart()
        ForTest.forInArrayLiteral()
        ForTest.forInRangeLiteral()
        ForTest.forInNestedArray()
        ForTest.forInFlow()
        ForTest.forInNest()
        ForTest.forenum()
        forInterface()
    }

    # for-in 遍历混合类型数组
    static forInMixedArray()
    {
        global.println("---------------forInMixedArray--------------")
        a = [1981,"mmmmm", 0xef, 33333L,[1988,2045]]
        for v in a
        {
            global.println("------------------$v.toString() ")
        }
    }

    # for-in 遍历枚举 BridgeKind（含 break/continue）
    static forInEnumBridgeKind()
    {
        global.println("---------------forInEnumBridgeKind--------------")
        for v in BridgeKind
        {
            if v == BridgeKind.SELF 
            {
                global.println( "BridgeKind--------------SELF " )
                continue
            }
            elif v == BridgeKind.JVM
            {
                global.println( "BridgeKind--------------JVM " )
                break
            }
            global.println( "BridgeKind= $v.name.toString() value = $v.value.toString()  " )
            global.println(v)
        }
    }

    # for-in 遍历 Range（1..100 快捷写法 + range() 函数）
    static forInRange()
    {
        global.println("---------------forInRange--------------")
        r1 = 1..100;    #快速int range  相当于 range( 1, 100, 1 )的调用        
        r2 = range( 1.0f, 200.0f, 1.0f );
        Range<double> r3 = new(3.2d, 54.3d, 0.22d );
        r4 = Range<short>( 1s, 100s, 2s );

        for v in r1  
        {
            global.println("value=$v");
        } 
        for v in r2  
        {
            global.println("value=$v");
        } 
        for v in r3  
        {
            global.println("value=$v");
        } 
        for v in r4 
        {
            global.println("value=$v");
        }
    }

    # for-in 嵌套 range 循环
    static forInNestedRange()
    {
        global.println("---------------forInNestedRange--------------")
        sum = 0
        for i in range(1,10, 2 )
        {
            sum += 10
            global.println( i )
            for n in range( 3, 6 )
            {
               sum += n
               global.println( "n=$n" )
            }
        }
        global.println("sum:"  + sum)
    }

    # 文档1.1.2: for i = 0{} 只赋值，内部变更
    static forInitOnly()
    {
        global.println("---------------forInitOnly--------------")
        i = 20
        for i = 1
        {
            if i > 22
            {
                break
            }
            i = i+2
            global.println("for i= $i ")
        }
        for i = 123
        {
            if i >= 130
            {
                break
            }
            i++
            global.println("i= $i ")
        }

        # 文档1.1.2 补充: for f1 = 0 只赋值，内部变更
        for f1 = 0
        {
            if f1 >= 5 { break }
            f1 = f1 + 1
            global.println("1.1.2 for-init-only f1=$f1")
        }
    }

    # 文档1.1.3: for i = 0, i < N{} 条件限制，内部更新
    static forConditionOnly()
    {
        global.println("---------------forConditionOnly--------------")
        for i = 0, i < 10
        {
            global.println("i= $i ")
            i++            
        }

        # 补充用例
        for f2 = 0, f2 < 3
        {
            global.println("1.1.3 for-cond f2=$f2")
            f2 = f2 + 1
        }
    }

    # 文档1.1.4: for i = 0, i < N, step 完整三段式 + continue/break
    static forFullThreePart()
    {
        global.println("---------------forFullThreePart--------------")        
        for i = 0, i <= 2, i+=2
        {            
            global.println("i= $i ");
            n = i * 10;
        }        
        
        for i = 0, i < 30, i++
        {
            global.println("i= $i ");
            n = i * 10;
            if n == 200{ break }

            if n % 2 == 0 {global.println("这是一个偶数 = $i ");continue }
        }        

        # 补充: 完整三段式
        for f3 = 0, f3 < 3, f3 += 1
        {
            global.println("1.1.4 for-full f3=$f3")
        }

        # 补充: continue/break 在条件遍历中
        for f4 = 0, f4 < 10, f4 += 1
        {
            if f4 == 2 { continue }
            if f4 == 5 { break }
            global.println("1.1.4 for-flow f4=$f4")
        }
    }

    # 文档1.2: for-in 数组字面量/变量直接遍历
    static forInArrayLiteral()
    {       
        global.println("---------------forInArrayLiteral--------------")     
        arr = [1,2,3];
        for v in arr
        {
            global.println(" v= $v ")
        }
        # 补充: 数组字面量直接遍历
        for v in [10, 20, 30]
        {
            global.println("1.2 for-in-literal v=$v")
        }
    }

    # for-in [1..4] range 字面量遍历
    static forInRangeLiteral()
    {       
        global.println("---------------forInRangeLiteral--------------")      
        for v in [1..4]   
        {
            global.println(" v= $v ")
        }
    }

    # for-in 嵌套数组遍历
    static forInNestedArray()
    {
        global.println("---------------forInNestedArray--------------")    
        a1 = [[1,2,3],[4,5,6],[7,8,9]]
        for v in a1
        {
            global.println("这里是索引" + v.index )
            for i = 0, i < v.length, i++
            {
                global.println("这里是值" + v.$i + "----" + v[i] )
            }
        }
    }

    # 文档1.2: for-in break/continue 控制流
    static forInFlow()
    {
        global.println("---------------forInFlow--------------")   
        for v in [1, 2, 3, 4, 5]
        {
            if v == 3 { continue }
            if v ==4 { break }
            global.println("1.2 for-in-flow v=$v")
        }

        # 空数组 [] 类型推断暂不支持，跳过此用例
        #global.println("1.2 for-in-empty skipped")
    }

    # 文档1.2: for-in 嵌套循环
    static forInNest()
    {
        global.println("---------------forInNest--------------")   
        for v in [1, 2]
        {
            for w in [10, 20]
            {
                global.println("1.2 for-in-nest v=$v w=$w")
            }
        }
    }

    enum EItType
    {
        It1 = 1
        It2 = 2
    }
    static forenum()
    {
        for v in EItType
        {
            global.println( "forenum:" + v.toString() )
        }
    }
    
    interface IPay
    {
        pay( int a )
        check()
    }
    public class Pay interface IPay
    {
        _paycash = 0
        override pay( int a ){
            this._paycash = a

            global.println("pay:" + a.toString() )
        }
        override check()
        {
            global.println("check:" + this._paycash.toString() )
        }
    }
    static forInterface()
    {
        global.println("---------------forInterface--------------")   
        IPay pay1 = Pay()
        pay1.check()
        pay1.pay(20)
    }    

    # 暂不支持的用例（块注释保留）
    #!
    # 暂不支持 List,Map,Set,Queue,Link
    # int i2 = 0;
    # List list = { 1, 2, 3};        
    # for it in list
    # {
    # }
    # 暂不支持Array<int>
    # Array<int> arr = [1,2,3];
    # for v in arr{
    #     global.println(" v= $v ")
    # }
    # 暂不支持 Array<object>
    # Array b = [{a=1}, {a=2}, {a = 3} ];        
    # for v in b{
    # }
    # 暂不支持 for in range
    # for v in EItType
    # {
    # }
    !#

    # --- 文档1.1.1: for{} 无条件循环 —— 当前不支持(解析器要求 for 后至少一个语句) ---
    # --- 文档1.2.1: x.index 索引访问 —— 当前未实现(indexVariable 已注释) ---
    # --- 文档1.4: for x in Map<T1,T2> —— 暂不支持 ---
}

#for关键字的的规则
#1. for的使用对于 一个目标的变量的循环遍历  

#1.1 for i = 0, i < 10, i++ 是使用for的 条件遍历法， 一般是 第一位是 数字赋值， 第二位是条件遍历  第三位是 变量更新 
#1.1.1 不写任何 即for{} 这种写法，可以在里边进行break  
#1.1.2 只写for i = 0{} 即，把第一个变量进行赋值，在里这进行变更 
#1.1.3 只写for i =0, i < 10 { }  进行条件限制 变更更新在内部
#1.1.4 写全的 for i = 0, i < 10, i++ 即 数字赋值，条件遍历  变量更新

#1.2 for x in content 是对某个可以iterate的进行遍历 ，如果是iterate进行遍历 
#1.2.1 x是遍历的迭代变量 未来要支持 x.index 即当前遍历的索引  x.value 当前值的读取，如果x.value 即使值为空，也可以告是空值 
#1.2.2 在遍历的时候， content 会把进行对x.index 进行赋值 
#1.2.3 x 如果直接读取，也是可以访问的，即 相当于 x.value 的方法

#1.3 for x in Enum 是对enum的遍历 ， 会把enum 里边的 staticMemberVariableArray[] 然后进行遍历 所以enum在构建的时候，会把当前的变量都存在这个数组里边

#1.4 for x in Map<T1,T2> 一般会在x中，直接可以读取 x.key, x.value T1 x.key    T2 x.value
