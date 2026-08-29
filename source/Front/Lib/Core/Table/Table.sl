
#二维表容器：CSV / Excel / 数据库结果集的中间数据层（仿 DataTable / pandas DataFrame 的精简版）。
#核心数据结构：
#   _columns   -- 列名表头 List<string>（列的唯一权威定义）
#   _rows      -- 数据行 List<Array<Object>>，每行为与列宽等长的定宽数组
#   _hasHeader -- CSV 序列化时是否输出表头行（解析时由入口指定，不影响行存储）
#单元格为 Object（string / 数值标量 / 布尔 / null 皆可），读取时经 getXxx 族做健壮类型转换。
#性能设计：克隆、合并、查找、排序、去重、CSV 解析/序列化、预览等遍历与比较的耗时操作
#全部通过 SystemTable* 系统调用映射到 CVM 原生实现（table_system_method.c），
#SL 层仅负责表头/行的结构性维护与类型化取值入口。
public class Table extends Object interface Core.IIterable<Array<Object>>, Core.IIterator<Array<Object>>
{
    # --- 核心字段 ---
    List<string> _columns = new()               # 列名表头
    List<Array<Object>> _rows = new()           # 数据行（每行定宽数组，列数变化时重建）
    bool _hasHeader = true                      # 序列化时是否输出表头行

    # --- 迭代器字段 ---
    int _index = -1
    Array<Object> _current = null

    #默认构造：无列无行，列名在使用时动态补充
    override _init_()
    {
    }
    #按列名构造（null 名自动补 "col{n}"）
    void _init_( Array<string> columns )
    {
        if columns == null
        {
            ret
        }
        int width = columns.length
        for c = 0, c < width, c++
        {
            string name = columns._getItem_(c)
            if name == null
            {
                name = "col" + c.toString()
            }
            this._columns.add(name)
        }
    }
    #按列名 + 预建空行数构造
    void _init_( Array<string> columns, int rowCount )
    {
        if columns != null
        {
            int width = columns.length
            for c = 0, c < width, c++
            {
                string name = columns._getItem_(c)
                if name == null
                {
                    name = "col" + c.toString()
                }
                this._columns.add(name)
            }
        }
        if rowCount < 0
        {
            rowCount = 0
        }
        int width2 = this._columns.length
        for r = 0, r < rowCount, r++
        {
            this._rows.add(Array<Object>(width2))
        }
    }
    #克隆构造：单元格值语义深拷贝（VM 层单次调用完成）
    void _init_( Table other )
    {
        if other == null
        {
            ret
        }
        SystemTableClone(this, other)
    }

    # ---- 基础属性 ----
    #数据行数
    get int rowCount()
    {
        ret this._rows.length
    }
    #列数
    get int columnCount()
    {
        ret this._columns.length
    }
    get bool isEmpty()
    {
        if this._rows.length <= 0
        {
            ret true
        }
        ret false
    }
    get bool isNotEmpty()
    {
        if this._rows.length > 0
        {
            ret true
        }
        ret false
    }
    #序列化时是否输出表头行
    get bool hasHeader()
    {
        ret this._hasHeader
    }
    set void hasHeader( bool value )
    {
        this._hasHeader = value
    }
    #列名数组拷贝（修改返回值不影响表头）
    Array<string> columnNames()
    {
        int width = this._columns.length
        Array<string> result = Array<string>(width)
        for c = 0, c < width, c++
        {
            result._setItem_(c, this._columns._getItem_(c))
        }
        ret result
    }
    #首行 / 末行（空表返回 null；返回的是行数组活引用）
    get Array<Object> first()
    {
        if this._rows.length <= 0
        {
            ret null
        }
        ret this._rows._getItem_(0)
    }
    get Array<Object> last()
    {
        int rowCount = this._rows.length
        if rowCount <= 0
        {
            ret null
        }
        ret this._rows._getItem_(rowCount - 1)
    }

