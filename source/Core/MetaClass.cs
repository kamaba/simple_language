//****************************************************************************
//  File:      MetaClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/6/12 12:00:00
//  Description: Meta class's attribute
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Compile;
using SimpleLanguage.Compile.CoreFileMeta;
using System.Linq;
using SimpleLanguage.Parse;

namespace SimpleLanguage.Core
{
    public enum EClassDefineType
    {
        StructDefine,
        InnerDefine,
        CodeDefine
    }
    public partial class MetaClass : MetaBase
    {
        public virtual string allClassName=> this.m_AllName;

        public EType eType => m_Type;
        public EClassDefineType classDefineType => m_ClassDefineType;
        public MetaClass extendClass => m_ExtendClass;
        public MetaType extendClassMetaType => m_ExtendClassMetaType;
        public int extendLevel => m_ExtendLevel;
        public bool isInterfaceClass => m_IsInterfaceClass;
        public List<MetaClass> interfaceClass => m_InterfaceClass;
        public MetaExpressNode defaultExpressNode => m_DefaultExpressNode;
        public Dictionary<string, MetaMemberVariable> allMetaMemberVariableDict
        {
            get
            {
                Dictionary<string, MetaMemberVariable> allMetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>(m_MetaMemberVariableDict);
                allMetaMemberVariableDict = allMetaMemberVariableDict.Concat(m_MetaExtendMemeberVariableDict).ToDictionary(k => k.Key, v => v.Value);
                return allMetaMemberVariableDict;
            }
        }
        public List<MetaMemberVariable> allMetaMemberVariableList
        {
            get
            {
                List<MetaMemberVariable> allMetaMemberVariableList = new List< MetaMemberVariable>(m_MetaMemberVariableDict.Count + m_MetaExtendMemeberVariableDict.Count);

                foreach (var v in allMetaMemberVariableDict)
                {
                    allMetaMemberVariableList.Add(v.Value);
                }
                return allMetaMemberVariableList;
            }
        }
        public List<MetaMemberFunction> nonStaticVirtualMetaMemberFunctionList => m_NonStaticVirtualMetaMemberFunctionList;
        public List<MetaMemberFunction> staticMetaMemberFunctionList => m_StaticMetaMemberFunctionList;
        public Dictionary<string, MetaMemberVariable> metaMemberVariableDict => m_MetaMemberVariableDict;
        public Dictionary<string, MetaMemberFunctionTemplateNode> metaMemberFunctionTemplateNodeDict => m_MetaMemberFunctionTemplateNodeDict;
        public Dictionary<string, MetaMemberVariable> metaExtendMemeberVariableDict => m_MetaExtendMemeberVariableDict;
        public List<MetaType> bindStructTemplateMetaClassList => m_BindStructTemplateMetaClassList;
        public Dictionary<Token, FileMetaClass> fileMetaClassDict => m_FileMetaClassDict;
        public bool isHandleExtendVariableDirty { get; set; } = false;


        protected int m_ExtendLevel = 0;
        protected EType m_Type = EType.None;
        protected Dictionary<Token, FileMetaClass> m_FileMetaClassDict = new Dictionary<Token, FileMetaClass>();
        protected MetaClass m_ExtendClass = null;
        protected MetaType m_ExtendClassMetaType = null;
        protected List<MetaType> m_BindStructTemplateMetaClassList = new List<MetaType>();
        protected List<MetaClass> m_InterfaceClass = new List<MetaClass>();
        protected List<MetaType> m_InterfaceMetaType = new List<MetaType>();
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected List<MetaMemberVariable> m_FileCollectMetaMemberVariable = new List<MetaMemberVariable>();
        protected Dictionary<string, MetaMemberVariable> m_MetaExtendMemeberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberFunctionTemplateNode> m_MetaMemberFunctionTemplateNodeDict = new Dictionary<string, MetaMemberFunctionTemplateNode>();
        //protected List<MetaMemberFunction> m_CurrentClassMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaMemberFunction> m_FileCollectMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaMemberFunction> m_NonStaticVirtualMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected List<MetaMemberFunction> m_StaticMetaMemberFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        //protected List<MetaMemberFunction> m_AllMetaMemberFunctionList = new List<MetaMemberFunction>();
        protected List<MetaMemberFunction> m_TempInnerFunctionList = new List<MetaMemberFunction>();// inner temp add , after combine to m_MetaMemberFunctionListDict 
        protected MetaExpressNode m_DefaultExpressNode = null;
        protected EClassDefineType m_ClassDefineType = EClassDefineType.InnerDefine;
        protected bool m_IsInterfaceClass = false;

