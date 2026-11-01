# MemMop

一个面向 **Windows** 的内存管理工具。

> **本仓库从零开始，不包含 WinMemoryCleaner 的代码。**
> 前身是 [MemMop-Archived](https://github.com/MiraCeo/MemMop-Archived)（1.x，基于
> [WinMemoryCleaner](https://github.com/IgorMundstein/WinMemoryCleaner) 修改，GPL v3）。

---

## 要做什么

同样底层能力下，**更可信的交互**。

现有工具（WinMemoryCleaner、Mem Reduct）用的是同一批 Windows Native API，能力上差别不大；
而它们都没有解决一个更实际的问题：**用户看不懂自己在做什么、代价是什么、到底有没有效果。**

所以这个项目不做这些：

- 不做"让电脑变快"的承诺 —— 清理待机内存不会增加可用内存
- 不为了功能数量增加选项 —— 说不清代价的操作不做
- 不隐藏风险 —— 每个操作旁必须写明它会让什么变差

要做的是三件可验证的事：

| 标准 | 含义 |
|---|---|
| **效果真实** | 区分状态变化与性能收益，不用好看的数字代替实际效果 |
| **行为透明** | 告诉用户做了什么、为什么做、代价是什么 |
| **控制可靠** | 用户授权范围内的操作不被绕过 |

完整定义见 [docs/SPEC.md](docs/SPEC.md)。

---

## 文档

| 文件 | 内容 |
|---|---|
| [docs/SPEC.md](docs/SPEC.md) | 产品规格：界面结构、设置与默认值、行为规则、指标口径、模块划分 |
| [docs/MEMORY-API.md](docs/MEMORY-API.md) | 底层调用知识：每个内存区域用什么系统调用、代价、已知坑 |
| [docs/PROVENANCE.md](docs/PROVENANCE.md) | 来源记录：哪些是原创、哪些来自公开资料 |
| [docs/EDITING.md](docs/EDITING.md) | 修改纪律与验证流程 |

---

## 状态

**立项阶段。** 代码尚未开始，仓库当前只有文档。

技术基础（待确认）：**.NET 8 + WPF**。

理由：WPF 在现代 .NET 上依然可用，迁移成本最低；界面层与清理引擎分离之后，将来若要换 UI 框架不必牵动底层。

---

## 许可证

待定。本仓库的代码为原创，不继承上游项目的许可证。
