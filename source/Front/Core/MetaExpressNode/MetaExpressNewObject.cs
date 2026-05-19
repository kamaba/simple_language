//****************************************************************************
//  File:      MetaExpressNewObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/12/04 12:00:00
//  Description:   meta new object express!
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace SimpleLanguage.Core
{
    // a = { i1 = 10 } 这个过程式的处理
    public class MetaBraceAssignStatements
    {
        public enum EAssignTargetType
        {
            None,
            MemberVariable,
            MemberData,
            ArrayValue,
            AnonVariable,
        }

        public int opLevel => m_MetaExpress.opLevel;
        public MetaExpressNodeBase expressNode => m_MetaExpress;
        public int id => m_Id;
        public string defineName => m_DefineName;

        private MetaMemberVariable m_MetaMemberVariable;
        private MetaMemberData m_MetaMemberData;
        private MetaExpressNodeBase m_MetaExpress;
        private MetaBlockStatements m_OwnerMetaBlockStatements;
        private MetaType m_DefineMetaType = null;
        private MetaBase m_OwnerMetaBase = null;
        private MetaType m_NewObjectMetaType = null;
        private string m_DefineName;
        private bool m_AssignBlockedByConst = false;
        private int m_Id = 0;
        private EAssignTargetType m_AssignTargetType = EAssignTargetType.None;

        private Token m_Token = null;
        private Token m_AssignToken = null;
        private FileMetaSymbolTerm m_FileMetaSymbolTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;

        public MetaBraceAssignStatements(FileMetaOpAssignSyntax fmos, MetaType newmt, MetaBlockStatements mbs, MetaBase owmt, MetaType defineMt)
        {
            m_NewObjectMetaType = newmt;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = owmt;
            m_DefineMetaType = defineMt;
            if (fmos != null)
            {
                m_Token = fmos.token;
                m_AssignToken = fmos.assignToken;
                MetaVariable targetMetaVariable = null;
                if (fmos.variableRef.isOnlyName)
                {
                    MetaType targetMetaType = null;
                    m_DefineName = fmos.variableRef.name;
                    if (m_NewObjectMetaType != null && m_NewObjectMetaType.isData)
                    {
                        var md = m_NewObjectMetaType.metaData;
                        m_MetaMemberData = md.GetMemberDataByName(m_DefineName);
                        m_AssignTargetType = EAssignTargetType.MemberData;
                        if (m_MetaMemberData == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "没有找到对应的成员数据");
                            return;
                        }
                        m_Id = m_MetaMemberData.GetHashCode();

                        if ( m_MetaMemberData.realMetaType == null )
                        {
                            m_MetaMemberData.CreateMetaExpress();
                            m_MetaMemberData.ParseMetaExpress();
                            m_MetaMemberData.ParseRealMetaType();
                        }
                        targetMetaType = m_MetaMemberData.GetFinalMetaType();
                        if (targetMetaType == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "没有找到对象类型");
                            return;
                        }

                        targetMetaVariable = m_MetaMemberData;
                    }
                    else
                    {
                        m_MetaMemberVariable = m_NewObjectMetaType.metaClass.GetMetaMemberVariableByName(m_DefineName);
                        m_AssignTargetType = EAssignTargetType.MemberVariable;
                        if(m_MetaMemberVariable == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "没有找到对应的成员变量");
                            return;
                        }
                        m_Id = m_MetaMemberVariable.GetHashCode();
                        if ( m_MetaMemberVariable.realMetaType == null )
                        {
                            m_MetaMemberVariable.CreateMetaExpress();
                            m_MetaMemberVariable.ParseMetaExpress();
                            m_MetaMemberVariable.ParseRealMetaType();
                        }
                        targetMetaType = m_MetaMemberVariable.GetFinalMetaType();
                        if (targetMetaType == null)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "没有找到对象类型");
                            return;
                        }
                        targetMetaVariable = m_MetaMemberVariable;
                    }

                    m_DefineMetaType = targetMetaType;
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.fme = fmos.express;
                    cep.equalMetaVariable = targetMetaVariable;
                    cep.metaType = m_DefineMetaType;
                    cep.ownerMBS = mbs;
                    cep.ownerMetaBase = owmt;
                    m_MetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);

                }
                else
                {
                    Log.AddMetaCoreLog( LID.MetaCoreAssertShowMessage, m_Token, "Error 在类" + mbs.ownerMetaClass?.allClassName + "函数: " + mbs.ownerMetaFunction.name
                        + " 语句: " + fmos.variableRef.ToTokenString());
                }
            }
        }
        public MetaBraceAssignStatements(FileMetaDefineVariableSyntax fmdvs, MetaType newmt, MetaBlockStatements mbs, MetaBase owmt, MetaType defineMt )
        {
            m_NewObjectMetaType = newmt;
            m_OwnerMetaBase = owmt;
            m_OwnerMetaBlockStatements = mbs;
            m_DefineMetaType = defineMt;
            if (fmdvs != null)
            {
                m_Token = fmdvs.nameToken;
                m_AssignToken = fmdvs.assignToken;

                m_DefineName = fmdvs.name;

                var fmcd = fmdvs.fileMetaClassDefine;
                var mdt = TypeManager.instance.GetMetaTypeByTemplateFunction(mbs.ownerMetaClass, mbs.ownerMetaFunction as MetaMemberFunction, fmcd);
                if (mdt == null)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreNotFoundMetaTypeByFMClassDefine, fmcd.classNameToken, "MetaBraceAssignStatements", fmcd.name);
                    return;
                }

                if (m_NewObjectMetaType != null && m_NewObjectMetaType.isData)
                {
                    m_MetaMemberData = MetaMemberData.CreateDeclared(m_NewObjectMetaType.metaData, m_DefineName, -1, mdt, true);
                    m_MetaMemberData.SetOwnerBlockstatements(mbs);
                    m_Id = m_MetaMemberData.GetHashCode();
                }
                else
                {
                    m_MetaMemberVariable = new MetaMemberVariable((MetaClass)null, m_DefineName);
                    m_MetaMemberVariable.SetOwnerMetaClass(m_NewObjectMetaType?.metaClass ?? mbs.ownerMetaClass);
                    m_MetaMemberVariable.SetOwnerBlockstatements(mbs);
                    m_MetaMemberVariable.SetMetaDefineType(mdt);
                    m_MetaMemberVariable.SetRealMetaType(new MetaType(mdt));
                    m_MetaMemberVariable.SetIsDefineMetaType(true);
                    m_Id = m_MetaMemberVariable.GetHashCode();
                }

                m_AssignTargetType = m_NewObjectMetaType != null && m_NewObjectMetaType.isData
                    ? EAssignTargetType.MemberData
                    : EAssignTargetType.MemberVariable;

                var fileExpress = fmdvs.express;
                var targetMetaVariable = (MetaVariable)m_MetaMemberData ?? m_MetaMemberVariable;
                var targetMetaType = targetMetaVariable?.defineMetaType != null ? new MetaType(targetMetaVariable.defineMetaType) : new MetaType(CoreMetaClassManager.objectMetaClass);

                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = fileExpress;
                cep.equalMetaVariable = targetMetaVariable;
                cep.metaType = targetMetaType;
                cep.ownerMBS = mbs;
                cep.ownerMetaBase = mbs.ownerMetaClass;
                m_MetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
            }
        }
        public MetaBraceAssignStatements(FileMetaCallTerm fmct, MetaType newmt, MetaBlockStatements mbs, MetaBase owmb, MetaType defineMt )
        {
            m_NewObjectMetaType = newmt;
            m_DefineMetaType = defineMt;
            m_OwnerMetaBase = owmb;
            m_FileMetaCallTerm = fmct;
            m_Token = fmct.token;
            m_MetaExpress = new MetaCallLinkExpressNode(fmct.callLink, defineMt.metaClass, mbs, null);
        }
        public MetaBraceAssignStatements(FileMetaSymbolTerm fmst, MetaType newmt,  MetaBlockStatements mbs, MetaBase owmb, MetaType defintMt )
        {
            m_NewObjectMetaType = newmt;
            m_FileMetaSymbolTerm = fmst;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = owmb;
            m_Token = fmst.token;
            m_DefineMetaType = defintMt;

            if (m_DefineMetaType.isMap )
            {
                if( fmst.symBolType != ETokenType.Colon )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "在Map里边，必须使用:个符号");
                    return;
                }
            }
            else
            {
                if (fmst.symBolType != ETokenType.Assign )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "在class或者是data里边，必须使用=个符号");
                    return;
                }
            }
            if (fmst.left is not FileMetaCallTerm fmct1)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "在class或者是data里边，前值应该使用filemetaCallTerm");
                return;
            }

            if( fmct1.callLink.callNodeList.Count > 0 )
            {
                m_DefineName = fmct1.callLink.callNodeList[fmct1.callLink.callNodeList.Count - 1].name;
            }
            if (m_DefineMetaType.isDynamicData || m_DefineMetaType.isDynamicClass )
            {
                if (m_DefineMetaType.isDynamicClass)
                {
                    m_MetaMemberVariable = new MetaMemberVariable((MetaClass)null, m_DefineName);
                    m_MetaMemberVariable.SetOwnerMetaClass(mbs.ownerMetaClass);
                    m_MetaMemberVariable.SetOwnerBlockstatements(mbs);
                    m_Id = m_MetaMemberVariable.GetHashCode();
                }
                else
                {
                    m_MetaMemberData = MetaMemberData.CreateDeclared(m_NewObjectMetaType.metaData, m_DefineName, -1, new MetaType(CoreMetaClassManager.objectMetaClass), false);
                    m_MetaMemberData.SetOwnerBlockstatements(m_OwnerMetaBlockStatements);
                    m_Id = m_MetaMemberData.GetHashCode();
                }
            }
            else
            {
                if (m_DefineMetaType.isData)
                {
                    m_MetaMemberData = m_DefineMetaType.metaData?.GetMemberDataByName(m_DefineName);
                    if (m_MetaMemberData == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 在类" + m_NewObjectMetaType.name + "函数: " + mbs?.ownerMetaFunction.name
                            + " 没有找到: 类" + m_NewObjectMetaType.name + " 变量:" + m_DefineName);
                    }
                    m_Id = m_MetaMemberData.GetHashCode();
                    //m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberData, m_OwnerMetaBlockStatements, m_FileMetaOpAssignSyntax?.express);
                }
                else if (m_DefineMetaType.isEnum)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "-----------------------------------Enum-------------------------");
                }
                else
                {
                    m_MetaMemberVariable = m_DefineMetaType.metaClass.GetMetaMemberVariableByName(m_DefineName);
                    if (m_MetaMemberVariable == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 在类" + m_DefineMetaType.metaClass?.allClassName + "函数: " + mbs?.ownerMetaFunction.name
                            + " 没有找到: 类" + m_DefineMetaType.metaClass?.allClassName + " 变量:" + m_DefineName);
                    }
                    m_Id = m_MetaMemberVariable.GetHashCode();
                    //m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberVariable, m_OwnerMetaBlockStatements, m_FileMetaOpAssignSyntax?.express);
                }
            }

            if (m_DefineMetaType.isDynamicData || m_DefineMetaType.isDynamicClass)
            {
                m_AssignTargetType = EAssignTargetType.AnonVariable;
            }
            else if (m_DefineMetaType.isEnum)
            {
                m_AssignTargetType = EAssignTargetType.None;
            }
            else if (m_DefineMetaType.isData)
            {
                m_AssignTargetType = EAssignTargetType.MemberData;
            }
            else
            {
                m_AssignTargetType = EAssignTargetType.MemberVariable;
            }

            if(fmst.right != null )
            {
                var targetMetaVariable = (MetaVariable)m_MetaMemberData ?? m_MetaMemberVariable;
                var targetMetaType = targetMetaVariable?.defineMetaType != null ? new MetaType(targetMetaVariable.defineMetaType) : new MetaType(CoreMetaClassManager.objectMetaClass);
                CreateExpressParam cep = new CreateExpressParam();
                cep.fme = fmst.right;
                cep.equalMetaVariable = targetMetaVariable;
                cep.metaType = targetMetaType;
                cep.ownerMBS = m_OwnerMetaBlockStatements;
                cep.ownerMetaBase = m_OwnerMetaBlockStatements.ownerMetaClass;
                m_MetaExpress = ExpressManager.CreateExpressNodeByCEP(cep);
            }
        }


        public MetaBraceAssignStatements( MetaType newmt, MetaBlockStatements mbs, MetaBase owmb, MetaType defineMt, MetaExpressNodeBase men )
        {
            m_NewObjectMetaType = newmt;
            m_OwnerMetaBase = owmb;
            m_OwnerMetaBlockStatements = mbs;
            m_DefineMetaType = defineMt;
            m_MetaExpress = men;
            m_Token = men.token;
            m_AssignTargetType = EAssignTargetType.ArrayValue;
        }
        public MetaBraceAssignStatements(MetaMemberVariable mmv, MetaBlockStatements mbs, MetaBase owmt, MetaExpressNodeBase men )
        {
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = owmt;
            m_MetaExpress = men;
            m_Token = men.token;
            this.m_MetaMemberVariable = mmv;
            m_DefineMetaType = mmv.GetFinalMetaType();
            m_AssignTargetType = EAssignTargetType.MemberVariable;
        }
        /// <summary>
        /// 对象初始化列表中的 data 字段赋值（目标为 <see cref="MetaMemberData"/>），与 <see cref="MetaMemberVariable"/> 分支对称。
        /// </summary>
        public MetaBraceAssignStatements(MetaMemberData mmd, MetaBlockStatements mbs, MetaBase owmt, MetaExpressNodeBase men, bool isAnon )
        {
            m_MetaMemberData = mmd;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = owmt;
            m_MetaExpress = men;
            m_Token = men.token;
            m_AssignTargetType = isAnon ? EAssignTargetType.AnonVariable : EAssignTargetType.MemberData;
            m_DefineMetaType = new MetaType(mmd.defineMetaType);
        }


        public void SetMetaMemberVariable(MetaMemberVariable mmv)
        {
            this.m_MetaMemberVariable = mmv;
            this.m_AssignTargetType = EAssignTargetType.MemberVariable;
        }
        public void SetMetaMemberData(MetaMemberData mmd)
        {
            this.m_MetaMemberData = mmd;
            this.m_AssignTargetType = EAssignTargetType.MemberData;
        }
        public void Parse( AllowUseSettings aus )
        {
            MetaVariable mv = null;
            switch( m_AssignTargetType )
            {
                case EAssignTargetType.MemberData:
                    {
                        if (m_MetaMemberData != null && m_MetaMemberData.isConst)
                        {
                            m_AssignBlockedByConst = true;
                            m_MetaExpress = null;
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_MetaMemberData.token,
                                "const 成员不允许在对象初始化中使用 '=' 重新赋值: " + m_MetaMemberData.name);
                            return;
                        }
                        mv = m_MetaMemberData;
                    }
                    break;
                case EAssignTargetType.MemberVariable:
                    {
                        if (m_MetaMemberVariable != null && m_MetaMemberVariable.isConst)
                        {
                            m_AssignBlockedByConst = true;
                            m_MetaExpress = null;
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_MetaMemberVariable.token,
                                "const 成员不允许在对象初始化中使用 '=' 重新赋值: " + m_MetaMemberVariable.name);
                            return;
                        }
                        mv = m_MetaMemberVariable;
                    }
                    break;
            }
            if( m_MetaExpress != null )
            {
                m_MetaExpress.Parse(aus);
                m_MetaExpress = ExpressManager.ConvertNewExpress(m_MetaExpress, m_DefineMetaType, mv );
            }
        }
        public MetaType GetRetMetaType()
        {
            if (m_MetaExpress != null)
            {
                return m_MetaExpress.GetReturnMetaType();
            }
            return null;
        }
        public void CalcReturnType()
        {
            if (m_MetaExpress != null)
            {
                m_MetaExpress.CalcReturnType();
                var expressRetMetaType = m_MetaExpress.GetReturnMetaType();

                switch( m_AssignTargetType )
                {
                    case EAssignTargetType.MemberVariable:
                        {
                            MetaType retMetaType = m_MetaMemberVariable.GetFinalMetaType();
                            MetaClass ownerMetaClass = m_MetaMemberVariable.ownerMetaClass;
                        }
                        break;
                    case EAssignTargetType.MemberData:
                        {
                            if (expressRetMetaType != null)
                            {
                            }
                        }
                        break;
                }

                // If array element type is explicitly declared (and not object),
                // force const literal conversion to target element type instead of numeric promotion.
                if (m_DefineMetaType != null
                    && m_DefineMetaType.metaClass != CoreMetaClassManager.objectMetaClass
                    && m_MetaExpress is MetaConstExpressNode constExpressNode)
                {
                    if (!TypeManager.TryAdjustConstExpressByDefineMetaType(constExpressNode, expressRetMetaType ))
                    {
                        return;
                    }
                }
            }
            else
            {
                if (!m_AssignBlockedByConst)
                {
                    System.Diagnostics.Debug.Assert(false);
                    System.Diagnostics.Debug.Write("使用{}赋值，表达式不允许为空!!");
                }
            }
        }
        /// <summary>
        /// new / 字面量初始化场景中，数组模板元素类型对齐（含嵌套数组）。
        /// </summary>
        internal static bool TryArrayElementAssignableForNewObject(MetaType targetArray, MetaType exprArray)
        {
            if (targetArray == null || exprArray == null) return false;
            if (!targetArray.IsArray() || !exprArray.IsArray()) return false;

            if (TypeManager.CompareMetaType(targetArray, exprArray))
            {
                return true;
            }

            var targetArgs0 = targetArray.GetGenTemplateMetaTypeList();
            var exprArgs0 = exprArray.GetGenTemplateMetaTypeList();
            if (targetArgs0 != null && exprArgs0 != null && targetArgs0.Count == 1 && exprArgs0.Count == 1)
            {
                var targetElement0 = targetArgs0[0];
                var exprElement0 = exprArgs0[0];
                if (targetElement0 != null && targetElement0.metaClass == CoreMetaClassManager.objectMetaClass)
                {
                    return true;
                }
                if (targetElement0 != null && exprElement0 != null && targetElement0.IsArray() && exprElement0.IsArray())
                {
                    return TryArrayElementAssignableForNewObject(targetElement0, exprElement0);
                }
            }

            var targetTemplate = targetArray.GetTemplateMetaClass();
            var exprTemplate = exprArray.GetTemplateMetaClass();
            if (targetTemplate != exprTemplate) return false;

            var targetArgs = targetArray.GetGenTemplateMetaTypeList();
            var exprArgs = exprArray.GetGenTemplateMetaTypeList();
            if (targetArgs == null || exprArgs == null || targetArgs.Count != exprArgs.Count || targetArgs.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < targetArgs.Count; i++)
            {
                var tArg = targetArgs[i];
                var eArg = exprArgs[i];
                if (!IsBraceAssignDeclaredCompatibleWithExpress(tArg, eArg))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 双动态（匿名推断）data：在 <see cref="MetaData.allClassName"/> 已一致的前提下，按字段名递归比对 <see cref="MetaMemberData.defineMetaType"/>。
        /// </summary>
        private static bool TryAnonymousDynamicDataStructuralCompatible(MetaData declaredData, MetaData expressData)
        {
            var dictD = declaredData.metaMemberDataDict;
            var dictE = expressData.metaMemberDataDict;
            if (dictD.Count != dictE.Count)
            {
                return false;
            }

            foreach (var kv in dictD)
            {
                if (!dictE.TryGetValue(kv.Key, out var exprMd))
                {
                    return false;
                }

                var dt = kv.Value?.defineMetaType;
                var et = exprMd?.defineMetaType;
                if (dt == null || et == null)
                {
                    return false;
                }

                if (!IsBraceAssignDeclaredCompatibleWithExpress(dt, et))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 声明侧与表达式返回类型在「<c>{}</c>/<c>[]</c> 初始化」语境下是否兼容：
        /// 先结构模板等价；数组走 <see cref=""/> 递归模板实参；
        /// <b>enum</b> 要求同一宿主且（若均带成员）name 或 index 一致；
        /// <b>data</b> 双动态先比类型全名再比各字段定义类型；具名 data 实例一致则通过；
        /// <b>class / 原语</b> 继承/接口/Num，其中核心数值按窄→宽拓宽规则收紧（与调用点 <see cref="ClassManager.IsNarrowerCorePrimitiveWideningOkForCallSite"/> 一致）。
        /// </summary>
        internal static bool IsBraceAssignDeclaredCompatibleWithExpress(MetaType declared, MetaType express)
        {
            if (declared == null || express == null)
            {
                return false;
            }
            if( declared.metaClass == CoreMetaClassManager.objectMetaClass )
            {
                return true;
            }

            if (TypeManager.CompareMetaType(declared, express))
            {
                return true;
            }

            if (declared.IsArray() && express.IsArray())
            {
                return TryArrayElementAssignableForNewObject(declared, express);
            }

            if (declared.isEnum && express.isEnum)
            {
                var de = declared.metaEnum;
                var ee = express.metaEnum;
                if (de != null && ee != null)
                {
                    bool sameEnumHost = ReferenceEquals(de, ee)
                        || string.Equals(de.allClassName, ee.allClassName, StringComparison.Ordinal);
                    if (!sameEnumHost)
                    {
                        return false;
                    }

                    var dv = declared.enumValue;
                    var ev = express.enumValue;
                    if (dv != null && ev != null)
                    {
                        return string.Equals(dv.name, ev.name, StringComparison.Ordinal)
                            || dv.index == ev.index;
                    }

                    return true;
                }
            }

            if (declared.isData && express.isData)
            {
                var dd = declared.metaData;
                var ed = express.metaData;
                if (dd != null && ed != null && dd.isDynamic && ed.isDynamic)
                {
                    if (!string.Equals(dd.allClassName, ed.allClassName, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    return TryAnonymousDynamicDataStructuralCompatible(dd, ed);
                }

                if (dd != null && ed != null && ReferenceEquals(dd, ed))
                {
                    return true;
                }
            }

            var dc = declared.metaClass;
            var ec = express.metaClass;
            if (dc == null || ec == null)
            {
                return false;
            }

            var relation = ClassManager.ValidateClassRelationByMetaClass(dc, ec);
            if (relation == EClassRelation.Same
                || relation == EClassRelation.Child
                || relation == EClassRelation.Interface)
            {
                return true;
            }

            if (relation == EClassRelation.Num)
            {
                return ClassManager.IsNarrowerCorePrimitiveWideningOkForCallSite(ec, dc);
            }

            return false;
        }
        /// <summary>
        /// 将成员/槽位声明类型（<see cref="m_DefineMetaType"/> 或成员上的 define）与本句右值返回类型对撞。
        /// 通过 <see cref="m_AssignTargetType"/> 区分 class/data 成员、匿名 data 字段、数组字面量槽位等场景。
        /// </summary>
        public void ValidateDefineAgainstDeclaredMetaType()
        {
            MetaVariable assignMemberMv = (MetaVariable)m_MetaMemberData ?? m_MetaMemberVariable;
            MetaType contentMt = GetRetMetaType();
            MetaType defineMt = m_DefineMetaType;

            // 数组字面量槽位须使用本句的槽位类型对撞；不要用外层左值成员类型回填 define（否则与嵌套 int[][] 等语义不符）。
            if (defineMt == null
                && m_AssignTargetType != EAssignTargetType.ArrayValue
                && assignMemberMv?.defineMetaType != null)
            {
                defineMt = new MetaType(assignMemberMv.defineMetaType);
            }

            if (defineMt == null)
            {
                return;
            }

            // class/data 初始化列表中「成员类型为数组、右值为整条数组字面量」：对齐数组类型本身；
            // 数组字面量 ArrayValue 子槽位虽可能挂外层 context 成员，但不走「成员整段数组赋值」分支。
            bool memberWholeArrayAssign =
                m_AssignTargetType != EAssignTargetType.ArrayValue
                && assignMemberMv != null
                && assignMemberMv.isDefineMetaType
                && defineMt.IsArray()
                && contentMt != null
                && contentMt.IsArray();

            if (memberWholeArrayAssign)
            {
                if (!IsBraceAssignDeclaredCompatibleWithExpress(defineMt, contentMt))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_AssignToken, "里边的元素与边的数据类型不对应，不对应，需要调整数据，或者是定义的结构 ");
                }
                return;
            }

            bool isOmittedExpression = expressNode == null;

            if (defineMt.IsArray())
            {
                var cmt = defineMt.GetMetaTypeByIndex(0);
                bool isNumLike =
                    cmt != null
                    && (ClassManager.IsNumberClass(cmt.metaClass) || ClassManager.IsAbstractNumberMetaType(cmt));
                // 与左值 / 成员声明的 Array<元素> 上可空一致：元素类型带 ? 或声明的模板实参为可空
                bool allowNullableForNumericElement =
                    (cmt?.isNullable == true)
                    || ( defineMt != null
                        && defineMt.IsArray()
                        && defineMt.GetMetaTypeByIndex(0)?.isNullable == true);
                bool isNullLiteral = contentMt != null && contentMt.isNull;
                if (isNumLike && (isOmittedExpression || isNullLiteral))
                {
                    if (!allowNullableForNumericElement)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                            "数组元素为数值/Num 类型时，仅当元素类型可空（?）或左值声明为 Array<可空类型> 时才允许空位或 null 字面量。");
                    }
                    return;
                }
                if (!isNumLike && (isOmittedExpression || isNullLiteral))
                {
                    // 非数值/Num：空位与 null 字面量均不在这里做强类型对撞
                    return;
                }
            }

            if (contentMt == null)
            {
                return;
            }

            if (defineMt.IsArray())
            {
                var cmt = defineMt.GetMetaTypeByIndex(0);

                bool isMatch;
                if (cmt?.GetTemplateMetaClass() == CoreMetaClassManager.objectMetaClass)
                {
                    // Array<Object> 允许任意元素类型。
                    isMatch = true;
                }
                else
                {
                    isMatch = IsBraceAssignDeclaredCompatibleWithExpress(cmt, contentMt);
                }

                if (!isMatch)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "里边的元素与边的数据类型不对应，不对应，需要调整数据，或者是定义的结构 ");
                }
            }
            else
            {
                if (!IsBraceAssignDeclaredCompatibleWithExpress(defineMt, contentMt))
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "里边的元素与外边定义的类11，不对应，需要调整数据，或者是定义的结构 ");
                }
            }
        }

        public string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_DefineName);
            sb.Append(m_AssignToken?.lexeme.ToString());
            sb.Append(m_MetaExpress?.ToFormatString());

            return sb.ToString();
        }
        public override string ToString()
        {
            return ToFormatString();
        }

        /// <summary>
        /// 数组/大括号字面值中多项赋值语句的公共元素类型推导（与原先 <c>MetaNewObjectStatementsContent.GetMaxLevelMetaType</c> 一致）。
        /// </summary>
        public static MetaType GetMaxLevelMetaType(IReadOnlyList<MetaBraceAssignStatements> assignStatementsList, MetaType defineMetaType)
        {
            var objmt = new MetaType(CoreMetaClassManager.objectMetaClass);
            if (assignStatementsList == null || assignStatementsList.Count == 0)
            {
                return objmt;
            }

            if (TryGetPreferredElementMetaTypeFromDefine(defineMetaType, out var preferredElementMetaType))
            {
                bool allAssignableToPreferred = true;
                for (int i = 0; i < assignStatementsList.Count; i++)
                {
                    var itemType = assignStatementsList[i].GetRetMetaType();
                    if (!IsArrayLiteralElementAssignableToTarget(preferredElementMetaType, itemType))
                    {
                        allAssignableToPreferred = false;
                        break;
                    }
                }

                if (allAssignableToPreferred)
                {
                    return new MetaType(preferredElementMetaType);
                }
            }

            if (assignStatementsList.Count == 1)
            {
                var only = assignStatementsList[0].GetRetMetaType();
                if (only == null || only.isNull)
                {
                    return objmt;
                }
                return only;
            }

            var types = new List<MetaType>(assignStatementsList.Count);
            for (int i = 0; i < assignStatementsList.Count; i++)
            {
                var t = assignStatementsList[i].GetRetMetaType();
                if (t == null || t.isNull)
                {
                    return objmt;
                }
                types.Add(t);
            }

            bool allNumeric = true;
            for (int i = 0; i < types.Count; i++)
            {
                if (!ClassManager.IsNumberClass(types[i].metaClass))
                {
                    allNumeric = false;
                    break;
                }
            }
            if (allNumeric)
            {
                bool hasInt64 = false;
                bool hasUInt64 = false;
                int maxRank = int.MinValue;
                for (int i = 0; i < types.Count; i++)
                {
                    var numericClass = types[i].metaClass;
                    if (numericClass == CoreMetaClassManager.int64MetaClass)
                    {
                        hasInt64 = true;
                    }
                    else if (numericClass == CoreMetaClassManager.uint64MetaClass)
                    {
                        hasUInt64 = true;
                    }

                    if (!NumberManager.TryGetLiteralPromotionRank(types[i].metaClass, out int rank))
                    {
                        return objmt;
                    }
                    if (rank > maxRank)
                    {
                        maxRank = rank;
                    }
                }

                if (hasInt64 && hasUInt64)
                {
                    return objmt;
                }

                var promotedMc = NumberManager.GetMetaClassForLiteralPromotionRank(maxRank);
                return promotedMc != null ? new MetaType(promotedMc) : objmt;
            }

            int frontOpLevel = 0;
            var mt = new MetaType(CoreMetaClassManager.objectMetaClass);
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            bool isAllSame = true;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
            for (int i = 0; i < assignStatementsList.Count - 1; i++)
            {
                MetaBraceAssignStatements cmc = assignStatementsList[i];
                MetaBraceAssignStatements nmc = assignStatementsList[i + 1];

                var cmcmt = cmc.GetRetMetaType();
                var nmcmt = nmc.GetRetMetaType();
                if (cmcmt.isNull)
                {
                    return objmt;
                }
                if (nmcmt.isNull)
                {
                    return objmt;
                }
                if (!TypeManager.CompareMetaType(cmcmt, nmcmt))
                {
                    if (cmcmt.IsArray() && nmcmt.IsArray()
                        && TryGetCompatibleArrayMetaType(cmcmt, nmcmt, out var compatibleArrayMetaType))
                    {
                        mt = compatibleArrayMetaType;
                        frontOpLevel = cmc.opLevel > nmc.opLevel ? cmc.opLevel : nmc.opLevel;
                        isAllSame = true;
                        continue;
                    }
                    return objmt;
                }
                if (cmc.opLevel == nmc.opLevel && nmc.opLevel > frontOpLevel)
                {
                    if (cmc.opLevel == 10)
                    {
                        var cutmt = cmc.GetRetMetaType();
                        var nextmt = nmc.GetRetMetaType();
                        var cur = cutmt.metaClass;
                        var next = nextmt.metaClass;
                        var relation = ClassManager.ValidateClassRelationByMetaClass(cur, next);
                        if (relation == EClassRelation.Same
                            || relation == EClassRelation.Child)
                        {
                            mt = nextmt;
                            frontOpLevel = cmc.opLevel;
                        }
                        else if (relation == EClassRelation.Parent)
                        {
                            mt = cutmt;
                        }
                        else
                        {
                            isAllSame = false;
                            break;
                        }
                    }
                    else
                    {
                        var currentType = cmc.GetRetMetaType();
                        var nextType = nmc.GetRetMetaType();
                        if (currentType != null && nextType != null
                            && currentType.IsArray() && nextType.IsArray()
                            && TryGetCompatibleArrayMetaType(currentType, nextType, out var compatibleArrayMetaType2))
                        {
                            mt = compatibleArrayMetaType2;
                            frontOpLevel = cmc.opLevel;
                            isAllSame = true;
                        }
                        else
                        {
                            mt = currentType;
                            frontOpLevel = cmc.opLevel;
                            isAllSame = true;
                        }
                    }

                }
                else
                {
                    var currentType = cmc.GetRetMetaType();
                    var nextType = nmc.GetRetMetaType();
                    if (currentType != null && nextType != null
                        && currentType.IsArray() && nextType.IsArray()
                        && TryGetCompatibleArrayMetaType(currentType, nextType, out var compatibleArrayMetaType3))
                    {
                        mt = compatibleArrayMetaType3;
                        frontOpLevel = Math.Max(cmc.opLevel, nmc.opLevel);
                        isAllSame = true;
                        continue;
                    }

                    if (nmc.opLevel > frontOpLevel)
                    {
                        if (cmc.opLevel > nmc.opLevel)
                        {
                            frontOpLevel = cmc.opLevel;
                            mt = cmc.GetRetMetaType();
                        }
                        else
                        {
                            frontOpLevel = nmc.opLevel;
                            mt = nmc.GetRetMetaType();
                        }
                    }
                }
            }
            return mt;
        }

        private static bool TryGetPreferredElementMetaTypeFromDefine(MetaType defineMetaType, out MetaType preferredElementMetaType)
        {
            preferredElementMetaType = null;
            if (defineMetaType == null || !defineMetaType.IsArray())
            {
                return false;
            }

            var defineTemplateList = defineMetaType.GetGenTemplateMetaTypeList();
            if (defineTemplateList == null || defineTemplateList.Count != 1)
            {
                return false;
            }

            preferredElementMetaType = defineTemplateList[0];
            return preferredElementMetaType != null;
        }

        private static bool IsArrayLiteralElementAssignableToTarget(MetaType targetMetaType, MetaType sourceMetaType)
        {
            if (targetMetaType == null || sourceMetaType == null)
            {
                return false;
            }

            if (TypeManager.CompareMetaType(targetMetaType, sourceMetaType))
            {
                return true;
            }

            if (targetMetaType.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                return true;
            }

            if (targetMetaType.IsArray() && sourceMetaType.IsArray())
            {
                var targetArgs = targetMetaType.GetGenTemplateMetaTypeList();
                var sourceArgs = sourceMetaType.GetGenTemplateMetaTypeList();
                if (targetArgs == null || sourceArgs == null || targetArgs.Count != 1 || sourceArgs.Count != 1)
                {
                    return false;
                }

                return IsArrayLiteralElementAssignableToTarget(targetArgs[0], sourceArgs[0]);
            }

            if (targetMetaType.IsArray() && sourceMetaType.metaClass == CoreMetaClassManager.objectMetaClass)
            {
                return true;
            }

            return false;
        }

        public static bool TryGetCompatibleArrayMetaType(MetaType leftArray, MetaType rightArray, out MetaType result)
        {
            result = null;
            if (leftArray == null || rightArray == null) return false;
            if (!leftArray.IsArray() || !rightArray.IsArray()) return false;

            var leftTemplate = leftArray.GetTemplateMetaClass();
            var rightTemplate = rightArray.GetTemplateMetaClass();
            if (leftTemplate != rightTemplate) return false;

            var leftArgs = leftArray.GetGenTemplateMetaTypeList();
            var rightArgs = rightArray.GetGenTemplateMetaTypeList();
            if (leftArgs == null || rightArgs == null || leftArgs.Count != rightArgs.Count || leftArgs.Count == 0)
            {
                return false;
            }

            var leftElement = leftArgs[0];
            var rightElement = rightArgs[0];

            if (TypeManager.CompareMetaType(leftElement, rightElement))
            {
                result = new MetaType(leftArray);
                return true;
            }

            if (leftElement.IsArray() && rightElement.IsArray())
            {
                if (!TryGetCompatibleArrayMetaType(leftElement, rightElement, out var nestedCompatible))
                {
                    return false;
                }

                MetaType build = new MetaType();
                build.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                build.AddDefineTemplateMetaType(nestedCompatible);
                result = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(build, true, out bool _);

                if (leftArray.arrayLength != -1)
                {
                    result.SetArrayLength(leftArray.arrayLength);
                }
                else if (rightArray.arrayLength != -1)
                {
                    result.SetArrayLength(rightArray.arrayLength);
                }
                return true;
            }

            return false;
        }
    }



    public sealed class MetaNewObjectExpressNode : MetaExpressNodeBase
    {
        public enum EStatementsContentType
        {
            None,
            ArrayValue,
            ClassValueAssign,
            DataValueAssign,
            DynamicClass,
            DynamicData,
        }

        public enum ENewType
        {
            DefaultType, //int32,uint32/string/..
            CommomClass,  //define class
            ArrayClass,     // array class
            ListClass,
            MapClass,
        }

        public bool needInitMemberVariable => m_NeedInitMemberVariable;
        public ENewType newType => m_NewType;
        public int arrayLength => m_ExpressReturnMetaType.arrayLength;
        public MetaExpressNodeBase arrayLengthExpress => m_ArrayLengthExpress;
        public List<MetaExpressNodeBase> metaInputParamList => m_MetaInputParamList;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public MetaVariable storeMetaVariable => m_StoreMetaVariable;
        public List<MetaBraceAssignStatements> assignStatementsList => m_AssignStatementsList;
        public int braceAssignCount => m_AssignStatementsList.Count;
        /// <summary>大括号/数组字面量左侧被赋值的变量（与原 <c>metaContent.equalMetaVariable</c> 一致）。</summary>
        public MetaVariable equalMetaVariable => m_StoreMetaVariable;
        public EStatementsContentType statementsContentType => m_StatementsContentType;

        /// <summary>
        /// 为 true 表示语法上已写明数组元素类型（如 <c>Array&lt;Int16&gt;(n){ ... }</c> 等由调用链构造的数组），
        /// 与仅由 <c>[1,2,3]</c> 推断的元素类型不同；赋值时不对左值做跨数值基类型的字面量强转。
        /// </summary>
        public bool usesExplicitArrayElementTypeSyntax => m_UsesExplicitArrayElementTypeSyntax;

        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        private List<FileMetaBraceTerm> m_FileMetaBraceTermList = new List<FileMetaBraceTerm>();
        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;

        private MetaExpressNodeBase m_MetaEnumValue = null;
        private readonly List<MetaBraceAssignStatements> m_AssignStatementsList = new List<MetaBraceAssignStatements>();
        private FileMetaBaseTerm m_BraceFileMetaBaseTerm = null;
        private MetaData m_BraceNewMetaData = null;
        private MetaData m_BraceNewTempMetaData = null;
        private EStatementsContentType m_StatementsContentType = EStatementsContentType.None;
        private ENewType m_NewType = ENewType.CommomClass;
        private bool m_NeedInitMemberVariable = true;

        private MetaType m_DefineMetaType = null;
        private MetaType m_NewMetaType = null;
        private MetaType m_ArrayCalcMetaType = null;
        private bool m_UsesExplicitArrayElementTypeSyntax = false;
        private MetaExpressNodeBase m_ArrayLengthExpress = null;
        private MetaVariable m_StoreMetaVariable = null; //模板或者是调用时的函数        
        private MetaMemberFunction m_MetaMemberFunction = null;
        private List<MetaExpressNodeBase> m_MetaInputParamList = new List<MetaExpressNodeBase>();


        /// <summary>
        /// 为匿名 <see cref="MetaData"/> 字面量构造 <see cref="MetaNewObjectExpressNode"/>：
        /// 先得到结构化 <paramref name="anonymousMetaData"/>，再按子 <see cref="MetaMemberData"/> 的已解析表达式填入 <see cref="MetaBraceAssignStatements"/>，不合成语法 <see cref="Node"/> 树。
        /// </summary>
        public static MetaNewObjectExpressNode CreateAnonymousDataNewObjectExpress(
            MetaMemberData braceLiteralOwner,
            MetaData anonymousMetaData,
            MetaBase ownerMeta,
            MetaBlockStatements mbs,
            bool preferSourceMemberExpress = true)
        {
            if (braceLiteralOwner == null || anonymousMetaData == null)
            {
                return null;
            }

            var anonymousType = new MetaType(anonymousMetaData);
            var node = new MetaNewObjectExpressNode(anonymousType, ownerMeta, mbs, braceLiteralOwner);
            node.m_Token = braceLiteralOwner.token;

            node.FillAnonymousDataAssignStatementsFromMemberDict(braceLiteralOwner, anonymousMetaData, mbs, preferSourceMemberExpress);

            return node;
        }

        //public MetaNewObjectExpressNode(MetaType mt, MetaClass ownerMC, MetaBlockStatements mbs, MetaVariable storeMv, MetaMemberFunction mmf)
        //{
        //    m_OwnerMetaClass = ownerMC;
        //    m_OwnerMetaBlockStatements = mbs;
        //    m_MetaType = new MetaType(mt);
        //    m_StoreMetaVariable = storeMv;
        //    m_MetaMemberFunction = mmf;
        //}
        // 解析后的[] 然后再进行newArray
        public MetaNewObjectExpressNode( MetaType defineMt, MetaArrayExpressNode maen, MetaBase mc, MetaBlockStatements mbs, MetaVariable equalMV)
        {
            m_DefineMetaType = defineMt;
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_NewType = ENewType.ArrayClass;
            m_Token = maen.token;
            m_StoreMetaVariable = equalMV;
            MetaType cmt = null;
            if(defineMt != null && defineMt.IsArray() )
            {
                var gtmtl = defineMt.GetGenTemplateMetaTypeList();
                cmt = gtmtl[0];
            }
            for (int i = 0; i < maen.metaCallArray.Count; i++)
            {
                MetaBraceAssignStatements mas = new MetaBraceAssignStatements(cmt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, maen.metaCallArray[i]);
                m_AssignStatementsList.Add(mas);
            }
            m_StatementsContentType = EStatementsContentType.ArrayValue;
        }
        // Class1 c = { a = 20, b = 20 };  => Class1 c = Class1(); c.a = 20; c.b = 20;
        // dynamic c = {a = 20, b = 20} => 动态类 
        // data c = {a = 20, b = 20} | c = {a = 20, b = 20} => 动态数据  
        // Map<int,string> map1 = new(10){ 1:"20", 2:"30", 3:"50" }
        // List<int> list1 = new(){ 1,2,3,4,5 }
        public MetaNewObjectExpressNode(FileMetaBraceTerm fmbt, MetaType mt, MetaBase ownerMC, MetaBlockStatements mbs, MetaVariable equalMV)
        {
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_DefineMetaType = new MetaType(mt);
            if (m_DefineMetaType != null)
            {
                if (m_DefineMetaType.IsArray())
                {
                    m_NewType = ENewType.ArrayClass;
                }
                else
                {
                    m_NewType = ENewType.CommomClass;
                }
            }
            m_Token = fmbt.token;
            m_BraceFileMetaBaseTerm = fmbt;
            m_StoreMetaVariable = equalMV;
        }
        // Array arr = [1,2,3]   [Class1(), Class2(), variable1.a.b(),100]
        public MetaNewObjectExpressNode(FileMetaBracketTerm fmbt, MetaType mt, MetaBase mc, MetaBlockStatements mbs, MetaVariable equalMV)
        {
            m_OwnerMetaBase = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_Token = fmbt.token;
            m_BraceFileMetaBaseTerm = fmbt;
            m_StoreMetaVariable = equalMV;

            m_DefineMetaType = new MetaType(mt);
            m_NewType = ENewType.ArrayClass;
        }
        // Class1(10){ c1 = 20, c2 = 30 }  int[2][]{ [1,2,3], [3,4,5] }
        public MetaNewObjectExpressNode(MetaType defineMt, MetaCallLinkExpressNode mcen)
        {
            m_DefineMetaType = defineMt != null ? new MetaType(defineMt) : null;
            m_OwnerMetaBase = mcen.ownerMetaBase;
            m_OwnerMetaBlockStatements = mcen.ownerMetaBlockStatements;
            m_StoreMetaVariable = mcen.GetStoreMetaVariable();

            m_MetaMemberFunction = mcen.metaCallLink.finalCallNode.methodCall?.function as MetaMemberFunction;
            m_NewMetaType = new MetaType(mcen.metaCallLink.finalCallNode.callMetaType);
            MetaCallNode initNode = null;
            if (mcen.metaCallLink.callNodeList.Count > 0)
            {
                for (int i = mcen.metaCallLink.callNodeList.Count - 1; i >= 0; i--)
                {
                    var n = mcen.metaCallLink.callNodeList[i];
                    if (n?.fileMetaBraceTerm != null || n?.fileMetaParTerm != null || n?.metaInputParamCollection != null)
                    {
                        initNode = n;
                        break;
                    }
                }
                if (initNode == null)
                {
                    initNode = mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1];
                }
            }
            if (mcen.metaCallLink.finalCallNode.callMetaType.IsArray())
            {
                m_NewType = ENewType.ArrayClass;
                m_UsesExplicitArrayElementTypeSyntax = true;

                if (mcen.metaCallLink.callNodeList.Count > 0)
                {
                    var lastNode = initNode ?? mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1];

                    m_Token = lastNode.token;
                    if (lastNode.metaInputParamCollection != null)
                    {
                        SetInputParams(lastNode.metaInputParamCollection);
                    }
                    else
                    {
                        if (lastNode.bracketExpressList?.Count > 0)
                        {
                            MetaArrayExpressNode mean = lastNode.bracketExpressList[0] as MetaArrayExpressNode;

                            if (mean.metaCallArray.Count == 1)
                            {
                                m_MetaInputParamList.Add(mean.metaCallArray[0]);
                            }
                            else
                            {
                                Log.AddMetaCoreLog(LID.MetaCoreArrayDiamondShould, m_Token, "", 1);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, "");
                        }
                    }

                    var fma = lastNode.fileMetaBraceTerm;
                    m_BraceFileMetaBaseTerm = fma;
                }
            }
            else
            {
                m_NewType = ENewType.CommomClass;
                if (mcen.metaCallLink.callNodeList.Count > 0)
                {
                    var lastNode = initNode ?? mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1];
                    m_Token = lastNode.token;
                    SetInputParams(lastNode.metaInputParamCollection);

                    var fma = lastNode.fileMetaBraceTerm;
                    m_BraceFileMetaBaseTerm = fma;
                }
            }
        }
        // dynamic c = { c1 = 100, c2 = 200 }
        public MetaNewObjectExpressNode(MetaClass ownermc, List<MetaData> list)
        {
            m_OwnerMetaBase = ownermc;
            m_OwnerMetaBlockStatements = null;

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            //MetaType mitp = new MetaType(MetaDynamicClass);
            //metaInputTemplateCollection.AddMetaTemplateParamsList(mitp);
            m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.arrayMetaClass, null, metaInputTemplateCollection);

            //MetaInputParamCollection mipc = new MetaInputParamCollection(mc, mbs);
            //mipc.AddMetaInputParam(new MetaInputParam(new MetaConstExpressNode(EType.Int32, m_MetaBraceOrBracketStatementsContent.count)));
            //MetaMemberFunction mmf = m_MetaType.metaClass.GetMetaMemberConstructFunction(mipc);

            //m_MetaConstructFunctionCall = new MetaMethodCall(m_MetaType.metaClass, mmf, mipc);
        }
        // 1..x
        public MetaNewObjectExpressNode(FileMetaConstValueTerm arrayLinkToken, MetaBase ownerMC, MetaBlockStatements mbs)
        {
            m_FileMetaConstValueTerm = arrayLinkToken;
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
            metaInputTemplateCollection.AddMetaTemplateParamsList(mitp);

            m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.rangeMetaClass, null, metaInputTemplateCollection);            
            m_NewMetaType = m_ExpressReturnMetaType;

            MetaInputParamCollection mdpc = new MetaInputParamCollection(ownerMC as MetaClass, mbs);
            String[] arr = m_FileMetaConstValueTerm.name.Split("..");
            m_Token = m_FileMetaConstValueTerm.token;
            if (arr.Length == 2)
            {
                int arr0 = 0;
                if (int.TryParse(arr[0], out arr0))
                {
                    MetaConstExpressNode mcen1 = new MetaConstExpressNode(EType.Int32, arr0);
                    MetaInputParam mip = new MetaInputParam(mcen1);
                    mdpc.AddMetaInputParam(mip);
                }
                else
                {
                    //处理前边定义过的变量
                }

                int arr1 = 0;
                if (int.TryParse(arr[1], out arr1))
                {
                    MetaConstExpressNode mcen2 = new MetaConstExpressNode(EType.Int32, arr[1]);
                    MetaInputParam mip2 = new MetaInputParam(mcen2);
                    mdpc.AddMetaInputParam(mip2);
                }
                else
                {
                    //处理前边定义过的变量
                }

                MetaInputParam mip3 = new MetaInputParam(new MetaConstExpressNode(EType.Int32, 1));
                mdpc.AddMetaInputParam(mip3);
            }
            var tfunction = m_ExpressReturnMetaType.GetMetaMemberConstructFunction(mdpc);

            if (tfunction != null)
            {
                //m_MetaConstructFunctionCall = new MetaMethodCall(null, null, tfunction, null, mdpc, null, null);
            }
        }
        // 手动构建NewObject表达式
        public MetaNewObjectExpressNode(MetaType mt, MetaBase ownerMC, MetaBlockStatements mbs)
            : this(mt, ownerMC, mbs, null)
        {
        }
        /// <summary>
        /// 手动构建 new 对象表达式；<paramref name="equalMV"/> 与语法上大括号初始化左侧变量一致时传入（如 data 成员匿名字面量）。
        /// </summary>
        public MetaNewObjectExpressNode(MetaType mt, MetaBase ownerMC, MetaBlockStatements mbs, MetaVariable equalMV)
        {
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_StoreMetaVariable = equalMV;
            m_DefineMetaType = new MetaType(mt);
            m_ExpressReturnMetaType = new MetaType(mt);
            if (m_ExpressReturnMetaType.IsArray())
            {
                m_NewType = ENewType.ArrayClass;
            }
            else
            {
                m_NewType = ENewType.CommomClass;
            }
        }

        public MetaType GetMaxLevelMetaType()
        {
            return MetaBraceAssignStatements.GetMaxLevelMetaType(m_AssignStatementsList, m_DefineMetaType );
        }

        public void ParseBraceStatementsContent(AllowUseSettings aws, MetaType mt )
        {
            if( m_BraceFileMetaBaseTerm is FileMetaBraceTerm fmbt2 )
            {

            }
            if (m_BraceFileMetaBaseTerm?.fileMetaExpressList?.Count > 0)
            {
                //Log.AddMetaCoreLog(LID.ShowExtendMessage, "解析大括号里边的内容");
                for (int i = 0; i < m_BraceFileMetaBaseTerm.fileMetaExpressList.Count; i++)
                {
                    var fas = m_BraceFileMetaBaseTerm.fileMetaExpressList[i];
                    HandleBraceTermNode(fas, mt, aws);
                }
            }
            else
            {
                if (m_BraceFileMetaBaseTerm is FileMetaBraceTerm braceTerm && braceTerm.fileMetaAssignSyntaxList.Count > 0)
                {
                    for (int i = 0; i < braceTerm.fileMetaAssignSyntaxList.Count; i++)
                    {
                        if (braceTerm.fileMetaAssignSyntaxList[i] is FileMetaDefineVariableSyntax fmdvs)
                        {
                            var mas = new MetaBraceAssignStatements(fmdvs, mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, null );
                            if (mas.expressNode == null)
                            {
                                continue;
                            }
                            m_AssignStatementsList.Add(mas);
                        }
                        else if (!mt.isDynamicData
                            && !mt.isDynamicClass
                            && !mt.IsArray()
                            && braceTerm.fileMetaAssignSyntaxList[i] is FileMetaOpAssignSyntax fmoas)
                        {
                            var mas = new MetaBraceAssignStatements(fmoas, mt, m_OwnerMetaBlockStatements, 
                                m_OwnerMetaBase, m_DefineMetaType);
                            if (mas.expressNode == null)
                            {
                                continue;
                            }
                            m_AssignStatementsList.Add(mas);
                        }
                    }
                }

                for (int i = 0; i < this.m_AssignStatementsList.Count; i++)
                {
                    var asl = this.m_AssignStatementsList[i];
                    asl.Parse(aws);
                    asl.CalcReturnType();
                }
            }
        }
        //处理在{ Node1, Node2  } 在{}大括号中的Node1, Node2 这样的节点 Node1, 可以是 aaa = 1, "aa":1, 2:33, [1,2,3] [1] 3, this.value 这样的形式
        public void HandleBraceTermNode( FileMetaBaseTerm fmbt, MetaType mt, AllowUseSettings aws)
        {
            if (mt.isData)
            {
                //动态数据类的定义 在该行语句前直接使用 data a = { aaa = 10, bbb = 20} 这样的形式
                if (mt.isDynamicData)
                {
                    string anname = "DynamicData_";
                    if (m_StoreMetaVariable != null)
                    {
                        anname = anname + m_StoreMetaVariable.name + "_";
                    }
                    if (m_BraceFileMetaBaseTerm != null)
                    {
                        anname = anname + m_BraceFileMetaBaseTerm.token?.path + "_" + m_BraceFileMetaBaseTerm.token?.sourceBeginLine.ToString() + "_" + GetHashCode().ToString();
                    }

                    m_BraceNewTempMetaData = new MetaData(anname, false, false, true);
                    if (m_StoreMetaVariable?.token != null)
                    {
                        m_BraceNewTempMetaData.AddPingToken(m_StoreMetaVariable.token );
                    }
                    m_BraceNewTempMetaData.AddPingToken(m_BraceFileMetaBaseTerm.token);
                    if (fmbt is FileMetaSymbolTerm fmst)                   
                    {

                        var mmd = MetaMemberData.CreateDeclared(mt.metaData, fmst.name, -1, new MetaType(CoreMetaClassManager.objectMetaClass), false);
                        mmd.SetOwnerBlockstatements(m_OwnerMetaBlockStatements);                        

                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements, m_OwnerMetaBase, m_DefineMetaType );
                        //mas.CalcReturnType();
                        m_AssignStatementsList.Add(mas);
                        m_BraceNewTempMetaData.AddMetaMemberData(mmd);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbolterm in data ", m_StoreMetaVariable?.name, "");
                        return;
                    }
                    MetaData retClass = ClassManager.instance.FindMetaDataByNameAndFormat(m_BraceNewTempMetaData);
                    if (retClass == null)
                    {
                        ClassManager.instance.AddAnonymousMetaData(m_BraceNewTempMetaData);
                        retClass = m_BraceNewTempMetaData;
                    }
                    m_BraceNewMetaData = retClass;
                    for (int i = 0; i < m_AssignStatementsList.Count; i++)
                    {
                        //var mmv = m_AssignStatementsList[i].metaMemberData;
                        //mmv.metaDefineType.SetRawMetaClass(m_BraceNewMetaData);
                        //mmv.metaDefineType.SetMetaClass(m_BraceNewMetaData);
                    }
                    m_NewMetaType.SetMetaData(m_BraceNewMetaData);
                    m_StatementsContentType = EStatementsContentType.DynamicData;
                    m_StoreMetaVariable?.SetMetaDefineType(m_NewMetaType);
                }
                else
                {
                    //固定数据类赋值 在该行语句前直接使用 data a{ aaa = 10; bbb = 20 }  a = { aaa = 10, bbb = 20} 这样的形式 前边data 已经定义过了
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, m_NewMetaType );
                        //mas.CalcReturnType();
                        m_AssignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbolterm", m_StoreMetaVariable?.name, "");
                        return;
                    }
                    m_StatementsContentType = EStatementsContentType.DataValueAssign;
                }
            }
            else if (mt.IsArray() )// 数组类型的处理
            {
                m_StatementsContentType = EStatementsContentType.ArrayValue;
                var genList = mt.GetGenTemplateMetaTypeList();
                if (genList.Count != 1 )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "");
                    return;
                }
                MetaType cmt = genList[0];
                if (fmbt is FileMetaBracketTerm fmst)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmst, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, mnoe );
                    m_AssignStatementsList.Add(mas);                    
                }
                else if (fmbt is FileMetaBraceTerm fmbrt)
                {
                    // 兼容多层数组字面量中使用大括号嵌套的写法：
                    // int[][][] a = { { {1,2}, {3,4} }, { {5,6}, {7,8} } };
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbrt, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, mnoe );
                    m_AssignStatementsList.Add(mas);
                }
                else if( fmbt is FileMetaCallTerm fmct )
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaBase = m_OwnerMetaBase;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = fmct;
                    cep.equalMetaVariable = m_StoreMetaVariable;
                    MetaExpressNodeBase men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());                    
                    var mas = new MetaBraceAssignStatements(mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.Parse(new AllowUseSettings());
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaConstValueTerm fmcvt)
                {
                    MetaConstExpressNode men = new MetaConstExpressNode(m_OwnerMetaBase, m_OwnerMetaBlockStatements, fmcvt);
                    men.Parse(new AllowUseSettings());
                    var mas = new MetaBraceAssignStatements(mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if( fmbt is FileMetaSymbolTerm fmst2 )
                {
                    if( fmst2.symBolType != ETokenType.Comma )
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "间隔符号不对,应该使用,");
                    }
                }
                else if( fmbt is FileMetaTermExpress termexpress )
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaBase = m_OwnerMetaBase;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = termexpress;
                    cep.equalMetaVariable = m_StoreMetaVariable;
                    MetaExpressNodeBase men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    men = ExpressManager.ConvertNewExpress(men, cep.metaType, m_StoreMetaVariable);                   
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 在数组里边应该是FileMetaBracketTerm 类型!");
                }
            }
            // Array<Object>(n){ ... } 中嵌套 [1,2] 时，子字面量节点的 defineMetaType 为元素类型 object（非 Array），
            // 不可走「普通类 { 成员= }」分支，须与数组槽一致地接纳常量/调用/[]/表达式。
            else if (mt != null && mt.metaClass == CoreMetaClassManager.objectMetaClass && !mt.IsArray())
            {
                m_StatementsContentType = EStatementsContentType.ArrayValue;
                MetaType cmt = mt;
                if (fmbt is FileMetaBracketTerm fmstOb)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmstOb, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, mnoe);
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaBraceTerm fmbrtOb)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbrtOb, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements, m_StoreMetaVariable);
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements(mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, mnoe);
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaCallTerm fmctOb)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaBase = m_OwnerMetaBase;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = fmctOb;
                    cep.equalMetaVariable = m_StoreMetaVariable;
                    MetaExpressNodeBase men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    var mas = new MetaBraceAssignStatements(mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.Parse(new AllowUseSettings());
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaConstValueTerm fmcvtOb)
                {
                    MetaConstExpressNode men = new MetaConstExpressNode(m_OwnerMetaBase, m_OwnerMetaBlockStatements, fmcvtOb);
                    men.Parse(new AllowUseSettings());
                    var mas = new MetaBraceAssignStatements(mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaSymbolTerm fmstOb2)
                {
                    if (fmstOb2.symBolType != ETokenType.Comma)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "间隔符号不对,应该使用,");
                    }
                }
                else if (fmbt is FileMetaTermExpress termexpressOb)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaBase = m_OwnerMetaBase;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = termexpressOb;
                    cep.equalMetaVariable = m_StoreMetaVariable;
                    MetaExpressNodeBase men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    men = ExpressManager.ConvertNewExpress(men, cep.metaType, m_StoreMetaVariable);
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Array<Object> 元素槽不支持该语法节点!");
                }
            }
            else if (mt.isMap)   // 映射类型的处理 使用   a:10, b:20  20:"aa" 这样的形式
            {
                if (fmbt is FileMetaSymbolTerm fmst)
                {
                    MetaBraceAssignStatements mas = new MetaBraceAssignStatements( fmst, mt,  m_OwnerMetaBlockStatements, m_OwnerMetaBase, m_NewMetaType);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                    m_StatementsContentType = EStatementsContentType.ClassValueAssign;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "isMap", m_StoreMetaVariable?.name, "");
                    return;
                }
            }
            else
            {
                /*
                //动态普通类的定义
                if (mt.isDynamicClass)
                {
                    MetaData anonClass = new MetaData("DynamicClass__" + GetHashCode(), false, false, true );
                    //构建匿名类中的项
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        var mas = new MetaBraceAssignStatements(fmst, m_BraceStatementsDefineMetaType, m_OwnerMetaBlockStatements);                        
                        mas.CalcReturnType();
                        m_AssignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbol term in dynamic class", m_StoreMetaVariable?.name, "");
                        return;
                    }

                    for (int i = 0; i < m_AssignStatementsList.Count; i++)
                    {
                        var mmv = m_AssignStatementsList[i].metaMemberVariable;
                        anonClass.AddMetaMemberVariable(m_AssignStatementsList[i].metaMemberVariable);
                    }
                    MetaData retClass = ClassManager.instance.FindMetaDataByNameAndFormat(anonClass);
                    if (retClass == null)
                    {
                        for (int i = 0; i < m_AssignStatementsList.Count; i++)
                        {
                            var mmv = m_AssignStatementsList[i].metaMemberVariable;
                            mmv.SetOwnerMetaClass(anonClass);
                        }
                        ClassManager.instance.AddAnonymousMetaData(anonClass);
                        retClass = anonClass;
                    }
                    else
                    {
                        var list = anonClass.metaMemberDataDict;
                        if (list.Count == m_AssignStatementsList.Count)
                        {

                        }
                    }
                    m_BraceStatementsDefineMetaType = new MetaType(retClass);
                    m_StatementsContentType = EStatementsContentType.DynamicClass;
                }
                else// 普通类赋值处理
                */
                {
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        var mas = new MetaBraceAssignStatements(fmst, mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, m_NewMetaType );
                        mas.Parse(aws);
                        mas.CalcReturnType();
                        m_AssignStatementsList.Add(mas);
                    }
                    else if( fmbt is FileMetaTermExpress fmte )
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbol term in common class", m_StoreMetaVariable?.name, "" );
                        return;
                    }
                    m_StatementsContentType = EStatementsContentType.ClassValueAssign;
                }
            }            
        }

        /// <summary>
        /// 用 <paramref name="braceLiteralOwner"/> 的 <see cref="MetaMemberData.metaMemberDataDict"/> 里已解析的
        /// <see cref="MetaMemberData.expressNode"/>（含嵌套匿名 data 的 <see cref="MetaNewObjectExpressNode"/>）
        /// 填充 <paramref name="node"/> 的初始化赋值列表，目标字段来自规范化后的 <paramref name="anonymousMetaData"/>。
        /// </summary>
        public void FillAnonymousDataAssignStatementsFromMemberDict(
            MetaMemberData braceLiteralOwner,
            MetaData anonymousMetaData,
            MetaBlockStatements mbs,
            bool preferSourceMemberExpress = true)
        {
            if (braceLiteralOwner == null || anonymousMetaData == null)
            {
                return;
            }

            m_AssignStatementsList.Clear();

            var ordered = new List<MetaMemberData>(braceLiteralOwner.metaMemberDataDict.Values);
            ordered.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));

            MetaData ownerMc = ownerMetaData;
            var parseSetting = new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress };

            foreach (var sourceField in ordered)
            {
                var targetField = anonymousMetaData.GetMemberDataByName(sourceField.name);
                if (targetField == null)
                {
                    continue;
                }

                MetaExpressNodeBase expr = null;

                var fieldForAssign = preferSourceMemberExpress ? sourceField : targetField;
                if (fieldForAssign == null)
                {
                    fieldForAssign = sourceField ?? targetField;
                }

                bool nestedStructuralData = fieldForAssign != null
                    && fieldForAssign.memberDataType == EMemberDataType.MemberData
                    && fieldForAssign.metaMemberDataDict.Count > 0;

                bool useNestFromHierarchy = nestedStructuralData
                    && (fieldForAssign.expressNode == null || fieldForAssign.expressNode is MetaNewObjectExpressNode);

                if (useNestFromHierarchy)
                {
                    MetaData nestedCanon = targetField.defineMetaType?.metaData;
                    if (nestedCanon == null)
                    {
                        nestedCanon = fieldForAssign.BuildAnonymousMetaDataType(out _);
                    }
                    if (nestedCanon != null)
                    {
                        if (fieldForAssign.expressNode is MetaNewObjectExpressNode existingNest)
                        {
                            existingNest.m_AssignStatementsList.Clear();
                            existingNest.FillAnonymousDataAssignStatementsFromMemberDict(fieldForAssign, nestedCanon, mbs, preferSourceMemberExpress);
                            existingNest.Parse(parseSetting);
                            existingNest.CalcReturnType();
                            expr = existingNest;
                        }
                        else
                        {
                            var nestedNode = CreateAnonymousDataNewObjectExpress(
                                fieldForAssign,
                                nestedCanon,
                                ownerMc,
                                mbs,
                                preferSourceMemberExpress);
                            if (nestedNode != null)
                            {
                                nestedNode.Parse(parseSetting);
                                nestedNode.CalcReturnType();
                                expr = nestedNode;
                            }
                        }
                    }
                }

                if (expr == null)
                {
                    expr = fieldForAssign?.expressNode;
                }
                if (expr == null)
                {
                    expr = preferSourceMemberExpress ? targetField.expressNode : sourceField.expressNode;
                }

                if (expr == null)
                {
                    continue;
                }

                var mas = new MetaBraceAssignStatements(sourceField, mbs, ownerMc, expr, true );
                m_AssignStatementsList.Add(mas);
            }

            Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
            CalcReturnType();
        }
        public override void Parse(AllowUseSettings auc)
        {
            //该函数，进行，计算出， 要创建的类，使用的初始化函数，以及，初始化成员的解析            //
            if (m_NewType == ENewType.ArrayClass )
            {
                if (m_NewMetaType != null && m_DefineMetaType != null )
                {
                    ParseBraceStatementsContent(auc, m_NewMetaType);
                }
                else if( m_NewMetaType != null && m_DefineMetaType == null )
                {
                    ParseBraceStatementsContent(auc, m_NewMetaType);
                }
                else if( m_NewMetaType == null && m_DefineMetaType != null )
                {
                    ParseBraceStatementsContent(auc, m_DefineMetaType);
                }
                else
                {
                    ParseBraceStatementsContent(auc, new MetaType(CoreMetaClassManager.objectMetaClass));
                }
                if (m_AssignStatementsList.Count > 0)
                {
                    MetaType inputType = GetMaxLevelMetaType();

                    var newMetaType = new MetaType(inputType);
                    //List<MetaType> listMT = new List<MetaType>();
                    //for( int i = 0; i < m_MetaBraceOrBracketStatementsContent.assignStatementsList.Count; i++ )
                    //{
                    //    var mt = m_MetaBraceOrBracketStatementsContent.assignStatementsList[i].GetRetMetaType();
                    //    listMT.Add(mt);
                    //}
                    MetaType newRMT = new MetaType();
                    newRMT.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                    newRMT.AddDefineTemplateMetaType(newMetaType);
                    newMetaType = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(newRMT, true, out bool isIGM);
                    newMetaType.SetArrayLength(m_AssignStatementsList.Count);
                    m_ArrayCalcMetaType = newMetaType;
                }

            }
            else if( m_NewType == ENewType.CommomClass )
            {
                if(m_NewMetaType != null )
                {
                    this.ParseBraceStatementsContent(auc, m_NewMetaType);
                }
                else if( m_DefineMetaType != null )
                {
                    this.ParseBraceStatementsContent(auc, m_DefineMetaType);
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "");
                }
                if (m_StatementsContentType == EStatementsContentType.DynamicClass)
                {
                }
                else if (m_StatementsContentType == EStatementsContentType.DynamicData)
                {
                }
                else
                {
                    // Before creating instance type, check abstract class restriction
                    var metaClass = m_DefineMetaType?.metaClass;
                    if (metaClass != null && metaClass.isAbstractClass)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error: cannot instantiate abstract class: " + metaClass.name);
                        m_NewMetaType = null;
                    }
                }
            }
        }
        public void SetInputParams(MetaInputParamCollection _paramCollection)
        {
            int defineCount = 0;
            List<MetaDefineParam> mpList = new();
            if (m_MetaMemberFunction != null )
            {
                if (m_MetaMemberFunction.metaMemberParamCollection != null)
                {
                    defineCount = m_MetaMemberFunction.metaMemberParamCollection.maxParamCount;
                    mpList = m_MetaMemberFunction.metaMemberParamCollection.metaDefineParamList;
                }
            }

            int inputCount = _paramCollection != null ? _paramCollection.metaInputParamList.Count : 0;
            for (int i = 0; i < defineCount; i++)
            {
                if (i < inputCount)
                {
                    MetaInputParam mip = _paramCollection.metaInputParamList[i];
                    m_MetaInputParamList.Add(mip.express);
                }
                else
                {
                    MetaDefineParam mdp = mpList[i];
                    if (mdp != null)
                    {
                        m_MetaInputParamList.Add(mdp.expressNode);
                    }
                }
            }

            if (newType == ENewType.ArrayClass
                && m_MetaInputParamList.Count == 0
                && _paramCollection != null
                && _paramCollection.metaInputParamList != null
                && _paramCollection.metaInputParamList.Count > 0)
            {
                for (int i = 0; i < _paramCollection.metaInputParamList.Count; i++)
                {
                    var mip = _paramCollection.metaInputParamList[i];
                    if (mip?.express != null)
                    {
                        m_MetaInputParamList.Add(mip.express);
                    }
                }
            }

            if( newType == ENewType.ArrayClass )
            {
                if( m_MetaInputParamList.Count == 1 )
                {
                    if (m_NewMetaType != null)
                    {
                        if (m_MetaInputParamList[0] is MetaConstExpressNode mcen )
                        {
                            int len = Convert.ToInt32(mcen.value);
                            m_NewMetaType.SetArrayLength(len );
                        }
                    }
                    else
                    {
                        if (m_MetaInputParamList[0] is MetaConstExpressNode mcen)
                        {
                            //m_NewMetaType.SetArrayLength((int)mcen.value);
                        }
                        //Debug.Assert(false);
                    }
                }
                else
                {
                    if (m_MetaInputParamList.Count == 0)
                    {
                        return;
                    }

                    var tokenText = m_Token?.lexeme?.ToString() ?? "<array-new>";
                    Log.AddMetaCoreLog(LID.MetaCoreArrayNotFoundSetLength, m_Token, "", tokenText );
                }
            }
        }
        public void SetStoreMetaVariable( MetaVariable smv )
        {
            this.m_StoreMetaVariable = smv;
        }
        public override void CalcReturnType()
        {
            base.CalcReturnType();

            var mipc = new MetaInputParamCollection(ownerMetaClass, m_OwnerMetaBlockStatements);

            if (!ResolveInitialExpressReturnMetaType())
            {
                return;
            }

            TryApplyNumericArrayElementMergeFromDefineAndNew();

            if (!ApplyExpressReturnFromNewMetaTypeIfNeeded())
            {
                return;
            }

            TryPromoteObjectArrayLiteralReturnType();
            TryApplyNumericArrayElementMergeDeadBranch();

            SyncArrayLiteralLengthFromContent();

            if (m_ExpressReturnMetaType == null)
            {
                return;
            }

            SetupArrayMemberFunctionAndLengthParam(mipc);

            if (!TryUnifyNumericArrayLiteralMemberTypes())
            {
                return;
            }
        }

        private void SetExpressReturnFromNewMetaType()
        {
            m_ExpressReturnMetaType = new MetaType(m_NewMetaType);
        }

        private void SetExpressReturnFromDefineMetaType()
        {
            m_ExpressReturnMetaType = new MetaType(m_DefineMetaType);
        }

        private bool IsDefineAndNewArrayClassNew()
        {
            return m_DefineMetaType != null && m_NewMetaType != null
                && m_DefineMetaType.IsArray() && m_NewMetaType.IsArray()
                && m_NewType == ENewType.ArrayClass;
        }

        private static bool TryBuildNumericMergedArrayMetaType(MetaType defineArray, MetaType newArray, out MetaType merged)
        {
            merged = null;
            var dEl = ClassManager.GetSingleTemplateArgMetaType(defineArray);
            var nEl = ClassManager.GetSingleTemplateArgMetaType(newArray);
            if (dEl == null || nEl == null)
            {
                return false;
            }

            if (!ClassManager.IsNumberClass(dEl.metaClass) || !ClassManager.IsNumberClass(nEl.metaClass))
            {
                return false;
            }

            if (TypeManager.CompareMetaType(dEl, nEl))
            {
                return false;
            }

            merged = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(defineArray, newArray);
            return merged != null;
        }

        private bool ResolveInitialExpressReturnMetaType()
        {
            if (m_DefineMetaType != null && m_NewMetaType != null)
            {
                if (m_NewMetaType.IsArray())
                {
                    return ResolveInitialReturnForArrayNew();
                }

                return ResolveInitialReturnForNonArrayNew();
            }

            if (m_NewMetaType != null && m_DefineMetaType == null)
            {
                SetExpressReturnFromNewMetaType();
                return true;
            }

            if (m_NewMetaType == null && m_DefineMetaType != null)
            {
                SetExpressReturnFromDefineMetaType();
                return true;
            }

            if (m_DefineMetaType == null && m_NewMetaType == null)
            {
                return true;
            }

            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "没有找到没有各种定义类型的方法");
            return true;
        }

        private bool ResolveInitialReturnForArrayNew()
        {
            if (m_StoreMetaVariable?.isDefineMetaType != true)
            {
                SetExpressReturnFromNewMetaType();
                return true;
            }

            if (!m_DefineMetaType.IsArray())
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "如果定义了，结构，必须与new对象的类型一样才可以");
                return false;
            }

            if (!TryResolveDefinedStoreArrayNewReturnType(out var numericMergedArrayMeta))
            {
                return false;
            }

            m_ExpressReturnMetaType = numericMergedArrayMeta != null ? numericMergedArrayMeta : new MetaType(m_NewMetaType);
            return true;
        }

        private bool TryResolveDefinedStoreArrayNewReturnType(out MetaType numericMergedArrayMeta)
        {
            numericMergedArrayMeta = null;
            var list1 = m_DefineMetaType.ArrayDimensionLengthList();
            var list2 = m_NewMetaType.ArrayDimensionLengthList();

            if (list1.Count != list2.Count || list1.Count == 0)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "定义数组与new数组 的维度不同");
                return false;
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (i == list1.Count - 1)
                {
                    if (!TryValidateInnermostArrayDimensionForStore(list1, list2, i, ref numericMergedArrayMeta))
                    {
                        return false;
                    }
                }
                else if (!TryValidateOuterArrayDimensionForStore(list1, list2, i))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryValidateInnermostArrayDimensionForStore(
            List<int> defineDims,
            List<int> newDims,
            int index,
            ref MetaType numericMergedArrayMeta)
        {
            if (defineDims[index] != -1)
            {
                if (newDims[index] != -1)
                {
                    if (newDims[index] != defineDims[index])
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "两个数组的定义长度不同");
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "最后一位数组定义，不能为实体值");
                    return false;
                }
            }

            var cmt1 = m_DefineMetaType.GetMetaTypeByIndex(0);
            var cmt2 = m_NewMetaType.GetMetaTypeByIndex(0);
            if (TypeManager.CompareMetaType(cmt1, cmt2))
            {
                return true;
            }

            if (ClassManager.IsNumberClass(cmt1.metaClass) && ClassManager.IsNumberClass(cmt2.metaClass))
            {
                numericMergedArrayMeta = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(
                    m_DefineMetaType, m_NewMetaType);
                return true;
            }

            if (cmt1.IsArray() && cmt2.IsArray()
                && MetaBraceAssignStatements.TryArrayElementAssignableForNewObject(cmt1, cmt2))
            {
                return true;
            }

            Log.AddMetaCoreLog(LID.MetaCoreArrayNotSupportInConvert, m_Token, "", cmt1.ToString(), cmt2.ToString());
            return false;
        }

        private bool TryValidateOuterArrayDimensionForStore(List<int> defineDims, List<int> newDims, int index)
        {
            if (defineDims[index] == -1)
            {
                if (newDims[index] == -1)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "不是最后一位 生成的数组，需要定义数组长度");
                    return false;
                }

                return true;
            }

            if (defineDims[index] != newDims[index])
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "最后一位数组定义，不能为实体值! 如果前边定义了长度，new的时候必须和前边的长度一样!");
                return false;
            }

            return true;
        }

        private bool ResolveInitialReturnForNonArrayNew()
        {
            if (m_DefineMetaType.IsArray())
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "如果定义了，结构，必须与new对象的类型一样才可以");
                return false;
            }

            if (m_NewMetaType.isClass)
            {
                if (m_NewMetaType.metaClass.IsContainMetaClass(m_DefineMetaType.metaClass))
                {
                    SetExpressReturnFromNewMetaType();
                    return true;
                }

                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "定义类型与new的类型不对应 ");
                return false;
            }

            SetExpressReturnFromNewMetaType();
            return true;
        }

        /// <summary>
        /// 左值 Array&lt;Int32&gt; + 字面量/右模板 Array&lt;Int16&gt;：未走“变量已带定义类型”分支时，仍以左值元素类型合并。
        /// </summary>
        private void TryApplyNumericArrayElementMergeFromDefineAndNew()
        {
            if (!IsDefineAndNewArrayClassNew())
            {
                return;
            }

            if (TryBuildNumericMergedArrayMetaType(m_DefineMetaType, m_NewMetaType, out var merged))
            {
                m_ExpressReturnMetaType = merged;
            }
        }

        private bool ApplyExpressReturnFromNewMetaTypeIfNeeded()
        {
            if (m_NewMetaType == null)
            {
                return true;
            }

            if (!m_NewMetaType.IsArray())
            {
                if (m_ExpressReturnMetaType == null)
                {
                    SetExpressReturnFromNewMetaType();
                }
                return true;
            }

            if (m_ExpressReturnMetaType == null)
            {
                SetExpressReturnFromNewMetaType();
                return true;
            }

            if (m_ExpressReturnMetaType.arrayLength == -1)
            {
                m_ExpressReturnMetaType.SetArrayLength(m_NewMetaType.arrayLength);
                return true;
            }

            if (m_ExpressReturnMetaType.arrayLength < m_NewMetaType.arrayLength)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "数组赋值内容给出的长度超出了定义长度!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Array&lt;Object&gt;(...){ [1], [2,3] }：object 槽中的嵌套数组字面量需暴露真实数组类型。
        /// </summary>
        private void TryPromoteObjectArrayLiteralReturnType()
        {
            if (m_NewType != ENewType.ArrayClass
                || m_NewMetaType == null
                || !m_NewMetaType.IsArray()
                || m_ExpressReturnMetaType == null
                || m_ExpressReturnMetaType.IsArray()
                || m_ExpressReturnMetaType.metaClass != CoreMetaClassManager.objectMetaClass)
            {
                return;
            }

            SetExpressReturnFromNewMetaType();
        }

        /// <summary>保留原条件（<c>m_NewMetaType == null &amp;&amp; m_NewMetaType != null</c> 恒 false），避免改动历史分支。</summary>
        private void TryApplyNumericArrayElementMergeDeadBranch()
        {
            if (m_DefineMetaType != null && m_NewMetaType == null && m_NewMetaType != null
                && m_DefineMetaType.IsArray() && m_NewMetaType.IsArray()
                && m_NewType == ENewType.ArrayClass)
            {
                if (TryBuildNumericMergedArrayMetaType(m_DefineMetaType, m_NewMetaType, out var mergedDr))
                {
                    m_ExpressReturnMetaType = mergedDr;
                }
            }
        }

        private void SetupArrayMemberFunctionAndLengthParam(MetaInputParamCollection mipc)
        {
            if (!m_ExpressReturnMetaType.IsArray())
            {
                return;
            }

            if (m_MetaInputParamList.Count == 0)
            {
                int alen = m_ExpressReturnMetaType.arrayLength;
                if (alen == -1 && m_NewMetaType == null)
                {
                    alen = 0;
                }

                mipc.AddMetaInputParam(new MetaInputParam(new MetaConstExpressNode(EType.Int32, alen)));
                mipc.CaleReturnType();

                m_MetaMemberFunction = CoreMetaClassManager.arrayMetaClass.GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, mipc);
                SetInputParams(mipc);
            }
            else if (m_MetaInputParamList.Count == 1)
            {
                if (m_MetaInputParamList[0] is MetaConstExpressNode mcen)
                {
                    mcen.value = m_ExpressReturnMetaType.arrayLength;
                }
            }
            else
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "--");
            }

            m_ArrayLengthExpress = m_MetaInputParamList[0];
            m_MetaMemberFunction = null;
        }

        private bool TryUnifyNumericArrayLiteralMemberTypes()
        {
            if (m_NewType != ENewType.ArrayClass
                || m_ExpressReturnMetaType == null
                || !m_ExpressReturnMetaType.IsArray())
            {
                return true;
            }

            if (NumberManager.TryUnifyNumericArrayLiteralMembersToDeclaredArrayType(this, m_ExpressReturnMetaType, m_Token))
            {
                return true;
            }

            if (!TryRebuildMetaTypeFromLiteralNumericPromotion())
            {
                return false;
            }

            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                "数组字面量无法全部强转为当前声明/推断的元素类型，已按数值升阶规则重新推断数组类型为 "
                + m_ExpressReturnMetaType.ToString() + "；请检查与左值类型是否仍可赋值。");

            if (!NumberManager.TryUnifyNumericArrayLiteralMembersToDeclaredArrayType(this, m_ExpressReturnMetaType, m_Token))
            {
                return false;
            }

            return true;
        }
        public void SetNewMetaType( MetaType newType )
        {
            m_NewMetaType = newType;
        }

        private void SyncArrayLiteralLengthFromContent()
        {
            if (m_NewType != ENewType.ArrayClass)
            {
                return;
            }

            int literalLength = m_AssignStatementsList.Count;
            if (literalLength < 0)
            {
                return;
            }
            if (m_NewMetaType != null && m_NewMetaType.IsArray() && m_NewMetaType.arrayLength == -1)
            {
                m_NewMetaType.SetArrayLength(literalLength);
            }

            if (m_ExpressReturnMetaType != null && m_ExpressReturnMetaType.IsArray() && m_ExpressReturnMetaType.arrayLength == -1)
            {
                m_ExpressReturnMetaType.SetArrayLength(literalLength);
            }
        }

        /// <summary>
        /// 赋值场景下左值为数组类型（如 <c>Array&lt;Int32&gt;</c>）时写入，供 <see cref="CalcReturnType"/> 与右值模板做数值元素对齐。
        /// </summary>
        public void SetAssignmentTargetArrayMetaType(MetaType leftArrayMetaType)
        {
            if (leftArrayMetaType != null && leftArrayMetaType.IsArray())
            {
                m_DefineMetaType = new MetaType(leftArrayMetaType);
                if (m_NewMetaType == null || !m_NewMetaType.IsArray())
                {
                    m_NewMetaType = new MetaType(leftArrayMetaType);
                }
                m_NewMetaType = m_DefineMetaType;
            }
        }

        /// <summary>
        /// 按字面量数值升阶规则（与 Parse 阶段一致）重建当前节点的 <see cref="m_MetaType"/>。
        /// 在无法强转为左值声明元素类型时作为兜底推断。
        /// </summary>
        private bool TryRebuildMetaTypeFromLiteralNumericPromotion()
        {
            if (m_AssignStatementsList == null || m_AssignStatementsList.Count == 0)
            {
                return false;
            }

            MetaType inputType = GetMaxLevelMetaType();
            if (inputType == null)
            {
                return false;
            }

            MetaType newRMT = new MetaType();
            newRMT.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
            newRMT.AddDefineTemplateMetaType(inputType);
            m_ExpressReturnMetaType = CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(newRMT, true, out _);
            m_ExpressReturnMetaType.SetArrayLength(m_AssignStatementsList.Count);
            return m_ExpressReturnMetaType != null;
        }
        public void CheckDefineVariableMetaTypeAndContentMetaType()
        {
            for (int i = 0; i < this.m_AssignStatementsList.Count; i++)
            {
                m_AssignStatementsList[i].ValidateDefineAgainstDeclaredMetaType();
            }
        }
        public override MetaType GetReturnMetaType()
        {
            if(this.expressReturnMetaType != null )
            {
                return expressReturnMetaType;
            }
            //if (m_MetaConstructFunctionCall != null)
            //{
            //    m_MetaType = m_MetaConstructFunctionCall.GeMetaDefineType();
            //}
            return expressReturnMetaType;
        }
        private string FormatBraceAssignListForDebug()
        {
            StringBuilder sb = new StringBuilder();
            if (m_AssignStatementsList.Count > 0)
            {
                sb.Append("{");
                for (int i = 0; i < m_AssignStatementsList.Count; i++)
                {
                    var mas = m_AssignStatementsList[i];
                    sb.Append(mas.ToFormatString());
                    if (i < m_AssignStatementsList.Count - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
            }
            return sb.ToString();
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();


            if( m_ExpressReturnMetaType.isEnum )
            {
                sb.Append(m_ExpressReturnMetaType.name );
                sb.Append(".");
                sb.Append(m_ExpressReturnMetaType.enumValue.name);
                if(m_MetaEnumValue != null)
                {
                    sb.Append("(");
                    sb.Append(m_MetaEnumValue.ToFormatString());
                    sb.Append(")");
                }
            }
            else if(m_ExpressReturnMetaType.isData )
            {
                sb.Append(m_ExpressReturnMetaType.name);
                sb.Append("{");
                if (m_AssignStatementsList.Count > 0)
                {
                    for( int i = 0; i < m_AssignStatementsList.Count ; i++ )
                    {
                        var bsc = m_AssignStatementsList[i];
                        if( bsc == null )
                        {
                            continue;
                        }
                        sb.Append(bsc.ToFormatString());

                        if( i < m_AssignStatementsList.Count - 1 )
                        {
                            sb.Append(",");
                        }
                    }
                }
                sb.Append("}");
            }
            else
            {
                if (m_ExpressReturnMetaType != null )
                {
                    sb.Append(m_ExpressReturnMetaType.name + "()");
                    sb.Append(".");
                }
                //if(m_MetaConstructFunctionCall.m_CallerMetaVariable != null )
                //{
                //    sb.Append(m_MetaConstructFunctionCall.m_CallerMetaVariable.name);
                //    sb.Append(".");
                //    sb.Append(m_MetaConstructFunctionCall.function.name);
                //    sb.Append("(");
                //    if( m_MetaConstructFunctionCall.metaInputParamCollection != null )
                //    {
                //        int count = m_MetaConstructFunctionCall.metaInputParamCollection.metaInputParamList.Count;
                //        for ( int i = 0; i < count; i++ )
                //        {
                //            var mp = m_MetaConstructFunctionCall.metaInputParamCollection.metaInputParamList[i];
                //            sb.Append(mp.ToFormatString());
                //            if( i < count - 1 )
                //            {
                //                sb.Append(",");
                //            }
                //        }
                //    }
                //    sb.Append(")");
                //}
                sb.Append(FormatBraceAssignListForDebug());
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return "new object: " + ToFormatString();
        }
        /// <summary>
        ///  Class2 c = new( 1, 2 );
        /// </summary>
        /// <param name="root"></param>
        /// <param name="mc"></param>
        /// <param name="mbs"></param>
        /// <param name="selfMc"></param>
        /// <returns></returns>
        /*
        public static MetaNewObjectExpressNode CreateNewObjectExpressNodeByPar(FileMetaParTerm root, MetaType mt, MetaClass omc, MetaBlockStatements mbs)
        {
            var fmct = (root as FileMetaParTerm);
            if (fmct == null) return null;
            if (mt == null) return null;

            MetaInputParamCollection mpc = new MetaInputParamCollection(root, omc, mbs);

            if( mpc.metaInputParamList.Count > 0 )
            {
                MetaMemberFunction mmf = mt.GetMetaMemberConstructFunction(mpc);

                if (mmf == null) return null;

              
                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode(root, mt, omc, mbs );

                return mnoen;

            }
            else
            {
                MetaNewObjectExpressNode mnoen = new MetaNewObjectExpressNode( root, mt, omc, mbs );

                return mnoen;
            }
        }
        */
        // 创建NewObject 即 Class c = Class(){ var1 = 1; } 的方式使用 1即生成的表达式
        /*
        public MetaExpressNodeBase CreateExpressNodeInNewObjectStatements(MetaVariable mv, MetaBlockStatements mbs, FileMetaBaseTerm fme)
        {
            if (fme == null)
            {
                Debug.Write("Error !!!!!!!!!!");
                return null;
            }

            FileMetaBaseTerm curFMBT = fme;
            if (fme.left == null && fme.right == null)
            {
                if (fme is FileMetaTermExpress)
                {
                    curFMBT = (fme as FileMetaTermExpress).root;
                }
            }

            MetaClass mc = mbs?.ownerMetaClass;
            MetaClass selfMC = mv?.metaDefineType?.metaClass;
            switch (curFMBT)
            {
                case FileMetaConstValueTerm constValueTerm:
                    {
                        MetaExpressNodeBase men = new MetaConstExpressNode(constValueTerm);

                        return men;
                    }
                case FileMetaCallTerm callTerm:
                    {
                        MetaCallLinkExpressNode clen = new MetaCallLinkExpressNode(callTerm.callLink, mc, mbs, null);
                        AllowUseSettings auc = new AllowUseSettings();
                        auc.useNotConst = false;
                        auc.useNotStatic = false;
                        auc.callConstructFunction = true;
                        auc.callFunction = true;
                        clen.Parse(auc);

                        return clen;
                    }
                case FileMetaBraceTerm fmbt:
                    {
                        MetaType mt = new MetaType(CoreMetaClassManager.objectMetaClass);
                        MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbt, mt, mc, mbs, mv);
                        return mnoe;
                    }
                case FileMetaBracketTerm fmbt:
                    {
                        if( mv is MetaMemberData )
                        {
                            MetaType mt = new MetaType(CoreMetaClassManager.arrayMetaClass);
                            MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbt, mt, mt.metaClass, mbs, mv );
                            return mnoe;
                        }
                        else
                        {
                            Debug.Write("Error 只有在data varname = {} 支持 { cha1 = [] } 的格式,其它的表达式中不支持");
                        }
                        break;
                    }
                default:
                    {
                        Debug.Write("Error 暂不支持该类型的在NewObject中的解析!!");
                    }
                    break;
            }
            return null;
        }
        
        //List<int> depthLength = new List<int>();
        //public void HnaldeArrayType( MetaCallNode lastNode, MetaType mt  )
        //{
        //    for (int i = 0; i < lastNode.bracketExpressList.Count; i++)
        //    {
        //        bool flag = true;
        //        if (lastNode.bracketExpressList[i] is MetaArrayExpressNode maen)
        //        {
        //            if( maen.metaCallArray.Count == 1 )
        //            {
        //                if( maen.metaCallArray[0] is MetaConstExpressNode mcenc )
        //                {
        //                    if (mcenc.eType == EType.Int32)
        //                    {
        //                        flag = false;
        //                        depthLength.Add((int)mcenc.value);
        //                    }
        //                }
        //            }
        //            else if( maen.metaCallArray.Count == 0 && i == lastNode.bracketExpressList.Count - 1 )
        //            {
        //                flag = false;
        //                depthLength.Add(-1);
        //            }
        //        }
        //        if (flag)
        //        {
        //            Log.AddMetaCoreLog(LID.ShowExtendMessage, "在[]中，只允许数字形式存在");
        //        }
        //    }
        //    int use_n_numone = 0;
        //    for( int i = depthLength.Count - 1; i >= 0; i--)
        //    {
        //        if(depthLength[i] == -1 )
        //        {
        //            if( use_n_numone == 2 )
        //            {
        //                Log.AddMetaCoreLog(LID.ShowExtendMessage, "在[]中，只允许从后边向前[3][-1][-1]这种形式，而不能使用[3][-1][2] 这种形式");
        //                continue;
        //            }
        //        }
        //        else
        //        {
        //            use_n_numone = 2;
        //        }
        //    }

        //    mt.SetArrayDismensionLength(depthLength);
        //}
        */
    }
}
