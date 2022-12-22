using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaModule
    {
        public MetaBase GetCSharpMetaClassOrNamespaceAndCreateByName( string name )
        {
            var mb = this.GetChildrenMetaBaseByName(name);

            if( mb == null && refFromType == RefFromType.CSharp )
            {
                mb = CSharpManager.FindAndCreateMetaBase( this, name);

                return mb;
            }

            return mb;
        }
    }
}
