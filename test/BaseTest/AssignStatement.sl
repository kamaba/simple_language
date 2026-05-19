import Std
import CSharp.SimpleLanguage
import CSharp.System

AssignStatement
{
    AssignBox
    {
        Int32 value = 0
        string name = "seed"
        Int32 flag = 0b0011
        Int32[] numbers = new(3) { 1, 2, 3 }

        get total()
        {
            ret this.value + this.numbers[0] + this.numbers[1] + this.numbers[2]
        }

        set score(obj)
        {
            this.value = obj as Int32
        }

        override string toString()
        {
            ret "AssignBox(value=" + this.value.toString() + ", name=" + this.name + ", flag=" + this.flag.toString() + ", n0=" + this.numbers[0].toString() + ", n1=" + this.numbers[1].toString() + ", n2=" + this.numbers[2].toString() + ")"
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
            ret "Level<T>(" + this.t.toString() + ")"
        }
    }

    static scalarAssignTest()
    {
        global.println("----- scalarAssignTest -----")

        Int32 expr = 0
        expr = (20 / 3).toInt32() + 104
        global.println("expr = (20 / 3).toInt32() + 104 -> " + expr.toString())

        Int32 addValue = 10
        addValue += 5
        global.println("addValue += 5 -> " + addValue.toString())

        Int32 minusValue = 50
        minusValue -= 12
        global.println("minusValue -= 12 -> " + minusValue.toString())

        Int32 multiplyValue = 6
        multiplyValue *= 7
        global.println("multiplyValue *= 7 -> " + multiplyValue.toString())

        Int32 divideValue = 47
        divideValue /= 5
        global.println("divideValue /= 5 -> " + divideValue.toString())

        Int32 moduloValue = 23
        moduloValue %= 5
        global.println("moduloValue %= 5 -> " + moduloValue.toString())

        Int32 shiftLeftValue = 3
        shiftLeftValue <<= 2
        global.println("shiftLeftValue <<= 2 -> " + shiftLeftValue.toString())

        Int32 shiftRightValue = -16
        shiftRightValue >>= 2
        global.println("shiftRightValue >>= 2 -> " + shiftRightValue.toString())

        Int32 andValue = 0b1100
        andValue &= 0b1010
        global.println("andValue &= 0b1010 -> " + andValue.toString())

        Int32 xorValue = 0b1010
        xorValue ^= 0b1100
        global.println("xorValue ^= 0b1100 -> " + xorValue.toString())

        Int32 orValue = 0b1010
        orValue |= 0b0100
        global.println("orValue |= 0b0100 -> " + orValue.toString())

        Int32 stepValue = 10
        stepValue++
        stepValue--
        #a = stepValue++;    #不允许这样处理，只可以直接使用++ --
        global.println("stepValue ++ / -- -> " + stepValue.toString())

        Num mixed = 10.0
        mixed += 2.5
        mixed -= 1.5
        mixed *= 2
        mixed /= 4
        global.println("Num mixed after += -= *= /= -> " + mixed.toString())
    }

    static memberAssignTest()
    {
        global.println("----- memberAssignTest -----")

        AssignBox box = AssignBox(){ value = 10, name = "member", flag = 0b0101 }
        box.value = 12
        box.value += 20
        box.value -= 5
        box.value *= 2
        box.value /= 3
        box.value %= 7
        box.value++
        box.value--

        box.flag &= 0b0110
        box.flag ^= 0b0011
        box.flag |= 0b1000
        box.flag <<= 1
        box.flag >>= 2

        box.score = 250
        box.name = "member-updated"

        global.println("box -> " + box.toString())
        global.println("box.total -> " + box.total.toString())

        # box.score += 1   # 负例：setter 仅支持 = 赋值
        # box.missing = 1  # 负例：不存在的成员不能作为左值
    }

    static arrayAssignTest()
    {
        global.println("----- arrayAssignTest -----")

        Int32[] values = new(4) { 10, 20, 30, 40 }
        values[0] = 15
        values.$1 += 5
        values[2] -= 7
        values.$3 *= 2
        values[0] /= 3
        values.$1 %= 6
        values[2]++
        values.$3--

        global.println("values[0] -> " + values[0].toString())
        global.println("values.$1 -> " + values.$1.toString())
        global.println("values[2] -> " + values[2].toString())
        global.println("values.$3 -> " + values.$3.toString())

        AssignBox[] boxes = new(2) { AssignBox(){ value = 1, name = "left" }, AssignBox(){ value = 2 } }
        boxes[0].value = 100
        boxes.$0.numbers.$1 = 222
        boxes[1] = new(){ value = 20, name = "brace-rebind", flag = 0b1111 }
        boxes.$1.value += 5
        boxes.$1.numbers[0] = 999

        global.println("boxes[0] -> " + boxes[0].toString())
        global.println("boxes.$1 -> " + boxes.$1.toString())
    }

    static objectInitAndReferenceAssignTest()
    {
        global.println("----- objectInitAndReferenceAssignTest -----")

        AssignBox left = new(){ value = 11, name = "left-init", flag = 0b0001 }
        AssignBox right = AssignBox(){ value = 22, name = "right-init", flag = 0b0010 }
        AssignBox alias = left

        alias.value = right.value + 100
        left = new(){ value = 33, name = "brace-left", flag = 0b0110 }
        right = AssignBox(){ value = left.value + alias.value, name = "copied", flag = alias.flag }

        global.println("left -> " + left.toString())
        global.println("right -> " + right.toString())
        global.println("alias -> " + alias.toString())
    }

    static genericAssignTest()
    {
        global.println("----- genericAssignTest -----")

        Level<int> lv = Level<int>(42)
        lv.t = 84
        global.println("Level<int> -> " + lv.toString())

        Level<AssignBox> wrapped = Level<AssignBox>(AssignBox(){ value = 5, name = "wrapped" })
        wrapped.t = AssignBox(){ value = lv.t, name = "generic-reassign", flag = 0b0100 }
        wrapped.t.score = 250
        wrapped.t.numbers.$2 = 333

        global.println("Level<AssignBox> -> " + wrapped.toString())
        global.println("wrapped.t -> " + wrapped.t.toString())
    }

    static fun()
    {
        global.println("========== AssignStatement (start) ==========")

        scalarAssignTest()
        memberAssignTest()
        arrayAssignTest()
        objectInitAndReferenceAssignTest()
        genericAssignTest()

        global.println("========== AssignStatement (end) ==========")
    }
}

# 测试面向：AssignStatement 支持的 =、+=、-=、*=、/=、%=、<<=、>>=、&=、^=、|=、++、--，以及局部变量、成员、数组下标、`$index`、setter、对象初始化、大括号重绑定、模板实例成员赋值。
# 预期：所有打印分支可顺利执行；setter 仅走 =；注释中的负例保持不启用，用于提示当前语义边界。
