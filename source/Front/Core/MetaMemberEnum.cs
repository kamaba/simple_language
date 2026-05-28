//****************************************************************************
//  File:      MetaMemberEnum.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: enum's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public sealed class MetaMemberEnum : MetaMemberVariable
    {
        public MetaConstExpressNode enumValueConstExpressNode => m_Express as MetaConstExpressNode;
        private bool isExplicitAssign => m_IsExplicitAssign;

        private bool m_IsExplicitAssign = false;       

        //public MetaMemberEnum(MetaClass mc, string name, MetaClass extendClass) : base(mc, name)
        //{
        //    if (extendClass != null)
        //    {
        //        m_DefineMetaType = new MetaType(extendClass);
        //        m_RealMetaType = new MetaType(extendClass);
        //        SetIsDefineMetaType(true);
        //    }
        //    else
        //    {
        //        m_DefineMetaType = new MetaType(CoreMetaClassManager.memberMetaClass);
        //        m_RealMetaType = new MetaType(CoreMetaClassManager.memberMetaClass);
        //        SetIsDefineMetaType(true);
        //    }
        //}
        public MetaMemberEnum(MetaEnum mc, FileMetaMemberVariable fmmv ) : base()
        {
            m_OwnerMetaBase = mc;
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            AddPingToken(fmmv.nameToken);
            m_Index = mc.metaMemberVariableDict.Count;
            m_FromType = EFromType.Code;
            m_VariableFrom = EVariableFrom.EnumMember;
            m_Permission = EPermission.Public;
            m_IsConst = true;
            m_IsStatic = false;
            if (fmmv.mutToken != null)
            {
                m_IsConst = false;
            }
            if (fmmv.staticToken != null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Enum 中不允许使用 static 关键字，枚举值的静态语义由系统处理!!");
            }
            if (m_FileMetaMemeberVariable.permissionToken?.type != null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token, "Error Enum中，不允许使用public/private等权限关键字!!");
            }

            SetOwnerMetaBase(mc);
        }
        public override void ParseDefineMetaType()
        {
            var me = ownerMetaEnum;
            if (me == null)
            {
                return;
            }
            if (me.extendMetaData != null)
            {
                m_DefineMetaType = new MetaType(me.extendMetaData);
                m_IsDefineMetaType = true;
            }
            else if (me.extendClass != null)
            {
                m_DefineMetaType = new MetaType(me.extendClass);
                m_IsDefineMetaType = true;
            }
            else
            {
                Log.AddMetaCoreLog( LID.MetaCoreAssertShowMessage, m_Token, "Error Enum成员没有找到定义类型，无法解析!!");
            }
        }
        public void SetIsExplicitAssign(bool value)
        {
            m_IsExplicitAssign = value;
        }
        public override void CalcParseLevel()
        {
        }
        public override void CreateMetaExpress()
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
                        ownerMetaBase = ownerMetaBase,
                        equalMetaVariable = this,
                        ownerMBS = m_OwnerMetaBlockStatements,
                        parsefrom = EParseFrom.MemberVariableExpress
                    };
                    m_Express = ExpressManager.CreateExpressNodeByCEP(cep);

                    if (m_Express == null)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 没有解析到Express的内容 在MetaMemberData 里边 372");
                    }
                    else
                    {
                        m_IsExplicitAssign = false;
                    }
                }
            }
        }
        public override bool ParseMetaExpress()
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                m_Express.CalcReturnType();           
                return true;
            }
            return true;
        }
        /// <summary>
        /// 保持 real 为成员值表达式类型；包装成 Core.Member 后 m_Express 会变，不应再以 m_Express 推导 real。
        /// </summary>
        public override void ParseRealMetaType()
        {
            if (m_Express != null)
            {
                var ret = m_Express.GetReturnMetaType();
                if (ret != null)
                {
                    SetRealMetaType(new MetaType(ret));
                }
            }
        }

        public MetaMemberVariable WrapAsEnumMemberObjectExpress()
        {
            if (m_Express == null)
            {
                return null;
            }

            var ownerEnum = ownerMetaEnum;
            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (ownerEnum == null || memberClass == null)
            {
                return null;
            }

            var memberType = new MetaType(memberClass);
            var wrappedNewObject = new MetaNewObjectExpressNode(memberType, ownerEnum, m_OwnerMetaBlockStatements, null);
            FillMemberNewObjectAssignList(
                wrappedNewObject,
                m_OwnerMetaBlockStatements,
                ownerEnum,
                m_Express,
                m_Name,
                m_Index);

            wrappedNewObject.CalcReturnType();

            var exportVariable = new MetaMemberVariable(ownerEnum, m_Name);
            exportVariable.SetVariableFrom(EVariableFrom.EnumMember);
            exportVariable.SetIndex(m_Index);
            exportVariable.SetIsDefineMetaType(true);
            exportVariable.SetMetaDefineType(memberType);
            exportVariable.SetRealMetaType(new MetaType(memberType));
            exportVariable.SetExpress(wrappedNewObject);

            var exportList = ownerEnum.exportMemberVariableList;
            int existsIndex = exportList.FindIndex(v => v != null && v.name == exportVariable.name);
            if (existsIndex >= 0)
            {
                exportList[existsIndex] = exportVariable;
            }
            else
            {
                exportList.Add(exportVariable);
            }

            return exportVariable;
        }
        /// <summary>
        /// 为 enum.values 数组生成一项：new Core.Member() 后按 name、value、index 顺序赋值（与 IRNewExpress 对象初始化一致）。
        /// 使用 Member 类模板上的 MetaMemberVariable，保证 IR 里按 hash 能匹配到字段。
        /// </summary>
        public MetaExpressNodeBase CreateValuesArrayElementExpress()
        {
            var valueExpr = m_Express;
            if (valueExpr == null)
                return null;
            // 已包装成 Member 时，m_EnumValueExpress 仍为原始值表达式；若仅有包装节点则无法再拆值
            if (m_Express == null && valueExpr is MetaNewObjectExpressNode mnoe
                && mnoe.GetReturnMetaType()?.metaClass == CoreMetaClassManager.memberMetaClass)
            {
                return null;
            }

            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (memberClass == null)
                return null;

            var memberType = new MetaType(memberClass);
            var newMember = new MetaNewObjectExpressNode(memberType, ownerMetaClass, m_OwnerMetaBlockStatements, null );
            FillMemberNewObjectAssignList(newMember, m_OwnerMetaBlockStatements, m_OwnerMetaBase, valueExpr, m_Name, m_Index);
            return newMember;
        }
        private void FillMemberNewObjectAssignList(
            MetaNewObjectExpressNode newMember,
            MetaBlockStatements mbs,
            MetaBase owmb,
            MetaExpressNodeBase valueExpr,
            string memberName,
            int memberIndex)
        {
            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (newMember?.assignStatementsList == null || valueExpr == null || memberClass == null)
            {
                return;
            }

            var nameMv = memberClass.GetMetaMemberVariableByName("name");
            var valueMv = memberClass.GetMetaMemberVariableByName("value");
            var indexMv = memberClass.GetMetaMemberVariableByName("index");
            if (nameMv == null || valueMv == null || indexMv == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Core.Member 缺少 name/value/index 字段，无法构造 Member 初始化");
                return;
            }

            var nameExpr = new MetaConstExpressNode(EType.String, memberName ?? string.Empty);
            nameExpr.CalcReturnType();
            var indexExpr = new MetaConstExpressNode(EType.Int32, memberIndex);
            indexExpr.CalcReturnType();

            var list = newMember.assignStatementsList;
            list.Add(new MetaBraceAssignStatements(nameMv, mbs, owmb, nameExpr));
            list.Add(new MetaBraceAssignStatements(valueMv, mbs, owmb, valueExpr));
            list.Add(new MetaBraceAssignStatements(indexMv, mbs, owmb, indexExpr));
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < realDeep; i++)
                sb.Append(Global.tabChar);

            if (isConst)
            {
                sb.Append("const ");
            }
            if (m_Express != null)
            {
                sb.Append(" = ");
                sb.Append(m_Express.ToFormatString());
            }
            sb.Append(";");

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_IsConst == false )
            {
                sb.Append("mut ");
            }
            sb.Append(m_Name);
            if (m_Express != null)
            {
                sb.Append(" = ");
                sb.Append(m_Express.ToFormatString());
            }
            sb.Append(";");

            return sb.ToString();
        }
    }
}
