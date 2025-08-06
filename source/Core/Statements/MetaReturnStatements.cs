//****************************************************************************
//  File:      MetaReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.IR;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public sealed  class MetaReturnStatements : MetaStatements
    {
        public MetaExpressNode express => m_Express;

        private FileMetaKeyReturnSyntax m_FileMetaReturnSyntax;
        private MetaType m_ReturnMetaDefineType;
        private MetaExpressNode m_Express = null;
        public MetaReturnStatements( MetaBlockStatements mbs, FileMetaKeyReturnSyntax fmrs ) : base(mbs)
        {
            m_FileMetaReturnSyntax = fmrs;

            MetaType mdt = mbs.ownerMetaFunction.metaDefineType;


            CreateExpressParam cep2 = new CreateExpressParam()
            {
                ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass,
                ownerMBS = m_OwnerMetaBlockStatements,
                metaType = mdt,
                fme = m_FileMetaReturnSyntax.returnExpress,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
                equalMetaVariable = mbs.ownerMetaFunction.returnMetaVariable
            };
            m_Express = ExpressManager.CreateExpressNode(cep2);
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                m_Express.CalcReturnType();
                m_ReturnMetaDefineType = m_Express.GetReturnMetaDefineType();
            }
            else
            {
                m_ReturnMetaDefineType = new MetaType( CoreMetaClassManager.voidMetaClass );
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
        public MetaClass returnMetaClass;
        public MetaExpressNode m_Express = null;

        private FileMetaKeyReturnSyntax m_FileMetaReturnSyntax;
        public MetaTRStatements(MetaBlockStatements mbs, FileMetaKeyReturnSyntax fmrs) : base(mbs)
        {
            m_FileMetaReturnSyntax = fmrs;

            if (m_FileMetaReturnSyntax?.returnExpress != null)
            {
                MetaType mdt = new MetaType(returnMetaClass);

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
            }
            if (m_Express != null)
            {
                m_Express.CalcReturnType();
                //returnMetaClass = ClassManager.instance.GetClassByMetaType(m_Express.returnType);
            }
            else
            {
                returnMetaClass = CoreMetaClassManager.voidMetaClass;
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
