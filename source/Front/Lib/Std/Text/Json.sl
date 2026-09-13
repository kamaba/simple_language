
#JSON 文件操作门面：在 BaseJson（内存解析/序列化/树操作基础能力）之上扩展文件读写。
#BaseJson 只处理文本；本类补充文件进出（load/readFrom/save 等）
#与关联路径（_path）的文件信息查询。
#典型用法：Json j = Json.load("config.json") -> j.getStr("server/host", "") / j.save()。
public class Text.Json extends BaseJson
{
    #当前关联的文件路径（load/readFrom/saveAs 成功时记录，save()/reload() 依据它读写）
    string _path = ""

    # ---- 静态工厂（从文件加载）----
    #读取文件并解析；文件不可读或内容非法返回空实例
    public static Json load( string path )
    {
        Json result = new()
        result.readFrom(path)
        ret result
    }

    # ---- 实例文件操作 ----
    #从文件读取并整体替换当前树；文件不存在、不可读或内容非法返回 false
    public bool readFrom( string path )
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
        if !this.parseText(text)
        {
            ret false
        }
        this._path = path
        ret true
    }
    #按关联路径重新加载
    public bool reload()
    {
        if this._path == null || this._path.length == 0
        {
            ret false
        }
        ret this.readFrom(this._path)
    }
    #以紧凑格式写回关联路径；未关联路径返回 false
    public bool save()
    {
        if this._path == null || this._path.length == 0
        {
            ret false
        }
        ret SystemFileWriteAllText(this._path, this.toJson())
    }
    #以缩进美化格式写回关联路径；未关联路径返回 false
    public bool savePretty()
    {
        if this._path == null || this._path.length == 0
        {
            ret false
        }
        ret SystemFileWriteAllText(this._path, this.toJsonPretty())
    }
    #以紧凑格式写入指定路径并把它记录为关联路径
    public bool saveAs( string path )
    {
        if path == null || path.length == 0
        {
            ret false
        }
        if !SystemFileWriteAllText(path, this.toJson())
        {
            ret false
        }
        this._path = path
        ret true
    }
    #以缩进美化格式写入指定路径并把它记录为关联路径
    public bool saveAsPretty( string path )
    {
        if path == null || path.length == 0
        {
            ret false
        }
        if !SystemFileWriteAllText(path, this.toJsonPretty())
        {
            ret false
        }
        this._path = path
        ret true
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
