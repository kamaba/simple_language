# Simple Language [中文](https://github.com/kamaba/simple_language/blob/main/README_CN.md)

------------------------------------------------------------------------

### Overview

SimpleLanguage is a **statically typed, object-oriented** language. The first implementation is built on the **C# / .NET** stack. The surface syntax is broadly C#-like with ideas from other languages. **The language is project-centric**: compilation, entry points, and global settings are driven by project files; you do not use it as a loose script without a project.

Roadmap (high level):

- **Phase 1**: Front-end; .NET integration; full language surface; templates and data-related types; IR export and execution on the built-in VM.  
- **Phase 2**: C99 rewrite; export to C# IR / JVM and similar; interop with C#/native or JVM; JavaScript and other targets; C99 export as libraries.  
- **Phase 3**: LLVM-style backends; multiple export targets; native packaging and linking; continued VM evolution.

### Language features

1. Simple notation, no enforced pretty-printing; code blocks mainly use braces.  
2. Comments support nesting and Markdown-style blocks.

### Design goals

1. Strong readability  
2. Strong writability  
3. Light syntactic sugar only when it supports (1) and (2)  
4. Pure object-oriented model  
5. Moderate use of inheritance and interfaces; avoid ambiguous constructs such as duplicate names in the same scope  

### Documentation and projects (essentials)

- **Master index** (bookmark this): [`md/INDEX.md`](md/INDEX.md) — one hub for syntax, project config, VM, logging, and AI-oriented docs.  
- **Syntax overview**: [`md/syntax/introduction.md`](md/syntax/introduction.md) — scope of the syntax docs, relation to IR, and conventions for examples.  
- **Project configuration (current)**: use **`<ProjectName>.sp`** together with a **same-named `<ProjectName>.jsonc`** in the same folder. JSONC holds source roots, entry file, compile file list, `compile` / `global` / `references`, and more. See [`md/project/project-config-jsonc-guide.md`](md/project/project-config-jsonc-guide.md).  
- **Entry points and `global`**: `_main_` / `_test_`, and how `global` maps to `Project{}` plus `global.data` from JSONC — [`md/project/project_sp-guide.md`](md/project/project_sp-guide.md).  
- **Debugging and export layout**: logs, `DebugCode` pipeline, and export paths — [`md/ai/DEBUG_WORKFLOW.md`](md/ai/DEBUG_WORKFLOW.md), [`md/ai/EXPORT_PATHS.md`](md/ai/EXPORT_PATHS.md).  
- **Typical CLI** (from the project guide): `sl new project`, `sl new classfile`, `sl c`, `sl c -e ir`, wired to `jsonc` as described in the JSONC guide above.

### Quick example

```csharp
file:test.sp

import CSharp.System;

DemoClass
{
    a = 0i;
    b = 100i;

    _init_( int _a, int _b )
    {
        this.a = _a;
        this.b = _b * 2;
    }

    Add()
    {
        return this.a + this.b;
    }
    PrintAddRes()
    {
        # if a=10 b=100 then output a[10]+b[200]=210
        Debug.Write( "a[$this.a ]+b[$this.b ]=" + Add().ToString() );
    }
}

ProjectEnter
{
    static Main()
    {    
        DC = DemoClass(10, 100);
        DC.PrintAddRes();
    }
    static Test()
    {
    }
}
```

### Syntax topics

The full, grouped list lives in **[`md/INDEX.md`](md/INDEX.md)** (section 3). Common links:

#### Basics

1. [Namespaces](md/syntax/namespace.md)  
2. [Base syntax](md/syntax/base.md)  
3. [Numbers](md/syntax/number.md)  
4. [Strings](md/syntax/string.md)  
5. [Variables](md/syntax/variable.md)  
6. [Expressions](md/syntax/express.md)  
7. [Operators](md/syntax/operator.md)  
8. [if](md/syntax/if.md)  
9. [switch](md/syntax/switch.md)  
10. [Loops](md/syntax/forwhiledowhile.md)  
11. [Methods / functions](md/syntax/function.md)  
12. [Enums](md/syntax/enum.md)  
13. [Data types](md/syntax/data.md)  
14. [Classes](md/syntax/class.md)  
15. [Objects](md/syntax/object.md)  
16. [Arrays](md/syntax/array.md)  

#### Advanced

1. [Inheritance](md/syntax/extend.md)  
2. [Interfaces](md/syntax/interface.md)  
3. [Labels and goto](md/syntax/labelgoto.md)  
4. [Macros](md/syntax/marco.md)  
5. [Modules and class organization in projects](md/project/project-module.md)  
6. [Casts](md/syntax/cast.md)  
7. [List](md/syntax/contraint/list.md)  
8. [Set](md/syntax/contraint/set.md)  
9. [Map](md/syntax/contraint/map.md)  
10. [Tuple](md/syntax/contraint/tuple.md)  
11. [Queue](md/syntax/contraint/queue.md)  
12. [Stack](md/syntax/contraint/stack.md)  
13. [try / catch](md/syntax/trycatch.md)  
14. [Templates](md/syntax/template.md)  
15. [global and the project](md/syntax/global.md)  

#### Standard library and runtime docs

1. [std / environment](md/syntax/std/env.md)  
2. [System methods](md/syntax/system_method.md)  
3. [Virtual machine notes](md/syntax/virtualmachine.md)  
4. [Export / IR](md/syntax/exporter.md)  

### Supported platforms

The active toolchain is **.NET**-based (`Front` compiler front-end and `VM`). For concrete paths and artifacts, see [`md/project/project-config-jsonc-guide.md`](md/project/project-config-jsonc-guide.md) and [`md/ai/EXPORT_PATHS.md`](md/ai/EXPORT_PATHS.md).

### Install and use

1. Clone the repository and install a compatible **.NET** SDK for the solution.  
2. Use the **`sl`** CLI to scaffold projects, register sources, and compile; see [`md/project/project-config-jsonc-guide.md`](md/project/project-config-jsonc-guide.md).  
3. When debugging compile or VM issues, follow [`md/ai/DEBUG_WORKFLOW.md`](md/ai/DEBUG_WORKFLOW.md) and inspect `Logs/` and `DebugCode/`.

### Contact

mail: kamaba233@gmail.com
