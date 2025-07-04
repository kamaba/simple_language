using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaModuleCSharp : MetaModule
    {
        public MetaModuleCSharp(string _name) : base(_name)
        {
        }

        public MetaClass GetCSharpMetaClassOrNamespaceAndCreateByName( string name )
        {
            //var mb = this.GetChildrenMetaBaseByName(name);

            //if( mb == null && refFromType == RefFromType.CSharp )
            //{
            //    mb = CSharpManager.FindAndCreateMetaBase( this, name);

            //    return mb;
            //}

            //return mb;
            return null;
        }
    }
}
