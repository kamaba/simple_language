//****************************************************************************
//  File:      IRMetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Parse;

namespace SimpleLanguage.IR
{
    public enum IRMetaVariableFrom
    {
        None,
        Argument,
        LocalStatement,
        Member,
        Static,
        Global,
        Return,
    }
    public class IRMetaVariable
    {
        public MetaExpressNode express => m_ExpressNode;
        public IRMetaClass irMetaClass => m_IRMetaClass;
        public IRMetaVariableFrom irMetaVariableFrom => m_IRMetaVariableFrom;

        MetaVariable m_MetaVariable = null;
        public int id { get; set; } = 0;
        public string name { get; set; }
        public string templateName => m_TemplateName;
        public int index { get; set; } = 0;
        public bool isTemplate => m_IsTemplate;

        private MetaExpressNode m_ExpressNode = null;
        private IRMetaClass m_IRMetaClass = null;
        private IRMetaVariableFrom m_IRMetaVariableFrom = IRMetaVariableFrom.None;
        private bool m_IsTemplate = false;
        private string m_TemplateName = "";

        public IRMetaVariable( MetaVariable mv )
        {
            m_MetaVariable = mv;
            id = mv.GetHashCode();
            name = mv.ownerMetaBlockStatements?.ownerMetaFunction.name + (mv.isStatic?"_static":"_local") + "[" + mv.name + "]";
            if( mv.variableFrom == MetaVariable.EVariableFrom.Member )
            {
                if( mv.isStatic )
                {
                    m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
                }
                else
                {
                    m_IRMetaVariableFrom = IRMetaVariableFrom.Member;
                }
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.Argument )
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.Argument;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.LocalStatement )
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.LocalStatement;
            }

            m_TemplateName = IRManager.GetIRNameByMetaType(mv.metaDefineType);
            //if( mv.metaDefineType.eType == EMetaTypeType.Template )
            //{
            //    m_IsTemplate = true;
            //    m_TemplateName = IRManager.GetIRNameByMetaType(mv.metaDefineType);
            //    m_IRMetaClass = new IRMetaClass(IRManager.instance, mv.metaDefineType.metaTemplate.name);
            //    if( m_IRMetaClass == null )
            //    {

            //    }
            //}
            //else 

            if( mv.metaDefineType.eType == EMetaTypeType.MetaClass )
            {
                if( !mv.metaDefineType.metaClass.isTemplateClass )
                {
                    m_IsTemplate = false;
                    m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(IRManager.GetIRNameByMetaClass(mv.metaDefineType.metaClass));


                    if (m_IRMetaClass == null)
                    {
                        Log.AddVM(EError.None, "没有找到相对应的类元素!!");
                    }
                }
                else
                {

                }
            }
            else
            {
                ////m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(IRManager.GetIRNameByMetaType(mv.metaDefineType));
                //string tname = IRManager.GetIRNameByMetaClass(mv.metaDefineType.templateMetaClass);
                //m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(tname);
                m_IsTemplate = true;
            }
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberEnum mme)
        {
            m_MetaVariable = mme;
            id = mme.GetHashCode();
            name = mme.ownerMetaClass.allClassName + "." + mme.name;
            m_ExpressNode = mme.express;
            m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            m_IRMetaClass = irmc;
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberData mmd)
        {
            m_MetaVariable = mmd;
            id = mmd.GetHashCode();
            name = mmd.ownerMetaClass.allClassName + "." + mmd.name;
            m_ExpressNode = mmd.expressNode;
            m_IRMetaVariableFrom = mmd.isStatic ? IRMetaVariableFrom.Static : IRMetaVariableFrom.Member;
            m_IRMetaClass = irmc;
        }
        public IRMetaVariable( IRMetaClass irmc, MetaMemberVariable mmv )
        {
            m_MetaVariable = mmv;
            id = mmv.GetHashCode();
            name = mmv.ownerMetaClass.allClassName + "." + mmv.name;
            m_ExpressNode = mmv.express;
            if (mmv.isStatic || mmv.isConst )
                m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            else
                m_IRMetaVariableFrom = IRMetaVariableFrom.Member;
            if(mmv.metaDefineType.eType == EMetaTypeType.Template )
            {
                m_IRMetaClass = new IRMetaClass( IRManager.instance, mmv.metaDefineType.metaTemplate.name );
            }
            else
            {
                m_IRMetaClass = IRManager.instance.GetIRMetaClassByName(IRManager.GetIRNameByMetaType(mmv.metaDefineType));
            }
        }
        public void SetExpress( MetaExpressNode men )
        {
            this.m_ExpressNode = men;
        }
        public override string ToString()
        {
            return name;
        }
    }
}
