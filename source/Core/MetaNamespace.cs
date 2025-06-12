//****************************************************************************
//  File:      MetaNamespace.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/30 12:00:00
//  Description: Meta enum's attribute
//****************************************************************************


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaNamespace : MetaBase
    {
        public List<MetaClass> metaClassList
        {
            get
            {
                List<MetaClass> list = new List<MetaClass>();
                foreach( var v in m_MetaClassDict.Values )
                {
                    list.Add( v );
                }
                return list;
            }
        }

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
                    while ( true )
                    {
                        if (mn.parentMetaNamespace != null)
                        {
                            mn = mn.parentMetaNamespace;
                            mnstack.Push(mn);
                        }
                        else
                            break;
                    }
                    while( true )
                    {
                        mn = mnstack.Pop();
                        m_NamespaceName = (m_NamespaceName + mn.name);
                        if (mnstack.Count > 0)
                            m_NamespaceName = m_NamespaceName + ".";
                        else
                            break;
                    }
                }
                return m_NamespaceName;
            }
        }
        public MetaNamespace parentMetaNamespace
        {
            get
            {
                if (parentNode == null) return null;
                return parentNode as MetaNamespace;
            }
        }
        private string m_NamespaceName = null;
        private Dictionary<string, MetaNamespace> m_MetaNamespaceDict = new Dictionary<string, MetaNamespace>();
        private Dictionary<string, MetaClass> m_MetaClassDict = new Dictionary<string, MetaClass>();
        public MetaNamespace(string _name)
        {
            m_Name = _name;
        }
        public void AddMetaNamespace(MetaNamespace mn)
        {
            m_MetaNamespaceDict.Add(mn.name, mn);

            AddMetaBase(mn.name, mn);
        }
        public void AddMetaClass( MetaClass mc )
        {
            m_MetaClassDict.Add(mc.name, mc);

            AddMetaBase(mc.name, mc);
        }
        public void PrintAllNamespace()
        {
            Debug.Write("---------------NamespaceBegin-----------" + Environment.NewLine);
            Debug.Write(ToAllNamespace());
            Debug.Write("--------------NamespaceEnd-------------");
        }
        public string ToAllNamespace()
        {
            StringBuilder sb = new StringBuilder();
            if( isNotAllowCreateName )
            {
                sb.Append("[NoAllowCreate]");
            }
            foreach (var v in m_ChildrenNameNodeDict)
            {
                sb.Append("namespace " + v.Key + Environment.NewLine );
            }
            return sb.ToString();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("namespace " + name + Environment.NewLine );
            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("{" + Environment.NewLine);

            foreach (var v in m_ChildrenNameNodeDict)
            {
                MetaBase mb = v.Value;
                if (mb is MetaNamespace)
                {
                    sb.Append((mb as MetaNamespace).ToFormatString());
                    sb.Append(Environment.NewLine);
                }
                else if (mb is MetaClass)
                {
                    sb.Append( (mb as MetaClass).ToFormatString() );
                    sb.Append(Environment.NewLine);
                }
                else
                {
                    sb.Append("Errrrrroooorrr ---" + mb.ToFormatString());
                    sb.Append(Environment.NewLine);
                }
            }

            for (int i = 0; i < deep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}" + Environment.NewLine);

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
