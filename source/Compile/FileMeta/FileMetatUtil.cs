//****************************************************************************
//  File:      FileMetaUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Compile.CoreFileMeta;
using SimpleLanguage.Compile.Grammer;
using SimpleLanguage.Compile.Parse;
using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class FileMetatUtil
    {
        public static List<string> GetLinkStringMidPeriodList(List<Token> tokenList)
        {
            List<string> stringList = new List<string>();
            for (int i = 0; i < tokenList.Count; i++)
            {
                var token = tokenList[i];
                if (token.lexeme == null)
                {
                    Console.WriteLine("检查到Import语句中，token内容lexeme为空!!");
                    return null;
                }
                if (token.type != ETokenType.Period)
                {
                    if (!GrammerUtil.IdentifierCheck(token.lexeme.ToString()))
                    {
                        Console.WriteLine("检查到Import语句中，导入名称不合规!!");
                        return null;
                    }
                    stringList.Add(token.lexeme.ToString());
                }
            }
            return stringList;
        }
        public static bool IsSymbol( Token token )
        {
            switch( token.type )
            {
                case ETokenType.Plus:
                case ETokenType.Minus:
                case ETokenType.Multiply:
                case ETokenType.Divide:
                case ETokenType.DoublePlus:     //++
                case ETokenType.DoubleMinus:    //--
                case ETokenType.Modulo:          // %
                case ETokenType.Not:             // !
                case ETokenType.Negative:        // ~
                case ETokenType.Shi:               //  <<
                case ETokenType.Shr:               //  >>
                case ETokenType.Less:            // >
                case ETokenType.GreaterOrEqual:  // >=
                case ETokenType.Greater:         // <
                case ETokenType.LessOrEqual:     // <=
                case ETokenType.Equal:           // ==
                case ETokenType.NotEqual:        // !=
                case ETokenType.Combine:         // &
                case ETokenType.InclusiveOr:     // |
                case ETokenType.XOR:             //  ^
                case ETokenType.Or:              // ||
                case ETokenType.And:             // &&  
                case ETokenType.PlusAssign:             // +=
                case ETokenType.MinusAssign:            // -=
                case ETokenType.MultiplyAssign:         // *=
                case ETokenType.DivideAssign:           // /=
                case ETokenType.ModuloAssign:           // %=
                case ETokenType.InclusiveOrAssign:      // |=
                case ETokenType.XORAssign:              // ^=
                    {
                        return true;
                    }
            }
            return false;
        }
        public static bool SplitNodeList(List<Node> nodeList, List<Node> preNodeList, List<Node> afterNodeList, ref Token assignToken)
        {
            bool isEqual = false;
            for (int i = 0; i < nodeList.Count; i++)
            {
                var n = nodeList[i];
                if (n.token.type == ETokenType.Assign)
                {
                    isEqual = true;
                    assignToken = n.token;
                    continue;
                }
                if (isEqual)
                    afterNodeList.Add(n);
                else
                    preNodeList.Add(n);
            }
            if (isEqual)
            {
                if (afterNodeList.Count == 0)
                {
                    Console.WriteLine("解析NodeStructVariable时有=号，但没有值内容 " + assignToken?.ToLexemeAllString() );
                    return false;
                }
            }
            if (preNodeList.Count == 0)
            {
                return false;
            }
            return true;
        }
        public static FileMetaBaseTerm CreateFileOneTerm( FileMeta fm, Node node, FileMetaTermExpress.EExpressType expressType)
        {
            FileMetaBaseTerm fmbt = null;
            if (node.nodeType == ENodeType.IdentifierLink
                || (node.nodeType == ENodeType.Key && 
                        (node.token?.type == ETokenType.This|| node.token?.type == ETokenType.Base ) ) 
                )
            {
                fmbt = new FileMetaCallTerm(fm, node);
                fmbt.priority = SignComputePriority.Level1;
            }
            else if (node.nodeType == ENodeType.ConstValue)
            {
                if( node.extendLinkNodeList.Count > 1 )
                {
                    fmbt = new FileMetaCallTerm(fm, node);
                    fmbt.priority = SignComputePriority.Level1;
                }
                else
                {
                    fmbt = new FileMetaConstValueTerm(fm, node.token);
                    fmbt.priority = SignComputePriority.Level1;
                }
            }
            else if( node.nodeType == ENodeType.Key )
            {

            }
            else if (node.nodeType == ENodeType.Par)
            {
                fmbt = new FileMetaParTerm(fm, node, expressType);
                fmbt.priority = SignComputePriority.Level1;
            }
            else if (node.nodeType == ENodeType.Brace)
            {
                fmbt = new FileMetaBraceTerm(fm, node);
                fmbt.priority = SignComputePriority.Level1;
            }
            else if (node.nodeType == ENodeType.Bracket)
            {
                fmbt = new FileMetaBracketTerm(fm, node);
                fmbt.priority = SignComputePriority.Level1;
            }
            else
            {
                Console.WriteLine("Error CreateFileOneTerm 单1表达式，没有找到该类型: " + node.token.type.ToString() + " 位置: " + node.token.ToLexemeAllString());
            }
            return fmbt;
        }
        public static FileMetaBaseTerm CreateFileMetaExpress(FileMeta fm, List<Node> nodeList, FileMetaTermExpress.EExpressType expressType)
        {
            if (nodeList.Count == 0)
                return null;

            FileMetaBaseTerm fmbt = null;
            if ( nodeList.Count == 1 )
            {
                fmbt = CreateFileOneTerm(fm, nodeList[0], expressType );
            }
            else
            {
                fmbt = new FileMetaTermExpress(fm, nodeList, expressType );
            }
            if( fmbt == null )
            {
                Console.WriteLine("Error 生成表达式错误!!");
                return null;
            }
            fmbt.BuildAST();
            return fmbt;
        }
    }
}
