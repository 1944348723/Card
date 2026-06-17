# 详细学习计划

## 项目判断

当前项目是一个 Unity TCG/卡牌游戏项目，核心特征如下：

- Unity 版本：`2022.3.62f3`。
- 主要代码目录：`Assets/Tcg/Scripts`。
- 主要数据目录：`Assets/Tcg/Resources`。
- 主要场景：`LoginMenu`、`Menu`、`OpenPack`、`Game`。
- 核心技术：ScriptableObject 数据、UGUI/TextMeshPro、URP、Netcode for GameObjects、UnityWebRequest API。
- 代码规模：约 243 个 C# 脚本，约 3.4 万行。

这个项目的学习重点不是从 UI 开始，而是先掌握“数据如何定义卡牌”和“规则如何执行对局”。UI、特效、菜单、联网都建立在这条主线上。

## 时间估算

按每天 2 到 3 小时学习估算：

| 背景 | 能改简单内容 | 能理解主流程 | 能独立维护 |
| --- | --- | --- | --- |
| 熟悉 Unity 和 C# | 3 到 5 天 | 2 到 3 周 | 4 到 6 周 |
| 有 Unity 基础但不熟卡牌/网络 | 1 周 | 3 到 4 周 | 6 到 8 周 |
| Unity/C# 都较新 | 2 到 3 周 | 6 到 8 周 | 10 周以上 |

这里的“独立维护”指能比较稳地处理卡牌规则、数据资产、对局流程、基础 UI 和常见联机问题。

## 阶段 0：准备和运行，0.5 到 1 天

目标：确认项目能打开，知道入口场景和基础配置。

要看：

