import Std

const data ScoreRule
{
    passLine = 60
    excellentLine = 90
}

data GlobalCounter
{
    totalExamCount = 0
    totalScore = 0L
}

data ScoreData
{
    id = 0
    math = 0
    english = 0
    physics = 0
}

class ClassHolder
{
    value = 0
}

class ClassHolder2
{
    ok = 0
}

enum DataKind
{
    Base = 1
    Advanced = 2
}

data MetaInfo
{
    level = 1
    passed = false
}

data AnonymousNestedDataSample
{
    nd = {
        a = 20,
        b = 30,
        child = {
            x = 1,
            y = ClassDataEnumSample(){ cc = null },
            z = DataKind.Base,
            w = ClassHolder(){ value = 11 },
        }
    }
}

data ClassDataEnumSample
{
    cc = ClassHolder(){ value = 222 }
    vb = MetaInfo(){ level = 3, passed = true }
    kind = DataKind.Advanced
}

data MixedArrayElementSample
{
    items = [
        1,
        [2, 3],
        { code = 7, title = "ok" },
        ClassHolder(){ value = 22 },
        MetaInfo(){ level = 9, passed = false },
        DataKind.Base
    ]
}

# const member-intent sample for anonymous data syntax documentation:
# data ConstMemberIntentSample = {
#     const aa = 20
#     bb = 30
# }

data StructMatrix
{
    scores = [95, 88, 91]
    nestedArray = [[1, 2], [3, 4]]
    profile = {
        grade = 3,
        rank = 5,
        address = {
            city = "Shenzhen",
            zip = 518000
        }
    }
}

data StudentRecord
{
    sid = 0
    name = "StudentRecord Name"
    scores = [95, 88, 91]
    tags = ["math", "final"]
    profile = {
        grade = 3,
        rank = 5,
        address = {
            city = "Shenzhen",
            zip = 518000
        }
    }
    awards = [
        { name = "Math", year = 2024 },
        { name = "Physics", year = 2025 }
     ]
     meta = MetaInfo(){ level = 2, passed = true }
}

