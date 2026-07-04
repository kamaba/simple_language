
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeDefType
    {
        public RuntimeClass runtimeClass => m_RuntimeClass;
        public RuntimeClass ownerRuntimeClass => m_OwnerRuntimeClass;
        public List<RuntimeDefType> runtimeDefTypeList => m_RuntimeDefTypeList;
        public int templateIndex => m_TemplateIndex;

        private RuntimeClass m_RuntimeClass = null;
        private RuntimeClass m_OwnerRuntimeClass = null;
        private List<RuntimeDefType> m_RuntimeDefTypeList = new List<RuntimeDefType>();
        private int m_TemplateIndex = -1;

        public RuntimeDefType()
        {

        }
        public RuntimeDefType(RuntimeClass _irMetaClass)
        {
            m_RuntimeClass = _irMetaClass;
        }
        public RuntimeDefType(RuntimeClass irmc, List<RuntimeDefType> irlist)
        {
            m_RuntimeClass = irmc;
            m_RuntimeDefTypeList = irlist;
        }

        /// <summary>
        /// Full wire shape from <see cref="SimpleLanuageVM.Load.SLRuntimeDefTypePackage"/> (owner + template slot + nested args).
        /// </summary>
        public RuntimeDefType(RuntimeClass runtimeClass, List<RuntimeDefType> irlist, RuntimeClass? ownerRuntimeClass, int templateIndex )
        {
            m_RuntimeClass = runtimeClass;
            m_RuntimeDefTypeList = irlist ?? new List<RuntimeDefType>();
            m_OwnerRuntimeClass = ownerRuntimeClass;
            m_TemplateIndex = templateIndex;
            //m_IsTemplate = isTemplate;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.runtimeClass.name);

            return sb.ToString();
        }
    }
}
