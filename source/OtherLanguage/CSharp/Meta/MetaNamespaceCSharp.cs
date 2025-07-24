using SimpleLanguage.CSharp;

namespace SimpleLanguage.Core
{
    public sealed class MetaNamespaceCSharp : MetaNamespace 
    {
        public MetaNamespaceCSharp(string _name) : base(_name)
        {
            m_RefFromType = RefFromType.CSharp;
        }
        public MetaNode GetChildrenMetaNodeByName(string name )
        {
            MetaNode mb = base.metaNode.GetChildrenMetaNodeByName(name);

            if (mb != null)
            {
                return mb;
            }
            mb = CSharpManager.FindCSharpClassOrNameSpace(this.name, name);

            if (mb != null)
            {
                //this.AddMetaBase(name, mb);
                if (mb.IsMetaClass() )
                {
                    //ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.csharpModule);
                }
            }

            return mb;
        }

        public MetaNode GetCSharpMetaClassOrNamespaceAndCreateByName(string name)
        {
            var mb = this.m_MetaNode.GetChildrenMetaNodeByName(name);

            if (mb == null && refFromType == RefFromType.CSharp)
            {
                //mb = CSharpManager.FindAndCreateMetaBase(this, name);

                return mb;
            }

            return mb;
        }
    }
}
