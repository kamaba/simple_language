//****************************************************************************
//  File:      MetaBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description:  this's a statement in function! same link table model!
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaBlockStatements : MetaStatements
    {
        public enum ETerminatorType
        {
            None,
            Next,
            Break,
            Continue,
            Return,
        }

        public ETerminatorType terminatorType => m_TerminatorType;
        public MetaStatements terminatorStatement => m_TerminatorStatement;

        private ETerminatorType m_TerminatorType = ETerminatorType.None;
        private MetaStatements m_TerminatorStatement = null;
        public override MetaFunction ownerMetaFunction => m_OwnerMetaFunction;
        public MetaStatements ownerMetaStatements => m_OwnerMetaStatements;
        public FileMetaBlockSyntax fileMetaBlockSyntax => m_FileMetaBlockSyntax;

        public MetaBlockStatements parent { get; set; } = null;
        protected MetaFunction m_OwnerMetaFunction = null;
        public bool isOnFunction = false;                   // on function or inner function 


        protected Dictionary<string, MetaVariable> m_MetaVariableDict = new Dictionary<string, MetaVariable>();
        protected List<MetaBlockStatements> m_ChildrenMetaBlockStatementsList = new List<MetaBlockStatements>();
        protected MetaStatements m_OwnerMetaStatements = null;        
        protected FileMetaBlockSyntax m_FileMetaBlockSyntax = null;
        public MetaBlockStatements(MetaBlockStatements mbs)
        {
            this.m_OwnerMetaFunction = mbs.ownerMetaFunction;
            this.m_OwnerMetaBlockStatements = mbs.m_OwnerMetaBlockStatements;
        }
        public MetaBlockStatements()
        {
        }

        public void DetectAndValidateTerminator(Token errorToken)
        {
            m_TerminatorType = ETerminatorType.None;
            m_TerminatorStatement = null;

            // Only check the top-level linked list. (Nested blocks have their own rules.)
            MetaStatements last = nextMetaStatements;
            if (last == null) return;
            while (last.nextMetaStatements != null)
            {
                last = last.nextMetaStatements;
            }

            if (last is MetaBreakStatements)
            {
                m_TerminatorType = ETerminatorType.Break;
                m_TerminatorStatement = last;
            }
            else if (last is MetaContinueStatements)
            {
                m_TerminatorType = ETerminatorType.Continue;
                m_TerminatorStatement = last;
            }
            else if (last is MetaReturnStatements)
            {
                m_TerminatorType = ETerminatorType.Return;
                m_TerminatorStatement = last;
            }
            else if (last is SimpleLanguage.Core.MetaNextStatements)
            {
                m_TerminatorType = ETerminatorType.Next;
                m_TerminatorStatement = last;
            }

            // Mutual exclusivity & must-be-last validation.
            bool hasBreak = ContainsStatement<MetaBreakStatements>();
            bool hasContinue = ContainsStatement<MetaContinueStatements>();
            bool hasReturn = ContainsStatement<MetaReturnStatements>();
            bool hasNext = ContainsStatement<SimpleLanguage.Core.MetaNextStatements>();

            int count = 0;
            if (hasBreak) count++;
            if (hasContinue) count++;
            if (hasReturn) count++;
            if (hasNext) count++;

            if (count == 0) return;

            if (count > 1)
            {
                Log.AddMetaCoreLog(LID.AutoMetaBlockStatementsL106, "Error 终结语句 next/break/continue/return 互斥" + (errorToken != null ? (" " + errorToken.ToLexemeAllString()) : ""));
                m_TerminatorType = ETerminatorType.None;
                m_TerminatorStatement = null;
                return;
            }

            // if exists, must be the last statement
            if (hasBreak && !IsLastStatement<MetaBreakStatements>(out _))
            {
                Log.AddMetaCoreLog(LID.AutoMetaBlockStatementsL115, "Error break 必须放到语句块的结尾" + (errorToken != null ? (" " + errorToken.ToLexemeAllString()) : ""));
                m_TerminatorType = ETerminatorType.None;
                m_TerminatorStatement = null;
                return;
            }
            if (hasContinue && !IsLastStatement<MetaContinueStatements>(out _))
            {
                Log.AddMetaCoreLog(LID.AutoMetaBlockStatementsL122, "Error continue 必须放到语句块的结尾" + (errorToken != null ? (" " + errorToken.ToLexemeAllString()) : ""));
                m_TerminatorType = ETerminatorType.None;
                m_TerminatorStatement = null;
                return;
            }
            if (hasReturn && !IsLastStatement<MetaReturnStatements>(out _))
            {
                Log.AddMetaCoreLog(LID.AutoMetaBlockStatementsL129, "Error return 必须放到语句块的结尾" + (errorToken != null ? (" " + errorToken.ToLexemeAllString()) : ""));
                m_TerminatorType = ETerminatorType.None;
                m_TerminatorStatement = null;
                return;
            }
            if (hasNext && !IsLastStatement<SimpleLanguage.Core.MetaNextStatements>(out _))
            {
                Log.AddMetaCoreLog(LID.AutoMetaBlockStatementsL136, "Error next 必须放到语句块的结尾" + (errorToken != null ? (" " + errorToken.ToLexemeAllString()) : ""));
                m_TerminatorType = ETerminatorType.None;
                m_TerminatorStatement = null;
                return;
            }
        }
        public MetaBlockStatements( MetaFunction mf )
        {
            m_OwnerMetaBlockStatements = null;
            m_OwnerMetaFunction = mf;
        }
        public MetaBlockStatements( MetaFunction mf, FileMetaBlockSyntax fmbs )
        {
            m_OwnerMetaBlockStatements = null;
            m_OwnerMetaFunction = mf;
            m_FileMetaBlockSyntax = fmbs;
            AddPingToken(fmbs?.token);
        }
        public MetaBlockStatements( MetaBlockStatements mbs, FileMetaBlockSyntax fmbs) : base(mbs)
        {
            mbs.m_ChildrenMetaBlockStatementsList.Add(this);
            m_OwnerMetaFunction = mbs.ownerMetaFunction;
            m_FileMetaBlockSyntax = fmbs;
            AddPingToken(fmbs?.token);
        }
        public void SetFileMetaBlockSyntax( FileMetaBlockSyntax blockSyntax )
        {
            m_FileMetaBlockSyntax = blockSyntax;
            AddPingToken(blockSyntax?.token);
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
            while(nextStatements != null )
            {
                if (nextStatements is MetaForStatements)
                    return nextStatements;
                else if (nextStatements is MetaWhileDoWhileStatements)
                    return nextStatements;
                nextStatements = nextStatements.nextMetaStatements;
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

        public bool IsLastStatement<TStatement>(out TStatement last) where TStatement : MetaStatements
        {
            last = null;
            MetaStatements cur = nextMetaStatements;
            if (cur == null) return false;

            while (cur.nextMetaStatements != null)
            {
                cur = cur.nextMetaStatements;
            }

            if (cur is TStatement t)
            {
                last = t;
                return true;
            }
            return false;
        }

        public bool ContainsStatement<TStatement>() where TStatement : MetaStatements
        {
            MetaStatements cur = nextMetaStatements;
            while (cur != null)
            {
                if (cur is TStatement) return true;
                cur = cur.nextMetaStatements;
            }
            return false;
        }

        public bool ValidateStatementMustBeLast<TStatement>(Token errorToken, string errorMessage) where TStatement : MetaStatements
        {
            if (!ContainsStatement<TStatement>()) return true;

            if (!IsLastStatement<TStatement>(out _))
            {
                Log.AddMetaCoreLog(LID.AutoMetaBlockStatementsL238, errorMessage + (errorToken != null ? (" " + errorToken.ToLexemeAllString()) : ""));
                return false;
            }
            return true;
        }
        public MetaVariable GetMetaVariable( string name )
        {
            if( m_MetaVariableDict.ContainsKey(name) )
            {
                return m_MetaVariableDict[name];
            }
            return null;
        }
        public bool AddMetaVariable(MetaVariable mv)
        {
            if (m_MetaVariableDict.ContainsKey(mv.name))
            {
                Token token = m_FileMetaBlockSyntax?.token;
                Debug.Write("error Class: [" + ownerMetaClass?.allClassName + "] Method: [" + ownerMetaFunction.functionAllName + "]" 
                    + "已定义过了变量名称!!! MBS:" + token?.ToLexemeAllString() + " var:" + mv.ToFormatString() );
                return false;
            }
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
        public bool UpdateMetaVariableDict( MetaVariable mv )
        {
            if (m_MetaVariableDict.ContainsKey(mv.name))
            {
                m_MetaVariableDict[mv.name] = mv;
                return true;
            }
            m_MetaVariableDict.Add(mv.name, mv);
            return true;
        }
        public void  GetCalcMetaVariableList( List<MetaVariable> list )
        {
            foreach (var v in m_MetaVariableDict)
            {
                if( !v.Value.isArgument )
                    list.Add(v.Value);
            }           
                
            foreach( var t in m_ChildrenMetaBlockStatementsList )
            {
                t.GetCalcMetaVariableList( list );
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
            var list = mmpc.metaDefineParamList;
            for ( int i = 0; i < list.Count; i++ )
            {
                var mmpcp = list[i];
                AddMetaVariable(mmpcp.metaVariable);
            }
        }
        //public override MetaStatements GenTemplateClassStatement( MetaGenTemplateClass mgt, MetaBlockStatements parentMs )
        //{
        //    MetaBlockStatements mbs = new MetaBlockStatements( parentMs );
        //    mbs.SetFileMetaBlockSyntax(m_FileMetaBlockSyntax);
        //    mbs.parent = parentMs;
        //    Dictionary<string,MetaVariable> tMvList = new Dictionary<string, MetaVariable>();
        //    foreach (var v in m_MetaVariableDict)
        //    {
        //        MetaVariable nmv = new MetaVariable(v.Value);
        //        nmv.GenTemplateMetaVaraible(mgt, mbs);
        //        tMvList.Add(nmv.name, nmv);
        //    }
        //    m_MetaVariableDict = tMvList;

        //    if (m_NextMetaStatements != null)
        //    {
        //        m_NextMetaStatements = m_NextMetaStatements.GenTemplateClassStatement( mgt, mbs );
        //    }
        //    mbs.SetNextStatements( m_NextMetaStatements );

        //    return mbs;
        //}
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);

            sb.Append("{" + Environment.NewLine);
            if (this.nextMetaStatements != null && this.nextMetaStatements.parentBlockStatements == this )
            {
                sb.AppendLine(nextMetaStatements.ToFormatString() );
            }
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append("}");

            if( this.nextMetaStatements != null && nextMetaStatements.parentBlockStatements != this )
            {
                sb.AppendLine(nextMetaStatements.ToFormatString());
            }
            return sb.ToString();
        }
    }
}
