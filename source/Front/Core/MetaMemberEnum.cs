//****************************************************************************
//  File:      MetaMemberEnum.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: enum's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaMemberEnum : MetaMemberVariable
    {
        private bool isExplicitAssign => m_IsExplicitAssign;
        private bool m_IsExplicitAssign = false;

        public MetaMemberEnum(MetaClass mc, FileMetaMemberVariable fmmv, MetaClass extendClass ) : base()
        {
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            AddPingToken(fmmv.nameToken);
            m_Index = mc.metaMemberVariableDict.Count;
            m_FromType = EFromType.Code;
            m_DefineMetaType = new MetaType(extendClass);
            
            if (extendClass == CoreMetaClassManager.byteMetaClass
                  || extendClass == CoreMetaClassManager.sbyteMetaClass
                  || extendClass == CoreMetaClassManager.int16MetaClass
                  || extendClass == CoreMetaClassManager.uint16MetaClass
                  || extendClass == CoreMetaClassManager.int32MetaClass
                  || extendClass == CoreMetaClassManager.uint32MetaClass
                  || extendClass == CoreMetaClassManager.int64MetaClass
                  || extendClass == CoreMetaClassManager.uint64MetaClass
                  || extendClass == CoreMetaClassManager.stringMetaClass)
            {
                SetIsDefineMetaType(true);
            }
            m_IsConst = fmmv.mutToken == null;
            if (m_IsConst == false)
                m_IsStatic = true;// enum 成员全部为static
            else
                m_IsStatic = false;
            m_VariableFrom = EVariableFrom.Member;
            if (fmmv.staticToken != null)
            {
                Log.AddInStructMeta(EError.None, "Error ENum中，不允许有静态关键字，而是全部是静态关键字!!");
            }

            if (string.IsNullOrEmpty(m_Name))
            {
                Log.AddInStructMeta(EError.None, "没有找到定义变量名称!");
                m_Name = "Error_" + GetHashCode().ToString();
            }
            if (m_FileMetaMemeberVariable.permissionToken?.type != null)
            {
                Log.AddInStructMeta(EError.None, "Error Enum中，不允许使用public/private等权限关键字!!");
                m_Permission = CompilerUtil.GetPerMissionByType(m_FileMetaMemeberVariable.permissionToken.type);
            }
            else
            {
                if (m_Name[0] == '_')
                {
                    m_Permission = EPermission.Private;
                }
            }

            SetOwnerMetaClass(mc);
        }
        public override void ParseDefineMetaType()
        {
            if (m_FileMetaMemeberVariable != null)
            {
                if (m_FileMetaMemeberVariable.express != null)
                {
                    m_IsExplicitAssign = true;
                    CreateExpressParam cep = new CreateExpressParam()
                    {
                        fme = m_FileMetaMemeberVariable.express,
                        metaType = m_DefineMetaType,
                        ownerMetaClass = m_OwnerMetaClass,
                        equalMetaVariable = this,
                        ownerMBS = m_OwnerMetaBlockStatements,
                        parsefrom = EParseFrom.MemberVariableExpress
                    };
                    m_Express = ExpressManager.CreateExpressNodeByCEP(cep);

                    if (m_Express == null)
                    {
                        Log.AddInStructMeta(EError.None, "Error 没有解析到Express的内容 在MetaMemberData 里边 372");
                    }
                else
                {
                    m_IsExplicitAssign = false;
                }
                }

                // enum member always has a define type; default real type follows define type until expression parsed.
                if (m_DefineMetaType == null)
                {
                    m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
                }
                if (m_RealMetaType == null)
                {
                    m_RealMetaType = new MetaType(m_DefineMetaType.metaClass);
                }

                SetIsDefineMetaType(m_IsExplicitAssign);
            }
        }
        public void SetIsExplicitAssign(bool value)
        {
            m_IsExplicitAssign = value;
        }
        public override bool ParseMetaExpress()
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                m_Express.CalcReturnType();

                m_RealMetaType = new MetaType(m_Express.GetReturnMetaClass());
                return true;
            }
            else
            {
                Debug.Assert(false, "必须给出定义");
                return false;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);

            sb.Append(permission.ToFormatString() + " ");
            if (isConst)
            {
                sb.Append("const ");
            }
            if (isStatic)
            {
                sb.Append("static ");
            }
            sb.Append(base.ToFormatString());
            if (m_Express != null)
            {
                sb.Append(" = ");
                sb.Append(m_Express.ToFormatString());
            }
            sb.Append(";");

            return sb.ToString();
        }
        public string ToTokenString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_FileMetaMemeberVariable.nameToken.sourceBeginLine + " 与父类的Token位置: "
                    + m_FileMetaMemeberVariable.nameToken.sourceBeginLine.ToString());

            return sb.ToString();
        }
        //public MetaMemberEnum(MetaClass mc, string _name)
        //{
        //    m_Name = _name;
        //    m_FromType = EFromType.Manual;
        //    m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
        //    m_IsInnerDefine = true;
        //    m_VariableFrom = EVariableFrom.Static;

        //    SetOwnerMetaClass(mc);
        //} 
        //public MetaMemberEnum( MetaClass ownerMc, string _name, MetaTemplate mt )
        //{
        //    m_Name = _name;
        //    m_FromType = EFromType.Manual;
        //    m_DefineMetaType = new MetaType( mt );
        //    m_IsInnerDefine = true;
        //    m_VariableFrom = EVariableFrom.Static;

        //    SetOwnerMetaClass(ownerMc);
        //}
        //public MetaMemberEnum(MetaClass mc, string _name, MetaClass _defineTypeClass )
        //{
        //    m_Name = _name;
        //    m_IsInnerDefine = true;
        //    m_FromType = EFromType.Manual;
        //    m_DefineMetaType = new MetaType(_defineTypeClass);
        //    m_DefineMetaType.SetMetaClass(_defineTypeClass);
        //    m_VariableFrom = EVariableFrom.Static;

        //    SetOwnerMetaClass(mc);
        //}
        //public MetaMemberEnum(MetaGenTemplateClass mtc, MetaMemberEnum mmv, List<MetaGenTemplate> mgt) : base(mmv)
        //{
        //    m_MetaGenTemplateList = mgt;
        //    m_Name = mmv.m_Name;
        //    m_IsInnerDefine = mmv.m_IsInnerDefine;
        //    m_FromType = mmv.m_FromType;
        //    m_DefineMetaType = mmv.m_DefineMetaType;
        //    m_VariableFrom = EVariableFrom.Static;
        //    m_PintTokenList = mmv.m_PintTokenList;

        //    SetOwnerMetaClass(mtc);
        //}

        //public override string ToFormatString()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    switch (m_FileMetaMemeberData.DataType)
        //    {
        //        case FileMetaMemberData.EMemberDataType.NameClass:
        //            {
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.AppendLine(m_Name);
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.AppendLine("{");
        //                foreach (var v in m_MetaMemberDataDict)
        //                {
        //                    sb.AppendLine(v.Value.ToFormatString());
        //                }
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append("}");

        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.Array:
        //            {
        //                int i = 0;
        //                for (i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append(m_Name + " = [");
        //                i = 0;
        //                foreach (var v in m_MetaMemberDataDict)
        //                {
        //                    sb.Append(v.Value.ToFormatString());
        //                    if (i < m_MetaMemberDataDict.Count - 1)
        //                        sb.Append(",");
        //                    i++;
        //                }
        //                sb.Append("]");
        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.NoNameClass:
        //            {
        //                sb.AppendLine();
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.AppendLine("{");
        //                foreach (var v in m_MetaMemberDataDict)
        //                {
        //                    sb.AppendLine(v.Value.ToFormatString());
        //                }
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append("}");
        //                //if( m_End )
        //                //{
        //                //    sb.AppendLine();
        //                //}
        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.KeyValue:
        //            {
        //                for (int i = 0; i < realDeep; i++)
        //                    sb.Append(Global.tabChar);
        //                sb.Append(m_Name + " = " + m_Express.ToFormatString() + ";");
        //            }
        //            break;
        //        case FileMetaMemberData.EMemberDataType.Value:
        //            {
        //                sb.Append(m_Express.ToFormatString());
        //            }
        //            break;
        //    }
        //    return sb.ToString();
        //}
        //------------------------------------end-----------------------------------------------//
    }
}
