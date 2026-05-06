BoolTest
{
    static compareTest()
    {
        global.println("----- compareTest -----")

        Num a = 3.14
        Num b = 2.5
        
        global.println("a > b  = " + (a > b).toString())
        global.println("a < b  = " + (a < b).toString())
        global.println("a == b = " + (a == b).toString())
        global.println("a != b = " + (a != b).toString())
        global.println("a >= b = " + (a >= b).toString())
        global.println("a <= b = " + (a <= b).toString())
    }

    static boolLiteralAndNotTest()
    {
        global.println("----- boolLiteralAndNotTest -----")

        bool t = true
        bool f = false
        bool nt = !t
        bool nf = !f

        global.println("true  -> " + t.toString())
        global.println("false -> " + f.toString())
        global.println("!true -> " + nt.toString())
        global.println("!false -> " + nf.toString())
    }

    static logicOperatorTest()
    {
        global.println("----- logicOperatorTest -----")

        bool a = true
        bool b = false

        global.println("a && b -> " + (a && b).toString())
        global.println("a || b -> " + (a || b).toString())
        global.println("(a && !b) || (b && !a) -> " + ((a && !b) || (b && !a)).toString())

        # && 优先级高于 ||
        global.println("true || false && false -> " + (true || false && false).toString())
        global.println("(true || false) && false -> " + ((true || false) && false).toString())
    }

    static boolFromCompareAndConditionTest()
    {
        global.println("----- boolFromCompareAndConditionTest -----")

        Int32 x = 12
        Int32 y = 7
        bool c1 = x > y
        bool c2 = x == y
        bool c3 = x >= 10 && x <= 20

        global.println("x > y -> " + c1.toString())
        global.println("x == y -> " + c2.toString())
        global.println("x in [10,20] -> " + c3.toString())

        if c3 && !c2
        {
            global.println("if(c3 && !c2) -> true branch")
        }
        else
        {
            global.println("if(c3 && !c2) -> false branch")
        }
    }

    static boolAssignFlowTest()
    {
        global.println("----- boolAssignFlowTest -----")

        bool ok = false
        Int32 score = 86

        ok = score >= 60
        global.println("ok = score >= 60 -> " + ok.toString())

        ok = ok && score < 100
        global.println("ok = ok && score < 100 -> " + ok.toString())

        ok = !ok
        global.println("ok = !ok -> " + ok.toString())

        ok = !ok || false
        global.println("ok = !ok || false -> " + ok.toString())
    }

    static fun()
    {
        global.println("========== BoolTest (start) ==========")
        compareTest()
        boolLiteralAndNotTest()
        logicOperatorTest()
        boolFromCompareAndConditionTest()
        boolAssignFlowTest()
        global.println("========== BoolTest (end) ==========")
    }
}
