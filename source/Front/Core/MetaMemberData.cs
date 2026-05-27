//****************************************************************************
//  File:      MetaMemberData.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2025/5/6 12:00:00
//  Description: class's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum EMemberDataType
    {
        None,
        ConstValue,
        MemberData,
        MemberArray,
        MemberClass,
    }
    public sealed class MetaMemberData : MetaVariable
    {
        public int index => m_Index;
        public EMemberDataType memberDataType => m_MemberDataType;
        public MetaExpressNodeBase expressNode => m_Express;

        private EMemberDataType m_MemberDataType = EMemberDataType.None;
        private MetaExpressNodeBase m_Express = null;
        private int m_Index = -1;
        private bool m_IsWithName = false;

        private FileMetaMemberData m_FileMetaMemeberData = null;
        private FileMetaSyntax m_FileMetaAssignSyntax = null;

        private MetaMemberData()
        {
            m_VariableFrom = EVariableFrom.DataMember;
        }
        public MetaMemberData(MetaData dmt, FileMetaSyntax fms, MetaBase ownerbase, MetaBlockStatements mbs, int index  )
        {
            m_DefineMetaType = new MetaType(dmt);
            SetOwnerMetaBase(ownerbase);
            m_OwnerMetaBlockStatements = mbs;
            m_IsConst = dmt.isConst;
            m_VariableFrom = EVariableFrom.DataMember;
            m_Token = fms.token;
            m_Name = fms.name;
            m_FileMetaAssignSyntax = fms;
            m_Index = index;
        }
        public MetaMemberData(MetaData mc, FileMetaMemberData fmmd, MetaBase owmb, int index, bool isStatic )
        {
            m_FileMetaMemeberData = fmmd;
            m_Name = fmmd.name;
            m_Index = index;
            m_IsStatic = isStatic;
            m_IsWithName = m_FileMetaMemeberData.isWithName;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            SetOwnerMetaBase(owmb);
            m_IsConst = mc.isConst || fmmd.isConst;
            m_Token = fmmd.nameToken;
            m_VariableFrom = EVariableFrom.DataMember;
            if (m_IsWithName)
            {
                m_Name = m_FileMetaMemeberData.name;
            }
            else
            {
                m_Name = m_Index.ToString();
            }
        }
        public MetaMemberData(MetaData owner, string name, int index)
        {
            this.m_Name = name;
            this.m_Index = index;
            this.m_IsWithName = true;
            this.m_DefineMetaType = new MetaType(owner);
            this.m_RealMetaType = new MetaType(this.m_DefineMetaType);
            this.m_IsDefineMetaType = true;
            this.SetOwnerMetaBase(owner);
            this.m_IsConst = owner?.isConst ?? false;
            this.m_MemberDataType = EMemberDataType.MemberData;
            this.m_VariableFrom = EVariableFrom.DataMember;
        }

        public static MetaMemberData CreateConst(MetaData owner, string name, int index, MetaConstExpressNode constExpress)
        {
            var mmd = new MetaMemberData();
            mmd.m_Name = name;
            mmd.m_Index = index;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;
            mmd.m_DefineMetaType = new MetaType(constExpress?.GetReturnMetaClass() ?? CoreMetaClassManager.objectMetaClass);
            mmd.m_RealMetaType = new MetaType(mmd.m_DefineMetaType);
            mmd.m_IsDefineMetaType = true;
            mmd.SetOwnerMetaBase(owner);
            mmd.m_IsConst = owner?.isConst ?? false;
            mmd.m_IsStatic = false;
            mmd.m_Express = constExpress;
            mmd.m_MemberDataType = EMemberDataType.ConstValue;
            mmd.AddPingToken(constExpress.token);
            return mmd;
        }

        public static MetaMemberData CreateArray(MetaData owner, string name, int index, MetaType elementType = null, int length = -1)
        {
            var mmd = new MetaMemberData();
            mmd.m_Name = name;
            mmd.m_Index = index;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;

            var et = elementType ?? new MetaType(CoreMetaClassManager.objectMetaClass);
            var arrType = new MetaType(CoreMetaClassManager.arrayMetaClass, new List<MetaType>() { et });
            arrType.SetArrayLength(length);

            mmd.m_DefineMetaType = arrType;
            mmd.m_RealMetaType = new MetaType(arrType);
            mmd.m_IsDefineMetaType = true;
            mmd.SetOwnerMetaBase(owner);
            mmd.m_IsConst = owner?.isConst ?? false;
            mmd.m_MemberDataType = EMemberDataType.MemberArray;
            return mmd;
        }
        public static MetaMemberData CreateDeclared(MetaData owner, string name, int index, MetaType defineMetaType, bool isDeclaredType)
        {
            var mmd = new MetaMemberData();
            mmd.m_Name = string.IsNullOrEmpty(name) ? index.ToString() : name;
            mmd.m_Index = index;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;
            mmd.SetOwnerMetaBase(owner);
            mmd.m_IsConst = owner?.isConst ?? false;

            var finalType = defineMetaType ?? new MetaType(CoreMetaClassManager.objectMetaClass);
            mmd.m_DefineMetaType = new MetaType(finalType);
            mmd.m_IsDefineMetaType = isDeclaredType;
            // Keep RealMetaType non-null even for inferred/object placeholders,
            // otherwise downstream code may read define/real shape and hit null.
            mmd.m_RealMetaType = new MetaType(finalType);

            if (finalType.isData)
            {
                mmd.m_MemberDataType = EMemberDataType.MemberData;
            }
            else if (finalType.IsArray())
            {
                mmd.m_MemberDataType = EMemberDataType.MemberArray;
            }
            else if (TypeManager.IsCoreMetaType(finalType))
            {
                mmd.m_MemberDataType = EMemberDataType.ConstValue;
            }
            else
            {
                mmd.m_MemberDataType = EMemberDataType.MemberClass;
            }
            return mmd;
        }

        /// <summary>
        /// 将前端按需构造的 <see cref="MetaMemberVariable"/> 并入所属 <see cref="MetaData"/> 的唯一成员表（<see cref="MetaData.metaMemberDataDict"/>），不再使用并行字典。
        /// </summary>
        public static MetaMemberData CreateFromInjectedMemberVariable(MetaData owner, MetaMemberVariable mmv, int fallbackIndex)
        {
            if (mmv == null || owner == null)
            {
                return null;
            }

            var mmd = new MetaMemberData();
            mmd.m_Name = mmv.name;
            mmd.m_Index = mmv.index >= 0 ? mmv.index : fallbackIndex;
            mmd.m_IsWithName = true;
            mmd.m_VariableFrom = EVariableFrom.DataMember;
            mmd.SetOwnerMetaBase(owner);
            mmd.m_IsStatic = mmv.isStatic;
            mmd.m_IsConst = mmv.isConst || owner.isConst;
            mmd.m_Permission = mmv.permission;

            var def = mmv.defineMetaType ?? new MetaType(CoreMetaClassManager.objectMetaClass);
            mmd.m_DefineMetaType = new MetaType(def);
            mmd.m_IsDefineMetaType = mmv.isDefineMetaType;

            if (mmv.realMetaType != null)
            {
                mmd.m_RealMetaType = new MetaType(mmv.realMetaType);
            }
            else
            {
                mmd.m_RealMetaType = new MetaType(mmd.m_DefineMetaType);
            }

            mmd.m_Express = mmv.express;

            if (def.isData)
            {
                mmd.m_MemberDataType = EMemberDataType.MemberData;
            }
            else if (def.IsArray())
            {
                mmd.m_MemberDataType = EMemberDataType.MemberArray;
            }
            else
            {
                mmd.m_MemberDataType = EMemberDataType.MemberClass;
            }

            if (mmv.token != null)
            {
                mmd.m_Token = mmv.token;
            }

            var plist = mmv.pingTokenList;
            if (plist != null)
            {
                for (int i = 0; i < plist.Count; i++)
                {
                    mmd.AddPingToken(plist[i]);
                }
            }

            return mmd;
        }
        public void SetIndex(int index) { m_Index = index; }
        public void SetExpress( MetaExpressNodeBase meb )
        {
            this.m_Express = meb;
        }
        internal static MetaExpressNodeBase CreateExpressFromFileMetaMemberData(
            FileMetaMemberData fmmd,
            MetaBase owner,
            MetaBlockStatements mbs,
            MetaType elementHint)
        {
            if (fmmd == null)
            {
                return null;
            }

            switch (fmmd.DataType)
            {
                case FileMetaMemberData.EMemberDataType.Data:
                    return new MetaAnonDataExpressNode(fmmd, owner, mbs, null);
                case FileMetaMemberData.EMemberDataType.Array:
                    return MetaArrayExpressNode.CreateFromFileMetaMemberData(fmmd, owner, mbs, elementHint);
                case FileMetaMemberData.EMemberDataType.Class:
                    if (fmmd.fileMetaCallTermValue?.callLink == null)
                    {
                        return null;
                    }
                    return new MetaCallLinkExpressNode(
                        fmmd.fileMetaCallTermValue.callLink,
                        owner,
                        mbs,
                        null);
                case FileMetaMemberData.EMemberDataType.ConstValue:
                    if (fmmd.fileMetaConstValue == null)
                    {
                        return null;
                    }
                    var mcen = new MetaConstExpressNode(owner, mbs, fmmd.fileMetaConstValue);
                    mcen.Parse(new AllowUseSettings());
                    mcen.CalcReturnType();
                    return mcen;
                default:
                    return null;
            }
        }
        public override void SetDeep(int deep)
        {
            m_Deep = deep;
        }
        public override void CalcParseLevel()
        {
            if (isConst)
            {
                parseLevel = s_ConstLevel;
                s_ConstLevel = s_ConstLevel + 10000;
            }
            else if (isStatic)
            {
                if (parseLevel == -1)
                {
                    if (m_DefineMetaType != null)
                    {
                        parseLevel = s_IsHaveRetStaticLevel;
                        s_IsHaveRetStaticLevel = s_IsHaveRetStaticLevel + 100000;
                    }
                    else
                    {
                        parseLevel = s_NoHaveRetStaticLevel;
                        s_NoHaveRetStaticLevel = s_NoHaveRetStaticLevel + 100000;
                    }

                }
            }
            else
            {
                if (parseLevel == -1)
                {
                    if (m_DefineMetaType != null)
                    {
                        parseLevel = s_DefineMetaTypeLevel;
                        s_DefineMetaTypeLevel = s_DefineMetaTypeLevel + 1000000;
                    }
                    else
                    {
                        parseLevel = s_ExpressLevel;
                        s_ExpressLevel = s_ExpressLevel + 1000000;
                    }
                }
            }

            if (m_Express != null)
            {
                ExpressManager.CalcParseLevel(parseLevel, m_Express);
            }
        }
        public override void CreateMetaExpress()
        {
            if (m_RealMetaType != null) return; //认为已经解析过了
            if (m_FileMetaMemeberData != null)
            {
                switch (m_FileMetaMemeberData.DataType)
                {
                    case FileMetaMemberData.EMemberDataType.Data:
                        {
                            m_MemberDataType = EMemberDataType.MemberData;
                            m_Express = CreateExpressFromFileMetaMemberData(
                                m_FileMetaMemeberData,
                                ownerMetaBase,
                                m_OwnerMetaBlockStatements,
                                m_DefineMetaType);
                            //m_Express.Parse(new AllowUseSettings());
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.Class:    // data Data{ $childData = Class1{}$ }
                        {
                            m_MemberDataType = EMemberDataType.MemberClass;
                            m_Express = new MetaCallLinkExpressNode(
                                m_FileMetaMemeberData.fileMetaCallTermValue.callLink,
                                ownerMetaBase,
                                m_OwnerMetaBlockStatements,
                                this);
                            //m_Express.Parse(new AllowUseSettings());
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.Array:      // data Data{ $childArray = [  ]$ }
                        {
                            m_MemberDataType = EMemberDataType.MemberArray;
                            MetaType elementHint = null;
                            if (m_DefineMetaType != null && m_DefineMetaType.IsArray()
                                && m_DefineMetaType.defineTemplateMetaTypeList.Count > 0)
                            {
                                elementHint = m_DefineMetaType.defineTemplateMetaTypeList[0];
                            }
                            m_Express = CreateExpressFromFileMetaMemberData(
                                m_FileMetaMemeberData,
                                ownerMetaBase,
                                m_OwnerMetaBlockStatements,
                                elementHint);
                            //m_Express.Parse(new AllowUseSettings());
                        }
                        break;
                    case FileMetaMemberData.EMemberDataType.ConstValue:  // data const    a = "aaa"
                        {
                            m_MemberDataType = EMemberDataType.ConstValue;
                            if (m_FileMetaMemeberData.fileMetaConstValue != null)
                            {
                                m_Express = new MetaConstExpressNode(ownerMetaBase, null, m_FileMetaMemeberData.fileMetaConstValue);
                                m_Express.Parse(new AllowUseSettings());
                                m_Express.CalcReturnType();
                                TryNormalizeDataConstNumericLiteralType();
                                var md = m_Express.GetReturnMetaType();
                                this.m_DefineMetaType = md;
                                this.m_RealMetaType = md;
                            }
                        }
                        break;
                    default:
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_FileMetaMemeberData.token, "");
                        }
                        break;
                }
            }

            if( m_FileMetaAssignSyntax != null )
            {
                if( m_FileMetaAssignSyntax is FileMetaOpAssignSyntax fmos )
                {
                    this.m_Token = fmos.token;
                    CreateExpressParam cep = new CreateExpressParam();
                    cep.fme = fmos.express;
                    cep.equalMetaVariable = null;
                    cep.metaType = null;
                    cep.ownerMBS = m_OwnerMetaBlockStatements;
                    cep.ownerMetaBase = m_OwnerMetaBase;

                    m_Express = ExpressManager.CreateExpressNodeByCEP(cep);
                }
                else if( m_FileMetaAssignSyntax is FileMetaCallSyntax fmcs )
                {
                    this.m_Token = fmcs.token;
                }
            }
        }
        public override bool ParseMetaExpress()
        {
            if (m_RealMetaType != null) return true; //认为已经解析过了
            if (this.m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
            }
            return true;
        }

        private void TryNormalizeDataConstNumericLiteralType()
        {
            if (m_Express is not MetaConstExpressNode constExpress)
            {
                return;
            }

            if (TryNormalizeDataConstIntegerLiteralType(constExpress))
            {
                return;
            }

            TryNormalizeDataConstFloatingLiteralType(constExpress);
        }

        private bool TryNormalizeDataConstIntegerLiteralType(MetaConstExpressNode constExpress)
        {
            if (constExpress == null)
            {
                return false;
            }

            var constTerm = m_FileMetaMemeberData?.fileMetaConstValue;
            var token = constTerm?.token ?? constExpress.token;
            if (token == null || (token.type != ETokenType.Number && token.type != ETokenType.NumberReal))
            {
                return false;
            }

            if (HasExplicitIntegerSuffix(token))
            {
                return false;
            }

            if (!TryGetUnsignedIntegerMagnitude(token.lexeme, out ulong magnitude))
            {
                return false;
            }

            bool isNegative = constTerm?.plusMinusToken?.type == ETokenType.Minus;
            if (isNegative)
            {
                if (magnitude <= (ulong)int.MaxValue + 1UL)
                {
                    constExpress.SetConstValue(EType.Int32, -(int)magnitude);
                    constExpress.CalcReturnType();
                    return true;
                }
                else if (magnitude <= (ulong)long.MaxValue + 1UL)
                {
                    constExpress.SetConstValue(EType.Int64, -(long)magnitude);
                    constExpress.CalcReturnType();
                    return true;
                }
                return false;
            }

            if (magnitude <= int.MaxValue)
            {
                constExpress.SetConstValue(EType.Int32, (int)magnitude);
                constExpress.CalcReturnType();
                return true;
            }
            else if (magnitude <= (ulong)long.MaxValue)
            {
                constExpress.SetConstValue(EType.Int64, (long)magnitude);
                constExpress.CalcReturnType();
                return true;
            }

            return false;
        }

        private bool TryNormalizeDataConstFloatingLiteralType(MetaConstExpressNode constExpress)
        {
            if (constExpress == null)
            {
                return false;
            }

            var token = m_FileMetaMemeberData?.fileMetaConstValue?.token ?? constExpress.token;
            if (token == null || (token.type != ETokenType.Number && token.type != ETokenType.NumberReal))
            {
                return false;
            }

            if (HasExplicitFloatingSuffix(token))
            {
                return false;
            }

            if (!TryGetFloatingMagnitude(token, out var magnitude))
            {
                return false;
            }

            if (double.IsNaN(magnitude))
            {
                return false;
            }

            if (Math.Abs(magnitude) > float.MaxValue || float.IsInfinity((float)magnitude))
            {
                constExpress.SetConstValue(EType.Float64, magnitude);
                constExpress.CalcReturnType();
                return true;
            }

            constExpress.SetConstValue(EType.Float32, (float)magnitude);
            constExpress.CalcReturnType();
            return true;
        }

        private static bool TryGetFloatingMagnitude(Token token, out double magnitude)
        {
            magnitude = 0d;
            if (token == null)
            {
                return false;
            }

            if (token.lexeme is float fv)
            {
                magnitude = fv;
                return true;
            }

            if (token.lexeme is double dv)
            {
                magnitude = dv;
                return true;
            }

            if (TryReadNumericLiteralRawText(token, out var raw))
            {
                return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out magnitude);
            }

            return false;
        }

        private static bool TryGetUnsignedIntegerMagnitude(object? valueObj, out ulong magnitude)
        {
            magnitude = 0UL;
            return valueObj switch
            {
                sbyte sv when sv >= 0 => (magnitude = (ulong)sv) >= 0,
                byte bv => (magnitude = bv) >= 0,
                short sv when sv >= 0 => (magnitude = (ulong)sv) >= 0,
                ushort usv => (magnitude = usv) >= 0,
                int iv when iv >= 0 => (magnitude = (ulong)iv) >= 0,
                uint uiv => (magnitude = uiv) >= 0,
                long lv when lv >= 0 => (magnitude = (ulong)lv) >= 0,
                ulong ulv => (magnitude = ulv) >= 0,
                _ => false,
            };
        }

        private static bool HasExplicitIntegerSuffix(Token token)
        {
            if (!TryReadNumericLiteralRawText(token, out var raw))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            raw = raw.Trim();
            if (raw.Length > 0 && (raw[0] == '+' || raw[0] == '-'))
            {
                raw = raw.Substring(1);
            }

            int i = 0;
            while (i < raw.Length && char.IsDigit(raw[i]))
            {
                i++;
            }

            if (i == 0 || i >= raw.Length)
            {
                return false;
            }

            if (raw[i] == '.')
            {
                return true;
            }

            return char.IsLetter(raw[i]);
        }

        private static bool HasExplicitFloatingSuffix(Token token)
        {
            if (!TryReadNumericLiteralRawText(token, out var raw))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            raw = raw.Trim();
            if (raw.Length > 0 && (raw[0] == '+' || raw[0] == '-'))
            {
                raw = raw.Substring(1);
            }

            int i = 0;
            while (i < raw.Length && char.IsDigit(raw[i]))
            {
                i++;
            }

            if (i < raw.Length && raw[i] == '.')
            {
                i++;
                while (i < raw.Length && char.IsDigit(raw[i]))
                {
                    i++;
                }
            }

            if (i >= raw.Length)
            {
                return false;
            }

            return char.IsLetter(raw[i]);
        }

        private static bool TryReadNumericLiteralRawText(Token token, out string raw)
        {
            raw = string.Empty;
            if (token == null || string.IsNullOrEmpty(token.path))
            {
                return false;
            }

            var path = token.path;
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path);
            }
            if (!File.Exists(path))
            {
                return false;
            }

            var lines = File.ReadAllLines(path);
            int lineIndex = token.sourceBeginLine - 1;
            if (lineIndex < 0 || lineIndex >= lines.Length)
            {
                return false;
            }

            var line = lines[lineIndex];
            int start = token.sourceBeginChar;
            if (start < 0 || start >= line.Length)
            {
                return false;
            }

            var sb = new StringBuilder();
            for (int i = start; i < line.Length; i++)
            {
                char c = line[i];
                if (char.IsWhiteSpace(c)
                    || c == ','
                    || c == ';'
                    || c == ')'
                    || c == ']'
                    || c == '}'
                    || c == ':'
                    || c == '+'
                    || c == '-'
                    || c == '*'
                    || c == '/')
                {
                    break;
                }
                sb.Append(c);
            }

            raw = sb.ToString();
            return raw.Length > 0;
        }

        private void SyncMemberDataTypeByMetaType(MetaType mt)
        {
            if (mt == null)
            {
                return;
            }

            if (mt.isData)
            {
                m_MemberDataType = EMemberDataType.MemberData;
            }
            else if (mt.IsArray())
            {
                m_MemberDataType = EMemberDataType.MemberArray;
            }
            else if( TypeManager.IsCoreMetaType(mt ) )
            {
                m_MemberDataType = EMemberDataType.ConstValue;
            }
            else
            {
                m_MemberDataType = EMemberDataType.MemberClass;
            }
        }

        public override void ParseRealMetaType()
        {
            if (m_Express == null)
            {
                return;
            }

            TryNormalizeDataConstNumericLiteralType();
            m_Express.CalcReturnType();
            var convertedExpress = ExpressManager.ConvertNewExpress(m_Express, m_Express.GetReturnMetaType(), this);
            if (convertedExpress != m_Express )
            {
                m_Express = convertedExpress;
                m_Express.CalcReturnType();
            }

            if (m_RealMetaType == null)
            {
                m_DefineMetaType = m_Express.GetReturnMetaType();
                if (m_DefineMetaType == null)
                {
                    string tokenText = m_FileMetaMemeberData?.fileMetaCallTermValue?.ToTokenString() ?? m_Name;
                    Log.AddMetaCoreLog(LID.MetaCoreDefineTypeIsNull, m_Token, tokenText);
                    return;
                }
                m_RealMetaType = m_DefineMetaType;
            }

            SyncMemberDataTypeByMetaType(m_Express.GetReturnMetaType() ?? m_DefineMetaType);
        }
        public bool IsIncludeMetaData(MetaData md)
        {
            if (md == null) return false;

            MetaData belongMD = ownerMetaData
                ?? (ownerMetaClass != null ? ClassManager.instance.FindMetaDataByName(ownerMetaClass.allName) : null);
            if (belongMD != null)
            {
                if (belongMD == md)
                {
                    return true;
                }
            }

            return false;
        }
        public string ToFormatString( bool isDynamic )
        {
            StringBuilder sb = new StringBuilder();
            switch (this.m_MemberDataType)
            {
                case EMemberDataType.MemberData:
                    {
                        if (isDynamic)
                        {
                            if (m_IsWithName)
                            {
                                sb.Append(m_Name);
                                sb.Append(" = ");
                            }
                            sb.Append(m_Express != null ? m_Express.ToFormatString() : "{}");
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            if (m_IsWithName)
                            {
                                sb.Append(m_Name);
                                sb.Append(" = ");
                            }
                            sb.Append(m_Express != null ? m_Express.ToFormatString() : "{}");
                        }
                    }
                    break;
                case EMemberDataType.MemberClass:
                    {
                        if (isDynamic)
                        {
                            if (m_IsWithName)
                            {
                                sb.Append(m_Name);
                                sb.Append(" = ");
                            }
                            sb.Append(m_Express.ToFormatString());
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.Append(m_Name + " = " );
                            sb.Append(m_Express.ToFormatString());
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                        }
                    }
                    break;
                case EMemberDataType.MemberArray:
                    {
                        if (isDynamic)
                        {
                            if (m_IsWithName)
                            {
                                sb.Append(m_Name);
                                sb.Append(" = ");
                            }
                            sb.Append(m_Express != null ? m_Express.ToFormatString() : "[]");
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.Append(m_Name + " = ");
                            sb.Append(m_Express != null ? m_Express.ToFormatString() : "[]");
                        }
                    }
                    break;
                case EMemberDataType.ConstValue:
                    {
                        if (isDynamic)
                        {
                            sb.Append(m_Express.ToFormatString());
                        }
                        else
                        {
                            for (int i = 0; i < realDeep; i++)
                                sb.Append(Global.tabChar);
                            sb.Append(m_Name + " = ");
                            sb.Append(m_Express.ToFormatString());
                        }                 
                    }
                    break;
                default:
                    {
                        sb.Append("有没有支持的类型: " + m_MemberDataType.ToString());
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "[" + sb.ToString() +"]" + "暂不支持其它类型1");
                    }
                    break;
            }
            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if ( m_IsWithName )
            {
                sb.Append(m_Name);
                sb.Append(" = ");
            }
            switch (this.m_MemberDataType)
            {
                case EMemberDataType.MemberData:
                    {
                        sb.Append(m_Express != null ? m_Express.ToFormatString() : "{}");
                    }
                    break;
                case EMemberDataType.MemberClass:
                    {
                        MetaType mt = GetFinalMetaType();                        
                        sb.Append(m_Express.ToFormatString());
                    }
                    break;
                case EMemberDataType.MemberArray:
                    {
                        sb.Append(m_Express != null ? m_Express.ToFormatString() : "[]");
                    }
                    break;
                case EMemberDataType.ConstValue:
                    {
                        sb.Append(m_Express.ToFormatString());
                    }
                    break;
                default:
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "error 暂不支持其它类型 123");
                    }
                    break;
            }
            return sb.ToString();
        }
    }
}



