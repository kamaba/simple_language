# AOT Exporter (IR -> LLVM IR)

This document describes the design and usage of the AOT exporter that lowers project IR to LLVM IR text (.ll) for later native compilation via LLVM tools.

Overview
- The exporter consumes `IRMethod`/`IRData` structures produced by the existing IR pipeline.
- Primitive types map to LLVM scalar types; objects are represented as opaque pointers to runtime-managed heap objects.
- For initial implementation we emit textual `.ll` files as placeholders and small functions; subsequent steps implement full instruction lowering.

Basic mapping
- Int32/UInt32 -> `i32`
- Int64/UInt64 -> `i64`
- Float32 -> `float`
- Float64/Num -> `double`
- Object/class -> `%objtype*` (opaque pointer)

Export flow
1. Inventory IR (done)
2. Emit module and required runtime intrinsics
3. For each IRMethod, lower its IRData sequence into LLVM basic blocks and instructions
4. Emit global data and metadata for classes and runtime types

Usage
- A small tool `Export/AOT/LLVMEmitter` is provided as prototype. It can be extended and integrated into build.

