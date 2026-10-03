# 验证 export.publicExport 三档类导出门槛（none/public/extern）
export class ExportOnlyClass
{
}

public class PublicClass
{
}

extern class ExternClass
{
}

class PlainClass
{
}

export data ExportOnlyData
{
    v = 0
}

public data PublicData
{
    v = 0
}

export enum ExportOnlyEnum
{
    A = 1
    B = 2
}

public enum PublicEnum
{
    A = 1
}
