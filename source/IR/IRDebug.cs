//****************************************************************************
//  File:      IRDebug.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: IR Debug data or information and logic
//****************************************************************************

using SimpleLanguage.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace SimpleLanguage.IR
{
    public struct DebugInfo
    {
        public string path;
        public string name;
        public int beginLine;
        public int beginChar;
        public int endLine;
        public int endChar;
        public string info;

        public override string ToString()
        {
            return path + " -> " + name;
        }
    }
}
