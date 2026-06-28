
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
        //public static RuntimeDefType CreateIRMetaTypeByGenTemplateMetaTypeList( RuntimeDefType type, RuntimeClass ownerIRMc)
        //{
        //    /*
        //    RuntimeDefType irmt = new();
        //    irmt.m_OwnerRuntimeClass = RuntimeClassManager.instance.GetRuntimeClassById(ownerIRMc.id);

        //    var gtmc = type.runtimeClass;
        //    if (type.isTemplate )
        //    {
        //        irmt.m_TemplateIndex = type.templateIndex;
        //    }
        //    irmt.m_RuntimeClass = RuntimeClassManager.instance.GetRuntimeClassById(type.runtimeClass.id );

        //    var lits = type.GetGenTemplateMetaTypeList();
        //    for (int i = 0; i < lits.Count; i++)
        //    {
        //        irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(lits[i], irmt.m_IROwnerMetaClass));
        //    }
        //    //for (int i = 0; i < type.genTemplateMetaTypeList.Count; i++)
        //    //{
        //    //    irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(type.genTemplateMetaTypeList[i], irmt.m_IROwnerMetaClass));
        //    //}
        //    if (irmt.m_IRMetaClass == null || irmt.m_IROwnerMetaClass == null)
        //    {
        //        Debug.Assert(false, "这个不可以为空!");
        //    }
        //    return irmt;
        //    */
        //    return null;
        //}
        //public static RuntimeDefType CreateIRMetaTypeByDefineTemplateMetaTypeList(RuntimeDefType type, RuntimeClass ownerIRMc)
        //{
        //    /*
        //    IRMetaType irmt = new();
        //    //irmt.m_IsArray = type.IsArray();
        //    irmt.m_IROwnerMetaClass = IRManager.instance.GetIRMetaClassById(ownerIRMc.id);
        //    if (type.eType == EMetaTypeType.MetaClass)
        //    {
        //        irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
        //    }
        //    else if (type.eType == EMetaTypeType.Template)
        //    {
        //        irmt.m_TemplateIndex = type.metaTemplate.index;
        //        irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
        //    }
        //    else
        //    {
        //        irmt.m_IRMetaClass = IRManager.instance.GetIRMetaClassById(type.GetTemplateMetaClass().GetHashCode());
        //    }

        //    for (int i = 0; i < type.defineTemplateMetaTypeList.Count; i++)
        //    {
        //        irmt.m_IRMetaTypeList.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(type.defineTemplateMetaTypeList[i], irmt.m_IROwnerMetaClass));
        //    }
        //    if (irmt.m_IRMetaClass == null || irmt.m_IROwnerMetaClass == null)
        //    {
        //        Debug.Assert(false, "这个不可以为空!");
        //    }
        //    return irmt;
        //    */
        //    return null;
        //}
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.runtimeClass.name);

            return sb.ToString();
        }
    }
}
