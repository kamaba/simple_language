# 极简语言 [English](https://github.com/kamaba/simple_language)

------------------------------------------------------------------------

### 简介: 极简语言是一门静态语言，它初版是在csharp的基础上写出来的，语法大体与C#有些相似，但又有其它语言的特点，他的工程配置与语言是一体的，所以在使用语言的时候，必须是在工程的基础上。语言分三期功能
- 第一个阶段: 
    1. 语言的前端解析
    2. 语言通过c#的平台的集成库
    3. 拥有自己完整的语言体系
    4. 但暂不支持模版操作
    5. 可以导出IR中间语言，并且在自己内部的虚拟机中运行。
- 第二个阶段：
    1. 语言可以导出c#的IR层，使用Mono或者是.NetCore虚拟机运行导出代码
    2. 并且在内部可以直接调用c#的库或者是c/c++的库，并且快整模块化。
    3. 可以导出javascript等语言，兼容javascript的一些库的执行。
- 第三个阶段: 
    1. 使用llvm中间层，
    2. 未来.netcore 的native功能
    3. 把语言本地化，脱离虚拟机运行，然后使用llvm转化，可以正常语言一些，直接打包，链接，运行。


### 语言的特色
1. 写法较为简单，无强制格式化行为，更多使用大括号来代表代码段。
2. 注释支持多层嵌套，并且支持markdown注释



### 语言的宗旨
1. 可读性较强
2. 可写性较强
3. 轻度的语法糖，一定要建立在1，2的基础之上。
4. 纯面向对象的语言。
5. 轻度使用继承，接口，不允许有重名变量.

### 语言初体验
```csharp
file:test.sp

import CSharp.System;

DemoClass
{
    a = 0i;
    b = 100i;

    __Init__( int _a, int _b )
    {
        this.a = _a;
        this.b = _b * 2;
    }

    Add()
    {
        return this.a + this.b;
    }
    PrintAddRes()
    {
        # 如果a=10 b=100的话 输入 a[10]+b[200]=210
        Console.Write( "a[@this.a]+b[@this.b]=" + Add().ToString() );
    }
}

ProjectEnter
{
    static Main()
    {    
        DC = DemoClass(10, 100);
        DC.PrintAddRes();
    }
    static Test()
    {
    }
}
```

