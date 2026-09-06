//****************************************************************************
//  File:      IRMetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/11/15 12:00:00
//  Description: Meta class's ir attribute
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR.Types;
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
        /// <summary>成员短名（不含 owner 前缀），ref module 反向构建 MetaMemberVariable 时使用。</summary>
        public string shortName => m_ShortName;
        public int index => m_Index;
        public DebugInfo debugInfo => m_DebugInfo;
        public bool isStatic => m_IsStatic;
        public bool isConst => m_IsConst;
        public EPermission permission => m_Permission;
        public List<IRData> irDataList => m_IRDataList;
        /// <summary>方法参数是否有默认表达式（影响 isMust 匹配）。</summary>
        public bool isHasExpress => m_HasExpress;
        public void SetHasExpress(bool value) { m_HasExpress = value; }
        // 解析顺序：源自 MetaMemberVariable.parseOrder，
        // 用于 IR 导出 / VM 加载阶段按依赖解析次序排序初始化表达式。
        // -1 表示该 IRMetaVariable 不参与解析顺序排序（例如局部变量、参数）。
        public int order => m_Order;

        /// <summary>
        /// 导出名称列表（含原名 + Nickname 别名）。
        /// 从 MetaMemberVariable 的 attribute 中收集 @Nickname 得到。
        /// </summary>
        public List<string> exportNameList => m_ExportNameList;
        private List<string> m_ExportNameList = new List<string>();

        /// <summary>导出用的合并名称（逗号分隔），供 SLIR 序列化使用。</summary>
        public string exportNames => m_ExportNameList.Count <= 1 ? null : string.Join(",", m_ExportNameList);


        private MetaExpressNodeBase m_ExpressNode = null;
        private List<IRData> m_IRDataList = new List<IRData>();
        private IRMetaType m_IRMetaType = null;
        private IRMetaVariableFrom m_IRMetaVariableFrom = IRMetaVariableFrom.None;
        //private bool m_IsTemplate = false;
        private int m_Id = -1;
        private int m_Index = -1;
        private string m_Name = "";
        private string m_ShortName = "";
        private DebugInfo m_DebugInfo;
        private bool m_IsStatic = false;
        private bool m_IsConst = false;
        private EPermission m_Permission = EPermission.Public;
        private bool m_HasExpress = false;
        private int m_Order = -1;
        //private MetaVariable m_MetaVariable = null;

        public IRMetaVariable( MetaVariable mv, int index = -1 )
        {
            //m_MetaVariable = mv;
            m_Id = mv.GetHashCode();
            m_Index = index;
            m_Name = mv.ownerMetaBlockStatements?.ownerMetaFunction.name + (mv.isStatic?"_static":"_local") + "[" + mv.name + "]";
            FillDebugInfo(mv, mv.name, "IRMetaVariable");           
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
            else if( mv.variableFrom == MetaVariable.EVariableFrom.ClosureVariable
                   || mv.variableFrom == MetaVariable.EVariableFrom.ClosureContext )
            {
                m_IRMetaVariableFrom = IRMetaVariableFrom.LocalStatement;
            }
            else
            {
                Log.AddIRLog(LID.IRNotFoundVariableFrom, mv.token, "", mv.variableFrom.ToString() );
            }
            IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaVariable(mv);
            // 与 MetaMemberVariable 一致：显式左值类型用 define，var / 首赋局部推断用 real（define 常见为 object 占位）
            MetaType exportMt = mv.GetFinalMetaType();
            if( exportMt.isEnum )
            {
                IRMetaClass irmcmm = IRManager.instance.GetIRMetaClassById(CoreMetaClassManager.memberMetaClass.classId);
                m_IRMetaType = new IRMetaType(irmcmm);
            }
            else
            {
                m_IRMetaType = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(exportMt, owirmc);
            }
        }
        public IRMetaVariable(IRMetaClass irmc, MetaMemberEnum mme, int fieldIndex)
        {
            m_Id = mme.GetHashCode();
            m_Index = fieldIndex;
            m_Name = (irmc?.irName ?? string.Empty) + "." + mme.name;
            FillDebugInfo(mme, mme.name, "IRMetaMemberEnum");
            m_ExpressNode = mme.express;
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
            var ownerLabel = mmd.ownerMetaBase?.allName ?? string.Empty;
            m_Name = ownerLabel + "." + mmd.name;
            FillDebugInfo(mmd, mmd.name, "IRMetaMemberData");
            m_ExpressNode = mmd.expressNode;
            m_IRMetaVariableFrom = mmd.isStatic ? IRMetaVariableFrom.Static : IRMetaVariableFrom.Member;
            m_IsStatic = mmd.isStatic;
            m_Permission = mmd.permission;
            MetaType mt = mmd.GetFinalMetaType();
            if (mt != null)
                m_IRMetaType = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mt, irmc);
        }
        public IRMetaVariable( IRMetaClass irmc, MetaMemberVariable mmv, int index = -1 )
        {
            //m_MetaVariable = mmv;
            m_Id = mmv.GetHashCode();
            m_Index = index;
            m_Name = mmv.ownerMetaBase.allName + "." + mmv.name;
            FillDebugInfo(mmv, mmv.name, "IRMetaMemberVariable");
            m_ExpressNode = mmv.express;
            m_IsStatic = mmv.isStatic;
            m_Permission = mmv.permission;
            m_Order = mmv.parseOrder;
            if (mmv.isStatic )
                m_IRMetaVariableFrom = IRMetaVariableFrom.Static;
            else
                m_IRMetaVariableFrom = IRMetaVariableFrom.Member;

            IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaVariable(mmv);
            m_IRMetaType = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mmv.GetFinalMetaType(), owirmc);

            // 收集成员变量的 @Nickname 别名
            CollectExportNames(mmv);
        }
        public void SetIsStatic( bool iss )
        {
            this.m_IsStatic = iss;
        }
        public void AddIRDataList( List<IRData> irdataList )
        {
            m_IRDataList.AddRange(irdataList);
        }
        public void AddIRData( IRData irdata )
        {
            m_IRDataList.Add(irdata);
        }

        /// <summary>
        /// 从 MetaMemberVariable 的 attribute 中收集 @Nickname 别名，
        /// 加上原名组成 exportNameList。
        /// </summary>
        private void CollectExportNames(MetaMemberVariable mmv)
        {
            m_ExportNameList.Clear();
            if (mmv == null) return;
            // 原名（短名）
            if (!string.IsNullOrEmpty(mmv.name))
                m_ExportNameList.Add(mmv.name);
            // 收集 @Nickname
            var attrs = mmv.attributeList;
            if (attrs == null) return;
            foreach (var attr in attrs)
            {
                if (attr == null || attr.name != "Nickname") continue;
                string nickname = attr.GetStringArg(0);
                if (!string.IsNullOrEmpty(nickname) && !m_ExportNameList.Contains(nickname))
                    m_ExportNameList.Add(nickname);
            }
        }

        /// <summary>
        /// 从导出的 SLFieldPackage 直接构建，用于 ref module 导入。
        /// 保留导出端写入的全部信息（权限 / static / const / 短名 / 顺序 / 类型），
        /// 以便后续反向构建 MetaMemberVariable/Data/Enum 时无损复原。
        /// </summary>
        public IRMetaVariable(IRMetaClass owner, SimpleLanguage.Export.SLIR.Types.SLFieldPackage field, IRMetaType irmt, int fieldIndex)
        {
            var flags = field?.flags ?? 0;
            m_Id = (owner?.id ?? 0).GetHashCode() ^ (field?.name ?? "").GetHashCode() ^ fieldIndex;
            m_Index = fieldIndex;
            m_ShortName = field?.name ?? "";
            m_Name = (owner?.irName ?? string.Empty) + "." + m_ShortName;
            m_DebugInfo = new DebugInfo();
            // SLFieldPackage flags: 1=private, 2=public, 4=export, 8=protected, 16=const, 32=static
            m_IsStatic = (flags & 32) != 0;
            m_IsConst = (flags & 16) != 0;
            if ((flags & 1) != 0) m_Permission = EPermission.Private;
            else if ((flags & 4) != 0) m_Permission = EPermission.Export;
            else if ((flags & 8) != 0) m_Permission = EPermission.Protected;
            else m_Permission = EPermission.Public;
            m_Order = field?.order ?? -1;
            m_IRMetaVariableFrom = m_IsStatic ? IRMetaVariableFrom.Static : IRMetaVariableFrom.Member;
            m_IRMetaType = irmt ?? new IRMetaType(IRManager.instance.GetIRMetaClassByName("Core.Object"));
        }

        /// <summary>
        /// 从导出的 SLVariablePackage 直接构建，用于 ref module IRMethod 参数/返回值/局部变量。
        /// </summary>
        public IRMetaVariable(SLVariablePackage var, IRMetaType irmt, IRMetaVariableFrom from)
        {
            m_Id = var?.id ?? 0;
            m_Index = var?.index ?? -1;
            m_Name = var?.name ?? "";
            m_DebugInfo = new DebugInfo();
            m_IRMetaVariableFrom = from;
            m_IRMetaType = irmt ?? new IRMetaType(IRManager.instance.GetIRMetaClassByName("Core.Object"));
            m_HasExpress = var?.hasExpress ?? false;
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
