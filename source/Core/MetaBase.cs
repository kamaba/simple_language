//****************************************************************************
//  File:      MetaBase.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2025/5/17 12:00:00
//  Description:  Core MetaBase is a basement class, attribute value has name or tree's deepvalue or tree struct node!
//****************************************************************************

using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public enum RefFromType
    {
        Local,
        CSharp,
        Javascript,
    }
    public class MetaBase
    {
        public EPermission permission => m_Permission;
        public virtual string name => m_Name;
        public int deep => m_Deep;
        public int realDeep
        {
            get
            {
                return m_Deep - anchorDeep;
            }
        }
        public RefFromType refFromType => m_RefFromType;
        public int anchorDeep => m_AnchorDeep;
        public MetaBase parentNode => m_ParentNode;
        public Dictionary<string, MetaBase> childrenNameNodeDict => m_ChildrenNameNodeDict;
        public virtual string allName
        {
            get
            {
                if (string.IsNullOrEmpty(m_AllName))
                {
                    m_AllName = m_ParentNode != null && !(m_ParentNode is MetaModule) ? parentNode.allName + "." + name : name;
                }
                return m_AllName;
            }
        }
        public string allNameIncludeModule
        {
            get
            {
                return m_ParentNode != null ? m_ParentNode.allNameIncludeModule + "." + name : name;
            }
        }


        protected EPermission m_Permission = EPermission.Public;
        protected RefFromType m_RefFromType = RefFromType.Local;
        protected string m_Name = "";
        protected string m_AllName = "";
        protected int m_Deep = 0;
        protected int m_AnchorDeep = 0;
        protected MetaBase m_ParentNode = null;
        protected Dictionary<string, MetaBase> m_ChildrenNameNodeDict = new Dictionary<string, MetaBase>();

        public MetaBase()
        {
            m_Deep = 0;
            m_AnchorDeep = 0;
            m_RefFromType = RefFromType.Local;
        }
        public MetaBase( MetaBase mb )
        {
            m_Name = mb.m_Name;
            m_AllName = mb.m_AllName;
            m_Deep = mb.m_Deep;
            m_AnchorDeep = mb.m_AnchorDeep;
            m_RefFromType = mb.m_RefFromType;
            m_ParentNode = mb.m_ParentNode;
            m_ChildrenNameNodeDict = mb.m_ChildrenNameNodeDict;
            m_Permission = mb.m_Permission;
        }
        public void SetRefFromType(  RefFromType type )
        {
            this.m_RefFromType = type;
        }
        public void SetName( string _name )
        {
            m_Name = name;
        }
        public virtual void SetDeep(int deep)
        {
            m_Deep = deep;
        }
        public virtual void SetAnchorDeep(int addep )
        {
            m_AnchorDeep = addep;
            foreach( var v in m_ChildrenNameNodeDict)
            {
                v.Value.SetAnchorDeep(addep);
            }
        }
        public virtual MetaBase GetChildrenMetaBaseByName( string name )
        {
            if (m_ChildrenNameNodeDict.ContainsKey(name))
                return m_ChildrenNameNodeDict[name];

            return null;
        }
        //该函数，只为调试效果时候使用，在编译逻辑里边不体现！
        public virtual MetaBase GetMetaBaseInParentNodeContainByName(string inputname)
        {
            MetaBase findParentClassMB = null;
            MetaBase tmb2 = this.parentNode;
            while (tmb2 != null)
            {
                if (tmb2.m_ChildrenNameNodeDict.ContainsKey(inputname))
                {
                    findParentClassMB = tmb2.m_ChildrenNameNodeDict[inputname];
                    break;
                }
                if (tmb2.parentNode == null) break;
                tmb2 = tmb2.parentNode;
            }
            return findParentClassMB;
        }
        public virtual MetaBase GetMetaBaseInParentByName(string inputname, bool isInclude = true)
        {
            if (m_Name == inputname && isInclude)
                return this;
            MetaBase findParentClassMB = null;
            MetaBase tmb2 = this.parentNode;
            while (tmb2 != null)
            {
                if (tmb2.m_Name == inputname )
                {
                    findParentClassMB = tmb2;
                    break;
                }
                if (tmb2.parentNode == null) break;
                tmb2 = tmb2.parentNode;
            }
            return findParentClassMB;
        }
        public virtual bool IsIncludeMetaBase( string name )
        {
            return m_ChildrenNameNodeDict.ContainsKey(name);
        }
        public virtual bool AddMetaBase(string name, MetaBase mb)
        {
            if ( !m_ChildrenNameNodeDict.ContainsKey(name))
            {
                mb.m_ParentNode = this;
                mb.m_Deep = this.deep + 1;
                m_ChildrenNameNodeDict.Add(name, mb);
                return true;
            }
            return false;
        }
        public bool RemoveMetaBase( MetaBase mb )
        {
            string key = "";
            foreach( var v in m_ChildrenNameNodeDict)
            {
                if( v.Value == mb )
                {
                    key = v.Key;
                    break;
                }
            }
            if( string.IsNullOrEmpty( key ) )
            {
                m_ChildrenNameNodeDict.Remove(key);
                return true;
            }
            return false;
        }
        public virtual string GetFormatString()
        {
            return "";
        }
        public virtual string ToFormatString()
        {
            return allName;
        }
    }
}
