# Scripts 目录说明

本文档用于说明 `Assets/Scripts/` 下的**代码结构**与**已实现功能模块**（不包含论文内容）。

## 目录结构（按当前项目实际文件）

```
Assets/Scripts
|-- Cars
|   |-- CarController.cs
|   |-- RotatingPreview.cs
|   `-- TrafficCar
|       |-- TrafficCar.cs
|       |-- TrafficCar.Behavior.cs
|       |-- TrafficCar.Decision.cs
|       |-- TrafficCar.Perception.cs
|       |-- TrafficCar.StateMachine.cs
|       |-- TrafficCarPool.cs
|       |-- TrafficCarRecycler.cs
|       `-- TrafficCarSpawner.cs
|-- SceneManagers
|   |-- GameSceneInitializer.cs
|   |-- LaneSystem.cs
|   |-- SideSceneManager.cs
|   `-- StreetManager.cs
|-- Tools
|   |-- CameraAspectRatioController.cs
|   |-- IPoolable.cs
|   |-- MobileInputManager.cs
|   |-- ObjectPool.cs
|   `-- TouchButton.cs
`-- UI
    |-- GameSettings.cs
    |-- MapIconButton.cs
    |-- MapInfo.cs
    |-- MenuButtonBinder.cs
    |-- MenuFlowManager.cs
    |-- SpeedDisplayManager.cs
    |-- VehicleIconButton.cs
    `-- VehicleInfo.cs
```

说明：
- Unity 会为脚本与目录生成对应的 `.meta` 文件，本结构中未展开展示。

## 功能模块说明

### 1) 玩家车辆（`Cars/`）

- `Cars/CarController.cs`
  - 玩家车辆核心控制：读取输入（键盘 `Vertical/Horizontal` + 空格刹车，或 `MobileInputManager` 移动端输入）。
  - 车辆物理：基于 `WheelCollider` 的四轮驱动、转向与刹车；包含最高车速 `maxSpeed`、最低速度 `minSpeed`（用于开局自动起步/防止速度过低）等参数。
  - 表现/UI：同步轮子模型旋转；可选显示速度 `Text`；刹车时通过 `MaterialPropertyBlock` 控制尾灯自发光。

- `Cars/RotatingPreview.cs`
  - 预览模型旋转：用于开始界面/选择界面展示车辆外观。

### 2) 交通 AI 车辆（`Cars/TrafficCar/`）

该部分将 AI 车拆分为多个 `partial class` 文件，便于按“感知-决策-行为”组织逻辑，并配套对象池复用。

- `Cars/TrafficCar/TrafficCar.cs`
  - AI 车主体：运行周期、参数配置（速度、检测距离、变道参数、车轮/尾灯引用等）与对象池接口实现（`IPoolable`）。

- `Cars/TrafficCar/TrafficCar.Perception.cs`
  - 感知层：通过 `Physics.BoxCast` 检测前方车辆距离；通过 `Physics.OverlapBox` 检查目标车道是否安全，生成感知结果（是否堵塞、是否可变道等）。

- `Cars/TrafficCar/TrafficCar.Decision.cs`
  - 决策层：根据前车距离、当前状态、变道冷却/概率等，选择巡航/跟车/变道/紧急刹车等目标状态，并构建统一命令。

- `Cars/TrafficCar/TrafficCar.StateMachine.cs`
  - 状态机数据定义：状态枚举、纵向/横向模式、感知结构体与命令结构体等。

- `Cars/TrafficCar/TrafficCar.Behavior.cs`
  - 行为层：执行命令（速度变化、紧急刹车尾灯、按道路方向设置速度向量）；变道时按目标车道 `Z` 进行横向平滑移动。

- `Cars/TrafficCar/TrafficCarPool.cs`
  - 交通车对象池管理：初始化 `ObjectPool<TrafficCar>`，定时生成交通车，并与回收器协作进行超范围回收。

