# 对应文档：`md/syntax/switch.md`（整型分支）
MdEx05SwitchValue
{
    static Run()
    {
        day = 3
        switch day
        {
            case 1 { global.println("Mon") }
            case 2 { global.println("Tue") }
            case 3 { global.println("Wed") }
            default { global.println("other") }
        }
    }
}
