# 项目状态快照（牢球派对）

> 用途：防止上下文压缩后“失忆”。每个阶段结束都更新一次本文件。

## 1. 当前目标
- 做出一个可展示的棋盘派对 Demo：回合循环稳定、玩家可操作、AI Agent 可介入决策、画面达到“可演示”水准。

## 2. 当前进度（截至 2026-03-12）
- 已有可运行的回合主循环（掷骰、移动、落点结算、回合推进、终局）。
- 已支持分叉路径与基础路径选择流程。
- 已有 Director 事件与道具决策流（原型级）。
- 已搭建 HUD 与棋盘可视化原型（正在重构展示层）。
- 已推送远端仓库：`origin/main`。

## 3. 最近关键提交
- `ea67fb9` feat: add sample board scene bootstrap and runtime board components
- `9e96aab` refactor: add unified action flow for roll path and shop decisions
- `e2cf3c0` feat: add initial gameplay architecture skeleton

## 4. 当前工作区（未提交）
> 来源：`git status --short`

- Modified:
  - `Assets/_Project/Scripts/Agents/Runtime/AgentBrain.cs`
  - `Assets/_Project/Scripts/Board/Runtime/BoardMovementSystem.cs`
  - `Assets/_Project/Scripts/Board/Runtime/BoardTile.cs`
  - `Assets/_Project/Scripts/Director/Runtime/DirectorSystem.cs`
  - `Assets/_Project/Scripts/GameFlow/Actions/ActionExecutor.cs`
  - `Assets/_Project/Scripts/GameFlow/Actions/GameActionType.cs`
  - `Assets/_Project/Scripts/GameFlow/Controllers/GameLoopController.cs`
  - `Assets/_Project/Scripts/GameFlow/Data/InventoryState.cs`
  - `Assets/_Project/Scripts/GameFlow/Data/MatchState.cs`
  - `Assets/_Project/Scripts/Rules/Tiles/TileEffectResolver.cs`
  - `Assets/_Project/Scripts/Tools/Runtime/SampleSceneAutoSetup.cs`
  - `Assets/_Project/Scripts/UI/Hud/HudController.cs`
  - `ProjectSettings/QualitySettings.asset`
- Untracked:
  - `Assets/_Project/Scripts/Board/Data/BoardLayoutDefinition.cs`
  - `Assets/_Project/Scripts/Board/Runtime/ActivePawnHighlightPresenter.cs`
  - `Assets/_Project/Scripts/Tools/Runtime/BoardPrototypeAutoSetup.cs`

## 5. 当前问题与风险
- 展示层在持续重构中，Unity Inspector 引用可能出现断链，需要逐项回归检查。
- 字体资源存在兼容问题风险（内置 Arial 在部分环境不可用），需要统一字体资产策略。
- 当前“可玩感”仍偏弱：虽然有流程，但玩家主动操作反馈还不够强。

## 6. 下一步（按优先级）
- P0：稳定展示层（HUD 布局、面板样式、按钮可见性与交互回归）。
- P0：修复所有 Console 报错/警告到可演示状态。
- P1：强化玩家可控环节（道具使用、分叉选择、关键确认反馈）。
- P1：统一棋盘视觉语言（连接件、分区、分叉提示、当前玩家底部指示）。
- P2：补 AI Agent 决策表现层（决策原因短文本、行为风格差异化）。

## 7. 这份文件怎么用
每次准备结束一段工作时，按下面模板更新：

```md
## 阶段更新（YYYY-MM-DD HH:mm）
- 本阶段完成：
- 新增问题：
- 代码改动范围：
- 是否已提交：
- 下一步第一件事：
```

## 8. 续聊快捷指令（直接复制给 Codex）
- `先读取 docs/STATUS.md，然后继续执行“下一步第一件事”。`
- `基于 docs/STATUS.md，给我今天 90 分钟可完成的任务清单。`
- `更新 docs/STATUS.md：写入你刚刚完成的改动和下一步。`
