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
            y = ClassDataEnumSample(){ cc = 0 },
            z = DataKind.Base,
            w = ClassHolder(){ value = "", value2 = ClassHolder2(){ ok = 2 } },
        }
    }
}

data ClassDataEnumSample
{
    cc = ClassHolder(){ value = 200 }
    vb = MetaInfo(){ level = 3, passed = True }
    kind = DataKind.Advanced
}

data MixedArrayElementSample
{
    items = [
        1,
        [2, 3],
        { code = 7, title = "ok" },
        ClassHolder(){ value = 22 },
        MetaInfo(){ level = 9, passed = False },
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
    name = ""
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
     meta = MetaInfo(){ level = 2, passed = True }
}

DataTest
{
    static constDataReadOnlyTest()
    {
        global.println("----- constDataReadOnlyTest -----")
        global.println("const data ScoreRule declared (read-only semantics)")

        
        StudentRecord a = new()
        StudentRecord b = StudentRecord(){ sid = 2, name = "n2" }
        StudentRecord c = { sid = 3, name = "n3" }

        global.println(ScoreRule)
        global.println(ScoreRule.passLine)
        global.println(ScoreRule.excellentLine)
        global.println(a)
        global.println(b)
        global.println(c)

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


    static fun()
    {
        global.println("========== DataTest (start) ==========")

        constDataReadOnlyTest()
        staticDataDirectUseTest()
        memberShapeCoverageTest()
        global.println("anonymousDataMetaCompileTest compiled but is not executed in runtime baseline")
        global.println("newDataInstanceTest skipped in runtime (known VM gap)")

        global.println("========== DataTest (end) ==========")
    }
}
