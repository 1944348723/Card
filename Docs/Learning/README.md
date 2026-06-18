# Card 项目学习文档

这个目录用于系统学习当前 Unity TCG 项目。建议先读 `01-study-plan.md`，再按任务需要查阅其他文档。

## 文档索引

- `01-study-plan.md`：详细学习计划、时间估算、阶段目标、每日安排。
- `02-project-map.md`：项目结构地图、关键目录、关键脚本职责。
- `03-code-reading-routes.md`：按业务流程阅读代码，包括出牌、攻击、能力结算、菜单进入对局。
- `04-data-driven-cards.md`：ScriptableObject 数据驱动卡牌系统说明。
- `05-exercises-and-checklists.md`：练习任务、验收清单、调试方法、术语表。

## 推荐学习顺序

1. 先跑通 Unity 项目，确认能进入单人或测试对局。
2. 读数据层：`DataLoader`、`CardData`、`AbilityData`、`GameplayData`。
3. 读纯规则层：`Game`、`Player`、`Card`、`GameLogic`。
4. 读一条完整行为链：玩家点击出牌，客户端发送动作，服务器验证并执行，客户端刷新表现。
5. 再读 UI、菜单、API、联机和 AI。

## 学习目标分级

- 入门目标：能改卡牌数值、卡组配置、基础规则参数。
- 维护目标：能定位对局 bug，新增简单卡牌能力，调整 UI 表现。
- 深入目标：能改联机流程、AI 行为、账号/API、复杂结算规则。

## 个人学习总结

`Resources/`下全是配置文件，`AssetData`是特效音效、`GameplayData`是全局通用游戏配置、`NetworkData`是网络配置，这三个是**全局配置**，剩下的目录中的都是**游戏内容数据**

**目录划分**

* **AI**: AI实现
* **Api**: 注册、登录、查询玩家数据、匹配等网络请求相关内容
* **Conditions**: 条件判断和过滤器
* **Data**: SO数据定义和加载
* **Effects**: 各种效果
* **FX**：特效、动画实现
* **GameClient**: 客户端视觉效果、逻辑
* **GameLogic**: 游戏逻辑
* **GameServer**: 服务端代码
* **Menu**: 各种面板
* **Network**: 网络相关
* **Tools**: 相对独立或较杂的代码
* **UI**: UI代码，暂时不知道和Menu区别在哪

### 思考
本项目的`ScriptableObject`配置文件好像不是在运行后加载转换为内存对象的单纯的配置文件，而是直接在游戏流程中使用的，提供了一些查询、判断的函数。在这种情况下，所有的字段都是`public`的，容易在运行时误修改，可能直接影响到配置。