# 验证 export.publicExport 三档类导出门槛（none/public/extern）
# 规则（详见 md/project/project-config-jsonc-guide.md）：
# - extern class/data/enum：类级显式导出标记，不受三态影响恒导出；
# - public class 与无标记 class：按三态档位决定（none=不导出 / public=照常 / extern=照常且包内记 Export）；
# - private/protected：恒不导出；
# - 旧语法 export class/data/enum 的 export 关键字已移除权限语义（映射 Null=不导出），
#   类级显式导出统一使用 extern 关键字。

extern class ExternClass
{
    const Int32 EF = 1;                  # 实例 const 成员（SLIR flags bit16 跨模块导出验证）
    public static const Int32 ESCF = 2;  # 静态 const 成员
}

public class PublicClass
{
}

class PlainClass
{
}

private class PrivateClass
{
}

extern data ExternData
{
    v = 0
}

public data PublicData
{
    v = 0
}

extern enum ExternEnum
{
    A = 1
}

public enum PublicEnum
{
    A = 1
}
