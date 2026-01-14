//****************************************************************************
//  File:      MetaExpressThreeItem.cs
// ------------------------------------------------
//  Copyright (c) author: Like Cheng kamaba233@gmail.com
//  DateTime: 2026/1/12 12:00:00
//  Description:  
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaThreeItemExpressNode : MetaExpressNode
    {
        public MetaExpressNode conditionExpress => m_ConditionExpress;
        public MetaExpressNode return1Express => m_Return1Express;
        public MetaExpressNode return2Express => m_Return2Express;

        private MetaExpressNode m_ConditionExpress = null;
        private MetaExpressNode m_Return1Express = null;
        private MetaExpressNode m_Return2Express = null;

        private FileMetaThreeItemSyntaxTerm m_FileMetaThreeItemSyntaxTerm = null;

        public MetaThreeItemExpressNode(MetaClass ownerMC, MetaBlockStatements mbs, FileMetaThreeItemSyntaxTerm fm ) 
        {
            m_OwnerMetaClass = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_FileMetaThreeItemSyntaxTerm = fm;
        }
        public override void Parse(AllowUseSettings auc)
        {
            CreateExpressParam conditionCep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass,
                metaType = new MetaType(CoreMetaClassManager.booleanMetaClass),
                fme = m_FileMetaThreeItemSyntaxTerm.conditionTerm,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
            };
            m_ConditionExpress = ExpressManager.CreateExpressNode(conditionCep);
            m_ConditionExpress.Parse(auc);

            CreateExpressParam return1Cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass,
                metaType = new MetaType(CoreMetaClassManager.objectMetaClass),
                fme = m_FileMetaThreeItemSyntaxTerm.return1Term,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
            };
            m_Return1Express = ExpressManager.CreateExpressNode(return1Cep);
            m_Return1Express.Parse(auc);

            CreateExpressParam return2Cep = new CreateExpressParam()
            {
                ownerMBS = m_OwnerMetaBlockStatements,
                ownerMetaClass = m_OwnerMetaBlockStatements.ownerMetaClass,
                metaType = new MetaType(CoreMetaClassManager.objectMetaClass ),
                fme = m_FileMetaThreeItemSyntaxTerm.return2Term,
                isStatic = false,
                isConst = false,
                parsefrom = EParseFrom.StatementRightExpress,
            };
            m_Return2Express = ExpressManager.CreateExpressNode(return2Cep);
            m_Return2Express.Parse(auc);
        }
        public override int CalcParseLevel(int level)
        {
            return 0;
        }
        public override void CalcReturnType()
        {
            m_MetaType = return1Express.GetReturnMetaDefineType();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_ConditionExpress.ToFormatString());
            sb.Append("  ?  ");
            sb.Append(m_Return1Express.ToFormatString());
            sb.Append("  :  ");
            sb.Append(m_Return2Express.ToFormatString());

            return sb.ToString();
        }
    }
}
