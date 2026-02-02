
namespace SimpleLanguage.VM.Runtime
{
    public class LocalRuntime
    {
        public static LocalRuntime s_Instance = null;
        public static LocalRuntime instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new LocalRuntime();
                }
                return s_Instance;
            }
        }
        static System.Collections.Generic.Stack<int> stackInt = new System.Collections.Generic.Stack<int>();

        // Bridge helpers that call into LocalRuntimeVM (which routes to Lib.* static helpers)
        public int ToInt32(object obj)
        {
            try
            {
                var res = SimpleLanguage.VM.Runtime.LocalRuntimeVM.Instance.Invoke("Num.ToInt32", obj);
                return res is int i ? i : Convert.ToInt32(res);
            }
            catch
            {
                return 0;
            }
        }

        public long ToInt64(object obj)
        {
            try
            {
                var res = SimpleLanguage.VM.Runtime.LocalRuntimeVM.Instance.Invoke("Num.ToInt64", obj);
                return res is long l ? l : Convert.ToInt64(res);
            }
            catch
            {
                return 0L;
            }
        }

        public double ToFloat64(object obj)
        {
            try
            {
                var res = SimpleLanguage.VM.Runtime.LocalRuntimeVM.Instance.Invoke("Num.ToFloat64", obj);
                return res is double d ? d : Convert.ToDouble(res);
            }
            catch
            {
                return 0.0;
            }
        }

        public bool ToBool(object obj)
        {
            try
            {
                var res = SimpleLanguage.VM.Runtime.LocalRuntimeVM.Instance.Invoke("Num.ToBool", obj);
                return res is bool b ? b : Convert.ToBoolean(res);
            }
            catch
            {
                return false;
            }
        }
    }
}
