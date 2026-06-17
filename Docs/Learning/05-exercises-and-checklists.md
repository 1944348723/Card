# 练习、清单和术语

## 学习练习

### 练习 1：画出项目地图

目标：熟悉目录职责。

步骤：

1. 打开 `Assets/Tcg/Scripts`。
2. 给每个子目录写一句话说明。
3. 找出每个子目录最重要的 1 到 3 个文件。
4. 和 `02-project-map.md` 对照，补充自己的理解。

验收：

- 能说出 `Data`、`GameLogic`、`GameClient`、`GameServer`、`Network` 的区别。

### 练习 2：追踪一张卡

目标：理解数据资产链。

步骤：

1. 选择一张卡，例如 `Assets/Tcg/Resources/Cards/Fire/firefox.asset`。
2. 记录它的 `id`、`type`、`mana`、`attack`、`hp`。
3. 找出它引用的 ability。
4. 找出 ability 的 trigger、target、effect。
5. 找出 effect 对应的 C# 类。

验收：

- 能解释这张卡为什么会产生当前效果。

### 练习 3：修改基础规则参数

目标：理解 `GameplayData.asset`。

步骤：

1. 打开 `Assets/Tcg/Resources/GameplayData.asset`。
2. 修改起始生命、起始手牌、回合时间等参数。
3. 运行一场测试对局。
4. 观察是否生效。

验收：

- 能判断哪些规则参数来自数据，哪些来自代码。

### 练习 4：新增一张简单卡

目标：学会无代码新增卡牌。

步骤：

1. 复制一张已有卡牌资产。
2. 修改唯一 `id`。
3. 修改标题、费用、攻击、生命。
4. 复用一个已有 ability。
5. 加入 `test_deck`。
6. 运行 `Game` 场景验证。

验收：

- 卡牌能出现在手牌或牌库中。
- 能被正常打出。
- 能力按预期触发。

### 练习 5：跟踪出牌动作

目标：掌握客户端到服务器再到规则层的完整链路。

步骤：

1. 从 `GameClient.PlayCard` 开始。
2. 找到 `GameAction.PlayCard`。
3. 找到 `GameServer.ReceivePlayCard`。
4. 找到 `GameLogic.PlayCard`。
5. 找到服务器广播和客户端刷新。

验收：

- 能画出出牌链路。
- 能说出每一层的职责。

### 练习 6：新增一个简单 Effect

目标：学会扩展规则。

步骤：

1. 参考 `EffectDamage.cs` 或 `EffectDraw.cs`。
2. 新增一个非常小的效果，例如“给目标回复固定生命”或“让玩家抽一张牌并加一点法力”。
3. 创建对应 effect asset。
4. 创建 ability asset 引用它。
5. 创建测试卡引用 ability。
6. 运行验证。

验收：

- 新效果能被 AbilityData 调用。
- 不影响其他卡牌。
- Console 没有空引用或数据错误。

### 练习 7：定位一个故意制造的问题

目标：训练排查能力。

可制造的问题：

- 把一张卡的 ability 引用删掉。
- 把 ability 的 target 改成不匹配的类型。
- 把 condition 改得过严。
- 把卡牌从 test deck 移除。

验收：

- 能根据现象定位到数据、规则、UI 或网络中的某一层。

## 日常学习记录模板

```markdown
# 学习记录

日期：
学习时长：

今天阅读的文件：

- 

今天理解的流程：

- 

今天遇到的问题：

- 

验证方式：

- 

明天继续：

- 
```

## 调试清单

### 启动失败或 Console 报错

- Unity 版本是否接近 `2022.3.62f3`。
- 包是否正常解析。
- `Resources` 中是否有重复 ID。
- `DataLoader` 是否报空引用。
- 场景中是否存在必要 Manager Prefab。

### 卡牌数据错误

- `id` 是否唯一。
- `team`、`rarity` 是否为空。
- `abilities` 是否有空引用。
- `traits`、`stats` 是否有空引用。
- 卡图、特效、音效缺失是否影响运行。

### 对局动作无效

- 当前是否在 `GameState.Play`。
- 当前是否是 `GamePhase.Main`。
- 是否轮到当前玩家。
- 是否处于选择器状态。
- `gameplay.IsResolving()` 是否正在结算。
- 卡牌是否属于该玩家。
- 卡牌是否在合法区域。

### 能力结算异常

- `AbilityTrigger` 是否正确。
- `AbilityTarget` 是否对应正确的 `DoEffect` 重载。
- `conditions_trigger` 是否满足。
- `conditions_target` 是否满足。
- `filters_target` 是否清空了目标。
- `chain_abilities` 是否为空或有空引用。
- `ResolveQueue` 是否被 selector 暂停。

