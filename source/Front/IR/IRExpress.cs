//****************************************************************************
//  File:      IRExpress.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  express convert ir code!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRExpressManager
    {
        public static IRExpressBase CreateExpress(IRMethod irMethod, MetaExpressNodeBase men )
        {
            IRExpressBase ireb = null;
            if ( men is MetaNewObjectExpressNode mnoe )
            {
                ireb = new IRNewExpress(irMethod, mnoe);
            }
            else
            {
                ireb = new IRExpress(irMethod, men);
            }
            return ireb;
        }
    }
    public abstract class IRExpressBase : IRBase
    {
        public IRExpressBase(IRMethod irMethod) :base(irMethod)
        {
            m_IRMethod = irMethod;
        }
    }
    public class IRExpress : IRExpressBase
    {
        public IRExpress( IRMethod irMethod, MetaConstExpressNode node ) : base( irMethod )
        {
            IRData irdata = new IRData();
            irdata.opCode = IRUtil.GetConstIROpCode(node.eType);
            if (node.eType == EType.String)
            {
                var s = node.value?.ToString() ?? string.Empty;
                irdata.index = IRManager.instance.AddStringIRStack(s);
                irdata.opValue = null;
            }
            else
            {
                irdata.SetOpValue(node.value);
            }
            irdata.SetDebugInfoByToken(node.token);
            AddIRData(irdata);
        }
        public IRExpress( IRMethod irMethod, MetaExpressNodeBase node ) : base( irMethod )
        {
            //m_Node = node;
            CreateIRDataOne(node);
        }
        protected void CreateIRDataOne(MetaExpressNodeBase node)
        {
            switch (node)
            {
                case MetaConstExpressNode mcn:
                    {
                        IRData irdata = new IRData();
                        irdata.opCode = IRUtil.GetConstIROpCode(mcn.eType);
                        if (mcn.eType == EType.String)
                        {
                            var s = mcn.value?.ToString() ?? string.Empty;
                            irdata.index = IRManager.instance.AddStringIRStack(s);
                            irdata.opValue = null;
                        }
                        else
                        {
                            irdata.SetOpValue(mcn.value);
                        }
                        irdata.SetDebugInfoByToken(mcn.token);
                        AddIRData(irdata);
                    }
                    break;
                case MetaUnaryOpExpressNode muoen:
                    {
                        MetaExpressNodeBase valNode = muoen.value;
                        CreateIRDataOne(valNode);
                        var signData = CreateOneSignIRData(muoen.opSign);
                        signData.SetDebugInfoByToken(muoen.token);
                        AddIRData(signData);
                    }
                    break;
                case MetaOpExpressNode moen:
                    {
                        MetaExpressNodeBase leftNode = moen.left;
                        MetaExpressNodeBase rightNode = moen.right;
                        CreateIRDataOne(leftNode);
                        if( moen.leftConvert != null )
                        {
                            IRConvert ircovn = new IRConvert(m_IRMethod, moen.leftConvert.oriType, moen.leftConvert.targetType);
                            AddIRData(ircovn.data);
                        }
                        CreateIRDataOne(rightNode);
                        if (moen.rightConvert != null)
                        {
                            IRConvert ircovn = new IRConvert(m_IRMethod, moen.rightConvert.oriType, moen.rightConvert.targetType);
                            AddIRData(ircovn.data);
                        }
                        var signData = IRUtil.CreateLeftAndRightIRData(moen.opSign, out bool flag );
                        if( !flag )
                        {
                            Log.AddIRLog(LID.MetaCoreAssertShowMessage, moen.token, "not have sign ");
                            return;
                        }
                        signData.SetDebugInfoByToken(moen.token);
                        AddIRData(signData);
                    }
                    break;
                case MetaCallLinkExpressNode mcn:
                    {
                        IRMetaCallLink irmc = new IRMetaCallLink();
                        if ( m_IRMethod == null )
                        {
                            irmc.ParseToIRDataList(null, mcn.metaCallLink.visitNodeList);
                        }
                        else
                        {
                            irmc.ParseToIRDataList(m_IRMethod, mcn.metaCallLink.visitNodeList);
                        }
                        for( int i = 0; i < irmc.irList.Count; i++ )
                        {
                            m_IRDataList.AddRange(irmc.irList[i].IRDataList);
                        }
                    }
                    break;
                case MetaArrayExpressNode maen:
                    {
                        foreach( var v in maen.metaCallArray )
                        {
                            var exp1 = IRExpressManager.CreateExpress(this.m_IRMethod, v);
                            m_IRDataList.AddRange(exp1.IRDataList);
                        }
                    }
                    break;
                case MetaNewObjectExpressNode mnoeNest:
                    {
                        var ireNest = IRExpressManager.CreateExpress(m_IRMethod, mnoeNest);
                        m_IRDataList.AddRange(ireNest.IRDataList);
                    }
                    break;
                case MetaThreeItemExpressNode mtien:
                    {
                        IRExpressBase iexress = IRExpressManager.CreateExpress(this.m_IRMethod, mtien.conditionExpress );
                        m_IRDataList.AddRange(iexress.IRDataList);

                        IRData elseirdata = new IRData();
                        IRData endirdata = new IRData();

                        IRBranch ifbranch = new IRBranch(m_IRMethod, EIROpCode.BrFalse, elseirdata );
                        m_IRDataList.AddRange(ifbranch.IRDataList);

                        IRExpressBase ireturn1Exress = IRExpressManager.CreateExpress(this.m_IRMethod, mtien.return1Express );
                        m_IRDataList.AddRange(ireturn1Exress.IRDataList);

                        IRBranch br = new IRBranch(m_IRMethod, EIROpCode.Br, endirdata);
                        m_IRDataList.AddRange(br.IRDataList);

                        m_IRDataList.Add(elseirdata);

                        IRExpressBase ireturn2Exress = IRExpressManager.CreateExpress(this.m_IRMethod, mtien.return2Express);
                        m_IRDataList.AddRange(ireturn2Exress.IRDataList);

                        m_IRDataList.Add(endirdata);

                    }
                    break;
                case MetaAsIsExpressNode maien:
                    {
                        IRMetaCallLink irmcl = new IRMetaCallLink();
                        irmcl.ParseToIRDataList(m_IRMethod, maien.currentVariableLink.visitNodeList);
                        for (int i = 0; i < irmcl.irList.Count; i++)
                        {
                            m_IRDataList.AddRange(irmcl.irList[i].IRDataList);
                        }
                        var owirmc = IRManager.GetIRMetaClassByMetaOwner(maien.ownerMetaBase)
                            ?? IRManager.instance.GetIRMetaClassByName("Core.Object");

                        // if 'isnot' then invert the boolean result of is-check after evaluation
                        bool isNot = maien.isIsNot;
                        if ( maien.isAs )
                        {                            
                            IRData irdata = new IRData();
                            irdata.opCode = EIROpCode.CastClass;
                            irdata.SetOpValue(IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(maien.convertTargetMetaType, owirmc));
                            var asTok = maien.fileMetaKeyAsIsSyntax?.asOrIsToken;
                            var tstr = maien.convertTargetMetaType?.ToFormatString() ?? "";
                            irdata.SetDebugInfoByToken(asTok, string.IsNullOrEmpty(tstr) ? "CastClass as" : $"CastClass as {tstr}");

                            m_IRDataList.Add(irdata);
                        }
                        else
                        {
                            IRData irdata = new IRData();
                            irdata.opCode = EIROpCode.CastClass;
                            irdata.SetOpValue(IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(maien.convertTargetMetaType, owirmc));
                            var isTok = maien.fileMetaKeyAsIsSyntax?.asOrIsToken;
                            var tstr2 = maien.convertTargetMetaType?.ToFormatString() ?? "";
                            irdata.SetDebugInfoByToken(isTok, string.IsNullOrEmpty(tstr2) ? "CastClass is" : $"CastClass is {tstr2}");
                            m_IRDataList.Add(irdata);

                            var owirmc2 = IRManager.GetIRMetaClassByMetaVariable(maien.convertTargetMetaVariable);
                            var irmt2 = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(maien.convertTargetMetaVariable.defineMetaType, owirmc);
                            IRStoreVariable irstore = IRStoreVariable.CreateIRStoreVariable(irmt2, owirmc2, m_IRMethod, maien.convertTargetMetaVariable);
                            AddIRRangeData(irstore.IRDataList);

                            IRLoadVariable irload3 = IRLoadVariable.CreateLoadVariable(irmt2, owirmc2, m_IRMethod, maien.convertTargetMetaVariable);
                            AddIRRangeData(irload3.IRDataList);

                            IRData irdata4 = new IRData();
                            irdata4.opCode = IRUtil.GetConstIROpCode( EType.Null );
                            irdata4.SetOpValue("null");
                            irdata4.SetDebugInfoByToken(maien.fileMetaKeyAsIsSyntax?.asOrIsToken, "is: null literal");
                            AddIRData(irdata4);

                            IRData irdata5 = new IRData();
                            irdata5.opCode = EIROpCode.Cne;
                            irdata5.SetDebugInfoByToken(maien.fileMetaKeyAsIsSyntax?.asOrIsToken, "is: Cne (compare to null)");
                            AddIRData(irdata5);
                            if (isNot)
                            {
                                // invert result: Cne gives boolean; to invert, use Not
                                IRData notData = new IRData();
                                notData.opCode = EIROpCode.Not;
                                notData.SetDebugInfoByToken(maien.fileMetaKeyAsIsSyntax?.asOrIsToken, "isNot: Not");
                                AddIRData(notData);
                            }
                        }
                    }
                    break;
                default:
                    {
                        Log.AddIRLog(LID.MetaCoreAssertShowMessage, node.token, "notfound express");
                    }
                    break;
            }
        }
        public IRData CreateOneSignIRData(ESingleOpSign opSign)
        {
            IRData data = new IRData();
            switch (opSign)
            {
                case ESingleOpSign.Neg:
                    {
                        data.opCode = EIROpCode.Neg;
                    }
                    break;
                case ESingleOpSign.Not:
                    {
                        data.opCode = EIROpCode.Not;
                    }
                    break;
            }
            return data;
        }

        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#LoadConst#" );
            sb.Append(base.ToIRString());
            return sb.ToString();
        }
    }

    public class IRNewExpress : IRExpressBase
    {
        public IRNewExpress(IRMethod irMethod, MetaConstExpressNode mnoen ) : base(irMethod)
        {
            IRData irdata = new IRData();
            irdata.opCode = IRUtil.GetConstIROpCode(mnoen.eType);
            if (mnoen.eType == EType.String)
            {
                var s = mnoen.value?.ToString() ?? string.Empty;
                irdata.index = IRManager.instance.AddStringIRStack(s);
                irdata.opValue = null;
            }
            else
            {
                irdata.opValue = mnoen.value;
            }
            //irdata.SetDebugInfoByToken( mcn.GetToken() );
            AddIRData(irdata);

            //var owirmc = IRManager.instance.GetIRMetaClassById(mnoen.ownerMetaClass.GetHashCode());
            //IRMetaClass newObjIRMC = IRManager.instance.GetIRMetaClassById(mnoen.metaType.GetTemplateMetaClass().GetHashCode());
            //var irmc = IRManager.instance.GetIRMetaClassById(mnoen.metaType.GetTemplateMetaClass().GetHashCode());
            //var newObjectIRMT = new IRMetaType(newObjIRMC);
            //IRNew irNew = new IRNew(irMethod, newObjIRMC);
            //AddIRRangeData(irNew.IRDataList);
        }
        public IRNewExpress(IRMethod irMethod, MetaNewObjectExpressNode mnoen ) : base(irMethod)
        {
            IRMetaClass owirmc = IRManager.GetIRMetaClassByMetaOwner(mnoen.ownerMetaBase);
            if( owirmc == null )
            {
                Log.AddIRLog(LID.MetaCoreAssertShowMessage, mnoen.token, "notfound owner mc !");
                return;
            }

            IRMetaType newObjectIRMT = null;
            IRMetaClass irmc = null;

            var returnMetaType = mnoen.GetReturnMetaType();
            if ( returnMetaType.IsArray() )
            {
                IRExpressBase ire = IRExpressManager.CreateExpress(irMethod, mnoen.arrayLengthExpress);
                m_IRDataList.AddRange(ire.IRDataList);

                var irMetaType = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mnoen.expressReturnMetaType, owirmc);
                IRNew irNew = new IRNew(irMethod, irMetaType, 0 );
                AddIRRangeData(irNew.IRDataList);

                if (mnoen.metaMemberFunction != null)
                {
                    IRDup irdup = new IRDup(irMethod);
                    m_IRDataList.AddRange(irdup.IRDataList);

                    var paramCount = mnoen.metaInputParamList.Count;
                    for (int j = 0; j < paramCount; j++)
                    {
                        IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, mnoen.metaInputParamList[j]);
                        AddIRRangeData(irexpress.IRDataList);
                    }

                    int callMethodIndex = -1;
                    string fname = "";

                    fname = mnoen.metaMemberFunction.virtualFunctionName;
                    irmc = IRManager.GetIRMetaClassByMetaType(mnoen.expressReturnMetaType);
                    if (irmc == null)
                    {
                        MetaClass mc2 = mnoen.expressReturnMetaType?.GetTemplateMetaClass();
                        if (mc2 == null)
                            mc2 = mnoen.metaMemberFunction.ownerMetaClass;
                        if (mc2 == null)
                            mc2 = mnoen.metaMemberFunction.sourceMetaMemberFunction?.ownerMetaClass;
                        irmc = mc2 != null ? IRManager.instance.GetIRMetaClassById(mc2.GetHashCode()) : null;
                    }

                    var runtimeMethod = irmc?.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                    if (callMethodIndex == -1 && mnoen.metaMemberFunction.sourceMetaMemberFunction != null)
                    {
                        var sourceMc = mnoen.metaMemberFunction.sourceMetaMemberFunction.ownerMetaClass;
                        var sourceIrmc = sourceMc != null ? IRManager.instance.GetIRMetaClassById(sourceMc.GetHashCode()) : null;
                        if (sourceIrmc != null)
                        {
                            var sourceMethod = sourceIrmc.GetIRNonStaticMethodIndexByMethod(fname, out var sourceIndex);
                            if (sourceIndex >= 0)
                            {
                                runtimeMethod = sourceMethod;
                                callMethodIndex = sourceIndex;
                            }
                        }
                    }
                    if (callMethodIndex == -1)
                    {
                        Log.AddIRLog(LID.IRNotFoundArrayInitFunction, mnoen.token, "" );
                    }
                    List<IRMetaType> functionMtList = new List<IRMetaType>();
                    var irmethodcall = new IRMethodCall(newObjectIRMT, functionMtList, runtimeMethod, paramCount);
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallVirt;
                    datacall.index = callMethodIndex;
                    datacall.opValue = irmethodcall;
                    datacall.SetDebugInfoByToken(mnoen.token);
                    AddIRData(datacall);
                }

                if (mnoen.assignStatementsList?.Count > 0)
                {
                    for (int y = 0; y < mnoen.assignStatementsList.Count; y++)
                    {
                        var asl = mnoen.assignStatementsList[y];

                        IRDup irdup = new IRDup(irMethod);
                        AddIRRangeData(irdup.IRDataList);

                        IRExpressBase irexp = IRExpressManager.CreateExpress(irMethod, asl.expressNode);
                        AddIRRangeData(irexp.IRDataList);

                        IRData irdatastore = new IRData();
                        irdatastore.index = y;
                        // object-initializer assignment path pushes: [..., array, value]
                        // mark StoreArrayIndex to read store target at top-2, value at top-1.
                        irdatastore.SetOpValue((byte)EStoreArrayIndexFlag.StoreTopMinus2_ValueTopMinus1);
                        irdatastore.opCode = EIROpCode.StoreArrayIndex;
                        m_IRDataList.Add(irdatastore);
                    }
                }
            }
            else
            {
                if (mnoen.expressReturnMetaType.eMetaTypeType == EMetaTypeType.MetaGenClass)
                {
                    if (mnoen.ownerMetaClass is MetaGenTemplateClass mgtc)
                    {
                        owirmc = IRManager.instance.GetIRMetaClassById(mgtc.metaTemplateClass.GetHashCode());
                    }
                    newObjectIRMT = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mnoen.expressReturnMetaType, owirmc);
                    irmc = IRManager.GetIRMetaClassByMetaType(mnoen.expressReturnMetaType);
                    IRNew irNew = new IRNew(irMethod, newObjectIRMT);
                    AddIRRangeData(irNew.IRDataList);

                }
                else if (mnoen.expressReturnMetaType.eMetaTypeType == EMetaTypeType.MetaClass
                    || mnoen.expressReturnMetaType.eMetaTypeType == EMetaTypeType.MetaData
                    || mnoen.expressReturnMetaType.eMetaTypeType == EMetaTypeType.MetaEnum)
                {
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mnoen.ownerMetaBase) ?? owirmc;
                    irmc = IRManager.GetIRMetaClassByMetaType(mnoen.expressReturnMetaType);
                    newObjectIRMT = new IRMetaType(irmc);
                    IRNew irNew = new IRNew(irMethod, irmc);
                    AddIRRangeData(irNew.IRDataList);
                }
                else
                {
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mnoen.ownerMetaBase) ?? owirmc;
                    irmc = IRManager.GetIRMetaClassByMetaType(mnoen.expressReturnMetaType);
                    newObjectIRMT = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mnoen.expressReturnMetaType, owirmc);

                    IRNew irNew = new IRNew(irMethod, newObjectIRMT);
                    AddIRRangeData(irNew.IRDataList);
                }

                //if (mnoen.needInitMemberVariable)
                //{
                //    //if (irmc.localIRMetaVariableList.Count > 0)
                //    //{
                //    //    for (int x = 0; x < irmc.localIRMetaVariableList.Count; x++)
                //    //    {
                //    //        var lirmv = irmc.localIRMetaVariableList[x];
                            
                //    //        IRExpressBase irexp = IRExpressManager.CreateExpress(irMethod, lirmv.express);
                //    //        AddIRRangeData(irexp.IRDataList);

                //    //        IRData irdata = new IRData();
                //    //        irdata.index = lirmv.index;
                //    //        irdata.opCode = EIROpCode.StoreNotStaticField1;
                //    //        irdata.SetDebugInfoByToken(lirmv.express.token);
                //    //        m_IRDataList.Add(irdata);
                //    //    }
                //    //}
                //}
                if (mnoen.metaMemberFunction != null)
                {
                    IRDup irdup = new IRDup(irMethod);
                    m_IRDataList.AddRange(irdup.IRDataList);

                    var paramCount = mnoen.metaInputParamList.Count;
                    for (int j = 0; j < paramCount; j++)
                    {
                        IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, mnoen.metaInputParamList[j]);
                        AddIRRangeData(irexpress.IRDataList);
                    }

                    int callMethodIndex = -1;
                    string fname = "";

                    fname = mnoen.metaMemberFunction.virtualFunctionName;
                    irmc = IRManager.GetIRMetaClassByMetaType(mnoen.expressReturnMetaType);
                    if (irmc == null)
                    {
                        MetaClass mc2 = mnoen.expressReturnMetaType?.GetTemplateMetaClass();
                        if (mc2 == null)
                            mc2 = mnoen.metaMemberFunction.ownerMetaClass;
                        if (mc2 == null)
                            mc2 = mnoen.metaMemberFunction.sourceMetaMemberFunction?.ownerMetaClass;
                        irmc = mc2 != null ? IRManager.instance.GetIRMetaClassById(mc2.GetHashCode()) : null;
                    }

                    var runtimeMethod = irmc?.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                    if (callMethodIndex == -1 && mnoen.metaMemberFunction.sourceMetaMemberFunction != null)
                    {
                        var sourceMc = mnoen.metaMemberFunction.sourceMetaMemberFunction.ownerMetaClass;
                        var sourceIrmc = sourceMc != null ? IRManager.instance.GetIRMetaClassById(sourceMc.GetHashCode()) : null;
                        if (sourceIrmc != null)
                        {
                            var sourceMethod = sourceIrmc.GetIRNonStaticMethodIndexByMethod(fname, out var sourceIndex);
                            if (sourceIndex >= 0)
                            {
                                runtimeMethod = sourceMethod;
                                callMethodIndex = sourceIndex;
                            }
                        }
                    }
                    if (callMethodIndex == -1)
                    {
                        Log.AddIRLog(LID.IRNotFoundArrayInitFunction, mnoen.token, "");
                    }
                    List<IRMetaType> functionMtList = new List<IRMetaType>();
                    var irmethodcall = new IRMethodCall(newObjectIRMT, functionMtList, runtimeMethod, paramCount);
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallVirt;
                    datacall.index = callMethodIndex;
                    datacall.opValue = irmethodcall;
                    datacall.SetDebugInfoByToken(mnoen.token);
                    AddIRData(datacall);
                }
                for (int y = 0; y < mnoen.assignStatementsList.Count; y++)
                {
                    var asl = mnoen.assignStatementsList[y];

                    IRDup irdup = new IRDup(irMethod);
                    AddIRRangeData(irdup.IRDataList);

                    IRExpressBase irexp = IRExpressManager.CreateExpress(irMethod, asl.expressNode);
                    AddIRRangeData(irexp.IRDataList);

                    var storeField = new IRStoreVariable(newObjectIRMT, irMethod, asl.id, IRMetaVariableFrom.Member);
                    if (storeField != null)
                    {
                        AddIRRangeData(storeField.IRDataList);
                    }
                }

            }
        }
    }
}
