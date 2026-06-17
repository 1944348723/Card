# 数据驱动卡牌系统

## 总览

这个项目的卡牌系统主要由 ScriptableObject 资产驱动。多数卡牌不需要写新代码，而是通过组合已有数据资产实现。

核心资产链：

```text
CardData
  -> AbilityData[]
    -> ConditionData[]
    -> FilterData[]
    -> EffectData[]
    -> StatusData[]
    -> chain_abilities[]
```

运行时链：

```text
CardData asset
  -> Card.Create(...)
    -> Card runtime instance
      -> 加入 Player 的 deck / hand / board / discard 等区域
      -> GameLogic 执行规则
```

## CardData：卡牌模板

路径：

- 脚本：`Assets/Tcg/Scripts/Data/CardData.cs`
- 资产：`Assets/Tcg/Resources/Cards`

重要字段：

| 字段 | 说明 |
| --- | --- |
| `id` | 卡牌唯一 ID，不允许重复。 |
| `title` | 显示名称。 |
| `art_full` | 完整卡图。 |
| `art_board` | 场上显示图。 |
| `type` | 卡牌类型：Hero、Character、Spell、Artifact、Secret、Equipment。 |
| `team` | 阵营。 |
| `rarity` | 稀有度。 |
| `mana` | 法力消耗。 |
| `attack` | 攻击力。 |
| `hp` | 生命值。 |
| `traits` | 标签型特性。 |
| `stats` | 带数值的特性。 |
| `abilities` | 能力列表。 |
| `text` | 卡面短文本。 |
| `desc` | 详细描述。 |
| `deckbuilding` | 是否可进入构筑。 |
| `packs` | 所属卡包。 |

学习重点：

- `CardData` 不保存对局中的当前伤害、疲劳、位置。
- 对局状态在 `Card` 中。
- `CardData.Get(id)` 从静态字典取模板。

## Card：运行时卡牌实例

路径：

- `Assets/Tcg/Scripts/GameLogic/Card.cs`

重要字段：

| 字段 | 说明 |
| --- | --- |
| `card_id` | 指向 `CardData.id`。 |
| `uid` | 对局内唯一实例 ID。 |
| `player_id` | 所属玩家。 |
| `variant_id` | 卡牌变体。 |
| `slot` | 场上位置。 |
| `exhausted` | 是否疲劳。 |
| `damage` | 已受到的伤害。 |
| `mana/attack/hp` | 当前基础数值。 |
| `*_ongoing` | 持续效果产生的临时数值。 |
| `status` | 状态列表。 |
| `abilities` | 能力 ID 列表。 |

关键区别：

```text
CardData = 卡牌模板，编辑器资产，静态配置
Card = 对局实例，运行时状态，可同步
```

## AbilityData：能力配置

路径：

- 脚本：`Assets/Tcg/Scripts/Data/AbilityData.cs`
- 资产：`Assets/Tcg/Resources/Abilities`

重要字段：

| 字段 | 说明 |
| --- | --- |
| `id` | 能力唯一 ID。 |
| `trigger` | 触发时机。 |
| `conditions_trigger` | 触发条件。 |
| `target` | 目标类型。 |
| `conditions_target` | 目标合法性条件。 |
| `filters_target` | 目标过滤器。 |
| `effects` | 执行效果。 |
| `status` | 附加状态。 |
| `value` | 通用数值。 |
| `duration` | 状态持续时间。 |
| `chain_abilities` | 连锁能力或选项。 |
| `mana_cost` | 主动技能消耗。 |
| `exhaust` | 主动技能是否导致疲劳。 |
| `title/desc` | 显示文本。 |

### 常见 Trigger

| Trigger | 含义 |
| --- | --- |
| `Ongoing` | 持续效果。 |
| `Activate` | 主动技能。 |
| `OnPlay` | 自己被打出时。 |
| `OnPlayOther` | 其他卡被打出时。 |
| `StartOfTurn` | 回合开始。 |
| `EndOfTurn` | 回合结束。 |
| `OnBeforeAttack` | 攻击造成伤害前。 |
| `OnAfterAttack` | 攻击造成伤害后。 |
| `OnBeforeDefend` | 被攻击造成伤害前。 |
| `OnAfterDefend` | 被攻击造成伤害后。 |
| `OnKill` | 击杀时。 |
| `OnDeath` | 自己死亡时。 |
| `OnDeathOther` | 其他卡死亡时。 |

### 常见 Target

| Target | 含义 |
| --- | --- |
| `Self` | 自己。 |
| `PlayerSelf` | 自己的玩家对象。 |
| `PlayerOpponent` | 对手玩家。 |
| `AllPlayers` | 所有玩家。 |
| `AllCardsBoard` | 所有场上卡。 |
| `AllCardsHand` | 所有手牌。 |
| `SelectTarget` | 玩家选择目标。 |
| `CardSelector` | 从某些卡牌中选择。 |
| `ChoiceSelector` | 从连锁能力中选择。 |
| `LastPlayed` | 最近打出的卡。 |
| `LastDestroyed` | 最近被摧毁的卡。 |
| `LastTargeted` | 最近目标。 |
| `LastSummoned` | 最近召唤或创建的卡。 |
| `EquippedCard` | 装备或被装备对象。 |

## ConditionData：条件

路径：

- 脚本基类：`Assets/Tcg/Scripts/Data/ConditionData.cs`
- 子类目录：`Assets/Tcg/Scripts/Conditions`
- 资产目录：`Assets/Tcg/Resources/Conditions`

用途：

