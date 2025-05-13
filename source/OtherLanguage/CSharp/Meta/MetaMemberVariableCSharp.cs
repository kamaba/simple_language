using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
//    public partial class MetaMemberData
//    {

//    }
    public partial class MetaMemberVariable
    {
        public PropertyInfo propertyInfo;
        public FieldInfo fieldInfo;

        public MetaMemberVariable(MetaClass mc, FieldInfo fi)
        {
            m_Name = fi.Name;
            fieldInfo = fi;
            m_FromType = EFromType.CSharp;
            string typeName = MetaType.GetClassNameByCSharpType( fi.DeclaringType );
            var defineMetaClassType = ClassManager.instance.GetClassByName(typeName);
            m_DefineMetaType = new MetaType(defineMetaClassType);

            SetOwnerMetaClass(mc);
        }
        public MetaMemberVariable(MetaClass mc, PropertyInfo pi)
        {
            m_Name = pi.Name;
            propertyInfo = pi;
            m_FromType = EFromType.CSharp;
            var defineMetaClassType = ClassManager.instance.GetClassByName(pi.DeclaringType.Name);
            if( defineMetaClassType == null )
            {
                string[] fullname = pi.DeclaringType.FullName.Split(".");
                MetaBase fmc = ModuleManager.instance.csharpModule;
                for( int i = 0; i < fullname.Length; i++ )
                {
                    var cfmc = fmc.GetChildrenMetaBaseByName(fullname[i]);
                    if(cfmc != null )
                    {
                        fmc = cfmc;
                        continue;
                    }
                    fmc = CSharpManager.FindAndCreateMetaBase(fmc, fullname[i]);
                }
                defineMetaClassType = fmc as MetaClass;
            }

            m_DefineMetaType = new MetaType(defineMetaClassType);

            SetOwnerMetaClass(mc);
        }
    }
}
