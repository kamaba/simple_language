# 行为组件基类：提供标准生命周期钩子，子类按需 override。
# 继承 Std 库的 Component。
public class Behaviour extends Component
{
    public bool isEnabled = true

    # ── 生命周期（子类 override）────────────────────────
    # 创建后立即调用一次
    public void awake()
    {
    }

    # 第一帧更新前调用一次
    public void start()
    {
    }

    # 每帧更新
    public void update()
    {
    }

    # 每帧在所有 update 之后调用
    public void lateUpdate()
    {
    }

    # 固定步长更新（物理 / 稳定逻辑）
    public void fixedUpdate()
    {
    }

    # 启用 / 禁用时触发
    public void onEnable()
    {
    }

    public void onDisable()
    {
    }

    # 销毁前清理
    public void dispose()
    {
    }

    # ── 启用控制 ─────────────────────────────────────────
    public void setEnabled( bool value )
    {
        if this.isEnabled == value
        {
            ret
        }
        this.isEnabled = value
        if value
        {
            this.onEnable()
        }
        else
        {
            this.onDisable()
        }
    }

    public bool getEnabled()
    {
        ret this.isEnabled
    }

    override string toString()
    {
        ret String.toFormat( "Behaviour(enabled={0})", this.isEnabled.toString() )
    }
}
