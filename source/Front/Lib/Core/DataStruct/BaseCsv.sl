
#CSV 文件门面：基于 Table 二维表容器实现的 CSV 读写工具。
#Table 承载列名表头、定宽数据行、类型化取值与查询统计能力（解析/序列化在 VM 层完成），
#本类负责 CSV 文本的进出与配置（表头开关、分隔符），并把 Table 的行/列/单元格/查询/统计接口透传出来。
#典型用法：Csv c = Csv("name,age\nalice,30") -> c.getInt(0, "age") / c.toString()。
public class BaseCsv extends Object
{
    #内部二维表（列名 + 数据行的权威载体）
    Table _table = new()
    #字段分隔符（构造时指定，默认逗号；序列化时使用）
    string _delimiter = ","

    #默认构造：空表（表头模式，逗号分隔）
    override _init_()
    {
    }
    #按 CSV 文本构造（首行为表头，逗号分隔；解析在 VM 层完成）
    void _init_( string csvText )
    {
        if csvText == null
        {
            ret
        }
        SystemTableCsvParse(this._table, csvText, true, ",")
    }
    #按 CSV 文本构造（可指定表头开关与分隔符；解析在 VM 层完成）
    void _init_( string csvText, bool hasHeader, string delimiter )
    {
        if delimiter == null || delimiter.length == 0
        {
            delimiter = ","
        }
        this._delimiter = delimiter
        if csvText == null
        {
            this._table.hasHeader = hasHeader
            ret
        }
        SystemTableCsvParse(this._table, csvText, hasHeader, delimiter)
    }
    #包装既有 Table（共享同一表对象，对 Csv 的修改即对 Table 的修改）
    void _init_( Table table )
    {
        if table != null
        {
            this._table = table
        }
    }

    # ---- 静态工厂 ----
    #解析 CSV 文本（首行为表头）
    public static BaseCsv parse( string csvText )
    {
        ret BaseCsv(csvText)
    }
    #解析无表头 CSV（列名自动补 col{n}）
    public static BaseCsv parseNoHeader( string csvText )
    {
        ret BaseCsv(csvText, false, ",")
    }
    #解析 CSV 文本（自定义表头开关与分隔符）
    public static BaseCsv parseDelimited( string csvText, bool hasHeader, string delimiter )
    {
        ret BaseCsv(csvText, hasHeader, delimiter)
    }

    # ---- 基础属性 ----
    #内部表活引用（可直接使用 Table 全部能力）
    get Table table()
    {
        ret this._table
    }
    get int rowCount()
    {
        ret this._table.rowCount
    }
    get int columnCount()
    {
        ret this._table.columnCount
    }
    get bool isEmpty()
    {
        ret this._table.isEmpty
    }
    get bool isNotEmpty()
    {
        ret this._table.isNotEmpty
    }
    #序列化时是否输出表头行
    get bool hasHeader()
    {
        ret this._table.hasHeader
    }
    set void hasHeader( bool value )
    {
        this._table.hasHeader = value
    }
    #当前分隔符
    get string delimiter()
    {
        ret this._delimiter
    }

    # ---- 行索引器（返回行数组活引用，越界返回 null）----
    public Array<Object> _getItem_( int row )
    {
        ret this._table._getItem_(row)
    }

    # ---- 表头维护（透传 Table）----
    public void addColumn( string name )
    {
        this._table.addColumn(name)
    }
    public void insertColumn( int colIndex, string name )
    {
        this._table.insertColumn(colIndex, name)
    }
    public bool removeColumn( string name )
    {
        ret this._table.removeColumn(name)
    }
    public bool removeColumnAt( int colIndex )
    {
        ret this._table.removeColumnAt(colIndex)
    }
    public bool renameColumn( string oldName, string newName )
    {
        ret this._table.renameColumn(oldName, newName)
    }
    public bool containsColumn( string name )
    {
        ret this._table.containsColumn(name)
    }
    public int getColumnIndex( string name )
    {
        ret this._table.getColumnIndex(name)
    }
    public string getColumnName( int colIndex )
    {
        ret this._table.getColumnName(colIndex)
    }
    public Array<string> columnNames()
    {
        ret this._table.columnNames()
    }
    public List<Object> getColumnValues( string name )
    {
        ret this._table.getColumnValues(name)
    }

    # ---- 行维护（透传 Table）----
    public int newRow()
    {
        ret this._table.newRow()
    }
    public int addRow( Array<Object> row )
    {
        ret this._table.addRow(row)
    }
    public int insertRow( int row, Array<Object> rowData )
    {
        ret this._table.insertRow(row, rowData)
    }
    public bool removeRowAt( int row )
    {
        ret this._table.removeRowAt(row)
    }
    #删除指定列上值匹配的所有行（返回删除行数）
    public int removeRowsWhere( string columnName, Object value )
    {
        ret this._table.removeRowsWhere(columnName, value)
    }
    #清空数据行（保留表头）
    public void clear()
    {
        this._table.clear()
    }
    #全部清空（表头 + 数据行）
    public void clearAll()
    {
        this._table.clearAll()
    }

