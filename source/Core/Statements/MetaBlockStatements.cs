//****************************************************************************
//  File:      MetaBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaBlockStatements : MetaStatements
    {
        public MetaBlockStatements parent = null;
        protected MetaFunction m_OwnerMetaFunction = null;
        public bool isOnFunction = false;                   // on function or inner function 

        public override MetaFunction ownerMetaFunction
        {
            get
            {
                return m_OwnerMetaFunction;
            }
        }

        private Dictionary<string, MetaVariable> m_MetaVariableDict = new Dictionary<string, MetaVariable>();
        protected List<MetaBlockStatements> m_ChildrenMetaBlockStatementsList = new List<MetaBlockStatements>();
        protected MetaStatements m_OwnerMetaStatements = null;
        private FileMetaBlockSyntax m_FileMetaBlockSyntax;

        public MetaBlockStatements( MetaFunction mf, FileMetaBlockSyntax fmbs )
        {
            m_OwnerMetaBlockStatements = null;
            m_OwnerMetaFunction = mf;
            m_FileMetaBlockSyntax = fmbs;
        }
        public MetaBlockStatements( MetaBlockStatements mbs, FileMetaBlockSyntax fmbs) : base(mbs)
        {
            mbs.m_ChildrenMetaBlockStatementsList.Add(this);
            m_OwnerMetaFunction = mbs.ownerMetaFunction;
            m_FileMetaBlockSyntax = fmbs;
        }
        public void SetFileMetaBlockSyntax( FileMetaBlockSyntax blockSyntax )
        {
            m_FileMetaBlockSyntax = blockSyntax;
        }
        public void SetOwnerMetaStatements( MetaStatements ms )
        {
            m_OwnerMetaStatements = ms;
        }
        public override void SetNextStatements(MetaStatements ms)
        {
            m_NextMetaStatements = ms;
        }
        public MetaStatements FindNearestMetaForStatementsOrMetaWhileOrDoWhileStatements()
        {
            if( m_OwnerMetaStatements is MetaForStatements 
                || m_OwnerMetaStatements is MetaWhileDoWhileStatements)
            {
                return m_OwnerMetaStatements;
            }
            var nextStatements = m_NextMetaStatements;
            while( nextMetaStatements != null )
            {
                if (nextMetaStatements is MetaForStatements)
                    return nextMetaStatements;
                else if (nextMetaStatements is MetaWhileDoWhileStatements)
                    return nextMetaStatements;
            }
            if (m_OwnerMetaBlockStatements != null )
            {
                return m_OwnerMetaBlockStatements.FindNearestMetaForStatementsOrMetaWhileOrDoWhileStatements();
            }
            return null;
        }
        public override void SetDeep(int dp)
        {
            m_Deep = dp;
            nextMetaStatements?.SetDeep(deep + 1);
        }
        public bool AddMetaVariable(MetaVariable mv)
        {
            if (m_MetaVariableDict.ContainsKey(mv.name))
            {
                Token token = m_FileMetaBlockSyntax?.token;
                Console.WriteLine("error Class: [" + ownerMetaClass?.allName + "] Method: [" + ownerMetaFunction.functionAllName + "]" 
                    + "已定义过了变量名称!!! MBS:" + token?.ToLexemeAllString() + " var:" + mv.ToFormatString() );
                return false;
            }
            mv.SetOwnerBlockstatements(this);
            m_MetaVariableDict.Add(mv.name, mv);
            return true;
        }
        public void AddFrontStatements( MetaStatements ms )
        {
            var t = nextMetaStatements;
            m_NextMetaStatements = ms;
            ms.SetNextStatements( t );
        }
        public void AddFrontToEndStatements( MetaStatements ms )
        {
            var t = nextMetaStatements;
            m_NextMetaStatements = ms;

            var tms = ms;
            while( true )
            {
                if( tms.nextMetaStatements != null )
                {
                    tms = tms.nextMetaStatements;
                }
                else
                {
                    break;
                }

            }
            tms.SetNextStatements( t );
        }
        public bool UpdateMetaVariable( MetaVariable mv )
        {
            mv.SetOwnerBlockstatements(this);
            if (m_MetaVariableDict.ContainsKey(mv.name))
            {
                m_MetaVariableDict[mv.name] = mv;
                return true;
            }
            m_MetaVariableDict.Add(mv.name, mv);
            return true;
        }
        public void  GetCalcMetaVariableList( List<MetaVariable> list, bool isIncludeArgument )
        {
            if (isIncludeArgument)
            {
                foreach (var v in m_MetaVariableDict)
                {
                    list.Add(v.Value);
                }
            }
            else
            {
                foreach (var v in m_MetaVariableDict)
                {
                    if( !v.Value.isArgument )
                        list.Add(v.Value);
                }
            }
                
            foreach( var t in m_ChildrenMetaBlockStatementsList )
            {
                t.GetCalcMetaVariableList( list, true );
            }
        }
        public bool GetIsMetaVariable( string name, bool isFromParent = true  )
        {
            if (m_MetaVariableDict.ContainsKey(name))
            {
                return true;
            }
            if (parentBlockStatements != null && isFromParent )
            {
                return parentBlockStatements.GetIsMetaVariable(name );
            }
            return false;
        }
        public bool AddOnlyNameMetaVariable( string name )
        {
            if (m_MetaVariableDict.ContainsKey(name))
            {
                return false;
            }
            m_MetaVariableDict.Add(name, null);
            return true;
        }
        public MetaVariable GetMetaVariableByName(string name, bool isFromParent = true )
        {
            if (m_MetaVariableDict.ContainsKey(name))
                return m_MetaVariableDict[name];

            if(parentBlockStatements != null && isFromParent )
            {
                return parentBlockStatements.GetMetaVariableByName(name, isFromParent);
            }
            return null;
        }
        public void SetMetaMemberParamCollection( MetaDefineParamCollection mmpc )
        {
            var list = mmpc.GetMetaDefineList();
            for ( int i = 0; i < list.Count; i++ )
            {
                var mmpcp = list[i];
                AddMetaVariable(mmpcp.metaVariable);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append("{" + Environment.NewLine);

            if( nextMetaStatements != null )
            {
                sb.Append(nextMetaStatements.ToFormatString() + Environment.NewLine);
            }
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");


            return sb.ToString();
        }
    }
}
