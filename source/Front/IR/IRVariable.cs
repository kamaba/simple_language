//****************************************************************************
//  File:      IRVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRLoadVariable : IRBase
    {
        private IRData m_LoadVarData = new IRData();
        public static IRLoadVariable CreateLoadVariable(IRMetaType irmt, IRMetaClass irmc, IRMethod _irMethod,  MetaVariable mv )
        {
            IRMetaVariable irmv = null;
            if (mv.variableFrom == MetaVariable.EVariableFrom.Global)
            {
                IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global );
                return irVar;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Argument)
            {
                int id = mv.GetHashCode();
                irmv = _irMethod.GetIRArgumentById(id);
                if(irmv == null )
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, "in argument", _irMethod.id, mv.name);
                }
                IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, irmv.index, IRMetaVariableFrom.Argument);
                return irVar;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.EnumMember )
            {
                int index = -1;
                irmc = IRManager.instance.GetIRMetaClassById(mv.ownerMetaClass.GetHashCode());
                index = irmc.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());

                IRMetaType irmt2 = new IRMetaType(irmc);
                IRLoadVariable irVar = new IRLoadVariable(irmt2, _irMethod, index, IRMetaVariableFrom.Static);
                return irVar;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Member)
            {
                int index = -1;
                if (irmc != null )
                {
                    MetaVariable gmv = mv;
                    if( mv.sourceMetaVariable != null )
                    {
                        gmv = mv.sourceMetaVariable;
                    }
                    index = irmc.GetMetaMemberVariableIndexByHashCode(gmv.GetHashCode());
                }
                if ( mv.isConst || mv.isStatic )
                {
                    //if (mv.realMetaType.GenTemplateIsIncludeTemplate())
                    //{
                    //    if (index == -1)
                    //    {
                    //        Log.AddIRLog(LID.Unknown, "没有找到对应成员变量的Index");
                    //        return null;
                    //    }
                    //    IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, index, IRMetaVariableFrom.Static);
                    //    return irVar;
                    //}
                    //else
                    //{
                    //    IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global );
                    //    return irVar;
                    //}
                    //
                    if (index == -1)
                    {
                        Log.AddIRLog(LID.IRMethodNotFoundVariable, "in const member index = -1", _irMethod.id, mv.name);
                        return null;
                    }
                    irmt = new IRMetaType(irmc);
                    IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, index, IRMetaVariableFrom.Static);
                    return irVar;
                }
                else
                {
                    if (index == -1)
                    {
                        Log.AddIRLog(LID.IRMethodNotFoundVariable, "in member index = -1", _irMethod.id, mv.name);
                        return null;
                    }
                    IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, index, IRMetaVariableFrom.Member);
                    return irVar;
                }
            }
            else if(mv.variableFrom == MetaVariable.EVariableFrom.ArrayValue )
            {
                if( mv is MetaVisitVariable mvv )
                {
                    IRLoadVariable irVar = new IRLoadVariable();
                    int index = -1;
                    if (mvv.fastVisit)
                    {
                        IRData irdata = new IRData();
                        string val = mvv.fastVisitConstExpressNode.value.ToString();
                        if( int.TryParse(val, out index) )
                        {
                            if( index < 0 )
                            {
                                Log.AddIRLog(LID.IRMethodNotFoundVariable, "in array value index = -1", _irMethod.id, mv.name);
                            }
                        }
                        irdata.opValue = index;
                        irdata.index = index;
                        irdata.opCode = EIROpCode.LoadArrayIndex;
                        IRBase irbase = new IRBase();
                        irbase.AddIRData(irdata);
                        irVar.m_IRDataList.AddRange(irbase.IRDataList);
                    }
                    else
                    {
                        IRExpressBase irexpress = IRExpressManager.CreateExpress(_irMethod, mvv.visitExpressNode );
                        irVar.m_IRDataList.AddRange(irexpress.IRDataList);


                        IRData irdata = new IRData();
                        irdata.opCode = EIROpCode.LoadArrayIndexField;
                        IRBase irbase = new IRBase();
                        irbase.AddIRData(irdata);
                        irVar.m_IRDataList.AddRange(irbase.IRDataList);
                    }
                    return irVar;
                }
                else
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, "in array value else branch", _irMethod.id, mv.name);
                }
            }
            else
            {
                irmv = _irMethod.GetIRLocalVariableById(mv.GetHashCode());
                if(irmv == null )
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, "in array other from", _irMethod.id, mv.name);
                    return null;
                }
                IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, irmv.index, IRMetaVariableFrom.LocalStatement);
                return irVar;
            }
            return null;
        }
        protected IRLoadVariable()
        {
        }
        public IRLoadVariable( IRMetaType irmt, IRMethod _irMethod, int id, IRMetaVariableFrom irmvf ) : base(_irMethod)
        {
            if( irmvf == IRMetaVariableFrom.Global )
            {
                m_LoadVarData.opCode = EIROpCode.LoadGlobal;
                m_LoadVarData.index = id;
                m_IRDataList.Add(m_LoadVarData);
            }
            else if (irmvf == IRMetaVariableFrom.Argument )
            {
                m_LoadVarData.opCode = EIROpCode.LoadArgument;
                m_LoadVarData.index = id;
                //data.SetDebugInfoByToken( mv.pingToken );
                m_IRDataList.Add(m_LoadVarData);
            }
            else if (irmvf == IRMetaVariableFrom.Member)
            {
                //irmv = _irMethod.GetIRLocalVariableById(id);
                //data.SetDebugInfoByToken(mv.pingToken);
                m_LoadVarData.index = id;
                m_LoadVarData.opCode = EIROpCode.LoadNotStaticField;
                m_IRDataList.Add(m_LoadVarData);
            }
            else if( irmvf == IRMetaVariableFrom.Array )
            {
                m_LoadVarData.opCode = EIROpCode.LoadArrayIndexField;
                m_IRDataList.Add(m_LoadVarData);
            }
            else if (irmvf == IRMetaVariableFrom.LocalStatement)
            {
                m_LoadVarData.opCode = EIROpCode.LoadLocal;
                m_LoadVarData.index = id;
                //data.SetDebugInfoByToken(mv.pingToken);
                m_IRDataList.Add(m_LoadVarData);
            }
            else if (irmvf == IRMetaVariableFrom.Static)
            {
                m_LoadVarData.opValue = irmt;
                m_LoadVarData.opCode = EIROpCode.LoadStaticField;
                m_LoadVarData.index = id;
                m_LoadVarData.debugStaticOwnerIrName = _irMethod?.irOwnerMetaClass?.irName;
                if(id < 0 )
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, "in static id < 0", _irMethod.id, id);
                }
                m_IRDataList.Add(m_LoadVarData);
            }
            else
            {
                Log.AddIRLog(LID.IRMethodNotFoundVariable, "in array other from", _irMethod.id, id );
            }
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#LoadVariable#");
            for (int i = 0; i < m_IRDataList.Count; i++)
            {
                sb.AppendLine(m_IRDataList[i].ToString());
            }
            return sb.ToString();
        }
    }
    public class IRStoreVariable : IRBase
    {
        private IRData m_Data = new IRData();

        public static IRStoreVariable CreateIRStoreVariable( IRMetaType irmt, IRMetaClass irmc, IRMethod _irMethod, MetaVariable mv )
        {
            IRMetaVariable irmv = null;
            if (mv.variableFrom == MetaVariable.EVariableFrom.Argument )
            {
                irmv = _irMethod.GetIRArgumentById(mv.GetHashCode());
                IRStoreVariable irsv = new IRStoreVariable(irmt,_irMethod, irmv.index, IRMetaVariableFrom.Argument);
                return irsv;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.LocalStatement)
            {
                irmv = _irMethod.GetIRLocalVariableById(mv.GetHashCode());
                IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, irmv.index, IRMetaVariableFrom.LocalStatement);
                return irsv;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Member)
            {
                int index = -1;
                var cirmc = irmc == null ? irmt.irMetaClass : irmc;

                MetaVariable gmv = mv;
                if( mv.sourceMetaVariable != null )
                {
                    gmv = mv.sourceMetaVariable;
                }
                if (cirmc != null )
                {
                    index = cirmc.GetMetaMemberVariableIndexByHashCode(gmv.GetHashCode());
                }
                if (gmv.isStatic)
                {
                    IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, index, IRMetaVariableFrom.Static);
                    return irsv;

                    //if ( mv.realMetaType.GenTemplateIsIncludeTemplate() )
                    //{
                    //    IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, index, IRMetaVariableFrom.Static);
                    //    return irsv;
                    //}
                    //else
                    //{
                    //    IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global);
                    //    return irsv;
                    //}
                }
                else
                {
                    IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, index, IRMetaVariableFrom.Member);
                    return irsv;
                }
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.EnumMember)
            {
                int index = -1;
                if (irmc != null)
                {
                    MetaVariable gmv = mv;
                    if (mv.sourceMetaVariable != null)
                    {
                        gmv = mv.sourceMetaVariable;
                    }
                    index = irmc.GetMetaMemberVariableIndexByHashCode(gmv.GetHashCode());
                }
                IRMetaType irmt2 = new IRMetaType(irmc);
                IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, index, IRMetaVariableFrom.Member);
                return irsv;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.ArrayValue)
            {
                IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Array );
                return irsv;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.Global )
            {
                IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global );
                return irsv;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return null;
        }
        public IRStoreVariable( IRMetaType irmt, IRMethod _irMethod, int id, IRMetaVariableFrom irmvf) : base(_irMethod)
        {
            if( irmvf == IRMetaVariableFrom.Global )
            {
                m_Data.index = id;
                m_Data.opCode = EIROpCode.StoreGlobal;
                m_IRDataList.Add(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.Static)
            {
                m_Data.opValue = irmt;
                m_Data.opCode = EIROpCode.StoreStaticField;
                m_Data.index = id;
                m_Data.debugStaticOwnerIrName = _irMethod?.irOwnerMetaClass?.irName;
                AddIRData(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.Member)
            {
                m_Data.index = id;
                m_Data.opCode = EIROpCode.StoreNotStaticField2;
                m_IRDataList.Add(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.Array)
            {
                m_Data.index = 0;
                m_Data.opValue = true;
                m_Data.opCode = EIROpCode.StoreArrayIndexField;
                m_IRDataList.Add(m_Data);
            }
            else if( irmvf == IRMetaVariableFrom.Argument )
            {
                m_Data.opCode = EIROpCode.StoreLocal;
                m_Data.index = id;
                m_IRDataList.Add(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.LocalStatement)
            {
                m_Data.opCode = EIROpCode.StoreLocal;
                m_Data.index = id;
                m_IRDataList.Add(m_Data);
            }
            else
            {
                Log.AddIRLog(LID.Unknown, $"SVM Error 没有找到加载变量的来源类型！");
            }
        }

        protected IRStoreVariable()
        {

        }
        public static IRStoreVariable CreateStaticReturnIRSV( )
        {
            IRStoreVariable irsv = new IRStoreVariable( );
            IRData storeNode = new IRData();
            storeNode.opCode = EIROpCode.StoreReturn;
            storeNode.index = 0;
            irsv.IRDataList.Add(storeNode);

            return irsv;
        }
        public override string ToIRString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#StoreVariable#");
            for( int i = 0; i < m_IRDataList.Count; i++ )
            {
                sb.AppendLine(m_IRDataList[i].ToString());
            }
            return sb.ToString();
        }
    }
}