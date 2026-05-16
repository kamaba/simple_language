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
        public MetaExpressNode enumValueExpress => m_EnumValueExpress;
        public MetaConstExpressNode enumValueConstExpressNode => m_EnumValueExpress as MetaConstExpressNode;
        private bool isExplicitAssign => m_IsExplicitAssign;

        private bool m_IsExplicitAssign = false;
        private MetaExpressNode m_EnumValueExpress = null;
        /// <summary>所属用户 enum（与 ownerMetaClass 多为 Core.Enum 模板不同，用于 extends 与定义类型）。</summary>
        private MetaEnum m_DefiningMetaEnum = null;
        private MetaEnum m_OwnerMetaEnum = null;

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
        public MetaMemberEnum(MetaEnum mc, FileMetaMemberVariable fmmv, bool parentIsConst ) : base()
        {
            m_OwnerMetaEnum = mc;
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            AddPingToken(fmmv.nameToken);
            m_Index = mc.metaMemberVariableDict.Count;
            m_FromType = EFromType.Code;
            m_VariableFrom = EVariableFrom.EnumMember;
            m_Permission = EPermission.Public;
            // Front 语义：enum 成员默认均为 const，只有显式 mut 才允许后续修改。
            m_IsConst = true;
            m_IsStatic = false;
            m_VariableFrom = MetaVariable.EVariableFrom.EnumMember;
            if (fmmv.mutToken != null)
            {
                m_IsConst = false;
            }
            if (fmmv.staticToken != null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error Enum 中不允许使用 static 关键字，枚举值的静态语义由系统处理!!");
            }
            if (m_FileMetaMemeberVariable.permissionToken?.type != null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, "Error Enum中，不允许使用public/private等权限关键字!!");
            }

            SetOwnerMetaClass(mc);
        }
        public override void ParseDefineMetaType()
        {
            var me = m_OwnerMetaEnum;
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
                        Log.AddMetaCoreLog(LID.AutoMetaMemberEnumL140, "Error 没有解析到Express的内容 在MetaMemberData 里边 372");
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
                m_EnumValueExpress = m_Express;
                var valueRet = m_Express.GetReturnMetaDefineType();
                if (valueRet != null)
                {
                    SetRealMetaType(new MetaType(valueRet));
                }
                return true;
            }
            return true;
        }
        /// <summary>
        /// 保持 real 为成员值表达式类型；包装成 Core.Member 后 m_Express 会变，不应再以 m_Express 推导 real。
        /// </summary>
        public override void ParseRealMetaType()
        {
            if (m_EnumValueExpress != null)
            {
                var ret = m_EnumValueExpress.GetReturnMetaDefineType();
                if (ret != null)
                {
                    SetRealMetaType(new MetaType(ret));
                }
            }
            else if (m_Express != null)
            {
                base.ParseRealMetaType();
            }
        }

        public void WrapAsMemberObjectExpress( MetaMemberEnum mme )
        {
            if (m_EnumValueExpress == null) return;

            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (memberClass == null) return;

            var memberType = new MetaType(memberClass);
            var newMember = new MetaNewObjectExpressNode(memberType, mme.ownerMetaClass, mme.m_OwnerMetaBlockStatements);
            newMember.metaContent.SetDefineMetaType(memberType);
            FillMemberNewObjectAssignList(newMember, mme.m_OwnerMetaBlockStatements, mme.m_EnumValueExpress, mme.m_Name, m_Index);

            m_Express = newMember;
            // m_DefineMetaType：enum extends 的声明类型；m_RealMetaType：成员值表达式类型（写入 Member.value）
            SetIsDefineMetaType(true);
        }
        /// <summary>
        /// 为 enum.values 数组生成一项：new Core.Member() 后按 name、value、index 顺序赋值（与 IRNewExpress 对象初始化一致）。
        /// 使用 Member 类模板上的 MetaMemberVariable，保证 IR 里按 hash 能匹配到字段。
        /// </summary>
        public MetaExpressNode CreateValuesArrayElementExpress()
        {
            var valueExpr = m_EnumValueExpress ?? m_Express;
            if (valueExpr == null)
                return null;
            // 已包装成 Member 时，m_EnumValueExpress 仍为原始值表达式；若仅有包装节点则无法再拆值
            if (m_EnumValueExpress == null && valueExpr is MetaNewObjectExpressNode mnoe
                && mnoe.GetReturnMetaDefineType()?.metaClass == CoreMetaClassManager.memberMetaClass)
            {
                return null;
            }

            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (memberClass == null)
                return null;

            var memberType = new MetaType(memberClass);
            var newMember = new MetaNewObjectExpressNode(memberType, ownerMetaClass, m_OwnerMetaBlockStatements);
            newMember.metaContent.SetDefineMetaType(memberType);
            FillMemberNewObjectAssignList(newMember, m_OwnerMetaBlockStatements, valueExpr, m_Name, m_Index);
            return newMember;
        }
        private static void FillMemberNewObjectAssignList(
            MetaNewObjectExpressNode newMember,
            MetaBlockStatements mbs,
            MetaExpressNode valueExpr,
            string memberName,
            int memberIndex)
        {
            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (newMember?.metaContent == null || valueExpr == null || memberClass == null)
                return;

            var nameMv = memberClass.GetMetaMemberVariableByName("name");
            var valueMv = memberClass.GetMetaMemberVariableByName("value");
            var indexMv = memberClass.GetMetaMemberVariableByName("index");
            if (nameMv == null || valueMv == null || indexMv == null)
            {
                Log.AddMetaCoreLog(LID.AutoMetaMemberEnumL242, "Error Core.Member 缺少 name/value/index 字段，无法构造 Member 初始化");
                return;
            }

            var nameExpr = new MetaConstExpressNode(EType.String, memberName ?? string.Empty);
            nameExpr.CalcReturnType();
            var indexExpr = new MetaConstExpressNode(EType.Int32, memberIndex);
            indexExpr.CalcReturnType();

            var list = newMember.metaContent.assignStatementsList;
            list.Add(new MetaBraceAssignStatements(mbs, nameExpr, nameMv));
            list.Add(new MetaBraceAssignStatements(mbs, valueExpr, valueMv));
            list.Add(new MetaBraceAssignStatements(mbs, indexExpr, indexMv));
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
    }
}
