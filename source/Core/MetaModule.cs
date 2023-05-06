using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaModule : MetaBase
    {
        public List<MetaNamespace> m_MetaNamespaceList = new List<MetaNamespace>();
        public List<MetaClass> m_MetaClassList = new List<MetaClass>();

        public MetaModule( string _name )
        {
            m_Name = _name;
        }
        public override void SetDeep(int deep)
        {
            base.SetDeep(deep);
            foreach( var v in m_MetaNamespaceList )
            {
                v.SetDeep(deep + 1);
            }
            foreach( var v in m_MetaClassList )
            {
                v.SetDeep(deep + 1);
            }
        }
        public void AddMetaNamespace(MetaNamespace namespaceName )
        {
            m_MetaNamespaceList.Add(namespaceName);

            AddMetaBase(namespaceName.name, namespaceName);
        }
        public void AddMetaClass( MetaClass mc )
        {
            m_MetaClassList.Add(mc);

            AddMetaBase(mc.name, mc);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("module " + name + Environment.NewLine + "{"  + Environment.NewLine );
            foreach (var v in childrenNameNodeDict)
            {
                MetaBase mb = v.Value;
                if( mb is MetaNamespace )
                {
                    sb.Append((mb as MetaNamespace).ToFormatString());
                    sb.Append(Environment.NewLine);
                }
                else if (mb is MetaClass)
                {
                    sb.Append(mb.ToFormatString());
                    sb.Append(Environment.NewLine);
                }
                else
                {
                    sb.Append("Errrrrroooorrr ---" + mb.ToFormatString());
                    sb.Append(Environment.NewLine);
                }
            }
            sb.Append("}");

            return sb.ToString();
        }
    }
}