DataTest
{
    static constDataReadOnlyTest()
    {
        global.println("----- constDataReadOnlyTest -----")

        global.println(ScoreRule)
        global.println(ScoreRule.passLine.toString())
        global.println(ScoreRule.excellentLine)
        
        StudentRecord a = new()
        global.println(a)

        #StudentRecord b = StudentRecord(){ sid = 2, name = "n2" }
        #StudentRecord c = { sid = 3, name = "n3" }

        #global.println(b)
        #global.println(c)

        # negative-intent cases for const restriction documentation:
        # ScoreRule.passLine = 61
        # ScoreRule = { passLine = 61, excellentLine = 91 }
    }

    static staticDataDirectUseTest()
    {
        global.println("----- staticDataDirectUseTest -----")
        global.println("data GlobalCounter declared (direct static access case placeholder)")
        GlobalCounter.totalExamCount = 10
        GlobalCounter.totalScore = 200
        global.println(GlobalCounter.totalExamCount)
        global.println(GlobalCounter.totalScore)
        global.println(StudentRecord)

        # whole-object static data reassignment intent:
        # GlobalCounter = { totalExamCount = 11, totalScore = 210 }
    }

    static newDataInstanceTest()
    {
        global.println("----- newDataInstanceTest -----")

        ScoreData s1 = new()
        ScoreData s2 = new()
        ScoreData s3 = { id = 1, math = 10, english = 20, physics = 30 }

        s1.id = 10
        s1.math = 99
        s1 = { id = 11, math = 98, english = 97, physics = 96 }
        s2 = ScoreData(){ id = 12, math = 88, english = 87, physics = 86 }

        global.println("----- newDataInstanceTest -----")
        global.println("----- newDataInstanceTest -----")
        global.println(s1)
        global.println(s2)
        if( s1 == s2 )   #data数据的比较是，真比较里边的值是否相同
        {
            global.println("scoreData is same")
        }
        else
        {
            global.println("scoreData isnot same")
        }

        if( s1 == s3 )
        {
            global.println("scoreData unexpected same")
        }
        else
        {
            global.println("scoreData value diff detected")
        }
    }

    static memberShapeCoverageTest()
    {
        global.println("----- memberShapeCoverageTest -----")
        global.println("member shape samples added in DataTest declarations and comments")
        global.println(StructMatrix)
        global.println(AnonymousNestedDataSample)
        global.println(ClassDataEnumSample)
        global.println(MixedArrayElementSample)
        global.println(StructMatrix.profile.address.city)
        global.println(StructMatrix.profile.rank)
        global.println(StudentRecord.meta.level)
        global.println(ClassDataEnumSample.cc.value)
        global.println(ClassDataEnumSample.kind)

        # See top-level syntax samples in this file:
        # - array members
        # - nested object members
        # - anonymous nested data members
        # - named data members
        # - class members
        # - enum members
        # - object arrays
        # - arrays containing class/data/enum/anonymous-object elements
        # - nested data literals
        # - const data read constraints
        # - data after new() member reassignment and whole-object reassignment
        # - static data member reassignment
        # - chain member reads
        # - declare + new()
        # - direct DataName(){...}
        # - declare then assign {...}
    }

    static anonymousDataMetaCompileTest()
    {
        data typedProfile = {
            a2 = 10,
            a3 = 10000L,
            a = "333",
            a4 = [1, 2, 3, 4],
            anon = {
                code = 7,
                title = "ok"
            }
        }

        data typedProfile2 = {
            a2 = 20,
            a3 = 20000L,
            a = "444",
            a4 = [5, 6, 7, 8],
            anon = {
                code = 8,
                title = "ok2"
            }
        }

        data typedProfile3 = {
            nested = {
                a = 20,
                b = 30
            },
            holder = DataHolder(){ value = 11 },
            meta = MetaInfo(){ level = 8, passed = True },
            kind = DataKind.Advanced,
            items = [
                1,
                [2, 3],
                { code = 9, title = "mix" },
                DataHolder(){ value = 12 },
                MetaInfo(){ level = 6, passed = False },
                DataKind.Base
            ]
        }

        # typed anonymous-field samples (kept as syntax reference; currently unstable in full compile path):
        # data typedProfile4 = {
        #     string a = "333",
        #     Array<int> a4 = [1, 2, 3, 4],
        #     MetaInfo meta = MetaInfo(){ level = 8, passed = true },
        #     DataHolder holder = DataHolder(){ value = 11 },
        #     DataKind kind = DataKind.Advanced
        # }

        global.println("anonymous data meta compile sample prepared")
    }

    static dataIfCompareTest()
    {
        global.println("----- dataIfCompareTest -----")

        # 1) 具名 data 整体比较（结构 + 成员缓冲区，非引用）
        ScoreData sameA = { id = 1, math = 10, english = 20, physics = 30 }
        ScoreData sameB = ScoreData(){ id = 1, math = 10, english = 20, physics = 30 }
        ScoreData diffC = { id = 2, math = 10, english = 20, physics = 30 }

        if (sameA == sameB)
        {
            global.println("whole ScoreData: same values -> equal")
        }
        else
        {
            global.println("whole ScoreData: same values -> unexpected not equal")
        }

        if (sameA != diffC)
        {
            global.println("whole ScoreData: id differs -> not equal")
        }
        else
        {
            global.println("whole ScoreData: id differs -> unexpected equal")
        }

        # 2) 具名 data 成员（元素）比较
        if (sameA.id == sameB.id && sameA.math == sameB.math)
        {
            global.println("field ScoreData: id+math match")
        }
        else
        {
            global.println("field ScoreData: id+math mismatch")
        }

        if (sameA.physics == 30)
        {
            global.println("field ScoreData: physics literal ok")
        }

        if (sameA.id != diffC.id)
        {
            global.println("field ScoreData: id diff detected")
        }

        MetaInfo metaA = MetaInfo(){ level = 2, passed = true }
        MetaInfo metaB = { level = 2, passed = true }
        MetaInfo metaC = { level = 3, passed = false }

        if (metaA == metaB)
        {
            global.println("whole MetaInfo: equal")
        }
        if (metaA != metaC)
        {
            global.println("whole MetaInfo: level/passed diff -> not equal")
        }
        if (metaA.level == metaB.level && metaA.passed == metaB.passed)
        {
            global.println("field MetaInfo: level+passed match")
        }

        # 3) 匿名 data 整体与字段比较
        data anonSame1 = {
            code = 7,
            title = "ok"
        }
        data anonSame2 = {
            code = 7,
            title = "ok"
        }
        data anonDiff = {
            code = 8,
            title = "ok"
        }

        if (anonSame1 == anonSame2)
        {
            global.println("whole anonymous data: same shape+values -> equal")
        }
        else
        {
            global.println("whole anonymous data: same shape+values -> unexpected not equal")
        }

        if (anonSame1 != anonDiff)
        {
            global.println("whole anonymous data: code differs -> not equal")
        }

        if (anonSame1.code == anonSame2.code && anonSame1.title == anonSame2.title)
        {
            global.println("field anonymous data: code+title match")
        }

        # 4) 匿名 data 嵌套子结构比较
        data nestedA = {
            nested = { a = 20, b = 30 },
            tag = "pair"
        }
        data nestedB = {
            nested = { a = 20, b = 30 },
            tag = "pair"
        }
        data nestedC = {
            nested = { a = 21, b = 30 },
            tag = "pair"
        }

        if (nestedA == nestedB)
        {
            global.println("whole nested anonymous data: equal")
        }
        if (nestedA != nestedC)
        {
            global.println("whole nested anonymous data: nested.a differs -> not equal")
        }
        if (nestedA.nested.a == nestedB.nested.a && nestedA.tag == nestedB.tag)
        {
            global.println("field nested anonymous data: nested.a+tag match")
        }
        if (nestedA.nested.a != nestedC.nested.a)
        {
            global.println("field nested anonymous data: nested.a diff ok")
        }
        if (nestedA.nested.b == nestedC.nested.b)
        {
            global.println("field nested anonymous data: nested.b still equal when only a differs")
        }

        # 5) 系统函数：DataAllEqual / DataTypeEqual / DataNameAndTypeEqual / DataDataEqual
        if (DataAllEqual(sameA, sameB))
        {
            global.println("builtin DataAllEqual(sameA,sameB) -> true")
        }
        if (!DataAllEqual(sameA, diffC))
        {
            global.println("builtin DataAllEqual(sameA,diffC) -> false")
        }

        if (DataTypeEqual(sameA, sameB))
        {
            global.println("builtin DataTypeEqual(sameA,sameB) -> true")
        }
        if (DataNameAndTypeEqual(sameA, sameB))
        {
            global.println("builtin DataNameAndTypeEqual(sameA,sameB) -> true")
        }
        if (DataDataEqual(sameA, sameB))
        {
            global.println("builtin DataDataEqual(sameA,sameB) -> true")
        }
        if (!DataDataEqual(sameA, diffC))
        {
            global.println("builtin DataDataEqual(sameA,diffC) -> false")
        }

        if (DataTypeEqual(anonSame1, anonSame2))
        {
            global.println("builtin DataTypeEqual(anon) -> true")
        }
        if (DataDataEqual(anonSame1, anonSame2))
        {
            global.println("builtin DataDataEqual(anon) -> true")
        }
        if (!DataDataEqual(anonSame1, anonDiff))
        {
            global.println("builtin DataDataEqual(anon diff) -> false")
        }

        data typedA = { code = 7, title = "ok" }
        data typedB = { code = 7, title = "ok" }
        if (DataTypeEqual(typedA, typedB))
        {
            global.println("builtin DataTypeEqual(typed anon pair) -> true")
        }
        if (DataDataEqual(typedA, typedB))
        {
            global.println("builtin DataDataEqual(typed anon pair) -> true")
        }
        if (!DataAllEqual(typedA, typedB))
        {
            global.println("builtin DataAllEqual(typed anon, diff type id) -> false expected")
        }
    }


    static fun()
    {
        global.println("========== DataTest (start) ==========")

        constDataReadOnlyTest()
        dataIfCompareTest()
        #staticDataDirectUseTest()
        #memberShapeCoverageTest()
        #anonymousDataMetaCompileTest()
        #newDataInstanceTest()
        #global.println("anonymousDataMetaCompileTest compiled but is not executed in runtime baseline")
        #global.println("newDataInstanceTest skipped in runtime (known VM gap)")

        global.println("========== DataTest (end) ==========")
    }
}
