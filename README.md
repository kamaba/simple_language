# Simple Language [中文](https://github.com/kamaba/simple_language/blob/main/README_CN.md)

------------------------------------------------------------------------

### Brief introduction: Simple language is a static language, its first edition is written on the basis of csharp, the syntax is roughly similar to C#, but it has the characteristics of other languages, its project configuration and language is assembly, so when using the language, must be on the basis of project. 
Language functions in three stages
1. The first stage: front-end analysis of the language. The language has its own complete language system through the integration library of c# platform, but does not support template operation for the time being.
2. Second stage: The language can export the IR layer of c#, using Mono or yes. The NetCore virtual machine runs the exported code and internally can call c# libraries or c/ C ++ libraries directly and quickly modularize. Can export javascript and other languages, compatible with the execution of some javascript libraries.
3. The third stage: use the llvm middle layer and the future.netcore's native functions to localize the language and run it away from the virtual machine, and then use llvm to transform it, which can be packaged, linked and run directly in some normal languages.


### Features of language
1. The writing method is relatively simple, no mandatory formatting behavior, and more curly brackets are used to represent code segments.
2. Annotations support multiple layers of nesting and markdown annotations



### The purpose of language
1. Strong readability
2. Strong writability
3. Mild grammatical sugars must be based on 1,2.
4. Pure object-oriented language.
5. Slight use of inheritance, interface, not allowed to have the same name variable.

### Support platform

### Install and use


### Contact Author
mail: kamaba233@gmail.com