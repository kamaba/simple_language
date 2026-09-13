//****************************************************************************
//  File:      MetaCallStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile;
using System;
using System.Text;

namespace SimpleLanguage.Core
{
    public partial class MetaCallStatements : MetaStatements
    {
        public MetaCallLink metaCallLink => m_MetaCallLink;
        public bool isHasReturnMetaVariable => m_IsHasReturnMetaVariable;
        public MetaExpressNodeBase expressNode => m_ExpressNode;

        private MetaCallLink m_MetaCallLink = null;
        private FileMetaCallSyntax m_FileMetaCallSyntax = null;
        private AllowUseSettings m_AllowUseSettings = new AllowUseSettings();
        private bool m_IsHasReturnMetaVariable = false;
        private MetaExpressNodeBase m_ExpressNode = null;
        public MetaCallStatements(MetaBlockStatements mbs, FileMetaCallSyntax fmcl) : base(mbs)
        {
            m_FileMetaCallSyntax = fmcl;

            m_AllowUseSettings.useNotStatic = false;
            m_AllowUseSettings.useNotConst = false;
            m_AllowUseSettings.callConstructFunction = true;
            m_AllowUseSettings.callFunction = true;

            if (fmcl.expressTerm != null)
            {
                // Expression statement (e.g. "try riskyFunc()")
                CreateExpressParam cep = new CreateExpressParam()
                {
                    ownerMetaBase = mbs.ownerMetaClass,
                    ownerMBS = mbs,
                    fme = fmcl.expressTerm,
                    isStatic = false,
                    isConst = false,
                    parsefrom = EParseFrom.StatementRightExpress,
                };
                var men = ExpressManager.CreateExpressNodeByCEP(cep);
                if (men != null)
                {
                    men.Parse(new AllowUseSettings() { parseFrom = EParseFrom.StatementRightExpress });
                    m_ExpressNode = men;
                }
            }
            else
            {
                m_MetaCallLink = new MetaCallLink(fmcl.variableRef, mbs.ownerMetaBase, mbs, null, null );
                m_MetaCallLink.Parse(m_AllowUseSettings);

                // Standalone call statements have no receiver for the return value.
                // If the final call returns a non-void value, mark it so the IR layer
                // can emit a Pop to discard the unused return value from the stack.
                var finalNode = m_MetaCallLink.finalCallNode;
                if (finalNode != null &&
                    (finalNode.visitType == MetaVisitNode.EVisitType.MethodCall ||
                     finalNode.visitType == MetaVisitNode.EVisitType.SystemCall))
                {
                    var fun = finalNode.methodCall?.function;
                    if (fun != null && fun.returnMetaVariable?.defineMetaType?.metaClass?.eType != EType.Void)
                    {
                        m_IsHasReturnMetaVariable = true;
                    }
                }
            }
        }
        public override void UpdateOwnerMetaClass(MetaBase ownerBase)
        {
            base.UpdateOwnerMetaClass(ownerBase);
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);
            sb.Append(m_MetaCallLink?.ToFormatString());
            sb.Append(Environment.NewLine);
            if (nextMetaStatements != null)
            {
                sb.AppendLine(nextMetaStatements.ToFormatString());
            }

            return sb.ToString();
        }
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(m_FileMetaCallLink.ToTokenString());
            return sb.ToString();

        }
    }
}