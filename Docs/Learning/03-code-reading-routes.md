# 代码阅读路线

本文按业务流程给出阅读顺序。学习时不要只按文件目录读，应该按“一个动作怎么发生”来追。

## 路线 1：启动时加载数据

目标：理解所有卡牌和配置怎么进入内存。

阅读顺序：

1. `Assets/Tcg/Scripts/Data/DataLoader.cs`
2. `Assets/Tcg/Scripts/Data/CardData.cs`
3. `Assets/Tcg/Scripts/Data/AbilityData.cs`
4. `Assets/Tcg/Scripts/Data/DeckData.cs`
5. `Assets/Tcg/Scripts/Data/GameplayData.cs`

关键调用：

```text
DataLoader.Awake
  -> LoadData
    -> CardData.Load
    -> TeamData.Load
    -> RarityData.Load
    -> TraitData.Load
    -> VariantData.Load
    -> PackData.Load
    -> LevelData.Load
    -> DeckData.Load
    -> AbilityData.Load
    -> StatusData.Load
    -> AvatarData.Load
    -> CardbackData.Load
    -> RewardData.Load
    -> CheckCardData
    -> CheckAbilityData
    -> CheckDeckData
```

要回答的问题：

- 为什么 ID 不能重复？
- 为什么 `Resources` 目录很重要？
- 静态 list 和 dictionary 分别解决什么问题？

## 路线 2：从菜单进入对局

目标：理解对局参数、玩家卡组、场景如何传递。

阅读顺序：

1. `Assets/Tcg/Scripts/Menu/MainMenu.cs`
2. `Assets/Tcg/Scripts/GameLogic/GameSettings.cs`
3. `Assets/Tcg/Scripts/GameClient/GameClient.cs`
4. `Assets/Tcg/Scripts/GameClient/GameClientMatchmaker.cs`
5. `Assets/Tcg/Scripts/Network/TcgNetwork.cs`

关键流程：

```text
MainMenu.StartGame
  -> 设置 GameClient.game_settings
  -> 设置 game_uid / server_url / scene
  -> FadeToScene(GameClient.game_settings.GetScene())

Game 场景加载
  -> GameClient.Start
  -> ConnectToAPI
  -> ConnectToServer
  -> ConnectToGame
  -> SendGameSettings
  -> SendPlayerSettings
```

要回答的问题：

- `GameSettings.Default` 里默认是什么？
- 单人、冒险、多人、P2P、观战如何区分？
- 玩家卡组什么时候传给服务器？

## 路线 3：出牌完整链路

目标：掌握最重要的一条对局动作链。

阅读顺序：

1. `Assets/Tcg/Scripts/GameClient/HandCard.cs`
2. `Assets/Tcg/Scripts/GameClient/PlayerControls.cs`
3. `Assets/Tcg/Scripts/GameClient/GameClient.cs`
4. `Assets/Tcg/Scripts/GameLogic/GameAction.cs`
5. `Assets/Tcg/Scripts/GameServer/GameServer.cs`
6. `Assets/Tcg/Scripts/GameLogic/Game.cs`
7. `Assets/Tcg/Scripts/GameLogic/GameLogic.cs`
8. 回到 `GameServer.cs`
9. 回到 `GameClient.cs`
10. `Assets/Tcg/Scripts/GameClient/BoardCard.cs`

核心链路：

```text
玩家拖动或点击手牌
  -> GameClient.PlayCard(card, slot)
    -> SendAction(GameAction.PlayCard, MsgPlayCard)
      -> GameServer.ReceivePlayCard
        -> 校验 player != null
        -> 校验 game_data.IsPlayerActionTurn(player)
        -> 校验 card.player_id == player.player_id
        -> gameplay.PlayCard(card, slot)
          -> game_data.CanPlayCard(card, slot)
          -> 扣除法力
          -> 从所有区域移除卡牌
          -> 加入 board / equip / secret / discard
          -> TriggerCardAbilityType(OnPlay)
          -> ResolveQueue.ResolveAll
          -> onCardPlayed
        -> GameServer.OnCardPlayed
          -> SendToAll(GameAction.CardPlayed)
            -> GameClient.OnCardPlayed
              -> 更新本地 game_data
              -> 触发 onCardPlayed
              -> UI/棋盘表现刷新
```

要重点看：

- `Game.CanPlayCard`
- `GameLogic.PlayCard`
- `GameServer.ReceivePlayCard`
- `GameClient.PlayCard`

常见问题定位：

- 不能出牌：先看 `Game.CanPlayCard`。
- 出牌后没效果：看 `AbilityData.trigger` 是否是 `OnPlay`，再看 `conditions_trigger`。
- 出牌后画面不刷新：看服务器是否发 `CardPlayed` 或 `RefreshAll`。

## 路线 4：攻击完整链路

目标：理解战斗、反击、疲劳、触发能力。

阅读顺序：

1. `PlayerControls.ReleaseClick`
2. `GameClient.AttackTarget`
3. `GameServer.ReceiveAttackTarget`
4. `Game.CanAttackTarget`
5. `GameLogic.AttackTarget`
6. `GameLogic.ResolveAttack`
7. `GameLogic.ResolveAttackHit`
8. `GameLogic.DamageCard`
9. `GameLogic.KillCard`

核心链路：

