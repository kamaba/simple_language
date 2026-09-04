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
using System.Collections.Generic;
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
                case MetaEmptyRetExpressNode meren:
                    {
                        // left ?? right
                        // If left is null or 0, return right; otherwise return left.
                        // 1. evaluate left
                        IRExpressBase leftExpress = IRExpressManager.CreateExpress(this.m_IRMethod, meren.return1Express);
                        m_IRDataList.AddRange(leftExpress.IRDataList);

                        // 2. dup left value on stack
                        IRData dupData = new IRData();
                        dupData.opCode = EIROpCode.Dup;
                        dupData.SetDebugInfoByToken(meren.token, "?? dup left");
                        m_IRDataList.Add(dupData);

                        // 3. load null for comparison
                        IRData nullData = new IRData();
                        nullData.opCode = EIROpCode.LoadConstNull;
                        nullData.SetDebugInfoByToken(meren.token, "?? load null");
                        m_IRDataList.Add(nullData);

                        // 4. compare != null
                        IRData cneData = new IRData();
                        cneData.opCode = EIROpCode.Cne;
                        cneData.SetDebugInfoByToken(meren.token, "?? Cne");
                        m_IRDataList.Add(cneData);

                        // 5. if null, jump to else (left is dup'd on stack)
                        IRData elseirdata = new IRData();
                        IRData endirdata = new IRData();

                        IRBranch ifbranch = new IRBranch(m_IRMethod, EIROpCode.BrFalse, elseirdata);
                        m_IRDataList.AddRange(ifbranch.IRDataList);

                        // 6. left is not null: keep left on stack as result, jump to end
                        IRBranch br = new IRBranch(m_IRMethod, EIROpCode.Br, endirdata);
                        m_IRDataList.AddRange(br.IRDataList);

                        // 7. left is null: pop the dup'd left, evaluate right
                        m_IRDataList.Add(elseirdata);

                        IRData popData2 = new IRData();
                        popData2.opCode = EIROpCode.Pop;
                        popData2.SetDebugInfoByToken(meren.token, "?? pop left, eval right");
                        m_IRDataList.Add(popData2);

                        IRExpressBase rightExpress = IRExpressManager.CreateExpress(this.m_IRMethod, meren.return2Express);
                        m_IRDataList.AddRange(rightExpress.IRDataList);

                        m_IRDataList.Add(endirdata);
                    }
                    break;
                case MetaTryExpressNode mten:
                    {
                        if (mten.tryMode == ETryMode.TryQuestion)
                        {
                            // try? expr: if exception, result is null
                            // IR: BeginTry(catch=catchNop) <expr> LeaveTry->endNop catchNop: Pop LoadConstNull endNop:
                            IRData catchNop = new IRData();
                            catchNop.opCode = EIROpCode.Nop;
                            IRData endNop = new IRData();
                            endNop.opCode = EIROpCode.Nop;

                            // BeginTry
                            IRData beginTryData = new IRData();
                            beginTryData.opCode = EIROpCode.BeginTry;
                            TryScopeData tsd = new TryScopeData();
                            tsd.catchTarget = catchNop;
                            tsd.finallyTarget = null;
                            beginTryData.SetOpValue(tsd);
                            beginTryData.SetDebugInfoByToken(mten.token, "try? BeginTry");
                            m_IRDataList.Add(beginTryData);

                            // Inner expression - set tryCatch context so calls are marked
                            bool savedTryCatch = m_IRMethod.isInTryCatch;
                            m_IRMethod.isInTryCatch = true;
                            IRExpressBase innerExpress = IRExpressManager.CreateExpress(this.m_IRMethod, mten.innerExpress);
                            m_IRMethod.isInTryCatch = savedTryCatch;
                            m_IRDataList.AddRange(innerExpress.IRDataList);

                            // LeaveTry -> end
                            IRData leaveTryData = new IRData();
                            leaveTryData.opCode = EIROpCode.LeaveTry;
                            leaveTryData.SetOpValue(endNop);
                            leaveTryData.SetDebugInfoByToken(mten.token, "try? LeaveTry");
                            m_IRDataList.Add(leaveTryData);

                            // Catch handler: pop exception, push null
                            m_IRDataList.Add(catchNop);

                            IRData popData = new IRData();
                            popData.opCode = EIROpCode.Pop;
                            popData.SetDebugInfoByToken(mten.token, "try? pop exception");
                            m_IRDataList.Add(popData);

                            IRData nullData = new IRData();
                            nullData.opCode = EIROpCode.LoadConstNull;
                            nullData.SetDebugInfoByToken(mten.token, "try? load null");
                            m_IRDataList.Add(nullData);

                            // End label
                            m_IRDataList.Add(endNop);
                        }
                        else if (mten.tryMode == ETryMode.Try)
                        {
                            // try expr: evaluate, exception caught by surrounding label{}catch{}
                            // Set tryCatch context so IRCallFunction marks call instructions.
                            bool savedTryCatch = m_IRMethod.isInTryCatch;
                            m_IRMethod.isInTryCatch = true;
                            IRExpressBase innerExpress = IRExpressManager.CreateExpress(this.m_IRMethod, mten.innerExpress);
                            m_IRMethod.isInTryCatch = savedTryCatch;
                            m_IRDataList.AddRange(innerExpress.IRDataList);
                        }
                        else if (mten.tryMode == ETryMode.TryExclamation)
                        {
                            // try! expr: evaluate, exception caught by surrounding label{}catch{}
                            // Same tryCatch marking as try - both are caught by enclosing catch
                            bool savedTryCatch = m_IRMethod.isInTryCatch;
                            m_IRMethod.isInTryCatch = true;
                            IRExpressBase innerExpress = IRExpressManager.CreateExpress(this.m_IRMethod, mten.innerExpress);
                            m_IRMethod.isInTryCatch = savedTryCatch;
                            m_IRDataList.AddRange(innerExpress.IRDataList);
                        }
                    }
                    break;
                case MetaCheckedExpressNode mcen:
                    {
                        // checked(expr): emit BeginChecked, evaluate expr, EndChecked
                        // On integer overflow, VM throws OverflowException (caught by surrounding label{}catch{})
                        IRData beginChecked = new IRData();
                        beginChecked.opCode = EIROpCode.BeginChecked;
                        beginChecked.SetDebugInfoByToken(mcen.token, "checked BeginChecked");
                        m_IRDataList.Add(beginChecked);

                        IRExpressBase innerExpress = IRExpressManager.CreateExpress(this.m_IRMethod, mcen.innerExpress);
                        m_IRDataList.AddRange(innerExpress.IRDataList);

                        IRData endChecked = new IRData();
                        endChecked.opCode = EIROpCode.EndChecked;
                        endChecked.SetDebugInfoByToken(mcen.token, "checked EndChecked");
                        m_IRDataList.Add(endChecked);
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
                        irmc = mc2 != null ? IRManager.instance.GetIRMetaClassById(mc2.classId) : null;
                    }

                    var runtimeMethod = irmc?.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                    if (callMethodIndex == -1 && mnoen.metaMemberFunction.sourceMetaMemberFunction != null)
                    {
                        var sourceMc = mnoen.metaMemberFunction.sourceMetaMemberFunction.ownerMetaClass;
                        var sourceIrmc = sourceMc != null ? IRManager.instance.GetIRMetaClassById(sourceMc.classId) : null;
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

                        IRExpressBase irexp = IRExpressManager.CreateExpress(irMethod, asl.valueExpressNode);
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
                        owirmc = IRManager.instance.GetIRMetaClassById(mgtc.metaTemplateClass.classId);
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
                        irmc = mc2 != null ? IRManager.instance.GetIRMetaClassById(mc2.classId) : null;
                    }

                    var runtimeMethod = irmc?.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                    if (callMethodIndex == -1 && mnoen.metaMemberFunction.sourceMetaMemberFunction != null)
                    {
                        var sourceMc = mnoen.metaMemberFunction.sourceMetaMemberFunction.ownerMetaClass;
                        var sourceIrmc = sourceMc != null ? IRManager.instance.GetIRMetaClassById(sourceMc.classId) : null;
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
                // List/IList/IMap 初始化: { val1, val2, ... } 通过调用 add 方法
                // 判断依据：新创建对象的类型是否有 add 方法
                int addMethodIndex = -1;
                IRMethod addMethod = irmc?.GetIRNonStaticMethodIndexByName("add", out addMethodIndex);
                if (addMethodIndex == -1 && irmc != null)
                {
                    var ownerMc = irmc.OwnerMetaClass;
                    if (ownerMc != null)
                    {
                        var baseMc = ownerMc.extendClass;
                        while (baseMc != null && addMethodIndex == -1)
                        {
                            var baseIrmc = IRManager.instance.GetIRMetaClassById(baseMc.classId);
                            if (baseIrmc != null)
                            {
                                addMethod = baseIrmc.GetIRNonStaticMethodIndexByName("add", out addMethodIndex);
                                if (addMethodIndex >= 0)
                                {
                                    irmc = baseIrmc;
                                    break;
                                }
                            }
                            baseMc = baseMc.extendClass;
                        }
                    }
                }
                if (addMethodIndex != -1 && mnoen.assignStatementsList?.Count > 0)
                {
                    for (int y = 0; y < mnoen.assignStatementsList.Count; y++)
                    {
                        var asl = mnoen.assignStatementsList[y];

                        // Dup: 复制对象引用到栈顶
                        IRDup irdup = new IRDup(irMethod);
                        AddIRRangeData(irdup.IRDataList);

                        int count = 1;
                        if( asl.keyExpressNode != null )
                        {
                            // 生成 key 表达式 IR
                            IRExpressBase keyIrexp = IRExpressManager.CreateExpress(irMethod, asl.keyExpressNode);

                            if(keyIrexp.IRDataList.Count == 0 )
                            {
                                Log.AddIRLog(LID.MetaCoreAssertShowMessage, asl.keyExpressNode?.token, "notfound owner mc !");
                                return;
                            }
                            AddIRRangeData(keyIrexp.IRDataList);
                            count++;
                        }

                        // 生成值表达式 IR
                        IRExpressBase irexp = IRExpressManager.CreateExpress(irMethod, asl.valueExpressNode);
                        AddIRRangeData(irexp.IRDataList);
                        if (irexp.IRDataList.Count == 0)
                        {
                            Log.AddIRLog(LID.MetaCoreAssertShowMessage, asl.valueExpressNode?.token, "notfound owner mc !");
                            return;
                        }

                        // 调用 add 方法 (CallVirt)
                        IRData calldata = new IRData();
                        calldata.opCode = EIROpCode.CallVirt;
                        calldata.index = addMethodIndex;
                        var paramTypes = new List<IRMetaType>();
                        var irmc_add = new IRMethodCall(newObjectIRMT, paramTypes, addMethod, count);
                        calldata.opValue = irmc_add;
                        calldata.SetDebugInfoByToken(asl.valueExpressNode.token);
                        AddIRData(calldata);

                        // Pop: 丢弃 add 方法的非 void 返回值（如 bool），保持栈上只有对象引用
                        bool isNonVoidReturn = false;
                        if (addMethod.methodReturnVariableList != null && addMethod.methodReturnVariableList.Count > 0)
                        {
                            var retIrMt = addMethod.methodReturnVariableList[0].irMetaType;
                            if (retIrMt != null && retIrMt.irMetaClass != null)
                            {
                                var retOwnerMc = retIrMt.irMetaClass;
                                isNonVoidReturn = retOwnerMc.irName != "Core.Void";
                            }
                        }
                        if (isNonVoidReturn)
                        {
                            IRData popData = new IRData();
                            popData.opCode = EIROpCode.Pop;
                            popData.SetDebugInfoByToken(asl.valueExpressNode.token);
                            AddIRData(popData);
                        }
                    }
                }
                else if (irmc.metaClassKind == IRMetaClassKind.Data )
                {
                    for (int x = 0; x < irmc.localIRMetaVariableList.Count; x++)
                    {
                        var lirmv = irmc.localIRMetaVariableList[x];

                        MetaExpressNodeBase menb = lirmv.express;

                        for (int y = 0; y < mnoen.assignStatementsList.Count; y++)
                        {
                            var asl = mnoen.assignStatementsList[y];

                            // asl.id 在 data 分支捕获的是成员声明序号，而 lirmv.id 是
                            // MetaMemberData.GetHashCode()，两者值域不同（历史回归导致
                            // 匹配永不命中、花括号覆盖值被静默丢弃）；这里统一按目标
                            // 成员哈希匹配，跨模块重建对象场景回退到成员名比较。
                            var targetMv = asl.targetMetaVariable;
                            bool matched = targetMv != null && targetMv.GetHashCode() == lirmv.id;
                            if (!matched && !string.IsNullOrEmpty(asl.defineName)
                                && asl.defineName == lirmv.name)
                            {
                                matched = true;
                            }
                            if (matched)
                            {
                                menb = asl.valueExpressNode;
                                mnoen.assignStatementsList.Remove(asl);
                                break;
                            }
                        }

                        IRExpressBase irexp = IRExpressManager.CreateExpress(irMethod, menb );
                        AddIRRangeData(irexp.IRDataList);

                        IRData irdata = new IRData();
                        irdata.index = lirmv.index;
                        irdata.opCode = EIROpCode.StoreNotStaticField1;
                        irdata.SetDebugInfoByToken(lirmv.express.token);
                        m_IRDataList.Add(irdata);
                    }
                }
                else
                {
                    for (int y = 0; y < mnoen.assignStatementsList.Count; y++)
                    {
                        var asl = mnoen.assignStatementsList[y];

                        // bind data 展开的类：brace 赋值目标是 set 访问器函数，
                        // 生成 [Dup obj] [value] [CallVirt set_x] (+Pop) 而非 StoreField。
                        if (asl.assignTargetType == MetaBraceAssignStatements.EAssignTargetType.SetMethodCall
                            && asl.setMetaMemberFunction != null)
                        {
                            IRDup irdup = new IRDup(irMethod);
                            AddIRRangeData(irdup.IRDataList);

                            IRExpressBase irexp = IRExpressManager.CreateExpress(irMethod, asl.valueExpressNode);
                            AddIRRangeData(irexp.IRDataList);

                            var setFunc = asl.setMetaMemberFunction;
                            string setVName = setFunc.virtualFunctionName;
                            var callIrmc = newObjectIRMT?.irMetaClass ?? irmc;
                            int callMethodIndex = -1;
                            var runtimeMethod = callIrmc?.GetIRNonStaticMethodIndexByMethod(setVName, out callMethodIndex);
                            if (callMethodIndex == -1 && setFunc.sourceMetaMemberFunction != null)
                            {
                                var sourceMc = setFunc.sourceMetaMemberFunction.ownerMetaClass;
                                var sourceIrmc = sourceMc != null ? IRManager.instance.GetIRMetaClassById(sourceMc.classId) : null;
                                if (sourceIrmc != null)
                                {
                                    var sourceMethod = sourceIrmc.GetIRNonStaticMethodIndexByMethod(setVName, out var sourceIndex);
                                    if (sourceIndex >= 0)
                                    {
                                        runtimeMethod = sourceMethod;
                                        callMethodIndex = sourceIndex;
                                    }
                                }
                            }
                            if (callMethodIndex == -1)
                            {
                                Log.AddIRLog(LID.ShowExtendMessage, asl.valueExpressNode?.token,
                                    "set accessor method not found in ir class: " + asl.defineName);
                            }

                            List<IRMetaType> functionMtList = new List<IRMetaType>();
                            var irmethodcall = new IRMethodCall(newObjectIRMT, functionMtList, runtimeMethod, 1);
                            IRData datacall = new IRData();
                            datacall.opCode = EIROpCode.CallVirt;
                            datacall.index = callMethodIndex;
                            datacall.opValue = irmethodcall;
                            datacall.SetDebugInfoByToken(asl.valueExpressNode?.token);
                            AddIRData(datacall);

                            // set 访问器返回 void 通常无需 Pop；防御性丢弃非 void 返回值
                            bool isNonVoidReturn = false;
                            if (runtimeMethod != null && runtimeMethod.methodReturnVariableList != null
                                && runtimeMethod.methodReturnVariableList.Count > 0)
                            {
                                var retIrMt = runtimeMethod.methodReturnVariableList[0].irMetaType;
                                if (retIrMt != null && retIrMt.irMetaClass != null)
                                {
                                    isNonVoidReturn = retIrMt.irMetaClass.irName != "Core.Void";
                                }
                            }
                            if (isNonVoidReturn)
                            {
                                IRData popData = new IRData();
                                popData.opCode = EIROpCode.Pop;
                                popData.SetDebugInfoByToken(asl.valueExpressNode?.token);
                                AddIRData(popData);
                            }
                            continue;
                        }

                        IRDup irdup2 = new IRDup(irMethod);
                        AddIRRangeData(irdup2.IRDataList);

                        IRExpressBase irexp2 = IRExpressManager.CreateExpress(irMethod, asl.valueExpressNode);
                        AddIRRangeData(irexp2.IRDataList);

                        // asl.id 捕获的是成员在声明类内的局部索引（继承场景下与扁平化索引不一致），
                        // 需按新对象类型的 IRMetaClass 重新解析扁平化字段索引（与普通赋值路径一致）。
                        int storeIndex = asl.id;
                        var targetMv = asl.targetMetaVariable;
                        var targetIrmc = newObjectIRMT?.irMetaClass ?? irmc;
                        if (targetMv != null && targetIrmc != null)
                        {
                            int resolved = targetIrmc.GetMetaMemberVariableIndexByHashCode(targetMv.GetHashCode());
                            if (resolved < 0 && targetMv.sourceMetaVariable != null)
                            {
                                resolved = targetIrmc.GetMetaMemberVariableIndexByHashCode(targetMv.sourceMetaVariable.GetHashCode());
                            }
                            if (resolved < 0 && !string.IsNullOrEmpty(asl.defineName))
                            {
                                resolved = targetIrmc.GetMetaMemberVariableIndexByName(asl.defineName);
                            }
                            if (resolved >= 0)
                            {
                                storeIndex = resolved;
                            }
                        }

                        var storeField = new IRStoreVariable(newObjectIRMT, irMethod, storeIndex, IRMetaVariableFrom.Member);
                        if (storeField != null)
                        {
                            AddIRRangeData(storeField.IRDataList);
                        }
                    }
                }

            }
        }
    }
}
