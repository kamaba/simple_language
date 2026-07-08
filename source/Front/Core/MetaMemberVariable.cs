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
        public MetaExpressNodeBase express => m_Express;
        public MetaConstExpressNode constExpressNode => m_Express as MetaConstExpressNode;  
        public bool isInnerDefine => m_IsInnerDefine;
        public int index => m_Index;
        public FileMetaMemberVariable fileMetaMemeberVariable => m_FileMetaMemeberVariable;
        // 解析顺序：在 ParseMetaExpress 首次执行时记录的全局自增序号。
        // 用于在 IR 导出/VM 加载时按依赖解析顺序还原成员初始化表达式的执行次序。
        // -1 表示尚未参与解析（兜底按声明顺序）。
        public int parseOrder => m_ParseOrder;

        protected EFromType m_FromType = EFromType.Code;
        protected int m_Index = -1;
        protected FileMetaMemberVariable m_FileMetaMemeberVariable;
        protected MetaExpressNodeBase m_Express = null;
        protected bool m_IsInnerDefine = false;
        protected List<MetaMemberVariable> m_TemplateChildMetaMemberVariableList = new List<MetaMemberVariable>();
        protected MetaClass m_SourceMetaClass = null;
        protected int m_ParseOrder = -1;
        // 防止依赖循环时 ParseMetaExpress 递归重入导致的死循环（A 依赖 B、B 依赖 A）。
        private bool m_IsParsingExpress = false;

        private static int s_NextParseOrder = 0;

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
            m_Token = mmv.m_Token;

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

            SetOwnerMetaBase(mc);
        }
        public MetaMemberVariable(MetaData md, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.DataMember;

            SetOwnerMetaBase(md);
        }
        public MetaMemberVariable(MetaEnum me, string _name)
        {
            m_Name = _name;
            m_FromType = EFromType.Manual;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsInnerDefine = true;
            m_VariableFrom = EVariableFrom.EnumMember;

            SetOwnerMetaBase(me);
        }
        public MetaMemberVariable( MetaClass mc, FileMetaMemberVariable fmmv )
        {
            m_FileMetaMemeberVariable = fmmv;
            m_Name = fmmv.name;
            m_Token = fmmv.token;
            m_FromType = EFromType.Code;
            m_DefineMetaType = new MetaType(CoreMetaClassManager.objectMetaClass);
            m_IsStatic = m_FileMetaMemeberVariable?.staticToken != null;
            m_IsConst = m_FileMetaMemeberVariable?.constToken != null;
            m_VariableFrom = EVariableFrom.ClassMember;

            if( string.IsNullOrEmpty( m_Name ) )
            {
                Log.AddMetaCoreLog(LID.ShowExtendMessage, "没有找到定义变量名称!");
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
            SetOwnerMetaBase(mc);

            if (fmmv?.attributeList != null && fmmv.attributeList.Count > 0)
            {
                for (int i = 0; i < fmmv.attributeList.Count; i++)
                {
                    m_AttributeList.Add(new MetaAttribute(fmmv.attributeList[i]));
                }
            }
        }
        public void SetFileMetaMemeberVariable(FileMetaMemberVariable fmmv )
        {
            m_FileMetaMemeberVariable = fmmv;
        }
        public void SetVariableFrom(EVariableFrom vfrom )
        {
            m_VariableFrom = vfrom;
        }
        public void SetIndex(int index)
        {
            this.m_Index = index;
        }
        public void SetExpress(MetaExpressNodeBase mcen)
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
            CreateCalcParseLevel();
        }
        protected void CreateCalcParseLevel()
        {
            if (this.isConst)
            {
                parseLevel = s_ConstLevel;
            }
            else if (isStatic)
            {
                if (parseLevel == -1)
                {
                    if (m_IsDefineMetaType)
                    {
                        parseLevel = s_StaticDefLevel;
                    }
                    else
                    {
                        parseLevel = s_StaticNonDefLevel;
                    }

                }
            }
            else
            {
                if (parseLevel == -1)
                {
                    if (m_IsDefineMetaType)
                    {
                        parseLevel = s_NonDefExpressLevel;
                    }
                    else
                    {
                        parseLevel = s_DefExpressLevel;
                    }
                }
            }
        }
        public override void SetParseLevel(int level)
        {
            parseLevel = level;
        }
        public override void CreateMetaExpress()
        {
            //if (m_Express != null)
            //{
            //    ExpressManager.CalcParseLevel(parseLevel, m_Express);
            //}
            if ( this.m_FileMetaMemeberVariable != null )
            {
                var express = this.m_FileMetaMemeberVariable?.express;
                if (express == null)
                {
                    Log.AddMetaCoreLog(LID.ShowExtendMessage, "Error 在类没有定义的变量中，不允许 使用{}的赋值方式!!" + express.token?.ToLexemeAllString());

                    return;
                }
                CreateExpressParam cep = new CreateExpressParam();
                cep.ownerMetaBase = ownerMetaBase;
                cep.metaType = m_DefineMetaType;
                cep.equalMetaVariable = this;
                cep.parsefrom = EParseFrom.MemberVariableExpress;
                cep.isConst = isConst;
                cep.isStatic = isStatic;
                cep.allowUseIfSyntax = false;
                cep.allowUseSwitchSyntax = false;
                cep.allowUseParSyntax = ProjectManager.isSupportConstructionFunctionOnlyParType;
                cep.allowUseBraceSyntax = ProjectManager.isSupportConstructionFunctionOnlyBraceType;
                cep.fme = express;

                this.m_Express = ExpressManager.CreateExpressNode(cep);
            }
            if( this.m_Express == null )
            {
                Token token = null;
                if ( this.m_FileMetaMemeberVariable?.express != null )
                {
                    token = this.m_FileMetaMemeberVariable?.express.token;                   
                }
                Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, token, $"Error [{this.ownerMetaClass.allName + "." + this.m_Name} ]配置成员变量时，必须需要有等号及后续的表达式!!");
            }
        }
        public override bool ParseMetaExpress()
        {
            // 解析顺序（order）必须在依赖被递归解析之后再分配：
            // 在 MetaCallNode.cs 第 2085 行，解析某成员表达式时若遇到尚未解析类型的其它成员，
            // 会主动调用该成员的 ParseMetaExpress() 提前解析。
            // 因此先 Parse 表达式（递归解析依赖），表达式解析完成后再分配 order，
            // 这样被依赖成员会先得到较小 order，VM 端据此先执行其初始化表达式。
            // m_IsParsingExpress 守卫用于在依赖循环时避免递归重入死循环。
            if (m_Express != null)
            {
                if (!m_IsParsingExpress)
                {
                    m_IsParsingExpress = true;
                    this.m_Express.Parse(new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress });
                    m_IsParsingExpress = false;
                }
            }
            else
            {
            }
            // 表达式（含其依赖）解析完成后分配 order；仅首次分配，循环依赖时由先返回者获得较小 order。
            if (m_ParseOrder < 0)
            {
                m_ParseOrder = s_NextParseOrder++;
            }
            if (m_DefineMetaType == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreDefineTypeIsNull, m_Token, "Error 表达式为空 或者 表达示必须有返回值", "express");
            }
            if (m_Express == null)
            {
                Log.AddMetaCoreLog(LID.MetaCoreExpressIsNull, m_Token, "express");
            }
            return true;
        }
        public override void ParseRealMetaType()
        {
            if( m_Express != null )
            {
                m_Express = ExpressManager.ConvertNewExpress(m_Express, m_DefineMetaType);
                m_Express.CalcReturnType();

                var enode = SimulateExpressRun(m_Express);
                if (enode != null && enode != m_Express)
                {
                    m_Express = enode;
                    m_Express.CalcReturnType();
                }
                m_RealMetaType = this.m_Express.GetReturnMetaType();
                foreach (var v in m_TemplateChildMetaMemberVariableList)
                {
                    if (!v.isDefineMetaType)
                    {
                        v.m_RealMetaType = m_RealMetaType;
                    }
                }

                //if (this.m_SourceMetaVariable == null)
                {
                    var relation = TypeManager.CompareLeftRightMetaType(m_DefineMetaType, m_RealMetaType, m_Token, out MetaType convertMt);
                    if (relation == false)
                    {
                        Log.AddMetaCoreLog(LID.ShowExtendMessage, m_Token, "Error 表达式中返回定义类型为空 " + m_Express.ToString());
                        return;
                    }
                    else
                    {
                        if (m_Express is MetaConstExpressNode mcen && m_IsDefineMetaType)
                        {
                            var t = m_DefineMetaType.eType;
                            if (t != m_RealMetaType.eType
                                && (t == EType.UInt8
                                || t == EType.Int8
                                || t == EType.Int16
                                || t == EType.UInt16
                                || t == EType.Int32
                                || t == EType.UInt32
                                || t == EType.Int64
                                || t == EType.UInt64
                                || t == EType.Float16
                                || t == EType.Float32
                                || t == EType.Float64))
                            {
                                mcen.SetNumType(t);
                            }
                        }
                    }
                }

            }
        }
        public MetaExpressNodeBase SimulateExpressRun(MetaExpressNodeBase node)
        {
            MetaExpressNodeBase newnode = node;
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
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(permission.ToFormatString() + " ");
            if (isConst)
            {
                sb.Append("const ");
            }
            if (isStatic)
            {
                sb.Append("static ");
            }
            sb.Append(base.ToString());
            if (m_Express != null)
            {
                sb.Append(" = ");
                sb.Append(m_Express.ToString());
            }
            sb.Append(";");

            return sb.ToString();
        }
    }
}
