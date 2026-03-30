
namespace SimpleLanguage.VM
{
    public class RuntimeVariable
    {
        public RuntimeDefType runtimeDefType => m_RuntimeDefType;
        public int id { get; set; }
        public int index { get; set; }
        public string name { get; set; } = string.Empty;
        public bool isConst { get; set; } = false;

        private RuntimeDefType m_RuntimeDefType = null;
        public RuntimeVariable() { }
        public RuntimeVariable(RuntimeDefType rdt, int id = 0, int index = -1, string name = "")
        {
            m_RuntimeDefType = rdt;
            this.id = id;
            this.index = index;
            this.name = name ?? string.Empty;
            this.isConst = false;
        }
    }
}
