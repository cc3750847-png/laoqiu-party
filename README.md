# 牢球派对

一个受 `Pummel Party` 启发、以 `AI Director + AI Agent` 为核心差异化的多人派对棋盘游戏 Unity 项目。

## 项目目标

- 保留派对棋盘游戏的核心循环：移动、道具、事件、小游戏、胜负结算
- 用 `AI Director` 动态制造局势变化、恩怨和 comeback
- 用 `AI Agent` 提供可控的 NPC / AI 玩家行为，而不是不可控的“黑盒 AI”

当前阶段目标是先完成 `MVP`：

- 1 张地图
- 3 个小游戏原型
- 1 套棋盘主循环
- 1 个导演系统
- 2 种 AI 人格

## 开发环境

- Unity：请使用当前项目对应的版本，见 [ProjectVersion.txt](d:/Unity/Unity%20Project/牢球派对/ProjectSettings/ProjectVersion.txt)
- IDE：Visual Studio Code / Rider / Visual Studio 均可
- Git：建议命令行提交，避免 VS Code 在大型资源变更时卡顿

## Unity 必设项

首次打开项目后请确认：

1. `Edit -> Project Settings -> Editor -> Asset Serialization -> Force Text`
2. `Edit -> Project Settings -> Editor -> Version Control -> Visible Meta Files`

这两项不对，后续 `scene/prefab/meta` 很容易出现无法合并或资源引用错乱。

## Git 规则

项目已配置：

- [.gitignore](d:/Unity/Unity%20Project/牢球派对/.gitignore)
- [.gitattributes](d:/Unity/Unity%20Project/牢球派对/.gitattributes)

### 允许提交的目录

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- 根目录下的工程说明文件

### 不要提交的目录

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `.vs/`
- `.idea/`

如果 `git status` 里又出现这些目录，先检查 `.gitignore` 是否被改坏。

## 提交流程

推荐使用命令行：

```powershell
git status
git add Assets Packages ProjectSettings .gitignore .gitattributes README.md
git commit -m "chore: initialize unity project docs"
```

提交前检查：

- 能进 Unity，不报脚本错误
- 场景资源没有丢引用
- 没把 `Library/Temp/Logs` 加进去

## 分支策略

当前建议用最轻量的分支模型：

- `main`
  - 始终保持可打开、可运行、可继续开发
- `feature/<name>`
  - 新功能开发分支
- `fix/<name>`
  - 修 bug 分支

命名例子：

- `feature/board-loop`
- `feature/director-system`
- `feature/minigame-bomb-tag`
- `fix/tile-event-order`

### 合并原则

- 一个分支只做一类事情
- 不把“规则重构 + UI 改动 + 小游戏内容”混在一个提交里
- 合并回 `main` 前至少保证 Unity 能正常打开

## 提交信息规范

先用简单可执行的约定，不搞复杂流程。

格式：

```text
<type>: <summary>
```

常用类型：

- `feat`: 新功能
- `fix`: 修复问题
- `refactor`: 重构
- `docs`: 文档修改
- `chore`: 工程维护、配置修改
- `test`: 测试相关

例子：

- `feat: add board turn loop`
- `feat: add director comeback event`
- `fix: prevent item use after turn end`
- `refactor: split economy logic from game loop`
- `docs: add project workflow guide`

## 当前推荐开发顺序

1. 棋盘主循环
2. 资源和道具体系
3. 导演事件系统
4. AI 玩家决策
5. 小游戏接入
6. UI 和表现打磨

不要一开始就接大模型、联网或语音系统。先验证主循环和 AI 介入是否真的让局更有趣。

## 目录约定

后续脚本建议按下面结构扩展：

```text
Assets/_Project/
  Scenes/
  Prefabs/
  Scripts/
    Core/
    GameFlow/
    Board/
    Rules/
    Director/
    Agents/
    Minigames/
    UI/
    Presentation/
  ScriptableObjects/
```

原则：

- 规则逻辑和表现层分离
- 不把所有代码塞进 `GameManager`
- ScriptableObject 负责配置，不负责复杂执行逻辑

## 协作注意事项

- 修改 `scene/prefab` 前先拉最新代码
- 大资源导入和系统重构不要混在同一次提交
- 如果 Unity 自动重写大量资源，先确认是否只是序列化设置变了
- 遇到冲突优先保住 `.meta` 文件的一致性

## 下一步

当前仓库刚完成初始化，建议下一步直接开始：

1. 创建 `Assets/_Project/` 目录结构
2. 搭建棋盘主循环脚本骨架
3. 建立第一个可运行的 `Board` 场景
