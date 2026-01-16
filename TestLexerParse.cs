using System;
using System.Collections.Generic;
using SimpleLanguage.Compile;

namespace SimpleLanguage.Test
{
    class TestLexerParse
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing LexerParse.ReadString method...");
            
            string testString = "Name:$name Score:$score a+b=${(a+b).toString()}";
            string testCode = "var str = \"" + testString + "\";";
            
            Console.WriteLine("Test code: " + testCode);
            Console.WriteLine();
            
            // Create lexer parse instance
            LexerParse lexer = new LexerParse("test.sl", testCode);
            
            // Parse to token list
            lexer.ParseToTokenList();
            
            // Print tokens
            Console.WriteLine("Generated tokens:");
            List<Token> tokens = lexer.listTokens;
            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                Console.WriteLine($"Token {i}: Type={token.type}, Lexeme={token.lexeme}, Line={token.sourceBeginLine}, Char={token.sourceBeginChar}");
                
                // Print children tokens if any
                if (token.childrenTokensList != null && token.childrenTokensList.Count > 0)
                {
                    Console.WriteLine("  Children tokens (each is a token list for one interpolation parameter):");
                    foreach (var childList in token.childrenTokensList)
                    {
                        Console.WriteLine("    Child list:");
                        foreach (var childToken in childList)
                        {
                            Console.WriteLine($"      Token: Type={childToken.type}, Lexeme={childToken.lexeme}");
                        }
                    }
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("Test completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}