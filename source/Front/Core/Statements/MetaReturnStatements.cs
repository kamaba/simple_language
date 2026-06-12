//****************************************************************************
//  File:      MetaReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed  class MetaReturnStatements : MetaStatements
    {
        public MetaExpressNodeBase express => m_Express;

        private FileMetaKeyReturnSyntax m_FileMetaReturnSyntax;
        private MetaType m_ReturnMetaDefineType;
        private MetaExpressNodeBase m_Express = null;
        public MetaReturnStatements( MetaBlockStatements mbs, FileMetaKeyReturnSyntax fmrs ) : base(mbs)
        {
            m_FileMetaReturnSyntax = fmrs;
            m_Token = fmrs.token;

            MetaType mdt = mbs.ownerMetaFunction.GetFinalMetaType();

            if(m_FileMetaReturnSyntax.returnExpress != null )
            {
                CreateExpressParam cep2 = new CreateExpressParam()
                {
                    ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass,
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = mdt,
                    fme = m_FileMetaReturnSyntax.returnExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress,
                    equalMetaVariable = mbs.ownerMetaFunction.returnMetaVariable
                };
                m_Express = ExpressManager.CreateExpressNode(cep2);
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                m_Express = ExpressManager.ConvertNewExpress(m_Express, null );
                m_Express.CalcReturnType();
                m_ReturnMetaDefineType = m_Express.GetReturnMetaType();
            }
            else
            {
                m_ReturnMetaDefineType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }

            if( !TypeManager.CompareLeftRightMetaType(mdt, m_ReturnMetaDefineType, m_Token, out MetaType convertMt  ) )
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "left compare right " + m_ReturnMetaDefineType.ToString(), mdt.ToString());
            }
        }        
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append("return ");
            sb.Append(m_Express?.ToFormatString());
            sb.Append(";");
            return sb.ToString();
        }
    }
    public sealed class MetaTRStatements : MetaStatements
    {
        public MetaType m_ReturnMetaType;
        public MetaExpressNodeBase m_Express = null;

        private FileMetaKeyReturnSyntax m_FileMetaReturnSyntax;
        public MetaTRStatements(MetaBlockStatements mbs, FileMetaKeyReturnSyntax fmrs) : base(mbs)
        {
            m_FileMetaReturnSyntax = fmrs;
            m_Token = fmrs.token;

            MetaType mdt = null;
            if (m_FileMetaReturnSyntax?.returnExpress != null)
            {
                CreateExpressParam cep2 = new CreateExpressParam()
                {
                    ownerMBS = m_OwnerMetaBlockStatements,
                    metaType = mdt,
                    fme = m_FileMetaReturnSyntax.returnExpress,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress
                };
                m_Express = ExpressManager.CreateExpressNode(cep2);
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                m_Express = ExpressManager.ConvertNewExpress(m_Express, null);
                m_Express.CalcReturnType();
            }
            if (m_Express != null)
            {
                m_ReturnMetaType = m_Express.GetReturnMetaType();
            }
            else
            {
                m_ReturnMetaType = new MetaType(CoreMetaClassManager.voidMetaClass);
            }
            if (!TypeManager.CompareLeftRightMetaType(m_ReturnMetaType, mdt, m_Token, out MetaType convertMt))
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "left compare right " + m_ReturnMetaType.ToString(), mdt.ToString());
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            if( this.trMetaVariable != null )
            {
                sb.Append(this.trMetaVariable.name);
                sb.Append(" = ");
            }
            sb.Append(m_Express?.ToFormatString());
            sb.Append(";");
            return sb.ToString();
        }
    }
}
