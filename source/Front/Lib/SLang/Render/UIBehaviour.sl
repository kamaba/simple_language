# UI 行为基类：在 Behaviour 之上补充指针 / 拖拽等交互事件。
public class UIBehaviour extends Behaviour
{
    public RectTransform rectTransform = null

    # 是否接收射线（UI 点击检测）
    public bool raycastTarget = true

    # 是否可交互
    public bool interactable = true

    public void _init_()
    {
        this.raycastTarget = true
        this.interactable = true
        this.rectTransform = null
    }

    # ── 指针事件（子类 override）────────────────────────
    public void onPointerEnter()
    {
    }

    public void onPointerExit()
    {
    }

    public void onPointerDown( Float32_2 position )
    {
    }

    public void onPointerUp( Float32_2 position )
    {
    }

    public void onClick( Float32_2 position )
    {
    }

    # delta 为屏幕空间位移
    public void onDrag( Float32_2 delta )
    {
    }

    public void onScroll( Float32 delta )
    {
    }

    # ── 选中状态 ─────────────────────────────────────────
    public void onSelect()
    {
    }

    public void onDeselect()
    {
    }

    # ── 命中检测 ─────────────────────────────────────────
    # 点是否落在矩形内（position / rect 均为 UI 空间）
    public bool containsPoint( Float32_2 position )
    {
        if this.rectTransform == null
        {
            ret false
        }
        ret this.rectTransform.containsPoint( position )
    }

    public void setInteractable( bool value )
    {
        this.interactable = value
    }

    override string toString()
    {
        ret String.toFormat( "UIBehaviour(raycast={0}, interactable={1})",
            this.raycastTarget.toString(), this.interactable.toString() )
    }
}
