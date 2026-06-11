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
        private MetaBase m_OwnerMetaBase = null;
        private MetaBlockStatements m_OwnerMetaBlockStatements;
        private MetaType m_DefineMetaType = null;
        private MetaType m_NewObjectMetaType = null;
        private MetaType m_ReturnMetaType = null;
        private string m_DefineName;
        private bool m_AssignBlockedByConst = false;
        private int m_Id = 0;
        private EAssignTargetType m_AssignTargetType = EAssignTargetType.None;

        private Token m_Token = null;
        private Token m_AssignToken = null;
        private FileMetaSymbolTerm m_FileMetaSymbolTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        private AllowUseSettings m_AllowUseSettings = null;

        public MetaBraceAssignStatements(FileMetaOpAssignSyntax fmos, MetaType newmt, MetaBlockStatements mbs, MetaBase owmt )
        {
            m_DefineMetaType = newmt;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = owmt;
            if (fmos == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, "MetaBraceAssignStatements not found FileMetaOpAssignSyntax");
                return;
            }
            
            m_Token = fmos.token;
            m_AssignToken = fmos.assignToken;
            if (!fmos.variableRef.isOnlyName)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error ??" + mbs.ownerMetaClass?.allName + "??: " + mbs.ownerMetaFunction.name
                    + " ??: " + fmos.variableRef.ToTokenString());
                return;
            }
            MetaVariable targetMetaVariable = null;
            MetaType targetMetaType = null;
            m_DefineName = fmos.variableRef.name;
            if (m_NewObjectMetaType != null )
            {
                if(m_NewObjectMetaType.isData )
                {
                    var md = m_NewObjectMetaType.metaData;
                    m_MetaMemberData = md.GetMemberDataByName(m_DefineName);
                    m_AssignTargetType = EAssignTargetType.MemberData;
                    if (m_MetaMemberData == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "???????????");
                        return;
                    }
                    m_Id = m_MetaMemberData.GetHashCode();

                    if (m_MetaMemberData.realMetaType == null)
                    {
                        m_MetaMemberData.CreateMetaExpress();
                        m_MetaMemberData.ParseMetaExpress();
                        m_MetaMemberData.ParseRealMetaType();
                    }
                    targetMetaType = m_MetaMemberData.GetFinalMetaType();
                    if (targetMetaType == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "????????");
                        return;
                    }

                    targetMetaVariable = m_MetaMemberData;
                    m_Id = m_MetaMemberData.index;
                }
                else if( m_NewObjectMetaType.isDynamicData )
                {
                    m_AssignTargetType = EAssignTargetType.MemberData;
                    targetMetaType = null;
                }
                else if( m_NewObjectMetaType.isClass )
                {
                    m_AssignTargetType = EAssignTargetType.MemberVariable;

                    m_MetaMemberVariable = m_NewObjectMetaType.metaClass.GetMetaMemberVariableByName(m_DefineName);
                    m_AssignTargetType = EAssignTargetType.MemberVariable;
                    if (m_MetaMemberVariable == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "???????????");
                        return;
                    }
                    m_Id = m_MetaMemberVariable.index;
                    if (m_MetaMemberVariable.realMetaType == null)
                    {
                        m_MetaMemberVariable.CreateMetaExpress();
                        m_MetaMemberVariable.ParseMetaExpress();
                        m_MetaMemberVariable.ParseRealMetaType();
                    }
                    targetMetaType = m_MetaMemberVariable.GetFinalMetaType();
                    if (targetMetaType == null)
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "????????");
                        return;
                    }
                    targetMetaVariable = m_MetaMemberVariable;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "");
                }
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
        public MetaBraceAssignStatements(FileMetaDefineVariableSyntax fmdvs, MetaType newmt, MetaBlockStatements mbs, MetaBase owmt )
        {
            m_NewObjectMetaType = newmt;
            m_OwnerMetaBase = owmt;
            m_OwnerMetaBlockStatements = mbs;
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
                    m_MetaMemberVariable.SetOwnerMetaBase(m_NewObjectMetaType?.metaClass ?? mbs.ownerMetaClass);
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
        public MetaBraceAssignStatements(FileMetaCallTerm fmct, MetaType newmt, MetaBlockStatements mbs, MetaBase owmb )
        {
            m_NewObjectMetaType = newmt;
            m_OwnerMetaBase = owmb;
            m_FileMetaCallTerm = fmct;
            m_Token = fmct.token;
            m_MetaExpress = new MetaCallLinkExpressNode(fmct.callLink, owmb, mbs, null);
        }
        public MetaBraceAssignStatements(FileMetaSymbolTerm fmst, MetaType newmt,  MetaBlockStatements mbs, MetaBase owmb )
        {
            m_NewObjectMetaType = newmt;
            m_FileMetaSymbolTerm = fmst;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = owmb;
            m_Token = fmst.token;

            if (m_DefineMetaType.isMap )
            {
                if( fmst.symBolType != ETokenType.Colon )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "?Map???????:???");
                    return;
                }
            }
            else
            {
                if (fmst.symBolType != ETokenType.Assign )
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "?class???data???????=???");
                    return;
                }
            }
            if (fmst.left is not FileMetaCallTerm fmct1)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "?class???data?????????filemetaCallTerm");
                return;
            }

            if( fmct1.callLink.callNodeList.Count > 0 )
            {
                m_DefineName = fmct1.callLink.callNodeList[fmct1.callLink.callNodeList.Count - 1].name;
            }
            if (m_DefineMetaType.isDynamicData )
            {                   
                m_MetaMemberData = MetaMemberData.CreateDeclared(m_NewObjectMetaType.metaData, m_DefineName, -1, new MetaType(CoreMetaClassManager.objectMetaClass), false);
                m_MetaMemberData.SetOwnerBlockstatements(m_OwnerMetaBlockStatements);
                m_Id = m_MetaMemberData.index;                
            }
            else
            {
                if (m_DefineMetaType.isData)
                {
                    m_MetaMemberData = m_DefineMetaType.metaData?.GetMemberDataByName(m_DefineName);
                    if (m_MetaMemberData == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error ??" + m_NewObjectMetaType.name + "??: " + mbs?.ownerMetaFunction.name
                            + " ????: ?" + m_NewObjectMetaType.name + " ??:" + m_DefineName);
                    }
                    m_Id = m_MetaMemberData.index;
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error ??" + m_DefineMetaType.metaClass?.allName + "??: " + mbs?.ownerMetaFunction.name
                            + " ????: ?" + m_DefineMetaType.metaClass?.allName + " ??:" + m_DefineName);
                    }
                    m_Id = m_MetaMemberVariable.GetHashCode();
                    //m_MetaExpress = CreateExpressNodeInNewObjectStatements(m_MetaMemberVariable, m_OwnerMetaBlockStatements, m_FileMetaOpAssignSyntax?.express);
                }
            }

            if (m_DefineMetaType.isDynamicData )
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
            m_ReturnMetaType = defineMt;
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
            m_Id = m_MetaMemberVariable.index;
            m_AssignTargetType = EAssignTargetType.MemberVariable;
        }        
        public MetaBraceAssignStatements(MetaMemberData mmd, MetaBlockStatements mbs, MetaBase owmt, MetaExpressNodeBase men )
        {
            m_MetaMemberData = mmd;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaBase = owmt;
            m_MetaExpress = men;
            m_Token = men.token;
            m_AssignTargetType =  EAssignTargetType.MemberData;
            m_DefineMetaType = new MetaType(mmd.GetFinalMetaType());
            m_Id = m_MetaMemberData.index;
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
                                "const ?????????????? '=' ????: " + m_MetaMemberData.name);
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
                                "const ?????????????? '=' ????: " + m_MetaMemberVariable.name);
                            return;
                        }
                        mv = m_MetaMemberVariable;
                    }
                    break;
            }
            if( m_MetaExpress != null )
            {
                m_MetaExpress.Parse(aus);
                m_MetaExpress.CalcReturnType();
                m_MetaExpress = ExpressManager.ConvertNewExpress(m_MetaExpress, m_DefineMetaType );
            }
        }
        public void SetDefineMetaType(MetaType defineMt)
        {
            m_DefineMetaType = defineMt;
        }
        public MetaType GetRetMetaType()
        {
            if(m_ReturnMetaType != null )
            {
                return m_ReturnMetaType;
            }
            if (m_MetaExpress != null)
            {
                m_ReturnMetaType = m_MetaExpress.GetReturnMetaType();
            }
            return m_ReturnMetaType;
        }
        public void CalcReturnType()
        {
            if(m_ReturnMetaType != null )
            {
                return;
            }

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
                    case EAssignTargetType.ArrayValue:
                        {
                            m_ReturnMetaType = m_DefineMetaType;
                        }
                        break;
                }

                // If array element type is explicitly declared (and not object),
                // force const literal conversion to target element type instead of numeric promotion.
                if (m_DefineMetaType != null
                    && m_DefineMetaType.metaClass != CoreMetaClassManager.objectMetaClass
                    && m_MetaExpress is MetaConstExpressNode constExpressNode)
                {
                    if (!ExpressManager.TryAdjustConstExpressByDefineMetaType(m_DefineMetaType, constExpressNode ))
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
                    System.Diagnostics.Debug.Write("??{}???????????!!");
                }
            }
        }
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
                        || string.Equals(de.allName, ee.allName, StringComparison.Ordinal);
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
                    if (!string.Equals(dd.allName, ed.allName, StringComparison.Ordinal))
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

            var relation = TypeManager.ValidateClassTypeRelation(dc, ec);
            if (relation == ETypeRelation.Same
                || relation == ETypeRelation.Child
                || relation == ETypeRelation.Interface)
            {
                return true;
            }

            if (relation == ETypeRelation.Num)
            {
                return TypeManager.IsNarrowerCorePrimitiveWideningOkForCallSite(ec, dc);
            }

            return false;
        }      
        public void ValidateDefineAgainstDeclaredMetaType()
        {
            MetaType contentMt = GetRetMetaType();
            bool isOmittedExpression = expressNode == null;

            bool ValidateEnumCompare(MetaType defineMt, MetaType expressMt, string scene)
            {
                if (defineMt == null || expressMt == null)
                {
                    return true;
                }

                if (!defineMt.isEnum && !expressMt.isEnum)
                {
                    return true;
                }

                if (!(defineMt.isEnum && expressMt.isEnum))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_AssignToken ?? m_Token,
                        scene + " defineMt is Enum and expressMt is Enum define=" + defineMt.ToString() + ", express=" + expressMt.ToString());
                    return false;
                }

                var de = defineMt.metaEnum;
                var ee = expressMt.metaEnum;
                bool sameEnumHost = de != null && ee != null
                    && (ReferenceEquals(de, ee)
                        || string.Equals(de.allName, ee.allName, StringComparison.Ordinal));
                if (!sameEnumHost)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_AssignToken ?? m_Token,
                        scene + " ????????????, define=" + defineMt.ToString() + ", express=" + expressMt.ToString());
                    return false;
                }

                var dv = defineMt.enumValue;
                var ev = expressMt.enumValue;
                if (dv != null && ev != null)
                {
                    bool sameEnumValue = string.Equals(dv.name, ev.name, StringComparison.Ordinal) || dv.index == ev.index;
                    if (!sameEnumValue)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_AssignToken ?? m_Token,
                            scene + " ??????????, define=" + defineMt.ToString() + ", express=" + expressMt.ToString());
                        return false;
                    }
                }

                return true;
            }

            bool ValidateCommon(MetaType defineMt, string scene)
            {
                if (defineMt == null)
                {
                    return true;
                }

                if (contentMt == null)
                {
                    return true;
                }

                if (!ValidateEnumCompare(defineMt, contentMt, scene))
                {
                    return false;
                }

                if (!IsBraceAssignDeclaredCompatibleWithExpress(defineMt, contentMt))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_AssignToken ?? m_Token,
                        scene + " ????????, define=" + defineMt.ToString() + ", express=" + contentMt.ToString());
                    return false;
                }

                return true;
            }

            bool ValidateArrayElement(MetaType arrayDefineMt, string scene)
            {
                if (arrayDefineMt == null)
                {
                    return true;
                }

                if (!arrayDefineMt.IsArray())
                {
                    return ValidateCommon(arrayDefineMt, scene);
                }

                var elementMt = arrayDefineMt.GetMetaTypeByIndex(0);
                if (elementMt == null)
                {
                    return true;
                }

                bool isNumLike = NumberManager.IsNumberClass(elementMt.metaClass) || TypeManager.IsAbstractNumberMetaType(elementMt);
                bool isNullLiteral = contentMt != null && contentMt.isNull;
                if (isNumLike && (isOmittedExpression || isNullLiteral) && elementMt.isNullable == false)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                        "???????????/Num ??????????????????????????? null");
                    return false;
                }

                if (isOmittedExpression || isNullLiteral || contentMt == null)
                {
                    return true;
                }

                if (contentMt.IsArray())
                {
                    if (IsBraceAssignDeclaredCompatibleWithExpress(arrayDefineMt, contentMt)
                        || IsBraceAssignDeclaredCompatibleWithExpress(elementMt, contentMt)
                        || TypeManager.TryGetCompatibleArrayMetaType(arrayDefineMt, contentMt, out _))
                    {
                        return true;
                    }

                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                        scene + " ???????????????, define=" + arrayDefineMt.ToString() + ", express=" + contentMt.ToString());
                    return false;
                }

                if (elementMt.metaClass == CoreMetaClassManager.objectMetaClass)
                {
                    return true;
                }

                if (!ValidateEnumCompare(elementMt, contentMt, scene + "(ArrayElement)"))
                {
                    return false;
                }

                if (!IsBraceAssignDeclaredCompatibleWithExpress(elementMt, contentMt))
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                        scene + " ???????????????, element=" + elementMt.ToString() + ", express=" + contentMt.ToString());
                    return false;
                }

                return true;
            }

            switch (m_AssignTargetType)
            {
                case EAssignTargetType.MemberVariable:
                {
                    var defineMt = m_MetaMemberVariable?.GetFinalMetaType() ?? m_DefineMetaType;
                    if (defineMt != null && defineMt.IsArray())
                    {
                        ValidateArrayElement(defineMt, "MemberVariable");
                    }
                    else
                    {
                        ValidateCommon(defineMt, "MemberVariable");
                    }
                }
                break;
                case EAssignTargetType.MemberData:
                {
                    var defineMt = m_MetaMemberData?.GetFinalMetaType() ?? m_DefineMetaType;
                    if (defineMt != null && defineMt.IsArray())
                    {
                        ValidateArrayElement(defineMt, "MemberData");
                    }
                    else
                    {
                        ValidateCommon(defineMt, "MemberData");
                    }
                }
                break;
                case EAssignTargetType.ArrayValue:
                {
                    ValidateArrayElement(m_DefineMetaType, "ArrayValue");
                }
                break;
                case EAssignTargetType.AnonVariable:
                {
                    var defineMt = m_MetaMemberData?.GetFinalMetaType()
                        ?? m_MetaMemberVariable?.GetFinalMetaType()
                        ?? m_DefineMetaType;
                    if (defineMt != null && defineMt.IsArray())
                    {
                        ValidateArrayElement(defineMt, "AnonVariable");
                    }
                    else
                    {
                        ValidateCommon(defineMt, "AnonVariable");
                    }
                }
                break;
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
        /// 
        /// </summary>
//        public static MetaType GetMaxLevelMetaType(IReadOnlyList<MetaBraceAssignStatements> assignStatementsList, MetaType defineMetaType)
//        {
//            var objmt = new MetaType(CoreMetaClassManager.objectMetaClass);
//            if (assignStatementsList == null || assignStatementsList.Count == 0)
//            {
//                return objmt;
//            }

//            if (TypeManager.TryGetPreferredElementMetaTypeFromDefine(defineMetaType, out var preferredElementMetaType))
//            {
//                bool allAssignableToPreferred = true;
//                for (int i = 0; i < assignStatementsList.Count; i++)
//                {
//                    var itemType = assignStatementsList[i].GetRetMetaType();
//                    if (!TypeManager.IsArrayLiteralElementAssignableToTarget(preferredElementMetaType, itemType))
//                    {
//                        allAssignableToPreferred = false;
//                        break;
//                    }
//                }

//                if (allAssignableToPreferred)
//                {
//                    return new MetaType(preferredElementMetaType);
//                }
//            }

//            if (assignStatementsList.Count == 1)
//            {
//                var only = assignStatementsList[0].GetRetMetaType();
//                if (only == null || only.isNull)
//                {
//                    return objmt;
//                }
//                return only;
//            }

//            var types = new List<MetaType>(assignStatementsList.Count);
//            for (int i = 0; i < assignStatementsList.Count; i++)
//            {
//                var t = assignStatementsList[i].GetRetMetaType();
//                if (t == null || t.isNull)
//                {
//                    return objmt;
//                }
//                types.Add(t);
//            }

//            bool allNumeric = true;
//            for (int i = 0; i < types.Count; i++)
//            {
//                if (!NumberManager.IsNumberClass(types[i].metaClass))
//                {
//                    allNumeric = false;
//                    break;
//                }
//            }
//            if (allNumeric)
//            {
//                bool hasInt64 = false;
//                bool hasUInt64 = false;
//                int maxRank = int.MinValue;
//                for (int i = 0; i < types.Count; i++)
//                {
//                    var numericClass = types[i].metaClass;
//                    if (numericClass == CoreMetaClassManager.int64MetaClass)
//                    {
//                        hasInt64 = true;
//                    }
//                    else if (numericClass == CoreMetaClassManager.uint64MetaClass)
//                    {
//                        hasUInt64 = true;
//                    }

//                    if (!NumberManager.TryGetLiteralPromotionRank(types[i].metaClass, out int rank))
//                    {
//                        return objmt;
//                    }
//                    if (rank > maxRank)
//                    {
//                        maxRank = rank;
//                    }
//                }

//                if (hasInt64 && hasUInt64)
//                {
//                    return objmt;
//                }

//                var promotedMc = NumberManager.GetMetaClassForLiteralPromotionRank(maxRank);
//                return promotedMc != null ? new MetaType(promotedMc) : objmt;
//            }

//            int frontOpLevel = 0;
//            var mt = new MetaType(CoreMetaClassManager.objectMetaClass);
//#pragma warning disable CS0219 // ????????????????
//            bool isAllSame = true;
//#pragma warning restore CS0219 // ????????????????
//            for (int i = 0; i < assignStatementsList.Count - 1; i++)
//            {
//                MetaBraceAssignStatements cmc = assignStatementsList[i];
//                MetaBraceAssignStatements nmc = assignStatementsList[i + 1];

//                var cmcmt = cmc.GetRetMetaType();
//                var nmcmt = nmc.GetRetMetaType();
//                if (cmcmt.isNull)
//                {
//                    return objmt;
//                }
//                if (nmcmt.isNull)
//                {
//                    return objmt;
//                }
//                if (!TypeManager.CompareMetaType(cmcmt, nmcmt))
//                {
//                    if (cmcmt.IsArray() && nmcmt.IsArray()
//                        && TypeManager.TryGetCompatibleArrayMetaType(cmcmt, nmcmt, out var compatibleArrayMetaType))
//                    {
//                        mt = compatibleArrayMetaType;
//                        frontOpLevel = cmc.opLevel > nmc.opLevel ? cmc.opLevel : nmc.opLevel;
//                        isAllSame = true;
//                        continue;
//                    }
//                    return objmt;
//                }
//                if (cmc.opLevel == nmc.opLevel && nmc.opLevel > frontOpLevel)
//                {
//                    if (cmc.opLevel == 10)
//                    {
//                        var cutmt = cmc.GetRetMetaType();
//                        var nextmt = nmc.GetRetMetaType();
//                        var cur = cutmt.metaClass;
//                        var next = nextmt.metaClass;
//                        var relation = TypeManager.ValidateClassTypeRelation(cur, next);
//                        if (relation == ETypeRelation.Same
//                            || relation == ETypeRelation.Child)
//                        {
//                            mt = nextmt;
//                            frontOpLevel = cmc.opLevel;
//                        }
//                        else if (relation == ETypeRelation.Parent)
//                        {
//                            mt = cutmt;
//                        }
//                        else
//                        {
//                            isAllSame = false;
//                            break;
//                        }
//                    }
//                    else
//                    {
//                        var currentType = cmc.GetRetMetaType();
//                        var nextType = nmc.GetRetMetaType();
//                        if (currentType != null && nextType != null
//                            && currentType.IsArray() && nextType.IsArray()
//                            && TypeManager.TryGetCompatibleArrayMetaType(currentType, nextType, out var compatibleArrayMetaType2))
//                        {
//                            mt = compatibleArrayMetaType2;
//                            frontOpLevel = cmc.opLevel;
//                            isAllSame = true;
//                        }
//                        else
//                        {
//                            mt = currentType;
//                            frontOpLevel = cmc.opLevel;
//                            isAllSame = true;
//                        }
//                    }

//                }
//                else
//                {
//                    var currentType = cmc.GetRetMetaType();
//                    var nextType = nmc.GetRetMetaType();
//                    if (currentType != null && nextType != null
//                        && currentType.IsArray() && nextType.IsArray()
//                        && TypeManager.TryGetCompatibleArrayMetaType(currentType, nextType, out var compatibleArrayMetaType3))
//                    {
//                        mt = compatibleArrayMetaType3;
//                        frontOpLevel = Math.Max(cmc.opLevel, nmc.opLevel);
//                        isAllSame = true;
//                        continue;
//                    }

//                    if (nmc.opLevel > frontOpLevel)
//                    {
//                        if (cmc.opLevel > nmc.opLevel)
//                        {
//                            frontOpLevel = cmc.opLevel;
//                            mt = cmc.GetRetMetaType();
//                        }
//                        else
//                        {
//                            frontOpLevel = nmc.opLevel;
//                            mt = nmc.GetRetMetaType();
//                        }
//                    }
//                }
//            }
//            return mt;
//        }
    }



    public sealed class MetaNewObjectExpressNode : MetaExpressNodeBase
    {
        //public enum EStatementsContentType
        //{
        //    None,
        //    ArrayValue,
        //    ClassValueAssign,
        //    DataValueAssign,
        //    DynamicData,
        //}

        public enum ENewType
        {
            DefaultType, //int32,uint32/string/..
            CommomClass,  //define class
            ArrayClass,     // array class
            ListClass,
            MapClass,
        }

        //public ENewType newType => m_NewType;
        public MetaExpressNodeBase arrayLengthExpress => m_ArrayLengthExpress;
        public List<MetaExpressNodeBase> metaInputParamList => m_MetaInputParamList;
        public MetaMemberFunction metaMemberFunction => m_MetaMemberFunction;
        public List<MetaBraceAssignStatements> assignStatementsList => m_AssignStatementsList;

        private FileMetaParTerm m_FileMetaParTerm = null;
        private FileMetaCallTerm m_FileMetaCallTerm = null;
        private List<FileMetaBraceTerm> m_FileMetaBraceTermList = new List<FileMetaBraceTerm>();
        private FileMetaConstValueTerm m_FileMetaConstValueTerm = null;

        private MetaExpressNodeBase m_MetaEnumValue = null;
        private readonly List<MetaBraceAssignStatements> m_AssignStatementsList = new List<MetaBraceAssignStatements>();
        private FileMetaBaseTerm m_BraceFileMetaBaseTerm = null;
        //private EStatementsContentType m_StatementsContentType = EStatementsContentType.None;
        private ENewType m_NewType = ENewType.CommomClass;

        private MetaType m_DefineMetaType = null;
        private MetaType m_NewMetaType = null;
        private MetaType m_ArrayCalcMetaType = null;
        private AllowUseSettings m_AllowUseSettings = null;
        private MetaExpressNodeBase m_ArrayLengthExpress = null;
        private MetaArrayExpressNode m_ArrayExpressNode = null;
        private MetaMemberFunction m_MetaMemberFunction = null;
        private List<MetaExpressNodeBase> m_MetaInputParamList = new List<MetaExpressNodeBase>();

        public static MetaNewObjectExpressNode CreateFromAnonymousMetaData(
            MetaData metaData,
            MetaBase mb,
            MetaBlockStatements mbs )
        {
            if (metaData == null )
            {
                return null;
            }
            var findmd = ClassManager.instance.FindMetaDataByNameAndType(metaData);
            if( findmd == null )
            {
                ClassManager.instance.AddAnonymousMetaData(metaData);
                metaData.HandleExtendContent();
                metaData.ParseExtendsRelation();
                findmd = metaData;
            }

            var anonymousType = new MetaType(findmd);
            var node = new MetaNewObjectExpressNode(anonymousType, mb, mbs);
            var ordered = metaData.GetMetaMemberDataList();
            ordered.Sort((a, b) => a.index.CompareTo(b.index));

            var parseSetting = new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress };
            foreach (var sourceField in ordered)
            {
                var expr = sourceField.expressNode;
                expr.SetOwnerBase(findmd);
                var mas = new MetaBraceAssignStatements(sourceField, mbs, findmd, expr);
                //mas.Parse(new AllowUseSettings());
                node.m_AssignStatementsList.Add(mas);
            }

            node.Parse(parseSetting);
            node.CalcReturnType();
            return node;
        }
        public MetaNewObjectExpressNode( MetaType defineMt, MetaArrayExpressNode maen, MetaBase owmb, MetaBlockStatements mbs)
        {
            m_DefineMetaType = defineMt;
            m_OwnerMetaBase = owmb;
            m_OwnerMetaBlockStatements = mbs;
            m_NewType = ENewType.ArrayClass;
            m_Token = maen.token;
            m_ArrayExpressNode = maen;
            //m_StatementsContentType = EStatementsContentType.ArrayValue;
        }
        // Class1 c = { a = 20, b = 20 };  => Class1 c = Class1(); c.a = 20; c.b = 20;
        // dynamic c = {a = 20, b = 20} => ??? 
        // data c = {a = 20, b = 20} | c = {a = 20, b = 20} => ????  
        // Map<int,string> map1 = new(10){ 1:"20", 2:"30", 3:"50" }
        // List<int> list1 = new(){ 1,2,3,4,5 }
        public MetaNewObjectExpressNode(FileMetaBraceTerm fmbt, MetaType mt, MetaBase owmb, MetaBlockStatements mbs)
        {
            m_OwnerMetaBase = owmb;
            m_OwnerMetaBlockStatements = mbs;
            m_DefineMetaType = mt;
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
        }
        // Array arr = [1,2,3]   [Class1(), Class2(), variable1.a.b(),100]
        public MetaNewObjectExpressNode(FileMetaBracketTerm fmbt, MetaType defineMt, MetaBase owmb, MetaBlockStatements mbs )
        {
            m_OwnerMetaBase = owmb;
            m_OwnerMetaBlockStatements = mbs;
            m_Token = fmbt.token;
            m_BraceFileMetaBaseTerm = fmbt;

            m_DefineMetaType = defineMt;
            m_NewType = ENewType.ArrayClass;
        }
        // Class1(10){ c1 = 20, c2 = 30 }  int[2][]{ [1,2,3], [3,4,5] }
        public MetaNewObjectExpressNode(MetaType defineMt, MetaCallLinkExpressNode mcen)
        {
            m_OwnerMetaBase = mcen.ownerMetaBase;
            m_OwnerMetaBlockStatements = mcen.ownerMetaBlockStatements;

            m_MetaMemberFunction = mcen.metaCallLink.finalCallNode.methodCall?.function as MetaMemberFunction;
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

            var lastNode = initNode ?? mcen.metaCallLink.callNodeList[mcen.metaCallLink.callNodeList.Count - 1];
            m_Token = lastNode.token;
            m_NewType = ENewType.CommomClass;
            m_DefineMetaType = defineMt;
            if (lastNode.token.type == ETokenType.New)
            {
                if( m_DefineMetaType == null )
                {
                    m_NewMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                }
                else
                {
                    m_NewMetaType = new MetaType(defineMt);
                }
                if (m_DefineMetaType.IsArray())
                {
                    m_NewType = ENewType.ArrayClass;
                }
            }
            else
            {
                m_NewMetaType = new MetaType( mcen.metaCallLink.finalCallNode.callMetaType );
                if (m_NewMetaType.IsArray())
                {
                    m_NewType = ENewType.ArrayClass;
                }
            }
            m_BraceFileMetaBaseTerm = lastNode.fileMetaBraceTerm;
            if (mcen.metaCallLink.finalCallNode.callMetaType.IsArray())
            {
                if (mcen.metaCallLink.callNodeList.Count > 0)
                {
                    if (lastNode.metaInputParamCollection != null)
                    {
                        SetInputParams(lastNode.metaInputParamCollection);
                    }
                }
            }
            else
            {
                if (mcen.metaCallLink.callNodeList.Count > 0)
                {
                    SetInputParams(lastNode.metaInputParamCollection);
                }
            }
        }
        // 1..x
        public MetaNewObjectExpressNode(FileMetaConstValueTerm arrayLinkToken, MetaBase ownerMetaBase, MetaBlockStatements mbs)
        {
            m_FileMetaConstValueTerm = arrayLinkToken;
            m_OwnerMetaBase = ownerMetaBase;
            m_OwnerMetaBlockStatements = mbs;

            var metaInputTemplateCollection = new MetaInputTemplateCollection();
            MetaType mitp = new MetaType(CoreMetaClassManager.int32MetaClass);
            metaInputTemplateCollection.AddMetaTemplateParamsList(mitp);

            m_ExpressReturnMetaType = new MetaType(CoreMetaClassManager.rangeMetaClass, null, metaInputTemplateCollection);            
            m_NewMetaType = m_ExpressReturnMetaType;

            MetaInputParamCollection mdpc = new MetaInputParamCollection(ownerMetaBase, mbs);
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
                    //??????????
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
                    //??????????
                }

                MetaInputParam mip3 = new MetaInputParam(new MetaConstExpressNode(EType.Int32, 1));
                mdpc.AddMetaInputParam(mip3);
            }
        }
        public MetaNewObjectExpressNode(MetaType mt, MetaBase ownerMC, MetaBlockStatements mbs)
        {
            m_OwnerMetaBase = ownerMC;
            m_OwnerMetaBlockStatements = mbs;
            m_DefineMetaType = mt;

            if (m_DefineMetaType.IsArray())
            {
                m_NewType = ENewType.ArrayClass;
            }
            else
            {
                m_NewType = ENewType.CommomClass;
            }
        }
        public void GetAssignStatementsArrayMetaType()
        {
            bool isArrayDefine = m_NewType == ENewType.ArrayClass
                || (m_NewMetaType != null && m_NewMetaType.IsArray())
                || (m_DefineMetaType != null && m_DefineMetaType.IsArray());

            if (isArrayDefine)
            {
                var mtList = new List<MetaType>(m_AssignStatementsList.Count);
                for (int i = 0; i < m_AssignStatementsList.Count; i++)
                {
                    var mt = m_AssignStatementsList[i]?.GetRetMetaType();
                    mtList.Add(mt);
                }

                var inputType = TypeManager.GetMaxCompatibleMetaTypeFromList(mtList);

                m_ArrayCalcMetaType = new MetaType();
                m_ArrayCalcMetaType.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
                m_ArrayCalcMetaType.AddDefineTemplateMetaType(inputType);
                m_ArrayCalcMetaType.SetArrayLength(m_AssignStatementsList.Count);
            }
            //Log.AddMetaCoreLog( LID.MetaCoreAssertShowMessage, m_Token, "MetaNewObjectExpressNode GetMaxLevelMetaType m_NewMetaType and m_DefineMetaType are null");
        }
        public void ParseBraceStatementsContent(AllowUseSettings aws, MetaType mt )
        {
            if (m_ArrayExpressNode != null)
            {
                MetaType cmt = null;
                if (mt?.IsArray() == true  )
                {
                    var ggtml = mt.GetGenTemplateMetaTypeList();
                    if (ggtml.Count > 0)
                    {
                        cmt = ggtml[0];
                    }
                }

                for (int i = 0; i < m_ArrayExpressNode.metaCallArray.Count; i++)
                {
                    var mca = m_ArrayExpressNode.metaCallArray[i];
                    mca = ExpressManager.ConvertNewExpress(mca, cmt );
                    MetaBraceAssignStatements mas = new MetaBraceAssignStatements(null, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, mca );
                    m_AssignStatementsList.Add(mas);
                }
            }
            else
            {
                if (m_BraceFileMetaBaseTerm?.fileMetaExpressList?.Count > 0)
                {
                    //Log.AddMetaCoreLog(LID.ShowExtendMessage, "??????????");
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
                                var mas = new MetaBraceAssignStatements(fmdvs, mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase);
                                if (mas.expressNode == null)
                                {
                                    continue;
                                }
                                m_AssignStatementsList.Add(mas);
                                mas.Parse(new AllowUseSettings());
                                mas.CalcReturnType();
                            }
                            else if (braceTerm.fileMetaAssignSyntaxList[i] is FileMetaOpAssignSyntax fmoas)
                            {
                                var mas = new MetaBraceAssignStatements(fmoas, mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase);
                                if (mas.expressNode == null)
                                {
                                    continue;
                                }
                                mas.Parse(new AllowUseSettings());
                                mas.CalcReturnType();
                                m_AssignStatementsList.Add(mas);
                            }
                        }
                    }
                }
            }
        }
        //???{ Node1, Node2  } ?{}?????Node1, Node2 ????? Node1, ??? aaa = 1, "aa":1, 2:33, [1,2,3] [1] 3, this.value ?????
        public void HandleBraceTermNode( FileMetaBaseTerm fmbt, MetaType mt, AllowUseSettings aws)
        {
            if (mt.isData)
            {
                //???????? ?????????? data a = { aaa = 10, bbb = 20} ?????
                if (mt.isDynamicData)
                {
                    if (fmbt is FileMetaSymbolTerm fmst)                   
                    {
                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, m_DefineMetaType, m_OwnerMetaBlockStatements, m_OwnerMetaBase );
                        mas.Parse(aws);
                        mas.CalcReturnType();
                        m_AssignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbolterm in data " );
                        return;
                    }
                    //m_StatementsContentType = EStatementsContentType.DynamicData;
                }
                else
                {
                    //??????? ?????????? data a{ aaa = 10; bbb = 20 }  a = { aaa = 10, bbb = 20} ????? ??data ??????
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        MetaBraceAssignStatements mas = new MetaBraceAssignStatements(fmst, mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase );
                        mas.Parse(aws);
                        mas.CalcReturnType();
                        m_AssignStatementsList.Add(mas);
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbolterm" );
                        return;
                    }
                    //m_StatementsContentType = EStatementsContentType.DataValueAssign;
                }
            }
            else if (mt.IsArray() )// ???????
            {
                //m_StatementsContentType = EStatementsContentType.ArrayValue;
                var genList = mt.GetGenTemplateMetaTypeList();
                if (genList.Count != 1 )
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "not define template meta type list");
                    return;
                }
                MetaType cmt = genList[0];
                //cmt.SetArrayLength(-1);
                if (fmbt is FileMetaBracketTerm fmst)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmst, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements );
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, mnoe );
                    m_AssignStatementsList.Add(mas);                    
                }
                else if (fmbt is FileMetaBraceTerm fmbrt)
                {
                    // ?????????????????????
                    // int[][][] a = { { {1,2}, {3,4} }, { {5,6}, {7,8} } };
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbrt, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements );
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
                    cep.metaType = null;
                    cep.fme = fmct;
                    cep.equalMetaVariable = null;
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
                    var mas = new MetaBraceAssignStatements(null, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else if( fmbt is FileMetaSymbolTerm fmst2 )
                {
                    if( fmst2.symBolType != ETokenType.Comma )
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "??????,????,");
                    }
                }
                else if( fmbt is FileMetaTermExpress termexpress )
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaBase = m_OwnerMetaBase;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = termexpress;
                    cep.equalMetaVariable = null;
                    MetaExpressNodeBase men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    men = ExpressManager.ConvertNewExpress(men, cep.metaType );                   
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, fmbt.token, "Error ????????FileMetaBracketTerm ??!");
                }
            }
            // Array<Object>(n){ ... } ??? [1,2] ????????? defineMetaType ????? object?? Array??
            // ??????? { ??= }????????????????/??/[]/????
            else if (mt != null && mt.metaClass == CoreMetaClassManager.objectMetaClass && !mt.IsArray())
            {
                //m_StatementsContentType = EStatementsContentType.ArrayValue;
                MetaType cmt = mt;
                if (fmbt is FileMetaBracketTerm fmstOb)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmstOb, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements );
                    mnoe.Parse(aws);
                    mnoe.CalcReturnType();
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, mnoe);
                    m_AssignStatementsList.Add(mas);
                }
                else if (fmbt is FileMetaBraceTerm fmbrtOb)
                {
                    MetaNewObjectExpressNode mnoe = new MetaNewObjectExpressNode(fmbrtOb, cmt, m_OwnerMetaBase, m_OwnerMetaBlockStatements );
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
                    cep.equalMetaVariable = null;
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
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "??????,????,");
                    }
                }
                else if (fmbt is FileMetaTermExpress termexpressOb)
                {
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.ownerMetaBase = m_OwnerMetaBase;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.metaType = new MetaType(cmt);
                    cep.fme = termexpressOb;
                    cep.equalMetaVariable = null;
                    MetaExpressNodeBase men = ExpressManager.CreateExpressNode(cep);
                    men.Parse(new AllowUseSettings());
                    men = ExpressManager.ConvertNewExpress(men, cep.metaType );
                    var mas = new MetaBraceAssignStatements( mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase, cmt, men);
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Array<Object> ???????????!");
                }
            }
            else if (mt.isMap)   // ??????? ??   a:10, b:20  20:"aa" ?????
            {
                if (fmbt is FileMetaSymbolTerm fmst)
                {
                    MetaBraceAssignStatements mas = new MetaBraceAssignStatements( fmst, mt,  m_OwnerMetaBlockStatements, m_OwnerMetaBase );
                    mas.CalcReturnType();
                    m_AssignStatementsList.Add(mas);
                    //m_StatementsContentType = EStatementsContentType.ClassValueAssign;
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "isMap" );
                    return;
                }
            }
            else
            {
                /*
                //????????
                if (mt.isDynamicClass)
                {
                    MetaData anonClass = new MetaData("DynamicClass__" + GetHashCode(), false, false, true );
                    //????????
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
                else// ???????
                */
                {
                    if (fmbt is FileMetaSymbolTerm fmst)
                    {
                        var mas = new MetaBraceAssignStatements(fmst, mt, m_OwnerMetaBlockStatements, m_OwnerMetaBase );
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
                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberShouldNameEqualExpressFormat, m_Token, "symbol term in common class" );
                        return;
                    }
                    //m_StatementsContentType = EStatementsContentType.ClassValueAssign;
                }
            }            
        }
        public override void Parse(AllowUseSettings auc)
        {
            m_AllowUseSettings = auc;
            if (m_NewMetaType != null)
            {
                ParseBraceStatementsContent(auc, m_NewMetaType);
            }
            else if (m_DefineMetaType != null)
            {
                ParseBraceStatementsContent(auc, m_DefineMetaType);
            }
            else
            {
                ParseBraceStatementsContent(auc, null);
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

            if ( m_MetaInputParamList.Count == 0
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

            if( this.m_NewType == ENewType.ArrayClass )
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
        public override void CalcReturnType()
        {
            if (m_ExpressReturnMetaType != null) return;

            if (m_AssignStatementsList.Count > 0)
            {
                if( this.m_NewType == ENewType.ArrayClass )
                {
                    if(m_ArrayCalcMetaType == null )
                    {
                        GetAssignStatementsArrayMetaType();
                    }

                }
                else if( m_NewType == ENewType.CommomClass )
                {
                    if(m_DefineMetaType == null && m_NewMetaType == null )
                    {
                        m_NewMetaType = new MetaType(CoreMetaClassManager.dynamicMetaData);
                    }
                }
                else
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "!!!!");
                }
            }
            var mipc = new MetaInputParamCollection(ownerMetaBase, m_OwnerMetaBlockStatements);

            if (m_NewMetaType != null )
            {
                m_ExpressReturnMetaType = new MetaType(m_NewMetaType);
                bool isArray = m_ExpressReturnMetaType.IsArray();
                if( m_DefineMetaType != null )
                {
                    if (!TypeManager.CompareLeftRightMetaType(m_DefineMetaType, m_NewMetaType, m_Token, out MetaType convertMt))
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "m_ExpressReturnMetaType is null");
                    }
                    if (isArray&& m_ExpressReturnMetaType.arrayLength == -1)
                    {
                        m_ExpressReturnMetaType.SetArrayLength(m_DefineMetaType.arrayLength);
                    }
                }
                if(m_ArrayCalcMetaType != null )
                {
                    if( !TypeManager.CompareLeftRightMetaType(m_NewMetaType, m_ArrayCalcMetaType, m_Token, out MetaType convertMt ) )
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "m_ExpressReturnMetaType is null");
                    }
                    if (isArray && m_ExpressReturnMetaType.arrayLength == -1)
                    {
                        m_ExpressReturnMetaType.SetArrayLength(m_ArrayCalcMetaType.arrayLength);
                    }
                }
            }
            else if (m_DefineMetaType != null )
            {
                if (m_ArrayCalcMetaType != null)
                {
                    if (TypeManager.CompareLeftRightMetaType(m_DefineMetaType, m_ArrayCalcMetaType, m_Token, out MetaType convertMt))
                    {
                        if(convertMt != null )
                        {
                            m_ExpressReturnMetaType = new MetaType(convertMt);
                        }
                        else
                        {
                            m_ExpressReturnMetaType = new MetaType(m_ArrayCalcMetaType);
                        }
                        if (m_DefineMetaType.arrayLength != -1)
                        {
                            m_ExpressReturnMetaType.SetArrayLength(m_DefineMetaType.arrayLength);
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "m_ExpressReturnMetaType is null");
                    }
                }
                else
                {
                    m_ExpressReturnMetaType = new MetaType(m_DefineMetaType);
                }

            }
            else if ( m_ArrayCalcMetaType != null)
            {
                m_ExpressReturnMetaType = m_ArrayCalcMetaType;
            }

            if (m_ExpressReturnMetaType == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "m_ExpressReturnMetaType is null");
                return;
            }

            if (m_ExpressReturnMetaType.IsArray())
            {
                SetupArrayMemberFunctionAndLengthParam(mipc);
                return;
            }

        }
        //private bool TryResolveNonArrayReturnMetaTypeByRelation(out MetaType result)
        //{
        //    result = null;

        //    if (m_DefineMetaType != null && m_NewMetaType != null)
        //    {
        //        bool related = MetaBraceAssignStatements.IsBraceAssignDeclaredCompatibleWithExpress(m_DefineMetaType, m_NewMetaType);
        //        if (!related)
        //        {
        //            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "??????????new??????????");
        //            return false;
        //        }
        //        result = new MetaType(m_NewMetaType);
        //        return true;
        //    }

        //    if (m_NewMetaType != null)
        //    {
        //        result = new MetaType(m_NewMetaType);
        //        return true;
        //    }

        //    if (m_DefineMetaType != null)
        //    {
        //        result = new MetaType(m_DefineMetaType);
        //        return true;
        //    }

        //    return true;
        //}
        //private bool TryResolveArrayReturnMetaTypeByRelation(out MetaType result)
        //{
        //    result = null;

        //    MetaType defineArray = (m_DefineMetaType != null && m_DefineMetaType.IsArray()) ? m_DefineMetaType : null;
        //    MetaType newArray = (m_NewMetaType != null && m_NewMetaType.IsArray()) ? m_NewMetaType : null;
        //    MetaType calcArray = (m_ArrayCalcMetaType != null && m_ArrayCalcMetaType.IsArray()) ? m_ArrayCalcMetaType : null;
        //    if (newArray != null)
        //    {
        //        result = new MetaType(newArray);
        //        return true;
        //    }
        //    else if (defineArray != null)
        //    {
        //        result = new MetaType(defineArray);
        //        return true;
        //    }
        //    else if (calcArray != null)
        //    {
        //        result = new MetaType(calcArray);
        //        return true;
        //    }
        //    return false;
            
        //    //if (defineArray != null && newArray != null)
        //    //{
        //    //    bool defineNewRelated = MetaBraceAssignStatements.IsBraceAssignDeclaredCompatibleWithExpress(defineArray, newArray);
        //    //    if (!defineNewRelated)
        //    //    {
        //    //        Log.AddMetaCoreLog(LID.MetaCoreArrayNotSupportInConvert, m_Token, "", defineArray.ToString(), newArray.ToString());
        //    //        return false;
        //    //    }

        //    //    var dEl = defineArray.GetMetaTypeByIndex(0);
        //    //    var nEl = newArray.GetMetaTypeByIndex(0);
        //    //    if (dEl != null && nEl != null
        //    //        && NumberManager.IsNumberClass(dEl.metaClass)
        //    //        && NumberManager.IsNumberClass(nEl.metaClass)
        //    //        && !TypeManager.CompareMetaType(dEl, nEl))
        //    //    {
        //    //        result = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(defineArray, newArray);
        //    //    }
        //    //    else
        //    //    {
        //    //        result = new MetaType(newArray);
        //    //    }
        //    //}
        //    //else if (newArray != null)
        //    //{
        //    //    result = new MetaType(newArray);
        //    //    return true;
        //    //}
        //    //else if (defineArray != null)
        //    //{
        //    //    result = new MetaType(defineArray);
        //    //    return true;
        //    //}

        //    //if (calcArray != null)
        //    //{
        //    //    if (result == null)
        //    //    {
        //    //        result = new MetaType(calcArray);
        //    //        return true;
        //    //    }

        //    //    bool resultCalcRelated = MetaBraceAssignStatements.IsBraceAssignDeclaredCompatibleWithExpress(result, calcArray);
        //    //    bool calcResultRelated = MetaBraceAssignStatements.IsBraceAssignDeclaredCompatibleWithExpress(calcArray, result);
        //    //    if (!resultCalcRelated && !calcResultRelated)
        //    //    {
        //    //        Log.AddMetaCoreLog(LID.MetaCoreArrayNotSupportInConvert, m_Token, "", result.ToString(), calcArray.ToString());
        //    //        return false;
        //    //    }

        //    //    // ?????????????????????????????????????????????????? define ????????????
        //    //    var retEl = result.GetMetaTypeByIndex(0);
        //    //    var calcEl = calcArray.GetMetaTypeByIndex(0);
        //    //    if (retEl != null && calcEl != null
        //    //        && NumberManager.IsNumberClass(retEl.metaClass)
        //    //        && NumberManager.IsNumberClass(calcEl.metaClass)
        //    //        && !TypeManager.CompareMetaType(retEl, calcEl))
        //    //    {
        //    //        result = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(result, calcArray);
        //    //    }
        //    //    else if (resultCalcRelated)
        //    //    {
        //    //        result = new MetaType(calcArray);
        //    //    }

        //    //    if (result.arrayLength == -1 && calcArray.arrayLength >= 0)
        //    //    {
        //    //        result.SetArrayLength(calcArray.arrayLength);
        //    //    }
        //    //}            
        //}
        //private bool TryValidateInnermostArrayDimensionForStore(
        //    List<int> defineDims,
        //    List<int> newDims,
        //    int index,
        //    ref MetaType numericMergedArrayMeta)
        //{
        //    if (defineDims[index] != -1)
        //    {
        //        if (newDims[index] != -1)
        //        {
        //            if (newDims[index] != defineDims[index])
        //            {
        //                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "???????????");
        //            }
        //        }
        //        else
        //        {
        //            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "???????????????");
        //            return false;
        //        }
        //    }

        //    var cmt1 = m_DefineMetaType.GetMetaTypeByIndex(0);
        //    var cmt2 = m_NewMetaType.GetMetaTypeByIndex(0);
        //    if (TypeManager.CompareMetaType(cmt1, cmt2))
        //    {
        //        return true;
        //    }

        //    if (NumberManager.IsNumberClass(cmt1.metaClass) && NumberManager.IsNumberClass(cmt2.metaClass))
        //    {
        //        numericMergedArrayMeta = NumberManager.BuildArrayMetaTypeCopyingElementFromDefinePreservingLength(
        //            m_DefineMetaType, m_NewMetaType);
        //        return true;
        //    }

        //    if (cmt1.IsArray() && cmt2.IsArray()
        //        && MetaBraceAssignStatements.TryArrayElementAssignableForNewObject(cmt1, cmt2))
        //    {
        //        return true;
        //    }

        //    Log.AddMetaCoreLog(LID.MetaCoreArrayNotSupportInConvert, m_Token, "", cmt1.ToString(), cmt2.ToString());
        //    return false;
        //}

        //private bool TryValidateOuterArrayDimensionForStore(List<int> defineDims, List<int> newDims, int index)
        //{
        //    if (defineDims[index] == -1)
        //    {
        //        if (newDims[index] == -1)
        //        {
        //            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "?????? ??????????????");
        //            return false;
        //        }

        //        return true;
        //    }

        //    if (defineDims[index] != newDims[index])
        //    {
        //        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "???????????????! ??????????new?????????????!");
        //        return false;
        //    }

        //    return true;
        //}
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
            }
            else
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "--");
            }

            this.m_ArrayLengthExpress = m_MetaInputParamList[0];
            m_MetaMemberFunction = null;
        }
        public void CheckDefineVariableMetaTypeAndContentMetaType()
        {
            if (!NumberManager.TryUnifyNumericArrayLiteralMembersToDeclaredArrayType(this, m_ExpressReturnMetaType, m_Token))
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token,
                    "??????????????????????????/???????????: " + m_ExpressReturnMetaType.ToString());
            }
            for (int i = 0; i < this.m_AssignStatementsList.Count; i++)
            {
                m_AssignStatementsList[i].ValidateDefineAgainstDeclaredMetaType();
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();


            if( this.m_ExpressReturnMetaType.isEnum )
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
                }            }

            return sb.ToString();
        }
        public override string ToString()
        {
            return "NewObjectExpressNode: " + ToFormatString();
        }
        /// <summary>
        /// ?? <paramref name="sourceField"/> ??????? <see cref="MetaMemberData.expressNode"/>?????? <see cref="MetaNewObjectExpressNode"/>????????ฆศ???งา???
        /// </summary>
        //public void FillAnonymousDataAssignStatementsFromMemberDict(
        //    MetaMemberData sourceField,
        //    MetaData anonymousMetaData,
        //    MetaBlockStatements mbs,
        //    bool preferSourceMemberExpress = true)
        //{
        //    if (sourceField == null || anonymousMetaData == null)
        //    {
        //        return;
        //    }

        //    m_AssignStatementsList.Clear();

        //    if (sourceField.expressNode is MetaNewObjectExpressNode nestedNew)
        //    {
        //        for (int i = 0; i < nestedNew.assignStatementsList.Count; i++)
        //        {
        //            m_AssignStatementsList.Add(nestedNew.assignStatementsList[i]);
        //        }
        //    }
        //    else if (sourceField.ownerMetaData != null)
        //    {
        //        var built = CreateFromAnonymousMetaData(
        //            anonymousMetaData,
        //            sourceField.ownerMetaData,
        //            ownerMetaBase,
        //            mbs,
        //            m_StoreMetaVariable);
        //        if (built != null)
        //        {
        //            for (int i = 0; i < built.assignStatementsList.Count; i++)
        //            {
        //                m_AssignStatementsList.Add(built.assignStatementsList[i]);
        //            }
        //        }
        //    }

        //    Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
        //    CalcReturnType();
        //}

        //private MetaData CreateAnonymousDataOwnerMetaDataForBraceLiteral()
        //{
        //    string anname = "DynamicData_";
        //    if (m_StoreMetaVariable != null)
        //    {
        //        anname = anname + m_StoreMetaVariable.name + "_";
        //    }
        //    if (m_BraceFileMetaBaseTerm != null)
        //    {
        //        anname = anname + m_BraceFileMetaBaseTerm.token?.path + "_" + m_BraceFileMetaBaseTerm.token?.sourceBeginLine.ToString() + "_" + GetHashCode().ToString();
        //    }

        //    var tempMetaData = new MetaData(anname, false, false, true);
        //    if (m_StoreMetaVariable?.token != null)
        //    {
        //        tempMetaData.AddPingToken(m_StoreMetaVariable.token);
        //    }
        //    tempMetaData.AddPingToken(m_BraceFileMetaBaseTerm?.token);
        //    return tempMetaData;
        //}

        //private void ResolveDynamicAnonymousDataBySharedFlow()
        //{
        //    if (m_NewMetaType == null || !m_NewMetaType.isDynamicData)
        //    {
        //        return;
        //    }
        //    if (m_AssignStatementsList.Count == 0)
        //    {
        //        return;
        //    }

        //    var ownerMetaData = CreateAnonymousDataOwnerMetaDataForBraceLiteral();
        //    int index = 0;
        //    for (int i = 0; i < m_AssignStatementsList.Count; i++)
        //    {
        //        var mas = m_AssignStatementsList[i];
        //        if (mas == null || string.IsNullOrWhiteSpace(mas.defineName))
        //        {
        //            continue;
        //        }

        //        var expr = mas.expressNode;
        //        if (expr != null)
        //        {
        //            expr.CalcReturnType();
        //        }

        //        var exprType = mas.GetRetMetaType() ?? expr?.GetReturnMetaType();
        //        var fieldType = exprType != null ? new MetaType(exprType) : new MetaType(CoreMetaClassManager.objectMetaClass);
        //        bool isDeclaredType = exprType != null && exprType.metaClass != CoreMetaClassManager.objectMetaClass;

        //        var child = MetaMemberData.CreateDeclared(ownerMetaData, mas.defineName, index, fieldType, isDeclaredType);
        //        index++;
        //        child.SetOwnerBlockstatements(m_OwnerMetaBlockStatements);
        //        child.SetExpress(expr);
        //        ownerMetaData.AddMetaMemberData(child);
        //    }

        //    var canonical = MetaData.ResolveCanonicalAnonymousType(
        //        ownerMetaData.GetMetaMemberDataList(),
        //        m_OwnerMetaBase,
        //        m_StoreMetaVariable?.name);
        //    if (canonical == null)
        //    {
        //        return;
        //    }

        //    m_BraceNewMetaData = canonical;
        //    m_NewMetaType.SetMetaData(m_BraceNewMetaData);
        //    m_StoreMetaVariable?.SetMetaDefineType(m_NewMetaType);

        //    var resolvedNewObject = MetaNewObjectExpressNode.CreateFromAnonymousMetaData(
        //        canonical,
        //        ownerMetaData,
        //        m_OwnerMetaBase,
        //        m_OwnerMetaBlockStatements,
        //        m_StoreMetaVariable);
        //    if (resolvedNewObject != null)
        //    {
        //        m_AssignStatementsList.Clear();
        //        m_AssignStatementsList.AddRange(resolvedNewObject.assignStatementsList);
        //    }
        //}
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
        // ??NewObject ? Class c = Class(){ var1 = 1; } ????? 1???????
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
                            Debug.Write("Error ???data varname = {} ?? { cha1 = [] } ???,??????????");
                        }
                        break;
                    }
                default:
                    {
                        Debug.Write("Error ?????????NewObject????!!");
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
        //            Log.AddMetaCoreLog(LID.ShowExtendMessage, "?[]???????????");
        //        }
        //    }
        //    int use_n_numone = 0;
        //    for( int i = depthLength.Count - 1; i >= 0; i--)
        //    {
        //        if(depthLength[i] == -1 )
        //        {
        //            if( use_n_numone == 2 )
        //            {
        //                Log.AddMetaCoreLog(LID.ShowExtendMessage, "?[]??????????[3][-1][-1]??????????[3][-1][2] ????");
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