```text
选择己方场上角色并释放到敌方卡牌
  -> GameClient.AttackTarget
  -> GameServer.ReceiveAttackTarget
  -> game_data.CanAttackTarget(attacker, target)
  -> gameplay.AttackTarget
    -> Trigger OnBeforeAttack / OnBeforeDefend
    -> TriggerSecrets
    -> ResolveQueue.AddAttack
    -> ResolveAttack
      -> onAttackStart
      -> ResolveAttackHit
        -> DamageCard(attacker, target, attacker.GetAttack())
        -> DamageCard(target, attacker, target.GetAttack())
        -> ExhaustBattle
        -> Trigger OnAfterAttack / OnAfterDefend
        -> onAttackEnd
        -> CheckForWinner
```

要回答的问题：

- 哪些状态会影响攻击，例如 `Stealth`、`Protected`、`Flying`、`Fury`？
- 什么时候会移除潜行？
- 攻击后为什么可能不疲劳？
- 伤害和击杀分别在哪里处理？

## 路线 5：能力触发和目标选择

目标：理解卡牌能力系统，这是项目最核心也最容易绕的部分。

阅读顺序：

1. `Assets/Tcg/Scripts/Data/AbilityData.cs`
2. `Assets/Tcg/Scripts/Data/ConditionData.cs`
3. `Assets/Tcg/Scripts/Data/EffectData.cs`
4. `Assets/Tcg/Scripts/GameLogic/GameLogic.cs`
5. `Assets/Tcg/Scripts/UI/SelectorPanel.cs`
6. `Assets/Tcg/Scripts/GameClient/GameClient.cs`

无目标能力：

```text
TriggerCardAbilityType
  -> TriggerCardAbility
    -> ResolveQueue.AddAbility
      -> ResolveCardAbility
        -> ResolveCardAbilityNoTarget
          -> ability.DoEffects(logic, caster)
          -> AfterAbilityResolved
```

卡牌目标能力：

```text
ResolveCardAbility
  -> ResolveCardAbilityCards
    -> ability.GetCardTargets
    -> ResolveEffectTarget(ability, caster, target)
      -> ability.DoEffects(logic, caster, target)
      -> onAbilityTargetCard
```

需要玩家选择目标：

```text
ResolveCardAbility
  -> ResolveCardAbilitySelector
    -> GoToSelectTarget
      -> game_data.selector = SelectTarget
      -> 等待客户端选择
        -> GameClient.SelectCard / SelectPlayer / SelectSlot
        -> GameServer.ReceiveSelect*
        -> GameLogic.Select*
        -> ResolveEffectTarget
```

要回答的问题：

- `AbilityTrigger` 决定什么时候触发。
- `AbilityTarget` 决定目标来自哪里。
- `conditions_trigger` 判断能力能不能触发。
- `conditions_target` 判断目标是否合法。
- `filters_target` 对候选目标二次筛选。
- `effects` 负责真正修改规则状态。
- `status` 负责附加状态。
- `chain_abilities` 负责连锁或选择。

## 路线 6：持续效果 Ongoing

目标：理解光环、持续加成、临时状态如何刷新。

阅读顺序：

1. `AbilityTrigger.Ongoing`
2. `GameLogic.UpdateOngoing`
3. `GameLogic.UpdateOngoingCards`
4. `GameLogic.UpdateOngoingAbilities`
5. `Card.ClearOngoing`
6. `Player.ClearOngoing`

核心思路：

```text
某些规则变动后
  -> UpdateOngoing
    -> 清理所有 ongoing 状态和数值
    -> 遍历场上卡牌和玩家
    -> 找到 Ongoing 能力
    -> 重新应用 ongoing 效果
    -> 检查因持续效果消失导致的死亡
```

注意：

- Ongoing 不应该永久修改基础数值。
- Ongoing 通常写入 `*_ongoing` 或 `ongoing_status`。
- 出牌、移动、伤害、死亡等操作后经常会调用 `UpdateOngoing`。

## 路线 7：登录和 API

目标：理解用户数据和在线接口，不作为第一阶段重点。

阅读顺序：

1. `NetworkData.cs`
2. `Authenticator.cs`
3. `AuthenticatorLocal.cs`
4. `AuthenticatorApi.cs`
5. `ApiClient.cs`
6. `UserData.cs`
7. `MainMenu.cs`
8. `CollectionPanel.cs`

核心链路：

```text
TcgNetwork 初始化认证器
  -> Authenticator.Create(NetworkData.auth_type)
    -> LocalSave 或 Api

LoginMenu
  -> Authenticator.Login / Register
    -> ApiClient.Login / Register
      -> UnityWebRequest
      -> UserData

MainMenu
  -> Authenticator.RefreshLogin
  -> LoadUserData
  -> DeckSelector.SetupUserDeckList
```

学习建议：

- 先不要依赖线上服务器。
- 如果只是学习规则和卡牌，使用本地或离线测试。
- API 只有在学习账号、开包、商城、排行榜时深入。

## 路线 8：新增一个卡牌效果

目标：学会扩展项目，而不是只阅读。

建议步骤：

1. 找一个接近目标的 `Effect*.cs`。
2. 复制其结构，新建一个子类。
3. 设置 `[CreateAssetMenu]`。
4. 重写对应 `DoEffect` 方法。
5. 在 Unity 中创建新的 Effect asset。
6. 创建或修改 Ability asset，把 effect 放进去。
7. 把 ability 放到一张测试卡上。
8. 把测试卡加入 `test_deck`。
9. 运行 `Game` 场景验证。

判断写在哪一层：

- 只是数值、目标、触发时机变化：改资产，不写代码。
- 需要新的效果行为：新增 `EffectData` 子类。
- 需要新的合法性判断：新增 `ConditionData` 子类。
- 需要新的目标筛选：新增 `FilterData` 子类。
- 需要改整场规则：改 `Game` 或 `GameLogic`，要格外小心。

