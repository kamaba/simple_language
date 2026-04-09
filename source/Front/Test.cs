
using SimpleLanguage;
using SimpleLanguage.Compile;
using System;
using System.Collections.Generic;

public class Test
{
    static void TestReadString()
    {
        Console.WriteLine("Testing LexerParse.ReadString method...");

        string testString = "Name:$name Score:$score a+b=${(a+b).toString()}";
        string testCode = "var str = \"" + testString + "\";";

        Console.WriteLine("Test code: " + testCode);
        Console.WriteLine();

        // Create lexer parse instance
        LexerParse lexer = new LexerParse("test.sl", testCode.ToCharArray());

        // Parse to token list
        lexer.ParseToTokenList();

        // Print tokens
        Console.WriteLine("Generated tokens:");
        List<Token> tokens = lexer.listTokens;
        for (int i = 0; i < tokens.Count; i++)
        {
            Token token = tokens[i];
            Console.WriteLine($"Token {i}: Type={token.type}, Lexeme={token.lexeme}, Line={token.sourceBeginLine}, Char={token.sourceBeginChar}");

            /*
            // Print children tokens if any
            if (token.childrenTokensList != null && token.childrenTokensList.Count > 0)
            {
                Console.WriteLine("  Children tokens:");
                foreach (Token childToken in token.childrenTokensList)
                {
                    Console.WriteLine($"    Child: Type={childToken.type}, Lexeme={childToken.lexeme}");

                    // Print grandchildren if any (for expressions)
                    if (childToken.childrenTokensList != null && childToken.childrenTokensList.Count > 0)
                    {
                        Console.WriteLine("      Grandchildren:");
                        foreach (Token grandChildToken in childToken.childrenTokensList)
                        {
                            Console.WriteLine($"        Grandchild: Type={grandChildToken.type}, Lexeme={grandChildToken.lexeme}");
                        }
                    }
                }
            }
            */
        }

        Console.WriteLine();
        Console.WriteLine("Test ReadString completed.");
        Console.WriteLine();
    }
}