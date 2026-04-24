
namespace SimpleLanguage.VM
{
    public class RuntimeVariable
    {
        public RuntimeDefType runtimeDefType => m_RuntimeDefType;
        public int id => m_Id;
        public int index => m_Index;
        public string name => m_Name;
        public DebugInfo? debugInfo => m_DebugInfo;


        private RuntimeDefType m_RuntimeDefType = null;
        private string m_Name = "";
        private int m_Index = 0;
        private int m_Id = 0;
        private DebugInfo? m_DebugInfo = null;
        public RuntimeVariable() { }
        public RuntimeVariable(RuntimeDefType rdt, int id = 0, int index = -1, string name = "", DebugInfo? debugInfo = null)
        {
            this.m_RuntimeDefType = rdt;
            this.m_Id = id;
            this.m_Index = index;
            this.m_Name = name ?? string.Empty;
            this.m_DebugInfo = debugInfo;
        }
    }
}
