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
        public static IRLoadVariable NewLoadVariable( IRMethod _irMethod, MetaVariable mv )
        {
            IRMetaVariable irmv = null;
            if (mv.variableFrom == MetaVariable.EVariableFrom.Global)
            {
                IRLoadVariable irVar = new IRLoadVariable(_irMethod, mv.GetHashCode(), IRMetaVariableFrom.Global );
                return irVar;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Argument)
            {
                irmv = _irMethod.GetIRArgumentById(mv.GetHashCode());
                IRLoadVariable irVar = new IRLoadVariable(_irMethod, irmv.index, IRMetaVariableFrom.Argument);
                return irVar;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Member)
            {
                var irmc = _irMethod.irManager.GetIRMetaClassByName(mv.ownerMetaClass.allClassName);
                var index = irmc.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());
                if ( mv.isStatic )
                {
                    IRLoadVariable irVar = new IRLoadVariable(_irMethod, index, IRMetaVariableFrom.Static, mv.isTemplate);
                    return irVar;                   
                }
                else
                {
                    IRLoadVariable irVar = new IRLoadVariable(_irMethod, index, IRMetaVariableFrom.Member);
                    return irVar;
                }
            }
            else
            {
                irmv = _irMethod.GetIRLocalVariableById(mv.GetHashCode());
                IRLoadVariable irVar = new IRLoadVariable(_irMethod, irmv.index, IRMetaVariableFrom.LocalStatement);
                return irVar;
            }
        }
        protected IRLoadVariable(IRMethod _irMethod, int id, IRMetaVariableFrom irmvf, bool isTemplate = false ) : base(_irMethod)
        {
            if( irmvf == IRMetaVariableFrom.Global )
            {
                m_Data.opCode = EIROpCode.LoadArgument;
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
                if (isTemplate)
                {
                    IRData sc2 = new IRData();
                    sc2.opCode = EIROpCode.SetCurrentClassCallClass;
                    m_IRDataList.Add(sc2);
                }

                m_Data.opCode = EIROpCode.LoadStaticField;
                m_Data.index = id;
                m_IRDataList.Add(m_Data);

                if( isTemplate )
                {
                    IRData sc2 = new IRData();
                    sc2.opCode = EIROpCode.UnSetCallClass;
                    m_IRDataList.Add(sc2);
                }
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

        public static IRStoreVariable CreateIRStoreVariable( IRMethod _irMethod, MetaVariable mv )
        {
            IRMetaVariable irmv = null;
            if (mv.variableFrom == MetaVariable.EVariableFrom.Argument )
            {
                irmv = _irMethod.GetIRArgumentById(mv.GetHashCode());
                IRStoreVariable irsv = new IRStoreVariable(_irMethod, irmv.index, IRMetaVariableFrom.Argument);
                return irsv;
            }
            else if( mv.variableFrom == MetaVariable.EVariableFrom.LocalStatement)
            {
                irmv = _irMethod.GetIRLocalVariableById(mv.GetHashCode());
                IRStoreVariable irsv = new IRStoreVariable(_irMethod, irmv.index, IRMetaVariableFrom.LocalStatement);
                return irsv;
            }
            else if (mv.variableFrom == MetaVariable.EVariableFrom.Member)
            {
                var irmc = _irMethod.irManager.GetIRMetaClassByName(mv.ownerMetaClass.allClassName);
                var index = irmc.GetMetaMemberVariableIndexByHashCode(mv.GetHashCode());
                if (mv.isStatic)
                {
                    IRStoreVariable irsv = new IRStoreVariable(_irMethod, index, IRMetaVariableFrom.Static, mv.isTemplate );
                    return irsv;
                }
                else
                {
                    IRStoreVariable irsv = new IRStoreVariable(_irMethod, index, IRMetaVariableFrom.Member);
                    return irsv;
                }
            }
            else
            {

            }
            return null;
        }
        public IRStoreVariable(IRMethod _irMethod, int id, IRMetaVariableFrom irmvf, bool isTemplate = false) : base(_irMethod)
        {
            if( irmvf == IRMetaVariableFrom.Global )
            {
                m_Data.index = id;
                m_Data.opCode = EIROpCode.StoreStaticField;
                m_IRDataList.Add(m_Data);
            }
            else if (irmvf == IRMetaVariableFrom.Static)
            {
                if(isTemplate )
                {
                    IRData sc2 = new IRData();
                    sc2.opCode = EIROpCode.SetCurrentClassCallClass;
                    AddIRData(sc2);
                }
                m_Data.opCode = EIROpCode.StoreStaticField;
                m_Data.index = id;
                AddIRData(m_Data);
                if (isTemplate)
                {
                    IRData sc2 = new IRData();
                    sc2.opCode = EIROpCode.UnSetCallClass;
                    AddIRData(sc2);
                }
            }
            else if (irmvf == IRMetaVariableFrom.Member)
            {
                m_Data.index = id;
                m_Data.opCode = EIROpCode.StoreNotStaticField;
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