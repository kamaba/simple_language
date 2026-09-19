# Flatten —— 展平层
#
# 用途：卷积/池化输出的多维特征 [C,H,W]（或任意形状）拉成一维，接全连接层
# 约定：输出形状固定为 [1, N]，N 为输入元素总数（batch 版本后续扩展为 [B, N]）

@Nickname("Flatten")
public class Flatten extends Layer
{
    public void _init_()
    {
        this.name = "Flatten"
        this.training = true
    }

    override Tensor forward( Tensor x )
    {
        this.inputCache = x
        int n = x.size()
        this.outputCache = x.flatten().reshape( Shape.matrix( 1, n ) )
        ret this.outputCache
    }

    override Tensor backward( Tensor gradOutput )
    {
        ret gradOutput.reshape( this.inputCache.shape() )
    }

    override string summary()
    {
        ret "Flatten()"
    }
}
