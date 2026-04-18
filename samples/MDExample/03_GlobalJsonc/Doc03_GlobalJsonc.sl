# 对应文档：`md/syntax/global.md`、`md/syntax/base.md`（`global.data`）
# 依赖 Core.jsonc 中已配置的 global.data（如 var1）；并入 Core 后可用。
MdEx03GlobalJsonc
{
    static Run()
    {
        global.println("from jsonc var1=" + global.var1.toString())
    }
}
