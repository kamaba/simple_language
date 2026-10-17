GotoLabel
{
    # fun 作为入口，统一调用所有 goto/label 分类测试
    static fun()
    {
        global.println("========== GotoLabelTest (start) ==========")

        backwardJumpLoopTest()
        forwardJumpSkipTest()
        multiLabelChainTest()
        gotoOutOfIfTest()
        gotoOutOfWhileTest()
        gotoLoopSumTest()
        forwardBackwardMixTest()
        labelNoGotoTest()

        global.println("========== GotoLabelTest (end) ==========")
    }

    # 1. 后向跳转: goto 到已执行过的 label, 实现循环 (1..5 累加)
    static backwardJumpLoopTest()
    {
        global.println("---------------backwardJumpLoopTest--------------")

        int i = 0
        int sum = 0
        label loopStart;
        i++
        sum += i
        if i < 5
        {
            goto loopStart;
        }
        global.println("backwardJump sum -> " + sum.toString())   # 15
    }

    # 2. 前向跳转: 跳过中间的赋值语句
    static forwardJumpSkipTest()
    {
        global.println("---------------forwardJumpSkipTest--------------")

        int a = 1
        goto skip;
        a = 100
        label skip;
        a += 2
        global.println("forwardJump a -> " + a.toString())   # 3
    }

    # 3. 多个标签链式跳转: l1 -> l2 -> l3, 中间的 a+=10 被跳过
    static multiLabelChainTest()
    {
        global.println("---------------multiLabelChainTest--------------")

        int a = 0
        label l1;
        a += 1
        if a == 1
        {
            goto l2;
        }
        a += 10
        label l2;
        if a == 1
        {
            goto l3;
        }
        label l3;
        a += 100
        global.println("multiLabel a -> " + a.toString())   # 101
    }

    # 4. if 块内 goto 跳出到函数级 label
    static gotoOutOfIfTest()
    {
        global.println("---------------gotoOutOfIfTest--------------")

        int a = 0
        if a == 0
        {
            goto endLabel;
        }
        a = 999
        label endLabel;
        global.println("gotoOutOfIf a -> " + a.toString())   # 0
    }

    # 5. while 循环内 goto 跳出循环
    static gotoOutOfWhileTest()
    {
        global.println("---------------gotoOutOfWhileTest--------------")

        int i = 0
        int hit = 0
        while i < 10
        {
            i++
            if i == 3
            {
                goto done;
            }
            hit++
        }
        label done;
        global.println("gotoOutOfWhile i -> " + i.toString())       # 3
        global.println("gotoOutOfWhile hit -> " + hit.toString())   # 2
    }

    # 6. 纯 goto 实现 1..10 求和循环
    static gotoLoopSumTest()
    {
        global.println("---------------gotoLoopSumTest--------------")

        int i = 1
        int sum = 0
        label head;
        sum += i
        i++
        if i <= 10
        {
            goto head;
        }
        global.println("gotoLoopSum sum -> " + sum.toString())   # 55
    }

    # 7. 前向 + 后向混合: 先 goto 循环累加, 再前向 goto 跳过 a+=1000
    static forwardBackwardMixTest()
    {
        global.println("---------------forwardBackwardMixTest--------------")

        int count = 0
        int a = 0
        label top;
        count++
        a += count
        if count < 3
        {
            goto top;
        }
        goto tail;
        a += 1000
        label tail;
        global.println("mix count -> " + count.toString())   # 3
        global.println("mix a -> " + a.toString())           # 6
    }

    # 8. label 未被任何 goto 引用 (label 指令应为 no-op)
    static labelNoGotoTest()
    {
        global.println("---------------labelNoGotoTest--------------")

        int a = 5
        label lonely;
        a *= 2
        global.println("labelNoGoto a -> " + a.toString())   # 10
    }
}
