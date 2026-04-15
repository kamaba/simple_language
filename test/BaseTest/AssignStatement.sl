import Std
import CSharp.SimpleLanguage
import CSharp.System

AssignStatement
{
    ArrClass
    {
        int i1 = 0
        i2 = "aaa"

        set i1set(int i111)
        {
            this.i1 = i111
        }
    }

    Level<T>
    {
        T t = new()

        _init_(obj)
        {
            this.t = obj as T
        }

        override string toString()
        {
            ret this.t.toString()
        }
    }

    static fun()
    {
        global.println("========== AssignStatement (start) ==========")

        ArrClass ac = new(){ i2 = "bbb" }
        ac.i1 = 10
        ac.i1 += 20
        ac.i1++
        # ac.i1 = ac.a1++;   # 负例：无 a1；且 ++ 仅允许出现在合法语句上下文中
        ac.i1 = (20 / 3).toInt32() + 104
        ac.i1set = 250
        global.println("ArrClass i1 (复合赋值/++/setter 后) -> " + ac.i1.toString())

        Level<int> lv = Level<int>(42)
        global.println("Level<int> toString -> " + lv.toString())

        global.println("========== AssignStatement (end) ==========")
    }
}

# 测试面向：复合赋值 +=、后缀 ++、成员 setter、泛型 Level<T> 构造与 as T 初始化。
# 预期：i1 经多步后等于 250；Level<int> 包装值为 42；不包含对不存在成员 a1 的引用。
