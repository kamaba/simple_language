//****************************************************************************
//  File:      MetaNamespace.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: MetaNamespace's attribute
//****************************************************************************
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaNamespace : MetaBase
    {
        public MetaNamespace(string _name)
        {
            m_Name = _name;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("namespace ");
            sb.AppendLine(m_Name);
            sb.AppendLine("{");
            foreach (var v in m_MetaNode.childrenMetaNodeDict)
            {
                sb.Append(v.Value.ToFormatString());
            }
            sb.AppendLine("}");
            return sb.ToString();
        }    
    }
}
