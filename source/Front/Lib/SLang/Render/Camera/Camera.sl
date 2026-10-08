# 相机组件：视场角 / 近远裁剪面 / 宽高比 + 视图 / 投影矩阵。
# 数学类型使用 Math 库的 Float32 系列：位置 -> Float32_3，矩阵 -> Float32_4x4。
public class Camera extends Component
{
    # 透视 / 正交
    public bool isOrthographic = false

    # 视场角（角度制）
    public Float32 fieldOfView = 60.0f
    public Float32 orthographicSize = 5.0f

    public Float32 nearClipPlane = 0.1f
    public Float32 farClipPlane = 1000.0f
    public Float32 aspect = 1.0f

    # 背景清屏色
    public Color background = Color.black()

    # 深度排序（值小先渲染）
    public Int32 depth = 0

    # ── 视图矩阵 ─────────────────────────────────────────
    # Transform 提供世界变换；若未挂 Transform 则用本地 identity
    public get Float32_4x4 worldToCameraMatrix()
    {
        Transform t = this.getTransform()
        if t == null
        {
            ret Float32_4x4.identity()
        }
        Float32_4x4 world = t.localToWorldMatrix()
        ret MatrixUtil.inverseRigid( world )
    }

    # 相机本地 -> 世界（供世界坐标推算用）
    public get Float32_4x4 cameraToWorldMatrix()
    {
        Transform t = this.getTransform()
        if t == null
        {
            ret Float32_4x4.identity()
        }
        ret t.localToWorldMatrix()
    }

    # ── 投影矩阵 ─────────────────────────────────────────
    public get Float32_4x4 projectionMatrix()
    {
        if this.isOrthographic
        {
            Float32 halfH = this.orthographicSize
            Float32 halfW = halfH * this.aspect
            ret Float32_4x4.ortho( 0.0f - halfW, halfW, 0.0f - halfH, halfH,
                this.nearClipPlane, this.farClipPlane )
        }
        ret Float32_4x4.perspective( Mathf.radians( this.fieldOfView ),
            this.aspect, this.nearClipPlane, this.farClipPlane )
    }

    # 视图 * 投影（提交绘制时直接传给 Renderer）
    public get Float32_4x4 viewProjectionMatrix()
    {
        ret this.projectionMatrix().multiply( this.worldToCameraMatrix() )
    }

    # ── 屏幕空间 ─────────────────────────────────────────
    # 世界坐标 -> 归一化设备坐标（-1..1）；超出裁剪体返回 null
    public Float32_3 worldToViewportPoint( Float32_3 worldPoint )
    {
        Float32_4x4 vp = this.viewProjectionMatrix()
        Float32_4 clip = vp.transformPoint( Float32_4( worldPoint.x, worldPoint.y, worldPoint.z, 1.0f ) )
        if clip.w <= 0.0f
        {
            ret null
        }
        ret Float32_3( clip.x / clip.w, clip.y / clip.w, clip.z / clip.w )
    }

    # 由屏幕像素坐标反投影到世界射线（屏幕原点左上）
    public Ray screenPointToRay( Float32 x, Float32 y, Int32 screenWidth, Int32 screenHeight )
    {
        Float32 ndcX = ( x / screenWidth ) * 2.0f - 1.0f
        Float32 ndcY = 1.0f - ( y / screenHeight ) * 2.0f

        Transform t = this.getTransform()
        Float32_3 origin = Float32_3.zero()
        Float32_3 forwardDir = Float32_3.forward()
        if t != null
        {
            origin = t.worldPosition()
            forwardDir = t.forward()
        }

        Float32_3 target = origin._add_( forwardDir ) as Float32_3
        Float32_3 right = this.up().cross( forwardDir ).normalize()
        Float32_3 up = forwardDir.cross( right ).normalize()

        # 由视场角推出近平面上射线方向
        Float32 tanFov = Mathf.tan( Mathf.radians( this.fieldOfView ) * 0.5f )
        Float32_3 dir = forwardDir
            ._add_( right.scale( ndcX * tanFov * this.aspect ) )
            ._add_( up.scale( ndcY * tanFov ) ) as Float32_3
        ret Ray( origin, dir.normalize() )
    }

    # ── 辅助 ─────────────────────────────────────────────
    public Float32_3 up()
    {
        Transform t = this.getTransform()
        if t == null
        {
            ret Float32_3.up()
        }
        ret t.up()
    }

    public Float32_3 position()
    {
        Transform t = this.getTransform()
        if t == null
        {
            ret Float32_3.zero()
        }
        ret t.worldPosition()
    }

    Transform getTransform()
    {
        Component c = this.GetComponent<Transform>()
        if c is Transform t
        {
            ret t
        }
        ret null
    }

    # 裁剪参数修正
    public void clampClipping()
    {
        if this.nearClipPlane <= 0.0f
        {
            this.nearClipPlane = 0.01f
        }
        if this.farClipPlane <= this.nearClipPlane
        {
            this.farClipPlane = this.nearClipPlane + 1.0f
        }
    }

    override string toString()
    {
        ret String.toFormat( "Camera(fov={0}, aspect={1}, ortho={2})",
            this.fieldOfView.toString(), this.aspect.toString(), this.isOrthographic.toString() )
    }
}
