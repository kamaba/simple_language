//****************************************************************************
//  File:      MetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: class's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.Core
{
    public enum EFromType : byte
    {
        Code = 1,         //写的.s代码
        Manual = 2,         //手动，通过c#代码
        CodeAndManual = 3,  //
        CSharp= 4,              //通过c#的dll文件，或者是编译完的代码的识别
        ManualAndCSharp = 6,     //手动注入的c#代码进逻辑解析
        All = 7
    }
    public class MetaMemberVariable : MetaVariable, IComparable<MetaMemberVariable>
    {
        public List<MetaAttribute> attributeList => m_AttributeList;
        public MetaMemberVariable sourceMetaMemberVariable => m_SourceMetaVariable as MetaMemberVariable;
        public MetaClass sourceMetaClass => m_SourceMetaClass;
        public EFromType fromType => m_FromType;
        public MetaExpressNode express => m_Express;
        public MetaConstExpressNode constExpressNode => m_Express as MetaConstExpressNode;  
        public int parseLevel { get; set; } = -1;
        public bool isInnerDefine => m_IsInnerDefine;
        public int index => m_Index;
        public FileMetaMemberVariable fileMetaMemeberVariable => m_FileMetaMemeberVariable;

        protected EFromType m_FromType = EFromType.Code;
        protected int m_Index = -1;
        protected FileMetaMemberVariable m_FileMetaMemeberVariable;
        protected MetaExpressNode m_Express = null;
        protected bool m_IsInnerDefine = false;
        protected List<MetaMemberVariable> m_TemplateChildMetaMemberVariableList = new List<MetaMemberVariable>();
        protected MetaClass m_SourceMetaClass = null;

        private readonly List<MetaAttribute> m_AttributeList = new List<MetaAttribute>();
        //private Dictionary< string, MetaGenTemplate> m_MetaGenTemplateDict = new Dictionary<string, MetaGenTemplate>();
        
        public static int s_ConstLevel = 10000000;
        public static int s_IsHaveRetStaticLevel = 100000000;
        public static int s_NoHaveRetStaticLevel = 200000000;
        public static int s_DefineMetaTypeLevel = 1000000000;
        public static int s_ExpressLevel = 1500000000;

#pragma warning disable CS0414 // 字段“MetaMemberVariable.m_MemberDataType”已被赋值，但从未使用过它的值
        private EMemberDataType m_MemberDataType = EMemberDataType.None;
#pragma warning restore CS0414 // 字段“MetaMemberVariable.m_MemberDataType”已被赋值，但从未使用过它的值


        public MetaMemberVariable( MetaMemberVariable mmv ) : base( mmv )
        {
            m_FromType = EFromType.Manual;
            m_IsInnerDefine = mmv.m_IsInnerDefine;
            m_Express = mmv.m_Express;
            m_VariableFrom = EVariableFrom.Member;

            this.m_FileMetaMemeberVariable = mmv.m_FileMetaMemeberVariable;
            m_Name = mmv.m_Name;
            this.m_PintTokenList = mmv.m_PintTokenList;
            m_Index = mmv.m_Index;
            m_FromType = mmv.m_FromType;  
            m_IsStatic = mmv.m_IsStatic;
            m_Permission = mmv.m_Permission;
            mmv.m_TemplateChildMetaMemberVariableList.Add(this);
        }
        protected MetaMemberVariable()
        {
        }
        public MetaMemberVariable(MetaClass mc, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.Member;

            SetOwnerMetaClass(mc);
        }
        public MetaMemberVariable( MetaClass mc, FileMetaMemberVariable fmmv )
        {
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            AddPingToken( fmmv.nameToken );
            m_Index = mc.metaMemberVariableDict.Count;
            m_FromType = EFromType.Code;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsStatic = m_FileMetaMemeberVariable?.staticToken != null;
            m_IsConst = m_FileMetaMemeberVariable?.constToken != null;
            m_VariableFrom = EVariableFrom.Member;

            if( string.IsNullOrEmpty( m_Name ) )
            {
                Log.AddMetaCoreLog(LID.Unknown, "没有找到定义变量名称!");
                m_Name = "Error_" + GetHashCode().ToString();
            }
            if (m_FileMetaMemeberVariable.permissionToken?.type != null)
            {
                m_Permission = CompilerUtil.GetPerMissionByType(m_FileMetaMemeberVariable.permissionToken.type );
            }
            else
            {
                if(m_Name[0] == '_' )
                {
                    m_Permission = EPermission.Private;
                }
            }
            m_SourceMetaClass = mc;
            SetOwnerMetaClass(mc);

            if (fmmv?.attributeList != null && fmmv.attributeList.Count > 0)
            {
                for (int i = 0; i < fmmv.attributeList.Count; i++)
                {
                    m_AttributeList.Add(new MetaAttribute(fmmv.attributeList[i]));
                }
            }
        }
        //public MetaMemberVariable(MetaGenTemplateClass mtc, MetaMemberVariable mmv, Dictionary<string,MetaGenTemplate> mgt) : base(mmv)
        //{
        //    m_MetaGenTemplateDict = mgt;
        //    m_Name = mmv.m_Name;
        //    m_DefineTypeName = mmv.m_DefineTypeName;
        //    m_IsInnerDefine = mmv.m_IsInnerDefine;
        //    m_FromType = mmv.m_FromType;
        //    m_DefineMetaType = mmv.m_DefineMetaType;
        //    m_VariableFrom = EVariableFrom.Member;
        //    m_PintTokenList = mmv.m_PintTokenList;
        //    m_FileMetaMemeberVariable = mmv.m_FileMetaMemeberVariable;

        //    SetOwnerMetaClass(mtc);
        //}
        public void SetVariableFrom(EVariableFrom vfrom )
        {
            m_VariableFrom = vfrom;
        }
        public override void ParseDefineMetaType()
        {
            if (m_FileMetaMemeberVariable?.classDefineRef != null)
            {
                m_DefineMetaType = TypeManager.instance.GetMetaTemplateClassAndRegisterExptendTemplateClassInstance(m_OwnerMetaClass, m_FileMetaMemeberVariable.classDefineRef);
                //if( m_DefineMetaType.isTemplate || m_DefineMetaType.eType == EMetaTypeType.TemplateClassWithTemplate )
                if( m_DefineMetaType != null )
                {
                    m_RealMetaType = new MetaType(m_DefineMetaType);
                }
                m_IsDefineMetaType = true;
            }
            else
            {
                m_IsDefineMetaType = false;
            }
        }
        public virtual void CalcParseLevel()
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
        public void CreateExpress()
        {
            if( this.m_FileMetaMemeberVariable != null )
            {
                if (this.m_FileMetaMemeberVariable?.DataType == FileMetaMemberVariable.EMemberDataType.Array)
                {
                    m_Express = CreateExpressNodeInClassMetaVariable();
                }
                else
                {
                    m_Express = CreateExpressNodeInClassMetaVariable();
                }
            }
            if( this.m_Express == null )
            {
                Debug.Assert(false);
                var ld = Log.AddMetaCoreLog( LID.Unknown, $"Error [{this.ownerMetaClass.allClassName + "." + this.m_Name} ]配置成员变量时，必须需要有等号及后续的表达式!!");
                ld.demo = "T t";
                ld.advan = "T t = null";
            }
        }
        public void SetExpress( MetaExpressNode mcen)
        {
            // Auto-filled const is not considered an explicit '=' from source, but it is a valid express for later stages.
            m_Express = mcen;
            if (m_RealMetaType == null)
            {
                m_RealMetaType = new MetaType(m_DefineMetaType?.metaClass ?? CoreMetaClassManager.objectMetaClass);
            }
        }
        public override bool ParseMetaExpress()
        {
            if (m_Express != null)
            {
                m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                m_Express = ExpressManager.ConvertNewExpress(m_Express, m_RealMetaType, this );
            }
            return true;
        }
        public void ParseChildMemberData()
        {
            if (m_FileMetaMemeberVariable == null)
            {
                return;
            }

            int count = m_FileMetaMemeberVariable.fileMetaMemberVariable.Count;

            if (m_FileMetaMemeberVariable.DataType == FileMetaMemberVariable.EMemberDataType.Array)
            {
                List<MetaDynamicClass> list = new List<MetaDynamicClass>();

                for (int i = 0; i < count; i++)
                {
                    var fmmv = m_FileMetaMemeberVariable.fileMetaMemberVariable[i];

                    MetaDynamicClass mdc = new MetaDynamicClass(fmmv.GetHashCode().ToString());
                    list.Add(mdc);

                    for (int j = 0; j < fmmv.fileMetaMemberVariable.Count; j++)
                    {
                        MetaMemberVariable mmv = new MetaMemberVariable(mdc, fmmv.fileMetaMemberVariable[j] as FileMetaMemberVariable);

                        mmv.ParseChildMemberData();
                        mmv.CalcReturnType();

                    }

                }
                m_Express = new MetaNewObjectExpressNode(this.m_OwnerMetaClass, list);
            }
            else if (m_FileMetaMemeberVariable.DataType == FileMetaMemberVariable.EMemberDataType.ConstVariable)
            {
                //CreateExpress();
            }
            else if (m_FileMetaMemeberVariable.DataType == FileMetaMemberVariable.EMemberDataType.KeyValue)
            {

            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var fmmv = m_FileMetaMemeberVariable.fileMetaMemberVariable[i];

                    MetaDynamicClass mdc = new MetaDynamicClass(i.GetHashCode().ToString());

                    MetaMemberVariable mmd = new MetaMemberVariable(mdc, fmmv);
                    if (!AddMetaVariable(mmd))
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error 命名有重名!!");
                    }
                }
            }
        }
        public void SetIndex( int index )
        {
            this.m_Index = index;
        }
        public void CalcReturnType()
        {
            string defineName = this.m_Name;
            if (m_Express != null)
            {
                m_Express.CalcReturnType();
                var enode = SimulateExpressRun(m_Express);
                if (enode != null)
                {
                    m_Express = enode;
                    m_Express.CalcReturnType();
                }
                if( m_Express is MetaConstExpressNode mcen )
                {
                    var curEType = CoreMetaClassManager.GetETypeByMetaClass(m_DefineMetaType.metaClass);
                    var expEType = mcen.eType;

                    // Define 类型是 object 时，直接沿用表达式常量的真实类型。
                    if (curEType == EType.Object)
                    {
                        curEType = expEType;
                    }

                    if (mcen.eType != curEType )
                    {
                        // 数字类型优先走范围检查+强转；其它类型按可转换性处理。
                        if (TryConvertConstValueByEType(curEType, mcen.value, out var convertedValue))
                        {
                            mcen.SetConstValue(curEType, convertedValue);
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.Unknown,
                                "Error 定义类型与表达式类型不匹配，且常量值超出目标类型范围: "
                                + "define=" + curEType + ", express=" + expEType + ", value=" + (mcen.value?.ToString() ?? "null"));
                        }
                    }
                    else
                    {
                        CalcDefineClassType();
                    }
                }
                else
                {
                    CalcDefineClassType();
                }
            }
            else
            {
            }
            if (m_Express == null || m_DefineMetaType == null)
            {
                Log.AddMetaCoreLog(LID.Unknown, "Error 表达式为空 或者 表达示必须有返回值");
                Debug.Assert(false, "");
            }
        }
        public int CompareTo(MetaMemberVariable mmv)
        {
            if (this.parseLevel > mmv.parseLevel)
                return 1;
            else
                return -1;
        }

        private static bool IsNumericEType(EType t)
        {
            return t == EType.Byte
                || t == EType.SByte
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

        private static bool TryConvertConstValueByEType(EType targetType, object input, out object converted)
        {
            converted = null;
            try
            {
                switch (targetType)
                {
                    case EType.Boolean:
                        converted = Convert.ToBoolean(input);
                        return true;
                    case EType.Byte:
                        converted = Convert.ToByte(input);
                        return true;
                    case EType.SByte:
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

        void CalcDefineClassType()
        {
            //var metaFunction = m_OwnerMetaBlockStatements?.ownerMetaFunction;
            string defineName = this.m_Name;
            if (m_RealMetaType == null)
            {
                if (m_Express != null)
                {
                    MetaConstExpressNode constExpressNode = m_Express as MetaConstExpressNode;
                    bool isCheckReturnType = true;
                    if (constExpressNode != null)
                    {
                        if (constExpressNode.eType == EType.Null)
                        {
                            isCheckReturnType = false;
                            m_RealMetaType = new MetaType(m_DefineMetaType);
                        }
                    }
                    if (isCheckReturnType)
                    {
                        var dmct = m_Express.GetReturnMetaDefineType();
                        if ( dmct != null)
                        {
                            if( !ClassManager.IsNumberClass(dmct.metaClass) )
                            {
                                if (dmct.metaClass == ownerMetaClass)
                                {
                                    Log.AddMetaCoreLog(LID.Unknown, "Error 自己类内部不允许包含 自己的实体，必须赋值为null");
                                    return;
                                }
                            }
                            m_RealMetaType = dmct;
                            if( !m_IsDefineMetaType )
                            {
                                m_DefineMetaType = new MetaType(m_RealMetaType);
                            }
                            foreach( var v in m_TemplateChildMetaMemberVariableList )
                            {
                                if( !v.isDefineMetaType )
                                {
                                    v.m_RealMetaType = m_RealMetaType;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (m_Express != null)
                {                    
                    ClassManager.EClassRelation relation = ClassManager.EClassRelation.No;
                    MetaConstExpressNode constExpressNode = m_Express as MetaConstExpressNode;
                    MetaClass curClass = this.m_DefineMetaType.metaClass;

                    MetaClass compareClass = null;
                    MetaType expressRetMetaDefineType = null;
                    if (constExpressNode != null && constExpressNode.eType == EType.Null)
                    {
                        relation = ClassManager.EClassRelation.Same;
                    }
                    else
                    {
                        expressRetMetaDefineType = m_Express.GetReturnMetaDefineType();
                        if (expressRetMetaDefineType == null)
                        {
                            Log.AddMetaCoreLog(LID.Unknown, "Error 表达式中返回定义类型为空 " + m_Express.ToTokenString());
                            return;
                        }

                        compareClass = expressRetMetaDefineType.metaClass;
                        relation = ClassManager.ValidateClassRelationByMetaClass(curClass, compareClass);
                    }

                    StringBuilder sb = new StringBuilder();
                    //sb.Append("Warning 在类: " + metaFunction?.ownerMetaClass.allName + " 函数: " + metaFunction?.name + "中  ");
                    if (curClass != null)
                    {
                        sb.Append(" 定义类 : " + curClass.allClassName );
                    }
                    if (defineName != null)
                    {
                        sb.Append(" 名称为: " + defineName?.ToString());
                    }
                    sb.Append("与后边赋值语句中 ");
                    if (compareClass != null)
                        sb.Append("表达式类为: " + compareClass.allClassName );
                    if (relation == ClassManager.EClassRelation.No)
                    {
                        sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                        Log.AddMetaCoreLog(LID.Unknown, sb.ToString());
                    }
                    else if (relation == ClassManager.EClassRelation.Same)
                    {
                        if( !(constExpressNode != null && constExpressNode.eType == EType.Null ) )
                        {
                            if( TypeManager.IsCoreMetaType( expressRetMetaDefineType ) )
                            {

                            }
                            else
                            {
                                if (!ClassManager.IsNumberClass(expressRetMetaDefineType.metaClass))
                                {
                                    if (expressRetMetaDefineType.metaClass == ownerMetaClass && (!m_IsStatic && !m_IsConst))
                                    {
                                        Log.AddMetaCoreLog(LID.Unknown, "Error 自己类内部不允许包含 自己的实体2，必须赋值为null");
                                        return;
                                    }
                                }
                            }
                            SetRealMetaType(expressRetMetaDefineType);
                        }
                    }
                    else if (relation == ClassManager.EClassRelation.Parent)
                    {
                        sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                        Log.AddMetaCoreLog(LID.Unknown, sb.ToString());
                    }
                    else if (relation == ClassManager.EClassRelation.Child)
                    {
                        if (compareClass != null)
                        {
                            if (expressRetMetaDefineType.IsArray() )
                            {

                            }
                            else
                            {
                                if (expressRetMetaDefineType.metaClass == ownerMetaClass)
                                {
                                    Log.AddMetaCoreLog(LID.Unknown, "Error 自己类内部不允许包含 自己的实体3，必须赋值为null");
                                    return;
                                }
                            }
                            SetRealMetaType(expressRetMetaDefineType);
                        }
                    }
                    else if(relation == ClassManager.EClassRelation.Similar )
                    {
                        if( compareClass != null )
                        {
                            if(IsClassAdapt(curClass, compareClass ) )
                            {
                                if( m_IsDefineMetaType )
                                {
                                    SetRealMetaType(expressRetMetaDefineType);
                                }
                                else
                                {
                                    SetRealMetaType(new MetaType(curClass) );
                                }
                            }
                        }
                    }
                    else
                    {
                        sb.Append("表达式错误，或者是定义类型错误");
                        Log.AddMetaCoreLog( LID.Unknown, sb.ToString());
                    }
                }
            }
        }
        public bool IsClassAdapt( MetaClass mc1, MetaClass mc2 )
        {
            if( mc1 == CoreMetaClassManager.int64MetaClass
                || mc1 == CoreMetaClassManager.uint64MetaClass )
            {
                if( mc2 == CoreMetaClassManager.byteMetaClass
                    || mc2 == CoreMetaClassManager.sbyteMetaClass
                    || mc2 == CoreMetaClassManager.int16MetaClass
                    || mc2 == CoreMetaClassManager.uint16MetaClass 
                    || mc2 == CoreMetaClassManager.int32MetaClass 
                    || mc2 == CoreMetaClassManager.uint32MetaClass )
                {
                    return true;
                }
            }
            else if (mc1 == CoreMetaClassManager.int32MetaClass
                || mc1 == CoreMetaClassManager.uint32MetaClass)
            {
                if (mc2 == CoreMetaClassManager.byteMetaClass
                    || mc2 == CoreMetaClassManager.sbyteMetaClass
                    || mc2 == CoreMetaClassManager.int16MetaClass
                    || mc2 == CoreMetaClassManager.uint16MetaClass)
                {
                    return true;
                }
            }
            else if (mc1 == CoreMetaClassManager.int16MetaClass
                || mc1 == CoreMetaClassManager.uint16MetaClass)
            {
                if (mc2 == CoreMetaClassManager.byteMetaClass
                    || mc2 == CoreMetaClassManager.sbyteMetaClass )
                {
                    return true;
                }
            }
            return false;
        }
        public MetaExpressNode SimulateExpressRun(MetaExpressNode node)
        {
            MetaExpressNode newnode = node;
            if ( node is MetaCallLinkExpressNode )
            {
                MetaCallLinkExpressNode mcen = node as MetaCallLinkExpressNode;
                if( mcen != null )
                {
                    newnode = mcen.ConvertConstExpressNode();
                }
            }
            else if( node is MetaOpExpressNode )
            {
                MetaOpExpressNode moen = node as MetaOpExpressNode;
                var left = SimulateExpressRun(moen.left);
                var right = SimulateExpressRun(moen.right);
                if (left != null)
                {
                    moen.SetLeft( left );
                }
                if (right != null)
                {
                    moen.SetRight( right );
                }
                newnode = node;
            }
            else if( node is MetaUnaryOpExpressNode )
            {
                MetaUnaryOpExpressNode muoen = node as MetaUnaryOpExpressNode;
                var v = SimulateExpressRun(muoen.value);
                if (v != null)
                {
                    muoen.SetValue( v );
                }
                newnode = node;
            }
            return newnode;
        }
        MetaExpressNode CreateExpressNodeInClassMetaVariable()
        {
            var express = this.m_FileMetaMemeberVariable?.express;
            if (express == null) return null;

            var root = express.root;
            if (root == null)
                return null;
            if (root.left == null && root.right == null)
            {
                var fmpt = root as FileMetaParTerm;
                var fmct = root as FileMetaCallTerm;
                var fmbt = root as FileMetaBraceTerm;
                if (m_DefineMetaType != null )
                {
                    if (fmpt != null)            // for example: Class1 obj = (1,2,3,4);
                    {
                        if( ProjectManager.isSupportConstructionFunctionOnlyParType )
                        {
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.Unknown, "Error 现在配置中，不支持成员变量中使用类的()构造方式!!");
                            return null;
                        }
                    }
                    else if (fmbt != null)
                    {
                        if (ProjectManager.isSupportConstructionFunctionOnlyBraceType)
                        {
                        }
                        else
                        {
                            Log.AddMetaCoreLog(LID.Unknown, "Error 在类变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                            return null;
                        }                        
                    }
                    else if (fmct != null)
                    {
                        if( fmct.callLink.callNodeList.Count > 0 )
                        {
                            var finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                            if( finalNode.fileMetaBraceTerm != null && !ProjectManager.isSupportConstructionFunctionConnectBraceType)
                            {
                                Log.AddMetaCoreLog(LID.Unknown, "Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                                return null;
                            }
                        }
                    }
                }
                else
                {
                    if(fmpt != null )
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error 在类没有定义的变量中，不允许 使用()的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmbt != null)
                    {
                        Log.AddMetaCoreLog(LID.Unknown, "Error 在类没有定义的变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmct != null)
                    {
                        if (fmct.callLink.callNodeList.Count > 0)
                        {
                            var finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                            if (finalNode.fileMetaBraceTerm != null && !ProjectManager.isSupportConstructionFunctionConnectBraceType)
                            {
                                Log.AddMetaCoreLog(LID.Unknown, "Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                                return null;
                            }
                        }
                    }
                }
            }

            CreateExpressParam cep = new CreateExpressParam();
            cep.ownerMetaClass = ownerMetaClass;
            cep.metaType = m_DefineMetaType;
            cep.equalMetaVariable = this;
            cep.parsefrom = EParseFrom.MemberVariableExpress;
            cep.isConst = isConst;
            cep.isStatic = isStatic;
            cep.allowUseIfSyntax = false;
            cep.allowUseSwitchSyntax = false;
            cep.allowUseParSyntax = ProjectManager.isSupportConstructionFunctionOnlyParType;
            cep.allowUseBraceSyntax = ProjectManager.isSupportConstructionFunctionOnlyBraceType;
            cep.fme = root;

            MetaExpressNode mn = ExpressManager.CreateExpressNode(cep);

            return mn;
        }
        public void SetSourceMetaClass( MetaClass mc )
        {
            this.m_SourceMetaClass = mc;
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
        /*
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();
            switch (m_FileMetaMemeberData.DataType)
            {
                case FileMetaMemberData.EMemberDataType.NameClass:
                    {
                        for (int i = 0; i < realDeep; i++)
                            sb.Append(Global.tabChar);
                        sb.AppendLine(m_Name);
                        for (int i = 0; i < realDeep; i++)
                            sb.Append(Global.tabChar);
                        sb.AppendLine("{");
                        foreach (var v in m_MetaMemberDataDict)
                        {
                            sb.AppendLine(v.Value.ToFormatString());
                        }
                        for (int i = 0; i < realDeep; i++)
                            sb.Append(Global.tabChar);
                        sb.Append("}");

                    }
                    break;
                case FileMetaMemberData.EMemberDataType.Array:
                    {
                        int i = 0;
                        for (i = 0; i < realDeep; i++)
                            sb.Append(Global.tabChar);
                        sb.Append(m_Name + " = [");
                        i = 0;
                        foreach (var v in m_MetaMemberDataDict)
                        {
                            sb.Append(v.Value.ToFormatString());
                            if (i < m_MetaMemberDataDict.Count - 1)
                                sb.Append(",");
                            i++;
                        }
                        sb.Append("]");
                    }
                    break;
                case FileMetaMemberData.EMemberDataType.NoNameClass:
                    {
                        sb.AppendLine();
                        for (int i = 0; i < realDeep; i++)
                            sb.Append(Global.tabChar);
                        sb.AppendLine("{");
                        foreach (var v in m_MetaMemberDataDict)
                        {
                            sb.AppendLine(v.Value.ToFormatString());
                        }
                        for (int i = 0; i < realDeep; i++)
                            sb.Append(Global.tabChar);
                        sb.Append("}");
                        //if( m_End )
                        //{
                        //    sb.AppendLine();
                        //}
                    }
                    break;
                case FileMetaMemberData.EMemberDataType.KeyValue:
                    {
                        for (int i = 0; i < realDeep; i++)
                            sb.Append(Global.tabChar);
                        sb.Append(m_Name + " = " + m_Express.ToFormatString() + ";");
                    }
                    break;
                case FileMetaMemberData.EMemberDataType.Value:
                    {
                        sb.Append(m_Express.ToFormatString());
                    }
                    break;
            }
            return sb.ToString();
        }
        */
    }
}
