
public class FrameData
{
	static DataFrame create()
    {

    }
    int    frameCount()
    {
        return 0;
    }
    void   record( string label, data snapshot ){}   # 追加一帧（已克隆的 data）
    void   recordMany( string label, Map<string,data> named ){}
    data   getFrame( int idx ){}                       # 取第 idx 帧的真实 data 副本
    string getLabel( int idx ){}
    Int64  getTimestamp( int idx ){}
    int    getDepth( int idx ){}                       # 该帧压栈深度
    # 对比（仅 data）
    string diff( int i, int j ){}                      # 第 i/j 帧字段级 diff 文本
    string diffPrev( int i ){}                        # diff(i-1, i)
    List<string> changedFields( int i, int j ){}       # 变化字段名
    bool   equals( int i, int j ){}                    # 用 DataAllEqual
    void   exportToFile( string path ){}
}

public class Monitor
{
    static Monitor watch( data d, string name )
    void watch( data d, string name )
    void setSampleEvery( int n )                     # 每 n 次 record 采一次
    void setCondition( bool cond )                   # 条件为真才采
    void setBreakOnChange( bool on )                 # 值变化即 Debug.Break()
    void record( string label )                      # 对所有 watch：SystemDataClone 后入 DataFrame
    DataFrame getFrameData()
    void clear()
}

public class Trace
{
    static Trace  create( string name )
    void setFile( string path )                      # 复用 Log 落盘
    void log( string msg, int level )                # + 当前压栈信息
    void pushScope( string scope ) / popScope( string scope )
    Array<string> stackSnapshot()
    void export( string path )
}