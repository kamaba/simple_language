//****************************************************************************
//  File:      IRVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using SimpleLanguage.Parse;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRLoadVariable : IRBase
    {
        private IRData m_Data = new IRData();
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
                System.Diagnostics.Debug.Assert(irmv != null);
                IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, irmv.index, IRMetaVariableFrom.Argument);
                return irVar;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Member)
            {
                int index = -1;
                if (irmc != null )
                {
                    index = irmc.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());
                }
                if( index == -1 )
                {
                    Log.AddGenIR(EError.None, "没有找到对应成员变量的Index");
                    return null;
                }
                if ( mv.isStatic )
                {
                    if (mv.metaDefineType.IsIncludeTemplate())
                    {
                        IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, index, IRMetaVariableFrom.Static);
                        return irVar;
                    }
                    else
                    {
                        IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global );
                        return irVar;
                    }               
                }
                else
                {
                    IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, index, IRMetaVariableFrom.Member);
                    return irVar;
                }
            }
            else
            {
                irmv = _irMethod.GetIRLocalVariableById(mv.GetHashCode());
                System.Diagnostics.Debug.Assert( irmv != null );
                IRLoadVariable irVar = new IRLoadVariable(irmt, _irMethod, irmv.index, IRMetaVariableFrom.LocalStatement);
                return irVar;
            }
        }
        protected IRLoadVariable( IRMetaType irmt, IRMethod _irMethod, int id, IRMetaVariableFrom irmvf ) : base(_irMethod)
        {
            if( irmvf == IRMetaVariableFrom.Global )
            {
                m_Data.opCode = EIROpCode.LocalGlobal;
                m_Data.index = id;
                m_IRDataList.Add(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.Argument )
            {
                m_Data.opCode = EIROpCode.LoadArgument;
                m_Data.index = id;
                //data.SetDebugInfoByToken( mv.pingToken );
                m_IRDataList.Add(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.Member)
            {
                //irmv = _irMethod.GetIRLocalVariableById(id);
                //data.SetDebugInfoByToken(mv.pingToken);
                m_Data.index = id;
                m_Data.opCode = EIROpCode.LoadNotStaticField;
                m_IRDataList.Add(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.LocalStatement)
            {
                m_Data.opCode = EIROpCode.LoadLocal;
                m_Data.index = id;
                //data.SetDebugInfoByToken(mv.pingToken);
                m_IRDataList.Add(m_Data);
            }
            else if( irmvf == IRMetaVariableFrom.Static )
            {
                m_Data.opValue = irmt;
                m_Data.opCode = EIROpCode.LoadStaticField;
                m_Data.index = id;
                m_IRDataList.Add(m_Data);
            }
            else
            {
                Log.AddVM( EError.None, $"SVM Error 没有找到加载变量的来源类型！");
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
                if (cirmc != null )
                {
                    index = cirmc.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());
                }
                if (mv.isStatic)
                {
                    if( mv.metaDefineType.IsIncludeTemplate() )
                    {
                        IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, index, IRMetaVariableFrom.Static);
                        return irsv;
                    }
                    else
                    {
                        IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global);
                        return irsv;
                    }
                }
                else
                {
                    IRStoreVariable irsv = new IRStoreVariable(irmt, _irMethod, index, IRMetaVariableFrom.Member);
                    return irsv;
                }
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
                AddIRData(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.Member)
            {
                m_Data.index = id;
                m_Data.opCode = EIROpCode.StoreNotStaticField2;
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
                Log.AddVM(EError.None, $"SVM Error 没有找到加载变量的来源类型！");
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