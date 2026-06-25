//****************************************************************************
//  File:      MetaExpressEmptyRet.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2026/6/18 12:00:00
//  Description:  ?? null coalescing operator
//****************************************************************************

using SimpleLanguage.Compile;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaEmptyRetExpressNode : MetaExpressNodeBase
    {
        public MetaExpressNodeBase return1Express => m_Return1Express;
        public MetaExpressNodeBase return2Express => m_Return2Express;

        private MetaExpressNodeBase m_Return1Express = null;
        private MetaExpressNodeBase m_Return2Express = null;

        private FileMetaEmptyRetSyntaxTerm m_FileMetaEmptyRetSyntaxTerm = null;

        public MetaEmptyRetExpressNode(MetaBase ownerMC, MetaBlockStatements mbs, FileMetaEmptyRetSyntaxTerm fm)
        {
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_FileMetaEmptyRetSyntaxTerm = fm;

            CreateExpressParam return1Cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass,
                metaType = new MetaType(CoreMetaClassManager.objectMetaClass),
                fme = m_FileMetaEmptyRetSyntaxTerm.left,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
            };
            m_Return1Express = ExpressManager.CreateExpressNode(return1Cep);

            CreateExpressParam return2Cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass,
                metaType = new MetaType(CoreMetaClassManager.objectMetaClass),
                fme = m_FileMetaEmptyRetSyntaxTerm.right,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
            };
            m_Return2Express = ExpressManager.CreateExpressNode(return2Cep);
        }

        public override void Parse(AllowUseSettings auc)
        {
            m_Return1Express.Parse(auc);
            m_Return2Express.Parse(auc);
        }
        //public override int CalcParseLevel(int level)
        //{
        //    int level1 = m_Return1Express.CalcParseLevel(level);
        //    int level2 = m_Return2Express.CalcParseLevel(level1);
        //    return level2;
        //}
        public override void CalcReturnType()
        {
            m_Return1Express.CalcReturnType();
            m_Return2Express.CalcReturnType();

            MetaType leftMt = m_Return1Express.GetReturnMetaType();
            MetaType rightMt = m_Return2Express.GetReturnMetaType();

            if (leftMt != null)
            {
                m_ExpressReturnMetaType = new MetaType(leftMt);
            }
            else if (rightMt != null)
            {
                m_ExpressReturnMetaType = new MetaType(rightMt);
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_Return1Express.ToFormatString());
            sb.Append(" ?? ");
            sb.Append(m_Return2Express.ToFormatString());
            return sb.ToString();
        }
    }
}
