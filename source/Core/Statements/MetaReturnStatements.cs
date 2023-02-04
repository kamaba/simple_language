//****************************************************************************
//  File:      MetaReturnStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/8/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaReturnStatements : MetaStatements
    {
        private FileMetaKeyReturnSyntax m_FileMetaReturnSyntax;
        private MetaType m_ReturnMetaDefineType;
        private MetaExpressNode m_Express = null;
        public MetaReturnStatements( MetaBlockStatements mbs, FileMetaKeyReturnSyntax fmrs ) : base(mbs)
        {
            m_FileMetaReturnSyntax = fmrs;

            MetaType mdt = new MetaType( CoreMetaClassManager.objectMetaClass );

            m_Express = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(m_OwnerMetaBlockStatements, mdt, m_FileMetaReturnSyntax.returnExpress, false, false );
            if (m_Express != null)
            {
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


    public partial class MetaTRStatements : MetaStatements
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
                m_Express = ExpressManager.instance.CreateExpressNodeInMetaFunctionCommonStatements(m_OwnerMetaBlockStatements, mdt, m_FileMetaReturnSyntax.returnExpress, false, false );
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
