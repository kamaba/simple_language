//****************************************************************************
//  File:      IRExpress.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description:  express convert ir code!
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Core.IR;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRExpress : IRBase
    {
        private IRManager m_IRManager = null;
        public IRExpress( IRMethod irMethod, MetaExpressNode node ) : base( irMethod )
        {
            //m_Node = node;
            CreateIRDataOne(node);
        }
        public IRExpress( IRManager _irManager, MetaExpressNode node ):base()
        {
            m_IRManager = _irManager;
            //m_Node = node;
            CreateIRDataOne(node);
        }
        public void CreateIRDataOne(MetaExpressNode node)
        {
            switch (node)
            {
                case MetaConstExpressNode mcn:
                    {
                        IRData irdata = new IRData();
                        irdata.opCode = IRManager.GetConstIROpCode(mcn.eType);
                        irdata.opValue = mcn.value;
                        //irdata.SetDebugInfoByToken( mcn.GetToken() );
                        AddIRData(irdata);
                    }
                    break;
                case MetaUnaryOpExpressNode muoen:
                    {
                        MetaExpressNode valNode = muoen.value;
                        CreateIRDataOne(valNode);
                        var signData = CreateOneSignIRData(muoen.opSign);
                        AddIRData(signData);
                    }
                    break;
                case MetaOpExpressNode moen:
                    {
                        MetaExpressNode leftNode = moen.left;
                        MetaExpressNode rightNode = moen.right;
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
                        var signData = IRUtil.CreateLeftAndRightIRData(moen.opSign);
                        //signData.SetDebugInfoByToken( moen.GetToken() );
                        AddIRData(signData);
                    }
                    break;
                case MetaCallLinkExpressNode mcn:
                    {
                        IRMetaCallLink irmc = new IRMetaCallLink();
                        if ( m_IRManager != null )
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
                case MetaAsIsExpressNode maien:
                    {
                        IRMetaCallLink irmcl = new IRMetaCallLink();
                        irmcl.ParseToIRDataList(m_IRMethod, maien.currentVariableLink.visitNodeList);
                        for (int i = 0; i < irmcl.irList.Count; i++)
                        {
                            m_IRDataList.AddRange(irmcl.irList[i].IRDataList);
                        }
                        var ownerMetaClass = maien.ownerMetaClass;
                        var owirmc = IRManager.instance.GetIRMetaClassById(ownerMetaClass.GetHashCode());

                        if ( maien.isAs )
                        {                            
                            IRData irdata = new IRData();
                            irdata.opCode = EIROpCode.CastClass;
                            irdata.opValue = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList( maien.convertTargetMetaType, owirmc);

                            m_IRDataList.Add(irdata);
                        }
                        else
                        {
                            IRData irdata = new IRData();
                            irdata.opCode = EIROpCode.CastClass;
                            irdata.opValue = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(maien.convertTargetMetaType, owirmc);
                            m_IRDataList.Add(irdata);

                            var owirmc2 = IRManager.instance.GetIRMetaClassById(maien.convertTargetMetaVariable.GetOwnerClassTemplateClass().GetHashCode());
                            var irmt2 = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(maien.convertTargetMetaVariable.defineMetaType, owirmc);
                            IRStoreVariable irstore = IRStoreVariable.CreateIRStoreVariable(irmt2, owirmc2, m_IRMethod, maien.convertTargetMetaVariable);
                            AddIRRangeData(irstore.IRDataList);

                            IRLoadVariable irload3 = IRLoadVariable.CreateLoadVariable(irmt2, owirmc2, m_IRMethod, maien.convertTargetMetaVariable);
                            AddIRRangeData(irload3.IRDataList);

                            IRData irdata4 = new IRData();
                            irdata4.opCode = IRManager.GetConstIROpCode( EType.Null );
                            irdata4.opValue = "null";
                            AddIRData(irdata4);

                            IRData irdata5 = new IRData();
                            irdata5.opCode = EIROpCode.Cne;
                            AddIRData(irdata5);
                        }
                    }
                    break;
                default:
                    {
                        Debug.Assert( false, "Error IR表达式错误!!");
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

    public class IRNewExpress : IRBase
    {
        public IRNewExpress(IRMethod irMethod, MetaNewObjectExpressNode mnoen ) : base(irMethod)
        {
            IRMetaClass owirmc = IRManager.instance.GetIRMetaClassById(mnoen.ownerMetaClass.GetHashCode()); 
            IRMetaType newObjectIRMT = null;
            IRMetaClass irmc = null;

            if( mnoen.newType == MetaNewObjectExpressNode.ENewType.ArrayClass )
            {
                IRExpress ire = new IRExpress(irMethod, mnoen.arrayLengthExpress );
                m_IRDataList.AddRange(ire.IRDataList);

                var irMetaType = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mnoen.metaType, owirmc);
                IRNew irNew = new IRNew(irMethod, irMetaType, 0 );
                AddIRRangeData(irNew.IRDataList);

                if (mnoen.metaMemberFunction != null)
                {
                    IRDup irdup = new IRDup(irMethod);
                    m_IRDataList.AddRange(irdup.IRDataList);

                    var paramCount = mnoen.metaInputParamList.Count;
                    for (int j = 0; j < paramCount; j++)
                    {
                        IRExpress irexpress = new IRExpress(m_IRMethod, mnoen.metaInputParamList[j]);
                        AddIRRangeData(irexpress.IRDataList);
                    }

                    int callMethodIndex = -1;
                    string fname = "";

                    MetaClass mc2 = null;
                    if (mnoen.metaMemberFunction.sourceMetaMemberFunction != null)
                        mc2 = mnoen.metaMemberFunction.sourceMetaMemberFunction.ownerMetaClass;
                    else
                        mc2 = mnoen.metaMemberFunction.ownerMetaClass;

                    fname = mnoen.metaMemberFunction.virtualFunctionName;
                    irmc = IRManager.instance.GetIRMetaClassById(mc2.GetHashCode());

                    var runtimeMethod = irmc.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                    if (callMethodIndex == -1)
                    {
                        Log.AddGenIR(EError.None, "没有找到构建对象函数!");
                    }
                    List<IRMetaType> functionMtList = new List<IRMetaType>();
                    var irmethodcall = new IRMethodCall(newObjectIRMT, functionMtList, runtimeMethod, paramCount);
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallVirt;
                    datacall.index = callMethodIndex;
                    datacall.opValue = irmethodcall;
                    //datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }

                if (mnoen.metaBraceOrBracketStatementsContent?.assignStatementsList?.Count > 0)
                {
                    for (int y = 0; y < mnoen.metaBraceOrBracketStatementsContent.assignStatementsList.Count; y++)
                    {
                        var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[y];

                        IRDup irdup = new IRDup(irMethod);
                        AddIRRangeData(irdup.IRDataList);

                        if( asl.expressNode is MetaNewObjectExpressNode mnoe )
                        {
                            IRNewExpress irexp = new IRNewExpress(irMethod, mnoe );
                            AddIRRangeData(irexp.IRDataList);
                        }
                        else
                        {
                            IRExpress irexp = new IRExpress(irMethod, asl.expressNode);
                            AddIRRangeData(irexp.IRDataList);
                        }

                        IRData irdatastore = new IRData();
                        irdatastore.index = y;
                        irdatastore.opValue = true;
                        irdatastore.opCode = EIROpCode.StoreArrayIndex;
                        m_IRDataList.Add(irdatastore);
                    }
                }
            }
            else
            {
                if (mnoen.metaType.eType == EMetaTypeType.MetaGenClass)
                {
                    if (mnoen.ownerMetaClass is MetaGenTemplateClass mgtc)
                    {
                        owirmc = IRManager.instance.GetIRMetaClassById(mgtc.metaTemplateClass.GetHashCode());
                    }
                    newObjectIRMT = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mnoen.metaType, owirmc);
                    irmc = IRManager.instance.GetIRMetaClassById(mnoen.metaType.GetTemplateMetaClass().GetHashCode());
                    IRNew irNew = new IRNew(irMethod, newObjectIRMT);
                    AddIRRangeData(irNew.IRDataList);

                }
                else if (mnoen.metaType.eType == EMetaTypeType.MetaClass)
                {
                    owirmc = IRManager.instance.GetIRMetaClassById(mnoen.ownerMetaClass.GetHashCode());
                    IRMetaClass newObjIRMC = IRManager.instance.GetIRMetaClassById(mnoen.metaType.GetTemplateMetaClass().GetHashCode());
                    irmc = IRManager.instance.GetIRMetaClassById(mnoen.metaType.GetTemplateMetaClass().GetHashCode());
                    newObjectIRMT = new IRMetaType(newObjIRMC);
                    IRNew irNew = new IRNew(irMethod, newObjIRMC);
                    AddIRRangeData(irNew.IRDataList);
                }
                else
                {
                    owirmc = IRManager.instance.GetIRMetaClassById(mnoen.ownerMetaClass.GetHashCode());                    
                    irmc = IRManager.instance.GetIRMetaClassById(mnoen.metaType.GetTemplateMetaClass().GetHashCode());
                    newObjectIRMT = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mnoen.metaType, owirmc);

                    IRNew irNew = new IRNew(irMethod, newObjectIRMT);
                    AddIRRangeData(irNew.IRDataList);
                }

                if (mnoen.needInitMemberVariable)
                {
                    if (irmc.localIRMetaVariableList.Count > 0)
                    {
                        for (int x = 0; x < irmc.localIRMetaVariableList.Count; x++)
                        {
                            var lirmv = irmc.localIRMetaVariableList[x];
                            if (mnoen.metaBraceOrBracketStatementsContent?.assignStatementsList?.Count > 0)
                            {
                                MetaExpressNode men = lirmv.express;
                                for (int y = 0; y < mnoen.metaBraceOrBracketStatementsContent.assignStatementsList.Count; y++)
                                {
                                    var asl = mnoen.metaBraceOrBracketStatementsContent.assignStatementsList[y];
                                    if (asl.metaMemberVariable.GetHashCode() == lirmv.id )
                                    {
                                        men = asl.expressNode;
                                        break;
                                    }
                                }

                                IRExpress irexp = new IRExpress(irMethod, men );
                                AddIRRangeData(irexp.IRDataList);                                

                                IRData irdata = new IRData();
                                irdata.index = lirmv.index;
                                irdata.opCode = EIROpCode.StoreNotStaticField1;
                                m_IRDataList.Add(irdata);
                            }
                        }
                    }
                }
                if (mnoen.metaMemberFunction != null)
                {
                    IRDup irdup = new IRDup(irMethod);
                    m_IRDataList.AddRange(irdup.IRDataList);

                    var paramCount = mnoen.metaInputParamList.Count;
                    for (int j = 0; j < paramCount; j++)
                    {
                        IRExpress irexpress = new IRExpress(m_IRMethod, mnoen.metaInputParamList[j]);
                        AddIRRangeData(irexpress.IRDataList);
                    }

                    int callMethodIndex = -1;
                    string fname = "";

                    MetaClass mc2 = null;
                    if (mnoen.metaMemberFunction.sourceMetaMemberFunction != null)
                        mc2 = mnoen.metaMemberFunction.sourceMetaMemberFunction.ownerMetaClass;
                    else
                        mc2 = mnoen.metaMemberFunction.ownerMetaClass;

                    fname = mnoen.metaMemberFunction.virtualFunctionName;
                    irmc = IRManager.instance.GetIRMetaClassById(mc2.GetHashCode());

                    var runtimeMethod = irmc.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                    if (callMethodIndex == -1)
                    {
                        Log.AddGenIR(EError.None, "没有找到构建对象函数!");
                    }
                    List<IRMetaType> functionMtList = new List<IRMetaType>();
                    var irmethodcall = new IRMethodCall(newObjectIRMT, functionMtList, runtimeMethod, paramCount);
                    IRData datacall = new IRData();
                    datacall.opCode = EIROpCode.CallVirt;
                    datacall.index = callMethodIndex;
                    datacall.opValue = irmethodcall;
                    //datacall.SetDebugInfoByToken(mf.pingToken);
                    AddIRData(datacall);
                }
            }
        }
    }
}