    # ---- 单元格读写（透传 Table）----
    public Object getValue( int row, int col )
    {
        ret this._table.getValue(row, col)
    }
    public Object getValueByName( int row, string columnName )
    {
        ret this._table.getValueByName(row, columnName)
    }
    public void setValue( int row, int col, Object value )
    {
        this._table.setValue(row, col, value)
    }
    public void setValueByName( int row, string columnName, Object value )
    {
        this._table.setValueByName(row, columnName, value)
    }

    # ---- 类型化取值（null / 越界 / 无法解析返回默认值）----
    public int getInt( int row, int col )
    {
        ret this._table.getInt(row, col)
    }
    public int getInt( int row, int col, int defaultValue )
    {
        ret this._table.getInt(row, col, defaultValue)
    }
    public int getIntByName( int row, string columnName, int defaultValue )
    {
        ret this._table.getIntByName(row, columnName, defaultValue)
    }
    public Float64 getFloat( int row, int col )
    {
        ret this._table.getFloat(row, col)
    }
    public Float64 getFloat( int row, int col, Float64 defaultValue )
    {
        ret this._table.getFloat(row, col, defaultValue)
    }
    public Float64 getFloatByName( int row, string columnName, Float64 defaultValue )
    {
        ret this._table.getFloatByName(row, columnName, defaultValue)
    }
    public bool getBool( int row, int col )
    {
        ret this._table.getBool(row, col)
    }
    public bool getBool( int row, int col, bool defaultValue )
    {
        ret this._table.getBool(row, col, defaultValue)
    }
    public bool getBoolByName( int row, string columnName, bool defaultValue )
    {
        ret this._table.getBoolByName(row, columnName, defaultValue)
    }
    public string getStr( int row, int col )
    {
        ret this._table.getStr(row, col)
    }
    public string getStr( int row, int col, string defaultValue )
    {
        ret this._table.getStr(row, col, defaultValue)
    }
    public string getStrByName( int row, string columnName, string defaultValue )
    {
        ret this._table.getStrByName(row, columnName, defaultValue)
    }

    # ---- 克隆 / 合并 / 连接（透传 Table）----
    #值语义深拷贝
    public BaseCsv clone()
    {
        ret BaseCsv(this._table.clone())
    }
    #结构克隆：只保留表头，不携带数据行
    public BaseCsv cloneStructure()
    {
        ret BaseCsv(this._table.cloneStructure())
    }
    #区间克隆（越界自动截断）
    public BaseCsv cloneRange( int rowStart, int rowCount, int colStart, int colCount )
    {
        ret BaseCsv(this._table.cloneRange(rowStart, rowCount, colStart, colCount))
    }
    #纵向合并（other 的行按列名对齐追加，缺失列补 null）
    public bool merge( Table other )
    {
        ret this._table.merge(other)
    }
    public bool mergeCsv( BaseCsv other )
    {
        if other == null
        {
            ret false
        }
        ret this._table.merge(other._table)
    }
    #内连接：以 keyColumn 为键
    public Table join( Table other, string keyColumn )
    {
        ret this._table.join(other, keyColumn)
    }
    public Table joinCsv( BaseCsv other, string keyColumn )
    {
        if other == null
        {
            ret null
        }
        ret this._table.join(other._table, keyColumn)
    }

    # ---- 查询（透传 Table，查找/排序/去重在 VM 层完成）----
    public Array<int> findRows( string columnName, Object value )
    {
        ret this._table.findRows(columnName, value)
    }
    public void sortBy( string columnName )
    {
        this._table.sortBy(columnName)
    }
    public void sortBy( string columnName, bool ascending )
    {
        this._table.sortBy(columnName, ascending)
    }
    public void sortByAt( int colIndex, bool ascending )
    {
        this._table.sortByAt(colIndex, ascending)
    }
    public int distinctRows()
    {
        ret this._table.distinctRows()
    }

    # ---- 统计（透传 Table）----
    public Float64 min( string columnName )
    {
        ret this._table.min(columnName)
    }
    public Float64 max( string columnName )
    {
        ret this._table.max(columnName)
    }
    public Float64 sum( string columnName )
    {
        ret this._table.sum(columnName)
    }
    public Float64 avg( string columnName )
    {
        ret this._table.avg(columnName)
    }

    # ---- 序列化 ----
    #序列化为 CSV（按当前表头开关，逗号分隔；转义在 VM 层完成）
    public string toCsv()
    {
        ret this._table.toCsv()
    }
    #序列化为 CSV（使用构造时指定的分隔符）
    public string toCsvDelimited()
    {
        ret this._table.toCsvDelimited(this._delimiter)
    }
    #序列化为 CSV（显式指定分隔符）
    public string toCsvDelimited( string delimiter )
    {
        ret this._table.toCsvDelimited(delimiter)
    }
    #无表头序列化（一次性开关，不改变表状态）
    public string toCsvNoHeader()
    {
        ret SystemTableCsvToString(this._table, false, this._delimiter)
    }

    # ---- 显示 ----
    #markdown 风格预览（默认前 10 行，VM 层完成）
    public string preview( int maxRows )
    {
        ret this._table.preview(maxRows)
    }
    override string toString()
    {
        ret this._table.toCsv()
    }
}
