@Nickname("Quat")
public class Quaternion
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f
    public Float32 z = 0.0f
    public Float32 w = 1.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0f
        this.y = 0.0f
        this.z = 0.0f
        this.w = 1.0f
    }

    public void _init_( Float32 _x, Float32 _y, Float32 _z, Float32 _w )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        this.w = _w
    }

    # 由轴角构造（axis 需已归一化）
    public void _init_( Float32_3 axis, Float32 radians )
    {
        Float32 half = radians * 0.5f
        Float32 s = Mathf.sin( half )
        this.x = axis.x * s
        this.y = axis.y * s
        this.z = axis.z * s
        this.w = Mathf.cos( half )
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float32 _getItem_( int index )
    {
        if index == 0
        {
            ret this.x
        }
        if index == 1
        {
            ret this.y
        }
        if index == 2
        {
            ret this.z
        }
        ret this.w
    }

    void _setItem_( int index, Float32 value )
    {
        if index == 0
        {
            this.x = value
        }
        elif index == 1
        {
            this.y = value
        }
        elif index == 2
        {
            this.z = value
        }
        else
        {
            this.w = value
        }
    }

    # ── 运算符重载 ───────────────────────────────────────
    # 说明：Quaternion * Quaternion 返回 Quaternion；Quaternion * Float32_3 返回 Float32_3。
    # 运算符重载是动态分派，返回类型统一声明为 Object；
    # 需要静态类型的场景请直接使用 multiply() / rotate()。
    override Object _mul_( Object obj1 )
    {
        if obj1 is Quaternion q
        {
            ret this.multiply( q )
        }
        if obj1 is Float32_3 v
        {
            ret this.rotate( v )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Quaternion q
        {
            ret this.x == q.x && this.y == q.y && this.z == q.z && this.w == q.w
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 四元数运算 ───────────────────────────────────────
    # Hamilton 积：this * q（先应用 q，再应用 this）
    Quaternion multiply( Quaternion q )
    {
        ret Quaternion(
            this.w * q.x + this.x * q.w + this.y * q.z - this.z * q.y,
            this.w * q.y - this.x * q.z + this.y * q.w + this.z * q.x,
            this.w * q.z + this.x * q.y - this.y * q.x + this.z * q.w,
            this.w * q.w - this.x * q.x - this.y * q.y - this.z * q.z )
    }

    # 用四元数旋转向量 v
    Float32_3 rotate( Float32_3 v )
    {
        Float32_3 qv = Float32_3( this.x, this.y, this.z )
        Float32_3 t = qv.cross( v )._mul_( 2.0f ) as Float32_3
        Float32_3 result = v._add_( t._mul_( this.w ) ) as Float32_3
        result = result._add_( qv.cross( t ) ) as Float32_3
        ret result
    }

    Float32 dot( Quaternion q )
    {
        ret this.x * q.x + this.y * q.y + this.z * q.z + this.w * q.w
    }

    Float32 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w
    }

    Float32 length()
    {
        ret Mathf.sqrt( this.lengthSquared() )
    }

    Quaternion normalize()
    {
        Float32 len = this.length()
        if len > 0.0f
        {
            ret Quaternion( this.x / len, this.y / len, this.z / len, this.w / len )
        }
        ret Quaternion.identity()
    }

    Quaternion conjugate()
    {
        ret Quaternion( -this.x, -this.y, -this.z, this.w )
    }

    Quaternion inverse()
    {
        Float32 sq = this.lengthSquared()
        if sq == 0.0f
        {
            ret Quaternion.identity()
        }
        Quaternion c = this.conjugate()
        ret Quaternion( c.x / sq, c.y / sq, c.z / sq, c.w / sq )
    }

    # 线性插值（结果需归一化）
    Quaternion lerp( Quaternion q, Float32 t )
    {
        ret Quaternion( this.x + ( q.x - this.x ) * t,
                        this.y + ( q.y - this.y ) * t,
                        this.z + ( q.z - this.z ) * t,
                        this.w + ( q.w - this.w ) * t ).normalize()
    }

    # 球面插值
    Quaternion slerp( Quaternion q, Float32 t )
    {
        Float32 cosTheta = this.dot( q )
        Quaternion target = q

        # 保证走最短弧
        if cosTheta < 0.0f
        {
            cosTheta = -cosTheta
            target = Quaternion( -q.x, -q.y, -q.z, -q.w )
        }

        if cosTheta > 0.9995f
        {
            ret this.lerp( target, t )
        }

        Float32 theta = Mathf.acos( cosTheta )
        Float32 sinTheta = Mathf.sin( theta )
        Float32 w1 = Mathf.sin( ( 1.0f - t ) * theta ) / sinTheta
        Float32 w2 = Mathf.sin( t * theta ) / sinTheta
        ret Quaternion( this.x * w1 + target.x * w2,
                        this.y * w1 + target.y * w2,
                        this.z * w1 + target.z * w2,
                        this.w * w1 + target.w * w2 ).normalize()
    }

    Quaternion set( Float32 _x, Float32 _y, Float32 _z, Float32 _w )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        this.w = _w
        ret this
    }

    Quaternion setIdentity()
    {
        this.x = 0.0f
        this.y = 0.0f
        this.z = 0.0f
        this.w = 1.0f
        ret this
    }

    Quaternion clone()
    {
        ret Quaternion( this.x, this.y, this.z, this.w )
    }

    # 转为 4x4 旋转矩阵
    Float32_4x4 toFloat32_4x4()
    {
        Float32 xx = this.x * this.x
        Float32 yy = this.y * this.y
        Float32 zz = this.z * this.z
        Float32 xy = this.x * this.y
        Float32 xz = this.x * this.z
        Float32 yz = this.y * this.z
        Float32 wx = this.w * this.x
        Float32 wy = this.w * this.y
        Float32 wz = this.w * this.z

        Float32_4x4 r = Float32_4x4()
        r.set( 0, 0, 1.0f - 2.0f * ( yy + zz ) )
        r.set( 0, 1, 2.0f * ( xy - wz ) )
        r.set( 0, 2, 2.0f * ( xz + wy ) )
        r.set( 1, 0, 2.0f * ( xy + wz ) )
        r.set( 1, 1, 1.0f - 2.0f * ( xx + zz ) )
        r.set( 1, 2, 2.0f * ( yz - wx ) )
        r.set( 2, 0, 2.0f * ( xz - wy ) )
        r.set( 2, 1, 2.0f * ( yz + wx ) )
        r.set( 2, 2, 1.0f - 2.0f * ( xx + yy ) )
        r.set( 3, 3, 1.0f )
        ret r
    }

    # 转为欧拉角（弧度，YXZ 顺序）
    Float32_3 toEuler()
    {
        Float32 sinPitch = 2.0f * ( this.w * this.x - this.y * this.z )
        Float32 pitch = Mathf.asin( Mathf.clamp( sinPitch, -1.0f, 1.0f ) )
        Float32 yaw = Mathf.atan2( 2.0f * ( this.w * this.y + this.x * this.z ),
                                  1.0f - 2.0f * ( this.x * this.x + this.y * this.y ) )
        Float32 roll = Mathf.atan2( 2.0f * ( this.w * this.z + this.x * this.y ),
                                   1.0f - 2.0f * ( this.x * this.x + this.z * this.z ) )
        ret Float32_3( pitch, yaw, roll )
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Quaternion identity()
    {
        ret Quaternion( 0.0f, 0.0f, 0.0f, 1.0f )
    }

    # 绕归一化轴 axis 旋转 radians
    public static Quaternion axisAngle( Float32_3 axis, Float32 radians )
    {
        Float32_3 a = axis.normalize()
        Float32 half = radians * 0.5f
        Float32 s = Mathf.sin( half )
        ret Quaternion( a.x * s, a.y * s, a.z * s, Mathf.cos( half ) )
    }

    # 欧拉角（弧度）转四元数，YXZ 顺序
    public static Quaternion euler( Float32 pitch, Float32 yaw, Float32 roll )
    {
        Float32 cy = Mathf.cos( yaw * 0.5f )
        Float32 sy = Mathf.sin( yaw * 0.5f )
        Float32 cp = Mathf.cos( pitch * 0.5f )
        Float32 sp = Mathf.sin( pitch * 0.5f )
        Float32 cr = Mathf.cos( roll * 0.5f )
        Float32 sr = Mathf.sin( roll * 0.5f )

        ret Quaternion(
            sp * cy * cr + cp * sy * sr,
            cp * sy * cr - sp * cy * sr,
            cp * cy * sr - sp * sy * cr,
            cp * cy * cr + sp * sy * sr )
    }

    public static Quaternion euler( Float32_3 e )
    {
        ret Quaternion.euler( e.x, e.y, e.z )
    }

    # 由 forward / up 构造朝向
    public static Quaternion lookRotation( Float32_3 forward, Float32_3 upHint )
    {
        Float32_3 f = forward.normalize()
        Float32_3 up = upHint
        Float32_3 r = up.cross( f )
        if r.lengthSquared() < 0.000001f
        {
            # forward 与 up 平行，换一个参考轴
            up = Float32_3( 0.0f, 0.0f, 1.0f )
            r = up.cross( f )
        }
        r = r.normalize()
        Float32_3 u = f.cross( r )

        Float32 m00 = r.x
        Float32 m01 = r.y
        Float32 m02 = r.z
        Float32 m10 = u.x
        Float32 m11 = u.y
        Float32 m12 = u.z
        Float32 m20 = f.x
        Float32 m21 = f.y
        Float32 m22 = f.z

        Float32 trace = m00 + m11 + m22
        if trace > 0.0f
        {
            Float32 s = Mathf.sqrt( trace + 1.0f ) * 2.0f
            ret Quaternion( ( m12 - m21 ) / s, ( m20 - m02 ) / s, ( m01 - m10 ) / s, 0.25f * s ).normalize()
        }
        if m00 > m11 && m00 > m22
        {
            Float32 s = Mathf.sqrt( 1.0f + m00 - m11 - m22 ) * 2.0f
            ret Quaternion( 0.25f * s, ( m10 + m01 ) / s, ( m20 + m02 ) / s, ( m12 - m21 ) / s ).normalize()
        }
        if m11 > m22
        {
            Float32 s = Mathf.sqrt( 1.0f + m11 - m00 - m22 ) * 2.0f
            ret Quaternion( ( m10 + m01 ) / s, 0.25f * s, ( m21 + m12 ) / s, ( m20 - m02 ) / s ).normalize()
        }
        Float32 s = Mathf.sqrt( 1.0f + m22 - m00 - m11 ) * 2.0f
        ret Quaternion( ( m20 + m02 ) / s, ( m21 + m12 ) / s, 0.25f * s, ( m01 - m10 ) / s ).normalize()
    }

    # 两个四元数之间的夹角（弧度）
    public static Float32 angle( Quaternion a, Quaternion b )
    {
        Float32 d = Mathf.abs( a.dot( b ) )
        if d > 1.0f
        {
            d = 1.0f
        }
        ret 2.0f * Mathf.acos( d )
    }

    public static Quaternion slerp( Quaternion a, Quaternion b, Float32 t )
    {
        ret a.slerp( b, t )
    }

    override string toString()
    {
        ret String.toFormat( "Quaternion(x={0}, y={1}, z={2}, w={3})",
            this.x, this.y, this.z, this.w )
    }
}
