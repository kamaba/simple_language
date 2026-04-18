# 04 — Range 与 `for ... in`

- **文档**：`md/syntax/range.md`
- **源码**：`Doc04_RangeFor.sl`
- **参考测试**：`test/BaseTest/RangeTest.sl`

说明：整型字面量 **`1..5`** 在部分文件组合下 Meta 曾出现 NRE；本示例改为与 `RangeTest` 一致的 **`Range<int>(1, 6, 1)`**（上界为开区间语义，效果接近 `1..5` 的迭代）。文档中的 `a..b` 写法仍见 `range.md` / `RangeTest.sl`。

并入 Core 后 `_main_`：`MdEx04RangeFor.Run();`。