        protected MetaClass()
        {

        }
        public MetaClass(string _name, EClassDefineType ecdt )
        {
            m_Name = _name;
            m_Type = EType.Class;
            m_ClassDefineType = ecdt;
            this.m_AllName = _name;
        }
        public MetaClass(string _name, EType _type  = EType.Class )
        {
            m_Name = _name;
            m_Type = _type;
            this.m_AllName = _name;
        }       
        public MetaClass( MetaClass mc ) : base(mc)
        {
            m_Name = mc.m_Name;
            this.m_AllName = mc.m_AllName;
            m_Type = mc.m_Type;
            m_FileMetaClassDict = mc.m_FileMetaClassDict;
            m_ExtendClass = mc.m_ExtendClass;
            if(m_ExtendClass != null )
            {
                m_ExtendLevel = m_ExtendClass.m_ExtendLevel;
            }
            m_InterfaceClass = mc.m_InterfaceClass;

            m_MetaMemberVariableDict = mc.m_MetaMemberVariableDict;
            m_FileCollectMetaMemberVariable = mc.m_FileCollectMetaMemberVariable;
            m_MetaExtendMemeberVariableDict = mc.m_MetaExtendMemeberVariableDict;

            m_MetaMemberFunctionTemplateNodeDict = mc.m_MetaMemberFunctionTemplateNodeDict;
            //m_CurrentClassMetaMemberFunctionList = mc.m_CurrentClassMetaMemberFunctionList;
            m_FileCollectMetaMemberFunctionList = mc.m_FileCollectMetaMemberFunctionList;
            m_NonStaticVirtualMetaMemberFunctionList = mc.m_NonStaticVirtualMetaMemberFunctionList;
            m_StaticMetaMemberFunctionList = mc.m_StaticMetaMemberFunctionList;
            m_DefaultExpressNode = mc.m_DefaultExpressNode;
        }
        public override void SetDeep( int deep )
        {
            this.m_Deep = deep;                        
            foreach (var v in m_MetaExtendMemeberVariableDict)
            {
                v.Value.SetDeep(deep + 1);
            }
            foreach (var v in m_MetaMemberVariableDict)
            {
                v.Value.SetDeep(deep + 1);
            }
            foreach (var v in m_NonStaticVirtualMetaMemberFunctionList)
            {
                v.SetDeep(deep + 1);
            }
            foreach (var v in m_StaticMetaMemberFunctionList)
            {
                v.SetDeep(deep + 1);
            }
        }
        public void UpdateClassAllName()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_MetaNode.GetAllName());

            if( m_MetaTemplateList.Count > 0 )
            {
                sb.Append("<");
                for( int i = 0; i < m_MetaTemplateList.Count; i++ )
                {
                    sb.Append(m_MetaTemplateList[i].name);
                    if(m_MetaTemplateList[i].extendsMetaClass != null )
                    {
                        sb.Append(":");
                        sb.Append(m_MetaTemplateList[i].extendsMetaClass.allClassName);
                    }
                    if( i < m_MetaTemplateList.Count - 1 )
                    {
                        sb.Append(",");
                    }
                }
                sb.Append(">");
            }

