# 网格：顶点缓冲 + 索引缓冲 + 包围盒。
# 使用 Math 库的 Float32_3 / Float32_2 与 Render 的 Color / Bounds。
public class Mesh
{
    Array<Float32_3> _vertices = null
    Array<Float32_3> _normals = null
    Array<Float32_2> _uvs = null
    Array<Color> _colors = null
    Array<Int32> _triangles = null

    Bounds _bounds = null
    bool _boundsDirty = true

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._vertices = Array<Float32_3>( 0 )
        this._normals = Array<Float32_3>( 0 )
        this._uvs = Array<Float32_2>( 0 )
        this._colors = Array<Color>( 0 )
        this._triangles = Array<Int32>( 0 )
        this._bounds = Bounds()
        this._boundsDirty = true
    }

    public void _init_( Array<Float32_3> vertices, Array<Int32> triangles )
    {
        this._init_()
        this.setVertices( vertices )
        this.setTriangles( triangles )
    }

    # ── 数量 ─────────────────────────────────────────────
    public get int vertexCount()
    {
        ret this._vertices.length
    }

    public get int triangleCount()
    {
        ret this._triangles.length / 3
    }

    public get Bounds bounds()
    {
        if this._boundsDirty
        {
            this.recalculateBounds()
        }
        ret this._bounds.clone()
    }

    # ── 读写 ─────────────────────────────────────────────
    public void setVertices( Array<Float32_3> vertices )
    {
        this._vertices = vertices
        this._boundsDirty = true
    }

    public Array<Float32_3> getVertices()
    {
        ret this._vertices
    }

    public Float32_3 getVertex( int index )
    {
        ret this._vertices[index]
    }

    public void setNormals( Array<Float32_3> normals )
    {
        this._normals = normals
    }

    public Array<Float32_3> getNormals()
    {
        ret this._normals
    }

    public void setUVs( Array<Float32_2> uvs )
    {
        this._uvs = uvs
    }

    public Array<Float32_2> getUVs()
    {
        ret this._uvs
    }

    public void setColors( Array<Color> colors )
    {
        this._colors = colors
    }

    public Array<Color> getColors()
    {
        ret this._colors
    }

    public void setTriangles( Array<Int32> triangles )
    {
        this._triangles = triangles
        this._boundsDirty = true
    }

    public Array<Int32> getTriangles()
    {
        ret this._triangles
    }

    # ── 计算 ─────────────────────────────────────────────
    # 由三角面累加法线（面积加权）
    public void recalculateNormals()
    {
        int vcount = this._vertices.length
        if vcount == 0
        {
            ret
        }

        Array<Float32_3> normals = Array<Float32_3>( vcount )
        int i = 0
        while i < vcount
        {
            normals[i] = Float32_3.zero()
            i++
        }

        int t = 0
        while t + 2 < this._triangles.length
        {
            Int32 i0 = this._triangles[t]
            Int32 i1 = this._triangles[t + 1]
            Int32 i2 = this._triangles[t + 2]
            Float32_3 a = this._vertices[i0]
            Float32_3 b = this._vertices[i1]
            Float32_3 c = this._vertices[i2]
            Float32_3 ab = b._sub_( a ) as Float32_3
            Float32_3 ac = c._sub_( a ) as Float32_3
            Float32_3 faceNormal = ab.cross( ac )

            normals[i0] = normals[i0]._add_( faceNormal ) as Float32_3
            normals[i1] = normals[i1]._add_( faceNormal ) as Float32_3
            normals[i2] = normals[i2]._add_( faceNormal ) as Float32_3
            t = t + 3
        }

        i = 0
        while i < vcount
        {
            Float32 len = normals[i].length()
            if len > 0.0f
            {
                normals[i] = normals[i].normalize()
            }
            else
            {
                normals[i] = Float32_3.up()
            }
            i++
        }

        this._normals = normals
    }

    public void recalculateBounds()
    {
        int vcount = this._vertices.length
        if vcount == 0
        {
            this._bounds = Bounds()
            this._boundsDirty = false
            ret
        }

        Float32_3 lo = this._vertices[0].clone()
        Float32_3 hi = this._vertices[0].clone()
        int i = 1
        while i < vcount
        {
            lo = Vector.min3( lo, this._vertices[i] )
            hi = Vector.max3( hi, this._vertices[i] )
            i++
        }
        this._bounds = Bounds( lo, hi )
        this._boundsDirty = false
    }

    # 对所有顶点应用矩阵变换（含法线）
    public void transform( Float32_4x4 m )
    {
        int i = 0
        while i < this._vertices.length
        {
            this._vertices[i] = m.transformPoint( this._vertices[i] )
            i++
        }
        if this._normals != null && this._normals.length == this._vertices.length
        {
            i = 0
            while i < this._normals.length
            {
                this._normals[i] = m.transformDirection( this._normals[i] ).normalize()
                i++
            }
        }
        this.recalculateBounds()
    }

    public void clear()
    {
        this._vertices = Array<Float32_3>( 0 )
        this._normals = Array<Float32_3>( 0 )
        this._uvs = Array<Float32_2>( 0 )
        this._colors = Array<Color>( 0 )
        this._triangles = Array<Int32>( 0 )
        this._bounds = Bounds()
        this._boundsDirty = true
    }

    # ── 静态工厂 ─────────────────────────────────────────
    public static Mesh fromVerticesAndTriangles( Array<Float32_3> vertices, Array<Int32> triangles )
    {
        Mesh m = Mesh( vertices, triangles )
        m.recalculateNormals()
        m.recalculateBounds()
        ret m
    }

    # XZ 平面上的四边形（法线 +Y），中心在原点
    public static Mesh quad( Float32 width, Float32 height )
    {
        Float32 hw = width * 0.5f
        Float32 hh = height * 0.5f

        Array<Float32_3> verts = Array<Float32_3>( 4 )
        verts[0] = Float32_3( -hw, 0.0f, -hh )
        verts[1] = Float32_3( hw, 0.0f, -hh )
        verts[2] = Float32_3( hw, 0.0f, hh )
        verts[3] = Float32_3( -hw, 0.0f, hh )

        Array<Float32_2> uvs = Array<Float32_2>( 4 )
        uvs[0] = Float32_2( 0.0f, 0.0f )
        uvs[1] = Float32_2( 1.0f, 0.0f )
        uvs[2] = Float32_2( 1.0f, 1.0f )
        uvs[3] = Float32_2( 0.0f, 1.0f )

        Array<Int32> tris = Array<Int32>( 6 )
        tris[0] = 0
        tris[1] = 3
        tris[2] = 1
        tris[3] = 1
        tris[4] = 3
        tris[5] = 2

        Mesh m = Mesh()
        m.setVertices( verts )
        m.setUVs( uvs )
        m.setTriangles( tris )
        m.recalculateNormals()
        m.recalculateBounds()
        ret m
    }

    # 立方体（每面独立顶点，便于硬边法线）
    public static Mesh cube( Float32 size )
    {
        Float32 h = size * 0.5f
        Array<Float32_3> verts = Array<Float32_3>( 24 )
        Array<Float32_3> norms = Array<Float32_3>( 24 )
        Array<Int32> tris = Array<Int32>( 36 )

        # 6 个面：+X, -X, +Y, -Y, +Z, -Z
        int v = 0
        int t = 0
        int face = 0
        while face < 6
        {
            Float32_3 n = Float32_3.zero()
            Float32_3 u = Float32_3.zero()
            Float32_3 w = Float32_3.zero()
            if face == 0
            {
                n = Float32_3( 1.0f, 0.0f, 0.0f )
                u = Float32_3( 0.0f, 0.0f, -1.0f )
                w = Float32_3( 0.0f, 1.0f, 0.0f )
            }
            elif face == 1
            {
                n = Float32_3( -1.0f, 0.0f, 0.0f )
                u = Float32_3( 0.0f, 0.0f, 1.0f )
                w = Float32_3( 0.0f, 1.0f, 0.0f )
            }
            elif face == 2
            {
                n = Float32_3( 0.0f, 1.0f, 0.0f )
                u = Float32_3( 1.0f, 0.0f, 0.0f )
                w = Float32_3( 0.0f, 0.0f, 1.0f )
            }
            elif face == 3
            {
                n = Float32_3( 0.0f, -1.0f, 0.0f )
                u = Float32_3( 1.0f, 0.0f, 0.0f )
                w = Float32_3( 0.0f, 0.0f, -1.0f )
            }
            elif face == 4
            {
                n = Float32_3( 0.0f, 0.0f, 1.0f )
                u = Float32_3( 1.0f, 0.0f, 0.0f )
                w = Float32_3( 0.0f, 1.0f, 0.0f )
            }
            else
            {
                n = Float32_3( 0.0f, 0.0f, -1.0f )
                u = Float32_3( -1.0f, 0.0f, 0.0f )
                w = Float32_3( 0.0f, 1.0f, 0.0f )
            }

            Float32_3 center = n.scale( h )
            verts[v] = center._add_( u.scale( -h )._add_( w.scale( -h ) ) ) as Float32_3
            verts[v + 1] = center._add_( u.scale( h )._add_( w.scale( -h ) ) ) as Float32_3
            verts[v + 2] = center._add_( u.scale( h )._add_( w.scale( h ) ) ) as Float32_3
            verts[v + 3] = center._add_( u.scale( -h )._add_( w.scale( h ) ) ) as Float32_3

            norms[v] = n
            norms[v + 1] = n
            norms[v + 2] = n
            norms[v + 3] = n

            tris[t] = v
            tris[t + 1] = v + 1
            tris[t + 2] = v + 2
            tris[t + 3] = v
            tris[t + 4] = v + 2
            tris[t + 5] = v + 3

            v = v + 4
            t = t + 6
            face = face + 1
        }

        Mesh m = Mesh()
        m.setVertices( verts )
        m.setNormals( norms )
        m.setTriangles( tris )
        m.recalculateBounds()
        ret m
    }

    override string toString()
    {
        ret String.toFormat( "Mesh(verts={0}, tris={1})",
            this._vertices.length.toString(), this._triangles.length.toString() )
    }
}
