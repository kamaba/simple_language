ArrayTest
{
    ArrClass
    {
        int i1 = 0;
        i2 = "aaa"
        
        _init_(int i1)
        {
            this.i1 = i1
        }

        _init_( int i1, string i2)
        {
            this.i1 = i1
            this.i2 = i2
        }

        override string toString()
        {
            ret "ArrClass(){ this.i1=" + this.i1.toString() + " this.i2= " +  this.i2.toString() + "}";
        }
    }
    Level<T>
    {
        T t = new()
        _init_()
        {

        }
        _init_( obj )
        {
            this.t = obj as T
        }
        override string toString()
        {
            ret "Level<T>(" + this.t.toString() + ")"
        }
    }

    # 统一：可迭代序列上的 for-in + println（合并多处相同模式）
    static forIIterator( obj )
    {
        global.println("==========forIIterator ==========")

        arr = obj as IIterable<object>
        for v in arr
        {
            if v != null
            {
                global.println("for-in -> " + v.toString() )
            }
            else
            {
                global.println("for-in -> null")
            }
        }
    }

    static forObject( arr )
    {
        global.println("==========forObject ==========")

        var iter = arr as ObjectArray
        if iter != null
        {
            for v in iter
            {
                if v != null
                {
                    global.println("1111111111= " + v.toString() )
                }
            }
        }
    }

    static arrayBasicApiTest()
    {
        
        global.println("========== Array.basic api ==========")
        Int32[] nums = Array<Int32>.create(3)
        nums.fill(7)
        nums.setValue(2, 99)
        int lenValue = nums.length
        lenValue += 1

        
        global.println("nums.length -> " + nums.length.toString())
        global.println("lenValue -> " + lenValue.toString())
        global.println("nums.getValue(0) -> " + nums.getValue(0).toString())
        global.println("nums.getValue(2) -> " + nums.getValue(2).toString())
        

        nums.index = 2
        global.println("nums.current() $ index=1 -> " + nums.current().toString())
        nums.current = 123
        global.println("nums.getValue(1) after set current -> " + nums.getValue(2).toString())
    }

    static arrayGenericElementTest()
    {
        global.println("========== Array.generic element ==========")
        ttt = Level<Level<int>>(){ t = Level<int>(120) }
        global.println("ttt -> " + ttt.toString())

        
        Level<Int32>[] levels = new(3) { Level<Int32>(10), Level<int>(){ t = 120 }, Level<Int32>(30) }
        levels[1].t += 5    #如果下标是应该报空指针错误
        for v in levels
        {
            if v != null
            {
                global.println("level item -> " + v.toString())
            }
        }
        
    }

    # Array.createInstance、[] / `$`、复合赋值、数值 for
    static arrayCreateInstanceIndexLoopTest()
    {
        global.println("========== createInstance / index / for-i ==========")
        intvalue = 16us
        Int32[] a2 = Array<Int32>.create(intvalue)
        
        a2[1] = 50
        a2.$1 += 100
        a2[1] = a2.$1 + 200
        a2[1]--
        global.println("createInstance= " + a2[1] )


        global.println("createInstance= " + intvalue )
        intvalue = 40
        intvalue -= 13
        global.println("createInstance= " + intvalue )
        
        for i = 0, i < a2.length, i++
        {
            if i > 0
            {
                a2[i] = a2[i-1] + 100
            }
            else
            {
                a2[0] = 123
            }
            global.println("createInstance= " + a2.$i )
        }
    }

    # ObjectArray（即 Array<Object>）须显式构造，不能把 int[] 直接赋给 object[]（数组引用类型不再协变）
    static arrayCovariantAndLiteralForInTest()
    {
        global.println("========== ObjectArray 装箱 + literal for-in ==========")
        
        ObjectArray boxed = object[2]
        #boxed[0] = 5
        #boxed[1] = 20 + 3.2f
        boxed[0] = [111,2222,3333]
        boxed.$1 = Level<Int32>(10)
        
        forIIterator(boxed)
        forObject(boxed) #这句不报错
        

        arr2  = [1000,2000,3000,1005]
        forIIterator(arr2)
        forObject(arr2) #这句也不报错，因为参数其实是object
        #ObjectArray oaa = arr2 #这句也报错
    }

    # IIterator<Num> <- 具体数值数组：Meta 层仅允许「遍历语义」的 Number 抽象协变（见 array.md）
    static arrayNumberIteratorFromConcreteArrayTest()
    {
        global.println("========== IIterator<Num> <- Int32[]（只读遍历） ==========")
        Int32[] concrete = Int32[2]
        #int[] concrete2 = Int64[2] #这句报错
        concrete[0] = 11
        concrete[1] = 22
        IIterator<Num> it = concrete.iterator  #允许 要使用iterator 调用函数，支持变成IIerator<Num> 的方式  支持接口的模板的泛变
        IIterable<Object> it2 = concrete;      #接口模板泛型的协变
        it.reset()
        while it.moveNext()
        {
            Num n = it.current()
            global.println("IIterator<Num> current -> " + n.toString())
        }
    }

    # 嵌套 Array<Object>、testArray、单层 + 多层遍历合并为一次深度 walk
    static arrayNestedObjectTreeTest()
    {
        global.println("========== nested Array<Object> / testArray / deep walk ==========")
        #!
        int[] aaaxx12 = Array<int>.create(2)
        aaaxx12[0] = 5
        aaaxx12[1] = 6
        forIIterator(aaaxx12)
        int[] axxx12 = [ 7,8,9,5 ]
        forIIterator(axxx12)
        Array<Array<int> > axxx13 = int[2][] { aaaxx12, [991,992,993,994] }
        forIIterator(axxx13)
        !#
        
        int[] axx22 = int[2]{ int(100.0f), Int32("101" ) }
        forIIterator(axx22)
        object[] axx23 = object[1]{ axx22 }
        forIIterator(axx23)

        #!               
        a1 = Array<Object>(3){ 1, axxx13, axx23 }
        forIIterator(a1)
        
        for v in a1
        {
            if v != null
            {
                global.println("nested level1 -> " + v.toString() )
                
                for v2 in v
                {
                    global.println("nested level2 -> " + v2.toString() )
                    for i = 0, i < v2.length, i++
                    {
                        global.println("nested level3 -> " + v2[i].toString() )
                    }
                }            
            }
            else
            {
                global.println("============index: " + v )
            }
        }
        !#
    }

    # object[][] 锯齿：不能整表用 int[][] 赋给 object[][]，逐行赋 object 可接受的行数组
    static arrayJagged2DAssignTest()
    {
        global.println("========== jagged object[][] assign ==========")
        int[][] jagged2 = int[2][]
        jagged2[0] = int[4]
        jagged2[1] = Array<int>(10)
        jagged2[0][0] = 999
        jagged2[0].$1 = 998
        jagged2.$0.$2 = 997
        jagged2.$0.$3 = 996
        jagged2[1] = [1,100,1000]
        jagged2[1].setValue( 0, 2222 );
        forIIterator(jagged2)
    }

    # int[] 字面量与下标读取
    static arrayIntLiteralReadTest()
    {
        global.println("========== int[] literal read ==========")
        int[] a33 = {1,2,3,4};  #支持这种写法 因为可以理解为 Array<int> a33 = Array<Int32>(4){ 1,2,3,4 } 仅支持 ClassName<T>(param){ ClassName.valurable }的写法
        int[] a332 = new(4){21,22,23,24} #支持这样的写法
        a33[3] = 123
        var aa333 =  a332[0];
        global.println("1111111111= " + a33[3] + "-----" + a33[0] + "  xxxxx=" + aa333 )
    }

    # int[4][] 不规则第二维
    static arrayRank2SparseJaggedTest()
    {
        global.println("========== int[4][] sparse jagged ==========")
        int[3][] a335 = {[11,22,33], int[3]{ 871,872,873 }, int[20] };
        a335[2][1] = 123
        global.println("1111111111= " + a335[2].toString() + "-----" + a335[0].toString() );
    }

    # 混合字面量、`$` 链、is ArrClass（原 a35 与 Level 矩阵重名，拆为 mixedNest / levelGrid）
    static arrayMixedLiteralDollarIndexTest()
    {
        global.println("========== mixed literal / `$` / is ArrClass ==========")
        var ac = ArrClass(){ i1 = 20, i2 = "mix" }
        #global.println("arr_class= " + ac.toString() )

        object[5][] mixedNest = [[137,138,139,ac,148],[[11],[13,14,15]], [ac], object[4]{1,ac,"aaa"} ];        
        #mixedNest = ObjectArray(2){ ObjectArray(5){0,1,2,ac,4}, ObjectArray(2){ [11], [13,14,15] } }
        
        forIIterator(mixedNest)

        int aa = 0
        #mixedNest.$1.$aa.$1 = 3000; #报错
        #mixedNest.$1 = 111
        global.println("1111111111= " + mixedNest.$1.toString() )
       
        var tt1 = mixedNest.$aa.$3
        if tt1 is ArrClass tt2
        {
            tt2.i1 = 200
            var aa1111 = tt2.i1;
            global.println("22222222= " +aa1111 )
        }
    }

    static arrayLevelMatrixTest()
    {
        global.println("========== Level<int>[][] matrix ==========")
        levelvar = Level<int>(100);
        levelvar2 = Level<int>(100);
        Level<int>[][] levelGrid = { [ levelvar, levelvar ], [ levelvar, levelvar ], [levelvar2] };
        levelGrid[2][0].t = 200
        forIIterator(levelGrid) #不支持这种方式，需要是接口的才可以
    }

    static arrayStringAndLevelVectorTest()
    {
        global.println("========== string[] + Level<int>[] vector ==========")
        #!
        strarr = string[6]{"abbc", "cccc", "a100"}
        forIIterator(strarr)   #不支持这种方式，需要是接口的才可以
        !#

        Level<int>[] a44 = new(15) { Level<int>(200) }
        a44[1] = Level<int>(10000)
        int xxx = -2
        a44[(xxx*2+5)].t = 100

        for i = 4, i < 8, i++
        {
            a44[i] = Level<int>( i * 10000 )
            a44[i].t += 135
        }

        forIIterator(a44)
    }

    # 异构 object[][]（float 行 + int 行）
    static arrayHeterogeneousObject2DTest()
    {
        global.println("========== heterogeneous object[][] ==========")
        object[][] a42 = { [1.2,1.3,1.4,1.5],[3,4,5] };
        forIIterator(a42)
    }

    static arrayCtorPrimitiveAndAliasTest()
    {
        global.println("========== ctor: primitive + alias ==========")
        intsByCtor = Array<int>(5){1,2,3,4,5}
        objsByAlias = ObjectArray(20)
        objectRows = Array<Object>(3){ intsByCtor, objsByAlias, ObjectArray(3) }
        
        forIIterator(objectRows)
    }

    static arrayCtorMultidimClassShapeTest()
    {
        global.println("========== ctor: multidim class shape ==========")
        int[][][] cube = { [[1,2,3],[1,2,3,4]], [[1,2,3],[5,6,7,8]] }
        cube[1][1][1] = 12
        forIIterator(cube)

        cubeValue = cube[1][1][1]
        global.println("==========cubevalue:" + cubeValue )

        ArrClass[][] arrclass1 = new(10)  #不支持这种方式
        arrclass1.fill( ArrClass[2] )
        forIIterator(arrclass1)

        arrClass2 = ArrClass[10][10][]
        forIIterator(arrClass2)
    }

    static arrayCtorLiteralMixTypeTest()
    {
        global.println("========== ctor: literal mix ==========")
        float[] floatsFromLiteral = {1.2,1.3,1.5}
        forIIterator(floatsFromLiteral)
        mixedObj = object[8]{"aa", 1, "232", 1.0f}
        forIIterator(mixedObj)
    }
    static arrayCtorFloatOverloadTest()
    {
        global.println("========== ctor: float overload ==========")
        float[] a7 = Array<float>(10){ 1.2, 2.2, 3.4 }
        float[] a8 = Array<float>(20){1,2,3,5,3.3}

        
        forIIterator(a7)
        forIIterator(a8)
    }

    static arrayArrClassIndexAndCurrentTest()
    {
        global.println("========== ArrClass[]: index/current ==========")
        ArrClass[] arr1 = new(3)
        int i11 = 2
        arr1[0] = ArrClass()
        arr1[1] = ArrClass(111)
        arr1[i11] = ArrClass(222, "str222str" )
        arr1.$i11.i1 = 10
        arr1[1] = { i1 = 20 }
        arr1[1].i1 = 10000
        arr1.$0.i1 = 330
        #arr1.$"aa".i1 = 20; #报错
        arr1.index = 2
        arr1.current.i1 = 10

        
        forIIterator(arr1)
    }

    static arrayArrClassForInAssignTest()
    {
        global.println("========== ArrClass[]: for-in assign ==========")
        ArrClass[] arr1 = new(3)
        arr1[0] = ArrClass()
        arr1[1] = ArrClass()
        arr1[2] = ArrClass()
        for a in arr1
        {
            if arr1.index == 2
            {
                arr1.current = ArrClass(){ i1 = 100 }
                global.println("=========arr1: " + a.toString() )
                continue
            }
            a.i1 = 200
            global.println("=========arr111---: " + a.toString() )
        }
    }

    static arrayForInLiteralIndexTest()
    {
        global.println("========== for-in literal index ==========")
        
        arr = [111,2222,33,44444444444]
        for a in arr
        {
            var forInIdx = arr.index + 1            
            global.println("=forIndex=$forInIdx" + arr.index )
        }
        

        for a in [11111111,2222222222,3,4444444444444]
        {
            #这里只允许使用a 像访问数组下标，只能通过变量获取
            global.println("=a==" + a.toString() )
        }
    }

    static arrayArrClassCountLoopWriteTest()
    {
        global.println("========== ArrClass[]: count loop write ==========")
        ArrClass[] arr1 = new(60)
        for i = 0, i < arr1.length
        {
            i++
            if i < 40
            {
                continue
            }

            arr1[i] = ArrClass()
            arr1[i].i1 = 100
            arr1.$i.i1 = 100
             global.println("====index: " + i + "  arrobj: " + arr1.$i.toString() )
            i += 2           
        }
    }
    static fun()
    {        
        arrayBasicApiTest()        
        arrayGenericElementTest()        
        arrayCreateInstanceIndexLoopTest()
        arrayCovariantAndLiteralForInTest()
        arrayNumberIteratorFromConcreteArrayTest()        
        arrayNestedObjectTreeTest()        
        arrayJagged2DAssignTest()        
        arrayIntLiteralReadTest()
        arrayRank2SparseJaggedTest()        
        arrayMixedLiteralDollarIndexTest()
        arrayLevelMatrixTest()
        arrayStringAndLevelVectorTest()        
        arrayHeterogeneousObject2DTest()
        arrayCtorPrimitiveAndAliasTest()
        arrayCtorMultidimClassShapeTest()
        arrayCtorLiteralMixTypeTest()
        arrayCtorFloatOverloadTest()
        arrayArrClassIndexAndCurrentTest()
        arrayArrClassForInAssignTest()
        arrayForInLiteralIndexTest()
        arrayArrClassCountLoopWriteTest()     
        
    }
}
# 3.1.1 先实现了，在函数里，直接调用C#层写的方法。

