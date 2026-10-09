public class Exclude extends Attribute
{
    # 编译前（PreCompile）生效: 识别到符号即跳过该段代码编译（成员整体不进 module.json）
    # 收编旧 @IF/@ELSE/@ENDIF 宏与 CompileBeforeManager static if
    # 条件参数（平台/DEBUG 等常量）由 Front 处理器求值, v1 先落无条件形态
    # 用法: @Exclude() 标注在 class/data/enum/成员变量/成员函数上

    _init_()
    {
        this._attributeStage = EAttributeStage.PreCompile
        this._attributeTargets = EAttributeTarget.All
    }
}
