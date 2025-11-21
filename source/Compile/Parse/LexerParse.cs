//****************************************************************************
//  File:      LexerParse.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/5/12 12:00:00
//  Description:  word lexer parse to token
//****************************************************************************

using SimpleLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleLanguage.Compile
{
    //词法解析
    public class LexerParse
    {
        public Token currentToken => m_CurrentToken;
        public List<Token> listTokens => m_ListTokens;
        public List<Token> GetListTokensWidthEnd()
        {
            List<Token> withEndList = new List<Token>(m_ListTokens);
            withEndList.Add(new Token(m_Path, ETokenType.Finished, END_CHAR, m_SourceLine, m_SourceChar));
            return withEndList;
        }

        const char END_CHAR = char.MaxValue;    //结尾字符

        private char m_CurChar;                              //当前字符
        private char m_TempChar;                             //临时字符
        private StringBuilder m_Builder = new StringBuilder();
        private List<Token> m_ListTokens = new List<Token>();
        private Token m_CurrentToken = null;
        private string m_Buffer;                    
        private int m_Length = 0;                      
        private int m_SourceLine = 0;                  //解析到当前的行数
        private int m_SourceChar = 0;                  //解析到当前行中的位置
        private int m_Index = 0;                       
        private string m_Path;
        public LexerParse( string path, string buffer )
        {
            m_Path = path;
            m_Buffer = buffer;
            m_Length = buffer.Length;
            m_SourceChar = 0;
            m_SourceLine = 0;
        }
        public void SetSourcePosition( int line, int _char )
        {
            m_SourceLine = line;
            m_SourceChar = _char;
        }
        char ReadChar()
        {
            ++m_Index;
            ++m_SourceChar;
            if (m_Index < m_Length)
            {
                return m_Buffer[m_Index];
            }
            else if (m_Index == m_Length)
            {
                return END_CHAR;
            }

            return END_CHAR;
            
        }
        char PeekChar()
        {
            int index = m_Index + 1;
            if (index < m_Length)
            {
                return m_Buffer[index];
            }
            else if (index == m_Length)
            {
                return END_CHAR;
            }
            return char.MinValue;
            //throw new LexerException(this, "End of source reached.");
        }
        void UndoChar()
        {
            if (m_Index == 0)
            {
                CompileManager.instance.AddCompileError("Error Cannot undo char beyond start of source.");
                return;
            }
            --m_Index;
            --m_SourceChar;
        }
        void AddLine()
        {
            m_SourceChar = 0;
            ++m_SourceLine;
        }
        void AddToken( ETokenType type)
        {
            AddToken(type, m_CurChar);
        }
        void AddToken( ETokenType type, object lexeme)
        {
            AddToken(type, lexeme, m_SourceLine, m_SourceChar);
        }
        void AddToken( ETokenType type, object lexeme, object extend )
        {
            AddToken(type, lexeme, extend, m_SourceLine, m_SourceChar );
        }
        void AddToken( ETokenType type, object lexeme, int sourceLine, int sourceChar)
        {
            m_CurrentToken = new Token(m_Path, type, lexeme, sourceLine, sourceChar);
            m_ListTokens.Add(m_CurrentToken);
            m_Builder.Length = 0;
        }
        void AddToken( ETokenType type, object lexeme, object extend, int sourceLine, int sourceChar )
        {
            m_CurrentToken = new Token(m_Path, type, lexeme, sourceLine, sourceChar, extend);
            m_ListTokens.Add(m_CurrentToken);
            m_Builder.Clear();
        }
        void AddChildrenToken(ETokenType type, object lexeme)
        {
            var token = new Token(m_Path, type, lexeme, m_SourceLine, m_SourceChar );
            AddChildrenToken(token);
        }
        public void AddChildrenToken( Token token )
        {
            if( m_CurrentToken != null )
            {
                m_CurrentToken.AddChildrenToken(token);
            }
        }
        bool IsHexDigit(char c)
        {
            if (char.IsDigit(c))
                return true;
            if ('a' <= c && c <= 'f')
                return true;
            if ('A' <= c && c <= 'F')
                return true;
            return false;
        }
        bool IsIdentifier2(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   c == '_';
        }
        private bool IsIdentifier(char ch)
        {
            return (ch == '_' || char.IsLetterOrDigit(ch));
        }       
        /// <summary> + </summary>
        void ReadPlus() 
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=')
            {
                AddToken(ETokenType.PlusAssign, "+=");
            } 
            else if( m_TempChar == '+' )
            {
                AddToken(ETokenType.DoublePlus, "++");
            }
            else 
            {
                AddToken(ETokenType.Plus, "+");
                UndoChar();
            }
        }
        /// <summary> - </summary>
        void ReadMinus()
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=')
            {
                AddToken(ETokenType.MinusAssign, "-=");
            }
            else if (m_TempChar == '-')
            {
                AddToken(ETokenType.DoubleMinus, "--");
            }
            else 
            {
                AddToken(ETokenType.Minus, "-");
                UndoChar();
            }
        }
        /// <summary> * </summary>
        void ReadMultiply() 
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=')
            {
                AddToken(ETokenType.MultiplyAssign, "*=");
            } 
            else
            {
                AddToken(ETokenType.Multiply, "*");
                UndoChar();
            }
        }
        /// <summary> / </summary>
        void ReadDivide()
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=') 
            {
                AddToken(ETokenType.DivideAssign, "/=");
            } 
            else 
            {
                AddToken(ETokenType.Divide, "/");
                UndoChar();
            }
        }
        /// <summary> % </summary>
        void ReadModulo() 
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=')
            {
                AddToken(ETokenType.ModuloAssign, "%=");
            } 
            else
            {
                AddToken(ETokenType.Modulo, "%");
                UndoChar();
            }
        }
        /// <summary> & </summary>
        void ReadAnd()
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '&') 
            {
                AddToken(ETokenType.And, "&&");
            } 
            else if (m_TempChar == '=')
            {
                AddToken(ETokenType.CombineAssign, "&=");
            }
            else 
            {
                AddToken(ETokenType.Combine, "&");
                UndoChar();
            }
        }
        /// <summary> | </summary>
        void ReadOr() 
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '|') 
            {
                AddToken(ETokenType.Or, "||");
            } 
            else if (m_TempChar == '=')
            {
                AddToken(ETokenType.InclusiveOrAssign, "|=");
            } 
            else
            {
                AddToken(ETokenType.InclusiveOr, "|");
                UndoChar();
            }
        }
        /// <summary> ! </summary>
        void ReadNot()
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=') 
            {
                if (ReadChar() == '=')
                {
                    AddToken(ETokenType.ValueNotEqual, "!==");
                } 
                else 
                {
                    AddToken(ETokenType.NotEqual, "!=");
                    UndoChar();
                }
            } 
            else 
            {
                AddToken(ETokenType.Not, "!");
                UndoChar();
            }
        }
        /// <summary> = </summary>
        void ReadAssign()
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=') 
            {
                if (ReadChar() == '=') 
                {
                    AddToken(ETokenType.ValueEqual, "===");
                }
                else
                {
                    AddToken(ETokenType.Equal, "==");
                    UndoChar();
                }
            } 
            else if (m_TempChar == '>') 
            {
                AddToken(ETokenType.Lambda, "=>");
            } 
            else
            {
                AddToken(ETokenType.Assign, "=");
                UndoChar();
            }
        }
        /// <summary> > </summary>
        void ReadGreater()
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=')
            {
                AddToken(ETokenType.GreaterOrEqual, ">=");
            }
            else if (m_TempChar == '>')
            {
                AddToken(ETokenType.Shr, ">>");
            }
            else 
            {
                AddToken(ETokenType.Greater, ">");
                UndoChar();
            }
        }
        /// <summary> < </summary>
        void ReadLess() 
        {
            m_TempChar = ReadChar();
            if (m_TempChar == '=')
            {
                AddToken(ETokenType.LessOrEqual, "<=");
            }
            else if (m_TempChar == '<')
            {
                AddToken(ETokenType.Shi, "<<");
                UndoChar();
            }
            else
            {
                AddToken(ETokenType.Less, "<");
                UndoChar();
            }
        }
        /// <summary> ^ </summary>
        void ReadXor()
        {
            if (ReadChar() == '=')
            {
                AddToken(ETokenType.XORAssign, "^=");
            } 
            else 
            {
                AddToken(ETokenType.XOR, "^");
                UndoChar();
            }
        }
        /// <summary> 读取数字 </summary>
        void ReadNumber()
        {
            m_Builder.Append(m_CurChar);

            int endPoint = 0;
            Char tfrontChar = Char.MinValue;
            do 
            {
                m_TempChar = ReadChar();
                if (char.IsDigit(m_TempChar)) 
                {
                    m_Builder.Append(m_TempChar);
                    tfrontChar = m_TempChar;
                    continue;
                } 
                else if (m_TempChar == '.')
                {
                    endPoint++;
                    m_Builder.Append(m_TempChar);
                }
                else if (m_TempChar == 's')
                {
                    if (endPoint == 0)  //1s
                    {
                        AddToken(ETokenType.Number, Int16.Parse(m_Builder.ToString()), EType.Int16);
                        break;
                    }
                    else if (endPoint == 1)  //1.s
                    {
                        m_Builder.Remove(m_Builder.Length - 1, 1);
                        AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                        UndoChar();
                        UndoChar();
                        break;
                    }
                }
                else if (m_TempChar == 'i')
                {
                    if (endPoint == 0)    //13i
                    {
                        AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                        break;
                    }
                    else if (endPoint == 1)  //  1.i
                    {
                        m_Builder.Remove(m_Builder.Length - 1, 1);
                        AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                        UndoChar();
                        UndoChar();
                        break;
                    }
                }
                else if( m_TempChar == 'f' )
                {
                    if( endPoint == 0 )     // 2f
                    {
                        var ld = Log.AddInHandleToken( m_Path, m_SourceLine, m_SourceChar, EError.None, "读取浮点形必须有小数点!!!" );
                        ld.demo = "2f";
                        ld.advan = "2.0f";
                        AddToken(ETokenType.Number, float.Parse(m_Builder.ToString()), EType.Float32);
                        break;
                    }
                    else if( endPoint == 1  )
                    {
                        if(Char.IsNumber(tfrontChar) )  //2.0f.
                        {
                            AddToken(ETokenType.Number, float.Parse(m_Builder.ToString()), EType.Float32 );
                            break;
                        }
                        else                            //2.f
                        {
                            m_Builder.Remove(m_Builder.Length - 1, 1 );
                            AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                            UndoChar();
                            UndoChar();
                            break;
                        }
                    }
                }
                else if (m_TempChar == 'd')
                {
                    if ( endPoint == 1 )
                    {
                        if(Char.IsNumber(tfrontChar) )
                        {
                            AddToken(ETokenType.Number, double.Parse(m_Builder.ToString()), EType.Float64 );
                            break;
                        }
                        else
                        {
                            m_Builder.Remove(m_Builder.Length - 1, 1);
                            AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                            UndoChar();
                            UndoChar();
                            break;
                        }
                    }
                }
                else if (m_TempChar == 'L')
                {
                    if (endPoint == 0)    //13L
                    {
                        AddToken(ETokenType.Number, long.Parse(m_Builder.ToString()), EType.Int64);
                        break;
                    }
                    else if (endPoint == 1)  //  1.L
                    {
                        m_Builder.Remove(m_Builder.Length - 1, 1);
                        AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                        UndoChar();
                        UndoChar();
                        break;
                    }
                }
                else if (m_TempChar == 'u')
                {
                    var m_TempChar2 = ReadChar();
                    if (m_TempChar2 == 's')  //1us
                    {
                        if (endPoint == 0)      //1us
                        {
                            AddToken(ETokenType.Number, UInt16.Parse(m_Builder.ToString()), EType.UInt16);
                        }
                        else if (endPoint == 1)   //1.us
                        {
                            m_Builder.Remove(m_Builder.Length - 2, 2);
                            AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                            UndoChar();
                            UndoChar();
                            UndoChar();
                            break;
                        }
                    }
                    else if (m_TempChar2 == 'i')
                    {
                        if (endPoint == 0)
                        {
                            AddToken(ETokenType.Number, UInt32.Parse(m_Builder.ToString()), EType.UInt32);
                        }
                        else if (endPoint == 1)
                        {
                            m_Builder.Remove(m_Builder.Length - 2, 2);
                            AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                            UndoChar();
                            UndoChar();
                            UndoChar();
                            break;
                        }
                    }
                    else if (m_TempChar2 == 'L')
                    {
                        if (endPoint == 0)
                        {
                            AddToken(ETokenType.Number, UInt64.Parse(m_Builder.ToString()), EType.UInt64);
                        }
                        else if (endPoint == 1)
                        {
                            m_Builder.Remove(m_Builder.Length - 2, 2);
                            AddToken(ETokenType.Number, Int32.Parse(m_Builder.ToString()), EType.Int32);
                            UndoChar();
                            UndoChar();
                            UndoChar();
                            break;
                        }
                    }
                    else
                    {
                        UndoChar();
                    }
                    break;
                }
                else
                {
                    if( endPoint > 2 )
                    {
                        Debug.Write("Error ReadNumber ... !!!");
                    }
                    //else if( endPoint == 3 )
                    //{
                    //    AddToken(ETokenType.NumberArrayLink, m_Builder.ToString(), EType.Array);
                    //}
                    else if ( endPoint == 2 )
                    {
                        AddToken(ETokenType.NumberArrayLink, m_Builder.ToString(), EType.Array );
                        UndoChar();
                    }
                    else if (endPoint == 1 )
                    {
                        if( char.IsLetter( m_TempChar ) )
                        {
                            var frontChar = m_Builder[m_Builder.Length - 1];
                            if( frontChar == '.' )
                            {
                                //LexelLogData lld = new LexelLogData() { m_}
                                Debug.Write("Error 不允许直接使用  number.function的方式，而是必须使用数据识别符才可以使用，例: 2.0f.ToString()");
                                m_Buffer.Remove(m_Buffer.Length - 1, 1);
                                AddToken(ETokenType.Number, float.Parse(m_Builder.ToString()), EType.Int32);
                                AddToken(ETokenType.Period, frontChar );
                                UndoChar();
                            }
                            else
                            {
                                AddToken(ETokenType.Number, float.Parse(m_Builder.ToString()), EType.Float32);
                                UndoChar();
                            }
                        }
                        else
                        {
                            AddToken(ETokenType.Number, float.Parse(m_Builder.ToString()), EType.Float32 );
                            UndoChar();
                        }
                    }
                    else
                    {
                        AddToken(ETokenType.Number, int.Parse(m_Builder.ToString()), EType.Int32);
                        UndoChar();
                    }
                    break;
                }
                tfrontChar = m_TempChar;
            } while (true);

            m_Index++;
            m_SourceChar++;
        }
        void ReadNumberOrHexOrOctOrBinNumber()
        {
            char t = ReadChar();
            if ( t == 'x' )
            {
                do
                {
                    m_TempChar = ReadChar();
                    if (IsHexDigit(m_TempChar))
                    {
                        m_Builder.Append(m_TempChar);
                    }
                    else if (m_TempChar == '_')
                    {

                    }
                    else
                    {
                        if(m_Builder.Length == 0 )
                        {
                            m_Builder.Append(0);
                        }
                        AddToken(ETokenType.Number, Convert.ToInt32(m_Builder.ToString(), 16), EType.Int32);
                        break;
                    }
                } while (true);
            }
            else if( t == 'o' )
            {
                do
                {
                    m_TempChar = ReadChar();
                    if ( '0' <= m_TempChar && m_TempChar <= '7')
                    {
                        m_Builder.Append(m_TempChar);
                    }
                    else if (m_TempChar == '_')
                    {

                    }
                    else
                    {
                        if (m_Builder.Length == 0)
                        {
                            m_Builder.Append(0);
                        }
                        AddToken(ETokenType.Number, Convert.ToInt32(m_Builder.ToString(), 8), EType.Int32);
                        break;
                    }
                } while (true);
            }
            else if( t == 'b' )
            {
                do
                {
                    m_TempChar = ReadChar();
                    if (49 == m_TempChar
                        || 48 == m_TempChar )
                    {
                        m_Builder.Append(m_TempChar);
                    }
                    else if( m_TempChar == '_' )
                    {

                    }
                    else
                    {
                        if (m_Builder.Length == 0)
                        {
                            m_Builder.Append(0);
                        }
                        AddToken(ETokenType.Number, Convert.ToInt32(m_Builder.ToString(), 2), EType.Int32);
                        break;
                    }
                } while (true);
                
            }
            else
            {
                UndoChar();
                ReadNumber();
            }
        }
        void ReadQuestionMark()
        {
            switch (ReadChar())
            {
                //case '?': AddToken(ETokenType.EmptyRet, "??"); return;
                //case '.': AddToken(ETokenType.QuestionMarkDot, "?."); return;
                default: AddToken(ETokenType.QuestionMark, "?"); UndoChar(); return;
            }
        }
        /// <summary> 读取 @ </summary>
        void ReadAt()
        {
            var ch = ReadChar();
            //if ( ch == '\"' )
            //{
            //    ReadString(true);
            //}
            //else if (ch == '{')
            //{
            //    ReadChar();
            //    AddToken( ETokenType.LeftBrace);
            //}
            //else 
            if( Char.IsNumber( ch ) || Char.IsLetter( ch ) )
            {
                AddToken(ETokenType.At, '@' );
            }
            else
            {
                Debug.Write("Error 不允许@后边加其它符号!!");
            }
        }
        /// <summary> 读取 }  </summary>
        void ReadRightBrace() 
        {
            //if (m_FormatString == EFormatString.None ) 
            //{
                AddToken( ETokenType.RightBrace, '}');
            //} 
            //else
            //{
            //    AddToken(ETokenType.RightPar, ')');
            //    AddToken(ETokenType.Plus, '+');
            //    if (m_FormatString == EFormatString.SingleQuotes || m_FormatString == EFormatString.DoubleQuotes || m_FormatString == EFormatString.Point) 
            //    {
            //        m_CurChar = m_FormatString == EFormatString.SingleQuotes ? '\'' : (m_FormatString == EFormatString.DoubleQuotes ? '\"' : '`');
            //        m_FormatString = EFormatString.None;
            //        ReadString();
            //    } 
            //    else
            //    {
            //        m_CurChar = m_FormatString == EFormatString.SimpleSingleQuotes ? '\'' : (m_FormatString == EFormatString.SimpleDoubleQuotes ? '\"' : '`');
            //        m_FormatString = EFormatString.None;
            //        ReadSimpleString(false);
            //    }
            //}
        }
        void ReadOrigenString()
        {
            m_Builder.Clear();
            do
            {
                this.m_TempChar = ReadChar();
                if (m_TempChar == '\'')
                {
                    AddToken(ETokenType.String, m_Builder.ToString(), EType.String );
                    m_Index++;
                    m_SourceChar++;
                    break;
                }
                else
                {
                    m_Builder.Append(m_TempChar);
                }
            } while (true);
        }
        /// <summary> 读取字符串 </summary>
        void ReadString( bool isWithAtOp ) 
        {
            //if( isWithAtOp )
            //{
            //    do
            //    {
            //        m_TempChar = ReadChar();
            //        if( m_TempChar == '"' )
            //        {
            //            AddToken(ETokenType.String, m_Builder.ToString());
            //            m_Index++;
            //            m_SourceChar++;
            //            break;
            //        }
            //        else
            //        {
            //            m_Builder.Append(m_TempChar);
            //        }
            //    } while (true);
            //}
            //else
            //{

            var stringBuilder = new StringBuilder();
            stringBuilder.Append( m_Builder.ToString() );
            AddToken(ETokenType.String,"" );
            do
            {
                m_TempChar = ReadChar();
                if (m_TempChar == '\\')
                {
                    m_TempChar = ReadChar();
                    switch (m_TempChar)
                    {
                        case '\'': m_Builder.Append('\''); break;
                        case '\"': m_Builder.Append('\"'); break;
                        case '\\': m_Builder.Append('\\'); break;
                        case '$': m_Builder.Append('$'); break;
                        case 'a': m_Builder.Append('\a'); break;
                        case 'b': m_Builder.Append('\b'); break;
                        case 'f': m_Builder.Append('\f'); break;
                        case 'n': m_Builder.Append('\n'); break;
                        case 'r': m_Builder.Append('\r'); break;
                        case 't': m_Builder.Append('\t'); break;
                        case 'v': m_Builder.Append('\v'); break;
                        case '0': m_Builder.Append('\0'); break;
                        case '/': m_Builder.Append("/"); break;
                        case '{': m_Builder.Append("{"); break;
                        case '}': m_Builder.Append("}"); break;
                        case 'u':
                            {
                                var hex = new System.Text.StringBuilder();
                                for (int i = 0; i < 4; i++)
                                {
                                    hex.Append(ReadChar());
                                }
                                m_Builder.Append((char)System.Convert.ToUInt16(hex.ToString(), 16));
                                break;
                            }
                        default:
                            Debug.Write("Error 读字符的时候，不支持当前的符号!! : |" + m_CurChar);
                            break;
                    }
                }
                else if (this.m_TempChar == '\n')
                {
                    Debug.Write("Error NotInterrupt 读字符的时候，不允许换行，请使用/r/t 一类的换行符!!");
                    m_Builder.Append(m_TempChar);
                }
                else if (m_TempChar == '"')
                {
                    stringBuilder.Append(m_Builder);
                    currentToken.SetLexeme(stringBuilder.ToString());
                    m_Index++;
                    m_SourceChar++;
                    break;
                }
                else if (m_TempChar == '{')
                {
                    stringBuilder.Append(m_Builder);
                    AddChildrenToken(ETokenType.String, m_Builder.ToString());
                    m_Builder.Clear();
                    int braceLevel = 0;
                    int startLine = m_SourceLine;
                    int startChar = m_SourceChar;
                    do
                    {
                        var tchar = ReadChar();
                        if (tchar == END_CHAR)
                            break;
                        
                        if( tchar == '}' )
                        {
                            if ( braceLevel == 0 )
                            {
                                break;
                            }
                            else
                            {
                                braceLevel--;
                            }
                        }
                        else if( tchar == '{' )
                        {
                            braceLevel++;
                        }
                        m_Builder.Append(tchar);
                    } while (true);

                    LexerParse lp = new LexerParse(m_Path, m_Builder.ToString());
                    lp.SetSourcePosition(startLine, startChar);
                    lp.ParseToTokenList();
                    var token1 = new Token();
                    token1.SetExtend("param");
                    for ( int i = 0; i < lp.listTokens.Count; i++ )
                    {
                        token1.AddChildrenToken(lp.listTokens[i]);
                    }
                    AddChildrenToken(token1);
                    stringBuilder.Append("{" + m_Builder + "}");
                    m_Builder.Clear();

                    var nextTempChar = ReadChar();
                    if (nextTempChar == '\"')
                    {
                        m_Index++;
                        m_SourceChar++;
                        currentToken.SetLexeme(stringBuilder.ToString());
                        break;
                    }
                    else
                    {
                        UndoChar();
                    }
                }
                else if (m_TempChar == '}')
                {
                    Debug.Write("Error  不允许}独立出现，一般与{配对出现，如果要显示}请使用\\}");
                    break;
                }
                else if (m_TempChar == '$')
                    {
                        var nextChar = ReadChar();
                        if( nextChar == '{' )
                        {
                            AddToken(ETokenType.String, m_Builder.ToString());
                            AddToken(ETokenType.Plus, '+');
                            AddToken(ETokenType.LeftPar, '(');
                            int braceLevel = 0;
                            int startLine = m_SourceLine;
                            int startChar = m_SourceChar;
                            do
                            {
                                var tchar = ReadChar();
                                if (tchar == '}')
                                {
                                    if (braceLevel == 0)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        braceLevel--;
                                    }
                                }
                                else if (tchar == '{')
                                {
                                    braceLevel++;
                                }
                                m_Builder.Append(tchar);
                            } while (true);

                            LexerParse lp = new LexerParse(m_Path, m_Builder.ToString());
                            lp.SetSourcePosition(startLine, startChar);
                            lp.ParseToTokenList();
                            m_ListTokens.AddRange(lp.listTokens);
                            m_Builder.Clear();
                            AddToken(ETokenType.RightPar, ')');

                            var nextTempChar = ReadChar();
                            if (nextTempChar == '\"')
                            {
                                m_Index++;
                                m_SourceChar++;
                                break;
                            }
                            else
                            {
                                AddToken(ETokenType.Plus, '+');
                            }
                        }
                        else
                        {
                            AddToken(ETokenType.String, m_Builder.ToString());
                            AddToken(ETokenType.Plus, '+');
                            //AddToken(ETokenType.LeftPar, '(');
                            int startLine = m_SourceLine;
                            int startChar = m_SourceChar;
                            int level = 0;
                            bool isCheckEndAtString = false;
                            do
                            {
                                var tempChar = ReadChar();
                                if (tempChar == ' ' || tempChar == '\"')
                                {
                                    if (level == 0)
                                    {
                                        isCheckEndAtString = tempChar != '\"';
                                        break;
                                    }
                                    else
                                    {
                                        m_Builder.Append(tempChar);
                                    }
                                }
                                else if (tempChar == '(' || tempChar == '[')
                                {
                                    m_Builder.Append(tempChar);
                                    level++;
                                }
                                else if (tempChar == ')' || tempChar == ']')
                                {
                                    m_Builder.Append(tempChar);
                                    level--;
                                }
                                else if (tempChar == '\'')
                                {
                                    m_Builder.Append('\"');
                                }
                                else
                                {
                                    m_Builder.Append(tempChar);
                                }
                            } while (true);

                            LexerParse lp = new LexerParse(m_Path, m_Builder.ToString());
                            lp.SetSourcePosition(startLine - 1, startChar - 1);
                            lp.ParseToTokenList();
                            m_ListTokens.AddRange(lp.listTokens);
                            m_Builder.Clear();
                            //AddToken(ETokenType.RightPar, ')');

                            if (isCheckEndAtString)//是否需要检查，后边的字符需要+符号处理
                            {
                                var nextTempChar = ReadChar();
                                if (nextTempChar == '\"')
                                {
                                    m_Index++;
                                    m_SourceChar++;
                                    break;
                                }
                                else
                                {
                                    AddToken(ETokenType.Plus, '+');
                                }
                            }
                        }
                    }
                else
                {
                    m_Builder.Append(m_TempChar);
                }
            }
            while (true);
            //}
        }      
        void SharpLevel( int topLevel )
        {
            m_Builder.Clear();
            char schar = char.MinValue;
            int checkBracket = 1;
            bool isBracket = false;
            StringBuilder bracketStringBuild = new StringBuilder();
#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
            int offsetLine = 0;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值
            while( true )
            {
                int index = m_Index + checkBracket;
                if (index < m_Length)
                {
                    schar = m_Buffer[index];
                }
                else
                {
                    Debug.Write("读取Sharp中[]内容出错!!!");
                    break;
                }

                if( checkBracket == 1 )
                {
                    if (schar == '[')
                        isBracket = true;
                    else
                        break;
                }
                else
                {
                    if( isBracket )
                    {
                        if (schar == ']')
                            break;
                        else
                            bracketStringBuild.Append(schar);
                    }
                    else
                    {
                        break;
                    }
                }
                checkBracket++;
            }
            int offset = m_Index++;
            if (isBracket )
                offset = offset + checkBracket + 1;

            int offset2 = 0;
            int curTopLevel = 0;
            bool isEnd = false;
            while( true )
            {
                if( offset >= m_Length)
                {
                    Debug.Write("注释没有结尾!!");
                    break;
                }
                
                m_TempChar = m_Buffer[offset];
                if (m_TempChar == '!')
                {
                    offset2 = 1;
                    curTopLevel = topLevel;
                    while ( true )
                    {
                        schar = m_Buffer[offset + offset2++];
                        if (schar == '#')
                        {
                            if( offset+offset2 >= m_Length )
                            {
                                isEnd = true;
                                break;
                            }
                            curTopLevel--;
                            if (curTopLevel <= 0)
                            {
                                if( m_Buffer[offset+offset2] == '#' )
                                {
                                    break;
                                }
                                isEnd = true;
                                break;
                            }
                        }
                        else
                            break;
                    }
                    if( !isEnd )
                        m_Builder.Append(m_TempChar);
                }
                else
                {
                    if( m_TempChar == '\n' )
                    {
                        m_SourceLine++;
                    }
                    m_Builder.Append(m_TempChar);
                }
                if(isEnd )
                {
                    m_Index = offset + 1;
                    break;
                }
                offset++;
            }

            AddToken(ETokenType.Sharp, m_Builder.ToString(), bracketStringBuild.ToString());
        }
        void ReadDollar()
        {
            var ch = ReadChar();
            if (ch == '\"')
            {
                ReadString(true);
            }
            //else if (ch == '{')
            //{
            //    ReadChar();
            //    AddToken( ETokenType.LeftBrace);
            //}
            else if (Char.IsNumber(ch) || Char.IsLetter(ch))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(ch);
                while (true)
                {
                    m_TempChar = ReadChar();
                    if(IsIdentifier2(m_TempChar) )
                    {
                        sb.Append(m_TempChar);
                    }
                    else
                    {
                        break;
                    }
                }
                AddToken(ETokenType.Dollar, '$', sb.ToString() );
            }
            else
            {
                Debug.Write("Error 不允许$后边加其它符号!!");
            }
        }
        void ReadSharp()
        {
            int topLevel = 1;
            while( true )
            {
                m_TempChar = ReadChar();
                if (m_TempChar == '!')
                {
                    SharpLevel(topLevel);
                    break;
                }
                else if (m_TempChar == '#')
                {
                    topLevel++;
                    m_Builder.Append(m_TempChar);
                }
                else
                {
                    do
                    {
                        if (!(m_TempChar == '\n'))
                        {
                            m_Builder.Append(m_TempChar);
                        }
                        else
                        {
                            break;
                        }
                        m_TempChar = ReadChar();
                        if (m_TempChar == END_CHAR)
                            break;
                    } while (true);
                    AddToken(ETokenType.Sharp, m_Builder.ToString(), "#" );
                    m_Index++;
                    m_SourceChar++;
                    m_SourceLine++;
                    m_Builder.Clear();
                    break;
                }
            }          
            
        }
        /// <summary> 读取关键字 </summary>
        void ReadIdentifier()
        {
            m_Builder.Append(m_CurChar);
            do 
            {
                m_TempChar = ReadChar();
                if (IsIdentifier(m_TempChar)) 
                {
                    m_Builder.Append(m_TempChar);
                }
                else
                {
                    UndoChar();
                    break;
                }
            } while (true);
            ETokenType tokenType;
            object extend = null;
            switch (m_Builder.ToString())
            {
                case "import":
                    tokenType = ETokenType.Import;
                    break;
                case "as":
                    tokenType = ETokenType.As;
                    break;
                case "is":
                    tokenType = ETokenType.Is;
                    break;
                case "namespace":
                    tokenType = ETokenType.Namespace;
                    break;
                case "class":
                    tokenType = ETokenType.Class;
                    extend = EType.Class;
                    break;
                case "extends":
                    tokenType = ETokenType.Extends;
                    break;
                case "enum":
                    tokenType = ETokenType.Enum;
                    extend = EType.Enum;
                    break;
                case "data":
                    tokenType = ETokenType.Data;
                    extend = EType.Data;
                    break;
                case "dynamic":
                    tokenType = ETokenType.Dynamic;
                    break;
                case "void":
                    tokenType = ETokenType.Void;
                    break;
                case "object":
                case "Object":
                    tokenType = ETokenType.Type;
                    extend = EType.Object;
                    break;
                case "byte":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Byte;
                    }
                    break;
                case "sbyte":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.SByte;
                    }
                    break;
                //case "char":
                //    {
                //        tokenType = ETokenType.Type;
                //        extend = EType.Char;
                //    }
                //    break;
                case "short":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Int16;
                    }
                    break;
                case "ushort":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.UInt16;
                    }
                    break;
                case "int":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Int32;
                    }
                    break;
                case "uint":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.UInt32;
                    }
                    break;
                case "bool":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Boolean;
                    }
                    break;
                case "long":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Int64;
                    }
                    break;
                case "ulong":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.UInt64;
                    }
                    break;
                case "half":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Float16;
                    }
                    break;
                case "float":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Float32;
                    }
                    break;
                case "double":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.Float64;
                    }
                    break;
                case "string":
                    {
                        tokenType = ETokenType.Type;
                        extend = EType.String;
                    }
                    break;
                case "get":
                    {
                        tokenType = ETokenType.Get;
                    }
                    break;
                case "set":
                    {
                        tokenType = ETokenType.Set;
                    }
                    break;
                case "if":
                    tokenType = ETokenType.If;
                    break;
                case "elif":
                    tokenType = ETokenType.ElseIf;
                    break;
                case "else":
                    tokenType = ETokenType.Else;
                    break;
                case "while":
                    tokenType = ETokenType.While;
                    break;
                case "dowhile":
                    tokenType = ETokenType.DoWhile;
                    break;
                case "const":
                    tokenType = ETokenType.Const;
                    break;
                case "mut":
                    tokenType = ETokenType.Mut;
                    break;
                case "final":
                    tokenType = ETokenType.Final;
                    break;
                case "static":
                    tokenType = ETokenType.Static;
                    break;
                case "partial":
                    tokenType = ETokenType.Partial;
                    break;
                case "for":
                    tokenType = ETokenType.For;
                    break;               
                case "in":
                    tokenType = ETokenType.In;
                    break;
                case "switch":
                    tokenType = ETokenType.Switch;
                    break;
                case "case":
                    tokenType = ETokenType.Case;
                    break;
                case "default":
                    tokenType = ETokenType.Default;
                    break;
                case "next":
                    tokenType = ETokenType.Next;
                    break;
                case "continue":
                    tokenType = ETokenType.Continue;
                    break;
                case "break":
                    tokenType = ETokenType.Break;                
                    break;
                case "goto":
                    tokenType = ETokenType.Goto;
                    break;
                case "extern":
                    tokenType = ETokenType.Extern;
                    break;
                case "public":
                    tokenType = ETokenType.Public;
                    break;
                case "protected":
                    tokenType = ETokenType.Projected;
                    break;
                case "private":
                    tokenType = ETokenType.Private;
                    break;
                case "operator":
                    tokenType = ETokenType.Operator;
                    break;
                case "interface":
                    tokenType = ETokenType.Interface;
                    break;
                //case "virtual":
                //    Debug.Write("Error virtual 但不能在代码中使用!!");
                //    tokenType = ETokenType.Virtual;
                //    return;
                case "override":
                    tokenType = ETokenType.Override;
                    break;
                case "params":
                    tokenType = ETokenType.Params;
                    break;
                case "tr":
                    tokenType = ETokenType.Transience;
                    break;
                case "ret":
                    tokenType = ETokenType.Return;
                    break;
                case "label":
                    tokenType = ETokenType.Label;
                    break;
                //case "let":
                //    tokenType = ETokenType.Let;
                //    break;
                case "global":
                    tokenType = ETokenType.Global;
                    break;
                case "try":
                    tokenType = ETokenType.Try;
                    break;
                case "catch":
                    tokenType = ETokenType.Catch;
                    break;
                case "throw":
                    tokenType = ETokenType.Throw;
                    break;
                case "null":
                    tokenType = ETokenType.Null;
                    break;
                case "var":
                    tokenType = ETokenType.Var;
                    break;
                case "this":
                    tokenType = ETokenType.This;
                    break;
                case "base":
                    tokenType = ETokenType.Base;
                    break;
                case "true":
                case "false":
                    tokenType = ETokenType.BoolValue;
                    extend = EType.Boolean;
                    break;
                case "new":
                    tokenType = ETokenType.New;
                    break;
                case "range":
                    tokenType = ETokenType.Identifier;
                    extend = EType.Range;
                    break;
                case "array":
                    tokenType = ETokenType.Identifier;
                    extend = EType.Array;
                    break;
                //case "async":
                //    tokenType = TokenType.Async;
                //    break;
                //case "await":
                //    tokenType = TokenType.Await;
                //    break;    
                default:
                    tokenType = ETokenType.Identifier;
                    break;
            }

            if (tokenType == ETokenType.Null) 
            {
                AddToken(tokenType, "null", m_SourceLine, m_SourceChar);
            } 
            else
            {
                AddToken(tokenType, m_Builder.ToString(), extend, m_SourceLine, m_SourceChar );
            }
        }
        /// <summary> 解析字符串 </summary>
        public void ParseToTokenList() 
        {    
            m_CurChar = END_CHAR;
            m_Builder.Clear();
            m_ListTokens.Clear();
            m_Index = 0;
            while ( m_Index < m_Length )
            {
                m_CurChar = m_Buffer[m_Index];
                if (m_CurChar == '\n')
                {
                    AddToken(ETokenType.LineEnd);
                    m_Index++;
                    AddLine();
                    continue;
                }
                else if(m_CurChar == ' ' )
                {
                    int num = 1;
                    int bline = m_SourceLine;
                    int bchar = m_SourceChar++;
                    m_Index++;
                    while ( m_Index < m_Length )
                    {
                        if(m_Buffer[m_Index] != ' ')
                        {
                            break;
                        }
                        m_SourceChar++;
                        m_Index++;
                        num++;
                    }

                    var spacetoken = new Token(m_Path, ETokenType.Space, "", bline, bchar);
                    spacetoken.SetSrouceEnd(m_SourceLine, m_SourceChar);
                    spacetoken.SetExtend(num);
                    //m_ListTokens.Add(spacetoken);
                }
                else if( m_CurChar == '\t' || m_CurChar == '\r' )
                {
                    m_Index++;
                    m_SourceChar++;
                    continue;
                }
                else if( m_CurChar == '.' )
                {
                    AddToken(ETokenType.Period, ".");
                    m_Index++;
                    m_SourceChar++;
                }
                else if(m_CurChar == ';')
                {
                    AddToken(ETokenType.SemiColon);
                    m_Index++;
                    m_SourceChar++;
                }
                else
                {
                    switch (m_CurChar)
                    {
                        case '(':
                            AddToken(ETokenType.LeftPar);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case ')':
                            AddToken(ETokenType.RightPar);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '[':
                            AddToken(ETokenType.LeftBracket);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case ']':
                            AddToken(ETokenType.RightBracket);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '{':
                            AddToken(ETokenType.LeftBrace);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '}':
                            ReadRightBrace();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case ',':
                            AddToken(ETokenType.Comma);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '~':
                            AddToken(ETokenType.Negative);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case ':':
                            AddToken(ETokenType.Colon);
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '?':
                            ReadQuestionMark();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '+':
                            ReadPlus();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '-':
                            ReadMinus();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '*':
                            ReadMultiply();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '/':
                            ReadDivide();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '%':
                            ReadModulo();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '=':
                            ReadAssign();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '&':
                            ReadAnd();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '|':
                            ReadOr();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '!':
                            ReadNot();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '>':
                            ReadGreater();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '<':
                            ReadLess();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '^':
                            ReadXor();
                            m_Index++;
                            m_SourceChar++;
                            break;
                        case '#':
                            ReadSharp();
                            break;
                        case '$':
                            ReadDollar();
                            break;
                        case '@':
                            ReadAt();
                            break;
                        case '\"':
                            ReadString(false);
                            break;
                        case '\'':
                            {
                                ReadOrigenString();
                            }
                            break;
                        case '0':
                            ReadNumberOrHexOrOctOrBinNumber();
                            break;
                        default:
                            if (char.IsDigit(m_CurChar))
                            {
                                ReadNumber();
                            }
                            else if (IsIdentifier(m_CurChar))
                            {
                                ReadIdentifier();
                                m_Index++;
                                m_SourceChar++;
                            }
                            else
                            {
                                var ld = Log.AddInHandleToken(m_Path, m_SourceLine, m_SourceChar, EError.UnMatchChar, $"解析错误，无法解析这种类型的字符[ {this.m_CurChar} ]");
                                
                            }
                            break;
                    }
                }
            }
        }
    }
}
