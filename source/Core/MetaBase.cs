using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Text;

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
        public EPermission permission = EPermission.Public;
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


        protected string m_AllName = "";
        protected int m_Deep = 0;
        protected RefFromType m_RefFromType;
        protected string m_Name = "";
        protected int m_AnchorDeep = 0;
        protected MetaBase m_ParentNode = null;
        protected Dictionary<string, MetaBase> m_ChildrenNameNodeDict = new Dictionary<string, MetaBase>();
       
        public MetaBase()
        {
            m_Deep = 0;
            m_AnchorDeep = 0;
            m_RefFromType = RefFromType.Local;
        }
        public void SetRefFromType( RefFromType rft )
        {
            m_RefFromType = rft;
        }
        public void SetName(string _name)
        {
            m_Name = _name;
        }
        public virtual void SetDeep(int deep)
        {
            m_Deep = deep;
        }
        public virtual void SetAnchorDeep(int addep )
        {
            m_AnchorDeep = addep;
            foreach( var v in childrenNameNodeDict )
            {
                v.Value.SetAnchorDeep(addep);
            }
        }
        public virtual MetaBase GetChildrenMetaBaseByName( string name )
        {
            if (childrenNameNodeDict.ContainsKey(name))
                return childrenNameNodeDict[name];

            return null;
        }
        public virtual MetaBase GetMetaBaseInParentNodeContainByName(string inputname)
        {
            MetaBase findParentClassMB = null;
            MetaBase tmb2 = this.parentNode;
            while (tmb2 != null)
            {
                if (tmb2.childrenNameNodeDict.ContainsKey(inputname))
                {
                    findParentClassMB = tmb2.childrenNameNodeDict[inputname];
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

        public MetaBase GetMetaBaseInParentAndInChildrenMetaBaseByName(string inputname )
        {
            //子类
            MetaBase fmc = GetChildrenMetaBaseByName(inputname);
            if (fmc != null )
            {
                return fmc;
            }
            //上级节点类或者是命名空间
            fmc = GetMetaBaseInParentNodeContainByName(inputname);
            if (fmc != null)
            {
                return fmc;
            }
            return null;
        }
        public virtual bool IsIncludeMetaBase( string name )
        {
            return childrenNameNodeDict.ContainsKey(name);
        }
        public virtual bool AddMetaBase(string name, MetaBase mb)
        {
            if ( !childrenNameNodeDict.ContainsKey(name))
            {
                mb.m_ParentNode = this;
                mb.m_Deep = this.deep + 1;
                childrenNameNodeDict.Add(name, mb);
                return true;
            }
            return false;
        }
        public bool RemoveMetaBase( MetaBase mb )
        {
            string key = "";
            foreach( var v in childrenNameNodeDict )
            {
                if( v.Value == mb )
                {
                    key = v.Key;
                    break;
                }
            }
            if( string.IsNullOrEmpty( key ) )
            {
                childrenNameNodeDict.Remove(key);
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
