using SimpleLanguage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleLanguage.Compile
{
    public class Token
    {
        public string path {get; protected set; }               //文件路径
        public ETokenType type { get; protected set; }         //标记类型
        public object lexeme { get; protected set; }          //标记值
        public object extend { get; protected set; }            //辅助标记，可为空
        public int sourceBeginLine { get; protected set; }         //开始所在行
        public int sourceBeginChar { get; protected set; }         //开始所在列
        public int sourceEndLine { get; protected set; }            //结束所在行
        public int sourceEndChar { get; protected set; }            //结束所在行
        public Token( string _path, ETokenType tokenType, object _lexeme, int sourceLine, int sourceChar, object _extend = null )
        {
            this.path = _path;
            this.type = tokenType;
            this.lexeme = _lexeme;
            this.sourceBeginLine = sourceLine + 1;
            this.sourceBeginChar = sourceChar;
            this.extend = _extend;
        }
        public void SetSrouceEnd( int endSourceLine, int endSourceChar )
        {
            this.sourceEndLine = endSourceLine;
            this.sourceEndChar = endSourceChar;
        }
        public void SetLexeme( object _lexeme )
        {
            this.lexeme = _lexeme;
        }
        public void SetLexeme( object _lexeme, ETokenType tokenType )
        {
            this.lexeme = _lexeme;
            this.type = tokenType;
        }
        public void SetExtend( object _extend )
        {
            this.extend = _extend;
        }
        public void SetBindFilePath( string _path )
        {
            path = _path;
        }
        public Token() { type = ETokenType.None; lexeme = ""; sourceBeginLine = 0; sourceBeginChar = 0; }
        public Token( Token token )
        {
            this.path = token.path;
            this.type = token.type;
            this.lexeme = token.lexeme;
            this.extend = token.extend;
            this.sourceBeginLine = token.sourceBeginChar;
            this.sourceBeginChar = token.sourceBeginLine;
            this.sourceEndChar = token.sourceEndChar;
            this.sourceEndLine = token.sourceEndLine;
        }    
        public override string ToString()
        {
            return lexeme.ToString();
        }
        public string ToAllString()
        {
            return $"Path:{ path } Line: { sourceBeginLine } Pos: { sourceBeginChar }  ";
        }
        public string ToLexemeAllString()
        {
            return $"Lex: { lexeme } Path:{ path } Line: { sourceBeginLine } Pos: { sourceBeginChar }  ";
        }
        public EType GetEType()
        {
            EType etype = EType.None;
            switch( type )
            {
                case ETokenType.Number:
                    {
                        etype = Enum.Parse<EType>( extend.ToString());
                    }
                    break;
                case ETokenType.Type:
                    {
                        etype = Enum.Parse<EType>(extend.ToString()); ;
                    }
                    break;
                case ETokenType.Boolean:
                    {
                        etype = EType.Boolean;
                    }
                    break;
                case ETokenType.String:
                    etype = EType.String;
                    break;
                case ETokenType.Null:
                    etype = EType.Null;
                    break;
                case ETokenType.NumberArrayLink:
                    etype = EType.Array;
                    break;
                default:
                    etype = EType.Class;
                    break;
            }
            return etype;
        }

        public string ToConstString()
        {
            string types = "";
            string val = "";
            if (type == ETokenType.Number)
            {
                EType etype = (EType)Enum.Parse(typeof(EType), extend?.ToString());

                switch (etype)
                {
                    case EType.Byte:
                        {
                            val = lexeme.ToString();
                        }
                        break;
                    case EType.SByte:
                        {
                            val = lexeme.ToString();
                        }
                        break;
                    case EType.Char:
                        {
                            val = '\'' + lexeme.ToString() + '\'';
                        }
                        break;
                    case EType.Int16:
                        {
                            val = lexeme.ToString();
                            types = "s";
                        }
                        break;
                    case EType.UInt16:
                        {
                            val = lexeme.ToString();
                            types = "us";
                        }
                        break;
                    case EType.Int32:
                        {
                            val = lexeme.ToString();
                            types = "i";
                        }
                        break;
                    case EType.UInt32:
                        {
                            val = lexeme.ToString();
                            types = "ui";
                        }
                        break;
                    case EType.Int64:
                        {
                            val = lexeme.ToString();
                            types = "uL";
                        }
                        break;
                    case EType.UInt64:
                        {
                            val = lexeme.ToString();
                            types = "L";
                        }
                        break;
                    case EType.Float:
                        {
                            float f = (float)lexeme;
                            val = f.ToString("0.0");
                            types = "f";
                        }
                        break;
                    case EType.Double:
                        {
                            double d = (double)lexeme;
                            val = d.ToString("0.00");
                            types = "d";
                        }
                        break;
                    default:
                        {
                            val = lexeme.ToString();
                        }
                        break;
                }
                return val + types;
            }
            else if (type == ETokenType.String)
            {
                return '\"' + lexeme.ToString() + '\"';

            }
            else
            {
                return lexeme.ToString() + types;
            }
        }
    }
}
