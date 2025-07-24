using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaMemberVariableCSharp : MetaMemberVariable
    {
        public PropertyInfo propertyInfo;
        public FieldInfo fieldInfo;

        public MetaMemberVariableCSharp(MetaClass mc, FieldInfo fi):base(mc, fi.Name)
        {
            fieldInfo = fi;
            m_FromType = EFromType.CSharp;
            string typeName = MetaTypeCSharp.GetClassNameByCSharpType( fi.DeclaringType );
            var defineMetaClassType = ClassManager.instance.GetClassByName(typeName);
            m_DefineMetaType = new MetaType(defineMetaClassType);
        }
        public MetaMemberVariableCSharp(MetaClass mc, PropertyInfo pi) : base(mc, pi.Name)
        {
            m_Name = pi.Name;
            propertyInfo = pi;
            m_FromType = EFromType.CSharp;
            var defineMetaClassType = ClassManager.instance.GetClassByName(pi.DeclaringType.Name);
            if( defineMetaClassType == null )
            {
                string[] fullname = pi.DeclaringType.FullName.Split(".");
                MetaNode fmc = ModuleManager.instance.csharpModule.metaNode;
                for( int i = 0; i < fullname.Length; i++ )
                {
                    var cfmc = fmc.GetChildrenMetaNodeByName(fullname[i]);
                    if(cfmc != null )
                    {
                        fmc = cfmc;
                        continue;
                    }
                    fmc = CSharpManager.FindAndCreateMetaNode(fmc, fullname[i]);
                }
                defineMetaClassType = fmc.GetMetaClassByTemplateCount(0);
            }

            m_DefineMetaType = new MetaType(defineMetaClassType);

            SetOwnerMetaClass(mc);
        }
    }
}
