//****************************************************************************
//  File:      MetaIfStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: handle if-elif-else statements, splite a one 'if' syntax statements and more
//  'elif' syntax  statements and one 'else' syntax statements
//****************************************************************************
using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaIfStatements : MetaStatements
    {
        public enum IfElseState
        {
            Null,
            If,
            ElseIf,
            Else,
        }
        public partial class MetaElseIfStatements
        {
            public MetaBlockStatements thenMetaStatements => m_ThenMetaStatements;

            private FileMetaKeyOnlySyntax m_ElseKeySyntax = null;
            private FileMetaConditionExpressSyntax m_IfOrElseIfKeySyntax = null;
            private MetaVariable m_BoolConditionVariable = null;
            private MetaExpressNode m_Express = null;
            private MetaExpressNode m_FinalExpress = null;
            private MetaBlockStatements m_ThenMetaStatements = null;
            private MetaAssignManager m_MetaAssignManager = null;
            private IfElseState m_IfElseState = IfElseState.Null;

            public int deep { get; private set; } = 0;
            public MetaElseIfStatements( MetaBlockStatements mbs, FileMetaConditionExpressSyntax ifexpress, MetaExpressNode conditionExpress )
            {
                m_IfOrElseIfKeySyntax = ifexpress;
                if( conditionExpress != null )
                {
                    m_Express = conditionExpress;
                    m_FinalExpress = ExpressManager.instance.CreateOptimizeAfterExpress(m_Express);
                }

                m_ThenMetaStatements = new MetaBlockStatements(mbs, ifexpress.executeBlockSyntax);

                MetaMemberFunction.CreateMetaSyntax(ifexpress.executeBlockSyntax, m_ThenMetaStatements);

                Parse();
            }
            public MetaElseIfStatements(MetaBlockStatements mbs, FileMetaKeyOnlySyntax elseKeySyntax )
            {
                m_ElseKeySyntax = elseKeySyntax;
                m_ThenMetaStatements = new MetaBlockStatements( mbs, elseKeySyntax.executeBlockSyntax );

                MetaMemberFunction.CreateMetaSyntax( elseKeySyntax.executeBlockSyntax, m_ThenMetaStatements);

                Parse();
            }
            public void ParseDefine( MetaVariable mv )
            {
                m_ThenMetaStatements.SetTRMetaVariable(mv);
            }
            public void SetIfElseState(IfElseState _ieState )
            {
                m_IfElseState = _ieState;
            }
            public void SetDeep( int d )
            {
                deep = d;
                m_ThenMetaStatements.SetDeep( deep );
            }
            private void Parse()
            {
                if (m_Express != null)
                {
                    m_Express.CalcReturnType();
                    m_MetaAssignManager = new MetaAssignManager( m_Express, m_ThenMetaStatements, new MetaType(CoreMetaClassManager.booleanMetaClass));
                    m_BoolConditionVariable = m_MetaAssignManager.metaVariable;
                    if( m_MetaAssignManager.isNeedSetMetaVariable )
                    {
                        m_ThenMetaStatements.UpdateMetaVariable(m_BoolConditionVariable);
                    }
                }
            }
            public string ToFormatString()
            {
                StringBuilder sb = new StringBuilder();
                
                if(m_IfElseState != IfElseState.Else )
                {
                    for (int i = 0; i < deep; i++)
                    {
                        sb.Append(Global.tabChar);
                    }
                    sb.Append(m_BoolConditionVariable.metaDefineType.metaClass.allName);
                    sb.Append(" ");
                    sb.Append(m_BoolConditionVariable.name);
                    sb.Append(" = ");
                    sb.Append(m_Express.ToFormatString());
                    sb.Append(Environment.NewLine);

                }
                for (int i = 0; i < deep; i++)
                {
                    sb.Append(Global.tabChar);
                }
                if (m_IfElseState == IfElseState.If )
                {
                    sb.Append("if ");
                    sb.Append(m_BoolConditionVariable.name);
                }
                else if (m_IfElseState == IfElseState.ElseIf)
                {
                    sb.Append("elif ");
                    sb.Append(m_BoolConditionVariable.name);
                }
                else if (m_IfElseState == IfElseState.Else)
                {
                    sb.Append("else");
                }
                if(m_ThenMetaStatements != null )
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(m_ThenMetaStatements.ToFormatString());
                }

                return sb.ToString();
            }
        }
        List<MetaElseIfStatements> metaElseIfStatements => m_MetaElseIfStatements;

        private List<MetaElseIfStatements> m_MetaElseIfStatements = new List<MetaElseIfStatements>();
        private FileMetaKeyIfSyntax m_FileMetaKeyIfSyntax = null;
        private MetaVariable m_MetaVariable = null;
        public MetaIfStatements(MetaBlockStatements mbs, FileMetaKeyIfSyntax fm, MetaVariable mv = null ) : base(mbs)
        {
            m_FileMetaKeyIfSyntax = fm;
            m_MetaVariable = mv;
            Parse();
        }
        private void Parse()
        {
            if(m_FileMetaKeyIfSyntax.ifExpressSyntax == null )
            {
                Console.WriteLine("Error 没有if语句!!");
            }
            MetaType mdt = null;
            if( m_MetaVariable != null )
            {
                mdt = m_MetaVariable.metaDefineType;
            }
            var express = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements( m_OwnerMetaBlockStatements, mdt, m_FileMetaKeyIfSyntax.ifExpressSyntax.conditionExpress );

            MetaIfStatements.MetaElseIfStatements msis = new MetaIfStatements.MetaElseIfStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyIfSyntax.ifExpressSyntax, express);
            AddIfEslseStateStatements(msis, IfElseState.If );
            if( msis.thenMetaStatements != null )
            {
                msis.thenMetaStatements.SetTRMetaVariable( m_MetaVariable );
            }

            for ( int i = 0; i < m_FileMetaKeyIfSyntax.elseIfExpressSyntax.Count; i++ )
            {
                var fmsthen = m_FileMetaKeyIfSyntax.elseIfExpressSyntax[i];

                var express2 = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(m_OwnerMetaBlockStatements, mdt, fmsthen.conditionExpress );

                MetaIfStatements.MetaElseIfStatements msis2 = new MetaIfStatements.MetaElseIfStatements(m_OwnerMetaBlockStatements, fmsthen, express2 );
                AddIfEslseStateStatements(msis2, IfElseState.ElseIf );
                if (msis2.thenMetaStatements != null)
                {
                    msis2.thenMetaStatements.SetTRMetaVariable(m_MetaVariable);
                }
            }
            
            if( m_FileMetaKeyIfSyntax.elseExpressSyntax != null )
            {
                MetaIfStatements.MetaElseIfStatements msis3 = new MetaIfStatements.MetaElseIfStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyIfSyntax.elseExpressSyntax);
                AddIfEslseStateStatements(msis3, IfElseState.Else );
                if (msis3.thenMetaStatements != null)
                {
                    msis3.thenMetaStatements.SetTRMetaVariable(m_MetaVariable);
                }
            }
        }
        public override void SetDeep(int dp)
        {
            for (int i = 0; i < m_MetaElseIfStatements.Count; i++)
            {
                m_MetaElseIfStatements[i].SetDeep(dp);
            }
            nextMetaStatements?.SetDeep(dp);
        }
        private void AddIfEslseStateStatements(MetaElseIfStatements meis, IfElseState ife )
        {
            m_MetaElseIfStatements.Add(meis);
            meis.SetIfElseState( ife );
        }
        public override void SetTRMetaVariable(MetaVariable mv)
        {
            for (int i = 0; i < m_MetaElseIfStatements.Count; i++)
            {
                m_MetaElseIfStatements[i].thenMetaStatements?.SetTRMetaVariable(mv);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < m_MetaElseIfStatements.Count; i++)
            {
                sb.Append(m_MetaElseIfStatements[i].ToFormatString() );
                sb.AppendLine();
            }
            sb.Append(nextMetaStatements?.ToFormatString());

            return sb.ToString();
        }
    }
}
