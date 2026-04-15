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
        public int deep => m_Deep;
        public virtual Token pingToken => m_PintTokenList.Count > 0 ? m_PintTokenList[0] : null;
        public List<Token> pingTokenList => m_PintTokenList;
        public int realDeep
        {
            get
            {
                return m_Deep - m_AnchorDeep;
            }
        }
        public EPermission permission => m_Permission;
        public virtual string name => m_Name;
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

        public MetaBase()
        {
            m_RefFromType = RefFromType.Local;
        }
        public MetaBase( MetaBase mb )
        {
            m_Name = mb.m_Name;
            m_AllName = mb.m_AllName;
            m_RefFromType = mb.m_RefFromType;
            m_Permission = mb.m_Permission;
            m_MetaNode = mb.m_MetaNode;
            m_PintTokenList = mb.m_PintTokenList;
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