### 语法说明
1. [命名空间](https://github.com/kamaba/simple_language/tree/main/md/namespace.md)
2. [基本语法](https://github.com/kamaba/simple_language/tree/main/md/base.md)
3. [类型转换](https://github.com/kamaba/simple_language/tree/main/md/string.md)
4. [变量](https://github.com/kamaba/simple_language/tree/main/md/string.md)
5. [运算符](https://github.com/kamaba/simple_language/tree/main/md/string.md)
6. [判断](https://github.com/kamaba/simple_language/tree/main/md/string.md)
7. [循环](https://github.com/kamaba/simple_language/tree/main/md/string.md)
8. [封装](https://github.com/kamaba/simple_language/tree/main/md/string.md)
9. [方法](https://github.com/kamaba/simple_language/tree/main/md/string.md)
10. [字符串](https://github.com/kamaba/simple_language/tree/main/md/string.md)
11. [枚举](https://github.com/kamaba/simple_language/tree/main/md/enum.md)
12. [类](https://github.com/kamaba/simple_language/tree/main/md/class.md)
13. [对象](https://github.com/kamaba/simple_language/tree/main/md//object.md)
14. [继承](https://github.com/kamaba/simple_language/tree/main/md/string.md)
15. [接口](https://github.com/kamaba/simple_language/tree/main/md/string.md)

### 支持平台

### 安装使用




Animations
{
    DataConainer #数据列表
    [
        char   #角色配置
        {
            Settings   #角色通用配置
            {
                PreviewActorClass = 未知;
                JoinRule = 未知;
                IKTargetDefinitions =  #可以理解为上车的位置,一般根据车门决定 
                [
                    {
                        index = 0;
                        0:IKGoalName = 关键帧名称"上车位置对象名称"
                        1:BoneName = "交互时角色骨骼节点名称";
                        2:AlphaCurveName = 较正曲线名称，可以为空
                        3:Provider = 自动/配固定骨骼
                        AutoParams = 自动情况的传入参数
                        0:TargetRole = car
                        1:BoneName = "自动时的骨骼名称"
                            BoneParams = 使用骨骼时，传入参数
                        TargetRole = ?
                        BoneName = "骨骼名称"
                    }
                ]
            }
            AnimDataContainer =   #角色动画相关配置
            [
                {
                    animation = 角色进入车，从拉门，到坐下的动画
                    animMaxStartTime = 等待最多的开始时间
                    requireFlyingMode 是否飞行模式，不落地
                    Metadata   #元数据
                    {
                        Defaults    #元数据动画内容管理工具的默认值。
                        {
                            
                        }
                    }
                    MeshToScene   #暂未发现作用
                    {

                    }
                }
            ]
        },
        car    #车的相关配置
        {
            Settings{暂无使用}
            AnimDataContainer
            [
                Animation = "进入车的，车的开门动画”
                AnimMaxStart = "等待最长开始播放时间"
                
            ]
        }
    ]
}
设置
{
    DisableCollisionBetweenActors = 取消在此过程中，碰撞
    Redius = 未知
}
对齐
{
    AlignmentSections  #对齐时间轴对齐 
    [
        {
            SectionName = "动画片断名称"
            ScenePivots =   #场景中的两个节点"
            [

            ]
        }
    ]
}
#辅助说明
1. ContextualAnimManager 是用来设置 复合动画交互的插件，通过配置动画，和通知系统，来操作整个动画过度过程
2. 动画下的数组，一般表示，交互的单位，都需要一定的配置。
3. 如果遇到配置UContextualAnimSceneInstance这个类，是个场景中的实体对象。
4. 

#通知的意义
1. AniNotify_MotionWarp_C OnWarpUpdate 车可能会移动，而我们仍然进入它，所以我们需要更新翘曲目标，以确保我们不会结束在错误的位置，基本上的作用，就是，角色与车车辆的位置同步
2. AnimNotify_MotionWarp_C OnWarpEnd 车辆与角色从角色绑定关系，不再同步他们的位置 。
3. CitySampleSimpleWarp CarExit 完全退出车辆通知
4. DoorOpen 开车辆门通知, 可以 一般配合蒙太奇一起使用，然后通知在蓝图中使用，主要用来控制在开门后下一步的逻辑执行,因为是逻辑执行，所以可以配置蒙太奇和其它参数一起使用。
4. DoorClose 关车辆门的通知, 可以 一般配合蒙太奇一起使用，然后通知在蓝图中使用，主要用来控制在开门后下一步的逻辑执行。
5. SetLeftFootLocationCarInteraction 在服务器执行  角色从左边位下载，并且发起通知。暂没发现该功能的具体使用。
6. Unlock 车辆解锁操作后执行下步动画相关。
7. Possess通知，是用来告诉服务器，同步执行角色控制权归车辆使用。
8. Unpossess 通知，用来告诉服务器，同步执行角色控制权归还角色自已。
9. Interact 发起进车操作通知，
10. DriverSeat角色坐到坐位上时的通知。

#动画曲线
1. PelvisAlpha  身体中心位置较正，与IK动画配合使用，需要具体看是那个动画片段下的那个，配合使用后，当参数传入系统中，然后拿到骨骼与参数，和动画片段时进行数值较正，例1 上车时身体重心的较正。
2. CarPassengerDoorRightHand    乘客右手，相对位置判断，通过右手全局位置，进行下边的逻辑执行。
3. CarPassengerDoorLeftHand 同上，当左手开门的时候，
4. SteeringWheelRightHandIKAlpha  与方向盘位置的IK较正，如果值为正，则与上边配置中关联部件的位置进行较正，然后调正手的位置。
5. SteeringWheelLeftHandIKAlpha 同上，左手握方向盘时，的IK较正。
5. CarDriverDoorRightHand  车驾使员与右手的较正，一般在在执行，出去开门动画时，通过 > 0值，获取曲线表，然后通过执行该骨骼节点的的位置较正
6. CarDriverDoorLeftHand  同上，驾驶通过左手的较正。
7. DisableFootPlacement  通过输入姿势后，拿到本地空间与车内空间的内边，进行脚部的较正。

动画曲线，一般配合，当前播的动画，还有动画中的时间轴取出相应的值，也会通过变量更新函数，然后配合UpdateDrivingVariables的逻辑执行，进行当前帧的逻辑判断，然后确定使用那个段的动画曲线，然后通过0-1的取值，进行是否使用该曲线的执行后续，如果执行了，则通过当前曲线的值，与帧中的骨骼数据，和目标数据，进行相乘，最后得到真实数据。

1. Vehicle_HeroCar_Enter_C_Montage
2. Vehicle_HeroCar_Enter_C_Passenger_Montage
3. VehTruck_Vehicle04_Enter_C_Montage
4. VehTruck_Vehicle04_Enter_C_Passenger_Montage


1. 车辆上车的过程
    1. 车辆通过controller类，是否有载具
    2. 如果有，则弹UI
    3. 确定后，开始执行上车流程
        1. 确定人物的状态，并且执行与车控制权的锁定
        2. 播放开车动画，并且有多个事件通知，
            1. 人物动画
            2. IK融合
            3. 车门动画
            4. 开始进入车辆
        3. 

1. 需要合并的代码
1. BP_CitySampleGameInstance_C, CitySampleGameInstance 2
2. BP_CitySampleGameGameMode_C, CitySampleGameMode 1
3. BP_CitySampleGameState, CitySampleGameState  1
4. BP_CitySampleWorldInfo,CitySampleWorldInfo, BP_CitySampleCheatManager   
5. BP_CitySampleCameraManager, ACitySamplePlayerCameraManager, APlayerCameraManager, UCitySampleCam_ThirdPerson, UCitySampleCameraMode, CitySampleCamera_Drone, UCitySampleCameraMode. PhotoModeComponent，CitySampleCamera_PhotoMode  BP_CitySampleHoverDrone
    - 角色的摄像机管理，包括，是否需要变成咱们自己的，自己的与车的过渡与管理， 
    - 是否都要继承与他的CameraMode，
    - 逻辑中,因为他的CameraMode，还是需要自己再建一个，CameraMode管理的东西。
6. 动画系统，是否需要迁移，Sample的动画管理类，还是需要合并，讨论。
    1. UCitySampleAnimInstance_Accessory : AnimInstance
    2. UCitySampleAnimInstance_Crowd : public UMassCrowdAnimInstance 
    3. UCitySampleAnimInstance_Crowd_Head : AnimInstance 
    4. FCitySampleAnimNode_CopyPoseRotations : public FAnimNode_Base
    5. FCitySampleAnimNode_CrowdSlopeWarping : public FAnimNode_SkeletalControlBase
    6. FCitySampleAnimNode_RequestInertialization : public FAnimNode_Base
    7. UCitySampleAnimNotifyState_PlayMontageOnFace : public UAnimNotifyState
    8. UCitySampleAnimSet_Accessory : public UPrimaryDataAsset
    9. UCitySampleAnimSet_Locomotion : public UPrimaryDataAsset
    10. URootMotionModifier_CitySampleSimpleWarp : public URootMotionModifier_Warp
    - 问题1: 是否只有必须使用他们的动画才能在进车动画中，正常使用。
    - 问题2: 现在阶段是否要加入Crowd机制
    - 问题3: 如果加入他的，是否与咱们自己的动画机制有冲突。
ACitySamplePlayerCameraManager, APlayerCameraManager
7. CitySampleCharacter， BP_CitySamplePlayerCharacter, CitySampleCharacterMovementComponent, CitySamplePlayerController,BP_CitySamplePC 角色需要修改的节点较多
    1. 动画
    2. 变量
    3. Compoment
    4. 动画蓝图
8. 需要修改的车辆相关 CitySampleDrivingState,CitySampleMassVehicleBase,CitySampleVehicleBase,UDrivableVehicleComponent, BP_VehicleBase, BP_VehicleBase_Deformable,BP_VehicleBase_Destruction, BP_VehicleBase_Drivable,BP_VehicleBase_Sandbox, BP_CarEntryInteraction,BP_CarExitInteraction BP_vehCar_vehicle[n]_Sandbox(车辆具体的类) 车辆修改的节点较多
    1. 蓝部
    2. 交互动画
    3. 执行逻辑
9. UI类，确定是否使用他的那套系统，还是融合成咱们自己的系统，在UI中，有控制器，和UI打开关闭管理，是否走自己的。 UI有，菜单，弹出控制容器，驾车界面，原来的UI管理器和逻辑调用。

10. 其它问题
    1. 角色是否坐车后，状态机还有一些机能消失，要不可能会有与世界的互动或者是其它功能出现。
    2. 角色上下车的过程在大世界中的过渡，什么节点要控制他的开放。

摄像机类的整合 1.19-1.20
角色类 7
    动画类 2.1-2.2
    A类的整合 1.16 - 1.18
    Controller类的整合 1.13-1.14
    与车的交互流程 2.3
车辆类 4
    车辆的蓝图调整 1.29-1.31
    车辆的交互及动画调整 2.4
UI类  1.28

MassCrowd 相关的功能支持 2.5
GameInstance,Mode,State和其它管理管理类 2.6
移完后的调整 2.7-2.9