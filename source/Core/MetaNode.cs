//****************************************************************************
//  File:      MetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Meta class's attribute
//****************************************************************************

using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum EStructNodeType
    {
        Module,
        Namespace,
        Class,
        Data,
        Enum
    }
    public class MetaNode
    {
        public string name => m_Name;
        public int deep => m_Deep;
        public int realDeep
        {
            get
            {
                return m_Deep - anchorDeep;
            }
        }
        public bool isMetaModule => m_MetaModule != null;
        public bool isMetaNamespace => m_MetaNamespace != null;
        public bool isMetaData => m_MetaData != null;
        public bool isMetaEnum => m_MetaEnum != null;
        
        public MetaNode parentNode => m_ParentNode;
        public MetaModule metaModule => m_MetaModule;
        public MetaNamespace metaNamespace => m_MetaNamespace;
        public MetaData metaData => m_MetaData;
        public MetaEnum metaEnum => m_MetaEnum;
        public int anchorDeep => m_AnchorDeep;

        public List<MetaClass> metaClassList
        {
            get
            {
                List<MetaClass> list = new List<MetaClass>();
                foreach( var v in m_MetaTemplateClassDict )
                {
                    list.Add(v.Value);
                }
                return list;
            }
        }
        public virtual string allName
        {
            get
            {
                if (string.IsNullOrEmpty(m_AllName))
                {
                    m_AllName = m_ParentNode != null && !(m_ParentNode.isMetaModule) ? parentNode.allName + "." + m_Name : m_Name;
                }
                return m_AllName;
            }
        }
        public string allNameIncludeModule
        {
            get
            {
                return m_ParentNode != null ? m_ParentNode.allNameIncludeModule + "." + m_Name : m_Name;
            }
        }
        
        public Dictionary<int, MetaClass> metaTemplateClassDict => m_MetaTemplateClassDict;
        public Dictionary<string, MetaNode> childrenMetaNodeDict => m_ChildrenMetaNodeDict;


        #region 属性
        protected int m_Deep = 0;
        protected int m_AnchorDeep = 0;
        protected MetaNode m_ParentNode = null;

        protected EStructNodeType m_EStructNodeType = EStructNodeType.Namespace;
        protected MetaModule m_MetaModule = null;
        protected MetaNamespace m_MetaNamespace = null;
        // 模板个数类
        protected Dictionary<int, MetaClass> m_MetaTemplateClassDict = new Dictionary<int, MetaClass>();
        protected MetaData m_MetaData = null;
        protected MetaEnum m_MetaEnum = null;
        protected string m_Name = "";
        protected string m_AllName = "";
        // 子节点
        protected Dictionary<string, MetaNode> m_ChildrenMetaNodeDict = new Dictionary<string, MetaNode>();
        
        //protected Dictionary<string, MetaNode> m_MetaTemplateClassNodeDict = new Dictionary<string, MetaNode>();
        #endregion

        public MetaNode()
        {
            m_Deep = 0;
            m_AnchorDeep = 0;
        }
        public MetaNode(MetaModule mm)
        {
            m_EStructNodeType = EStructNodeType.Module;
            this.m_MetaModule = mm;
            this.m_Name = mm.name;
            mm.SetMetaNode(this);
        }
        public MetaNode(MetaNamespace mn)
        {
            m_EStructNodeType = EStructNodeType.Namespace;
            this.m_MetaNamespace = mn;
            this.m_Name = mn.name;
            mn.SetMetaNode(this);
        }
        public MetaNode(MetaData md)
        {
            m_EStructNodeType = EStructNodeType.Data;
            this.m_MetaData = md;
            this.m_Name = md.name;
            md.SetMetaNode(this);
        }
        public MetaNode(MetaEnum me)
        {
            m_EStructNodeType = EStructNodeType.Enum;
            this.m_MetaEnum = me;
            this.m_Name = me.name;
            me.SetMetaNode(this);
        }
        public MetaNode(MetaClass mc)
        {
            m_EStructNodeType = EStructNodeType.Class;
            mc.SetMetaNode( this );
            this.m_Name = mc.name;
            if (this.m_MetaTemplateClassDict.ContainsKey(mc.metaTemplateList.Count ) )
            {
                Log.AddInStructMeta(EError.None, $"已有该模板数量的类{mc.name} :{mc.metaTemplateList.Count}");
                return;
            }
            this.m_MetaTemplateClassDict.Add(mc.metaTemplateList.Count, mc);
        }
        public string GetAllName()
        {
            StringBuilder sb = new StringBuilder();

            List<string> list = new List<string>();
            MetaNode mn = this;
            while( mn != null )
            {
                list.Add(mn.name);
                mn = mn.parentNode;
            }
            for( int i = list.Count-1; i >= 0; i-- )
            {
                sb.Append(list[i]);
                if( i > 0 )
                {
                    sb.Append(".");
                }
            }
            return sb.ToString();
        }
        public MetaNode AddMetaNamespace(MetaNamespace namespaceName)
        {           
            if( m_MetaNamespace != null || this.isMetaModule )
            {
                var node = new MetaNode(namespaceName);
                AddMetaNode(node);
                return node;
            }
            else
            {
                Log.AddInStructMeta(EError.ParseFileError, "添加命名空间");
            }

            return null;
        }
        public MetaNode AddMetaEnum(MetaEnum me)
        {
            MetaNode mn = new MetaNode(me);

            AddMetaNode(mn);

            return mn;
        }
        public MetaNode AddMetaData(MetaData me)
        {
            MetaNode mn = new MetaNode(me);

            AddMetaNode(mn);

            return mn;
        }
        public MetaNode AddMetaClass( MetaClass mc )
        {
            MetaNode node = null;
            if (m_ChildrenMetaNodeDict.ContainsKey(mc.name))
            {
                node = m_ChildrenMetaNodeDict[mc.name];
                
                if(node != null )
                {
                    if( node.m_MetaTemplateClassDict.ContainsKey( mc.metaTemplateList.Count ) )
                    {
                        Log.AddInStructMeta(EError.None, "添加add metaclass metanode节点有问题! 有重复:" + mc.allClassName );
                        return null;
                    }
                    else
                    {
                        mc.SetMetaNode(node);
                        node.m_MetaTemplateClassDict.Add(mc.metaTemplateList.Count, mc);
                    }
                }
            }
            else
            {
                node = new MetaNode(mc);
                node.m_ParentNode = this;
                m_ChildrenMetaNodeDict.Add(node.name, node);
            }

            return node;
        }
        public bool AddMetaNode( MetaNode mn )
        {
            if( m_ChildrenMetaNodeDict.ContainsKey( mn.name ) )
            {
                Log.AddInStructMeta(EError.None, "添加 add metanode metanode节点有问题! 有重复");
                return false;
            }
            mn.m_ParentNode = this;
            m_ChildrenMetaNodeDict.Add(mn.name, mn);
            return true;
        }
        public bool IsMetaClass()
        {
            if (m_MetaTemplateClassDict.Count > 0)
                return true;
            return false;
        }
        public bool IsIncludeMetaNode(string name)
        {
            return m_ChildrenMetaNodeDict.ContainsKey(name);
        }
        public virtual MetaNode GetChildrenMetaNodeByName(string name)
        {
            if (m_ChildrenMetaNodeDict.ContainsKey(name))
                return m_ChildrenMetaNodeDict[name];

            return null;
        }
        public MetaNamespace GetMetaNamespaceByName( string name )
        {
            if( m_ChildrenMetaNodeDict.TryGetValue(name, out var ns ) )
            {
                if( ns.metaNamespace != null )
                { return ns.metaNamespace; }
            }
            return null;
        }
        public MetaClass GetMetaClassByTemplateCount(int count )
        {
            if (m_MetaTemplateClassDict.ContainsKey(count))
            {
                return m_MetaTemplateClassDict[count];
            }
            return null;
        }
        //public MetaNode GetMetaBaseByTopLevel(string _name)
        //{
        //    //if ( m_ChildrenMetaClassDict.ContainsKey(_name))
        //    //return m_ChildrenMetaClassDict[_name];

        //    MetaNode parentMB = parentNode;
        //    while (true)
        //    {
        //        if (parentMB != null)
        //        {
        //            //var rmb = parentMB.GetChildrenMetaBaseByName(_name);
        //            //if (rmb != null) return rmb;

        //            //parentMB = parentMB.parentNode;
        //        }
        //        else
        //            break;
        //    }
        //    return null;
        //}
        public void SetDeep(int deep)
        {
            if ( m_MetaModule != null )
            {
                m_MetaModule.SetDeep(deep);
            }
            if( m_MetaNamespace != null )
            {
                m_MetaNamespace.SetDeep(deep);
            }
            if (m_MetaData != null)
            {
                m_MetaData.SetDeep(deep);
            }
            if (m_MetaEnum != null)
            {
                m_MetaEnum.SetDeep(deep);
            }
            foreach (var v in m_MetaTemplateClassDict)
            {
                v.Value.SetDeep(deep);
            }
            foreach (var v in m_ChildrenMetaNodeDict)
            {
                v.Value.SetDeep( deep + 1 );
            }
        }
        public virtual void SetAnchorDeep(int addep)
        {
            m_AnchorDeep = addep;
            foreach (var v in m_ChildrenMetaNodeDict)
            {
                v.Value.SetAnchorDeep(deep);
            }
        }
        //该函数，只为调试效果时候使用，在编译逻辑里边不体现！
        public virtual MetaNode GetMetaBaseInParentByName(string inputname, bool isInclude = true)
        {
            if (m_Name == inputname && isInclude)
                return this;
            MetaNode findParentClassMB = null;
            MetaNode tmb2 = this.parentNode;
            while (tmb2 != null)
            {
                if (tmb2.m_ChildrenMetaNodeDict.ContainsKey(inputname))
                {
                    findParentClassMB = tmb2.m_ChildrenMetaNodeDict[inputname];
                    break;
                }
                if (tmb2.parentNode == null) break;
                tmb2 = tmb2.parentNode;
            }
            return findParentClassMB;
        }
        public bool RemoveMetaChildNode(MetaNode mb)
        {
            string key = "";
            foreach (var v in m_ChildrenMetaNodeDict )
            {
                if (v.Value == mb)
                {
                    key = v.Key;
                    break;
                }
            }
            if (string.IsNullOrEmpty(key))
            {
                m_ChildrenMetaNodeDict.Remove(key);
                return true;
            }
            return false;
        }
        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_MetaModule != null)
            {
                sb.Append(m_MetaModule.ToFormatString());
            }
            else if (m_MetaNamespace != null)
            {
                sb.Append(m_MetaNamespace.ToFormatString());
            }
            else if (m_MetaData != null)
            {
                sb.Append(m_MetaData.ToFormatString());
            }
            else if (m_MetaEnum != null)
            {
                sb.Append(m_MetaEnum.ToFormatString());
            }
            else if (m_MetaTemplateClassDict.Count > 0)
            {
                foreach (var v in m_MetaTemplateClassDict)
                {
                    sb.Append(v.Value.ToFormatString());
                }
            }
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(this.allName);
            
            return sb.ToString();
        }
        //------------------------------------------------
        //public void PrintAllNamespace()
        //{
        //    Debug.Write("---------------NamespaceBegin-----------" + Environment.NewLine);
        //    Debug.Write(ToAllNamespace());
        //    Debug.Write("--------------NamespaceEnd-------------");
        //}
        //public string ToAllNamespace()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    if (isNotAllowCreateName)
        //    {
        //        sb.Append("[NoAllowCreate]");
        //    }
        //    //foreach (var v in m_ChildrenNameNodeDict)
        //    //{
        //    //    sb.Append("namespace " + v.Key + Environment.NewLine );
        //    //}
        //    return sb.ToString();
        //}
        //public override string ToFormatString()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    //for (int i = 0; i < deep; i++)
        //    //    sb.Append(Global.tabChar);
        //    //sb.Append("namespace " + name + Environment.NewLine );
        //    //for (int i = 0; i < deep; i++)
        //    //    sb.Append(Global.tabChar);
        //    //sb.Append("{" + Environment.NewLine);

        //    //foreach (var v in m_ChildrenNameNodeDict)
        //    //{
        //    //    MetaBase mb = v.Value;
        //    //    if (mb is MetaNamespace)
        //    //    {
        //    //        sb.Append((mb as MetaNamespace).ToFormatString());
        //    //        sb.Append(Environment.NewLine);
        //    //    }
        //    //    else if (mb is MetaClass)
        //    //    {
        //    //        sb.Append( (mb as MetaClass).ToFormatString() );
        //    //        sb.Append(Environment.NewLine);
        //    //    }
        //    //    else
        //    //    {
        //    //        sb.Append("Errrrrroooorrr ---" + mb.ToFormatString());
        //    //        sb.Append(Environment.NewLine);
        //    //    }
        //    //}

        //    //for (int i = 0; i < deep; i++)
        //    //    sb.Append(Global.tabChar);
        //    sb.Append("}" + Environment.NewLine);

        //    return sb.ToString();
        //}
        //----------------------------------------------------------------
    }
}
