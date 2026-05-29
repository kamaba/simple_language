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
        public MetaMemberVariable relationMemberVariable => m_RelationMemberVariable;
        public MetaConstExpressNode enumValueConstExpressNode => m_Express as MetaConstExpressNode;
        private bool isExplicitAssign => m_IsExplicitAssign;

        private bool m_IsExplicitAssign = false;

        private MetaMemberVariable m_RelationMemberVariable = null;

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
        public void SetRelationMemberVariable(MetaMemberVariable mmv )
        {
            this.m_RelationMemberVariable = mmv;
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


                    if (m_RelationMemberVariable.express is MetaNewObjectExpressNode mnoen)
                    {
                        var valueMv = CoreMetaClassManager.memberMetaClass.GetMetaMemberVariableByName("value");
                        if (valueMv == null)
                        {
                            Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error Core.Member 缺少 name/value/index 字段，无法构造 Member 初始化");
                            return;
                        }
                        valueMv.SetIsDefineMetaType(true);
                        valueMv.SetMetaDefineType(m_DefineMetaType);
                        var list = mnoen.assignStatementsList;
                        list.Add(new MetaBraceAssignStatements(valueMv, null, ownerMetaBase, m_Express));
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

        public static MetaMemberVariable WrapAsEnumMemberObjectExpress( MetaEnum me, FileMetaMemberVariable fmmv, int index )
        {
            var ownerEnum = me;
            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (ownerEnum == null || memberClass == null)
            {
                return null;
            }

            var memberType = new MetaType(memberClass);
            var wrappedNewObject = new MetaNewObjectExpressNode(memberType, ownerEnum, null, null);

            var nameMv = memberClass.GetMetaMemberVariableByName("name");
            var valueMv = memberClass.GetMetaMemberVariableByName("value");
            var indexMv = memberClass.GetMetaMemberVariableByName("index");
            if (nameMv == null || valueMv == null || indexMv == null)
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, fmmv.token, "Error Core.Member 缺少 name/value/index 字段，无法构造 Member 初始化");
                return null;
            }

            var nameExpr = new MetaConstExpressNode(EType.String, fmmv.name );
            nameExpr.SetToken(fmmv.token);
            nameExpr.CalcReturnType();
            var indexExpr = new MetaConstExpressNode(EType.Int32, index );
            indexExpr.SetToken(fmmv.token);
            indexExpr.CalcReturnType();

            var list = wrappedNewObject.assignStatementsList;
            list.Add(new MetaBraceAssignStatements(nameMv, null, ownerEnum, nameExpr));
            list.Add(new MetaBraceAssignStatements(indexMv, null, ownerEnum, indexExpr));
            //list.Add(new MetaBraceAssignStatements(valueMv, null, ownerEnum, null));

            wrappedNewObject.CalcReturnType();

            var exportVariable = new MetaMemberVariable(ownerEnum, fmmv.name );
            exportVariable.SetToken(fmmv.token);
            exportVariable.SetVariableFrom(EVariableFrom.EnumMember);
            exportVariable.SetIndex(index);
            exportVariable.SetIsStatic(true);
            exportVariable.SetIsConst(fmmv.mutToken != null);
            exportVariable.SetIsDefineMetaType(true);
            exportVariable.SetMetaDefineType(memberType);
            exportVariable.SetRealMetaType(new MetaType(memberType));
            exportVariable.SetExpress(wrappedNewObject);

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
