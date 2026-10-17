import Std;
import Core;

# ============================================================================
# CSharpTest3 — @csharp_mono(){} 内联块 WinForms 窗口 + HTTP json 请求回显
#
# 需求：全部用 C#（mono BCL）内置的窗口渲染能力——
#   1) 弹出一个简单窗口（System.Windows.Forms.Form，System.Drawing 字体）；
#   2) 窗口内先显示一段编译期固化的文字（Label 初始文本）；
#   3) 后台线程 HTTP GET 一个返回 json 的 URL（http://httpbin.org/get，
#   4) 响应文本经 BeginInvoke 送回 UI 线程，显示进窗口的文本控件（Label）；
#   5) 结果经出通道 $n（阶段标志和）回传 SL 侧断言。
#
# 实现要点（块整编契约 + mono BCL 约束）：
#   1) 代码段被包进方法体 —— 不能定义 C# 类，全部匿名方法内联；
#   2) WinForms 消息循环要求 STA —— 块内开专用子线程
#      SetApartmentState(STA) 跑 Application.Run，Join 等待收尾；
#   3) System.Threading 与 System.Windows.Forms 均含 Timer —— CS0104
#      歧义，必须全限定 System.Windows.Forms.Timer；
#   4) System.Windows.Forms.dll / System.Drawing.dll 不在 vendored BCL
#      最初三件套（mscorlib/System/System.Core）内 —— 已从官方 mono
#      6.12.0.206 发行包提取补进插件 BCL 树（lib/mono/4.5/）；编译期
#      靠 csc 默认 csc.rsp 引用 Framework 同名程序集（同标识 4.0.0.0，
#      显式 /r: mono 版反而 CS1703 重复导入），运行期 mono 经
#      assemblies_path 从插件 BCL 树解析加载；
#   5) 出通道首期仅支持 1 个：$n = 三个阶段标志之和（窗口已显示 1 +
#      HTTP 响应非空 2 + Label 已更新 4，全过 == 7）——各标志仅由单一线程
#      写入，Join 屏障后求和，避免并发读改写丢更新；HTTP 响应体在块内
#      Console.WriteLine 打印（VM 控制台/Result.txt 镜像可见）；
#   6) Timer 双保险自动关窗：Label 更新后 1.2s dwell 关窗 + 10s safety
#      兜底（保证测试进程可自动结束，不卡 CI）；
#   7) HTTP 失败容错：Label 显示错误文本（flags 缺 HTTP 位 -> 断言 FAIL）。
# 注意：本文件 SL 层禁用行内注释（Lexer 单行 # 注释会吞换行）。
# ============================================================================

class CSharpTest3
{
    # 断言计数（供 fun 尾部汇总）
    static int passed = 0
    static int failed = 0

    # 简单断言辅助：打印 PASS / FAIL
    static check( string name, bool cond )
    {
        if ( cond )
        {
            passed = passed + 1
            Console.println( "  [PASS] " + name )
        }
        else
        {
            failed = failed + 1
            Console.println( "  [FAIL] " + name )
        }
    }

