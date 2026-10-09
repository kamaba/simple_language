# 纹理：CPU 侧像素缓冲（RGBA，每通道 Float32，值域 [0,1]）。
# 采样使用 Math 库的 Float32_2 作为 UV。
public class Texture
{
    public Int32 width = 0
    public Int32 height = 0
    public string path = ""

    # 像素数据：RGBA 连续排布，每像素 4 个分量，行优先
    Array<Float32> _pixels = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.width = 0
        this.height = 0
        this.path = ""
        this._pixels = Array<Float32>( 0 )
    }

    public void _init_( Int32 _width, Int32 _height )
    {
        this.width = _width
        this.height = _height
        this.path = ""
        int count = _width * _height * 4
        if count > 0
        {
            this._pixels = Array<Float32>( count )
        }
        else
        {
            this._pixels = Array<Float32>( 0 )
        }
    }

    # ── 属性 ─────────────────────────────────────────────
    public get int pixelCount()
    {
        ret this.width * this.height
    }

    public get bool isValid()
    {
        ret this.width > 0 && this.height > 0 && this._pixels.length > 0
    }

    public get Float32 aspect()
    {
        if this.height <= 0
        {
            ret 0.0f
        }
        ret this.width / this.height
    }

    # ── 像素读写 ─────────────────────────────────────────
    int _offset( Int32 x, Int32 y )
    {
        ret ( y * this.width + x ) * 4
    }

    public void setPixel( Int32 x, Int32 y, Color c )
    {
        if x < 0 || y < 0 || x >= this.width || y >= this.height
        {
            ret
        }
        int o = this._offset( x, y )
        this._pixels[o] = c.r
        this._pixels[o + 1] = c.g
        this._pixels[o + 2] = c.b
        this._pixels[o + 3] = c.a
    }

    public Color getPixel( Int32 x, Int32 y )
    {
        if x < 0 || y < 0 || x >= this.width || y >= this.height
        {
            ret Color.clear()
        }
        int o = this._offset( x, y )
        ret Color( this._pixels[o], this._pixels[o + 1], this._pixels[o + 2], this._pixels[o + 3] )
    }

    # ── 采样 ─────────────────────────────────────────────
    # 最近邻采样（UV 超出 [0,1] 时做重复平铺）
    public Color sample( Float32_2 uv )
    {
        if !this.isValid()
        {
            ret Color.clear()
        }

        Float32 u = uv.x
        Float32 v = uv.y
        u = u - Mathf.floor( u )
        v = v - Mathf.floor( v )

        Int32 x = Mathf.truncate( u * this.width )
        Int32 y = Mathf.truncate( v * this.height )
        if x >= this.width
        {
            x = this.width - 1
        }
        if y >= this.height
        {
            y = this.height - 1
        }
        if x < 0
        {
            x = 0
        }
        if y < 0
        {
            y = 0
        }
        ret this.getPixel( x, y )
    }

    # 双线性采样
    public Color sampleBilinear( Float32_2 uv )
    {
        if !this.isValid()
        {
            ret Color.clear()
        }

        Float32 u = uv.x - Mathf.floor( uv.x )
        Float32 v = uv.y - Mathf.floor( uv.y )
        Float32 fx = u * this.width - 0.5f
        Float32 fy = v * this.height - 0.5f

        Int32 x0 = Mathf.truncate( Mathf.floor( fx ) )
        Int32 y0 = Mathf.truncate( Mathf.floor( fy ) )
        Float32 tx = fx - x0
        Float32 ty = fy - y0

        Color c00 = this.getPixelClamped( x0, y0 )
        Color c10 = this.getPixelClamped( x0 + 1, y0 )
        Color c01 = this.getPixelClamped( x0, y0 + 1 )
        Color c11 = this.getPixelClamped( x0 + 1, y0 + 1 )

        Color top = c00.lerp( c10, tx )
        Color bottom = c01.lerp( c11, tx )
        ret top.lerp( bottom, ty )
    }

    # 带边界钳制的像素读取（用于双线性采样）
    Color getPixelClamped( Int32 x, Int32 y )
    {
        if x < 0
        {
            x = 0
        }
        if y < 0
        {
            y = 0
        }
        if x >= this.width
        {
            x = this.width - 1
        }
        if y >= this.height
        {
            y = this.height - 1
        }
        ret this.getPixel( x, y )
    }

    # ── 操作 ─────────────────────────────────────────────
    public void fill( Color c )
    {
        int i = 0
        int count = this._pixels.length
        while i + 3 < count
        {
            this._pixels[i] = c.r
            this._pixels[i + 1] = c.g
            this._pixels[i + 2] = c.b
            this._pixels[i + 3] = c.a
            i = i + 4
        }
    }

    # 重新分配尺寸（内容丢失）
    public void resize( Int32 w, Int32 h )
    {
        this.width = w
        this.height = h
        int count = w * h * 4
        if count > 0
        {
            this._pixels = Array<Float32>( count )
        }
        else
        {
            this._pixels = Array<Float32>( 0 )
        }
    }

    # 生成棋盘格（调试用）
    public void fillChecker( Int32 cellSize, Color a, Color b )
    {
        if cellSize <= 0
        {
            cellSize = 8
        }
        Int32 y = 0
        while y < this.height
        {
            Int32 x = 0
            while x < this.width
            {
                Int32 cx = x / cellSize
                Int32 cy = y / cellSize
                if ( cx + cy ) % 2 == 0
                {
                    this.setPixel( x, y, a )
                }
                else
                {
                    this.setPixel( x, y, b )
                }
                x = x + 1
            }
            y = y + 1
        }
    }

    # ── 静态工厂 ─────────────────────────────────────────
    public static Texture solid( Int32 width, Int32 height, Color c )
    {
        Texture t = Texture( width, height )
        t.fill( c )
        ret t
    }

    # 默认白色纹理（可作为占位）
    public static Texture white( Int32 size )
    {
        ret Texture.solid( size, size, Color.white() )
    }

    # 从文件加载：依赖外部函数 Render.loadTexture（由渲染后端注册）
    public static Texture load( string filePath )
    {
        object result = SystemCallExternalFunction( "Render.loadTexture", filePath )
        if result is Texture t
        {
            ret t
        }
        ret Texture()
    }

    override string toString()
    {
        ret String.toFormat( "Texture({0}x{1}, path={2})",
            this.width.toString(), this.height.toString(), this.path )
    }
}
