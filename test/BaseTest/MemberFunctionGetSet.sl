import Std

namespace NSMFSetGet{

Level1
{
    int _Level1_var1 = 123

    get Level1_var1()
    {
        ret this._Level1_var1
    }

    set Level1_var1(obj)
    {
        this._Level1_var1 = obj as int
    }
}

Level2 extends Level1
{
    int _Level2_var1 = 10

    get Level2_var1()
    {
        ret this._Level2_var1
    }

    set Level2_var1(obj)
    {
        this._Level2_var1 = obj
    }

    override set Level1_var1(obj)
    {
        this.Level2_var1 = obj
        this._Level1_var1 = obj
    }

    int b = 20
}

MFSetGet
{
    static fun()
    {
        global.println("========== MemberFunctionGetSet (start) ==========")
        var c = Level2(){}

        c.Level1_var1 = 100
        c.Level2_var1 = 200

        global.println("Level1_var1 -> " + c.Level1_var1.toString())
        global.println("Level2_var1 -> " + c.Level2_var1.toString())
        global.println("========== MemberFunctionGetSet (end) ==========")
    }
}

}
# 测试面向：属性语法 get/set 与同名访问器、子类 override set 写回父类逻辑字段。
# 预期：先赋 Level1_var1=100 再 Level2_var1=200 后，两路 getter 反映最终一致或分层语义（以编译器为准）；无 _Level2_var1._a 一类笔误。
