//****************************************************************************
//  File:      IRVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRLoadVariable : IRBase
    {
        private IRData m_LoadVarData = new IRData();
        public static IRLoadVariable CreateLoadVariable(IRMetaType irmt, IRMetaClass irmc, IRMethod _irMethod,  MetaVariable mv )
        {
            IRMetaVariable irmv = null;
            // ── 闭包共享捕获上下文拦截 ──
            // 宿主函数体内读取被闭包捕获的变量时, 路由到共享上下文数组槽,
            // 与闭包体 (经代理变量) 读写同一份数据, 实现"捕获即共享"语义。
            // 闭包函数自身生成 IR 时 bindMetaFunction 是闭包函数 (hasClosureContext=false), 不触发;
            // 代理变量自身不拦截 (直接经 arg0 读上层 ctx, 保持嵌套闭包的直通语义);
            // __closure_ctx__ 不在注册表, 走下方正常 LoadLocal 分支。
            if (_irMethod != null
                && _irMethod.bindMetaFunction is MetaMemberFunction hostMmf
                && hostMmf.hasClosureContext
                && !(mv is MetaClosureContextVariable))
            {
                var cap = hostMmf.GetClosureCapture(mv);
                if (cap != null)
                {
                    var ctxIrmv = _irMethod.GetIRLocalVariableById(hostMmf.closureContextVariable.GetHashCode());
                    if (ctxIrmv != null)
                    {
                        IRLoadVariable irVar = new IRLoadVariable();

                        IRData loadCtx = new IRData();
                        loadCtx.opCode = EIROpCode.LoadLocal;
                        loadCtx.index = ctxIrmv.index;
                        loadCtx.SetDebugInfoByToken(mv.token, "LoadLocal __closure_ctx__");
                        irVar.m_IRDataList.Add(loadCtx);

                        IRData loadIdx = new IRData();
                        loadIdx.opCode = EIROpCode.LoadArrayIndex;
                        loadIdx.index = cap.slotIndex;
                        loadIdx.SetDebugInfoByToken(mv.token, "LoadArrayIndex shared capture:" + mv.name);
                        irVar.m_IRDataList.Add(loadIdx);

                        return irVar;
                    }
                }
            }
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
                var fieldOwner = IRManager.GetIRMetaClassByMetaVariable(mv);
                var index = fieldOwner?.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode()) ?? -1;
                var irvar = new IRLoadVariable(new IRMetaType(fieldOwner), _irMethod, index, IRMetaVariableFrom.Static);
                return irvar;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.ClassMember)
            {
                int index = -1;
                if (irmc == null)
                {
                    // 调用方未提供归属 IRMetaClass（如闭包调用分支
                    // CreateLoadVariable(null, null, ...)）时按变量自身归属兜底，
                    // 否则静态成员（如 @DllImport 绑定的 Func 变量）index 解析必失败。
                    irmc = IRManager.GetIRMetaClassByMetaVariable(mv);
                }
                if (irmc != null)
                {
                    MetaVariable gmv = mv;
                    if (mv.sourceMetaVariable != null)
                    {
                        gmv = mv.sourceMetaVariable;
                    }
                    index = irmc.GetMetaMemberVariableIndexByHashCode(gmv.GetHashCode());
                }
                if ( mv.isStatic)
                {
                    // For const member variables (including injected project global.data primitive fields),
                    // prefer direct const load IR instead of runtime static field load.
                    if (mv is MetaMemberVariable mmv && mmv.constExpressNode != null)
                    {
                        IRLoadVariable constLoadVar = new IRLoadVariable();
                        var irexp = new IRExpress(_irMethod, mmv.constExpressNode);
                        constLoadVar.m_IRDataList.AddRange(irexp.IRDataList);
                        return constLoadVar;
                    }

                    //if (mv.realMetaType.GenTemplateIsIncludeTemplate())
                    //{
                    //    if (index == -1)
                    //    {
                    //        Log.AddIRLog(LID.ShowExtendMessage, "没有找到对应成员变量的Index");
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
            else if (mv.variableFrom == MetaVariable.EVariableFrom.DataMember)
            {
                Debug.Assert(irmc != null, "DataMember should have a valid owner class");
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
                if (index == -1 && mv is MetaMemberData mmdIndex)
                {
                    // Fallback for data members that are not yet bound in hash->index map.
                    index = mmdIndex.index;
                }
                if ( mv.isStatic)
                {
                    // For const member variables (including injected project global.data primitive fields),
                    // prefer direct const load IR instead of runtime static field load.
                    if (mv is MetaMemberVariable mmv && mmv.constExpressNode != null)
                    {
                        IRLoadVariable constLoadVar = new IRLoadVariable();
                        var irexp = new IRExpress(_irMethod, mmv.constExpressNode);
                        constLoadVar.m_IRDataList.AddRange(irexp.IRDataList);
                        return constLoadVar;
                    }
                    if (mv is MetaMemberData mmdConst && mmdConst.expressNode is MetaConstExpressNode constDataExpress)
                    {
                        IRLoadVariable constLoadVar = new IRLoadVariable();
                        var irexp = new IRExpress(_irMethod, constDataExpress);
                        constLoadVar.m_IRDataList.AddRange(irexp.IRDataList);
                        return constLoadVar;
                    }

                    //if (mv.realMetaType.GenTemplateIsIncludeTemplate())
                    //{
                    //    if (index == -1)
                    //    {
                    //        Log.AddIRLog(LID.ShowExtendMessage, "没有找到对应成员变量的Index");
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
            else if( mv.variableFrom == MetaVariable.EVariableFrom.ClosureContext )
            {
                // 闭包捕获变量读取: LoadArgument 0 (context 数组) + LoadArrayIndex slot
                if( mv is MetaClosureContextVariable ccv )
                {
                    IRLoadVariable irVar = new IRLoadVariable();
                    IRData loadArg = new IRData();
                    loadArg.opCode = EIROpCode.LoadArgument;
                    loadArg.index = 0;
                    loadArg.SetDebugInfoByToken( mv.token, "LoadArgument closure context" );
                    irVar.m_IRDataList.Add( loadArg );

                    IRData loadIdx = new IRData();
                    loadIdx.opCode = EIROpCode.LoadArrayIndex;
                    loadIdx.index = ccv.slotIndex;
                    loadIdx.SetDebugInfoByToken( mv.token, "LoadArrayIndex closure capture:" + mv.name );
                    irVar.m_IRDataList.Add( loadIdx );
                    return irVar;
                }
                return null;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.ArrayValue )
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
                        irdata.SetDebugInfoByToken(mvv.token ?? mvv.fastVisitConstExpressNode?.token, "LoadArrayIndex");
                        IRBase irbase = new IRBase();
                        irbase.AddIRData(irdata);
                        irVar.m_IRDataList.AddRange(irbase.IRDataList);
                    }
                    else
                    {
                        if(mvv.visitExpressNode != null )
                        {
                            IRExpressBase irexpress = IRExpressManager.CreateExpress(_irMethod, mvv.visitExpressNode);
                            irVar.m_IRDataList.AddRange(irexpress.IRDataList);


                            IRData irdata = new IRData();
                            irdata.opCode = EIROpCode.LoadArrayIndexField;
                            irdata.SetDebugInfoByToken(mvv.token ?? mvv.visitExpressNode?.token);
                            IRBase irbase = new IRBase();
                            irbase.AddIRData(irdata);
                            irVar.m_IRDataList.AddRange(irbase.IRDataList);
                        }
                        else if( mvv.methodCall != null )
                        {                            
                            IRCallFunction irCallFun = new IRCallFunction(_irMethod);
                            irCallFun.Parse(mvv.methodCall);
                            irVar.m_IRDataList.AddRange(irCallFun.IRDataList);
                        }
                        else
                        {
                            Log.AddIRLog(LID.IRMethodNotFoundVariable, "in array value else branch", _irMethod.id, mv.name);
                        }
                    }
                    return irVar;
                }
                else
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, "in array value else branch", _irMethod.id, mv.name);
                }
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.None )
            {
                /* 返回变量（returnMetaVariable）的 variableFrom 是 None。
                 * ref module 方法只跑 ParseArgumentsOnly，返回值在 m_MethodReturnList，
                 * 不在 m_MethodLocalVariableList 中。 */
                irmv = _irMethod.GetReturnVariableById(mv.GetHashCode());
                if(irmv == null)
                    irmv = _irMethod.GetIRLocalVariableById(mv.GetHashCode());
                if(irmv == null )
                {
                    Log.AddIRLog(LID.IRMethodNotFoundVariable, "in array other from", _irMethod.id, mv.name);
                    return null;
                }
                IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, irmv.index, IRMetaVariableFrom.LocalStatement);
                return irVar;
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
            // Default debug info: derive from method's bound MetaFunction token so even
            // synthesized load instructions carry a source location.
            m_LoadVarData.SetDebugInfoByToken(_irMethod?.bindMetaFunction?.token, irmvf.ToString());
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
                m_IRDataList.Add(m_LoadVarData);
            }
            else if (irmvf == IRMetaVariableFrom.Member)
            {
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
                m_IRDataList.Add(m_LoadVarData);
            }
            else if (irmvf == IRMetaVariableFrom.Static)
            {
                m_LoadVarData.opValue = irmt;
                m_LoadVarData.opCode = EIROpCode.LoadStaticField;
                m_LoadVarData.index = id;
                if(_irMethod != null )
                {
                    m_LoadVarData.debugStaticOwnerIrName = _irMethod?.irOwnerMetaClass?.irName;
                }
                else
                {
                    m_LoadVarData.debugStaticOwnerIrName = irmt.irMetaClass?.irName;
                }
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
            // ── 闭包共享捕获上下文拦截 ── (与 Load 侧对称, 值已在栈上)
            // 栈序 [..., value] -> [LoadLocal __ctx__] -> [..., value, array]
            // -> StoreArrayIndex slot (StoreTopMinus1_ValueTopMinus2: 数组 top-1, 值 top-2)
            // 拦截范围与 Load 侧一致: 仅宿主函数内的注册表变量, 代理变量不拦截。
            if (_irMethod != null
                && _irMethod.bindMetaFunction is MetaMemberFunction hostMmf
                && hostMmf.hasClosureContext
                && !(mv is MetaClosureContextVariable))
            {
                var cap = hostMmf.GetClosureCapture(mv);
                if (cap != null)
                {
                    var ctxIrmv = _irMethod.GetIRLocalVariableById(hostMmf.closureContextVariable.GetHashCode());
                    if (ctxIrmv != null)
                    {
                        IRStoreVariable irsv = new IRStoreVariable();

                        IRData loadCtx = new IRData();
                        loadCtx.opCode = EIROpCode.LoadLocal;
                        loadCtx.index = ctxIrmv.index;
                        loadCtx.SetDebugInfoByToken(mv.token, "LoadLocal __closure_ctx__");
                        irsv.IRDataList.Add(loadCtx);

                        IRData storeIdx = new IRData();
                        storeIdx.opCode = EIROpCode.StoreArrayIndex;
                        storeIdx.index = cap.slotIndex;
                        storeIdx.SetOpValue((byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2);
                        storeIdx.SetDebugInfoByToken(mv.token, "StoreArrayIndex shared capture:" + mv.name);
                        irsv.IRDataList.Add(storeIdx);

                        return irsv;
                    }
                }
            }
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
            else if (mv.variableFrom == MetaVariable.EVariableFrom.ClassMember)
            {
                int index = -1;
                var cirmc = irmc == null ? irmt.irMetaClass : irmc;

                MetaVariable gmv = mv;
                if (mv.sourceMetaVariable != null)
                {
                    gmv = mv.sourceMetaVariable;
                }
                if (cirmc != null)
                {
                    // 优先用实例化后的成员变量哈希查找，找不到再用源模板成员变量哈希
                    index = cirmc.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());
                    if (index < 0)
                    {
                        index = cirmc.GetMetaMemberVariableIndexByHashCode(gmv.GetHashCode());
                    }
                    // 哈希查找都失败时，用字段名查找
                    if (index < 0 && !string.IsNullOrEmpty(gmv.name))
                    {
                        index = cirmc.GetMetaMemberVariableIndexByName(gmv.name);
                    }
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
            else if (mv.variableFrom == MetaVariable.EVariableFrom.DataMember)
            {
                int index = -1;
                irmt = new IRMetaType(irmc);

                MetaVariable gmv = mv;
                if (mv.sourceMetaVariable != null)
                {
                    gmv = mv.sourceMetaVariable;
                }
                if (irmc != null)
                {
                    index = irmc.GetMetaMemberVariableIndexByHashCode(gmv.GetHashCode());
                }
                if (index == -1 && mv is MetaMemberData mmdIndex)
                {
                    // Fallback for data members that are not yet bound in hash->index map.
                    index = mmdIndex.index;
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
                var fieldOwner = IRManager.GetIRMetaClassByMetaVariable(mv);
                IRMetaType storageIrmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(
                        new MetaType(CoreMetaClassManager.memberMetaClass), fieldOwner);
                IRStoreVariable irsv = new IRStoreVariable(storageIrmt, _irMethod, 2, IRMetaVariableFrom.Member );
                return irsv;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.ArrayValue)
            {
                if (mv is MetaVisitVariable mvv && mvv.visitType == MetaVisitVariable.EVisitType.MethodCall)
                {
                    // MethodCall 模式：_setItem_ 方法调用
                    // 左侧已经执行了 _setItem_ 方法调用（在 IRAssignStatements 中处理）
                    // 这里不需要生成额外的 store 指令，直接返回空
                    return new IRStoreVariable();
                }
                IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Array );
                return irsv;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.ClosureContext )
            {
                // 闭包捕获变量写入: 值已在栈上 -> LoadArgument 0 (context) -> StoreArrayIndex slot
                // 栈序 [..., value, array] -> StoreTopMinus1_ValueTopMinus2 (数组 top-1, 值 top-2)
                if( mv is MetaClosureContextVariable ccv )
                {
                    IRStoreVariable irsv = new IRStoreVariable();

                    IRData loadArg = new IRData();
                    loadArg.opCode = EIROpCode.LoadArgument;
                    loadArg.index = 0;
                    loadArg.SetDebugInfoByToken( mv.token, "LoadArgument closure context" );
                    irsv.IRDataList.Add( loadArg );

                    IRData storeIdx = new IRData();
                    storeIdx.opCode = EIROpCode.StoreArrayIndex;
                    storeIdx.index = ccv.slotIndex;
                    storeIdx.SetOpValue( (byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2 );
                    storeIdx.SetDebugInfoByToken( mv.token, "StoreArrayIndex closure capture:" + mv.name );
                    irsv.IRDataList.Add( storeIdx );
                    return irsv;
                }
                return null;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.ClosureVariable )
            {
                // 闭包变量本身是宿主函数局部变量
                irmv = _irMethod.GetIRLocalVariableById( mv.GetHashCode() );
                if( irmv == null )
                {
                    Log.AddIRLog( LID.IRMethodNotFoundVariable, mv.token, "in closure variable", _irMethod.id, mv.name );
                    return null;
                }
                IRStoreVariable irsv = new IRStoreVariable( irmt, _irMethod, irmv.index, IRMetaVariableFrom.LocalStatement );
                return irsv;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.Global )
            {
                IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global );
                return irsv;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.None )
            {
                // 返回变量（returnMetaVariable）的 variableFrom 是 None。
                // ref module 方法的返回值在 m_MethodReturnList，不在 m_MethodLocalVariableList 中。
                irmv = _irMethod.GetReturnVariableById(mv.GetHashCode());
                if(irmv == null)
                    irmv = _irMethod.GetIRLocalVariableById(mv.GetHashCode());
                if(irmv != null)
                {
                    IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, irmv.index, IRMetaVariableFrom.LocalStatement);
                    return irsv;
                }
            }
            else
            {
                Log.AddIRLog(LID.IRVariableFromNotHandle, mv.token, "",  mv.name );
            }
            return null;
        }
        public IRStoreVariable( IRMetaType irmt, IRMethod _irMethod, int id, IRMetaVariableFrom irmvf) : base(_irMethod)
        {
            // Default debug info from the bound method's token.
            m_Data.SetDebugInfoByToken(_irMethod?.bindMetaFunction?.token, irmvf.ToString());
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
                if( id == -1 )
                {
                    Log.AddIRLog(LID.MetaCoreAssertShowMessage, $"SVM Error 没有找到加载变量的来源类型！");
                }
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
                // 参数槽与局部槽编号空间独立（Argument #0..N / Local #0..M），
                // 参数赋值必须写入参数槽：StoreLocal 只写局部槽，idx 越界时赋值会被丢弃。
                m_Data.opCode = EIROpCode.StoreArgument;
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
                Log.AddIRLog(LID.ShowExtendMessage, $"SVM Error 没有找到加载变量的来源类型！");
            }
        }

        protected IRStoreVariable()
        {

        }

        /// <summary>
        /// O3 常数融合：尝试把 [LoadConst*][Store*] 融合为单条 Store*ConstValue。
        /// payload 布局(EmbedIndexInPayload 后)：[index:4][etype:1][value:N]；
        /// 数组形式为 [index:4][flag:1][etype:1][value:N]。
        /// 栈效果：与经典 Store* 相同（数组/实例仍在栈上），但不再 pop 值，值来自 payload。
        /// 仅 optimizeLevel &gt;= 3 时生成；任何不满足条件的情况返回 false，
        /// 由调用方完整回退经典 LoadConst + Store 路径。
        /// </summary>
        public static bool TryCreateConstValueStore( IRMetaType irmt, IRMetaClass irmc, IRMethod _irMethod, MetaVariable mv, MetaConstExpressNode constNode, out IRStoreVariable fusedStore )
        {
            fusedStore = null;
            // -O3 未开启时必须走原逻辑
            if( ProjectManager.optimizeLevel < 3 ) return false;
            // AOT(MLIR) 后端尚未支持新 opcode，跳过融合以免 AOT 退化为解释执行
            if( _irMethod != null && _irMethod.isAot ) return false;
            if( constNode == null || constNode.value == null ) return false;

            // 常量类型必须有对应的 LoadConst 指令（保证可打包）
            var constLoadOpcode = IRUtil.GetConstIROpCode( constNode.eType );
            if( constLoadOpcode == EIROpCode.Nop ) return false;

            // 复用经典路由取得目标 store 指令与 index
            var baseStore = CreateIRStoreVariable( irmt, irmc, _irMethod, mv );
            if( baseStore == null ) return false;
            var dataList = baseStore.IRDataList;
            // 多指令序列（闭包捕获上下文等）不融合
            if( dataList == null || dataList.Count != 1 ) return false;
            var baseData = dataList[0];
            if( baseData == null ) return false;

            EIROpCode fusedOpcode;
            switch( baseData.opCode )
            {
                case EIROpCode.StoreLocal: fusedOpcode = EIROpCode.StoreLocalConstValue; break;
                case EIROpCode.StoreArgument: fusedOpcode = EIROpCode.StoreArgumentConstValue; break;
                case EIROpCode.StoreGlobal: fusedOpcode = EIROpCode.StoreGlobalConstValue; break;
                case EIROpCode.StoreNotStaticField1: fusedOpcode = EIROpCode.StoreNotStaticField1ConstValue; break;
                case EIROpCode.StoreNotStaticField2: fusedOpcode = EIROpCode.StoreNotStaticField2ConstValue; break;
                case EIROpCode.StoreArrayIndex: fusedOpcode = EIROpCode.StoreArrayIndexConstValue; break;
                default:
                    // StoreStaticField(payload 为变长 runtimeDefType JSON)与
                    // StoreArrayIndexField(下标在栈上)不融合
                    return false;
            }

            // StoreArrayIndex 经典 payload 的 flag 字节（数组/值出栈顺序）
            byte flagByte = (byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2;
            if( fusedOpcode == EIROpCode.StoreArrayIndexConstValue )
            {
                if( !( baseData.opValue is bool flagValue ) ) return false;
                flagByte = flagValue
                    ? (byte)EStoreArrayIndexFlag.StoreTopMinus2_ValueTopMinus1
                    : (byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2;
            }

            // 值字节：借道 LoadConst* 的打包路径，保证与经典 LoadConst 布局完全一致
            byte[] valueBytes;
            if( constNode.eType == EType.String )
            {
                var s = constNode.value?.ToString() ?? string.Empty;
                int stringId = IRManager.instance.AddStringIRStack( s );
                valueBytes = BitConverter.GetBytes( stringId );
            }
            else
            {
                var packHelper = new IRData();
                packHelper.opCode = constLoadOpcode;
                packHelper.SetOpValue( constNode.value );
                valueBytes = packHelper.Payload;
                if( valueBytes == null || valueBytes.Length == 0 ) return false;
            }

            // [etype:1][value:N]，数组形式再前置 [flag:1]
            byte[] payload = new byte[ valueBytes.Length + 1 ];
            payload[0] = (byte)constNode.eType;
            Buffer.BlockCopy( valueBytes, 0, payload, 1, valueBytes.Length );
            if( fusedOpcode == EIROpCode.StoreArrayIndexConstValue )
            {
                var withFlag = new byte[ payload.Length + 1 ];
                withFlag[0] = flagByte;
                Buffer.BlockCopy( payload, 0, withFlag, 1, payload.Length );
                payload = withFlag;
            }

            IRData fusedData = new IRData();
            fusedData.opCode = fusedOpcode;
            fusedData.index = baseData.index;
            fusedData.Payload = payload;
            fusedData.UpdateByteLength();
            fusedData.SetDebugInfoByToken( constNode.token, "O3 fused store " + fusedOpcode.ToString() );

            fusedStore = new IRStoreVariable();
            fusedStore.IRDataList.Add( fusedData );
            return true;
        }

        /// <summary>
        /// O3 常数融合：ret &lt;常量&gt; 时把 [LoadConst*][StoreReturn] 融合为 StoreReturnConstValue。
        /// payload 布局(EmbedIndexInPayload 后)：[index:4(=0)][etype:1][value:N]。
        /// </summary>
        public static bool TryCreateReturnConstValueStore( IRMethod _irMethod, MetaConstExpressNode constNode, out IRStoreVariable fusedStore )
        {
            fusedStore = null;
            // -O3 未开启时必须走原逻辑
            if( ProjectManager.optimizeLevel < 3 ) return false;
            if( _irMethod != null && _irMethod.isAot ) return false;
            if( constNode == null || constNode.value == null ) return false;

            var constLoadOpcode = IRUtil.GetConstIROpCode( constNode.eType );
            if( constLoadOpcode == EIROpCode.Nop ) return false;

            byte[] valueBytes;
            if( constNode.eType == EType.String )
            {
                var s = constNode.value?.ToString() ?? string.Empty;
                int stringId = IRManager.instance.AddStringIRStack( s );
                valueBytes = BitConverter.GetBytes( stringId );
            }
            else
            {
                var packHelper = new IRData();
                packHelper.opCode = constLoadOpcode;
                packHelper.SetOpValue( constNode.value );
                valueBytes = packHelper.Payload;
                if( valueBytes == null || valueBytes.Length == 0 ) return false;
            }

            IRData fusedData = new IRData();
            fusedData.opCode = EIROpCode.StoreReturnConstValue;
            fusedData.index = 0;
            var payload = new byte[ valueBytes.Length + 1 ];
            payload[0] = (byte)constNode.eType;
            Buffer.BlockCopy( valueBytes, 0, payload, 1, valueBytes.Length );
            fusedData.Payload = payload;
            fusedData.UpdateByteLength();
            fusedData.SetDebugInfoByToken( constNode.token, "O3 fused store StoreReturnConstValue" );

            fusedStore = new IRStoreVariable();
            fusedStore.IRDataList.Add( fusedData );
            return true;
        }

        public static IRStoreVariable CreateStaticReturnIRSV(IRMethod irMethod = null, Token token = null)
        {
            IRStoreVariable irsv = new IRStoreVariable( );
            IRData storeNode = new IRData();
            storeNode.opCode = EIROpCode.StoreReturn;
            storeNode.index = 0;
            storeNode.SetDebugInfoByToken(token ?? irMethod?.bindMetaFunction?.token, "StoreReturn");
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