//****************************************************************************
//  File:      MetaVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description:  all variable 's define, if it's iterator style then use IteratorMetaVariable, other custom same style!
//****************************************************************************

using SimpleLanguage.Compile;

using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SimpleLanguage.Core
{
    public class MetaVariable : MetaBase
    {
        public enum EVariableFrom
        {
            None,
            Static,
            Global,
            Argument,
            LocalStatement,
            Member,
            ArrayValue,
            EnumMember,
        }
        public bool isDefineMetaType => m_IsDefineMetaType;
        public virtual bool isStatic => m_IsStatic;
        public virtual bool isConst => m_IsConst;
        public virtual bool isParsed => m_IsParsed;
        public bool isArgument => m_VariableFrom == EVariableFrom.Argument;
        public bool isGlobal => m_VariableFrom == EVariableFrom.Global;
        public bool isArray
        {
            get { return m_IsDefineMetaType ? (m_DefineMetaType != null ? m_DefineMetaType.IsArray() : false) : (m_RealMetaType != null ? m_RealMetaType.IsArray() : false); }
        }

        public MetaBlockStatements ownerMetaBlockStatements => m_OwnerMetaBlockStatements;
        public EVariableFrom variableFrom => m_VariableFrom;
        public MetaType defineMetaType => m_DefineMetaType;
        public MetaType realMetaType => m_RealMetaType;
        public MetaClass ownerMetaClass => m_OwnerMetaClass;
        public MetaVariable sourceMetaVariable => m_SourceMetaVariable;

        private Token m_VariableNameToken = null;

        #region 属性

        protected MetaClass m_OwnerMetaClass = null;
        protected MetaType m_DefineMetaType = null;
        protected MetaType m_RealMetaType = null;
        protected EVariableFrom m_VariableFrom;
        protected bool m_IsParsed = false;
        protected bool m_IsStatic = false;
        protected bool m_IsConst = false;
        protected bool m_IsDefineMetaType = false;      //该字段是表明，该类型使用了定义类型， 如果是var 或者是没定义的，则可以使用真实的类型
        //用来存放扩展包含变量
        protected Dictionary<string, MetaVariable> m_MetaVariableDict = new Dictionary<string, MetaVariable>();
        protected MetaBlockStatements m_OwnerMetaBlockStatements = null;
        protected MetaVariable m_SourceMetaVariable = null;
        #endregion

        protected MetaVariable() { }
        public MetaVariable(MetaVariable mv) : base(mv)
        {
            m_OwnerMetaClass = mv.m_OwnerMetaClass;
            m_DefineMetaType = new MetaType( mv.m_DefineMetaType );
            if(mv.m_RealMetaType != null )
                m_RealMetaType = new MetaType(mv.m_RealMetaType);
            m_VariableFrom = mv.m_VariableFrom;
            m_PintTokenList = mv.m_PintTokenList;
            m_IsStatic = mv.m_IsStatic;
            m_IsConst = mv.m_IsConst;
            m_IsParsed = mv.m_IsParsed;
            m_SourceMetaVariable = mv;
            m_IsDefineMetaType = mv.m_IsDefineMetaType;

            foreach ( var v in mv.m_MetaVariableDict)
            {
                m_MetaVariableDict.Add(v.Key, new MetaVariable( v.Value ) );
            }
            m_OwnerMetaBlockStatements = mv.m_OwnerMetaBlockStatements;
        }
        public MetaVariable(string _name, EVariableFrom from, MetaBlockStatements mbs, MetaClass ownerClass, MetaType mdt )
        {
            m_Name = _name;
            m_VariableFrom = from;
            m_OwnerMetaBlockStatements = mbs;
            m_OwnerMetaClass = ownerClass;
            m_DefineMetaType = mdt;
            if (m_DefineMetaType == null )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            else
            {
                m_IsDefineMetaType = true;
            }
            if (mdt != null)
            {
                m_RealMetaType = new MetaType(mdt);
            }
        } 
        public virtual void SetOwnerMetaClass(MetaClass ownerclass)
        {
            m_OwnerMetaClass = ownerclass;
        }
        public void SetIsStatic( bool iss )
        {
            this.m_IsStatic = iss;
        }
        public void SetIsConst(bool isc)
        {
            this.m_IsConst = isc;
        }
        public void SetVariableNameToken( Token token )
        {
            m_VariableNameToken = token;
        }
        public void SetIsDefineMetaType( bool flag )
        {
            this.m_IsDefineMetaType = flag;
        }
        public MetaType GetDefineMetaTypeIsDefine()
        {
            if( this.m_IsDefineMetaType )
            {
                return m_DefineMetaType;
            }
            return null;
        }
        public MetaType GetFinalMetaType()
        {
            if (this.m_IsDefineMetaType)
            {
                return m_DefineMetaType;
            }
            else
            {
                if (m_RealMetaType != null)
                {
                    return m_RealMetaType;
                }
            }
            return null;
        }
        public MetaClass GetFinalTemplateMetaClass()
        {
            MetaClass mc = CoreMetaClassManager.objectMetaClass;
            if (m_IsDefineMetaType)
            {                
                if (m_DefineMetaType.metaClass is MetaGenTemplateClass mgtc)
                {
                    mc = mgtc.metaTemplateClass;
                }
                else
                {
                    mc = m_DefineMetaType.metaClass;
                }
            }
            else 
            {
                if(m_RealMetaType != null )
                {
                    if (m_RealMetaType.metaClass is MetaGenTemplateClass mgtc)
                    {
                        mc = mgtc.metaTemplateClass;
                    }
                    else
                    {
                        mc = m_RealMetaType.metaClass;
                    }
                }
            }
            return mc;
        }
        public void SetRealMetaType( MetaType realMt )
        {
            this.m_RealMetaType = realMt;
        }
        public bool GetIsCanCanIterate()
        {
            MetaClass mc = GetFinalTemplateMetaClass();

            if (mc is MetaEnum)
            {
                return true;
            }
            else
            {
                MetaClass findMc = ClassManager.instance.GetClassByName("Core.IIterable");
                if (mc.GetInterfaceByMetaClass(findMc))
                {
                    return true;
                }
                MetaClass findMc2 = ClassManager.instance.GetClassByName("Core.IIterable<T>", 1 );
                if (mc.GetInterfaceByMetaClass(findMc2))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryAdjustConstExpressByDefineMetaType(MetaConstExpressNode mcen, MetaType defineMetaType)
        {
            if (mcen == null || defineMetaType == null)
            {
                return false;
            }

            var curEType = CoreMetaClassManager.GetETypeByMetaClass(defineMetaType.metaClass);

            // 0b/0o/0x 常量必须在“已有前置定义类型”下才允许进入 constValue。
            if (curEType == EType.Object && IsRadixNumberLiteral(mcen))
            {
                Log.AddMetaCoreLog(LID.Unknown, "Error 0b/0o/0x 常量必须配合前置定义类型使用，例如: byte v = 0b1010;");
                return false;
            }

            if (curEType == EType.Object)
            {
                curEType = mcen.eType;
            }

            if (mcen.eType == curEType)
            {
                return true;
            }

            return TryAdjustConstExpressByDefineEType(mcen, curEType);
        }
        public MetaClass GetOwnerClassTemplateClass()
        {
            if( m_OwnerMetaClass is MetaGenTemplateClass mgtc )
            {
                return mgtc.metaTemplateClass;
            }
            return m_OwnerMetaClass;
        }
        public virtual MetaClass GetTemplateMetaClass()
        {
            if( isArray )
            {
                return CoreMetaClassManager.arrayMetaClass;
            }

            if( m_IsDefineMetaType )
            {
                if ( m_DefineMetaType.metaClass is MetaGenTemplateClass mgtc)
                {
                    return mgtc.metaTemplateClass;
                }
                return m_DefineMetaType.metaClass;
            }
            else
            {
                if( m_RealMetaType.metaClass is MetaGenTemplateClass mgtc )
                {
                    return mgtc.metaTemplateClass;
                }
                return m_RealMetaType.metaClass;
            }
        }
        public void SetDefineMetaClass(MetaClass defineClass)
        {
            m_DefineMetaType.SetMetaClass(defineClass);
        }
        public void SetMetaDefineType( MetaType mdt )
        {
            m_DefineMetaType = mdt;
        }
        public virtual void SetOwnerBlockstatements(MetaBlockStatements mbs)
        {
            m_OwnerMetaBlockStatements = mbs;
        }
        public virtual void ParseDefineMetaType()
        {

        }
        public virtual void ParseRealMetaType()
        {

        }
        public virtual bool Parse()
        {
            return true;
        }
        public virtual void CreateMetaExpress()
        {

        }
        public virtual bool ParseMetaExpress()
        {
            return true;
        }        
        public bool AddMetaVariable( MetaVariable mv )
        {
            if(m_MetaVariableDict.ContainsKey(mv.name) )
            {
                return false;
            }
            m_MetaVariableDict.Add(mv.name, mv);
            return true;
        }
        public virtual MetaVariable GetMetaVariable( string name )
        {
            if( m_MetaVariableDict.ContainsKey( name ))
            {
                return m_MetaVariableDict[name];
            }
            return null;
        }
        public virtual string ToStatementString()
        {
            return "";
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[" + m_DefineMetaType.ToFormatString() + "]");
            sb.Append(m_Name);
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[" + m_DefineMetaType.ToString() + "]");
            sb.Append(m_Name);
            return sb.ToString();
        }

        public static bool IsNumericEType(EType t)
        {
            return t == EType.UInt8
                || t == EType.Int8
                || t == EType.Int16
                || t == EType.UInt16
                || t == EType.Int32
                || t == EType.UInt32
                || t == EType.Int64
                || t == EType.UInt64
                || t == EType.Float16
                || t == EType.Float32
                || t == EType.Float64
                || t == EType.Num;
        }

        public static bool TryConvertConstValueByEType(EType targetType, object input, out object converted)
        {
            converted = null;
            try
            {
                switch (targetType)
                {
                    case EType.Boolean:
                        converted = Convert.ToBoolean(input);
                        return true;
                    case EType.UInt8:
                        converted = Convert.ToByte(input);
                        return true;
                    case EType.Int8:
                        converted = Convert.ToSByte(input);
                        return true;
                    case EType.Int16:
                        converted = Convert.ToInt16(input);
                        return true;
                    case EType.UInt16:
                        converted = Convert.ToUInt16(input);
                        return true;
                    case EType.Int32:
                        converted = Convert.ToInt32(input);
                        return true;
                    case EType.UInt32:
                        converted = Convert.ToUInt32(input);
                        return true;
                    case EType.Int64:
                        converted = Convert.ToInt64(input);
                        return true;
                    case EType.UInt64:
                        converted = Convert.ToUInt64(input);
                        return true;
                    case EType.Float16:
                        converted = (Half)Convert.ToSingle(input);
                        return true;
                    case EType.Float32:
                        converted = Convert.ToSingle(input);
                        return true;
                    case EType.Float64:
                    case EType.Num:
                        converted = Convert.ToDouble(input);
                        return true;
                    case EType.String:
                        converted = Convert.ToString(input) ?? string.Empty;
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                converted = null;
                return false;
            }
        }

        public static bool TryAdjustConstExpressByDefineEType(MetaConstExpressNode mcen, EType defineEType)
        {
            if (mcen == null)
            {
                return false;
            }

            if (defineEType == EType.Object && IsRadixNumberLiteral(mcen))
            {
                Log.AddMetaCoreLog(LID.Unknown, "Error 0b/0o/0x 常量必须配合前置定义类型使用，例如: uint v = 0xFF;");
                return false;
            }

            var curEType = defineEType;
            var expEType = mcen.eType;
            Token token = mcen.token;

            if (expEType == EType.Null)
            {
                return true;
            }

            if (IsNumericEType(curEType) && IsNumericEType(expEType))
            {
                if (curEType == expEType)
                {
                    return true;
                }

                if(curEType == EType.Num )
                {
                    return true;
                }

                bool canConvert = false;
                switch (curEType)
                {
                    case EType.Int8:
                    case EType.UInt8:
                        canConvert = (expEType == EType.UInt8 || expEType == EType.Int8);
                        break;
                    case EType.Int16:
                    case EType.UInt16:
                        canConvert = expEType == EType.UInt8 || expEType == EType.Int8
                            || expEType == EType.UInt16 || expEType == EType.Int16;
                        break;
                    case EType.Int32:
                    case EType.UInt32:
                    case EType.Float32:
                        canConvert = expEType == EType.UInt8 || expEType == EType.Int8
                            || expEType == EType.UInt16 || expEType == EType.Int16
                            || expEType == EType.Int32 || expEType == EType.UInt32;
                        break;
                    case EType.Int64:
                    case EType.UInt64:
                    case EType.Float64:
                        canConvert = true;
                        break;
                }

                if (canConvert && TryConvertConstValueByEType(curEType, mcen.value, out var convertedValue))
                {
                    mcen.SetConstValue(curEType, convertedValue);
                    return true;
                }

                if (canConvert && IsRadixNumberLiteral(mcen)
                    && TryConvertRadixUnsignedToSignedByEType(curEType, mcen.value, out var radixConvertedValue))
                {
                    mcen.SetConstValue(curEType, radixConvertedValue);
                    return true;
                }

                Log.AddMetaCoreLog(LID.MetaCoreExpressTypeGEDefineType, token, (mcen.value?.ToString() ?? "null"), curEType.ToString(), expEType.ToString());
                return false;
            }

            if (expEType != curEType)
            {
                if (TryConvertConstValueByEType(curEType, mcen.value, out var convertedValue))
                {
                    mcen.SetConstValue(curEType, convertedValue);
                    return true;
                }

                if (IsRadixNumberLiteral(mcen)
                    && TryConvertRadixUnsignedToSignedByEType(curEType, mcen.value, out var radixConvertedValue))
                {
                    mcen.SetConstValue(curEType, radixConvertedValue);
                    return true;
                }

                Log.AddMetaCoreLog(LID.MetaCoreExpressTypeGEDefineType, token, (mcen.value?.ToString() ?? "null"), curEType.ToString(), expEType.ToString());
                return false;
            }

            return true;
        }

        private static bool IsRadixNumberLiteral(MetaConstExpressNode mcen)
        {
            var token = mcen?.token;
            if (token == null)
            {
                return false;
            }

            if (token.type == ETokenType.NumberReal)
            {
                return true;
            }

            if (token.type != ETokenType.Number)
            {
                return false;
            }

            if (string.IsNullOrEmpty(token.path) || !File.Exists(token.path))
            {
                return false;
            }

            try
            {
                var lines = File.ReadAllLines(token.path);
                int lineIndex = token.sourceBeginLine - 1;
                if (lineIndex < 0 || lineIndex >= lines.Length)
                {
                    return false;
                }

                var line = lines[lineIndex];
                int start = token.sourceBeginChar;
                if (start < 0 || start + 1 >= line.Length)
                {
                    return false;
                }

                return line[start] == '0' &&
                       (line[start + 1] == 'x' || line[start + 1] == 'X'
                        || line[start + 1] == 'o' || line[start + 1] == 'O'
                        || line[start + 1] == 'b' || line[start + 1] == 'B');
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvertRadixUnsignedToSignedByEType(EType targetType, object input, out object converted)
        {
            converted = null;
            try
            {
                ulong u = Convert.ToUInt64(input);
                switch (targetType)
                {
                    case EType.Int8:
                        if (u <= byte.MaxValue)
                        {
                            converted = unchecked((sbyte)(byte)u);
                            return true;
                        }
                        break;
                    case EType.Int16:
                        if (u <= ushort.MaxValue)
                        {
                            converted = unchecked((short)(ushort)u);
                            return true;
                        }
                        break;
                    case EType.Int32:
                        if (u <= uint.MaxValue)
                        {
                            converted = unchecked((int)(uint)u);
                            return true;
                        }
                        break;
                    case EType.Int64:
                        converted = unchecked((long)u);
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }


    public class MetaVisitVariable : MetaVariable
    {
        /*
         * 访问变量 一般使用 $x $x 必须先定义
         * int a = 20; Array arr = Array<int>( 1,2,3); 
         * int b = arr.$a; 这里的$a就是访问变量，使用arr为localMV, 使用m_VisitMetaVariable 是a 如果是常量，则保存
         * 常量的  arr.$0  m_VisitMV = null; m_AtName = "0";  返回值本身就是一个变量，相当于已经访问过了，在defineType
         * 中，返回模版类中的名称
         */
        public enum EVisitType
        {
            Link,
            AT
        }
        public bool fastVisit => m_FastVisit;
        public MetaExpressNode visitExpressNode => m_VisitExpressNode;
        public MetaConstExpressNode fastVisitConstExpressNode => m_VisitExpressNode as MetaConstExpressNode;

        private MetaVariable m_SourceMetaVariable = null;
        private EVisitType m_VisitType = EVisitType.AT;
        //private MetaCallLink m_TargetMetaVisitCallLink = null;
        string m_AtName = "";
        private bool m_FastVisit = false;
        private MetaExpressNode m_VisitExpressNode = null;
        private int? m_Index = null;

        public MetaVisitVariable(MetaVariable source, MetaVariable target)
        {
            m_VisitType = EVisitType.Link;
            m_SourceMetaVariable = source;
           // m_TargetMetaVariable = target;
            m_DefineMetaType = target.defineMetaType;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaConstExpressNode mvv)
        {
            m_VariableFrom = EVariableFrom.ArrayValue;
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_IsDefineMetaType = lmv.isDefineMetaType;   
            m_VisitExpressNode = mvv;
            m_FastVisit = true;
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaCallLink mvv)
        {
            m_VariableFrom = EVariableFrom.ArrayValue;
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_FastVisit = false;
            if (lmv.isArray)
            {
                if (mvv == null && string.IsNullOrEmpty(m_AtName))
                {
                    Log.AddMetaCoreLog(LID.Unknown, "Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_VisitExpressNode = new MetaCallLinkExpressNode(mvv);

                if (mvv.visitNodeList.Count == 1)
                {
                    if (mvv.visitNodeList[0].constValueExpress != null)
                    {
                        if (mvv.visitNodeList[0].constValueExpress.eType == EType.Int32)
                        {
                            m_Index = (int)mvv.visitNodeList[0].constValueExpress.value;
                            m_FastVisit = true;
                        }
                    }
                }
            }
        }
        public MetaVisitVariable(string _name, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv, MetaOpExpressNode moe )
        {
            m_VariableFrom = EVariableFrom.ArrayValue;
            m_Name = _name;
            m_AtName = _name;
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_SourceMetaVariable = lmv;
            m_FastVisit = false;
            if (lmv.isArray)
            {
                if (moe == null && string.IsNullOrEmpty(m_AtName))
                {
                    Log.AddMetaCoreLog(LID.Unknown, "Error VisitMetaVariable访问变量访问位置不能同时为空!!");
                    return;
                }
                m_VisitExpressNode = moe;
            }
        }
        public override void ParseDefineMetaType()
        {
            MetaType getMt = null;
            if ( this.m_SourceMetaVariable.isDefineMetaType)
            {
                if (m_SourceMetaVariable.defineMetaType.IsArray() )
                {
                    var mtlist = m_SourceMetaVariable.defineMetaType.GetGenTemplateMetaTypeList();
                    if (mtlist.Count > 0 )
                    {
                        getMt = new MetaType( mtlist[0] );
                    }
                }
                else
                {

                }
            }
            
            if(getMt == null )
            {
                m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            }
            else
            {
                m_DefineMetaType = new MetaType(getMt);
            }
        }
        public override void  ParseRealMetaType()
        {
            if (m_SourceMetaVariable.realMetaType.IsArray() )
            {
                var mtlist = m_SourceMetaVariable.realMetaType.GetGenTemplateMetaTypeList();
                if( mtlist.Count == 1 )
                {
                    m_RealMetaType = new MetaType(mtlist[0]);
                    if (m_IsDefineMetaType == false)
                    {
                        m_DefineMetaType = new MetaType(m_RealMetaType);
                    }
                }
                else
                {
                    Debug.Assert(false, "");
                }
            }
            else
            {
                m_RealMetaType = new MetaType(m_SourceMetaVariable.realMetaType.metaClass);
            }
        }
        public void SetNotUseFast()
        {
            m_FastVisit = false;
        }        
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_VisitType == EVisitType.Link)
            {
                if (m_SourceMetaVariable != null)
                {
                    sb.Append("[" + m_SourceMetaVariable.defineMetaType.name + "]");
                    sb.Append(m_SourceMetaVariable.name);
                    sb.Append(".");
                }
                //sb.Append("[" + m_TargetMetaVisitNode..name + "]");
                //sb.Append(m_TargetMetaVariable.name);
            }
            else
            {
                sb.Append(m_SourceMetaVariable.name);
                if (m_SourceMetaVariable.isArray)
                {
                    sb.Append("[");
                    //sb.Append(m_DefineMetaType.ToFormatString());
                    sb.Append(m_Name);
                    sb.Append("]");
                    //sb.Append(m_Express.ToFormatString());
                }
                else
                {
                    //sb.Append(m_TargetMetaVariable.name);
                }
            }

            return sb.ToString();
        }
    }
    public class MetaIteratorVariable : MetaVariable
    {
        int m_Index = 0;
        MetaVariable m_ContentMetaVariable = null;
        //MetaType m_OrgMetaDefineType = null;
        //MetaVariable m_IndexMetaVariable = null;
        //MetaVariable m_ValueMetaVariable = null;
        FileMetaClassDefine m_FileMetaClassDefine = null;
        private Token m_VariableNameToken = null;

        public MetaIteratorVariable(FileMetaClassDefine _fmcl, Token variableNameToken, MetaClass mc, MetaBlockStatements mbs, MetaVariable lmv )
        {
            m_FileMetaClassDefine = _fmcl;
            m_VariableFrom = EVariableFrom.LocalStatement;
            m_VariableNameToken = variableNameToken;
            m_Name = variableNameToken.lexeme.ToString();
            m_OwnerMetaClass = mc;
            m_OwnerMetaBlockStatements = mbs;
            m_ContentMetaVariable = lmv;
            //m_OrgMetaDefineType = orgMC;
            //m_IndexMetaVariable = new MetaVariable("index", EVariableFrom.ArrayInner, mbs, mc, new MetaType(CoreMetaClassManager.int32MetaClass));
            //m_ValueMetaVariable = new MetaVariable("value", EVariableFrom.ArrayInner, mbs, mc, new MetaType(orgMC.metaClass));
            //m_IndexMetaVariable.AddPingToken(lmv.pingToken);
            //m_ValueMetaVariable.AddPingToken(lmv.pingToken);
        }
        public override bool Parse()
        {
            if(m_FileMetaClassDefine != null )
            {
                m_DefineMetaType = TypeManager.instance.GetMetaTypeByTemplateFunction(ownerMetaClass, m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction, m_FileMetaClassDefine );
                m_IsDefineMetaType = true;
            }
            else
            {
                if( m_DefineMetaType == null )
                {
                    if(m_ContentMetaVariable.isDefineMetaType )
                    {
                        m_DefineMetaType = m_ContentMetaVariable.defineMetaType.GetMetaTypeByIndex(0);
                    }
                    else
                    {
                        m_DefineMetaType = m_ContentMetaVariable.realMetaType.GetMetaTypeByIndex(0);
                    }
                }
            }
            m_RealMetaType = new MetaType(m_DefineMetaType);
            return true;
        }
        public override MetaVariable GetMetaVariable(string name)
        {
            //if (name == "index")
            //{
            //    return m_IndexMetaVariable;
            //}
            //else if (name == "value")
            //{
            //    return m_ValueMetaVariable;
            //}
            if (m_MetaVariableDict.ContainsKey(name))
            {
                return m_MetaVariableDict[name];
            }
            //return m_OrgMetaDefineType.metaClass.GetMetaMemberVariableByName(name);
            return null;
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_ContentMetaVariable.name);
            if (m_ContentMetaVariable.isArray)
            {
                sb.Append("[");
                //sb.Append(m_DefineMetaType.ToFormatString());
                sb.Append(m_Name);
                sb.Append("]");
                //sb.Append(m_Express.ToFormatString());
            }
            else
            {

            }

            return sb.ToString();
        }
        public override string ToString()
        {
            return m_Name;
        }
    }
}
