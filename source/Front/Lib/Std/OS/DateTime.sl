namespace OS
{
    # DateTime 日期时间类型，API 参照 C# 的 System.DateTime 设计
    # 内部表示：Unix 纪元毫秒（Int64）+ 是否按 UTC 解释（bool）
    # 用法:
    #   import Std;
    #   OS.DateTime now = OS.DateTime.now()
    #   Console.println(now.toString())                     # 2026-08-25 14:30:00
    #   Console.println(now.toString("yyyy/MM/dd"))         # 2026/08/25
    #   OS.DateTime dt = OS.DateTime(2024, 2, 29, 12, 0, 0) # 指定分量构造（本地时区）
    #   OS.DateTime p = OS.DateTime.parse("2024-02-29T12:00:00")
    #   Console.println(dt.addDays(1).toString())           # 2024-03-01 12:00:00
    #   Console.println(OS.DateTime.daysInMonth(2024, 2))   # 29
    #   Console.println(OS.DateTime.isLeapYear(2024))       # true
    #
    # 说明:
    #   - dayOfWeek 取值 0..6，0=周日，与 C# DayOfWeek 枚举一致
    #   - parse 失败时得到 DateTime.MinValue（isValid 为 false），不抛异常
    #   - add* 系列返回新实例，不修改自身（与 C# 一致）
    #   - addMonths/addYears 遵循 C# 语义：目标月不足时"日"截断到月末
    public class DateTime
    {
        # Unix 纪元毫秒（内部唯一时刻状态）
        Int64 _unixMillis = 0
        # 是否按 UTC 解释分量（false 时按本地时区，类似 C# DateTimeKind）
        bool _isUtc = false
        # 是否为有效值（字符串解析失败时为 false）
        bool _isValid = false

        # ---------------------------------------------------------------
        # 构造函数
        # ---------------------------------------------------------------

        # 无参构造：DateTime.MinValue（0001-01-01 00:00:00）
        override _init_()
        {
            this._unixMillis = SystemDateTimeMinValueMillis()
            this._isUtc = false
            this._isValid = true
        }

        # 从 Unix 毫秒构造（本地时区语义）
        void _init_( Int64 unixMillis )
        {
            this._unixMillis = unixMillis
            this._isUtc = false
            this._isValid = true
        }

        # 从 Unix 毫秒构造并指定是否 UTC
        void _init_( Int64 unixMillis, bool isUtc )
        {
            this._unixMillis = unixMillis
            this._isUtc = isUtc
            this._isValid = true
        }

        # 从字符串解析构造（格式 "yyyy-M-d[ H:m:s[.fff]]"，也接受 'T' 分隔）
        # 解析失败得到 MinValue 且 isValid 为 false
        void _init_( string text )
        {
            Int64 m = SystemDateTimeTryParseMillis(text, 0)
            if SystemDateTimeIsValidMillis(m)
            {
                this._unixMillis = m
                this._isUtc = false
                this._isValid = true
            }
            else
            {
                this._unixMillis = SystemDateTimeMinValueMillis()
                this._isUtc = false
                this._isValid = false
            }
        }

        # 从年月日构造（本地时区零点）
        void _init_( int year, int month, int day )
        {
            this._unixMillis = SystemDateTimeMakeMillis(year, month, day, 0, 0, 0, 0, 0)
            this._isUtc = false
            this._isValid = true
        }

        # 从年月日时分秒构造（本地时区）
        void _init_( int year, int month, int day, int hour, int minute, int second )
        {
            this._unixMillis = SystemDateTimeMakeMillis(year, month, day, hour, minute, second, 0, 0)
            this._isUtc = false
            this._isValid = true
        }

        # 从年月日时分秒毫秒构造（本地时区）
        void _init_( int year, int month, int day, int hour, int minute, int second, int millisecond )
        {
            this._unixMillis = SystemDateTimeMakeMillis(year, month, day, hour, minute, second, millisecond, 0)
            this._isUtc = false
            this._isValid = true
        }

        # ---------------------------------------------------------------
        # 静态工厂 / 静态方法
        # ---------------------------------------------------------------

        # 当前本地时间
        # 类似 C# DateTime.Now
        static DateTime now()
        {
            ret DateTime(SystemDateTimeNowMillis(), false)
        }

        # 当前 UTC 时间（与 now() 表示同一时刻，分量按 UTC 换算）
        # 类似 C# DateTime.UtcNow
        static DateTime utcNow()
        {
            ret DateTime(SystemDateTimeNowMillis(), true)
        }

        # 今天零点（本地时区）
        # 类似 C# DateTime.Today
        static DateTime today()
        {
            var dt = DateTime(SystemDateTimeNowMillis(), false)
            ret dt.date
        }

        # 从 Unix 毫秒构造（本地时区语义）
        static DateTime fromUnixTimeMillis( Int64 millis )
        {
            ret DateTime(millis, false)
        }

        # 解析字符串，失败返回 MinValue（isValid 为 false）
        # 类似 C# DateTime.Parse（此处不抛异常）
        static DateTime parse( string text )
        {
            ret DateTime(text)
        }

        # 尝试解析字符串
        # 类似 C# DateTime.TryParse
        static bool tryParse( string text )
        {
            Int64 m = SystemDateTimeTryParseMillis(text, 0)
            ret SystemDateTimeIsValidMillis(m)
        }

        # 最小值 0001-01-01 00:00:00（类似 C# DateTime.MinValue）
        static DateTime minValue()
        {
            ret DateTime(SystemDateTimeMinValueMillis(), true)
        }

        # 最大值 9999-12-31 23:59:59.999（类似 C# DateTime.MaxValue）
        static DateTime maxValue()
        {
            ret DateTime(SystemDateTimeMaxValueMillis(), true)
        }

        # 某年某月的天数
        # 类似 C# DateTime.DaysInMonth
        static int daysInMonth( int year, int month )
        {
            ret SystemDateTimeDaysInMonth(year, month)
        }

        # 是否闰年
        # 类似 C# DateTime.IsLeapYear
        static bool isLeapYear( int year )
        {
            ret SystemDateTimeIsLeapYear(year)
        }

        # ---------------------------------------------------------------
        # 属性
        # ---------------------------------------------------------------

        # Unix 纪元毫秒
        get Int64 unixTimeMillis()
        {
            ret this._unixMillis
        }

        # Unix 纪元秒
        get Int64 unixTimeSeconds()
        {
            ret this._unixMillis / 1000
        }

        # 是否按 UTC 解释分量
        get bool isUtc()
        {
            ret this._isUtc
        }

        # 是否有效值（字符串解析失败时为 false）
        get bool isValid()
        {
            ret this._isValid
        }

        # 年
        get int year()
        {
            ret SystemDateTimeGetYear(this._unixMillis, this.utcFlag())
        }

        # 月（1..12）
        get int month()
        {
            ret SystemDateTimeGetMonth(this._unixMillis, this.utcFlag())
        }

        # 日（1..31）
        get int day()
        {
            ret SystemDateTimeGetDay(this._unixMillis, this.utcFlag())
        }

        # 时（0..23）
        get int hour()
        {
            ret SystemDateTimeGetHour(this._unixMillis, this.utcFlag())
        }

        # 分（0..59）
        get int minute()
        {
            ret SystemDateTimeGetMinute(this._unixMillis, this.utcFlag())
        }

        # 秒（0..59）
        get int second()
        {
            ret SystemDateTimeGetSecond(this._unixMillis, this.utcFlag())
        }

        # 毫秒（0..999）
        get int millisecond()
        {
            ret SystemDateTimeGetMillisecond(this._unixMillis, this.utcFlag())
        }

        # 星期几（0=周日 1=周一 ... 6=周六，与 C# DayOfWeek 枚举一致）
        get int dayOfWeek()
        {
            ret SystemDateTimeGetDayOfWeek(this._unixMillis, this.utcFlag())
        }

        # 星期几英文名
        get string dayOfWeekName()
        {
            int dow = this.dayOfWeek
            if dow == 0
            {
                ret "Sunday"
            }
            if dow == 1
            {
                ret "Monday"
            }
            if dow == 2
            {
                ret "Tuesday"
            }
            if dow == 3
            {
                ret "Wednesday"
            }
            if dow == 4
            {
                ret "Thursday"
            }
            if dow == 5
            {
                ret "Friday"
            }
            ret "Saturday"
        }

        # 一年中的第几天（1..366）
        get int dayOfYear()
        {
            ret SystemDateTimeGetDayOfYear(this._unixMillis, this.utcFlag())
        }

        # 当天零点（保留时区语义）
        # 类似 C# DateTime.Date
        get DateTime date()
        {
            ret DateTime(this.year, this.month, this.day)
        }

        # ---------------------------------------------------------------
        # 加减运算（返回新实例，不修改自身）
        # ---------------------------------------------------------------

        # 加年（负数为减）
        DateTime addYears( int years )
        {
            ret this.addMonths(years * 12)
        }

        # 加月（负数为减；目标月天数不足时"日"截断到月末，与 C# 一致）
        DateTime addMonths( int months )
        {
            Int64 m = SystemDateTimeAddMonths(this._unixMillis, months, this.utcFlag())
            ret DateTime(m, this._isUtc)
        }

        # 加天（负数为减）
        DateTime addDays( int days )
        {
            Int64 ms = days
            ret this.addMilliseconds(ms * 86400000)
        }

        # 加小时（负数为减）
        DateTime addHours( int hours )
        {
            Int64 ms = hours
            ret this.addMilliseconds(ms * 3600000)
        }

        # 加分钟（负数为减）
        DateTime addMinutes( int minutes )
        {
            Int64 ms = minutes
            ret this.addMilliseconds(ms * 60000)
        }

        # 加秒（负数为减）
        DateTime addSeconds( int seconds )
        {
            Int64 ms = seconds
            ret this.addMilliseconds(ms * 1000)
        }

        # 加毫秒（负数为减）
        DateTime addMilliseconds( Int64 millis )
        {
            ret DateTime(this._unixMillis + millis, this._isUtc)
        }

        # ---------------------------------------------------------------
        # 比较 / 输出
        # ---------------------------------------------------------------

        # 比较两个时刻：小于返回 -1，相等返回 0，大于返回 1
        # 类似 C# DateTime.CompareTo
        Int64 compareTo( DateTime other )
        {
            Int64 om = other.unixTimeMillis
            if this._unixMillis < om
            {
                ret -1
            }
            if this._unixMillis > om
            {
                ret 1
            }
            ret 0
        }

        # 默认格式化：yyyy-MM-dd HH:mm:ss
        override string toString()
        {
            ret this.toString("yyyy-MM-dd HH:mm:ss")
        }

        # 自定义格式化
        # 支持 token: yyyy yy MM M dd d HH H hh h mm m ss s fff ff f tt
        # 单字母标准格式: d D t T f F g G s u o R（简化映射，见文档）
        string toString( string format )
        {
            ret SystemDateTimeFormat(this._unixMillis, format, this.utcFlag())
        }

        # 日期部分：yyyy-MM-dd
        string toDateString()
        {
            ret this.toString("yyyy-MM-dd")
        }

        # 时间部分：HH:mm:ss
        string toTimeString()
        {
            ret this.toString("HH:mm:ss")
        }

        # ---------------------------------------------------------------
        # 内部辅助
        # ---------------------------------------------------------------

        # _isUtc 转 Int32 标志（系统调用参数用）
        Int32 utcFlag()
        {
            if this._isUtc
            {
                ret 1
            }
            ret 0
        }
    }
}
