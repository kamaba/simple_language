# Dropout —— 随机失活
#
# 训练时：以概率 p 置零，其余元素放大 1/(1-p)（inverted dropout，推理无需缩放）
# 推理时：直接透传

@Nickname("Dropout")
public class Dropout extends Layer
{
    Float32 _p = 0.5f
    Tensor _mask = null

    public void _init_( Float32 p )
    {
        this.name = "Dropout"
        this._p = p
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        if !this.training
        {
            this.outputCache = x
            ret x
        }
        Rng rng = Rng.global()
        Tensor mask = Tensor( x.shape() )
        Float32 keep = 1.0f - this._p
        Float32 scale = 1.0f / keep
        for i = 0, i < x.size(), i++
        {
            if rng.nextFloat() < keep
            {
                mask.setAt( i, scale )
            }
            else
            {
                mask.setAt( i, 0.0f )
            }
        }
        this._mask = mask
        this.outputCache = x.mul( mask )
        ret this.outputCache
    }

    override Tensor backward( Tensor gradOutput )
    {
        if this._mask == null
        {
            ret gradOutput
        }
        ret gradOutput.mul( this._mask )
    }

    override string summary()
    {
        ret "Dropout(p=" + SystemConvertString( this._p ) + ")"
    }
}
