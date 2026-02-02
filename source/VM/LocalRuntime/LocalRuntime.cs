
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

        static Stack<int> stackInt = new Stack<int>();
    }
}
