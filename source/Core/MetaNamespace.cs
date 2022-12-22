using SimpleLanguage.Compile;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaNamespace : MetaBase
    {
        private string m_NamespaceName = null;

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
        public MetaNamespace(string _name)
        {
            m_Name = _name;
        }
        public Dictionary<string, MetaNamespace> m_MetaNamespaceDict = new Dictionary<string, MetaNamespace>();
        public Dictionary<string, MetaClass> m_MetaClassDict = new Dictionary<string, MetaClass>();

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
        public MetaBase GetMetaBaseByAllName( string allName )
        {
            List<string> list = new List<string>();
            if(CompilerUtil.CheckNameList( allName, list ) )
            {
                MetaBase findMB = this;
                for( int i = 0; i < list.Count; i++ )
                {
                    findMB = findMB.GetChildrenMetaBaseByName(list[i]);
                    if (findMB == null) break;
                }
                return findMB;
            }


            return null;
        }
        public void PrintAllNamespace()
        {
            Console.Write("---------------NamespaceBegin-----------" + Environment.NewLine);
            Console.Write(ToAllNamespace());
            Console.Write("--------------NamespaceEnd-------------");
        }
        public string ToAllNamespace()
        {
            StringBuilder sb = new StringBuilder();
            if( isNotAllowCreateName )
            {
                sb.Append("[NoAllowCreate]");
            }
            foreach (var v in childrenNameNodeDict )
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

            foreach (var v in childrenNameNodeDict)
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