/*
public void StructNewObjectData()
{
    var mne = m_Express as MetaNewObjectExpressNode;
    var cne = m_Express as MetaCallLinkExpressNode;
    if (mne != null)
    {
        for (int i = 0; i < mne.assignStatementsList?.Count; i++)
        {

            if (m_MemberDataType == EMemberDataType.MemberData && m_MetaMemberDataDict.Count > 0)
            {
                ResolveAnonymousDataMetaType();
            }
            var asl = mne.assignStatementsList[i];

            if (asl == null) continue;

            MetaMemberData addMmd = null;
            if (m_MemberDataType == EMemberDataType.MemberArray)
            {
                var mcen = asl.expressNode as MetaConstExpressNode;
                var mnoe = asl.expressNode as MetaNewObjectExpressNode;
                if (mcen != null)
                {
                    addMmd = new MetaMemberData(this, i.ToString(), i, mcen);
                    addMmd.ParseMetaExpress();
                }
                if (mnoe != null)
                {
                    addMmd = new MetaMemberData(this, i.ToString(), i, mnoe);
                    addMmd.ParseMetaExpress();
                }
            }
            else if (m_MemberDataType == EMemberDataType.MemberData)
            {
                addMmd = asl.metaMemberData;
            }

            if (addMmd == null) continue;

            if (m_MetaMemberDataDict.ContainsKey(addMmd.name))
            {
                Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, cne.token, "534", cne.token, addMmd.name);
                continue;
            }
            m_MetaMemberDataDict.Add(addMmd.name, addMmd);
        }
    }
    else if (cne != null)
    {
        MetaMemberData addMmd = new MetaMemberData(this, name, 0, cne);
        if (m_MetaMemberDataDict.ContainsKey(addMmd.name))
        {
            Log.AddMetaCoreLog(LID.MetaCoreDefineNameRepeat, cne.token, "534", cne.token, addMmd.name);
        }
        m_MetaMemberDataDict.Add(addMmd.name, addMmd);
    }
    // 子成员（含嵌套匿名 data）须先完成 ParseMetaExpress，父级组装 MetaNewObjectExpressNode / MetaBraceAssignStatements 时才能拿到右值表达式。
    if (m_MemberDataType == EMemberDataType.MemberData && m_MetaMemberDataDict.Count > 0)
    {
        var orderedChildren = new List<MetaMemberData>(m_MetaMemberDataDict.Values);
        orderedChildren.Sort((a, b) => a.dataFieldOrderIndex.CompareTo(b.dataFieldOrderIndex));
        foreach (var child in orderedChildren)
        {
            child.ParseMetaExpress();
        }
    }
    // 匿名/结构化 data 字面量：BuildAnonymousMetaDataType 后，把 m_MetaMemberDataDict 各字段的 expressNode
    //（含子字段若为匿名 MetaData 则已是 MetaNewObjectExpressNode）写入本层 MetaNewObjectExpressNode 的 assign 列表。
    if (m_MemberDataType == EMemberDataType.MemberData && m_MetaMemberDataDict.Count > 0)
    {
        ResolveAnonymousDataMetaType();
    }
    if (m_DefineMetaType != null)
    {
        m_IsDefineMetaType = true;
        if (m_RealMetaType == null)
            m_RealMetaType = new MetaType(m_DefineMetaType);
    }
}
*/