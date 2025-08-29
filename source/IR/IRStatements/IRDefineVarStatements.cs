//****************************************************************************
//  File:     IRDefineVarStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.Statements;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRDefineVarStatements : IRStatements
    {
        IRExpress m_IRExpress = null;
        public IRDefineVarStatements( IRMethod _method ) 
        {
            this.irMethod = _method;
        }     
        public void ParseIRStatements(MetaDefineVarStatements ms)
        {
            MetaNewObjectExpressNode mnoen = null;
            IRMetaClass irmc = null;
            if (ms.expressNode != null)
            {
                mnoen = ms.expressNode as MetaNewObjectExpressNode;
                if (mnoen != null)
                {
                    irmc = IRManager.instance.GetIRMetaClassByName(ms.expressNode.GetReturnMetaClass().allClassName);
                    IRNew irNew = new IRNew(irMethod, irmc );
                    m_IRStatements.Add(irNew);
                }
                else
                {
                    m_IRExpress = new IRExpress(irMethod, ms.expressNode);
                    m_IRStatements.Add(m_IRExpress);
                }
            }
            IRStoreVariable irStoreVar = IRStoreVariable.CreateIRStoreVariable(irMethod, irmc, ms.defineVarMetaVariable);
            //if(m_FileMetaOpAssignSyntax != null )
            //{
            //    irStoreVar.data.SetDebugInfoByToken(m_FileMetaOpAssignSyntax.assignToken);
            //}
            m_IRStatements.Add(irStoreVar);

            if (mnoen != null)
            {
                var mt = mnoen.GetReturnMetaDefineType();

            }
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(" #new var ");
            //sb.Append(m_MetaVariable.ToFormatString());
            //if (m_IRExpress != null)
            //{
            //    sb.Append(" = " + m_IRExpress.ToIRString() );
            //}
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