### 客户端画面异常

- 本地 `game_data` 是否收到刷新。
- `GameClient` 对应事件是否触发。
- UI 或 Board 对象是否监听了事件。
- Prefab 组件引用是否缺失。
- 对象池或实例化位置是否正确。

### 网络异常

- `NetworkData.asset` 的 url、port 是否正确。
- 单人模式是 `UseNetcode` 还是 `Offline`。
- `TcgNetwork` 是否启动成功。
- 客户端是否发送了 `action`。
- 服务器是否监听并执行 `ReceiveAction`。
- 服务器是否通过 `SendToAll` 广播刷新。

## 常用搜索命令

在 PowerShell 中可以使用：

```powershell
rg "PlayCard" Assets\Tcg\Scripts
rg "AbilityTrigger.OnPlay" Assets\Tcg\Scripts
rg "class Effect" Assets\Tcg\Scripts\Effects
rg "CreateAssetMenu" Assets\Tcg\Scripts
rg "ReceivePlayCard" Assets\Tcg\Scripts
```

按文件列出脚本：

```powershell
rg --files Assets\Tcg\Scripts -g "*.cs"
```

按数据资产查找：

```powershell
rg "id: firefox" Assets\Tcg\Resources
rg "play_deal_damage1" Assets\Tcg\Resources
```

## 术语表

| 术语 | 含义 |
| --- | --- |
| ScriptableObject | Unity 数据资产类型，用来在编辑器中配置卡牌、能力、效果等。 |
| CardData | 卡牌静态模板。 |
| Card | 对局中的卡牌实例。 |
| AbilityData | 能力配置，决定触发、目标、条件、效果。 |
| EffectData | 能力真正执行的效果基类。 |
| ConditionData | 条件判断基类。 |
| FilterData | 目标过滤器基类。 |
| StatusData | 状态定义。 |
| TraitData | 特性或标签定义。 |
| DeckData | 预设卡组定义。 |
| Game | 对局状态容器。 |
| GameLogic | 对局规则执行器。 |
| GameServer | 服务器侧对局管理和动作验证。 |
| GameClient | 客户端对局连接、动作发送、刷新接收。 |
| GameAction | 网络动作和刷新事件 ID 常量。 |
| ResolveQueue | 规则结算队列，用于顺序执行能力、攻击、秘密和回调。 |
| Selector | 玩家选择目标、卡牌、选项或费用时的状态。 |
| Ongoing | 持续效果，通常每次状态变化后重新计算。 |
| Mulligan | 起手换牌阶段。 |
| UID | 对局中卡牌实例的唯一 ID。 |
| ID | 静态数据资产 ID，例如卡牌 ID 或能力 ID。 |

## 维护前检查

改规则前先确认：

- 这个需求能否只通过数据资产完成。
- 是否已有类似 `Effect`、`Condition`、`Filter`。
- 是否会影响 AI 预测。
- 是否会影响服务器验证。
- 是否需要客户端 UI 增加提示。

改 UI 前先确认：

- UI 是否只读状态，不直接改规则。
- 是否存在服务器刷新事件。
- Prefab 引用是否会被改坏。
- 是否影响不同分辨率或移动端。

改网络前先确认：

- 新动作是否需要新的 `GameAction`。
- 是否需要新的 `NetworkMsg`。
- 服务器是否验证所有权、回合、目标合法性。
- 客户端是否只是请求，不直接决定结果。

## 个人能力路线

### 第一档：内容配置

能做：

- 改卡牌数值。
- 新增卡牌。
- 改卡组。
- 改基础规则参数。

需要掌握：

- `CardData`
- `AbilityData`
- `GameplayData`
- `DeckData`

### 第二档：规则扩展

能做：

- 新增简单效果。
- 新增条件。
- 修规则 bug。
- 调整攻击或回合流程。

需要掌握：

- `Game`
- `GameLogic`
- `EffectData`
- `ConditionData`
- `ResolveQueue`

### 第三档：客户端维护

能做：

- 改对局 UI。
- 改拖拽、点击、选择目标。
- 改动画和特效触发。

需要掌握：

- `GameClient`
- `PlayerControls`
- `BoardCard`
- `HandCard`
- `GameUI`

### 第四档：联机和账号

能做：

- 改匹配和连接。
- 改 API 登录、用户数据、卡包。
- 修联机同步问题。

需要掌握：

- `GameServer`
- `TcgNetwork`
- `NetworkMessaging`
- `NetworkMsg`
- `ApiClient`
- `Authenticator`

