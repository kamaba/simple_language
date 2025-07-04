//****************************************************************************
//  File:      MetaNamespace.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************


using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public class MetaNamespace : MetaBase
    {
        public bool isNotAllowCreateName { get; set; } = false;
        public string namespaceName
        {
            get
            {
                if(m_NamespaceName == null )
                {
                    Stack<MetaNamespace> mnstack = new Stack<MetaNamespace>();
                    MetaNamespace mn = this;
                    mnstack.Push(this);
                    //while ( true )
                    //{
                    //    if (mn.parentMetaNamespace != null)
                    //    {
                    //        mn = mn.parentMetaNamespace;
                    //        mnstack.Push(mn);
                    //    }
                    //    else
                    //        break;
                    //}
                    //while( true )
                    //{
                    //    mn = mnstack.Pop();
                    //    m_NamespaceName = (m_NamespaceName + mn.name);
                    //    if (mnstack.Count > 0)
                    //        m_NamespaceName = m_NamespaceName + ".";
                    //    else
                    //        break;
                    //}
                }
                return m_NamespaceName;
            }
        }
        private string m_NamespaceName = null;
        public MetaNamespace(string _name)
        {
            m_Name = _name;
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
