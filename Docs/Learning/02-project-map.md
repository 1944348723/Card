# 项目结构地图

## 顶层结构

| 路径 | 作用 |
| --- | --- |
| `Assets/Tcg` | 项目主体，包含脚本、资源、场景、Prefab、配置资产。 |
| `Assets/Tcg/Scripts` | 所有主要 C# 代码。 |
| `Assets/Tcg/Resources` | 数据驱动资产，被 `Resources.LoadAll` 加载。 |
| `Assets/Tcg/Scenes` | 菜单、对局、服务器、工具场景。 |
| `Assets/Tcg/Prefabs` | UI、对局对象、特效、管理器 Prefab。 |
| `Assets/Tcg/Sprites` | 卡图、UI、图标、棋盘图等美术资源。 |
| `Assets/Tcg/Install` | API 压缩包、Insomnia 请求、Linux service 示例。 |
| `ProjectSettings` | Unity 项目设置。 |
| `Packages` | Unity 包依赖。 |

## Scripts 目录职责

| 目录 | 职责 | 先读文件 |
| --- | --- | --- |
| `Data` | ScriptableObject 数据定义和加载 | `DataLoader.cs`、`CardData.cs`、`AbilityData.cs` |
| `GameLogic` | 对局状态、规则、动作常量 | `Game.cs`、`GameLogic.cs`、`Player.cs`、`Card.cs` |
| `GameClient` | 对局客户端、输入、棋盘表现 | `GameClient.cs`、`PlayerControls.cs`、`BoardCard.cs` |
| `GameServer` | 服务器侧房间、动作验证、广播刷新 | `GameServer.cs`、`ServerManager.cs` |
| `Network` | Netcode 封装、消息、认证 | `TcgNetwork.cs`、`NetworkMsg.cs`、`Authenticator.cs` |
| `Api` | HTTP API、用户数据、请求响应结构 | `ApiClient.cs`、`UserData.cs` |
| `Effects` | 能力效果实现 | `EffectDamage.cs`、`EffectDraw.cs`、`EffectSummon.cs` |
| `Conditions` | 触发和目标条件判断 | `ConditionOwner.cs`、`ConditionTarget.cs` |
| `AI` | 随机 AI、Minimax AI、估值 | `AIPlayer.cs`、`AILogic.cs` |
| `Menu` | 登录、主菜单、收藏、匹配、开包 | `MainMenu.cs`、`LoginMenu.cs` |
| `UI` | 通用 UI 组件和对局 UI | `GameUI.cs`、`CardUI.cs`、`SelectorPanel.cs` |
| `FX` | 特效和动画辅助 | `DamageFX.cs`、`Projectile.cs` |
| `Tools` | 通用工具、导入导出、场景跳转 | `SceneNav.cs`、`ResolveQueue.cs`、`CardExporter.cs` |

## Resources 数据资产

| 目录 | 数据类型 | 对应脚本 |
| --- | --- | --- |
| `Cards` | 卡牌模板 | `CardData.cs` |
| `Abilities` | 卡牌能力配置 | `AbilityData.cs` |
| `Effects` | 能力效果资产 | `EffectData.cs` 和 `Effect*.cs` |
| `Conditions` | 条件资产 | `ConditionData.cs` 和 `Condition*.cs` |
| `Decks` | 预设卡组 | `DeckData.cs` |
| `Levels` | 冒险/谜题关卡 | `LevelData.cs`、`DeckPuzzleData.cs` |
| `Status` | 状态效果定义 | `StatusData.cs` |
| `Teams` | 阵营 | `TeamData.cs` |
| `Traits` | 特性/数值标签 | `TraitData.cs` |
| `Rarities` | 稀有度 | `RarityData.cs` |
| `Packs` | 卡包 | `PackData.cs` |
| `Variants` | 卡牌变体 | `VariantData.cs` |
| `Avatars` | 头像 | `AvatarData.cs` |
| `Cardbacks` | 卡背 | `CardbackData.cs` |

## 场景地图

| 场景 | 作用 | 是否主流程 |
| --- | --- | --- |
| `Assets/Tcg/Scenes/Menu/LoginMenu.unity` | 登录入口 | 是 |
| `Assets/Tcg/Scenes/Menu/Menu.unity` | 主菜单、卡组、模式入口 | 是 |
| `Assets/Tcg/Scenes/Menu/OpenPack.unity` | 开包表现 | 是 |
| `Assets/Tcg/Scenes/Game/Game.unity` | 2D 对局主场景 | 是 |
| `Assets/Tcg/Scenes/Game/Game3D.unity` | 3D 对局场景 | 否，扩展场景 |
| `Assets/Tcg/Scenes/Server/Server.unity` | 专用服务器场景 | 否，部署相关 |
| `Assets/Tcg/Scenes/Tool/CardExporter.unity` | 卡牌导出工具 | 否，工具 |
| `Assets/Tcg/Scenes/Tool/CardUploader.unity` | 卡牌上传工具 | 否，工具 |
| `Assets/Tcg/Scenes/Tool/ChangePermission.unity` | 权限工具 | 否，工具 |
| `Assets/Tcg/Scenes/Tool/TestP2P.unity` | P2P 测试 | 否，工具 |

Build Settings 当前启用主流程场景：`LoginMenu`、`Menu`、`OpenPack`、`Game`。

## 核心类关系

```text
DataLoader
  -> Resources.LoadAll
  -> CardData / AbilityData / DeckData / GameplayData / StatusData ...

CardData
  -> AbilityData[]
  -> TeamData / RarityData / TraitData / PackData

AbilityData
  -> ConditionData[]
  -> FilterData[]
  -> EffectData[]
  -> StatusData[]
  -> chain_abilities[]

Game
  -> Player[]
  -> GameSettings
  -> 当前回合、阶段、选择器、最近目标等状态

Player
  -> cards_deck / cards_hand / cards_board / cards_equip
  -> cards_discard / cards_secret / cards_temp
  -> hero / status / traits

Card
  -> card_id / uid / player_id / slot
  -> attack / hp / mana / damage / status / abilities
  -> 非序列化缓存 CardData / VariantData / AbilityData

GameLogic
  -> 修改 Game / Player / Card
  -> 触发 Ability
  -> 通过 ResolveQueue 控制结算顺序
  -> 发出 UnityAction 事件

GameServer
  -> 接收 GameClient 动作
  -> 验证玩家、回合、卡牌所有权
  -> 调用 GameLogic
  -> 广播刷新事件和状态

GameClient
  -> 发送玩家动作
  -> 接收服务器刷新
  -> 驱动 UI、棋盘、特效
```

## 关键设计原则

### 静态数据和运行时状态分离

`CardData` 是卡牌模板，存在于 `.asset` 中。`Card` 是对局中创建出来的实例，有唯一 `uid`、当前伤害、状态、所在区域。

### 规则由服务器或本地主机执行

客户端不应该直接决定规则结果。客户端发送请求，服务器或本地主机验证后调用 `GameLogic`。

### 卡牌能力通过数据组合

多数卡牌不需要写新代码，只需要组合：

- 触发时机：`AbilityTrigger`
- 目标类型：`AbilityTarget`
- 条件：`ConditionData`
- 过滤器：`FilterData`
- 效果：`EffectData`
- 状态：`StatusData`
- 连锁能力：`chain_abilities`

### 结算队列解耦规则和表现

`ResolveQueue` 让攻击、能力、秘密和阶段切换按顺序执行，并允许插入延迟和目标选择。

