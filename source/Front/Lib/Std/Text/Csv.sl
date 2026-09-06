
#CSV 文件操作门面：在 BaseCsv（内存解析/序列化/表操作基础能力）之上扩展文件读写。
#BaseCsv 只提供基础方法；本类补充文件进出（load/readFrom/save/appendTo 等）
#与关联路径（_path）的文件信息查询。
#典型用法：Csv c = Csv.load("data.csv") -> c.getIntByName(0, "age", 0) / c.save()。
public class Text.Csv extends BaseCsv
{
    #当前关联的文件路径（load/readFrom/saveAs 成功时记录，save()/reload() 依据它读写）
    string _path = ""

    # ---- 静态工厂（从文件加载）----
    #读取文件并解析（首行为表头，逗号分隔）；文件不可读返回空表实例
    public static Csv load( string path )
    {
        Csv result = new()
        result.readFrom(path)
        ret result
    }
    #读取无表头文件（列名自动补 col{n}）
    public static Csv loadNoHeader( string path )
    {
        Csv result = new()
        result.readFrom(path, false, ",")
        ret result
    }
    #读取文件（自定义表头开关与分隔符）
    public static Csv loadDelimited( string path, bool hasHeader, string delimiter )
    {
        Csv result = new()
        result.readFrom(path, hasHeader, delimiter)
        ret result
    }

    # ---- 实例文件操作 ----
    #从文件读取并整体替换当前表（首行为表头，逗号分隔）
    public bool readFrom( string path )
    {
        ret this.readFrom(path, true, ",")
    }
    #从文件读取并整体替换当前表（自定义表头开关与分隔符）
    public bool readFrom( string path, bool hasHeader, string delimiter )
    {
        if path == null || path.length == 0
        {
            ret false
        }
        #文件不存在直接失败（ReadAllText 对缺失文件可能返回空串而非 null，需显式检查）
        if !SystemFileExists(path)
        {
            ret false
        }
        string text = SystemFileReadAllText(path)
        if text == null
        {
            ret false
        }
        BaseCsv parsed = BaseCsv(text, hasHeader, delimiter)
        this._table = parsed._table
        this._delimiter = parsed._delimiter
        this._path = path
        ret true
    }
    #按关联路径重新加载（沿用当前表头开关与分隔符）
    public bool reload()
    {
        if this._path == null || this._path.length == 0
        {
            ret false
        }
        ret this.readFrom(this._path, this.hasHeader, this._delimiter)
    }
    #写回关联路径（使用构造时指定的分隔符）；未关联路径返回 false
    public bool save()
    {
        if this._path == null || this._path.length == 0
        {
            ret false
        }
        ret SystemFileWriteAllText(this._path, this.toCsvDelimited())
    }
    #写入指定路径并把它记录为关联路径
    public bool saveAs( string path )
    {
        if path == null || path.length == 0
        {
            ret false
        }
        if !SystemFileWriteAllText(path, this.toCsvDelimited())
        {
            ret false
        }
        this._path = path
        ret true
    }
    #追加数据行到指定文件（不含表头，避免重复表头；文件不存在时改为整表写入含表头）
    public bool appendTo( string path )
    {
        if path == null || path.length == 0
        {
            ret false
        }
        if SystemFileExists(path)
        {
            ret SystemFileAppendText(path, this.toCsvNoHeader())
        }
        ret SystemFileWriteAllText(path, this.toCsvDelimited())
    }

    # ---- 关联文件信息 ----
    #当前关联的文件路径（未关联为空串）
    get string path()
    {
        ret this._path
    }
    #关联文件是否存在
    get bool fileExists()
    {
        if this._path == null || this._path.length == 0
        {
            ret false
        }
        ret SystemFileExists(this._path)
    }
    #关联文件大小（字节；未关联或不存在为 0）
    public Int64 fileSize()
    {
        if this._path == null || this._path.length == 0
        {
            ret 0
        }
        ret SystemFileGetSize(this._path)
    }
}
