#小写容器关键字语法糖测试：
#   map()   => Map<Object,Object>          map(8) => Map<Object,Object>(8)
#   list()  => List<Object>                stack()/set()/queue() 同理 => <Object>
#   set(10) => Set<Object>(10)             array(10) => Array<Object>(10)
#   range(1,10) => Range<int>(1,10)        tuple() => Tuple（无模板动态元组）
#同时验证：小写名作局部变量仍优先、属性 get/set 语法不受 set 容器关键字影响
LowercaseContainerTest
{
    # 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[LowercaseContainerTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[LowercaseContainerTest] " + name + " : FAIL" )
        }
    }

    # ============ map() => Map<Object,Object> ============
    static testMap()
    {
        m = map()
        m.add( 1, "one" )
        m.add( 2, "two" )
        check( "map() length", m.length == 2 )
        check( "map() containsKey", m.containsKey( 1 ) == true )
        check( "map() containsValue", m.containsValue( "two" ) == true )
        check( "map() _getItem_", ( m._getItem_( 1 ) as string ) == "one" )

        m._setItem_( 3, "three" )
        check( "map() _setItem_ length", m.length == 3 )

        # 下标读写（键为 int，装箱为 Object）
        check( "map() subscript read", ( m[2] as string ) == "two" )
        m[4] = "four"
        check( "map() subscript write", ( m[4] as string ) == "four" )

        # 指定容量构造 map(8) => Map<Object,Object>(8)
        m8 = map( 8 )
        check( "map(8) capacity", m8.capacity == 8 )

        # 显式模板实参 map<int,string>() => Map<int,string>
        mi = map<int,string>()
        mi.add( 10, "ten" )
        check( "map<int,string>() length", mi.length == 1 )
        check( "map<int,string>() get", ( mi._getItem_( 10 ) as string ) == "ten" )
    }

    # ============ list() => List<Object> ============
    static testList()
    {
        l = list()
        l.add( 1 )
        l.add( "two" )
        l.add( 3.5 )
        check( "list() length", l.length == 3 )
        check( "list() contains int", l.contains( 1 ) == true )
        check( "list() contains string", l.contains( "two" ) == true )
        check( "list() isEmpty", l.isEmpty == false )

        # 显式模板实参 list<int>() => List<int>
        li = list<int>()
        li.add( 1 )
        li.add( 2 )
        check( "list<int>() length", li.length == 2 )
        check( "list<int>() contains", li.contains( 2 ) == true )
    }

    # ============ stack() => Stack<Object> ============
    static testStack()
    {
        s = stack()
        s.push( 1 )
        s.push( "two" )
        s.push( 3 )
        check( "stack() length", s.length == 3 )
        check( "stack() peek", ( s.peek as int ) == 3 )
        check( "stack() bottom", ( s.bottom as int ) == 1 )
        check( "stack() pop", ( s.pop() as int ) == 3 )
        check( "stack() length after pop", s.length == 2 )
        check( "stack() contains", s.contains( 1 ) == true )
    }

    # ============ set() => Set<Object> ============
    static testSet()
    {
        st = set()
        st.add( 1 )
        st.add( "two" )
        check( "set() add duplicate false", st.add( 1 ) == false )
        check( "set() length", st.length == 2 )
        check( "set() contains int", st.contains( 1 ) == true )
        check( "set() contains string", st.contains( "two" ) == true )
        check( "set() isEmpty", st.isEmpty == false )

        # 指定容量构造 set(10) => Set<Object>(10)
        st10 = set( 10 )
        check( "set(10) capacity", st10.capacity == 10 )

        # 显式模板实参 set<int>() => Set<int>
        si = set<int>()
        si.add( 1 )
        si.add( 2 )
        si.add( 2 )
        check( "set<int>() length", si.length == 2 )
        check( "set<int>() contains", si.contains( 2 ) == true )
    }

    # ============ queue() => Queue<Object> ============
    static testQueue()
    {
        q = queue()
        q.enqueue( 1 )
        q.enqueue( "two" )
        q.enqueue( 3 )
        check( "queue() length", q.length == 3 )
        check( "queue() peek", ( q.peek as int ) == 1 )
        check( "queue() rear", ( q.rear as int ) == 3 )
        check( "queue() dequeue", ( q.dequeue() as int ) == 1 )
        check( "queue() length after dequeue", q.length == 2 )
        check( "queue() contains", q.contains( "two" ) == true )

        # 显式模板实参 queue<int>() => Queue<int>
        qi = queue<int>()
        qi.enqueue( 1 )
        qi.enqueue( 2 )
        check( "queue<int>() length", qi.length == 2 )
        check( "queue<int>() dequeue", ( qi.dequeue() as int ) == 1 )
    }

    # ============ array(10) => Array<Object>(10) ============
    static testArray()
    {
        a = array( 10 )
        check( "array(10) length", a.length == 10 )
        a[0] = 100
        a[9] = "nine"
        check( "array(10) read [0]", ( a[0] as int ) == 100 )
        check( "array(10) read [9]", ( a[9] as string ) == "nine" )

        # 显式模板实参 array<int>(4) => Array<int>(4)
        ai = array<int>( 4 )
        check( "array<int>(4) length", ai.length == 4 )
        ai[0] = 42
        check( "array<int>(4) read [0]", ai[0] == 42 )
    }

    # ============ range(1,10) => Range<int>(1,10)（end 不含） ============
    static testRange()
    {
        count = 0
        first = 0
        last = 0
        for v in range( 1, 10 )
        {
            if count == 0
            {
                first = v
            }
            last = v
            count = count + 1
        }
        check( "range(1,10) count", count == 9 )
        check( "range(1,10) first", first == 1 )
        check( "range(1,10) last", last == 9 )

        # 单参形式 range(5) => 0..4
        c2 = 0
        for v in range( 5 )
        {
            c2 = c2 + 1
        }
        check( "range(5) count", c2 == 5 )

        # 步长形式 range(1,10,3) => 1,4,7
        c3 = 0
        last3 = 0
        for v in range( 1, 10, 3 )
        {
            last3 = v
            c3 = c3 + 1
        }
        check( "range(1,10,3) count", c3 == 3 )
        check( "range(1,10,3) last", last3 == 7 )

        # 大写显式模板 Range<int> 仍走原路径
        c4 = 0
        for v in Range<int>( 1, 4 )
        {
            c4 = c4 + 1
        }
        check( "Range<int>(1,4) count", c4 == 3 )
    }

    # ============ tuple() => Tuple（无模板动态元组） ============
    static testTuple()
    {
        Tuple t = tuple()
        check( "tuple() length", t.length == 0 )
        check( "tuple() isEmpty", t.isEmpty == true )

        t.add( 1 ).add( "two" ).add( 3.5 )
        check( "tuple() add chain length", t.length == 3 )
        check( "tuple() $0", ( t.$0 as int ) == 1 )
        check( "tuple() $1", ( t.$1 as string ) == "two" )
        check( "tuple() toString", t.toString() == "(1, two, 3.5)" )

        # 带参构造 tuple(1,"a") => Tuple(1,"a")
        Tuple t2 = tuple( 1, "a" )
        check( "tuple(1,a) length", t2.length == 2 )
        check( "tuple(1,a) $0", ( t2.$0 as int ) == 1 )
        check( "tuple(1,a) $1", ( t2.$1 as string ) == "a" )

        # 大写 Tuple 显式模板仍走原路径
        Tuple<int,string> t3 = Tuple<int,string>( 5, "five" )
        check( "Tuple<int,string> item1", t3.item1 == 5 )
        check( "Tuple<int,string> item2", t3.item2 == "five" )
    }

    # ============ 小写容器名作局部变量名：本地变量优先 ============
    static testLocalVariableName()
    {
        List<int> list = List<int>()
        list.add( 1 )
        list.add( 2 )
        check( "local var list priority", list.length == 2 )

        # 项目内 GenClass.sl 定义了用户 Map<T1,T2>（成员 m1/m2），此处 `Map` 解析到该用户类
        # 验证名为 map 的局部变量与小写关键字糖共存（等价 GenClass.sl 的既有用法）
        Map<int,string> map = Map<int,string>()
        map.m1 = 10
        map.m2 = "mm"
        check( "local var map member m1", map.m1 == 10 )
        check( "local var map member m2", map.m2 == "mm" )

        Stack<int> stack = Stack<int>()
        stack.push( 1 )
        check( "local var stack priority", stack.length == 1 )

        Queue<int> queue = Queue<int>()
        queue.enqueue( 1 )
        check( "local var queue priority", queue.length == 1 )

        Tuple tuple = Tuple( 1 )
        check( "local var tuple priority", tuple.length == 1 )
    }

    # ============ 属性 get/set 语法不受 set 容器关键字影响 ============
    class PropCoreForm
    {
        int _pv = 0
        get int pv()
        {
            ret this._pv
        }
        set void pv( int v )
        {
            this._pv = v
        }
    }

    class PropBlockForm
    {
        int bv = 0
        {
            get()
            {
                ret this.bv
            }
            set( int v )
            {
                this.bv = v
            }
        }
    }

    static testPropertyGetSet()
    {
        p = PropCoreForm()
        p.pv = 42
        check( "property set void pv( int v )", p.pv == 42 )

        b = PropBlockForm()
        b.bv = 7
        check( "property block set( int v )", b.bv == 7 )
    }

    static fun()
    {
        global.println( "========== LowercaseContainerTest (start) ==========" )
        testMap()
        testList()
        testStack()
        testSet()
        testQueue()
        testArray()
        testRange()
        testTuple()
        testLocalVariableName()
        testPropertyGetSet()
        global.println( "========== LowercaseContainerTest (end) ==========" )
    }
}

#!
LowercaseContainerTest 测试说明：
1. 小写容器关键字（用户需求）：
   map() => Map<Object,Object>（2 个默认模板实参 Object）
   list()/stack()/set()/queue()/array() => <Object>（1 个默认实参）
   set(10)/map(8)/array(10) => 指定容量构造
   range(1,10) => Range<int>(1,10)   tuple() => Tuple（无模板）
2. 显式模板实参形式 map<int,string>()/list<int>()/array<int>(4) 同时可用。
3. 兼容性：小写名（list/map/stack/queue/tuple）作局部变量名时本地变量优先。
4. 兼容性：属性 setter 两种形式（set void pv(int v) 与 set( int v ){} 块形式）
   不被误判为容器调用。
!#
