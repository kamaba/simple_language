namespace OS
{
    # Timer 提供高精度时钟和延时功能，类似 Dart 的 Stopwatch、C# 的 Stopwatch、Swift 的 DispatchSourceTimer
    # 用法:
    #   OS.Timer t = new()
    #   t.start()
    #   ... 执行代码 ...
    #   Int64 elapsed = t.elapsed
    #   Console.println("耗时: " + elapsed.toString() + "ms")
    #
    #   OS.Timer.sleep(500)  # 延时 500ms
    #   Int64 now = OS.Timer.now()  # Unix 毫秒时间戳
    #   Int64 clock = OS.Timer.clock()  # 高精度单调时钟
    public class Timer
    {
        # 单调时钟起点（毫秒）
        Int64 _startMillis = 0
        # 停止时记录的终点
        Int64 _stopMillis = 0
        # 是否正在运行
        bool _isRunning = false

        # 高精度单调时钟（毫秒），用于基准测试
        # 类似 C# Stopwatch.GetTimestamp() / Dart DateTime.now().millisecondsSinceEpoch
        static Int64 clock()
        {
            ret SystemTimerClock()
        }

        # Unix 时间戳（毫秒）
        # 类似 C# DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / Dart DateTime.now().millisecondsSinceEpoch
        static Int64 now()
        {
            ret SystemTimerNowMillis()
        }

        # 睡眠指定毫秒数
        # 类似 C# Thread.Sleep(ms) / Dart Future.delayed / Swift Thread.sleep(for:)
        static void sleep(int milliseconds)
        {
            SystemSleep(milliseconds)
        }

        # 开始计时
        # 类似 C# Stopwatch.Start() / Dart Stopwatch.start()
        void start()
        {
            this._startMillis = SystemTimerClock()
            this._isRunning = true
        }

        # 停止计时
        # 类似 C# Stopwatch.Stop() / Dart Stopwatch.stop()
        void stop()
        {
            if this._isRunning
            {
                this._stopMillis = SystemTimerClock()
                this._isRunning = false
            }
        }

        # 重置计时器
        # 类似 C# Stopwatch.Reset() / Dart Stopwatch.reset()
        void reset()
        {
            this._startMillis = 0
            this._stopMillis = 0
            this._isRunning = false
        }

        # 重启计时器（等价于 reset + start）
        # 类似 C# Stopwatch.Restart()
        void restart()
        {
            this.reset()
            this.start()
        }

        # 已耗时（毫秒）
        # 如果正在运行，返回从 start 到当前的毫秒数
        # 如果已停止，返回从 start 到 stop 的毫秒数
        # 类似 C# Stopwatch.ElapsedMilliseconds / Dart Stopwatch.elapsedMilliseconds
        get Int64 elapsed()
        {
            if this._isRunning
            {
                ret SystemTimerClock() - this._startMillis
            }
            ret this._stopMillis - this._startMillis
        }

        # 是否正在运行
        # 类似 C# Stopwatch.IsRunning / Dart Stopwatch.isRunning
        get bool isRunning()
        {
            ret this._isRunning
        }
    }
}
