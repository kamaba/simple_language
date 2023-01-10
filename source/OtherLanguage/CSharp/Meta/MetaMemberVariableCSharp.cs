using SimpleLanguage.CSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaMemberData
    {

    }
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
            m_DefineMetaType = new MetaType(defineMetaClassType);

            SetOwnerMetaClass(mc);
        }
    }
}
