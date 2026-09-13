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
        RefModule,
    }
    public class MetaBase
    {
        public int deep => m_Deep;
        public virtual Token token => m_Token;
        public EType eType => m_Type;
        public virtual List<Token> pingTokenList => m_PintTokenList;
        public int realDeep
        {
            get
            {
                return m_Deep - m_AnchorDeep;
            }
        }
        public EPermission permission => m_Permission;
        public virtual string name => m_Name;
        /// <summary>
        /// 类全名。优先返回 m_AllName（由 UpdateClassAllName 设置，含模块名+模板参数）；
        /// 若为空则回退到 m_MetaNode.GetAllName()（含模块名，不含模板参数）；
        /// 最后回退到 m_Name。
        /// 这确保 classId 始终基于 moduleName.namespace.className 格式生成。
        /// </summary>
        public virtual string allName =>
            !string.IsNullOrEmpty(m_AllName) ? m_AllName :
            (m_MetaNode != null ? m_MetaNode.GetAllName() : m_Name);
        /// <summary>
        /// 类身份的确定型 id（按 allName 的 FNV-1a 32-bit 哈希，跨会话稳定）。
        /// allName 格式为 moduleName.namespaceName.className.childClassName，
        /// 确保同一类在不同模块引用场景下 classId 一致。
        /// </summary>
        public int classId => ClassManager.GetClassId(allName);
        public RefFromType refFromType => m_RefFromType;
        public MetaNode metaNode => m_MetaNode;
        public string pathName => m_MetaNode?.GetAllName();

        protected EPermission m_Permission = EPermission.Public;
        protected RefFromType m_RefFromType = RefFromType.Local;
        protected string m_Name = "";
        protected string m_AllName = "";
        protected MetaNode m_MetaNode = null;
        protected int m_Deep = 0;
        protected int m_AnchorDeep = 0;
        protected List<Token> m_PintTokenList = new List<Token>();
        protected Token m_Token = null;
        protected EType m_Type = EType.None;

        public MetaBase()
        {
            m_RefFromType = RefFromType.Local;
        }
        protected MetaBase( string name, RefFromType refFromType, EPermission permission, MetaNode metaNode )
        {
            m_Name = name;
            m_RefFromType = refFromType;
            m_Permission = permission;
            m_MetaNode = metaNode;
        }
        public MetaBase( MetaBase mb )
        {
            m_Name = mb.m_Name;
            m_AllName = mb.m_AllName;
            m_RefFromType = mb.m_RefFromType;
            m_Permission = mb.m_Permission;
            m_MetaNode = mb.m_MetaNode;
            m_PintTokenList = mb.m_PintTokenList;
            m_Type = mb.m_Type;
        }
        public void SetToken( Token token )
        {
            m_Token = token;
        }
        public void AddPingToken(Token token)
        {
            if (token == null)
            {
                return;
            }
            var find1 = m_PintTokenList.Find(
                a => a.sourceBeginLine == token.sourceBeginLine
                && a.sourceBeginChar == token.sourceBeginChar
                && a.sourceEndLine == token.sourceEndLine
                && a.sourceEndChar == token.sourceEndChar
                && a.path == token.path);
            if (find1 == null)
            {
                m_PintTokenList.Add(token);
            }
            if( m_Token == null )
            {
                m_Token = token;
            }
        }
        public void AddPingToken(string path, int beginline, int beginpos, int endline, int endpos)
        {
            var pingToken = new Token(path, ETokenType.None, "", beginline, beginpos);
            pingToken.SetSrouceEnd(endline, endpos);

            var find1 = m_PintTokenList.Find(a => a.sourceBeginLine == beginline && a.sourceBeginChar == beginpos);
            if (find1 == null)
            {
                m_PintTokenList.Add(pingToken);
            }
            if (m_Token == null)
            {
                m_Token = token;
            }
        }
        public void SetRefFromType(  RefFromType type )
        {
            this.m_RefFromType = type;
        }
        public void SetName( string _name )
        {
            m_Name = _name;
        }
        public virtual void SetAnchorDeep(int addep)
        {
            m_AnchorDeep = addep;
        }
        public virtual void SetDeep( int deep )
        {
            m_Deep = deep;
        }
        public void SetMetaNode(MetaNode mn)
        {
            this.m_MetaNode = mn;
        }
        public virtual void UpdateOwner( MetaBase mb ) { }
        public virtual string GetFormatString()
        {
            return "";
        }
        public virtual string ToFormatString()
        {
            return "";
        }
    }
}