    # ---- 行索引器（返回行数组活引用，越界返回 null）----
    public Array<Object> _getItem_( int row )
    {
        if row < 0 || row >= this._rows.length
        {
            ret null
        }
        ret this._rows._getItem_(row)
    }
    #整行替换（经规整对齐当前列宽，非法索引忽略）
    public void _setItem_( int row, Array<Object> value )
    {
        if row < 0 || row >= this._rows.length
        {
            ret
        }
        if value == null
        {
            ret
        }
        this._rows._setItem_(row, this.normalizeRow(value))
    }

    # ---- 表头维护 ----
    #追加列（null 名自动补 "col{n}"；已有行末尾补 null 单元格）
    public void addColumn( string name )
    {
        int width = this._columns.length
        if name == null
        {
            name = "col" + width.toString()
        }
        this._columns.add(name)
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            Array<Object> oldRow = this._rows._getItem_(r)
            Array<Object> newRow = Array<Object>(width + 1)
            int srcLen = oldRow.length
            int c = 0
            while c < srcLen
            {
                newRow._setItem_(c, oldRow._getItem_(c))
                c++
            }
            this._rows._setItem_(r, newRow)
        }
    }
    #在 colIndex 处插入列（越界时退化为末尾追加；已有行在 colIndex 处插入 null 单元格）
    public void insertColumn( int colIndex, string name )
    {
        int width = this._columns.length
        if name == null
        {
            name = "col" + width.toString()
        }
        if colIndex < 0 || colIndex > width
        {
            this.addColumn(name)
            ret
        }
        this._columns.insert(colIndex, name)
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            Array<Object> oldRow = this._rows._getItem_(r)
            Array<Object> newRow = Array<Object>(width + 1)
            int srcLen = oldRow.length
            int c = 0
            while c < srcLen && c < colIndex
            {
                newRow._setItem_(c, oldRow._getItem_(c))
                c++
            }
            c = colIndex
            while c < srcLen
            {
                newRow._setItem_(c + 1, oldRow._getItem_(c))
                c++
            }
            this._rows._setItem_(r, newRow)
        }
    }
    #按名删除列（返回是否删除成功）
    public bool removeColumn( string name )
    {
        ret this.removeColumnAt(this.getColumnIndex(name))
    }
    #按下标删除列（所有行同步删除对应单元格）
    public bool removeColumnAt( int colIndex )
    {
        int width = this._columns.length
        if colIndex < 0 || colIndex >= width
        {
            ret false
        }
        this._columns.removeAt(colIndex)
        int newWidth = width - 1
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            Array<Object> oldRow = this._rows._getItem_(r)
            Array<Object> newRow = Array<Object>(newWidth)
            int srcLen = oldRow.length
            int c = 0
            while c < colIndex && c < srcLen
            {
                newRow._setItem_(c, oldRow._getItem_(c))
                c++
            }
            c = colIndex
            while c < newWidth && c + 1 < srcLen
            {
                newRow._setItem_(c, oldRow._getItem_(c + 1))
                c++
            }
            this._rows._setItem_(r, newRow)
        }
        ret true
    }
    #列重命名（列不存在返回 false）
    public bool renameColumn( string oldName, string newName )
    {
        int colIndex = this.getColumnIndex(oldName)
        if colIndex < 0
        {
            ret false
        }
        if newName == null
        {
            newName = "col" + colIndex.toString()
        }
        this._columns._setItem_(colIndex, newName)
        ret true
    }
    #是否包含指定列
    public bool containsColumn( string name )
    {
        if this.getColumnIndex(name) >= 0
        {
            ret true
        }
        ret false
    }
    #列名 -> 列下标（不存在返回 -1）
    public int getColumnIndex( string name )
    {
        int width = this._columns.length
        for c = 0, c < width, c++
        {
            if this._columns._getItem_(c) == name
            {
                ret c
            }
        }
        ret -1
    }
    #列下标 -> 列名（越界返回 null）
    public string getColumnName( int colIndex )
    {
        if colIndex < 0 || colIndex >= this._columns.length
        {
            ret null
        }
        ret this._columns._getItem_(colIndex)
    }
    #取整列值（列不存在返回空列表）
    public List<Object> getColumnValues( string name )
    {
        List<Object> result = new()
        int colIndex = this.getColumnIndex(name)
        if colIndex < 0
        {
            ret result
        }
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            Array<Object> row = this._rows._getItem_(r)
            if colIndex < row.length
            {
                result.add(row._getItem_(colIndex))
            }
            else
            {
                result.add(null)
            }
        }
        ret result
    }

    # ---- 行维护 ----
    #新建一行全 null 行并返回行号
    public int newRow()
    {
        Array<Object> row = Array<Object>(this._columns.length)
        this._rows.add(row)
        ret this._rows.length - 1
    }
    #追加一行（行宽超过列数时自动扩列补名；返回新行行号）
    public int addRow( Array<Object> row )
    {
        int rowLen = 0
        if row != null
        {
            rowLen = row.length
        }
        if rowLen > this._columns.length
        {
            this.expandRows(rowLen)
        }
        this._rows.add(this.normalizeRow(row))
        ret this._rows.length - 1
    }
    #在 row 处插入一行（越界忽略返回 -1；行宽超过列数时自动扩列补名）
    public int insertRow( int row, Array<Object> rowData )
    {
        int rowCount = this._rows.length
        if row < 0 || row > rowCount
        {
            ret -1
        }
        int rowLen = 0
        if rowData != null
        {
            rowLen = rowData.length
        }
        if rowLen > this._columns.length
        {
            this.expandRows(rowLen)
        }
        this._rows.insert(row, this.normalizeRow(rowData))
        ret row
    }
    #按下标删除行（返回是否删除成功）
    public bool removeRowAt( int row )
    {
        if row < 0 || row >= this._rows.length
        {
            ret false
        }
        this._rows.removeAt(row)
        ret true
    }
    #删除指定列上值匹配的所有行（返回删除行数；查找在 VM 层完成）
    public int removeRowsWhere( string columnName, Object value )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret 0
        }
        Array<int> hits = this.findRows(columnName, value)
        int n = hits.length
        for i = n - 1, i >= 0, i--
        {
            this._rows.removeAt(hits._getItem_(i))
        }
        ret n
    }
    #清空数据行（保留表头，迭代器复位）
    public void clear()
    {
        this._rows.clear()
        this.reset()
    }
    #全部清空（表头 + 数据行）
    public void clearAll()
    {
        this._columns.clear()
        this._rows.clear()
        this.reset()
    }

    # ---- 内部维护 ----
    #规整行宽：返回与当前列数等长的新数组（不足补 null，超出截断；null 行返回全 null 行）
    Array<Object> normalizeRow( Array<Object> row )
    {
        int width = this._columns.length
        Array<Object> result = Array<Object>(width)
        int count = 0
        if row != null
        {
            count = row.length
        }
        if count > width
        {
            count = width
        }
        for i = 0, i < count, i++
        {
            result._setItem_(i, row._getItem_(i))
        }
        ret result
    }
    #扩列到 width：新列名默认 "col{n}"，已有行重建为等宽数组（末尾补 null）
    void expandRows( int width )
    {
        int oldWidth = this._columns.length
        if width <= oldWidth
        {
            ret
        }
        int i = oldWidth
        while i < width
        {
            this._columns.add("col" + i.toString())
            i++
        }
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            Array<Object> oldRow = this._rows._getItem_(r)
            Array<Object> newRow = Array<Object>(width)
            int srcLen = oldRow.length
            int c = 0
            while c < srcLen
            {
                newRow._setItem_(c, oldRow._getItem_(c))
                c++
            }
            this._rows._setItem_(r, newRow)
        }
    }

    # ---- 单元格读写 ----
    #原始 Object 取值（越界 / null 单元格返回 null）
    public Object getValue( int row, int col )
    {
        if row < 0 || row >= this._rows.length
        {
            ret null
        }
        Array<Object> rowData = this._rows._getItem_(row)
        if col < 0 || col >= rowData.length
        {
            ret null
        }
        ret rowData._getItem_(col)
    }
    public Object getValueByName( int row, string columnName )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret null
        }
        ret this.getValue(row, colIndex)
    }
    #原始 Object 写值（越界忽略）
    public void setValue( int row, int col, Object value )
    {
        if row < 0 || row >= this._rows.length
        {
            ret
        }
        Array<Object> rowData = this._rows._getItem_(row)
        if col < 0 || col >= rowData.length
        {
            ret
        }
        rowData._setItem_(col, value)
    }
    public void setValueByName( int row, string columnName, Object value )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret
        }
        this.setValue(row, colIndex, value)
    }

    #类型化取值（健壮转换在 VM 层完成：null / 越界 / 无法解析返回默认值）
    public int getInt( int row, int col )
    {
        ret SystemTableGetCellInt(this, row, col, 0)
    }
    public int getInt( int row, int col, int defaultValue )
    {
        ret SystemTableGetCellInt(this, row, col, defaultValue)
    }
    public int getIntByName( int row, string columnName, int defaultValue )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret defaultValue
        }
        ret SystemTableGetCellInt(this, row, colIndex, defaultValue)
    }
    public Float64 getFloat( int row, int col )
    {
        ret SystemTableGetCellFloat(this, row, col, 0.0d)
    }
    public Float64 getFloat( int row, int col, Float64 defaultValue )
    {
        ret SystemTableGetCellFloat(this, row, col, defaultValue)
    }
    public Float64 getFloatByName( int row, string columnName, Float64 defaultValue )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret defaultValue
        }
        ret SystemTableGetCellFloat(this, row, colIndex, defaultValue)
    }
    public bool getBool( int row, int col )
    {
        ret SystemTableGetCellBool(this, row, col, false)
    }
    public bool getBool( int row, int col, bool defaultValue )
    {
        ret SystemTableGetCellBool(this, row, col, defaultValue)
    }
    public bool getBoolByName( int row, string columnName, bool defaultValue )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret defaultValue
        }
        ret SystemTableGetCellBool(this, row, colIndex, defaultValue)
    }
    #字符串取值（null / 越界返回 ""；数值单元格转最短字面量）
    public string getStr( int row, int col )
    {
        ret SystemTableGetCellString(this, row, col, "")
    }
    public string getStr( int row, int col, string defaultValue )
    {
        ret SystemTableGetCellString(this, row, col, defaultValue)
    }
    public string getStrByName( int row, string columnName, string defaultValue )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret defaultValue
        }
        ret SystemTableGetCellString(this, row, colIndex, defaultValue)
    }

    # ---- 克隆 ----
    #值语义深拷贝（行数组与单元格重建，字符串对象共享引用）
    public Table clone()
    {
        ret Table(this)
    }
    #结构克隆：只保留表头，不携带数据行
    public Table cloneStructure()
    {
        Table result = new()
        int width = this._columns.length
        for c = 0, c < width, c++
        {
            result.addColumn(this._columns._getItem_(c))
        }
        ret result
    }
    #区间克隆：行 [rowStart, rowStart+rowCount) x 列 [colStart, colStart+colCount)（越界自动截断）
    public Table cloneRange( int rowStart, int rowCount, int colStart, int colCount )
    {
        int rows = this._rows.length
        int width = this._columns.length
        if rowStart < 0
        {
            rowStart = 0
        }
        if rowStart > rows
        {
            rowStart = rows
        }
        if rowCount < 0
        {
            rowCount = 0
        }
        if rowStart + rowCount > rows
        {
            rowCount = rows - rowStart
        }
        if colStart < 0
        {
            colStart = 0
        }
        if colStart > width
        {
            colStart = width
        }
        if colCount < 0
        {
            colCount = 0
        }
        if colStart + colCount > width
        {
            colCount = width - colStart
        }
        Table result = new()
        int c = colStart
        while c < colStart + colCount
        {
            result.addColumn(this._columns._getItem_(c))
            c++
        }
        int r = rowStart
        while r < rowStart + rowCount
        {
            Array<Object> srcRow = this._rows._getItem_(r)
            Array<Object> newRow = Array<Object>(colCount)
            int i = 0
            while i < colCount
            {
                int srcCol = colStart + i
                if srcCol < srcRow.length
                {
                    newRow._setItem_(i, srcRow._getItem_(srcCol))
                }
                i++
            }
            result.addRow(newRow)
            r++
        }
        ret result
    }
    #行挑选克隆：按行号数组抽取行（非法行号跳过）
    public Table cloneRows( Array<int> rowIndices )
    {
        Table result = this.cloneStructure()
        if rowIndices == null
        {
            ret result
        }
        int width = this._columns.length
        int rowCount = this._rows.length
        int n = rowIndices.length
        for i = 0, i < n, i++
        {
            int r = rowIndices._getItem_(i)
            if r >= 0
            {
                if r < rowCount
                {
                    Array<Object> srcRow = this._rows._getItem_(r)
                    Array<Object> newRow = Array<Object>(width)
                    int c = 0
                    while c < width
                    {
                        if c < srcRow.length
                        {
                            newRow._setItem_(c, srcRow._getItem_(c))
                        }
                        c++
                    }
                    result.addRow(newRow)
                }
            }
        }
        ret result
    }

    # ---- 合并 ----
    #纵向合并：other 的行按列名对齐追加（缺失列自动补建并 pad null；返回是否成功）
    public bool merge( Table other )
    {
        if other == null
        {
            ret false
        }
        ret SystemTableMerge(this, other)
    }
    #纵向合并的新表版本：返回 this 与 other 合并后的新表
    public Table union( Table other )
    {
        Table result = Table(this)
        if other != null
        {
            result.merge(other)
        }
        ret result
    }
    #内连接：以 keyColumn 为键，右表列（键列除外）拼接到左侧行后；右表重名列加 "_2" 后缀。
    #表头构建、键倒排索引与行匹配拼接全部在 VM 层完成（table_system_method.c），键为单元格字符串形式。
    public Table join( Table other, string keyColumn )
    {
        if this.containsColumn(keyColumn) == false
        {
            ret null
        }
        if other == null
        {
            ret null
        }
        if other.containsColumn(keyColumn) == false
        {
            ret null
        }
        Table result = new()
        if SystemTableJoin(this, other, keyColumn, result) == false
        {
            ret null
        }
        ret result
    }

    # ---- 查询 ----
    #查找指定列上值匹配的全部行号（升序数组；列不存在返回空数组；查找在 VM 层完成）
    public Array<int> findRows( string columnName, Object value )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret Array<int>(0)
        }
        int rowCount = this._rows.length
        Array<int> buf = Array<int>(rowCount)
        int n = SystemTableFindRows(this, colIndex, value, buf)
        if n == buf.length
        {
            ret buf
        }
        if n <= 0
        {
            ret Array<int>(0)
        }
        Array<int> result = Array<int>(n)
        for i = 0, i < n, i++
        {
            result._setItem_(i, buf._getItem_(i))
        }
        ret result
    }
    #按列排序（升序；稳定排序在 VM 层完成）
    public void sortBy( string columnName )
    {
        this.sortByAt(this.getColumnIndex(columnName), true)
    }
    public void sortBy( string columnName, bool ascending )
    {
        this.sortByAt(this.getColumnIndex(columnName), ascending)
    }
    #按列下标排序（数值按数值序，字符串按字典序，null 最前）
    public void sortByAt( int colIndex, bool ascending )
    {
        if colIndex < 0 || colIndex >= this._columns.length
        {
            ret
        }
        SystemTableSortRows(this, colIndex, ascending)
    }
    #整行去重（保留首次出现；返回删除行数，比较在 VM 层完成）
    public int distinctRows()
    {
        ret SystemTableDistinctRows(this)
    }

    # ---- 统计（非 null 单元格参与；无法解析为数值的按 0 计）----
    public Float64 min( string columnName )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret 0.0d
        }
        Float64 result = 0.0d
        bool hasValue = false
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            if this.getValue(r, colIndex) != null
            {
                Float64 v = this.getFloat(r, colIndex, 0.0d)
                if hasValue == false
                {
                    result = v
                    hasValue = true
                }
                elif v < result
                {
                    result = v
                }
            }
        }
        ret result
    }
    public Float64 max( string columnName )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret 0.0d
        }
        Float64 result = 0.0d
        bool hasValue = false
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            if this.getValue(r, colIndex) != null
            {
                Float64 v = this.getFloat(r, colIndex, 0.0d)
                if hasValue == false
                {
                    result = v
                    hasValue = true
                }
                elif v > result
                {
                    result = v
                }
            }
        }
        ret result
    }
    public Float64 sum( string columnName )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret 0.0d
        }
        Float64 total = 0.0d
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            if this.getValue(r, colIndex) != null
            {
                total = total + this.getFloat(r, colIndex, 0.0d)
            }
        }
        ret total
    }
    public Float64 avg( string columnName )
    {
        int colIndex = this.getColumnIndex(columnName)
        if colIndex < 0
        {
            ret 0.0d
        }
        Float64 total = 0.0d
        int validCount = 0
        int rowCount = this._rows.length
        for r = 0, r < rowCount, r++
        {
            if this.getValue(r, colIndex) != null
            {
                total = total + this.getFloat(r, colIndex, 0.0d)
                validCount++
            }
        }
        if validCount <= 0
        {
            ret 0.0d
        }
        ret total / validCount.toFloat64()
    }

    # ---- CSV ----
    #解析 CSV 文本（首行为表头）
    public static Table fromCsv( string csvText )
    {
        Table result = new()
        if csvText == null
        {
            ret result
        }
        SystemTableCsvParse(result, csvText, true, ",")
        ret result
    }
    #解析无表头 CSV（列名自动补 col{n}）
    public static Table fromCsvNoHeader( string csvText )
    {
        Table result = new()
        if csvText == null
        {
            ret result
        }
        SystemTableCsvParse(result, csvText, false, ",")
        ret result
    }
    #序列化为 CSV（含转义：分隔符/引号/换行触发引号包裹，"" 转义内嵌引号）
    public string toCsv()
    {
        ret SystemTableCsvToString(this, this._hasHeader, ",")
    }
    public string toCsvDelimited( string delimiter )
    {
        if delimiter == null || delimiter.length == 0
        {
            delimiter = ","
        }
        ret SystemTableCsvToString(this, this._hasHeader, delimiter)
    }

    # ---- 显示 ----
    #markdown 风格预览（默认前 10 行，VM 层完成）
    public string preview( int maxRows )
    {
        ret SystemTableToString(this, maxRows)
    }
    override string toString()
    {
        ret SystemTableToString(this, 10)
    }

    # ---- 迭代器（foreach 直接遍历数据行数组）----
    override void reset()
    {
        this._index = -1
        this._current = null
    }
    override bool moveNext()
    {
        this._index++
        bool hasNextVar = this._index < this._rows.length
        if hasNextVar
        {
            this._current = this._rows._getItem_(this._index)
        }
        else
        {
            this._current = null
        }
        ret hasNextVar
    }
    override get Array<Object> current()
    {
        ret this._current
    }
    override get Core.IIterator<Array<Object>> iterator()
    {
        ret this
    }
}
