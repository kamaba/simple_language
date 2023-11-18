//****************************************************************************
//  File:      IRNewStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaNewStatements
    {
        IRExpress m_IRExpress = null;
        public override void ParseIRStatements()
        {
            MetaNewObjectExpressNode mnoen = null;
            if ( m_ExpressNode != null)
            {
                if( m_ExpressNode is MetaNewObjectExpressNode )
                {
                    mnoen = m_ExpressNode as MetaNewObjectExpressNode;
                    IRNew irNew = new IRNew( irMethod, mnoen.GetReturnMetaDefineType());
                    m_IRStatements.Add( irNew );
                }
                else
                {
                    m_IRExpress = new IRExpress(irMethod, m_ExpressNode);
                    m_IRStatements.Add(m_IRExpress);
                }
            }

            IRStoreVariable irStoreVar = new IRStoreVariable(irMethod, m_MetaVariable);
            if(m_FileMetaOpAssignSyntax != null )
            {
                irStoreVar.data.SetDebugInfoByToken(m_FileMetaOpAssignSyntax.assignToken);
            }
            m_IRStatements.Add(irStoreVar);

            if ( mnoen!= null )
            {
                var metaClass = mnoen.GetReturnMetaClass();
                var mmvs = metaClass.localMetaMemberVariables;
                // Class1{ a = 1; b = 2 }
                for ( int i = 0; i < mmvs.Count; i++ )
                {
                    IRExpress irexp = new IRExpress(irMethod, mmvs[i].express);
                    m_IRStatements.Add(irexp);

                    IRLoadVariable irLoadVar1 = new IRLoadVariable(irMethod, m_MetaVariable );
                    m_IRStatements.Add(irLoadVar1);

                    IRStoreVariable irStoreVar2 = new IRStoreVariable(irMethod, mmvs[i]);
                    m_IRStatements.Add(irStoreVar2);
                }
                // Class1().Init();
                var irCallFun = new IRCallFunction(irMethod, mnoen.constructFunctionCall);
                m_IRStatements.Add(irCallFun);

                // { a = 1, b = 2 }
                for (int i = 0; i < mnoen.metaBraceOrBracketStatementsContent?.assignStatementsList.Count; i++)
                {
                    var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[i];

                    IRLoadVariable mmvsNodeVar = new IRLoadVariable(irMethod, m_MetaVariable);
                    m_IRStatements.Add(mmvsNodeVar);

                    IRStoreVariable irStoreNodeVar3 = new IRStoreVariable(irMethod, asl.metaMemberVariable );
                    m_IRStatements.Add(irStoreNodeVar3);
                }
            }
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("#new var ");
            sb.Append(m_MetaVariable.ToFormatString() );
            if(m_ExpressNode != null )
            {
                sb.Append( " = " + m_ExpressNode.ToFormatString());
            }
            sb.AppendLine(" #");

            sb.AppendLine("{");
            for (int i = 0; i < m_IRStatements.Count; i++)
            {
                sb.AppendLine(m_IRStatements[i].ToIRString());
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
