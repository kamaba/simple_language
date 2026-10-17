using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaModule : MetaBase
    {
        public MetaModule( string _name )
        {
            m_Name = _name;
            m_MetaNode = new MetaNode(this);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Module ");
            sb.AppendLine( m_Name );
            sb.AppendLine("{");
            foreach( var v in m_MetaNode.childrenMetaNodeDict)
            {
                sb.Append(v.Value.ToFormatString());
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
