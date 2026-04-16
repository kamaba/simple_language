//****************************************************************************
//  File:      FileMetaUtil.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.Logging;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleLanguage.Compile
{
    public class FileMetatUtil
    {
        public static bool IdentifierCheck(string name)
        {
            if (name == null)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "自定义字符错误, 参数不可以为空!!");
                return false;
            }

            if (name.Equals(string.Empty) || name.Length == 0)
            {
                Log.AddFileMetaLog(LID.ShowExtendMessage, "自定义字符错误, 不可以为空!!");
                return false;
            }
            string pattern = @"^[A-Za-z0-9_]+$";

            Match match = Regex.Match(name, pattern);

            if (!match.Success)
            {
                return false;
            }
            return true;
        }
        public static List<string> GetLinkStringMidPeriodList(List<Token> tokenList)
        {
            List<string> stringList = new List<string>();
            for (int i = 0; i < tokenList.Count; i++)
            {
                var token = tokenList[i];
                if (token.lexeme == null)
                {
                    Log.AddFileMetaLog(LID.AutoFileMetatUtilL47, "检查到Import语句中，token内容lexeme为空!!");
                    return null;
                }
                if (token.type != ETokenType.Period)
                {
                    if (!IdentifierCheck(token.lexeme.ToString()))
                    {
                        Log.AddFileMetaLog(LID.AutoFileMetatUtilL54, "检查到Import语句中，导入名称不合规!!");
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
                case ETokenType.CombineAssign:          // &=
                case ETokenType.InclusiveOrAssign:      // |=
                case ETokenType.XORAssign:              // ^=
                case ETokenType.ShiAssign:              // <<=
                case ETokenType.ShrAssign:              // >>=
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
                    Log.AddFileMetaLog(LID.AutoFileMetatUtilL125, "解析NodeStructVariable时有=号，但没有值内容 " + assignToken?.ToLexemeAllString() );
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
                        (node.token?.type == ETokenType.This
                        || node.token?.type == ETokenType.Base
                        || node.token?.type == ETokenType.Global ) ) 
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
            else if (node.nodeType == ENodeType.Par)
            {
                if (node.extendLinkNodeList.Count > 1)
                {
                    fmbt = new FileMetaCallTerm(fm, node);
                    fmbt.priority = SignComputePriority.Level1;
                }
                else
                {
                    fmbt = new FileMetaParTerm(fm, node, expressType);
                    fmbt.priority = SignComputePriority.Level1;
                }
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
                Log.AddFileMetaLog(LID.AutoFileMetatUtilL186, "Error CreateFileOneTerm 单1表达式，没有找到该类型: " + node.token.type.ToString() + " 位置: " + node.token.ToLexemeAllString());
            }
            return fmbt;
        }
        public static FileMetaBaseTerm CreateFileMetaExpress(FileMeta fm, List<Node> nodeList, FileMetaTermExpress.EExpressType expressType)
        {
            if (nodeList == null || nodeList.Count == 0)
                return null;

            // 单节点直接走基础创建
            if (nodeList.Count == 1)
            {
                var fot =  CreateFileOneTerm(fm, nodeList[0], expressType);
                fot.BuildAST();
                return fot;
            }

            FileMetaBaseTerm fmbt = null;

            // 1) 优先在当前这一层判断三元 ?:，因为它的优先级最低，需要最先把整体拆成三个子表达式，
            //    然后对子表达式再递归调用 CreateFileMetaExpress，继续做 as/is、三元等识别。
            int questionIndex = -1;
            int colonIndex = -1;
            int depth = 0; // 简单跳过括号/大括号/中括号里的 ? :
            for (int i = 0; i < nodeList.Count; i++)
            {
                var n = nodeList[i];
                if (n.nodeType == ENodeType.Par || n.nodeType == ENodeType.Brace || n.nodeType == ENodeType.Bracket)
                {
                    depth++;
                }
                else if (n.nodeType == ENodeType.SemiColon || n.nodeType == ENodeType.LineEnd)
                {
                    // 语句分隔，重置
                    if (depth == 0 && questionIndex >= 0 && colonIndex > questionIndex)
                        break;
                }

                if (depth > 0)
                {
                    // 括号内部的 ?: 不在这一层处理
                    if (n.nodeType == ENodeType.Par || n.nodeType == ENodeType.Brace || n.nodeType == ENodeType.Bracket)
                    {
                        depth--;
                    }
                    continue;
                }

                if (n.nodeType == ENodeType.QuestionMark )
                {
                    if (questionIndex < 0)
                        questionIndex = i;
                }
                else if (n.nodeType == ENodeType.Colon && questionIndex >= 0)
                {
                    colonIndex = i;
                    break;
                }
            }

            if (questionIndex > 0 && colonIndex > questionIndex + 1 && colonIndex < nodeList.Count - 1)
            {
                // 拆成 condition ? trueExpr : falseExpr
                var condList = nodeList.GetRange(0, questionIndex);
                var trueList = nodeList.GetRange(questionIndex + 1, colonIndex - questionIndex - 1);
                var falseList = nodeList.GetRange(colonIndex + 1, nodeList.Count - colonIndex - 1);

                // 三个部分继续递归走同样逻辑（内部还可以再包含 as/is、三元等）
                fmbt = new FileMetaThreeItemSyntaxTerm(fm,
                    condList,
                    trueList,
                    falseList);
            }
            else
            {
                // 2) 当前层不存在顶层 ?:，再判断 as/is
                int asIsIndex = -1;
                for (int i = 0; i < nodeList.Count; i++)
                {
                    var t = nodeList[i].token?.type;
                    if (t == ETokenType.As || t == ETokenType.Is || t == ETokenType.IsNot)
                    {
                        asIsIndex = i;
                        break;
                    }
                }

                if (asIsIndex > 0 && asIsIndex < nodeList.Count - 1)
                {
                    var asIsNode = nodeList[asIsIndex];
                    var leftNodes = nodeList.GetRange(0, asIsIndex);
                    var rightCount = nodeList.Count - asIsIndex - 1;
                    List<Node> typeNodes = null;
                    Node optionalVarNode = null;

                    if (asIsNode.token?.type == ETokenType.As)
                    {
                        // as 支持泛型类型写法（例如: arr as Array<Object>），
                        // 右侧类型可能被拆成多个节点，需整体作为 typeNodes 处理。
                        typeNodes = nodeList.GetRange(asIsIndex + 1, rightCount);
                    }
                    else if (rightCount == 1)
                    {
                        // var1 as Class1  或  var1 is Class1
                        typeNodes = new List<Node> { nodeList[asIsIndex + 1] };
                    }
                    else if (rightCount >= 2 && (asIsNode.token?.type == ETokenType.Is || asIsNode.token?.type == ETokenType.IsNot))
                    {
                        // var1 is Class1 var2
                        // 兼容泛型类型：若末尾不是可作为变量名的节点，则整段都作为类型。
                        var tailNode = nodeList[nodeList.Count - 1];
                        bool tailCanBeVarName =
                            tailNode?.nodeType == ENodeType.IdentifierLink
                            || tailNode?.token?.type == ETokenType.Identifier;
                        if (tailCanBeVarName)
                        {
                            typeNodes = nodeList.GetRange(asIsIndex + 1, rightCount - 1);
                            optionalVarNode = tailNode;
                        }
                        else
                        {
                            typeNodes = nodeList.GetRange(asIsIndex + 1, rightCount);
                        }
                    }

                    if (typeNodes != null)
                    {
                        // 右侧类型可能是拆开的泛型节点（例如 Array < Object >），
                        // 先做单行节点归并，确保 as/is 能拿到完整类型定义。
                        typeNodes = StructParse.HandleNodeSingleLine(typeNodes);
                        fmbt = new FileMetaAsOrIsTerm(fm, leftNodes, asIsNode.token, typeNodes, optionalVarNode);
                    }
                }

                // 3) 既不是 ?: 也不是 as/is，则作为普通表达式交给 FileMetaTermExpress
                if (fmbt == null)
                {
                    fmbt = new FileMetaTermExpress(fm, nodeList, expressType);
                }
            }

            if (fmbt == null)
            {
                Log.AddFileMetaLog(LID.AutoFileMetatUtilL329, "Error 生成表达式错误!!");
                return null;
            }
            fmbt.BuildAST();
            return fmbt;
        }
    }
}
