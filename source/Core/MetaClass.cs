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
using System.Xml.Linq;

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
        public Dictionary<string, MetaMemberVariable> metaMemberVariableDict => m_MetaMemberVariableDict;
        public Dictionary<string, MetaMemberFunctionTemplateNode> metaMemberFunctionTemplateNodeDict => m_MetaMemberFunctionTemplateNodeDict;
        public Dictionary<string, MetaMemberVariable> metaExtendMemeberVariableDict => m_MetaExtendMemeberVariableDict;
        public Dictionary<Token, FileMetaClass> fileMetaClassDict => m_FileMetaClassDict;
        public bool isHandleExtendVariableDirty { get; set; } = false;


        protected int m_ExtendLevel = 0;
        protected EType m_Type = EType.None;
        protected Dictionary<Token, FileMetaClass> m_FileMetaClassDict = new Dictionary<Token, FileMetaClass>();
        protected MetaClass m_ExtendClass = null;
        protected MetaType m_ExtendClassMetaType = null;
        protected List<MetaClass> m_InterfaceClass = new List<MetaClass>();
        protected List<MetaType> m_InterfaceMetaType = new List<MetaType>();
        protected Dictionary<string, MetaMemberVariable> m_MetaMemberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberVariable> m_MetaExtendMemeberVariableDict = new Dictionary<string, MetaMemberVariable>();
        protected Dictionary<string, MetaMemberFunctionTemplateNode> m_MetaMemberFunctionTemplateNodeDict = new Dictionary<string, MetaMemberFunctionTemplateNode>();
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
        public MetaClass( MetaClass mc )
        {
            m_Name = mc.m_Name;
            this.m_AllName = m_Name;
            m_Type = mc.m_Type;
            m_FileMetaClassDict = mc.m_FileMetaClassDict;
            m_ExtendClass = mc.m_ExtendClass;
            if(m_ExtendClass != null )
            {
                m_ExtendLevel = m_ExtendClass.m_ExtendLevel + 1;
            }
            m_InterfaceClass = mc.m_InterfaceClass;
            //m_ChildrenMetaClassDict = mc.m_ChildrenMetaClassDict;

            m_MetaMemberVariableDict = mc.m_MetaMemberVariableDict;
            m_MetaExtendMemeberVariableDict = mc.m_MetaExtendMemeberVariableDict;
            m_MetaMemberFunctionTemplateNodeDict = mc.m_MetaMemberFunctionTemplateNodeDict;
            m_DefaultExpressNode = mc.m_DefaultExpressNode;
        }
        public void SetDeep( int deep )
        {
            //m_Deep = deep;

            //foreach( var v in m_ChildrenMetaClassDict )
            //{
            //    //v.Value.SetDeep(deep + 1);
            //}

            //foreach( var v in m_MetaMemberVariableDict )
            //{
            //    v.Value.SetDeep(deep + 1);
            //}
            //foreach( var v in m_MetaMemberFunctionTemplateNodeDict )
            //{
            //    //v.Value.SetDeep(deep + 1);
            //}
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
            if (this.extendClass != null)
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
            if( this.m_MetaTemplateList.Count == 0 && this.m_ExtendClassMetaType != null )
            {
                this.m_ExtendClass = this.m_ExtendClassMetaType.metaClass;
            }
        }
        public virtual void UpdateInterfaceMetaClass()
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

        public virtual void HandleExtendData()
        {
            if( this.m_ExtendClass == null )
            {
                return;
            }
            foreach (var v in m_ExtendClass.m_MetaMemberVariableDict)
            {
                var c = v.Value;
                if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                {
                    var ld = Log.AddInStructMeta( EError.None, $"Error 继承的类123:{m_AllName} 在继承的父类{m_ExtendClass?.m_AllName} 中已包含:{c.name} " );
                    ld.valDict.Add(EMetaType.MetaClass, this );
                    ld.valDict.Add(EMetaType.MetaExtendsClass, m_ExtendClass);
                    ld.valDict.Add(EMetaType.MetaMemberVariable, c);
                    continue;
                }
                this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
            }
            foreach (var v in m_ExtendClass.m_MetaExtendMemeberVariableDict )
            {
                var c = v.Value;
                if (this.m_MetaMemberVariableDict.ContainsKey(c.name))
                {
                    Log.AddInStructMeta(EError.None, $"Error 继承的类321:{m_AllName} 在继承的父类{m_ExtendClass.m_AllName} 中已包含:{c.name} ");
                    continue;
                }
                this.m_MetaExtendMemeberVariableDict.Add(c.name, c);
            }
        }
        public virtual void ParseMemberVariableDefineMetaType()
        {
            foreach (var it in m_MetaMemberVariableDict)
            {
                it.Value.ParseDefineMetaType();
            }
        }
        public virtual void ParseMemberTemplateFunction()
        {
            foreach (var it in m_MetaMemberFunctionTemplateNodeDict )
            {
                //if (it.Value.isTemplateFunction)
                //{
                //    it.Value.CreateTemplateChildFunction();
                //}
            }
        }
        public virtual void ParseMemberFunctionDefineMetaType()
        {
            foreach (var it in m_MetaMemberFunctionTemplateNodeDict )
            {
                //if (!it.Value.isTemplateFunction)
                //{
                //    it.Value.ParseDefineMetaType();
                //}
            }
        }
        public bool CheckInterface()
        {
            return true;
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
                AddMetaMemberVariable(mmv);
            }
            foreach (var v2 in fmc.memberFunctionList)
            {
                var mn = m_MetaNode.GetChildrenMetaNodeByName(v2.name);
                if (mn != null)
                {
                    Log.AddInStructMeta(EError.None, "Error MetaClass MemberVarAndFunc已有定义类: " + m_AllName + "中 已有: " + v2.token?.ToLexemeAllString() + "的元素!!");
                    continue;
                }

                MetaMemberFunction mmf = new MetaMemberFunction( this, v2 );
                AddMetaMemberFunction(mmf);
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
            //AddDefineInstanceValue();

            if (m_DefaultExpressNode == null )
            {
                MetaType mdt = new MetaType(this);
                var defaultFunction = GetMetaMemberConstructDefaultFunction();
                MetaMethodCall mfc = null;
                if (defaultFunction != null)
                {
                    mfc = new MetaMethodCall(this, defaultFunction );
                }
                m_DefaultExpressNode = new MetaNewObjectExpressNode(mdt, this, null, mfc);
            }

            List<MetaMemberFunction> addList = new List<MetaMemberFunction>();
            for( int i = 0; i < m_TempInnerFunctionList.Count; i++ ) 
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
                            RemoveMetaMemberFunction(curFun);
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
            for( int i = 0; i < addList.Count; i++ )
            {
                AddMetaMemberFunction(addList[i]);
            }
            m_TempInnerFunctionList.Clear();
        }
