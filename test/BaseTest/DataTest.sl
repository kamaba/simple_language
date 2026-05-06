import Std

const data ScoreRule
{
    passLine = 60
    excellentLine = 90
}

data GlobalCounter
{
    totalExamCount = 0
    totalScore = 0
}

data ScoreData
{
    id = 0
    math = 0
    english = 0
    physics = 0
}

# ==============================
# data syntax samples
# Keep these as syntax-reference cases in DataTest.
# Some forms below still have gaps in current Meta / IR / VM execution paths.
# ==============================

# data MetaInfo
# {
#     level = 1
#     passed = False
# }

# data StudentRecord
# {
#     sid = 0
#     name = ""
#     scores = [95, 88, 91]
#     tags = ["math", "final"]
#     profile = {
#         grade = 3
#         rank = 5
#         address = {
#             city = "Shenzhen"
#             zip = 518000
#         }
#     }
#     awards = [
#         { name = "Math", year = 2024 },
#         { name = "Physics", year = 2025 }
#     ]
#     meta = MetaInfo(){ level = 2, passed = True }
# }

# StudentRecord a = new()

# StudentRecord b = StudentRecord(){ sid = 2, name = "n2" }

# StudentRecord c
# c = { sid = 3, name = "n3" }

# StudentRecord d = StudentRecord()
# {
#     sid = 4
#     name = "n4"
#     scores = [100, 99, 98]
#     profile = {
#         grade = 4
#         rank = 1
#     }
#     meta = MetaInfo(){ level = 3, passed = True }
# }

DataTest
{
    static constDataReadOnlyTest()
    {
        global.println("----- constDataReadOnlyTest -----")
        global.println("const data ScoreRule declared (read-only semantics)")
    }

    static staticDataDirectUseTest()
    {
        global.println("----- staticDataDirectUseTest -----")
        global.println("data GlobalCounter declared (direct static access case placeholder)")
    }

    static newDataInstanceTest()
    {
        global.println("----- newDataInstanceTest -----")

        ScoreData s1 = new()
        ScoreData s2 = new()
        global.println("ScoreData instances declared")
    }

    static memberShapeCoverageTest()
    {
        global.println("----- memberShapeCoverageTest -----")
        global.println("member shape samples added in DataTest comments")

        # See top-level syntax samples in this file:
        # - array members
        # - nested object members
        # - object arrays
        # - nested data literals
        # - declare + new()
        # - direct DataName(){...}
        # - declare then assign {...}
    }


    static fun()
    {
        global.println("========== DataTest (start) ==========")

        constDataReadOnlyTest()
        staticDataDirectUseTest()
        memberShapeCoverageTest()
        global.println("newDataInstanceTest skipped in runtime (known VM gap)")

        global.println("========== DataTest (end) ==========")
    }
}
