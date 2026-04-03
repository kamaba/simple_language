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
        private bool isExplicitAssign => m_IsExplicitAssign;
        private bool m_IsExplicitAssign = false;
        private MetaExpressNode m_EnumValueExpress = null;

        public MetaExpressNode enumValueExpress => m_EnumValueExpress;
        public MetaConstExpressNode enumValueConstExpressNode => m_EnumValueExpress as MetaConstExpressNode;

        public MetaMemberEnum(MetaClass mc, string name, MetaClass extendClass) : base(mc, name)
        {
        }
        public MetaMemberEnum(MetaClass mc, FileMetaMemberVariable fmmv, MetaClass extendClass, bool parentIsConst ) : base()
        {
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            AddPingToken(fmmv.nameToken);
            m_Index = mc.metaMemberVariableDict.Count;
            m_FromType = EFromType.Code;
            m_VariableFrom = EVariableFrom.EnumMember;
            m_Permission = EPermission.Public;

            m_DefineMetaType = new MetaType(CoreMetaClassManager.memberMetaClass);
            m_RealMetaType = new MetaType(CoreMetaClassManager.memberMetaClass);
            SetIsDefineMetaType(true);

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

            }



            if (string.IsNullOrEmpty(m_Name))
            {
                Log.AddInStructMeta(EError.None, "没有找到定义变量名称!");
                m_Name = "Error_" + GetHashCode().ToString();
            }
            SetOwnerMetaClass(mc);

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
            // 成员语义：如果枚举本身声明为 const，则成员均视为 const；否则成员视为 static
            if (parentIsConst)
            {
                m_IsConst = true;
                m_IsStatic = false;
            }
            else
            {
                m_IsConst = false;
                m_IsStatic = true;
            }
            m_VariableFrom = MetaVariable.EVariableFrom.EnumMember;
            if (fmmv.mutToken != null)
            {
                if (parentIsConst)
                {
                    Log.AddInStructMeta(EError.None, "Warning Enum 中声明为 const 时，成员不能使用 mut，成员仍视为 const");
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "Warning Enum 成员使用 mut，枚举成员语义仍由枚举定义决定（默认为 static）");
                }
            }
            if (fmmv.staticToken != null)
            {
                Log.AddInStructMeta(EError.None, "Error Enum 中不允许使用 static 关键字，枚举值的静态语义由系统处理!!");
            }

            if (string.IsNullOrEmpty(m_Name))
            {
                Log.AddInStructMeta(EError.None, "没有找到定义变量名称!");
                m_Name = "Error_" + GetHashCode().ToString();
            }
            if (m_FileMetaMemeberVariable.permissionToken?.type != null)
            {
                Log.AddInStructMeta(EError.None, "Error Enum中，不允许使用public/private等权限关键字!!");
                var permission = CompilerUtil.GetPerMissionByType(m_FileMetaMemeberVariable.permissionToken.type);
                if( permission == EPermission.Private || permission == EPermission.Protected )
                {
                    Debug.Assert(false);
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
                m_EnumValueExpress = m_Express;
                return true;
            }
            else
            {
                Debug.Assert(false, "必须给出定义");
                return false;
            }
        }

        public void WrapAsMemberObjectExpress()
        {
            if (m_EnumValueExpress == null) return;

            var memberClass = CoreMetaClassManager.memberMetaClass;
            if (memberClass == null) return;

            var rmdt = m_EnumValueExpress.GetReturnMetaDefineType();
            var nameMember = CreateBuiltInMemberAssignTarget(memberClass, "name", null);
            var valueMember = CreateBuiltInMemberAssignTarget(memberClass, "value", rmdt);
            var indexMember = CreateBuiltInMemberAssignTarget(memberClass, "index", null);
            if (nameMember == null || valueMember == null || indexMember == null)
            {
                Log.AddInStructMeta(EError.None, "Error Core.Member 缺少 name/value/index 字段，无法包装 enum member");
                return;
            }

            var memberType = new MetaType(memberClass);
            var newMember = new MetaNewObjectExpressNode(memberType, m_OwnerMetaClass, m_OwnerMetaBlockStatements);
            newMember.metaContent.SetMetaType(memberType);

            var nameExpr = new MetaConstExpressNode(EType.String, m_Name ?? string.Empty);
            nameExpr.CalcReturnType();
            indexMember.SetExpress(nameExpr);

            var indexExpr = new MetaConstExpressNode(EType.Int32, m_Index);
            indexExpr.CalcReturnType();
            indexMember.SetExpress(indexExpr);

            valueMember.SetExpress(m_EnumValueExpress);
            valueMember.SetRealMetaType(m_EnumValueExpress.GetReturnMetaDefineType());

            //newMember.metaContent.assignStatementsList.Add(
            //    new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, 
            //    nameExpr, nameMember));
            //newMember.metaContent.assignStatementsList.Add(
            //    new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, m_EnumValueExpress, valueMember));
            //newMember.metaContent.assignStatementsList.Add(
            //    new MetaBraceAssignStatements(m_OwnerMetaBlockStatements, 
            //    indexExpr, indexMember));

            m_Express = newMember;
            m_DefineMetaType = memberType;
            m_RealMetaType = new MetaType(memberClass);
            SetIsDefineMetaType(true);
        }

        private MetaMemberVariable CreateBuiltInMemberAssignTarget(MetaClass memberClass, string fieldName, MetaType? valueType)
        {
            var template = memberClass.GetMetaMemberVariableByName(fieldName);
            if (template == null) return null;

            var target = new MetaMemberVariable(template);
            if (valueType != null)
            {
                target.SetMetaDefineType(new MetaType(valueType));
                target.SetRealMetaType(new MetaType(valueType));
                target.SetIsDefineMetaType(true);
            }
            return target;
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