- `Cars/TrafficCar/TrafficCarSpawner.cs`
  - 生成器：计算生成位置（通常在玩家前方），设置车道与速度波动；并在生成点做占用检测（`Physics.CheckSphere`）避免重叠。

- `Cars/TrafficCar/TrafficCarRecycler.cs`
  - 回收器：协程定时检查交通车相对玩家的位置，超过前/后阈值时回收至对象池。

### 3) 场景与道路循环（`SceneManagers/`）

- `SceneManagers/GameSceneInitializer.cs`
  - 进入地图场景后读取 `GameSettings.SelectedVehicle`，实例化玩家车辆，并把 `CinemachineVirtualCamera.Follow` 指向玩家。

- `SceneManagers/LaneSystem.cs`
  - 车道系统：管理车道数与车道宽度，提供 `GetLanePosition(laneIndex)` 用于计算车道中心 `Z`；支持编辑器 Gizmos 可视化车道线。

- `SceneManagers/StreetManager.cs`
  - 道路块循环：以队列复用道路预制块，根据相机位置触发把最前/最后的道路块移动到队尾，形成无限道路效果（沿 `X` 方向）。

- `SceneManagers/SideSceneManager.cs`
  - 两侧场景块循环：与 `StreetManager` 类似，用队列分别管理左右两侧场景块，在相机推进时进行复用（可选开启调试日志）。

### 4) 通用工具（`Tools/`）

- `Tools/MobileInputManager.cs`
  - 移动端输入汇总：提供 `throttle/steer/handbrake`，并对目标输入进行平滑插值。

- `Tools/TouchButton.cs`
  - 触控按钮组件：实现 `IPointerDown/Up`，把 UI 按钮事件映射为 `MobileInputManager` 的油门/刹车/手刹/左右转向输入。

- `Tools/ObjectPool.cs` + `Tools/IPoolable.cs`
  - 通用对象池：统一对象创建、获取、回收；被 `TrafficCarPool` 用于管理交通车复用。

- `Tools/CameraAspectRatioController.cs`
  - 视角适配：根据屏幕宽高比调整 `CinemachineVirtualCamera` 的 FOV。

### 5) 菜单与配置（`UI/`）

- `UI/GameSettings.cs`
  - 跨场景设置单例：保存车辆列表/地图列表与当前选中索引，并通过 `DontDestroyOnLoad` 在场景间保留。

- `UI/VehicleInfo.cs` / `UI/MapInfo.cs`
  - 数据结构：车辆（预览 prefab、游戏 prefab、图标）与地图（场景名、图标）信息，用于菜单选择与进场景加载。

- `UI/MenuFlowManager.cs`
  - 菜单流程控制：主界面/车辆选择/地图选择面板切换；车辆预览实例化；相机在菜单视角之间平滑移动；开始游戏时 `LoadScene(sceneName)`。

- `UI/MenuButtonBinder.cs`
  - UI 按钮绑定：把场景中各按钮统一绑定到 `MenuFlowManager` 的公开方法，减少手动连线错误。

- `UI/VehicleIconButton.cs` / `UI/MapIconButton.cs`
  - 图标按钮脚本：为固定图标绑定索引，点击后通知 `MenuFlowManager` 更新选中项。

- `UI/SpeedDisplayManager.cs`
  - 速度显示：从场景中查找 `CarController`，实时显示当前速度、最高速度（可选）、滑条进度与接近限速提示。

## 典型运行流程（以脚本逻辑为准）

1. 开始界面：`GameSettings` 常驻 + `MenuFlowManager` 负责车辆/地图选择与预览。
2. 点击开始：`MenuFlowManager.StartGame()` 加载选中地图场景。
3. 进入地图场景：`GameSceneInitializer` 根据 `GameSettings.SelectedVehicle` 生成玩家车并设置虚拟相机跟随。
4. 游戏进行：`CarController` 处理玩家输入与车辆运动；`TrafficCarPool` 周期性生成交通车并由 `TrafficCarRecycler` 回收；`StreetManager/SideSceneManager` 做道路与两侧场景循环。