    # ---------------- WinForms 窗口 + HTTP json 请求 + Label 回显 ----------------
    # 主流程（全部在 @csharp_mono(){} 块内完成，SL 侧只收结果）：
    #   STA 子线程建 Form + 全填充 Label（初始固化的文字）
    #   -> 窗口 Shown 后后台线程 HttpWebRequest GET http://httpbin.org/get
    #   -> form.BeginInvoke 回 UI 线程把响应文本写进 Label
    #   -> dwell/safety 双 Timer 自动关窗 -> Join 返回
    #   -> $n(标志和：1 窗口已显示 + 2 HTTP 响应非空 + 4 Label 已更新) 单出
    #      通道回传 SL 侧断言（块出通道首期仅支持 1 个，响应体在块内
    #      Console.WriteLine 打印 -> VM 控制台/Result.txt 镜像可见）
    static testWinFormsHttp()
    {
        Console.println( "===== CSharpTest3.testWinFormsHttp =====" )
        int n = 0
        @csharp_mono()
        {
            using System;
            using System.Drawing;
            using System.IO;
            using System.Net;
            using System.Text;
            using System.Threading;
            using System.Windows.Forms;

            // 阶段标志：各仅由单一线程写入（UI 线程 / HTTP 线程），
            // Join 屏障后求和 —— 1 窗口已显示 + 2 HTTP 响应非空 + 4 Label 已更新
            int shownFlag = 0;
            int httpFlag = 0;
            int labelFlag = 0;
            string json = "";

            Thread uiThread = new Thread(new ThreadStart(delegate
            {
                try
                {
                    Form form = new Form();
                    form.Text = "CSharpTest3 - WinForms + HTTP json";
                    form.Width = 600;
                    form.Height = 240;
                    form.StartPosition = FormStartPosition.CenterScreen;

                    // 文本控件（TextLabel）：初始显示一段编译期固化的文字
                    Label label = new Label();
                    label.Dock = DockStyle.Fill;
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    label.Font = new Font("Microsoft Sans Serif", 10.0f);
                    label.Text = "CSharpTest3: window created, HTTP GET http://httpbin.org/get ...";
                    form.Controls.Add(label);

                    // safety 兜底：10s 强制关窗（保证测试进程可自动结束）
                    System.Windows.Forms.Timer safety = new System.Windows.Forms.Timer();
                    safety.Interval = 10000;
                    safety.Tick += new EventHandler(delegate(object s, EventArgs e)
                    {
                        safety.Stop();
                        form.Close();
                    });
                    safety.Start();

                    // dwell：Label 更新后停留 1.2s 再关窗（让人能看清结果）
                    System.Windows.Forms.Timer dwell = new System.Windows.Forms.Timer();
                    dwell.Interval = 1200;
                    dwell.Tick += new EventHandler(delegate(object s, EventArgs e)
                    {
                        dwell.Stop();
                        form.Close();
                    });

                    // 窗口已显示标志 + 此时才派发 HTTP 请求：Shown 在 UI 线程
                    // 触发，句柄已建好 —— 之后 HTTP 线程的 BeginInvoke 不会在
                    // 无句柄的控件上抢先创建句柄（否则句柄会归属 HTTP 线程）
                    form.Shown += new EventHandler(delegate(object s, EventArgs e)
                    {
                        shownFlag = 1;
                        // 后台线程发 HTTP 请求（不阻塞 UI 消息循环）
                        ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state)
                        {
                            string text;
                            string prefix;
                            try
                            {
                                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://httpbin.org/get");
                                req.Method = "GET";
                                req.Timeout = 8000;
                                req.UserAgent = "SLang-CSharpTest3";
                                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                Stream stream = resp.GetResponseStream();
                                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                                text = reader.ReadToEnd();
                                reader.Close();
                                resp.Close();
                                prefix = "httpbin json (len " + text.Length.ToString() + "):";
                                if (text.Length > 0)
                                {
                                    httpFlag = 2;
                                }
                            }
                            catch (Exception ex)
                            {
                                text = "HTTP ERROR: " + ex.ToString();
                                prefix = "request failed:";
                            }
                            json = text;
                            // 响应送回 UI 线程写进 Label（TextLabel）
                            try
                            {
                                form.BeginInvoke(new MethodInvoker(delegate
                                {
                                    label.Text = prefix + Environment.NewLine + text;
                                    labelFlag = 4;
                                    dwell.Start();
                                }));
                            }
                            catch (Exception)
                            {
                            }
                        }));
                    });

                    Application.Run(form);
                }
                catch (Exception ex)
                {
                    json = "UI ERROR: " + ex.ToString();
                }
            }));
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiThread.Join();

            int n = shownFlag + httpFlag + labelFlag;
            Console.WriteLine("[CSharpTest3] flags=" + n.ToString() + " (shown=1 http=2 label=4), body=" + json);
            $n <- n;
        }
        Console.println( "WinFormsHttp flags = " + n.toString() )
        check( "winformsHttp: window shown(1) + http json fetched(2)", n == 3 || n == 7 )
        check( "winformsHttp: label updated with json body(4) -> all flags == 7", n == 7 )
    }

    static fun()
    {
        Console.println( "========== CSharpTest3 start ==========" )
        testWinFormsHttp()
        Console.println( "========== CSharpTest3 end: passed=" + passed.toString() + " failed=" + failed.toString() + " ==========" )
    }
}
