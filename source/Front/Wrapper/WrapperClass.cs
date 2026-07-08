//****************************************************************************
//  File:      WrapperClass.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/21 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using System.Collections.Generic;

namespace SimpleLanguage.Wrapper
{
    public class WrapperClass
    {
        public int id { get; set; }
        public byte type { get; set; } = 0; // 0: class, 1: data 2:enum 3:interface
        public string name { get; set; }
        public int byteCount { get; set; }
        public int templateCount { get; set; }
        public bool needInitMemberVariable { get; set; }
        public List<WrapperVariable> localVariables { get; set; } = new List<WrapperVariable>();
        public List<WrapperVariable> staticVariables { get; set; } = new List<WrapperVariable>();
    }
    public class WrapperClassManager
    {
        public WrapperClass GeneralPEClass( MetaClass mc )
        {
            WrapperClass pec = new WrapperClass();
            /*
            pec.id = mc.Id;
            pec.type = (int)mc.Type;
            pec.name = mc.Name;
            pec.byteCount = mc.ByteCount;
            pec.templateCount = mc.TemplateCount;
            pec.needInitMemberVariable = mc.NeedInitMemberVariable;
            pec.localVariables = new List<PEVariable>();
            foreach ( var v in mc.LocalVariables )
            {
                PEVariable pev = new PEVariable();
                pev.id = v.Id;
                pev.type = (int)v.Type;
                pev.name = v.Name;
                pev.byteOffset = v.ByteOffset;
                pev.isInitialized = v.IsInitialized;
                pec.localVariables.Add( pev );
            }
            pec.staticVariables = new List<PEVariable>();
            foreach ( var v in mc.StaticVariables )
            {
                PEVariable pev = new PEVariable();
                pev.id = v.Id;
                pev.type = (int)v.Type;
                pev.name = v.Name;
                pev.byteOffset = v.ByteOffset;
                pev.isInitialized = v.IsInitialized;
                pec.staticVariables.Add( pev );
            }
            */
            return pec;
        }
    }
}