            this.m_AllName = sb.ToString();
        }
        public void SetDefaultExpressNode( MetaExpressNode defaultExpressNode )
        {
            m_DefaultExpressNode = defaultExpressNode;
        }
        public virtual void Parse()
        {
        //    ParseExtendsRelation();
        //    ParseTemplateRelation();
        //    HandleExtendData();
        }
        public virtual void ParseInnerVariable()
        {
        }
        public virtual void ParseInnerFunction()
        {
        }
        public virtual void ParseInner()
        {
            ParseInnerVariable();
            ParseInnerFunction();
        }
        public void SetClassDefineType( EClassDefineType ecdt )
        {
            var type = typeof(MetaClass);

            this.m_ClassDefineType = ecdt;
        }
        public virtual void ParseExtendsRelation()
        {
            if( this.classDefineType == EClassDefineType.InnerDefine )
            {
                return;
            }
            if( CoreMetaClassManager.IsIncludeMetaClass( this ) )
            {
                return;
            }

            if (this.m_ExtendClassMetaType != null)
            {
                Log.AddInStructMeta(EError.None, "已绑定过了继承类 : " + extendClass.name );
                return;
            }
            foreach( var v in m_FileMetaClassDict )
            {
                var mc = v.Value;
                if(mc.fileMetaExtendClass == null )
                {
                    continue;
                }
                if(this.m_ExtendClassMetaType != null )
                {
                    Log.AddInStructMeta(EError.None, "已绑定过了继承类 : " + mc.metaClass.extendClass.name );
                    continue;
                }

                MetaType getmt = TypeManager.instance.GetMetaTemplateClassAndRegisterExptendTemplateClassInstance( this, mc.fileMetaExtendClass );
                if (getmt != null)
                {
                    this.m_ExtendClassMetaType = getmt;
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "没有发现继承类的类型!!! " + mc.metaClass.extendClass.name );
                }
            }

            if(m_ExtendClassMetaType == null && this != CoreMetaClassManager.objectMetaClass )
            {
                m_ExtendClassMetaType = new MetaType( CoreMetaClassManager.objectMetaClass );
            }

            if (!m_ExtendClassMetaType.IsIncludeTemplate())
            {
                this.m_ExtendClass = this.m_ExtendClassMetaType.metaClass;
            }
            else
            {
                this.m_ExtendClass = m_ExtendClassMetaType.metaClass;
            }
        }
        public virtual void ParseInterfaceRelation()
        {
            m_InterfaceMetaType.Clear();
            foreach ( var v in this.fileMetaClassDict )
            {
                for( int i = 0; i < v.Value.interfaceClassList.Count; i++ )
                {
                    var icd = v.Value.interfaceClassList[i];

                    MetaType getmt = TypeManager.instance.GetMetaTemplateClassAndRegisterExptendTemplateClassInstance(this, icd );
                    if (getmt == null )
                    {
                        Log.AddInStructMeta(EError.None, "没有找到接口相关的定义类!!");
                        continue;
                    }
                    m_InterfaceMetaType.Add(getmt);
                }
            }
            if (this.m_MetaTemplateList.Count == 0)
            {
                for( int i = 0; i < m_InterfaceMetaType.Count; i++ )
                {
                    AddInterfaceClass(m_InterfaceMetaType[i].metaClass);
                }
            }
        }
        public virtual void HandleExtendMemberVariable()
        {
            if(m_ExtendClass == null )
            {
                foreach( var v in this.m_FileCollectMetaMemberVariable )
                {
                    m_MetaMemberVariableDict.Add(v.name, v);
                }
                return;
            }
            else
            {
                foreach (var v in m_ExtendClass.m_MetaExtendMemeberVariableDict)
                {
                    var c = v.Value;
                    if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                    {
                        var ld = Log.AddInStructMeta(EError.None, $"Error 继承的类123:{m_AllName} 在继承的父类{m_ExtendClass?.m_AllName} 中已包含:{c.name} ");
                        ld.valDict.Add(EMetaType.MetaClass, this);
                        ld.valDict.Add(EMetaType.MetaExtendsClass, m_ExtendClass);
                        ld.valDict.Add(EMetaType.MetaMemberVariable, c);
                        continue;
                    }
                    this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
                }
                foreach (var v in m_ExtendClass.m_MetaMemberVariableDict)
                {
                    var c = v.Value;
                    if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                    {
                        var ld = Log.AddInStructMeta(EError.None, $"Error 继承的类123:{m_AllName} 在继承的父类{m_ExtendClass?.m_AllName} 中已包含:{c.name} ");
                        ld.valDict.Add(EMetaType.MetaClass, this);
                        ld.valDict.Add(EMetaType.MetaExtendsClass, m_ExtendClass);
                        ld.valDict.Add(EMetaType.MetaMemberVariable, c);
                        continue;
                    }
                    this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
                }
                foreach (var c in this.m_FileCollectMetaMemberVariable)
                {
                    if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                    {
                        Log.AddInStructMeta(EError.None, $"Error 继承的类321:{m_AllName} 在继承的父类{m_ExtendClass.m_AllName} 中已包含:{c.name} ");
                        continue;
                    }
                    this.m_MetaMemberVariableDict.Add(c.name, c);
                }
            }
        }
        public virtual void HandleExtendMemberFunction()
        {
            if (this.m_ExtendClass == null)
            {
                foreach (var v in m_FileCollectMetaMemberFunctionList )
                {
                    if (v.isStatic)
                    {
                        m_StaticMetaMemberFunctionList.Add(v);
                    }
                    else
                    {
                        if (v.isWithInterface) continue;
                        //if (v.isConstructInitFunction) continue;
                        m_NonStaticVirtualMetaMemberFunctionList.Add(v);
                    }
                }
            }
            else
            {

                bool canAdd = false;
                foreach (var v in this.m_ExtendClass.m_NonStaticVirtualMetaMemberFunctionList)
                {
                    canAdd = true;
                    var efun = v;
                    //if (efun.isConstructInitFunction) { continue; }

                    foreach (var v2 in m_FileCollectMetaMemberFunctionList)
                    {
                        //if (v2.isConstructInitFunction) continue;
                        if (efun.IsEqualMetaFunction(v2))
                        {
                            canAdd = false;
                            m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                            continue;
                        }                        
                    }
                    if (canAdd)
                    {
                        m_NonStaticVirtualMetaMemberFunctionList.Add(efun);
                    }
                }

                foreach (var v2 in this.m_FileCollectMetaMemberFunctionList)
                {
                    if (v2.isStatic)
                    {
                        var find = m_StaticMetaMemberFunctionList.Find(a => a == v2);
                        if (find != null) continue;

                        m_StaticMetaMemberFunctionList.Add(v2);
                    }
                    else
                    {
                        var find = m_NonStaticVirtualMetaMemberFunctionList.Find(a => a == v2);
                        if (find != null) continue;

                        m_NonStaticVirtualMetaMemberFunctionList.Add(v2);
                    }
                }
            }




            foreach (var v2 in m_NonStaticVirtualMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }
            foreach (var v2 in m_StaticMetaMemberFunctionList)
            {
                //var find = m_AllMetaMemberFunctionList.Find(a => a == v2);
                //if (find != null) continue;

                AddMetaMemberFunction(v2);
                //m_AllMetaMemberFunctionList.Add(v2);
            }


            List<MetaMemberFunction> addList = new List<MetaMemberFunction>();
            for (int i = 0; i < this.m_TempInnerFunctionList.Count; i++)
            {
                MetaMemberFunction mmf = m_TempInnerFunctionList[i];

                bool isAdd = true;
                if (m_MetaMemberFunctionTemplateNodeDict.ContainsKey(mmf.name))
                {
                    var list = m_MetaMemberFunctionTemplateNodeDict[mmf.name];
                    MetaMemberFunction curFun = list.IsSameMetaMemeberFunction(mmf);
                    if (curFun != null)
                    {
                        isAdd = false;
                        if (mmf.isCanRewrite)
                        {
                            //int index = list.IndexOf(curFun);
                            //list[index] = mmf;
                        }
                        else
                        {
                            isAdd = true;
                            break;
                        }
                    }
                }
                if (isAdd)
                {
                    addList.Add(mmf);
                }
            }
            for (int i = 0; i < addList.Count; i++)
            {
                var v = addList[i];
                if (v.isStatic)
                {
                    m_StaticMetaMemberFunctionList.Add(v);
                }
                else
                {
                    m_NonStaticVirtualMetaMemberFunctionList.Add(v);
                }
            }
            m_TempInnerFunctionList.Clear();
        }
        public virtual void ParseMemberVariableDefineMetaType()
        {
            foreach (var it in this.m_FileCollectMetaMemberVariable )
            {
                it.ParseDefineMetaType();
            }
        }
        public virtual void ParseMemberFunctionDefineMetaType()
        {
            foreach (var it in m_FileCollectMetaMemberFunctionList )
            {
                it.ParseDefineMetaType();
            }
        }
        public bool CheckInterface()
        {
            return true;
        }
        public MetaType AddMetaPreTemplateClass( MetaType mt, bool isParse, out bool isGenMetaClass )
        {
            isGenMetaClass = false;
            if ( mt.metaClass == null )
            {
                return null;
            }
            MetaGenTemplateClass mgtc = mt.metaClass.AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList( mt.templateMetaTypeList);

            if ( mgtc  != null )
            {
                isGenMetaClass = true;
                if(isParse )
                {
                    mgtc.Parse();
                }
                return new MetaType(mgtc, mt.templateMetaTypeList);
            }

            var find = BindStructTemplateMetaClassList( mt );
            if( find == null )
            {
                this.m_BindStructTemplateMetaClassList.Add(new MetaType(mt) );
            }
            return mt;
        }
        public MetaGenTemplateClass AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList( List<MetaType> templateMetaTypeList )
        {
            List<MetaClass> mcList = new List<MetaClass>();
            for (int i = 0; i < templateMetaTypeList.Count; i++)
            {
                var mtc = templateMetaTypeList[i];
                if (mtc.eType == EMetaTypeType.MetaClass
                    || mtc.eType == EMetaTypeType.MetaGenClass )
                {
                    mcList.Add(mtc.metaClass);
                }
            }
            if (mcList.Count == templateMetaTypeList.Count)
            {
                MetaGenTemplateClass mgtc = AddInstanceMetaClass(mcList);
                return mgtc;
            }
            return null;
        }
        public MetaType BindStructTemplateMetaClassList( MetaType mt )
        {
            foreach( var v in m_BindStructTemplateMetaClassList )
            {
                if(MetaType.EqualMetaDefineType(v,mt ) )
                {
                    return v;
                }
            }
            return null;
        }
