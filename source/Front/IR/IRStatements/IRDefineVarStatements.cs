//****************************************************************************
//  File:     IRDefineVarStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/14 12:00:00
//  Description:
//****************************************************************************

using SimpleLanguage.Core;

using System.Text;

namespace SimpleLanguage.IR
{
    public class IRDefineVarStatements : IRStatements
    {
        IRExpressBase m_IRExpress = null;
        public IRDefineVarStatements( IRMethod _method ) 
        {
            this.irMethod = _method;
        }     
        public void ParseIRStatements(MetaDefineVarStatements ms)
        {
            MetaNewObjectExpressNode mnoen = null;
            IRMetaClass irmc = null;
            IRMetaType irmt = null;
            if (ms.expressNode != null)
            {
                m_IRExpress = IRExpressManager.CreateExpress(irMethod, ms.expressNode);
                m_IRStatements.Add(m_IRExpress);

                // If the expression's return type differs from the variable's
                // declared type (and both are numeric), emit a Convert instruction
                // before the store.  This handles cases like:
                //   Byte b8 = someInt32Var;   -> LoadLocal + Convert_I8 + StoreLocal
                // Const literals are already folded at parse time (see
                // MetaDefineVarStatements), so this path is mainly for non-const
                // right-hand expressions.
                var expType = ms.expressNode.GetReturnMetaType();
                var varType = ms.defineVarMetaVariable.GetFinalMetaType();
                if (expType != null && varType != null)
                {
                    var expEType = CoreMetaClassManager.GetETypeByMetaClass(expType.metaClass);
                    var varEType = CoreMetaClassManager.GetETypeByMetaClass(varType.metaClass);
                    if (expEType != varEType
                        && NumberManager.IsNumericEType(expEType)
                        && NumberManager.IsNumericEType(varEType))
                    {
                        IRConvert irconv = new IRConvert(irMethod, expEType, varEType);
                        m_IRStatements.Add(irconv);
                    }
                }
            }
            IRStoreVariable irStoreVar = IRStoreVariable.CreateIRStoreVariable(irmt, irmc, irMethod, ms.defineVarMetaVariable);
            //if(m_FileMetaOpAssignSyntax != null )
            //{
            //    irStoreVar.data.SetDebugInfoByToken(m_FileMetaOpAssignSyntax.assignToken);
            //}
            m_IRStatements.Add(irStoreVar);

            if (mnoen != null)
            {
                var mt = mnoen.GetReturnMetaType();
            }
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(" #new var ");
            //sb.Append(m_MetaVariable);
            if (m_IRExpress != null)
            {
                sb.Append(" = " + m_IRExpress.ToIRString());
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