#endif
        
        public void SetExtendClass(MetaClass sec)
        {
            m_ExtendClass = sec;
            m_ExtendLevel = m_ExtendClass.m_ExtendLevel + 1;
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
        public void AddMetaMemberFunction( MetaMemberFunction mmf )
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
            find.AddMetaMemberFunction(mmf);
            //AddMetaBase(mmf.functionAllName, mmf);
        }
        public void RemoveMetaMemberFunction( MetaMemberFunction mmf )
        {
            //if (m_MetaMemberFunctionDict.ContainsKey(mmf.name))
            //{
            //    var list = m_MetaMemberFunctionDict[mmf.name];

            //    list.RemoveMetaMemberFunction(mmf);
            //}
            //RemoveMetaBase(mmf);

            //MethodManager.instance.AddMemeberFunction(mmf);
        }
        public void AddDefineConstructFunction()
        {
            MetaMemberFunction mmf = GetMetaMemberConstructDefaultFunction();
            if (mmf == null)
            {
                mmf = new MetaMemberFunction(this, "_init_");
                mmf.SetDefineMetaClass(this);
                AddMetaMemberFunction(mmf);
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
        public List<MetaMemberVariable> GetMetaMemberVariableListByFlag( bool isStatic, bool isConst )
        {
            List<MetaMemberVariable> mmvList = new List<MetaMemberVariable>();
            MetaMemberVariable tempMmv = null;
            foreach (var v in m_MetaMemberVariableDict)
            {
                tempMmv = v.Value;
                if( (tempMmv.isStatic == isStatic) && (tempMmv.isConst==isConst) )
                {
                    mmvList.Add(tempMmv);
                }
            }
            foreach (var v in m_MetaExtendMemeberVariableDict)
            {
                tempMmv = v.Value;
                if ((tempMmv.isStatic == isStatic) && (tempMmv.isConst == isConst))
                {
                    mmvList.Add(tempMmv);
                }
            }
            return mmvList;
        }
        public virtual MetaMemberFunction GetMetaDefineGetSetMemberFunctionByName( string name, bool isGet , bool isSet )
        {
            //if (!m_MetaMemberFunctionDict.ContainsKey(name))
            //{
            //    if (m_ExtendClass != null)
            //    {
            //        var func = m_ExtendClass.GetMetaDefineGetSetMemberFunctionByName(name, isGet, isSet);
            //        if (func != null)
            //        {
            //            return func;
            //        }
            //    }
            //    return null;
            //}
            //var mmf = m_MetaMemberFunctionDict[name];

            //for (int i = 0; i < mmf.Count; i++)
            //{
            //    var fun = mmf[i];
            //    if ( (fun.isGet == isGet) || (fun.isSet == isSet ) )
            //        return fun;
            //}
            return null;
        }
        public virtual MetaMemberFunction GetMetaMemberFunctionByNameAndInputParamCollect(string name, MetaInputParamCollection mmpc, bool isIncludeExtendClass = true )
        {
            //if (!m_MetaMemberFunctionDict.ContainsKey(name))
            //{
            //    if (isIncludeExtendClass  && m_ExtendClass != null )
            //    {
            //        var func = m_ExtendClass.GetMetaMemberFunctionByNameAndInputParamCollect(name, mmpc, isIncludeExtendClass );
            //        if (func != null)
            //        {
            //            return func;
            //        }
            //    }
            //    return null;
            //}
            
            //var mmf = m_MetaMemberFunctionDict[name];
            //for (int i = 0; i < mmf.Count; i++)
            //{
            //    var fun = mmf[i];
            //    if( fun.isTemplateFunction )
            //    {
            //        return fun;
            //    }
            //    else
            //    {
            //        if (fun.IsEqualMetaInputParamCollection(mmpc))
            //            return fun;
            //    }
            //}

            return null;
        }
        //该方法，只能查找Cast<T1>() 模版函数使用 不能用Class<T>{ Fun() } 这种的
        //暂不支持使用模版方法的查找
        //public MetaMemberFunction GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(string name, MetaInputTemplateCollection mitc, MetaInputParamCollection mmpc)
        //{
        //    if (!m_MetaMemberFunctionListDict.ContainsKey(name))
        //    {
        //        if ( m_ExtendClass != null)
        //        {
        //            var func = m_ExtendClass.GetMetaMemberFunctionByNameAndTemplateCollectInputParamCollect(name, mitc, mmpc);
        //            if (func != null)
        //            {
        //                return func;
        //            }
        //        }
        //        return null;
        //    }


        //    var mmf = m_MetaMemberFunctionListDict[name];

        //    for (int i = 0; i < mmf.Count; i++)
        //    {
        //        var fun = mmf[i];
        //        if (fun.IsEqualMetaTemplateCollectionAndMetaParamCollection(mitc, mmpc))
        //            return fun;
        //    }
        //    return null;
        //}
        public MetaMemberFunction GetMetaMemberConstructDefaultFunction()
        {
            return GetMetaMemberConstructFunction(null);
        }
        public virtual MetaMemberFunction GetMetaMemberConstructFunction( MetaInputParamCollection mmpc )
        {
            return GetMetaMemberFunctionByNameAndInputParamCollect("_init_", mmpc, false );
        }
        public MetaMemberFunction GetFirstMetaMemberFunctionByName( string name )
        {
            return GetMetaMemberFunctionByNameAndInputParamCollect( name, null );
        }
        public List<MetaMemberFunction> GetMemberFunctionList()
        {
            List<MetaMemberFunction> mmf = new List<MetaMemberFunction>();

            //foreach( var v in m_MetaMemberFunctionDict )
            //{
            //    mmf.Add(v.Value);
            //}
            return mmf;
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

            if( this.isTemplateClass )
            {
                stringBuilder.Append("<");
                for( int i = 0; i < this.metaTemplateList.Count; i++ )
                {
                    stringBuilder.Append(this.metaTemplateList[i].name);
                    if(i < this.metaTemplateList.Count - 1 )
                    {
                        stringBuilder.Append(",");
                    }
                }
                stringBuilder.Append(">");
            }

            return stringBuilder.ToString();
        }
        public override string ToFormatString()
        {
            return GetFormatString(false);
        }
        public virtual string ToDefineTypeString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(name);

            return stringBuilder.ToString();

        }
        public string GetFormatString( bool isShowNamespace )
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Clear();
            //for (int i = 0; i < realDeep; i++)
            //    stringBuilder.Append(Global.tabChar);
            //stringBuilder.Append(permission.ToFormatString());
            //stringBuilder.Append(" ");
            //if(isShowNamespace)
            //{
            //    stringBuilder.Append("class ");
            //    if (topLevelMetaNamespace != null)
            //    {
            //        stringBuilder.Append(topLevelMetaNamespace.allName + ".");
            //    }
            //    stringBuilder.Append( name);
            //}
            //else
            //{
            //    stringBuilder.Append("class " + name);
            //}
            ////if (m_MetaTemplateList.Count > 0)
            ////{
            ////    stringBuilder.Append("<");
            ////    for (int i = 0; i < m_MetaTemplateList.Count; i++)
            ////    {
            ////        stringBuilder.Append(m_MetaTemplateList[i].ToFormatString());
            ////        if (i < m_MetaTemplateList.Count - 1)
            ////        {
            ////            stringBuilder.Append(",");
            ////        }
            ////    }
            ////    stringBuilder.Append(">");
            ////}
            //if ( m_ExtendClass != null )
            //{
            //    stringBuilder.Append(" extends ");
            //    stringBuilder.Append(m_ExtendClass.allName);
            //    //var mtl = m_ExtendClass.metaTemplateList;
            //    //if( mtl.Count > 0 )
            //    //{
            //    //    stringBuilder.Append("<");
            //    //    for (int i = 0; i < mtl.Count; i++)
            //    //    {
            //    //        stringBuilder.Append(mtl[i].ToFormatString());
            //    //        if (i < mtl.Count - 1)
            //    //        {
            //    //            stringBuilder.Append(",");
            //    //        }
            //    //    }
            //    //    stringBuilder.Append(">");
            //    //}
            //}
            //if( m_InterfaceClass.Count > 0 )
            //{
            //    stringBuilder.Append(" interface ");
            //}
            //for( int i = 0; i < m_InterfaceClass.Count; i++ )
            //{
            //    stringBuilder.Append(m_InterfaceClass[i].allName);
            //    if( i != m_InterfaceClass.Count - 1 )
            //        stringBuilder.Append(",");
            //}
            //stringBuilder.Append(Environment.NewLine);

            //for (int i = 0; i < realDeep; i++)
            //    stringBuilder.Append(Global.tabChar);
            //stringBuilder.Append("{" + Environment.NewLine);

            //foreach (var v in m_ChildrenNameNodeDict)
            //{
            //    MetaBase mb = v.Value;
            //    if (mb is MetaClass)
            //    {
            //        if (mb is MetaEnum)
            //        {
            //            stringBuilder.Append((mb as MetaEnum).ToFormatString());
            //            stringBuilder.Append(Environment.NewLine);
            //        }
            //        else
            //        {
            //            stringBuilder.Append((mb as MetaClass).ToFormatString());
            //            stringBuilder.Append(Environment.NewLine);
            //        }
            //    }
            //    else if (mb is MetaMemberVariable)
            //    {
            //        MetaMemberVariable mmv = mb as MetaMemberVariable;
            //        if( mmv.fromType == EFromType.Code )
            //        {
            //            stringBuilder.Append(mmv.ToFormatString());
            //            stringBuilder.Append(Environment.NewLine);
            //        }
            //    }
            //    else if (mb is MetaMemberFunction)
            //    {
            //        MetaMemberFunction mmfc = mb as MetaMemberFunction;
            //        if( mmfc.methodCallType == EMethodCallType.Local )
            //        {
            //            stringBuilder.Append(mmfc.ToFormatString());
            //            stringBuilder.Append(Environment.NewLine);
            //        }
            //    }
            //    else
            //    {
            //        stringBuilder.Append("Errrrrroooorrr ---" + mb.ToFormatString());
            //        stringBuilder.Append(Environment.NewLine);
            //    }
            //}
            //stringBuilder.Append(Environment.NewLine);
            ////if( m_MetaGenTemplateClassList.Count > 0 )
            ////{
            ////    for (int i = 0; i <= realDeep; i++)
            ////        stringBuilder.Append(Global.tabChar);
            ////    stringBuilder.AppendLine("------------Generator Template List-------------");
            ////    for (int i = 0; i < m_MetaGenTemplateClassList.Count; i++)
            ////    {
            ////        stringBuilder.Append(m_MetaGenTemplateClassList[i].ToFormatString());
            ////        stringBuilder.Append(Environment.NewLine);
            ////    }
            ////}

            //for (int i = 0; i < realDeep; i++)
            //    stringBuilder.Append(Global.tabChar);
            stringBuilder.Append("}" + Environment.NewLine);

            return stringBuilder.ToString();
        }
    }
}
