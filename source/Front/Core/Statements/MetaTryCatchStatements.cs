//****************************************************************************
//  File:      MetaTryCatchStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/07/23 12:00:00
//  Description: try / catch / finally / throw statement classes
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// One catch clause: optional type filter, optional variable binding, and a body block.
    /// </summary>
    public sealed class MetaCatchClause
    {
        public Token catchToken => m_CatchToken;
        public Token typeToken => m_TypeToken;
        public Token varToken => m_VarToken;
        public MetaBlockStatements bodyStatements => m_BodyStatements;
        public string typeName => m_TypeToken?.lexeme?.ToString();
        public string varName => m_VarToken?.lexeme?.ToString();

        private Token m_CatchToken;
        private Token m_TypeToken;
        private Token m_VarToken;
        private MetaBlockStatements m_BodyStatements;

        public MetaCatchClause(Token catchToken, Token typeToken, Token varToken)
        {
            m_CatchToken = catchToken;
            m_TypeToken = typeToken;
            m_VarToken = varToken;
        }

        public void SetBodyStatements(MetaBlockStatements mbs) { m_BodyStatements = mbs; }
    }

    /// <summary>
    /// try { } catch [Type e] { } ... finally { }
    /// </summary>
    public sealed class MetaTryStatements : MetaStatements
    {
        public MetaBlockStatements tryBlockStatements => m_TryBlock;
        public List<MetaCatchClause> catchClauses => m_CatchClauses;
        public MetaBlockStatements finallyBlockStatements => m_FinallyBlock;
        public FileMetaKeyTrySyntax fileMetaKeyTrySyntax => m_FileMetaKeyTrySyntax;

        private FileMetaKeyTrySyntax m_FileMetaKeyTrySyntax = null;
        private MetaBlockStatements m_TryBlock = null;
        private List<MetaCatchClause> m_CatchClauses = new List<MetaCatchClause>();
        private MetaBlockStatements m_FinallyBlock = null;

        public MetaTryStatements(MetaBlockStatements mbs, FileMetaKeyTrySyntax fmts) : base(mbs)
        {
            m_FileMetaKeyTrySyntax = fmts;
            m_Token = fmts.token;
            AddPingToken(m_Token);
            Parse();
        }

        private void Parse()
        {
            // --- try body ---
            if (m_FileMetaKeyTrySyntax.tryBlockSyntax != null)
            {
                m_TryBlock = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyTrySyntax.tryBlockSyntax);
                MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyTrySyntax.tryBlockSyntax, m_TryBlock);
            }

            // --- catch clauses ---
            foreach (var fcc in m_FileMetaKeyTrySyntax.catchClauses)
            {
                AddPingToken(fcc.catchToken);
                var clause = new MetaCatchClause(fcc.catchToken, fcc.typeToken, fcc.varToken);

                if (fcc.executeBlockSyntax != null)
                {
                    var catchBlock = new MetaBlockStatements(m_OwnerMetaBlockStatements, fcc.executeBlockSyntax);
                    MetaMemberFunction.CreateMetaSyntax(fcc.executeBlockSyntax, catchBlock);
                    clause.SetBodyStatements(catchBlock);
                }

                m_CatchClauses.Add(clause);
            }

            // --- finally body ---
            if (m_FileMetaKeyTrySyntax.finallyBlockSyntax != null)
            {
                m_FinallyBlock = new MetaBlockStatements(m_OwnerMetaBlockStatements, m_FileMetaKeyTrySyntax.finallyBlockSyntax);
                MetaMemberFunction.CreateMetaSyntax(m_FileMetaKeyTrySyntax.finallyBlockSyntax, m_FinallyBlock);
            }
        }

        public override void SetDeep(int dp)
        {
            m_Deep = dp;
            m_TryBlock?.SetDeep(dp);
            foreach (var c in m_CatchClauses)
                c.bodyStatements?.SetDeep(dp);
            m_FinallyBlock?.SetDeep(dp);
            if (m_NextMetaStatements != null)
                m_NextMetaStatements.SetDeep(dp);
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++) sb.Append(Global.tabChar);
            sb.Append("try");
            if (m_TryBlock != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(m_TryBlock.ToFormatString());
            }
            foreach (var c in m_CatchClauses)
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < realDeep; i++) sb.Append(Global.tabChar);
                sb.Append("catch");
                if (c.typeToken != null) sb.Append(" " + c.typeToken.lexeme);
                if (c.varToken != null) sb.Append(" " + c.varToken.lexeme);
                if (c.bodyStatements != null)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(c.bodyStatements.ToFormatString());
                }
            }
            if (m_FinallyBlock != null)
            {
                sb.Append(Environment.NewLine);
                for (int i = 0; i < realDeep; i++) sb.Append(Global.tabChar);
                sb.Append("finally");
                sb.Append(Environment.NewLine);
                sb.Append(m_FinallyBlock.ToFormatString());
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// throw expression  (bare throw for re-throw when expression is null)
    /// </summary>
    public sealed class MetaThrowStatements : MetaStatements
    {
        public MetaExpressNodeBase express => m_Express;
        public bool isRethrow => m_Express == null;

        private FileMetaKeyThrowSyntax m_FileMetaThrowSyntax;
        private MetaExpressNodeBase m_Express = null;

        public MetaThrowStatements(MetaBlockStatements mbs, FileMetaKeyThrowSyntax fmts) : base(mbs)
        {
            m_FileMetaThrowSyntax = fmts;
            m_Token = fmts.token;
            AddPingToken(m_Token);

            if (m_FileMetaThrowSyntax.throwExpress != null)
            {
                CreateExpressParam cep = new CreateExpressParam()
                {
                    ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass,
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = null,
                    fme = m_FileMetaThrowSyntax.throwExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress,
                };
                m_Express = ExpressManager.CreateExpressNode(cep);
                if (m_Express != null)
                {
                    m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                    m_Express = ExpressManager.ConvertNewExpress(m_Express, null);
                    m_Express.CalcReturnType();
                }
            }
        }

        public override string ToFormatString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++) sb.Append(Global.tabChar);
            sb.Append("throw");
            if (m_Express != null)
            {
                sb.Append(" ");
                sb.Append(m_Express.ToFormatString());
            }
            return sb.ToString();
        }
    }
}