- 判断能力是否能触发。
- 判断目标是否合法。
- 限制卡牌类型、阵营、状态、槽位、所有者、数量等。

常见子类：

| 子类 | 用途 |
| --- | --- |
| `ConditionOwner` | 判断目标属于自己、敌方或任意玩家。 |
| `ConditionTarget` | 判断目标类型。 |
| `ConditionCardType` | 判断卡牌类型。 |
| `ConditionStatus` | 判断是否有某状态。 |
| `ConditionTrait` | 判断是否有某特性。 |
| `ConditionStat` | 判断攻击、生命等数值。 |
| `ConditionSlotEmpty` | 判断槽位是否为空。 |
| `ConditionOnce` | 限制一次性触发。 |

## FilterData：目标过滤器

路径：

- 脚本基类：`Assets/Tcg/Scripts/Data/FilterData.cs`
- 子类目录：`Assets/Tcg/Scripts/Conditions`

用途：

- 从合法目标列表中挑选一部分。

常见子类：

| 子类 | 用途 |
| --- | --- |
| `FilterRandom` | 随机目标。 |
| `FilterHighestStat` | 最高某项数值。 |
| `FilterLowestStat` | 最低某项数值。 |
| `FilterFirst` | 第一个。 |
| `FilterLast` | 最后一个。 |

## EffectData：效果

路径：

- 脚本基类：`Assets/Tcg/Scripts/Data/EffectData.cs`
- 子类目录：`Assets/Tcg/Scripts/Effects`
- 资产目录：`Assets/Tcg/Resources/Effects`

用途：

- 真正修改游戏状态。

常见子类：

| 子类 | 用途 |
| --- | --- |
| `EffectDamage` | 造成伤害。 |
| `EffectHeal` | 治疗。 |
| `EffectDraw` | 抽牌。 |
| `EffectDiscard` | 弃牌。 |
| `EffectSummon` | 召唤。 |
| `EffectCreate` | 创建卡牌。 |
| `EffectDestroy` | 摧毁卡牌。 |
| `EffectMana` | 修改法力。 |
| `EffectAddStat` | 增加数值。 |
| `EffectSetStat` | 设置数值。 |
| `EffectAddTrait` | 增加特性。 |
| `EffectAddAbility` | 增加能力。 |
| `EffectTransform` | 变形。 |
| `EffectShuffle` | 洗牌。 |
| `EffectRoll` | 掷骰。 |
| `EffectRepeat` | 重复效果。 |

## StatusData：状态

路径：

- 脚本：`Assets/Tcg/Scripts/Data/StatusData.cs`
- 资产：`Assets/Tcg/Resources/Status`

常见状态含义：

- `Stealth`：潜行，不能被指定或攻击。
- `SpellImmunity`：法术免疫。
- `Protected`：保护。
- `Flying`：飞行。
- `Fury`：可能影响多次攻击。
- `Poisoned`：中毒。
- `Sleep`：睡眠或不能行动。

实际状态枚举以 `StatusData.cs` 为准。

## 新增卡牌的推荐步骤

### 不写代码的新增卡

适合：只是换数值、换图、复用已有能力。

1. 在 `Resources/Cards` 中复制一张相近卡牌。
2. 修改 `id`，确保全局唯一。
3. 修改 `title`、`mana`、`attack`、`hp`、`text`。
4. 挂载已有 `AbilityData`。
5. 设置 `deckbuilding` 和 `packs`。
6. 加入一个测试 deck。
7. 运行对局验证。

### 需要新效果的卡

适合：现有 `Effect*.cs` 无法表达目标行为。

1. 在 `Effects` 目录找最相近的子类。
2. 新增一个 `EffectData` 子类。
3. 实现需要的 `DoEffect` 重载。
4. 添加 `[CreateAssetMenu]`。
5. 在 Unity 中创建对应 effect asset。
6. 创建或修改 ability asset 引用它。
7. 让卡牌引用这个 ability。
8. 加入测试卡组验证。

### 需要新条件的卡

适合：目标合法性或触发条件无法表达。

1. 在 `Conditions` 目录找相近子类。
2. 新增一个 `ConditionData` 子类。
3. 实现触发条件或目标条件方法。
4. 创建 condition asset。
5. 在 ability 的 `conditions_trigger` 或 `conditions_target` 引用。

## 卡牌问题排查

### 卡牌没有加载

检查：

- 是否在 `Assets/Tcg/Resources` 下。
- `id` 是否为空。
- `id` 是否重复。
- `DataLoader.CheckCardData` 是否报错。

### 卡牌不能出

检查：

- 当前是否轮到该玩家。
- 是否在手牌中。
- 法力是否足够。
- 目标槽位是否有效。
- 是否打到了自己的合法区域。
- 装备目标是否是己方角色。
- 法术目标是否通过 `IsPlayTargetValid`。

### 能力不触发

检查：

- 卡牌是否引用了能力。
- `trigger` 是否正确。
- `conditions_trigger` 是否满足。
- `ConditionOnce` 是否限制了重复触发。
- 是否处在正确阶段。

### 能力触发但没效果

检查：

- `target` 是否正确。
- `conditions_target` 是否过严。
- `filters_target` 是否过滤掉所有目标。
- `effects` 是否为空。
- `EffectData.DoEffect` 重载是否覆盖了对应目标类型。

### UI 显示不对

检查：

- `CardUI` 是否读取了正确字段。
- `CardData.text` 和 `AbilityData.desc` 是否更新。
- Prefab 是否绑定了正确组件。
- 对局中的 `Card` 当前数值是否被 ongoing 效果影响。

