//****************************************************************************
//  File:      MetaMemberVariable.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description: class's memeber variable metadata and member 'data' metadata
//****************************************************************************
using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
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
    public class MetaMemberVariable : MetaVariable
    {
        public List<MetaAttribute> attributeList => m_AttributeList;
        public MetaMemberVariable sourceMetaMemberVariable => m_SourceMetaVariable as MetaMemberVariable;
        public MetaClass sourceMetaClass => m_SourceMetaClass;
        public EFromType fromType => m_FromType;
        public MetaExpressNode express => m_Express;
        public MetaConstExpressNode constExpressNode => m_Express as MetaConstExpressNode;  
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
        

#pragma warning disable CS0414 // 字段“MetaMemberVariable.m_MemberDataType”已被赋值，但从未使用过它的值
        private EMemberDataType m_MemberDataType = EMemberDataType.None;
#pragma warning restore CS0414 // 字段“MetaMemberVariable.m_MemberDataType”已被赋值，但从未使用过它的值


        public MetaMemberVariable( MetaMemberVariable mmv ) : base( mmv )
        {
            m_FromType = EFromType.Manual;
            m_IsInnerDefine = mmv.m_IsInnerDefine;
            m_Express = mmv.m_Express;
            m_VariableFrom = EVariableFrom.ClassMember;

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
            m_VariableFrom = EVariableFrom.ClassMember;
        }
        public MetaMemberVariable(MetaClass mc, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.ClassMember;

            SetOwnerMetaClass(mc);
        }
        public MetaMemberVariable(MetaData md, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.DataMember;

            SetOwnerMetaClass(md);
        }
        public MetaMemberVariable(MetaEnum me, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.EnumMember;

            SetOwnerMetaClass(me);
        }
        public MetaMemberVariable( MetaClass mc, FileMetaMemberVariable fmmv )
        {
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            AddPingToken( fmmv.nameToken );
            m_Token = fmmv.nameToken;
            m_Index = mc.metaMemberVariableDict.Count;
            m_FromType = EFromType.Code;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsStatic = m_FileMetaMemeberVariable?.staticToken != null;
            m_IsConst = m_FileMetaMemeberVariable?.constToken != null;
            m_VariableFrom = EVariableFrom.ClassMember;

            if( string.IsNullOrEmpty( m_Name ) )
            {
                Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL107, "没有找到定义变量名称!");
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
        public void SetVariableFrom(EVariableFrom vfrom )
        {
            m_VariableFrom = vfrom;
        }
        public void SetExpress(MetaExpressNode mcen)
        {
            // Auto-filled const is not considered an explicit '=' from source, but it is a valid express for later stages.
            m_Express = mcen;
        }
        public override void ParseDefineMetaType()
        {
            if (m_FileMetaMemeberVariable?.classDefineRef != null)
            {
                m_DefineMetaType = TypeManager.instance.GetMetaTemplateClassAndRegisterExptendTemplateClassInstance(ownerMetaClass, m_FileMetaMemeberVariable.classDefineRef);                
                m_IsDefineMetaType = true;
            }
            else
            {
                m_IsDefineMetaType = false;
            }
        }
        public override void CalcParseLevel()
        {
            if (isConst)
            {
                parseLevel = MetaMemberVariable.s_ConstLevel;
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
            if( this.m_FileMetaMemeberVariable != null )
            {
                if (this.m_FileMetaMemeberVariable?.DataType == global::SimpleLanguage.Compile.EMemberDataType.Array)
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
                if( this.m_FileMetaMemeberVariable?.express != null )
                {
                    var tokens = this.m_FileMetaMemeberVariable?.express.GetTokens();
                    //var ld = Log.AddMetaCoreLog(LID.ShowExtendMessage, $"Error [{this.ownerMetaClass.allClassName + "." + this.m_Name} ]配置成员变量时，必须需要有等号及后续的表达式!!");
                    //ld.demo = "T t";
                    //ld.advan = "T t = null";
                }
                else
                {

                }
            }
        }
        public override bool ParseMetaExpress()
        {
            if (m_Express != null)
            {
                this.m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                m_Express = ExpressManager.ConvertNewExpress(m_Express, m_DefineMetaType, this );
                m_Express.CalcReturnType();
            }
            return true;
        }
        public override void ParseRealMetaType()
        {
            if( m_Express != null )
            {
                m_RealMetaType = new MetaType(m_Express.GetReturnMetaDefineType());
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
                    if (!MetaVariable.TryAdjustConstExpressByDefineMetaType(mcen, m_DefineMetaType))
                    {
                        // 类型不匹配时日志已在 TryAdjustConstExpressByDefineMetaType 内输出
                    }
                    else
                    {
                        var curEType = CoreMetaClassManager.GetETypeByMetaClass(m_DefineMetaType.metaClass);
                        if (curEType == EType.Object)
                        {
                            curEType = mcen.eType;
                        }
                        if (curEType == EType.Boolean || curEType == EType.String)
                        {
                        }
                        else
                        {
                            CalcDefineClassType();
                        }
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
            if( m_DefineMetaType == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreDefineTypeIsNull, "Error 表达式为空 或者 表达示必须有返回值", "express" );
            }
            if (m_Express == null )
            {
                Log.AddMetaCoreLog(LID.MetaCoreExpressIsNull, "", "express" );
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
                                    Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL403, "Error 自己类内部不允许包含 自己的实体，必须赋值为null");
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
                    var relation = TypeManager.ResolveAssignRelation(
                        m_DefineMetaType,
                        m_Express,
                        false,
                        true,
                        out MetaType expressRetMetaDefineType,
                        out MetaClass curClass,
                        out MetaClass compareClass,
                        out bool isNullConstExpress,
                        this);
                    if (relation == EClassRelation.CompareClassError)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 表达式中返回定义类型为空 " + m_Express.ToTokenString());
                        return;
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
                    if (relation == EClassRelation.No)
                    {
                        sb.Append("类型不相同，可能会有强转，强转后可能默认值为null");
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, sb.ToString());
                    }
                    else if (relation == EClassRelation.Same)
                    {
                        //if( !isNullConstExpress )
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
                                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberNotAllowInstanceInSelfMetaClass, m_Token, 
                                            "in member variable", ownerMetaClass?.allClassName ?? m_OwnerMetaClass?.name, m_Name );
                                        return;
                                    }
                                }
                            }
                            SetRealMetaType(expressRetMetaDefineType);
                        }
                    }
                    else if (relation == EClassRelation.Parent)
                    {
                        sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, sb.ToString());
                    }
                    else if( relation == EClassRelation.Num )
                    {
                        sb.Append("类型不相同，可能会有强转， 返回值是父类型向子类型转换，存在错误转换!!");
                        //Log.AddMetaCoreLog(LID.ShowExtendMessage, sb.ToString());
                    }
                    else if (relation == EClassRelation.Child)
                    {
                        if (compareClass != null)
                        {
                            if (expressRetMetaDefineType.IsArray() )
                            {

                            }
                            else
                            {
                                if( !this.isStatic && !this.isConst )
                                {
                                    if (expressRetMetaDefineType.metaClass == ownerMetaClass)
                                    {
                                        Log.AddMetaCoreLog(LID.MetaCoreMetaMemberNotAllowInstanceInSelfMetaClass, m_Token,
                                            "in member variable", ownerMetaClass?.allClassName ?? m_OwnerMetaClass?.name, m_Name);
                                        return;
                                    }
                                }
                            }
                            SetRealMetaType(expressRetMetaDefineType);
                        }
                    }
                    else if(relation == EClassRelation.Similar )
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
                        Log.AddMetaCoreLog( LID.ShowExtendMessage, sb.ToString());
                    }
                }
            }
        }
        public bool IsClassAdapt( MetaClass mc1, MetaClass mc2 )
        {
            if( mc1 == CoreMetaClassManager.int64MetaClass
                || mc1 == CoreMetaClassManager.uint64MetaClass )
            {
                if( mc2 == CoreMetaClassManager.uint8MetaClass
                    || mc2 == CoreMetaClassManager.int8MetaClass
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
                if (mc2 == CoreMetaClassManager.uint8MetaClass
                    || mc2 == CoreMetaClassManager.int8MetaClass
                    || mc2 == CoreMetaClassManager.int16MetaClass
                    || mc2 == CoreMetaClassManager.uint16MetaClass)
                {
                    return true;
                }
            }
            else if (mc1 == CoreMetaClassManager.int16MetaClass
                || mc1 == CoreMetaClassManager.uint16MetaClass)
            {
                if (mc2 == CoreMetaClassManager.uint8MetaClass
                    || mc2 == CoreMetaClassManager.int8MetaClass )
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
                            Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL638, "Error 现在配置中，不支持成员变量中使用类的()构造方式!!");
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
                            Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL649, "Error 在类变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
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
                                Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL660, "Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                                return null;
                            }
                        }
                    }
                }
                else
                {
                    if(fmpt != null )
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL670, "Error 在类没有定义的变量中，不允许 使用()的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmbt != null)
                    {
                        Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL675, "Error 在类没有定义的变量中，不允许 使用{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
                        return null;
                    }
                    else if (fmct != null)
                    {
                        if (fmct.callLink.callNodeList.Count > 0)
                        {
                            var finalNode = fmct.callLink.callNodeList[fmct.callLink.callNodeList.Count - 1];
                            if (finalNode.fileMetaBraceTerm != null && !ProjectManager.isSupportConstructionFunctionConnectBraceType)
                            {
                                Log.AddMetaCoreLog(LID.AutoMetaMemberVariableL685, "Error 在类变量中，不允许 使用Class()后带{}的赋值方式!!" + fmbt.token?.ToLexemeAllString());
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
