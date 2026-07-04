
namespace SimpleLanguage.VM
{
    public class RuntimeMethod
    {
        public string id { get; set; } = "";
        public string virtualFunctionName { get; set; } = "";
        public string onlyFunctionName { get; set; } = "";
        public bool interfaceMethod => m_InterfaceMethod;
        public RuntimeClass ownerMetaClass => m_OwnerMetaClass;
        public List<RuntimeVariable> methodArgumentList => m_MethodArgumentList;
        public List<RuntimeVariable> methodLocalVariableList => m_MethodLocalVariableList;
        public List<RuntimeVariable> methodReturnVariableList => m_MethodReturnList;
        public List<Instruction> InstructionList => m_InstructionList;

        private List<RuntimeVariable> m_MethodArgumentList = new List<RuntimeVariable>();
        private List<RuntimeVariable> m_MethodLocalVariableList = new List<RuntimeVariable>();
        private List<RuntimeVariable> m_MethodReturnList = new List<RuntimeVariable>();
        private List<Instruction> m_InstructionList = new List<Instruction>();
        private RuntimeClass m_OwnerMetaClass = null;
        private bool m_InterfaceMethod = false;

        internal void SetOwner(RuntimeClass rc)
        {
            m_OwnerMetaClass = rc;
        }

        internal void SetInterfaceMethodFlag(bool v)
        {
            m_InterfaceMethod = v;
        }
    }
}
