//****************************************************************************
//  File:      MetaNamespace.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************


using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaNamespace : MetaBase
    {
        public bool isNotAllowCreateName { get; set; } = false;
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
        //public override int GetHashCode()
        //{
        //    return base.GetHashCode();
        //}
        //public override bool Equals(object obj)
        //{
        //    return base.Equals(obj); 
        //}
        //public static bool operator == ( Namespace lhs, Namespace rhs )
        //{
        //    if( lhs.nameStack.Count == rhs.nameStack.Count )
        //    {
        //        var las = lhs.nameStack.ToArray();
        //        var ras = lhs.nameStack.ToArray();
        //        for ( int i = 0; i < las.Length; i++ )
        //        {
        //            if (las[i] != ras[i])
        //                return false;
        //        }
        //        return true;
        //    }
        //    return lhs.Equals( rhs );
        //}
        //public static bool operator !=(Namespace lhs, Namespace rhs)
        //{
        //    if (lhs.nameStack.Count == rhs.nameStack.Count)
        //    {
        //        var las = lhs.nameStack.ToArray();
        //        var ras = lhs.nameStack.ToArray();
        //        for (int i = 0; i < las.Length; i++)
        //        {
        //            if (las[i] != ras[i])
        //                return true;
        //        }
        //        return false;
        //    }
        //    return !lhs.Equals(rhs);
        //}        
    }
}
