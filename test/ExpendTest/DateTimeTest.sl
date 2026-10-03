import Std;

DateTimeTest
{
    # 简单断言辅助：打印 PASS / FAIL
    static check(string name, bool cond)
    {
        if cond
        {
            Console.println("  [PASS] " + name)
        }
        else
        {
            Console.println("  [FAIL] " + name)
        }
    }

    # 测试构造函数族
    static testConstructors()
    {
        Console.println("===== DateTimeTest.testConstructors =====")
        # 年月日构造
        var d1 = OS.DateTime(2024, 2, 29)
        Console.println("DateTime(2024,2,29)            = " + d1.toString())
        check("year==2024", d1.year == 2024)
        check("month==2", d1.month == 2)
        check("day==29", d1.day == 29)
        check("hour==0", d1.hour == 0)
        # 年月日时分秒构造
        var d2 = OS.DateTime(2024, 2, 29, 12, 30, 45)
        Console.println("DateTime(2024,2,29,12,30,45)   = " + d2.toString())
        check("hour==12", d2.hour == 12)
        check("minute==30", d2.minute == 30)
        check("second==45", d2.second == 45)
        # 年月日时分秒毫秒构造
        var d3 = OS.DateTime(2024, 2, 29, 12, 30, 45, 123)
        Console.println("含毫秒构造                     = " + d3.toString("yyyy-MM-dd HH:mm:ss.fff"))
        check("millisecond==123", d3.millisecond == 123)
        # 字符串解析构造
        var d4 = OS.DateTime("2024-06-15 08:09:10")
        Console.println("DateTime(\"2024-06-15 08:09:10\") = " + d4.toString())
        check("字符串构造 year==2024", d4.year == 2024)
        check("字符串构造 month==6", d4.month == 6)
        check("字符串构造 day==15", d4.day == 15)
        # Unix 毫秒构造
        Int64 zero = 0
        var d5 = OS.DateTime(zero)
        Console.println("DateTime(0).unixTimeMillis = " + d5.unixTimeMillis.toString())
        check("unixTimeMillis==0", d5.unixTimeMillis == 0)
        Console.println("DateTime(0) 本地时间 = " + d5.toString())
    }

    # 测试静态 now / utcNow / today
    static testNow()
    {
        Console.println("===== DateTimeTest.testNow =====")
        var now = OS.DateTime.now()
        var utc = OS.DateTime.utcNow()
        var today = OS.DateTime.today()
        Console.println("now()    = " + now.toString("yyyy-MM-dd HH:mm:ss.fff"))
        Console.println("utcNow() = " + utc.toString("yyyy-MM-dd HH:mm:ss.fff"))
        Console.println("today()  = " + today.toString())
        check("now() 有效", now.isValid)
        # now 与 utcNow 是两次独立取时，毫秒可能相差几毫秒，只要求同一时刻（双向差值 < 5 秒）
        check("now 与 utcNow 表示同一时刻(now-utc<5秒)", now.unixTimeMillis - utc.unixTimeMillis < 5000)
        check("now 与 utcNow 表示同一时刻(utc-now<5秒)", utc.unixTimeMillis - now.unixTimeMillis < 5000)
        check("today() hour==0", today.hour == 0)
        check("today() minute==0", today.minute == 0)
        check("today() second==0", today.second == 0)
        check("today() 与 now() 同年", today.year == now.year)
        check("today() 与 now() 同月", today.month == now.month)
        check("today() 与 now() 同日", today.day == now.day)
    }

    # 测试 addDays
    static testAddDays()
    {
        Console.println("===== DateTimeTest.testAddDays =====")
        var d = OS.DateTime(2024, 2, 28)
        var a = d.addDays(1)
        Console.println("2024-02-28 + 1 天 = " + a.toString())
        check("闰年 2 月底 +1 天仍 2 月", a.month == 2)
        check("闰年 2 月底 +1 天日==29", a.day == 29)
        var b = d.addDays(2)
        Console.println("2024-02-28 + 2 天 = " + b.toString())
        check("跨月到 3 月", b.month == 3)
        check("跨月日==1", b.day == 1)
        var base2 = OS.DateTime(2023, 2, 28)
        var c = base2.addDays(1)
        Console.println("2023-02-28 + 1 天 = " + c.toString())
        check("平年 2 月底 +1 天到 3 月", c.month == 3)
        check("平年 2 月底 +1 天日==1", c.day == 1)
        var base3 = OS.DateTime(2024, 12, 31)
        var e = base3.addDays(1)
        Console.println("2024-12-31 + 1 天 = " + e.toString())
        check("跨年到 2025", e.year == 2025)
        check("跨年日==1", e.day == 1)
        # 负数减天
        var f = d.addDays(-28)
        Console.println("2024-02-28 - 28 天 = " + f.toString())
        check("减 28 天到 1 月", f.month == 1)
        check("减 28 天日==31", f.day == 31)
    }

    # 测试 addMonths / addYears（C# 语义：目标月天数不足时日截断到月末）
    static testAddMonths()
    {
        Console.println("===== DateTimeTest.testAddMonths =====")
        var jan31 = OS.DateTime(2024, 1, 31)
        var r1 = jan31.addMonths(1)
        Console.println("2024-01-31 + 1 月 = " + r1.toString())
        check("闰年截断月==2", r1.month == 2)
        check("闰年截断日==29", r1.day == 29)
        var jan31b = OS.DateTime(2025, 1, 31)
        var r2 = jan31b.addMonths(1)
        Console.println("2025-01-31 + 1 月 = " + r2.toString())
        check("平年截断日==28", r2.day == 28)
        var feb29 = OS.DateTime(2024, 2, 29)
        var r3 = feb29.addYears(1)
        Console.println("2024-02-29 + 1 年 = " + r3.toString())
        check("addYears 截断日==28", r3.day == 28)
        check("addYears 年==2025", r3.year == 2025)
        var r4 = feb29.addMonths(-12)
        Console.println("2024-02-29 - 12 月 = " + r4.toString())
        check("减 12 月年==2023", r4.year == 2023)
        check("减 12 月日==28", r4.day == 28)
        var mar31 = OS.DateTime(2024, 3, 31)
        var r5 = mar31.addMonths(-1)
        Console.println("2024-03-31 - 1 月 = " + r5.toString())
        check("减 1 月到 2 月", r5.month == 2)
        check("减 1 月日==29", r5.day == 29)
        # 加 0 个月不变
        var r6 = jan31.addMonths(0)
        check("加 0 个月时刻不变", r6.unixTimeMillis == jan31.unixTimeMillis)
        # add* 返回新实例不修改自身
        check("addMonths 不修改自身", jan31.day == 31)
    }

    # 测试 addHours / addMinutes / addSeconds / addMilliseconds
    static testAddTime()
    {
        Console.println("===== DateTimeTest.testAddTime =====")
        var d = OS.DateTime(2024, 2, 29, 23, 59, 59)
        var s = d.addSeconds(1)
        Console.println("2024-02-29 23:59:59 + 1 秒 = " + s.toString())
        check("秒进位跨日", s.day == 1)
        check("秒进位到 3 月", s.month == 3)
        check("秒归零", s.second == 0)
        var baseH = OS.DateTime(2024, 2, 29, 23, 0, 0)
        var h = baseH.addHours(1)
        Console.println("2024-02-29 23:00:00 + 1 时 = " + h.toString())
        check("时进位跨日", h.day == 1)
        check("时归零", h.hour == 0)
        var baseM = OS.DateTime(2024, 2, 29, 23, 59, 0)
        var m = baseM.addMinutes(1)
        Console.println("2024-02-29 23:59:00 + 1 分 = " + m.toString())
        check("分进位跨日", m.day == 1)
        check("分归零", m.minute == 0)
        var baseMs = OS.DateTime(2024, 2, 29, 12, 30, 45)
        var ms = baseMs.addMilliseconds(1000)
        Console.println("12:30:45 + 1000ms = " + ms.toString("yyyy-MM-dd HH:mm:ss.fff"))
        check("+1000ms 秒进位", ms.second == 46)
        check("+1000ms 毫秒归零", ms.millisecond == 0)
        # 负数减时
        var back = d.addHours(-24)
        Console.println("2024-02-29 23:59:59 - 24 时 = " + back.toString())
        check("减 24 时回到 2-28", back.day == 28)
        check("addSeconds 不修改自身", d.second == 59)
    }

    # 测试 date 属性（当天零点）
    static testDateGetter()
    {
        Console.println("===== DateTimeTest.testDateGetter =====")
        var d = OS.DateTime(2024, 2, 29, 15, 30, 45)
        var dateOnly = d.date
        Console.println("date = " + dateOnly.toString())
        check("date 保留年", dateOnly.year == 2024)
        check("date 保留月", dateOnly.month == 2)
        check("date 保留日", dateOnly.day == 29)
        check("date 时间归零 hour==0", dateOnly.hour == 0)
        check("date 时间归零 minute==0", dateOnly.minute == 0)
        check("date 时间归零 second==0", dateOnly.second == 0)
    }

    # 测试 parse / tryParse
    static testParse()
    {
        Console.println("===== DateTimeTest.testParse =====")
        var t = OS.DateTime.parse("2024-02-29T12:00:00")
        Console.println("parse(\"2024-02-29T12:00:00\") = " + t.toString())
        check("T 分隔解析有效", t.isValid)
        check("T 分隔 year==2024", t.year == 2024)
        check("T 分隔 hour==12", t.hour == 12)
        var loose = OS.DateTime.parse("2024-2-9 1:2:3")
        Console.println("parse(\"2024-2-9 1:2:3\") = " + loose.toString())
        check("宽松格式月==2", loose.month == 2)
        check("宽松格式日==9", loose.day == 9)
        check("宽松格式时==1", loose.hour == 1)
        check("宽松格式分==2", loose.minute == 2)
        check("宽松格式秒==3", loose.second == 3)
        var bad = OS.DateTime.parse("not-a-date")
        Console.println("parse(\"not-a-date\") isValid = " + bad.isValid)
        check("无效字符串 isValid==false", bad.isValid == false)
        check("无效字符串退化为 MinValue year==1", bad.year == 1)
        check("tryParse 无效返回 false", OS.DateTime.tryParse("not-a-date") == false)
        check("tryParse 有效返回 true", OS.DateTime.tryParse("2024-01-01"))
    }

    # 测试格式化输出
    static testFormat()
    {
        Console.println("===== DateTimeTest.testFormat =====")
        var d = OS.DateTime(2024, 2, 29, 15, 6, 9, 123)
        Console.println("默认 toString()     = " + d.toString())
        check("默认格式", d.toString() == "2024-02-29 15:06:09")
        Console.println("yyyy/MM/dd          = " + d.toString("yyyy/MM/dd"))
        check("斜杠格式", d.toString("yyyy/MM/dd") == "2024/02/29")
        Console.println("dd/MM/yyyy HH:mm:ss = " + d.toString("dd/MM/yyyy HH:mm:ss"))
        check("日月年格式", d.toString("dd/MM/yyyy HH:mm:ss") == "29/02/2024 15:06:09")
        Console.println("yy-M-d              = " + d.toString("yy-M-d"))
        check("短格式", d.toString("yy-M-d") == "24-2-29")
        Console.println("含毫秒              = " + d.toString("yyyy-MM-dd HH:mm:ss.fff"))
        check("毫秒格式", d.toString("yyyy-MM-dd HH:mm:ss.fff") == "2024-02-29 15:06:09.123")
        Console.println("12 小时制 hh:mm tt  = " + d.toString("hh:mm tt"))
        check("12 小时制", d.toString("hh:mm tt") == "03:06 PM")
        Console.println("toDateString()      = " + d.toDateString())
        check("toDateString", d.toDateString() == "2024-02-29")
        Console.println("toTimeString()      = " + d.toTimeString())
        check("toTimeString", d.toTimeString() == "15:06:09")
    }

    # 测试 isLeapYear / daysInMonth
    static testLeapYearAndDaysInMonth()
    {
        Console.println("===== DateTimeTest.testLeapYearAndDaysInMonth =====")
        Console.println("isLeapYear(2024) = " + OS.DateTime.isLeapYear(2024))
        check("2024 是闰年", OS.DateTime.isLeapYear(2024))
        check("2023 是平年", OS.DateTime.isLeapYear(2023) == false)
        check("1900 是平年（百年不闰）", OS.DateTime.isLeapYear(1900) == false)
        check("2000 是闰年（四百年再闰）", OS.DateTime.isLeapYear(2000))
        check("1600 是闰年", OS.DateTime.isLeapYear(1600))
        Console.println("daysInMonth(2024, 2) = " + OS.DateTime.daysInMonth(2024, 2))
        check("2024 年 2 月 29 天", OS.DateTime.daysInMonth(2024, 2) == 29)
        check("2023 年 2 月 28 天", OS.DateTime.daysInMonth(2023, 2) == 28)
        check("2024 年 1 月 31 天", OS.DateTime.daysInMonth(2024, 1) == 31)
        check("2024 年 4 月 30 天", OS.DateTime.daysInMonth(2024, 4) == 30)
        check("2024 年 12 月 31 天", OS.DateTime.daysInMonth(2024, 12) == 31)
    }

    # 测试 dayOfWeek / dayOfWeekName / dayOfYear
    static testDayOfWeek()
    {
        Console.println("===== DateTimeTest.testDayOfWeek =====")
        var d = OS.DateTime(2024, 2, 29)
        Console.println("2024-02-29 是 " + d.dayOfWeekName + " (dayOfWeek=" + d.dayOfWeek + ")")
        check("2024-02-29 是周四(4)", d.dayOfWeek == 4)
        check("2024-02-29 dayOfWeekName==Thursday", d.dayOfWeekName == "Thursday")
        var ny = OS.DateTime(2024, 1, 1)
        Console.println("2024-01-01 是 " + ny.dayOfWeekName + " (dayOfWeek=" + ny.dayOfWeek + ")")
        check("2024-01-01 是周一(1)", ny.dayOfWeek == 1)
        var y2k = OS.DateTime(2000, 1, 1)
        Console.println("2000-01-01 是 " + y2k.dayOfWeekName + " (dayOfWeek=" + y2k.dayOfWeek + ")")
        check("2000-01-01 是周六(6)", y2k.dayOfWeek == 6)
        var eoy = OS.DateTime(2024, 12, 31)
        Console.println("2024-12-31 dayOfYear = " + eoy.dayOfYear)
        check("闰年最后一天 dayOfYear==366", eoy.dayOfYear == 366)
        var eoy2 = OS.DateTime(2023, 12, 31)
        check("平年最后一天 dayOfYear==365", eoy2.dayOfYear == 365)
        var mid = OS.DateTime(2024, 3, 1)
        check("2024-03-01 dayOfYear==61", mid.dayOfYear == 61)
    }

    # 测试 compareTo
    static testCompare()
    {
        Console.println("===== DateTimeTest.testCompare =====")
        var a = OS.DateTime(2024, 1, 1)
        var b = OS.DateTime(2024, 6, 1)
        var c = OS.DateTime(2024, 1, 1)
        Int64 r1 = a.compareTo(b)
        Int64 r2 = b.compareTo(a)
        Int64 r3 = a.compareTo(c)
        Console.println("2024-01-01 compareTo 2024-06-01 = " + r1.toString())
        Console.println("2024-06-01 compareTo 2024-01-01 = " + r2.toString())
        Console.println("2024-01-01 compareTo 2024-01-01 = " + r3.toString())
        check("早于返回 -1", r1 == -1)
        check("晚于返回 1", r2 == 1)
        check("相等返回 0", r3 == 0)
    }

    # 测试 Unix 毫秒往返 / unixTimeSeconds
    static testUnixMillis()
    {
        Console.println("===== DateTimeTest.testUnixMillis =====")
        var src = OS.DateTime(2024, 2, 29, 12, 0, 0)
        Int64 raw = src.unixTimeMillis
        var copy = OS.DateTime.fromUnixTimeMillis(raw)
        Console.println("src  = " + src.toString())
        Console.println("copy = " + copy.toString())
        check("Unix 毫秒往返相等", copy.unixTimeMillis == raw)
        Console.println("unixTimeSeconds = " + copy.unixTimeSeconds.toString())
        Int64 expectSec = raw / 1000
        check("unixTimeSeconds == millis / 1000", copy.unixTimeSeconds == expectSec)
    }

    # 测试 MinValue / MaxValue
    static testMinMax()
    {
        Console.println("===== DateTimeTest.testMinMax =====")
        var mv = OS.DateTime.minValue()
        var xv = OS.DateTime.maxValue()
        Console.println("MinValue = " + mv.toString())
        Console.println("MaxValue = " + xv.toString("yyyy-MM-dd HH:mm:ss.fff"))
        check("MinValue 默认格式", mv.toString() == "0001-01-01 00:00:00")
        check("MaxValue 毫秒格式", xv.toString("yyyy-MM-dd HH:mm:ss.fff") == "9999-12-31 23:59:59.999")
        check("MinValue 有效", mv.isValid)
        check("MaxValue 有效", xv.isValid)
        Int64 cmp = mv.compareTo(xv)
        check("MinValue 小于 MaxValue", cmp < 0)
    }

    static fun()
    {
        Console.println("========== DateTimeTest 开始 ==========")
        testConstructors()
        testNow()
        testAddDays()
        testAddMonths()
        testAddTime()
        testDateGetter()
        testParse()
        testFormat()
        testLeapYearAndDaysInMonth()
        testDayOfWeek()
        testCompare()
        testUnixMillis()
        testMinMax()
        Console.println("========== DateTimeTest 结束 ==========")
    }
}
