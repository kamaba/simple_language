# Useful AI Prompts for the SimpleLanguage Repo

This file contains curated prompts to use when asking an AI assistant about the codebase.

General exploration
- "Summarize the responsibilities of `source/Core/MetaClass.cs` and list its public APIs."
- "List all files that reference `MetaMemberFunction.isAbstract` and summarize where abstract handling occurs."

Refactor or implement features
- "Implement `Num.abs()` for Int32 and Float32: show code changes and tests."
- "Optimize `Map.sl` to use hash buckets: propose a design and a patch to implement it." 

Documentation
- "Generate API docs for `ExpressManager` describing entry points and options."
- "Create examples showcasing `abstract` and `override` semantics in the language, using files under `samples/`.")

Testing
- "Create a suite of sample scripts for collection APIs: Array.fill, List.add/remove, Map.add/getValue." 
- "Generate unit tests that assert abstract class enforcement (class with abstract method; subclass without override should error)."

Automation
- "Create a GitHub Action that runs `dotnet build`, `dotnet test` and re-generates `md/ai` docs using AI on push."

Tips
- Provide the AI with relevant files or functions to improve accuracy.
- Keep prompts specific and include the expected output type (code diff, test cases, documentation).