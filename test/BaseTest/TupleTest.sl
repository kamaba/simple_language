TupleTest
{
    # 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[TupleTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[TupleTest] " + name + " : FAIL" )
        }
    }

    # ============ 模板形式：Tuple<T1> 单元素 ============
    static templateTuple1Test()
    {
        Tuple<int> t = Tuple<int>( 100 )
        check( "Tuple<int> item1", t.item1 == 100 )
        check( "Tuple<int> length", t.length == 1 )
        check( "Tuple<int> $0", ( t.$0 as int ) == 100 )
        check( "Tuple<int> toString", t.toString() == "(100)" )

        t.$0 = 200
        check( "Tuple<int> setItem $0", t.item1 == 200 )
        check( "Tuple<int> getItem after set", ( t.$0 as int ) == 200 )

        # 越界：get 返回 null，set 静默忽略
        check( "Tuple<int> out-of-range $1 is null", t.$1 == null )
        t.$1 = 300
        check( "Tuple<int> out-of-range set ignored", t.item1 == 200 )

        # 方法直接调用形式
        check( "Tuple<int> _getItem_ call", ( t._getItem_( 0 ) as int ) == 200 )
        t._setItem_( 0, 400 )
        check( "Tuple<int> _setItem_ call", t.item1 == 400 )
    }

    # ============ 模板形式：Tuple<T1,T2> 双元素 ============
    static templateTuple2Test()
    {
        Tuple<int,string> t = Tuple<int,string>( 1, "one" )
        check( "Tuple<int,string> item1", t.item1 == 1 )
        check( "Tuple<int,string> item2", t.item2 == "one" )
        check( "Tuple<int,string> length", t.length == 2 )
        check( "Tuple<int,string> $0", ( t.$0 as int ) == 1 )
        check( "Tuple<int,string> $1", ( t.$1 as string ) == "one" )
        check( "Tuple<int,string> toString", t.toString() == "(1, one)" )

        t.$0 = 10
        t.$1 = "ten"
        check( "Tuple<int,string> setItem $0", t.item1 == 10 )
        check( "Tuple<int,string> setItem $1", t.item2 == "ten" )
        check( "Tuple<int,string> out-of-range $2 is null", t.$2 == null )

        # double 元素
        Tuple<double,string> td = Tuple<double,string>( 3.5, "d" )
        check( "Tuple<double,string> $0", ( td.$0 as double ) == 3.5 )
        check( "Tuple<double,string> $1", ( td.$1 as string ) == "d" )
    }

    # ============ 模板形式：3 元 / 4 元 ============
    static templateTuple34Test()
    {
        Tuple<string,int,bool> t3 = Tuple<string,int,bool>( "a", 2, true )
        check( "Tuple<T1,T2,T3> item1", t3.item1 == "a" )
        check( "Tuple<T1,T2,T3> item2", t3.item2 == 2 )
        check( "Tuple<T1,T2,T3> item3", t3.item3 == true )
        check( "Tuple<T1,T2,T3> length", t3.length == 3 )
        check( "Tuple<T1,T2,T3> $0", ( t3.$0 as string ) == "a" )
        check( "Tuple<T1,T2,T3> $1", ( t3.$1 as int ) == 2 )
        check( "Tuple<T1,T2,T3> $2", ( t3.$2 as bool ) == true )
        check( "Tuple<T1,T2,T3> toString", t3.toString() == "(a, 2, True)" )

        t3.$1 = 20
        check( "Tuple<T1,T2,T3> setItem $1", t3.item2 == 20 )
        check( "Tuple<T1,T2,T3> out-of-range $3 is null", t3.$3 == null )

        Tuple<int,int,int,int> t4 = Tuple<int,int,int,int>( 1, 2, 3, 4 )
        check( "Tuple<T1..T4> length", t4.length == 4 )
        check( "Tuple<T1..T4> $0", ( t4.$0 as int ) == 1 )
        check( "Tuple<T1..T4> $3", ( t4.$3 as int ) == 4 )
        check( "Tuple<T1..T4> toString", t4.toString() == "(1, 2, 3, 4)" )
        check( "Tuple<T1..T4> out-of-range $4 is null", t4.$4 == null )
        t4.$2 = 33
        check( "Tuple<T1..T4> setItem $2", t4.item3 == 33 )
    }

    # ============ 模板形式：5~8 元（高 arity） ============
    static templateTuple5To8Test()
    {
        Tuple<int,int,int,int,int> t5 = Tuple<int,int,int,int,int>( 1, 2, 3, 4, 5 )
        check( "Tuple<T1..T5> length", t5.length == 5 )
        check( "Tuple<T1..T5> $4", ( t5.$4 as int ) == 5 )
        check( "Tuple<T1..T5> toString", t5.toString() == "(1, 2, 3, 4, 5)" )

        Tuple<int,int,int,int,int,int> t6 = Tuple<int,int,int,int,int,int>( 1, 2, 3, 4, 5, 6 )
        check( "Tuple<T1..T6> length", t6.length == 6 )
        check( "Tuple<T1..T6> $5", ( t6.$5 as int ) == 6 )

        Tuple<int,int,int,int,int,int,int> t7 = Tuple<int,int,int,int,int,int,int>( 1, 2, 3, 4, 5, 6, 7 )
        check( "Tuple<T1..T7> length", t7.length == 7 )
        check( "Tuple<T1..T7> $6", ( t7.$6 as int ) == 7 )

        Tuple<int,int,int,int,int,int,int,int> t8 = Tuple<int,int,int,int,int,int,int,int>( 1, 2, 3, 4, 5, 6, 7, 8 )
        check( "Tuple<T1..T8> length", t8.length == 8 )
        check( "Tuple<T1..T8> $0", ( t8.$0 as int ) == 1 )
        check( "Tuple<T1..T8> $7", ( t8.$7 as int ) == 8 )
        t8.$7 = 80
        check( "Tuple<T1..T8> setItem $7", t8.item8 == 80 )
        check( "Tuple<T1..T8> out-of-range $8 is null", t8.$8 == null )
    }

    # ============ 模板形式：static create / null 元素 ============
    static templateTupleCreateNullTest()
    {
        Tuple<int,string> tc = Tuple<int,string>.create( 9, "nine" )
        check( "Tuple<int,string>.create item1", tc.item1 == 9 )
        check( "Tuple<int,string>.create item2", tc.item2 == "nine" )
        check( "Tuple<int,string>.create toString", tc.toString() == "(9, nine)" )

        Tuple<int,int,int> tc3 = Tuple<int,int,int>.create( 1, 2, 3 )
        check( "Tuple<T1..T3>.create length", tc3.length == 3 )
        check( "Tuple<T1..T3>.create $2", ( tc3.$2 as int ) == 3 )

        # null 元素：字段访问 / 下标访问 / toString
        Tuple<string,string> tn = Tuple<string,string>( "x", null )
        check( "Tuple null item2", tn.item2 == null )
        check( "Tuple null $1 is null", tn.$1 == null )
        check( "Tuple null toString", tn.toString() == "(x, null)" )
    }

    # ============ 无模板形式：空元组与 1~8 参构造 ============
    static plainTupleCtorTest()
    {
        Tuple empty = Tuple()
        check( "Tuple() length", empty.length == 0 )
        check( "Tuple() isEmpty", empty.isEmpty == true )
        check( "Tuple() toString", empty.toString() == "()" )
        check( "Tuple() out-of-range $0 is null", empty.$0 == null )

        Tuple t1 = Tuple( 1 )
        check( "Tuple(1) length", t1.length == 1 )
        check( "Tuple(1) $0", ( t1.$0 as int ) == 1 )
        check( "Tuple(1) toString", t1.toString() == "(1)" )

        Tuple t2 = Tuple( "a", 2 )
        check( "Tuple(a,2) length", t2.length == 2 )
        check( "Tuple(a,2) $0", ( t2.$0 as string ) == "a" )
        check( "Tuple(a,2) $1", ( t2.$1 as int ) == 2 )
        check( "Tuple(a,2) toString", t2.toString() == "(a, 2)" )

        Tuple t3 = Tuple( 1, 2.5, "three" )
        check( "Tuple(3 args) length", t3.length == 3 )
        check( "Tuple(3 args) $0", ( t3.$0 as int ) == 1 )
        check( "Tuple(3 args) $1", ( t3.$1 as double ) == 2.5 )
        check( "Tuple(3 args) $2", ( t3.$2 as string ) == "three" )

        Tuple t4 = Tuple( 1, 2, 3, 4 )
        check( "Tuple(4 args) length", t4.length == 4 )
        check( "Tuple(4 args) $3", ( t4.$3 as int ) == 4 )

        Tuple t5 = Tuple( 1, 2, 3, 4, 5 )
        check( "Tuple(5 args) length", t5.length == 5 )
        check( "Tuple(5 args) $4", ( t5.$4 as int ) == 5 )

        Tuple t6 = Tuple( 1, 2, 3, 4, 5, 6 )
        check( "Tuple(6 args) length", t6.length == 6 )
        check( "Tuple(6 args) $5", ( t6.$5 as int ) == 6 )

        Tuple t7 = Tuple( 1, 2, 3, 4, 5, 6, 7 )
        check( "Tuple(7 args) length", t7.length == 7 )
        check( "Tuple(7 args) $6", ( t7.$6 as int ) == 7 )

        Tuple t8 = Tuple( 1, 2, 3, 4, 5, 6, 7, 8 )
        check( "Tuple(8 args) length", t8.length == 8 )
        check( "Tuple(8 args) $0", ( t8.$0 as int ) == 1 )
        check( "Tuple(8 args) $7", ( t8.$7 as int ) == 8 )
    }

    # ============ 无模板形式：create(params) 不限长 ============
    static plainTupleCreateTest()
    {
        Tuple t = Tuple.create( 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 )
        check( "Tuple.create 10 items length", t.length == 10 )
        check( "Tuple.create $0", ( t.$0 as int ) == 1 )
        check( "Tuple.create $5", ( t.$5 as int ) == 6 )
        check( "Tuple.create $9", ( t.$9 as int ) == 10 )
        check( "Tuple.create toString", t.toString() == "(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)" )

        # 混合类型元素
        Tuple m = Tuple.create( "x", 1, 2.5, true, null )
        check( "Tuple.create mixed length", m.length == 5 )
        check( "Tuple.create mixed $0", ( m.$0 as string ) == "x" )
        check( "Tuple.create mixed $1", ( m.$1 as int ) == 1 )
        check( "Tuple.create mixed $3", ( m.$3 as bool ) == true )
        check( "Tuple.create mixed $4 is null", m.$4 == null )
        check( "Tuple.create mixed toString", m.toString() == "(x, 1, 2.5, True, null)" )

        # 空工厂
        Tuple e = Tuple.create()
        check( "Tuple.create() length", e.length == 0 )
        check( "Tuple.create() toString", e.toString() == "()" )
    }

    # ============ 无模板形式：add 链式追加与自动扩容 ============
    static plainTupleAddTest()
    {
        Tuple t = Tuple()
        t.add( 1 ).add( 2 ).add( 3 )
        check( "add chain length", t.length == 3 )
        check( "add chain $0", ( t.$0 as int ) == 1 )
        check( "add chain $2", ( t.$2 as int ) == 3 )
        check( "add chain toString", t.toString() == "(1, 2, 3)" )

        # 初始容量 4，添加 20 个元素触发多次扩容（4->8->16->32）
        Tuple big = Tuple()
        for i = 0, i < 20, i++
        {
            big.add( i )
        }
        check( "add 20 items length", big.length == 20 )
        check( "add 20 items $0", ( big.$0 as int ) == 0 )
        check( "add 20 items $10", ( big.$10 as int ) == 10 )
        check( "add 20 items $19", ( big.$19 as int ) == 19 )

        # 构造 4 个后继续 add（超出初始容量）
        Tuple ext = Tuple( "a", "b", "c", "d" )
        check( "Tuple(4 args) before add", ext.length == 4 )
        ext.add( "e" ).add( "f" )
        check( "Tuple add beyond capacity length", ext.length == 6 )
        check( "Tuple add beyond capacity $4", ( ext.$4 as string ) == "e" )
        check( "Tuple add beyond capacity $5", ( ext.$5 as string ) == "f" )
        check( "Tuple add beyond capacity toString", ext.toString() == "(a, b, c, d, e, f)" )
    }

    # ============ 无模板形式：下标读写与越界 ============
    static plainTupleIndexTest()
    {
        Tuple t = Tuple( "a", "b", "c" )
        t.$1 = "B"
        check( "plain setItem $1", ( t.$1 as string ) == "B" )
        check( "plain setItem keeps $0", ( t.$0 as string ) == "a" )
        check( "plain setItem keeps $2", ( t.$2 as string ) == "c" )

        # 方法直接调用形式
        check( "plain _getItem_ call", ( t._getItem_( 0 ) as string ) == "a" )
        t._setItem_( 2, "C" )
        check( "plain _setItem_ call", ( t.$2 as string ) == "C" )

        # 越界：get 返回 null，set 静默忽略
        check( "plain out-of-range $3 is null", t.$3 == null )
        check( "plain _getItem_(-1) is null", t._getItem_( -1 ) == null )
        t.$3 = "D"
        t._setItem_( 100, "E" )
        check( "plain out-of-range set ignored", t.length == 3 )
    }

    # ============ 无模板形式：indexOf / contains / clear ============
    static plainTupleSearchTest()
    {
        Tuple t = Tuple( 10, 20, 30 )
        check( "indexOf 20", t.indexOf( 20 ) == 1 )
        check( "indexOf 30", t.indexOf( 30 ) == 2 )
        check( "indexOf missing", t.indexOf( 99 ) == -1 )
        check( "contains 10", t.contains( 10 ) == true )
        check( "contains missing", t.contains( 99 ) == false )
        check( "before clear isEmpty", t.isEmpty == false )

        t.clear()
        check( "after clear length", t.length == 0 )
        check( "after clear isEmpty", t.isEmpty == true )
        check( "after clear $0 is null", t.$0 == null )

        # clear 后可继续 add
        t.add( "re" )
        check( "add after clear length", t.length == 1 )
        check( "add after clear $0", ( t.$0 as string ) == "re" )
    }

    # ============ 嵌套元组 ============
    static nestedTupleTest()
    {
        # 模板嵌套模板
        Tuple<int, Tuple<string,bool> > nested = Tuple<int, Tuple<string,bool> >( 1, Tuple<string,bool>( "y", true ) )
        check( "nested template item1", nested.item1 == 1 )
        check( "nested template item2.item1", nested.item2.item1 == "y" )
        check( "nested template item2.item2", nested.item2.item2 == true )
        check( "nested template $1.item1", ( ( nested.$1 as Tuple<string,bool> ).item1 ) == "y" )
        check( "nested template toString", nested.toString() == "(1, (y, True))" )

        # 无模板套模板
        Tuple outer = Tuple( 1, Tuple<int,string>( 5, "five" ), "tail" )
        check( "plain outer length", outer.length == 3 )
        Tuple<int,string> inner = outer.$1 as Tuple<int,string>
        check( "plain outer inner item1", inner.item1 == 5 )
        check( "plain outer inner item2", inner.item2 == "five" )
        check( "plain outer toString", outer.toString() == "(1, (5, five), tail)" )

        # 模板套无模板
        Tuple<string, Tuple> nested2 = Tuple<string, Tuple>( "key", Tuple( 1, 2 ) )
        check( "template outer item2 length", nested2.item2.length == 2 )
        check( "template outer item2 $1", ( nested2.item2.$1 as int ) == 2 )
    }

    static fun()
    {
        global.println( "========== TupleTest (start) ==========" )
        templateTuple1Test()
        templateTuple2Test()
        templateTuple34Test()
        templateTuple5To8Test()
        templateTupleCreateNullTest()
        plainTupleCtorTest()
        plainTupleCreateTest()
        plainTupleAddTest()
        plainTupleIndexTest()
        plainTupleSearchTest()
        nestedTupleTest()
        global.println( "========== TupleTest (end) ==========" )
    }
}

#!
TupleTest 测试说明（对应 Core 的 Tuple.sl）：
1. 模板形式 Tuple<T1>~Tuple<T1..T8>：字段 item1..itemN、length、
   $index 下标读写（_getItem_/_setItem_）、static create、toString、null 元素、越界。
2. 无模板形式 Tuple：空构造/1~8 参构造、create(params) 不限长、
   add 链式追加与自动扩容（4->8->16...）、indexOf/contains/clear、toString。
3. 嵌套：模板套模板、无模板套模板、模板套无模板。
4. 越界语义：get 返回 null，set 静默忽略。
!#