#if EditorMode
        public void BindFileMetaClass(FileMetaClass fmc)
        {
            if (m_FileMetaClassDict.ContainsKey(fmc.token))
            {
                return;
            }
            fmc.SetMetaClass(this);
            m_FileMetaClassDict.Add(fmc.token, fmc);

            if(m_IsInterfaceClass == false )
            {
                m_IsInterfaceClass = fmc.preInterfaceToken != null;
            }
        }
        public void ParseFileMetaClassMemeberVarAndFunc( FileMetaClass fmc )
        {
            bool isHave = false;
            foreach (var v2 in fmc.memberVariableList)
            {
                var mn = m_MetaNode.GetChildrenMetaNodeByName(v2.name);
                if( mn != null )
                {
                    Log.AddInStructMeta(EError.None, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    continue;
                }

                MetaMemberVariable cmmv = GetMetaMemberVariableByName(v2.name);
                if (cmmv != null)
                {
                    if(cmmv != null && cmmv.isInnerDefine )
                    {
                        break;
                    }
                    else
                    {
                        Log.AddInStructMeta(EError.None, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    }
                    isHave = true;
                }
                else
                    isHave = false;
                MetaMemberVariable mmv = new MetaMemberVariable(this, v2);
                if (isHave)
                {
                    mmv.SetName(mmv.name + "__repeat__");
                }
                m_FileCollectMetaMemberVariable.Add(mmv);
                MetaVariableManager.instance.AddMetaMemberVariable(mmv);
            }
            foreach (var v2 in fmc.memberFunctionList)
            {
                var mn = this.m_MetaNode.GetChildrenMetaNodeByName(v2.name);
                if (mn != null)
                {
                    Log.AddInStructMeta(EError.None, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    continue;
                }

                MetaMemberFunction mmf = new MetaMemberFunction( this, v2 );
                m_FileCollectMetaMemberFunctionList.Add(mmf);
                MethodManager.instance.AddOriginalMemeberFunction(mmf);
            }            
        }
        //解析 自动构建函数  
        public virtual void ParseDefineComplete()
        {
            if(m_IsInterfaceClass )
            {
                return;
            }
            AddDefineConstructFunction();
            if (m_DefaultExpressNode == null )
            {
                MetaType mdt = new MetaType(this);
                if( eType == EType.Data )
                {
                    return;
                }
                var defaultFunction = GetMetaMemberConstructDefaultFunction();
                if (defaultFunction == null)
                {
                    Log.AddInStructMeta(EError.None, "没有找发现默认构造函数");
                    return;
                }
                m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, defaultFunction.metaBlockStatements);
            }
        }
#endif        
        public void SetExtendClass(MetaClass sec)
        {
            this.m_ExtendClass = sec;
        }
        public void CalcExtendLevel()
        {
            if( this.m_ExtendClass == null )
            {
                m_ExtendLevel = 0;
            }
            else
            {
                MetaClass mc = m_ExtendClass;
                int level = 0;
                while (mc != null)
                {
                    level++;
                    mc = mc.extendClass;
                }
                m_ExtendLevel = level;
            }
        }
        public bool IsParseMetaClass(MetaClass parentClass, bool isIncludeSelf = true )
        {
            MetaClass mc = isIncludeSelf ? this : this.m_ExtendClass;
            while( mc != null )
            {
                if (mc == CoreMetaClassManager.objectMetaClass)
                    break;
                
                if( mc == parentClass)
                {
                    return true;
                }
                mc = mc.m_ExtendClass;
            }
            return false;
        }
        public void AddInterfaceClass(MetaClass aic)
        {
            if (!m_InterfaceClass.Contains(aic))
            {
                m_InterfaceClass.Add(aic);
            }
            else
            {
                Log.AddInStructMeta(EError.None, "重复添加接口");
            }
        }
        public void AddMetaMemberVariable( MetaMemberVariable mmv, bool isAddManager = true )
        {
            if( m_MetaMemberVariableDict.ContainsKey( mmv.name ) )
            {
                return;
            }
            m_MetaMemberVariableDict.Add(mmv.name, mmv);
            //AddMetaBase(mmv.name, mmv);
            if( isAddManager )
            {
                MetaVariableManager.instance.AddMetaMemberVariable(mmv);
            }
        }
        public void AddInnerMetaMemberFunction( MetaMemberFunction mmf )
        {
            m_TempInnerFunctionList.Add(mmf);
        }
        public bool AddMetaMemberFunction( MetaMemberFunction mmf )
        {
            MetaMemberFunctionTemplateNode find = null;
            if(this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(mmf.name ) )
            {
                find = m_MetaMemberFunctionTemplateNodeDict[mmf.name];                
            }
            else
            {
                find = new MetaMemberFunctionTemplateNode();
                m_MetaMemberFunctionTemplateNodeDict.Add(mmf.name, find);
            }
            if( find.AddMetaMemberFunction(mmf) )
            {
                //m_CurrentClassMetaMemberFunctionList.Add(mmf);
                //m_AllMetaMemberFunctionList.Add(mmf);
                return true;
            }
            return false;
        }
        public void AddDefineConstructFunction()
        {
            MetaMemberFunction mmf = GetMetaMemberConstructDefaultFunction();
            if (mmf == null)
            {
                mmf = new MetaMemberFunction(this, "_init_");
                mmf.SetReturnMetaClass(CoreMetaClassManager.voidMetaClass);
                mmf.Parse();
                AddMetaMemberFunction(mmf);
                MethodManager.instance.AddOriginalMemeberFunction(mmf);
            }
        }
        public void AddDefineInstanceValue()
        {
            MetaMemberVariable mmv = this.GetMetaMemberVariableByName( "instance" );
            if (mmv == null)
            {
                mmv = new MetaMemberVariable(this, "instance");
                mmv.SetDefineMetaClass(this);
                AddMetaMemberVariable(mmv);
            }
        }
        public void HandleExtendClassVariable()
        {
            isHandleExtendVariableDirty = true;
            if ( m_ExtendClass != null)
            {
                foreach (var v in m_ExtendClass.m_MetaMemberVariableDict)
                {
                    m_MetaExtendMemeberVariableDict.Add(v.Key,v.Value);
                }
            }
        }
        public virtual MetaMemberVariable GetMetaMemberVariableByName(string name)
        {
            if (m_MetaMemberVariableDict.ContainsKey(name))
            {
                return m_MetaMemberVariableDict[name];
            }
            if (m_MetaExtendMemeberVariableDict.ContainsKey(name))
            {
                return m_MetaExtendMemeberVariableDict[name];
            }
            return null;
        }
        public virtual List<MetaMemberVariable>  GetMetaMemberVariableListByFlag( bool isStatic )
        {
            List<MetaMemberVariable> mmvList = new List<MetaMemberVariable>();
            MetaMemberVariable tempMmv = null;
            foreach (var v in m_MetaMemberVariableDict)
            {
                tempMmv = v.Value;
                if( isStatic )
                {
                    if ( tempMmv.isStatic == isStatic || tempMmv.isConst == isStatic)
                    {
                        mmvList.Add(tempMmv);
                    }
                }
                else
                {
                    if( tempMmv.isStatic == false && tempMmv.isConst == false )
                    {
                        mmvList.Add(tempMmv);
                    }
                }
            }
            foreach (var v in m_MetaExtendMemeberVariableDict)
            {
                tempMmv = v.Value;
                if (isStatic)
                {
                    if (tempMmv.isStatic == isStatic || tempMmv.isConst == isStatic)
                    {
                        mmvList.Add(tempMmv);
                    }
                }
                else
                {
                    if (tempMmv.isStatic == false && tempMmv.isConst == false)
                    {
                        mmvList.Add(tempMmv);
                    }
                }
            }
            return mmvList;
        }
        public virtual MetaMemberFunction GetMetaDefineGetSetMemberFunctionByName(string name, MetaInputParamCollection inputParam, bool isGet, bool isSet)
        {
            if (!m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name))
            {
                return null;
            }
            var tnode = m_MetaMemberFunctionTemplateNodeDict[name];
            

            if (!tnode.metaTemplateFunctionNodeDict.ContainsKey(0))
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[0];

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(inputParam != null ? inputParam.count : 0);
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                if (fun.isTemplateFunction)
                {
                    return fun;
                }
                else
                {
                    if (fun.IsEqualMetaInputParamCollection(inputParam))
                        return fun;
                }
            }
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputTemplateInputParamCount(string name, int templateParamCount, MetaInputParamCollection inputParam, bool isIncludeExtendClass = true )
        {
            if (!this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name) )
            {
                return null;
            }
            var tnode = this.m_MetaMemberFunctionTemplateNodeDict[name];
            if( !tnode.metaTemplateFunctionNodeDict.ContainsKey(templateParamCount) )
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[templateParamCount];

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(inputParam != null ? inputParam.count : 0 );
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                if (fun.isTemplateFunction)
                {
                    return fun;
                }
                else
                {
                    if (fun.IsEqualMetaInputParamCollection(inputParam))
                        return fun;
                }
            }
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputTemplateInputParam(string name, List<MetaClass> mtList, MetaInputParamCollection inputParam, bool isIncludeExtendClass = true)
        {
            if (!this.m_MetaMemberFunctionTemplateNodeDict.ContainsKey(name))
            {
                return null;
            }
            int templateParamCount = 0;
            if( mtList != null )
            {
                templateParamCount = mtList.Count;
            }
            var tnode = this.m_MetaMemberFunctionTemplateNodeDict[name];
            if (!tnode.metaTemplateFunctionNodeDict.ContainsKey(templateParamCount))
            {
                return null;
            }
            var tfunctionNode = tnode.metaTemplateFunctionNodeDict[templateParamCount];

            var list = tfunctionNode.GetMetaMemberFunctionListByParamCount(inputParam != null ? inputParam.count : 0);
            if (list == null) return null;

            for (int i = 0; i < list.Count; i++)
            {
                var fun = list[i];
                if (fun.isTemplateFunction)
                {
                    var gfun = fun.GetGenTemplateFunction(mtList);

                    if( gfun != null )
                    {
                        return gfun;
                    }
                    return fun;
                }
                else
                {
                    if (fun.IsEqualMetaInputParamCollection(inputParam))
                        return fun;
                }
            }
            return null;
        }
        public MetaMemberFunction GetMetaMemberConstructDefaultFunction()
        {
            return GetMetaMemberConstructFunction(null);
        }
        public virtual MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection mmpc )
        {
            return GetMetaMemberFunctionByNameAndInputTemplateInputParamCount("_init_", 0, mmpc, false );
        }
        public MetaMemberFunction GetFirstMetaMemberFunctionByName( string name )
        {
            return GetMetaMemberFunctionByNameAndInputTemplateInputParamCount( name, 0, null );
        }
        public List<MetaMemberFunction> GetMemberInterfaceFunction()
        {
            List<MetaMemberFunction> mmf = new List<MetaMemberFunction>();

            //foreach( var v in m_MetaMemberFunctionDict )
            //{
            //    var fun = v.Value;
            //    if (fun.isWithInterface)
            //    {
            //        mmf.Add(fun);
            //    }
            //}
            return mmf;
        }
        public bool GetMemberInterfaceFunctionByFunc( MetaMemberFunction func )
        {
            //foreach( var v in m_MetaMemberFunctionDict )
            //{
            //    if (v.Value.Equals(func))
            //    {
            //        return true;
            //    }
            //}
            return true;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (this.isGenTemplate)
            {
                stringBuilder.Append(" [Gen] ");
            }
            else
            {
                if (this.isTemplateClass)
                {
                    stringBuilder.Append(" [Template] ");
                }
            }              

            stringBuilder.Append(allClassName);

            return stringBuilder.ToString();
        }
        public override string ToFormatString()
        {
            return GetFormatString(false);
        }
        public virtual string ToDefineTypeString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(allClassName);

            return stringBuilder.ToString();

        }
        public string GetFormatString( bool isShowNamespace )
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append(permission.ToFormatString());
            stringBuilder.Append(" ");
            if (isShowNamespace)
            {
                stringBuilder.Append("class ");
                //if (topLevelMetaNamespace != null)
                //{
                //    stringBuilder.Append(topLevelMetaNamespace.allName + ".");
                //}
                stringBuilder.Append(name);
            }
            else
            {
                stringBuilder.Append("class " + name);
            }
            if (m_MetaTemplateList.Count > 0)
            {
                stringBuilder.Append("<");
                for (int i = 0; i < m_MetaTemplateList.Count; i++)
                {
                    stringBuilder.Append(m_MetaTemplateList[i].ToFormatString());
                    if (i < m_MetaTemplateList.Count - 1)
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append(">");
            }
            if (m_ExtendClass != null)
            {
                stringBuilder.Append(" extends ");
                stringBuilder.Append(m_ExtendClass.allClassName);
                var mtl = m_ExtendClass.metaTemplateList;
                if (mtl.Count > 0)
                {
                    stringBuilder.Append("<");
                    for (int i = 0; i < mtl.Count; i++)
                    {
                        stringBuilder.Append(mtl[i].ToFormatString());
                        if (i < mtl.Count - 1)
                        {
                            stringBuilder.Append(",");
                        }
                    }
                    stringBuilder.Append(">");
                }
            }
            if (m_InterfaceMetaType.Count > 0)
            {
                stringBuilder.Append(" interface ");
            }
            for (int i = 0; i < m_InterfaceMetaType.Count; i++)
            {
                stringBuilder.Append(m_InterfaceMetaType[i].ToFormatString() );
                if (i != m_InterfaceClass.Count - 1)
                    stringBuilder.Append(",");
            }
            stringBuilder.Append(Environment.NewLine);

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("{" + Environment.NewLine);

            foreach (var v2 in m_MetaNode.childrenMetaNodeDict )
            {
                stringBuilder.Append(v2.Value.ToFormatString());
            }

            foreach (var v in m_MetaMemberVariableDict )
            {
                stringBuilder.AppendLine(v.Value.ToFormatString());
            }

            foreach (var v in m_StaticMetaMemberFunctionList )
            {
                MetaMemberFunction mmfc = v;
                stringBuilder.Append(mmfc.ToFormatString());
                stringBuilder.Append(Environment.NewLine);
            }

            foreach (var v in m_NonStaticVirtualMetaMemberFunctionList)
            {
                MetaMemberFunction mmfc = v;
                stringBuilder.Append(mmfc.ToFormatString());
                stringBuilder.Append(Environment.NewLine);
            }
            //if( m_MetaGenTemplateClassList.Count > 0 )
            //{
            //    for (int i = 0; i <= realDeep; i++)
            //        stringBuilder.Append(Global.tabChar);
            //    stringBuilder.AppendLine("------------Generator Template List-------------");
            //    for (int i = 0; i < m_MetaGenTemplateClassList.Count; i++)
            //    {
            //        stringBuilder.Append(m_MetaGenTemplateClassList[i].ToFormatString());
            //        stringBuilder.Append(Environment.NewLine);
            //    }
            //}

            for (int i = 0; i < realDeep; i++)
                stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
