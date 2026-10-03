# Embedding —— 词嵌入查表层
#
# table: [vocabSize, dim]，forward 按 id 取行，backward 把梯度 scatter-add 回对应行
# 输入 ids 形状 [N]（一维）或 [N,1]，输出 [N, dim]

@Nickname("Embedding")
public class Embedding extends Layer
{
    Int32 _vocabSize = 0
    Int32 _dim = 0

    public Tensor table = null
    Array<Int32> _lastIds = null

    public void _init_( Int32 vocabSize, Int32 dim )
    {
        this.name = "Embedding"
        this._vocabSize = vocabSize
        this._dim = dim
        this.table = Init.normal( Shape.matrix( vocabSize, dim ), 0.0f, 0.02f )
        this.addParam( this.table )
    }

    get int vocabSize()
    {
        ret this._vocabSize
    }

    get int dim()
    {
        ret this._dim
    }

    override Tensor forward( Tensor ids )
    {
        this.inputCache = ids
        int n = ids.size()
        Array<Int32> idList = Array<Int32>( n )
        for i = 0, i < n, i++
        {
            idList[i] = SystemConvertInt32( ids.at( i ) )
        }
        this._lastIds = idList
        Tensor out = Tensor( Shape.matrix( n, this._dim ) )
        for i = 0, i < n, i++
        {
            int id = idList[i]
            for j = 0, j < this._dim, j++
            {
                out.set( i, j, this.table.get( id, j ) )
            }
        }
        this.outputCache = out
        ret out
    }

    override Tensor backward( Tensor gradOutput )
    {
        Array<Tensor> gs = this.gradients()
        for i = 0, i < this._lastIds.length, i++
        {
            int id = this._lastIds[i]
            for j = 0, j < this._dim, j++
            {
                Float32 v = gs[0].get( id, j )
                gs[0].set( id, j, v + gradOutput.get( i, j ) )
            }
        }
        # 嵌入层一般不向输入回传梯度（输入是离散 id）
        ret gradOutput
    }

    override string summary()
    {
        ret "Embedding(" + SystemConvertString( this._vocabSize ) + "x" + SystemConvertString( this._dim ) + ")"
    }
}
