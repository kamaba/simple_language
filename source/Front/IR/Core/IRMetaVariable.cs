//****************************************************************************
//  File:      IRMetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;

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
        Array,
    }
    public class IRMetaVariable
    {
        public MetaExpressNodeBase express => m_ExpressNode;
        public IRMetaType irMetaType => m_IRMetaType;
        public int id => m_Id;
        public string name => m_Name;
        public int index => m_Index;
        public DebugInfo debugInfo => m_DebugInfo;
        public bool isConst => m_IsConst;
        public bool isStatic => m_IsStatic;
        public EPermission permission => m_Permission;
        public List<IRData> irDataList => m_IRDataList;


        private MetaExpressNodeBase m_ExpressNode = null;
        private List<IRData> m_IRDataList = new List<IRData>();
        private IRMetaType m_IRMetaType = null;
        private IRMetaVariableFrom m_IRMetaVariableFrom = IRMetaVariableFrom.None;
        //private bool m_IsTemplate = false;
        private int m_Id = -1;
        private int m_Index = -1;
        private string m_Name = "";
        private DebugInfo m_DebugInfo;
        private bool m_IsConst = false;
        private bool m_IsStatic = false;
        private EPermission m_Permission = EPermission.Public;
        //private MetaVariable m_MetaVariable = null;

        public IRMetaVariable( MetaVariable mv, int index = -1 )
        {
            //m_MetaVariable = mv;
            m_Id = mv.GetHashCode();
            m_Index = index;
            m_Name = mv.ownerMetaBlockStatements?.ownerMetaFunction.name + (mv.isStatic?"_static":"_local") + "[" + mv.name + "]";
            FillDebugInfo(mv, mv.name, "IRMetaVariable");
            m_IsConst = mv.isConst;
            m_IsStatic = mv.isStatic;
            m_Permission = mv.permission;
            if( mv.variableFrom == MetaVariable.EVariableFrom.ClassMember )
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
            else if( mv.variableFrom == MetaVariable.EVariableFrom.None )
            {

            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Argument)
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.Argument;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.LocalStatement)
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.LocalStatement;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Global)
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.Global;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.ArrayValue )
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.Array;
            }
            else
            {
                Log.AddIRLog(LID.IRNotFoundVariableFrom, mv.token, "", mv.variableFrom.ToString() );
            }
            IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(mv.GetOwnerClassTemplateClass().GetHashCode());
            // 与 MetaMemberVariable 一致：显式左值类型用 define，var / 首赋局部推断用 real（define 常见为 object 占位）
            MetaType exportMt = mv.GetFinalMetaType() ?? mv.defineMetaType;
            m_IRMetaType = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(exportMt, owirmc);
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberEnum mme, int fieldIndex)
        {
            m_Id = mme.GetHashCode();
            m_Index = fieldIndex;
            m_Name = (irmc?.irName ?? string.Empty) + "." + mme.name;
            FillDebugInfo(mme, mme.name, "IRMetaMemberEnum");
            m_ExpressNode = mme.express ?? mme.enumValueExpress;
            m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            m_IsStatic = true;
            m_Permission = mme.permission;
            // 静态字段实际存放 Core.Member；底层 extends 类型在 defineMetaType，表达式值类型在 realMetaType。
            var exportMt = new MetaType(CoreMetaClassManager.memberMetaClass);
            m_IRMetaType = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(exportMt, irmc);
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberData mmd, int fieldIndex)
        {
            m_Id = mmd.GetHashCode();
            m_Index = fieldIndex;
            var ownerLabel = mmd.ownerMetaData?.allClassName ?? mmd.ownerMetaClass?.allClassName ?? string.Empty;
            m_Name = ownerLabel + "." + mmd.name;
            FillDebugInfo(mmd, mmd.name, "IRMetaMemberData");
            m_ExpressNode = mmd.expressNode;
            m_IRMetaVariableFrom = mmd.isStatic ? IRMetaVariableFrom.Static : IRMetaVariableFrom.Member;
            m_IsStatic = mmd.isStatic;
            m_IsConst = mmd.isConst;
            m_Permission = mmd.permission;
            MetaType mt = mmd.realMetaType ?? mmd.defineMetaType;
            if (mt != null)
                m_IRMetaType = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mt, irmc);
        }
        public IRMetaVariable( IRMetaClass irmc, MetaMemberVariable mmv, int index = -1 )
        {
            //m_MetaVariable = mmv;
            m_Id = mmv.GetHashCode();
            m_Index = index;
            m_Name = mmv.ownerMetaClass.allClassName + "." + mmv.name;
            FillDebugInfo(mmv, mmv.name, "IRMetaMemberVariable");
            m_ExpressNode = mmv.express;
            m_IsConst = mmv.isConst;
            m_IsStatic = mmv.isStatic;
            m_Permission = mmv.permission;
            if (mmv.isStatic || mmv.isConst )
                m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            else
                m_IRMetaVariableFrom = IRMetaVariableFrom.Member;

            IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(mmv.GetOwnerClassTemplateClass().GetHashCode());
            m_IRMetaType = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mmv.GetFinalMetaType(), owirmc);
        }

        private void FillDebugInfo(MetaBase mb, string fallbackName, string info)
        {
            m_DebugInfo = new DebugInfo
            {
                name = fallbackName ?? string.Empty,
                info = info ?? string.Empty,
            };

            if (mb == null)
            {
                return;
            }

            var tk = mb.token;
            if (tk == null && mb.pingTokenList != null && mb.pingTokenList.Count > 0)
            {
                tk = mb.pingTokenList[0];
            }

            if (tk == null)
            {
                return;
            }

            m_DebugInfo.path = tk.path ?? string.Empty;
            m_DebugInfo.beginLine = tk.sourceBeginLine;
            m_DebugInfo.beginChar = tk.sourceBeginChar;
            m_DebugInfo.endLine = tk.sourceEndLine;
            m_DebugInfo.endChar = tk.sourceEndChar;
            if (string.IsNullOrEmpty(m_DebugInfo.name))
            {
                m_DebugInfo.name = tk.lexeme?.ToString() ?? string.Empty;
            }
        }
        public override string ToString()
        {
            return name;
        }
    }
}
