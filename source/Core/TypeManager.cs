//****************************************************************************
//  File:      TypeManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2023/5/12 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Parse;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleLanguage.Core
{
    public class TypeManager
    {
        public static TypeManager s_Instance = null;
        public static TypeManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new TypeManager();
                }
                return s_Instance;
            }
        }

        public void UpdateMetaType( MetaType mt, MetaGenTemplateClass mgtc )
        {
            bool isNeedReg = false;
            MetaClass findfn = null;
            if ( mt.isTemplate )
            {
                var gmgt = mgtc.GetMetaGenTemplate(mt.metaTemplate.name);
                if( gmgt != null )
                {
                    mt.SetMetaClass(gmgt.metaType.metaClass);
                    mt.SetMetaTemplate(null);
                    findfn = gmgt.metaType.metaClass;
                }
            }
            List<MetaClass> regMCList = new List<MetaClass>();
            if ( mt.templateMetaTypeList.Count > 0 )
            {
                isNeedReg = true;
                for (int i = 0; i < mt.templateMetaTypeList.Count; i++)
                {
                    UpdateMetaType(mt.templateMetaTypeList[i], mgtc);
                    regMCList.Add(mt.templateMetaTypeList[i].metaClass);
                    if (mt.templateMetaTypeList[i].isTemplate)
                    {
                        isNeedReg = false;
                    }
                }
            }
            if (findfn != null && isNeedReg)
            {
                var newmc = findfn.AddInstanceMetaClass(regMCList);
                mt.SetMetaClass(newmc);
            }

        }
        #region 模板类定义处理区
        public MetaType GetMetaTemplateClassAndRegisterExptendTemplateClassInstance(MetaClass curMc, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            var curMc2 = curMc.GetTreeStructNode();

            MetaClass getmc = ClassManager.instance.GetMetaClassByRef(curMc2, fmcd);
            if (getmc == null)
            {
                var mt = curMc.GetMetaTemplateByName(fmcd.stringList[0]);
                if (mt == null)
                {
                    Log.AddInStructMeta(EError.None, $"没有找到模板类中，对应的模板，名称为{fmcd.stringList[0]}请仔细检查模板的命名与使用模板命名是否对应", fmcd.classNameToken );
                }
                else
                {
                    return new MetaType(mt);
                }

            }
            else
            {
                return GetMetaTemplateClassByTemplateList(curMc2, getmc, fmcd.inputTemplateNodeList);
            }
            return null;
        }
        MetaType GetMetaTemplateClassByTemplateList(MetaClass curMc, MetaClass getmc, List<FileInputTemplateNode> inputTemplateNodeList)
        {
            if (inputTemplateNodeList.Count == 0)
            {
                return new MetaType(getmc);
            }
            var findfn = getmc.GetTemplateMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if (findfn != null)
            {
                getmc = findfn;
            }
            var mt = new MetaType(getmc);
            //这里，要注册实体模板类
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                MetaType mt2 = GetAndRegisterTemplateDefineMetaTemplateClass(curMc, findfn, inputTemplateNodeList[i]);

                mt.AddTemplateMetaType(mt2);
            }


            return mt;
        }
        MetaType GetAndRegisterTemplateDefineMetaTemplateClass(MetaClass ownerMc, MetaClass findMc, FileInputTemplateNode fmtd)
        {
            var newmc = ClassManager.instance.GetMetaClassByNameAndFileMeta(ownerMc, fmtd.fileMeta, fmtd.nameList);
            FileMetaCallNode cnode = null;
            if (newmc != null)
            {
                if (fmtd.inputTemplateCount == 0)
                {
                    return new MetaType(newmc);
                }
                var findfn = newmc.GetTemplateMetaClassByTemplateCount(fmtd.inputTemplateCount);

                var mt = new MetaType(findfn);

                List<MetaClass> regMCList = new List<MetaClass>();
                bool isNeedReg = true;
                for (int i = 0; i < fmtd.defineClassCallLink.callNodeList.Count; i++)
                {
                    var dcc = fmtd.defineClassCallLink.callNodeList[i];
                    cnode = dcc;

                    for (int j = 0; j < dcc.inputTemplateNodeList.Count; j++)
                    {
                        var itn = dcc.inputTemplateNodeList[j];
                        var mt2 = GetAndRegisterTemplateDefineMetaTemplateClass(ownerMc, findfn, itn);
                        if (mt2.isTemplate)
                        {
                            isNeedReg = false;
                        }
                        regMCList.Add(mt2.metaClass);
                        mt.AddTemplateMetaType(mt2);
                    }
                }
                if (findfn != null && isNeedReg)
                {
                    newmc = findfn.AddInstanceMetaClass(regMCList);
                    return new MetaType(newmc);
                }
                return mt;
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    var mt = findMc.GetMetaTemplateByName(fmtd.nameList[0]);
                    if (mt == null)
                    {
                        Log.AddInStructMeta(EError.None, "没有找到模板类中，对应的模板，请仔细检查模板的命名与使用模板命名是否对应", cnode?.token );
                    }
                    else
                    {
                        return new MetaType(mt);
                    }
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "使用模板类中使用.连接符号，模板中不允许使用.");
                }
            }
            return new MetaType(newmc);
        }
        #endregion

        #region 模板函数处理区
        public MetaType GetMetaTypeByTemplateFunction(MetaClass curMc, MetaMemberFunction mmf, FileMetaClassDefine fmcd)
        {
            if (fmcd == null) return null;

            MetaClass getmc = ClassManager.instance.GetMetaClassByRef(curMc, fmcd);
            if (getmc == null)
            {
                var gmtbn = curMc.GetMetaTemplateByName(fmcd.stringList[0]);
                if (gmtbn != null)
                {
                    return new MetaType(gmtbn);
                }
                else if( mmf != null )
                {
                    var mt = mmf.GetMetaDefineTemplateByName(fmcd.stringList[0]);
                    return new MetaType(mt);
                }
                else
                {
                    Log.AddInStructMeta(EError.None, $"没有找到{fmcd.stringList[0]} 的相关类!");
                }

            }
            else
            {
                return GetMetaTypeByTemplateList(curMc, getmc, mmf, fmcd.inputTemplateNodeList);
            }
            return null;
        }
        public MetaType GetMetaTypeByTemplateList(MetaClass curMc, MetaClass getmc, MetaMemberFunction mmf, List<FileInputTemplateNode> inputTemplateNodeList)
        {
            if (inputTemplateNodeList.Count == 0)
            {
                return new MetaType(getmc);
            }
            var findfn = getmc.GetTemplateMetaClassByTemplateCount(inputTemplateNodeList.Count);
            if (findfn != null)
            {
                getmc = HandleInputTemplateNodeList(curMc, findfn, mmf, inputTemplateNodeList );
            }
            return new MetaType(getmc);
        }
        MetaClass HandleInputTemplateNodeList( MetaClass curMc, MetaClass findfn, MetaMemberFunction mmf, List<FileInputTemplateNode> inputTemplateNodeList)
        {
            var getmc = findfn;
            List<MetaClass> regMCList = new List<MetaClass>();
            //这里，要注册实体模板类
            for (int i = 0; i < inputTemplateNodeList.Count; i++)
            {
                var t = RegisterTemplateDefineMetaTemplateFunction(curMc, mmf, inputTemplateNodeList[i]);
                if( t.isTemplate == false )
                    regMCList.Add(t.metaClass);
            }
            if (findfn != null)
            {
                if( regMCList.Count == inputTemplateNodeList.Count)
                {
                    getmc = findfn.AddInstanceMetaClass(regMCList);
                }
            }
            return getmc;
        }
        public MetaType RegisterTemplateDefineMetaTemplateFunction(MetaClass ownerMc, MetaMemberFunction mmf, FileInputTemplateNode fmtd)
        {
            var newmc = ClassManager.instance.GetMetaClassByNameAndFileMeta(ownerMc, fmtd.fileMeta, fmtd.nameList);
            if (newmc != null)
            {
                if (fmtd.inputTemplateCount == 0)
                {
                    return new MetaType(newmc);
                }
                var findfn = newmc.GetTemplateMetaClassByTemplateCount(fmtd.inputTemplateCount);

                if( findfn == null )
                {
                    Log.AddInStructMeta(EError.None, "没有找到相对应的模板类!!");
                    return null;
                }
                
                var dcc = fmtd.defineClassCallLink.callNodeList[fmtd.defineClassCallLink.callNodeList.Count - 1];

                var getmc2 = HandleInputTemplateNodeList(ownerMc, findfn, mmf, dcc.inputTemplateNodeList);

                if (getmc2 != null)
                {
                    return new MetaType(getmc2);
                }                
                return new MetaType(newmc);
            }
            else
            {
                if (fmtd.nameList.Count == 1)
                {
                    if( ownerMc != null )
                    {
                        var mgtc2 = ownerMc.GetMetaTemplateByName(fmtd.nameList[0]);
                        if( mgtc2 != null )
                        {
                            return new MetaType(mgtc2);
                        }
                    }
                    if(mmf != null  )
                    {
                        var mt = mmf.GetMetaDefineTemplateByName(fmtd.nameList[0]);
                        if( mt != null )
                        {
                            return new MetaType(mt);
                        }
                    }            
                }
                else
                {
                    Log.AddInStructMeta(EError.None, "----fmtd.nameList.count > 1 ");
                }
            }
            return null;
        }

        #endregion
    }
}
