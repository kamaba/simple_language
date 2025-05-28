using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace SimpleLanguage.Core
{
    public sealed class MetaNamespaceCSharp : MetaNamespace 
    {
        public MetaNamespaceCSharp(string _name) : base(_name)
        {
            m_RefFromType = RefFromType.CSharp;
        }
        public override MetaBase GetChildrenMetaBaseByName(string name )
        {
            MetaBase mb = base.GetChildrenMetaBaseByName(name); 

            if( mb != null )
            {
                return mb;
            }
            mb = CSharpManager.FindCSharpClassOrNameSpace(this.allName, name);

            if( mb != null )
            {
                this.AddMetaBase(name, mb);
                if( mb is MetaClass mc )
                {
                    ClassManager.instance.AddMetaClass(mc, ModuleManager.instance.csharpModule);
                }
            }

            return mb;
        }

        public MetaBase GetCSharpMetaClassOrNamespaceAndCreateByName( string name )
        {
            var mb = this.GetChildrenMetaBaseByName(name);

            if( mb == null && refFromType == RefFromType.CSharp )
            {
                mb = CSharpManager.FindAndCreateMetaBase(this, name);

                return mb;
            }

            return mb;
        }
    }
}
