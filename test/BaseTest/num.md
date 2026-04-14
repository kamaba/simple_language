# Number/Type Writing Notes (Token + FileMeta)

## 1) Token layer findings

Scanned file: `source/Front/Compile/Parse/LexerParse.cs`

- Built-in numeric **keywords** recognized directly by lexer are lowercase:
  - `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `bool`, `string`
- Mapping in lexer keyword switch:
  - `short -> EType.Int16`
  - `ushort -> EType.UInt16`
  - `int -> EType.Int32`
  - `uint -> EType.UInt32`
  - `long -> EType.Int64`
  - `ulong -> EType.UInt64`
  - `byte -> EType.Byte`
  - `sbyte -> EType.SByte`
- Numeric literal suffix (also lexer):
  - `1s -> Int16`
  - `1us -> UInt16`
  - `1i -> Int32`
  - `1ui -> UInt32`
  - `1L -> Int64`
  - `1uL -> UInt64`
  - `1.0f -> Float32`
  - `1.0d -> Float64`

## 2) FileMeta / type system findings

Scanned files:
- `source/Front/Compile/FileMeta/FileMetatUtil.cs`
- `source/Front/Core/TypeManager.cs`
- `source/Front/Core/BaseMetaClass/CoreMetaClassManager.cs`
- `source/Front/Define.cs`

- Core runtime type enum is canonical PascalCase: `Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Float32`, `Float64`, `Boolean`, `String`, `Num`.
- `FileMeta` phase itself does not hardcode only-lowercase aliases; it resolves type/class references through meta/class lookup.
- Therefore uppercase core type names (e.g. `Int16`, `Byte`) can be used as class-style type names in code style.

## 3) Practical writing convention (recommended)

To keep project style consistent and unambiguous:

- Prefer core type names in declarations:
  - `Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Float32`, `Float64`, `Boolean`, `String`, `Num`
- Keep alias relation clear:
  - `short == Int16`
  - `byte == Byte`
  - `int == Int32`
  - `long == Int64`
- For tests/docs, prefer one style only (PascalCase core names), avoid mixed lowercase aliases.

## 4) NumberTest update done

`test/BaseTest/NumberTest.sl` has been updated to this style:

- `int/byte/sbyte/int16/...` -> `Int32/Byte/SByte/Int16/...`
- `num` -> `Num`
- `string` -> `String`
- cast style adjusted to `as Int32`