- `ProjectSettings/ProjectVersion.txt`
- `Packages/manifest.json`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/Tcg/Scenes/Menu/LoginMenu.unity`
- `Assets/Tcg/Scenes/Menu/Menu.unity`
- `Assets/Tcg/Scenes/Game/Game.unity`
- `Assets/Tcg/Resources/NetworkData.asset`
- `Assets/Tcg/Resources/GameplayData.asset`

任务：

- 用 Unity 2022.3.62f3 或兼容的 2022.3 LTS 打开项目。
- 先不要改代码，进入 `Game` 或主菜单流程，观察 Console。
- 确认当前认证模式、单人模式网络模式、服务器地址。
- 记录启动时 `DataLoader` 是否报重复 ID、空引用等错误。

产出：

- 一份自己的运行记录：打开哪个场景、是否能进入对局、Console 有哪些错误。

## 阶段 1：项目结构和资源组织，1 到 2 天

目标：知道每个目录大概做什么，遇到问题能先找对位置。

重点目录：

- `Assets/Tcg/Scripts/Data`：静态配置数据和 ScriptableObject 类型。
- `Assets/Tcg/Scripts/GameLogic`：对局状态和规则执行。
- `Assets/Tcg/Scripts/GameClient`：客户端对局表现、输入和刷新。
- `Assets/Tcg/Scripts/GameServer`：服务器侧对局管理和动作验证。
- `Assets/Tcg/Scripts/Network`：Netcode 封装、消息、认证。
- `Assets/Tcg/Scripts/Api`：账号、卡包、用户数据等 HTTP API。
- `Assets/Tcg/Scripts/Effects`：卡牌能力的具体效果。
- `Assets/Tcg/Scripts/Conditions`：能力触发和目标合法性判断。
- `Assets/Tcg/Scripts/UI`：通用 UI 组件。
- `Assets/Tcg/Scripts/Menu`：登录、主菜单、卡组、匹配、开包。
- `Assets/Tcg/Resources`：所有可被 `Resources.LoadAll` 加载的数据资产。

建议读法：

1. 不要一开始打开所有 UI 文件。
2. 先看目录名和文件名。
3. 只读每个目录中 1 到 3 个代表文件。
4. 把“数据、规则、客户端、服务器、UI”五类代码分开理解。

产出：

- 能口头说明：一张卡从 `.asset` 到游戏中 `Card` 实例的大概路径。

## 阶段 2：数据系统，2 到 4 天

目标：理解这个项目为什么是数据驱动的，以及如何新增/调整卡牌。

必读文件：

- `Assets/Tcg/Scripts/Data/DataLoader.cs`
- `Assets/Tcg/Scripts/Data/GameplayData.cs`
- `Assets/Tcg/Scripts/Data/CardData.cs`
- `Assets/Tcg/Scripts/Data/AbilityData.cs`
- `Assets/Tcg/Scripts/Data/ConditionData.cs`
- `Assets/Tcg/Scripts/Data/EffectData.cs`
- `Assets/Tcg/Scripts/Data/DeckData.cs`
- `Assets/Tcg/Scripts/Data/StatusData.cs`

要理解的问题：

- `DataLoader.LoadData()` 启动时加载了哪些类型？
- `Resources.LoadAll<T>()` 为什么要求数据资产放在 `Resources` 下？
- `CardData` 和运行时 `Card` 的区别是什么？
- `AbilityData` 的 `trigger`、`target`、`conditions_*`、`effects` 分别负责什么？
- `ConditionData` 和 `EffectData` 为什么设计成基类加很多子类？

练习：

- 找一张卡，例如 `Assets/Tcg/Resources/Cards/Fire/firefox.asset`。
- 找到它引用的 ability。
- 继续找到 ability 引用的 condition 和 effect。
- 修改这张卡的 mana、attack、hp，然后运行验证。
- 复制一张现有卡牌资产，改 ID、标题、数值，放入测试卡组。

产出：

- 能新增一张不需要新代码的简单卡。

## 阶段 3：运行时对局状态，2 到 3 天

目标：区分“静态配置”和“运行时状态”。

必读文件：

- `Assets/Tcg/Scripts/GameLogic/Game.cs`
- `Assets/Tcg/Scripts/GameLogic/Player.cs`
- `Assets/Tcg/Scripts/GameLogic/Card.cs`
- `Assets/Tcg/Scripts/GameLogic/Slot.cs`
- `Assets/Tcg/Scripts/GameLogic/GameSettings.cs`

要理解的问题：

- `Game` 保存了哪些可同步状态？
- `Player` 里有哪些卡牌区域：deck、hand、board、equip、discard、secret、temp？
- `CardData` 是卡牌模板，`Card` 是对局中的卡牌实例，这两者如何连接？
- 卡牌 UID 和卡牌 ID 的区别是什么？
- `Game.CanPlayCard`、`Game.CanAttackTarget`、`Game.IsPlayerActionTurn` 为什么在状态层判断？

练习：

- 跟踪一张牌从 deck 到 hand，再到 board 或 discard。
- 在 Debug 模式下观察 `Game.players[0].cards_hand` 和 `cards_board`。
- 找出一个卡牌不可出牌的原因，例如法力不足、槽位非法、目标非法。

产出：

- 能看懂一次对局状态快照。

## 阶段 4：规则执行和结算队列，4 到 6 天

目标：理解对局规则的核心。

必读文件：

- `Assets/Tcg/Scripts/GameLogic/GameLogic.cs`
- `Assets/Tcg/Scripts/Tools/ResolveQueue.cs`
- `Assets/Tcg/Scripts/Effects/EffectDamage.cs`
- `Assets/Tcg/Scripts/Effects/EffectDraw.cs`
- `Assets/Tcg/Scripts/Effects/EffectSummon.cs`
- `Assets/Tcg/Scripts/Conditions/ConditionOwner.cs`
- `Assets/Tcg/Scripts/Conditions/ConditionTarget.cs`
- `Assets/Tcg/Scripts/Conditions/FilterRandom.cs`

建议先追这几条流程：

1. `StartGame`
2. `StartTurn`
3. `PlayCard`
4. `TriggerCardAbilityType`
5. `ResolveCardAbility`
6. `AttackTarget`
7. `DamageCard`
8. `EndTurn`

要理解的问题：

- 为什么 `GameLogic` 才是真正规则执行者？
- `Game` 为什么更像状态容器？
- `ResolveQueue` 为什么存在？
- 选择目标时为什么结算队列会暂停？
- 持续能力 `Ongoing` 如何刷新？
- 秘密牌、连锁能力、选择器是怎么接入主流程的？

练习：

- 新增一个简单效果，例如“造成固定伤害”可以先复用已有 `EffectDamage`。
- 如果现有 `Effect` 不够，再参考已有 `Effect*.cs` 写一个新效果。
- 给一张测试卡添加这个效果，验证执行顺序。

产出：

- 能定位一个规则问题应该看 `Game`、`GameLogic`、`AbilityData`、`EffectData` 里的哪一层。

## 阶段 5：客户端输入和表现，3 到 5 天

目标：理解玩家操作如何从鼠标输入变成服务器动作，再变成画面反馈。

必读文件：

- `Assets/Tcg/Scripts/GameClient/GameClient.cs`
- `Assets/Tcg/Scripts/GameClient/PlayerControls.cs`
- `Assets/Tcg/Scripts/GameClient/BoardCard.cs`
- `Assets/Tcg/Scripts/GameClient/HandCard.cs`
- `Assets/Tcg/Scripts/GameClient/GameBoard.cs`
- `Assets/Tcg/Scripts/UI/GameUI.cs`
- `Assets/Tcg/Scripts/UI/CardUI.cs`
- `Assets/Tcg/Scripts/UI/SelectorPanel.cs`

要理解的问题：

- `PlayerControls` 负责哪些输入？
- `GameClient.PlayCard` 发出的是什么消息？
- `GameClient` 的 `onCardPlayed`、`onAttackStart` 等事件由谁触发？
- `BoardCard` 和 `CardUI` 分别服务于什么显示场景？
- UI 是否直接改规则状态？正常情况下不应该。

练习：

- 跟踪一次拖动手牌到棋盘槽位。
- 找到出牌动画或特效在哪里触发。
- 修改一个 UI 文本或显示格式，确认不影响规则层。

产出：

- 能修改基础对局 UI 和表现，不破坏规则。

## 阶段 6：服务器、网络和动作验证，4 到 7 天

目标：理解“客户端请求动作，服务器验证并执行”的结构。

必读文件：

- `Assets/Tcg/Scripts/GameServer/GameServer.cs`
- `Assets/Tcg/Scripts/GameLogic/GameAction.cs`
- `Assets/Tcg/Scripts/Network/TcgNetwork.cs`
- `Assets/Tcg/Scripts/Network/NetworkMsg.cs`
- `Assets/Tcg/Scripts/Network/NetworkMessaging.cs`
- `Assets/Tcg/Scripts/Network/NetworkData.cs`

要理解的问题：

- `GameAction` 里哪些是客户端命令，哪些是服务器刷新？
- `GameServer.ReceivePlayCard` 如何验证玩家身份、回合、卡牌所有权？
- `GameServer` 为什么持有 `GameLogic`？
- `GameClient` 为什么不直接执行规则？
- 单人模式、HostP2P、Multiplayer、Observer 的差别是什么？

推荐阅读路线：

1. `GameClient.PlayCard`
2. `GameAction.PlayCard`
3. `GameServer.ReceivePlayCard`
4. `GameLogic.PlayCard`
5. `GameServer.OnCardPlayed`
6. `GameClient.OnCardPlayed`

产出：

- 能解释一个动作如何完成“输入、发送、验证、执行、广播、刷新”。

## 阶段 7：菜单、账号和 API，3 到 5 天

目标：知道对局前的数据从哪里来，包括登录、用户数据、卡组、匹配。

必读文件：

- `Assets/Tcg/Scripts/Menu/LoginMenu.cs`
- `Assets/Tcg/Scripts/Menu/MainMenu.cs`
- `Assets/Tcg/Scripts/Menu/CollectionPanel.cs`
- `Assets/Tcg/Scripts/GameClient/GameClientMatchmaker.cs`
- `Assets/Tcg/Scripts/Api/ApiClient.cs`
- `Assets/Tcg/Scripts/Api/UserData.cs`
- `Assets/Tcg/Scripts/Network/Authenticator.cs`
- `Assets/Tcg/Scripts/Network/AuthenticatorLocal.cs`
- `Assets/Tcg/Scripts/Network/AuthenticatorApi.cs`

要理解的问题：

- 本地登录和 API 登录的区别是什么？
- 玩家卡组如何从菜单传给对局？
- `GameClient.game_settings` 和 `GameClient.player_settings` 为什么是静态字段？
- 卡包、金币、收藏、好友、排行榜哪些依赖 API？

学习建议：

- 前期优先使用本地测试模式。
- 不要一开始搭 API 服务器。
- 只有需要线上账号、开包、商城、排行榜时再深入 API。

产出：

- 能调整菜单进入对局的默认设置。

## 阶段 8：AI 和工具，2 到 4 天

目标：理解辅助系统，不作为第一优先级。

必读文件：

- `Assets/Tcg/Scripts/AI/AIPlayer.cs`
- `Assets/Tcg/Scripts/AI/AIPlayerRandom.cs`
- `Assets/Tcg/Scripts/AI/AIPlayerMM.cs`
- `Assets/Tcg/Scripts/AI/AILogic.cs`
- `Assets/Tcg/Scripts/AI/AIHeuristic.cs`
- `Assets/Tcg/Scripts/Tools/CardExporter.cs`
- `Assets/Tcg/Scripts/Tools/CardUploader.cs`

要理解的问题：

- AI 为什么直接调用 `GameLogic` 而不是走客户端网络消息？
- Minimax AI 如何复制游戏状态进行预测？
- 工具场景用于导出、上传、权限调整，和普通玩家流程不同。

产出：

- 能调整 AI 难度和简单行为。

## 每周安排样例

### 第 1 周：能跑和能改数据

- 第 1 天：打开项目、跑场景、看 Console。
- 第 2 天：读 `DataLoader`、`GameplayData`。
- 第 3 天：读 `CardData`、找 3 张卡牌资产。
- 第 4 天：读 `AbilityData`，找 ability 到 effect 的引用链。
- 第 5 天：改一张卡，加入测试卡组。
- 第 6 天：读 `DeckData`、`UserData` 的卡组结构。
- 第 7 天：整理笔记，画出“卡牌数据流”。

### 第 2 周：读懂规则层

- 第 1 天：读 `Game`。
- 第 2 天：读 `Player` 和 `Card`。
- 第 3 天：读 `GameLogic.StartGame`、`StartTurn`。
- 第 4 天：读 `GameLogic.PlayCard`。
- 第 5 天：读 `TriggerCardAbilityType` 和 `ResolveCardAbility`。
- 第 6 天：读 `AttackTarget`、`DamageCard`、`KillCard`。
- 第 7 天：用一场对局验证自己的理解。

### 第 3 周：客户端和服务器

- 第 1 天：读 `GameClient.Start` 和注册事件。
- 第 2 天：读 `PlayerControls`。
- 第 3 天：读 `GameServer.Receive*` 系列方法。
- 第 4 天：跟踪 `PlayCard` 从输入到刷新。
- 第 5 天：跟踪 `Attack` 从输入到刷新。
- 第 6 天：读 `NetworkMsg` 和 `GameAction`。
- 第 7 天：整理“动作链路图”。

### 第 4 周：菜单、API、AI 和实践

- 第 1 天：读 `MainMenu`。
- 第 2 天：读 `Authenticator` 和 `ApiClient`。
- 第 3 天：读 `GameClientMatchmaker`。
- 第 4 天：读 AI 基类和 Minimax 入口。
- 第 5 天：做一个小功能，例如新增卡牌或新增效果。
- 第 6 天：做一次 bug 定位演练。
- 第 7 天：整理项目个人文档。

## 学习时的优先级

优先级高：

- 数据资产和 ScriptableObject。
- `Game`、`Player`、`Card`。
- `GameLogic`。
- 出牌、攻击、能力触发。
- `GameClient` 和 `GameServer` 动作链路。

优先级中：

- 对局 UI。
- 菜单和卡组。
- 本地认证。
- AI。

优先级低，后期再看：

- API 服务器部署。
- 工具场景。
- 美术资源细节。
- 特效动画细节。

## 判断自己是否学懂

达到以下状态，说明已经具备基本维护能力：

- 能新增一张只复用现有效果的卡。
- 能新增一个简单 `EffectData` 子类，并通过资产引用使用。
- 能说明 `CardData` 和 `Card` 的区别。
- 能说明 `Game` 和 `GameLogic` 的区别。
- 能跟踪一次出牌从输入到服务器再到客户端刷新的完整链路。
- 能判断一个 bug 是数据问题、规则问题、UI 问题还是网络同步问题。

