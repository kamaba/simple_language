VM C-Style Coding Guide

Goal
- Move VM implementation toward a C-like coding style to make it straightforward to port to C/C++ later and to reduce runtime allocations and high-level C# features.

Principles
- Avoid high-level LINQ, delegates, events, and reflection in hot VM code paths.
- Prefer explicit arrays and simple collections over heavy generics where possible.
- Use explicit loops (for/while) instead of foreach when performance matters.
- Minimize allocations: reuse buffers, avoid temporary boxed objects in hot loops.
- Keep error handling simple: use integer error codes or out parameters in low-level code (avoid exceptions for control flow in hot paths).
- Use plain data structures (structs or classes with simple fields) to represent VM state and avoid complex GC pressure.
- Limit use of C# language sugar (var, extension methods) in core VM files to keep code obvious and portable.

Files to prioritize
- source/VM/InnerCLRRuntime/* (runtime core)
- source/VM/LocalRuntime/* (memory manager)
- source/VM/Object/* (object representations and manager)

Refactoring checklist (for each file)
1. Identify hot paths (loops, instruction dispatch) and remove allocations.
2. Replace foreach with indexed for loops where possible.
3. Replace Dictionaries in hot paths with arrays or preallocated maps; keep Dictionary only for tooling or non-hot code.
4. Convert complex objects used only as data carriers into structs where appropriate.
5. Add small helper methods for repeated patterns (push/pop stack) that operate on raw arrays.

Example (before -> after)
// before (allocating List and using LINQ)
// var data = someList.Where(x => x.active).ToList();
// after (C-style)
// for (int i=0;i<someList.Count;i++) { if (someList[i].active) { /* process inline without allocations */ } }

Notes
- This repo targets .NET 6; some conveniences are allowed, but core VM should avoid features that complicate porting.
- Do not attempt to rewrite the entire VM at once. Proceed incrementally, file by file, and run builds/tests after each change.

If you want, I will start converting a few core VM files to the style above. Which file should I refactor first? (suggestions: RuntimeVM.cs, ObjectManager.cs, LocalRuntimeVM.cs)