#!
1. 使用Array为数组的关键字，通过模板T实例化，生成一个数组
2. 生成数组的方式有Array<int> Array<object> Array<Array<int> > 传统这种的方式,也可以使用 int[] object[], string[][][] 这种方式生成数组
3. 生成数组还可以使用直接赋值的方式 比如 val = [1,2,3,4]  这种情况，会自动计算数组的初始长度
4. 如果定义了前边的类型，可以直接使用{}的方式 比如 int[] val = {1,2,3,4}, 这种情况，会自动计算数组的初始长度
5. 如果使用了生成函数方式 比如  int[] val = int[5]{1,2,3,4} 当然也可以省略掉前边的 val = int[5]{1,2,3} 在使用函数生成时，必须要给数组的最后一维增加长度
6. 数组引用类型默认不协变（不能把 int[] 赋给 object[] 等）；数值抽象例外见 array.md（IIterator<Num>、const Array<Num>）
7. 数组如果继承了IIterator, IIterable, 相关内容后，即可进行for的遍历
8. 数组的访问，可以通过 val[1][2] 这种方式访问，也可以使用 val.$1.$2 这种方式访问，`$1` = `[1]` 是相同的，语法上，没有差别


1. 在使用迭代器时，需要先new一个iterateVariable 对象
2. 然后把IIterable放到本地的_iterable节点中
3. 
!#

# ArrayTest：按 `arrayXxxTest` 分类；`forInPrintNullable` 合并原多处相同 for-in；`arrayNestedObjectTreeTest` 合并对 a1 的重复遍历；原 `a35`/`a4` 重名拆为 `mixedNest`/`levelGrid` 与 `nestedObjRows`/`floatsFromLiteral`；`ArrClass` 字段统一为 `i1`；`ArrClass[] arr1` 显式 `new(1001)`。`arrayCovariantAndLiteralForInTest` 使用显式 `ObjectArray` 装箱（不再 `object[] = int[]`）；`arrayNumberIteratorFromConcreteArrayTest` 演示 `IIterator<Num> <- Int32[]`；`arrayJagged2DAssignTest` 用 `object[2][]` 逐行赋行数组。
